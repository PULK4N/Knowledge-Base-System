import importlib.util
import tempfile
import unittest
from pathlib import Path


SCRIPT = Path(__file__).parents[1] / "hooks" / "load_policies.py"
SPEC = importlib.util.spec_from_file_location("load_policies", SCRIPT)
load_policies = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(load_policies)


class FakeClient:
    def __init__(self, result):
        self.result = result
        self.requested_repository = None
        self.closed = False

    def get_policies(self, repository_path):
        self.requested_repository = repository_path
        return self.result

    def close(self):
        self.closed = True


class LoadPoliciesTests(unittest.TestCase):
    def test_first_prompt_loads_and_caches_policies(self):
        with tempfile.TemporaryDirectory() as cwd, tempfile.TemporaryDirectory() as data:
            client = FakeClient({"status": "OK", "policies": "Use focused tests."})
            output = load_policies.process_hook(
                self.event(cwd),
                client_factory=lambda: client,
                data_directory=Path(data),
            )

            self.assertIn("Use focused tests.", self.context(output))
            self.assertEqual(cwd, client.requested_repository)
            self.assertTrue(client.closed)
            self.assertEqual(1, len(list(Path(data).glob("policies-*.json"))))

    def test_later_prompt_uses_session_cache_without_another_request(self):
        with tempfile.TemporaryDirectory() as cwd, tempfile.TemporaryDirectory() as data:
            first = FakeClient({"status": "OK", "policies": "Cached policy"})
            event = self.event(cwd)
            load_policies.process_hook(
                event,
                client_factory=lambda: first,
                data_directory=Path(data),
            )

            output = load_policies.process_hook(
                event,
                client_factory=lambda: self.fail("MCP should not be called twice"),
                data_directory=Path(data),
            )

            self.assertIsNone(output)

            resumed_event = {**event, "hook_event_name": "SessionStart"}
            restored = load_policies.process_hook(
                resumed_event,
                client_factory=lambda: self.fail("MCP should not be called on resume"),
                data_directory=Path(data),
            )
            self.assertIn("Cached policy", self.context(restored))

    def test_unmapped_repository_is_not_cached_and_requires_user_choice(self):
        with tempfile.TemporaryDirectory() as cwd, tempfile.TemporaryDirectory() as data:
            client = FakeClient(
                {
                    "status": "RepositoryMappingRequired",
                    "message": "Stop and ask the user.",
                    "projects": [{"projectName": "Existing"}],
                }
            )
            output = load_policies.process_hook(
                self.event(cwd),
                client_factory=lambda: client,
                data_directory=Path(data),
            )

            context = self.context(output)
            self.assertIn("Stop repository reasoning", context)
            self.assertIn("Existing", context)
            self.assertEqual([], list(Path(data).glob("policies-*.json")))

    def test_sse_messages_are_parsed(self):
        messages = load_policies._parse_messages(
            'event: message\ndata: {"jsonrpc":"2.0","id":2,"result":{}}\n\n'
        )

        self.assertEqual(2, messages[0]["id"])

    @staticmethod
    def event(cwd):
        return {
            "hook_event_name": "UserPromptSubmit",
            "session_id": "session-1",
            "cwd": cwd,
        }

    @staticmethod
    def context(output):
        return output["hookSpecificOutput"]["additionalContext"]


if __name__ == "__main__":
    unittest.main()
