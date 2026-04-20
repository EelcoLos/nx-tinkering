# ms-graph-demo

Demonstrates how to migrate from ASP.NET MVC `[Authorize(Policy = "...")]` to
FastEndpoints `Policies()` when using Azure AD (Microsoft Entra ID) JWT bearer
tokens and Microsoft Graph permissions.

## What this shows

| Problem                                                                | Solution                                                                                |
|------------------------------------------------------------------------|-----------------------------------------------------------------------------------------|
| `Policies("User.Read")` always returns 403                             | Register `PermissionRequirementHandler` in DI                                           |
| FastEndpoints `Scopes()` / `Permissions()` never match Azure AD tokens | Set `ScopeClaimType = "scp"` and `PermissionsClaimType = "roles"` in `UseFastEndpoints` |
| Adding a new Graph permission requires a lot of boilerplate            | `AddPermissionPolicies()` registers one named policy per permission in a loop           |

## Configuration

### ⚠️ Do not commit real credentials

The `appsettings.json` in this project contains **placeholder** values only.
Before running the app, supply real Azure AD values through one of these approaches:

**Option A – `appsettings.Development.json` (local dev, git-ignored)**

```json
{
  "AzureAd": {
    "TenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
    "ClientId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
    "Audience": "api://xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
  }
}
```

**Option B – environment variables**

```bash
AzureAd__TenantId=<tenant-id>
AzureAd__ClientId=<client-id>
AzureAd__Audience=api://<client-id>
```

**Option C – Azure Key Vault / Azure App Configuration (production)**

Load secrets via `builder.Configuration.AddAzureKeyVault(...)` or
`builder.Configuration.AddAzureAppConfiguration(...)` before `Build()`.

## Running locally

```bash
dotnet run --project apps/ms-graph-demo/MsGraphDemo.csproj
```

Scalar is available in development alongside the OpenAPI document at `/openapi/v1.json`.

## Endpoints

| Method | Route    | Required permission                     |
|--------|----------|-----------------------------------------|
| GET    | `/me`    | `User.Read` (delegated or app)          |
| GET    | `/users` | `User.ReadWrite.All` (delegated or app) |
