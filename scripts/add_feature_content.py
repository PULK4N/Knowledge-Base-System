#!/usr/bin/env python3
"""Create features, feature plans and feature records through the FeatureModule REST API.

Plan and record content is read from a UTF-8 Markdown file and sent unchanged.

Examples:
    ./scripts/add_feature_content.py feature --project-id <id> --name "Name" --summary "..." --status "..."
    ./scripts/add_feature_content.py plan <featureId> plan.md --title "Plan 1" --branch feature/x
    ./scripts/add_feature_content.py plan <featureId> plan.md --title "Plan 2" --branch feature/y --previous-plan-id <planId>
    ./scripts/add_feature_content.py record <featureId> notes.md --type conversation --title "Q&A" --plan-id <planId>
"""

from __future__ import annotations

import argparse
import json
import sys
import uuid
from pathlib import Path

from migrate_codex_skill import DEFAULT_BASE_URL, post_json


def uuid_argument(value: str) -> str:
    try:
        return str(uuid.UUID(value))
    except ValueError as error:
        raise argparse.ArgumentTypeError(
            f"Invalid ID '{value}'. Expected a UUID."
        ) from error


def parse_args(argv: list[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Create feature content from UTF-8 Markdown files."
    )
    parser.add_argument(
        "--base-url",
        default=DEFAULT_BASE_URL,
        help=f"Knowledge Base API base URL (default: {DEFAULT_BASE_URL})",
    )
    parser.add_argument(
        "--timeout",
        type=float,
        default=30.0,
        help="HTTP timeout in seconds (default: 30)",
    )
    commands = parser.add_subparsers(dest="command", required=True)

    feature = commands.add_parser("feature", help="Create a feature.")
    feature.add_argument("--project-id", required=True, type=uuid_argument)
    feature.add_argument("--name", required=True)
    feature.add_argument("--summary", required=True)
    feature.add_argument("--status", required=True)
    feature.add_argument("--composite-branch")

    plan = commands.add_parser("plan", help="Add a plan from a Markdown file.")
    plan.add_argument("feature_id", type=uuid_argument)
    plan.add_argument("markdown_file", type=Path)
    plan.add_argument("--title", required=True)
    plan.add_argument("--branch", required=True)
    plan.add_argument("--branch-description", default="")
    plan.add_argument("--previous-plan-id", type=uuid_argument)
    plan.add_argument(
        "--status",
        default="NotStarted",
        choices=("NotStarted", "InProgress", "Delayed", "Completed"),
    )

    record = commands.add_parser("record", help="Add a record from a Markdown file.")
    record.add_argument("feature_id", type=uuid_argument)
    record.add_argument("markdown_file", type=Path)
    record.add_argument("--type", required=True, help="Lowercase record type, such as conversation or research.")
    record.add_argument("--title", required=True)
    record.add_argument(
        "--plan-id",
        dest="plan_ids",
        action="append",
        default=[],
        type=uuid_argument,
        help="Plan to link the record to; repeat for several plans.",
    )

    return parser.parse_args(argv)


def build_request(args: argparse.Namespace) -> tuple[str, dict[str, object]]:
    if args.command == "feature":
        return "/api/features", {
            "projectId": args.project_id,
            "name": args.name,
            "summary": args.summary,
            "status": args.status,
            "compositeBranchName": args.composite_branch,
        }

    content = args.markdown_file.read_text(encoding="utf-8")
    if args.command == "plan":
        return f"/api/features/{args.feature_id}/plans", {
            "title": args.title,
            "content": content,
            "status": args.status,
            "branchName": args.branch,
            "branchDescription": args.branch_description,
            "previousPlanId": args.previous_plan_id,
        }

    return f"/api/features/{args.feature_id}/records", {
        "type": args.type.strip().lower(),
        "title": args.title,
        "content": content,
        "planIds": args.plan_ids,
    }


def main(argv: list[str] | None = None) -> int:
    args = parse_args(argv)
    path, payload = build_request(args)
    result = post_json(
        args.base_url.rstrip("/") + path,
        payload,
        args.timeout,
    )
    print(json.dumps(result, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    sys.exit(main())
