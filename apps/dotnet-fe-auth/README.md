# dotnet-fe-auth

FastEndpoints API that issues JWTs (`POST /api/login`) and protects endpoints with JWT bearer auth (`GET /api/validate-token`, `POST /api/user/create`). Used by `angular-auth-example`.

## Run

The JWT signing key is read from configuration (`Jwt:SigningKey`) and is not stored in source. For local development, store it with user-secrets (at least 32 bytes):

```sh
dotnet user-secrets set "Jwt:SigningKey" "<random 32+ byte secret>" --project apps/dotnet-fe-auth
npx nx run dotnet-fe-auth:run
```

In other environments, set the `Jwt__SigningKey` environment variable. Issuer and audience come from `Jwt:Issuer` / `Jwt:Audience` in `appsettings.json`.

The API reference (Scalar) is at <https://localhost:5001/scalar/v1> in Development. The demo password is `SecureDevPassword123!`.

## OpenAPI spec

`wwwroot/api/v1.json` is exported by FastEndpoints.OpenApi. Regenerate it, together with the Angular client, with:

```sh
npx nx run angular-auth-example:generate-client
```
