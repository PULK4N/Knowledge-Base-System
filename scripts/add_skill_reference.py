#!/usr/bin/env python3
"""Add one Markdown reference to a skill through the SkillsModule REST API.

Examples:
    ./scripts/add_skill_reference.py SKILL_ID path/to/guide.md
    ./scripts/add_skill_reference.py SKILL_ID path/to/guide.md \
        --relative-path references/setup/guide.md \
        --load-automatically
"""

from __future__ import annotations

import argparse
import sys
import uuid
from pathlib import Path, PurePosixPath

from migrate_codex_skill import DEFAULT_BASE_URL, post_json


def skill_id(value: str) -> str:
    try:
        return str(uuid.UUID(value))
    except ValueError as error:
        raise argparse.ArgumentTypeError(
            f"Invalid skill ID '{value}'. Expected a UUID."
        ) from error


def parse_args(argv: list[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Add a UTF-8 Markdown reference to an existing skill."
    )
    parser.add_argument("skill_id", type=skill_id)
    parser.add_argument("markdown_file", type=Path)
    parser.add_argument(
        "--relative-path",
        help=(
            "Path stored in the skill. Defaults to references/<file name>."
        ),
    )
    parser.add_argument(
        "--load-automatically",
        action="store_true",
        help="Include this reference whenever the skill is loaded.",
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
    return parser.parse_args(argv)


def validate_markdown_file(markdown_file: Path) -> None:
    if markdown_file.suffix.lower() != ".md":
        raise ValueError(f"Expected a .md file: {markdown_file}")
    if not markdown_file.is_file():
        raise ValueError(f"Markdown file was not found: {markdown_file}")


def resolve_relative_path(
    markdown_file: Path,
    requested_path: str | None,
) -> str:
    value = requested_path or f"references/{markdown_file.name}"
    path = PurePosixPath(value)

    if (
        not value.strip()
        or path.is_absolute()
        or "\\" in value
        or ".." in path.parts
        or value.endswith("/")
    ):
        raise ValueError(
            "Reference path must be a non-empty relative POSIX file path "
            "without '..'."
        )
    return path.as_posix()


def add_reference(args: argparse.Namespace) -> str:
    validate_markdown_file(args.markdown_file)
    relative_path = resolve_relative_path(
        args.markdown_file,
        args.relative_path,
    )
    content = args.markdown_file.read_text(encoding="utf-8-sig")
    endpoint = (
        f"{args.base_url.rstrip('/')}/api/skills/"
        f"{args.skill_id}/references"
    )

    post_json(
        endpoint,
        {
            "relativePath": relative_path,
            "content": content,
            "loadAutomatically": args.load_automatically,
        },
        args.timeout,
    )
    return relative_path


def main(argv: list[str] | None = None) -> int:
    args = parse_args(argv)
    try:
        relative_path = add_reference(args)
        print(relative_path)
        return 0
    except (OSError, UnicodeError, ValueError, RuntimeError) as error:
        print(f"Adding skill reference failed: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
