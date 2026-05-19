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



static string GenerateSecretKey()
{
    var key = new byte[32];
    using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
    {
        rng.GetBytes(key);
    }
    return Convert.ToBase64String(key);
}

const string IdentityHostUrl = "http://localhost:5050";

var bld = WebApplication.CreateBuilder(args);
bld.WebHost.UseUrls(IdentityHostUrl);

var jwtSecret = bld.Configuration["JWT_SECRET_KEY"] ?? GenerateSecretKey();
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

// Initialize in-memory user database with demo users
var userDb = new UserDatabase();
userDb.SeedDemoUsers();

// Initialize agent database from configuration
var agentDb = new AgentDatabase();
agentDb.SeedAgentsFromConfig(bld.Configuration);

bld.Services.AddSingleton(userDb);
bld.Services.AddSingleton(agentDb);
bld.Services.AddSingleton(new JwtService(signingKey, jwtSecret));
bld.Services.AddFastEndpoints();

var app = bld.Build();
app.UseFastEndpoints();

// ============ Endpoints ============












// ============ Domain Models ============

// ============ Services ============

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
    public int ExpiresIn { get; set; } = 3600;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "Bearer";
}

sealed class AgentTokenRequest
{
    [JsonPropertyName("agentId")]
    public string? AgentId { get; set; }

    [JsonPropertyName("agentSecret")]
    public string? AgentSecret { get; set; }
}

sealed class TokenResponse
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("expiresIn")]
    public int ExpiresIn { get; set; } = 3600;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "Bearer";
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
    public Dictionary<string, object>? Claims { get; set; }
}

sealed class HealthResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "healthy";

    [JsonPropertyName("service")]
    public string Service { get; set; } = "identity";
}

sealed class LoginEndpoint : Endpoint<LoginRequest, LoginResponse>
{
    private readonly UserDatabase _userDb;
    private readonly JwtService _jwtService;

    public LoginEndpoint(UserDatabase userDb, JwtService jwtService)
    {
        _userDb = userDb;
        _jwtService = jwtService;
    }

    public override void Configure()
    {
        Post("/auth/login");
        AllowAnonymous();
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
        {
            ThrowError(r => r.Problem = "Username and password required", statusCode: 400);
            return;
        }

        var user = _userDb.GetUser(req.Username, req.Password);
        if (user == null)
        {
            await SendAsync(new { error = "Invalid credentials" }, statusCode: 401, cancellation: ct);
            return;
        }

        var token = _jwtService.GenerateUserToken(user);
        await SendAsync(new LoginResponse { Token = token }, cancellation: ct);
    }
}

sealed class AgentTokenEndpoint : Endpoint<AgentTokenRequest, TokenResponse>
{
    private readonly AgentDatabase _agentDb;
    private readonly JwtService _jwtService;

    public AgentTokenEndpoint(AgentDatabase agentDb, JwtService jwtService)
    {
        _agentDb = agentDb;
        _jwtService = jwtService;
    }

    public override void Configure()
    {
        Get("/auth/agent/token");
        AllowAnonymous();
    }

    public override async Task HandleAsync(AgentTokenRequest req, CancellationToken ct)
    {
        // Get credentials from query string or body
        var agentId = HttpContext.Request.Query["agentId"].ToString() 
            ?? req.AgentId 
            ?? HttpContext.Request.Query["agent_id"].ToString();
        var agentSecret = HttpContext.Request.Query["agentSecret"].ToString() 
            ?? req.AgentSecret 
            ?? HttpContext.Request.Query["agent_secret"].ToString();

        if (string.IsNullOrWhiteSpace(agentId) || string.IsNullOrWhiteSpace(agentSecret))
        {
            await SendAsync(new { error = "agentId and agentSecret required" }, statusCode: 400, cancellation: ct);
            return;
        }

        var agent = _agentDb.GetAgent(agentId, agentSecret);
        if (agent == null)
        {
            await SendAsync(new { error = "Invalid agent credentials" }, statusCode: 401, cancellation: ct);
            return;
        }

        var token = _jwtService.GenerateAgentToken(agent);
        await SendAsync(new TokenResponse { Token = token }, cancellation: ct);
    }
}

