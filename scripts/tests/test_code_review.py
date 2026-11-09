"""Behavior checks for discovery, batching, snapshots and direct reviewer execution."""
from __future__ import annotations

import contextlib
import io
import json
import os
import subprocess
import sys
import tempfile
import threading
import time
import unittest
import uuid
from pathlib import Path
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "codex"))
import cached_reviewer
import run_code_review
from review_context import capture_diff, collect_reviewers, discover, model_key
from review_process import Cancellation, OwnedProcesses, ReviewCancelled
from review_execution import ReviewerProcesses
from run_codex_reviewers import GitComparison


class CodeReviewTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.repo = self.root / "repo"
        self.repo.mkdir()
        self.git("init", "-q")
        self.git("config", "user.email", "review-test@example.invalid")
        self.git("config", "user.name", "Reviewer test")
        (self.repo / "code.txt").write_text("original\n")
        self.git("add", ".")
        self.git("commit", "-qm", "initial")
        self.git("branch", "-M", "origin-branch")
        self.git("checkout", "-qb", "implementation")

    def git(self, *arguments):
        return subprocess.check_output(["git", "-C", str(self.repo), *arguments], stderr=subprocess.STDOUT).decode().strip()

    def context(self):
        return {"feature": {"id": str(uuid.uuid4()), "projectId": str(uuid.uuid4()), "summary": "Shared overview"},
                "plan": {"id": str(uuid.uuid4()), "content": "Shared plan"}, "originBranch": "origin-branch",
                "reviewNotes": [], "reviewers": [{"id": str(uuid.uuid4()), "title": "Feature checks",
                    "description": "Unique feature instruction", "reviewLevel": "Easy"}],
                "policies": "Review read only", "records": [], "skill": None}

    def project(self):
        return {"levelModels": {"Easy": {"model": "test-model", "reasoning": "high"}},
                "reviewSkills": [{"skillId": str(uuid.uuid4()), "skillName": "Project skill", "reviewLevel": "Easy",
                                  "reviewNotes": "Unique project note"}]}

    def skill(self, skill_id):
        return {"id": skill_id, "content": "Unique skill body", "references": {"details.md": {"content": "Reference body"}}}

    def test_diff_uses_both_branch_tips_and_excludes_working_tree(self):
        self.git("checkout", "origin-branch")
        (self.repo / "source-only.txt").write_text("source-only\n")
        self.git("add", ".")
        self.git("commit", "-qm", "source advanced")
        self.git("checkout", "implementation")
        (self.repo / "code.txt").write_text("uncommitted change\n")
        work = self.root / "git-output"
        work.mkdir()
        cancellation = Cancellation()
        owner = OwnedProcesses(cancellation)
        self.addCleanup(owner.close)
        git = GitComparison(owner, self.repo, work)
        _, branch = discover(git)
        result = capture_diff(git, branch, "origin-branch", self.root)
        diff = (self.root / "committed.diff").read_text()
        self.assertIn("source-only.txt", diff)
        self.assertNotIn("uncommitted change", diff)
        self.assertEqual(self.git("rev-parse", "origin-branch"), result["originCommit"])

    def test_assignments_have_distinct_sources_and_task_suffix(self):
        context, project = self.context(), self.project()
        context["reviewers"][0]["id"] = project["reviewSkills"][0]["skillId"]
        values = collect_reviewers(context, {"taskMessage": "Configured task"}, project, self.skill)
        self.assertEqual(["project", "feature"], [value["source"] for value in values])
        self.assertNotEqual(values[0]["key"], values[1]["key"])
        self.assertIn("Reference body", values[0]["input"])
        self.assertIn("Unique project note", values[0]["input"])
        self.assertNotIn("Unique skill body", values[1]["input"])
        self.assertTrue(all(value["input"].endswith("Configured task\n") for value in values))
        self.assertEqual(model_key(values[0]), model_key(values[1]))
        self.assertNotEqual(model_key(values[0]), model_key(dict(values[0], reasoningLevel="low")))
        self.assertNotEqual(model_key(values[0]), model_key(dict(values[0], model="another")))

    def test_missing_model_or_incomplete_skill_is_a_setup_failure(self):
        for invalid_skill in (False, True):
            with self.subTest(invalid_skill=invalid_skill):
                project = self.project()
                if not invalid_skill:
                    project["levelModels"] = {}
                with self.assertRaises(ValueError):
                    collect_reviewers(self.context(), {"taskMessage": "Review"}, project,
                        lambda skill_id: dict(self.skill(skill_id), otherReferences=["missing.md"]))

    def test_batches_wait_for_the_entire_batch_and_continue_after_failure(self):
        barrier = threading.Barrier(5)
        lock = threading.Lock()
        finished = set()
        maximum = [0]
        active = [0]
        def invoke(item):
            with lock:
                active[0] += 1
                maximum[0] = max(maximum[0], active[0])
                if item["index"] >= 5:
                    self.assertTrue(set(range(5)).issubset(finished))
            if item["index"] < 5:
                barrier.wait(timeout=5)
            with lock:
                active[0] -= 1
                finished.add(item["index"])
            if item["index"] == 1:
                raise RuntimeError("simulated reviewer failure")
            return dict(item, status="completed")
        items = [{"index": index} for index in range(7)]
        run_code_review.execute_batches(items, 5, invoke, lambda item: None, Cancellation())
        self.assertEqual(5, maximum[0])
        self.assertEqual("failed", items[1]["status"])
        self.assertEqual("completed", items[-1]["status"])

    def test_coordinator_snapshots_inputs_and_reports_partial_results(self):
        context, project = self.context(), self.project()
        caches = []
        def prepare(shared, assignment, repo, directory, cancellation):
            self.assertIn("Shared overview", shared)
            self.assertNotIn("Unique project note", shared)
            self.assertNotIn("Unique feature instruction", shared)
            directory.mkdir(parents=True)
            caches.append(assignment["model"])
            (directory / "cache.json").write_text("{}")
            return {"threadId": str(uuid.uuid4())}
        def invoke(item, *unused):
            if item["source"] == "feature":
                return dict(item, status="failed", error="simulated failure")
            findings = Path(item["outputDirectory"]) / "findings.md"
            findings.write_text("Nothing found")
            return dict(item, status="completed", result={"findingsPath": str(findings)})
        previous = Path.cwd()
        try:
            os.chdir(self.repo)
            with patch.dict(os.environ, {"CODEX_REVIEW_OUTPUT_ROOT": str(self.root / "runs")}), \
                 patch.object(run_code_review, "load_review", return_value=(context, {"batchSize": 5, "taskMessage": "Final task"}, project)), \
                 patch.object(run_code_review, "get_json", side_effect=lambda url, path, timeout, cancellation: self.skill(path.split("/")[-1])), \
                 patch.object(run_code_review, "prepare_cache", side_effect=prepare), \
                 patch.object(run_code_review, "invoke_reviewer", side_effect=invoke), \
                 contextlib.redirect_stdout(io.StringIO()) as output:
                code = run_code_review.main([])
            result = json.loads(output.getvalue())
        finally:
            os.chdir(previous)
        self.assertEqual(1, code)
        self.assertEqual("partial", result["status"])
        self.assertEqual(1, len(caches))
        report = Path(result["reportPath"]).read_text()
        self.assertIn("Nothing found", report)
        self.assertIn("simulated failure", report)
        self.assertTrue(all(Path(item["inputPath"]).is_file() for item in result["reviewers"]))
        self.assertTrue(all("--cache-id" in item["runnerParameters"] for item in result["reviewers"]))

    def test_direct_runner_uses_saved_input_without_kb_and_keeps_attempts(self):
        fake = self.root / "fake_codex.py"
        fake.write_text('import json,sys\nassert "Saved reviewer instructions" in sys.stdin.read()\n'
                        'print(json.dumps({"type":"item.completed","item":{"type":"agent_message","text":"Nothing found"}}))\n'
                        'print(json.dumps({"type":"turn.completed","usage":{"input_tokens":100,"cached_input_tokens":80,"output_tokens":3}}))\n')
        prompt = self.root / "input.md"
        prompt.write_text("Saved reviewer instructions")
        params = ["--cache-id", str(uuid.uuid4()), "--model", "test-model", "--reasoning-level", "high",
                  "--input-file", str(prompt), "--output-dir", str(self.root / "attempts"),
                  "--repo", str(self.repo), "--expected-head", self.git("rev-parse", "HEAD")]
        for attempt in range(2):
            with patch.object(cached_reviewer, "native_command", return_value=[sys.executable, str(fake), "-"]), \
                 contextlib.redirect_stdout(io.StringIO()) as output:
                code = cached_reviewer.main(params)
            self.assertEqual(0, code, output.getvalue())
            self.assertEqual("Nothing found", Path(json.loads(output.getvalue())["findingsPath"]).read_text())
        self.assertEqual(2, len(list((self.root / "attempts").glob("attempt-*"))))
        with patch.object(cached_reviewer, "native_command") as spawn, contextlib.redirect_stdout(io.StringIO()) as output:
            code = cached_reviewer.main(params[:-1] + ["0" * 40])
        self.assertEqual(1, code)
        self.assertIn("HEAD changed", output.getvalue())
        spawn.assert_not_called()

    @unittest.skipIf(os.name == "nt", "POSIX process-group cleanup; Windows uses Job Objects")
    def test_coordinator_cancellation_lets_runner_reap_its_child(self):
        wrapper = self.root / "wrapper.py"
        pid_file = self.root / "child.pid"
        modules = Path(__file__).resolve().parents[1] / "codex"
        wrapper.write_text(
            'import sys\nfrom pathlib import Path\nsys.path.insert(0, ' + repr(str(modules)) + ')\n'
            'from review_process import Cancellation, OwnedProcesses, ReviewCancelled\n'
            'c=Cancellation()\nc.install()\nowner=OwnedProcesses(c)\nroot=Path(' + repr(str(self.root)) + ')\n'
            'try:\n    owner.run([sys.executable,"-c","import time; time.sleep(60)"],root,root/"child.out",root/"child.err",started=lambda pid:(root/"child.pid").write_text(str(pid)))\n'
            'except ReviewCancelled:\n    pass\nfinally:\n    owner.close()\n    c.close()\n', encoding="utf-8")
        cancellation = Cancellation()
        owner = ReviewerProcesses(cancellation)
        errors = []
        def run():
            try:
                owner.run([sys.executable, str(wrapper)], self.root, self.root / "wrapper.out", self.root / "wrapper.err")
            except Exception as error:
                errors.append(error)
        thread = threading.Thread(target=run)
        thread.start()
        try:
            deadline = time.monotonic() + 5
            while (not pid_file.exists() or not pid_file.read_text().strip()) and time.monotonic() < deadline:
                time.sleep(.01)
            self.assertTrue(pid_file.exists())
            child_pid = int(pid_file.read_text())
            cancellation.cancel()
            thread.join(timeout=5)
            self.assertFalse(thread.is_alive())
            self.assertTrue(all(isinstance(error, ReviewCancelled) for error in errors), errors)
            with self.assertRaises(ProcessLookupError):
                os.kill(child_pid, 0)
        finally:
            cancellation.cancel()
            thread.join(timeout=5)
            owner.close()


if __name__ == "__main__":
    unittest.main()
