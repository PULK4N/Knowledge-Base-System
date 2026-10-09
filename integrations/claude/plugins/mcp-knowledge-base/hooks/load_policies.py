#!/usr/bin/env python3
"""Override CLAUDE.md with repository policies from the HTTP API before Claude works."""

from __future__ import annotations

import hashlib
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
from pathlib import Path
from typing import Any, BinaryIO, Callable


DEFAULT_MCP_URL = "http://localhost:5231/mcp"
POLICY_PATH = "/api/policies"
AGENT_FAMILY_PATH = "/api/policies/agent-families"
DEFAULT_AGENT_FAMILY = "claude"
POLICY_FILE_NAME = "CLAUDE.md"
POLICY_DOCUMENT_MARKER = "# General policies"
AGENT_FAMILY_NOT_FOUND_STATUS = "AgentFamilyNotFound"
REPLACE_ATTEMPTS = 5
REPLACE_RETRY_SECONDS = 0.1


class PolicyBootstrapError(RuntimeError):
    pass


class AgentFamilyMissingError(PolicyBootstrapError):
    """The knowledge base has no such agent family yet."""


class PolicyHttpClient:
    """Reads repository policies from the knowledge base HTTP API.

    Policies are deliberately not fetched over MCP. Serving them only over HTTP
    keeps this hook the single place that decides which agent family the
    session belongs to.
    """

    def __init__(
        self,
        url: str,
        agent_family_url: str,
        timeout_seconds: int = 20,
    ) -> None:
        self._url = url
        self._agent_family_url = agent_family_url
        self._timeout_seconds = timeout_seconds

    def get_policies(
        self, repository_path: str, agent_family: str
    ) -> dict[str, Any]:
        query = urllib.parse.urlencode(
            {"repositoryPath": repository_path, "agentFamily": agent_family}
        )
        request = urllib.request.Request(f"{self._url}?{query}", method="GET")
        request.add_header("Accept", "application/json")

        try:
            with urllib.request.urlopen(
                request, timeout=self._timeout_seconds
            ) as response:
                body = response.read().decode("utf-8")
        except urllib.error.HTTPError as error:
            details = error.read().decode("utf-8", errors="replace")
            if error.code == 400 and _is_missing_agent_family(details):
                raise AgentFamilyMissingError(agent_family) from error
            raise PolicyBootstrapError(
                f"Policy API returned HTTP {error.code}: {details or error.reason}"
            ) from error
        except (OSError, urllib.error.URLError) as error:
            raise PolicyBootstrapError(
                f"Policy API is unavailable: {error}"
            ) from error

        return _parse_result(body)

    def create_agent_family(self, agent_family: str) -> None:
        """Register the family this plugin loads policies for.

        The plugin knows which agent it serves, so a knowledge base that has
        never seen this agent is provisioned rather than failing the session.
        """
        payload = json.dumps(
            {
                "agentFamilyName": agent_family,
                "description": (
                    f"Policies applied only to {agent_family} sessions."
                ),
            }
        ).encode("utf-8")
        request = urllib.request.Request(
            self._agent_family_url, data=payload, method="POST"
        )
        request.add_header("Content-Type", "application/json")
        request.add_header("Accept", "application/json")

        try:
            urllib.request.urlopen(
                request, timeout=self._timeout_seconds
            ).close()
        except urllib.error.HTTPError:
            # A concurrent session may have created it first; the retried read
            # decides whether the family is really usable.
            pass
        except (OSError, urllib.error.URLError) as error:
            raise PolicyBootstrapError(
                f"Could not create agent family '{agent_family}': {error}"
            ) from error

    def get_project(self, repository_path: str) -> dict[str, Any]:
        query = urllib.parse.urlencode({"repositoryPath": repository_path})
        request = urllib.request.Request(
            f"{self._url.rstrip('/')}/projects/by-repository?{query}", method="GET"
        )
        request.add_header("Accept", "application/json")
        try:
            with urllib.request.urlopen(
                request, timeout=self._timeout_seconds
            ) as response:
                body = response.read().decode("utf-8")
        except urllib.error.HTTPError as error:
            details = error.read().decode("utf-8", errors="replace")
            raise PolicyBootstrapError(
                f"Project API returned HTTP {error.code}: {details or error.reason}"
            ) from error
        except (OSError, urllib.error.URLError) as error:
            raise PolicyBootstrapError(f"Project API is unavailable: {error}") from error
        return _parse_result(body, "Project API")

    def close(self) -> None:
        """Kept so callers can manage the client uniformly; HTTP needs no teardown."""


