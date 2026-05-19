using FastEndpoints;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? "your-256-bit-secret-key-must-be-min-32-chars";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

builder.Services.AddSingleton<JwtService>(new JwtService(signingKey));
builder.Services.AddSingleton<UserDatabase>();
builder.Services.AddFastEndpoints();

var app = builder.Build();
app.Services.GetRequiredService<UserDatabase>().SeedDemoUsers();
app.UseFastEndpoints();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
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
            new User { UserId = "user-1", Username = "admin", PasswordHash = "demo123" },
            new User { UserId = "user-2", Username = "user", PasswordHash = "user456" }
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
    }

    public override async Task HandleAsync(EmptyRequest _, CancellationToken ct)
    {
        var requestedAgentId = HttpContext.Request.Query["agentId"].FirstOrDefault();
        var agentId = string.IsNullOrWhiteSpace(requestedAgentId) ? "identity-agent" : requestedAgentId.Trim();
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
