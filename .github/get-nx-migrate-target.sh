#!/usr/bin/env bash
set -euo pipefail

# Script to determine nx migrate target and NX_VERSION from Dependabot metadata or package.json
# Outputs to GitHub Actions using $GITHUB_OUTPUT environment variable:
# migrate-target=<pkg@version>
# nx-version=<version>
# skip=true (when cannot determine)

GITHUB_OUTPUT=${GITHUB_OUTPUT:-/dev/stdout}
UPDATED_DEPS_JSON=${UPDATED_DEPS:-}

log() { echo "[get-nx-migrate-target] $*" >&2; }
write_output() { echo "$1" >> "$GITHUB_OUTPUT"; }

MIGRATE_TARGET=""
NX_VERSION=""
PKG_NAME=""

# Try parsing Dependabot updated-dependencies-json if provided
if [ -n "$UPDATED_DEPS_JSON" ] && [ "$UPDATED_DEPS_JSON" != "null" ]; then
  # Find first matching nx or @nx/* dependency with a new-version
  entry=$(echo "$UPDATED_DEPS_JSON" | jq -r '.[] | select(."dependency-name" | test("^(nx|@nx/.+)$")) | @base64' | head -n1 || true)
  if [ -n "$entry" ]; then
    decoded=$(echo "$entry" | base64 --decode)
    dep_name=$(echo "$decoded" | jq -r '."dependency-name"')
    new_version=$(echo "$decoded" | jq -r '."new-version"')
    if [ -n "$new_version" ] && [ "$new_version" != "null" ]; then
      # normalize version (strip ^ ~ v)
      norm_version=$(echo "$new_version" | sed -E 's/^[\^~v]+//')
      MIGRATE_TARGET="${dep_name}@${norm_version}"
      NX_VERSION="$norm_version"
      PKG_NAME="$dep_name"
      log "Determined from Dependabot metadata: $MIGRATE_TARGET"
    fi
  fi
fi

# Fallback: inspect package.json for nx/@nx dependencies
if [ -z "$MIGRATE_TARGET" ]; then
  if [ -f package.json ]; then
    # Look for explicit nx dep first
    nx_ver=$(jq -r '.dependencies["nx"] // .devDependencies["nx"] // empty' package.json || true)
    if [ -n "$nx_ver" ]; then
      PKG_NAME="nx"
      NX_VERSION=$(echo "$nx_ver" | sed -E 's/^[\^~v]+//')
      MIGRATE_TARGET="nx@${NX_VERSION}"
      log "Determined from package.json nx dep: $MIGRATE_TARGET"
    else
      # find first @nx/* package
      nx_pkg=$(jq -r '(.dependencies // {}) + (.devDependencies // {}) | to_entries[] | select(.key | test("^@nx/")) | .key' package.json | head -n1 || true)
      if [ -n "$nx_pkg" ]; then
        nx_pkg_ver=$(jq -r '(.dependencies // {}) + (.devDependencies // {}) | to_entries[] | select(.key | test("^@nx/")) | .value' package.json | head -n1 || true)
        if [ -n "$nx_pkg_ver" ]; then
          PKG_NAME="$nx_pkg"
          NX_VERSION=$(echo "$nx_pkg_ver" | sed -E 's/^[\^~v]+//')
          MIGRATE_TARGET="${PKG_NAME}@${NX_VERSION}"
          log "Determined from package.json @nx package: $MIGRATE_TARGET"
        fi
      fi
    fi
  fi
fi

# Final validation
if [ -z "$NX_VERSION" ]; then
  log "Could not determine NX version. Skipping automated migrate."
  write_output "skip=true"
  write_output "reason=Could not determine NX version from Dependabot metadata or package.json"
  exit 0
fi

# Normalize NX_VERSION
NX_VERSION=$(echo "$NX_VERSION" | sed -E 's/^[\^~v]+//')

# Ensure MIGRATE_TARGET is set
if [ -z "$MIGRATE_TARGET" ]; then
  # default to nx@NX_VERSION
  MIGRATE_TARGET="nx@${NX_VERSION}"
fi

write_output "migrate-target=${MIGRATE_TARGET}"
write_output "nx-version=${NX_VERSION}"
write_output "skip=false"

exit 0
