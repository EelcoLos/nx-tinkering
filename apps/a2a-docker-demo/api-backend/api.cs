#:sdk Microsoft.NET.Sdk.Web
#:package FastEndpoints@8.*-*
#:package A2A@1.*-*
#:package System.IdentityModel.Tokens.Jwt@7.*
#:package Microsoft.IdentityModel.Tokens@7.*
#:property ManagePackageVersionsCentrally=false

using A2A;
using FastEndpoints;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

const string ApiHostUrl = "http://localhost:5056";
const string IdentityServiceUrl = "http://localhost:5050";
const string ClassifierServiceUrl = "http://localhost:5052";
const string AssessorServiceUrl = "http://localhost:5053";
const string RouterServiceUrl = "http://localhost:5054";
const string HandlerServiceUrl = "http://localhost:5055";

var bld = WebApplication.CreateBuilder(args);
bld.WebHost.UseUrls(ApiHostUrl);

var jwtSecret = bld.Configuration["JWT_SECRET_KEY"] ?? throw new InvalidOperationException("JWT_SECRET_KEY not configured");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
var jwtValidator = new JwtValidator(signingKey);

bld.Services.AddSingleton(jwtValidator);
bld.Services.AddCors(o => o.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
bld.Services.AddFastEndpoints();

var app = bld.Build();
app.UseCors("AllowAll");

// JWT validation middleware
app.Use(async (context, next) =>
{
    var path = context.Request.Path.ToString();

    if (path == "/health" || path == "/api/auth/login")
    {
        await next();
        return;
    }

    var authHeader = context.Request.Headers["Authorization"].ToString();
    if (string.IsNullOrWhiteSpace(authHeader))
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsJsonAsync(new { error = "Missing authorization header" });
        return;
    }

    var token = authHeader.Replace("Bearer ", "").Trim();
    var claims = jwtValidator.ValidateToken(token);

    if (claims == null)
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsJsonAsync(new { error = "Invalid or expired token" });
        return;
    }

    context.Items["jwt_claims"] = claims;
    await next();
});

app.UseFastEndpoints();
app.Run();

// ============ Endpoints ============

sealed class LoginRequest
{
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("password")]
    public string? Password { get; set; }
}

sealed class LoginResponse
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("expiresIn")]
    public int ExpiresIn { get; set; }
}

sealed class TriageRequest
{
    [JsonPropertyName("input")]
    public string? Input { get; set; }
}

