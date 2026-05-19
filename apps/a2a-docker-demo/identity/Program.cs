using FastEndpoints;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

const string JwtSecret = "your-256-bit-secret-key-must-be-min-32-chars-1234567890123";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));

builder.Services.AddSingleton<JwtService>(new JwtService(signingKey));
builder.Services.AddSingleton<UserDatabase>();
builder.Services.AddFastEndpoints();

var app = builder.Build();
app.Services.GetRequiredService<UserDatabase>().SeedDemoUsers();
app.UseFastEndpoints();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));
app.Run();

// ==================================================================

class UserDatabase
{
    private List<User> Users = new();

    public class User
    {
        public string UserId { get; set; } = "";
        public string Username { get; set; } = "";
        public string PasswordHash { get; set; } = "";
    }

    public void SeedDemoUsers() => Users.AddRange(new[]
    {
        new User { UserId = "user-1", Username = "admin", PasswordHash = "demo123" },
        new User { UserId = "user-2", Username = "user", PasswordHash = "user456" }
    });

    public User? GetByUsername(string username) => Users.FirstOrDefault(u => u.Username == username);
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
            Subject = new ClaimsIdentity(new[] { new Claim("sub", userId), new Claim("username", username), new Claim("type", "user") }),
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
            Subject = new ClaimsIdentity(new[] { new Claim("sub", agentId), new Claim("agent_id", agentId), new Claim("type", "agent") }),
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
        
        if (user == null || user.PasswordHash != req.Password)
        {
            HttpContext.Response.StatusCode = 401;
            await HttpContext.Response.WriteAsJsonAsync(new { error = "Invalid credentials" }, cancellationToken: ct);
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
        var jwt = Resolve<JwtService>();
        var token = jwt.GenerateAgentToken("identity-agent");
        HttpContext.Response.StatusCode = 200;
        await HttpContext.Response.WriteAsJsonAsync(new { token }, cancellationToken: ct);
    }
}
