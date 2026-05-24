using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace A2ADemo.Identity;

public sealed class JwtService(string jwtSecretKey)
{
    private readonly SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(jwtSecretKey));

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
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
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
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
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
                IssuerSigningKey = key,
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