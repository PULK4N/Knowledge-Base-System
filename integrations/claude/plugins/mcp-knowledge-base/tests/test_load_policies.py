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
    def __init__(self, result, missing_agent_family=False):
        self.result = result
        self.missing_agent_family = missing_agent_family
        self.requested_repository = None
        self.requested_agent_family = None
        self.created_families = []
        self.read_count = 0
        self.closed = False

    def get_policies(self, repository_path, agent_family):
        self.requested_repository = repository_path
        self.requested_agent_family = agent_family
        self.read_count += 1
        if self.missing_agent_family and agent_family not in self.created_families:
            raise load_policies.AgentFamilyMissingError(agent_family)
        return self.result

    def create_agent_family(self, agent_family):
        self.created_families.append(agent_family)

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
            self.assertIn("CLAUDE.md", self.context(output))
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
            first = FakeClient(
                {"status": "OK", "policies": "# General policies\n\nCached policy"}
            )
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

    def test_policy_url_is_derived_from_the_mcp_base_address(self):
        with mock.patch.dict(
            load_policies.os.environ,
            {"MCP_KNOWLEDGE_BASE_URL": "http://knowledge-base:5231/mcp"},
            clear=True,
        ):
            self.assertEqual(
                "http://knowledge-base:5231/api/policies",
                load_policies._policy_url(),
            )

        with mock.patch.dict(
            load_policies.os.environ,
            {"MCP_KNOWLEDGE_BASE_API_URL": "http://elsewhere/"},
            clear=True,
        ):
            self.assertEqual(
                "http://elsewhere/api/policies", load_policies._policy_url()
            )
            self.assertEqual(
                "http://elsewhere/api/policies/agent-families",
                load_policies._agent_family_url(),
            )

    def test_agent_family_defaults_to_claude_and_is_configurable(self):
        with mock.patch.dict(load_policies.os.environ, {}, clear=True):
            self.assertEqual("claude", load_policies._agent_family())

        with mock.patch.dict(
            load_policies.os.environ,
            {"MCP_KNOWLEDGE_BASE_AGENT_FAMILY": "  in-house-agent  "},
            clear=True,
        ):
            self.assertEqual("in-house-agent", load_policies._agent_family())

    def test_agent_family_is_sent_with_the_repository_path(self):
        with tempfile.TemporaryDirectory() as cwd, tempfile.TemporaryDirectory() as data:
            client = FakeClient({"status": "OK", "policies": "Family policy."})
            with mock.patch.dict(
                load_policies.os.environ,
                {"MCP_KNOWLEDGE_BASE_AGENT_FAMILY": "in-house-agent"},
                clear=True,
            ):
                load_policies.process_hook(
                    self.event(cwd),
                    client_factory=lambda: client,
                    data_directory=Path(data),
                )

            self.assertEqual(cwd, client.requested_repository)
            self.assertEqual("in-house-agent", client.requested_agent_family)

    def test_http_errors_stop_the_session(self):
        client = load_policies.PolicyHttpClient(
            "http://localhost:1/api/policies",
            "http://localhost:1/api/policies/agent-families",
        )

        with self.assertRaises(load_policies.PolicyBootstrapError):
            client.get_policies("/workspace/repo", "claude")

    def test_missing_agent_family_is_created_and_the_read_retried(self):
        with tempfile.TemporaryDirectory() as cwd, tempfile.TemporaryDirectory() as data:
            client = FakeClient(
                {"status": "OK", "policies": "# General policies"},
                missing_agent_family=True,
            )
            load_policies.process_hook(
                self.event(cwd),
                client_factory=lambda: client,
                data_directory=Path(data),
            )

            self.assertEqual(["claude"], client.created_families)
            self.assertEqual(2, client.read_count)
            self.assertEqual(
                "# General policies",
                (Path(cwd) / "CLAUDE.md").read_text(encoding="utf-8"),
            )

    def test_a_family_missing_after_creation_stops_the_session(self):
        class NeverCreated(FakeClient):
            def create_agent_family(self, agent_family):
                pass

        with tempfile.TemporaryDirectory() as cwd, tempfile.TemporaryDirectory() as data:
            with self.assertRaises(load_policies.PolicyBootstrapError):
                load_policies.process_hook(
                    self.event(cwd),
                    client_factory=lambda: NeverCreated(
                        {"status": "OK", "policies": "x"},
                        missing_agent_family=True,
                    ),
                    data_directory=Path(data),
                )

    def test_an_existing_policy_document_is_refreshed_silently(self):
        with tempfile.TemporaryDirectory() as cwd, tempfile.TemporaryDirectory() as data:
            claude_md = Path(cwd) / "CLAUDE.md"
            claude_md.write_text(
                "# General policies\n\n## Old\nStale.", encoding="utf-8"
            )

            output = load_policies.process_hook(
                self.event(cwd),
                client_factory=lambda: FakeClient(
                    {"status": "OK", "policies": "# General policies\n\n## New\nFresh."}
                ),
                data_directory=Path(data),
            )

            self.assertIsNone(output)
            self.assertIn("Fresh.", claude_md.read_text(encoding="utf-8"))

    def test_a_policy_document_without_general_policies_is_announced(self):
        with tempfile.TemporaryDirectory() as cwd, tempfile.TemporaryDirectory() as data:
            claude_md = Path(cwd) / "CLAUDE.md"
            claude_md.write_text("# Handwritten notes", encoding="utf-8")

            output = load_policies.process_hook(
                self.event(cwd),
                client_factory=lambda: FakeClient(
                    {"status": "OK", "policies": "# General policies"}
                ),
                data_directory=Path(data),
            )

            self.assertEqual(
                "Policies written to CLAUDE.md.", self.context(output)
            )

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
