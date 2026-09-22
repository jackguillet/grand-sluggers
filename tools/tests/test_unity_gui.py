import importlib.util
import io
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import tempfile
import time
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("unity_gui", ROOT / "tools" / "unity_gui.py")
gui = importlib.util.module_from_spec(spec)
spec.loader.exec_module(gui)

EDITOR = "/Applications/Unity/Hub/Editor/6000.5.9f1/Unity.app/Contents/MacOS/Unity"
LISTING = "\n".join([
    "  501 Tue Sep 22 10:00:00 2026     " + EDITOR + " -projectPath /wt/a/unity -executeMethod X.Y -logFile /tmp/l",
    "  502 Mon Sep 21 20:00:00 2026     " + EDITOR,
    "  503 Sat Aug 22 19:36:08 2026     /Applications/Unity Hub.app/Contents/MacOS/Unity Hub",
    "  504 Tue Sep 22 09:00:00 2026     " + EDITOR + " -projectpath /wt/b/unity/ -useHub -hubIPC",
    "  505 Tue Sep 22 09:00:05 2026     " + EDITOR + " -adb2 -batchMode -noUpm -name AssetImportWorker0 -projectPath /wt/b/unity",
    "  506 Tue Sep 22 09:10:00 2026     /usr/bin/tail -f " + EDITOR,
])


def use_temp_support(test):
    """Every lock test runs against its own support folder, never the Mac's real lock or delivered windows."""
    temp = tempfile.TemporaryDirectory()
    test.addCleanup(temp.cleanup)
    test.support = Path(temp.name)
    environment = patch.dict(os.environ, {"GS_SUPPORT_DIR": str(test.support)})
    environment.start()
    test.addCleanup(environment.stop)
    test.lock = test.support / "unity-gui.lock"


def sleeper(test):
    """A live process to hold the lock, stopped when the test ends."""
    child = subprocess.Popen(["sleep", "60"])
    test.addCleanup(stop, child)
    return child


def stop(child):
    if child.poll() is None:
        child.kill()
    child.wait()


class LockCase(unittest.TestCase):
    def setUp(self):
        use_temp_support(self)

    def process(self):
        return sleeper(self)

    stop = staticmethod(stop)


