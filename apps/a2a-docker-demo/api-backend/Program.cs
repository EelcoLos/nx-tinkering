using FastEndpoints;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var settings = ServiceSettings.Create();

builder.Services.AddCors(options =>
{
    // NOTE: In production, restrict CORS to specific trusted origins
    // Example: policy.WithOrigins("https://yourdomain.com")
    // For demo/local development, we allow all origins
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
builder.Services.AddSingleton<IdentityClient>();
builder.Services.AddSingleton<RequestAuthorizer>();
builder.Services.AddSingleton<ServiceRegistrar>();
builder.Services.AddSingleton<TriageStore>();
builder.Services.AddSingleton<DownstreamGateway>();
builder.Services.AddFastEndpoints();

var app = builder.Build();

// CORS must come early, before routing
app.UseCors("AllowAll");

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/health")
        || context.Request.Path.StartsWithSegments("/auth/login")
        || context.Request.Path.StartsWithSegments("/api/auth/login"))
    {
        await next();
        return;
    }

    if (context.Request.Path.StartsWithSegments("/api/services") || context.Request.Path.StartsWithSegments("/api/triage"))
    {
        var authorizer = context.RequestServices.GetRequiredService<RequestAuthorizer>();
        var validatedToken = await authorizer.ValidateBearerAsync(context, "user", context.RequestAborted);
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

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.UseFastEndpoints();
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
    public string ServiceName { get; init; } = "api-backend";
    public int Port { get; init; } = 5056;
    public string ServiceBaseUrl { get; init; } = "http://api-backend:5056";
    public string IdentityServiceUrl { get; init; } = "http://identity:5050";
    public string DiscoveryServiceUrl { get; init; } = "http://discovery:5051";
    public string ClassifierServiceUrl { get; init; } = "http://classifier:5052";
    public string AssessorServiceUrl { get; init; } = "http://assessor:5053";
    public string RouterServiceUrl { get; init; } = "http://router:5054";
    public string HandlerServiceUrl { get; init; } = "http://handler:5055";
    public string JwtSecretKey { get; init; } = "";
    public string AgentId { get; init; } = "api-backend-agent";

    public static ServiceSettings Create() => new ServiceSettings
    {
        ServiceBaseUrl = Environment.GetEnvironmentVariable("API_BACKEND_SERVICE_URL") ?? "http://api-backend:5056",
        IdentityServiceUrl = Environment.GetEnvironmentVariable("IDENTITY_SERVICE_URL") ?? "http://identity:5050",
        DiscoveryServiceUrl = Environment.GetEnvironmentVariable("DISCOVERY_SERVICE_URL") ?? "http://discovery:5051",
        ClassifierServiceUrl = Environment.GetEnvironmentVariable("CLASSIFIER_SERVICE_URL") ?? "http://classifier:5052",
        AssessorServiceUrl = Environment.GetEnvironmentVariable("ASSESSOR_SERVICE_URL") ?? "http://assessor:5053",
        RouterServiceUrl = Environment.GetEnvironmentVariable("ROUTER_SERVICE_URL") ?? "http://router:5054",
        HandlerServiceUrl = Environment.GetEnvironmentVariable("HANDLER_SERVICE_URL") ?? "http://handler:5055",
        JwtSecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? "your-256-bit-secret-key-must-be-min-32-chars",
        AgentId = Environment.GetEnvironmentVariable("API_BACKEND_AGENT_ID") ?? "api-backend-agent"
    };
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

    public async Task<(int statusCode, string body)> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient();
        using var response = await client.PostAsJsonAsync($"{_settings.IdentityServiceUrl}/auth/login", request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        return ((int)response.StatusCode, body);
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
            return null;
            
        const string prefix = "Bearer ";
        return authorizationHeader.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? authorizationHeader[prefix.Length..].Trim()
            : null;
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
            Description = "Website-facing FastEndpoints API gateway.",
            Skills = new List<string> { "triage-orchestration" },
            AgentCard = new AgentCard
            {
                Id = _settings.ServiceName,
                Name = _settings.ServiceName,
                Description = "Website-facing FastEndpoints API gateway.",
                Url = _settings.ServiceBaseUrl,
                Version = "v2",
                Skills = new List<AgentSkill>
                {
                    new AgentSkill { Id = "triage-orchestration", Name = "triage-orchestration", Description = "Coordinates the multi-step triage workflow." }
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

class DownstreamGateway
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IdentityClient _identityClient;
    private readonly ServiceSettings _settings;

    public DownstreamGateway(IHttpClientFactory httpClientFactory, IdentityClient identityClient, ServiceSettings settings)
    {
        _httpClientFactory = httpClientFactory;
        _identityClient = identityClient;
        _settings = settings;
    }

    public async Task<IReadOnlyCollection<ServiceSummary>> GetServicesAsync(CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient();
        var token = await _identityClient.GetAgentTokenAsync(ct);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_settings.DiscoveryServiceUrl}/services");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<ServiceSummary>>(cancellationToken: ct) ?? new List<ServiceSummary>();
    }

    public async Task<AgentCard?> GetServiceCardAsync(string serviceId, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient();
        var token = await _identityClient.GetAgentTokenAsync(ct);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_settings.DiscoveryServiceUrl}/services/{Uri.EscapeDataString(serviceId)}/card");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AgentCard>(cancellationToken: ct);
    }

    public async Task<TriageRecord> RunTriageAsync(string input, CancellationToken ct)
    {
        var startTime = DateTimeOffset.UtcNow;
        var agentToken = await _identityClient.GetAgentTokenAsync(ct);
        var client = _httpClientFactory.CreateClient();
        var record = new TriageRecord
        {
            Id = $"triage-{Guid.NewGuid():N}"[..19],
            Input = input,
            Status = "processing",
            CreatedAt = startTime,
            UpdatedAt = startTime
        };

        // Classifier
        var classifyStart = DateTimeOffset.UtcNow;
        var classification = await PostAsync<ClassificationResponse>(client, $"{_settings.ClassifierServiceUrl}/skills/classify", new
        {
            input,
            metadata = new { agent_jwt = agentToken }
        }, ct);
        record.Classification = classification.ClassificationType;
        record.UpdatedAt = DateTimeOffset.UtcNow;
        record.Trace.Add(new TraceEntry
        {
            Service = "Classifier",
            Status = "completed",
            TimestampMs = (long)(DateTimeOffset.UtcNow - classifyStart).TotalMilliseconds,
            Result = classification.ClassificationType
        });

        // Assessor
        var assessStart = DateTimeOffset.UtcNow;
        var assessment = await PostAsync<AssessmentResponse>(client, $"{_settings.AssessorServiceUrl}/skills/assess", new
        {
            classification = classification.ClassificationType,
            metadata = new { agent_jwt = agentToken }
        }, ct);
        record.Priority = assessment.Priority;
        record.UpdatedAt = DateTimeOffset.UtcNow;
        record.Trace.Add(new TraceEntry
        {
            Service = "Assessor",
            Status = "completed",
            TimestampMs = (long)(DateTimeOffset.UtcNow - assessStart).TotalMilliseconds,
            Result = assessment.Priority
        });

        // Router
        var routeStart = DateTimeOffset.UtcNow;
        var routing = await PostAsync<RoutingResponse>(client, $"{_settings.RouterServiceUrl}/skills/route", new
        {
            priority = assessment.Priority,
            metadata = new { agent_jwt = agentToken }
        }, ct);
        record.NextHandler = routing.NextHandler;
        record.UpdatedAt = DateTimeOffset.UtcNow;
        record.Trace.Add(new TraceEntry
        {
            Service = "Router",
            Status = "completed",
            TimestampMs = (long)(DateTimeOffset.UtcNow - routeStart).TotalMilliseconds,
            Result = routing.NextHandler
        });

        // Handler
        var handleStart = DateTimeOffset.UtcNow;
        var handling = await PostAsync<HandlingResponse>(client, $"{_settings.HandlerServiceUrl}/skills/handle", new
        {
            input,
            classification = classification.ClassificationType,
            priority = assessment.Priority,
            metadata = new { agent_jwt = agentToken }
        }, ct);

        record.Status = handling.Status;
        record.TicketId = handling.TicketId;
        record.Summary = handling.Summary;
        record.UpdatedAt = DateTimeOffset.UtcNow;
        record.Trace.Add(new TraceEntry
        {
            Service = "Handler",
            Status = "completed",
            TimestampMs = (long)(DateTimeOffset.UtcNow - handleStart).TotalMilliseconds,
            Result = handling.TicketId
        });

        return record;
    }

    private static async Task<T> PostAsync<T>(HttpClient client, string url, object payload, CancellationToken ct)
    {
        using var response = await client.PostAsJsonAsync(url, payload, ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
        if (body is null)
        {
            throw new InvalidOperationException($"{url} returned an empty response.");
        }

        return body;
    }
}

class TriageStore
{
    private readonly ConcurrentDictionary<string, TriageRecord> _records = new(StringComparer.OrdinalIgnoreCase);

    public void Save(TriageRecord record) => _records[record.Id] = record;

    public TriageRecord? Get(string id) => _records.TryGetValue(id, out var record) ? record : null;
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

class ServiceSummary
{
    [JsonPropertyName("service_id")] public string ServiceId { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("base_url")] public string BaseUrl { get; set; } = "";
    [JsonPropertyName("port")] public int Port { get; set; }
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("skills")] public List<string> Skills { get; set; } = new();
    [JsonPropertyName("registered_at")] public DateTimeOffset RegisteredAt { get; set; }
}

class TriageRecord
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("input")] public string Input { get; set; } = "";
    [JsonPropertyName("classification")] public string? Classification { get; set; }
    [JsonPropertyName("priority")] public string? Priority { get; set; }
    [JsonPropertyName("next_handler")] public string? NextHandler { get; set; }
    [JsonPropertyName("ticket_id")] public string? TicketId { get; set; }
    [JsonPropertyName("summary")] public string? Summary { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = "pending";
    [JsonPropertyName("error")] public string? Error { get; set; }
    [JsonPropertyName("trace")] public List<TraceEntry> Trace { get; set; } = new();
    [JsonPropertyName("created_at")] public DateTimeOffset CreatedAt { get; set; }
    [JsonPropertyName("updated_at")] public DateTimeOffset UpdatedAt { get; set; }
}

class TraceEntry
{
    [JsonPropertyName("service")] public string Service { get; set; } = "";
    [JsonPropertyName("status")] public string Status { get; set; } = "";
    [JsonPropertyName("timestamp_ms")] public long TimestampMs { get; set; }
    [JsonPropertyName("result")] public string Result { get; set; } = "";
}

class ClassificationResponse
{
    [JsonPropertyName("classification_type")] public string ClassificationType { get; set; } = "general";
}

class AssessmentResponse
{
    [JsonPropertyName("priority")] public string Priority { get; set; } = "normal";
}

class RoutingResponse
{
    [JsonPropertyName("next_handler")] public string NextHandler { get; set; } = "general-handler";
}

class HandlingResponse
{
    [JsonPropertyName("status")] public string Status { get; set; } = "completed";
    [JsonPropertyName("ticket_id")] public string TicketId { get; set; } = "";
    [JsonPropertyName("summary")] public string Summary { get; set; } = "";
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

class LoginRequest
{
    [JsonPropertyName("username")] public string? Username { get; set; }
    [JsonPropertyName("password")] public string? Password { get; set; }
}

class SubmitTriageRequest
{
    [JsonPropertyName("input")] public string? Input { get; set; }
}

class LoginEndpoint : Endpoint<LoginRequest, object>
{
    public override void Configure()
    {
        Verbs(Http.POST);
        Routes("/auth/login", "/api/auth/login");
        AllowAnonymous();
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        var identityClient = Resolve<IdentityClient>();
        var (statusCode, body) = await identityClient.LoginAsync(req, ct);
        HttpContext.Response.StatusCode = statusCode;
        HttpContext.Response.ContentType = "application/json";
        await HttpContext.Response.WriteAsync(body, ct);
    }
}

class ListServicesEndpoint : Endpoint<EmptyRequest, object>
{
    public override void Configure() 
    { 
        Get("/api/services");
        AllowAnonymous();
    }

    public override async Task HandleAsync(EmptyRequest _, CancellationToken ct)
    {
        var gateway = Resolve<DownstreamGateway>();
        var services = await gateway.GetServicesAsync(ct);
        await HttpContext.Response.WriteAsJsonAsync(services, cancellationToken: ct);
    }
}

class GetServiceCardEndpoint : Endpoint<EmptyRequest, object>
{
    public override void Configure()
    {
        Get("/api/services/{id}/card");
        AllowAnonymous();
    }

    public override async Task HandleAsync(EmptyRequest _, CancellationToken ct)
    {
        var serviceId = HttpContext.Request.RouteValues["id"]?.ToString() ?? string.Empty;
        var gateway = Resolve<DownstreamGateway>();
        var card = await gateway.GetServiceCardAsync(serviceId, ct);
        if (card is null)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            await HttpContext.Response.WriteAsJsonAsync(new { error = "Service not found" }, cancellationToken: ct);
            return;
        }

        await HttpContext.Response.WriteAsJsonAsync(card, cancellationToken: ct);
    }
}

class SubmitTriageEndpoint : Endpoint<SubmitTriageRequest, object>
{
    public override void Configure()
    {
        Post("/api/triage");
        AllowAnonymous();
    }

    public override async Task HandleAsync(SubmitTriageRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Input))
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await HttpContext.Response.WriteAsJsonAsync(new { error = "input is required" }, cancellationToken: ct);
            return;
        }

        var gateway = Resolve<DownstreamGateway>();
        var store = Resolve<TriageStore>();

        try
        {
            var record = await gateway.RunTriageAsync(req.Input.Trim(), ct);
            store.Save(record);
            await HttpContext.Response.WriteAsJsonAsync(record, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            var failedRecord = new TriageRecord
            {
                Id = $"triage-{Guid.NewGuid():N}"[..19],
                Input = req.Input.Trim(),
                Status = "failed",
                Error = ex.Message,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            store.Save(failedRecord);
            HttpContext.Response.StatusCode = StatusCodes.Status502BadGateway;
            await HttpContext.Response.WriteAsJsonAsync(failedRecord, cancellationToken: ct);
        }
    }
}

class GetTriageStatusEndpoint : Endpoint<EmptyRequest, object>
{
    public override void Configure()
    {
        Get("/api/triage/{id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(EmptyRequest _, CancellationToken ct)
    {
        var triageId = HttpContext.Request.RouteValues["id"]?.ToString() ?? string.Empty;
        var store = Resolve<TriageStore>();
        var record = store.Get(triageId);
        if (record is null)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            await HttpContext.Response.WriteAsJsonAsync(new { error = "Triage request not found" }, cancellationToken: ct);
            return;
        }

        await HttpContext.Response.WriteAsJsonAsync(record, cancellationToken: ct);
    }
}