def process_hook(
    event: dict[str, Any],
    *,
    client_factory: Callable[[], PolicyHttpClient] | None = None,
    data_directory: Path | None = None,
) -> dict[str, Any] | None:
    event_name = str(event.get("hook_event_name", ""))
    session_id = str(event.get("session_id", ""))
    if not session_id:
        raise PolicyBootstrapError("Claude hook input did not include session_id.")

    cache_path = _cache_path(session_id, data_directory)
    if event_name == "SessionEnd":
        cache_path.unlink(missing_ok=True)
        return None

    repository_path = _repository_path(str(event.get("cwd", "")))
    cached = _read_cache(cache_path)
    cached_result = cached.get("result") if cached else None
    if not (
        cached
        and cached.get("repositoryPath") == repository_path
        and isinstance(cached_result, dict)
    ):
        cached_result = None

    client = (
        client_factory()
        if client_factory
        else PolicyHttpClient(_policy_url(), _agent_family_url())
    )
    try:
        result = _fetch_policies(client, repository_path, _agent_family())
        project = (
            client.get_project(repository_path)
            if _get_case_insensitive(result, "status") == "OK"
            else None
        )
    finally:
        client.close()

    status = str(_get_case_insensitive(result, "status") or "")
    if status not in ("OK", "RepositoryMappingRequired"):
        raise PolicyBootstrapError(
            f"Policy retrieval returned unexpected status '{status or 'missing'}'."
        )

    output = _policy_file_output(event_name, repository_path, result, project)
    if status == "OK":
        if cached_result is not None:
            changes = _policy_changes(
                str(_get_case_insensitive(cached_result, "policies") or ""),
                str(_get_case_insensitive(result, "policies") or ""),
            )
            if changes:
                output = _context_output(event_name, "POLICY CHANGE:\n" + changes)
        # Advance the comparison baseline only after all available repositories were written.
        _write_cache(
            cache_path,
            {"repositoryPath": repository_path, "result": result},
        )
    return output


def _policy_sections(document: str) -> dict[tuple[str, str, int], str]:
    """Identify policies by scope and heading, preserving duplicate occurrences.

    The API emits scope headings with # and policy headings with ##. Deeper
    headings and fenced examples belong to the policy body.
    """
    sections: dict[tuple[str, str, int], str] = {}
    scope = ""
    heading = ""
    lines: list[str] = []
    counts: dict[tuple[str, str], int] = {}
    fence = ""

    def flush() -> None:
        body = "\n".join(lines).strip()
        if not body:
            return
        identity = (scope, heading)
        occurrence = counts.get(identity, 0)
        counts[identity] = occurrence + 1
        sections[(scope, heading, occurrence)] = body

    for line in document.splitlines():
        marker = re.match(r"^ {0,3}(`{3,}|~{3,})", line)
        if marker:
            token = marker.group(1)
            if not fence:
                fence = token
            elif token[0] == fence[0] and len(token) >= len(fence):
                fence = ""
            lines.append(line)
            continue
        match = re.match(r"^(#{1,2})\s+(.+)$", line) if not fence else None
        if match:
            flush()
            lines = []
            if len(match.group(1)) == 1:
                scope = line.strip()
                heading = ""
            else:
                heading = line.strip()
                lines.append(line)
        else:
            lines.append(line)
    flush()
    return sections


def _policy_changes(previous: str, current: str) -> str:
    before = _policy_sections(previous)
    after = _policy_sections(current)
    changes = [
        "\n\n".join(part for part in (key[0], body) if part)
        for key, body in after.items()
        if before.get(key) != body
    ]
    changes.extend(
        "Removed policy (no longer applies):\n"
        + "\n".join(part for part in key[:2] if part)
        + ("\n" + body if not key[1] else "")
        for key, body in before.items()
        if key not in after
    )
    return "\n\n".join(changes)


def _fetch_policies(
    client: PolicyHttpClient, repository_path: str, agent_family: str
) -> dict[str, Any]:
    """Read policies, creating this plugin's agent family if it is missing."""
    try:
        return client.get_policies(repository_path, agent_family)
    except AgentFamilyMissingError:
        client.create_agent_family(agent_family)

    try:
        return client.get_policies(repository_path, agent_family)
    except AgentFamilyMissingError as error:
        raise PolicyBootstrapError(
            f"Agent family '{agent_family}' is still missing after creating it."
        ) from error


def _policy_file_output(
    event_name: str, repository_path: str, result: dict[str, Any],
    project: dict[str, Any] | None,
) -> dict[str, Any] | None:
    status = str(_get_case_insensitive(result, "status") or "")
    if status == "RepositoryMappingRequired":
        return _context_output(
            event_name, _mapping_required_context(repository_path, result)
        )

    policies = _get_case_insensitive(result, "policies") or ""
    document = _policy_document(str(policies))
    announce = not _has_loaded_policies(repository_path)
    _write_project_policy_files(repository_path, project, document)

    if not announce:
        # The agent reads the policy file on its own; saying so every turn only
        # spends context on something it already has.
        return None
    return _context_output(
        event_name,
        f"Policies written to {POLICY_FILE_NAME}.",
    )


