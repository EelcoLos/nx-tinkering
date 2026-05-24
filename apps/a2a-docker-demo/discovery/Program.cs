using A2ADemo.Common;
using A2ADemo.Discovery;
using FastEndpoints;

var builder = WebApplication.CreateBuilder(args);
var settings = ServiceSettings.Create();

builder.Services.AddSingleton(settings);
builder.Services.AddSingleton<IIdentityServiceSettings>(settings);
builder.Services.AddHttpClient();
builder.Services.AddSingleton(new JwtService(settings.JwtSecretKey));
builder.Services.AddSingleton<IdentityClient>();
builder.Services.AddSingleton<RequestAuthorizer>();
builder.Services.AddSingleton<ServiceRegistry>();
builder.Services.AddAuthorization();
builder.Services.AddFastEndpoints();
builder.Services.AddServiceTelemetry(
    settings.OtelEnabled,
    settings.ServiceName,
    settings.OtelServiceNamespace,
    settings.OtelExporterEndpoint,
    settings.ServiceName);

var app = builder.Build();

app.Services.GetRequiredService<ServiceRegistry>().SeedSelf();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/health"))
    {
        await next();
        return;
    }

    if (context.Request.Path.StartsWithSegments("/services") || context.Request.Path.StartsWithSegments("/register"))
    {
        var authorizer = context.RequestServices.GetRequiredService<RequestAuthorizer>();
        var validatedToken = await authorizer.ValidateBearerAsync(context, "agent", context.RequestAborted);
        if (validatedToken is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid or expired token" }, cancellationToken: context.RequestAborted);
            return;
        }

        context.User = RequestAuthorizer.CreatePrincipal(validatedToken);
        context.Items["validated_token"] = validatedToken;
    }

    await next();
});

app.UseAuthorization();
app.UseFastEndpoints();
app.Run();