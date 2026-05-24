using FastEndpoints;
using A2ADemo.Common;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var settings = AuthSettings.Create();

if (!settings.OidcEnabled)
{
    if (string.IsNullOrWhiteSpace(settings.JwtSecretKey) || settings.JwtSecretKey.Length < 32)
    {
        throw new InvalidOperationException("JWT_SECRET_KEY environment variable must be set and at least 32 characters long");
    }
}

builder.Services.AddSingleton(settings);
builder.Services.AddHttpClient();

builder.Services.AddSingleton<JwtService>(new JwtService(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.JwtSecretKey))));
builder.Services.AddSingleton<OidcAuthClient>();
builder.Services.AddSingleton<UserDatabase>();
builder.Services.AddFastEndpoints();
builder.Services.AddServiceTelemetry(
    settings.OtelEnabled,
    settings.ServiceName,
    settings.OtelServiceNamespace,
    settings.OtelExporterEndpoint,
    settings.ServiceName);

var app = builder.Build();
app.Services.GetRequiredService<UserDatabase>().SeedDemoUsers();
app.UseFastEndpoints();
app.Run();

class UserDatabase
{
    private readonly List<User> _users = new();

    public class User
    {
        public string UserId { get; set; } = "";
        public string Username { get; set; } = "";
        public string PasswordHash { get; set; } = "";
    }

    public void SeedDemoUsers()
    {
        if (_users.Count > 0)
        {
            return;
        }

        _users.AddRange(new[]
        {
            new User
            {
                UserId = "user-1",
                Username = Environment.GetEnvironmentVariable("DEMO_USER_USERNAME") ?? "admin",
                PasswordHash = Environment.GetEnvironmentVariable("DEMO_USER_PASSWORD") ?? "demo123"
            },
            new User
            {
                UserId = "user-2",
                Username = Environment.GetEnvironmentVariable("DEMO_USER2_USERNAME") ?? "user",
                PasswordHash = Environment.GetEnvironmentVariable("DEMO_USER2_PASSWORD") ?? "user456"
            }
        });
    }

    public User? GetByUsername(string username) => _users.FirstOrDefault(u => u.Username == username);
}

class JwtService
{
    private readonly SymmetricSecurityKey _key;

    public JwtService(SymmetricSecurityKey key) => _key = key;

    public string GenerateUserToken(string userId, string username)
    {
        var handler = new JwtSecurityTokenHandler();
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", userId),
                new Claim("username", username),
                new Claim("type", "user")
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256)
        };

        return handler.WriteToken(handler.CreateToken(descriptor));
    }

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

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            return handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            }, out _);
        }
        catch
        {
            return null;
        }
    }
}

