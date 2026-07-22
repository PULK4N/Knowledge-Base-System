# Agent Memory MCP

An event-sourced MCP server for durable agent skills and working policies. It uses the existing projects under `API/EventSourcing` without modifying them.

## MCP endpoint and tools

The Streamable HTTP endpoint is `http://localhost:8080/mcp` and the health endpoint is `http://localhost:8080/health`.

Skills:

- `skill_save`, `skill_update`, `skill_delete`
- `skill_get`, `skill_search`

Policies:

- `policy_save`, `policy_update`, `policy_delete`
- `policy_get`, `policy_search`

Every skill or policy is an aggregate. Updates and soft deletes append events, reads replay those events, and active names are case-insensitively unique. Search excludes deleted records and disabled policies by default. Policy priority controls result order.

## Database setup

No EF migration is included. After SQL Server is available, create and apply one from the repository root:

```bash
dotnet ef migrations add InitialAgentMemory \
  --project API/EventSourcing/Persistence/Persistence.csproj \
  --startup-project API/MemoryMcp/MemoryMcp.csproj

dotnet ef database update \
  --project API/EventSourcing/Persistence/Persistence.csproj \
  --startup-project API/MemoryMcp/MemoryMcp.csproj
```

For Docker, set a strong password and start the stack:

```bash
export MSSQL_SA_PASSWORD='replace-with-a-strong-password'
docker-compose up --build
```

Run the migration from the host after the SQL Server container is healthy. The default development connection string targets `localhost:1433`; the container overrides it to use the `sqlserver` service.

## Local verification

```bash
dotnet build API/MemoryMcp/MemoryMcp.csproj -m:1
dotnet test API/MemoryMcp.Tests/MemoryMcp.Tests.csproj -m:1
```
