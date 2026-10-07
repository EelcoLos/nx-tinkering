# A2A Docker Demo - Triage Workflow

A Docker Compose demo of the [A2A (Agent-to-Agent) protocol](https://a2a-protocol.org/latest/specification/)
built with [FastEndpoints.A2A](https://fast-endpoints.com/). A website submits a request, the API backend
orchestrates four specialist agents over A2A JSON-RPC (classify, assess, route, handle), and every hop is
traced to Grafana/Tempo. Identity is handled by a small identity service in front of Keycloak.

## Architecture

```
 Browser ──► website (nginx, :8080)
    │
    ▼  user JWT
 api-backend (:5056)  ── DownstreamGateway: central orchestrator ──┐
    │  login                                                        │ agent JWT, A2A SendMessage
    ▼                                                               ▼
 identity (:5050) ──► Keycloak (:8081)       classifier (:5052) → assessor (:5053) → router (:5054) → handler (:5055)

 discovery (:5051)  legacy registry, not used by the triage flow
 Tempo / Prometheus / Grafana (:3001)  traces and health metrics
```

The specialists never call each other. `api-backend/DownstreamGateway.cs` fetches an agent token from the
identity service, then calls each specialist's `/a2a` endpoint in sequence and feeds the result of one step
into the next:

1. `classifier` turns the input text into a classification
2. `assessor` turns the classification into a priority
3. `router` turns the priority into the next handler
4. `handler` creates a ticket and returns the outcome

## Services

| Service | Port | Nx project | Key files |
| --- | --- | --- | --- |
| identity | 5050 | `identity` | `identity/LoginEndpoint.cs`, `AgentTokenEndpoint.cs`, `ValidateTokenEndpoint.cs` |
| discovery | 5051 | `discovery` | `discovery/ServiceRegistry.cs` |
| classifier | 5052 | `classifier` | `classifier/SkillEndpoint.cs` |
| assessor | 5053 | `assessor` | `assessor/SkillEndpoint.cs` |
| router | 5054 | `router` | `router/SkillEndpoint.cs` |
| handler | 5055 | `handler` | `handler/SkillEndpoint.cs` |
| api-backend | 5056 | `api-backend` | `api-backend/DownstreamGateway.cs`, `SubmitTriageEndpoint.cs` |
| website | 8080 | - | `website/public/` (static HTML/CSS/JS) |
| common | - | `A2ADemo.Common` | shared auth, hosting and telemetry helpers |

### Skills

Each specialist exposes one FastEndpoints endpoint as an A2A skill. The orchestrator selects it with
`metadata.skill` on the A2A `SendMessage` call.

| Skill id | REST route | Input | Output |
| --- | --- | --- | --- |
| `classifier` | `POST /skills/classify` | `input` | `classification_type`: `incident`, `defect`, `feature_request`, `inquiry`, `general` |
| `assessor` | `POST /skills/assess` | `classification` | `priority`: `critical`, `high`, `medium`, `low`, `normal` |
| `router` | `POST /skills/route` | `priority` | `next_handler`: `urgent-handler`, `priority-handler`, `standard-handler`, `self-service-handler`, `general-handler` |
| `handler` | `POST /skills/handle` | `input`, `classification`, `priority` | `status`, `ticket_id`, `summary` |
| `triage_orchestration` (api-backend) | `POST /api/triage` | `input` | full triage record |

### Endpoints

- **identity**: `POST /auth/login`, `GET /auth/agent/token?agentId=...`, `POST /auth/validate`
- **discovery**: `GET /services`, `GET /services/{id}/card` (agent JWT)
- **api-backend**: `POST /api/auth/login`, `GET /api/services`, `GET /api/services/{id}/card`,
  `POST /api/triage`, `GET /api/triage/{id}` (all except login need a user JWT)
- **every A2A service** (specialists and api-backend):
  - `GET /.well-known/agent-card.json` - public. The card declares the agent bearer scheme in
    `securitySchemes`/`securityRequirements`; skills are only listed when a valid agent JWT is sent.
  - `POST /a2a` - A2A JSON-RPC endpoint (agent JWT)
  - `POST /skills/...` - REST form of the skill (agent JWT)
- **all services**: `GET /health`

## Running

### Docker Compose (recommended)

```bash
cd apps/a2a-docker-demo
cp .env.example .env   # set JWT_SECRET_KEY (32+ chars); OIDC_* and OTEL_* defaults match the compose stack
docker compose -f docker-compose.local.yml up --build -d
./test-stack.sh        # health and login smoke test (test-e2e.sh [host] adds auth checks)
```

| URL | What |
| --- | --- |
| http://localhost:8080 | website (log in as `admin` / `demo123` or `user` / `user456`) |
| http://localhost:5056/health | API backend |
| http://localhost:8081 | Keycloak (realm `a2a-local`, created by the `keycloak-init` service) |
| http://localhost:3001/d/a2a-tool-calling/a2a-tool-calling-overview | Grafana tool-calling dashboard |

Stop with `docker compose -f docker-compose.local.yml down -v`. For a Docker Swarm deployment see
[DEPLOYMENT.md](DEPLOYMENT.md).

### From source

Build everything with Nx from the repository root:

```bash
npx nx run-many -t build -p identity discovery classifier assessor router handler api-backend
```

Run a service with `dotnet run --project apps/a2a-docker-demo/<service>/<service>.csproj`. Service URLs default
to the compose host names (`http://classifier:5052` etc.), so set the `*_SERVICE_URL` variables from
`.env.example` to `http://localhost:<port>` and `ASPNETCORE_URLS` per service. Serve `website/public` with any
static web server, for example the `serve a2a website` VS Code task.

The services also run over HTTPS: trust the dev certificate (`dotnet dev-certs https --trust`) and use `https://`
in `ASPNETCORE_URLS` and the `*_SERVICE_URL` variables. The card's `supportedInterfaces` URL follows the service
base URL. The compose stack itself is HTTP only.

## Trying the A2A surface

```bash
# user login (through the API backend or directly against identity)
curl -X POST http://localhost:5056/api/auth/login -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"demo123"}'

# submit a triage request
curl -X POST http://localhost:5056/api/triage -H "Authorization: Bearer <USER_JWT>" \
  -H "Content-Type: application/json" -d '{"input":"Server is down - critical issue"}'

# public agent card (no skills), then the same card with an agent token (skills included)
curl http://localhost:5052/.well-known/agent-card.json
TOKEN=$(curl -s "http://localhost:5050/auth/agent/token?agentId=classifier-agent" | jq -r .token)
curl http://localhost:5052/.well-known/agent-card.json -H "Authorization: Bearer $TOKEN"
```

User tokens carry `type: user`, agent tokens `type: agent` plus `agent_id`. Agent surfaces reject user tokens.
With `OIDC_ENABLED=true` the identity service gets tokens from Keycloak (one client per agent); otherwise it signs
local JWTs with `JWT_SECRET_KEY`.

## Observability

All services export OpenTelemetry traces to Tempo (`OTEL_EXPORTER_OTLP_ENDPOINT`). The orchestrator emits an
`invoke_workflow a2a-triage` span with `execute_tool classifier|assessor|router|handler` children, tagged with the
GenAI semantic conventions (`gen_ai.provider.name`, `gen_ai.operation.name`, `gen_ai.tool.name`). Grafana is
provisioned with Tempo and Prometheus data sources and two dashboards in the `A2A Demo` folder.

## Extending the demo

To add a specialist:

1. Copy one of the specialist folders (for example `classifier/`), rename the project, and change
   `SkillEndpoint.cs` (route, `this.A2ASkill(id: ...)`, logic) and `ServiceSettings.cs` (name, port, env vars).
2. Add the agent id and OIDC client to `.env.example`, `keycloak/bootstrap/init-keycloak.sh` and both compose files.
3. Add the service URL to `api-backend/ServiceSettings.cs` and a step in `DownstreamGateway.RunTriageAsync`
   (plus an entry in `GetKnownServices`).

Demo users come from `identity/AuthSettings.cs` (`DEMO_USER_*` variables) and, with OIDC enabled, from the
Keycloak bootstrap script.

## Security

This is a demo: in-memory users, an unauthenticated agent token endpoint and permissive CORS. See
[SECURITY.md](SECURITY.md) before reusing any of it.
