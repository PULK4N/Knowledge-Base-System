"""Run every KB-configured reviewer for the current checkout, without arguments."""
from __future__ import annotations

import argparse
import json
import os
import sys
import threading
import uuid
from concurrent.futures import ThreadPoolExecutor, as_completed
from datetime import datetime, timezone
from pathlib import Path

from review_context import capture_diff, collect_reviewers, discover, load_review, model_key, save_json
from review_execution import invoke_reviewer, prepare_cache, runner_parameters
from review_process import Cancellation, OwnedProcesses, ReviewCancelled
from run_codex_reviewers import GitComparison, build_shared_prompt, cache_root, get_json, write_utf8


def write_report(run: Path, status: dict) -> Path:
    lines = ["# Code review report", "", "Run status: **" + status["status"] + "**", ""]
    if status.get("error"):
        lines.extend(["## Run error", "", status["error"], ""])
    if not status.get("reviewers"):
        lines.append("No reviewers were run.")
    for item in status.get("reviewers", []):
        lines.extend(["## " + item["title"], "", "- Reviewer: `" + item["key"] + "`",
                      "- Model: `" + item["model"] + "` / `" + item["reasoningLevel"] + "`",
                      "- Status: **" + item["status"] + "**"])
        if item.get("cacheId"):
            lines.append("- Shared session: `" + item["cacheId"] + "`")
        if item.get("inputPath"):
            lines.extend(["- Input: [reviewer input](" + Path(item["inputPath"]).relative_to(run).as_posix() + ")", ""])
        findings = item.get("result", {}).get("findingsPath")
        if findings:
            try:
                lines.extend([Path(findings).read_text(encoding="utf-8"), ""])
            except OSError as error:
                lines.extend(["Findings could not be read from " + findings + ": " + str(error), ""])
        elif item.get("error"):
            lines.extend(["Review incomplete: " + item["error"], ""])
        if item.get("runnerParameters"):
            command = [sys.executable, str(Path(__file__).with_name("run_codex_reviewers.py")), *item["runnerParameters"]]
            lines.extend(["Direct runner invocation (argument list):", "", "```json", json.dumps(command, indent=2), "```", ""])
    path = run / "report.md"
    write_utf8(path, "\n".join(lines) + "\n")
    return path


def execute_batches(assignments: list[dict], batch_size: int, invoke, completed, cancellation: Cancellation) -> None:
    for offset in range(0, len(assignments), batch_size):
        cancellation.check()
        batch = assignments[offset:offset + batch_size]
        with ThreadPoolExecutor(max_workers=len(batch)) as executor:
            futures = {executor.submit(invoke, dict(item)): item for item in batch}
            for future in as_completed(futures):
                cancellation.check()
                item = futures[future]
                try:
                    item.update(future.result())
                except ReviewCancelled:
                    raise
                except Exception as error:
                    item.update(status="failed", error=str(error))
                completed(item)


def main(argv=None) -> int:
    argparse.ArgumentParser(description=__doc__).parse_args(argv)
    cancellation = Cancellation()
    output = Path(os.environ.get("CODEX_REVIEW_OUTPUT_ROOT", cache_root() / "runs")).resolve()
    run = output / ("review-" + str(uuid.uuid4()))
    status = {"status": "running", "startedAt": datetime.now(timezone.utc).isoformat(), "reviewers": []}
    active = []
    lock = threading.Lock()

    def register(child):
        with lock:
            cancellation.check()
            active.append(child)

    def unregister(child):
        with lock:
            active.remove(child)

    def terminate():
        with lock:
            children = list(active)
        for child in children:
            child.cancel()

    code = 1
    try:
        run.mkdir(parents=True)
        cancellation.install()
        save_json(run / "status.json", status)
        git_directory = run / "git"
        git_directory.mkdir()
        owner = OwnedProcesses(cancellation)
        try:
            git = GitComparison(owner, Path.cwd(), git_directory)
            repo, branch = discover(git)
            base_url = os.environ.get("API_URL", "http://localhost:5231")
            context, settings, project = load_review(base_url, repo, branch, cancellation)
            comparison = capture_diff(git, branch, context["originBranch"], run)
        finally:
            owner.close()
        status.update(comparison)
        status.update(featureId=context["feature"]["id"], planId=context["plan"]["id"], batchSize=settings["batchSize"])
        save_json(run / "settings.json", settings)
        assignments = collect_reviewers(context, settings, project,
            lambda skill_id: get_json(base_url, "/api/skills/" + skill_id, 30, cancellation))
        status["reviewers"] = assignments
        shared = build_shared_prompt(context, comparison, run)
        write_utf8(run / "shared-context.md", shared)
        for item in assignments:
            directory = run / "reviewers" / (item["source"] + "-" + item["sourceId"])
            directory.mkdir(parents=True)
            input_path = directory / "input.md"
            write_utf8(input_path, item.pop("input"))
            item.update(inputPath=str(input_path), outputDirectory=str(directory))
        save_json(run / "status.json", status)
        caches = {}
        for item in assignments:
            key = model_key(item)
            if key not in caches:
                directory = run / "caches" / key
                try:
                    seed = prepare_cache(shared, item, repo, directory, cancellation)
                    caches[key] = {"id": seed["threadId"], "path": str(directory / "cache.json")}
                except (ReviewCancelled, KeyboardInterrupt):
                    raise
                except Exception as error:
                    caches[key] = {"error": str(error)}
            cache = caches[key]
            if cache.get("error"):
                item.update(status="failed", error=cache["error"])
            else:
                item.update(cacheId=cache["id"], cacheMetadataPath=cache["path"])
                item["runnerParameters"] = runner_parameters(item, repo, comparison["headCommit"])
        cancellation.terminate = terminate
        pending = [item for item in assignments if item["status"] == "pending"]
        execute_batches(pending, settings["batchSize"],
            lambda item: invoke_reviewer(item, repo, comparison["headCommit"], cancellation, register, unregister),
            lambda item: save_json(run / "status.json", status), cancellation)
        cancellation.check()
        code = 1 if any(item["status"] != "completed" for item in assignments) else 0
        status["status"] = "completed" if code == 0 else "partial"
    except (ReviewCancelled, KeyboardInterrupt):
        cancellation.cancel()
        status["status"] = "cancelled"
        for item in status["reviewers"]:
            if item["status"] == "pending":
                item["status"] = "cancelled"
        code = 130
    except Exception as error:
        status.update(status="failed", error=str(error))
    finally:
        terminate()
        cancellation.close()
        status.update(exitCode=code, finishedAt=datetime.now(timezone.utc).isoformat(), outputDirectory=str(run))
        if run.is_dir():
            try:
                status["reportPath"] = str(write_report(run, status))
            except Exception as error:
                status["reportError"] = str(error)
            status["statusPath"] = str(run / "status.json")
            save_json(run / "status.json", status)
        print(json.dumps(status, ensure_ascii=True))
    return code


if __name__ == "__main__":
    raise SystemExit(main())
