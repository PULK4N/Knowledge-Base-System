#!/usr/bin/env python3
"""Queue useful Claude hook payloads and request summaries after compaction."""

from __future__ import annotations

import base64
import json
import os
import re
import subprocess
import sys
import tempfile
import time
import urllib.error
import urllib.parse
import urllib.request
import uuid
from pathlib import Path
from typing import IO, Any, Callable

try:
    import fcntl
except ImportError:  # Windows has no fcntl; msvcrt locks the same byte range.
    fcntl = None
    import msvcrt


DEFAULT_MEMORY_HOOK_URL = (
    "http://localhost:5231/api/memory/claude/prompt-hooks"
)
MEMORY_HOOK_PATH = "/api/memory/claude/prompt-hooks"
DEFAULT_TOOL_USE_HOOK_URL = (
    "http://localhost:5231/api/memory/claude/tool-calls"
)
TOOL_USE_HOOK_PATH = "/api/memory/claude/tool-calls"
RECORDED_EVENTS = frozenset({"UserPromptSubmit", "Stop"})
IMAGE_PLACEHOLDER = re.compile(r"\[Image #(\d+)\]")
IMAGE_MEDIA_TYPES = {
    ".png": "image/png",
    ".jpg": "image/jpeg",
    ".jpeg": "image/jpeg",
    ".gif": "image/gif",
    ".webp": "image/webp",
}
IMAGE_EXTENSIONS = {
    "image/png": "png",
    "image/jpeg": "jpg",
    "image/gif": "gif",
    "image/webp": "webp",
}


class MemoryHookError(RuntimeError):
    pass


class MemoryHookRejected(MemoryHookError):
    """A queued record the Memory API can never accept, however often it is sent.

    Retrying such a record forever would block every record queued behind it, so
    the drain worker sets it aside and carries on with the rest of the queue.
    """


class MemoryApiClient:
    def __init__(self, url: str, timeout_seconds: int = 20) -> None:
        self._url = url
        self._timeout_seconds = timeout_seconds

    def record(self, payload: dict[str, Any]) -> None:
        request = urllib.request.Request(
            self._url,
            data=json.dumps(payload, separators=(",", ":")).encode("utf-8"),
            method="POST",
        )
        request.add_header("Content-Type", "application/json")

        try:
            with urllib.request.urlopen(
                request, timeout=self._timeout_seconds
            ) as response:
                response.read()
        except urllib.error.HTTPError as error:
            details = error.read().decode("utf-8", errors="replace")
            failure = (
                MemoryHookRejected
                if 400 <= error.code < 500
                else MemoryHookError
            )
            raise failure(
                f"Memory API returned HTTP {error.code}: "
                f"{details[:500] or error.reason}"
            ) from error
        except (OSError, urllib.error.URLError) as error:
            raise MemoryHookError(f"Memory API is unavailable: {error}") from error


class RoutedMemoryApiClient(MemoryApiClient):
    """Sends each queued record to the endpoint that accepts its hook event.

    Both hook scripts share one queue so records keep their order, but tool
    exchanges belong to a different Memory API endpoint than prompts and
    replies.
    """

    def __init__(self) -> None:
        super().__init__(_memory_hook_url())
        self._tool_client = MemoryApiClient(_tool_use_hook_url())

    def record(self, payload: dict[str, Any]) -> None:
        if payload.get("hook_event_name") == "PostToolUse":
            self._tool_client.record(payload)
            return

        super().record(payload)


