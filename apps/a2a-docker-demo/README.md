# A2A Protocol Docker Demo - Triage Workflow

This is a comprehensive demonstration of the FastEndpoints A2A (Agent-to-Agent) protocol with real network communication, service discovery, and identity management. The demo implements a triage workflow where multiple specialist A2A services work together to classify, assess, route, and handle incoming requests.

## Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                      Docker Stack Network                            │
├─────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  ┌──────────────────────────┐  ┌──────────────────┐  ┌────────────┐ │
│  │  Identity Service        │  │  Static Website  │  │  API       │ │
│  │  (Port 5050)             │  │  (Port 8080)     │  │  Backend   │ │
│  │  - User authentication   │  │  - User login    │  │  (Port     │ │
│  │  - Agent token issuance  │  │  - Dashboard     │  │  5056)     │ │
│  │  - Token validation      │  └──────────────────┘  └────────────┘ │
│  └──────────────────────────┘           │                    │      │
│           ▲                              └────┬───────────────┘      │
│           │ (JWT validation)                  │                     │
│    ┌──────┴──────────────────────────────────┘                      │
│    │                                                                  │
│  ┌──────────────────────────┐  ┌──────────────────┐                 │
│  │ Discovery Service        │  │ Classifier       │                 │
│  │ (Port 5051)              │  │ (Port 5052)      │                 │
│  │ - Service registry       │  │ - A2A Specialist │                 │
│  │ - Agent lookup           │  │ - JWT-protected  │                 │
│  └──────────────────────────┘  └──────────────────┘                 │
│           ▲                             │                           │
│           │                             ▼                           │
│  ┌────────────────────────────┐  ┌──────────────────┐               │
│  │ Router Service             │  │ Assessor         │               │
│  │ (Port 5054)                │  │ (Port 5053)      │               │
│  │ - A2A Specialist           │  │ - A2A Specialist │               │
│  │ - JWT-protected            │  │ - JWT-protected  │               │
│  └────────────────────────────┘  └──────────────────┘               │
│           │                             │                           │
│           └──────────┬──────────────────┘                           │
│                      ▼                                              │
│                ┌──────────────────┐                                 │
│                │ Handler          │                                 │
│                │ (Port 5055)      │                                 │
│                │ - A2A Specialist │                                 │
│                │ - JWT-protected  │                                 │
│                └──────────────────┘                                 │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

## Services

### Identity Service (Port 5050)
Central authentication and token issuance service for both users and agents.

**Endpoints:**
- `POST /auth/login` - User login (returns JWT)
- `GET /auth/agent/token` - Demo agent token issuance by `agentId`
- `POST /auth/validate` - Token validation
- `GET /health` - Health check

**Demo Users:**
- Username: `admin`, Password: `demo123`
- Username: `user`, Password: `user456`

### Discovery Service (Port 5051)
Legacy registry service retained for compatibility experiments. The active triage flow no longer depends on service self-registration and discovers specialists directly from their protected agent cards.

**Endpoints:**
- `GET /services` - List the registry contents (agent JWT required)
- `GET /services/{id}/card` - Get a legacy registered card (agent JWT required)
- `GET /health` - Health check

### Classifier Service (Port 5052)
First specialist in the triage flow. Analyzes incoming requests and determines their type and urgency.

**Skills:**
- `classifier_skill` - Classifies requests as technical_issue, inquiry, defect, feature_request, or general

**Urgency Levels:** critical, high, normal, low

### Assessor Service (Port 5053)
Second specialist. Assigns priority levels based on classification.

**Skills:**
- `assessor_skill` - Assigns priority (1-5) and determines handler assignment

**Output:** Priority level, assigned handler

### Router Service (Port 5054)
Third specialist. Routes requests to appropriate handlers based on priority.

**Skills:**
- `router_skill` - Determines routing queue and estimated wait time

**Routing Queues:** urgent_queue, standard_queue, low_priority_queue

### Handler Service (Port 5055)
Final specialist. Processes and resolves triage requests, creates tickets.

**Skills:**
- `handler_skill` - Executes resolution, creates tickets

