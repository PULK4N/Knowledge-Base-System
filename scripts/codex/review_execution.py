"""Shared cache preparation and cancellation of independently invoked reviewers."""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import signal
import subprocess
import sys
from pathlib import Path

from review_process import Cancellation, OwnedProcesses
from run_codex_reviewers import codex_command, ensure_shared_context
from review_context import save_json


def native_command(model: str, reasoning: str, repo: Path) -> list[str]:
    return codex_command(argparse.Namespace(codex=os.environ.get("CODEX_REVIEW_EXECUTABLE"),
        agent=model, reasoning_level=reasoning), repo)


def prepare_cache(shared: str, assignment: dict, repo: Path, directory: Path,
                  cancellation: Cancellation) -> dict:
    directory.mkdir(parents=True, exist_ok=True)
    command = native_command(assignment["model"], assignment["reasoningLevel"], repo)
    executable = Path(command[0])
    info = executable.stat()
    identity = {"layoutVersion": 5, "sharedPrefixSha256": hashlib.sha256(shared.encode("utf-8")).hexdigest(),
                "sharedPrefixBytes": len(shared.encode("utf-8")), "agent": assignment["model"],
                "reasoningLevel": assignment["reasoningLevel"], "codexExecutable": str(executable),
                "codexSize": info.st_size, "codexModifiedNs": info.st_mtime_ns,
                "codexHome": str(Path(os.environ.get("CODEX_HOME", Path.home() / ".codex")).resolve())}
    with_owner = OwnedProcesses(cancellation)
    try:
        status = {"status": "preparing"}
        seed = ensure_shared_context(command, identity, shared, repo, with_owner, cancellation, directory, status)
        save_json(directory / "cache.json", seed)
        return seed
    finally:
        with_owner.close()


class ReviewerProcesses(OwnedProcesses):
    """Let a runner handle SIGTERM and stop its own Codex process group first."""

    def terminate(self) -> None:
        if os.name != "nt" and self.process is not None and self.process.poll() is None:
            try:
                os.killpg(self.process.pid, signal.SIGTERM)
                self.process.wait(timeout=10)
            except (ProcessLookupError, subprocess.TimeoutExpired):
                pass
        super().terminate()


def runner_parameters(assignment: dict, repo: Path, head: str) -> list[str]:
    return ["--cache-id", assignment["cacheId"], "--model", assignment["model"],
            "--reasoning-level", assignment["reasoningLevel"],
            "--input-file", assignment["inputPath"], "--repo", str(repo),
            "--expected-head", head, "--output-dir", assignment["outputDirectory"],
            "--cache-metadata", assignment["cacheMetadataPath"]]


def invoke_reviewer(assignment: dict, repo: Path, head: str, cancellation: Cancellation,
                    register, unregister) -> dict:
    child = Cancellation()
    owner = ReviewerProcesses(child)
    directory = Path(assignment["outputDirectory"])
    registered = False
    try:
        directory.mkdir(parents=True, exist_ok=True)
        register(child)
        registered = True
        cancellation.check()
        parameters = runner_parameters(assignment, repo, head)
        assignment["runnerParameters"] = parameters
        command = [sys.executable, str(Path(__file__).with_name("run_codex_reviewers.py")), *parameters]
        result = owner.run(command, repo, directory / "runner.stdout", directory / "runner.stderr")
        output = (directory / "runner.stdout").read_text(encoding="utf-8")
        summary = json.loads(output.strip().splitlines()[-1]) if output.strip() else {}
        assignment["result"] = summary
        assignment["status"] = "completed" if result == 0 and summary.get("status") == "completed" else "failed"
        if assignment["status"] == "failed":
            assignment["error"] = summary.get("error", "Reviewer runner exited with code " + str(result))
        return assignment
    finally:
        owner.close()
        if registered:
            unregister(child)
