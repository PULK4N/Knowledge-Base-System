#!/usr/bin/env python3
"""Migrate Claude usage, counting each assistant message ID once per session."""
from __future__ import annotations

import sys
from dataclasses import dataclass, field
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from claude.set_prompt_models import (
    SessionTurn, _prompt_text, _queued_prompt, normalize_text,
    parse_timestamp, read_memory_prompts, _seconds_apart,
)
from prompt_usage_common import count, guid, records, run


@dataclass
class UsageTurn(SessionTurn):
    replies: dict[str, dict | None] = field(default_factory=dict)
    complete: bool = False
    invalid: bool = False


def read_session(path: Path):
    session_id = guid(path.stem)
    if not session_id:  # Excludes subagent files.
        return None, None
    turns: list[UsageTurn] = []
    owners: dict[str, UsageTurn] = {}
    seen_users = set()
    for entry in records(path):
        if entry.get("isSidechain"):
            continue
        queued = _queued_prompt(entry)
        message = entry.get("message", {})
        if queued:
            turns.append(UsageTurn(queued.text, queued.timestamp))
            continue
        if not isinstance(message, dict):
            continue
        if entry.get("type") == "user" and not entry.get("isMeta"):
            text = _prompt_text(message.get("content"))
            entry_id = entry.get("uuid")
            if entry_id and entry_id in seen_users:
                continue
            if entry_id:
                seen_users.add(entry_id)
            if text is not None:
                turns.append(UsageTurn(text, parse_timestamp(entry.get("timestamp"))))
        elif entry.get("type") == "assistant" and turns and message.get("model") != "<synthetic>":
            message_id = message.get("id")
            if not message_id:
                turns[-1].invalid = True
                continue
            turn = owners.setdefault(message_id, turns[-1])
            for block in message.get("content") or []:
                if isinstance(block, dict) and block.get("type") == "tool_use" and block.get("id"):
                    turn.tool_use_ids.add(block["id"])
            usage = message.get("usage")
            parsed = None
            if isinstance(usage, dict):
                keys = ("input_tokens", "cache_read_input_tokens", "cache_creation_input_tokens", "output_tokens")
                values = [count(usage.get(key)) for key in keys]
                if all(value is not None for value in values):
                    details = usage.get("output_tokens_details") or {}
                    parsed = dict(zip(("inputTokens", "cacheReadInputTokens", "cacheCreationInputTokens", "outputTokens"), values))
                    parsed["thinkingTokens"] = count(details.get("thinking_tokens"))
            previous = turn.replies.get(message_id)
            # Streaming copies may carry progressively larger output counts.
            if previous and parsed:
                if any(previous[key] != parsed[key] for key in ("inputTokens", "cacheReadInputTokens", "cacheCreationInputTokens")):
                    turn.invalid = True
                if previous["outputTokens"] > parsed["outputTokens"]:
                    parsed = previous
            turn.replies[message_id] = parsed or previous
            if message.get("stop_reason") == "end_turn":
                turn.complete = True
            elif message.get("stop_reason") == "tool_use":
                turn.complete = False

    def proposals(conversation):
        prompts = read_memory_prompts(conversation)
        matches = {}
        claimed = set()
        # Match one-to-one. Never assign the same reply's usage to two prompts.
        for prompt in prompts:
            overlaps = [i for i, turn in enumerate(turns) if turn.tool_use_ids & prompt.tool_use_ids]
            candidates = overlaps or [i for i, turn in enumerate(turns)
                                      if prompt.text is not None and normalize_text(turn.text) == normalize_text(prompt.text)]
            if len(candidates) > 1:
                candidates = [i for i in candidates if _seconds_apart(turns[i].timestamp, prompt.timestamp) <= 10]
            if len(candidates) == 1:
                matches[prompt.prompt_id] = candidates[0]
        multiplicities = {index: list(matches.values()).count(index) for index in set(matches.values())}
        result = {}
        for prompt_id, index in matches.items():
            turn = turns[index]
            if multiplicities[index] != 1 or index in claimed or not turn.complete or turn.invalid or not turn.replies or any(value is None for value in turn.replies.values()):
                continue
            claimed.add(index)
            usages = list(turn.replies.values())
            result[prompt_id] = {key: None if any(usage[key] is None for usage in usages)
                                else sum(usage[key] for usage in usages) for key in usages[0]}
        return result, len(prompts) - len(result)

    return session_id, proposals


if __name__ == "__main__":
    raise SystemExit(run("claude", read_session, Path.home() / ".claude" / "projects"))
