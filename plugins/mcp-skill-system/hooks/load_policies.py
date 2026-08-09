#!/usr/bin/env python3
"""Load repository policies from MCP before Codex handles a prompt."""

from __future__ import annotations

import hashlib
import json
import os
import subprocess
import sys
import tempfile
import urllib.error
import urllib.request
from pathlib import Path
from typing import Any, Callable


PROTOCOL_VERSION = "2025-11-25"
DEFAULT_MCP_URL = "http://localhost:5231/mcp"
POLICY_TOOL = "policy_get_by_repository"


class PolicyBootstrapError(RuntimeError):
    pass


class McpHttpClient:
    def __init__(self, url: str, timeout_seconds: int = 20) -> None:
        self._url = url
        self._timeout_seconds = timeout_seconds
        self._session_id: str | None = None

    def get_policies(self, repository_path: str) -> dict[str, Any]:
        initialized = self._request(
            {
                "jsonrpc": "2.0",
                "id": 1,
                "method": "initialize",
                "params": {
                    "protocolVersion": PROTOCOL_VERSION,
                    "capabilities": {},
                    "clientInfo": {
                        "name": "mcp-skill-system-codex-hook",
                        "version": "0.2.0",
                    },
                },
            },
            request_id=1,
        )
        negotiated_version = _get_case_insensitive(
            initialized.get("result", {}), "protocolVersion"
        )
        if not negotiated_version:
            raise PolicyBootstrapError("MCP initialize returned no protocol version.")

        self._request(
            {
                "jsonrpc": "2.0",
                "method": "notifications/initialized",
                "params": {},
            },
            request_id=None,
        )
        response = self._request(
            {
                "jsonrpc": "2.0",
                "id": 2,
                "method": "tools/call",
                "params": {
                    "name": POLICY_TOOL,
                    "arguments": {"repositoryPath": repository_path},
                },
            },
            request_id=2,
        )
        return _read_tool_result(response)

    def close(self) -> None:
        if not self._session_id:
            return
        request = urllib.request.Request(self._url, method="DELETE")
        request.add_header("Mcp-Session-Id", self._session_id)
        try:
            urllib.request.urlopen(request, timeout=2).close()
        except (OSError, urllib.error.URLError):
            pass

    def _request(
        self, payload: dict[str, Any], request_id: int | None
    ) -> dict[str, Any]:
        body = json.dumps(payload, separators=(",", ":")).encode("utf-8")
        request = urllib.request.Request(self._url, data=body, method="POST")
        request.add_header("Content-Type", "application/json")
        request.add_header("Accept", "application/json, text/event-stream")
        request.add_header("MCP-Protocol-Version", PROTOCOL_VERSION)
        if self._session_id:
            request.add_header("Mcp-Session-Id", self._session_id)

        try:
            with urllib.request.urlopen(
                request, timeout=self._timeout_seconds
            ) as response:
                session_id = response.headers.get("Mcp-Session-Id")
                if session_id:
                    self._session_id = session_id
                response_body = response.read().decode("utf-8")
        except urllib.error.HTTPError as error:
            details = error.read().decode("utf-8", errors="replace")
            raise PolicyBootstrapError(
                f"MCP returned HTTP {error.code}: {details or error.reason}"
            ) from error
        except (OSError, urllib.error.URLError) as error:
            raise PolicyBootstrapError(f"MCP is unavailable: {error}") from error

        if request_id is None:
            return {}

        for message in _parse_messages(response_body):
            if message.get("id") != request_id:
                continue
            if "error" in message:
                raise PolicyBootstrapError(
                    f"MCP request failed: {json.dumps(message['error'])}"
                )
            return message
        raise PolicyBootstrapError("MCP returned no response for the request.")


