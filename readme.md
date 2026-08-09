# MCP Skill System

## Purpose

This project is intended to become the authoritative context system from which
an LLM works. Policies, skills, and durable conversational memory should live
here instead of being distributed across local instruction files, separately
installed skills, ad-hoc prompt text, and model-specific memory features.

The MCP server is not meant to be an optional enhancement. Once integrated, an
agent should depend on it for its user- and project-specific operating context.
It must not silently fall back to stale local skills or unrelated memory when
the MCP server is unavailable.

The migration is complete only after existing durable policies, useful skills,
and relevant memories have been imported and the corresponding model-specific
stores have been disabled. Keeping two writable instruction or skill stores
would create split-brain behavior and undermine this project's purpose.

There is one unavoidable boundary: platform system and safety instructions
remain outside this MCP and have higher authority. A small bootstrap instruction
must also exist outside the server to tell the agent where the MCP is and that
policy retrieval is mandatory. The MCP cannot require its own use until the
agent has first been instructed to call it.

This document is both the product direction and an implementation checklist.
Some behavior described below is not implemented yet. The current-state section
must be kept honest as the system evolves.

## Core concepts

The system separates five kinds of context. They should not be treated as
interchangeable.

| Context | Purpose | Example |
| --- | --- | --- |
| General policy | Durable instruction that applies everywhere | “Use the smallest focused test target.” |
| Topic policy | Durable instruction shared by a technology or work domain | “For cloud deployments, never print credentials.” |
| Project policy | Durable instruction specific to one project or repository | “Do not run tests in this repository.” |
| Skill | Reusable procedure or specialized working knowledge | `acumatica-frontend` |
| Memory | Evidence about previous conversations and work | A summary of an earlier implementation discussion |

Policies say what the agent must or must not do. Skills explain how to perform
a kind of work. Memories help recover previous context, but are not
instructions. A remembered statement must not override a policy.

The event stream and aggregate state are authoritative. PostgreSQL text rows,
full-text indexes, vector embeddings, summaries, and Markdown-like skill chunks
are rebuildable retrieval projections.

## Required agent workflow

The normal workflow should be:

1. A new agent session begins with the user's prompt.
2. Before the prompt reaches the agent, a host pre-request hook reads the
   session's authoritative `cwd` and binds it to the MCP request or session
   context.
3. During session initialization, before substantive reasoning or
   repository-changing tool use, the agent requests policies once without
   supplying, resolving, or guessing a repository path. Policy retrieval uses
   the hook-provided `cwd`.
4. If the repository is not mapped to a policy project:
   1. The agent stops the requested work.
   2. It shows the user the available projects and their repository paths.
   3. It asks whether the repository belongs to one of them or asks for a
      unique name for a new project.
   4. It never guesses, selects, or creates a project without the user's answer.
   5. It creates or updates the mapping and retries policy retrieval.
5. If the repository is mapped, the agent receives the complete applicable
   policy context: general policies, the project policies, and policies from
   topics related to that project.
6. The agent resolves skills:
   1. Skills explicitly required by a policy are fetched first.
   2. If the prompt suggests specialized work, the agent searches for matching
      skills.
   3. It fetches the selected skill and only the references needed for the
      current task.
   4. A skill recommendation must not override policy.
7. The agent performs the task normally while following the retrieved context.
8. When the user corrects the agent, the correction is treated as a candidate
   durable policy and assigned the narrowest correct scope.
9. The agent adds a new policy or updates the existing equivalent policy. It
   should avoid creating duplicates for differently worded versions of the same
   rule.

Policies are not retrieved again on every user turn. The session continues with
the policy context loaded during initialization. When the agent writes a policy
because of a correction, that correction applies immediately from the current
conversation and is persisted for future sessions; another retrieval is not
required. If the active repository changes, the host should treat that as a new
repository context and initialize its policies before work continues.

### Trusted repository context

