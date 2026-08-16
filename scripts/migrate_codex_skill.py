#!/usr/bin/env python3
"""Migrate one installed Codex skill through the SkillsModule REST API.

Examples:
    python3 scripts/migrate_codex_skill.py angular-code-writter
    python3 scripts/migrate_codex_skill.py angular-code-writter --execute
    python3 scripts/migrate_codex_skill.py imagegen --execute
"""

from __future__ import annotations

import argparse
import json
import mimetypes
import os
import re
import sys
import textwrap
import urllib.error
import urllib.request
import uuid
from dataclasses import dataclass
from pathlib import Path
from typing import Callable


DEFAULT_SKILLS_ROOT = Path.home() / ".codex" / "skills"
DEFAULT_BASE_URL = "http://localhost:5231"
SKILL_FILE_NAME = "SKILL.md"
ATTACHMENT_DIRECTORIES = frozenset({"assets", "attachments"})
IGNORED_DIRECTORIES = frozenset(
    {".git", ".svn", "__pycache__", "node_modules"}
)
IGNORED_FILES = frozenset({".DS_Store"})


@dataclass(frozen=True)
class TextReference:
    relative_path: str
    content: str


@dataclass(frozen=True)
class AttachmentFile:
    relative_path: str
    source_path: Path
    file_type: str
    size: int