class TurnRegistry:
    """Assigns the turn identifier that groups one prompt with its reply.

    Claude Code hook payloads carry a session id but no turn id, so the turn is
    minted here: ``UserPromptSubmit`` starts a new turn and every later hook in
    the same session reuses it until the next prompt arrives.
    """

    def __init__(self, root: Path) -> None:
        self._root = root
        self._root.mkdir(mode=0o700, parents=True, exist_ok=True)

    def start_turn(self, session_id: str) -> str:
        turn_id = str(uuid.uuid4())
        self._write(session_id, turn_id)
        return turn_id

    def current_turn(self, session_id: str) -> str:
        try:
            turn_id = self._path(session_id).read_text(encoding="utf-8").strip()
        except OSError:
            turn_id = ""

        if not turn_id:
            return self.start_turn(session_id)

        try:
            return str(uuid.UUID(turn_id))
        except ValueError:
            return self.start_turn(session_id)

    def end_session(self, session_id: str) -> None:
        self._path(session_id).unlink(missing_ok=True)

    def _write(self, session_id: str, turn_id: str) -> None:
        path = self._path(session_id)
        temporary = path.with_suffix(".tmp")
        temporary.write_text(turn_id, encoding="utf-8")
        temporary.chmod(0o600)
        temporary.replace(path)

    def _path(self, session_id: str) -> Path:
        return self._root / f"{session_id}.turn"


class MemoryHookQueue:
    def __init__(self, root: Path) -> None:
        self._root = root
        self._root.mkdir(mode=0o700, parents=True, exist_ok=True)
        self._root.chmod(0o700)

    def enqueue(self, payload: dict[str, Any]) -> None:
        name = f"{time.time_ns():020d}-{uuid.uuid4().hex}"
        temporary = self._root / f"{name}.tmp"
        queued = self._root / f"{name}.json"
        temporary.write_text(
            json.dumps(_without_lone_surrogates(payload)), encoding="utf-8"
        )
        temporary.chmod(0o600)
        temporary.replace(queued)

    def drain(self, client: MemoryApiClient) -> None:
        lock_path = self._root / "drain.lock"
        with lock_path.open("a+", encoding="utf-8") as lock:
            lock_path.chmod(0o600)
            if not _acquire_drain_lock(lock):
                return

            rejected: MemoryHookRejected | None = None
            try:
                while True:
                    queued = sorted(self._root.glob("*.json"))
                    if not queued:
                        if rejected is None:
                            self._failure_path().unlink(missing_ok=True)
                        else:
                            self.record_failure(rejected)
                        return

                    for path in queued:
                        claimed = path.with_suffix(".sending")
                        try:
                            path.replace(claimed)
                        except FileNotFoundError:
                            continue

                        try:
                            client.record(self._read(claimed))
                        except MemoryHookRejected as error:
                            rejected = error
                            claimed.replace(claimed.with_suffix(".rejected"))
                        except Exception:
                            claimed.replace(path)
                            raise
                        else:
                            claimed.unlink(missing_ok=True)
            finally:
                _release_drain_lock(lock)

    def _read(self, path: Path) -> dict[str, Any]:
        try:
            payload = json.loads(path.read_text(encoding="utf-8"))
        except json.JSONDecodeError as error:
            raise MemoryHookRejected(
                f"Queued hook payload '{path.name}' is not valid JSON: {error}"
            ) from error

        if not isinstance(payload, dict):
            raise MemoryHookRejected(
                f"Queued hook payload '{path.name}' is not an object."
            )

        return _without_lone_surrogates(payload)

    def record_failure(self, error: Exception) -> None:
        failure = self._failure_path()
        failure.write_text(str(error)[:1000], encoding="utf-8")
        failure.chmod(0o600)

    def last_failure(self) -> str | None:
        try:
            message = self._failure_path().read_text(encoding="utf-8").strip()
        except OSError:
            return None
        return message or None

    def _failure_path(self) -> Path:
        return self._root / "last-error.txt"


def _acquire_drain_lock(lock: IO[str]) -> bool:
    """Take the exclusive drain lock, or report that another worker holds it.

    Only one worker may send queued records at a time. POSIX waits on ``flock``;
    Windows has no ``fcntl``, and ``msvcrt.locking`` gives up after its own retry
    window, in which case the worker that owns the lock drains the same queue.
    """
    if fcntl is not None:
        fcntl.flock(lock.fileno(), fcntl.LOCK_EX)
        return True

    lock.seek(0)
    try:
        msvcrt.locking(lock.fileno(), msvcrt.LK_LOCK, 1)
    except OSError:
        return False
    return True


