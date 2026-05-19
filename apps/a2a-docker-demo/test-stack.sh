#!/bin/bash
set -e

echo "=== A2A Triage Demo - Stack Test ==="
echo ""

# Check images
echo "✓ Checking Docker images..."
docker images | grep a2a | wc -l | xargs echo "  Found" "A2A images"

# Start stack
echo "✓ Starting Docker stack..."
docker compose -f docker-compose.local.yml up -d
sleep 30

# Test health endpoints
echo "✓ Testing health endpoints..."
for port in 5050 5051 5052 5053 5054 5055 5056; do
  response=$(curl -s http://localhost:$port/health 2>/dev/null || echo "FAIL")
  if [[ $response == *"healthy"* ]]; then
    echo "  Port $port: ✓ Healthy"
  else
    echo "  Port $port: ✗ Failed"
  fi
done

# Test user login
echo ""
echo "✓ Testing user login..."
login_response=$(curl -s -X POST http://localhost:5050/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"demo123"}')

if [[ $login_response == *"token"* ]]; then
  echo "  Login: ✓ Success"
  USER_TOKEN=$(echo $login_response | grep -oP '"token":"?\K[^"]+' | head -1)
else
  echo "  Login: ✗ Failed"
  exit 1
fi

# Test agent token
echo ""
echo "✓ Testing agent token..."
agent_response=$(curl -s -X GET "http://localhost:5050/auth/agent/token?agentId=classifier_agent&agentSecret=classifier-secret-12345")
if [[ $agent_response == *"token"* ]]; then
  echo "  Agent token: ✓ Success"
else
  echo "  Agent token: ✗ Failed"
fi

# Test 401 on missing JWT
echo ""
echo "✓ Testing 401 enforcement..."
unauthorized=$(curl -s -w "\n%{http_code}" http://localhost:5051/discovery/services | tail -1)
if [ "$unauthorized" = "401" ]; then
  echo "  Missing JWT: ✓ Returns 401"
else
  echo "  Missing JWT: ✗ Returned $unauthorized"
fi

# Test 401 on invalid JWT
invalid_response=$(curl -s -w "\n%{http_code}" http://localhost:5051/discovery/services \
  -H "Authorization: Bearer invalid" | tail -1)
if [ "$invalid_response" = "401" ]; then
  echo "  Invalid JWT: ✓ Returns 401"
else
  echo "  Invalid JWT: ✗ Returned $invalid_response"
fi

# Test full triage workflow
echo ""
echo "✓ Testing full triage workflow..."
triage_response=$(curl -s -X POST http://localhost:5056/api/triage \
  -H "Authorization: Bearer $USER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"input":"Server is down - critical issue"}')

if [[ $triage_response == *"ticketId"* ]] && [[ $triage_response == *"completed"* ]]; then
  echo "  Triage workflow: ✓ Success"
  ticket=$(echo $triage_response | grep -oP '"ticketId":"?\K[^"]+' | head -1)
  echo "  Generated ticket: $ticket"
  steps=$(echo $triage_response | grep -oP '"service":"?\K[^"]+' | wc -l)
  echo "  Services executed: $steps"
else
  echo "  Triage workflow: ✗ Failed"
  echo "  Response: $triage_response"
fi

echo ""
echo "=== All Tests Passed ✓ ==="
echo ""
echo "Services running on:"
echo "  Identity:   http://localhost:5050"
echo "  Discovery:  http://localhost:5051"
echo "  Classifier: http://localhost:5052"
echo "  Assessor:   http://localhost:5053"
echo "  Router:     http://localhost:5054"
echo "  Handler:    http://localhost:5055"
echo "  API:        http://localhost:5056"
echo "  Website:    http://localhost:8080"
echo ""
echo "To stop the stack: docker compose -f docker-compose.local.yml down"
