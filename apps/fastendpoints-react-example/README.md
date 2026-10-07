# fastendpoints-react-example

A React 19 app that compares two OpenAPI client generators against the same
FastEndpoints backend (`apps/fastendpoints-react-api`):

- **Hey API** (`@hey-api/openapi-ts`) + TanStack Query: generated query/mutation
  options, token sent via `client.setConfig({ auth })`.
- **Orval** + React Query: generated hooks, token sent via a custom fetch
  mutator (`src/app/api-clients.ts`).

Toggle between the stacks, log in, and the protected screen calls
`GET /api/validate-token` with an `Authorization: Bearer` header. A slim Redux
slice (`libs/fastendpoints-react-state`) holds the active stack and the token.
The app uses react-router v8, `<form action>` + `useActionState` and the
React Compiler.

## Run it

```sh
# once: the API reads its JWT signing key from user-secrets
dotnet user-secrets set Jwt:SigningKey "<at least 32 random characters>" --project apps/fastendpoints-react-api

npx nx run fastendpoints-react-api:run       # https://localhost:5002 (Scalar at /scalar)
npx nx serve fastendpoints-react-example     # http://localhost:4300, proxies /api to the API
```

Demo credentials: `demo@fastendpoints.dev` / `SecureDevPassword123!` (prefilled).

## Regenerate the clients

```sh
npx nx run fastendpoints-react-example:generate-clients
```

This exports the API's OpenAPI document to
`apps/fastendpoints-react-api/wwwroot/api/v1.json` (`fastendpoints-react-api:openapi`)
and regenerates `src/generated/orval` and `src/generated/hey-api`. Both steps are
cached; `generate-orval` / `generate-hey-api` run a single generator.

## Tests

```sh
npx nx test fastendpoints-react-example        # Vitest
npx nx e2e fastendpoints-react-example-e2e     # Playwright, API mocked with page.route
```
