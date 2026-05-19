# A2A Protocol Demo - Deployment Guide

## Quick Start (Local Development)

### Prerequisites
- Docker and Docker Compose installed
- Ports 5050-5056 and 8080 available

### 1. Setup Environment
```bash
cp .env.example .env
# Edit .env with your configuration
```

### 2. Build Images
```bash
docker build -t a2a-identity -f identity/Dockerfile identity
docker build -t a2a-discovery -f discovery/Dockerfile discovery
docker build -t a2a-classifier -f classifier/Dockerfile classifier
docker build -t a2a-assessor -f assessor/Dockerfile assessor
docker build -t a2a-router -f router/Dockerfile router
docker build -t a2a-handler -f handler/Dockerfile handler
docker build -t a2a-api -f api-backend/Dockerfile api-backend
docker build -t a2a-website -f website/Dockerfile website
```

### 3. Start Stack
```bash
docker compose -f docker-compose.local.yml up -d
sleep 30
docker compose -f docker-compose.local.yml ps
```

### 4. Verify Services
```bash
./test-stack.sh
```

### 5. Access Services
- **Website**: http://localhost:8080
- **API**: http://localhost:5056
- **Identity**: http://localhost:5050
- **Discovery**: http://localhost:5051
- **Classifier**: http://localhost:5052
- **Assessor**: http://localhost:5053
- **Router**: http://localhost:5054
- **Handler**: http://localhost:5055

### 6. Cleanup
```bash
docker compose -f docker-compose.local.yml down -v
```

## Production Deployment (Docker Swarm)

### Prerequisites
- Docker Swarm initialized (`docker swarm init`)
- Portainer installed (optional but recommended)

### 1. Build and Push Images
```bash
docker build -t <registry>/a2a-identity:1.0 -f identity/Dockerfile identity
docker build -t <registry>/a2a-discovery:1.0 -f discovery/Dockerfile discovery
# ... build remaining services ...
docker push <registry>/a2a-*:1.0
```

### 2. Update docker-compose.yml
Replace image names with your registry:
```yaml
services:
  identity:
    image: <registry>/a2a-identity:1.0
  # ... update all services ...
```

### 3. Create Stack
```bash
cp .env.example .env.production
# Edit .env.production with production values
docker stack deploy -c docker-compose.yml a2a-demo-triage
```

### 4. Monitor Stack
```bash
docker stack ps a2a-demo-triage
docker service logs a2a-demo-triage_identity
docker service logs a2a-demo-triage_api
```

### 5. Access via Portainer
- Open Portainer dashboard
- Go to Stacks → a2a-demo-triage
- View service status and logs
- Access services via Portainer proxy

### 6. Remove Stack
```bash
docker stack rm a2a-demo-triage
```

## Scaling Services

To scale specific services in production:
```bash
docker service scale a2a-demo-triage_classifier=3
docker service scale a2a-demo-triage_assessor=2
docker service scale a2a-demo-triage_handler=5
```

## Monitoring & Troubleshooting

### View Service Logs
```bash
docker service logs a2a-demo-triage_<service>
```

### Health Status
Each service exposes `/health` endpoint:
```bash
curl http://localhost:5050/health
curl http://localhost:5051/health
# ... etc
```

### Common Issues

**Services not communicating:**
- Verify network connectivity: `docker network inspect a2a-docker-demo_a2a-network`
- Check service DNS: `docker exec <container> nslookup identity`

**401 Unauthorized:**
- Verify JWT_SECRET_KEY is consistent across all services
- Check token expiration (default: 1 hour)
- Ensure Authorization header format: `Bearer <token>`

**Port conflicts:**
- List ports in use: `netstat -tlnp | grep LISTEN`
- Update docker-compose.yml port mappings if needed

## Environment Variables

See `.env.example` for all configurable variables:
- JWT_SECRET_KEY - Secret for token signing
- *_AGENT_ID / *_AGENT_SECRET - Service credentials
- DEMO_USER_USERNAME / DEMO_USER_PASSWORD - Demo user credentials

## Backup & Recovery

### Backup Stack Configuration
```bash
docker stack ps a2a-demo-triage > stack-backup.txt
docker service inspect a2a-demo-triage_<service> > service-backup.json
```

### Redeploy Stack
```bash
docker stack rm a2a-demo-triage
sleep 10
docker stack deploy -c docker-compose.yml a2a-demo-triage
```

