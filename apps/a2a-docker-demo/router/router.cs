#:sdk Microsoft.NET.Sdk.Web
#:package FastEndpoints@8.*-*
#:package FastEndpoints.A2A@1.0.0-beta.1
#:package A2A@1.*-*
#:package System.IdentityModel.Tokens.Jwt@7.*
#:package Microsoft.IdentityModel.Tokens@7.*
#:property ManagePackageVersionsCentrally=false
using A2A;
using FastEndpoints;
using FastEndpoints.A2A;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var doc = JsonDocument.Parse(json);



const string RouterHostUrl = "http://localhost:5054";
const string IdentityServiceUrl = "http://localhost:5050";

var bld = WebApplication.CreateBuilder(args);
bld.WebHost.UseUrls(RouterHostUrl);
bld.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.TypeInfoResolverChain.Add(new DefaultJsonTypeInfoResolver()));

var jwtSecret = bld.Configuration["JWT_SECRET_KEY"] ?? throw new InvalidOperationException("JWT_SECRET_KEY not configured");
var agentId = bld.Configuration["ROUTER_AGENT_ID"] ?? "router_agent";
var agentSecret = bld.Configuration["ROUTER_AGENT_SECRET"] ?? "default-secret-router";

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
var jwtValidator = new JwtValidator(signingKey);
var agentJwtProvider = new AgentJwtProvider(agentId, agentSecret, IdentityServiceUrl);

bld.Services.AddSingleton(jwtValidator);
bld.Services.AddSingleton(agentJwtProvider);

bld.Services
   .AddFastEndpoints()
   .AddA2A(o =>
   {
       o.AgentName = "fe-router-agent";
       o.Description = "Router specialist agent - routes requests to appropriate handlers based on priority.";
       o.Version = "1.0.0";
       o.SkillVisibilityFilter = (_, _, _) => true;
   });

var app = bld.Build();

app.Use(async (context, next) =>
{
    var path = context.Request.Path.ToString();

    if (path == "/health" || path == "/.well-known/agent-card.json")
    {
        await next();
        return;
    }

    if (!path.StartsWith("/a2a"))
    {
        var authHeader = context.Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrWhiteSpace(authHeader))
        {
            var token = authHeader.Replace("Bearer ", "").Trim();
            var claims = jwtValidator.ValidateToken(token);

            if (claims == null)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid or expired token" });
                return;
            }

            context.Items["jwt_claims"] = claims;
        }
    }

    await next();
});

app.UseFastEndpoints()
    .UseA2A(rpcPattern: "/a2a", agentCardPattern: "/.well-known/agent-card.json");


// ============ Endpoints ============






// ============ Services ============

sealed class RouterRequest
{
    [JsonPropertyName("input")]
    public string? Input { get; set; }

    [JsonPropertyName("priority")]
    public int Priority { get; set; }

    [JsonPropertyName("priorityLabel")]
    public string? PriorityLabel { get; set; }

    [JsonPropertyName("assignedTo")]
    public string? AssignedTo { get; set; }
}

sealed class RouterResponse
{
    [JsonPropertyName("result")]
    public string? Result { get; set; }

    [JsonPropertyName("route")]
    public string? Route { get; set; }

    [JsonPropertyName("handlerQueue")]
    public string? HandlerQueue { get; set; }

    [JsonPropertyName("estimatedWaitTime")]
    public int EstimatedWaitTime { get; set; }
}

sealed class HealthResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "healthy";

    [JsonPropertyName("service")]
    public string Service { get; set; } = "router";
}

sealed class RouterSkillEndpoint : Endpoint<RouterRequest, RouterResponse>
{
    public override void Configure()
    {
        Post("/skills/route");
        AllowAnonymous();

        this.A2ASkill(
            id: "router_skill",
            tags: ["triage", "route"],
            configure: skill =>
            {
                skill.Name = "Router";
                skill.Description = "Routes requests to appropriate handlers based on priority level. Determines queue and estimated wait time.";
                skill.Examples = ["Route this critical issue to the urgent queue"];
                skill.InputModes = ["application/json"];
                skill.OutputModes = ["application/json"];
            });
    }

    public override async Task HandleAsync(RouterRequest req, CancellationToken ct)
    {
        var priority = req.Priority;
        var assignedTo = req.AssignedTo ?? "standard_handler";

        string route;
        string handlerQueue;
        int estimatedWaitTime;

        if (priority >= 4)
        {
            route = "urgent_queue";
            handlerQueue = "handler_urgent";
            estimatedWaitTime = 5;
        }
        else if (priority == 3)
        {
            route = "standard_queue";
            handlerQueue = "handler_standard";
            estimatedWaitTime = 15;
        }
        else
        {
            route = "low_priority_queue";
            handlerQueue = "handler_background";
            estimatedWaitTime = 60;
        }

        var response = new RouterResponse
        {
            Result = $"Routed to {route} (estimated wait: {estimatedWaitTime}min)",
            Route = route,
            HandlerQueue = handlerQueue,
            EstimatedWaitTime = estimatedWaitTime,
        };

        await Send.OkAsync(response, ct);
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

sealed class AgentJwtProvider
{
    private readonly string _agentId;
    private readonly string _agentSecret;
    private readonly string _identityServiceUrl;
    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public AgentJwtProvider(string agentId, string agentSecret, string identityServiceUrl)
    {
        _agentId = agentId;
        _agentSecret = agentSecret;
        _identityServiceUrl = identityServiceUrl;
    }

    public async Task<string> GetTokenAsync()
    {
        if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiry.AddMinutes(-5))
        {
            return _cachedToken;
        }

        try
        {
            var url = $"{_identityServiceUrl}/auth/agent/token?agentId={_agentId}&agentSecret={_agentSecret}";
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return string.Empty;
            }

            var json = await response.Content.ReadAsStringAsync();
            var token = doc.RootElement.GetProperty("token").GetString();

            _cachedToken = token;
            _tokenExpiry = DateTime.UtcNow.AddHours(1);

            return token ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}


app.Run();
