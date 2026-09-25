"""tools/original_ip.py keeps Nintendo names out of what a player sees or hears (AGENTS.md "Art")."""
import importlib.util
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("original_ip", ROOT / "tools" / "original_ip.py")
ip = importlib.util.module_from_spec(spec)
spec.loader.exec_module(ip)


class OriginalIpTests(unittest.TestCase):
    def test_the_repository_is_clean(self):
        self.assertEqual([], ip.scan(ip.tracked()))

    def test_a_string_literal_is_a_hit_and_a_comment_is_not(self):
        code = '// Never Nintendo samples.\nvar name = "Mario Kart bat";\n'
        self.assertEqual([(2, "Mario")], ip.hits_in(code, strings_only=True))

    def test_data_text_and_plurals_are_hits(self):
        self.assertEqual([(1, "mushrooms"), (1, "Princess")], ip.hits_in('{"prop": "mushrooms", "who": "Princess"}', False))

    def test_an_original_name_passes(self):
        self.assertEqual([], ip.hits_in('{"captain": "rio", "park": "harbor-diamond", "note": "Marionette"}', False))
