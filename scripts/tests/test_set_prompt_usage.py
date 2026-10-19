from __future__ import annotations

import importlib.util
import json
import sys
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

SCRIPTS = Path(__file__).parents[1]
sys.path.insert(0, str(SCRIPTS))
from prompt_usage_common import run, FIELDS


def load(provider):
    spec = importlib.util.spec_from_file_location(provider + '_usage', SCRIPTS / provider / 'set_prompt_usage.py')
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


claude = load('claude')
codex = load('codex')
SESSION = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
FIRST = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
SECOND = 'cccccccc-cccc-cccc-cccc-cccccccccccc'


def conversation(*ids):
    return {'messages': [{'promptId': value, 'hookEventName': 'UserPromptSubmit', 'message': 'Question',
                          'timestamp': '2026-10-19T10:00:00Z'} for value in ids]}


def assistant(identifier, output=10, stop='end_turn'):
    return {'type': 'assistant', 'message': {'id': identifier, 'model': 'claude', 'stop_reason': stop,
            'content': [], 'usage': {'input_tokens': 2, 'cache_read_input_tokens': 5,
            'cache_creation_input_tokens': 3, 'output_tokens': output,
            'output_tokens_details': {'thinking_tokens': 1}}}}


def event(kind, **values):
    return {'type': 'event_msg', 'payload': {'type': kind, **values}}


def tokens(input_count, output, **extra):
    usage = {'input_tokens': input_count, 'cached_input_tokens': 0, 'cache_write_input_tokens': 0,
             'output_tokens': output, 'reasoning_output_tokens': 0, **extra}
    return event('token_count', info={'total_token_usage': usage, 'last_token_usage': usage})


