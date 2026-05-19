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



const string ClassifierHostUrl = "http://localhost:5052";
const string IdentityServiceUrl = "http://localhost:5050";
const string DiscoveryServiceUrl = "http://localhost:5051";

var bld = WebApplication.CreateBuilder(args);
bld.WebHost.UseUrls(ClassifierHostUrl);
bld.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.TypeInfoResolverChain.Add(new DefaultJsonTypeInfoResolver()));

// JWT configuration
var jwtSecret = bld.Configuration["JWT_SECRET_KEY"] ?? throw new InvalidOperationException("JWT_SECRET_KEY not configured");
var agentId = bld.Configuration["CLASSIFIER_AGENT_ID"] ?? "classifier_agent";
var agentSecret = bld.Configuration["CLASSIFIER_AGENT_SECRET"] ?? "default-secret-classifier";

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
var jwtValidator = new JwtValidator(signingKey);
var agentJwtProvider = new AgentJwtProvider(agentId, agentSecret, IdentityServiceUrl);

bld.Services.AddSingleton(jwtValidator);
bld.Services.AddSingleton(agentJwtProvider);

bld.Services
   .AddFastEndpoints()
   .AddA2A(o =>
   {
       o.AgentName = "fe-classifier-agent";
       o.Description = "Classifier specialist agent - analyzes incoming requests and determines type and urgency.";
       o.Version = "1.0.0";
       o.SkillVisibilityFilter = (_, _, _) => true;
   });

var app = bld.Build();

// JWT validation middleware for incoming A2A requests
app.Use(async (context, next) =>
{
    var path = context.Request.Path.ToString();

    // Skip auth for health and unauthenticated agent card
    if (path == "/health" || path == "/.well-known/agent-card.json")
    {
        await next();
        return;
    }

    // For A2A requests, validate token from metadata (will be extracted in endpoint)
    // For HTTP requests, validate Authorization header
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

sealed class ClassifierRequest
{
    [JsonPropertyName("input")]
    public string? Input { get; set; }
}

sealed class ClassifierResponse
{
    [JsonPropertyName("result")]
    public string? Result { get; set; }

    [JsonPropertyName("classification")]
    public string? Classification { get; set; }

    [JsonPropertyName("urgency")]
    public string? Urgency { get; set; }
}

sealed class HealthResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "healthy";

    [JsonPropertyName("service")]
    public string Service { get; set; } = "classifier";
}

sealed class ClassifierSkillEndpoint : Endpoint<ClassifierRequest, ClassifierResponse>
{
    public override void Configure()
    {
        Post("/skills/classify");
        AllowAnonymous();

        this.A2ASkill(
            id: "classifier_skill",
            tags: ["triage", "classify"],
            configure: skill =>
            {
                skill.Name = "Classifier";
                skill.Description = "Analyzes incoming requests and determines their type and initial urgency level.";
                skill.Examples = ["Classify this issue: server is down"];
                skill.InputModes = ["application/json"];
                skill.OutputModes = ["application/json"];
            });
    }

    public override async Task HandleAsync(ClassifierRequest req, CancellationToken ct)
    {
        var input = req.Input ?? string.Empty;
        var lowerInput = input.ToLowerInvariant();

        // Simple classification logic
        string classification;
        string urgency;

        if (lowerInput.Contains("error") || lowerInput.Contains("crash") || lowerInput.Contains("critical"))
        {
            classification = "technical_issue";
            urgency = "critical";
        }
        else if (lowerInput.Contains("question") || lowerInput.Contains("how") || lowerInput.Contains("what"))
        {
            classification = "inquiry";
            urgency = "low";
        }
        else if (lowerInput.Contains("problem") || lowerInput.Contains("bug") || lowerInput.Contains("fail"))
        {
            classification = "defect";
            urgency = "high";
        }
        else if (lowerInput.Contains("feature") || lowerInput.Contains("request") || lowerInput.Contains("enhancement"))
        {
            classification = "feature_request";
            urgency = "normal";
        }
        else
        {
            classification = "general";
            urgency = "normal";
        }

        var response = new ClassifierResponse
        {
            Result = $"Classified: {classification}, Urgency: {urgency}",
            Classification = classification,
            Urgency = urgency,
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
