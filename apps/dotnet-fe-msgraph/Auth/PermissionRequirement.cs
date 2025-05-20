using Microsoft.AspNetCore.Authorization;

namespace DotnetFeMsGraph.Auth;

public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission ?? throw new ArgumentNullException(nameof(permission));
    }
}

public class PermissionRequirementHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // Get all claims of type 'scopes' from the user
        var scopeClaims = context.User.Claims
            .Where(c => c.Type == "scopes" || c.Type == "scp" || c.Type == "http://schemas.microsoft.com/identity/claims/scope")
            .ToList();

        // Check if any of the scope claims contain the required permission
        foreach (var claim in scopeClaims)
        {
            // Microsoft Graph scopes can be space-delimited in a single claim
            var scopes = claim.Value.Split(' ');
            
            if (scopes.Any(s => s.Equals(requirement.Permission, StringComparison.OrdinalIgnoreCase)))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
        }

        return Task.CompletedTask;
    }
}