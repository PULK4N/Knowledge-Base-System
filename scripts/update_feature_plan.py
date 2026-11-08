#!/usr/bin/env python3
"""Replace a feature plan's title and content through the FeatureModule REST API.

The content is read from a UTF-8 Markdown file and sent unchanged. The plan's
record links are kept. The API sets the status sent with the update, so pass
the plan's current status to keep it.

Example:
    ./scripts/update_feature_plan.py <featureId> <planId> plan.md \
        --title "Plan 3" --status InProgress
"""

from __future__ import annotations

import argparse
import json
import sys
import uuid
from pathlib import Path

from migrate_codex_skill import DEFAULT_BASE_URL, post_json

PLAN_STATUSES = ("NotStarted", "InProgress", "Delayed", "Completed")


def uuid_argument(value: str) -> str:
    try:
        return str(uuid.UUID(value))
    except ValueError as error:
        raise argparse.ArgumentTypeError(
            f"Invalid ID '{value}'. Expected a UUID."
        ) from error


def parse_args(argv: list[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Replace a feature plan's content from a UTF-8 Markdown file."
    )
    parser.add_argument("feature_id", type=uuid_argument)
    parser.add_argument("plan_id", type=uuid_argument)
    parser.add_argument("markdown_file", type=Path)
    parser.add_argument("--title", required=True)
    parser.add_argument("--status", required=True, choices=PLAN_STATUSES)
    parser.add_argument("--additional-info", default="")
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
    return parser.parse_args(argv)


def build_request(args: argparse.Namespace) -> tuple[str, dict[str, object]]:
    return (
        f"/api/features/{args.feature_id}/plans/{args.plan_id}/update",
        {
            "title": args.title,
            "content": args.markdown_file.read_text(encoding="utf-8"),
            "status": args.status,
            "additionalInfo": args.additional_info,
            "recordIds": None,
        },
    )


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
