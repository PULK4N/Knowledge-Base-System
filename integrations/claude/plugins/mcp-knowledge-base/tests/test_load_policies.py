import importlib.util
import tempfile
import unittest
from pathlib import Path
from unittest import mock


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
    def test_mcp_url_uses_knowledge_base_override(self):
        with mock.patch.dict(
            load_policies.os.environ,
            {"MCP_KNOWLEDGE_BASE_URL": "http://knowledge-base/mcp"},
            clear=True,
        ):
            self.assertEqual(
                "http://knowledge-base/mcp", load_policies._mcp_url()
            )

    def test_session_start_overrides_claude_md_with_policies(self):
        with tempfile.TemporaryDirectory() as cwd, tempfile.TemporaryDirectory() as data:
            client = FakeClient({"status": "OK", "policies": "Use focused tests."})
            output = load_policies.process_hook(
                self.event(cwd),
                client_factory=lambda: client,
                data_directory=Path(data),
            )

            claude_md = Path(cwd) / "CLAUDE.md"
            self.assertEqual(
                "Use focused tests.", claude_md.read_text(encoding="utf-8")
            )
            self.assertEqual(cwd, client.requested_repository)
            self.assertTrue(client.closed)
            self.assertEqual(1, len(list(Path(data).glob("policies-*.json"))))

    def test_existing_claude_md_is_replaced_and_announced(self):
        with tempfile.TemporaryDirectory() as cwd, tempfile.TemporaryDirectory() as data:
            claude_md = Path(cwd) / "CLAUDE.md"
            claude_md.write_text("# Handwritten notes", encoding="utf-8")

            output = load_policies.process_hook(
                self.event(cwd),
                client_factory=lambda: FakeClient(
                    {"status": "OK", "policies": "Authoritative policy"}
                ),
                data_directory=Path(data),
            )

            content = claude_md.read_text(encoding="utf-8")
            self.assertEqual("Authoritative policy", content)
            self.assertIn("CLAUDE.md", self.context(output))

    def test_policies_are_not_pushed_into_context(self):
        with tempfile.TemporaryDirectory() as cwd, tempfile.TemporaryDirectory() as data:
            output = load_policies.process_hook(
                self.event(cwd),
                client_factory=lambda: FakeClient(
                    {"status": "OK", "policies": "Secret sauce policy"}
                ),
                data_directory=Path(data),
            )

            self.assertNotIn("Secret sauce policy", self.context(output))

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

    def test_deleted_claude_md_is_restored_from_the_session_cache(self):
        with tempfile.TemporaryDirectory() as cwd, tempfile.TemporaryDirectory() as data:
            event = self.event(cwd)
            load_policies.process_hook(
                event,
                client_factory=lambda: FakeClient(
                    {"status": "OK", "policies": "Cached policy"}
                ),
                data_directory=Path(data),
            )
            claude_md = Path(cwd) / "CLAUDE.md"
            claude_md.unlink()

            output = load_policies.process_hook(
                {**event, "hook_event_name": "UserPromptSubmit"},
                client_factory=lambda: self.fail("MCP should not be called on resume"),
                data_directory=Path(data),
            )

            self.assertIn("Cached policy", claude_md.read_text(encoding="utf-8"))
            self.assertIn("CLAUDE.md", self.context(output))

    def test_unmapped_repository_leaves_claude_md_untouched(self):
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
            self.assertFalse((Path(cwd) / "CLAUDE.md").exists())
            self.assertEqual([], list(Path(data).glob("policies-*.json")))

    def test_session_end_clears_the_session_cache(self):
        with tempfile.TemporaryDirectory() as cwd, tempfile.TemporaryDirectory() as data:
            event = self.event(cwd)
            load_policies.process_hook(
                event,
                client_factory=lambda: FakeClient({"status": "OK", "policies": "Any"}),
                data_directory=Path(data),
            )

            output = load_policies.process_hook(
                {**event, "hook_event_name": "SessionEnd"},
                client_factory=lambda: self.fail("MCP should not be called on end"),
                data_directory=Path(data),
            )

            self.assertIsNone(output)
            self.assertEqual([], list(Path(data).glob("policies-*.json")))

    def test_sse_messages_are_parsed(self):
        messages = load_policies._parse_messages(
            'event: message\ndata: {"jsonrpc":"2.0","id":2,"result":{}}\n\n'
        )

        self.assertEqual(2, messages[0]["id"])

    @staticmethod
    def event(cwd):
        return {
            "hook_event_name": "SessionStart",
            "source": "startup",
            "session_id": "session-1",
            "cwd": cwd,
        }

    @staticmethod
    def context(output):
        return output["hookSpecificOutput"]["additionalContext"]


if __name__ == "__main__":
    unittest.main()