sealed class ValidateTokenEndpoint : Endpoint<ValidateTokenRequest, ValidateTokenResponse>
{
    private readonly JwtService _jwtService;

    public ValidateTokenEndpoint(JwtService jwtService)
    {
        _jwtService = jwtService;
    }

    public override void Configure()
    {
        Post("/auth/validate");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ValidateTokenRequest req, CancellationToken ct)
    {
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
        var token = authHeader?.Replace("Bearer ", "").Trim() ?? req.Token;

        if (string.IsNullOrWhiteSpace(token))
        {
            await SendAsync(new ValidateTokenResponse { Valid = false }, statusCode: 401, cancellation: ct);
            return;
        }

        var claims = _jwtService.ValidateToken(token);
        if (claims == null)
        {
            await SendAsync(new ValidateTokenResponse { Valid = false }, statusCode: 401, cancellation: ct);
            return;
        }

        var claimsDict = new Dictionary<string, object>();
        foreach (var claim in claims)
        {
            claimsDict[claim.Type] = claim.Value ?? "";
        }

        await SendAsync(new ValidateTokenResponse { Valid = true, Claims = claimsDict }, cancellation: ct);
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

sealed record Agent(string AgentId, string AgentSecret, string AgentType);

sealed class UserDatabase
{
    private readonly List<User> _users = new();

    public void SeedDemoUsers()
    {
        _users.Clear();
        _users.Add(new User("user_1", "admin", HashPassword("demo123")));
        _users.Add(new User("user_2", "user", HashPassword("user456")));
    }

    public User? GetUser(string username, string password)
    {
        var user = _users.FirstOrDefault(u => u.Username == username);
        if (user == null) return null;

        if (!VerifyPassword(password, user.PasswordHash)) return null;

        return user;
    }

    private static string HashPassword(string password)
    {
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
        _agents.Clear();

        var agents = new[]
        {
            ("CLASSIFIER_AGENT_ID", "CLASSIFIER_AGENT_SECRET", "specialist"),
            ("ASSESSOR_AGENT_ID", "ASSESSOR_AGENT_SECRET", "specialist"),
            ("ROUTER_AGENT_ID", "ROUTER_AGENT_SECRET", "specialist"),
            ("HANDLER_AGENT_ID", "HANDLER_AGENT_SECRET", "specialist"),
        };

        foreach (var (idKey, secretKey, agentType) in agents)
        {
            var agentId = config[idKey] ?? idKey.Replace("_AGENT_ID", "").ToLowerInvariant();
            var agentSecret = config[secretKey] ?? "default-secret-" + agentId;

            _agents.Add(new Agent(agentId, agentSecret, agentType));
        }
    }

    public Agent? GetAgent(string agentId, string agentSecret)
    {
        var agent = _agents.FirstOrDefault(a => a.AgentId == agentId);
        if (agent == null) return null;

        if (agent.AgentSecret != agentSecret) return null;

        return agent;
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

    public string GenerateUserToken(User user)
    {
        var claims = new List<System.Security.Claims.Claim>
        {
            new("sub", user.UserId),
            new("username", user.Username),
            new("type", "user"),
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new System.Security.Claims.ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256Signature),
        };

        var token = _tokenHandler.CreateToken(tokenDescriptor);
        return _tokenHandler.WriteToken(token);
    }

    public string GenerateAgentToken(Agent agent)
    {
        var claims = new List<System.Security.Claims.Claim>
        {
            new("sub", agent.AgentId),
            new("agent_id", agent.AgentId),
            new("agent_type", agent.AgentType),
            new("type", "agent"),
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new System.Security.Claims.ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256Signature),
        };

        var token = _tokenHandler.CreateToken(tokenDescriptor);
        return _tokenHandler.WriteToken(token);
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




app.Run();
