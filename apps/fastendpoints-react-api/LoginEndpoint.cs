using FastEndpoints;
using FastEndpoints.Security;
using FluentValidation;

namespace FastEndpointsReactApi;

public class LoginEndpoint : Endpoint<LoginRequest, LoginResponse>
{
  // Demo-only credential check; a real app would verify against a user store.
  public const string DemoPassword = "SecureDevPassword123!";

  public override void Configure()
  {
    Post("/api/login");
    Description(d => d.WithName("login").WithTags("Auth"));
    AllowAnonymous();
  }

  public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
  {
    // Signing key, issuer and audience come from the JwtCreationOptions configured in Program.cs.
    var token = JwtBearer.CreateToken(o =>
    {
      o.ExpireAt = DateTime.UtcNow.AddHours(1);
      o.User.Claims.Add(("sub", req.Email), ("email", req.Email));
    });

    await Send.OkAsync(new() { AccessToken = token }, cancellation: ct);
  }
}

public class LoginRequest
{
  public required string Email { get; set; }
  public required string Password { get; set; }
}

public class LoginResponse
{
  public required string AccessToken { get; set; }
}

public class LoginEndpointValidator : Validator<LoginRequest>
{
  public LoginEndpointValidator()
  {
    RuleFor(x => x.Email).NotEmpty().EmailAddress();
    RuleFor(x => x.Password).NotEmpty().Equal(LoginEndpoint.DemoPassword).WithMessage("Invalid password");
  }
}
