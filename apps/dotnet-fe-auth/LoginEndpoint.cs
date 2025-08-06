using FastEndpoints;
using FluentValidation;

namespace DotnetFeAuth;

public class LoginEndpoint(FETokenHandler tokenHandler) : Endpoint<LoginRequest, LoginResponse>
{
  public override void Configure()
  {
    Post("/api/login");
    Description(d => d.WithName("login"));
    AllowAnonymous();
  }

  public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
  {
    var token = tokenHandler.GenerateToken(req.Email, req.Password);
    Console.WriteLine(token);
    await Send.OkAsync(new() { Token = token }, cancellation: ct);
  }
}
public class LoginRequest
{
  public required string Email { get; set; }
  public required string Password { get; set; }
}

public class LoginResponse
{
  public required string Token { get; set; }
}

public class LoginEndpointValidator : Validator<LoginRequest>
{
  public LoginEndpointValidator()
  {
    RuleFor(x => x.Email).NotEmpty().EmailAddress();
    RuleFor(x => x.Password).NotEmpty().Equal("SecureDevPassword123!").WithMessage("Invalid password");
  }
}