class GuiLockTests(unittest.TestCase):
    def setUp(self):
        use_temp_support(self)

    def process(self):
        return sleeper(self)

    def test_a_live_holder_refuses_the_second_session_by_name(self):
        holder = self.process()
        gui.acquire("tools/still-gate.sh --park crystal-rink", holder.pid, worktree="/wt/a")
        with self.assertRaises(gui.LockHeld) as refused:
            gui.acquire("local-player delivery of main", os.getpid(), worktree="/wt/b")
        message = str(refused.exception)
        self.assertIn("tools/still-gate.sh --park crystal-rink", message)
        self.assertIn("pid " + str(holder.pid), message)
        self.assertIn("worktree /wt/a", message)
        self.assertIn("since ", message)
        self.assertEqual(holder.pid, json.loads(self.lock.read_text())["pid"])

    def test_the_record_names_pid_worktree_purpose_command_and_start(self):
        holder = self.process()
        record, notes, fresh = gui.acquire("still gate", holder.pid, worktree="/wt/a")
        self.assertTrue(fresh)
        self.assertEqual([], notes)
        saved = json.loads(self.lock.read_text())
        self.assertEqual(record, saved)
        self.assertEqual({"pid", "processStart", "purpose", "worktree", "command", "started"}, set(saved))
        self.assertEqual("/wt/a", saved["worktree"])
        self.assertIn("sleep 60", saved["command"])
        self.assertRegex(saved["started"], r"^\d{4}-\d\d-\d\dT\d\d:\d\d:\d\dZ$")
        self.assertTrue(saved["processStart"])

    def test_a_dead_holders_lock_is_cleared_and_taken(self):
        holder = self.process()
        gui.acquire("capture loop", holder.pid, worktree="/wt/a")
        stop(holder)
        record, notes, fresh = gui.acquire("still gate", os.getpid(), worktree="/wt/b")
        self.assertTrue(fresh)
        self.assertEqual(os.getpid(), record["pid"])
        self.assertEqual(1, len(notes))
        self.assertIn("stale", notes[0])
        self.assertIn("capture loop (pid " + str(holder.pid), notes[0])

    def test_a_reused_pid_is_not_the_holder(self):
        self.lock.write_text(json.dumps(dict(pid=os.getpid(), processStart="Thu Jan 1 00:00:00 1970",
                                             purpose="an old session", worktree="/wt/old", started="1970-01-01T00:00:00Z")))
        holder = self.process()
        record, notes, fresh = gui.acquire("still gate", holder.pid)
        self.assertTrue(fresh)
        self.assertEqual(holder.pid, record["pid"])
        self.assertIn("an old session", notes[0])

    def test_the_same_holder_takes_it_again_without_owning_a_second_release(self):
        with gui.hold("delivery", worktree="/wt/a"):
            with gui.hold("delivery, nested", worktree="/wt/a"):
                pass
            self.assertEqual(os.getpid(), json.loads(self.lock.read_text())["pid"])
        self.assertFalse(self.lock.exists())

    def test_hold_releases_when_the_block_fails(self):
        with self.assertRaises(ValueError):
            with gui.hold("delivery", worktree="/wt/a"):
                raise ValueError("build failed")
        self.assertFalse(self.lock.exists())

    def test_only_the_holder_releases(self):
        holder = self.process()
        gui.acquire("still gate", holder.pid)
        self.assertFalse(gui.release(os.getpid()))
        self.assertTrue(self.lock.exists())
        self.assertTrue(gui.release(holder.pid))
        self.assertFalse(self.lock.exists())
        self.assertFalse(gui.release(holder.pid))

    def test_an_unreadable_lock_is_refused_not_cleared(self):
        self.lock.write_text("{half a record")
        with self.assertRaises(gui.LockHeld) as refused:
            gui.acquire("still gate", os.getpid())
        self.assertIn(str(self.lock), str(refused.exception))
        self.assertEqual("{half a record", self.lock.read_text())

    def test_a_pid_that_is_not_running_cannot_take_it(self):
        gone = self.process()
        stop(gone)
        with self.assertRaises(RuntimeError):
            gui.acquire("still gate", gone.pid)
        self.assertFalse(self.lock.exists())

    def test_the_cli_names_the_holder_and_exits_75(self):
        holder = self.process()
        script = str(ROOT / "tools" / "unity_gui.py")
        taken = subprocess.run([sys.executable, script, "acquire", "--pid", str(holder.pid), "--purpose", "hand test",
                                "--worktree", "/wt/a"], capture_output=True, text=True)
        self.assertEqual(0, taken.returncode, taken.stderr)
        second = subprocess.run([sys.executable, script, "acquire", "--pid", str(os.getpid()), "--purpose", "second"],
                                capture_output=True, text=True)
        self.assertEqual(gui.HELD, second.returncode)
        self.assertIn("hand test (pid " + str(holder.pid) + ", worktree /wt/a", second.stderr)
        status = subprocess.run([sys.executable, script, "status"], capture_output=True, text=True)
        self.assertIn("held by hand test", status.stdout)