**Output:** Ticket ID, resolution strategy

### API Backend (Port 5056)
HTTP gateway for the website. Coordinates the triage workflow.

**Endpoints:**
- `POST /api/auth/login` - Forward user login to identity service
- `GET /api/services` - List available services (user JWT required)
- `POST /api/triage` - Submit triage request (user JWT required)
- `GET /api/triage/{id}` - Fetch a triage result by id (user JWT required)
- `GET /.well-known/agent-card.json` - A2A agent card (agent JWT required)
- `POST /a2a` - A2A JSON-RPC endpoint (agent JWT required)
- `GET /health` - Health check

### Static Website (Port 8080)
User interface for the demo, served as static files by nginx in Docker.

**Features:**
- Interactive login
- Service discovery viewer
- Triage request form
- Request history with flow visualization
- Observability deep-link to Grafana

## Running Locally (for Development)

### Prerequisites
- .NET 10 SDK
- Docker

### 1. Setup Environment

```bash
cd apps/a2a-docker-demo
cp .env.example .env
# Edit .env and set proper values for:
# - JWT_SECRET_KEY (min 32 characters)
# - OIDC_* values if using local Keycloak auth flow
# - OTEL_* values for trace export to Tempo
```

### 2. Recommended Local Stack

For the full experience, including Keycloak, Grafana, Tempo, and the static website:

```bash
docker compose -f docker-compose.local.yml up --build -d
```

Then open:

- **Website**: http://localhost:8080
- **API Backend**: http://localhost:5056/health
- **Identity Service**: http://localhost:5050/health
- **Keycloak**: http://localhost:8081
- **Grafana (observability)**: http://localhost:3001

### 3. Optional Source-Based Service Runs

If you want to debug the .NET services directly, run them in separate terminals:

Terminal 1 - Identity Service:
```bash
dotnet run identity/identity.csproj
```

Terminal 2 - Discovery Service:
```bash
dotnet run discovery/discovery.csproj
```

Terminal 3 - Classifier Service:
```bash
dotnet run classifier/classifier.csproj
```

Terminal 4 - Assessor Service:
```bash
dotnet run assessor/assessor.csproj
```

Terminal 5 - Router Service:
```bash
dotnet run router/router.csproj
```

Terminal 6 - Handler Service:
```bash
dotnet run handler/handler.csproj
```

Terminal 7 - API Backend:
```bash
dotnet run api-backend/api-backend.csproj
```

For the browser UI in source-based runs, keep the website container from the compose stack running or serve `website/public` from any static web server. The website is no longer a React app and does not have `npm start` scripts.

### 4. Access the Application

- **Website**: http://localhost:8080
- **API**: http://localhost:5056
- **Identity Service**: http://localhost:5050
- **Keycloak**: http://localhost:8081
- **Grafana (observability)**: http://localhost:3001

### Local HTTPS

The FastEndpoints services in this demo can run over HTTPS without changing the
endpoint code. The A2A package already emits the agent-card `supportedInterfaces`
URL from `ServiceBaseUrl`, so if the service URLs are configured as `https://...`,
the published A2A discovery surface follows that.

For local source-based runs on Windows/macOS, the workable path is:

```powershell
dotnet dev-certs https --trust
```

Then start each .NET service with HTTPS URLs and matching service base URLs, for
example:

```powershell
$env:ASPNETCORE_URLS = 'https://localhost:5056'
$env:API_BACKEND_SERVICE_URL = 'https://localhost:5056'
$env:IDENTITY_SERVICE_URL = 'https://localhost:5050'
$env:CLASSIFIER_SERVICE_URL = 'https://localhost:5052'
$env:ASSESSOR_SERVICE_URL = 'https://localhost:5053'
$env:ROUTER_SERVICE_URL = 'https://localhost:5054'
$env:HANDLER_SERVICE_URL = 'https://localhost:5055'
dotnet run api-backend/api-backend.csproj
```

The website now follows the page scheme for API and Grafana links, so serving the
UI over HTTPS will no longer force mixed `http://` requests to the API.

