# A2A Protocol Docker Demo - Triage Workflow

This is a comprehensive demonstration of the FastEndpoints A2A (Agent-to-Agent) protocol with real network communication, identity management, and a complete triage workflow. The demo implements a multi-service architecture where specialist agents work together to classify, assess, route, and handle incoming requests.

## Architecture

### Services Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                      Docker Swarm Stack                              │
├─────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  ┌──────────────────────────┐  ┌──────────────────┐                 │
│  │  Identity Service        │  │  Website         │                 │
│  │  (Port 5050)             │  │  (Port 8080)     │                 │
│  │  - User authentication   │  │  - Login form    │                 │
│  │  - Agent token issuance  │  │  - Triage UI     │                 │
│  │  - A2A skills            │  │  - Results view  │                 │
│  └──────────────────────────┘  └──────────────────┘                 │
│           ▲                             │                           │
│           │                             ▼                           │
│           │                  ┌──────────────────┐                   │
│           │                  │  API Backend     │                   │
│           │                  │  (Port 5056)     │                   │
│           │                  │  - Orchestration │                   │
│           │                  │  - Login endpoint│                   │
│           │                  │  - Triage flow   │                   │
│           │                  └──────────────────┘                   │
│           │                        │ │ │ │                         │
│           └─────────────┬──────────┤ │ │ │                         │
│                         │          │ │ │ │                         │
│           ┌─────────────┼──┬───────┼─┼─┘ │                         │
│           ▼             │  │       │ │   │                         │
│  ┌──────────────────┐  │  │       │ │   │                         │
│  │ Classifier       │  │  ▼       │ │   │                         │
│  │ (Port 5052)      │  │ ┌───────────┐  │                         │
│  │ - Classify text  │  │ │ Assessor  │  │                         │
│  │ - A2A Skill      │  │ │ (5053)    │  │                         │
│  └──────────────────┘  │ └───────────┘  │                         │
│           │             │      │        │                         │
│           │             │      ▼        │                         │
│           │             │  ┌───────────┐                          │
│           └─────────────┼──│  Router   │                          │
│                         │  │ (5054)    │                          │
│                         │  └───────────┘                          │
│                         │      │                                  │
│                         │      ▼                                  │
│                         │  ┌───────────┐                          │
│                         └──│  Handler  │                          │
│                            │ (5055)    │                          │
│                            └───────────┘                          │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

## Services

### Identity Service (Port 5050)
Central authentication and token issuance service for both users and agents.

**Endpoints:**
- `POST /auth/login` - User login (returns JWT token and user_id)
- `POST /api/token` - Agent token issuance  
- `GET /health` - Health check
- `POST /a2a` - A2A JSON-RPC endpoint (requires Bearer token)

**Demo Credentials:**
- Username: `demo`, Password: `demo123`
- Username: `admin`, Password: `admin123`

**Service Discovery:**
- Services are discovered via JWT claims (no separate discovery service)
- Each token includes list of available services in claims
- Services authenticate with identity to get tokens at startup

### Classifier Service (Port 5052)
First A2A specialist in the triage flow. Analyzes incoming requests and determines their type.

**A2A Skill:**
- Skill ID: `classify-text`
- Input: Text to classify
- Output: Classification (incident, defect, inquiry, feature)

### Assessor Service (Port 5053)
Second A2A specialist. Assigns priority levels based on classification.

**A2A Skill:**
- Skill ID: `assess-priority`
- Input: Classification result
- Output: Priority (critical, high, medium, low)

### Router Service (Port 5054)
Third A2A specialist. Routes incidents to appropriate teams based on priority.

**A2A Skill:**
- Skill ID: `route-incident`
- Input: Priority level
- Output: Team assignment (ops-critical, ops-high, ops-standard, support-tier-1)

### Handler Service (Port 5055)
Final A2A specialist. Creates tickets for incidents.

**A2A Skill:**
- Skill ID: `create-ticket`
- Input: Subject and assigned team
- Output: Ticket ID and status

