#!/usr/bin/env python3
"""Every take on hero-shared, both hands, validated per frame, with clay sheets.

One pose table in body terms (flex forward, abduct outward, twist; lean, turn,
tilt; lift) is the single source of motion. The script converts to bone-local
Eulers, solves the swing hands to the contract targets, aims the batting
stance at data/art/batting-stance.json, keys the bat, reflects handed takes
across the sagittal plane exactly, falsifies every frame, and exports one
armature-only FBX per clip (and `{clip}-L` for handed clips).

  /opt/homebrew/bin/blender -b --python tools/blender/hero_shared_takes.py -- \
    --out unity/Assets/Art/Animation/Clips \
    --resources unity/Assets/Resources/Art/Animation/Clips \
    --sheets scratchpad/takes [--only swing,pitch]

Contract: docs/character-motion.md. Runtime clocks: src/GrandSluggers.Sim/Motion.cs.
"""
from __future__ import annotations

import argparse
import json
import math
import shutil
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Quaternion, Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import batting_stance  # noqa: E402
import hero_shared_blockout as body  # noqa: E402

FPS = 60
RUN_HZ = 2.55
RUN_DUR = 1 / RUN_HZ
JUMP_DUR = 0.55
JUMP_PEAK = 4.2
HOLD = 0.20

LIMBS = ("lUpper", "rUpper", "lFore", "rFore", "lThigh", "rThigh", "lShin", "rShin")
SPINE = ("root", "pelvis", "spine", "torso", "neck", "head")
MIRROR = {name: (("r" if name[0] == "l" else "l") + name[1:]
                if name.startswith(("l", "r")) and name != "root" else name)
          for name in body.BONES}
ORDER = tuple(body.BONES)
REFLECT = Matrix(((-1, 0, 0, 0), (0, 1, 0, 0), (0, 0, 1, 0), (0, 0, 0, 1)))


# ---------------------------------------------------------------- body terms
#
# A pose is {bone: {term: degrees}} plus optional "lift" (root, world units).
#   spine (root/torso/head): lean (+ forward), turn (+ toward the character's
#     left), tilt (+ left shoulder down)
#   limbs: flex (+ forward; on a shin + is the knee bend, heel back), abduct
#     (+ away from the body), twist (+ same sense on both sides).
# Terms are converted per side so a left and a right limb read alike and the
# mirror is a sign flip by construction. assert_conventions() proves the
# signs on the built rig before any take is baked.

def _euler_for(bone: str, terms: dict) -> tuple[float, float, float]:
    flex = terms.get("flex", 0.0)
    if bone in SPINE:
        lean = terms.get("lean", 0.0)
        turn = terms.get("turn", 0.0)
        tilt = terms.get("tilt", 0.0)
        return (math.radians(lean), math.radians(turn), math.radians(-tilt))
    abduct = terms.get("abduct", 0.0)
    twist = terms.get("twist", 0.0)
    left = bone.startswith("l")
    # A knee bends the heel back; every other limb term is + forward.
    x = flex if bone.endswith("Shin") else -flex
    y = twist if left else -twist
    z = -abduct if left else abduct
    return (math.radians(x), math.radians(y), math.radians(z))


def _lerp(a, b, u):
    return a + (b - a) * u


def _smooth(u):
    u = max(0.0, min(1.0, u))
    return u * u * (3 - 2 * u)


def _blend_pose(a: dict, b: dict, u: float) -> dict:
    out = {}
    bones = set(a) | set(b)
    for bone in bones:
        if bone == "lift":
            out["lift"] = _lerp(a.get("lift", 0.0), b.get("lift", 0.0), u)
            continue
        ta = a.get(bone, {})
        tb = b.get(bone, {})
        out[bone] = {k: _lerp(ta.get(k, 0.0), tb.get(k, 0.0), u) for k in set(ta) | set(tb)}
    return out


def pose_at(keys, t: float, ease: bool, loop: bool, duration: float) -> dict:
    times = [k[0] for k in keys]
    if loop:
        t = t % duration
        if t > times[-1]:
            a, b = keys[-1], (duration, keys[0][1])
            u = (t - a[0]) / max(1e-9, duration - a[0])
            return _blend_pose(a[1], b[1], _smooth(u) if ease else u)
    index, u = batting_stance.span_at(t, times)
    a = keys[index][1]
    b = keys[min(index + 1, len(keys) - 1)][1]
    return _blend_pose(a, b, _smooth(u) if ease else u)


def clear_pose(arm):
    for pb in arm.pose.bones:
        pb.rotation_mode = "XYZ"
        pb.rotation_euler = (0.0, 0.0, 0.0)
        pb.rotation_quaternion = (1.0, 0.0, 0.0, 0.0)
        pb.location = (0.0, 0.0, 0.0)
        pb.scale = (1.0, 1.0, 1.0)
    bpy.context.view_layer.update()


def apply_pose(arm, pose: dict):
    clear_pose(arm)
    for bone, terms in pose.items():
        if bone == "lift":
            continue
        pb = arm.pose.bones[bone]
        pb.rotation_mode = "XYZ"
        pb.rotation_euler = _euler_for(bone, terms)
    lift = pose.get("lift", 0.0)
    if lift:
        # root points up: bone-local Y is world Z.
        arm.pose.bones["root"].location = (0.0, lift, 0.0)
    bpy.context.view_layer.update()


def center(name: str) -> Vector:
    return batting_stance.rendered_center(name)


def assert_conventions(arm):
    """The sign conventions above, proven on the built rig."""
    def probe(pose, name):
        apply_pose(arm, pose)
        return center(name)
    rest_l = probe({}, "lHand")
    rest_r = probe({}, "rHand")
    assert probe({"lUpper": {"flex": 40}}, "lHand").y < rest_l.y - 0.3, "left flex must move the hand forward (-Y)"
    assert probe({"rUpper": {"flex": 40}}, "rHand").y < rest_r.y - 0.3, "right flex must move the hand forward (-Y)"
    assert probe({"lUpper": {"abduct": 40}}, "lHand").x > rest_l.x + 0.3, "left abduct must move the hand outward (+X)"
    assert probe({"rUpper": {"abduct": 40}}, "rHand").x < rest_r.x - 0.3, "right abduct must move the hand outward (-X)"
    head_rest = probe({}, "headMesh")
    assert probe({"torso": {"lean": 30}}, "headMesh").y < head_rest.y - 0.5, "torso lean must move the head forward"
    stripe_rest = probe({}, "Stripe")
    assert probe({"torso": {"turn": 40}}, "Stripe").x > stripe_rest.x + 0.2, "torso turn must swing the chest to the left (+X)"
    assert probe({"torso": {"tilt": 30}}, "headMesh").x > head_rest.x + 0.3, "tilt + must drop toward the left (+X)"
    foot_rest = probe({}, "lShoe")
    assert probe({"lThigh": {"flex": 40}}, "lShoe").y < foot_rest.y - 0.3, "thigh flex must move the foot forward"
    assert probe({"lShin": {"flex": 60}}, "lShoe").y > foot_rest.y + 0.2, "shin flex must bend the knee, heel back"
    assert abs(probe({"lift": 1.0}, "Hip").z - probe({}, "Hip").z - 1.0) < 1e-3, "lift must raise the root by world units"
    clear_pose(arm)