@dataclass(frozen=True)
class SkillPackage:
    source_directory: Path
    name: str
    description: str
    content: str
    tags: tuple[str, ...]
    references: tuple[TextReference, ...]
    attachments: tuple[AttachmentFile, ...]


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description=(
            "Migrate one skill from ~/.codex/skills through the "
            "SkillsModule REST controller. Without --execute, only the "
            "migration plan is shown."
        )
    )
    parser.add_argument(
        "skill_name",
        help=(
            "Installed skill name. Direct skills and .system skills are "
            "supported."
        ),
    )
    parser.add_argument(
        "--skills-root",
        type=Path,
        default=DEFAULT_SKILLS_ROOT,
        help=f"Installed skill root (default: {DEFAULT_SKILLS_ROOT})",
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
    parser.add_argument(
        "--execute",
        action="store_true",
        help="Perform REST writes; otherwise only print the migration plan",
    )
    return parser.parse_args()


def resolve_skill_directory(skills_root: Path, skill_name: str) -> Path:
    if (
        not skill_name
        or skill_name in {".", ".."}
        or Path(skill_name).name != skill_name
        or "/" in skill_name
        or "\\" in skill_name
    ):
        raise ValueError("Skill name must be one directory name.")

    root = skills_root.expanduser()
    candidates = [root / skill_name, root / ".system" / skill_name]
    exact_matches = [
        candidate
        for candidate in candidates
        if candidate.is_dir() and (candidate / SKILL_FILE_NAME).is_file()
    ]

    if exact_matches:
        skill_directory = exact_matches[0]
    else:
        metadata_matches: list[Path] = []
        search_roots = [root, root / ".system"]
        for search_root in search_roots:
            if not search_root.is_dir():
                continue
            for candidate in sorted(search_root.iterdir()):
                skill_file = candidate / SKILL_FILE_NAME
                if not candidate.is_dir() or not skill_file.is_file():
                    continue
                try:
                    metadata, _ = parse_skill_markdown(skill_file)
                except (OSError, UnicodeError, ValueError):
                    continue
                if metadata["name"] == skill_name:
                    metadata_matches.append(candidate)

        if len(metadata_matches) != 1:
            if metadata_matches:
                locations = ", ".join(str(path) for path in metadata_matches)
                raise ValueError(
                    f"Skill name '{skill_name}' is ambiguous: {locations}"
                )
            raise ValueError(
                f"Skill '{skill_name}' was not found below {root}."
            )
        skill_directory = metadata_matches[0]

    if skill_directory.is_symlink():
        raise ValueError(
            f"Refusing to migrate symlinked skill directory: {skill_directory}"
        )

    return skill_directory


def parse_skill_markdown(
    skill_file: Path,
) -> tuple[dict[str, str | list[str]], str]:
    source = skill_file.read_text(encoding="utf-8-sig")
    lines = source.splitlines()
    if not lines or lines[0].strip() != "---":
        raise ValueError(f"{skill_file} has no YAML frontmatter.")

    try:
        closing_index = next(
            index
            for index, line in enumerate(lines[1:], start=1)
            if line.strip() == "---"
        )
    except StopIteration as error:
        raise ValueError(
            f"{skill_file} has unterminated YAML frontmatter."
        ) from error

    metadata = parse_flat_frontmatter(lines[1:closing_index])
    name = metadata.get("name")
    description = metadata.get("description")
    if not isinstance(name, str) or not name.strip():
        raise ValueError(f"{skill_file} frontmatter requires a name.")
    if not isinstance(description, str) or not description.strip():
        raise ValueError(f"{skill_file} frontmatter requires a description.")

    metadata["name"] = name.strip()
    metadata["description"] = description.strip()
    content = "\n".join(lines[closing_index + 1 :]).strip()
    return metadata, content


def parse_flat_frontmatter(
    lines: list[str],
) -> dict[str, str | list[str]]:
    metadata: dict[str, str | list[str]] = {}
    index = 0

    while index < len(lines):
        line = lines[index]
        if not line.strip() or line.lstrip().startswith("#"):
            index += 1
            continue
        if line[:1].isspace():
            index += 1
            continue

        match = re.match(r"^([A-Za-z][A-Za-z0-9_-]*):(?:\s*(.*))?$", line)
        if not match:
            index += 1
            continue

        key = match.group(1)
        raw_value = (match.group(2) or "").strip()
        index += 1

        if raw_value[:1] in {"|", ">"}:
            block_lines: list[str] = []
            while index < len(lines):
                next_line = lines[index]
                if next_line and not next_line[:1].isspace():
                    break
                block_lines.append(next_line)
                index += 1
            block = textwrap.dedent("\n".join(block_lines)).strip()
            metadata[key] = (
                fold_yaml_block(block)
                if raw_value.startswith(">")
                else block
            )
            continue

        if not raw_value:
            list_values: list[str] = []
            while index < len(lines):
                next_line = lines[index]
                if next_line and not next_line[:1].isspace():
                    break
                stripped = next_line.strip()
                if stripped.startswith("- "):
                    list_values.append(parse_scalar(stripped[2:].strip()))
                index += 1
            metadata[key] = list_values
            continue

        metadata[key] = (
            parse_inline_list(raw_value)
            if key == "tags" and raw_value.startswith("[")
            else parse_scalar(raw_value)
        )

    return metadata


def parse_scalar(value: str) -> str:
    if len(value) >= 2 and value[0] == value[-1] == '"':
        try:
            parsed = json.loads(value)
        except json.JSONDecodeError:
            return value[1:-1]
        return str(parsed)
    if len(value) >= 2 and value[0] == value[-1] == "'":
        return value[1:-1].replace("''", "'")
    return value


def parse_inline_list(value: str) -> list[str]:
    try:
        parsed = json.loads(value)
    except json.JSONDecodeError:
        parsed = [
            parse_scalar(item.strip())
            for item in value[1:-1].split(",")
            if item.strip()
        ]
    if not isinstance(parsed, list):
        raise ValueError(f"Expected a YAML list, received: {value}")
    return [str(item).strip() for item in parsed if str(item).strip()]


def fold_yaml_block(value: str) -> str:
    paragraphs = re.split(r"\n\s*\n", value)
    return "\n".join(
        " ".join(line.strip() for line in paragraph.splitlines())
        for paragraph in paragraphs
    )


def load_skill_package(
    skills_root: Path,
    skill_name: str,
) -> SkillPackage:
    source_directory = resolve_skill_directory(skills_root, skill_name)
    metadata, content = parse_skill_markdown(
        source_directory / SKILL_FILE_NAME
    )
    references: list[TextReference] = []
    attachments: list[AttachmentFile] = []

    for source_path in iter_skill_files(source_directory):
        relative_path = source_path.relative_to(source_directory).as_posix()
        if relative_path == SKILL_FILE_NAME:
            continue

        path_parts = Path(relative_path).parts
        top_directory = path_parts[0].lower() if len(path_parts) > 1 else ""
        file_bytes = source_path.read_bytes()

        if top_directory in ATTACHMENT_DIRECTORIES:
            attachments.append(
                create_attachment(source_path, relative_path, len(file_bytes))
            )
            continue

        try:
            text = file_bytes.decode("utf-8-sig")
        except UnicodeDecodeError:
            if top_directory == "references":
                raise ValueError(
                    f"Reference is not UTF-8 text: {relative_path}"
                )
            attachments.append(
                create_attachment(source_path, relative_path, len(file_bytes))
            )
            continue

        if "\x00" in text:
            if top_directory == "references":
                raise ValueError(
                    f"Reference contains binary data: {relative_path}"
                )
            attachments.append(
                create_attachment(source_path, relative_path, len(file_bytes))
            )
            continue

        references.append(TextReference(relative_path, text))

    tags_value = metadata.get("tags", [])
    if isinstance(tags_value, str):
        tags = tuple(
            tag.strip() for tag in tags_value.split(",") if tag.strip()
        )
    else:
        tags = tuple(tags_value)

    return SkillPackage(
        source_directory=source_directory,
        name=str(metadata["name"]),
        description=str(metadata["description"]),
        content=content,
        tags=tags,
        references=tuple(
            sorted(references, key=lambda reference: reference.relative_path)
        ),
        attachments=tuple(
            sorted(
                attachments,
                key=lambda attachment: attachment.relative_path,
            )
        ),
    )


def iter_skill_files(source_directory: Path) -> list[Path]:
    files: list[Path] = []
    for current_root, directory_names, file_names in os.walk(
        source_directory,
        followlinks=False,
    ):
        current_directory = Path(current_root)
        for directory_name in list(directory_names):
            directory_path = current_directory / directory_name
            if directory_path.is_symlink():
                raise ValueError(
                    f"Refusing to follow symlinked directory: {directory_path}"
                )
        directory_names[:] = sorted(
            directory_name
            for directory_name in directory_names
            if directory_name not in IGNORED_DIRECTORIES
        )

        for file_name in sorted(file_names):
            if file_name in IGNORED_FILES:
                continue
            source_path = current_directory / file_name
            if source_path.is_symlink():
                raise ValueError(
                    f"Refusing to migrate symlinked file: {source_path}"
                )
            if source_path.is_file():
                files.append(source_path)

    return sorted(
        files,
        key=lambda path: path.relative_to(source_directory).as_posix(),
    )


def create_attachment(
    source_path: Path,
    relative_path: str,
    size: int,
) -> AttachmentFile:
    file_type = (
        mimetypes.guess_type(source_path.name)[0]
        or "application/octet-stream"
    )
    return AttachmentFile(
        relative_path=relative_path,
        source_path=source_path,
        file_type=file_type,
        size=size,
    )


def post_json(
    endpoint: str,
    payload: dict[str, object],
    timeout: float,
) -> dict[str, object]:
    request = urllib.request.Request(
        endpoint,
        data=json.dumps(payload, ensure_ascii=False).encode("utf-8"),
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    return open_json_request(request, endpoint, timeout)


def upload_attachment(
    endpoint: str,
    attachment: AttachmentFile,
    timeout: float,
) -> dict[str, object] | list[object]:
    boundary = f"----codex-skill-{uuid.uuid4().hex}"
    file_name = safe_form_value(attachment.source_path.name)
    body = b"".join(
        [
            f"--{boundary}\r\n".encode("ascii"),
            (
                'Content-Disposition: form-data; name="Files"; '
                f'filename="{file_name}"\r\n'
            ).encode("utf-8"),
            f"Content-Type: {attachment.file_type}\r\n\r\n".encode(
                "ascii"
            ),
            attachment.source_path.read_bytes(),
            b"\r\n",
            f"--{boundary}--\r\n".encode("ascii"),
        ]
    )
    request = urllib.request.Request(
        endpoint,
        data=body,
        headers={
            "Content-Type": f"multipart/form-data; boundary={boundary}",
            "Content-Length": str(len(body)),
        },
        method="POST",
    )
    result = open_json_request(request, endpoint, timeout)
    if isinstance(result, (dict, list)):
        return result
    raise RuntimeError(f"Unexpected attachment response from {endpoint}.")


def safe_form_value(value: str) -> str:
    return (
        value.replace("\\", "\\\\")
        .replace('"', '\\"')
        .replace("\r", "")
        .replace("\n", "")
    )


def open_json_request(
    request: urllib.request.Request,
    endpoint: str,
    timeout: float,
) -> dict[str, object]:
    try:
        with urllib.request.urlopen(request, timeout=timeout) as response:
            response_body = response.read().decode("utf-8")
    except urllib.error.HTTPError as error:
        response_body = error.read().decode("utf-8", errors="replace")
        raise RuntimeError(
            f"HTTP {error.code} from {endpoint}: {response_body}"
        ) from error
    except urllib.error.URLError as error:
        raise RuntimeError(
            f"Could not reach {endpoint}: {error.reason}"
        ) from error

    if not response_body:
        return {}
    try:
        parsed = json.loads(response_body)
    except json.JSONDecodeError as error:
        raise RuntimeError(
            f"Invalid JSON response from {endpoint}: {response_body}"
        ) from error
    if not isinstance(parsed, dict):
        return {"value": parsed}
    return parsed


def execute_migration(
    package: SkillPackage,
    base_url: str,
    timeout: float,
    progress: Callable[[str], None] = print,
) -> str:
    skills_endpoint = f"{base_url.rstrip('/')}/api/skills"
    created = post_json(
        skills_endpoint,
        {
            "name": package.name,
            "description": package.description,
            "content": package.content,
            "tags": list(package.tags),
            "references": {},
        },
        timeout,
    )
    raw_skill_id = created.get("skillId")
    try:
        skill_id = str(uuid.UUID(str(raw_skill_id)))
    except (ValueError, TypeError, AttributeError) as error:
        raise RuntimeError(
            f"Create response did not contain a valid skillId: {created}"
        ) from error

    progress(f"Created skill '{package.name}' ({skill_id}).")
    skill_endpoint = f"{skills_endpoint}/{skill_id}"

    for reference in package.references:
        post_json(
            f"{skill_endpoint}/references",
            {
                "relativePath": reference.relative_path,
                "content": reference.content,
            },
            timeout,
        )
        progress(f"Added reference: {reference.relative_path}")

    for attachment in package.attachments:
        upload_attachment(
            f"{skill_endpoint}/attachments",
            attachment,
            timeout,
        )
        progress(f"Uploaded attachment: {attachment.relative_path}")

    return skill_id


def print_plan(package: SkillPackage, base_url: str) -> None:
    print(f"Source directory: {package.source_directory}")
    print(f"Migration endpoint: {base_url.rstrip('/')}/api/skills")
    print(f"Skill name: {package.name}")
    print(f"Tags: {', '.join(package.tags) if package.tags else '<none>'}")
    print(f"References: {len(package.references)}")
    for reference in package.references:
        print(f"  - {reference.relative_path}")
    print(f"Attachments: {len(package.attachments)}")
    for attachment in package.attachments:
        print(
            f"  - {attachment.relative_path} "
            f"({attachment.file_type}, {attachment.size} bytes)"
        )


def main() -> int:
    args = parse_args()
    try:
        package = load_skill_package(args.skills_root, args.skill_name)
        print_plan(package, args.base_url)

        if not args.execute:
            print("Dry run only. Add --execute to perform the migration.")
            return 0

        skill_id = execute_migration(
            package,
            args.base_url,
            args.timeout,
        )
        print(f"Migration completed for skill ID {skill_id}.")
        return 0
    except (OSError, UnicodeError, ValueError, RuntimeError) as error:
        print(f"Skill migration failed: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
