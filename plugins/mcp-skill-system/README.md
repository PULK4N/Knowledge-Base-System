# MCP Skill System Codex plugin

This plugin connects Codex to the local MCP Skill System and loads repository
policies before Codex handles the first prompt of a session.

The policy hook obtains `cwd` from Codex, resolves the Git repository root when
available, and calls `policy_get_by_repository` itself. The model never chooses
the path used for policy retrieval. Successful policy context is cached in the
plugin data directory for the Codex session and restored after compaction.

If MCP is unavailable, the hook stops the prompt. If repository mapping is
required, it injects the available projects and forces the agent to ask the
user rather than guessing. After a project mapping tool succeeds, the hook
automatically retries policy loading.

The memory hook queues `UserPromptSubmit` and `Stop` payloads in the plugin data
directory, then forwards them to the Memory API from a detached worker. This
keeps embedding work outside the active hook while retaining failed records for
a later retry. Tool-call payloads are intentionally not recorded because they
can be large and may contain sensitive command output.

After compaction, the plugin adds developer context requiring Codex to create a
short checkpoint and persist it through `memory_summary_add`. An explicit user
request to save or refresh the summary uses the same MCP tool.

The bundled MCP server points to `http://localhost:5231/mcp`, matching the main
Docker Compose file. Set `MCP_SKILL_SYSTEM_URL` for the hook and override the
plugin MCP server URL in Codex configuration when using another address.
Set `MCP_SKILL_SYSTEM_MEMORY_HOOK_URL` only when the HTTP memory-ingestion route
is hosted separately.

Plugin hooks must be reviewed and trusted after installation. Start a new Codex
session after enabling the plugin.

Run the local checks with:

```bash
python3 -m unittest discover -s plugins/mcp-skill-system/tests -v
python3 /home/nikola/.codex/skills/.system/plugin-creator/scripts/validate_plugin.py plugins/mcp-skill-system
```