# ---------------------------------------------------------------- the takes

def K(**bones):
    return bones


def limb(flex=0.0, abduct=0.0, twist=0.0):
    return {"flex": flex, "abduct": abduct, "twist": twist}


def spine(lean=0.0, turn=0.0, tilt=0.0):
    return {"lean": lean, "turn": turn, "tilt": tilt}


REST = K(torso=spine(4), lUpper=limb(6, 12), rUpper=limb(6, 12), lFore=limb(8), rFore=limb(8),
         lThigh=limb(4), rThigh=limb(4), lShin=limb(6), rShin=limb(6))

IDLE = [
    (0.00, K(torso=spine(4), head=spine(0, 0), lUpper=limb(6, 12), rUpper=limb(6, 12), lFore=limb(8), rFore=limb(8),
             lThigh=limb(4), rThigh=limb(4), lShin=limb(6), rShin=limb(6))),
    (1.00, K(torso=spine(7), head=spine(2, 5), lUpper=limb(4, 14), rUpper=limb(4, 14), lFore=limb(10), rFore=limb(10),
             lThigh=limb(4), rThigh=limb(4), lShin=limb(6), rShin=limb(6))),
]

FIELD = [
    (0.00, K(torso=spine(22), head=spine(-6), lUpper=limb(34, 22), rUpper=limb(34, 22), lFore=limb(48), rFore=limb(48),
             lThigh=limb(26, 10), rThigh=limb(26, 10), lShin=limb(30), rShin=limb(30), lift=-0.12)),
    (1.00, K(torso=spine(25), head=spine(-4, 4), lUpper=limb(36, 24), rUpper=limb(36, 24), lFore=limb(50), rFore=limb(50),
             lThigh=limb(28, 10), rThigh=limb(28, 10), lShin=limb(32), rShin=limb(32), lift=-0.16)),
]

CHEER = [
    (0.00, K(torso=spine(-6), head=spine(-10), lUpper=limb(150, 25), rUpper=limb(150, 25), lFore=limb(20), rFore=limb(20),
             lThigh=limb(2), rThigh=limb(2), lShin=limb(4), rShin=limb(4), lift=0.0)),
    (0.40, K(torso=spine(-8), head=spine(-12), lUpper=limb(165, 30), rUpper=limb(165, 30), lFore=limb(10), rFore=limb(10),
             lThigh=limb(-4), rThigh=limb(-4), lShin=limb(2), rShin=limb(2), lift=0.35)),
]

CHARM = [
    (0.00, K(torso=spine(2, 0, 4), head=spine(4, 8, 10), lUpper=limb(10, 60), rUpper=limb(10, 60), lFore=limb(30), rFore=limb(30),
             lThigh=limb(4), rThigh=limb(4), lShin=limb(6), rShin=limb(6))),
    (0.60, K(torso=spine(2, 0, -4), head=spine(4, -8, -10), lUpper=limb(12, 66), rUpper=limb(12, 66), lFore=limb(34), rFore=limb(34),
             lThigh=limb(4), rThigh=limb(4), lShin=limb(6), rShin=limb(6))),
]


def _stride(amp: float, duration: float):
    """One gait cycle. Phase 0: left foot planted forward, right arm forward."""
    plant = K(torso=spine(10 + 6 * amp, 16 * amp), head=spine(2, 6 * amp),
              lUpper=limb(-58 * amp, 8), rUpper=limb(50 * amp, 8), lFore=limb(16), rFore=limb(36 * amp + 8),
              lThigh=limb(54 * amp), rThigh=limb(-38 * amp), lShin=limb(10), rShin=limb(52 * amp + 6),
              lift=0.10 * amp)
    passing = K(torso=spine(12 + 6 * amp, 0), head=spine(2, 0),
                lUpper=limb(-4, 8), rUpper=limb(-4, 8), lFore=limb(20), rFore=limb(20),
                lThigh=limb(8 * amp), rThigh=limb(8 * amp), lShin=limb(10), rShin=limb(40 * amp + 6),
                lift=0.16 * amp)
    other = K(torso=spine(10 + 6 * amp, -16 * amp), head=spine(2, -6 * amp),
              lUpper=limb(50 * amp, 8), rUpper=limb(-58 * amp, 8), lFore=limb(36 * amp + 8), rFore=limb(16),
              lThigh=limb(-38 * amp), rThigh=limb(54 * amp), lShin=limb(52 * amp + 6), rShin=limb(10),
              lift=0.10 * amp)
    passing2 = K(torso=spine(12 + 6 * amp, 0), head=spine(2, 0),
                 lUpper=limb(-4, 8), rUpper=limb(-4, 8), lFore=limb(20), rFore=limb(20),
                 lThigh=limb(8 * amp), rThigh=limb(8 * amp), lShin=limb(40 * amp + 6), rShin=limb(10),
                 lift=0.16 * amp)
    return [(0.0, plant), (duration * 0.25, passing), (duration * 0.5, other), (duration * 0.75, passing2)]


RUN = _stride(1.0, RUN_DUR)
WALK = _stride(0.45, RUN_DUR / 0.55)


def _jump_keys():
    keys = []
    for t in (0.0, 0.12, 0.28, 0.42, 0.55):
        u = t / JUMP_DUR
        take = _smooth(min(1.0, u / 0.28))
        hang = _smooth(max(0.0, (u - 0.22) / 0.28))
        land = _smooth(max(0.0, (u - 0.62) / 0.38))
        coil = _lerp(55, 18, take)
        air = _lerp(coil, 8, hang)
        thigh = _lerp(air, 42, land)
        shin = _lerp(_lerp(70, 20, take), _lerp(12, 50, land), min(1.0, hang * 0.2 + land))
        arms = _lerp(_lerp(-20, 150, take), 40, land)
        keys.append((t, K(torso=spine(_lerp(18, 4, hang) + 10 * land), head=spine(-8 * hang),
                          lUpper=limb(arms, 14), rUpper=limb(arms, 14), lFore=limb(20), rFore=limb(20),
                          lThigh=limb(thigh, 6), rThigh=limb(thigh, 6), lShin=limb(shin), rShin=limb(shin),
                          lift=JUMP_PEAK * math.sin(math.pi * u))))
    return keys


