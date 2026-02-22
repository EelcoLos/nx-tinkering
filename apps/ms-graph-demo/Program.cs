using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using MsGraphDemo;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// 1. Azure AD JWT bearer authentication via Microsoft.Identity.Web.
//    Configure TenantId, ClientId and Instance in appsettings.json (AzureAd section).
// ---------------------------------------------------------------------------
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration);

// ---------------------------------------------------------------------------
// 2. Register the custom PermissionRequirementHandler so ASP.NET Core's
//    authorization pipeline can evaluate PermissionRequirement policies.
//    Without this registration Policies("User.Read") will always fail even
//    though the user has the permission in their token.
// ---------------------------------------------------------------------------
builder.Services.AddSingleton<IAuthorizationHandler, PermissionRequirementHandler>();

// ---------------------------------------------------------------------------
// 3. Register one authorization policy per Graph permission dynamically.
//    This is equivalent to calling [Authorize(Policy = "User.Read")] etc.
//    on controller actions, but centralised and without repetition.
// ---------------------------------------------------------------------------
var graphPermissions = new[]
{
    "User.Read",
    "User.ReadWrite",
    "User.ReadWrite.All",
    "User.Read.All",
    "Directory.Read.All",
    "Directory.ReadWrite.All",
    "GroupMember.Read.All",
    "Group.Read.All",
    "Group.ReadWrite.All",
    "Organization.Read.All",
};

builder.Services
    .AddAuthorization(options => AddPermissionPolicies(options, graphPermissions))
    .AddFastEndpoints()
    .SwaggerDocument(o =>
    {
        o.DocumentSettings = s =>
        {
            s.Title = "MS Graph Demo API";
            s.Version = "v1";
        };
    });

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// ---------------------------------------------------------------------------
// 4. Tell FastEndpoints which JWT claim type carries delegated scopes.
//    Azure AD uses "scp" (space-separated), not the FE default "scope".
//    Similarly, app-only permissions live in the "roles" claim, not "permissions".
//    The PermissionRequirementHandler covers both, so endpoints that use
//    Policies() work for delegated AND application permission flows.
// ---------------------------------------------------------------------------
app.UseFastEndpoints(c =>
{
    c.Security.ScopeClaimType = "scp";
    c.Security.PermissionsClaimType = "roles";
})
.UseSwaggerGen();

app.Run();

// ---------------------------------------------------------------------------
// Helper: register one named policy per permission so that both FastEndpoints'
// Policies("...") and traditional [Authorize(Policy = "...")] work identically.
// ---------------------------------------------------------------------------
static void AddPermissionPolicies(AuthorizationOptions options, IEnumerable<string> permissions)
{
    foreach (var permission in permissions)
        options.AddPolicy(permission, policy => policy.Requirements.Add(new PermissionRequirement(permission)));
}
