#!/usr/bin/env python3
"""Queue Claude tool inputs and outputs for memory recording."""

from __future__ import annotations

import json
import os
import subprocess
import sys
from pathlib import Path
from typing import Any, Callable

from write_memory import (
    MemoryHookError,
    MemoryHookQueue,
    TurnRegistry,
    _backlog_warning,
    _detached_process_options,
    _queue_directory,
    _required_guid,
    _tool_use_hook_url as _shared_tool_use_hook_url,
    _turn_directory,
    drain_queue,
)


def process_hook(
    event: dict[str, Any],
    *,
    queue: MemoryHookQueue | None = None,
    turns: TurnRegistry | None = None,
    worker_starter: Callable[[], None] | None = None,
) -> dict[str, Any] | None:
    if str(event.get("hook_event_name", "")) != "PostToolUse":
        return None

    session_id = _required_guid(event, "session_id")
    tool_queue = queue or MemoryHookQueue(_queue_directory())
    turn_registry = turns or TurnRegistry(_turn_directory())
    turn_id = turn_registry.current_turn(session_id)

    previous_failure = tool_queue.last_failure()
    tool_queue.enqueue({**event, "turn_id": turn_id})
    (worker_starter or _start_worker)()
    return _backlog_warning(previous_failure)


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
        drain_queue()
        return 0

    try:
        event = json.load(sys.stdin)
        if not isinstance(event, dict):
            raise MemoryHookError("Claude hook input was not a JSON object.")
        output = process_hook(event)
    except (MemoryHookError, OSError, json.JSONDecodeError) as error:
        output = {
            "systemMessage": (
                "MCP Knowledge Base could not queue this tool-use hook. Normal "
                f"work can continue, but this record may be missing: {error}"
            )
        }

    if output is not None:
        print(json.dumps(output))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
