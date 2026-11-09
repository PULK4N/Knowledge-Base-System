#!/usr/bin/env python3
"""Create a feature plan or linked record from an approved UTF-8 file."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

from migrate_codex_skill import DEFAULT_BASE_URL, post_json
from update_feature_plan import uuid_argument


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("kind", choices=("plan", "record"))
    parser.add_argument("feature_id", type=uuid_argument)
    parser.add_argument("markdown_file", type=Path)
    parser.add_argument("--title", required=True)
    parser.add_argument("--branch-name", help="Required when creating a plan")
    parser.add_argument("--plan-id", action="append", type=uuid_argument,
                        default=[], help="Link a record to this plan; repeatable")
    parser.add_argument("--record-type", default="conversation")
    parser.add_argument("--additional-info", default="")
    parser.add_argument("--source-reference", default="")
    parser.add_argument("--base-url", default=DEFAULT_BASE_URL)
    parser.add_argument("--timeout", type=float, default=30.0)
    args = parser.parse_args(argv)
    if args.kind == "plan" and not args.branch_name:
        parser.error("Creating a plan requires --branch-name")
    if args.kind == "plan" and args.plan_id:
        parser.error("--plan-id links records, not plans")
    if args.kind == "record" and args.branch_name:
        parser.error("--branch-name is only valid for plans")

    payload: dict[str, object] = {
        "title": args.title,
        "content": args.markdown_file.read_text(encoding="utf-8"),
    }
    if args.kind == "plan":
        suffix = "plans"
        payload.update({
            "branchName": args.branch_name,
            "contentType": "Markdown",
            "status": "NotStarted",
            "additionalInfo": args.additional_info,
        })
    else:
        suffix = "records"
        payload.update({
            "type": args.record_type,
            "planIds": args.plan_id,
            "sourceType": "Other",
            "sourceReference": args.source_reference,
        })

    result = post_json(
        f"{args.base_url.rstrip('/')}/api/features/{args.feature_id}/{suffix}",
        payload,
        args.timeout,
    )
    print(json.dumps(result, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
