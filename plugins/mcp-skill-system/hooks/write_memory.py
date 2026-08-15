#!/usr/bin/env python3
"""Queue useful Codex hook payloads and request summaries after compaction."""

from __future__ import annotations

import fcntl
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
from pathlib import Path
from typing import Any, Callable


DEFAULT_MEMORY_HOOK_URL = (
    "http://localhost:5231/api/memory/codex/prompt-hooks"
)
RECORDED_EVENTS = frozenset({"UserPromptSubmit", "Stop"})


class MemoryHookError(RuntimeError):
    pass


class MemoryApiClient:
    def __init__(self, url: str, timeout_seconds: int = 20) -> None:
        self._url = url
        self._timeout_seconds = timeout_seconds

    def record(self, payload: dict[str, Any]) -> None:
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
            fcntl.flock(lock.fileno(), fcntl.LOCK_EX)

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
                        client.record(payload)
                    except Exception:
                        claimed.replace(path)
                        raise
                    else:
                        claimed.unlink(missing_ok=True)

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
        memory_queue.enqueue(event)
        start_worker()
        return _backlog_warning(previous_failure)

    if event_name == "SessionEnd":
        start_worker()

    return None


def drain_queue(
    *,
    queue: MemoryHookQueue | None = None,
    client: MemoryApiClient | None = None,
) -> None:
    memory_queue = queue or MemoryHookQueue(_queue_directory())
    api_client = client or MemoryApiClient(_memory_hook_url())
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
        "conversation and call the MCP Skill System tool memory_summary_add with "
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
            "MCP Skill System queued this memory record, but an earlier queued "
            f"record has not reached the Memory API yet: {previous_failure}"
        )
    }


def _queue_directory() -> Path:
    configured = os.environ.get("PLUGIN_DATA")
    root = (
        Path(configured)
        if configured
        else Path(tempfile.gettempdir()) / "mcp-skill-system-plugin"
    )
    return root / "memory-queue"


def _memory_hook_url() -> str:
    override = os.environ.get("MCP_SKILL_SYSTEM_MEMORY_HOOK_URL")
    if override:
        return override

    mcp_url = os.environ.get("MCP_SKILL_SYSTEM_URL")
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


def _start_worker() -> None:
    subprocess.Popen(
        [sys.executable, str(Path(__file__).resolve()), "--drain"],
        cwd=str(Path(__file__).resolve().parent),
        stdin=subprocess.DEVNULL,
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
        close_fds=True,
        start_new_session=True,
    )


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
                "MCP Skill System could not queue this memory hook. Normal work "
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
