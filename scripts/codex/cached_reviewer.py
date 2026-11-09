"""Run one reviewer against an existing shared session, without KB requests."""
from __future__ import annotations

import argparse
import json
import os
import tempfile
import uuid
from datetime import datetime, timezone
from pathlib import Path

from review_context import save_json
from review_execution import native_command
from review_process import Cancellation, OwnedProcesses, ReviewCancelled
from run_codex_reviewers import (GitComparison, cache_usage, incremental_usage, read_reviewer_events,
                                load_cache_seed, uuid_argument, write_utf8)


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--cache-id", required=True, type=uuid_argument)
    parser.add_argument("--model", required=True)
    parser.add_argument("--reasoning-level", required=True)
    parser.add_argument("--input-file", required=True, type=Path)
    parser.add_argument("--output-dir", required=True, type=Path)
    parser.add_argument("--repo", type=Path, default=Path.cwd())
    parser.add_argument("--expected-head", required=True)
    parser.add_argument("--cache-metadata", type=Path)
    args = parser.parse_args(argv)
    cancellation = Cancellation()
    owner = None
    attempt = args.output_dir.resolve() / ("attempt-" + str(uuid.uuid4()))
    status = {"status": "running", "cacheId": args.cache_id, "model": args.model,
              "reasoningLevel": args.reasoning_level, "startedAt": datetime.now(timezone.utc).isoformat()}
    code = 1
    try:
        attempt.mkdir(parents=True)
        cancellation.install()
        owner = OwnedProcesses(cancellation)
        save_json(attempt / "status.json", status)
        repo = args.repo.resolve()
        with tempfile.TemporaryDirectory(prefix="kb-review-git-") as temporary:
            git = GitComparison(owner, repo, Path(temporary))
            actual = git.text(["rev-parse", "--verify", "HEAD^{commit}"])
            if actual != args.expected_head:
                raise ValueError("Checkout HEAD changed; restore the reviewed commit or start a new coordinator run")
        prompt = args.input_file.read_text(encoding="utf-8")
        if not prompt.strip():
            raise ValueError("Reviewer input is empty")
        write_utf8(attempt / "input.md", prompt)
        seed = None
        if args.cache_metadata:
            seed = load_cache_seed(args.cache_metadata, {"threadId": args.cache_id,
                "agent": args.model, "reasoningLevel": args.reasoning_level})
        command = native_command(args.model, args.reasoning_level, repo)
        executable_info = Path(command[0]).stat()
        if seed and (seed.get("codexExecutable") != command[0]
                     or seed.get("codexSize") != executable_info.st_size
                     or seed.get("codexModifiedNs") != executable_info.st_mtime_ns
                     or seed.get("codexHome") != str(Path(os.environ.get("CODEX_HOME", Path.home() / ".codex")).resolve())):
            raise ValueError("Cached session belongs to another Codex executable or home")
        command[-1:] = ["fork", args.cache_id, "-"]
        stdout, stderr = attempt / "reviewer.stdout", attempt / "reviewer.stderr"
        def started(pid):
            status["reviewerPid"] = pid
            save_json(attempt / "status.json", status)
        result = owner.run(command, repo, stdout, stderr, attempt / "input.md", started)
        status["reviewerExitCode"] = result
        if result:
            raise RuntimeError("Reviewer exited with code " + str(result) + "; see " + str(stderr))
        diagnostics = stderr.read_text(encoding="utf-8", errors="replace")
        if "helper_unknown_error: setup refresh had errors" in diagnostics or "rejected: blocked by policy" in diagnostics:
            raise RuntimeError("Reviewer sandbox could not read source; see " + str(stderr))
        message, usage = read_reviewer_events(stdout)
        findings = attempt / "findings.md"
        write_utf8(findings, message)
        baseline = seed.get("tokenUsage") if seed else None
        measured = incremental_usage(usage, baseline) if baseline else None
        status.update(status="completed", findingsPath=str(findings), reportedTokenUsage=usage,
                      tokenUsage=measured, cacheUsage=cache_usage(measured))
        code = 0
    except (ReviewCancelled, KeyboardInterrupt):
        cancellation.cancel()
        status["status"] = "cancelled"
        code = 130
    except Exception as error:
        status.update(status="failed", error=str(error))
    finally:
        if owner is not None:
            owner.close()
        cancellation.close()
        status.update(exitCode=code, finishedAt=datetime.now(timezone.utc).isoformat(),
                      outputDirectory=str(attempt), statusPath=str(attempt / "status.json"))
        if attempt.is_dir():
            save_json(attempt / "status.json", status)
        print(json.dumps(status))
    return code
