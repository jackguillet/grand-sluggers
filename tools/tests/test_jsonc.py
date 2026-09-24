"""tools/jsonc.py reads every data file the way the sim does (DataJson: comments skipped, trailing commas allowed)."""
import importlib.util
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("jsonc", ROOT / "tools" / "jsonc.py")
jsonc = importlib.util.module_from_spec(spec)
spec.loader.exec_module(jsonc)


class JsoncTests(unittest.TestCase):
    def test_every_data_file_reads(self):
        files = sorted((ROOT / "data").rglob("*.json"))
        self.assertGreater(len(files), 30)
        for path in files:
            with self.subTest(path=str(path.relative_to(ROOT))):
                jsonc.load(path)

    def test_comments_go_and_strings_stay(self):
        text = '{\n  // a note\n  "url": "http://x/*y*/", /* block */ "a": [1, 2,],\n  "q": "say \\"//\\"",\n}\n'
        self.assertEqual({"url": "http://x/*y*/", "a": [1, 2], "q": 'say "//"'}, jsonc.loads(text))

    def test_a_trailing_comma_inside_a_string_stays(self):
        text = '{"note": "ends, }", "list": [1, /* last */ ], "b": "x ,]",}'
        self.assertEqual({"note": "ends, }", "list": [1], "b": "x ,]"}, jsonc.loads(text))

    def test_an_unterminated_block_comment_is_refused(self):
        with self.assertRaises(ValueError):
            jsonc.loads('{"a": 1 /* open')


if __name__ == "__main__":
    unittest.main()
