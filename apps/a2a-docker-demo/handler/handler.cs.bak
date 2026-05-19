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



const string HandlerHostUrl = "http://localhost:5055";
const string IdentityServiceUrl = "http://localhost:5050";

var bld = WebApplication.CreateBuilder(args);
bld.WebHost.UseUrls(HandlerHostUrl);
bld.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.TypeInfoResolverChain.Add(new DefaultJsonTypeInfoResolver()));

var jwtSecret = bld.Configuration["JWT_SECRET_KEY"] ?? throw new InvalidOperationException("JWT_SECRET_KEY not configured");
var agentId = bld.Configuration["HANDLER_AGENT_ID"] ?? "handler_agent";
var agentSecret = bld.Configuration["HANDLER_AGENT_SECRET"] ?? "default-secret-handler";

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
var jwtValidator = new JwtValidator(signingKey);
var agentJwtProvider = new AgentJwtProvider(agentId, agentSecret, IdentityServiceUrl);

bld.Services.AddSingleton(jwtValidator);
bld.Services.AddSingleton(agentJwtProvider);

bld.Services
   .AddFastEndpoints()
   .AddA2A(o =>
   {
       o.AgentName = "fe-handler-agent";
       o.Description = "Handler specialist agent - final processor for triage requests. Executes resolution based on priority.";
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

sealed class HandlerRequest
{
    [JsonPropertyName("input")]
    public string? Input { get; set; }

    [JsonPropertyName("route")]
    public string? Route { get; set; }

    [JsonPropertyName("handlerQueue")]
    public string? HandlerQueue { get; set; }

    [JsonPropertyName("priority")]
    public int Priority { get; set; }
}

sealed class HandlerResponse
{
    [JsonPropertyName("result")]
    public string? Result { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("ticketId")]
    public string? TicketId { get; set; }

    [JsonPropertyName("resolution")]
    public string? Resolution { get; set; }
}

sealed class HealthResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "healthy";

    [JsonPropertyName("service")]
    public string Service { get; set; } = "handler";
}

sealed class HandlerSkillEndpoint : Endpoint<HandlerRequest, HandlerResponse>
{
    public override void Configure()
    {
        Post("/skills/handle");
        AllowAnonymous();

        this.A2ASkill(
            id: "handler_skill",
            tags: ["triage", "process", "resolve"],
            configure: skill =>
            {
                skill.Name = "Handler";
                skill.Description = "Processes and resolves triage requests. Creates tickets and provides resolution based on priority level.";
                skill.Examples = ["Handle this routed request and create a ticket"];
                skill.InputModes = ["application/json"];
                skill.OutputModes = ["application/json"];
            });
    }

    public override async Task HandleAsync(HandlerRequest req, CancellationToken ct)
    {
        var ticketId = $"TKT-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        var handlerQueue = req.HandlerQueue ?? "handler_standard";
        var priority = req.Priority;

        string resolution;
        if (priority >= 4)
        {
            resolution = "Escalated to senior support team for immediate handling.";
        }
        else if (priority == 3)
        {
            resolution = "Assigned to standard support queue. Expected resolution time: 24-48 hours.";
        }
        else
        {
            resolution = "Added to background queue. Will be processed in the next batch.";
        }

        var response = new HandlerResponse
        {
            Result = $"Request processed: Ticket {ticketId} created",
            Status = "in_queue",
            TicketId = ticketId,
            Resolution = resolution,
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
