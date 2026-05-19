using FastEndpoints;
using FastEndpoints.A2A;
using ApiBackend.Infrastructure;
using ApiBackend.Services;
using System.Net.Http.Headers;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var settings = ServiceSettings.Create("api_backend", 5056, "http://api-backend:5056");

if (string.IsNullOrWhiteSpace(settings.JwtSecretKey) || settings.JwtSecretKey.Length < 32)
{
    throw new InvalidOperationException(
        "JWT_SECRET_KEY environment variable must be set and at least 32 characters long");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddSingleton(settings);
builder.Services.AddHttpClient();
builder.Services.AddSingleton(new JwtService(settings.JwtSecretKey));

builder.Services.AddFastEndpoints();
builder.Services.AddA2A(options =>
{
    options.AgentName = "api-orchestrator";
    options.Description = "A2A orchestration agent that coordinates triage workflow";
    options.Version = "1.0.0";
    options.Url = $"{settings.ServiceBaseUrl}/a2a";
    options.SkillVisibilityFilter = (endpoint, principal, context) => 
    {
        var authHeader = context.Request.Headers.Authorization.ToString();
        return !string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);
    };
});

var app = builder.Build();

app.UseCors("AllowAll");

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "";
    
    if (path == "/health" || path.StartsWith("/.well-known") || path == "/api/triage")
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

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "api-backend" }));

app.MapPost("/api/triage", async (TriageRequest req, IHttpClientFactory httpClientFactory, ServiceSettings svc, CancellationToken ct) =>
{
    var input = (req.Input ?? "").Trim();
    if (string.IsNullOrEmpty(input))
    {
        return Results.BadRequest(new { error = "Input is required" });
    }

    var bearerToken = "test-token";
    var client = httpClientFactory.CreateClient();
    var steps = new List<(string, string)>();

    try
    {
        var classifyResult = await A2AHelper.CallServiceAsync(client, "http://classifier:5052", "classify-text", new { text = input }, bearerToken, ct);
        var classification = classifyResult["classification"]?.ToString() ?? "unknown";
        steps.Add(("Classifier", classification));

        var assessResult = await A2AHelper.CallServiceAsync(client, "http://assessor:5053", "assess-priority", new { classification }, bearerToken, ct);
        var priority = assessResult["priority"]?.ToString() ?? "medium";
        steps.Add(("Assessor", priority));

        var routeResult = await A2AHelper.CallServiceAsync(client, "http://router:5054", "route-incident", new { priority }, bearerToken, ct);
        var team = routeResult["team"]?.ToString() ?? "ops-standard";
        steps.Add(("Router", team));

        var handleResult = await A2AHelper.CallServiceAsync(client, "http://handler:5055", "create-ticket", new { subject = input, team }, bearerToken, ct);
        var ticketId = handleResult["ticketId"]?.ToString() ?? $"TKT-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        steps.Add(("Handler", ticketId));

        var result = new TriageWorkflowResult(
            Id: $"triage-{Guid.NewGuid():N}"[..19],
            Input: input,
            Status: "completed",
            Steps: steps,
            Summary: $"Classified as {classification}, assessed {priority}, routed to {team}, ticket {ticketId} created"
        );

        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        var result = new TriageWorkflowResult(
            Id: $"triage-{Guid.NewGuid():N}"[..19],
            Input: input,
            Status: "error",
            Steps: steps,
            Error: ex.Message
        );

        return Results.Ok(result);
    }
});

app.UseFastEndpoints();
app.UseA2A();

app.Run();

record TriageRequest(string? Input);

record TriageWorkflowResult(string Id, string Input, string Status, List<(string, string)> Steps, string? Summary = null, string? Error = null);

static class A2AHelper
{
    public static async Task<Dictionary<string, object>> CallServiceAsync(HttpClient client, string baseUrl, string skillId, object parameters, string bearerToken, CancellationToken ct)
    {
        var rpcRequest = new
        {
            jsonrpc = "2.0",
            id = Guid.NewGuid().ToString(),
            method = "SendMessage",
            @params = new
            {
                message = new
                {
                    messageId = Guid.NewGuid().ToString(),
                    role = "ROLE_USER",
                    parts = new[] { new { data = parameters } }
                },
                metadata = new { skill = skillId }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/a2a");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        request.Content = JsonContent.Create(rpcRequest);

        using var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(jsonResponse);
        var resultElement = doc.RootElement.GetProperty("result");
        var messageElement = resultElement.GetProperty("message");
        var partsElement = messageElement.GetProperty("parts");
        var firstPart = partsElement[0];
        var dataElement = firstPart.GetProperty("data");

        return JsonSerializer.Deserialize<Dictionary<string, object>>(dataElement.GetRawText()) ?? new();
    }
}
