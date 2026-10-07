using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;

namespace MsGraphDemo;

/// <summary>
/// Authorization policies based on this API's <b>own</b> permissions, as exposed in its
/// Entra ID app registration. Microsoft Graph permissions (User.Read, User.ReadBasic.All, ...)
/// belong in Graph tokens, never in tokens issued for this API.
/// </summary>
public static class ApiPolicies
{
  /// <summary>Delegated scope from "Expose an API" (token claim <c>scp</c>).</summary>
  public const string AccessAsUserScope = "access_as_user";

  /// <summary>App role for daemon callers (token claim <c>roles</c>).</summary>
  public const string UsersListAppRole = "Users.List";

  public const string AccessAsUser = nameof(AccessAsUser);
  public const string ListUsers = nameof(ListUsers);

  public static void Configure(AuthorizationOptions options)
  {
    options.AddPolicy(AccessAsUser, p => p.RequireScope(AccessAsUserScope));
    options.AddPolicy(ListUsers, p => p.RequireScopeOrAppPermission([AccessAsUserScope], [UsersListAppRole]));
  }
}
