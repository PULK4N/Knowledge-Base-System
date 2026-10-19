# Migrate prompt usage from Claude and Codex logs

Requires Python 3.10 or newer and the Knowledge Base API with V2 usage events
and the prompt-usage database migration. No Python packages are
needed. Both scripts run unchanged on Windows and Linux.

The scripts update **existing memory prompts** only. They do not create memories
or import chat text. The source logs remain unchanged. Dry runs read the API to
match prompts and compare existing counts; they perform no writes.

## Replacing the original counts-only migration

V1 usage events are retained but do nothing during replay. V2 writes store the
full request JSON in the event and prompt state, including `sourceRecords` and,
for Codex, the original `baselineRecord` used to calculate deltas. Claude source
records include streaming copies even though counts deduplicate their message IDs.

After deploying V2, replay `MemorySummaryProjector` and
`MemoryConversationProjector` for the affected memories before rerunning these
scripts. This clears projected V1-only counts; otherwise the scripts will see
old populated projections and skip them. V2 usage remains write-once.

## Claude

```sh
python scripts/claude/set_prompt_usage.py --dry-run
python scripts/claude/set_prompt_usage.py --execute
```

The default source is `Path.home() / ".claude" / "projects"`. To select one
project directory or one session file, supply it as a positional argument:

```powershell
py -3 scripts/claude/set_prompt_usage.py "$HOME/.claude/projects/<project-folder>" --execute
```

Claude replies are deduplicated by `message.id`, including copies produced by
streaming, then summed within each user prompt. Tool-result messages do not
start new prompts; queued user prompts do. Synthetic replies and sidechains
are excluded. Prompt matching uses tool-use IDs, then prompt text (timestamps
resolve repeated text). Ambiguous matches are skipped. One source turn is never
charged to multiple memory prompts. A final `end_turn` reply is required.

## Codex

```sh
python scripts/codex/set_prompt_usage.py --dry-run
python scripts/codex/set_prompt_usage.py --execute
```

The default source is `Path.home() / ".codex" / "sessions"`. Folders are searched
recursively. Archived sessions can be processed separately:

```powershell
py -3 scripts/codex/set_prompt_usage.py "$HOME/.codex/archived_sessions" --execute
```

Codex turn IDs match memory prompt IDs directly. Usage is calculated from
changes in `total_token_usage`, never by summing cumulative snapshots. Repeated
snapshots and rate-limit-only records do not add usage. Only completed turns are
written. Active/aborted turns, unknown required counters, counter resets and an
unverifiable initial baseline are skipped. Subagent files are excluded to avoid
attributing inherited parent history to a child conversation.

## Options and reruns

Both scripts accept:

- `--base-url http://localhost:5231`
- `--session-id <UUID>` (repeatable)
- `--timeout 30` (HTTP and per-session projection verification timeout)
- A source file or folder, including paths containing spaces.
- `--execute` to write; `--dry-run` is the default.

Prompts with any existing usage count are skipped, even if the new values
differ. The API rejects subsequent usage events through
`PromptUsageMustNotBeSetValidator`; a concurrent already-set rejection is also
treated as a skip. Each written session is read back until its projected counts
match. Wait for one execution to finish before rerunning. A projection
timeout reports failure even if the API accepted the writes; let projections
catch up before retrying.

Unknown reasoning counts stay null. Missing required counts and incomplete or
ambiguous prompts are skipped, not converted to zero. Conversation totals may
therefore remain null after migration. Claude input is normalized by the API to
include cache reads and writes. No prices or monetary costs are inferred.

Exit code 0 means no processing/verification failures, not that every prompt had
usable usage. The final summary reports written/would-write, already-set, skipped,
failed, and matched session counts. Close or finish active conversations and
rerun to migrate their completed turns. Malformed JSON causes that file to fail
without writing any of its prompts.
