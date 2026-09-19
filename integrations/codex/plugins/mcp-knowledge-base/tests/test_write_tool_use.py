import importlib.util
import json
import sys
import tempfile
import unittest
from pathlib import Path
from unittest import mock


SCRIPT = Path(__file__).parents[1] / "hooks" / "write_tool_use.py"
HOOKS_DIRECTORY = SCRIPT.parent
sys.path.insert(0, str(HOOKS_DIRECTORY))
SPEC = importlib.util.spec_from_file_location("write_tool_use", SCRIPT)
write_tool_use = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(write_tool_use)


SESSION_ID = "019fb72e-e0c3-7452-b32b-5bbf65433c98"
TURN_ID = "019fb72e-e3c3-7093-a89d-050d309ca4ac"


class WriteToolUseTests(unittest.TestCase):
    def test_tool_use_hook_url_uses_knowledge_base_override(self):
        with mock.patch.dict(
            write_tool_use.os.environ,
            {
                "MCP_KNOWLEDGE_BASE_TOOL_USE_HOOK_URL": (
                    "http://knowledge-base/tool-calls"
                )
            },
            clear=True,
        ):
            self.assertEqual(
                "http://knowledge-base/tool-calls",
                write_tool_use._tool_use_hook_url(),
            )

    def test_tool_use_hook_url_uses_knowledge_base_mcp_url(self):
        with mock.patch.dict(
            write_tool_use.os.environ,
            {"MCP_KNOWLEDGE_BASE_URL": "http://knowledge-base/mcp"},
            clear=True,
        ):
            self.assertEqual(
                "http://knowledge-base/api/memory/codex/tool-calls",
                write_tool_use._tool_use_hook_url(),
            )

    def test_post_tool_use_queues_input_and_response(self):
        with tempfile.TemporaryDirectory() as data:
            queue = write_tool_use.MemoryHookQueue(Path(data))
            starts = []
            event = self.event(
                tool_name="Bash",
                tool_input={"command": "dotnet test"},
                tool_response={"output": "Passed", "exit_code": 0},
            )

            output = write_tool_use.process_hook(
                event,
                queue=queue,
                worker_starter=lambda: starts.append(True),
            )

            self.assertIsNone(output)
            self.assertEqual([True], starts)
            queued = list(Path(data).glob("*.json"))
            self.assertEqual(1, len(queued))
            self.assertEqual(event, json.loads(queued[0].read_text(encoding="utf-8")))

    def test_either_worker_routes_a_mixed_queue_and_retries_failed_tool_calls(self):
        import write_memory

        for worker in (write_memory, write_tool_use):
            with self.subTest(worker=worker.__name__), tempfile.TemporaryDirectory() as data:
                queue = write_memory.MemoryHookQueue(Path(data) / "memory-queue")
                events = [
                    self.event(hook_event_name="UserPromptSubmit", prompt="Hello"),
                    self.event(tool_name="Bash", tool_use_id="call-1"),
                    self.event(hook_event_name="Stop", last_assistant_message="Done"),
                ]
                for event in events:
                    queue.enqueue(event)
                prompt_url = "http://knowledge-base/custom-prompts"
                tool_url = "http://knowledge-base/custom-tools"
                sent = []

                def send(request, **kwargs):
                    payload = json.loads(request.data)
                    if payload["hook_event_name"] == "PostToolUse":
                        raise OSError("offline")
                    sent.append((request.full_url, payload))
                    return mock.MagicMock()

                with mock.patch.dict(
                    write_memory.os.environ,
                    {
                        "PLUGIN_DATA": data,
                        "MCP_KNOWLEDGE_BASE_MEMORY_HOOK_URL": prompt_url,
                        "MCP_KNOWLEDGE_BASE_TOOL_USE_HOOK_URL": tool_url,
                    },
                    clear=True,
                ), mock.patch.object(sys, "argv", [str(worker.__file__), "--drain"]):
                    with mock.patch.object(write_memory.urllib.request, "urlopen", side_effect=send):
                        self.assertEqual(0, worker.main())
                    self.assertEqual([(prompt_url, events[0])], sent)
                    self.assertEqual(2, len(list(queue._root.glob("*.json"))))
                    self.assertIn("offline", queue.last_failure())

                    with mock.patch.object(write_memory.urllib.request, "urlopen") as deliver:
                        self.assertEqual(0, worker.main())
                    self.assertEqual(
                        [(tool_url, events[1]), (prompt_url, events[2])],
                        [(call.args[0].full_url, json.loads(call.args[0].data))
                         for call in deliver.call_args_list],
                    )
                    self.assertEqual([], list(queue._root.glob("*.json")))
                    self.assertIsNone(queue.last_failure())

    def test_other_events_are_ignored(self):
        with tempfile.TemporaryDirectory() as data:
            queue = write_tool_use.MemoryHookQueue(Path(data))
            output = write_tool_use.process_hook(
                self.event(hook_event_name="Stop", last_assistant_message="Done"),
                queue=queue,
                worker_starter=lambda: self.fail("worker should not start"),
            )

            self.assertIsNone(output)
            self.assertEqual([], list(Path(data).glob("*.json")))

    @staticmethod
    def event(**values):
        return {
            "hook_event_name": "PostToolUse",
            "session_id": SESSION_ID,
            "turn_id": TURN_ID,
            **values,
        }


if __name__ == "__main__":
    unittest.main()
