from pathlib import Path
import os
import shutil
import subprocess
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[2]


class UnityProjectVersionTests(unittest.TestCase):
    def run_gate(self, actual_version):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / 'tools').mkdir()
            settings = root / 'unity' / 'ProjectSettings'
            settings.mkdir(parents=True)
            (settings / 'ProjectVersion.txt').write_text('m_EditorVersion: 6000.5.9f1\n')
            script = root / 'tools' / 'unity-project-validate.sh'
            shutil.copy2(ROOT / 'tools' / script.name, script)
            # A process double emits evidence; no Unity process or license is used.
            editor = root / 'editor-double.py'
            editor.write_text('''#!/usr/bin/env python3
import json, os
from pathlib import Path
Path(os.environ['GS_VALIDATION_EVIDENCE']).write_text(json.dumps({
    'ok': True,
    'revision': os.environ['GS_VALIDATION_REVISION'],
    'unityVersion': os.environ['TEST_ACTUAL_UNITY_VERSION']
}))
''')
            editor.chmod(0o755)
            for command in (
                ['git', 'init', '-q'],
                ['git', 'add', 'tools/unity-project-validate.sh',
                 'unity/ProjectSettings/ProjectVersion.txt', 'editor-double.py'],
                ['git', '-c', 'user.name=Test', '-c', 'user.email=test@example.invalid',
                 '-c', 'commit.gpgsign=false', 'commit', '-qm', 'fixture'],
            ):
                subprocess.run(command, cwd=root, check=True, capture_output=True)
            env = dict(os.environ, UNITY_EDITOR_BIN=str(editor),
                       GS_UNITY_BATCH_LICENSED='1', TEST_ACTUAL_UNITY_VERSION=actual_version)
            return subprocess.run([str(script)], cwd=root, env=env,
                                  text=True, capture_output=True)

    def test_pinned_editor_evidence_passes(self):
        result = self.run_gate('6000.5.9f1')
        self.assertEqual(0, result.returncode, result.stderr)

    def test_other_editor_cannot_claim_a_reproducible_pass(self):
        result = self.run_gate('6000.5.10f1')
        self.assertNotEqual(0, result.returncode)
        self.assertIn('Unity 6000.5.9f1', result.stderr)

    def test_missing_editor_version_cannot_pass(self):
        result = self.run_gate('')
        self.assertNotEqual(0, result.returncode)


if __name__ == '__main__':
    unittest.main()
