import importlib.util
import json
import re
import sys
import tempfile
import unittest
import uuid
from pathlib import Path
from unittest import mock


SCRIPT = Path(__file__).parents[1] / "hooks" / "write_tool_use.py"
HOOKS_DIRECTORY = SCRIPT.parent
sys.path.insert(0, str(HOOKS_DIRECTORY))
SPEC = importlib.util.spec_from_file_location("write_tool_use", SCRIPT)
write_tool_use = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(write_tool_use)

import write_memory


SESSION_ID = "019fb72e-e0c3-7452-b32b-5bbf65433c98"


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

    def test_tool_use_hook_url_uses_the_claude_endpoint(self):
        with mock.patch.dict(
            write_tool_use.os.environ,
            {"MCP_KNOWLEDGE_BASE_URL": "http://knowledge-base/mcp"},
            clear=True,
        ):
            self.assertEqual(
                "http://knowledge-base/api/memory/claude/tool-calls",
                write_tool_use._tool_use_hook_url(),
            )

    def test_post_tool_use_is_queued_with_the_turn_of_the_current_prompt(self):
        with tempfile.TemporaryDirectory() as data:
            queue, turns = self.storage(data)
            starts = []
            write_memory.process_hook(
                self.event("UserPromptSubmit", prompt="Run the tests"),
                queue=queue,
                turns=turns,
                worker_starter=lambda: None,
            )
            prompt_turn = self.queued_payloads(data)[0]["turn_id"]
            event = self.event(
                "PostToolUse",
                tool_name="Bash",
                tool_use_id="tool-use-1",
                tool_input={"command": "dotnet test"},
                tool_response={"output": "Passed"},
            )

            output = write_tool_use.process_hook(
                event,
                queue=queue,
                turns=turns,
                worker_starter=lambda: starts.append(True),
            )

            self.assertIsNone(output)
            self.assertEqual([True], starts)
            queued = self.queued_payloads(data)
            self.assertEqual(2, len(queued))
            self.assertEqual({**event, "turn_id": prompt_turn}, queued[1])

    def test_a_tool_call_without_a_prompt_still_records_a_turn(self):
        with tempfile.TemporaryDirectory() as data:
            queue, turns = self.storage(data)

            write_tool_use.process_hook(
                self.event("PostToolUse", tool_name="Read"),
                queue=queue,
                turns=turns,
                worker_starter=lambda: None,
            )

            turn_id = self.queued_payloads(data)[0]["turn_id"]
            self.assertEqual(turn_id, str(uuid.UUID(turn_id)))

    def test_pre_tool_use_adds_the_session_to_every_skill_mutation(self):
        for tool in write_tool_use.SKILL_MUTATION_TOOLS:
            with self.subTest(tool=tool):
                event = self.event(
                    "PreToolUse",
                    tool_name=f"mcp__plugin_mcp-knowledge-base_mcp-knowledge-base__{tool}",
                    tool_input={"name": "example", "sessionId": "model-supplied"},
                )
                original = json.loads(json.dumps(event))

                output = write_tool_use.process_hook(event)

                self.assertEqual(
                    {
                        "hookSpecificOutput": {
                            "hookEventName": "PreToolUse",
                            "updatedInput": {
                                "name": "example",
                                "sessionId": SESSION_ID,
                            },
                        }
                    },
                    output,
                )
                self.assertEqual(original, event)

    def test_feature_mutations_receive_session_context_through_configured_matcher(self):
        hooks = json.loads((HOOKS_DIRECTORY / "hooks.json").read_text(encoding="utf-8"))
        matcher = hooks["hooks"]["PreToolUse"][0]["matcher"]
        source = (
            SCRIPT.parents[5] / "API" / "DomainsModules" / "FeatureModule" / "MCP" / "FeatureMcpFunctions.cs"
        ).read_text(encoding="utf-8")
        mutations = re.findall(r'Guid\?, Guid\?, Task<[^\n]+\n\s*"([^"]+)"', source)
        self.assertEqual(19, len(mutations))
        self.assertEqual(set(mutations), write_tool_use.FEATURE_MUTATION_TOOLS)
        for tool in mutations:
            with self.subTest(tool=tool):
                event = self.event(
                    "PreToolUse",
                    tool_name=f"mcp__plugin_mcp-knowledge-base_mcp-knowledge-base__{tool}",
                    tool_input={"featureId": "feature-id", "sessionId": "stale"},
                )
                self.assertIsNotNone(re.search(matcher, event["tool_name"]))
                result = write_tool_use.process_hook(event)["hookSpecificOutput"]
                self.assertEqual(SESSION_ID, result["updatedInput"]["sessionId"])
                self.assertEqual("feature-id", result["updatedInput"]["featureId"])
                self.assertEqual("stale", event["tool_input"]["sessionId"])
        for tool in (
            "feature_get",
            "feature_list",
            "feature_plan_get",
            "feature_record_list",
            "feature_research_discovery_search",
        ):
            with self.subTest(read=tool):
                name = f"mcp__plugin_mcp-knowledge-base_mcp-knowledge-base__{tool}"
                self.assertIsNone(re.search(matcher, name))
                self.assertIsNone(write_tool_use.process_hook(
                    self.event("PreToolUse", tool_name=name, tool_input={})
                ))

    def test_policy_mutations_receive_session_context_through_configured_matcher(self):
        hooks = json.loads((HOOKS_DIRECTORY / "hooks.json").read_text(encoding="utf-8"))
        matcher = hooks["hooks"]["PreToolUse"][0]["matcher"]
        source_directory = SCRIPT.parents[5] / "API" / "DomainsModules" / "PolicyModule" / "MCP"
        mutations = {
            name
            for source in source_directory.glob("*PolicyMcpFunctions.cs")
            for name in re.findall(r'"(policy_[a-z_]+)"', source.read_text(encoding="utf-8"))
            if name.endswith(("_add", "_update", "_remove", "_create", "_delete"))
        }
        self.assertEqual(24, len(mutations))
        self.assertEqual(mutations, write_tool_use.POLICY_MUTATION_TOOLS)
        for tool in mutations:
            with self.subTest(tool=tool):
                event = self.event(
                    "PreToolUse",
                    tool_name=f"mcp__plugin_mcp-knowledge-base_mcp-knowledge-base__{tool}",
                    tool_input={"title": "policy", "sessionId": "stale"},
                )
                self.assertIsNotNone(re.search(matcher, event["tool_name"]))
                result = write_tool_use.process_hook(event)["hookSpecificOutput"]
                self.assertEqual(SESSION_ID, result["updatedInput"]["sessionId"])
                self.assertEqual("policy", result["updatedInput"]["title"])
                self.assertEqual("stale", event["tool_input"]["sessionId"])
        for tool in (
            "policy_general_list",
            "policy_project_list",
            "policy_project_get_by_name",
            "policy_topic_policy_list",
            "policy_agent_family_list",
        ):
            with self.subTest(read=tool):
                name = f"mcp__plugin_mcp-knowledge-base_mcp-knowledge-base__{tool}"
                self.assertIsNone(re.search(matcher, name))
                self.assertIsNone(write_tool_use.process_hook(
                    self.event("PreToolUse", tool_name=name, tool_input={})
                ))

    def test_pre_tool_use_rejects_invalid_context(self):
        for values in ({"session_id": "invalid"}, {"tool_input": None}):
            with self.subTest(values=values):
                event = self.event(
                    "PreToolUse", tool_name="skill_add", tool_input={}
                )
                event.update(values)
                with self.assertRaises(write_memory.MemoryHookError):
                    write_tool_use.process_hook(event)

    def test_pre_tool_use_ignores_other_tools(self):
        for tool in ("skill_get", "skill_reference_get", "Bash"):
            with self.subTest(tool=tool):
                self.assertIsNone(write_tool_use.process_hook(
                    self.event("PreToolUse", tool_name=tool, tool_input={})
                ))

    def test_other_events_are_ignored(self):
        with tempfile.TemporaryDirectory() as data:
            queue, turns = self.storage(data)

            output = write_tool_use.process_hook(
                self.event("Stop", last_assistant_message="Done"),
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
                write_tool_use.process_hook(
                    {
                        "hook_event_name": "PostToolUse",
                        "session_id": "not-a-guid",
                        "tool_name": "Bash",
                    },
                    queue=queue,
                    turns=turns,
                    worker_starter=lambda: None,
                )

            self.assertEqual([], list((Path(data) / "queue").glob("*.json")))

    def test_either_worker_routes_a_mixed_queue_and_retries_failed_tool_calls(self):
        for worker in (write_memory, write_tool_use):
            with self.subTest(worker=worker.__name__), tempfile.TemporaryDirectory() as data:
                queue = write_memory.MemoryHookQueue(
                    Path(data) / "memory-queue"
                )
                events = [
                    self.event(
                        "UserPromptSubmit", prompt="Hello", turn_id=SESSION_ID
                    ),
                    self.event(
                        "PostToolUse", tool_name="Bash", turn_id=SESSION_ID
                    ),
                    self.event(
                        "Stop", last_assistant_message="Done", turn_id=SESSION_ID
                    ),
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
                        "CLAUDE_PLUGIN_DATA": data,
                        "MCP_KNOWLEDGE_BASE_MEMORY_HOOK_URL": prompt_url,
                        "MCP_KNOWLEDGE_BASE_TOOL_USE_HOOK_URL": tool_url,
                    },
                    clear=True,
                ), mock.patch.object(
                    sys, "argv", [str(worker.__file__), "--drain"]
                ):
                    with mock.patch.object(
                        write_memory.urllib.request,
                        "urlopen",
                        side_effect=send,
                    ):
                        self.assertEqual(0, worker.main())
                    self.assertEqual([(prompt_url, events[0])], sent)
                    self.assertEqual(
                        2, len(list(queue._root.glob("*.json")))
                    )
                    self.assertIn("offline", queue.last_failure())

                    with mock.patch.object(
                        write_memory.urllib.request, "urlopen"
                    ) as deliver:
                        self.assertEqual(0, worker.main())
                    self.assertEqual(
                        [(tool_url, events[1]), (prompt_url, events[2])],
                        [
                            (
                                call.args[0].full_url,
                                json.loads(call.args[0].data),
                            )
                            for call in deliver.call_args_list
                        ],
                    )
                    self.assertEqual([], list(queue._root.glob("*.json")))
                    self.assertIsNone(queue.last_failure())

    @staticmethod
    def storage(data):
        return (
            write_memory.MemoryHookQueue(Path(data) / "queue"),
            write_memory.TurnRegistry(Path(data) / "turns"),
        )

    @staticmethod
    def queued_payloads(data):
        return [
            json.loads(path.read_text(encoding="utf-8"))
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
