using FastEndpoints;
using System.Threading;
using System.Threading.Tasks;

namespace DotnetFeAuth;

public class LoginEndpoint(FETokenHandler tokenHandler) : Endpoint<LoginRequest>
{
    public override void Configure()
    {
        Post("/api/login");
        Description(d => d.WithName("login"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        var token = tokenHandler.GenerateToken("test", "SecureDevPassword123!");
        Console.WriteLine(token);
        await SendAsync(new { Token = token }, cancellation: ct);
    }
}
public class LoginRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
}
