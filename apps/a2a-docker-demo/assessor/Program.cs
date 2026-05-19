using FastEndpoints;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var settings = ServiceSettings.Create("assessor", 5053, "http://assessor:5053");

builder.Services.AddSingleton(settings);
builder.Services.AddHttpClient();
builder.Services.AddSingleton(new JwtService(settings.JwtSecretKey));
builder.Services.AddSingleton<IdentityClient>();
builder.Services.AddSingleton<RequestAuthorizer>();
builder.Services.AddSingleton<ServiceRegistrar>();
builder.Services.AddFastEndpoints();

var app = builder.Build();

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/health"))
    {
        await next();
        return;
    }

    if (context.Request.Path.StartsWithSegments("/skills"))
    {
        var authorizer = context.RequestServices.GetRequiredService<RequestAuthorizer>();
        var validatedToken = await authorizer.ValidateBodyOrBearerAsync(context, "agent", context.RequestAborted);
        if (validatedToken is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid or expired token" }, cancellationToken: context.RequestAborted);
            return;
        }

        context.Items["validated_token"] = validatedToken;
    }

    await next();
});

app.UseFastEndpoints();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.Lifetime.ApplicationStarted.Register(() =>
{
    _ = Task.Run(async () =>
    {
        using var scope = app.Services.CreateScope();
        try
        {
            await scope.ServiceProvider.GetRequiredService<ServiceRegistrar>().RegisterAsync();
        }
        catch
        {
        }
    });
});
app.Run();

class ServiceSettings
{
    public string ServiceName { get; init; } = "";
    public int Port { get; init; }
    public string ServiceBaseUrl { get; init; } = "";
    public string DiscoveryServiceUrl { get; init; } = "";
    public string IdentityServiceUrl { get; init; } = "";
    public string JwtSecretKey { get; init; } = "";
    public string AgentId { get; init; } = "";

    public static ServiceSettings Create(string serviceName, int port, string defaultBaseUrl)
    {
        var envPrefix = serviceName.ToUpperInvariant().Replace('-', '_');
        return new ServiceSettings
        {
            ServiceName = serviceName,
            Port = port,
            ServiceBaseUrl = Environment.GetEnvironmentVariable($"{envPrefix}_SERVICE_URL") ?? defaultBaseUrl,
            DiscoveryServiceUrl = Environment.GetEnvironmentVariable("DISCOVERY_SERVICE_URL") ?? "http://discovery:5051",
            IdentityServiceUrl = Environment.GetEnvironmentVariable("IDENTITY_SERVICE_URL") ?? "http://identity:5050",
            JwtSecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? "your-256-bit-secret-key-must-be-min-32-chars",
            AgentId = Environment.GetEnvironmentVariable($"{envPrefix}_AGENT_ID") ?? $"{serviceName}-agent"
        };
    }
}

class JwtService
{
    private readonly SymmetricSecurityKey _key;

    public JwtService(string jwtSecretKey) => _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey));

    public string GenerateAgentToken(string agentId)
    {
        var handler = new JwtSecurityTokenHandler();
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", agentId),
                new Claim("agent_id", agentId),
                new Claim("type", "agent")
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256)
        };

        return handler.WriteToken(handler.CreateToken(descriptor));
    }

    public ValidatedToken? ValidateLocal(string token, string? expectedType = null)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            }, out _);

            var validatedToken = new ValidatedToken
            {
                Subject = principal.FindFirst("sub")?.Value ?? "",
                Type = principal.FindFirst("type")?.Value ?? "",
                AgentId = principal.FindFirst("agent_id")?.Value,
                Username = principal.FindFirst("username")?.Value
            };

            if (!string.IsNullOrWhiteSpace(expectedType) && !string.Equals(validatedToken.Type, expectedType, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return validatedToken;
        }
        catch
        {
            return null;
        }
    }
}

class IdentityClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ServiceSettings _settings;
    private readonly JwtService _jwtService;

    public IdentityClient(IHttpClientFactory httpClientFactory, ServiceSettings settings, JwtService jwtService)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings;
        _jwtService = jwtService;
    }

    public async Task<ValidatedToken?> ValidateTokenAsync(string token, string expectedType, CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            using var response = await client.PostAsJsonAsync($"{_settings.IdentityServiceUrl}/auth/validate", new ValidateTokenRequest { Token = token }, ct);
            if (response.IsSuccessStatusCode)
            {
                var validated = await response.Content.ReadFromJsonAsync<ValidatedToken>(cancellationToken: ct);
                if (validated is not null && string.Equals(validated.Type, expectedType, StringComparison.OrdinalIgnoreCase))
                {
                    return validated;
                }

                return null;
            }
        }
        catch
        {
        }

        return _jwtService.ValidateLocal(token, expectedType);
    }

    public async Task<string> GetAgentTokenAsync(CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"{_settings.IdentityServiceUrl}/auth/agent/token?agentId={Uri.EscapeDataString(_settings.AgentId)}";
            var tokenResponse = await client.GetFromJsonAsync<AgentTokenResponse>(url, ct);
            if (!string.IsNullOrWhiteSpace(tokenResponse?.Token))
            {
                return tokenResponse.Token!;
            }
        }
        catch
        {
        }

        return _jwtService.GenerateAgentToken(_settings.AgentId);
    }
}

class RequestAuthorizer
{
    private readonly IdentityClient _identityClient;

    public RequestAuthorizer(IdentityClient identityClient) => _identityClient = identityClient;

