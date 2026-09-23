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


FEATURE_MUTATION_TOOLS = frozenset(
    {
        "feature_add",
        "feature_remove",
        "feature_status_update",
        "feature_summary_update",
        "feature_skill_add",
        "feature_skill_remove",
        "feature_record_add",
        "feature_record_update",
        "feature_record_remove",
        "feature_review_note_add",
        "feature_review_note_update",
        "feature_review_note_remove",
        "feature_research_discovery_add",
        "feature_research_discovery_update",
        "feature_research_discovery_remove",
        "feature_plan_add",
        "feature_plan_current_update",
        "feature_plan_current_change",
        "feature_plan_remove",
    }
)


def process_hook(
    event: dict[str, Any],
    *,
    queue: MemoryHookQueue | None = None,
    turns: TurnRegistry | None = None,
    worker_starter: Callable[[], None] | None = None,
) -> dict[str, Any] | None:
    event_name = str(event.get("hook_event_name", ""))
    if event_name == "PreToolUse":
        return _inject_mutation_session(event)
    if event_name != "PostToolUse":
        return None

    session_id = _required_guid(event, "session_id")
    tool_queue = queue or MemoryHookQueue(_queue_directory())
    turn_registry = turns or TurnRegistry(_turn_directory())
    turn_id = turn_registry.current_turn(session_id)

    previous_failure = tool_queue.last_failure()
    tool_queue.enqueue({**event, "turn_id": turn_id})
    (worker_starter or _start_worker)()
    return _backlog_warning(previous_failure)


def _inject_skill_session(event: dict[str, Any]) -> dict[str, Any] | None:
    return _inject_mutation_session(event)


def _inject_mutation_session(event: dict[str, Any]) -> dict[str, Any] | None:
    """Adds the Claude session to a skill or feature change so the API can link it to memory.

    ``permissionDecision`` is left out on purpose: Claude Code applies
    ``updatedInput`` on its own, so the user's normal permission rules still
    decide whether the change may run.
    """
    tool_name = str(event.get("tool_name", "")).rsplit("__", 1)[-1]
    if tool_name not in SKILL_MUTATION_TOOLS | FEATURE_MUTATION_TOOLS:
        return None

    session_id = _required_guid(event, "session_id")
    tool_input = event.get("tool_input")
    if not isinstance(tool_input, dict):
        raise MemoryHookError("Claude PreToolUse input did not include tool_input.")

    return {
        "hookSpecificOutput": {
            "hookEventName": "PreToolUse",
            "updatedInput": {**tool_input, "sessionId": session_id},
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
