using FastEndpoints;
using A2ADemo.Common;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var settings = ServiceSettings.Create("discovery", 5051, "http://discovery:5051");

builder.Services.AddSingleton(settings);
builder.Services.AddHttpClient();
builder.Services.AddSingleton(new JwtService(settings.JwtSecretKey));
builder.Services.AddSingleton<IdentityClient>();
builder.Services.AddSingleton<RequestAuthorizer>();
builder.Services.AddSingleton<ServiceRegistry>();
builder.Services.AddFastEndpoints();
builder.Services.AddServiceTelemetry(
    settings.OtelEnabled,
    settings.ServiceName,
    settings.OtelServiceNamespace,
    settings.OtelExporterEndpoint,
    settings.ServiceName);

var app = builder.Build();
app.Services.GetRequiredService<ServiceRegistry>().SeedSelf();

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/health"))
    {
        await next();
        return;
    }

    if (context.Request.Path.StartsWithSegments("/services") || context.Request.Path.StartsWithSegments("/register"))
    {
        var authorizer = context.RequestServices.GetRequiredService<RequestAuthorizer>();
        var validatedToken = await authorizer.ValidateBearerAsync(context, "agent", context.RequestAborted);
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
app.Run();

class ServiceSettings
{
    public string ServiceName { get; init; } = "";
    public int Port { get; init; }
    public string ServiceBaseUrl { get; init; } = "";
    public string IdentityServiceUrl { get; init; } = "";
    public string JwtSecretKey { get; init; } = "";
    public string AgentId { get; init; } = "";
    public bool OtelEnabled { get; init; }
    public string OtelExporterEndpoint { get; init; } = "http://tempo:4317";
    public string OtelServiceNamespace { get; init; } = "a2a-docker-demo";

    public static ServiceSettings Create(string serviceName, int port, string defaultBaseUrl)
    {
        var envPrefix = serviceName.ToUpperInvariant().Replace('-', '_');
        return new ServiceSettings
        {
            ServiceName = serviceName,
            Port = port,
            ServiceBaseUrl = Environment.GetEnvironmentVariable($"{envPrefix}_SERVICE_URL") ?? defaultBaseUrl,
            IdentityServiceUrl = Environment.GetEnvironmentVariable("IDENTITY_SERVICE_URL") ?? "http://identity:5050",
            JwtSecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? "your-256-bit-secret-key-must-be-min-32-chars",
            AgentId = Environment.GetEnvironmentVariable($"{envPrefix}_AGENT_ID") ?? $"{serviceName}-agent",
            OtelEnabled = bool.TryParse(Environment.GetEnvironmentVariable("OTEL_ENABLED"), out var enabled) && enabled,
            OtelExporterEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "http://tempo:4317",
            OtelServiceNamespace = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAMESPACE") ?? "a2a-docker-demo"
        };
    }
}

class JwtService
{
    private readonly SymmetricSecurityKey _key;

    public JwtService(string jwtSecretKey) => _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey));

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
}

class RequestAuthorizer
{
    private readonly IdentityClient _identityClient;

    public RequestAuthorizer(IdentityClient identityClient) => _identityClient = identityClient;

    public async Task<ValidatedToken?> ValidateBearerAsync(HttpContext context, string expectedType, CancellationToken ct)
    {
        var token = GetBearerToken(context.Request.Headers.Authorization);
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        return await _identityClient.ValidateTokenAsync(token, expectedType, ct);
    }

    private static string? GetBearerToken(string? authorizationHeader)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader))
        {
            return null;
        }

        const string prefix = "Bearer ";
        return authorizationHeader.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? authorizationHeader[prefix.Length..].Trim()
            : null;
    }
}

class ServiceRegistry
{
    private readonly ConcurrentDictionary<string, RegisteredService> _services = new(StringComparer.OrdinalIgnoreCase);
    private readonly ServiceSettings _settings;

    public ServiceRegistry(ServiceSettings settings) => _settings = settings;

