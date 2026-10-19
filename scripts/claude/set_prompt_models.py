#!/usr/bin/env python3
"""Set the model of every recorded Claude memory prompt from session files.

Claude Code stores each session as ``<session id>.jsonl``. The memory plugin
records prompts under its own turn ids, which do not appear in that file, so
each memory prompt is matched to a session turn by:

1. the tool-use ids of its tool calls,
2. otherwise the prompt text,
3. otherwise the closest prompt time.

The model of the first assistant reply in the matched turn is then sent to the
Memory API. Prompts that already have a model are skipped.

Examples:
    python3 scripts/claude/set_prompt_models.py \
        ~/.claude/projects/-home-nikola-Documents-github-Knowledge-base-system
    python3 scripts/claude/set_prompt_models.py <folder> --dry-run
"""

from __future__ import annotations

import argparse
import json
import sys
import urllib.error
import urllib.parse
import urllib.request
import uuid
from dataclasses import dataclass, field
from datetime import datetime
from pathlib import Path
from typing import Any


DEFAULT_BASE_URL = "http://localhost:5231"
DEFAULT_TIMEOUT_SECONDS = 30.0
TIMESTAMP_TOLERANCE_SECONDS = 10.0
SYNTHETIC_MODEL = "<synthetic>"
ALREADY_SET_VALIDATOR = "PromptModelMustNotBeSetValidator"
QUEUED_PROMPT_MODES = ("prompt", "task-notification")


@dataclass
class SessionTurn:
    text: str
    timestamp: datetime | None
    model: str | None = None
    tool_use_ids: set[str] = field(default_factory=set)


@dataclass(frozen=True)
class MemoryPrompt:
    prompt_id: str
    text: str | None
    timestamp: datetime | None
    tool_use_ids: frozenset[str]


@dataclass
class Counts:
    set: int = 0
    already_set: int = 0
    unmatched: int = 0
    failed: int = 0
    skipped_sessions: int = 0


class ApiError(Exception):
    def __init__(self, status: int | None, body: str) -> None:
        super().__init__(f"HTTP {status}: {body[:300]}")
        self.status = status
        self.body = body


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description=(
            "Read Claude Code session .jsonl files from a folder and set the "
            "model of each recorded memory prompt through the Memory API."
        )
    )
    parser.add_argument(
        "folder",
        type=Path,
        help="Folder with Claude Code session .jsonl files",
    )
    parser.add_argument(
        "--base-url",
        default=DEFAULT_BASE_URL,
        help=f"Memory server base URL (default: {DEFAULT_BASE_URL})",
    )
    parser.add_argument(
        "--timeout",
        type=float,
        default=DEFAULT_TIMEOUT_SECONDS,
        help="HTTP timeout in seconds",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Show the models that would be set without calling the API",
    )
    return parser.parse_args()


def parse_timestamp(value: Any) -> datetime | None:
    if not isinstance(value, str) or not value:
        return None

    try:
        return datetime.fromisoformat(value.replace("Z", "+00:00"))
    except ValueError:
        # Python before 3.11 rejects fractions that are not 3 or 6 digits.
        head, _, rest = value.replace("Z", "").partition(".")
        fraction = "".join(ch for ch in rest if ch.isdigit())[:6]
        try:
            return datetime.fromisoformat(
                f"{head}.{fraction.ljust(6, '0')}+00:00"
            )
        except ValueError:
            return None


def normalize_text(text: str | None) -> str:
    return " ".join((text or "").split())


def _prompt_text(content: Any) -> str | None:
    """Returns the prompt text of a user entry, or None for tool results."""
    if isinstance(content, str):
        return content

    if not isinstance(content, list) or not content:
        return None

    if all(
        isinstance(block, dict) and block.get("type") == "tool_result"
        for block in content
    ):
        return None

    return "\n".join(
        block.get("text", "")
        for block in content
        if isinstance(block, dict) and block.get("type") == "text"
    )


def _queued_prompt(entry: dict[str, Any]) -> SessionTurn | None:
    """Returns a prompt typed while Claude was still working.

    Claude Code stores such a prompt as a ``queued_command`` attachment
    instead of a user entry, and the replies that follow answer it.
    """
    attachment = entry.get("attachment")
    if (
        entry.get("type") != "attachment"
        or not isinstance(attachment, dict)
        or attachment.get("type") != "queued_command"
        or attachment.get("commandMode", "prompt") not in QUEUED_PROMPT_MODES
        or not isinstance(attachment.get("prompt"), str)
    ):
        return None

    return SessionTurn(
        text=attachment["prompt"],
        timestamp=parse_timestamp(
            attachment.get("timestamp") or entry.get("timestamp")
        ),
    )


