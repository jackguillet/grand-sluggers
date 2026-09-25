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
import json
import re
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


SIM = "src/GrandSluggers.Sim"


class SimPackageMetaTests(unittest.TestCase):
    """The Sim is a local Unity package, so every import writes a script meta beside each source (#1217). Those guids are
    read by nothing: no asset references a Sim script (the Sim has noEngineReferences) and every assembly links the Sim
    by name. So script metas are local and ignored, and these checks keep that safe."""

    def test_no_sim_script_meta_is_tracked(self):
        tracked = [str(p.relative_to(ROOT)) for p in tracked_metas()
                   if str(p.relative_to(ROOT)).startswith(SIM + "/") and p.name.endswith(".cs.meta")]
        self.assertEqual([], sorted(tracked), "Sim script metas are local (.gitignore); git rm --cached them")

    def test_sim_script_metas_are_ignored(self):
        probe = f"{SIM}/SimScriptMetaProbe.cs.meta"
        result = subprocess.run(["git", "check-ignore", "-q", "--no-index", probe], cwd=ROOT)
        self.assertEqual(0, result.returncode, f"{probe} must be ignored by .gitignore")

    def test_the_sim_stays_a_guid_free_package(self):
        # No engine types in the Sim, so no scene, prefab or asset can hold a Sim script by guid.
        sim = json.loads((ROOT / SIM / "GrandSluggers.Sim.asmdef").read_text(encoding="utf-8"))
        self.assertTrue(sim.get("noEngineReferences"), "the Sim asmdef must keep noEngineReferences")
        # Every Unity assembly references others by name; a GUID: reference would need the tracked guid back.
        for asmdef in sorted((ROOT / "unity").rglob("*.asmdef")):
            if "Library" in asmdef.parts or "PackageCache" in asmdef.parts:
                continue
            refs = json.loads(re.sub(r"^\s*//.*$", "", asmdef.read_text(encoding="utf-8"), flags=re.M)).get("references", [])
            by_guid = [r for r in refs if r.startswith("GUID:")]
            self.assertEqual([], by_guid, f"{asmdef.relative_to(ROOT)} references by GUID; reference assemblies by name")
