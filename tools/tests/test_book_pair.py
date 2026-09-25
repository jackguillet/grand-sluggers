"""tools/book-pair.py: docs/how-to-play.md and HowToPlay change in the same PR, or the PR says why not."""
import importlib.util
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("book_pair", ROOT / "tools" / "book-pair.py")
pair = importlib.util.module_from_spec(spec)
spec.loader.exec_module(pair)

BOOK = "docs/how-to-play.md"
CODE = "src/GrandSluggers.Sim/HowToPlay.cs"


class BookPairTests(unittest.TestCase):
    def test_both_or_neither_holds(self):
        self.assertIsNone(pair.verdict({BOOK, CODE}, set()))
        self.assertIsNone(pair.verdict({"src/GrandSluggers.Sim/Match.cs"}, set()))

    def test_one_side_alone_fails_and_names_the_other(self):
        self.assertIn(CODE, pair.verdict({BOOK}, set()))
        self.assertIn(BOOK, pair.verdict({"src/GrandSluggers.Sim/HowToPlay.Screens.cs"}, set()))

    def test_the_override_label_passes(self):
        self.assertIsNone(pair.verdict({CODE}, {pair.OVERRIDE}))

    def test_tutorial_copy_is_not_the_book(self):
        self.assertIsNone(pair.verdict({"src/GrandSluggers.Sim/HowToPlay.Tutorials.cs"}, set()))
