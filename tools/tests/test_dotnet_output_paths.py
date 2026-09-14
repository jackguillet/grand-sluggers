from pathlib import Path
import json
import subprocess
import unittest

ROOT = Path(__file__).resolve().parents[2]


class DotnetOutputPathsTests(unittest.TestCase):
    def test_both_configurations_keep_generated_files_outside_unity_package(self):
        package = ROOT / "src/GrandSluggers.Sim"
        for config in ("Debug", "Release"):
            with self.subTest(config=config):
                output = subprocess.check_output([
                    "dotnet", "msbuild", str(package / "GrandSluggers.Sim.csproj"),
                    "-nologo", f"-p:Configuration={config}",
                    "-getProperty:BaseIntermediateOutputPath,OutputPath",
                ], cwd=ROOT, text=True)
                properties = json.loads(output)["Properties"]
                for key in ("BaseIntermediateOutputPath", "OutputPath"):
                    path = (package / properties[key]).resolve()
                    self.assertTrue(path.is_relative_to(ROOT / ".artifacts"), (key, path))
                    self.assertFalse(path.is_relative_to(package), (key, path))


if __name__ == "__main__":
    unittest.main()
