import argparse
import importlib.util
import os
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import Mock, call, patch

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





class RuntimeDataTests(unittest.TestCase):
    def test_the_shipped_package_leaves_agent_data_and_scripts_behind(self):
        data = Path(__file__).parents[2] / "data"
        files = player.runtime_data_files(data)
        self.assertIn("rules/match.json", files)
        self.assertIn("art/audio-clips/glove.wav", files)
        self.assertFalse([f for f in files if f.startswith("agent/")])
        self.assertFalse([f for f in files if f.endswith(".py")])

    def test_only_listed_folders_and_extensions_are_copied(self):
        with tempfile.TemporaryDirectory() as temp:
            data = Path(temp) / "data"
            for relative in ("rules/a.json", "rules/deep/b.WAV", "rules/bake.py", "rules/.DS_Store", "agent/ledger.json"):
                (data / relative).parent.mkdir(parents=True, exist_ok=True)
                (data / relative).write_text("x")
            (data / "package.json").write_text('{"runtime": ["rules"], "tooling": ["agent"], "extensions": [".json", ".wav"]}')
            target = Path(temp) / "release" / "data"
            player.copy_runtime_data(data, target)
            copied = sorted(p.relative_to(target).as_posix() for p in target.rglob("*") if p.is_file())
            self.assertEqual(["rules/a.json", "rules/deep/b.WAV"], copied)


class TrialOverlayTests(unittest.TestCase):
    """The window can play a trial overlay (#715) — only one the built revision carries, named as the game names it."""

    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.source = Path(self.temp.name)
        (self.source / "trials" / "c80" / "rules").mkdir(parents=True)

    def test_no_trial_is_the_shipped_data(self):
        self.assertIsNone(player.trial_overlay(self.source, None))
        self.assertIsNone(player.trial_overlay(self.source, ""))

    def test_a_trial_the_revision_carries_is_named_as_the_game_names_it(self):
        self.assertEqual("trials/c80", player.trial_overlay(self.source, "trials/c80"))
        self.assertEqual("trials/c80", player.trial_overlay(self.source, "trials/c80/"))

    def test_a_missing_trial_stops_before_anything_restarts(self):
        with self.assertRaises(RuntimeError):
            player.trial_overlay(self.source, "trials/c70")

    def test_only_a_folder_under_trials_is_an_overlay(self):
        for name in ("data", "/tmp/trials/c80", "trials/../data", "c80", "trials"):
            with self.subTest(name=name), self.assertRaises(RuntimeError):
                player.trial_overlay(self.source, name)


class ReplaceTests(unittest.TestCase):
    """A delivery names the window it would close and closes it only when told to (--replace)."""

    WINDOW = dict(pid=77559, start="", kind="main", revision="d49c527351aced99d9fe8c52d7743f3a3b781d3b",
                  dataProfile="trials/pitch5", app="/releases/main-d49c527351-1/GrandSluggers.app",
                  delivered="2026-09-22T17:20:37Z")

    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.support = Path(self.temp.name)
        environment = patch.dict(os.environ, {"GS_SUPPORT_DIR": str(self.support)})
        environment.start()
        self.addCleanup(environment.stop)

    def args(self, **overrides):
        values = dict(preview=None, trial=None, timeout=900, replace=False, fresh_library=False, keep=3, prune_only=False)
        values.update(overrides)
        return argparse.Namespace(**values)

    def test_an_open_window_is_named_and_nothing_is_built_without_replace(self):
        for preview in (None, "/nonexistent/preview-worktree"):
            with self.subTest(preview=preview), \
                    patch.object(player.unity_gui, "players", return_value=[self.WINDOW]), \
                    patch.object(player, "sync_main", side_effect=AssertionError("main moved")) as sync:
                with self.assertRaises(RuntimeError) as refused:
                    player.deliver(self.args(preview=preview))
                message = str(refused.exception)
                for text in ("main d49c527351 on trials/pitch5", "pid 77559", "delivered 2026-09-22T17:20:37Z",
                             "--replace", "nothing was built"):
                    self.assertIn(text, message)
                sync.assert_not_called()
                self.assertFalse((self.support / "unity-gui.lock").exists())

    def test_no_open_window_needs_no_replace(self):
        player.refuse_unless_replace([], False, "nothing was built.")

    def test_replace_names_each_window_it_closes(self):
        with patch.object(player.os, "kill", side_effect=[None, ProcessLookupError()]) as kill, \
                patch.object(player, "log") as log:
            player.close_players([self.WINDOW], "main 5c58133700 on the shipped data", "/releases/new/GrandSluggers.app")
        self.assertEqual(call(77559, player.signal.SIGTERM), kill.call_args_list[0])
        closing = log.call_args.args[0]
        self.assertIn("Closing main d49c527351 on trials/pitch5 (pid 77559", closing)
        self.assertIn("for main 5c58133700 on the shipped data", closing)

    def test_a_held_gui_lock_refuses_the_delivery_by_name(self):
        holder = subprocess.Popen(["sleep", "60"])
        self.addCleanup(holder.wait)
        self.addCleanup(holder.kill)
        player.unity_gui.acquire("tools/still-gate.sh --park crystal-rink", holder.pid, worktree="/wt/a")
        with patch.object(player.unity_gui, "players", side_effect=AssertionError("looked past the lock")), \
                patch.object(player, "sync_main", side_effect=AssertionError("main moved")):
            with self.assertRaises(player.unity_gui.LockHeld) as refused:
                player.deliver(self.args())
        self.assertIn("tools/still-gate.sh --park crystal-rink (pid " + str(holder.pid), str(refused.exception))


