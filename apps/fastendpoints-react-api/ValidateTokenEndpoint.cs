using FastEndpoints;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace FastEndpointsReactApi;

public class ValidateTokenEndpoint(IConfiguration configuration) : Endpoint<ValidateTokenRequest, ValidateTokenResponse>
{
  public override void Configure()
  {
    Get("/api/validate-token");
    Description(d => d.WithName("validateToken").WithTags("Auth"));
    AllowAnonymous();
  }

  public override async Task HandleAsync(ValidateTokenRequest req, CancellationToken ct)
  {
    try
    {
      var tokenHandler = new JwtSecurityTokenHandler();
      var validationResult = await tokenHandler.ValidateTokenAsync(req.Token, new TokenValidationParameters
      {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = AuthTokenSettings.CreateSigningKey(configuration),
        ValidateIssuer = true,
        ValidIssuer = AuthTokenSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = AuthTokenSettings.Audience,
        ClockSkew = TimeSpan.Zero
      });

      await Send.OkAsync(new ValidateTokenResponse { IsValid = validationResult.IsValid }, cancellation: ct);
    }
    catch (SecurityTokenException)
    {
      await Send.OkAsync(new ValidateTokenResponse { IsValid = false }, cancellation: ct);
    }
    catch (ArgumentException)
    {
      await Send.OkAsync(new ValidateTokenResponse { IsValid = false }, cancellation: ct);
    }
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