sealed class AuthSettings
{
    public string ServiceName { get; init; } = "identity";
    public bool OidcEnabled { get; init; }
    public string JwtSecretKey { get; init; } = "";
    public string OtelExporterEndpoint { get; init; } = "http://tempo:4317";
    public string OtelServiceNamespace { get; init; } = "a2a-docker-demo";
    public bool OtelEnabled { get; init; }
    public string OidcTokenEndpoint { get; init; } = "";
    public string OidcIntrospectionEndpoint { get; init; } = "";
    public string OidcUserClientId { get; init; } = "";
    public string OidcUserClientSecret { get; init; } = "";
    public string OidcIdentityClientId { get; init; } = "";
    public string OidcIdentityClientSecret { get; init; } = "";
    public Dictionary<string, AgentClientCredentials> AgentClients { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public static AuthSettings Create()
    {
        var discoveryAgentId = Environment.GetEnvironmentVariable("DISCOVERY_AGENT_ID") ?? "discovery-agent";
        var classifierAgentId = Environment.GetEnvironmentVariable("CLASSIFIER_AGENT_ID") ?? "classifier-agent";
        var assessorAgentId = Environment.GetEnvironmentVariable("ASSESSOR_AGENT_ID") ?? "assessor-agent";
        var routerAgentId = Environment.GetEnvironmentVariable("ROUTER_AGENT_ID") ?? "router-agent";
        var handlerAgentId = Environment.GetEnvironmentVariable("HANDLER_AGENT_ID") ?? "handler-agent";
        var apiBackendAgentId = Environment.GetEnvironmentVariable("API_BACKEND_AGENT_ID") ?? "api-backend-agent";

        return new AuthSettings
        {
            OidcEnabled = bool.TryParse(Environment.GetEnvironmentVariable("OIDC_ENABLED"), out var enabled) && enabled,
            JwtSecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? "your-256-bit-secret-key-must-be-min-32-chars",
            OtelEnabled = bool.TryParse(Environment.GetEnvironmentVariable("OTEL_ENABLED"), out var otelEnabled) && otelEnabled,
            OtelExporterEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "http://tempo:4317",
            OtelServiceNamespace = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAMESPACE") ?? "a2a-docker-demo",
            OidcTokenEndpoint = Environment.GetEnvironmentVariable("OIDC_TOKEN_ENDPOINT") ?? "",
            OidcIntrospectionEndpoint = Environment.GetEnvironmentVariable("OIDC_INTROSPECTION_ENDPOINT") ?? "",
            OidcUserClientId = Environment.GetEnvironmentVariable("OIDC_USER_CLIENT_ID") ?? "website-client",
            OidcUserClientSecret = Environment.GetEnvironmentVariable("OIDC_USER_CLIENT_SECRET") ?? "",
            OidcIdentityClientId = Environment.GetEnvironmentVariable("OIDC_IDENTITY_CLIENT_ID") ?? "identity-facade",
            OidcIdentityClientSecret = Environment.GetEnvironmentVariable("OIDC_IDENTITY_CLIENT_SECRET") ?? "",
            AgentClients = new Dictionary<string, AgentClientCredentials>(StringComparer.OrdinalIgnoreCase)
            {
                [discoveryAgentId] = new AgentClientCredentials(
                    Environment.GetEnvironmentVariable("OIDC_DISCOVERY_CLIENT_ID") ?? discoveryAgentId,
                    Environment.GetEnvironmentVariable("OIDC_DISCOVERY_CLIENT_SECRET") ?? ""),
                [classifierAgentId] = new AgentClientCredentials(
                    Environment.GetEnvironmentVariable("OIDC_CLASSIFIER_CLIENT_ID") ?? classifierAgentId,
                    Environment.GetEnvironmentVariable("OIDC_CLASSIFIER_CLIENT_SECRET") ?? ""),
                [assessorAgentId] = new AgentClientCredentials(
                    Environment.GetEnvironmentVariable("OIDC_ASSESSOR_CLIENT_ID") ?? assessorAgentId,
                    Environment.GetEnvironmentVariable("OIDC_ASSESSOR_CLIENT_SECRET") ?? ""),
                [routerAgentId] = new AgentClientCredentials(
                    Environment.GetEnvironmentVariable("OIDC_ROUTER_CLIENT_ID") ?? routerAgentId,
                    Environment.GetEnvironmentVariable("OIDC_ROUTER_CLIENT_SECRET") ?? ""),
                [handlerAgentId] = new AgentClientCredentials(
                    Environment.GetEnvironmentVariable("OIDC_HANDLER_CLIENT_ID") ?? handlerAgentId,
                    Environment.GetEnvironmentVariable("OIDC_HANDLER_CLIENT_SECRET") ?? ""),
                [apiBackendAgentId] = new AgentClientCredentials(
                    Environment.GetEnvironmentVariable("OIDC_API_BACKEND_CLIENT_ID") ?? apiBackendAgentId,
                    Environment.GetEnvironmentVariable("OIDC_API_BACKEND_CLIENT_SECRET") ?? "")
            }
        };
    }
}

sealed record AgentClientCredentials(string ClientId, string ClientSecret);

sealed class OidcAuthClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AuthSettings _settings;

