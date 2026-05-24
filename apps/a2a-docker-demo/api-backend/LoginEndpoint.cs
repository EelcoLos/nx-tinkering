using FastEndpoints;
using System.Text.Json.Serialization;

namespace A2ADemo.ApiBackend;

public sealed record LoginRequest(
    string? Username,
    string? Password);

public sealed class LoginEndpoint(AuthenticationGateway authenticationGateway) : Endpoint<LoginRequest, object>
{
    public override void Configure()
    {
        Verbs(Http.POST);
        Routes("/auth/login", "/api/auth/login");
        AllowAnonymous();
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        var (statusCode, body) = await authenticationGateway.LoginAsync(req, ct);
        HttpContext.Response.StatusCode = statusCode;
        HttpContext.Response.ContentType = "application/json";
        await HttpContext.Response.WriteAsync(body, ct);
    }
}