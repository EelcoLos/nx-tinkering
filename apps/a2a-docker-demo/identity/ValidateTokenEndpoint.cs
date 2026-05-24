using A2ADemo.Common;
using FastEndpoints;

namespace A2ADemo.Identity;

public sealed class ValidateTokenEndpoint(
    AuthSettings settings,
    OidcAuthClient oidcAuthClient,
    JwtService jwtService) : Endpoint<ValidateTokenRequest, ValidatedToken>
{
    public override void Configure()
    {
        Post("/auth/validate");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ValidateTokenRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Token))
        {
            AddError("Missing token");
            await Send.ErrorsAsync(StatusCodes.Status401Unauthorized, ct);
            return;
        }

        if (settings.OidcEnabled)
        {
            var oidcResult = await oidcAuthClient.ValidateTokenAsync(req.Token, ct);
            if (oidcResult is null)
            {
                AddError("Invalid or expired token");
                await Send.ErrorsAsync(StatusCodes.Status401Unauthorized, ct);
                return;
            }

            await Send.OkAsync(oidcResult, ct);
            return;
        }

        var validatedToken = jwtService.ToValidatedToken(req.Token);
        if (validatedToken is null)
        {
            AddError("Invalid or expired token");
            await Send.ErrorsAsync(StatusCodes.Status401Unauthorized, ct);
            return;
        }

        await Send.OkAsync(validatedToken, ct);
    }
}