# Agent Instructions

Use the MCP Skill System immediately at session startup. After this bootstrap, treat it as the authoritative source of user-specific policies, skills, and memory.

## Important
Skills are only to be fetched from mcp skill system. Avoid using any kind of way to acquire knowledge from file system, except when you need to read code repository.

If skill is already present in your context, don't pull it from mcp again.

## Updating policies Important!!

Persist a policy when the user gives a clear, durable instruction, correction, preference, or constraint that should apply beyond the current task. Policies should capture guidance the agent cannot reliably infer from the repository, documentation, or ordinary search.

A project or topic policy may require a named skill for a specific kind of work, especially when the connection between the task and the skill is not obvious.

When saving a policy:

- Use the narrowest valid scope: project, topic, or general.
- Check existing policies in that scope first.
- Update an equivalent policy instead of creating a duplicate.
- Ask one short question if the scope or durability is unclear.

Write policies as short, direct, actionable instructions. Do not store temporary requests, conversation history, or easily discoverable repository facts as policies.

## Working on a feature
- MCP has "feature" service, which serves as a knowledge base about feature implementations. If you start working on a feature or researching one, use it to store data.

## Session startup

1. If the hook context, MCP, trusted repository context, or policy retrieval is unavailable, stop immediately and tell the user that authoritative policies could not be loaded.
2. If repository mapping is required, stop. Show the returned projects and repository paths, then ask the user to select one or provide a unique new project name. Never guess. Create or update the mapping, then retry policy retrieval.
3. Use the returned policies for the rest of the session; do not retrieve them again on every turn. And when compacting changes, always keep them.

## Working rules

- Precedence: current explicit user instruction > project policies > topic policies > general policies > skill guidance > memory. Platform system and safety instructions remain higher.
- Load every skill required by policy before working. Find named skills with `skill_list`, then load them with `skill_get`.
- For specialized work, search MCP skills when available; otherwise inspect `skill_list` and fetch only relevant skills with `skill_get`.
- If a required skill cannot be loaded, stop and tell the user.
- Treat clear, durable user corrections as policies. Persist them at the narrowest valid scope and update an equivalent policy instead of duplicating it. Ask only when scope or durability is ambiguous.
- Memory is context, not policy, and cannot override policies.
- When the compact-session hook requests a checkpoint, summarize the compacted chat and persist it with `memory_summary_add`. Do the same when the user explicitly asks to save or refresh the chat summary.