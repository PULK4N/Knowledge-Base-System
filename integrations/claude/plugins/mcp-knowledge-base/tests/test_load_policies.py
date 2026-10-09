import importlib.util
import io
import json
import os
import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path
from unittest import mock


SCRIPT = Path(__file__).parents[1] / "hooks" / "load_policies.py"
HOOKS_CONFIG = Path(__file__).parents[1] / "hooks" / "hooks.json"
SPEC = importlib.util.spec_from_file_location("load_policies", SCRIPT)
load_policies = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(load_policies)


class FakeClient:
    def __init__(self, result, missing_agent_family=False, repository_paths=None):
        self.result = result
        self.repository_paths = repository_paths
        self.project_read_count = 0
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

    def get_project(self, repository_path):
        self.project_read_count += 1
        return {"repositoryPaths": self.repository_paths if self.repository_paths is not None else [repository_path]}

    def create_agent_family(self, agent_family):
        self.created_families.append(agent_family)

    def close(self):
        self.closed = True


class LoadPoliciesTests(unittest.TestCase):
    def test_project_request_encodes_windows_and_linux_paths(self):
        client = load_policies.PolicyHttpClient(
            "https://kb.example/api/policies", "https://kb.example/api/policies/agent-families"
        )
        for path in ("/home/user/a repo+#", "C:\\Users\\Name\\a repo+#", "\\\\server\\share\\repo"):
            with self.subTest(path=path):
                project = {"projectId": "project-1", "repositoryPaths": [path]}
                response = mock.MagicMock()
                response.__enter__.return_value.read.return_value = json.dumps(project).encode("utf-8")
                with mock.patch.object(load_policies.urllib.request, "urlopen", return_value=response) as send:
                    self.assertEqual(project, client.get_project(path))
                request = send.call_args.args[0]
                url = load_policies.urllib.parse.urlsplit(request.full_url)
                self.assertEqual("/api/policies/projects/by-repository", url.path)
                self.assertEqual({"repositoryPath": [path]}, load_policies.urllib.parse.parse_qs(url.query))

    def test_project_failure_preserves_files_and_session_baseline(self):
        with tempfile.TemporaryDirectory() as root:
            client = FakeClient({"status": "OK", "policies": "# General policies\n## Rule\nOld"})
            data = Path(root) / "cache"
            event = self.event(root)
            load_policies.process_hook(event, client_factory=lambda: client, data_directory=data)
            client.result["policies"] = "# General policies\n## Rule\nNew"
            with mock.patch.object(client, "get_project", side_effect=load_policies.PolicyBootstrapError("Project unavailable")):
                with self.assertRaisesRegex(load_policies.PolicyBootstrapError, "Project unavailable"):
                    load_policies.process_hook(event, client_factory=lambda: client, data_directory=data)
            self.assertIn("Old", (Path(root) / "CLAUDE.md").read_text(encoding="utf-8"))
            output = load_policies.process_hook(event, client_factory=lambda: client, data_directory=data)
            self.assertIn("POLICY CHANGE:", self.context(output))
            self.assertIn("New", self.context(output))

    def test_invalid_project_does_not_write_policies(self):
        for paths in (None, "wrong", [1], ["/another/repository"]):
            with self.subTest(paths=paths), tempfile.TemporaryDirectory() as root:
                client = FakeClient({"status": "OK", "policies": "Policy"})
                with mock.patch.object(client, "get_project", return_value={"repositoryPaths": paths}):
                    with self.assertRaises(load_policies.PolicyBootstrapError):
                        load_policies.process_hook(self.event(root), client_factory=lambda: client, data_directory=Path(root) / "cache")
                self.assertFalse((Path(root) / "CLAUDE.md").exists())

    def test_mapping_required_does_not_fetch_project(self):
        with tempfile.TemporaryDirectory() as root:
            client = FakeClient({"status": "RepositoryMappingRequired", "projects": []})
            load_policies.process_hook(self.event(root), client_factory=lambda: client, data_directory=Path(root) / "cache")
            self.assertEqual(0, client.project_read_count)

    def test_syncs_all_project_repositories_on_every_fetch(self):
        with tempfile.TemporaryDirectory() as root:
            first, second, unrelated, data = [
                Path(root) / name for name in ("first", "second", "unrelated", "data")
            ]
            for directory in (first, second, unrelated):
                directory.mkdir()
            client = FakeClient({
                "status": "OK", "policies": "# General policies\n## Tests\nCurrent.",
            }, repository_paths=[str(first), str(second), str(second)])
            event = self.event(str(first))
            for attempt in range(2):
                with self.subTest(attempt=attempt):
                    (second / "CLAUDE.md").write_text("Stale", encoding="utf-8")
                    with mock.patch.object(load_policies, "_write_policy_file", wraps=load_policies._write_policy_file) as writer:
                        load_policies.process_hook(event, client_factory=lambda: client, data_directory=data)
                    self.assertEqual(2, writer.call_count)
                    for directory in (first, second):
                        self.assertEqual(client.result["policies"], (directory / "CLAUDE.md").read_text(encoding="utf-8"))
                    self.assertFalse((unrelated / "CLAUDE.md").exists())
            self.assertEqual(2, client.read_count)
            self.assertEqual(2, client.project_read_count)

    def test_unavailable_and_relative_paths_are_reported_without_creating_directories(self):
        with tempfile.TemporaryDirectory() as root:
            missing = Path(root) / "missing"
            client = FakeClient({
                "status": "OK", "policies": "# General policies\n## Tests\nCurrent.",
            }, repository_paths=[root, str(missing), "relative/repository"])
            with mock.patch.object(load_policies.sys, "stderr", new_callable=io.StringIO) as stderr:
                load_policies.process_hook(self.event(root), client_factory=lambda: client, data_directory=Path(root) / "data")
            self.assertIn(str(missing), stderr.getvalue())
            self.assertIn("relative/repository", stderr.getvalue())
            self.assertFalse(missing.exists())
            self.assertTrue((Path(root) / "CLAUDE.md").exists())

    def test_secondary_write_failure_attempts_remaining_repositories_and_keeps_baseline(self):
        with tempfile.TemporaryDirectory() as root:
            directories = [Path(root) / name for name in ("first", "second", "third")]
            for directory in directories:
                directory.mkdir()
            client = FakeClient({
                "status": "OK", "policies": "# General policies\n## Tests\nOld.",
            }, repository_paths=[str(path) for path in directories])
            event = self.event(str(directories[0]))
            data = Path(root) / "data"
            load_policies.process_hook(event, client_factory=lambda: client, data_directory=data)
            client.result = {**client.result, "policies": "# General policies\n## Tests\nNew."}
            original_writer = load_policies._write_policy_file

            def write(path, document):
                if path == str(directories[1]):
                    raise load_policies.PolicyBootstrapError("Permission denied")
                return original_writer(path, document)

            with mock.patch.object(load_policies, "_write_policy_file", side_effect=write):
                with self.assertRaisesRegex(load_policies.PolicyBootstrapError, "Permission denied"):
                    load_policies.process_hook(event, client_factory=lambda: client, data_directory=data)
            self.assertIn("New.", (directories[2] / "CLAUDE.md").read_text(encoding="utf-8"))
            output = load_policies.process_hook(event, client_factory=lambda: client, data_directory=data)
            self.assertIn("POLICY CHANGE:", self.context(output))
            self.assertIn("New.", (directories[1] / "CLAUDE.md").read_text(encoding="utf-8"))

    def test_policy_loader_runs_for_every_session_start_source(self):
        hooks = json.loads(HOOKS_CONFIG.read_text(encoding="utf-8"))["hooks"]
        policy_loader = next(
            group
            for group in hooks["SessionStart"]
            if any(
                "load_policies.py" in hook["command"]
                for hook in group["hooks"]
            )
        )

        self.assertEqual(
            "startup|resume|clear|compact|fork", policy_loader["matcher"]
        )

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

    def test_later_prompt_fetches_api_even_when_policies_are_unchanged(self):
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
                {**event, "hook_event_name": "UserPromptSubmit"},
                client_factory=lambda: first,
                data_directory=Path(data),
            )

            self.assertIsNone(output)
            self.assertEqual(2, first.read_count)

    def test_deleted_claude_md_is_restored_from_the_api(self):
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
                client_factory=lambda: FakeClient(
                    {"status": "OK", "policies": "Cached policy"}
                ),
                data_directory=Path(data),
            )

            self.assertIn("Cached policy", claude_md.read_text(encoding="utf-8"))
            self.assertIn("CLAUDE.md", self.context(output))

    def test_each_session_receives_changes_even_if_another_session_updated_file(self):
        with tempfile.TemporaryDirectory() as cwd, tempfile.TemporaryDirectory() as data:
            original = "# General policies\n\n## Tests\nOld.\n\n## Style\nKeep."
            updated = original.replace("Old.", "New.")
            client = FakeClient({"status": "OK", "policies": original})
            first = self.event(cwd)
            second = {**first, "session_id": "session-2"}
            for event in (first, second):
                load_policies.process_hook(
                    event, client_factory=lambda: client, data_directory=Path(data)
                )
            client.result = {"status": "OK", "policies": updated}
            for event in (second, first):
                with self.subTest(session=event["session_id"]):
                    output = load_policies.process_hook(
                        {**event, "hook_event_name": "UserPromptSubmit"},
                        client_factory=lambda: client,
                        data_directory=Path(data),
                    )
                    self.assertEqual(
                        "POLICY CHANGE:\n# General policies\n\n## Tests\nNew.",
                        self.context(output),
                    )
                    self.assertIsNone(load_policies.process_hook(
                        event, client_factory=lambda: client, data_directory=Path(data)
                    ))

    def test_policy_diffs_preserve_scope_and_report_additions_and_removals(self):
        previous = (
            "# General policies\n\n## Same title\nKeep.\n\n"
            "# Project policies\n\n## Same title\nOld.\n\n## Removed\nObsolete."
        )
        current = (
            "# General policies\n\n## Same title\nKeep.\n\n"
            "# Project policies\n\n## Same title\nNew.\n\n## Added\nFresh."
        )
        changes = load_policies._policy_changes(previous, current)
        self.assertNotIn("General policies", changes)
        self.assertNotIn("Keep.", changes)
        self.assertIn("# Project policies\n\n## Same title\nNew.", changes)
        self.assertIn("## Added\nFresh.", changes)
        self.assertIn("Removed policy (no longer applies):\n# Project policies\n## Removed", changes)

    def test_file_edits_do_not_trigger_policy_changes(self):
        with tempfile.TemporaryDirectory() as cwd, tempfile.TemporaryDirectory() as data:
            event = self.event(cwd)
            policies = "# General policies\n## Tests\nKeep."
            client = FakeClient({"status": "OK", "policies": policies})
            load_policies.process_hook(event, client_factory=lambda: client, data_directory=Path(data))
            claude_md = Path(cwd) / "CLAUDE.md"
            claude_md.write_text("# General policies\n## Other session\nDifferent.", encoding="utf-8")
            output = load_policies.process_hook(event, client_factory=lambda: client, data_directory=Path(data))
            self.assertIsNone(output)
            self.assertEqual(policies, claude_md.read_text(encoding="utf-8"))

    def test_api_failure_is_not_hidden_by_cached_policies(self):
        with tempfile.TemporaryDirectory() as cwd, tempfile.TemporaryDirectory() as data:
            event = self.event(cwd)
            client = FakeClient({"status": "OK", "policies": "# General policies\n## Tests\nKeep."})
            load_policies.process_hook(event, client_factory=lambda: client, data_directory=Path(data))
            with mock.patch.object(client, "get_policies", side_effect=load_policies.PolicyBootstrapError("API unavailable")):
                with self.assertRaises(load_policies.PolicyBootstrapError):
                    load_policies.process_hook(event, client_factory=lambda: client, data_directory=Path(data))

    def test_policy_examples_and_subheadings_remain_part_of_changed_policy(self):
        previous = "# General policies\n## Tests\n### Example\n```python\n# Comment\nold()\n```"
        current = previous.replace("old()", "new()")
        self.assertEqual(current.replace("policies\n", "policies\n\n", 1),
                         load_policies._policy_changes(previous, current))

    def test_failed_write_does_not_advance_session_baseline(self):
        with tempfile.TemporaryDirectory() as cwd, tempfile.TemporaryDirectory() as data:
            event = self.event(cwd)
            client = FakeClient({"status": "OK", "policies": "# General policies\n## Tests\nOld."})
            load_policies.process_hook(event, client_factory=lambda: client, data_directory=Path(data))
            client.result = {"status": "OK", "policies": "# General policies\n## Tests\nNew."}
            with mock.patch.object(load_policies, "_write_policy_file", side_effect=load_policies.PolicyBootstrapError("Write failed")):
                with self.assertRaises(load_policies.PolicyBootstrapError):
                    load_policies.process_hook(event, client_factory=lambda: client, data_directory=Path(data))
            output = load_policies.process_hook(event, client_factory=lambda: client, data_directory=Path(data))
            self.assertIn("POLICY CHANGE:", self.context(output))
            self.assertIn("New.", self.context(output))

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

    def test_hook_input_is_read_as_utf8_with_or_without_bom(self):
        event = {"cwd": "C:\\Users\\Đorđe Á", "prompt": "Zdravo 😀"}
        encoded = json.dumps(event, ensure_ascii=False).encode("utf-8")
        for raw in (encoded, b"\xef\xbb\xbf" + encoded):
            with self.subTest(bom=raw != encoded):
                self.assertEqual(event, load_policies._read_event(io.BytesIO(raw)))
        with self.assertRaises(load_policies.PolicyBootstrapError):
            load_policies._read_event(io.BytesIO(b'{"cwd": "\xff"}'))

    @unittest.skipUnless(shutil.which("git"), "git is not installed")
    def test_non_ascii_git_root_is_found_from_a_subfolder(self):
        with tempfile.TemporaryDirectory() as root:
            repository = Path(root) / "Đorđe repo"
            nested = repository / "src" / "módulo"
            nested.mkdir(parents=True)
            subprocess.run(["git", "init", "-q", str(repository)], check=True)
            self.assertEqual(
                os.path.normcase(os.path.realpath(repository)),
                os.path.normcase(os.path.realpath(load_policies._repository_path(str(nested)))),
            )

    def test_replace_retries_a_locked_file_only_on_windows(self):
        for os_name, raises in (("nt", False), ("posix", True)):
            with self.subTest(os_name=os_name):
                source, destination = mock.Mock(), Path("CLAUDE.md")
                source.replace.side_effect = [PermissionError("locked"), None]
                with mock.patch.object(load_policies.os, "name", os_name), \
                        mock.patch.object(load_policies.time, "sleep") as sleep:
                    if raises:
                        with self.assertRaises(PermissionError):
                            load_policies._replace_file(source, destination)
                    else:
                        load_policies._replace_file(source, destination)
                self.assertEqual(1 if raises else 2, source.replace.call_count)
                self.assertEqual(0 if raises else 1, sleep.call_count)

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