def _release_drain_lock(lock: IO[str]) -> None:
    if fcntl is not None:
        fcntl.flock(lock.fileno(), fcntl.LOCK_UN)
        return

    lock.seek(0)
    msvcrt.locking(lock.fileno(), msvcrt.LK_UNLCK, 1)


def _without_lone_surrogates(value: Any) -> Any:
    """Replace unpaired surrogates anywhere in a hook payload.

    Hook input that is not valid UTF-8, such as a multi-byte character split
    across a truncation boundary, decodes to lone surrogates like U+DC90 under
    Python's surrogateescape handler. json.dumps writes those out as escaped
    surrogates, which System.Text.Json parses but then refuses either to read as
    a string or to write back out, so the Memory API answers HTTP 500.
    """
    if isinstance(value, str):
        return _replace_lone_surrogates(value)

    if isinstance(value, dict):
        return {
            _without_lone_surrogates(key): _without_lone_surrogates(item)
            for key, item in value.items()
        }

    if isinstance(value, list):
        return [_without_lone_surrogates(item) for item in value]

    return value


def _replace_lone_surrogates(text: str) -> str:
    """Turn each undecodable byte back into one replacement character.

    surrogateescape maps bytes 0x80-0xFF to U+DC80-U+DCFF, so encoding with the
    same handler recovers the original bytes and decoding them leniently marks
    only the broken ones. Surrogates from any other source cannot be encoded
    that way and are replaced individually instead.
    """
    try:
        return text.encode("utf-8", "surrogateescape").decode(
            "utf-8", "replace"
        )
    except UnicodeEncodeError:
        return text.encode("utf-8", "replace").decode("utf-8")


def process_hook(
    event: dict[str, Any],
    *,
    queue: MemoryHookQueue | None = None,
    turns: TurnRegistry | None = None,
    worker_starter: Callable[[], None] | None = None,
) -> dict[str, Any] | None:
    event_name = str(event.get("hook_event_name", ""))

    if event_name == "SessionStart" and event.get("source") == "compact":
        session_id = _required_guid(event, "session_id")
        return _summary_output(session_id)

    memory_queue = queue or MemoryHookQueue(_queue_directory())
    turn_registry = turns or TurnRegistry(_turn_directory())
    start_worker = worker_starter or _start_worker

    if event_name in RECORDED_EVENTS:
        session_id = _required_guid(event, "session_id")
        turn_id = (
            turn_registry.start_turn(session_id)
            if event_name == "UserPromptSubmit"
            else turn_registry.current_turn(session_id)
        )
        previous_failure = memory_queue.last_failure()
        record = {**event, "turn_id": turn_id}
        images = (
            _prompt_images_or_none(event)
            if event_name == "UserPromptSubmit"
            else []
        )
        memory_queue.enqueue(
            {"hook": record, "attachments": images} if images else record
        )
        start_worker()
        return _backlog_warning(previous_failure)

    if event_name == "SessionEnd":
        session_id = str(event.get("session_id", ""))
        if session_id:
            turn_registry.end_session(session_id)
        start_worker()

    return None


def _prompt_images_or_none(event: dict[str, Any]) -> list[dict[str, Any]]:
    """Read the prompt's images without ever losing the prompt itself.

    Finding images depends on undocumented Claude Code storage that can change.
    When it fails, the prompt is still recorded without attachments; its hook
    input keeps the prompt_id and transcript_path, so the images can be added
    later once the reading logic is fixed.
    """
    try:
        return prompt_images(event)
    except Exception:
        return []


def prompt_images(event: dict[str, Any]) -> list[dict[str, Any]]:
    """Return the images pasted into a prompt, in placeholder order.

    Hook input carries only the prompt text, where a pasted image appears as a
    placeholder such as "[Image #2]". Claude Code saves each pasted image as
    ``images/<number>.<ext>`` next to the session scratchpad when it is pasted,
    so it is there before the prompt is submitted. The session transcript holds
    the same images but is written asynchronously and may not have the prompt
    yet, so it is only read for numbers whose file is missing.
    """
    numbers = list(dict.fromkeys(IMAGE_PLACEHOLDER.findall(str(event.get("prompt", "")))))
    if not numbers:
        return []

    images = _pasted_image_files(event, numbers)
    if any(number not in images for number in numbers):
        transcript_images = _transcript_images(event)
        for number in numbers:
            images.setdefault(number, transcript_images.get(number))

    return [images[number] for number in numbers if images.get(number)]