class EditorTests(LockCase):
    def test_editors_name_their_project_and_a_bare_editor_has_none(self):
        found = {e["pid"]: e["project"] for e in gui.editors(LISTING)}
        self.assertEqual({501: gui._norm("/wt/a/unity"), 502: None, 504: gui._norm("/wt/b/unity")}, found)

    def test_the_editor_for_a_project_is_that_projects_gui_editor(self):
        self.assertEqual(504, gui.editor_for("/wt/b/unity", LISTING)["pid"])
        self.assertEqual(501, gui.editor_for("/wt/a/unity/", LISTING)["pid"])
        self.assertIsNone(gui.editor_for("/wt/c/unity", LISTING))

    def own_the_lock(self):
        gui.acquire("still gate", os.getpid())

    def test_front_is_by_pid_through_system_events(self):
        self.own_the_lock()
        with patch.object(gui, "_ps", return_value=LISTING), patch.object(gui, "_osascript") as osascript:
            gui.front(501, os.getpid())
        self.assertEqual('tell application "System Events" to set frontmost of '
                         '(first process whose unix id is 501) to true\n', osascript.call_args.args[0])

    def test_the_script_goes_to_osascript_on_stdin(self):
        with patch.object(gui.subprocess, "run") as run:
            gui._osascript("script")
        self.assertEqual(["osascript"], run.call_args.args[0])
        self.assertEqual("script", run.call_args.kwargs["input"])

    def test_menu_clicks_the_item_in_that_pid(self):
        self.own_the_lock()
        with patch.object(gui, "_ps", return_value=LISTING), patch.object(gui, "_osascript") as osascript:
            gui.menu(504, os.getpid(), "Grand Sluggers", "Capture Still Gate")
        script = osascript.call_args.args[0]
        self.assertIn("tell (first process whose unix id is 504)", script)
        self.assertIn('click menu item "Capture Still Gate" of menu "Grand Sluggers" of menu bar 1', script)
        self.assertNotIn('process "Unity"', script)
        self.assertNotIn('application "Unity"', script)

    def test_only_the_lock_holder_fronts_or_clicks_an_editor(self):
        holder = self.process()
        with patch.object(gui, "_ps", return_value=LISTING), patch.object(gui, "_osascript") as osascript:
            with self.assertRaises(gui.LockHeld):
                gui.front(501, os.getpid())
            gui.acquire("someone else's capture", holder.pid, worktree="/wt/a")
            with self.assertRaises(gui.LockHeld) as refused:
                gui.menu(501, os.getpid(), "Grand Sluggers", "Capture Still Gate")
        osascript.assert_not_called()
        self.assertIn("someone else's capture", str(refused.exception))

    def test_quit_is_the_normal_quit_to_that_editor_and_holder_only(self):
        with patch.object(gui, "_ps", return_value=LISTING), \
                patch.object(gui, "request_normal_quit", return_value=True) as quit_request:
            with self.assertRaises(gui.LockHeld):
                gui.quit_editor(502, os.getpid())
            quit_request.assert_not_called()
            self.own_the_lock()
            gui.quit_editor(502, os.getpid())
        quit_request.assert_called_once_with(502)

    def test_front_refuses_a_pid_that_is_not_a_gui_editor(self):
        self.own_the_lock()
        with patch.object(gui, "_ps", return_value=LISTING), patch.object(gui, "_osascript") as osascript:
            for pid in (503, 505, 506, 999):
                with self.subTest(pid=pid), self.assertRaises(RuntimeError):
                    gui.front(pid, os.getpid())
        osascript.assert_not_called()


