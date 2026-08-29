import importlib.util
import tempfile
import unittest
import uuid
from pathlib import Path
from unittest import mock


SCRIPT = Path(__file__).parents[1] / "hooks" / "write_memory.py"
SPEC = importlib.util.spec_from_file_location("write_memory", SCRIPT)
write_memory = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(write_memory)


SESSION_ID = "019fb72e-e0c3-7452-b32b-5bbf65433c98"
OTHER_SESSION_ID = "019fb72e-e5c3-7452-b32b-5bbf65433c98"


class FakeClient:
    def __init__(self):
        self.payloads = []

    def record(self, payload):
        self.payloads.append(payload)


class FailingClient:
    def record(self, payload):
        raise write_memory.MemoryHookError("API offline")


class WriteMemoryTests(unittest.TestCase):
    def test_memory_hook_url_uses_knowledge_base_override(self):
        with mock.patch.dict(
            write_memory.os.environ,
            {
                "MCP_KNOWLEDGE_BASE_MEMORY_HOOK_URL": (
                    "http://knowledge-base/memory-hooks"
                )
            },
            clear=True,
        ):
            self.assertEqual(
                "http://knowledge-base/memory-hooks",
                write_memory._memory_hook_url(),
            )

    def test_memory_hook_url_uses_the_claude_endpoint(self):
        with mock.patch.dict(
            write_memory.os.environ,
            {"MCP_KNOWLEDGE_BASE_URL": "http://knowledge-base/mcp"},
            clear=True,
        ):
            self.assertEqual(
                "http://knowledge-base/api/memory/claude/prompt-hooks",
                write_memory._memory_hook_url(),
            )

    def test_user_prompt_is_queued_with_a_generated_turn_id(self):
        with tempfile.TemporaryDirectory() as data:
            queue, turns = self.storage(data)
            starts = []

            output = write_memory.process_hook(
                self.event("UserPromptSubmit", prompt="Remember this"),
                queue=queue,
                turns=turns,
                worker_starter=lambda: starts.append(True),
            )

            self.assertIsNone(output)
            self.assertEqual([True], starts)
            payload = self.queued_payload(data)
            self.assertEqual("Remember this", payload["prompt"])
            self.assertEqual(SESSION_ID, payload["session_id"])
            uuid.UUID(payload["turn_id"])

    def test_stop_reuses_the_turn_id_of_the_preceding_prompt(self):
        with tempfile.TemporaryDirectory() as data:
            queue, turns = self.storage(data)

            for event_name in ("UserPromptSubmit", "Stop"):
                write_memory.process_hook(
                    self.event(event_name),
                    queue=queue,
                    turns=turns,
                    worker_starter=lambda: None,
                )

            prompt_payload, stop_payload = self.queued_payloads(data)
            self.assertEqual(
                prompt_payload["turn_id"], stop_payload["turn_id"]
            )

    def test_each_prompt_starts_a_new_turn(self):
        with tempfile.TemporaryDirectory() as data:
            queue, turns = self.storage(data)

            for _ in range(2):
                write_memory.process_hook(
                    self.event("UserPromptSubmit"),
                    queue=queue,
                    turns=turns,
                    worker_starter=lambda: None,
                )

            first, second = self.queued_payloads(data)
            self.assertNotEqual(first["turn_id"], second["turn_id"])

    def test_turns_are_tracked_per_session(self):
        with tempfile.TemporaryDirectory() as data:
            _, turns = self.storage(data)

            first = turns.start_turn(SESSION_ID)
            other = turns.start_turn(OTHER_SESSION_ID)

            self.assertNotEqual(first, other)
            self.assertEqual(first, turns.current_turn(SESSION_ID))
            self.assertEqual(other, turns.current_turn(OTHER_SESSION_ID))

    def test_stop_without_a_prompt_still_records_a_turn(self):
        with tempfile.TemporaryDirectory() as data:
            queue, turns = self.storage(data)

            write_memory.process_hook(
                self.event("Stop"),
                queue=queue,
                turns=turns,
                worker_starter=lambda: None,
            )

            uuid.UUID(self.queued_payload(data)["turn_id"])

    def test_session_end_clears_the_tracked_turn(self):
        with tempfile.TemporaryDirectory() as data:
            queue, turns = self.storage(data)
            turns.start_turn(SESSION_ID)

            write_memory.process_hook(
                self.event("SessionEnd", reason="clear"),
                queue=queue,
                turns=turns,
                worker_starter=lambda: None,
            )

            self.assertEqual(
                [], list((Path(data) / "turns").glob("*.turn"))
            )

    def test_queued_payload_is_forwarded_by_queue_drain(self):
        with tempfile.TemporaryDirectory() as data:
            queue, _ = self.storage(data)
            event = self.event("Stop")
            queue.enqueue(event)
            client = FakeClient()

            queue.drain(client)

            self.assertEqual([event], client.payloads)
            self.assertEqual([], list((Path(data) / "queue").glob("*.json")))

    def test_failed_delivery_remains_queued_for_a_later_retry(self):
        with tempfile.TemporaryDirectory() as data:
            queue, _ = self.storage(data)
            event = self.event("UserPromptSubmit", prompt="Keep me")
            queue.enqueue(event)

            write_memory.drain_queue(queue=queue, client=FailingClient())

            self.assertEqual(
                1, len(list((Path(data) / "queue").glob("*.json")))
            )
            self.assertEqual("API offline", queue.last_failure())

            client = FakeClient()
            queue.drain(client)
            self.assertEqual([event], client.payloads)
            self.assertIsNone(queue.last_failure())

    def test_compaction_requests_a_summary_for_the_current_thread(self):
        output = write_memory.process_hook(
            {
                "hook_event_name": "SessionStart",
                "source": "compact",
                "session_id": SESSION_ID,
            }
        )

        context = output["hookSpecificOutput"]["additionalContext"]
        self.assertIn("memory_summary_add", context)
        self.assertIn(SESSION_ID, context)
        self.assertIn("two-to-four paragraph", context)

    def test_session_start_without_compaction_records_nothing(self):
        with tempfile.TemporaryDirectory() as data:
            queue, turns = self.storage(data)

            output = write_memory.process_hook(
                {
                    "hook_event_name": "SessionStart",
                    "source": "startup",
                    "session_id": SESSION_ID,
                },
                queue=queue,
                turns=turns,
                worker_starter=lambda: self.fail("worker should not start"),
            )

            self.assertIsNone(output)
            self.assertEqual([], list((Path(data) / "queue").glob("*.json")))

    def test_tool_payloads_are_not_recorded(self):
        with tempfile.TemporaryDirectory() as data:
            queue, turns = self.storage(data)

            output = write_memory.process_hook(
                self.event("PostToolUse", tool_response="large output"),
                queue=queue,
                turns=turns,
                worker_starter=lambda: self.fail("worker should not start"),
            )

            self.assertIsNone(output)
            self.assertEqual([], list((Path(data) / "queue").glob("*.json")))

    def test_a_session_without_a_guid_is_reported_not_recorded(self):
        with tempfile.TemporaryDirectory() as data:
            queue, turns = self.storage(data)

            with self.assertRaises(write_memory.MemoryHookError):
                write_memory.process_hook(
                    {
                        "hook_event_name": "UserPromptSubmit",
                        "session_id": "not-a-guid",
                    },
                    queue=queue,
                    turns=turns,
                    worker_starter=lambda: None,
                )

            self.assertEqual([], list((Path(data) / "queue").glob("*.json")))

    @staticmethod
    def storage(data):
        return (
            write_memory.MemoryHookQueue(Path(data) / "queue"),
            write_memory.TurnRegistry(Path(data) / "turns"),
        )

    @classmethod
    def queued_payload(cls, data):
        return cls.queued_payloads(data)[0]

    @staticmethod
    def queued_payloads(data):
        return [
            write_memory.json.loads(path.read_text(encoding="utf-8"))
            for path in sorted((Path(data) / "queue").glob("*.json"))
        ]

    @staticmethod
    def event(event_name, **values):
        return {
            "hook_event_name": event_name,
            "session_id": SESSION_ID,
            "cwd": "/home/nikola/Documents/github/Knowledge-base-system",
            **values,
        }


if __name__ == "__main__":
    unittest.main()
