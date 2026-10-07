# ms-graph-demo

FastEndpoints API secured with Microsoft Entra ID (via Microsoft.Identity.Web) that calls
Microsoft Graph on behalf of its callers.

## What this shows

| Concern                     | Approach                                                                                                  |
|-----------------------------|-----------------------------------------------------------------------------------------------------------|
| Who may call this API       | The API's **own** permissions: delegated scope `access_as_user`, app role `Users.List` for daemons        |
| Policy checks               | Microsoft.Identity.Web `RequireScope` / `RequireScopeOrAppPermission`, used via FastEndpoints `Policies()` |
| What the API may do in Graph | Graph permissions on the API's registration, used when it exchanges the caller's token (OBO) or as app    |

Graph permissions such as `User.Read` never appear in tokens for this API: a client asks for
`api://<client-id>/access_as_user`, and the API then gets its own Graph token.

## Endpoints

| Method | Route    | Caller needs                                   | Graph call                                                           |
|--------|----------|------------------------------------------------|----------------------------------------------------------------------|
| GET    | `/me`    | scope `access_as_user`                         | `GET /me`, on-behalf-of the user, delegated `User.Read`              |
| GET    | `/users` | scope `access_as_user` **or** app role `Users.List` | `GET /users?$top=25`: delegated `User.ReadBasic.All` (users) or application `User.Read.All` (daemons) |

## App registration (Entra admin center)

1. **App registrations > New registration**: name `ms-graph-demo-api`, single tenant. Note the
   *Application (client) ID* and *Directory (tenant) ID*.
2. **Expose an API**: set the Application ID URI to `api://<client-id>`, then **Add a scope**
   `access_as_user` (who can consent: admins and users).
3. **App roles > Create app role**: display name `Users.List`, allowed member types
   *Applications*, value `Users.List`.
4. **API permissions > Add a permission > Microsoft Graph**:
   - Delegated: `User.Read`, `User.ReadBasic.All`
   - Application: `User.Read.All` (only needed for daemon callers)

   Then **Grant admin consent**.
5. **Certificates & secrets > New client secret**, and store it with user-secrets (never in
   `appsettings.json`):

   ```bash
   dotnet user-secrets set "AzureAd:ClientSecret" "<secret>" --project apps/ms-graph-demo
   ```

6. Client apps: grant a user-facing client the delegated `access_as_user` permission, or a
   daemon client the `Users.List` application permission (admin consent), both under
   *My APIs > ms-graph-demo-api*.

Put the IDs in `appsettings.Development.json` (git-ignored) or environment variables:

```bash
AzureAd__TenantId=<tenant-id>
AzureAd__ClientId=<client-id>
AzureAd__Audience=api://<client-id>
```

## Running locally

```bash
dotnet run --project apps/ms-graph-demo/MsGraphDemo.csproj -- --environment Development
```

User-secrets and Scalar are only enabled in Development; the OpenAPI document is at
`/openapi/v1.json`. Tokens are cached in memory.