def read_session_turns(path: Path) -> list[SessionTurn]:
    """Splits a session file into turns, one per user prompt."""
    turns: list[SessionTurn] = []

    with path.open(encoding="utf-8", errors="replace") as lines:
        for line in lines:
            try:
                entry = json.loads(line)
            except json.JSONDecodeError:
                continue

            if not isinstance(entry, dict) or entry.get("isSidechain"):
                continue

            queued = _queued_prompt(entry)
            if queued is not None:
                turns.append(queued)
                continue

            message = entry.get("message")
            if not isinstance(message, dict):
                continue

            if entry.get("type") == "user":
                if entry.get("isMeta"):
                    continue
                text = _prompt_text(message.get("content"))
                if text is not None:
                    turns.append(
                        SessionTurn(
                            text=text,
                            timestamp=parse_timestamp(entry.get("timestamp")),
                        )
                    )
            elif entry.get("type") == "assistant" and turns:
                turn = turns[-1]
                model = message.get("model")
                if (
                    turn.model is None
                    and isinstance(model, str)
                    and model
                    and model != SYNTHETIC_MODEL
                ):
                    turn.model = model
                content = message.get("content")
                if isinstance(content, list):
                    turn.tool_use_ids.update(
                        block["id"]
                        for block in content
                        if isinstance(block, dict)
                        and block.get("type") == "tool_use"
                        and isinstance(block.get("id"), str)
                    )

    return turns


def read_memory_prompts(conversation: dict[str, Any]) -> list[MemoryPrompt]:
    texts: dict[str, str] = {}
    timestamps: dict[str, datetime | None] = {}
    tool_use_ids: dict[str, set[str]] = {}
    order: list[str] = []

    def remember(prompt_id: str, timestamp: Any) -> None:
        if prompt_id not in timestamps:
            order.append(prompt_id)
            timestamps[prompt_id] = parse_timestamp(timestamp)
            tool_use_ids[prompt_id] = set()

    for message in conversation.get("messages") or []:
        prompt_id = str(message.get("promptId", ""))
        if not prompt_id:
            continue
        remember(prompt_id, message.get("timestamp"))
        if message.get("hookEventName") == "UserPromptSubmit":
            texts.setdefault(prompt_id, message.get("message") or "")

    for tool_call in conversation.get("toolCalls") or []:
        prompt_id = str(tool_call.get("promptId", ""))
        if not prompt_id:
            continue
        remember(prompt_id, tool_call.get("timestamp"))
        if tool_call.get("toolUseId"):
            tool_use_ids[prompt_id].add(str(tool_call["toolUseId"]))

    return [
        MemoryPrompt(
            prompt_id=prompt_id,
            text=texts.get(prompt_id),
            timestamp=timestamps[prompt_id],
            tool_use_ids=frozenset(tool_use_ids[prompt_id]),
        )
        for prompt_id in order
    ]


def _seconds_apart(first: datetime | None, second: datetime | None) -> float:
    if first is None or second is None:
        return float("inf")
    return abs((first - second).total_seconds())


def match_prompts(
    prompts: list[MemoryPrompt],
    turns: list[SessionTurn],
) -> dict[str, SessionTurn]:
    """Matches memory prompts to session turns.

    A tool-use id match is exact, so it may share a turn with other prompts:
    the plugin can record the tool calls of one reply under a later prompt,
    such as a ``/login`` command that never appears in the session file.
    Text and time matches take each remaining turn at most once.
    """
    matches: dict[str, SessionTurn] = {}
    used: set[int] = set()

    def claim(prompt: MemoryPrompt, candidates: list[int]) -> None:
        free = [index for index in candidates if index not in used]
        if not free:
            return
        index = min(
            free,
            key=lambda i: _seconds_apart(turns[i].timestamp, prompt.timestamp),
        )
        used.add(index)
        matches[prompt.prompt_id] = turns[index]

    for prompt in prompts:
        overlaps = [
            (len(turn.tool_use_ids & prompt.tool_use_ids), turn)
            for turn in turns
        ]
        best_overlap, best_turn = max(
            overlaps, key=lambda pair: pair[0], default=(0, None)
        )
        if best_overlap and best_turn is not None:
            matches[prompt.prompt_id] = best_turn

    for prompt in prompts:
        if prompt.prompt_id in matches or prompt.text is None:
            continue
        text = normalize_text(prompt.text)
        claim(
            prompt,
            [
                index
                for index, turn in enumerate(turns)
                if normalize_text(turn.text) == text
            ],
        )

    for prompt in prompts:
        if prompt.prompt_id in matches:
            continue
        claim(
            prompt,
            [
                index
                for index, turn in enumerate(turns)
                if _seconds_apart(turn.timestamp, prompt.timestamp)
                <= TIMESTAMP_TOLERANCE_SECONDS
            ],
        )

    return matches


