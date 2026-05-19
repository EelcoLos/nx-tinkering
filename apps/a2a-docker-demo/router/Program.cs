using FastEndpoints;
using FastEndpoints.A2A;
using Router.Infrastructure;
using Router.Services;

var builder = WebApplication.CreateBuilder(args);
var settings = ServiceSettings.Create("router", 5054, "http://router:5054");

if (string.IsNullOrWhiteSpace(settings.JwtSecretKey) || settings.JwtSecretKey.Length < 32)
{
    throw new InvalidOperationException(
        "JWT_SECRET_KEY environment variable must be set and at least 32 characters long");
}

builder.Services.AddSingleton(settings);
builder.Services.AddHttpClient();
builder.Services.AddSingleton(new JwtService(settings.JwtSecretKey));

builder.Services.AddFastEndpoints();
builder.Services.AddA2A(options =>
{
    options.AgentName = "incident-router";
    options.Description = "A2A agent that routes incidents to appropriate teams";
    options.Version = "1.0.0";
    options.Url = $"{settings.ServiceBaseUrl}/a2a";
    options.SkillVisibilityFilter = (endpoint, principal, context) => 
    {
        var authHeader = context.Request.Headers.Authorization.ToString();
        return !string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);
    };
});

var app = builder.Build();

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "";
    
    if (path == "/health" || path.StartsWith("/.well-known"))
    {
        await next();
        return;
    }

    if (path == "/a2a")
    {
        var authHeader = context.Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
            return;
        }
    }

    await next();
});

app.UseFastEndpoints();
app.UseA2A();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "router" }));

app.Run();