Repository identity must not be a model-controlled MCP argument. A pre-request
hook, such as Codex `UserPromptSubmit`, receives the host-provided session
`cwd` and forwards or binds it to trusted MCP request/session context. The
policy tool then reads that context, so the agent only asks for policies and
cannot mistype, substitute, or invent the repository path.

The integration should fail closed. If `cwd` is missing, cannot be
canonicalized, or is ambiguous, policy retrieval must not accept a fallback
path from the model; the agent must stop and report that trusted repository
context is unavailable.

“No policies” in the gating step primarily means that the current repository is
not mapped to a policy project. A mapped project may legitimately have no
project-specific rules and still inherit general or topic policies.

## Policy scope and learning from corrections

Every user correction should be evaluated for durable meaning. This does not
mean that every transient sentence is blindly persisted.

Use the narrowest scope that fully captures the rule:

- Use a project policy when the instruction refers to the current repository,
  its commands, its architecture, or its team conventions.
- Use a topic policy when the instruction should apply across projects that use
  the same technology or work domain.
- Use a general policy only when the user clearly intends the rule to apply to
  essentially all work.
- Use a skill when the content is a reusable procedure rather than a behavioral
  constraint.
- Use memory when the content is historical context rather than an instruction.

Examples:

- “Do not run tests in this repository” is a project policy.
- “When working on any Acumatica frontend, load the `acumatica-frontend` skill”
  is normally a topic policy. It may be a project policy if the instruction is
  intended only for one repository.
- “Always tell me before deleting data” is a general policy.
- “Use this sequence to publish an Acumatica customization” belongs in a skill.
- “Yesterday we decided to use PostgreSQL” belongs in memory unless the user
  turns it into a project rule.

Before adding a policy, inspect policies in the selected scope. Update an
equivalent rule when one exists. If the correction is clearly temporary, such
as “do not run tests yet,” do not make it durable without confirmation. If the
scope or durability is ambiguous, ask the user one short question.

Project names are unique. The current implementation normalizes them by
trimming and comparing case-insensitively.

## Policy precedence

The intended precedence should be based on authority and specificity:

1. Platform system and safety requirements.
2. The user's explicit instruction in the current conversation.
3. Project policies.
4. Related topic policies.
5. General policies.
6. Skill guidance.
7. Retrieved memories.

When two policies at the same scope conflict, the agent should not silently
choose whichever text appeared last. It should identify the conflict, ask the
user which rule is intended, and update or remove the obsolete policy.

The implementation does not yet encode this precedence explicitly. The current
compiled policy text is ordered as general, project, then related topics. That
ordering must not be mistaken for a reliable conflict-resolution mechanism.

## Required bootstrap instruction

The retrieval timing and precedence rules must be part of the external
`AGENTS.md`, plugin prompt, or equivalent host-level instruction. They cannot
live only inside MCP policies because the agent needs them before its first MCP
call.

A minimal bootstrap prompt should communicate the following contract:

```text
Use MCP Skill System as the only source of user-specific policies, skills, and
durable memory.

At the beginning of each agent session, request policies through MCP exactly
once before substantive reasoning or repository-changing work. Do not supply
or infer a repository path: a host pre-request hook provides the authoritative
cwd to MCP. If MCP or trusted repository context is unavailable, stop. If
repository mapping is required, ask the user to select an existing project or
provide a unique new project name. Never guess the mapping.

For conflicts, follow this precedence: the current explicit user instruction,
project policies, related topic policies, general policies, skill guidance,
then retrieved memory. Platform system and safety requirements remain higher
than all of these.

Treat clear, durable user corrections as policies. Persist them at the
narrowest valid scope, updating an equivalent policy instead of adding a
duplicate. Ask the user only when durability or scope is ambiguous.
```

This bootstrap should remain deliberately small. Detailed policies and skills
belong in the MCP rather than being duplicated into the bootstrap prompt.

## Skill behavior

Skills are Markdown-like documents with optional text references and binary
attachments.

- Skill names must be unique and stable enough for policies to refer to them.
- Main skill content and text references are eligible for text and vector
  search.
