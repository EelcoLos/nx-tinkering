using A2ADemo.ApiBackend;

var builder = WebApplication.CreateBuilder(args);
var settings = builder.Services.AddConfiguredToolServiceSettings<ServiceSettings>(ServiceSettings.Configure);

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
builder.Services.AddToolServiceA2A(settings, "Website-facing A2A triage orchestrator.");
builder.Services.AddExceptionHandler<TriageExceptionHandler>();
builder.Services.AddSingleton<TriageStore>();
builder.Services.AddSingleton<AuthenticationGateway>();
builder.Services.AddSingleton<DownstreamGateway>();

var app = builder.Build();

app.UseCors("AllowAll");
app.UseExceptionHandler();
app.Use(async (context, next) =>
{
  if (context.Request.Path.StartsWithSegments("/health")
      || context.Request.Path.StartsWithSegments("/auth/login")
      || context.Request.Path.StartsWithSegments("/api/auth/login"))
  {
    await next();
    return;
  }

  if (context.Request.Path.StartsWithSegments("/a2a")
      || context.Request.Path.StartsWithSegments("/.well-known"))
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

    context.User = RequestAuthorizer.CreatePrincipal(validatedToken);
    context.Items["validated_token"] = validatedToken;
  }

  await next();
});

app.UseAuthorization();
app.UseFastEndpoints();
app.UseA2A(rpcPattern: "/a2a", agentCardPattern: "/.well-known/agent-card.json");
app.Run();