JUMP = _jump_keys()

# Named default motion data; both hands are baked from this one source.
BASEBALL = json.loads((Path(__file__).resolve().parents[2] / "data/art/baseball-takes.json").read_text())
BASEBALL_TAKES = {row["id"]: row for row in BASEBALL["takes"]}

SCOOP = [
    (0.00, K(torso=spine(14, 4), head=spine(10), lUpper=limb(20, 10), rUpper=limb(22, 10), lFore=limb(24), rFore=limb(26),
             lThigh=limb(28, 8), rThigh=limb(24, 8), lShin=limb(24), rShin=limb(22), lift=-0.10)),
    (0.10, K(torso=spine(24, 2), head=spine(16), lUpper=limb(34, 8), rUpper=limb(38, 8), lFore=limb(38), rFore=limb(42),
             lThigh=limb(42, 10), rThigh=limb(38, 10), lShin=limb(36), rShin=limb(34), lift=-0.30)),
    (0.22, K(torso=spine(32, 0), head=spine(18), lUpper=limb(44, 6), rUpper=limb(48, 6), lFore=limb(48), rFore=limb(52),
             lThigh=limb(50, 12), rThigh=limb(46, 12), lShin=limb(44), rShin=limb(42), lift=-0.45)),
    (0.50, K(torso=spine(10, -4), head=spine(6), lUpper=limb(16, 12), rUpper=limb(14, 12), lFore=limb(18), rFore=limb(20),
             lThigh=limb(20, 4), rThigh=limb(18, 4), lShin=limb(16), rShin=limb(14), lift=-0.04)),
]

SLIDE = [
    (0.00, K(torso=spine(12), head=spine(4), lUpper=limb(10, 18), rUpper=limb(10, 18), lFore=limb(14), rFore=limb(16),
             lThigh=limb(20), rThigh=limb(12), lShin=limb(18), rShin=limb(14), lift=0.0)),
    (0.18, K(torso=spine(-30, 0, 6), head=spine(20), lUpper=limb(-28, 22), rUpper=limb(-48, 12), lFore=limb(14), rFore=limb(16),
             lThigh=limb(70, 12), rThigh=limb(84, -8), lShin=limb(58), rShin=limb(66), lift=-0.55)),
    (0.40, K(torso=spine(-10, 0, 4), head=spine(12), lUpper=limb(-10, 22), rUpper=limb(-20, 12), lFore=limb(14), rFore=limb(16),
             lThigh=limb(42, 6), rThigh=limb(30, -4), lShin=limb(38), rShin=limb(42), lift=-0.42)),
]

CATCH = [
    (0.00, K(torso=spine(6), head=spine(-6), lUpper=limb(40, 24), rUpper=limb(40, 24), lFore=limb(30), rFore=limb(30),
             lThigh=limb(12, 6), rThigh=limb(12, 6), lShin=limb(14), rShin=limb(14))),
    (HOLD, K(torso=spine(-4), head=spine(-14), lUpper=limb(130, 18), rUpper=limb(130, 18), lFore=limb(20), rFore=limb(20),
             lThigh=limb(10, 6), rThigh=limb(10, 6), lShin=limb(12), rShin=limb(12))),
]

DIVE = [
    (0.00, K(torso=spine(70), head=spine(-30), lUpper=limb(170, 10), rUpper=limb(170, 10), lFore=limb(6), rFore=limb(6),
             lThigh=limb(-30, 6), rThigh=limb(-30, 6), lShin=limb(20), rShin=limb(20), lift=0.20)),
    (HOLD, K(torso=spine(72), head=spine(-32), lUpper=limb(172, 10), rUpper=limb(172, 10), lFore=limb(6), rFore=limb(6),
             lThigh=limb(-32, 6), rThigh=limb(-32, 6), lShin=limb(20), rShin=limb(20), lift=0.18)),
]

CROUCH = [
    (0.00, K(torso=spine(32), head=spine(-10), lUpper=limb(40, 18), rUpper=limb(40, 18), lFore=limb(60), rFore=limb(60),
             lThigh=limb(68, 12), rThigh=limb(68, 12), lShin=limb(70), rShin=limb(70), lift=-0.35)),
    (HOLD, K(torso=spine(33), head=spine(-10), lUpper=limb(40, 18), rUpper=limb(40, 18), lFore=limb(60), rFore=limb(60),
             lThigh=limb(68, 12), rThigh=limb(68, 12), lShin=limb(70), rShin=limb(70), lift=-0.36)),
]

STEAL_LEAD = [
    (0.00, K(torso=spine(22, 12), head=spine(0, 14), lUpper=limb(28, 22), rUpper=limb(12, 28), lFore=limb(30), rFore=limb(30),
             lThigh=limb(42, 10), rThigh=limb(18, 6), lShin=limb(36), rShin=limb(20), lift=-0.35)),
    (HOLD, K(torso=spine(24, 12), head=spine(0, 16), lUpper=limb(30, 22), rUpper=limb(14, 28), lFore=limb(30), rFore=limb(30),
             lThigh=limb(44, 10), rThigh=limb(20, 6), lShin=limb(36), rShin=limb(20), lift=-0.36)),
]

SPIN = [
    (0.00, K(torso=spine(4), head=spine(-4), lUpper=limb(10, 70), rUpper=limb(10, 70), lFore=limb(10), rFore=limb(10),
             lThigh=limb(4), rThigh=limb(4), lShin=limb(6), rShin=limb(6))),
    (HOLD, K(torso=spine(4), head=spine(-4), lUpper=limb(10, 72), rUpper=limb(10, 72), lFore=limb(10), rFore=limb(10),
             lThigh=limb(4), rThigh=limb(4), lShin=limb(6), rShin=limb(6))),
]

# The two swings (#613) live in data/art/swing-takes.json, shared with
# SwingPresentation.SlapKeys / ChargeKeys: per key the rendered hand centers,
# the grip socket and the barrel direction (Unity batter-local, right-handed),
# and the legs in body terms. Arms are solved, the spine is aimed at
# data/art/batting-stance.json, the bat is keyed.
SWING_CATALOG = Path(__file__).resolve().parents[2] / "data/art/swing-takes.json"
SWING_SLAP = "swing-slap"
SWING_CHARGE = "swing-charge"


