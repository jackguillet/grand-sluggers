"""The Unity editor every tool uses is the one unity/ProjectSettings/ProjectVersion.txt pins. An upgrade moves the
tools with it, and an editor that is not installed is a named refusal, never a silent run in the old editor."""
import importlib.util
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("unity_gui", ROOT / "tools" / "unity_gui.py")
gui = importlib.util.module_from_spec(spec)
spec.loader.exec_module(gui)

PINNED = "m_EditorVersion: 6000.9.1f1\nm_EditorVersionWithRevision: 6000.9.1f1 (0123456789ab)\n"


def temp_dir(test):
    temp = tempfile.TemporaryDirectory()
    test.addCleanup(temp.cleanup)
    return Path(temp.name)


def install(hub, version, sdks=()):
    """A fake Hub editor: Unity.app with an executable, a bundled dotnet and one csc.dll per SDK version."""
    app = hub / version / "Unity.app"
    (app / "Contents" / "MacOS").mkdir(parents=True)
    (app / "Contents" / "MacOS" / "Unity").write_text("")
    scripting = app / "Contents" / "Resources" / "Scripting" / "DotNetSdk"
    scripting.mkdir(parents=True)
    (scripting / "dotnet").write_text("#!/bin/sh\n")
    (scripting / "dotnet").chmod(0o755)
    for sdk in sdks:
        csc = scripting / "sdk" / sdk / "Roslyn" / "bincore" / "csc.dll"
        csc.parent.mkdir(parents=True)
        csc.write_text("")


class ProjectVersionTests(unittest.TestCase):
    def test_reads_the_pinned_line_not_the_first_line(self):
        project = temp_dir(self)
        (project / "ProjectSettings").mkdir()
        (project / "ProjectSettings" / "ProjectVersion.txt").write_text("# header\n" + PINNED)
        self.assertEqual("6000.9.1f1", gui.project_version(project))

    def test_a_project_without_a_pin_is_refused(self):
        project = temp_dir(self)
        (project / "ProjectSettings").mkdir()
        (project / "ProjectSettings" / "ProjectVersion.txt").write_text("m_EditorVersionWithRevision: x\n")
        with self.assertRaisesRegex(ValueError, "m_EditorVersion"):
            gui.project_version(project)

    def test_the_shipped_project_pins_an_editor(self):
        self.assertRegex(gui.project_version(ROOT / "unity"), r"^\d+\.\d+\.\d+[abfp]\d+$")


class EditorBinaryTests(unittest.TestCase):
    def setUp(self):
        self.hub = temp_dir(self)
        environment = patch.dict(os.environ, {"UNITY_HUB_EDITORS": str(self.hub)})
        environment.start()
        self.addCleanup(environment.stop)

    def test_the_installed_pinned_editor_is_found(self):
        install(self.hub, "6000.9.1f1")
        binary = gui.editor_binary("6000.9.1f1")
        self.assertEqual(self.hub / "6000.9.1f1" / "Unity.app" / "Contents" / "MacOS" / "Unity", binary)

    def test_a_missing_editor_names_the_version_and_what_is_installed(self):
        install(self.hub, "6000.5.9f1")
        with self.assertRaises(RuntimeError) as refused:
            gui.editor_binary("6000.9.1f1")
        self.assertIn("Unity 6000.9.1f1", str(refused.exception))
        self.assertIn("installed: 6000.5.9f1", str(refused.exception))


class UnityCompileScriptTests(unittest.TestCase):
    """tools/unity-compile.sh resolves the same editor before it compiles anything."""

    def run_script(self, pinned=PINNED, installs=()):
        root = temp_dir(self)
        (root / "tools").mkdir()
        shutil.copy2(ROOT / "tools" / "unity-compile.sh", root / "tools" / "unity-compile.sh")
        for folder in ("src/GrandSluggers.Sim", "unity/Assets/Scripts/Runtime", "unity/Assets/Editor"):
            (root / folder).mkdir(parents=True)
            (root / folder / "A.cs").write_text("")
        (root / "unity" / "Assets" / "Scripts" / "B.cs").write_text("")
        (root / "unity" / "ProjectSettings").mkdir()
        (root / "unity" / "ProjectSettings" / "ProjectVersion.txt").write_text(pinned)
        hub = root / "hub"
        hub.mkdir()
        for version, sdks in installs:
            install(hub, version, sdks)
        env = {k: v for k, v in os.environ.items() if k != "UNITY_EDITOR"}
        env["UNITY_HUB_EDITORS"] = str(hub)
        return subprocess.run(["zsh", str(root / "tools" / "unity-compile.sh")], env=env,
                              text=True, capture_output=True)

    def test_an_editor_that_is_not_installed_is_refused_by_version(self):
        result = self.run_script(installs=[("6000.5.9f1", ["8.0.318"])])
        self.assertEqual(1, result.returncode)
        self.assertIn("Unity 6000.9.1f1 (ProjectVersion.txt) is not installed", result.stderr)
        self.assertIn("installed: 6000.5.9f1", result.stderr)

    def test_an_editor_without_a_bundled_compiler_is_refused(self):
        result = self.run_script(installs=[("6000.9.1f1", [])])
        self.assertEqual(1, result.returncode)
        self.assertIn("bundles no C# compiler", result.stderr)

    def test_a_project_without_a_pin_is_refused(self):
        result = self.run_script(pinned="m_EditorVersionWithRevision: x\n")
        self.assertEqual(1, result.returncode)
        self.assertIn("does not pin m_EditorVersion", result.stderr)


if __name__ == "__main__":
    unittest.main()
