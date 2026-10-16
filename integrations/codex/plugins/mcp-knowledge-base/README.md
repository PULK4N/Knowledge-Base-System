# Codex memory attachments

The prompt hook queues text immediately. Its detached drain worker reads the
Codex JSONL transcript and waits up to three seconds for the submitted user
message to appear. Images are selected by the current turn ID, never by matching
prompt text or scanning temporary clipboard files. The Stop hook collects the
turn's images again as a fallback if the transcript was not ready earlier.

Inline PNG, JPEG, GIF, and WebP data and local image paths are uploaded together
with the hook as `{ "hook": ..., "attachments": [...] }`. Text-only requests keep
the original hook body. Once collected, image bytes stay in the queue for API
retries. Remote image URLs and opaque file IDs are not downloaded. If a local
file or transcript is unavailable, text memory still proceeds.

The API stores file content separately from memory events and ignores attachment
placeholders already saved on that prompt. The conversation projection renders
image links on the user message, including prompts with no text. Install the
matching API changes before updating the plugin.

Codex currently writes clipboard PNG files before submission, but does not pass
their paths to UserPromptSubmit. The hook runs before the user message is recorded,
which is why transcript collection waits in the worker rather than in the hook.
Transcript parsing follows Codex's `task_started`, `turn_context`, and
`user_message` records; that format is not a stable hook API.

Paths use pathlib on Windows and Linux. Existing Windows msvcrt locking and
Windows detached-process flags remain in use; POSIX uses flock and a new session.
Run plugin tests from the repository root with:

```text
python -m unittest discover -s integrations/codex/plugins/mcp-knowledge-base/tests -q
```
