using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace FastEndpointsReactApi;

public static class AuthTokenSettings
{
  public const string Audience = "https://localhost:5002";
  public const string Issuer = "https://localhost:5002";
  public const string ValidPassword = "SecureDevPassword123!";
  public const string SigningSecret = "your-256-bit-secret-your-256-bit-secret";

  public static readonly SymmetricSecurityKey SigningKey =
    new(Encoding.UTF8.GetBytes(SigningSecret));
}
