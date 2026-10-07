using FastEndpoints;
using FastEndpoints.Security;

namespace FastEndpointsReactApi;

/// <summary>
/// Protected endpoint: the JWT bearer middleware validates the `Authorization: Bearer` header,
/// so reaching the handler means the token is valid. Returns the caller's identity.
/// </summary>
public class ValidateTokenEndpoint : EndpointWithoutRequest<ValidateTokenResponse>
{
  public override void Configure()
  {
    Get("/api/validate-token");
    Description(d => d.WithName("validateToken").WithTags("Auth"));
  }

  public override async Task HandleAsync(CancellationToken ct)
  {
    await Send.OkAsync(new()
    {
      Email = User.ClaimValue("email") ?? string.Empty,
      ExpiresAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(User.ClaimValue("exp") ?? "0")),
    }, cancellation: ct);
  }
}

public class ValidateTokenResponse
{
  public required string Email { get; set; }
  public required DateTimeOffset ExpiresAt { get; set; }
}
