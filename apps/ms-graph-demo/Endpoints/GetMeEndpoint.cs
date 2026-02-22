using FastEndpoints;

namespace MsGraphDemo;

/// <summary>
/// Returns basic profile information for the authenticated user.
///
/// Secured with the "User.Read" policy, which is dynamically registered at startup
/// via <see cref="PermissionRequirement"/> so the same pattern scales to any number
/// of Graph permissions without repeating boilerplate for each one.
///
/// FastEndpoints wires this to ASP.NET Core's standard authorization pipeline via
/// <c>Policies("User.Read")</c> — identical to [Authorize(Policy = "User.Read")]
/// on a controller action.
/// </summary>
public class GetMeEndpoint : EndpointWithoutRequest<MeResponse>
{
    public override void Configure()
    {
        Get("/me");
        Policies("User.Read");
        Description(d => d.WithName("GetMe").WithTags("Graph"));
    }

    public override Task HandleAsync(CancellationToken ct)
    {
        var user = HttpContext.User;
        var response = new MeResponse
        {
            DisplayName = user.FindFirst("name")?.Value ?? user.Identity?.Name ?? string.Empty,
            Email = user.FindFirst("preferred_username")?.Value ?? user.FindFirst("email")?.Value ?? string.Empty,
            ObjectId = user.FindFirst("oid")?.Value ?? user.FindFirst("sub")?.Value ?? string.Empty,
        };
        return Send.OkAsync(response, ct);
    }
}

public class MeResponse
{
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ObjectId { get; set; } = string.Empty;
}
