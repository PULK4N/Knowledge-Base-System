from __future__ import annotations

import argparse
import sys
import tempfile
import unittest
from pathlib import Path
from unittest.mock import call, patch

SCRIPTS_DIRECTORY = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(SCRIPTS_DIRECTORY))

import add_skill_reference  # noqa: E402
import create_skill  # noqa: E402


class CreateSkillTests(unittest.TestCase):
    def test_create_reads_frontmatter_and_posts_controller_payload(self) -> None:
        skill_id = "11111111-1111-1111-1111-111111111111"
        with tempfile.TemporaryDirectory() as temporary_directory:
            skill_file = Path(temporary_directory) / "SKILL.md"
            skill_file.write_text(
                "---\n"
                "name: example-skill\n"
                "description: Example description\n"
                "tags: [\"api\", \"example\"]\n"
                "---\n"
                "# Instructions\n",
                encoding="utf-8",
            )
            args = create_skill.parse_args([str(skill_file)])

            with patch.object(
                create_skill,
                "post_json",
                return_value={"status": "OK", "skillId": skill_id},
            ) as post_json:
                result = create_skill.create_skill(args)

        self.assertEqual(skill_id, result)
        post_json.assert_called_once_with(
            "http://localhost:5231/api/skills",
            {
                "name": "example-skill",
                "description": "Example description",
                "content": "# Instructions",
                "tags": ["api", "example"],
                "references": {},
            },
            30.0,
        )

    def test_plain_markdown_accepts_required_cli_metadata(self) -> None:
        skill_id = "22222222-2222-2222-2222-222222222222"
        with tempfile.TemporaryDirectory() as temporary_directory:
            skill_file = Path(temporary_directory) / "instructions.md"
            skill_file.write_text("# Instructions\n", encoding="utf-8")
            args = create_skill.parse_args(
                [
                    str(skill_file),
                    "--name",
                    "plain-skill",
                    "--description",
                    "Plain description",
                    "--tag",
                    "plain",
                ]
            )

            with patch.object(
                create_skill,
                "post_json",
                return_value={"skillId": skill_id},
            ):
                result = create_skill.create_skill(args)

        self.assertEqual(skill_id, result)


class AddSkillReferenceTests(unittest.TestCase):
    def test_add_reference_posts_file_content_and_default_path(self) -> None:
        skill_id = "33333333-3333-3333-3333-333333333333"
        with tempfile.TemporaryDirectory() as temporary_directory:
            reference_file = Path(temporary_directory) / "guide.md"
            reference_file.write_text("# Guide\n", encoding="utf-8")
            args = add_skill_reference.parse_args(
                [skill_id, str(reference_file), "--load-automatically"]
            )

            with patch.object(
                add_skill_reference,
                "post_json",
                return_value={"status": "OK"},
            ) as post_json:
                result = add_skill_reference.add_reference(args)

        self.assertEqual("references/guide.md", result)
        post_json.assert_has_calls(
            [
                call(
                    f"http://localhost:5231/api/skills/{skill_id}/references",
                    {
                        "relativePath": "references/guide.md",
                        "content": "# Guide\n",
                        "loadAutomatically": True,
                    },
                    30.0,
                )
            ]
        )

    def test_rejects_parent_segments_in_relative_path(self) -> None:
        with self.assertRaisesRegex(ValueError, "relative POSIX"):
            add_skill_reference.resolve_relative_path(
                Path("guide.md"),
                "references/../guide.md",
            )

    def test_rejects_invalid_skill_id(self) -> None:
        with self.assertRaises(argparse.ArgumentTypeError):
            add_skill_reference.skill_id("not-a-uuid")


if __name__ == "__main__":
    unittest.main()
