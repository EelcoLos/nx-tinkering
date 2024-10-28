using FastEndpoints;
using System.Threading;
using System.Threading.Tasks;

namespace DotnetFeAuth
{
    public class LoginEndpoint : Endpoint<LoginRequest>
    {
        public override void Configure()
        {
            Post("/api/login");
            AllowAnonymous();
        }

        public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
        {
            var tokenHandler = new TokenHandler();
            var token = tokenHandler.GenerateToken(req.Email, req.Password);
            await SendAsync(new { Token = token }, cancellation: ct);
        }
    }
}