### API Backend (Port 5056)
Orchestration service that coordinates the triage workflow and serves the website.

**Endpoints:**
- `POST /auth/login` - User login (returns demo-user-token)
- `POST /api/triage` - Submit request for triage
- `GET /health` - Health check
- `POST /a2a` - A2A JSON-RPC endpoint (requires Bearer token)

### Website (Port 8080)
Interactive dashboard for submitting triage requests and viewing results.

**Features:**
- User login form
- Triage request submission
- Real-time workflow progress display
- Request history viewer
- Responsive design
- Auto-detects HTTP/HTTPS protocol for API calls

## Quick Start

### Prerequisites
- Docker and Docker Compose
- Docker Swarm initialized (or use local compose)

### Local Development

1. **Set environment variables:**
```bash
cp .env.example .env
# Edit .env and set JWT_SECRET_KEY to a 32+ character string
```

2. **Start the stack:**
```bash
docker compose up -d
```

3. **Access the website:**
- Open http://localhost:8080
- Login with demo/demo123
- Submit triage requests

### Docker Swarm Deployment

1. **Deploy the stack:**
```bash
docker stack deploy -c docker-compose.yml a2a-demo
```

2. **Access on Swarm:**
- Website: http://10.0.0.3:8080
- API: http://10.0.0.3:5056

## API Usage

### Login
```bash
curl -X POST http://localhost:5056/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "demo",
    "password": "demo123"
  }'
```

Response:
```json
{
  "token": "demo-user-token",
  "user_id": "demo",
  "message": "Login successful"
}
```

### Submit Triage Request
```bash
curl -X POST http://localhost:5056/api/triage \
  -H "Content-Type: application/json" \
  -d '{
    "input": "system is down"
  }'
```

Response:
```json
{
  "id": "triage-abc123def456",
  "input": "system is down",
  "status": "completed",
  "steps": [
    {
      "service": "Classifier",
      "result": "incident"
    },
    {
      "service": "Assessor",
      "result": "critical"
    },
    {
      "service": "Router",
      "result": "ops-critical"
    },
    {
      "service": "Handler",
      "result": "TKT-1779226913"
    }
  ],
  "summary": "Classified as incident, assessed critical, routed to ops-critical, ticket TKT-1779226913 created"
}
```

## A2A Protocol Implementation

All services implement the FastEndpoints A2A protocol with the following features:

- **JSON-RPC 2.0 Endpoint:** `/a2a` on each service
- **Authentication:** Bearer token in Authorization header
- **Skills Registration:** Auto-generated agent cards at `/.well-known/agent-card.json`
- **Message Format:** Standard A2A message envelope with messageId, role, parts
- **Error Handling:** Proper 401/403 responses for auth failures

### Service-to-Service Communication

Services call each other using A2A JSON-RPC messages:

```bash
# From API backend to Classifier
curl -X POST http://classifier:5052/a2a \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "SendMessage",
    "id": "msg-123",
    "params": {
      "recipient": "classifier-agent",
      "skillName": "classify-text",
      "input": "system is down"
    }
  }'
```

## Environment Variables

Create a `.env` file with these variables:

```bash
JWT_SECRET_KEY=your-secret-key-minimum-32-characters-1234567890123
```

**REQUIRED:** `JWT_SECRET_KEY` must be at least 32 characters. Services will fail to start if not set.

## Docker Images

All services are built from .NET code with multi-stage Docker builds:

- `a2a-identity:v8-a2a`
- `a2a-classifier:v8-a2a`
- `a2a-assessor:v8-a2a`
- `a2a-router:v8-a2a`
- `a2a-handler:v8-a2a`
- `a2a-api-backend:v9-a2a`
- `a2a-website:v12`

### Building Images Locally

Use `docker compose build` to rebuild images:

```bash
# Build all services
docker compose build

# Build specific service
docker compose build api-backend

# Build without cache (forces rebuild)
docker compose build --no-cache
```

