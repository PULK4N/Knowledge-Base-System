# MCP integration tests

These tests call the deployed Streamable HTTP MCP endpoint without referencing
production projects. The integration stack contains the real API, PostgreSQL
with pgvector, Ollama, and `qwen3-embedding:0.6b`.

Run the complete clean-slate workflow from the repository root:

```bash
./scripts/run-integration-tests.sh
```

The script builds and starts `docker-compose.integration-tests.yml`, waits for
the API migrations and MCP host, runs this test project, prints container logs
on failure, and always removes containers, networks, and volumes afterward.

The default API port is `5232`. Override it when needed:

```bash
MCP_INTEGRATION_API_PORT=6232 ./scripts/run-integration-tests.sh
```

The Compose file deliberately declares no persistent volumes. The cleanup also
removes anonymous volumes declared by base images, so PostgreSQL and Ollama start
empty on every run. A future seeded mode can be added as a separate Compose
override without changing these clean-slate tests.