    public OidcAuthClient(IHttpClientFactory httpClientFactory, AuthSettings settings)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var token = await RequestTokenAsync(
            new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["username"] = request.Username ?? string.Empty,
                ["password"] = request.Password ?? string.Empty,
                ["client_id"] = _settings.OidcUserClientId,
                ["client_secret"] = _settings.OidcUserClientSecret
            },
            ct);

        if (token is null)
        {
            return null;
        }

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token.AccessToken);
        var subject = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

        return new LoginResponse
        {
            Token = token.AccessToken,
            UserId = subject ?? token.Subject ?? token.Username
        };
    }

    public async Task<string?> GetAgentTokenAsync(string agentId, CancellationToken ct)
    {
        if (!_settings.AgentClients.TryGetValue(agentId, out var client))
        {
            return null;
        }

        var token = await RequestTokenAsync(
            new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = client.ClientId,
                ["client_secret"] = client.ClientSecret
            },
            ct);

        return token?.AccessToken;
    }

    public async Task<ValidatedTokenResponse?> ValidateTokenAsync(string token, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, _settings.OidcIntrospectionEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["token"] = token
            })
        };

        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_settings.OidcIdentityClientId}:{_settings.OidcIdentityClientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);

        using var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<OidcIntrospectionResponse>(cancellationToken: ct);
        if (payload is null || !payload.Active)
        {
            return null;
        }

        var isServiceAccount = !string.IsNullOrWhiteSpace(payload.ClientId)
            && payload.Username?.StartsWith("service-account-", StringComparison.OrdinalIgnoreCase) == true;
        var isUser = !string.IsNullOrWhiteSpace(payload.Username) && !isServiceAccount;
        return new ValidatedTokenResponse
        {
            Subject = payload.Subject ?? string.Empty,
            Type = isUser ? "user" : "agent",
            AgentId = isUser ? null : payload.ClientId,
            Username = payload.Username
        };
    }

    private async Task<OidcTokenResponse?> RequestTokenAsync(Dictionary<string, string> form, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient();
        using var response = await client.PostAsync(_settings.OidcTokenEndpoint, new FormUrlEncodedContent(form), ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var token = await response.Content.ReadFromJsonAsync<OidcTokenResponse>(cancellationToken: ct);
        return string.IsNullOrWhiteSpace(token?.AccessToken) ? null : token;
    }
}

class LoginRequest
{
    [JsonPropertyName("username")] public string? Username { get; set; }
    [JsonPropertyName("password")] public string? Password { get; set; }
}

class LoginResponse
{
    [JsonPropertyName("token")] public string? Token { get; set; }
    [JsonPropertyName("user_id")] public string? UserId { get; set; }
}

sealed class OidcTokenResponse
{
    [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
    [JsonPropertyName("sub")] public string? Subject { get; set; }
    [JsonPropertyName("preferred_username")] public string? Username { get; set; }
}

sealed class OidcIntrospectionResponse
{
    [JsonPropertyName("active")] public bool Active { get; set; }
    [JsonPropertyName("sub")] public string? Subject { get; set; }
    [JsonPropertyName("username")] public string? Username { get; set; }
    [JsonPropertyName("client_id")] public string? ClientId { get; set; }
}

class ValidateTokenRequest
{
    [JsonPropertyName("token")] public string? Token { get; set; }
}

class ValidatedTokenResponse
{
    [JsonPropertyName("subject")] public string Subject { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("agent_id")] public string? AgentId { get; set; }
    [JsonPropertyName("username")] public string? Username { get; set; }
}

class LoginEndpoint : Endpoint<LoginRequest, LoginResponse>
{
    public override void Configure()
    {
        Post("/auth/login");
        AllowAnonymous();
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        var settings = Resolve<AuthSettings>();
        if (settings.OidcEnabled)
        {
            var oidc = Resolve<OidcAuthClient>();
            var oidcResponse = await oidc.LoginAsync(req, ct);
            if (oidcResponse is null)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await HttpContext.Response.WriteAsJsonAsync(new { error = "Invalid username or password" }, cancellationToken: ct);
                return;
            }

            await HttpContext.Response.WriteAsJsonAsync(oidcResponse, cancellationToken: ct);
            return;
        }

