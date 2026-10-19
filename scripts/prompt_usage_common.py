"""Shared, standard-library-only support for prompt usage migrations."""
from __future__ import annotations

import argparse
import json
import sys
import time
import uuid
from pathlib import Path
from typing import Any

from claude.set_prompt_models import ApiError, MemoryApi

FIELDS = ("inputTokens", "cachedInputTokens", "cacheWriteInputTokens", "outputTokens", "reasoningOutputTokens")
MAX_COUNT = 2**63 - 1


def count(value: Any) -> int | None:
    return value if type(value) is int and 0 <= value <= MAX_COUNT else None


def guid(value: Any) -> str | None:
    try:
        return str(uuid.UUID(str(value)))
    except ValueError:
        return None


def records(path: Path):
    with path.open(encoding="utf-8-sig") as stream:
        for number, line in enumerate(stream, 1):
            if not line.strip():
                continue
            try:
                record = json.loads(line)
            except json.JSONDecodeError as error:
                raise ValueError(f"Invalid JSON on line {number}; file may still be active") from error
            if isinstance(record, dict):
                yield record


def normalize(provider: str, usage: dict) -> dict:
    if provider == "claude":
        return dict(zip(FIELDS, (
            usage["inputTokens"] + usage["cacheReadInputTokens"] + usage["cacheCreationInputTokens"],
            usage["cacheReadInputTokens"], usage["cacheCreationInputTokens"],
            usage["outputTokens"], usage["thinkingTokens"],
        )))
    return {field: usage[field] for field in FIELDS}


def valid_usage(provider: str, usage: dict) -> bool:
    values = normalize(provider, usage)
    required = [values[field] for field in FIELDS[:4]]
    reasoning = values[FIELDS[4]]
    return (all(count(value) is not None for value in required)
            and values["inputTokens"] + values["outputTokens"] <= MAX_COUNT
            and values["cachedInputTokens"] + values["cacheWriteInputTokens"] <= values["inputTokens"]
            and (reasoning is None or count(reasoning) is not None and reasoning <= values["outputTokens"]))


def stored_usage(conversation: dict) -> dict[str, dict]:
    return {message["promptId"]: {field: message.get(field) for field in FIELDS}
            for message in conversation.get("messages", []) if message.get("promptId")}


def run(provider: str, reader, default_folder: Path) -> int:
    parser = argparse.ArgumentParser(description=f"Set recorded {provider} prompt usage from local JSONL logs. Dry run by default.")
    parser.add_argument("source", nargs="?", type=Path, default=default_folder,
                        help="Session file or folder (searched recursively)")
    parser.add_argument("--base-url", default="http://localhost:5231")
    parser.add_argument("--timeout", type=float, default=30)
    parser.add_argument("--session-id", action="append", default=[])
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--execute", action="store_true", help="Write usage events and verify projected counts")
    mode.add_argument("--dry-run", action="store_true", help="Only read logs and API data (default)")
    args = parser.parse_args()
    source = args.source.expanduser()
    if not source.exists() or args.timeout <= 0:
        parser.error("Source must exist and timeout must be positive")
    requested = {guid(value) for value in args.session_id}
    if None in requested:
        parser.error("Session IDs must be UUIDs")
    paths = [source] if source.is_file() else sorted(source.rglob("*.jsonl"))
    api = MemoryApi(args.base_url, args.timeout)
    totals = dict(set=0, unchanged=0, skipped=0, failed=0, sessions=0)
    seen = set()
    for path in paths:
        try:
            session_id, parsed = reader(path)
            if not session_id or requested and session_id not in requested:
                continue
            if session_id in seen:
                raise ValueError(f"Duplicate session file for {session_id}; select one file explicitly")
            seen.add(session_id)
            memory_id = api.find_memory_id(session_id)
            if not memory_id:
                continue
            totals["sessions"] += 1
            conversation = api.get_conversation(memory_id)
            proposals, skipped = parsed(conversation)
            totals["skipped"] += skipped
            existing = stored_usage(conversation)
            pending = {}
            for prompt_id, usage in proposals.items():
                if not valid_usage(provider, usage):
                    raise ValueError(f"Invalid usage for prompt {prompt_id}")
                expected = normalize(provider, usage)
                if existing.get(prompt_id) == expected:
                    totals["unchanged"] += 1
                    continue
                print(f"{session_id} {prompt_id}: {'set' if args.execute else 'would set'} "
                      f"input={expected['inputTokens']} output={expected['outputTokens']}", flush=True)
                if args.execute:
                    api._send("PUT", f"/api/memory/{provider}/sessions/{session_id}/prompts/{prompt_id}/usage", usage)
                    pending[prompt_id] = expected
                totals["set"] += 1
            if pending:
                deadline = time.monotonic() + args.timeout
                while True:
                    current = stored_usage(api.get_conversation(memory_id))
                    if all(current.get(key) == value for key, value in pending.items()):
                        break
                    if time.monotonic() >= deadline:
                        raise ValueError("Writes accepted but projected counts did not match before timeout")
                    time.sleep(0.5)
                print(f"{session_id}: verified {len(pending)} prompt updates", flush=True)
        except (ApiError, OSError, ValueError, TypeError, KeyError) as error:
            print(f"{path.name}: failed: {error}", file=sys.stderr, flush=True)
            totals["failed"] += 1
            if isinstance(error, ApiError) and error.status in (None, 404, 405):
                break
    if requested - seen:
        print("Requested sessions not found: " + ", ".join(sorted(requested - seen)), file=sys.stderr)
        totals["failed"] += 1
    print(("Executed" if args.execute else "Dry run") + ": " + ", ".join(f"{k}={v}" for k, v in totals.items()))
    return 1 if totals["failed"] else 0
