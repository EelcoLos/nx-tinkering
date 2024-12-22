using FastEndpoints;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DotnetFeAuth;

public class ValidateTokenEndpoint : Endpoint<ValidateTokenRequest, ValidateTokenResponse>
{
  public override void Configure()
  {
    Post("/api/validate-token");
    Description(d => d.WithName("validatetoken"));
    AllowAnonymous();
  }

  public override async Task HandleAsync(ValidateTokenRequest req, CancellationToken ct)
  {
    var response = new ValidateTokenResponse();

    try
    {
      var tokenHandler = new JwtSecurityTokenHandler();
      var key = Encoding.UTF8.GetBytes("your-256-bit-secret-your-256-bit-secret"); // Replace with your actual secret key

      tokenHandler.ValidateToken(req.Token, new TokenValidationParameters
      {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
      }, out SecurityToken validatedToken);

      var jwtToken = (JwtSecurityToken)validatedToken;

      response.IsValid = true;
    }
    catch
    {
      response.IsValid = false;
    }

    await SendAsync(response, cancellation: ct);
  }
}

public class ValidateTokenRequest
{
  public required string Token { get; set; }
}

public class ValidateTokenResponse
{
  public bool IsValid { get; set; }
}