def _write_project_policy_files(
    repository_path: str, project: dict[str, Any] | None, document: str
) -> None:
    """Synchronize local project repositories; paths on other machines may be absent."""
    mapped_paths = _get_case_insensitive(project, "repositoryPaths")
    if not isinstance(mapped_paths, list) or any(
        not isinstance(path, str) for path in mapped_paths
    ):
        raise PolicyBootstrapError("Project API returned invalid repositoryPaths.")
    if repository_path not in mapped_paths:
        raise PolicyBootstrapError("Project API did not include the requested repository.")

    seen: set[str] = set()
    failures: list[str] = []
    for path in [repository_path, *mapped_paths]:
        normalized = os.path.normcase(os.path.normpath(path))
        if normalized in seen:
            continue
        seen.add(normalized)
        if path != repository_path and (
            not os.path.isabs(path) or not os.path.isdir(path)
        ):
            print(f"Policy sync skipped unavailable repository: {path}", file=sys.stderr)
            continue
        try:
            _write_policy_file(path, document)
        except PolicyBootstrapError as error:
            failures.append(str(error))
    if failures:
        raise PolicyBootstrapError("\n".join(failures))


def _has_loaded_policies(repository_path: str) -> bool:
    """True when the agent already has a policy document worth reading."""
    try:
        existing = _policy_file_path(repository_path).read_text(
            encoding="utf-8"
        )
    except (OSError, UnicodeDecodeError):
        return False
    return POLICY_DOCUMENT_MARKER in existing


def _repository_path(cwd: str) -> str:
    if not cwd or not os.path.isabs(cwd) or not os.path.isdir(cwd):
        raise PolicyBootstrapError("Claude did not provide a valid absolute cwd.")

    normalized_cwd = os.path.normpath(cwd)
    try:
        completed = subprocess.run(
            ["git", "-C", normalized_cwd, "rev-parse", "--show-toplevel"],
            check=False,
            capture_output=True,
            timeout=3,
            env={**os.environ, "GIT_OPTIONAL_LOCKS": "0"},
        )
    except (OSError, subprocess.SubprocessError):
        return normalized_cwd

    # Decode as a file name rather than with the console code page, which on
    # Windows garbles non-ASCII folder names that git prints as UTF-8.
    git_root = os.fsdecode(completed.stdout).strip()
    if (
        completed.returncode == 0
        and os.path.isabs(git_root)
        and os.path.isdir(git_root)
    ):
        return os.path.normpath(git_root)
    return normalized_cwd


def _mcp_url() -> str:
    override = os.environ.get("MCP_KNOWLEDGE_BASE_URL")
    if override:
        return override

    url = _plugin_config().get("url")
    if isinstance(url, str) and url:
        return url
    return DEFAULT_MCP_URL


def _api_base() -> str:
    """Resolve the API root from the configured MCP base address."""
    override = os.environ.get("MCP_KNOWLEDGE_BASE_API_URL")
    if override:
        return override.rstrip("/")

    base = _mcp_url().rstrip("/")
    if base.endswith("/mcp"):
        base = base[: -len("/mcp")]
    return base.rstrip("/")


def _policy_url() -> str:
    return f"{_api_base()}{POLICY_PATH}"


def _agent_family_url() -> str:
    return f"{_api_base()}{AGENT_FAMILY_PATH}"


def _agent_family() -> str:
    """The agent family this hook loads policies for.

    Families are free-form names defined in the knowledge base, so the value is
    configurable; it only defaults to this plugin's own agent.
    """
    override = os.environ.get("MCP_KNOWLEDGE_BASE_AGENT_FAMILY")
    if override and override.strip():
        return override.strip()

    configured = _plugin_config().get("agentFamily")
    if isinstance(configured, str) and configured.strip():
        return configured.strip()
    return DEFAULT_AGENT_FAMILY


def _plugin_config() -> dict[str, Any]:
    plugin_root = os.environ.get("CLAUDE_PLUGIN_ROOT")
    if not plugin_root:
        return {}

    config_path = Path(plugin_root) / ".mcp.json"
    try:
        config = json.loads(config_path.read_text(encoding="utf-8"))
        server = config["mcpServers"]["mcp-knowledge-base"]
    except (OSError, KeyError, TypeError, json.JSONDecodeError):
        return {}
    return server if isinstance(server, dict) else {}


