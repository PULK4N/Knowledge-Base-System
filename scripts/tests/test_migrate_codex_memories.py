from __future__ import annotations

import importlib.util
import json
import sqlite3
import sys
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch


SCRIPT_PATH = (
    Path(__file__).parents[1] / "migrate_codex_memories.py"
)
SPEC = importlib.util.spec_from_file_location(
    "migrate_codex_memories",
    SCRIPT_PATH,
)
assert SPEC is not None
assert SPEC.loader is not None
migration = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = migration
SPEC.loader.exec_module(migration)


class MigrateCodexMemoriesTests(unittest.TestCase):
    def test_load_stage_outputs_and_build_payload(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            database = Path(temporary_directory) / "memories.sqlite"
            self._create_database(database)

            outputs = migration.load_stage_outputs(database, [])

        self.assertEqual(1, len(outputs))
        self.assertEqual(
            {
                "session_id": "019fb72e-e0c3-7452-b32b-5bbf65433c98",
                "raw_memory": "Raw memory",
                "rollout_summary": "Rollout summary",
                "source": "codex-stage1-output",
            },
            outputs[0].payload("codex-stage1-output"),
        )

    def test_checkpoint_round_trip_is_scoped_by_endpoint(self) -> None:
        value = {
            "http://localhost:5231/api/memory/codex/migrations": {
                "019fb72e-e0c3-7452-b32b-5bbf65433c98": "fingerprint"
            }
        }

        with tempfile.TemporaryDirectory() as temporary_directory:
            checkpoint = Path(temporary_directory) / "checkpoint.json"
            migration.save_checkpoint(checkpoint, value)

            loaded = migration.load_checkpoint(checkpoint)

        self.assertEqual(value, loaded)

    def test_post_migration_sends_expected_json(self) -> None:
        payload = {
            "session_id": "019fb72e-e0c3-7452-b32b-5bbf65433c98",
            "raw_memory": "Raw memory",
            "rollout_summary": "Rollout summary",
            "source": "codex-stage1-output",
        }
        response = _Response()

        with patch.object(
            migration.urllib.request,
            "urlopen",
            return_value=response,
        ) as urlopen:
            status, body = migration.post_migration(
                "http://localhost:5231/api/memory/codex/migrations",
                payload,
                30,
            )

        request = urlopen.call_args.args[0]
        self.assertEqual(200, status)
        self.assertEqual('{"status":"OK"}', body)
        self.assertEqual("POST", request.method)
        self.assertEqual(
            payload,
            json.loads(request.data.decode("utf-8")),
        )
        self.assertEqual(
            "application/json",
            request.headers["Content-type"],
        )

    @staticmethod
    def _create_database(database: Path) -> None:
        with sqlite3.connect(database) as connection:
            connection.execute(
                """
                CREATE TABLE stage1_outputs (
                    thread_id TEXT PRIMARY KEY,
                    source_updated_at INTEGER NOT NULL,
                    raw_memory TEXT NOT NULL,
                    rollout_summary TEXT NOT NULL,
                    rollout_slug TEXT,
                    generated_at INTEGER NOT NULL,
                    usage_count INTEGER,
                    last_usage INTEGER,
                    selected_for_phase2 INTEGER NOT NULL DEFAULT 0,
                    selected_for_phase2_source_updated_at INTEGER
                )
                """
            )
            connection.execute(
                """
                INSERT INTO stage1_outputs (
                    thread_id,
                    source_updated_at,
                    raw_memory,
                    rollout_summary,
                    rollout_slug,
                    generated_at
                ) VALUES (?, ?, ?, ?, ?, ?)
                """,
                (
                    "019fb72e-e0c3-7452-b32b-5bbf65433c98",
                    1,
                    "Raw memory",
                    "Rollout summary",
                    "hook-mcp-event-capture-test",
                    2,
                ),
            )


class _Response:
    status = 200

    def __enter__(self) -> "_Response":
        return self

    def __exit__(self, *args: object) -> None:
        return None

    @staticmethod
    def read() -> bytes:
        return b'{"status":"OK"}'


if __name__ == "__main__":
    unittest.main()