        var db = Resolve<UserDatabase>();
        var user = db.GetByUsername(req.Username ?? "");

        if (user is null || user.PasswordHash != req.Password)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await HttpContext.Response.WriteAsJsonAsync(new { error = "Invalid username or password" }, cancellationToken: ct);
            return;
        }

        var jwt = Resolve<JwtService>();
        var token = jwt.GenerateUserToken(user.UserId, user.Username);
        await HttpContext.Response.WriteAsJsonAsync(new LoginResponse { Token = token, UserId = user.UserId }, cancellationToken: ct);
    }
}

class AgentTokenEndpoint : Endpoint<EmptyRequest, object>
{
    public override void Configure()
    {
        Get("/auth/agent/token");
        AllowAnonymous();
        Description(d => d
            .WithName("Get Agent Token")
            .WithDescription("DEMO ONLY: In production, this endpoint must require authentication (client credentials, mTLS, or API key). Currently allows any agent ID to be requested."));
    }

    public override async Task HandleAsync(EmptyRequest _, CancellationToken ct)
    {
        var requestedAgentId = HttpContext.Request.Query["agentId"].FirstOrDefault();
        var agentId = string.IsNullOrWhiteSpace(requestedAgentId) ? "identity-agent" : requestedAgentId.Trim();

        var settings = Resolve<AuthSettings>();
        if (settings.OidcEnabled)
        {
            var oidc = Resolve<OidcAuthClient>();
            var oidcToken = await oidc.GetAgentTokenAsync(agentId, ct);
            if (string.IsNullOrWhiteSpace(oidcToken))
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await HttpContext.Response.WriteAsJsonAsync(new { error = $"No OIDC client mapping found for agent '{agentId}'" }, cancellationToken: ct);
                return;
            }

            await HttpContext.Response.WriteAsJsonAsync(new { token = oidcToken, agent_id = agentId }, cancellationToken: ct);
            return;
        }

        var jwt = Resolve<JwtService>();
        var token = jwt.GenerateAgentToken(agentId);
        await HttpContext.Response.WriteAsJsonAsync(new { token, agent_id = agentId }, cancellationToken: ct);
    }
}

class ValidateTokenEndpoint : Endpoint<ValidateTokenRequest, object>
{
    public override void Configure()
    {
        Post("/auth/validate");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ValidateTokenRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Token))
        {
            HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await HttpContext.Response.WriteAsJsonAsync(new { error = "Missing token" }, cancellationToken: ct);
            return;
        }

        var settings = Resolve<AuthSettings>();
        if (settings.OidcEnabled)
        {
            var oidc = Resolve<OidcAuthClient>();
            var oidcResult = await oidc.ValidateTokenAsync(req.Token, ct);
            if (oidcResult is null)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await HttpContext.Response.WriteAsJsonAsync(new { error = "Invalid or expired token" }, cancellationToken: ct);
                return;
            }

            await HttpContext.Response.WriteAsJsonAsync(oidcResult, cancellationToken: ct);
            return;
        }

        var jwt = Resolve<JwtService>();
        var principal = jwt.ValidateToken(req.Token);
        if (principal is null)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await HttpContext.Response.WriteAsJsonAsync(new { error = "Invalid or expired token" }, cancellationToken: ct);
            return;
        }

        var response = new ValidatedTokenResponse
        {
            Subject = principal.FindFirst("sub")?.Value ?? "",
            Type = principal.FindFirst("type")?.Value ?? "",
            AgentId = principal.FindFirst("agent_id")?.Value,
            Username = principal.FindFirst("username")?.Value
        };

        await HttpContext.Response.WriteAsJsonAsync(response, cancellationToken: ct);
    }
}

sealed class HealthEndpoint : HealthEndpointBase;