def process_hook(
    event: dict[str, Any],
    *,
    client_factory: Callable[[], McpHttpClient] | None = None,
    data_directory: Path | None = None,
) -> dict[str, Any] | None:
    event_name = str(event.get("hook_event_name", ""))
    session_id = str(event.get("session_id", ""))
    if not session_id:
        raise PolicyBootstrapError("Codex hook input did not include session_id.")

    cache_path = _cache_path(session_id, data_directory)
    if event_name == "SessionEnd":
        cache_path.unlink(missing_ok=True)
        return None

    repository_path = _repository_path(str(event.get("cwd", "")))
    cached = _read_cache(cache_path)
    cached_result = cached.get("result") if cached else None
    if (
        cached
        and cached.get("repositoryPath") == repository_path
        and isinstance(cached_result, dict)
    ):
        if event_name == "SessionStart":
            return _context_output(
                event_name,
                _policy_context(repository_path, cached_result),
            )
        return None

    if cached:
        cache_path.unlink(missing_ok=True)

    client = client_factory() if client_factory else McpHttpClient(_mcp_url())
    try:
        result = client.get_policies(repository_path)
    finally:
        client.close()

    status = str(_get_case_insensitive(result, "status") or "")
    if status == "OK":
        _write_cache(
            cache_path,
            {"repositoryPath": repository_path, "result": result},
        )
    elif status != "RepositoryMappingRequired":
        raise PolicyBootstrapError(
            f"Policy retrieval returned unexpected status '{status or 'missing'}'."
        )

    return _context_output(event_name, _policy_context(repository_path, result))


def _repository_path(cwd: str) -> str:
    if not cwd or not os.path.isabs(cwd) or not os.path.isdir(cwd):
        raise PolicyBootstrapError("Codex did not provide a valid absolute cwd.")

    normalized_cwd = os.path.normpath(cwd)
    try:
        completed = subprocess.run(
            ["git", "-C", normalized_cwd, "rev-parse", "--show-toplevel"],
            check=False,
            capture_output=True,
            text=True,
            timeout=3,
            env={**os.environ, "GIT_OPTIONAL_LOCKS": "0"},
        )
    except (OSError, subprocess.SubprocessError):
        return normalized_cwd

    git_root = completed.stdout.strip()
    if completed.returncode == 0 and os.path.isabs(git_root):
        return os.path.normpath(git_root)
    return normalized_cwd


def _mcp_url() -> str:
    override = os.environ.get("MCP_SKILL_SYSTEM_URL")
    if override:
        return override

    plugin_root = os.environ.get("PLUGIN_ROOT")
    if plugin_root:
        config_path = Path(plugin_root) / ".mcp.json"
        try:
            config = json.loads(config_path.read_text(encoding="utf-8"))
            server = config["mcpServers"]["mcp-skill-system"]
            url = server.get("url")
            if isinstance(url, str) and url:
                return url
        except (OSError, KeyError, TypeError, json.JSONDecodeError):
            pass
    return DEFAULT_MCP_URL


def _cache_path(session_id: str, data_directory: Path | None) -> Path:
    root = data_directory
    if root is None:
        configured = os.environ.get("PLUGIN_DATA")
        root = (
            Path(configured)
            if configured
            else Path(tempfile.gettempdir()) / "mcp-skill-system-plugin"
        )
    root.mkdir(mode=0o700, parents=True, exist_ok=True)
    digest = hashlib.sha256(session_id.encode("utf-8")).hexdigest()
    return root / f"policies-{digest}.json"


def _read_cache(path: Path) -> dict[str, Any] | None:
    try:
        value = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError):
        return None
    return value if isinstance(value, dict) else None


def _write_cache(path: Path, value: dict[str, Any]) -> None:
    temporary = path.with_suffix(".tmp")
    temporary.write_text(json.dumps(value), encoding="utf-8")
    temporary.chmod(0o600)
    temporary.replace(path)


