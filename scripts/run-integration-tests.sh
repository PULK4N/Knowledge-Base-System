#!/usr/bin/env bash

set -Eeuo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
compose_file="${repository_root}/docker-compose.integration-tests.yml"
project_name="${MCP_INTEGRATION_COMPOSE_PROJECT_NAME:-mcp-knowledge-base-integration-tests}"
api_port="${MCP_INTEGRATION_API_PORT:-5232}"
export MCP_INTEGRATION_API_PORT="${api_port}"
export MCP_INTEGRATION_MCP_URL="http://localhost:${api_port}/mcp"

if docker compose version >/dev/null 2>&1; then
    compose=(docker compose)
elif command -v docker-compose >/dev/null 2>&1; then
    compose=(docker-compose)
else
    echo "Docker Compose is required to run integration tests." >&2
    exit 1
fi

compose_command=(
    "${compose[@]}"
    --project-name "${project_name}"
    --file "${compose_file}"
)

cleanup() {
    exit_code=$?
    trap - EXIT

    if (( exit_code != 0 )); then
        "${compose_command[@]}" logs --no-color || true
    fi

    "${compose_command[@]}" down --volumes --remove-orphans || true
    exit "${exit_code}"
}
trap cleanup EXIT

"${compose_command[@]}" down --volumes --remove-orphans
"${compose_command[@]}" up --build --detach --renew-anon-volumes

for ((attempt = 1; attempt <= 300; attempt++)); do
    if curl --fail --silent \
        "http://localhost:${api_port}/swagger/v1/swagger.json" \
        >/dev/null; then
        break
    fi

    if (( attempt == 300 )); then
        echo "The integration API did not become ready in time." >&2
        exit 1
    fi

    sleep 2
done

dotnet test \
    "${repository_root}/API/IntegrationTests/IntegrationTests.csproj" \
    --logger "console;verbosity=normal"
