using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace A2ADemo.Identity;

public sealed class JwtService(Func<string> getJwtSecretKey)
{
    private SymmetricSecurityKey CreateKey() => new(Encoding.UTF8.GetBytes(getJwtSecretKey()));

    public string GenerateUserToken(string userId, string username)
    {
        var handler = new JwtSecurityTokenHandler();
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim("sub", userId),
                new Claim("username", username),
                new Claim("type", "user")
            ]),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(CreateKey(), SecurityAlgorithms.HmacSha256)
        };

        return handler.WriteToken(handler.CreateToken(descriptor));
    }

    public string GenerateAgentToken(string agentId)
    {
        var handler = new JwtSecurityTokenHandler();
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim("sub", agentId),
                new Claim("agent_id", agentId),
                new Claim("type", "agent")
            ]),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(CreateKey(), SecurityAlgorithms.HmacSha256)
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
                IssuerSigningKey = CreateKey(),
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

    public ValidatedToken? ToValidatedToken(string token)
    {
        var principal = ValidateToken(token);
        return principal is null
            ? null
            : new ValidatedToken(
                principal.FindFirst("sub")?.Value ?? string.Empty,
                principal.FindFirst("type")?.Value ?? string.Empty,
                principal.FindFirst("agent_id")?.Value,
                principal.FindFirst("username")?.Value);
    }
}