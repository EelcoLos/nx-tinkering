using FastEndpoints;
using System.Security.Claims;

namespace DotnetFeAuth;

public class ValidateTokenEndpoint : EndpointWithoutRequest<ValidateTokenResponse>
{
  public override void Configure()
  {
    Get("/api/validate-token");
    Description(d => d.WithName("validatetoken"));
  }

  public override async Task HandleAsync(CancellationToken ct)
  {
    await Send.OkAsync(new() { Email = User.FindFirstValue("email")! }, cancellation: ct);
  }
}

public class ValidateTokenResponse
{
  public required string Email { get; set; }
}