def _pasted_image_files(
    event: dict[str, Any], numbers: list[str]
) -> dict[str, dict[str, Any]]:
    scratchpad = str(event.get("scratchpad_dir", ""))
    if not scratchpad:
        return {}

    folder = Path(scratchpad).parent / "images"
    images: dict[str, dict[str, Any]] = {}
    for number in numbers:
        for path in sorted(folder.glob(f"{number}.*")):
            media_type = IMAGE_MEDIA_TYPES.get(path.suffix.lower())
            if media_type is None:
                continue
            try:
                content = path.read_bytes()
            except OSError:
                continue
            images[number] = _image(
                number, media_type, base64.b64encode(content).decode("ascii")
            )
            break
    return images


def _transcript_images(event: dict[str, Any]) -> dict[str, dict[str, Any]]:
    """Read the pasted images of the prompt from the session transcript.

    Claude Code stores a pasted image as a base64 block in the user entry's
    message content and numbers it in ``imagePasteIds`` in the same order.
    Images nested in tool results are not pasted by the user and are left out.
    """
    prompt_id = str(event.get("prompt_id", ""))
    transcript_path = str(event.get("transcript_path", ""))
    if not prompt_id or not transcript_path:
        return {}

    images: dict[str, dict[str, Any]] = {}
    try:
        with Path(transcript_path).open(
            encoding="utf-8", errors="replace"
        ) as transcript:
            for line in transcript:
                # Most lines are neither this prompt nor an image; skip them
                # without parsing, since transcripts can be large.
                if prompt_id not in line or '"image"' not in line:
                    continue
                try:
                    entry = json.loads(line)
                except json.JSONDecodeError:
                    continue
                images.update(_entry_images(entry, prompt_id))
    except OSError:
        return {}
    return images


def _entry_images(entry: Any, prompt_id: str) -> dict[str, dict[str, Any]]:
    if (
        not isinstance(entry, dict)
        or entry.get("type") != "user"
        or entry.get("promptId") != prompt_id
    ):
        return {}

    message = entry.get("message")
    content = message.get("content") if isinstance(message, dict) else None
    paste_ids = entry.get("imagePasteIds")
    if not isinstance(content, list) or not isinstance(paste_ids, list):
        return {}

    sources = [
        block["source"]
        for block in content
        if isinstance(block, dict)
        and block.get("type") == "image"
        and isinstance(block.get("source"), dict)
        and block["source"].get("type") == "base64"
        and block["source"].get("data")
    ]
    return {
        str(number): _image(
            str(number),
            str(source.get("media_type") or "application/octet-stream"),
            str(source["data"]),
        )
        for number, source in zip(paste_ids, sources)
    }


def _image(number: str, media_type: str, content: str) -> dict[str, Any]:
    extension = IMAGE_EXTENSIONS.get(media_type, "bin")
    return {
        "name": f"image-{number}.{extension}",
        "media_type": media_type,
        "content": content,
        "placeholder": f"[Image #{number}]",
    }


def drain_queue(
    *,
    queue: MemoryHookQueue | None = None,
    client: MemoryApiClient | None = None,
) -> None:
    memory_queue = queue or MemoryHookQueue(_queue_directory())
    api_client = client or RoutedMemoryApiClient()
    try:
        memory_queue.drain(api_client)
    except Exception as error:
        memory_queue.record_failure(error)


def _required_guid(event: dict[str, Any], field: str) -> str:
    value = str(event.get(field, ""))
    try:
        return str(uuid.UUID(value))
    except ValueError as error:
        raise MemoryHookError(
            f"Claude hook input did not include a valid {field}."
        ) from error


