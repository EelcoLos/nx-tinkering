#!/bin/bash

echo "=== A2A Protocol End-to-End Test ==="
echo

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Test counter
TESTS_PASSED=0
TESTS_FAILED=0

# Helper function to test endpoint
test_endpoint() {
    local name=$1
    local method=$2
    local url=$3
    local data=$4
    local expected_status=$5
    
    echo -n "Testing $name... "
    
    if [ -z "$data" ]; then
        response=$(curl -s -w "\n%{http_code}" -X $method "$url")
    else
        response=$(curl -s -w "\n%{http_code}" -X $method "$url" -H "Content-Type: application/json" -d "$data")
    fi
    
    http_code=$(echo "$response" | tail -n1)
    body=$(echo "$response" | sed '$d')
    
    if [ "$http_code" = "$expected_status" ]; then
        echo -e "${GREEN}✓ ($http_code)${NC}"
        TESTS_PASSED=$((TESTS_PASSED + 1))
    else
        echo -e "${RED}✗ Expected $expected_status, got $http_code${NC}"
        TESTS_FAILED=$((TESTS_FAILED + 1))
        echo "Response: $body"
    fi
}

echo "Waiting for services to be ready..."
sleep 5

# Test Health Endpoints
echo -e "\n${YELLOW}=== Health Checks ===${NC}"
test_endpoint "Identity Health" "GET" "http://localhost:5050/health" "" "200"
test_endpoint "Discovery Health" "GET" "http://localhost:5051/health" "" "200"
test_endpoint "Classifier Health" "GET" "http://localhost:5052/health" "" "200"
test_endpoint "Assessor Health" "GET" "http://localhost:5053/health" "" "200"
test_endpoint "Router Health" "GET" "http://localhost:5054/health" "" "200"
test_endpoint "Handler Health" "GET" "http://localhost:5055/health" "" "200"
test_endpoint "API Backend Health" "GET" "http://localhost:5056/health" "" "200"

# Test Authentication
echo -e "\n${YELLOW}=== Authentication Tests ===${NC}"
test_endpoint "Valid User Login" "POST" "http://localhost:5050/auth/login" '{"username":"admin","password":"demo123"}' "200"
test_endpoint "Invalid User Login" "POST" "http://localhost:5050/auth/login" '{"username":"admin","password":"wrong"}' "401"

# Test Agent Token
echo -e "\n${YELLOW}=== Agent Token Tests ===${NC}"
test_endpoint "Get Agent Token" "GET" "http://localhost:5050/auth/agent/token" "" "200"

# Test API Endpoints
echo -e "\n${YELLOW}=== API Endpoint Tests ===${NC}"
# Get user token first
USER_TOKEN=$(curl -s -X POST "http://localhost:5050/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"demo123"}' | grep -o '"token":"[^"]*' | cut -d'"' -f4)

if [ -n "$USER_TOKEN" ]; then
    echo "User Token obtained: ${USER_TOKEN:0:20}..."
    
    # Test services endpoint with valid token
    echo -n "Testing /api/services with valid token... "
    response=$(curl -s -w "\n%{http_code}" -X GET "http://localhost:5056/api/services" \
      -H "Authorization: Bearer $USER_TOKEN")
    http_code=$(echo "$response" | tail -n1)
    if [ "$http_code" = "200" ]; then
        echo -e "${GREEN}✓ ($http_code)${NC}"
        TESTS_PASSED=$((TESTS_PASSED + 1))
    else
        echo -e "${RED}✗ Expected 200, got $http_code${NC}"
        TESTS_FAILED=$((TESTS_FAILED + 1))
    fi
    
    # Test services endpoint without token (should fail)
    test_endpoint "Services endpoint without token (should fail)" "GET" "http://localhost:5056/api/services" "" "401"
else
    echo -e "${RED}Failed to obtain user token${NC}"
fi

# Summary
echo -e "\n${YELLOW}=== Test Summary ===${NC}"
echo -e "Passed: ${GREEN}$TESTS_PASSED${NC}"
echo -e "Failed: ${RED}$TESTS_FAILED${NC}"

if [ $TESTS_FAILED -eq 0 ]; then
    echo -e "\n${GREEN}All tests passed!${NC}"
    exit 0
else
    echo -e "\n${RED}Some tests failed!${NC}"
    exit 1
fi
