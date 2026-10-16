import base64
import importlib.util
import json
import tempfile
import unittest
from pathlib import Path
from unittest import mock

SCRIPT = Path(__file__).parents[1] / 'hooks' / 'write_memory.py'
SPEC = importlib.util.spec_from_file_location('attachment_write_memory', SCRIPT)
write_memory = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(write_memory)

SESSION = '019fb72e-e0c3-7452-b32b-5bbf65433c98'
TURN = '019fb72e-e3c3-7093-a89d-050d309ca4ac'
IMAGE = 'data:image/png;base64,' + base64.b64encode(b'image content').decode('ascii')


def record(kind, **payload):
    return json.dumps({'type': kind, 'payload': payload}) + '\n'


def transcript(turn=TURN, **message):
    return (record('event_msg', type='task_started', turn_id=turn)
            + record('turn_context', turn_id=turn)
            + record('event_msg', type='user_message', message='Same prompt', **message))


class AttachmentTests(unittest.TestCase):
    def event(self, path, name='Stop'):
        return dict(session_id=SESSION, turn_id=TURN, hook_event_name=name,
                    transcript_path=str(path), cwd=str(path.parent))

    def test_only_current_turn_images_are_uploaded_with_stop_or_prompt(self):
        for name in ('Stop', 'UserPromptSubmit'):
            with self.subTest(name=name), tempfile.TemporaryDirectory() as directory:
                path = Path(directory) / 'rollout.jsonl'
                path.write_text(transcript('old-turn', images=[IMAGE])
                                + transcript(images=[IMAGE])
                                + transcript('next-turn', images=[IMAGE]), encoding='utf-8')
                event = self.event(path, name)
                result = write_memory._with_attachments(event)
                self.assertEqual(event, result['hook'])
                self.assertEqual(1, len(result['attachments']))
                image = result['attachments'][0]
                self.assertEqual(b'image content', base64.b64decode(image['content']))
                self.assertEqual('[Image #1]', image['placeholder'])
                self.assertNotIn('data:image', json.dumps(result['hook']))

    def test_local_paths_support_spaces_unicode_absolute_and_relative(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            image = root / 'pasted č image.JPEG'
            image.write_bytes(b'local image')
            path = root / 'rollout.jsonl'
            for source in (str(image), image.name):
                with self.subTest(source=source):
                    path.write_text(transcript(local_images=[source]), encoding='utf-8')
                    result = write_memory._with_attachments(self.event(path))
                    self.assertEqual('image/jpeg', result['attachments'][0]['media_type'])
                    self.assertEqual(b'local image', base64.b64decode(result['attachments'][0]['content']))

    def test_missing_malformed_or_unmatched_transcript_keeps_text(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'rollout.jsonl'
            event = self.event(path)
            self.assertEqual(event, write_memory._with_attachments(event))
            for content in ('not json\n', 'null\n', transcript('other', images=[IMAGE]),
                            record('event_msg', type='user_message', images=[IMAGE]),
                            transcript(images=['https://example.com/image.png', 'data:image/png;base64,!']),
                            transcript(local_images=['missing.png'])):
                with self.subTest(content=content):
                    path.write_text(content, encoding='utf-8')
                    self.assertEqual(event, write_memory._with_attachments(event))

    def test_worker_waits_for_submission_record_and_sends_images(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'rollout.jsonl'
            event = self.event(path, 'UserPromptSubmit')
            event['prompt'] = 'Same prompt'
            def flush(_):
                path.write_text(transcript(images=[IMAGE]), encoding='utf-8')
            queue = write_memory.MemoryHookQueue(Path(directory) / 'queue')
            queue.enqueue(event)
            with mock.patch.object(write_memory.time, 'sleep', side_effect=flush) as sleep, \
                    mock.patch.object(write_memory.urllib.request, 'urlopen') as send:
                queue.drain(write_memory.RoutedMemoryApiClient())
            sleep.assert_called_once_with(0.1)
            body = json.loads(send.call_args.args[0].data)
            self.assertEqual(event, body['hook'])
            self.assertEqual(1, len(body['attachments']))

    def test_prompt_hook_itself_never_waits_for_transcript(self):
        with tempfile.TemporaryDirectory() as directory:
            queue = write_memory.MemoryHookQueue(Path(directory) / 'queue')
            event = self.event(Path(directory) / 'missing.jsonl', 'UserPromptSubmit')
            with mock.patch.object(write_memory.time, 'sleep') as sleep:
                write_memory.process_hook(event, queue=queue, worker_starter=lambda: None)
            sleep.assert_not_called()
            queued = next((Path(directory) / 'queue').glob('*.json'))
            self.assertEqual(event, json.loads(queued.read_text(encoding='utf-8')))

    def test_failed_delivery_keeps_image_bytes_for_retry(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'rollout.jsonl'
            path.write_text(transcript(images=[IMAGE]), encoding='utf-8')
            queue_path = Path(directory) / 'queue'
            queue = write_memory.MemoryHookQueue(queue_path)
            queue.enqueue(self.event(path))
            client = mock.Mock()
            client.record.side_effect = OSError('offline')
            with self.assertRaises(OSError):
                queue.drain(client)
            queued = json.loads(next(queue_path.glob('*.json')).read_text(encoding='utf-8'))
            self.assertEqual(1, len(queued['attachments']))
            path.write_text('', encoding='utf-8')
            client.record.side_effect = None
            queue.drain(client)
            self.assertEqual(queued, client.record.call_args.args[0])

    def test_worker_timeout_keeps_prompt_available_for_stop_fallback(self):
        event = self.event(Path('missing.jsonl'), 'UserPromptSubmit')
        with mock.patch.object(write_memory.time, 'monotonic', side_effect=[0, 4]), \
                mock.patch.object(write_memory.time, 'sleep') as sleep:
            self.assertEqual(event, write_memory._with_attachments(event, wait_for_prompt=True))
        sleep.assert_not_called()

    def test_windows_lock_and_process_selection(self):
        windows = mock.Mock(LK_LOCK=1, LK_UNLCK=2)
        lock = mock.Mock()
        with mock.patch.object(write_memory, 'fcntl', None), \
                mock.patch.object(write_memory, 'msvcrt', windows, create=True):
            self.assertTrue(write_memory._acquire_drain_lock(lock))
            write_memory._release_drain_lock(lock)
            self.assertEqual([mock.call(lock.fileno(), 1, 1), mock.call(lock.fileno(), 2, 1)],
                             windows.locking.call_args_list)
        with mock.patch.object(write_memory.subprocess, 'DETACHED_PROCESS', 8, create=True), \
                mock.patch.object(write_memory.subprocess, 'CREATE_NEW_PROCESS_GROUP', 512, create=True):
            self.assertEqual({'creationflags': 520}, write_memory._detached_process_options())


if __name__ == '__main__':
    unittest.main()
