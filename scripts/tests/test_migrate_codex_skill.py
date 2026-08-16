from __future__ import annotations

import importlib.util
import json
import sys
import tempfile
import unittest
import uuid
from pathlib import Path
from unittest.mock import call, patch


SCRIPT_PATH = Path(__file__).parents[1] / "migrate_codex_skill.py"
SPEC = importlib.util.spec_from_file_location(
    "migrate_codex_skill",
    SCRIPT_PATH,
)
assert SPEC is not None
assert SPEC.loader is not None
migration = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = migration
SPEC.loader.exec_module(migration)


class MigrateCodexSkillTests(unittest.TestCase):
    def test_loads_metadata_and_classifies_references_and_attachments(
        self,
    ) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            skills_root = Path(temporary_directory) / "skills"
            skill_directory = skills_root / "example-skill"
            (skill_directory / "references").mkdir(parents=True)
            (skill_directory / "scripts").mkdir()
            (skill_directory / "assets").mkdir()
            (skill_directory / "SKILL.md").write_text(
                "\n".join(
                    [
                        "---",
                        "name: example-skill",
                        "description: Migrate an example skill.",
                        "tags:",
                        "  - migration",
                        "  - test",
                        "---",
                        "",
                        "# Workflow",
                        "",
                        "Keep the content.",
                    ]
                ),
                encoding="utf-8",
            )
            (skill_directory / "references" / "guide.md").write_text(
                "Reference guide",
                encoding="utf-8",
            )
            (skill_directory / "scripts" / "check.py").write_text(
                "print('ok')",
                encoding="utf-8",
            )
            (skill_directory / "assets" / "logo.txt").write_text(
                "asset bytes",
                encoding="utf-8",
            )
            (skill_directory / "archive.bin").write_bytes(b"\x00\x01")

            package = migration.load_skill_package(
                skills_root,
                "example-skill",
            )

        self.assertEqual("example-skill", package.name)
        self.assertEqual("Migrate an example skill.", package.description)
        self.assertEqual(("migration", "test"), package.tags)
        self.assertEqual(
            "# Workflow\n\nKeep the content.",
            package.content,
        )
        self.assertEqual(
            ["references/guide.md", "scripts/check.py"],
            [
                reference.relative_path
                for reference in package.references
            ],
        )
        self.assertEqual(
            ["archive.bin", "assets/logo.txt"],
            [
                attachment.relative_path
                for attachment in package.attachments
            ],
        )

    def test_rejects_binary_content_inside_references(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            skills_root = Path(temporary_directory) / "skills"
            skill_directory = skills_root / "binary-reference"
            (skill_directory / "references").mkdir(parents=True)
            (skill_directory / "SKILL.md").write_text(
                "\n".join(
                    [
                        "---",
                        "name: binary-reference",
                        "description: Invalid reference.",
                        "---",
                        "Content",
                    ]
                ),
                encoding="utf-8",
            )
            (skill_directory / "references" / "bad.bin").write_bytes(
                b"\xff\xfe"
            )

            with self.assertRaisesRegex(ValueError, "not UTF-8 text"):
                migration.load_skill_package(
                    skills_root,
                    "binary-reference",
                )

    def test_execute_uses_create_reference_and_attachment_endpoints(
        self,
    ) -> None:
        skill_id = "11111111-1111-1111-1111-111111111111"
        package = migration.SkillPackage(
            source_directory=Path("/skills/example"),
            name="example",
            description="Description",
            content="Content",
            tags=("tag",),
            references=(
                migration.TextReference("references/guide.md", "Guide"),
            ),
            attachments=(
                migration.AttachmentFile(
                    "assets/logo.png",
                    Path("/skills/example/assets/logo.png"),
                    "image/png",
                    10,
                ),
            ),
        )

        with patch.object(
            migration,
            "post_json",
            side_effect=[
                {"status": "OK", "skillId": skill_id},
                {"status": "OK"},
            ],
        ) as post_json:
            with patch.object(
                migration,
                "upload_attachment",
                return_value=[],
            ) as upload_attachment:
                result = migration.execute_migration(
                    package,
                    "http://localhost:5231/",
                    30,
                    progress=lambda _: None,
                )

        self.assertEqual(skill_id, result)
        self.assertEqual(
            call(
                "http://localhost:5231/api/skills",
                {
                    "name": "example",
                    "description": "Description",
                    "content": "Content",
                    "tags": ["tag"],
                    "references": {},
                },
                30,
            ),
            post_json.call_args_list[0],
        )
        self.assertEqual(
            call(
                f"http://localhost:5231/api/skills/{skill_id}/references",
                {
                    "relativePath": "references/guide.md",
                    "content": "Guide",
                },
                30,
            ),
            post_json.call_args_list[1],
        )
        upload_attachment.assert_called_once_with(
            f"http://localhost:5231/api/skills/{skill_id}/attachments",
            package.attachments[0],
            30,
        )

    def test_upload_attachment_uses_files_multipart_field(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            source_path = Path(temporary_directory) / "logo.png"
            source_path.write_bytes(b"png-bytes")
            attachment = migration.AttachmentFile(
                "assets/logo.png",
                source_path,
                "image/png",
                len(b"png-bytes"),
            )
            response = _Response(b"[]")

            with patch.object(
                migration.urllib.request,
                "urlopen",
                return_value=response,
            ) as urlopen:
                migration.upload_attachment(
                    "http://localhost:5231/api/skills/"
                    "11111111-1111-1111-1111-111111111111/attachments",
                    attachment,
                    30,
                )

        request = urlopen.call_args.args[0]
        self.assertEqual("POST", request.method)
        self.assertIn(
            b'form-data; name="Files"; filename="logo.png"',
            request.data,
        )
        self.assertIn(b"Content-Type: image/png", request.data)
        self.assertIn(b"png-bytes", request.data)


class _Response:
    status = 200

    def __init__(self, body: bytes) -> None:
        self._body = body

    def __enter__(self) -> "_Response":
        return self

    def __exit__(self, *args: object) -> None:
        return None

    def read(self) -> bytes:
        return self._body


if __name__ == "__main__":
    unittest.main()
