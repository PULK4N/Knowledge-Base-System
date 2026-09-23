#!/usr/bin/env python3
"""Queue Codex tool inputs and outputs for memory recording."""

from __future__ import annotations

import json
import os
import subprocess
import sys
import uuid
from pathlib import Path
from typing import Any, Callable

from write_memory import (
    MemoryHookError,
    MemoryHookQueue,
    _backlog_warning,
    _detached_process_options,
    _queue_directory,
    _required_guid,
    _tool_use_hook_url as _shared_tool_use_hook_url,
    drain_queue,
)


DEFAULT_TOOL_USE_HOOK_URL = (
    "http://localhost:5231/api/memory/codex/tool-calls"
)
SKILL_MUTATION_TOOLS = frozenset(
    {
        "skill_add",
        "skill_update",
        "skill_delete",
        "skill_reference_add",
        "skill_reference_update",
        "skill_reference_auto_load_update",
        "skill_reference_delete",
        "skill_attachment_add",
        "skill_attachment_delete",
    }
)


def process_hook(
    event: dict[str, Any],
    *,
    queue: MemoryHookQueue | None = None,
    worker_starter: Callable[[], None] | None = None,
) -> dict[str, Any] | None:
    event_name = str(event.get("hook_event_name", ""))
    if event_name == "PreToolUse":
        return _inject_skill_session(event)
    if event_name != "PostToolUse":
        return None

    _required_guid(event, "session_id")
    _required_guid(event, "turn_id")

    tool_queue = queue or MemoryHookQueue(_queue_directory())
    previous_failure = tool_queue.last_failure()
    tool_queue.enqueue(event)
    (worker_starter or _start_worker)()
    return _backlog_warning(previous_failure)


def _inject_skill_session(event: dict[str, Any]) -> dict[str, Any] | None:
    tool_name = str(event.get("tool_name", "")).rsplit("__", 1)[-1]
    if tool_name not in SKILL_MUTATION_TOOLS:
        return None

    session_id = _required_guid(event, "session_id")
    tool_input = event.get("tool_input")
    if not isinstance(tool_input, dict):
        raise MemoryHookError("Codex PreToolUse input did not include tool_input.")

    updated_input = dict(tool_input)
    updated_input["sessionId"] = session_id

    payload = event.get("payload")
    for container in (event, payload, tool_input):
        if not isinstance(container, dict):
            continue
        memory_aggregate_id = container.get(
            "memoryAggregateId",
            container.get("memory_aggregate_id"),
        )
        if memory_aggregate_id is None:
            continue
        try:
            updated_input["memoryAggregateId"] = str(
                uuid.UUID(str(memory_aggregate_id))
            )
        except ValueError as error:
            raise MemoryHookError(
                "Codex PreToolUse included an invalid memory aggregate ID."
            ) from error
        break

    return {
        "hookSpecificOutput": {
            "hookEventName": "PreToolUse",
            "permissionDecision": "allow",
            "updatedInput": updated_input,
        }
    }


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


def _tool_use_hook_url() -> str:
    return _shared_tool_use_hook_url()


def main() -> int:
    if len(sys.argv) == 2 and sys.argv[1] == "--drain":
        drain_queue(
            queue=MemoryHookQueue(_queue_directory()),
        )
        return 0

    try:
        event = json.load(sys.stdin)
        if not isinstance(event, dict):
            raise MemoryHookError("Codex hook input was not a JSON object.")
        output = process_hook(event)
    except (MemoryHookError, OSError, json.JSONDecodeError) as error:
        output = {
            "systemMessage": (
                "MCP Knowledge Base could not queue this tool-use hook. Normal work "
                f"can continue, but this record may be missing: {error}"
            )
        }

    if output is not None:
        print(json.dumps(output))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