class DeliveredPlayerTests(LockCase):
    def release(self, name, **revision):
        folder = self.support / "local-player" / "releases" / name
        folder.mkdir(parents=True)
        (folder / "revision.json").write_text(json.dumps(revision))
        (folder / "launch-evidence.json").write_text(json.dumps(dict(utc="2026-09-22T17:20:37Z")))
        return str(folder) + "/GrandSluggers.app/Contents/MacOS/Grand Sluggers"

    def test_players_name_the_revision_kind_and_trial_they_play(self):
        trial = self.release("main-d49c527351-1", revision="d49c527351aced99d9fe8c52d7743f3a3b781d3b", kind="main",
                             dataProfile="trials/pitch5")
        preview = self.release("preview-abcdef0123-2", revision="abcdef0123456789", kind="preview")
        checkout = "/repo/main/unity/Builds/osx/GrandSluggers.app/Contents/MacOS/Grand Sluggers"
        listing = "\n".join([
            "77559 Tue Sep 22 10:20:34 2026 " + trial + " -screen-fullscreen 0 -logFile x",
            "77600 Tue Sep 22 10:30:00 2026 " + preview,
            "77601 Tue Sep 22 10:31:00 2026 " + checkout + " -screen-width 1280",
            "77602 Tue Sep 22 10:32:00 2026 /wt/other/unity/Builds/osx/GrandSluggers.app/Contents/MacOS/Grand Sluggers",
            "77603 Tue Sep 22 10:33:00 2026 /usr/bin/tail -f " + trial,
        ])
        found = gui.players(checkout="/repo/main", listing=listing)
        self.assertEqual([77559, 77600, 77601], [p["pid"] for p in found])
        self.assertEqual("main d49c527351 on trials/pitch5 (pid 77559, delivered 2026-09-22T17:20:37Z)",
                         gui.describe_player(found[0]))
        self.assertEqual("preview abcdef0123 on the shipped data (pid 77600, delivered 2026-09-22T17:20:37Z)",
                         gui.describe_player(found[1]))
        self.assertIn("primary checkout", gui.describe_player(found[2]))

    def test_a_capture_that_fronts_an_editor_refuses_while_jacks_window_is_open(self):
        trial = self.release("main-d49c527351-1", revision="d49c527351aced99d9fe8c52d7743f3a3b781d3b", kind="main",
                             dataProfile="trials/pitch5")
        listing = "77559 Tue Sep 22 10:20:34 2026 " + trial
        with patch.object(gui, "_ps", return_value=listing), patch("sys.stderr", new_callable=io.StringIO) as err:
            code = gui.main(["acquire", "--pid", str(os.getpid()), "--purpose", "tools/still-gate.sh", "--focus"])
            self.assertEqual(gui.HELD, code)
            self.assertIn("Jack's game window is open: main d49c527351 on trials/pitch5 (pid 77559", err.getvalue())
            self.assertIn("--player-open-ok", err.getvalue())
            self.assertFalse(self.lock.exists())
            self.assertEqual(0, gui.main(["acquire", "--pid", str(os.getpid()), "--purpose", "tools/still-gate.sh",
                                          "--focus", "--player-open-ok"]))
        self.assertTrue(self.lock.exists())


class ToolsTakeTheLockTests(unittest.TestCase):
    """The next capture helper, build or delivery that drives a GUI editor takes the lock too."""

    DRIVES = re.compile(r"-executeMethod|unity_gui\.py\" \"\$@\"|gs-player-request|PlayerBuildGate")
    BATCH = re.compile(r"^[^#\n]*-batchmode", re.MULTILINE)

    def tools(self):
        for path in sorted((ROOT / "tools").rglob("*")):
            if path.is_file() and path.suffix in (".sh", ".py", ".zsh") and "tests" not in path.parts \
                    and path.name != "unity_gui.py":
                yield path

    def test_every_tool_that_drives_a_gui_editor_takes_the_lock(self):
        driving = []
        for path in self.tools():
            text = path.read_text()
            if not self.DRIVES.search(text) or self.BATCH.search(text):
                continue  # a batch run is the licensed CI runner's gate, not the GUI editor
            driving.append(path.name)
            with self.subTest(tool=path.name):
                self.assertRegex(text, r"acquire --pid \$\$|unity_gui\.hold\(")
                if path.suffix != ".py":
                    self.assertIn("trap 'gui release --pid $$' EXIT", text)
        self.assertGreaterEqual(set(driving), {"still-gate.sh", "still-gate-character.sh", "build-player.sh",
                                               "local-player.py"})

    def test_no_tool_fronts_or_starts_unity_by_name(self):
        by_name = re.compile(r'application\s+"Unity"|process\s+"Unity"|open\s+-a\s+"?Unity(?!\s*Hub)|pkill\s+(-x\s+)?Unity')
        places = list(self.tools()) + [ROOT / "tools" / "unity_gui.py"]
        places += [p for p in (ROOT / ".grok" / "skills").rglob("*") if p.is_file()]
        places += [p for p in (ROOT / "unity" / "Assets" / "Editor").rglob("*.cs")]
        for path in places:
            with self.subTest(path=str(path.relative_to(ROOT))):
                self.assertIsNone(by_name.search(path.read_text(errors="ignore")))