- Markdown is split into bounded semantic chunks around headings.
- Binary attachments are not embedded or included in ordinary skill search.
- Search should return compact candidates first. The full skill should be
  fetched only after selection.
- A project or topic policy may require a named skill for a category of work.

The long-term design should use a stable skill identity or explicit relation
for policy-required skills. Depending only on a free-text skill name can break
when a skill is renamed.

## Memory lifecycle

Raw memories and summaries have different write paths.

### Raw memories

Raw conversational memory is recorded automatically by Codex hooks. It must not
depend on the model remembering to call an MCP tool. Hook payloads are grouped
by thread and prompt so that later retrieval can locate surrounding context.

Hook processing should be fast and durable. A hook may enqueue or POST a record,
but should not block the active agent while generating embeddings or summaries.

### Summaries

A chat summary is a compact checkpoint over the conversation. A summary should
be generated and written:

- when Codex compacts the conversation; or
- when the user explicitly asks to save or refresh the summary.

Compaction is a trigger, not the summary itself. The compact hook must cause a
generative model to produce a few useful paragraphs and then persist them with
`memory_summary_add`. The summary should capture goals, decisions, important
changes, verification results, unresolved work, and durable user preferences.
It should omit repetitive tool noise and secrets.

The current timestamp-based summary coverage has an accepted race margin. A
summary may miss a prompt that arrives while it is being generated. This is
acceptable for now and can later be replaced with an explicit event-order or
“summarized through prompt” marker.

### Retrieval

Memory search is intentionally small and progressive:

1. One hybrid search call generates one query embedding and performs text and
   vector retrieval.
2. Chunk results from the same session are collapsed.
3. Near-duplicate fork summaries are removed conservatively.
4. At most two session-level results are returned.
5. A summary hit returns the authoritative session summary without repeating
   raw matched text.
6. A message hit returns the session summary, the matched text, and a prompt ID.
7. If more context is needed, the agent requests a bounded prompt window before
   and after that prompt. This follow-up does not generate another embedding.

Search and prompt-window responses use an approximate token budget of four
characters per token. Exact counting is model-specific and can be introduced
behind a tokenizer abstraction later.

Memory is supporting evidence. A correction discovered in memory does not
become an active rule until it is represented as a policy.

## Failure behavior

Because this MCP is intended to be authoritative, failures should be visible:

- If the repository has no project mapping, stop and ask the user to resolve it.
- If the MCP server or policy query is unavailable, report that authoritative
  context could not be loaded. Do not silently continue with unrelated local
  skills or cached instructions.
- If a required skill cannot be found, report the missing skill instead of
  improvising as if it had been loaded.
- If policy scopes conflict, ask the user and repair the stored policy set.
- If a memory search returns nothing, continue without historical context;
  memory absence is not a policy failure.

“Fail closed” for policies means that when policy retrieval fails during
session initialization, the agent pauses repository-specific reasoning and
changes, reports that authoritative context is unavailable, and offers to
retry. It does not inherently require blocking generic conversation that does
not depend on repository policies. Whether that limited fallback is allowed is
still an open product decision.

## Implementation principles

When extending this repository:

- Implement one requested layer at a time and verify it before expanding scope.
- Keep Domain, Application, API/MCP, Persistence, and provider-specific wiring
  separate.
- Keep MCP functions and HTTP controllers thin. They resolve scoped application
  actions, map inputs, obtain the executor, and execute them.
- Never construct commands or queries inside a controller or MCP function.
- Keep event-family interfaces empty and persist immutable concrete versions.
- Put state preconditions in YAML-selected validators, not defensive event
  `Apply` branches.
- Treat event-sourced state as authoritative and search/vector data as
  rebuildable projections.
- Make snapshot projections idempotent.
- Add focused unit tests and realistic clean-slate MCP integration tests for
  externally visible workflows.
- Keep this README's current-state and known-gap sections synchronized with the
  code.

