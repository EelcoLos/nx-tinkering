# A2A Protocol Docker Demo - Verification Guide

This guide provides step-by-step verification procedures for the A2A triage workflow implementation.

## Quick Health Check

Verify all services are running:

```bash
docker compose ps
```

Expected: All 7 services should show "Up (healthy)"

Check individual service health:

```bash
curl http://localhost:5050/health  # Identity
curl http://localhost:5052/health  # Classifier
curl http://localhost:5053/health  # Assessor
curl http://localhost:5054/health  # Router
curl http://localhost:5055/health  # Handler
curl http://localhost:5056/health  # API Backend
curl http://localhost:8080/health  # Website
```

All should return: `{"status":"healthy","service":"<service-name>"}`

---

## Authentication Testing

### Test User Login (via API Backend)

**SUCCESS CASE:**
```bash
curl -X POST http://localhost:5056/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"demo","password":"demo123"}'
```

Expected Response:
```json
{
  "token": "demo-user-token",
  "user_id": "demo",
  "message": "Login successful"
}
```

**FAILURE CASE (Wrong Password):**
```bash
curl -X POST http://localhost:5056/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"demo","password":"wrong"}'
```

Expected: HTTP 401 Unauthorized

---

## A2A Endpoint Testing

All services expose the A2A protocol at `/a2a` endpoint.

### Get Agent Card (Service Discovery)

```bash
curl -s "http://localhost:5052/.well-known/agent-card.json" | jq .
```

Expected: JSON agent card with:
- `agentName`: Service identifier (e.g., "classifier-agent")
- `skills`: List of registered A2A skills
- `description`: Human-readable description
- `version`: Version number

### A2A Authentication Requirement

**WITHOUT Bearer Token (should fail with 401):**
```bash
curl -X POST http://localhost:5052/a2a \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"SendMessage","id":"test"}'
```

Expected: HTTP 401 Unauthorized with `{"error":"Unauthorized"}`

**WITH Bearer Token (should succeed):**
```bash
curl -X POST http://localhost:5052/a2a \
  -H "Authorization: Bearer test-token" \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc":"2.0",
    "method":"SendMessage",
    "id":"test-123",
    "params":{"recipient":"classifier-agent","skillName":"classify-text","input":"test"}
  }'
```

Expected: HTTP 200 with skill response

---

## Triage Workflow Testing

### Submit Triage Request

```bash
curl -X POST http://localhost:5056/api/triage \
  -H "Content-Type: application/json" \
  -d '{"input":"critical server failure"}'
```

Expected Response:
```json
{
  "id": "triage-abc123def456",
  "input": "critical server failure",
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

### Verify Workflow Steps

Each triage request should:

1. **Classifier** - Returns one of: incident, defect, inquiry, feature
2. **Assessor** - Returns one of: critical, high, medium, low
3. **Router** - Returns one of: ops-critical, ops-high, ops-standard, support-tier-1
4. **Handler** - Returns a ticket ID: TKT-xxxxxxxxxx

---

## Service Communication Testing

Test inter-service A2A communication by checking service logs:

```bash
# Watch Classifier logs
docker compose logs classifier -f

# In another terminal, submit triage request
curl -X POST http://localhost:5056/api/triage \
  -H "Content-Type: application/json" \
  -d '{"input":"test"}'
```

Expected: Classifier logs should show:
- Incoming A2A request
- Bearer token validation
- Skill execution
- JSON-RPC response

---

## Website UI Testing

1. **Open website:** http://localhost:8080 (or http://10.0.0.3:8080 on Swarm)

2. **Login:**
   - Username: `demo`
   - Password: `demo123`
   - Click "Login"

3. **Submit Request:**
   - Type text into "Enter request" field
   - Click "Submit Triage Request"
   - Wait for "Request completed" message

4. **View Results:**
   - Check "Results" section shows classification, priority, team, ticket
   - Check "Request History" shows submitted request with all steps

5. **Protocol Support:**
   - If accessing via HTTPS, verify API calls still work (no mixed content warning)
   - Website automatically detects protocol (http/https)

---

## Logging Verification

Check that infrastructure logging is at WARNING level (not INFO):

```bash
docker compose logs api-backend | grep "Microsoft.AspNetCore.Hosting"
```

Expected: Logs should show `warn:` level, not `info:`

---

## Error Scenarios

### Missing JWT_SECRET_KEY

1. Remove JWT_SECRET_KEY from .env
2. Restart services: `docker compose restart`
3. Check logs: `docker compose logs identity`

Expected: Service should fail to start with message about JWT_SECRET_KEY validation

### Service Unavailable

Stop a service and try to submit triage:

```bash
docker compose stop classifier
curl -X POST http://localhost:5056/api/triage \
  -H "Content-Type: application/json" \
  -d '{"input":"test"}'
```

Expected: Request should fail with error mentioning connection refused or service unavailable

---

## Security Verification

### Token Validation

Verify that invalid tokens are rejected:

```bash
curl -X POST http://localhost:5052/a2a \
  -H "Authorization: Bearer invalid-token-123" \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"SendMessage","id":"test"}'
```

Expected: HTTP 401 Unauthorized (current implementation validates token structure)

### No Hardcoded Secrets

Verify docker-compose.yml does not have hardcoded JWT_SECRET_KEY defaults:

```bash
grep "JWT_SECRET_KEY=" docker-compose.yml
```

Expected: Should show `JWT_SECRET_KEY=${JWT_SECRET_KEY}` without `:-default` fallback

---

## Performance Testing

### Concurrent Requests

Submit multiple triage requests concurrently:

```bash
for i in {1..5}; do
  curl -X POST http://localhost:5056/api/triage \
    -H "Content-Type: application/json" \
    -d "{\"input\":\"request $i\"}" &
done
wait
```

Expected: All requests complete successfully with unique IDs

---

## Verification Checklist

- [ ] All 7 services healthy and running
- [ ] Health endpoints return 200 OK
- [ ] User login endpoint works (demo/demo123)
- [ ] Triage request returns 4-step workflow
- [ ] Each step shows correct service and result
- [ ] Website loads and displays results
- [ ] A2A endpoints require Bearer token (401 without)
- [ ] Agent cards available at /.well-known/agent-card.json
- [ ] Infrastructure logging at WARNING level
- [ ] No hardcoded secrets in docker-compose.yml
- [ ] Website auto-detects HTTP/HTTPS protocol
- [ ] Error handling for missing services
- [ ] Concurrent requests handled correctly

---

## Troubleshooting

### Services Won't Start
Check logs: `docker compose logs`
Verify JWT_SECRET_KEY is set: `echo $JWT_SECRET_KEY`

### Login Fails  
Try correct credentials: demo/demo123
Check API backend: `curl http://localhost:5056/health`

### Triage Returns Empty Steps
Verify all services are running: `docker compose ps`
Check service logs for errors: `docker compose logs classifier`

### Website Shows Old Content
Clear browser cache (Ctrl+F5)
Rebuild website: `docker compose build --no-cache website`

---

## Test Commands Quick Reference

```bash
# Health checks
docker compose ps
curl http://localhost:5050/health

# Login
curl -X POST http://localhost:5056/auth/login \
  -d '{"username":"demo","password":"demo123"}'

# Triage
curl -X POST http://localhost:5056/api/triage \
  -d '{"input":"test"}'

# Agent card
curl http://localhost:5052/.well-known/agent-card.json | jq

# Logs
docker compose logs -f identity
docker compose logs -f api-backend

# Rebuild
docker compose build --no-cache
docker compose up -d
```
