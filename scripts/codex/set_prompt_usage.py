#!/usr/bin/env python3
"""Migrate completed Codex turns using deltas of cumulative token counters."""
from __future__ import annotations

import sys
from dataclasses import dataclass, field
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from prompt_usage_common import FIELDS, count, guid, records, run

SOURCE_FIELDS = ("input_tokens", "cached_input_tokens", "cache_write_input_tokens", "output_tokens", "reasoning_output_tokens")


@dataclass
class Turn:
    totals: list[int | None] = field(default_factory=lambda: [0] * 5)
    source_records: list[dict] = field(default_factory=list)
    baseline_record: dict | None = None
    seen: bool = False
    complete: bool = False
    invalid: bool = False


def read_session(path: Path):
    session_id = None
    turns: dict[str, Turn] = {}
    active = None
    previous = None
    previous_record = None
    forked = False
    for entry in records(path):
        payload = entry.get("payload")
        if not isinstance(payload, dict):
            continue
        kind = payload.get("type")
        if entry.get("type") == "session_meta":
            if isinstance(payload.get("source"), dict) and "subagent" in payload["source"]:
                return None, None
            identifier = guid(payload.get("id"))
            if session_id and identifier != session_id:
                raise ValueError("Conflicting session IDs")
            session_id = identifier
            forked = forked or bool(payload.get("forked_from_id"))
        elif entry.get("type") == "event_msg" and kind == "task_started":
            active = guid(payload.get("turn_id"))
            if active:
                turns.setdefault(active, Turn())
        elif entry.get("type") == "turn_context":
            identifier = guid(payload.get("turn_id"))
            if identifier and identifier != active:
                active = identifier
                turns.setdefault(active, Turn())
        elif entry.get("type") == "event_msg" and kind in ("task_complete", "task_completed", "turn_aborted"):
            identifier = guid(payload.get("turn_id")) or active
            if identifier in turns:
                turns[identifier].complete = kind != "turn_aborted"
                turns[identifier].invalid |= kind == "turn_aborted"
        elif entry.get("type") == "event_msg" and kind == "token_count":
            info = payload.get("info")
            if not isinstance(info, dict):  # Rate-limit-only updates carry no usage.
                continue
            total = info.get("total_token_usage")
            if not isinstance(total, dict):
                if active:
                    turns[active].invalid = True
                continue
            current = [count(total.get(key)) for key in SOURCE_FIELDS]
            turn = turns.get(active)
            if previous is None:
                last = info.get("last_token_usage") or {}
                # Establish a zero baseline only when the first total is one request.
                # An inherited or truncated first total must never be charged to this turn.
                first_request = not forked and all(current[i] is not None and current[i] == count(last.get(SOURCE_FIELDS[i])) for i in (0, 3))
                baseline = [0] * 5 if first_request else current
                if turn and not first_request:
                    turn.invalid = True
            else:
                baseline = previous
            if turn:
                if not turn.seen:
                    turn.baseline_record = previous_record
                turn.source_records.append(entry)
                turn.seen = True
                for i, value in enumerate(current):
                    before = baseline[i]
                    if value is None or before is None:
                        turn.totals[i] = None
                    elif value < before:
                        turn.invalid = True  # Counter reset; later turns use the new baseline.
                    elif turn.totals[i] is not None:
                        turn.totals[i] += value - before
            previous = current
            previous_record = entry

    def proposals(conversation):
        prompt_ids = {message["promptId"] for message in conversation.get("messages", []) if message.get("promptId")}
        prompt_ids.update(call["promptId"] for call in conversation.get("toolCalls", []) if call.get("promptId"))
        result = {}
        for prompt_id in prompt_ids:
            turn = turns.get(prompt_id)
            if not turn or not turn.complete or not turn.seen or turn.invalid or any(value is None for value in turn.totals[:4]):
                continue
            result[prompt_id] = dict(zip(FIELDS, turn.totals))
            result[prompt_id]["sourceRecords"] = turn.source_records
            result[prompt_id]["baselineRecord"] = turn.baseline_record
        return result, len(prompt_ids) - len(result)

    return session_id, proposals


if __name__ == "__main__":
    raise SystemExit(run("codex", read_session, Path.home() / ".codex" / "sessions"))
