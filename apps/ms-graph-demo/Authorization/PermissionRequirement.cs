namespace MsGraphDemo;

/// <summary>
/// An <see cref="Microsoft.AspNetCore.Authorization.IAuthorizationRequirement"/> that represents
/// a required Microsoft Graph permission (scope or app role).
/// </summary>
/// <param name="Permission">The permission value to require, e.g. "User.Read" or "User.ReadWrite.All".</param>
public record PermissionRequirement(string Permission) : Microsoft.AspNetCore.Authorization.IAuthorizationRequirement;
