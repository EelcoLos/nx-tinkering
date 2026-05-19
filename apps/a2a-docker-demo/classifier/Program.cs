using FastEndpoints;
using FastEndpoints.A2A;
using Classifier.Infrastructure;
using Classifier.Services;

var builder = WebApplication.CreateBuilder(args);
var settings = ServiceSettings.Create("classifier", 5052, "http://classifier:5052");

// Validate JWT secret key at startup
if (string.IsNullOrWhiteSpace(settings.JwtSecretKey) || settings.JwtSecretKey.Length < 32)
{
    throw new InvalidOperationException(
        "JWT_SECRET_KEY environment variable must be set and at least 32 characters long");
}

// Services
builder.Services.AddSingleton(settings);
builder.Services.AddHttpClient();
builder.Services.AddSingleton(new JwtService(settings.JwtSecretKey));

// FastEndpoints with A2A protocol
builder.Services.AddFastEndpoints();
builder.Services.AddA2A(options =>
{
    options.AgentName = "text-classifier";
    options.Description = "A2A agent that classifies text into categories";
    options.Version = "1.0.0";
    options.Url = $"{settings.ServiceBaseUrl}/a2a";
    // Show skills to any authenticated caller (anyone with a Bearer token)
    options.SkillVisibilityFilter = (endpoint, principal, context) => 
    {
        var authHeader = context.Request.Headers.Authorization.ToString();
        return !string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);
    };
});

var app = builder.Build();

// Allow health and discovery endpoints without auth
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "";
    
    // Public endpoints
    if (path == "/health" || path.StartsWith("/.well-known"))
    {
        await next();
        return;
    }

    // A2A endpoint requires Bearer token
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
app.UseA2A();  // Enable A2A JSON-RPC endpoint at /a2a and agent card at /.well-known/agent-card.json

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "classifier" }));

app.Run();
