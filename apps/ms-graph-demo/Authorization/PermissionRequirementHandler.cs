using Microsoft.AspNetCore.Authorization;

namespace MsGraphDemo;

/// <summary>
/// Handles <see cref="PermissionRequirement"/> by checking the authenticated user's token claims.
///
/// Azure AD tokens carry permissions in two claim types depending on the flow:
///   - Delegated (user) permissions → <c>scp</c> claim (space-separated string)
///   - Application permissions      → <c>roles</c> claim (individual claim per role)
///
/// This handler succeeds if the required permission is present in either claim.
/// </summary>
public class PermissionRequirementHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // Check delegated (user) permissions from the 'scp' claim (space-separated).
        var scopeClaim = context.User.FindFirst("scp")?.Value ?? string.Empty;
        var scopes = scopeClaim.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (scopes.Contains(requirement.Permission, StringComparer.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Check application permissions from individual 'roles' claims.
        var hasRole = context.User.Claims
            .Where(c => string.Equals(c.Type, "roles", StringComparison.OrdinalIgnoreCase))
            .Any(c => string.Equals(c.Value, requirement.Permission, StringComparison.OrdinalIgnoreCase));

        if (hasRole)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