class LibrarySeedAndPruneTests(unittest.TestCase):
    """Delivery clones the last good build's Library and keeps only the newest few releases and build worktrees."""

    def git(self, cwd, *args):
        return subprocess.check_output(["git", *args], cwd=cwd, text=True, stderr=subprocess.DEVNULL).strip()

    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name).resolve()
        self.main = self.root / "main"
        self.main.mkdir()
        self.git(self.main, "init", "-b", "main")
        self.git(self.main, "config", "user.name", "Delivery test")
        self.git(self.main, "config", "user.email", "test@example.invalid")
        (self.main / "unity/ProjectSettings").mkdir(parents=True)
        (self.main / "unity/ProjectSettings/ProjectVersion.txt").write_text("m_EditorVersion: 6000.0.1f1\n")
        self.git(self.main, "add", "unity")
        self.git(self.main, "commit", "-m", "initial")
        self.state = self.root / "state"
        (self.state / "releases").mkdir(parents=True)
        for patcher in (patch.object(player.unity_gui, "players", return_value=[]),
                        patch.object(player.unity_gui, "editor_for", return_value=None),
                        patch.object(player, "log")):
            patcher.start()
            self.addCleanup(patcher.stop)

    def build(self, stamp, library=True, version="6000.0.1f1"):
        worktree = self.root / "scratchpad" / ("wt-player-abcdef1234-" + str(stamp))
        worktree.parent.mkdir(exist_ok=True)
        self.git(self.main, "worktree", "add", "--detach", str(worktree), "HEAD")
        (worktree / "unity/ProjectSettings/ProjectVersion.txt").write_text("m_EditorVersion: " + version + "\n")
        if library:
            (worktree / "unity/Library").mkdir()
            (worktree / "unity/Library/marker").write_text(str(stamp))
        return worktree

    def release(self, stamp, source):
        release = self.state / "releases" / ("main-abcdef1234-" + str(stamp))
        (release / "GrandSluggers.app").mkdir(parents=True)
        (release / "revision.json").write_text(player.json.dumps(dict(source=str(source))))
        return release

    @unittest.skipUnless(player.sys.platform == "darwin", "APFS clone (cp -c) is the Mac delivery path")
    def test_the_newest_good_build_seeds_the_library(self):
        old, new = self.build(1), self.build(2)
        self.release(1, old)
        self.release(2, new)
        fresh = self.build(3, library=False)
        seed = player.seed_library(fresh / "unity", "6000.0.1f1", self.state)
        self.assertEqual(new, seed)
        self.assertEqual("2", (fresh / "unity/Library/marker").read_text())

    def test_no_seed_from_another_unity_version_an_open_editor_or_a_failed_build(self):
        other = self.build(1, version="2022.3.1f1")
        self.release(1, other)
        self.build(2)  # built, but no release names it: never a seed
        opened = self.build(3)
        self.release(3, opened)
        fresh = self.build(4, library=False)
        with patch.object(player.unity_gui, "editor_for",
                          side_effect=lambda project, listing=None: dict(pid=9) if project == opened / "unity" else None):
            self.assertIsNone(player.seed_library(fresh / "unity", "6000.0.1f1", self.state))
        self.assertFalse((fresh / "unity/Library").exists())

    def test_prune_keeps_the_newest_and_every_open_windows_release(self):
        builds = [self.build(i) for i in range(1, 7)]
        releases = [self.release(i, b) for i, b in enumerate(builds, 1)]
        window = dict(pid=1, app=str(releases[0] / "GrandSluggers.app"))
        with patch.object(player.unity_gui, "players", return_value=[window]):
            removed = player.prune(self.main, self.state, keep=2)
        self.assertEqual((3, 3), removed)
        self.assertEqual({releases[0], releases[4], releases[5]}, set(player.releases_newest_first(self.state)))
        remaining = {b for b in builds if b.exists()}
        self.assertEqual({builds[0], builds[4], builds[5]}, remaining)
        self.assertNotIn(str(builds[1]), self.git(self.main, "worktree", "list"))

    def test_prune_never_removes_a_worktree_with_an_editor_open(self):
        builds = [self.build(i) for i in range(1, 4)]
        with patch.object(player.unity_gui, "editor_for",
                          side_effect=lambda project, listing=None: dict(pid=9) if project == builds[0] / "unity" else None):
            player.prune(self.main, self.state, keep=1)
        self.assertTrue(builds[0].exists())
        self.assertFalse(builds[1].exists())
        self.assertTrue(builds[2].exists())