def _load_swings():
    doc = json.loads(SWING_CATALOG.read_text())
    swings = {}
    for row in doc["takes"]:
        keys = sorted(row["keys"], key=lambda k: k["t"])
        times = [round(float(k["t"]), 4) for k in keys]
        swings[row["id"]] = {
            "times": times,
            "legs": {t: {bone: (v if bone == "lift" else limb(v["flex"], v["abduct"], v["twist"]))
                         for bone, v in k["legs"].items()} for t, k in zip(times, keys)},
            "hands": {t: {"lFore": tuple(k["leftHand"]), "rFore": tuple(k["rightHand"])} for t, k in zip(times, keys)},
            "grip": {t: tuple(k["grip"]) for t, k in zip(times, keys)},
            "barrel": {t: tuple(k["barrel"]) for t, k in zip(times, keys)},
        }
    return doc, swings


SWING_DOC, SWINGS = _load_swings()
SWING_CONTACT = float(SWING_DOC["contactAt"])
SWING_FINISH = float(SWING_DOC["finishAt"])
# The follow-through key (Motion.SwingDur); the take goes on to its held finish.
SWING_DUR = 0.50
HAND_MESH = {"lFore": "lHand", "rFore": "rHand"}
ARM_PARENT = {"lFore": "lUpper", "rFore": "rUpper"}
HANDLE_LENGTH = 0.85 * 1.28
ELBOW_POLE_DROP = 2.5
HAND_SOLVE_TOLERANCE = 0.01
STANCE_LANDMARKS = dict(chest_front="Stripe", chest_center="torsoMesh",
                        eye_left="EyeL", eye_right="EyeR", head_center="headMesh",
                        foot_left="lShoe", foot_right="rShoe")
# #623: the bat never passes through the head. The physical bat HeroActor draws runs
# from the knob 0.29 model units behind the grip to the barrel end 2.10 past it, radius
# 0.12 (SwingPresentation.BatStartFromGrip / BatEndFromGrip / ModelBarrelRadius), all
# times the shared bat scale. Its surface must stay this far from the rendered head.
BAT_SCALE = 1.28
BAT_FROM_GRIP = (-0.29 * BAT_SCALE, 2.10 * BAT_SCALE)
BAT_RADIUS = 0.12 * BAT_SCALE
BAT_HEAD_CLEARANCE = float(SWING_DOC["batHeadClearance"])


def _interp_table(table, t, times):
    index, u = batting_stance.span_at(t, times)
    a = table[times[index]]
    b = table[times[min(index + 1, len(times) - 1)]]
    if isinstance(a, dict):
        return {k: tuple(_lerp(x, y, u) for x, y in zip(a[k], b[k])) for k in a}
    return tuple(_lerp(x, y, u) for x, y in zip(a, b))


# ------------------------------------------------------------ swing solving

ARM_UPPER_LEN = next((Vector(j["tail"]) - Vector(j["head"])).length
                     for j in body.RIG["joints"] if j["name"] == "rUpper")
# The hand mesh center in the forearm's frame: 0.83 down the bone, 0.10 forward.
HAND_IN_FORE = Vector((0.0, 2.48 - body.ANATOMY["handCenter"][2], body.ANATOMY["handCenter"][1]))


def _frame(head: Vector, y_axis: Vector, x_axis: Vector) -> Matrix:
    y = y_axis.normalized()
    x = (x_axis - y * x_axis.dot(y)).normalized()
    z = x.cross(y).normalized()
    m = Matrix.Identity(4)
    m.col[0][:3] = x
    m.col[1][:3] = y
    m.col[2][:3] = z
    m.col[3][:3] = head
    return m


def solve_two_bone(arm, fore: str, hand_target: Vector, pole: Vector):
    """Analytic two-bone IK: put the rendered hand center on `hand_target`
    with the elbow toward `pole`. Deterministic; no constraint evaluation.
    Returns the miss distance."""
    upper = ARM_PARENT[fore]
    shoulder = (arm.matrix_world @ arm.pose.bones[upper].head).copy()
    # Include the authored wrist articulation in the effector offset. The
    # solver remains two-bone, but the hands no longer freeze to the forearm.
    hand_in_fore = (arm.matrix_world @ arm.pose.bones[fore].matrix).inverted() @ center(HAND_MESH[fore])
    effector_len = hand_in_fore.length
    to_target = hand_target - shoulder
    reach = to_target.length
    max_reach = ARM_UPPER_LEN + effector_len - 1e-4
    if reach > max_reach:
        to_target = to_target.normalized() * max_reach
        reach = max_reach
    u = to_target.normalized()
    side = pole - shoulder
    v = (side - u * side.dot(u))
    if v.length < 1e-6:
        v = Vector((0.0, 0.0, -1.0)) - u * Vector((0.0, 0.0, -1.0)).dot(u)
    v.normalize()
    cos_a = max(-1.0, min(1.0, (ARM_UPPER_LEN ** 2 + reach ** 2 - effector_len ** 2) / (2 * ARM_UPPER_LEN * reach)))
    sin_a = math.sqrt(max(0.0, 1 - cos_a * cos_a))
    elbow = shoulder + (u * cos_a + v * sin_a) * ARM_UPPER_LEN
    hinge = u.cross(v).normalized()
    # Effector direction from the elbow, then the forearm Y axis so that
    # R_f · HAND_IN_FORE lands on the target: rotate the effector back by the
    # hand offset angle within the bend plane.
    d = (shoulder + to_target - elbow).normalized()
    offset_angle = math.atan2(-hand_in_fore.z, hand_in_fore.y)
    best = None
    for sign in (1.0, -1.0):
        y_f = Matrix.Rotation(sign * offset_angle, 3, hinge) @ d
        m_f = _frame(elbow, y_f, hinge)
        landed = elbow + (m_f.to_3x3() @ hand_in_fore)
        miss = (landed - (shoulder + to_target)).length
        if best is None or miss < best[0]:
            best = (miss, m_f)
    m_u = _frame(shoulder, elbow - shoulder, hinge)
    for name, matrix in ((upper, m_u), (fore, best[1])):
        pb = arm.pose.bones[name]
        pb.rotation_mode = "QUATERNION"
        pb.matrix = arm.matrix_world.inverted() @ matrix
        bpy.context.view_layer.update()
    return (center(HAND_MESH[fore]) - hand_target).length


