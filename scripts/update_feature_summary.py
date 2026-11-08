#!/usr/bin/env python3
"""Replace a feature overview with the unchanged content of a UTF-8 file."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

from migrate_codex_skill import DEFAULT_BASE_URL, post_json
from update_feature_plan import uuid_argument


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("feature_id", type=uuid_argument)
    parser.add_argument("markdown_file", type=Path)
    parser.add_argument("--base-url", default=DEFAULT_BASE_URL)
    parser.add_argument("--timeout", type=float, default=30.0)
    args = parser.parse_args(argv)

    result = post_json(
        f"{args.base_url.rstrip('/')}/api/features/{args.feature_id}/summary",
        {"summary": args.markdown_file.read_text(encoding="utf-8")},
        args.timeout,
    )
    print(json.dumps(result, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