def _read_tool_result(response: dict[str, Any]) -> dict[str, Any]:
    result = response.get("result")
    if not isinstance(result, dict):
        raise PolicyBootstrapError("MCP tool returned no result object.")
    if result.get("isError") is True:
        raise PolicyBootstrapError(_tool_text(result) or "MCP policy tool failed.")

    structured = result.get("structuredContent")
    if isinstance(structured, dict):
        return structured

    text = _tool_text(result)
    try:
        parsed = json.loads(text)
    except json.JSONDecodeError as error:
        raise PolicyBootstrapError("MCP policy tool returned invalid JSON.") from error
    if not isinstance(parsed, dict):
        raise PolicyBootstrapError("MCP policy tool returned a non-object result.")
    return parsed


def _tool_text(result: dict[str, Any]) -> str:
    content = result.get("content")
    if not isinstance(content, list):
        return ""
    return "\n".join(
        block.get("text", "")
        for block in content
        if isinstance(block, dict)
        and block.get("type") == "text"
        and isinstance(block.get("text"), str)
    )


def _parse_messages(body: str) -> list[dict[str, Any]]:
    if not body.strip():
        return []
    try:
        value = json.loads(body)
        return [value] if isinstance(value, dict) else []
    except json.JSONDecodeError:
        messages: list[dict[str, Any]] = []
        for line in body.splitlines():
            if not line.startswith("data:"):
                continue
            try:
                value = json.loads(line[5:].strip())
            except json.JSONDecodeError:
                continue
            if isinstance(value, dict):
                messages.append(value)
        return messages


def _policy_context(repository_path: str, result: dict[str, Any]) -> str:
    status = str(_get_case_insensitive(result, "status") or "")
    if status == "RepositoryMappingRequired":
        message = _get_case_insensitive(result, "message") or ""
        projects = _get_case_insensitive(result, "projects") or []
        return (
            "MCP Skill System could not load policies because the trusted "
            f"repository is not mapped.\nTrusted repository: {repository_path}\n"
            "Stop repository reasoning and changes. Show the projects below and ask "
            "the user to select one or provide a unique new project name. Never "
            "guess. Use MCP to create or update the mapping; the plugin will then "
            "retry policy loading.\n"
            f"{message}\nProjects:\n{json.dumps(projects, indent=2)}"
        )

    policies = _get_case_insensitive(result, "policies") or ""
    return (
        "MCP Skill System loaded the authoritative policy context for this "
        f"session using trusted repository '{repository_path}'. Do not retrieve "
        "repository policies again during this session.\n\n"
        "Precedence: current explicit user instruction > project policies > topic "
        "policies > general policies > skill guidance > memory. Platform system "
        "and safety instructions remain higher.\n\n"
        f"POLICIES BEGIN\n{policies}\nPOLICIES END\n\n"
        "Load every policy-required skill through MCP before working. For other "
        "specialized work, use MCP skill search when available; otherwise use "
        "skill_list and skill_get. Stop if a required skill cannot be loaded. "
        "Persist clear durable corrections as policies at the narrowest valid scope."
    )


def _context_output(event_name: str, context: str) -> dict[str, Any]:
    return {
        "hookSpecificOutput": {
            "hookEventName": event_name,
            "additionalContext": context,
        }
    }


def _failure_output(message: str) -> dict[str, Any]:
    reason = (
        "MCP Skill System policy bootstrap failed. Stop without inspecting or "
        f"changing the repository. {message}"
    )
    return {"continue": False, "stopReason": reason, "systemMessage": reason}


def _get_case_insensitive(value: Any, key: str) -> Any:
    if not isinstance(value, dict):
        return None
    lowered = key.casefold()
    return next(
        (item for name, item in value.items() if str(name).casefold() == lowered),
        None,
    )


def main() -> int:
    try:
        event = json.load(sys.stdin)
        if not isinstance(event, dict):
            raise PolicyBootstrapError("Codex hook input was not a JSON object.")
        output = process_hook(event)
    except (PolicyBootstrapError, OSError, json.JSONDecodeError) as error:
        output = _failure_output(str(error))

    if output is not None:
        print(json.dumps(output))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
