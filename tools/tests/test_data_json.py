import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location("data_json", Path(__file__).parents[1] / "data_json.py")
data_json = importlib.util.module_from_spec(spec)
spec.loader.exec_module(data_json)

REPO = Path(__file__).parents[2]


class DataJsonTests(unittest.TestCase):
    def test_every_data_file_reads(self):
        files = sorted((REPO / "data").rglob("*.json"))
        self.assertTrue(files)
        for path in files:
            with self.subTest(path=str(path.relative_to(REPO))):
                self.assertIsNotNone(data_json.read(path))

    def test_comments_and_trailing_commas_go_and_strings_stay(self):
        text = '''{
          // a note, with a comma,
          "url": "http://x//y", /* block, ] */ "slash": "a \\" // b",
          "list": [1, 2, /* last */ ],
          "note": "ends, }",
        }'''
        self.assertEqual({"url": "http://x//y", "slash": 'a " // b', "list": [1, 2], "note": "ends, }"},
                         data_json.loads(text))

    def test_an_unterminated_block_comment_is_refused(self):
        with self.assertRaises(ValueError):
            data_json.loads('{"a": 1 /* never closed }')


if __name__ == "__main__":
    unittest.main()
