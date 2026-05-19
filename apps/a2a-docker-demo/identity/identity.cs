#:sdk Microsoft.NET.Sdk.Web
#:package FastEndpoints@8.*-*
#:package System.IdentityModel.Tokens.Jwt@7.*
#:package Microsoft.IdentityModel.Tokens@7.*
#:property ManagePackageVersionsCentrally=false
using FastEndpoints;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

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

    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

sealed class AgentTokenRequest
{
    [JsonPropertyName("agent_id")]
    public string? AgentId { get; set; }

    [JsonPropertyName("agent_secret")]
    public string? AgentSecret { get; set; }
}

sealed class TokenResponse
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("agent_id")]
    public string? AgentId { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; } = 3600;

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

sealed class ValidateTokenRequest
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }
}

sealed class ValidateTokenResponse
{
    [JsonPropertyName("valid")]
    public bool Valid { get; set; }

    [JsonPropertyName("claims")]
    public Dictionary<string, string>? Claims { get; set; }
}

sealed class HealthResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}

sealed class LoginEndpoint : Endpoint<LoginRequest, LoginResponse>
{
    public override void Configure()
    {
        Post("/auth/login");
        AllowAnonymous();
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        var userDb = HttpContext.RequestServices.GetRequiredService<UserDatabase>();
        var jwtService = HttpContext.RequestServices.GetRequiredService<JwtService>();

        if (string.IsNullOrEmpty(req.Username) || string.IsNullOrEmpty(req.Password))
        {
            ThrowError(r =>
            {
                r.StatusCode = StatusCodes.Status400BadRequest;
                r.Message = "Username and password required";
            });
            return;
        }

        var user = userDb.ValidateUser(req.Username, req.Password);
        if (user == null)
        {
            ThrowError(r =>
            {
                r.StatusCode = StatusCodes.Status401Unauthorized;
                r.Message = "Invalid credentials";
            });
            return;
        }

        var token = jwtService.GenerateUserToken(user.UserId, user.Username);
        await SendAsync(new LoginResponse
        {
            Token = token,
            UserId = user.UserId,
            Message = "Login successful"
        });
    }
}

sealed class AgentTokenEndpoint : Endpoint<AgentTokenRequest, TokenResponse>
{
    public override void Configure()
    {
        Get("/auth/agent/token");
        AllowAnonymous();
    }

    public override async Task HandleAsync(AgentTokenRequest req, CancellationToken ct)
    {
        var agentDb = HttpContext.RequestServices.GetRequiredService<AgentDatabase>();
        var jwtService = HttpContext.RequestServices.GetRequiredService<JwtService>();

        if (string.IsNullOrEmpty(req.AgentId) || string.IsNullOrEmpty(req.AgentSecret))
        {
            ThrowError(r =>
            {
                r.StatusCode = StatusCodes.Status400BadRequest;
                r.Message = "Agent ID and secret required";
            });
            return;
        }

        var agent = agentDb.ValidateAgent(req.AgentId, req.AgentSecret);
        if (agent == null)
        {
            ThrowError(r =>
            {
                r.StatusCode = StatusCodes.Status401Unauthorized;
                r.Message = "Invalid agent credentials";
            });
            return;
        }

        var token = jwtService.GenerateAgentToken(agent.AgentId, agent.AgentType);
        await SendAsync(new TokenResponse
        {
            Token = token,
            AgentId = agent.AgentId,
            Message = "Agent token generated"
        });
    }
}

sealed class ValidateTokenEndpoint : Endpoint<ValidateTokenRequest, ValidateTokenResponse>
{
    public override void Configure()
    {
        Post("/auth/validate");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ValidateTokenRequest req, CancellationToken ct)
    {
        var jwtService = HttpContext.RequestServices.GetRequiredService<JwtService>();

        if (string.IsNullOrEmpty(req.Token))
        {
            ThrowError(r =>
            {
                r.StatusCode = StatusCodes.Status400BadRequest;
                r.Message = "Token required";
            });
            return;
        }

        var claims = jwtService.ValidateToken(req.Token);
        if (claims == null)
        {
            await SendAsync(new ValidateTokenResponse { Valid = false });
            return;
        }

        await SendAsync(new ValidateTokenResponse
        {
            Valid = true,
            Claims = claims.ToDictionary(c => c.Type, c => c.Value)
        });
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
        await SendAsync(new HealthResponse
        {
            Status = "healthy",
            Timestamp = DateTime.UtcNow
        });
    }
}

