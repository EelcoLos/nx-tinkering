using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace FastEndpointsReactApi;

public class AuthTokenHandler(IConfiguration configuration)
{
  // Ensure the secret key is at least 32 bytes (256 bits) long
  private readonly SymmetricSecurityKey signingKey =
    AuthTokenSettings.CreateSigningKey(configuration);

  public string GenerateToken(string email, string password)
  {
    if (password != AuthTokenSettings.ValidPassword)
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
      Issuer = AuthTokenSettings.Issuer,
      Audience = AuthTokenSettings.Audience,
      SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256Signature)
    };

    var token = tokenHandler.CreateToken(tokenDescriptor);
    return tokenHandler.WriteToken(token);
  }
}
