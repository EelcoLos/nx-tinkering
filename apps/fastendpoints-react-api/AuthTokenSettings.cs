using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace FastEndpointsReactApi;

public static class AuthTokenSettings
{
  private const string SigningSecretConfigurationKey = "Jwt:SigningSecret";
  private const string SigningSecretEnvironmentVariable = "FASTENDPOINTS_REACT_API_SIGNING_SECRET";

  public const string Audience = "https://localhost:5002";
  public const string Issuer = "https://localhost:5002";
  public const string ValidPassword = "SecureDevPassword123!";

  public static SymmetricSecurityKey CreateSigningKey(IConfiguration configuration)
  {
    var signingSecret = Environment.GetEnvironmentVariable(SigningSecretEnvironmentVariable)
      ?? configuration[SigningSecretConfigurationKey];

    if (string.IsNullOrWhiteSpace(signingSecret))
    {
      throw new InvalidOperationException(
        $"JWT signing secret is not configured. Set the '{SigningSecretEnvironmentVariable}' environment variable or '{SigningSecretConfigurationKey}' configuration value.");
    }

    return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingSecret));
  }
}
