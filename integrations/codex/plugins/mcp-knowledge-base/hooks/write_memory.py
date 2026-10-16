#!/usr/bin/env python3
"""Queue useful Codex hook payloads and request summaries after compaction."""

from __future__ import annotations

import base64
import binascii
import json
import os
import subprocess
import sys
import tempfile
import time
import urllib.error
import urllib.parse
import urllib.request
import uuid
from datetime import datetime
from pathlib import Path
from typing import IO, Any, Callable

try:
    import fcntl
except ImportError:  # Windows has no fcntl; msvcrt locks the same byte range.
    fcntl = None
    import msvcrt


DEFAULT_MEMORY_HOOK_URL = (
    "http://localhost:5231/api/memory/codex/prompt-hooks"
)
RECORDED_EVENTS = frozenset({"UserPromptSubmit", "Stop"})
IMAGE_EXTENSIONS = {
    "image/png": "png",
    "image/jpeg": "jpg",
    "image/gif": "gif",
    "image/webp": "webp",
}


class MemoryHookError(RuntimeError):
    pass


def _filter_unpaired_surrogates(value: Any) -> Any:
    """Remove orphaned UTF-16 halves while preserving valid Unicode pairs."""
    if isinstance(value, str):
        return value.encode("utf-16-le", errors="surrogatepass").decode(
            "utf-16-le", errors="ignore"
        )
    if isinstance(value, list):
        return [_filter_unpaired_surrogates(item) for item in value]
    if isinstance(value, dict):
        filtered = {}
        for key, item in value.items():
            clean_key = _filter_unpaired_surrogates(key)
            if clean_key in filtered:
                raise MemoryHookError(
                    "Filtering invalid Unicode produced duplicate payload keys."
                )
            filtered[clean_key] = _filter_unpaired_surrogates(item)
        return filtered
    return value


class MemoryApiClient:
    def __init__(self, url: str, timeout_seconds: int = 20) -> None:
        self._url = url
        self._timeout_seconds = timeout_seconds

    def record(self, payload: dict[str, Any]) -> None:
        payload = _filter_unpaired_surrogates(payload)
        request = urllib.request.Request(
            self._url,
            data=json.dumps(payload, separators=(",", ":")).encode("utf-8"),
            method="POST",
        )
        request.add_header("Content-Type", "application/json")

        try:
            with urllib.request.urlopen(
                request, timeout=self._timeout_seconds
            ) as response:
                response.read()
        except urllib.error.HTTPError as error:
            details = error.read().decode("utf-8", errors="replace")
            raise MemoryHookError(
                f"Memory API returned HTTP {error.code}: "
                f"{details[:500] or error.reason}"
            ) from error
        except (OSError, urllib.error.URLError) as error:
            raise MemoryHookError(f"Memory API is unavailable: {error}") from error


class RoutedMemoryApiClient(MemoryApiClient):
    """Select the destination for each record in the shared hook queue."""

    def __init__(self) -> None:
        super().__init__(_memory_hook_url())
        self._tool_client = MemoryApiClient(_tool_use_hook_url())

    def record(self, payload: dict[str, Any]) -> None:
        if payload.get("hook_event_name") == "PostToolUse":
            self._tool_client.record(payload)
        else:
            super().record(payload)