    public async Task<ValidatedToken?> ValidateBodyOrBearerAsync(HttpContext context, string expectedType, CancellationToken ct)
    {
        var token = GetBearerToken(context.Request.Headers.Authorization);
        token ??= await TryReadTokenFromBodyAsync(context);

        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        return await _identityClient.ValidateTokenAsync(token, expectedType, ct);
    }

    private static string? GetBearerToken(string authorizationHeader)
    {
        const string prefix = "Bearer ";
        return authorizationHeader.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? authorizationHeader[prefix.Length..].Trim()
            : null;
    }

    private static async Task<string?> TryReadTokenFromBodyAsync(HttpContext context)
    {
        if (!context.Request.HasJsonContentType())
        {
            return null;
        }

        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("metadata", out var metadata))
            {
                if (metadata.TryGetProperty("agent_jwt", out var agentJwt) && agentJwt.ValueKind == JsonValueKind.String)
                {
                    return agentJwt.GetString();
                }

                if (metadata.TryGetProperty("agentJwt", out var camelCaseAgentJwt) && camelCaseAgentJwt.ValueKind == JsonValueKind.String)
                {
                    return camelCaseAgentJwt.GetString();
                }
            }

            if (document.RootElement.TryGetProperty("agent_jwt", out var rootAgentJwt) && rootAgentJwt.ValueKind == JsonValueKind.String)
            {
                return rootAgentJwt.GetString();
            }
        }
        catch
        {
        }

        return null;
    }
}

class ServiceRegistrar
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IdentityClient _identityClient;
    private readonly ServiceSettings _settings;

    public ServiceRegistrar(IHttpClientFactory httpClientFactory, IdentityClient identityClient, ServiceSettings settings)
    {
        _httpClientFactory = httpClientFactory;
        _identityClient = identityClient;
        _settings = settings;
    }

    public async Task RegisterAsync()
    {
        await Task.Delay(TimeSpan.FromSeconds(1));
        var token = await _identityClient.GetAgentTokenAsync(CancellationToken.None);
        var registration = new ServiceRegistrationRequest
        {
            ServiceId = _settings.ServiceName,
            Name = _settings.ServiceName,
            BaseUrl = _settings.ServiceBaseUrl,
            Port = _settings.Port,
            Description = "A2A specialist that turns classifications into priorities.",
            Skills = new List<string> { "priority-assessment" },
            AgentCard = new AgentCard
            {
                Id = _settings.ServiceName,
                Name = _settings.ServiceName,
                Description = "A2A specialist that turns classifications into priorities.",
                Url = _settings.ServiceBaseUrl,
                Version = "v2",
                Skills = new List<AgentSkill>
                {
                    new AgentSkill { Id = "priority-assessment", Name = "priority-assessment", Description = "Determines priority from a prior classification." }
                }
            }
        };

        try
        {
            var client = _httpClientFactory.CreateClient();
            using var message = new HttpRequestMessage(HttpMethod.Post, $"{_settings.DiscoveryServiceUrl}/register")
            {
                Content = JsonContent.Create(registration)
            };
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            await client.SendAsync(message);
        }
        catch
        {
        }
    }
}

class ServiceRegistrationRequest
{
    [JsonPropertyName("service_id")] public string? ServiceId { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("base_url")] public string? BaseUrl { get; set; }
    [JsonPropertyName("port")] public int Port { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("skills")] public List<string>? Skills { get; set; }
    [JsonPropertyName("agent_card")] public AgentCard? AgentCard { get; set; }
}

class AgentCard
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("url")] public string Url { get; set; } = "";
    [JsonPropertyName("version")] public string Version { get; set; } = "v2";
    [JsonPropertyName("skills")] public List<AgentSkill> Skills { get; set; } = new();
}

class AgentSkill
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
}

class AgentTokenResponse
{
    [JsonPropertyName("token")] public string? Token { get; set; }
}

class ValidateTokenRequest
{
    [JsonPropertyName("token")] public string? Token { get; set; }
}

class ValidatedToken
{
    [JsonPropertyName("subject")] public string Subject { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("agent_id")] public string? AgentId { get; set; }
    [JsonPropertyName("username")] public string? Username { get; set; }
}

class A2AMetadata
{
    [JsonPropertyName("agent_jwt")] public string? AgentJwt { get; set; }
}

class AssessRequest
{
    [JsonPropertyName("classification")] public string? Classification { get; set; }
    [JsonPropertyName("metadata")] public A2AMetadata? Metadata { get; set; }
}

class AssessResponse
{
    [JsonPropertyName("priority")] public string Priority { get; set; } = "normal";
    [JsonPropertyName("result")] public string Result { get; set; } = "";
}

class SkillEndpoint : Endpoint<AssessRequest, AssessResponse>
{
    public override void Configure()
    {
        Post("/skills/assess");
        AllowAnonymous();
    }

    public override async Task HandleAsync(AssessRequest req, CancellationToken ct)
    {
var classification = (req.Classification ?? string.Empty).Trim().ToLowerInvariant();
var priority = classification switch
{
    "incident" => "critical",
    "defect" => "high",
    "feature_request" => "medium",
    "inquiry" => "low",
    _ => "normal"
};

var response = new AssessResponse
{
    Priority = priority,
    Result = $"Priority assessed as {priority}."
};
        await HttpContext.Response.WriteAsJsonAsync(response, cancellationToken: ct);
    }
}
