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
        self.requested_agent_family = None
        self.closed = False

    def get_policies(self, repository_path, agent_family):
        self.requested_repository = repository_path
        self.requested_agent_family = agent_family
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
            {"MCP_KNOWLEDGE_BASE_API_URL": "http://elsewhere/policies/"},
            clear=True,
        ):
            self.assertEqual(
                "http://elsewhere/policies", load_policies._policy_url()
            )

    def test_agent_family_defaults_to_codex_and_is_configurable(self):
        with mock.patch.dict(load_policies.os.environ, {}, clear=True):
            self.assertEqual("codex", load_policies._agent_family())

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
        client = load_policies.PolicyHttpClient("http://localhost:1/api/policies")

        with self.assertRaises(load_policies.PolicyBootstrapError):
            client.get_policies("/workspace/repo", "codex")

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
