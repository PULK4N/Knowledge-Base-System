#!/usr/bin/env python3
"""Queue Codex tool inputs and outputs for memory recording."""

from __future__ import annotations

import json
import os
import re
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
    read_event,
)


DEFAULT_TOOL_USE_HOOK_URL = (
    "http://localhost:5231/api/memory/codex/tool-calls"
)
KNOWLEDGE_BASE_TOOL = re.compile(
    r"^mcp__.*mcp[-_]knowledge[-_]base__(?P<name>[a-z_]+)$"
)
"""Matches tools of the knowledge base MCP server and captures the tool name."""


READ_TOOL_SUFFIXES = (
    "_get",
    "_get_by_name",
    "_get_by_repository",
    "_list",
    "_search",
    "_search_by_time",
)
"""Knowledge base tools with these endings only read, so they get no session."""


def process_hook(
    event: dict[str, Any],
    *,
    queue: MemoryHookQueue | None = None,
    worker_starter: Callable[[], None] | None = None,
) -> dict[str, Any] | None:
    event_name = str(event.get("hook_event_name", ""))
    if event_name == "PreToolUse":
        return _inject_mutation_session(event)
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
    return _inject_mutation_session(event)


def _inject_mutation_session(event: dict[str, Any]) -> dict[str, Any] | None:
    if not _is_knowledge_base_mutation(str(event.get("tool_name", ""))):
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


def _is_knowledge_base_mutation(tool_name: str) -> bool:
    """Every knowledge base tool can change data unless its name marks it as a read."""
    match = KNOWLEDGE_BASE_TOOL.match(tool_name)
    return match is not None and not match.group("name").endswith(READ_TOOL_SUFFIXES)


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
        event = read_event(sys.stdin.buffer)
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