def solve_rendered_hands(arm, targets):
    missed = {}
    for fore, target in targets.items():
        target = Vector(target)
        shoulder = arm.matrix_world @ arm.pose.bones[ARM_PARENT[fore]].head
        pole = shoulder + Vector((0.0, 0.0, -ELBOW_POLE_DROP))
        missed[fore] = solve_two_bone(arm, fore, target, pole)
    return missed


def aim_bat(arm, direction_unity, grip_unity):
    """Socket -Y from the grip toward the barrel; the keyed Euler is the roll seed."""
    bat = arm.pose.bones["bat"]
    bpy.context.view_layer.update()
    current = -(bat.matrix.to_3x3() @ Vector((0, 1, 0))).normalized()
    target = batting_stance.unity_to_dcc(direction_unity)
    correction = current.rotation_difference(target)
    matrix = bat.matrix.copy()
    bat.rotation_mode = "QUATERNION"
    bat.matrix = Matrix.LocRotScale(
        batting_stance.unity_to_dcc(grip_unity, normalize=False),
        correction @ matrix.to_quaternion(),
        matrix.to_scale())
    bpy.context.view_layer.update()


def point_segment_distance(point, start, end):
    axis = end - start
    u = 0 if axis.length_squared < 1e-8 else max(0, min(1, (point - start).dot(axis) / axis.length_squared))
    return (point - (start + axis * u)).length


def head_sphere():
    """The rendered head as a sphere: its posed mesh center and largest half-extent."""
    deps = bpy.context.evaluated_depsgraph_get()
    ob = bpy.data.objects["headMesh"].evaluated_get(deps)
    mesh = ob.to_mesh()
    points = [ob.matrix_world @ v.co for v in mesh.vertices]
    ob.to_mesh_clear()
    low = Vector(tuple(min(p[i] for p in points) for i in range(3)))
    high = Vector(tuple(max(p[i] for p in points) for i in range(3)))
    return (low + high) * 0.5, max(high[i] - low[i] for i in range(3)) * 0.5


def bat_head_clearance(grip, axis):
    """Surface-to-surface distance from the physical bat to the rendered head (negative = inside)."""
    head, radius = head_sphere()
    start = grip + axis * BAT_FROM_GRIP[0]
    end = grip + axis * BAT_FROM_GRIP[1]
    return point_segment_distance(head, start, end) - radius - BAT_RADIUS


def ground_support(arm):
    deps=bpy.context.evaluated_depsgraph_get()
    low=1e6
    for name in ("lShoe","rShoe"):
        ob=bpy.data.objects[name].evaluated_get(deps);mesh=ob.to_mesh()
        low=min(low,min((ob.matrix_world @ v.co).z for v in mesh.vertices));ob.to_mesh_clear()
    root=arm.pose.bones["root"];matrix=root.matrix.copy();matrix.translation.z-=low
    root.matrix=matrix;bpy.context.view_layer.update()


def pose_swing_frame(arm, t, clip=SWING_SLAP):
    swing = SWINGS[clip]
    times = swing["times"]
    apply_pose(arm, pose_at([(k, swing["legs"][k]) for k in times], t, ease=False, loop=False, duration=SWING_FINISH))
    ground_support(arm)
    batting_stance.author_visible_stance(arm, t, bats=batting_stance.BATS_RIGHT, **STANCE_LANDMARKS)
    targets = {name: batting_stance.unity_to_dcc(v, normalize=False)
               for name, v in _interp_table(swing["hands"], t, times).items()}
    missed = solve_rendered_hands(arm, targets)
    for name, distance in missed.items():
        if distance > HAND_SOLVE_TOLERANCE:
            raise RuntimeError(f"{clip} {name} hand solve missed at {t:.4f}: {distance:.4f}")
    aim_bat(arm, _interp_table(swing["barrel"], t, times), _interp_table(swing["grip"], t, times))


def validate_swing_frame(arm, t, bats, clip=SWING_SLAP):
    swing = SWINGS[clip]
    bat = arm.pose.bones["bat"]
    actual = -(bat.matrix.to_3x3() @ Vector((0, 1, 0))).normalized()
    direction = Vector(_interp_table(swing["barrel"], t, swing["times"]))
    if bats == batting_stance.BATS_LEFT:
        direction.x = -direction.x
    expected = batting_stance.unity_to_dcc(tuple(direction))
    if actual.dot(expected) < 0.999:
        raise RuntimeError(f"swing {bats} bat direction missed at {t:.4f}: {tuple(actual)} vs {tuple(expected)}")
    grip = bat.head
    handle_end = grip + actual * HANDLE_LENGTH
    along = {}
    for name in ("lHand", "rHand"):
        hand = center(name)
        distance = point_segment_distance(hand, grip, handle_end)
        if distance > 0.30:
            raise RuntimeError(f"swing {bats} {name} left the handle at {t:.4f}: {distance:.3f}")
        along[name] = (hand - grip).dot(actual)
    lead = "lHand" if bats == batting_stance.BATS_RIGHT else "rHand"
    top = "rHand" if lead == "lHand" else "lHand"
    if not 0.0 <= along[lead] < along[top]:
        raise RuntimeError(f"swing {bats}: lead {lead} must hold the knob end at {t:.4f}: {along}")
    clearance = bat_head_clearance(grip, actual)
    if clearance < BAT_HEAD_CLEARANCE:
        raise RuntimeError(f"{clip} {bats}: the bat passes {clearance:+.3f} from the head at {t:.4f}; "
                           f"it must clear by {BAT_HEAD_CLEARANCE:.2f} (#623)")
    batting_stance.validate_visible_stance(t, bats=bats, arm_ob=arm, **STANCE_LANDMARKS)


def validate_pitch_release(arm, bats):
    """At Release the throwing hand is forward of the chest and high, and the
    stride foot (opposite the throwing hand) stands forward of the other."""
    throw_hand = "rHand" if bats == batting_stance.BATS_RIGHT else "lHand"
    stride, back = ("lShoe", "rShoe") if throw_hand == "rHand" else ("rShoe", "lShoe")
    hand = center(throw_hand)
    chest = center("torsoMesh")
    head = center("headMesh")
    if hand.y > chest.y - 0.6:
        raise RuntimeError(f"pitch {bats}: throwing hand is not forward of the chest at release ({hand.y:.2f} vs {chest.y:.2f})")
    if hand.z < head.z - 0.6:
        raise RuntimeError(f"pitch {bats}: release is not over the top ({hand.z:.2f} vs head {head.z:.2f})")
    if center(stride).y > center(back).y - 0.5:
        raise RuntimeError(f"pitch {bats}: stride foot {stride} is not forward of {back}")


