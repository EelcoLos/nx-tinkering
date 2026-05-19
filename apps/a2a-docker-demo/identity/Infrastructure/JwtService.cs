using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Identity.Infrastructure;

public class JwtService
{
    private readonly string _secretKey;
    private static readonly string[] AvailableServices = new[]
    {
        "classifier:5052",
        "assessor:5053",
        "router:5054",
        "handler:5055",
        "identity:5050",
        "api-backend:5056"
    };

    public JwtService(string secretKey)
    {
        _secretKey = secretKey;
    }

    public string IssueAgentToken(string agentId, string[] scopes)
    {
        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, agentId),
            new("agent_id", agentId),
            new("type", "agent")
        };

        foreach (var scope in scopes)
        {
            claims.Add(new("scope", scope));
        }

        foreach (var service in AvailableServices)
        {
            claims.Add(new("service", service));
        }

        var token = new JwtSecurityToken(
            issuer: "a2a-identity-provider",
            audience: "a2a-agents",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
