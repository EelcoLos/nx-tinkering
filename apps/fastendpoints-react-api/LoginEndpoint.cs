using FastEndpoints;
using FluentValidation;

namespace FastEndpointsReactApi;

public class LoginEndpoint(AuthTokenHandler tokenHandler) : Endpoint<LoginRequest, LoginResponse>
{
  public override void Configure()
  {
    Post("/api/login");
    Description(d => d.WithName("login").WithTags("Auth"));
    AllowAnonymous();
  }

  public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
  {
    var token = tokenHandler.GenerateToken(req.Email, req.Password);
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
    RuleFor(x => x.Password).NotEmpty().Equal(AuthTokenSettings.ValidPassword).WithMessage("Invalid password");
  }
}