@unittest.skipUnless(shutil.which("zsh"), "the capture scripts are zsh")
class StillGateLockTests(LockCase):
    """The capture scripts refuse a held lock before they touch Unity or the request."""

    def copy_tools(self):
        root = self.support / "wt"
        (root / "tools").mkdir(parents=True)
        for name in ("still-gate.sh", "still-gate-character.sh", "unity_gui.py"):
            shutil.copy(ROOT / "tools" / name, root / "tools" / name)
        return root

    def test_a_held_lock_refuses_the_capture_by_name_and_writes_no_request(self):
        root = self.copy_tools()
        holder = self.process()
        gui.acquire("another session's capture", holder.pid, worktree="/wt/a")
        for script in ("still-gate.sh", "still-gate-character.sh"):
            with self.subTest(script=script):
                run = subprocess.run(["zsh", str(root / "tools" / script)], capture_output=True, text=True)
                self.assertEqual(gui.HELD, run.returncode, run.stdout + run.stderr)
                self.assertIn("another session's capture (pid " + str(holder.pid), run.stderr)
                self.assertFalse((root / "unity" / "Temp" / "gs-still-request.json").exists())
        self.assertEqual(holder.pid, json.loads(self.lock.read_text())["pid"])

    def capture(self):
        """still-gate.sh against a stand-in editor on its worktree. A stub osascript fails every click, so no
        real window is fronted; the script still holds the lock and waits for the done file."""
        root = self.copy_tools()
        stub = self.support / "bin"
        stub.mkdir()
        (stub / "osascript").write_text("#!/bin/sh\nexit 1\n")
        (stub / "osascript").chmod(0o755)
        binary = self.support / "Editor" / "Unity.app" / "Contents" / "MacOS" / "Unity"
        binary.parent.mkdir(parents=True)
        binary.write_text("#!/bin/sh\nwhile :; do sleep 1; done\n")
        binary.chmod(0o755)
        editor = subprocess.Popen([str(binary), "-projectPath", str(root / "unity")])
        self.addCleanup(self.stop, editor)
        env = dict(os.environ, PATH=str(stub) + os.pathsep + os.environ["PATH"])
        script = subprocess.Popen(["zsh", str(root / "tools" / "still-gate.sh"), "--timeout", "60"], env=env,
                                  stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
        self.addCleanup(self.stop, script)
        for _ in range(100):
            if self.lock.exists() or script.poll() is not None:
                break
            time.sleep(0.1)
        self.assertIsNone(script.poll(), "the capture ended before it held the lock")
        self.assertEqual(script.pid, json.loads(self.lock.read_text())["pid"])
        time.sleep(3)  # both clicks are past; the script is waiting for the done file
        self.assertIsNone(script.poll())
        return root, editor, script

    def test_the_capture_holds_the_lock_until_the_done_file_and_then_releases_it(self):
        root, _, script = self.capture()
        (root / "unity" / "Temp" / "gs-still-done.json").write_text("{}")
        out, err = script.communicate(timeout=20)
        self.assertEqual(0, script.returncode, out + err)
        self.assertIn("Holding the GUI Unity lock", out)
        self.assertFalse(self.lock.exists())

    def test_a_terminated_capture_releases_the_lock(self):
        _, _, script = self.capture()
        script.terminate()
        script.communicate(timeout=20)
        self.assertEqual(143, script.returncode)
        self.assertFalse(self.lock.exists())

    def test_a_capture_whose_editor_quits_releases_the_lock(self):
        _, editor, script = self.capture()
        self.stop(editor)
        out, err = script.communicate(timeout=20)
        self.assertEqual(1, script.returncode)
        self.assertIn("quit before the capture finished", err)
        self.assertFalse(self.lock.exists())

    def test_with_no_editor_on_the_worktree_the_capture_releases_the_lock(self):
        root = self.copy_tools()
        run = subprocess.run(["zsh", str(root / "tools" / "still-gate.sh")], capture_output=True, text=True)
        self.assertEqual(0, run.returncode, run.stdout + run.stderr)
        self.assertIn("No Unity editor is open on " + str(root / "unity"), run.stdout)
        self.assertIn("Took the GUI Unity lock: tools/still-gate.sh", run.stderr)
        self.assertTrue((root / "unity" / "Temp" / "gs-still-request.json").exists())
        self.assertFalse(self.lock.exists())


if __name__ == "__main__":
    unittest.main()
