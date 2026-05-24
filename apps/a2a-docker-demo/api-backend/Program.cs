using A2ADemo.ApiBackend;
using A2ADemo.Common;
using FastEndpoints;

var builder = WebApplication.CreateBuilder(args);
var settings = ServiceSettings.Create();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddToolServiceInfrastructure(settings, DemoTelemetry.ActivitySourceName);
builder.Services.AddSingleton<ServiceRegistrar>();
builder.Services.AddSingleton<TriageStore>();
builder.Services.AddSingleton<AuthenticationGateway>();
builder.Services.AddSingleton<DownstreamGateway>();

var app = builder.Build();

app.UseCors("AllowAll");
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/health")
        || context.Request.Path.StartsWithSegments("/auth/login")
        || context.Request.Path.StartsWithSegments("/api/auth/login"))
    {
        await next();
        return;
    }

    if (context.Request.Path.StartsWithSegments("/api/services") || context.Request.Path.StartsWithSegments("/api/triage"))
    {
        var authorizer = context.RequestServices.GetRequiredService<RequestAuthorizer>();
        var validatedToken = await authorizer.ValidateBearerAsync(context, "user", context.RequestAborted);
        if (validatedToken is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid or expired token" }, cancellationToken: context.RequestAborted);
            return;
        }

        context.Items["validated_token"] = validatedToken;
    }

    await next();
});

app.UseFastEndpoints();
app.RegisterToolServiceOnStarted<ServiceRegistrar>();
app.Run();