class MemoryHookQueue:
    def __init__(self, root: Path) -> None:
        self._root = root
        self._root.mkdir(mode=0o700, parents=True, exist_ok=True)
        self._root.chmod(0o700)

    def enqueue(self, payload: dict[str, Any]) -> None:
        name = f"{time.time_ns():020d}-{uuid.uuid4().hex}"
        temporary = self._root / f"{name}.tmp"
        queued = self._root / f"{name}.json"
        temporary.write_text(json.dumps(payload), encoding="utf-8")
        temporary.chmod(0o600)
        temporary.replace(queued)

    def drain(self, client: MemoryApiClient) -> None:
        lock_path = self._root / "drain.lock"
        with lock_path.open("a+", encoding="utf-8") as lock:
            lock_path.chmod(0o600)
            if not _acquire_drain_lock(lock):
                return

            try:
                while True:
                    queued = sorted(self._root.glob("*.json"))
                    if not queued:
                        self._failure_path().unlink(missing_ok=True)
                        return

                    for path in queued:
                        claimed = path.with_suffix(".sending")
                        try:
                            path.replace(claimed)
                        except FileNotFoundError:
                            continue

                        try:
                            payload = json.loads(claimed.read_text(encoding="utf-8"))
                            if not isinstance(payload, dict):
                                raise MemoryHookError(
                                    f"Queued hook payload '{claimed.name}' is not an object."
                                )
                            prepared = _with_attachments(payload, wait_for_prompt=True)
                            if prepared is not payload:
                                # Keep bytes in the retry record even if Codex removes
                                # the pasted file or transcript before the next drain.
                                claimed.write_text(json.dumps(prepared), encoding="utf-8")
                            payload = prepared
                            client.record(payload)
                        except Exception:
                            claimed.replace(path)
                            raise
                        else:
                            claimed.unlink(missing_ok=True)
            finally:
                _release_drain_lock(lock)

    def record_failure(self, error: Exception) -> None:
        failure = self._failure_path()
        failure.write_text(str(error)[:1000], encoding="utf-8")
        failure.chmod(0o600)

    def last_failure(self) -> str | None:
        try:
            message = self._failure_path().read_text(encoding="utf-8").strip()
        except OSError:
            return None
        return message or None

    def _failure_path(self) -> Path:
        return self._root / "last-error.txt"


def _acquire_drain_lock(lock: IO[str]) -> bool:
    """Take the exclusive drain lock, or report that another worker holds it.

    Only one worker may send queued records at a time. POSIX waits on ``flock``;
    Windows has no ``fcntl``, and ``msvcrt.locking`` gives up after its own retry
    window, in which case the worker that owns the lock drains the same queue.
    """
    if fcntl is not None:
        fcntl.flock(lock.fileno(), fcntl.LOCK_EX)
        return True

    lock.seek(0)
    try:
        msvcrt.locking(lock.fileno(), msvcrt.LK_LOCK, 1)
    except OSError:
        return False
    return True


def _release_drain_lock(lock: IO[str]) -> None:
    if fcntl is not None:
        fcntl.flock(lock.fileno(), fcntl.LOCK_UN)
        return

    lock.seek(0)
    msvcrt.locking(lock.fileno(), msvcrt.LK_UNLCK, 1)


def process_hook(
    event: dict[str, Any],
    *,
    queue: MemoryHookQueue | None = None,
    worker_starter: Callable[[], None] | None = None,
) -> dict[str, Any] | None:
    event_name = str(event.get("hook_event_name", ""))

    if event_name == "SessionStart" and event.get("source") == "compact":
        session_id = _required_guid(event, "session_id")
        return _summary_output(session_id)

    memory_queue = queue or MemoryHookQueue(_queue_directory())
    start_worker = worker_starter or _start_worker

    if event_name in RECORDED_EVENTS:
        _required_guid(event, "session_id")
        _required_guid(event, "turn_id")
        previous_failure = memory_queue.last_failure()
        if event_name == "UserPromptSubmit" and event.get("transcript_path"):
            event = dict(event, memory_submitted_at_ms=int(time.time() * 1000))
        memory_queue.enqueue(event)
        start_worker()
        return _backlog_warning(previous_failure)

    if event_name == "SessionEnd":
        start_worker()

    return None


def _with_attachments(
    event: dict[str, Any], *, wait_for_prompt: bool = False
) -> dict[str, Any]:
    """Read images after submission, scoped by the transcript's turn identity.

    UserPromptSubmit runs before the user message is written to the rollout.
    The detached worker briefly waits for that record; Stop retries if the
    rollout was not flushed yet. Never match by prompt text: consecutive turns
    can repeat it. Only the worker waits, so submission remains unblocked.
    """
    if (
        event.get("hook_event_name") not in RECORDED_EVENTS
        or not event.get("transcript_path")
    ):
        return event

    wait_seconds = (
        3 if wait_for_prompt and event["hook_event_name"] == "UserPromptSubmit" else 0
    )
    deadline = time.monotonic() + wait_seconds
    while True:
        attachments, found = _transcript_attachments(event)
        if found or time.monotonic() >= deadline:
            break
        time.sleep(0.1)
    return {"hook": event, "attachments": attachments} if attachments else event


