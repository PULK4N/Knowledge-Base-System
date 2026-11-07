#!/usr/bin/env python3
"""Add general policies from Markdown files through the PolicyModule REST API.

Each file holds one policy: the first "# " heading is the title and the
remaining text is the description.

Examples:
    ./scripts/add_general_policy.py policies/01-some-policy.md
    ./scripts/add_general_policy.py policies/*.md --dry-run
"""

from __future__ import annotations

import argparse
import sys
import uuid
from pathlib import Path

from migrate_codex_skill import DEFAULT_BASE_URL, post_json


def policy_id(value: str) -> str:
    try:
        return str(uuid.UUID(value))
    except ValueError as error:
        raise argparse.ArgumentTypeError(
            f"Invalid policy ID '{value}'. Expected a UUID."
        ) from error


def parse_args(argv: list[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Add general policies from UTF-8 Markdown files."
    )
    parser.add_argument("markdown_files", type=Path, nargs="+")
    parser.add_argument(
        "--update",
        metavar="POLICY_ID",
        type=policy_id,
        help="Replace this existing policy with the single given file.",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Print the parsed title and description without posting.",
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


def read_policy_markdown(markdown_file: Path) -> tuple[str, str]:
    if markdown_file.suffix.lower() != ".md":
        raise ValueError(f"Expected a .md file: {markdown_file}")
    if not markdown_file.is_file():
        raise ValueError(f"Markdown file was not found: {markdown_file}")

    source = markdown_file.read_text(encoding="utf-8-sig").strip()
    first_line, _, body = source.partition("\n")
    if not first_line.startswith("# "):
        raise ValueError(
            f"First line must be a '# ' title heading: {markdown_file}"
        )

    title = first_line[2:].strip()
    description = body.strip()
    if not title or not description:
        raise ValueError(
            f"Policy needs a title and a description: {markdown_file}"
        )
    return title, description


def add_policy(
    markdown_file: Path,
    base_url: str,
    timeout: float,
) -> str:
    title, description = read_policy_markdown(markdown_file)
    endpoint = f"{base_url.rstrip('/')}/api/policies/general"
    response = post_json(
        endpoint,
        {"title": title, "description": description},
        timeout,
    )

    try:
        return str(uuid.UUID(str(response.get("policyId"))))
    except (ValueError, TypeError, AttributeError) as error:
        raise RuntimeError(
            f"Add response did not contain a valid policyId: {response}"
        ) from error


def update_policy(
    markdown_file: Path,
    policy_id: str,
    base_url: str,
    timeout: float,
) -> None:
    title, description = read_policy_markdown(markdown_file)
    endpoint = f"{base_url.rstrip('/')}/api/policies/general/update"
    post_json(
        endpoint,
        {"policyId": policy_id, "title": title, "description": description},
        timeout,
    )


def main(argv: list[str] | None = None) -> int:
    args = parse_args(argv)
    try:
        if args.update and len(args.markdown_files) != 1:
            raise ValueError("--update takes exactly one Markdown file.")

        for markdown_file in args.markdown_files:
            if args.update and not args.dry_run:
                update_policy(
                    markdown_file, args.update, args.base_url, args.timeout
                )
                print(f"{markdown_file.name}: updated {args.update}")
                continue

            if args.dry_run:
                title, description = read_policy_markdown(markdown_file)
                print(f"{markdown_file.name}: {title}\n{description}\n")
                continue

            policy_id = add_policy(markdown_file, args.base_url, args.timeout)
            print(f"{markdown_file.name}: {policy_id}")
        return 0
    except (OSError, UnicodeError, ValueError, RuntimeError) as error:
        print(f"Adding general policy failed: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
