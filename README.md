# nx-tinkering

An Nx 23 monorepo for trying out .NET 10 (FastEndpoints), Angular 22 and React 19 ideas side by side.
Each app has its own README with details; this page is the map.

Run tasks through Nx: `npx nx run-many -t lint test build`, or `npx nx affected -t ...`.
.NET packages use central package management (`Directory.Packages.props`); tests run on xunit.v3 with
Microsoft.Testing.Platform (`global.json`). TypeScript projects use Vitest.

## Setup

Requires Node, the .NET SDK from `global.json`, and Docker for `a2a-docker-demo`. Once per clone:

```sh
npm ci
npm run setup
```

`npm run setup` (`scripts/setup.js`) is safe to re-run and only fills in what is missing:

- exports the HTTPS dev cert to `apps/angular-auth-example/ssl/` (run `dotnet dev-certs https --trust` once if your browser warns)
- generates a `Jwt:SigningKey` user-secret for `dotnet-fe-auth` and `fastendpoints-react-api`, never overwriting an existing one
- copies `apps/a2a-docker-demo/.env.example` to `.env`

After that, every launch config in `.vscode/launch.json` and every `.http` file works locally. Exceptions: `ms-graph-demo` needs
an Entra app registration (see its README), `x402-demo` needs a pay-to address, and `a2a-docker-demo` needs the stack running
(`npx nx run a2a-docker-demo:compose-up`).

## Projects

| Project                                                                                                          | Stack                                                            | In Nx graph           |
| ---------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------- | --------------------- |
| `apps/dotnet-fe-auth` + `apps/angular-auth-example`                                                              | FastEndpoints JWT API + zoneless Angular client                  | yes                   |
| `apps/fastendpoints-react-api` + `apps/fastendpoints-react-example` (+ `-e2e`), `libs/fastendpoints-react-state` | FastEndpoints JWT API + React client comparing Orval and Hey API | yes                   |
| `apps/nx-tinkering` (+ `-e2e`)                                                                                   | Plain Angular welcome app                                        | yes                   |
| `apps/api-demo` (+ `.Test`)                                                                                      | FastEndpoints API with OpenAPI + Scalar                          | yes                   |
| `apps/fakelogger-demo` (+ `.Test`)                                                                               | `FakeLogger<T>` log assertions                                   | yes                   |
| `apps/ms-graph-demo` (+ `.Test`)                                                                                 | Entra-secured API calling Microsoft Graph                        | yes                   |
| `libs/my-dotnet-lib`                                                                                             | Minimal NuGet library                                            | yes                   |
| `apps/a2a-docker-demo`                                                                                           | Multi-agent A2A system on Docker Compose                         | yes                   |
| `apps/a2a-demo`, `apps/a2a-demo-fastendpoints`                                                                   | Single-file A2A coordinator/specialist                           | yes (file-based apps) |
| `apps/fe_onefile`, `apps/strongytypedid`, `apps/x402-demo`                                                       | Single-file FastEndpoints apps                                   | yes (file-based apps) |

File-based apps run with `dotnet run <file>.cs` and still take package versions from `Directory.Packages.props`. The local plugin `tools/nx-plugins/dotnet-file-apps.ts` adds them to the Nx graph with `build` and `run` targets; the a2a demos become one project per folder (`a2a-demo-coordinator`, `a2a-demo-specialist`, and the same for `a2a-demo-fastendpoints`).

`tools/nx-plugins/docker-compose.ts` gives `apps/a2a-docker-demo` the targets `compose-up`, `compose-down` and `e2e` (needs Docker and a `.env` copied from `.env.example`).

## FastEndpoints + Angular (`dotnet-fe-auth`, `angular-auth-example`)

A FastEndpoints JWT API (FastEndpoints.Security) and a zoneless Angular 22 client using Signal Forms. The client
is generated from the API's OpenAPI document with Orval (Angular mode).

1. Run [Setup](#setup) once (signing key and dev cert).
2. Start the API: `npx nx run dotnet-fe-auth:run` (Scalar UI at https://localhost:5001/scalar/v1)
3. `npx nx serve angular-auth-example`; `/api` is proxied to the API.
4. After changing endpoints: `npx nx run angular-auth-example:generate-client`

The token lives in `localStorage` to keep the demo small; real apps should prefer HttpOnly cookies or a BFF.

## FastEndpoints + React (`fastendpoints-react-*`)

- React 19 + Vite 8 with the React Compiler, react-router v8, `<form action>` + `useActionState`
- Two OpenAPI clients from the same FastEndpoints spec: Hey API + TanStack Query and Orval + React Query
  (`npx nx run fastendpoints-react-example:generate-clients`)
- JWT via FastEndpoints.Security; both clients send `Authorization: Bearer`
- A slim Redux Toolkit slice (`libs/fastendpoints-react-state`) for the stack toggle and token
- Vitest unit tests; Playwright e2e with the API mocked via `page.route`

See `apps/fastendpoints-react-example/README.md` for run steps and demo credentials.

## Other .NET demos

- **api-demo**: FastEndpoints API with an OpenAPI document and Scalar UI. `ApiDemo.Test` tests the endpoint with `Factory.Create`.
- **fakelogger-demo**: `FakeLogger<T>` from `Microsoft.Extensions.Diagnostics.Testing`, asserting level, message and
  structured state (`GetStructuredStateValue`). Run with `npx nx test fakelogger-demo.Test`.
- **ms-graph-demo**: Entra-secured API exposing its own `access_as_user` scope and `Users.List` app role, calling
  Microsoft Graph on behalf of the user or as the app via Microsoft.Identity.Web. App registration steps are in its README.
- **my-dotnet-lib**: minimal library packed by the inferred `npx nx run libs-my-dotnet-lib:pack`.
- **fe_onefile**: a whole FastEndpoints API in one file: `dotnet run apps/fe_onefile/FE_OneFile.cs`.
- **strongytypedid**: Vogen strongly typed IDs in JSON bodies and route params: `dotnet run apps/strongytypedid/app.cs`.
- **x402-demo**: `/x402/premium` gated behind x402 v2 payments, with a MetaMask test page: `dotnet run apps/x402-demo/app.cs`.

## A2A demos

Both target the A2A v1.0 spec. The .NET SDKs (`A2A`/`A2A.AspNetCore` and `FastEndpoints.A2A`) are still prerelease.

- **a2a-demo**: keyless coordinator → specialist agents on the A2A .NET SDK. See `apps/a2a-demo/README.md`.
- **a2a-demo-fastendpoints**: the same design with FastEndpoints.A2A skills. See `apps/a2a-demo-fastendpoints/README.md`.
- **a2a-docker-demo**: an API backend orchestrating four specialist agents (classifier, assessor, router, handler)
  over A2A JSON-RPC, with agent JWTs from an identity service in front of Keycloak. Each agent publishes a public
  agent card declaring its bearer security scheme; traces go to Tempo/Grafana.
  Run `docker compose -f apps/a2a-docker-demo/docker-compose.local.yml up --build -d`.