class EditorShutdownTests(unittest.TestCase):
    def editor(self, pid=4242):
        process = Mock()
        process.pid = pid
        process.poll.return_value = None
        return process

    def assert_not_signaled(self, process):
        process.terminate.assert_not_called()
        process.kill.assert_not_called()
        process.send_signal.assert_not_called()

    def test_completed_editor_is_not_signaled(self):
        process = self.editor()
        process.poll.return_value = 0
        with patch.object(player, "request_normal_quit") as quit_request:
            player.wait_for_editor_shutdown(process, True)
        process.wait.assert_not_called()
        quit_request.assert_not_called()
        self.assert_not_signaled(process)

    def test_editor_that_exits_after_a_good_build_gets_no_quit_request(self):
        process = self.editor()
        process.wait.return_value = 0
        with patch.object(player, "request_normal_quit") as quit_request:
            player.wait_for_editor_shutdown(process, True)
        process.wait.assert_called_once_with(timeout=20)
        quit_request.assert_not_called()
        self.assert_not_signaled(process)

    def test_idle_editor_after_a_good_build_gets_one_normal_quit(self):
        process = self.editor()
        process.wait.side_effect = [subprocess.TimeoutExpired("Unity", 20), 0]
        with patch.object(player, "request_normal_quit", return_value=True) as quit_request, \
                patch.object(player, "log") as log:
            player.wait_for_editor_shutdown(process, True)
        quit_request.assert_called_once_with(4242)
        self.assertEqual([call(timeout=20), call(timeout=120)], process.wait.call_args_list)
        self.assertNotIn("still open for inspection", " ".join(c.args[0] for c in log.call_args_list))
        self.assert_not_signaled(process)

    def test_slow_editor_is_left_for_normal_shutdown(self):
        process = self.editor()
        process.wait.side_effect = subprocess.TimeoutExpired("Unity", 20)
        with patch.object(player, "request_normal_quit") as quit_request, patch.object(player, "log") as log:
            player.wait_for_editor_shutdown(process, False)
        process.wait.assert_called_once_with(timeout=20)
        quit_request.assert_not_called()
        self.assert_not_signaled(process)
        self.assertIn("quit it normally", log.call_args.args[0])

    def test_editor_that_stays_open_after_the_quit_request_is_left_open(self):
        process = self.editor()
        process.wait.side_effect = subprocess.TimeoutExpired("Unity", 20)
        with patch.object(player, "request_normal_quit", return_value=True), patch.object(player, "log") as log:
            player.wait_for_editor_shutdown(process, True)
        self.assertEqual([call(timeout=20), call(timeout=120)], process.wait.call_args_list)
        self.assert_not_signaled(process)
        self.assertIn("still open for inspection", log.call_args.args[0])

    def test_normal_quit_is_the_quit_apple_event_to_the_exact_pid(self):
        reply = subprocess.CompletedProcess([], 0, stdout="asked\n", stderr="")
        with patch.object(player.subprocess, "run", return_value=reply) as run, \
                patch.object(player.os, "kill") as kill:
            self.assertTrue(player.request_normal_quit(4242))
        command = run.call_args.args[0]
        self.assertEqual(["osascript", "-l", "JavaScript", "-e"], command[:4])
        self.assertIn("runningApplicationWithProcessIdentifier(4242)", command[4])
        self.assertIn("app.terminate", command[4])
        kill.assert_not_called()

    def test_normal_quit_reports_an_app_it_could_not_ask(self):
        for outcome in (subprocess.CompletedProcess([], 0, stdout="missing\n", stderr=""),
                        subprocess.CompletedProcess([], 1, stdout="", stderr="execution error"),
                        OSError("no osascript"),
                        subprocess.TimeoutExpired("osascript", 30)):
            with patch.object(player.subprocess, "run", side_effect=[outcome]):
                self.assertFalse(player.request_normal_quit(4242))


if __name__ == "__main__":
    unittest.main()
