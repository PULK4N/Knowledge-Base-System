#!/usr/bin/env bash

set -Eeuo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
compose_file="${repository_root}/docker-compose.yml"

if docker compose version >/dev/null 2>&1; then
    compose=(docker compose)
elif command -v docker-compose >/dev/null 2>&1; then
    compose=(docker-compose)
else
    echo "Docker Compose is required to rebuild the API." >&2
    exit 1
fi

"${compose[@]}" \
    --file "${compose_file}" \
    up \
    --detach \
    --build \
    --no-deps \
    api

"${compose[@]}" --file "${compose_file}" ps api