## Current implementation state

### Policies

Implemented:

- Event-sourced general, topic, and project policies.
- Project-to-repository mapping.
- Unique project names.
- Policy CRUD and list MCP functions.
- `policy_get_by_repository` with the required stop-and-ask fallback.
- Combined general, project, and related-topic policy text projections.

Not complete:

- Policy retrieval still exposes
  `policy_get_by_repository(repositoryPath)`. It must be replaced or wrapped by
  context-aware retrieval with no model-supplied repository argument, plus a
  pre-request hook that binds the host-provided `cwd` to the MCP request or
  session.
- Policy precedence and same-scope conflict resolution are not explicit.
- Repository paths are matched as stored strings; canonicalization rules for
  casing, symlinks, trailing separators, containers, and multi-repository
  workspaces are not defined.
- Corrections are not yet automatically classified and persisted as policies.
- Policy-to-skill requirements are currently free-text instructions rather than
  stable relations.

### Skills

Implemented:

- Event-sourced skill CRUD, references, and attachments.
- MCP list/get/write functions.
- Full-text and vector projection of skill Markdown and text references.
- Binary attachment exclusion from the search projection.

Not complete:

- Hybrid skill search exists in Persistence but has no Application query or MCP
  search function.
- There is no automatic policy-required-skill resolution.

### Memories

Implemented:

- Hook payload ingestion through the Memory API.
- Thread-to-memory aggregate mapping.
- Event-sourced chat summaries and `memory_summary_add`.
- Full-text and vector projection code for raw hook chunks and summaries.
- Application queries for compact memory search and bounded prompt windows.

Not complete or incorrect:

- Memory search and prompt-window queries are not exposed through MCP yet.
- `ChatSummaryAddedV1` does not currently list `MemorySearchProjector` in
  `StateMachines/memory.yaml`. A new summary therefore does not immediately
  refresh its text/vector projection unless another projected memory event runs
  afterward.
- The repository does not contain the complete production hook forwarding and
  compact-summary generation workflow.
- Fork lineage is not stored explicitly. Similar summaries are currently
  deduplicated heuristically rather than through a parent/child thread model.
- Hook payload filtering, secret redaction, retention, and deletion rules are
  not defined.

### Operations and trust

Implemented:

- PostgreSQL with pgvector.
- Ollama with `qwen3-embedding:0.6b`.
- Clean-slate Docker Compose integration tests for policy and skill MCP flows.
- A root `AGENTS.md` with the mandatory policy-retrieval bootstrap.

Not complete:

- Authentication, user/workspace tenancy, and authorization boundaries are not
  implemented; the current executor provider is temporary.
- Memory MCP integration scenarios are not covered yet.
- Backup, retention, and disaster-recovery expectations are not documented.
- The bootstrap and trusted-`cwd` policy retrieval flow are not yet covered by
  an end-to-end hook/MCP integration test.

## Established agent decisions

1. Retrieve repository policies once, during session initialization. Do not
   retrieve them again for every user turn.
2. Automatically persist corrections that are clearly durable and scoped. Ask
   only when durability or scope is ambiguous.
3. Use project policies over related topic policies, topic policies over general
   policies, policies over skill guidance, and skills over retrieved memory.
   Carry this rule in the external bootstrap instruction.

## Decisions still needed

Recommended defaults are shown here so development can continue without hiding
open product decisions.

1. **MCP outage:** decide whether generic, non-project conversation may continue
   when session-start policy retrieval fails. Repository-specific work remains
   paused either way.
2. **Multi-repository workspaces:** retrieve policy context for every active
   repository and make conflicts explicit.
3. **Topic creation:** require user confirmation before creating a new topic;
   relating an existing topic to a project may be automatic when a policy
   explicitly requires it.
4. **Forks:** add explicit parent-thread identity if inherited history and
   deterministic deduplication become important.
5. **Memory privacy:** define which hook events and payload fields may be stored,
   how secrets are redacted, and how users delete their data.
