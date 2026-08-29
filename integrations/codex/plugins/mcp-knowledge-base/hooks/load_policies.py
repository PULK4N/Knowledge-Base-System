#!/usr/bin/env python3
"""Override AGENTS.md with repository policies from the HTTP API before Codex works."""

from __future__ import annotations

import hashlib
import json
import os
import subprocess
import sys
import tempfile
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path
from typing import Any, Callable


DEFAULT_MCP_URL = "http://localhost:5231/mcp"
POLICY_PATH = "/api/policies"
AGENT_FAMILY_PATH = "/api/policies/agent-families"
DEFAULT_AGENT_FAMILY = "codex"
POLICY_FILE_NAME = "AGENTS.md"
POLICY_DOCUMENT_MARKER = "# General policies"
AGENT_FAMILY_NOT_FOUND_STATUS = "AgentFamilyNotFound"


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
        return _policy_file_output(event_name, repository_path, cached_result)

    if cached:
        cache_path.unlink(missing_ok=True)

    client = (
        client_factory()
        if client_factory
        else PolicyHttpClient(_policy_url(), _agent_family_url())
    )
    try:
        result = _fetch_policies(client, repository_path, _agent_family())
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

    return _policy_file_output(event_name, repository_path, result)


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
    event_name: str, repository_path: str, result: dict[str, Any]
) -> dict[str, Any] | None:
    status = str(_get_case_insensitive(result, "status") or "")
    if status == "RepositoryMappingRequired":
        return _context_output(
            event_name, _mapping_required_context(repository_path, result)
        )

    policies = _get_case_insensitive(result, "policies") or ""
    document = _policy_document(str(policies))
    announce = not _has_loaded_policies(repository_path)
    _write_policy_file(repository_path, document)

    if not announce:
        # The agent reads the policy file on its own; saying so every turn only
        # spends context on something it already has.
        return None
    return _context_output(
        event_name,
        f"Policies written to {POLICY_FILE_NAME}.",
    )


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
    plugin_root = os.environ.get("PLUGIN_ROOT")
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
        configured = os.environ.get("PLUGIN_DATA")
        root = (
            Path(configured)
            if configured
            else Path(tempfile.gettempdir()) / "mcp-knowledge-base-plugin"
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

    temporary = path.with_name(f"{POLICY_FILE_NAME}.mcp-tmp")
    try:
        temporary.write_text(document, encoding="utf-8")
        temporary.replace(path)
    except OSError as error:
        temporary.unlink(missing_ok=True)
        raise PolicyBootstrapError(
            f"Could not write authoritative policies to {path}: {error}"
        ) from error
    return True


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


def _parse_result(body: str) -> dict[str, Any]:
    try:
        parsed = json.loads(body)
    except json.JSONDecodeError as error:
        raise PolicyBootstrapError(
            "Policy API returned invalid JSON."
        ) from error
    if not isinstance(parsed, dict):
        raise PolicyBootstrapError("Policy API returned a non-object result.")
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
