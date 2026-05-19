namespace Classifier.Infrastructure;

using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

public class JwtService
{
    private readonly SymmetricSecurityKey _key;

    public JwtService(string jwtSecretKey)
    {
        if (string.IsNullOrWhiteSpace(jwtSecretKey) || jwtSecretKey.Length < 32)
            throw new ArgumentException("JWT secret key must be at least 32 characters");
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey));
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
                AgentId = principal.FindFirst("agent_id")?.Value
            };

            if (!string.IsNullOrWhiteSpace(expectedType) && 
                !string.Equals(validatedToken.Type, expectedType, StringComparison.OrdinalIgnoreCase))
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

public record ValidatedToken
{
    public string Subject { get; set; } = "";
    public string Type { get; set; } = "";
    public string? AgentId { get; set; }
}
