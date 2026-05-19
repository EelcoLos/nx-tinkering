# A2A Protocol Docker Demo - Triage Workflow

This is a comprehensive demonstration of the FastEndpoints A2A (Agent-to-Agent) protocol with real network communication, service discovery, and identity management. The demo implements a triage workflow where multiple specialist A2A services work together to classify, assess, route, and handle incoming requests.

## Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                      Docker Stack Network                            │
├─────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  ┌──────────────────────────┐  ┌──────────────────┐  ┌────────────┐ │
│  │  Identity Service        │  │  React Website   │  │  API       │ │
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
- `GET /auth/agent/token` - Agent token issuance
- `POST /auth/validate` - Token validation
- `GET /health` - Health check

**Demo Users:**
- Username: `admin`, Password: `demo123`
- Username: `user`, Password: `user456`

### Discovery Service (Port 5051)
Service registry for A2A agents. Allows services to register themselves and discover other services.

**Endpoints:**
- `POST /discovery/register` - Register a service (agent JWT required)
- `GET /discovery/services` - List all registered services (user JWT required)
- `GET /discovery/services/{serviceId}` - Get specific service info (user JWT required)
- `POST /skills/discover-services` - A2A skill for agent-to-agent discovery
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
- `GET /health` - Health check

### React Website (Port 8080)
User interface for the demo.

**Features:**
- Interactive login
- Service discovery viewer
- Triage request form
- Request history with flow visualization
- Real-time status updates

## Running Locally (for Development)

### Prerequisites
- .NET 9 SDK
- Node.js 18+
- Docker (for full stack deployment)

### 1. Setup Environment

```bash
cd apps/a2a-docker-demo
cp .env.example .env
# Edit .env and set proper values for:
# - JWT_SECRET_KEY (min 32 characters)
# - Agent credentials
# - Demo user credentials
```

### 2. Run Services (in separate terminals)

Terminal 1 - Identity Service:
```bash
dotnet run apps/a2a-docker-demo/identity/identity.cs
```

Terminal 2 - Discovery Service:
```bash
dotnet run apps/a2a-docker-demo/discovery/discovery.cs
```

Terminal 3 - Classifier Service:
```bash
dotnet run apps/a2a-docker-demo/classifier/classifier.cs
```

Terminal 4 - Assessor Service:
```bash
dotnet run apps/a2a-docker-demo/assessor/assessor.cs
```

Terminal 5 - Router Service:
```bash
dotnet run apps/a2a-docker-demo/router/router.cs
```

Terminal 6 - Handler Service:
```bash
dotnet run apps/a2a-docker-demo/handler/handler.cs
```

Terminal 7 - API Backend:
```bash
dotnet run apps/a2a-docker-demo/api-backend/api.cs
```

Terminal 8 - React Website:
```bash
cd apps/a2a-docker-demo/website
npm install
npm start
```

### 3. Access the Application

- **Website**: http://localhost:8080
- **API**: http://localhost:5056
- **Identity Service**: http://localhost:5050

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

### 3. Access Services

- **Website**: http://localhost:8080
- **API Backend**: http://localhost:5056/health
- **Identity**: http://localhost:5050/health
- **Discovery**: http://localhost:5051/health
- **Classifier**: http://localhost:5052/health
- **Assessor**: http://localhost:5053/health
- **Router**: http://localhost:5054/health
- **Handler**: http://localhost:5055/health

### 4. Run End-to-End Tests

```bash
bash test-e2e.sh
```

### 5. Remove Stack

```bash
docker stack rm a2a-demo
```

## Testing the A2A Protocol

### 1. User Login
```bash
curl -X POST http://localhost:5050/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"demo123"}'
```

### 2. Get Agent Token
```bash
curl -X GET "http://localhost:5050/auth/agent/token?agentId=classifier_agent&agentSecret=classifier-secret-change-this"
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
curl http://localhost:5051/discovery/services \
  -H "Authorization: Bearer <USER_JWT>"
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
  "agent_id": "classifier_agent",
  "agent_type": "specialist",
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
- `CLASSIFIER_AGENT_ID` / `CLASSIFIER_AGENT_SECRET` - Classifier credentials
- `ASSESSOR_AGENT_ID` / `ASSESSOR_AGENT_SECRET` - Assessor credentials
- `ROUTER_AGENT_ID` / `ROUTER_AGENT_SECRET` - Router credentials
- `HANDLER_AGENT_ID` / `HANDLER_AGENT_SECRET` - Handler credentials

## Extending the Demo

### Adding a New Specialist Service

1. Create `apps/a2a-docker-demo/new-service/service.cs` (copy from classifier.cs pattern)
2. Add credentials to `.env.example` and `.env`:
   ```
   NEW_SERVICE_AGENT_ID=new_service_agent
   NEW_SERVICE_AGENT_SECRET=new-service-secret
   ```
3. Add service to `docker-compose.yml`
4. Service auto-registers with Discovery on startup
5. Update API backend to call new service in the triage flow

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
- Verify agent credentials in .env match what services are using

### Discovery Service Shows No Services
- Ensure specialist services are running
- Check they are sending registration requests to discovery
- Verify they have valid agent JWTs

## Architecture Notes

- **Project-based .NET services**: Each service has its own directory with Program.cs, .csproj, and Dockerfile
- **Multi-stage Docker builds**: Services compiled with .NET 11 preview SDK, deployed with lean aspnet runtime
- **Smaller images**: ~123MB per service (vs 700MB+ single-stage builds)
- **In-memory registries**: Discovery and identity services use in-memory storage (suitable for demo)
- **JWT validation at boundaries**: Each service validates tokens on incoming requests
- **No external dependencies**: Uses only standard .NET/FastEndpoints libraries
- **Docker Swarm optimized**: Configured for deployment on Docker Stack with overlay network
- **Service discovery via DNS**: Services communicate using internal service names (e.g., http://identity:5050)

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
