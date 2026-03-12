#!/usr/bin/env bash
set -euo pipefail

# Usage: set-package-version.sh <version>
# Safely updates package.json top-level version to the provided version

if [ "$#" -ne 1 ]; then
  echo "Usage: $0 <version>" >&2
  exit 2
fi

VERSION="$1"

# Simple semver-ish check (major.minor or major.minor.patch)
if ! echo "$VERSION" | grep -Eq '^[0-9]+(\.[0-9]+){1,2}$'; then
  echo "Invalid version format: $VERSION" >&2
  exit 3
fi

if ! command -v jq >/dev/null 2>&1; then
  echo "jq is required in the runner to modify package.json" >&2
  exit 4
fi

TMP_FILE=$(mktemp)
jq --arg version "$VERSION" '.version = $version' package.json > "$TMP_FILE"
mv "$TMP_FILE" package.json

echo "package.json version set to $VERSION"
exit 0
