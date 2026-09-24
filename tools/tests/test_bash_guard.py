import importlib.util
import json
from pathlib import Path
import subprocess
import sys
import unittest

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("bash_guard", ROOT / "tools" / "bash_guard.py")
guard = importlib.util.module_from_spec(spec)
spec.loader.exec_module(guard)


def denied(command):
    return guard.decide({"tool_name": "Bash", "tool_input": {"command": command}}) is not None


class SweepingAdds(unittest.TestCase):
    def test_refuses_every_sweeping_form(self):
        for command in [
            "git add -A",
            "git add --all",
            "git add .",
            "git add ./",
            "git add :/",
            "git add -Av",
            "git -C /tmp/wt add -A",
            "git -c core.autocrlf=false add .",
            "cd /tmp/wt && git add -A && git commit -m x",
            "git status; git add .",
            "bash -c 'git add -A'",
            "FOO=1 git add --all",
        ]:
            with self.subTest(command=command):
                self.assertTrue(denied(command))

    def test_allows_explicit_paths(self):
        for command in [
            "git add tools/bash_guard.py .claude/settings.json",
            "git add -p src/GrandSluggers.Sim/Rules.cs",
            "git add -- docs/how-to-play.md",
            "git commit -m 'git add -A is banned'",
            "echo 'git add .'",
            "git status && git diff --stat",
            "git add .gitignore",
            "git add ./tools/bash_guard.py",
        ]:
            with self.subTest(command=command):
                self.assertFalse(denied(command))


class FullSuite(unittest.TestCase):
    def test_refuses_dotnet_test_without_a_filter(self):
        for command in [
            "dotnet test",
            "dotnet test GrandSluggers.sln",
            "cd src && dotnet test GrandSluggers.Sim.Tests",
            "dotnet build && dotnet test --no-build",
            "sh -c \"dotnet test\"",
        ]:
            with self.subTest(command=command):
                self.assertTrue(denied(command))

    def test_allows_filtered_runs_and_the_fast_script(self):
        for command in [
            "dotnet test --filter 'FullyQualifiedName~RulesTests&Kind!=Balance'",
            "dotnet test src/GrandSluggers.Sim.Tests --filter=Kind!=Balance",
            "dotnet test --list-tests",
            "tools/test-fast.sh RulesTests",
            "dotnet build GrandSluggers.sln",
            "dotnet run --project src/GrandSluggers.Cli -- match",
        ]:
            with self.subTest(command=command):
                self.assertFalse(denied(command))


class HookContract(unittest.TestCase):
    def run_hook(self, payload):
        return subprocess.run([sys.executable, str(ROOT / "tools" / "bash_guard.py")],
                              input=json.dumps(payload), capture_output=True, text=True, check=True)

    def test_deny_is_a_pre_tool_use_decision_with_advice(self):
        out = self.run_hook({"tool_name": "Bash", "tool_input": {"command": "git add -A"}})
        decision = json.loads(out.stdout)["hookSpecificOutput"]
        self.assertEqual("PreToolUse", decision["hookEventName"])
        self.assertEqual("deny", decision["permissionDecision"])
        self.assertIn("explicit paths", decision["permissionDecisionReason"])

    def test_allowed_and_unparseable_commands_print_nothing(self):
        for payload in [
            {"tool_name": "Bash", "tool_input": {"command": "ls"}},
            {"tool_name": "Bash", "tool_input": {"command": "echo 'unclosed"}},
            {"tool_name": "Edit", "tool_input": {"file_path": "x"}},
        ]:
            with self.subTest(payload=payload):
                self.assertEqual("", self.run_hook(payload).stdout)

    def test_settings_wire_the_guard_on_bash(self):
        settings = json.loads((ROOT / ".claude" / "settings.json").read_text())
        commands = [h["command"] for entry in settings["hooks"]["PreToolUse"] if entry.get("matcher") == "Bash"
                    for h in entry["hooks"]]
        self.assertTrue(any("tools/bash_guard.py" in c for c in commands))


if __name__ == "__main__":
    unittest.main()