Current limitation: the Docker stack is still HTTP internally. Moving the full
compose stack to HTTPS requires certificate material plus trust distribution for
every container-to-container hop, and Keycloak is currently bootstrapped in
dev-mode HTTP on `8081`. If you want full Docker HTTPS next, the clean options are
either a TLS reverse proxy for browser-facing routes or a shared internal CA with
per-service certificates.

Keycloak configuration is bootstrapped automatically by the `keycloak-init` service in compose. It creates:

- Realm: `a2a-local`
- Demo users: `admin`, `user`
- OIDC clients: `website-client`, `identity-facade`, `discovery-agent`, `classifier-agent`, `assessor-agent`, `router-agent`, `handler-agent`, `api-backend-agent`

### Preloaded Grafana Dashboard

Grafana is provisioned automatically with a Tempo datasource and a dashboard for tool-calling traces:

- **Dashboard URL**: http://localhost:3001/d/a2a-tool-calling/a2a-tool-calling-overview
- **Folder**: `A2A Demo`
- **Dashboard**: `A2A Tool Calling Overview`

The dashboard is preconfigured to surface spans emitted by the API backend orchestration flow, including:

- `invoke_workflow a2a-triage`
- `execute_tool classifier`
- `execute_tool assessor`
- `execute_tool router`
- `execute_tool handler`

## Running with Docker Stack

### Prerequisites
- Docker Swarm initialized (`docker swarm init`)
- Portainer (optional, for GUI management)

### 1. Deploy Stack

```bash
cd apps/a2a-docker-demo
cp .env.example .env
# Edit .env with production values

docker stack deploy -c docker-compose.yml a2a-demo
```

### 2. Verify Services

```bash
docker service ls
docker stack ps a2a-demo
```

All services should show `1/1` replicas in the REPLICAS column.

For local development on a single machine, prefer `docker compose -f docker-compose.local.yml up --build -d` over Swarm.

### 3. Access Services

**From Docker Host (127.0.0.1):**
- **Website**: http://127.0.0.1:8080
- **API Backend**: http://127.0.0.1:5056/health
- **Identity**: http://127.0.0.1:5050/health


### 4. Run End-to-End Tests

```bash

# From Docker host (10.x.x.x or 192.x.x.x), test against
bash test-e2e.sh 10.x.x.x

# Uses default 127.0.0.1
bash test-e2e.sh
```

### 5. Remove Stack

```bash
docker stack rm a2a-demo
```

## Testing the A2A Protocol

### Quick Network Test
```bash
# Test health localhost
curl http://localhost:5050/health
```

### 1. User Login
```bash
curl -X POST http://localhost:5050/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"demo123"}'
```

### 2. Get Agent Token
```bash
curl -X GET "http://localhost:5050/auth/agent/token?agentId=classifier-agent"
```

### 3. Submit Triage Request
```bash
curl -X POST http://localhost:5056/api/triage \
  -H "Authorization: Bearer <USER_JWT>" \
  -H "Content-Type: application/json" \
  -d '{"input":"Server is down - critical issue"}'
```

### 4. List Services
```bash
curl http://localhost:5056/api/services \
  -H "Authorization: Bearer <USER_JWT>"
```

### 5. Call Protected Agent Surfaces
```bash
curl http://localhost:5051/services \
  -H "Authorization: Bearer <AGENT_JWT>"

curl http://localhost:5056/.well-known/agent-card.json \
  -H "Authorization: Bearer <AGENT_JWT>"
```

## JWT Token Format

### User JWT Claims
```json
{
  "sub": "user_id",
  "username": "admin",
  "type": "user",
  "iat": 1234567890,
  "exp": 1234571490
}
```

### Agent JWT Claims
```json
{
  "sub": "agent_id",
  "agent_id": "classifier-agent",
  "type": "agent",
  "iat": 1234567890,
  "exp": 1234571490
}
```

## A2A Communication Flow