def _summary_output(session_id: str) -> dict[str, Any]:
    context = (
        "This chat has just been compacted. Before continuing normal work, "
        "write a concise two-to-four paragraph checkpoint from the compacted "
        "conversation and call the MCP Knowledge Base tool memory_summary_add with "
        f"threadId '{session_id}'. Capture goals, decisions, important changes, "
        "verification, unresolved work, and durable user preferences. Omit "
        "repetitive tool output, credentials, and other secrets."
    )
    return {
        "hookSpecificOutput": {
            "hookEventName": "SessionStart",
            "additionalContext": context,
        }
    }


def _backlog_warning(previous_failure: str | None) -> dict[str, Any] | None:
    if not previous_failure:
        return None
    return {
        "systemMessage": (
            "MCP Knowledge Base queued this memory record, but an earlier queued "
            f"record has not reached the Memory API yet: {previous_failure}"
        )
    }


def _data_directory() -> Path:
    configured = os.environ.get("CLAUDE_PLUGIN_DATA")
    return (
        Path(configured)
        if configured
        else Path(tempfile.gettempdir()) / "mcp-knowledge-base-claude-plugin"
    )


def _queue_directory() -> Path:
    return _data_directory() / "memory-queue"


def _turn_directory() -> Path:
    return _data_directory() / "memory-turns"


def _memory_hook_url() -> str:
    override = os.environ.get("MCP_KNOWLEDGE_BASE_MEMORY_HOOK_URL")
    if override:
        return override

    mcp_url = os.environ.get("MCP_KNOWLEDGE_BASE_URL")
    if not mcp_url:
        return DEFAULT_MEMORY_HOOK_URL

    parsed = urllib.parse.urlsplit(mcp_url)
    return urllib.parse.urlunsplit(
        (parsed.scheme, parsed.netloc, MEMORY_HOOK_PATH, "", "")
    )


def _tool_use_hook_url() -> str:
    override = os.environ.get("MCP_KNOWLEDGE_BASE_TOOL_USE_HOOK_URL")
    if override:
        return override

    mcp_url = os.environ.get("MCP_KNOWLEDGE_BASE_URL")
    if not mcp_url:
        return DEFAULT_TOOL_USE_HOOK_URL

    parsed = urllib.parse.urlsplit(mcp_url)
    return urllib.parse.urlunsplit(
        (parsed.scheme, parsed.netloc, TOOL_USE_HOOK_PATH, "", "")
    )


def _start_worker() -> None:
    subprocess.Popen(
        [sys.executable, str(Path(__file__).resolve()), "--drain"],
        cwd=str(Path(__file__).resolve().parent),
        stdin=subprocess.DEVNULL,
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
        close_fds=True,
        **_detached_process_options(),
    )


def _detached_process_options() -> dict[str, Any]:
    """Keep the drain worker alive and silent after the hook process exits.

    ``start_new_session`` is POSIX-only and is ignored on Windows, where the
    equivalent is detaching the child from the console it would inherit.
    """
    if hasattr(subprocess, "DETACHED_PROCESS"):
        return {
            "creationflags": (
                subprocess.DETACHED_PROCESS | subprocess.CREATE_NEW_PROCESS_GROUP
            )
        }
    return {"start_new_session": True}


def main() -> int:
    if len(sys.argv) == 2 and sys.argv[1] == "--drain":
        drain_queue()
        return 0

    event_name = ""
    try:
        event = json.load(sys.stdin)
        if not isinstance(event, dict):
            raise MemoryHookError("Claude hook input was not a JSON object.")
        event_name = str(event.get("hook_event_name", ""))
        output = process_hook(event)
    except (MemoryHookError, OSError, json.JSONDecodeError) as error:
        output = {
            "systemMessage": (
                "MCP Knowledge Base could not queue this memory hook. Normal work "
                f"can continue, but this record may be missing: {error}"
            )
        }

    if output is not None:
        print(json.dumps(output))
    elif event_name == "Stop":
        print("{}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