class UsageTests(unittest.TestCase):
    def parse(self, module, entries, conv):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / (SESSION + '.jsonl')
            path.write_text('\n'.join(json.dumps(entry) for entry in entries), encoding='utf-8')
            session, proposals = module.read_session(path)
            self.assertEqual(SESSION, session)
            return proposals(conv)

    def test_claude_deduplicates_streaming_replies_and_sums_requests(self):
        rows = [{'type': 'user', 'message': {'content': 'Question'}},
                assistant('one', 1, 'tool_use'), assistant('one', 10, 'tool_use'),
                {'type': 'user', 'message': {'content': [{'type': 'tool_result'}]}}, assistant('two')]
        result, skipped = self.parse(claude, rows, conversation(FIRST))
        self.assertEqual(0, skipped)
        self.assertEqual(20, result[FIRST]['outputTokens'])
        self.assertEqual(4, result[FIRST]['inputTokens'])
        self.assertEqual(2, result[FIRST]['thinkingTokens'])
        self.assertEqual([rows[1], rows[2], rows[4]], result[FIRST]['sourceRecords'])

    def test_claude_ambiguous_or_incomplete_turns_are_not_written(self):
        rows = [{'type': 'user', 'message': {'content': 'Question'}}, assistant('one')]
        self.assertEqual(({}, 2), self.parse(claude, rows, conversation(FIRST, SECOND)))
        rows[-1] = assistant('one', stop='tool_use')
        self.assertEqual(({}, 1), self.parse(claude, rows, conversation(FIRST)))

    def test_claude_missing_usage_does_not_become_zero(self):
        rows = [{'type': 'user', 'message': {'content': 'Question'}}, assistant('one'), assistant('two')]
        del rows[1]['message']['usage']
        self.assertEqual(({}, 1), self.parse(claude, rows, conversation(FIRST)))

    def codex_rows(self):
        return [{'type': 'session_meta', 'payload': {'id': SESSION}},
                event('task_started', turn_id=FIRST), tokens(100, 10), tokens(100, 10),
                tokens(250, 30), event('task_complete', turn_id=FIRST),
                event('task_started', turn_id=SECOND), tokens(300, 40), event('task_complete', turn_id=SECOND)]

    def test_codex_deltas_do_not_double_count_cumulative_snapshots(self):
        result, skipped = self.parse(codex, self.codex_rows(), conversation(FIRST, SECOND))
        self.assertEqual(0, skipped)
        self.assertEqual((250, 30), (result[FIRST]['inputTokens'], result[FIRST]['outputTokens']))
        self.assertEqual((50, 10), (result[SECOND]['inputTokens'], result[SECOND]['outputTokens']))
        self.assertEqual(self.codex_rows()[4], result[SECOND]['baselineRecord'])
        self.assertEqual([self.codex_rows()[7]], result[SECOND]['sourceRecords'])

    def test_codex_active_reset_and_forked_turns_are_skipped(self):
        rows = self.codex_rows()
        self.assertEqual(1, self.parse(codex, rows[:-1], conversation(FIRST, SECOND))[1])
        rows[4] = tokens(10, 2)
        self.assertNotIn(FIRST, self.parse(codex, rows, conversation(FIRST, SECOND))[0])
        rows = self.codex_rows()
        rows[0]['payload']['forked_from_id'] = SECOND
        self.assertNotIn(FIRST, self.parse(codex, rows, conversation(FIRST, SECOND))[0])

    def test_codex_unknown_required_counter_is_not_assumed_zero(self):
        rows = self.codex_rows()
        del rows[2]['payload']['info']['total_token_usage']['cache_write_input_tokens']
        self.assertNotIn(FIRST, self.parse(codex, rows, conversation(FIRST, SECOND))[0])

    def test_codex_subagent_files_are_excluded(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / 'rollout.jsonl'
            path.write_text(json.dumps({'type': 'session_meta', 'payload': {
                'id': SESSION, 'source': {'subagent': {'thread_spawn': {}}}}}), encoding='utf-8')
            self.assertEqual((None, None), codex.read_session(path))

    def test_claude_queued_prompt_and_missing_reasoning(self):
        rows = [{'type': 'user', 'message': {'content': 'Question'}}, assistant('one'),
                {'type': 'attachment', 'attachment': {'type': 'queued_command', 'prompt': 'Next'}},
                assistant('two')]
        del rows[-1]['message']['usage']['output_tokens_details']
        conv = conversation(FIRST, SECOND)
        conv['messages'][1]['message'] = 'Next'
        result, skipped = self.parse(claude, rows, conv)
        self.assertEqual(0, skipped)
        self.assertEqual(10, result[SECOND]['outputTokens'])
        self.assertIsNone(result[SECOND]['thinkingTokens'])

    def test_malformed_log_fails_before_proposals(self):
        for module in (claude, codex):
            with self.subTest(module=module.__name__), tempfile.TemporaryDirectory() as folder:
                path = Path(folder) / (SESSION + '.jsonl')
                path.write_text('{unfinished', encoding='utf-8')
                with self.assertRaises(ValueError):
                    module.read_session(path)

    def test_existing_different_or_partial_usage_is_never_overwritten(self):
        usage = dict(zip(FIELDS, (100, 0, 0, 10, None)))
        usage['sourceRecords'] = [{'original': {'extra': 42}}]
        class Api:
            def __init__(self, *args): pass
            def find_memory_id(self, session): return SESSION
            def get_conversation(self, memory):
                return {'messages': [{'promptId': FIRST, 'outputTokens': 0}]}
            def _send(self, *args):
                raise AssertionError('Already-set usage must not be sent')
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / 'session.jsonl'
            path.write_text('{}', encoding='utf-8')
            reader = lambda path: (SESSION, lambda conv: ({FIRST: usage}, 0))
            with patch('prompt_usage_common.MemoryApi', Api), patch.object(sys, 'argv', ['script', str(path), '--execute']):
                self.assertEqual(0, run('codex', reader, path))

    def test_cli_dry_run_execute_verify_and_unchanged(self):
        usage = dict(zip(FIELDS, (100, 0, 0, 10, None)))
        usage['sourceRecords'] = [{'original': {'extra': 42}}]
        class Api:
            written = []
            def __init__(self, *args): pass
            def find_memory_id(self, session): return SESSION
            def get_conversation(self, memory):
                return {'messages': [{'promptId': FIRST, **(usage if self.written else {})}]}
            def _send(self, method, path, body): self.written.append((method, path, body))
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / 'session.jsonl'
            path.write_text('{}', encoding='utf-8')
            reader = lambda path: (SESSION, lambda conv: ({FIRST: usage}, 0))
            with patch('prompt_usage_common.MemoryApi', Api):
                for options, expected_writes in [([], 0), (['--execute'], 1), (['--execute'], 1)]:
                    with patch.object(sys, 'argv', ['script', str(path), *options]):
                        self.assertEqual(0, run('codex', reader, path))
                        self.assertEqual(expected_writes, len(Api.written))
        self.assertEqual('PUT', Api.written[0][0])
        self.assertIn('/codex/sessions/', Api.written[0][1])
        self.assertEqual([{'original': {'extra': 42}}], Api.written[0][2]['sourceRecords'])


if __name__ == '__main__':
    unittest.main()