```
User Request (with user JWT)
    ↓
[API Backend] validates user JWT
    ↓
Calls Classifier Service (with agent JWT in metadata)
    ↓
[Classifier] validates agent JWT
    ↓
Calls Assessor Service (with agent JWT)
    ↓
[Assessor] validates agent JWT
    ↓
Calls Router Service (with agent JWT)
    ↓
[Router] validates agent JWT
    ↓
Calls Handler Service (with agent JWT)
    ↓
[Handler] validates agent JWT
    ↓
Returns result through the chain
```

**All 401 responses indicate JWT validation failures.**

## Environment Variables

See `.env.example` for all configuration options.

**Key Variables:**
- `JWT_SECRET_KEY` - Secret for signing JWT tokens
- `OIDC_ENABLED` - Toggle Keycloak-backed auth vs local JWT fallback
- `OIDC_*_CLIENT_ID` / `OIDC_*_CLIENT_SECRET` - OIDC client credentials for user and agent flows
- `*_AGENT_ID` - Logical agent identifiers advertised in tokens and service metadata
- `OTEL_EXPORTER_OTLP_ENDPOINT` - Tempo/OTLP endpoint for traces

## Extending the Demo

### Adding a New Specialist Service

1. Create `apps/a2a-docker-demo/new-service/service.cs` (copy from classifier.cs pattern)
2. Add identity configuration to `.env.example` and `.env`:
   ```
   NEW_SERVICE_AGENT_ID=new-service-agent
   OIDC_NEW_SERVICE_CLIENT_ID=new-service-agent
   OIDC_NEW_SERVICE_CLIENT_SECRET=new-service-agent-secret
   ```
3. Add the new service to `docker-compose.yml` and `docker-compose.local.yml`
4. Expose its protected `/.well-known/agent-card.json` and `/a2a` surfaces
5. Update [api-backend/DownstreamGateway.cs](api-backend/DownstreamGateway.cs) to include the new service in the orchestrated flow
6. If you still want the legacy discovery service to show it, add an explicit registry entry there as well

### Adding New Users

Edit the `UserDatabase` seeding in `identity.cs` and rebuild.

### Customizing Triage Logic

Edit the classification, assessment, routing, and handling logic in respective service files.

## Troubleshooting

### Services Can't Find Each Other
- Ensure all services are running and reachable
- Check `docker network ls` for correct network
- Verify environment variables point to correct URLs

### 401 Unauthorized Errors
- Verify JWT token is valid and not expired
- Check JWT_SECRET_KEY is consistent across all services
- Verify OIDC client mappings and `*_AGENT_ID` values match what services are using
- Remember that `/a2a`, `/.well-known/agent-card.json`, and discovery `/services` require an agent token, not a user token

### Discovery Service Shows No Services
- The current triage flow does not depend on dynamic discovery registration
- The legacy registry only contains entries that are explicitly seeded or added
- Use `/api/services` on the API backend to inspect the active orchestrated service list

## Architecture Notes

- **Project-based .NET services**: Each service has its own directory with Program.cs, .csproj, and Dockerfile
- **Multi-stage Docker builds**: Services compile with the .NET 10 Alpine SDK and run on the .NET 10 Alpine ASP.NET runtime
- **Static website**: The browser UI is plain HTML/CSS/JS served by nginx
- **In-memory registries**: Discovery and identity services use in-memory storage (suitable for demo)
- **JWT validation at boundaries**: Each service validates tokens on incoming requests
- **Protected A2A surfaces**: Agent cards and `/a2a` endpoints require agent bearer tokens
- **Docker Swarm optimized**: Configured for deployment on Docker Stack with overlay network
- **Known-service orchestration**: The API backend fetches protected agent cards from known service base URLs and then calls their A2A endpoints

## Security Considerations

This is a **demonstration project**. For production:

- Use strong, randomly generated JWT_SECRET_KEY
- Implement token refresh/rotation
- Use persistent database for users and services (not in-memory)
- Implement rate limiting
- Add request logging and monitoring
- Use HTTPS/TLS for all communication
- Implement proper CORS policies
- Add authentication to all sensitive endpoints
- Implement audit logging for authorization events

## License

MIT
