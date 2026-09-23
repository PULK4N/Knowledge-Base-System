import json
import unittest
from pathlib import Path

PLUGIN_ROOT = Path(__file__).parents[1]


class PluginManifestTests(unittest.TestCase):
    def test_manifest_does_not_repeat_the_default_hooks_file(self):
        # Claude Code loads hooks/hooks.json on its own; naming it in the
        # manifest as well makes newer versions reject it as a duplicate.
        manifest = json.loads(
            (PLUGIN_ROOT / ".claude-plugin" / "plugin.json").read_text(encoding="utf-8")
        )
        declared = manifest.get("hooks", [])
        declared = [declared] if isinstance(declared, str) else declared

        self.assertTrue((PLUGIN_ROOT / "hooks" / "hooks.json").is_file())
        self.assertNotIn(
            "hooks/hooks.json",
            [str(Path(path)).replace("\\", "/") for path in declared],
        )


if __name__ == "__main__":
    unittest.main()