def validate_feet_on_ground(arm, clip, t, allowed_sink):
    low = min(center("lShoe").z, center("rShoe").z)
    if low < -0.30 - allowed_sink:
        raise RuntimeError(f"{clip}: a foot is under the dirt at {t:.3f} ({low:.2f})")


# ------------------------------------------------------------ mirror

def rest_matrices(arm):
    return {b.name: (arm.matrix_world @ b.matrix_local).copy() for b in arm.data.bones}


def reflect_pose(arm, source: dict, rest: dict):
    """Set the pose that is the exact sagittal reflection of `source`
    ({bone: armature-space matrix}). D = posed · rest⁻¹ is the world delta of
    a bone; its mirror partner gets S·D·S · rest(partner)."""
    for name in ORDER:
        partner = MIRROR[name]
        if name in ("bat", "glove"):
            # Sockets reflect as rigid props: the same position mirror and
            # frame rule as a bone, without a partner rest to hang on.
            target = REFLECT @ source[name] @ REFLECT
        else:
            delta = source[partner] @ rest[partner].inverted()
            target = REFLECT @ delta @ REFLECT @ rest[name]
        pb = arm.pose.bones[name]
        pb.rotation_mode = "QUATERNION"
        pb.matrix = target
        bpy.context.view_layer.update()


def landmark_snapshot():
    return {n: center(n) for n in body.LANDMARKS}


def assert_reflected(right: dict, left: dict, clip: str, t: float):
    pairs = {"lHand": "rHand", "rHand": "lHand", "lShoe": "rShoe", "rShoe": "lShoe", "EyeL": "EyeR", "EyeR": "EyeL"}
    for name, r in right.items():
        partner = pairs.get(name, name)
        expected = Vector((-r.x, r.y, r.z))
        actual = left[partner]
        if (actual - expected).length > 1e-3:
            raise RuntimeError(f"{clip}-L is not the reflection of {clip} at {t:.3f}: {partner} {tuple(actual)} vs {tuple(expected)}")


# ------------------------------------------------------------ baking

class Take:
    def __init__(self, clip, keys=None, duration=None, loop=False, handed=False, ease=True,
                 mark=None, sheet_times=None, sink=0.0, custom=None, validate=None, view="three-quarter"):
        self.clip = clip
        self.view = view
        self.keys = keys or []
        self.duration = duration if duration is not None else self.keys[-1][0]
        self.loop = loop
        self.handed = handed
        self.ease = ease
        self.mark = mark
        self.sheet_times = sheet_times or [k[0] for k in self.keys]
        self.sink = sink
        self.custom = custom
        self.validate = validate


def frame_times(take):
    frames = int(round(take.duration * FPS))
    if take.loop:
        return [(f, f / FPS) for f in range(frames)]
    return [(f, f / FPS) for f in range(frames + 1)]


def local_rotation(pb):
    if pb.rotation_mode == "QUATERNION":
        return pb.rotation_quaternion.copy()
    if pb.rotation_mode == "AXIS_ANGLE":
        return Quaternion(pb.rotation_axis_angle[1:], pb.rotation_axis_angle[0])
    return pb.rotation_euler.to_quaternion()


def local_snapshot(arm):
    """Parent-relative location and rotation per bone: exactly what a key stores."""
    return {name: (arm.pose.bones[name].location.copy(), local_rotation(arm.pose.bones[name])) for name in ORDER}


def write_action(arm, name, frames, locals_by_frame):
    """Key recorded locals into a fresh action. Posing never runs with an
    action assigned: Blender re-evaluates fcurves on every depsgraph update
    and would overwrite a pose set between keys."""
    action = bpy.data.actions.new(name)
    action.use_fake_user = True
    arm.animation_data.action = action
    for frame, _ in frames:
        bpy.context.scene.frame_set(frame + 1)
        for bone, (location, rotation) in locals_by_frame[frame].items():
            pb = arm.pose.bones[bone]
            pb.rotation_mode = "QUATERNION"
            pb.location = location
            pb.rotation_quaternion = rotation
            pb.keyframe_insert(data_path="rotation_quaternion", frame=frame + 1)
            pb.keyframe_insert(data_path="location", frame=frame + 1)
    linearize(action)
    arm.animation_data.action = None
    return action


def snapshot_matrices(arm):
    return {name: arm.pose.bones[name].matrix.copy() for name in ORDER}


def linearize(action):
    for layer in action.layers:
        for strip in layer.strips:
            for bag in strip.channelbags:
                for fc in bag.fcurves:
                    for kp in fc.keyframe_points:
                        kp.interpolation = "LINEAR"


def export_take(arm, action, take, out_dir: Path, name: str):
    scene = bpy.context.scene
    arm.animation_data.action = action
    frames = frame_times(take)
    scene.frame_start = 1
    scene.frame_end = frames[-1][0] + 1
    scene.frame_set(1)
    path = out_dir / f"{name}.fbx"
    # Same call shape as the body export: every object considered, only the
    # armature written. Exporting a lone selected armature made the exporter
    # key the armature object itself, and that object-level curve carried the
    # Blender-to-Unity basis as animation: the body lay flat on the dirt.
    bpy.ops.object.select_all(action="SELECT")
    bpy.context.view_layer.objects.active = arm
    bpy.ops.export_scene.fbx(
        filepath=str(path),
        use_selection=False,
        object_types={"ARMATURE"},
        bake_anim=True,
        bake_anim_use_all_bones=True,
        bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=False,
        bake_anim_force_startend_keying=True,
        bake_anim_step=1.0,
        # 0.0 keeps every constant curve, including one on the armature object
        # that carries the DCC-to-Unity basis as animation and lays the body
        # flat. A near-zero factor drops constant curves and nothing else.
        bake_anim_simplify_factor=0.01,
        **body.FBX_AXES,
    )
    return path


