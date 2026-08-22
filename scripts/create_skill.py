#!/usr/bin/env python3
"""Create one skill from a Markdown file through the SkillsModule REST API.

Examples:
    ./scripts/create_skill.py path/to/SKILL.md
    ./scripts/create_skill.py notes.md \
        --name example-skill \
        --description "Use these project notes" \
        --tag project \
        --tag notes
"""

from __future__ import annotations

import argparse
import sys
import uuid
from pathlib import Path

from migrate_codex_skill import (
    DEFAULT_BASE_URL,
    parse_skill_markdown,
    post_json,
)


def parse_args(argv: list[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Create a skill from a UTF-8 Markdown file."
    )
    parser.add_argument("markdown_file", type=Path)
    parser.add_argument(
        "--name",
        help="Skill name. Defaults to the Markdown frontmatter name.",
    )
    parser.add_argument(
        "--description",
        help=(
            "Skill description. Defaults to the Markdown frontmatter "
            "description."
        ),
    )
    parser.add_argument(
        "--tag",
        dest="tags",
        action="append",
        help=(
            "Skill tag. Repeat for multiple tags. When supplied, these "
            "replace frontmatter tags."
        ),
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


def read_skill_markdown(
    markdown_file: Path,
) -> tuple[dict[str, str | list[str]], str]:
    validate_markdown_file(markdown_file)
    source = markdown_file.read_text(encoding="utf-8-sig")

    if source.splitlines()[:1] == ["---"]:
        return parse_skill_markdown(markdown_file)

    return {}, source.strip()


def validate_markdown_file(markdown_file: Path) -> None:
    if markdown_file.suffix.lower() != ".md":
        raise ValueError(f"Expected a .md file: {markdown_file}")
    if not markdown_file.is_file():
        raise ValueError(f"Markdown file was not found: {markdown_file}")


def resolve_tags(
    command_line_tags: list[str] | None,
    metadata: dict[str, str | list[str]],
) -> list[str]:
    if command_line_tags is not None:
        return [tag.strip() for tag in command_line_tags if tag.strip()]

    metadata_tags = metadata.get("tags", [])
    if isinstance(metadata_tags, str):
        return [tag.strip() for tag in metadata_tags.split(",") if tag.strip()]
    return [tag.strip() for tag in metadata_tags if tag.strip()]


def create_skill(args: argparse.Namespace) -> str:
    metadata, content = read_skill_markdown(args.markdown_file)
    name = (args.name or metadata.get("name") or "").strip()
    description = (
        args.description or metadata.get("description") or ""
    ).strip()

    if not name:
        raise ValueError(
            "Skill name is required. Add frontmatter name or pass --name."
        )
    if not description:
        raise ValueError(
            "Skill description is required. Add frontmatter description or "
            "pass --description."
        )

    endpoint = f"{args.base_url.rstrip('/')}/api/skills"
    response = post_json(
        endpoint,
        {
            "name": name,
            "description": description,
            "content": content,
            "tags": resolve_tags(args.tags, metadata),
            "references": {},
        },
        args.timeout,
    )

    try:
        return str(uuid.UUID(str(response.get("skillId"))))
    except (ValueError, TypeError, AttributeError) as error:
        raise RuntimeError(
            f"Create response did not contain a valid skillId: {response}"
        ) from error


def main(argv: list[str] | None = None) -> int:
    args = parse_args(argv)
    try:
        skill_id = create_skill(args)
        print(skill_id)
        return 0
    except (OSError, UnicodeError, ValueError, RuntimeError) as error:
        print(f"Skill creation failed: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