## Testing

### Health Checks
```bash
curl http://localhost:5050/health  # Identity
curl http://localhost:5052/health  # Classifier
curl http://localhost:5053/health  # Assessor
curl http://localhost:5054/health  # Router
curl http://localhost:5055/health  # Handler
curl http://localhost:5056/health  # API Backend
curl http://localhost:8080/health  # Website
```

### End-to-End Workflow Test
See `A2A_PROTOCOL_GUIDE.md` for detailed testing instructions and request/response examples.

## Logging

Services log at INFO level by default. Infrastructure component logging (Microsoft.AspNetCore.Hosting.Diagnostics) is set to WARNING to reduce verbosity.

Configure logging levels in `appsettings.json` in each service directory.

## Security Considerations

### Authentication
- All A2A endpoints require Bearer token authentication (401 if missing)
- User login returns demo token (for demo purposes)
- Agent tokens are issued by Identity service at startup

### Secrets
- JWT_SECRET_KEY is loaded from environment variable
- Services fail at startup if JWT_SECRET_KEY is not set or is too short
- No credentials or secrets are hardcoded in source code

### Demo Limitations
- Uses plaintext demo credentials (not hashed)
- No bcrypt/Argon2 for production use
- No request validation/sanitization middleware
- CORS is open to all origins (for demo)

For production, implement:
- Bcrypt/Argon2 password hashing
- Request validation middleware
- Restricted CORS configuration
- Rate limiting
- API key management
- Audit logging

## Architecture Decisions

### Service Discovery via JWT Claims
- Original: Separate Discovery service (port 5051)
- Current: Service list embedded in JWT claims
- **Benefit:** Simpler architecture, no separate service, discovery built-in to auth

### Website Protocol Auto-Detection
- Website automatically detects HTTP vs HTTPS
- Uses `window.location.protocol` for protocol selection
- Works in both HTTP and HTTPS environments

### A2A-First Communication
- All inter-service communication uses A2A JSON-RPC
- No custom HTTP endpoints between services
- Standard-compliant FastEndpoints.A2A implementation

## Troubleshooting

### Services Won't Start
- Check JWT_SECRET_KEY is set in .env and is 32+ characters
- Check Docker Swarm is initialized (or use local compose)
- Check port conflicts on 5050-5056, 8080

### Login Fails
- Verify credentials are demo/demo123 or admin/admin123
- Check API backend is running: curl http://localhost:5056/health
- Check browser console for CORS errors

### Triage Request Returns Empty Steps
- Check all services are healthy: docker compose ps
- Check service logs: docker compose logs classifier
- Verify bearer token is valid in localStorage

### Website Shows Old HTML
- Clear browser cache (Ctrl+F5 / Cmd+Shift+R)
- Or rebuild website container: docker compose build --no-cache website

## Files Structure

```
a2a-docker-demo/
├── README.md                 # This file
├── A2A_PROTOCOL_GUIDE.md    # Detailed A2A protocol testing guide
├── DEPLOYMENT.md            # Deployment instructions
├── VERIFICATION.md          # Verification and testing procedures
├── docker-compose.yml       # Docker Compose configuration
├── .env.example             # Example environment variables
├── identity/                # Identity service
│   ├── Program.cs
│   ├── appsettings.json
│   ├── identity.csproj
│   └── Dockerfile
├── classifier/              # Classifier A2A service
├── assessor/                # Assessor A2A service
├── router/                  # Router A2A service
├── handler/                 # Handler A2A service
├── api-backend/             # API orchestration service
└── website/                 # Nginx + HTML website
    └── public/index.html
```

## Support

For issues or questions about the A2A protocol implementation, see:
- `A2A_PROTOCOL_GUIDE.md` - Protocol details and examples
- `VERIFICATION.md` - Testing and verification procedures
- FastEndpoints documentation: https://dev.fastendpoints-doc-site.pages.dev/

## License

Demo project - see repository for license details.