def _transcript_attachments(event: dict[str, Any]) -> tuple[list[dict[str, Any]], bool]:
    attachments = []
    messages = _transcript_messages(event)
    submitted_at = event.get("memory_submitted_at_ms")
    found = False
    for message_index, payload in enumerate(messages):
        if event["hook_event_name"] == "UserPromptSubmit":
            if submitted_at is not None:
                recorded_at = payload.get("memory_recorded_at_ms")
                if recorded_at is None or recorded_at < submitted_at:
                    continue
            elif message_index != 0:
                continue
        found = True
        image_index = 0
        for source in payload.get("images") or []:
            image_index += 1
            attachment = _inline_image(source, image_index)
            if attachment:
                attachment["user_message_index"] = message_index
                attachments.append(attachment)
        for source in payload.get("local_images") or []:
            image_index += 1
            attachment = _local_image(source, event, image_index)
            if attachment:
                attachment["user_message_index"] = message_index
                attachments.append(attachment)
        if event["hook_event_name"] == "UserPromptSubmit":
            break
    return attachments, found


def _transcript_messages(event: dict[str, Any]) -> list[dict[str, Any]]:
    messages = []
    current_turn = None
    try:
        with Path(event["transcript_path"]).open(encoding="utf-8") as transcript:
            for line in transcript:
                try:
                    record = json.loads(line)
                except json.JSONDecodeError:
                    continue
                if not isinstance(record, dict):
                    continue
                payload = record.get("payload")
                if not isinstance(payload, dict):
                    continue
                record_type = record.get("type")
                if record_type == "turn_context" or (
                    record_type == "event_msg" and payload.get("type") == "task_started"
                ):
                    current_turn = payload.get("turn_id")
                if (
                    current_turn == event["turn_id"]
                    and record_type == "event_msg"
                    and payload.get("type") == "user_message"
                ):
                    timestamp = record.get("timestamp")
                    try:
                        recorded_at = int(datetime.fromisoformat(timestamp.replace("Z", "+00:00")).timestamp() * 1000)
                    except (AttributeError, ValueError, OverflowError):
                        recorded_at = None
                    messages.append(dict(payload, memory_recorded_at_ms=recorded_at))
    except (OSError, UnicodeError):
        # Text memory must still be delivered if a rollout is no longer available.
        return []
    return messages


def _inline_image(source: Any, index: int) -> dict[str, Any] | None:
    if not isinstance(source, str) or not source.startswith("data:"):
        return None
    header, separator, content = source.partition(",")
    media_type = header[5:-7].lower() if header.endswith(";base64") else ""
    if not separator or not header.endswith(";base64") or media_type not in IMAGE_EXTENSIONS:
        return None
    try:
        decoded = base64.b64decode(content, validate=True)
    except (ValueError, binascii.Error):
        return None
    if not decoded:
        return None
    return _image_upload(decoded, media_type, index)


def _local_image(source: Any, event: dict[str, Any], index: int) -> dict[str, Any] | None:
    if not isinstance(source, str) or not source:
        return None
    path = Path(source)
    if not path.is_absolute():
        path = Path(event.get("cwd") or Path.cwd()) / path
    extension = path.suffix.lower().lstrip(".")
    media_type = next(
        (media for media, ext in IMAGE_EXTENSIONS.items() if ext == extension),
        "image/jpeg" if extension == "jpeg" else None,
    )
    if media_type is None:
        return None
    try:
        content = path.read_bytes()
    except OSError:
        return None
    return _image_upload(content, media_type, index) if content else None


def _image_upload(content: bytes, media_type: str, index: int) -> dict[str, Any]:
    return {
        "name": f"image-{index}.{IMAGE_EXTENSIONS[media_type]}",
        "media_type": media_type,
        "content": base64.b64encode(content).decode("ascii"),
        "placeholder": f"[Image #{index}]",
    }


def drain_queue(
    *,
    queue: MemoryHookQueue | None = None,
    client: MemoryApiClient | None = None,
) -> None:
    memory_queue = queue or MemoryHookQueue(_queue_directory())
    api_client = client or RoutedMemoryApiClient()
    try:
        memory_queue.drain(api_client)
    except Exception as error:
        memory_queue.record_failure(error)


