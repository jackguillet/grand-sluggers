from pathlib import Path
import subprocess
import unittest


ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / "tools" / "dcc-still.sh"


class DccStillContractTests(unittest.TestCase):
    def test_print_names_the_pr_path(self):
        output = subprocess.check_output(
            ["zsh", str(SCRIPT), "body", "--print"], cwd=ROOT, text=True
        )
        self.assertIn("scratchpad/stills/dcc-body.png", output)

        extras = subprocess.check_output(
            ["zsh", str(SCRIPT), "extras", "--print"], cwd=ROOT, text=True
        )
        self.assertIn("scratchpad/stills/dcc-extras.png", extras)

        takes = subprocess.check_output(
            ["zsh", str(SCRIPT), "takes", "swing", "--print"], cwd=ROOT, text=True
        )
        self.assertIn("scratchpad/stills/dcc-swing.png", takes)

        harbor = subprocess.check_output(
            ["zsh", str(SCRIPT), "harbor", "--print"], cwd=ROOT, text=True
        )
        self.assertIn("scratchpad/stills/dcc-harbor-kit.png", harbor)

    def test_script_does_not_diff_pixels(self):
        script = SCRIPT.read_text()
        self.assertNotIn("compare", script.lower())
        self.assertNotIn("magick", script.lower())
        self.assertIn("scratchpad/stills", script)


if __name__ == "__main__":
    unittest.main()
