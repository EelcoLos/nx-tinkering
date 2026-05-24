#!/bin/sh
set -eu

KEYCLOAK_URL="${KEYCLOAK_URL:-http://keycloak:8080}"
REALM="${KEYCLOAK_REALM:-a2a-local}"
ADMIN_USER="${KEYCLOAK_ADMIN_USER:-admin}"
ADMIN_PASSWORD="${KEYCLOAK_ADMIN_PASSWORD:-admin}"

wait_for_keycloak() {
  echo "[keycloak-init] waiting for Keycloak at ${KEYCLOAK_URL}..."
  for i in $(seq 1 90); do
    if /opt/keycloak/bin/kcadm.sh config credentials \
      --server "${KEYCLOAK_URL}" \
      --realm master \
      --user "${ADMIN_USER}" \
      --password "${ADMIN_PASSWORD}" >/dev/null 2>&1; then
      echo "[keycloak-init] Keycloak is reachable and admin auth works"
      return 0
    fi
    sleep 2
  done

  echo "[keycloak-init] Keycloak did not become ready in time"
  return 1
}

create_realm_if_missing() {
  if /opt/keycloak/bin/kcadm.sh get realms/${REALM} >/dev/null 2>&1; then
    echo "[keycloak-init] realm '${REALM}' already exists"
  else
    echo "[keycloak-init] creating realm '${REALM}'"
    /opt/keycloak/bin/kcadm.sh create realms -s realm="${REALM}" -s enabled=true >/dev/null
  fi
}

ensure_user() {
  username="$1"
  password="$2"
  email="${3:-${username}@local.test}"
  first_name="${4:-Demo}"
  last_name="${5:-User}"

  if /opt/keycloak/bin/kcadm.sh get users -r "${REALM}" -q username="${username}" | grep -q '"username"'; then
    echo "[keycloak-init] user '${username}' already exists"
  else
    echo "[keycloak-init] creating user '${username}'"
    /opt/keycloak/bin/kcadm.sh create users -r "${REALM}" -s username="${username}" -s enabled=true >/dev/null
  fi

  user_id="$(/opt/keycloak/bin/kcadm.sh get users -r "${REALM}" -q username="${username}" | sed -n 's/.*"id"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' | head -n 1)"

  if [ -n "${user_id}" ]; then
    /opt/keycloak/bin/kcadm.sh update "users/${user_id}" -r "${REALM}" \
      -s enabled=true \
      -s email="${email}" \
      -s emailVerified=true \
      -s firstName="${first_name}" \
      -s lastName="${last_name}" \
      -s 'requiredActions=[]' >/dev/null
  fi

  /opt/keycloak/bin/kcadm.sh set-password -r "${REALM}" --username "${username}" --new-password "${password}" --temporary=false >/dev/null
}

ensure_client() {
  client_id="$1"
  client_secret="$2"
  direct_grants="$3"
  service_accounts="$4"

  if /opt/keycloak/bin/kcadm.sh get clients -r "${REALM}" -q clientId="${client_id}" | grep -q '"clientId"'; then
    echo "[keycloak-init] client '${client_id}' already exists"
  else
    echo "[keycloak-init] creating client '${client_id}'"
    /opt/keycloak/bin/kcadm.sh create clients -r "${REALM}" \
      -s clientId="${client_id}" \
      -s enabled=true \
      -s protocol=openid-connect \
      -s publicClient=false \
      -s secret="${client_secret}" \
      -s directAccessGrantsEnabled="${direct_grants}" \
      -s standardFlowEnabled=false \
      -s serviceAccountsEnabled="${service_accounts}" >/dev/null
  fi
}

wait_for_keycloak

create_realm_if_missing

ensure_user "${DEMO_USER_USERNAME:-admin}" "${DEMO_USER_PASSWORD:-demo123}" "admin@local.test" "Demo" "Admin"
ensure_user "${DEMO_USER2_USERNAME:-user}" "${DEMO_USER2_PASSWORD:-user456}" "user@local.test" "Demo" "User"

ensure_client "${OIDC_USER_CLIENT_ID:-website-client}" "${OIDC_USER_CLIENT_SECRET:-website-client-secret}" true false
ensure_client "${OIDC_IDENTITY_CLIENT_ID:-identity-facade}" "${OIDC_IDENTITY_CLIENT_SECRET:-identity-facade-secret}" false true
ensure_client "${OIDC_DISCOVERY_CLIENT_ID:-discovery-agent}" "${OIDC_DISCOVERY_CLIENT_SECRET:-discovery-agent-secret}" false true
ensure_client "${OIDC_CLASSIFIER_CLIENT_ID:-classifier-agent}" "${OIDC_CLASSIFIER_CLIENT_SECRET:-classifier-agent-secret}" false true
ensure_client "${OIDC_ASSESSOR_CLIENT_ID:-assessor-agent}" "${OIDC_ASSESSOR_CLIENT_SECRET:-assessor-agent-secret}" false true
ensure_client "${OIDC_ROUTER_CLIENT_ID:-router-agent}" "${OIDC_ROUTER_CLIENT_SECRET:-router-agent-secret}" false true
ensure_client "${OIDC_HANDLER_CLIENT_ID:-handler-agent}" "${OIDC_HANDLER_CLIENT_SECRET:-handler-agent-secret}" false true
ensure_client "${OIDC_API_BACKEND_CLIENT_ID:-api-backend-agent}" "${OIDC_API_BACKEND_CLIENT_SECRET:-api-backend-agent-secret}" false true

echo "[keycloak-init] bootstrap complete"
