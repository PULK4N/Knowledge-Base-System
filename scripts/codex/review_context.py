"""Discover a review from Git and KB, and snapshot each configured assignment."""
from __future__ import annotations

import hashlib
import json
import os
import urllib.parse
from pathlib import Path

from run_codex_reviewers import GitComparison, get_json, uuid_argument, write_utf8


def save_json(path: Path, value: dict) -> None:
    temporary = path.with_suffix(path.suffix + ".tmp")
    write_utf8(temporary, json.dumps(value, ensure_ascii=False, indent=2) + "\n")
    temporary.replace(path)


def discover(git: GitComparison) -> tuple[Path, str]:
    root = Path(git.text(["rev-parse", "--show-toplevel"])).resolve()
    branch = git.text(["symbolic-ref", "--quiet", "--short", "HEAD"])
    if not branch:
        raise ValueError("A named Git branch is required; detached HEAD cannot resolve a KB plan")
    git.repo = root
    return root, branch


def capture_diff(git: GitComparison, branch: str, origin: str, run: Path) -> dict:
    if not origin.strip() or origin.startswith("-"):
        raise ValueError("The KB origin must be a Git branch name")
    head = git.text(["rev-parse", "--verify", "HEAD^{commit}"])
    base = git.text(["rev-parse", "--verify", "--end-of-options", origin + "^{commit}"])
    patch = git.command(["diff", "--no-ext-diff", "--no-textconv", "--binary", base, head, "--"])
    (run / "committed.diff").write_bytes(patch.read_bytes())
    return {"repository": str(git.repo), "branch": branch, "originBranch": origin,
            "headCommit": head, "originCommit": base}


def load_review(base_url: str, repo: Path, branch: str, cancellation, timeout: float = 30) -> tuple[dict, dict, dict]:
    def get(path: str) -> dict:
        return get_json(base_url, path, timeout, cancellation)
    project = get("/api/policies/projects/by-repository?" +
                  urllib.parse.urlencode({"repositoryPath": str(repo)}))
    project_id = uuid_argument(project["projectId"])
    context = get("/api/features/review-context?" +
                  urllib.parse.urlencode({"projectId": project_id, "branchName": branch}))
    if uuid_argument(context["feature"]["projectId"]) != project_id:
        raise ValueError("Resolved feature does not belong to the repository's project")
    settings = get("/api/settings/code-review")
    if type(settings.get("batchSize")) is not int or settings["batchSize"] <= 0:
        raise ValueError("Administration batch size must be a positive integer")
    if not isinstance(settings.get("taskMessage"), str) or not settings["taskMessage"].strip():
        raise ValueError("Administration review task message is empty")
    project_review = get("/api/projects/" + project_id + "/review")
    policies = get("/api/policies?" + urllib.parse.urlencode(
        {"repositoryPath": str(repo), "agentFamily": "codex"}))
    if policies.get("status") != "OK" or not policies.get("policies", "").strip():
        raise ValueError("Applicable repository policies are unavailable")
    context["policies"] = policies["policies"]
    context["records"] = []
    context["skill"] = None
    if not isinstance(context.get("reviewNotes"), list) or not isinstance(context.get("reviewers"), list):
        raise ValueError("Review context is missing review notes or feature reviewers")
    return context, settings, project_review


def collect_reviewers(context: dict, settings: dict, project: dict, fetch_skill) -> list[dict]:
    result = []
    models = project.get("levelModels", {})
    for source, items in (("project", project.get("reviewSkills", [])),
                          ("feature", context["reviewers"])):
        for item in items:
            source_id = uuid_argument(item["skillId"] if source == "project" else item["id"])
            key = source + ":" + source_id
            model = models.get(item.get("reviewLevel"))
            if not model or not (model.get("model") or "").strip() or not (model.get("reasoning") or "").strip():
                raise ValueError("Missing project model mapping for " + key)
            title = (item.get("skillName") or source_id) if source == "project" else item["title"]
            if source == "project":
                skill = fetch_skill(source_id)
                if (skill.get("isDeleted") or str(skill.get("id", "")).lower() != source_id
                        or not skill.get("content") or skill.get("otherReferences")):
                    raise ValueError("Skill or full skill references are unavailable: " + source_id)
                parts = ["# Project reviewer: " + title, "## Skill", skill["content"]]
                references = skill.get("references", {})
                if not isinstance(references, dict):
                    raise ValueError("Invalid skill references for " + source_id)
                for path, reference in references.items():
                    parts.extend(["## Reference: " + path, reference["content"]])
                if (item.get("reviewNotes") or "").strip():
                    parts.extend(["## Reviewer note", item["reviewNotes"]])
            else:
                if not (item.get("description") or "").strip():
                    raise ValueError("Feature reviewer instructions are empty: " + source_id)
                parts = ["# Feature reviewer: " + title, "## Reviewer instructions", item["description"]]
            parts.extend(["## Review task", settings["taskMessage"]])
            result.append({"key": key, "source": source, "sourceId": source_id, "title": title,
                           "model": model["model"], "reasoningLevel": model["reasoning"],
                           "input": "\n\n".join(parts) + "\n", "status": "pending"})
    if len({item["key"] for item in result}) != len(result):
        raise ValueError("The KB returned duplicate reviewers")
    return result


def model_key(reviewer: dict) -> str:
    value = json.dumps([reviewer["model"], reviewer["reasoningLevel"]])
    return hashlib.sha256(value.encode("utf-8")).hexdigest()[:20]
