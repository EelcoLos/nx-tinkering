using FastEndpoints;
using System.Text.Json.Serialization;

namespace A2ADemo.Identity;

public sealed record LoginRequest(
    [property: JsonPropertyName("username")] string? Username,
    [property: JsonPropertyName("password")] string? Password);

public sealed record LoginResponse(
    [property: JsonPropertyName("token")] string? Token,
    [property: JsonPropertyName("user_id")] string? UserId);

public sealed class LoginEndpoint(
    AuthSettings settings,
    OidcAuthClient oidcAuthClient,
    UserDatabase userDatabase,
    JwtService jwtService) : Endpoint<LoginRequest, LoginResponse>
{
    public override void Configure()
    {
        Post("/auth/login");
        AllowAnonymous();
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        if (settings.OidcEnabled)
        {
            var oidcResponse = await oidcAuthClient.LoginAsync(req, ct);
            if (oidcResponse is null)
            {
                AddError("Invalid username or password");
                await Send.ErrorsAsync(StatusCodes.Status401Unauthorized, ct);
                return;
            }

            await Send.OkAsync(oidcResponse, ct);
            return;
        }

        var user = userDatabase.GetByUsername(req.Username ?? string.Empty);
        if (user is null || user.PasswordHash != req.Password)
        {
            AddError("Invalid username or password");
            await Send.ErrorsAsync(StatusCodes.Status401Unauthorized, ct);
            return;
        }

        await Send.OkAsync(new LoginResponse(jwtService.GenerateUserToken(user.UserId, user.Username), user.UserId), ct);
    }
}