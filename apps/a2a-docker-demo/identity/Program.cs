using FastEndpoints;
using FastEndpoints.A2A;
using Identity.Infrastructure;
using Identity.Services;

var builder = WebApplication.CreateBuilder(args);
var settings = ServiceSettings.Create("identity", 5050, "http://identity:5050");

if (string.IsNullOrWhiteSpace(settings.JwtSecretKey) || settings.JwtSecretKey.Length < 32)
{
    throw new InvalidOperationException(
        "JWT_SECRET_KEY environment variable must be set and at least 32 characters long");
}

builder.Services.AddSingleton(settings);
builder.Services.AddSingleton(new JwtService(settings.JwtSecretKey));

builder.Services.AddFastEndpoints();
builder.Services.AddA2A(options =>
{
    options.AgentName = "identity-provider";
    options.Description = "Identity provider for issuing agent credentials";
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
    
    if (path == "/health" || path.StartsWith("/.well-known") || path == "/api/token")
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

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "identity" }));

app.MapPost("/api/token", (JwtService jwt) =>
{
    var token = jwt.IssueAgentToken("api-orchestrator", new[] { "triage:execute" });
    return Results.Ok(new { access_token = token, token_type = "Bearer", expires_in = 3600 });
});

app.UseFastEndpoints();
app.UseA2A();

app.Run();
