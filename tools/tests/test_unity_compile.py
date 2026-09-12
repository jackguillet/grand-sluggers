from pathlib import Path
import subprocess
import unittest


ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / "tools" / "unity-compile.sh"


class UnityCompileContractTests(unittest.TestCase):
    def test_source_inventory_covers_all_tracked_unity_csharp(self):
        output = subprocess.check_output(
            [str(SCRIPT), "--list-sources"], cwd=ROOT, text=True
        ).splitlines()

        expected = {
            str(path.relative_to(ROOT))
            for path in (ROOT / "src" / "GrandSluggers.Sim").rglob("*.cs")
            if "bin" not in path.parts and "obj" not in path.parts
        }
        expected.update(
            str(path.relative_to(ROOT))
            for path in (ROOT / "unity" / "Assets" / "Scripts").rglob("*.cs")
        )
        expected.update(
            str(path.relative_to(ROOT))
            for path in (ROOT / "unity" / "Assets" / "Editor").rglob("*.cs")
        )

        self.assertEqual(expected, set(output))
        self.assertIn("unity/Assets/Scripts/MatchBootstrap.cs", output)

    def test_compiler_has_no_checkout_specific_library_fallback(self):
        script = SCRIPT.read_text()
        self.assertNotIn("/Users/", script)
        self.assertIn("UNITY_PACKAGE_ASSEMBLIES", script)


if __name__ == "__main__":
    unittest.main()
