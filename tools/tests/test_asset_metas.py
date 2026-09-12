"""Unity .meta identity is a rail, not a side effect of somebody's import.

Every branch in the repo once conflicted with main on the same 57-63 paths
because a bulk commit added Unity-generated .meta files that had been generated
independently on main and on the branches. A GUID is a reference: two assets
sharing one, or a .meta left behind after its asset moved, silently breaks the
prefab, controller, or scene that points at it.

These checks run in the portable CI job, so the next bulk `git add` fails here
instead of in the editor.
"""

from pathlib import Path
import subprocess
import unittest


ROOT = Path(__file__).resolve().parents[2]


def tracked_metas():
    output = subprocess.check_output(
        ["git", "ls-files", "-z", "*.meta"], cwd=ROOT, text=True
    )
    return [ROOT / name for name in output.split("\0") if name]


def guid_of(path):
    for line in path.read_text(encoding="utf-8", errors="replace").splitlines():
        if line.startswith("guid:"):
            return line.split(":", 1)[1].strip()
    return None


class AssetMetaIdentityTests(unittest.TestCase):
    def test_every_tracked_meta_declares_a_guid(self):
        missing = [
            str(path.relative_to(ROOT))
            for path in tracked_metas()
            if not guid_of(path)
        ]
        self.assertEqual([], sorted(missing), "tracked .meta without a guid")

    def test_guids_are_unique_across_tracked_metas(self):
        seen = {}
        collisions = []
        for path in tracked_metas():
            guid = guid_of(path)
            if not guid:
                continue
            name = str(path.relative_to(ROOT))
            if guid in seen:
                collisions.append(f"{guid} shared by {seen[guid]} and {name}")
            else:
                seen[guid] = name
        self.assertEqual([], sorted(collisions), "duplicate Unity GUIDs")

    def test_no_meta_outlives_its_asset(self):
        orphans = [
            str(path.relative_to(ROOT))
            for path in tracked_metas()
            if not path.with_suffix("").exists()
        ]
        self.assertEqual([], sorted(orphans), ".meta with no matching asset")
