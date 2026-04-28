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
    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.UTF8.GetBytes("your-256-bit-secret-your-256-bit-secret"); // Replace with your actual secret key

    var validationResult = await tokenHandler.ValidateTokenAsync(req.Token, new TokenValidationParameters
    {
      ValidateIssuerSigningKey = true,
      IssuerSigningKey = new SymmetricSecurityKey(key),
      ValidateIssuer = false,
      ValidateAudience = false,
      ClockSkew = TimeSpan.Zero
    });

    var response = new ValidateTokenResponse
    {
      IsValid = validationResult.IsValid,
    };

    await Send.OkAsync(response, cancellation: ct);
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
