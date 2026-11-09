"""Exercise the full coordinator with a fixture KB and the authenticated native CLI.

Run from the repository root. Requires Git and an existing native Codex login.
CODEX_REVIEW_TEST_MODEL and CODEX_REVIEW_TEST_REASONING select the fixture mapping.
Artifacts are retained in the printed temporary directory for inspection.
"""
from __future__ import annotations

import json
import os
import subprocess
import sys
import tempfile
import threading
import urllib.parse
import uuid
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path


def main() -> int:
    root = Path(tempfile.mkdtemp(prefix="code-review-integration-"))
    repo = root / "checkout"
    repo.mkdir()
    def git(*args):
        subprocess.run(["git", "-C", str(repo), *args], check=True, capture_output=True)
    git("init", "-q")
    git("config", "user.email", "review-test@example.invalid")
    git("config", "user.name", "Review integration test")
    (repo / "totals.py").write_text("def total(values):\n    return sum(values)\n", encoding="utf-8")
    git("add", ".")
    git("commit", "-qm", "initial")
    git("branch", "-M", "source")
    git("checkout", "-qb", "implementation")
    with (repo / "totals.py").open("a", encoding="utf-8") as stream:
        stream.write("\n\ndef double_total(values):\n    return total(values) * 2\n")
    git("add", ".")
    git("commit", "-qm", "add double total")
    project_id, feature_id, plan_id, skill_id, reviewer_id = [str(uuid.uuid4()) for _ in range(5)]
    model = os.environ.get("CODEX_REVIEW_TEST_MODEL", "gpt-6-luna")
    reasoning = os.environ.get("CODEX_REVIEW_TEST_REASONING", "xhigh")
    task = ("Review the supplied diff using your assigned skill or feature reviewer instructions. "
            "Report concrete errors with file and line. If there are no errors, return Nothing found. "
            "Do not modify code or run tests.")
    documents = {
        "/api/policies/projects/by-repository": {"projectId": project_id},
        "/api/features/review-context": {
            "feature": {"id": feature_id, "projectId": project_id, "name": "Totals", "summary": "Provide correct arithmetic totals.", "status": "In progress"},
            "plan": {"id": plan_id, "title": "Double totals", "content": "Add double_total(values), returning twice the sum, including zero for an empty list."},
            "originBranch": "source", "reviewNotes": [],
            "reviewers": [{"id": reviewer_id, "title": "Empty totals", "description": "Check that the new function returns zero for an empty list.", "reviewLevel": "Easy"}]},
        "/api/settings/code-review": {"batchSize": 5, "taskMessage": task},
        "/api/projects/" + project_id + "/review": {
            "levelModels": {"Easy": {"model": model, "reasoning": reasoning}},
            "reviewSkills": [{"skillId": skill_id, "skillName": "Arithmetic correctness", "reviewNotes": "Pay attention to multiplication.", "reviewLevel": "Easy"}]},
        "/api/skills/" + skill_id: {"id": skill_id, "content": "Review arithmetic functions for incorrect addition, multiplication, or off-by-one mistakes.", "references": {}},
        "/api/policies": {"status": "OK", "policies": "Review read only. Do not run builds, edit files, fetch KB context, or spawn other agents."},
    }
    class Handler(BaseHTTPRequestHandler):
        def do_GET(self):
            document = documents.get(urllib.parse.urlparse(self.path).path)
            self.send_response(200 if document else 404)
            self.send_header("Content-Type", "application/json")
            self.end_headers()
            self.wfile.write(json.dumps(document or {}).encode("utf-8"))
        def log_message(self, *args):
            pass
    server = ThreadingHTTPServer(("127.0.0.1", 0), Handler)
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    environment = dict(os.environ, API_URL="http://127.0.0.1:" + str(server.server_port),
                       CODEX_REVIEW_OUTPUT_ROOT=str(root / "runs"), CODEX_REVIEW_CACHE_ROOT=str(root / "cache"))
    coordinator = Path(__file__).resolve().parents[1] / "codex" / "run_code_review.py"
    try:
        result = subprocess.run([sys.executable, str(coordinator)], cwd=repo, env=environment, text=True, capture_output=True)
    finally:
        server.shutdown()
        server.server_close()
        thread.join()
    (root / "coordinator.stdout").write_text(result.stdout, encoding="utf-8")
    (root / "coordinator.stderr").write_text(result.stderr, encoding="utf-8")
    status = json.loads(result.stdout)
    assert len(status["reviewers"]) == 2, status
    assert len({item.get("cacheId") for item in status["reviewers"]}) == 1, status
    assert Path(status["reportPath"]).is_file(), status
    print(json.dumps({"exitCode": result.returncode, "artifacts": str(root), "report": status["reportPath"],
                      "reviewers": [{"source": item["source"], "status": item["status"], "error": item.get("error"),
                                     "cacheUsage": item.get("result", {}).get("cacheUsage")} for item in status["reviewers"]]}))
    return result.returncode


if __name__ == "__main__":
    raise SystemExit(main())