    public void SeedSelf()
    {
        _services[_settings.ServiceName] = new RegisteredService
        {
            ServiceId = _settings.ServiceName,
            Name = "Discovery Service",
            BaseUrl = _settings.ServiceBaseUrl,
            Port = _settings.Port,
            Description = "Service registry for A2A services.",
            Skills = new List<string> { "service-discovery" },
            RegisteredAt = DateTimeOffset.UtcNow,
            AgentCard = new AgentCard
            {
                Id = _settings.ServiceName,
                Name = "discovery",
                Description = "Registers and returns available A2A services.",
                Url = _settings.ServiceBaseUrl,
                Version = "v2",
                Skills = new List<AgentSkill>
                {
                    new AgentSkill { Id = "service-discovery", Name = "service-discovery", Description = "Lists registered services and their cards." }
                }
            }
        };
    }

    public IReadOnlyCollection<RegisteredService> GetAll() => _services.Values.OrderBy(service => service.ServiceId).ToArray();

    public AgentCard? GetCard(string serviceId) => _services.TryGetValue(serviceId, out var service) ? service.AgentCard : null;

    public RegisteredService Upsert(ServiceRegistrationRequest request)
    {
        var registered = new RegisteredService
        {
            ServiceId = request.ServiceId?.Trim() ?? string.Empty,
            Name = string.IsNullOrWhiteSpace(request.Name) ? request.ServiceId?.Trim() ?? string.Empty : request.Name.Trim(),
            BaseUrl = request.BaseUrl?.Trim() ?? string.Empty,
            Port = request.Port,
            Description = request.Description?.Trim() ?? string.Empty,
            Skills = request.Skills?.Where(skill => !string.IsNullOrWhiteSpace(skill)).Select(skill => skill.Trim()).ToList() ?? new List<string>(),
            AgentCard = request.AgentCard ?? new AgentCard(),
            RegisteredAt = DateTimeOffset.UtcNow
        };

        _services[registered.ServiceId] = registered;
        return registered;
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

class RegisteredService
{
    [JsonPropertyName("service_id")] public string ServiceId { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("base_url")] public string BaseUrl { get; set; } = "";
    [JsonPropertyName("port")] public int Port { get; set; }
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("skills")] public List<string> Skills { get; set; } = new();
    [JsonPropertyName("registered_at")] public DateTimeOffset RegisteredAt { get; set; }
    [JsonPropertyName("agent_card")] public AgentCard AgentCard { get; set; } = new();
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

class ListServicesEndpoint : Endpoint<EmptyRequest, object>
{
    public override void Configure()
    {
        Get("/services");
        AllowAnonymous();
    }

    public override async Task HandleAsync(EmptyRequest _, CancellationToken ct)
    {
        var registry = Resolve<ServiceRegistry>();
        await HttpContext.Response.WriteAsJsonAsync(registry.GetAll(), cancellationToken: ct);
    }
}

class GetServiceCardEndpoint : Endpoint<EmptyRequest, object>
{
    public override void Configure()
    {
        Get("/services/{id}/card");
        AllowAnonymous();
    }

    public override async Task HandleAsync(EmptyRequest _, CancellationToken ct)
    {
        var serviceId = HttpContext.Request.RouteValues["id"]?.ToString() ?? string.Empty;
        var registry = Resolve<ServiceRegistry>();
        var card = registry.GetCard(serviceId);
        if (card is null)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            await HttpContext.Response.WriteAsJsonAsync(new { error = "Service not found" }, cancellationToken: ct);
            return;
        }

        await HttpContext.Response.WriteAsJsonAsync(card, cancellationToken: ct);
    }
}

class RegisterServiceEndpoint : Endpoint<ServiceRegistrationRequest, object>
{
    public override void Configure()
    {
        Post("/register");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ServiceRegistrationRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.ServiceId) || string.IsNullOrWhiteSpace(req.BaseUrl))
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await HttpContext.Response.WriteAsJsonAsync(new { error = "service_id and base_url are required" }, cancellationToken: ct);
            return;
        }

        var registry = Resolve<ServiceRegistry>();
        var registered = registry.Upsert(req);
        await HttpContext.Response.WriteAsJsonAsync(new
        {
            status = "registered",
            service_id = registered.ServiceId,
            registered_at = registered.RegisteredAt
        }, cancellationToken: ct);
    }
}

sealed class HealthEndpoint : HealthEndpointBase;