sealed record Agent(string AgentId, string AgentSecret, string AgentType);

sealed class UserDatabase
{
    private sealed record User(string UserId, string Username, string PasswordHash);

    private readonly List<User> _users = new();

    public void SeedDemoUsers()
    {
        _users.Add(new User("user1", "admin", HashPassword("demo123")));
        _users.Add(new User("user2", "user", HashPassword("user456")));
    }

    public (string UserId, string Username)? ValidateUser(string username, string password)
    {
        var user = _users.FirstOrDefault(u => u.Username == username);
        return user != null && VerifyPassword(password, user.PasswordHash)
            ? (user.UserId, user.Username)
            : null;
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    private static bool VerifyPassword(string password, string hash)
    {
        var hashOfInput = HashPassword(password);
        return hashOfInput == hash;
    }
}

sealed class AgentDatabase
{
    private readonly List<Agent> _agents = new();

    public void SeedAgentsFromConfig(IConfiguration config)
    {
        var agents = new[]
        {
            (config["CLASSIFIER_AGENT_ID"], config["CLASSIFIER_AGENT_SECRET"], "classifier"),
            (config["ASSESSOR_AGENT_ID"], config["ASSESSOR_AGENT_SECRET"], "assessor"),
            (config["ROUTER_AGENT_ID"], config["ROUTER_AGENT_SECRET"], "router"),
            (config["HANDLER_AGENT_ID"], config["HANDLER_AGENT_SECRET"], "handler"),
        };

        foreach (var (id, secret, type) in agents)
        {
            if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(secret))
            {
                _agents.Add(new Agent(id, secret, type));
            }
        }
    }

    public Agent? ValidateAgent(string agentId, string agentSecret)
    {
        return _agents.FirstOrDefault(a => a.AgentId == agentId && a.AgentSecret == agentSecret);
    }

    public IEnumerable<Agent> GetAllAgents()
    {
        return _agents.AsReadOnly();
    }
}

sealed class JwtService
{
    private readonly SymmetricSecurityKey _signingKey;
    private readonly string _jwtSecret;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    public JwtService(SymmetricSecurityKey signingKey, string jwtSecret)
    {
        _signingKey = signingKey;
        _jwtSecret = jwtSecret;
    }

    public string GenerateUserToken(string userId, string username)
    {
        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new System.Security.Claims.Claim("sub", userId),
            new System.Security.Claims.Claim("username", username),
            new System.Security.Claims.Claim("type", "user"),
        };

        var token = new JwtSecurityToken(
            issuer: "IdentityService",
            audience: "All",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return _tokenHandler.WriteToken(token);
    }

    public string GenerateAgentToken(string agentId, string agentType)
    {
        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new System.Security.Claims.Claim("sub", agentId),
            new System.Security.Claims.Claim("agent_id", agentId),
            new System.Security.Claims.Claim("agent_type", agentType),
            new System.Security.Claims.Claim("type", "agent"),
        };

        var token = new JwtSecurityToken(
            issuer: "IdentityService",
            audience: "All",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials);

        return _tokenHandler.WriteToken(token);
    }

    public System.Security.Claims.ClaimsPrincipal? ValidateTokenInternal(string token)
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
            return principal;
        }
        catch
        {
            return null;
        }
    }

    public System.Security.Claims.ClaimCollection? ValidateToken(string token)
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

const string IdentityHostUrl = "http://localhost:5050";

string GenerateSecretKey()
{
    var key = new byte[32];
    using (var rng = RandomNumberGenerator.Create())
    {
        rng.GetBytes(key);
    }
    return Convert.ToBase64String(key);
}

var bld = WebApplication.CreateBuilder(args);
bld.WebHost.UseUrls(IdentityHostUrl);

var jwtSecret = bld.Configuration["JWT_SECRET_KEY"] ?? GenerateSecretKey();
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

var userDb = new UserDatabase();
userDb.SeedDemoUsers();

var agentDb = new AgentDatabase();
agentDb.SeedAgentsFromConfig(bld.Configuration);

bld.Services.AddSingleton(userDb);
bld.Services.AddSingleton(agentDb);
bld.Services.AddSingleton(new JwtService(signingKey, jwtSecret));
bld.Services.AddFastEndpoints();

var app = bld.Build();
app.UseFastEndpoints();

app.Run();
