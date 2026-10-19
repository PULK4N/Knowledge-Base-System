from __future__ import annotations

import importlib.util
import json
import sys
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch


SCRIPT_PATH = Path(__file__).parents[1] / "claude" / "set_prompt_models.py"
SPEC = importlib.util.spec_from_file_location(
    "set_claude_prompt_models",
    SCRIPT_PATH,
)
assert SPEC is not None
assert SPEC.loader is not None
script = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = script
SPEC.loader.exec_module(script)


SESSION_ID = "9ba64417-934f-4ff6-9ebf-b042096a3a46"


def user(text: str, timestamp: str) -> dict:
    return {
        "type": "user",
        "timestamp": timestamp,
        "message": {"role": "user", "content": text},
    }


def assistant(model: str, tool_use_id: str | None = None) -> dict:
    content = [{"type": "text", "text": "reply"}]
    if tool_use_id:
        content.append({"type": "tool_use", "id": tool_use_id})
    return {
        "type": "assistant",
        "message": {"model": model, "content": content},
    }


def tool_result(timestamp: str) -> dict:
    return {
        "type": "user",
        "timestamp": timestamp,
        "message": {"content": [{"type": "tool_result", "content": "ok"}]},
    }


def queued(text: str, timestamp: str) -> dict:
    return {
        "type": "attachment",
        "timestamp": timestamp,
        "attachment": {
            "type": "queued_command",
            "prompt": text,
            "commandMode": "prompt",
            "timestamp": timestamp,
        },
    }


SESSION_ENTRIES = [
    user("First prompt", "2026-10-19T09:00:00.100Z"),
    assistant("<synthetic>"),
    assistant("claude-opus-5-5", "toolu_1"),
    tool_result("2026-10-19T09:00:05.000Z"),
    {**assistant("claude-haiku-5-5"), "isSidechain": True},
    user("Second prompt", "2026-10-19T09:05:00.100Z"),
    assistant("claude-sonnet-5-5"),
    queued("Typed while working", "2026-10-19T09:05:10.000Z"),
    assistant("claude-sonnet-5-5", "toolu_2"),
    user("Interrupted prompt", "2026-10-19T09:10:00.100Z"),
]

CONVERSATION = {
    "messages": [
        {
            "promptId": "p1",
            "timestamp": "2026-10-19T09:00:00.300Z",
            "hookEventName": "UserPromptSubmit",
            "message": "First prompt",
        },
        {
            "promptId": "p2",
            "timestamp": "2026-10-19T09:05:00.300Z",
            "hookEventName": "UserPromptSubmit",
            "message": "Second  prompt",
        },
        {
            "promptId": "p3",
            "timestamp": "2026-10-19T09:05:10.300Z",
            "hookEventName": "UserPromptSubmit",
            "message": "Typed while working",
        },
        {
            "promptId": "p4",
            "timestamp": "2026-10-19T09:06:00.000Z",
            "hookEventName": "UserPromptSubmit",
            "message": "/login",
        },
        {
            "promptId": "p5",
            "timestamp": "2026-10-19T09:10:00.300Z",
            "hookEventName": "UserPromptSubmit",
            "message": "Interrupted prompt",
        },
    ],
    "toolCalls": [
        {"promptId": "p4", "toolUseId": "toolu_2"},
    ],
}


class FakeApi:
    def __init__(self, already_set: set[str]) -> None:
        self.already_set = already_set
        self.calls: list[tuple[str, str, str]] = []

    def find_memory_id(self, session_id: str) -> str:
        return "memory-1"

    def get_conversation(self, memory_id: str) -> dict:
        return CONVERSATION

    def set_prompt_model(self, session_id: str, prompt_id: str, model: str) -> None:
        self.calls.append((session_id, prompt_id, model))
        if prompt_id in self.already_set:
            raise script.ApiError(
                500,
                "EventValidationException: PromptModelMustNotBeSetValidator",
            )


class SetClaudePromptModelsTests(unittest.TestCase):
    def setUp(self) -> None:
        self._directory = tempfile.TemporaryDirectory()
        self.session_file = Path(self._directory.name) / f"{SESSION_ID}.jsonl"
        self.session_file.write_text(
            "\n".join(json.dumps(entry) for entry in SESSION_ENTRIES) + "\n",
            encoding="utf-8",
        )

    def tearDown(self) -> None:
        self._directory.cleanup()

    def test_matches_prompts_to_the_model_of_their_reply(self) -> None:
        turns = script.read_session_turns(self.session_file)
        prompts = script.read_memory_prompts(CONVERSATION)

        matches = script.match_prompts(prompts, turns)

        self.assertEqual(
            {
                "p1": "claude-opus-5-5",
                "p2": "claude-sonnet-5-5",
                "p3": "claude-sonnet-5-5",
                "p4": "claude-sonnet-5-5",
                "p5": None,
            },
            {prompt_id: turn.model for prompt_id, turn in matches.items()},
        )

    def test_continues_when_the_model_is_already_set(self) -> None:
        api = FakeApi(already_set={"p1"})
        counts = script.Counts()

        with patch("builtins.print"):
            script.process_session(api, self.session_file, counts, dry_run=False)

        self.assertEqual(
            ["p1", "p2", "p3", "p4"],
            [prompt_id for _, prompt_id, _ in api.calls],
        )
        self.assertEqual((3, 1, 1, 0), (
            counts.set, counts.already_set, counts.unmatched, counts.failed
        ))

    def test_dry_run_does_not_call_the_api(self) -> None:
        api = FakeApi(already_set=set())
        counts = script.Counts()

        with patch("builtins.print"):
            script.process_session(api, self.session_file, counts, dry_run=True)

        self.assertEqual([], api.calls)
        self.assertEqual(4, counts.set)


if __name__ == "__main__":
    unittest.main()