def bake(arm, take: Take, out_dir: Path, sheets: Path | None, resources: Path | None):
    import clay
    scene = bpy.context.scene
    scene.render.fps = FPS
    if arm.animation_data is None:
        arm.animation_data_create()
    arm.animation_data.action = None
    rest = rest_matrices(arm)
    frames = frame_times(take)
    matrices = {}
    snaps = {}
    locals_r = {}
    tiles = []

    def want_tile(t):
        return sheets is not None and any(abs(t - st) < 0.5 / FPS for st in take.sheet_times)

    def fail_sheet():
        if sheets is not None and tiles:
            clay.sheet(tiles, sheets / f"{take.clip}-FAILED.png", columns=min(5, len(tiles)))

    for frame, t in frames:
        if take.custom is not None:
            take.custom(arm, t)
        else:
            apply_pose(arm, pose_at(take.keys, t, take.ease, take.loop, take.duration))
        review_equipment(take.clip)
        if want_tile(t):
            tiles.append(clay.render(sheets / f"{take.clip}-{t:.2f}.png", take.view, 360, 480))
        try:
            if take.validate is not None:
                take.validate(arm, t, batting_stance.BATS_RIGHT)
            validate_feet_on_ground(arm, take.clip, t, take.sink)
        except Exception:
            fail_sheet()
            raise
        matrices[frame] = snapshot_matrices(arm)
        snaps[frame] = landmark_snapshot()
        locals_r[frame] = local_snapshot(arm)
    right = write_action(arm, take.clip, frames, locals_r)
    outputs = [export_take(arm, right, take, out_dir, take.clip)]
    arm.animation_data.action = None

    if take.handed:
        locals_l = {}
        for frame, t in frames:
            reflect_pose(arm, matrices[frame], rest)
            review_equipment(take.clip, left=True)
            if want_tile(t):
                tiles.append(clay.render(sheets / f"{take.clip}-L-{t:.2f}.png", clay.mirror_view(take.view), 360, 480))
            try:
                assert_reflected(snaps[frame], landmark_snapshot(), take.clip, t)
                if take.validate is not None:
                    take.validate(arm, t, batting_stance.BATS_LEFT)
            except Exception:
                fail_sheet()
                raise
            locals_l[frame] = local_snapshot(arm)
        left = write_action(arm, take.clip + "-L", frames, locals_l)
        outputs.append(export_take(arm, left, take, out_dir, take.clip + "-L"))
        arm.animation_data.action = None

    if sheets is not None and tiles:
        clay.sheet(tiles, sheets / f"{take.clip}.png", columns=min(5, len(tiles)))
    for out in outputs:
        print("take", out.name, out.stat().st_size)
        if resources is not None:
            resources.mkdir(parents=True, exist_ok=True)
            shutil.copy2(out, resources / out.name)
    clear_pose(arm)
    return outputs


def swing_frame(clip):
    def custom(arm, t):
        pose_swing_frame(arm, t, clip)
    return custom


def swing_validate(clip):
    def validate(arm, t, bats):
        validate_swing_frame(arm, t, bats, clip)
    return validate


def pitch_validate(arm, t, bats):
    if abs(t - 0.42) < 0.5 / FPS:
        validate_pitch_release(arm, bats)


def held_swing_frame(t_source):
    def custom(arm, t):
        pose_swing_frame(arm, t_source, SWING_SLAP)
    return custom


def miss_frame(arm, t):
    pose_swing_frame(arm, SWING_DUR, SWING_SLAP)
    head = arm.pose.bones["head"]
    head.rotation_mode = "QUATERNION"
    head.matrix = head.matrix @ Matrix.Rotation(math.radians(25), 4, "X")
    bpy.context.view_layer.update()


def bunt_frame(arm, t):
    row = BASEBALL["bunt"]
    apply_pose(arm, row["pose"])
    ground_support(arm)
    batting_stance.author_visible_stance(arm, 0.0, bats=batting_stance.BATS_RIGHT, **STANCE_LANDMARKS)
    axis = Vector(row["barrel"]).normalized()
    grip = Vector(row["grip"])
    targets = {"lFore": batting_stance.unity_to_dcc(grip + axis*row["leadAlong"], normalize=False),
               "rFore": batting_stance.unity_to_dcc(grip + axis*row["topAlong"], normalize=False)}
    missed = solve_rendered_hands(arm, targets)
    if max(missed.values()) > HAND_SOLVE_TOLERANCE:
        raise RuntimeError(f"bunt hand solve missed: {missed}")
    aim_bat(arm, axis, grip)


def bunt_validate(arm, t, bats):
    row=BASEBALL["bunt"]; bat=arm.pose.bones["bat"]
    axis=-(bat.matrix.to_3x3() @ Vector((0,1,0))).normalized()
    lead,top=("lHand","rHand") if bats==batting_stance.BATS_RIGHT else ("rHand","lHand")
    for hand,along in ((lead,row["leadAlong"]),(top,row["topAlong"])):
        miss=(center(hand)-(bat.head+axis*along)).length
        if miss > .02: raise RuntimeError(f"bunt {bats} {hand} misses bat by {miss:.3f}")
    if abs(axis.z) > .02: raise RuntimeError("bunt barrel must be level")


def solve_leg(arm, side, ankle):
    upper=arm.pose.bones[side+"Thigh"];lower=arm.pose.bones[side+"Shin"]
    hip=upper.head.copy();target=Vector(ankle);delta=target-hip;reach=delta.length
    a=arm.data.bones[side+"Thigh"].length;b=arm.data.bones[side+"Shin"].length
    if reach>a+b+.001:raise RuntimeError(f"{side} planted foot is unreachable: {reach:.3f}>{a+b:.3f}")
    direction=delta.normalized();pole=Vector((0,-1,0))
    bend=(pole-direction*pole.dot(direction)).normalized()
    cosine=max(-1,min(1,(a*a+reach*reach-b*b)/(2*a*reach)))
    knee=hip+a*(direction*cosine+bend*math.sqrt(1-cosine*cosine))
    hinge=direction.cross(bend).normalized()
    for pb,head,tail in ((upper,hip,knee),(lower,knee,target)):
        pb.rotation_mode="QUATERNION";pb.matrix=_frame(head,tail-head,hinge)
        bpy.context.view_layer.update()
    # Independent ankle: shoe rests flat while shin bends. The entire solved
    # pose, including feet and their sockets, is reflected for the other hand.
    foot=arm.pose.bones[side+"Foot"];matrix=arm.data.bones[side+"Foot"].matrix_local.copy()
    matrix.translation=target;foot.rotation_mode="QUATERNION";foot.matrix=matrix
    bpy.context.view_layer.update()


def baseball_frame(clip):
    row=BASEBALL_TAKES[clip];keys=row["keys"];times=[k["t"] for k in keys]
    def custom(arm,t):
        apply_pose(arm,pose_at([(k["t"],k["pose"]) for k in keys],t,True,False,row["duration"]))
        index,u=batting_stance.span_at(t,times);u=_smooth(u)
        a=keys[index]["feet"];b=keys[min(index+1,len(keys)-1)]["feet"]
        root=arm.pose.bones["root"];matrix=root.matrix.copy()
        matrix.translation=Vector((0,-_lerp(a["travel"],b["travel"],u),_lerp(a["rootLift"],b["rootLift"],u)))
        root.matrix=matrix;bpy.context.view_layer.update()
        for side,name in (("l","left"),("r","right")):
            solve_leg(arm,side,tuple(_lerp(x,y,u) for x,y in zip(a[name],b[name])))
    return custom


