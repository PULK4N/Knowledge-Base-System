#!/usr/bin/env python3
"""Migrate Codex stage-one memory outputs to the MemoryModule API.

Examples:
    python3 scripts/migrate_codex_memories.py
    python3 scripts/migrate_codex_memories.py --execute
    python3 scripts/migrate_codex_memories.py \
        --thread-id 019fb72e-e0c3-7452-b32b-5bbf65433c98 \
        --execute
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import sqlite3
import sys
import tempfile
import urllib.error
import urllib.request
import uuid
from dataclasses import dataclass
from pathlib import Path


DEFAULT_DATABASE = Path("~/.codex/memories_1.sqlite").expanduser()
DEFAULT_BASE_URL = "http://localhost:5231"
DEFAULT_ENDPOINT_PATH = "/api/memory/codex/migrations"
DEFAULT_SOURCE = "codex-stage1-output"
DEFAULT_CHECKPOINT = Path(
    "~/.codex/memory-server-migration-checkpoint.json"
).expanduser()


@dataclass(frozen=True)
class StageOutput:
    thread_id: str
    raw_memory: str
    rollout_summary: str
    rollout_slug: str | None

    def payload(self, source: str) -> dict[str, str]:
        return {
            "session_id": str(uuid.UUID(self.thread_id)),
            "raw_memory": self.raw_memory,
            "rollout_summary": self.rollout_summary,
            "source": source,
        }

    def fingerprint(self, source: str) -> str:
        content = json.dumps(
            self.payload(source),
            ensure_ascii=False,
            sort_keys=True,
            separators=(",", ":"),
        ).encode("utf-8")
        return hashlib.sha256(content).hexdigest()


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description=(
            "Read Codex stage1_outputs from SQLite and send them to the "
            "MemoryModule migration endpoint. Without --execute, only the "
            "migration plan is shown."
        )
    )
    parser.add_argument(
        "--database",
        type=Path,
        default=DEFAULT_DATABASE,
        help=f"Codex memories SQLite database (default: {DEFAULT_DATABASE})",
    )
    parser.add_argument(
        "--base-url",
        default=DEFAULT_BASE_URL,
        help=f"Memory server base URL (default: {DEFAULT_BASE_URL})",
    )
    parser.add_argument(
        "--source",
        default=DEFAULT_SOURCE,
        help=f"Value stored in the migration payload source field (default: {DEFAULT_SOURCE})",
    )
    parser.add_argument(
        "--checkpoint",
        type=Path,
        default=DEFAULT_CHECKPOINT,
        help=(
            "Successful migration checkpoint "
            f"(default: {DEFAULT_CHECKPOINT})"
        ),
    )
    parser.add_argument(
        "--thread-id",
        action="append",
        default=[],
        help="Migrate only this thread ID; may be supplied more than once",
    )
    parser.add_argument(
        "--timeout",
        type=float,
        default=30.0,
        help="HTTP timeout in seconds (default: 30)",
    )
    parser.add_argument(
        "--execute",
        action="store_true",
        help="Perform HTTP requests; otherwise only print the plan",
    )
    parser.add_argument(
        "--force",
        action="store_true",
        help="Send rows even when their fingerprint is already checkpointed",
    )
    return parser.parse_args()


def migration_endpoint(base_url: str) -> str:
    return f"{base_url.rstrip('/')}{DEFAULT_ENDPOINT_PATH}"


def load_stage_outputs(
    database: Path,
    requested_thread_ids: list[str],
) -> list[StageOutput]:
    normalized_thread_ids = [
        str(uuid.UUID(thread_id)) for thread_id in requested_thread_ids
    ]
    database_uri = f"{database.expanduser().resolve().as_uri()}?mode=ro"

    with sqlite3.connect(database_uri, uri=True) as connection:
        connection.row_factory = sqlite3.Row
        parameters: list[str] = []
        where_clause = ""

        if normalized_thread_ids:
            placeholders = ", ".join("?" for _ in normalized_thread_ids)
            where_clause = f"WHERE thread_id IN ({placeholders})"
            parameters.extend(normalized_thread_ids)

        rows = connection.execute(
            f"""
            SELECT
                thread_id,
                raw_memory,
                rollout_summary,
                rollout_slug
            FROM stage1_outputs
            {where_clause}
            ORDER BY generated_at, thread_id
            """,
            parameters,
        ).fetchall()

    outputs = [
        StageOutput(
            thread_id=str(uuid.UUID(row["thread_id"])),
            raw_memory=row["raw_memory"],
            rollout_summary=row["rollout_summary"],
            rollout_slug=row["rollout_slug"],
        )
        for row in rows
    ]

    if normalized_thread_ids:
        loaded_thread_ids = {output.thread_id for output in outputs}
        missing_thread_ids = sorted(
            set(normalized_thread_ids) - loaded_thread_ids
        )
        if missing_thread_ids:
            raise ValueError(
                "No stage1_outputs row found for: "
                + ", ".join(missing_thread_ids)
            )

    return outputs


def load_checkpoint(checkpoint: Path) -> dict[str, dict[str, str]]:
    checkpoint = checkpoint.expanduser()
    if not checkpoint.exists():
        return {}

    with checkpoint.open("r", encoding="utf-8") as checkpoint_file:
        value = json.load(checkpoint_file)

    return {
        endpoint: {
            str(uuid.UUID(thread_id)): fingerprint
            for thread_id, fingerprint in thread_fingerprints.items()
        }
        for endpoint, thread_fingerprints in value.items()
    }


def save_checkpoint(
    checkpoint: Path,
    value: dict[str, dict[str, str]],
) -> None:
    checkpoint = checkpoint.expanduser()
    checkpoint.parent.mkdir(parents=True, exist_ok=True)

    temporary_file_descriptor, temporary_file_name = tempfile.mkstemp(
        dir=checkpoint.parent,
        prefix=f".{checkpoint.name}.",
        suffix=".tmp",
        text=True,
    )
    try:
        with os.fdopen(
            temporary_file_descriptor,
            "w",
            encoding="utf-8",
        ) as temporary_file:
            json.dump(value, temporary_file, indent=2, sort_keys=True)
            temporary_file.write("\n")

        os.replace(temporary_file_name, checkpoint)
    except BaseException:
        try:
            os.unlink(temporary_file_name)
        except FileNotFoundError:
            pass
        raise


def post_migration(
    endpoint: str,
    payload: dict[str, str],
    timeout: float,
) -> tuple[int, str]:
    request = urllib.request.Request(
        endpoint,
        data=json.dumps(payload, ensure_ascii=False).encode("utf-8"),
        headers={"Content-Type": "application/json"},
        method="POST",
    )

    try:
        with urllib.request.urlopen(request, timeout=timeout) as response:
            return response.status, response.read().decode("utf-8")
    except urllib.error.HTTPError as error:
        response_body = error.read().decode("utf-8", errors="replace")
        raise RuntimeError(
            f"HTTP {error.code} from {endpoint}: {response_body}"
        ) from error


def main() -> int:
    args = parse_args()
    endpoint = migration_endpoint(args.base_url)

    try:
        outputs = load_stage_outputs(args.database, args.thread_id)
        checkpoint = load_checkpoint(args.checkpoint)
        endpoint_checkpoint = checkpoint.setdefault(endpoint, {})
        pending_outputs = [
            output
            for output in outputs
            if args.force
            or endpoint_checkpoint.get(output.thread_id)
            != output.fingerprint(args.source)
        ]

        print(f"Source database: {args.database.expanduser()}")
        print(f"Migration endpoint: {endpoint}")
        print(f"Stage outputs found: {len(outputs)}")
        print(f"Already checkpointed and unchanged: {len(outputs) - len(pending_outputs)}")
        print(f"Pending migrations: {len(pending_outputs)}")

        for index, output in enumerate(pending_outputs, start=1):
            slug = output.rollout_slug or "no-rollout-slug"
            print(
                f"[{index}/{len(pending_outputs)}] "
                f"{output.thread_id} ({slug})"
            )

            if not args.execute:
                continue

            status, response_body = post_migration(
                endpoint,
                output.payload(args.source),
                args.timeout,
            )
            print(f"  HTTP {status}: {response_body or '<empty response>'}")

            endpoint_checkpoint[output.thread_id] = output.fingerprint(
                args.source
            )
            save_checkpoint(args.checkpoint, checkpoint)

        if not args.execute and pending_outputs:
            print("Dry run only. Add --execute to perform the migration.")
        elif args.execute:
            print("Migration completed.")

        return 0
    except (OSError, sqlite3.Error, ValueError, RuntimeError) as error:
        print(f"Migration failed: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