def _cache_path(session_id: str, data_directory: Path | None) -> Path:
    root = data_directory
    if root is None:
        configured = os.environ.get("CLAUDE_PLUGIN_DATA")
        root = (
            Path(configured)
            if configured
            else Path(tempfile.gettempdir()) / "mcp-knowledge-base-claude-plugin"
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


def _policy_file_path(repository_path: str) -> Path:
    return Path(repository_path) / POLICY_FILE_NAME


def _write_policy_file(repository_path: str, document: str) -> bool:
    """Overwrite CLAUDE.md; return True when the file content changed."""
    path = _policy_file_path(repository_path)
    try:
        if path.read_text(encoding="utf-8") == document:
            return False
    except (OSError, UnicodeDecodeError):
        pass

    temporary: Path | None = None
    try:
        # Each session needs its own temporary file. Close it before replacing
        # the destination so the same sequence works on Windows and Linux.
        with tempfile.NamedTemporaryFile(
            mode="w", encoding="utf-8", dir=path.parent,
            prefix=f"{POLICY_FILE_NAME}.", suffix=".mcp-tmp", delete=False,
        ) as stream:
            temporary = Path(stream.name)
            stream.write(document)
        _replace_file(temporary, path)
    except OSError as error:
        if temporary is not None:
            temporary.unlink(missing_ok=True)
        raise PolicyBootstrapError(
            f"Could not write authoritative policies to {path}: {error}"
        ) from error
    return True


def _replace_file(source: Path, destination: Path) -> None:
    """Move source over destination, retrying briefly on Windows.

    Windows refuses to replace a file while another process has it open, which
    happens when sessions in several mapped repositories sync at the same time.
    Linux replaces open files, so a permission error there is real.
    """
    for attempt in range(REPLACE_ATTEMPTS):
        try:
            source.replace(destination)
            return
        except PermissionError:
            if os.name != "nt" or attempt == REPLACE_ATTEMPTS - 1:
                raise
            time.sleep(REPLACE_RETRY_SECONDS * (attempt + 1))


def _policy_document(policies: str) -> str:
    return policies


def _mapping_required_context(repository_path: str, result: dict[str, Any]) -> str:
    message = _get_case_insensitive(result, "message") or ""
    projects = _get_case_insensitive(result, "projects") or []
    return (
        "MCP Knowledge Base could not load policies because the trusted "
        f"repository is not mapped.\nTrusted repository: {repository_path}\n"
        "Stop repository reasoning and changes. Show the projects below and ask "
        "the user to select one or provide a unique new project name. Never "
        "guess. Use MCP to create or update the mapping; the plugin will then "
        f"retry policy loading and rewrite {POLICY_FILE_NAME}.\n"
        f"{message}\nProjects:\n{json.dumps(projects, indent=2)}"
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
        "MCP Knowledge Base policy bootstrap failed. Stop without inspecting or "
        f"changing the repository. {message}"
    )
    return {"continue": False, "stopReason": reason, "systemMessage": reason}


def _parse_result(body: str, source: str = "Policy API") -> dict[str, Any]:
    try:
        parsed = json.loads(body)
    except json.JSONDecodeError as error:
        raise PolicyBootstrapError(
            f"{source} returned invalid JSON."
        ) from error
    if not isinstance(parsed, dict):
        raise PolicyBootstrapError(f"{source} returned a non-object result.")
    return parsed


def _is_missing_agent_family(body: str) -> bool:
    try:
        parsed = json.loads(body)
    except json.JSONDecodeError:
        return False
    status = _get_case_insensitive(parsed, "status")
    return str(status or "") == AGENT_FAMILY_NOT_FOUND_STATUS


def _get_case_insensitive(value: Any, key: str) -> Any:
    if not isinstance(value, dict):
        return None
    lowered = key.casefold()
    return next(
        (item for name, item in value.items() if str(name).casefold() == lowered),
        None,
    )


def _read_event(stream: BinaryIO) -> Any:
    """Parse hook input as UTF-8, which Claude Code sends on every platform.

    Text-mode stdin on Windows uses the console code page, which garbles
    non-ASCII paths and prompts or fails on bytes that page cannot decode.
    """
    try:
        return json.loads(stream.read().decode("utf-8-sig"))
    except UnicodeDecodeError as error:
        raise PolicyBootstrapError("Claude hook input was not UTF-8.") from error


def main() -> int:
    try:
        event = _read_event(sys.stdin.buffer)
        if not isinstance(event, dict):
            raise PolicyBootstrapError("Claude hook input was not a JSON object.")
        output = process_hook(event)
    except (PolicyBootstrapError, OSError, json.JSONDecodeError) as error:
        output = _failure_output(str(error))

    if output is not None:
        print(json.dumps(output))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