def baseball_validate(clip):
    release=BASEBALL_TAKES[clip]["releaseAt"]
    def validate(arm,t,bats):
        for side in ("l", "r"):
            if (arm.pose.bones[side+"Glove"].head-arm.pose.bones[side+"Wrist"].head).length > .001:
                raise RuntimeError(f"{clip} {bats} glove left wrist at {t}")
            if (arm.pose.bones[side+"Release"].head-center(side+"Hand")).length > .002:
                raise RuntimeError(f"{clip} {bats} release socket left palm at {t}")
        if abs(t-release)>.5/FPS:return
        if clip.startswith("pitch"):validate_pitch_release(arm,bats)
        else:
            hand="rHand" if bats==batting_stance.BATS_RIGHT else "lHand"
            if center(hand).y >= center("torsoMesh").y-.4:
                raise RuntimeError(f"{clip} {bats} release hand must lead chest")
    return validate


TAKES = [
    Take("idle", IDLE, duration=2.0, loop=True),
    Take("field", FIELD, duration=2.0, loop=True, sink=0.2),
    Take("cheer", CHEER, duration=0.8, loop=True),
    Take("charm", CHARM, duration=1.2, loop=True),
    Take("walk", WALK, duration=RUN_DUR / 0.55, loop=True, sink=0.3),
    Take("run", RUN, duration=RUN_DUR, loop=True, sink=0.3),
    Take("jump", JUMP, duration=JUMP_DUR, sink=0.2),
    *[Take(row["id"], [(k["t"],k["pose"]) for k in row["keys"]], duration=row["duration"],
           handed=True, mark=row["releaseAt"], custom=baseball_frame(row["id"]), validate=baseball_validate(row["id"]), sink=.3, view="three-quarter-right")
      for row in BASEBALL["takes"]],
    # Slap and charge (#613): both meet the ball at Contact and end on the held finish (#583).
    *[Take(clip, None, view="three-quarter-right", duration=SWING_FINISH, handed=True, ease=False, mark=SWING_CONTACT,
           custom=swing_frame(clip), validate=swing_validate(clip), sheet_times=SWINGS[clip]["times"], sink=0.2)
      for clip in (SWING_SLAP, SWING_CHARGE)],
    Take("checkSwing", None, view="three-quarter-right", duration=HOLD, handed=True, custom=held_swing_frame(0.20), validate=None,
         sheet_times=[0.0], sink=0.2),
    Take("bunt", None, view="three-quarter-right", duration=HOLD, handed=True, custom=bunt_frame, validate=bunt_validate, sheet_times=[0.0], sink=0.6),
    Take("miss", None, view="three-quarter-right", duration=HOLD, handed=True, custom=miss_frame, sheet_times=[0.0], sink=0.2),
    Take("catch", CATCH, duration=HOLD),
    Take("dive", DIVE, duration=HOLD, sink=1.0),
    Take("crouch", CROUCH, duration=HOLD, sink=0.8),
    Take("stealLead", STEAL_LEAD, duration=HOLD, sink=0.8),
    Take("spin", SPIN, duration=HOLD),
    Take("scoop", SCOOP, duration=0.50, mark=0.22, sink=0.9),
    Take("slide", SLIDE, duration=0.40, mark=0.18, sink=1.2),
]


def add_review_equipment(arm):
    """Use the actual authored props in evidence, seated at their bind origins."""
    import hero_shared_extras as extras
    props = extras.build_props({k:body.mat(k,v) for k,v in {**body.PALETTE,**extras.EXTRA_COLORS}.items()})
    keep={"bat-wood":"bat", "glove-brown":"lGlove", "glove-brown-R":"rGlove"}
    for name, ob in list(props.items()):
        if name not in keep:
            bpy.data.objects.remove(ob,do_unlink=True);continue
        bone=keep[name]
        matrix=arm.data.bones[bone].matrix_local
        rotation=Matrix.Rotation(math.pi/2 if bone=="bat" else -math.pi/2,4,"X")
        scale=1.28 if bone=="bat" else 1.42
        for v in ob.data.vertices:
            co=v.co.copy()
            if bone=="bat":co.z+=.85
            v.co=matrix @ (rotation @ (co*scale))
        body.skin(ob,arm,bone)
        ob.hide_render=True
    return keep


def review_equipment(clip, left=False):
    batting=clip.startswith("swing-") or clip in ("bunt","checkSwing","miss")
    for name in ("bat-wood","glove-brown","glove-brown-R"):
        ob=bpy.data.objects.get(name)
        if ob:
            ob.hide_render = (not batting if name=="bat-wood" else
                              batting or (name.endswith("-R") != left))



def main(argv):
    p = argparse.ArgumentParser()
    p.add_argument("--out", required=True)
    p.add_argument("--resources", default="")
    p.add_argument("--sheets", default="")
    p.add_argument("--only", default="")
    p.add_argument("--blend", default="", help="Save editable rig, mesh, props and baked actions for inspection.")
    args = p.parse_args(argv)
    out = Path(args.out).resolve()
    out.mkdir(parents=True, exist_ok=True)
    resources = Path(args.resources).resolve() if args.resources else None
    sheets = Path(args.sheets).resolve() if args.sheets else None
    only = {s.strip() for s in args.only.split(",") if s.strip()}
    arm = body.build_scene()
    add_review_equipment(arm)
    assert_conventions(arm)
    print("conventions ok")
    for take in TAKES:
        if only and take.clip not in only:
            continue
        bake(arm, take, out, sheets, resources)
    if args.blend:
        arm.animation_data.action=bpy.data.actions.get(SWING_SLAP)
        bpy.context.scene.frame_start=1
        bpy.context.scene.frame_end=37
        bpy.context.scene.frame_set(1)
        review_equipment(SWING_SLAP)
        bpy.ops.wm.save_as_mainfile(filepath=str(Path(args.blend).resolve()))
    print("takes done")


if __name__ == "__main__":
    argv = sys.argv
    if "--" in argv:
        argv = argv[argv.index("--") + 1 :]
    else:
        argv = argv[1:]
    try:
        main(argv)
    except Exception:
        # Blender -b exits 0 when a --python script raises; a take that misses its
        # contract must fail the bake gate (docs/character-motion.md).
        import traceback
        traceback.print_exc()
        sys.stdout.flush()
        sys.exit(1)
