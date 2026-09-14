import importlib.util
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import Mock, patch

spec = importlib.util.spec_from_file_location("local_player", Path(__file__).parents[1] / "local-player.py")
player = importlib.util.module_from_spec(spec)
spec.loader.exec_module(player)


class MainDeliveryTests(unittest.TestCase):
    def git(self, cwd, *args):
        return subprocess.check_output(["git", *args], cwd=cwd, text=True, stderr=subprocess.DEVNULL).strip()

    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        root = Path(self.temp.name)
        self.origin = root / "origin.git"
        self.git(root, "init", "--bare", str(self.origin))
        self.main = root / "main"
        self.git(root, "clone", str(self.origin), str(self.main))
        self.git(self.main, "config", "user.name", "Delivery test")
        self.git(self.main, "config", "user.email", "test@example.invalid")
        self.git(self.main, "checkout", "-b", "main")
        (self.main / "game").write_text("old")
        (self.main / "unrelated").write_text("original")
        self.git(self.main, "add", "game", "unrelated")
        self.git(self.main, "commit", "-m", "initial")
        self.git(self.main, "push", "origin", "main")
        self.old = self.git(self.main, "rev-parse", "HEAD")
        self.author = root / "author"
        self.git(root, "clone", "-b", "main", str(self.origin), str(self.author))
        self.git(self.author, "config", "user.name", "Delivery test")
        self.git(self.author, "config", "user.email", "test@example.invalid")
        (self.author / "game").write_text("merged update")
        self.git(self.author, "add", "game")
        self.git(self.author, "commit", "-m", "update")
        self.git(self.author, "push", "origin", "main")

    def test_fast_forward_preserves_unrelated_local_edits(self):
        (self.main / "unrelated").write_text("local user edit")
        revision = player.sync_main(self.main)
        self.assertNotEqual(self.old, revision)
        self.assertEqual("merged update", (self.main / "game").read_text())
        self.assertEqual("local user edit", (self.main / "unrelated").read_text())

    def test_overlapping_local_edits_refuse_without_losing_work(self):
        (self.main / "game").write_text("unsaved user game")
        with self.assertRaises(subprocess.CalledProcessError):
            player.sync_main(self.main)
        self.assertEqual(self.old, self.git(self.main, "rev-parse", "HEAD"))
        self.assertEqual("unsaved user game", (self.main / "game").read_text())

    def test_other_branch_is_not_switched_or_updated(self):
        self.git(self.main, "checkout", "-b", "other-task")
        with self.assertRaises(RuntimeError):
            player.sync_main(self.main)
        self.assertEqual("other-task", self.git(self.main, "branch", "--show-current"))
        self.assertEqual(self.old, self.git(self.main, "rev-parse", "HEAD"))

    def test_diverged_main_is_not_reset(self):
        (self.main / "unrelated").write_text("unpublished work")
        self.git(self.main, "add", "unrelated")
        self.git(self.main, "commit", "-m", "unpublished")
        local = self.git(self.main, "rev-parse", "HEAD")
        with self.assertRaises(subprocess.CalledProcessError):
            player.sync_main(self.main)
        self.assertEqual(local, self.git(self.main, "rev-parse", "HEAD"))

    def test_build_evidence_must_match_revision_and_harbor_scene(self):
        revision = "a" * 40
        player.validate_build_evidence({
            "ok": True,
            "revision": revision,
            "scene": "Assets/Scenes/HarborDiamond.unity"
        }, revision)

        with self.assertRaises(RuntimeError):
            player.validate_build_evidence({
                "ok": True,
                "revision": "b" * 40,
                "scene": "Assets/Scenes/HarborDiamond.unity"
            }, revision)
        with self.assertRaises(RuntimeError):
            player.validate_build_evidence({
                "ok": True,
                "revision": revision,
                "scene": "Assets/Scenes/Other.unity"
            }, revision)





class EditorShutdownTests(unittest.TestCase):
    def test_completed_editor_is_not_signaled(self):
        process = Mock()
        process.poll.return_value = 0
        player.wait_for_editor_shutdown(process)
        process.wait.assert_not_called()
        process.terminate.assert_not_called()
        process.kill.assert_not_called()

    def test_slow_editor_is_left_for_normal_shutdown(self):
        process = Mock()
        process.poll.return_value = None
        process.wait.side_effect = subprocess.TimeoutExpired("Unity", 20)
        with patch.object(player, "log") as log:
            player.wait_for_editor_shutdown(process)
        process.wait.assert_called_once_with(timeout=20)
        process.terminate.assert_not_called()
        process.kill.assert_not_called()
        self.assertIn("quit it normally", log.call_args.args[0])


if __name__ == "__main__":
    unittest.main()
