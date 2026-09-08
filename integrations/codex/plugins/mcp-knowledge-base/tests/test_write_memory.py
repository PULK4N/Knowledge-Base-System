import importlib.util
import json
import tempfile
import unittest
from pathlib import Path
from unittest import mock


SCRIPT = Path(__file__).parents[1] / "hooks" / "write_memory.py"
SPEC = importlib.util.spec_from_file_location("write_memory", SCRIPT)
write_memory = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(write_memory)


SESSION_ID = "019fb72e-e0c3-7452-b32b-5bbf65433c98"
TURN_ID = "019fb72e-e3c3-7093-a89d-050d309ca4ac"


class FakeClient:
    def __init__(self):
        self.payloads = []

    def record(self, payload):
        self.payloads.append(payload)


class FailingClient:
    def record(self, payload):
        raise write_memory.MemoryHookError("API offline")


class WriteMemoryTests(unittest.TestCase):
    def test_delivery_filters_invalid_unicode_including_existing_queue_records(self):
        cases = [
            ("before\udc9dafter", "beforeafter"),
            ("\ud800hello\udfff", "hello"),
            ("\ud800\ud83d\ude00\udc9d", "\U0001f600"),
            ("\U0001f600 caf\u00e9 \u4f60\u597d", "\U0001f600 caf\u00e9 \u4f60\u597d"),
            (r"literal \uDC9D", r"literal \uDC9D"),
        ]
        for original, expected in cases:
            with self.subTest(original=repr(original)), tempfile.TemporaryDirectory() as data:
                queue = write_memory.MemoryHookQueue(Path(data))
                event = self.event(
                    "Stop", nested=[{"text\udc9d": original}, None, True, 42]
                )
                queue.enqueue(event)
                with mock.patch.object(write_memory.urllib.request, "urlopen") as send:
                    queue.drain(write_memory.MemoryApiClient("http://localhost/memory"))

                request = send.call_args.args[0]
                sent = json.loads(request.data)
                self.assertEqual(
                    [{"text": expected}, None, True, 42], sent["nested"]
                )
                self.assertEqual(SESSION_ID, sent["session_id"])
                self.assertEqual([], list(Path(data).glob("*.json")))

    def test_filter_does_not_silently_overwrite_colliding_keys(self):
        with mock.patch.object(write_memory.urllib.request, "urlopen") as send:
            with self.assertRaisesRegex(write_memory.MemoryHookError, "duplicate"):
                write_memory.MemoryApiClient("http://localhost/memory").record(
                    {"text": "first", "text\udc9d": "second"}
                )
            send.assert_not_called()

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

    def test_memory_hook_url_uses_knowledge_base_mcp_url(self):
        with mock.patch.dict(
            write_memory.os.environ,
            {"MCP_KNOWLEDGE_BASE_URL": "http://knowledge-base/mcp"},
            clear=True,
        ):
            self.assertEqual(
                "http://knowledge-base/api/memory/codex/prompt-hooks",
                write_memory._memory_hook_url(),
            )

    def test_user_prompt_is_queued_and_worker_is_started(self):
        with tempfile.TemporaryDirectory() as data:
            queue = write_memory.MemoryHookQueue(Path(data))
            starts = []
            event = self.event("UserPromptSubmit", prompt="Remember this")

            output = write_memory.process_hook(
                event,
                queue=queue,
                worker_starter=lambda: starts.append(True),
            )

            self.assertIsNone(output)
            self.assertEqual([True], starts)
            queued = list(Path(data).glob("*.json"))
            self.assertEqual(1, len(queued))
            self.assertIn("Remember this", queued[0].read_text(encoding="utf-8"))

    def test_stop_payload_is_forwarded_by_queue_drain(self):
        with tempfile.TemporaryDirectory() as data:
            queue = write_memory.MemoryHookQueue(Path(data))
            event = self.event("Stop", last_assistant_message="Implemented it.")
            queue.enqueue(event)
            client = FakeClient()

            queue.drain(client)

            self.assertEqual([event], client.payloads)
            self.assertEqual([], list(Path(data).glob("*.json")))

    def test_failed_delivery_remains_queued_for_a_later_retry(self):
        with tempfile.TemporaryDirectory() as data:
            queue = write_memory.MemoryHookQueue(Path(data))
            event = self.event("UserPromptSubmit", prompt="Keep me")
            queue.enqueue(event)

            write_memory.drain_queue(queue=queue, client=FailingClient())

            self.assertEqual(1, len(list(Path(data).glob("*.json"))))
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

    def test_tool_payloads_are_not_recorded(self):
        with tempfile.TemporaryDirectory() as data:
            queue = write_memory.MemoryHookQueue(Path(data))
            output = write_memory.process_hook(
                self.event("PostToolUse", tool_response="large output"),
                queue=queue,
                worker_starter=lambda: self.fail("worker should not start"),
            )

            self.assertIsNone(output)
            self.assertEqual([], list(Path(data).glob("*.json")))

    @staticmethod
    def event(event_name, **values):
        return {
            "hook_event_name": event_name,
            "session_id": SESSION_ID,
            "turn_id": TURN_ID,
            **values,
        }


if __name__ == "__main__":
    unittest.main()
