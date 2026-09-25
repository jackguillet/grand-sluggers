"""tools/spec-provenance.py refuses an added spec line that names an issue, a PR or a date, and nothing else."""
import importlib.util
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("spec_provenance", ROOT / "tools" / "spec-provenance.py")
checker = importlib.util.module_from_spec(spec)
spec.loader.exec_module(checker)

DIFF = """diff --git a/docs/spec/05-batting.md b/docs/spec/05-batting.md
--- a/docs/spec/05-batting.md
+++ b/docs/spec/05-batting.md
@@ -10,1 +10,4 @@
-- The window is 9 frames. ✅ (#860)
+- The window is 9 frames. ✅ (S-125)
+- The barrel narrows on a charge, shipped in PR #883.
+- Jack accepted it on September 22, 2026.
+- Measured on 2026-09-22.
"""


class SpecProvenanceTests(unittest.TestCase):
    def test_only_added_lines_with_provenance_fail(self):
        found = [(p, n, m) for p, n, m, _ in checker.offences(DIFF)]
        self.assertEqual([("docs/spec/05-batting.md", 11, "PR #"),
                          ("docs/spec/05-batting.md", 12, "September 22, 2026"),
                          ("docs/spec/05-batting.md", 13, "2026-")], found)

    def test_a_removed_line_and_a_rule_pass(self):
        diff = DIFF.split("+- The barrel")[0]
        self.assertEqual([], checker.offences(diff))

    def test_the_shipped_spec_carries_no_provenance(self):
        files = sorted((ROOT / "docs" / "spec").glob("*.md")) + [ROOT / "docs" / "gameplay-spec.md"]
        self.assertGreater(len(files), 10)
        for path in files:
            for n, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
                with self.subTest(path=path.name, line=n):
                    self.assertIsNone(checker.PROVENANCE.search(line))


if __name__ == "__main__":
    unittest.main()
