using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace DotnetFeAuth;

public class FETokenHandler
{
    // Ensure the secret key is at least 32 bytes (256 bits) long

    private const string ValidPassword = "SecureDevPassword123!";

    public string GenerateToken(string email, string password)
    {
        var key = "your-256-bit-secret-your-256-bit-secret"u8.ToArray();
        if (password != ValidPassword)
        {
            throw new UnauthorizedAccessException("Invalid password.");
        }

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, email),
            new Claim(JwtRegisteredClaimNames.Email, email)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = "https://localhost:5001",
            Audience = "https://localhost:5001",
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
