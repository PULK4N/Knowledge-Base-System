#!/usr/bin/env python3
"""Replace a feature record's content from an approved UTF-8 file."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

from migrate_codex_skill import DEFAULT_BASE_URL, post_json
from update_feature_plan import uuid_argument


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("feature_id", type=uuid_argument)
    parser.add_argument("record_id", type=uuid_argument)
    parser.add_argument("markdown_file", type=Path)
    parser.add_argument("--title", required=True)
    parser.add_argument("--record-type", default="conversation")
    parser.add_argument("--source-type", default="Other")
    parser.add_argument("--source-reference", default="")
    parser.add_argument("--base-url", default=DEFAULT_BASE_URL)
    parser.add_argument("--timeout", type=float, default=30.0)
    args = parser.parse_args(argv)

    payload = {
        "recordId": args.record_id,
        "type": args.record_type,
        "title": args.title,
        "content": args.markdown_file.read_text(encoding="utf-8"),
        "sourceType": args.source_type,
        "sourceReference": args.source_reference,
    }
    result = post_json(
        f"{args.base_url.rstrip('/')}/api/features/{args.feature_id}/records/update",
        payload,
        args.timeout,
    )
    print(json.dumps(result, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
