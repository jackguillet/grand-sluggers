"""Score every baked frame of the shipped swing.fbx against the runtime contract.

Reads the file back from disk, so this measures what ships, not the authoring
scene. It cannot replace the Unity still gate: it has no Unity importer, no
prefab bind, and no camera. It does cover the geometry the gate measures.
"""
import sys, json, importlib.util
from pathlib import Path
repo = Path("/home/user/grand-sluggers")
sys.path.insert(0, str(repo / "tools/blender"))
import bpy
from mathutils import Vector

rel = "unity/Assets/Art/Animation/Clips/swing.fbx"
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(repo / rel), ignore_leaf_bones=False)
import batting_stance
spec = importlib.util.spec_from_file_location("swing", repo / "tools/blender/hero_shared_swing.py")
swing = importlib.util.module_from_spec(spec); spec.loader.exec_module(swing)

def U(v): return Vector((-v.x, v.z, -v.y))
def flat(v): return Vector((v.x, 0.0, v.z)).normalized()
def cloud(n):
    deps = bpy.context.evaluated_depsgraph_get()
    ob = bpy.data.objects[n].evaluated_get(deps); m = ob.to_mesh()
    pts = [ob.matrix_world @ v.co for v in m.vertices]; ob.to_mesh_clear(); return pts
def center(n):
    p = cloud(n)
    lo = Vector(tuple(min(q[i] for q in p) for i in range(3)))
    hi = Vector(tuple(max(q[i] for q in p) for i in range(3)))
    return (lo + hi) * 0.5
def extent(n):
    c, p = center(n), cloud(n)
    return max(max(abs(q[i]-c[i]) for q in p) for i in range(3))
def seg(p,a,b):
    ax=b-a; u=0 if ax.length_squared<1e-9 else max(0,min(1,(p-a).dot(ax)/ax.length_squared))
    return (p-(a+ax*u)).length
def r3(v): return [round(c, 4) for c in v]

arm = next(o for o in bpy.data.objects if o.type=="ARMATURE")
HANDLE, ALIGN, RISE = (0.85-0.10)*1.28, 0.9659258, 0.70
BODIES = {"rio":(1.1269,1.0620,1.1151), "vale":(1.1127,1.4632,1.1765),
          "zig":(1.0632,0.6608,0.9900), "brondo":(1.5352,1.1328,1.4620),
          "konga":(1.5729,1.5340,1.5659), "ashlord":(1.5954,1.6992,1.6142)}
HELD = swing.KEY_TIMES[1] * 0.5
first, last = (int(round(v)) for v in arm.animation_data.action.frame_range)

frames, failures = [], []
for frame in range(first, last + 1):
    t = round((frame - first) / 60.0, 6)
    bpy.context.scene.frame_set(frame); bpy.context.view_layer.update()
    bat = arm.pose.bones["bat"]
    d = -(bat.matrix.to_3x3() @ Vector((0,1,0))).normalized()
    du, want = U(d), Vector(swing.barrel_direction_at(t))
    grip, hend = bat.head, bat.head + d*HANDLE
    hands = {}
    for h in ("lHand","rHand"):
        dist, tol = seg(center(h), grip, hend), extent(h) + 0.08*1.28
        hands[h] = {"toHandle": round(dist,4), "gateTolerance": round(tol,4)}
        if dist > tol: failures.append(f"f{frame} {h} off handle {dist:.3f} > {tol:.3f}")
    chest = flat(U(center("Stripe") - center("torsoMesh")))
    eyes = (center("EyeL") + center("EyeR")) * 0.5
    face = -flat(U(eyes - center("headMesh")))
    feet = flat(U(center("rShoe") - center("lShoe")))
    stance = batting_stance.target_at(t)
    want_chest = Vector((-stance["chest"].x, stance["chest"].z, -stance["chest"].y))
    rise = {}
    for b, s in BODIES.items():
        w = Vector((du.x*s[0], du.y*s[1], du.z*s[2]))
        rise[b] = round(w.y / w.length, 4)
    held = t <= HELD + 1e-9
    row = {"frame": frame, "t": t, "held": held,
           "chestForward": r3(chest), "renderedEyesForward": r3(face), "feetLine": r3(feet),
           "barrelDirection": r3(du), "barrelDotContract": round(du.dot(want), 4),
           "chestDotContract": round(chest.dot(want_chest), 4),
           "hands": hands, "loadedBarrelRise": rise}
    if chest.dot(want_chest) < 0.995:
        failures.append(f"f{frame} chest off contract {chest.dot(want_chest):.4f}")
    if du.dot(want) < 0.999:
        failures.append(f"f{frame} barrel off contract {du.dot(want):.4f}")
    if abs(feet.z) < ALIGN: failures.append(f"f{frame} feet {abs(feet.z):.3f}")
    if held:
        if chest.x < ALIGN: failures.append(f"f{frame} held chest {chest.x:.3f}")
        if face.z < ALIGN: failures.append(f"f{frame} held eyes {face.z:.3f}")
        for b, v in rise.items():
            if v < RISE: failures.append(f"f{frame} {b} loaded barrel rise {v:.3f}")
    frames.append(row)

out = {
    "ok": not failures,
    "source": rel,
    "tool": f"Blender {bpy.app.version_string} (bpy module, linux)",
    "covers": [
        "every baked frame, read back from the shipped FBX",
        "chest / rendered face / feet vs BattingStance, held window at the gate's alignment dot",
        "rendered hand meshes vs the physical handle at the gate's own tolerance",
        "barrel direction vs SwingPresentation",
        "loaded barrel rise after each shared captain's SharedRootScale",
    ],
    "doesNotCover": [
        "the Unity still gate itself (no Unity editor on this runner)",
        "human look acceptance, which stays a separate gate",
    ],
    "worstBarrelDotContract": min(r["barrelDotContract"] for r in frames),
    "worstHandOverTolerance": round(max(
        r["hands"][h]["toHandle"] / r["hands"][h]["gateTolerance"]
        for r in frames for h in ("lHand", "rHand")), 4),
    "lowestHeldBarrelRise": min(
        v for r in frames if r["held"] for v in r["loadedBarrelRise"].values()),
    "failures": failures,
    "frames": frames,
}
dest = repo / "scratchpad/validation/549-sideways-stance/dcc-frames-after.json"
dest.write_text(json.dumps(out, indent=1) + "\n")
print("ok" if out["ok"] else "FAIL", dest)
print("worst barrel dot", out["worstBarrelDotContract"],
      "worst hand/tolerance", out["worstHandOverTolerance"],
      "lowest held rise", out["lowestHeldBarrelRise"])
