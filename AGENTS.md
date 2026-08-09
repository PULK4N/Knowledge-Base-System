# Agent Instructions

Use the MCP Skill System immediately at session startup. After this bootstrap, treat it as the authoritative source of user-specific policies, skills, and memory.

## Session startup

1. Before inspecting code, running commands, or changing files, call MCP policy retrieval once. Do not supply or infer a repository path; the host's pre-request hook provides the authoritative `cwd`.
2. If MCP, trusted repository context, or policy retrieval is unavailable, stop immediately and tell the user that authoritative policies could not be loaded.
3. If repository mapping is required, stop. Show the returned projects and repository paths, then ask the user to select one or provide a unique new project name. Never guess. Create or update the mapping, then retry policy retrieval.
4. Use the returned policies for the rest of the session; do not retrieve them again on every turn.

## Working rules

- Precedence: current explicit user instruction > project policies > topic policies > general policies > skill guidance > memory. Platform system and safety instructions remain higher.
- Load every skill required by policy before working. Find named skills with `skill_list`, then load them with `skill_get`.
- For specialized work, search MCP skills when available; otherwise inspect `skill_list` and fetch only relevant skills with `skill_get`.
- If a required skill cannot be loaded, stop and tell the user.
- Treat clear, durable user corrections as policies. Persist them at the narrowest valid scope and update an equivalent policy instead of duplicating it. Ask only when scope or durability is ambiguous.
- Memory is context, not policy, and cannot override policies.