def _required_guid(event: dict[str, Any], field: str) -> str:
    value = str(event.get(field, ""))
    try:
        return str(uuid.UUID(value))
    except ValueError as error:
        raise MemoryHookError(
            f"Codex hook input did not include a valid {field}."
        ) from error


def _summary_output(session_id: str) -> dict[str, Any]:
    context = (
        "This chat has just been compacted. Before continuing normal work, "
        "write a concise two-to-four paragraph checkpoint from the compacted "
        "conversation and call the MCP Knowledge Base tool memory_summary_add with "
        f"threadId '{session_id}'. Capture goals, decisions, important changes, "
        "verification, unresolved work, and durable user preferences. Omit "
        "repetitive tool output, credentials, and other secrets."
    )
    return {
        "hookSpecificOutput": {
            "hookEventName": "SessionStart",
            "additionalContext": context,
        }
    }


def _backlog_warning(previous_failure: str | None) -> dict[str, Any] | None:
    if not previous_failure:
        return None
    return {
        "systemMessage": (
            "MCP Knowledge Base queued this memory record, but an earlier queued "
            f"record has not reached the Memory API yet: {previous_failure}"
        )
    }


def _queue_directory() -> Path:
    configured = os.environ.get("PLUGIN_DATA")
    root = (
        Path(configured)
        if configured
        else Path(tempfile.gettempdir()) / "mcp-knowledge-base-plugin"
    )
    return root / "memory-queue"


def _memory_hook_url() -> str:
    override = os.environ.get("MCP_KNOWLEDGE_BASE_MEMORY_HOOK_URL")
    if override:
        return override

    mcp_url = os.environ.get("MCP_KNOWLEDGE_BASE_URL")
    if not mcp_url:
        return DEFAULT_MEMORY_HOOK_URL

    parsed = urllib.parse.urlsplit(mcp_url)
    return urllib.parse.urlunsplit(
        (
            parsed.scheme,
            parsed.netloc,
            "/api/memory/codex/prompt-hooks",
            "",
            "",
        )
    )


def _tool_use_hook_url() -> str:
    override = os.environ.get("MCP_KNOWLEDGE_BASE_TOOL_USE_HOOK_URL")
    if override:
        return override

    parsed = urllib.parse.urlsplit(
        os.environ.get("MCP_KNOWLEDGE_BASE_URL") or "http://localhost:5231/mcp"
    )
    return urllib.parse.urlunsplit(
        (parsed.scheme, parsed.netloc, "/api/memory/codex/tool-calls", "", "")
    )


def _start_worker() -> None:
    subprocess.Popen(
        [sys.executable, str(Path(__file__).resolve()), "--drain"],
        cwd=str(Path(__file__).resolve().parent),
        stdin=subprocess.DEVNULL,
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
        close_fds=True,
        **_detached_process_options(),
    )


def _detached_process_options() -> dict[str, Any]:
    """Keep the drain worker alive and silent after the hook process exits.

    ``start_new_session`` is POSIX-only and is ignored on Windows, where the
    equivalent is detaching the child from the console it would inherit.
    """
    if hasattr(subprocess, "DETACHED_PROCESS"):
        return {
            "creationflags": (
                subprocess.DETACHED_PROCESS | subprocess.CREATE_NEW_PROCESS_GROUP
            )
        }
    return {"start_new_session": True}


def main() -> int:
    if len(sys.argv) == 2 and sys.argv[1] == "--drain":
        drain_queue()
        return 0

    event_name = ""
    try:
        event = json.load(sys.stdin)
        if not isinstance(event, dict):
            raise MemoryHookError("Codex hook input was not a JSON object.")
        event_name = str(event.get("hook_event_name", ""))
        output = process_hook(event)
    except (MemoryHookError, OSError, json.JSONDecodeError) as error:
        output = {
            "systemMessage": (
                "MCP Knowledge Base could not queue this memory hook. Normal work "
                f"can continue, but this record may be missing: {error}"
            )
        }

    if output is not None:
        print(json.dumps(output))
    elif event_name == "Stop":
        print("{}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