class MemoryApi:
    def __init__(self, base_url: str, timeout: float) -> None:
        self._base_url = base_url.rstrip("/")
        self._timeout = timeout

    def find_memory_id(self, session_id: str) -> str | None:
        query = urllib.parse.urlencode({"search": session_id, "pageSize": 25})
        result = self._send("GET", f"/api/memories?{query}")
        for item in result.get("items") or []:
            if str(item.get("threadId", "")).lower() == session_id.lower():
                return str(item["memoryId"])
        return None

    def get_conversation(self, memory_id: str) -> dict[str, Any]:
        return self._send("GET", f"/api/memories/{memory_id}/conversation")

    def set_prompt_model(
        self,
        session_id: str,
        prompt_id: str,
        model: str,
    ) -> None:
        self._send(
            "PUT",
            f"/api/memory/sessions/{session_id}/prompts/{prompt_id}/model",
            {"model": model},
        )

    def _send(
        self,
        method: str,
        path: str,
        body: dict[str, Any] | None = None,
    ) -> dict[str, Any]:
        request = urllib.request.Request(
            f"{self._base_url}{path}",
            data=None if body is None else json.dumps(body).encode("utf-8"),
            method=method,
            headers={"Content-Type": "application/json"},
        )
        try:
            with urllib.request.urlopen(request, timeout=self._timeout) as response:
                content = response.read().decode("utf-8")
        except urllib.error.HTTPError as error:
            raise ApiError(
                error.code,
                error.read().decode("utf-8", errors="replace"),
            ) from error
        except urllib.error.URLError as error:
            raise ApiError(None, str(error.reason)) from error

        return json.loads(content) if content else {}


def session_id_of(path: Path) -> str | None:
    try:
        return str(uuid.UUID(path.stem))
    except ValueError:
        return None


def process_session(
    api: MemoryApi,
    path: Path,
    counts: Counts,
    dry_run: bool,
) -> None:
    session_id = session_id_of(path)
    if session_id is None:
        counts.skipped_sessions += 1
        return

    memory_id = api.find_memory_id(session_id)
    if memory_id is None:
        print(f"{session_id}: no memory recorded, skipped")
        counts.skipped_sessions += 1
        return

    prompts = read_memory_prompts(api.get_conversation(memory_id))
    matches = match_prompts(prompts, read_session_turns(path))

    for prompt in prompts:
        turn = matches.get(prompt.prompt_id)
        if turn is None or turn.model is None:
            reason = (
                "no matching prompt in the session file"
                if turn is None
                else "the prompt has no assistant reply"
            )
            print(f"{session_id} {prompt.prompt_id}: skipped, {reason}")
            counts.unmatched += 1
            continue

        if dry_run:
            print(f"{session_id} {prompt.prompt_id}: would set {turn.model}")
            counts.set += 1
            continue

        try:
            api.set_prompt_model(session_id, prompt.prompt_id, turn.model)
        except ApiError as error:
            if ALREADY_SET_VALIDATOR in error.body:
                counts.already_set += 1
                continue
            print(f"{session_id} {prompt.prompt_id}: failed: {error}")
            counts.failed += 1
            continue

        print(f"{session_id} {prompt.prompt_id}: set {turn.model}")
        counts.set += 1


def main() -> int:
    args = parse_args()
    folder = args.folder.expanduser()
    if not folder.is_dir():
        print(f"Folder not found: {folder}", file=sys.stderr)
        return 2

    api = MemoryApi(args.base_url, args.timeout)
    counts = Counts()

    for path in sorted(folder.glob("*.jsonl")):
        try:
            process_session(api, path, counts, args.dry_run)
        except ApiError as error:
            print(f"{path.name}: failed: {error}")
            counts.failed += 1

    print(
        f"Set: {counts.set}, already set: {counts.already_set}, "
        f"no model found: {counts.unmatched}, failed: {counts.failed}, "
        f"sessions skipped: {counts.skipped_sessions}"
    )
    return 1 if counts.failed else 0


if __name__ == "__main__":
    sys.exit(main())