sealed class TriageResponse
{
    [JsonPropertyName("ticketId")]
    public string? TicketId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("steps")]
    public List<TriageStep>? Steps { get; set; }

    [JsonPropertyName("finalResult")]
    public string? FinalResult { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

sealed class TriageStep
{
    [JsonPropertyName("service")]
    public string? Service { get; set; }

    [JsonPropertyName("action")]
    public string? Action { get; set; }

    [JsonPropertyName("result")]
    public string? Result { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}

sealed class HealthResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "healthy";

    [JsonPropertyName("service")]
    public string Service { get; set; } = "api";
}

sealed class LoginEndpoint : Endpoint<LoginRequest, LoginResponse>
{
    public override void Configure()
    {
        Post("/api/auth/login");
        AllowAnonymous();
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var response = await client.PostAsJsonAsync($"{IdentityServiceUrl}/auth/login", req, ct);

        if (!response.IsSuccessStatusCode)
        {
            await SendAsync(new { error = "Invalid credentials" }, statusCode: 401, cancellation: ct);
            return;
        }

        var result = await response.Content.ReadAsAsync<LoginResponse>(ct);
        await SendAsync(result, cancellation: ct);
    }
}

sealed class SubmitTriageEndpoint : Endpoint<TriageRequest, TriageResponse>
{
    public override void Configure()
    {
        Post("/api/triage");
    }

    public override async Task HandleAsync(TriageRequest req, CancellationToken ct)
    {
        var ticketId = $"TRG-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        var steps = new List<TriageStep>();
        var input = req.Input ?? "general request";

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

            // Step 1: Classify
            var step1 = await CallService(client, 
                ClassifierServiceUrl + "/skills/classify",
                new { input },
                "classifier",
                "Classify request",
                ct);
            steps.Add(step1);

            if (!step1.Success)
            {
                var response = new TriageResponse
                {
                    TicketId = ticketId,
                    Status = "failed",
                    Steps = steps,
                    Error = "Classification failed",
                };
                await SendAsync(response, statusCode: 500, cancellation: ct);
                return;
            }

            var classifyData = step1.Result ?? "{}";
            var classifyJson = JsonSerializer.Deserialize<JsonElement>(classifyData);
            
            // Step 2: Assess
            var assessInput = new
            {
                input,
                classification = classifyJson.TryGetProperty("classification", out var c) ? c.GetString() : "general",
                urgency = classifyJson.TryGetProperty("urgency", out var u) ? u.GetString() : "normal"
            };

            var step2 = await CallService(client,
                AssessorServiceUrl + "/skills/assess",
                assessInput,
                "assessor",
                "Assess priority",
                ct);
            steps.Add(step2);

            if (!step2.Success)
            {
                var response = new TriageResponse
                {
                    TicketId = ticketId,
                    Status = "failed",
                    Steps = steps,
                    Error = "Assessment failed",
                };
                await SendAsync(response, statusCode: 500, cancellation: ct);
                return;
            }

            var assessData = step2.Result ?? "{}";
            var assessJson = JsonSerializer.Deserialize<JsonElement>(assessData);

            // Step 3: Route
            var routeInput = new
            {
                input,
                priority = assessJson.TryGetProperty("priority", out var p) ? p.GetInt32() : 3,
                priorityLabel = assessJson.TryGetProperty("priorityLabel", out var pl) ? pl.GetString() : "Normal",
                assignedTo = assessJson.TryGetProperty("assignedTo", out var a) ? a.GetString() : "standard_handler"
            };

            var step3 = await CallService(client,
                RouterServiceUrl + "/skills/route",
                routeInput,
                "router",
                "Route to handler",
                ct);
            steps.Add(step3);

            if (!step3.Success)
            {
                var response = new TriageResponse
                {
                    TicketId = ticketId,
                    Status = "failed",
                    Steps = steps,
                    Error = "Routing failed",
                };
                await SendAsync(response, statusCode: 500, cancellation: ct);
                return;
            }

            var routeData = step3.Result ?? "{}";
            var routeJson = JsonSerializer.Deserialize<JsonElement>(routeData);

            // Step 4: Handle
            var handleInput = new
            {
                input,
                route = routeJson.TryGetProperty("route", out var r) ? r.GetString() : "standard_queue",
                handlerQueue = routeJson.TryGetProperty("handlerQueue", out var hq) ? hq.GetString() : "handler_standard",
                priority = routeJson.TryGetProperty("priority", out var pri) ? pri.GetInt32() : 3
            };

            var step4 = await CallService(client,
                HandlerServiceUrl + "/skills/handle",
                handleInput,
                "handler",
                "Create ticket",
                ct);
            steps.Add(step4);

            var finalResponse = new TriageResponse
            {
                TicketId = ticketId,
                Status = step4.Success ? "completed" : "partial",
                Steps = steps,
                FinalResult = step4.Result,
            };

            await SendAsync(finalResponse, statusCode: step4.Success ? 200 : 206, cancellation: ct);
        }
        catch (Exception ex)
        {
            var response = new TriageResponse
            {
                TicketId = ticketId,
                Status = "error",
                Steps = steps,
                Error = ex.Message,
            };

            await SendAsync(response, statusCode: 500, cancellation: ct);
        }
    }

    private static async Task<TriageStep> CallService(
        HttpClient client,
        string url,
        object payload,
        string serviceName,
        string action,
        CancellationToken ct)
    {
        var step = new TriageStep
        {
            Service = serviceName,
            Action = action,
            Timestamp = DateTime.UtcNow,
            Success = false,
        };

        try
        {
            var response = await client.PostAsJsonAsync(url, payload, ct);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(ct);
                step.Result = content;
                step.Success = true;
            }
            else
            {
                step.Result = $"HTTP {response.StatusCode}";
                step.Success = false;
            }
        }
        catch (Exception ex)
        {
            step.Result = $"Error: {ex.Message}";
            step.Success = false;
        }

        return step;
    }
}

sealed class HealthEndpoint : EndpointWithoutRequest<HealthResponse>
{
    public override void Configure()
    {
        Get("/health");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await SendAsync(new HealthResponse(), cancellation: ct);
    }
}

// ============ Services ============

sealed class JwtValidator
{
    private readonly SymmetricSecurityKey _signingKey;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    public JwtValidator(SymmetricSecurityKey signingKey)
    {
        _signingKey = signingKey;
    }

    public IEnumerable<System.Security.Claims.Claim>? ValidateToken(string token)
    {
        try
        {
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _signingKey,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            };

            var principal = _tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal.Claims;
        }
        catch
        {
            return null;
        }
    }
}
