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
Stage 3 motion (data/agent/dcc-stages.json): --sheets scratchpad/takes/{clip}.png, then --out.
"""
from __future__ import annotations

import argparse
import math
import shutil
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import jsonc  # noqa: E402  (the one reader for data files with // notes)

import bpy
from mathutils import Matrix, Quaternion, Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import batting_stance  # noqa: E402
import hero_shared_blockout as body  # noqa: E402

FPS = 60
RUN_HZ = 2.55
RUN_DUR = 1 / RUN_HZ
WALK_DUR = RUN_DUR / 0.55
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

# Motion styles (CH-12, CF-5): data/art/clips.json `styles`. A style re-bakes the
# styled clips from this one pose table with its own gait, idle, stance, windup
# and signature beat (STYLE_POSES below), into `{out}/styles/{id}/{clip}`. A
# style whose `reach` is not 1 moves the elbow and wrist in every take, so it
# re-bakes every clip.
REPO = Path(__file__).resolve().parents[2]
CLIPS_DOC = jsonc.load(REPO / "data/art/clips.json")
STYLE_DOC = CLIPS_DOC["styles"]
STYLE_ROWS = {row["id"]: row for row in STYLE_DOC["rows"]}
STYLED_CLIPS = tuple(STYLE_DOC["clips"])
STYLE_FOLDER = "styles"
# The style being baked (None: the shared takes) and the joint offsets its reach keys.
ACTIVE = {"style": None}
REACH = {"elbow": 0.0, "wrist": 0.0}


def use_style(style: str | None):
    """Pose the scene for a style: its build channels on the mesh, its reach on the joints."""
    ACTIVE["style"] = style
    row = STYLE_ROWS[style] if style else {"reachScale": 1.0, "bootsScale": 1.0}
    body.set_build({"reach": float(row["reachScale"]), "boots": float(row["bootsScale"])})
    REACH["elbow"], REACH["wrist"] = body.reach_offsets(float(row["reachScale"]))


# ---------------------------------------------------------------- body terms
#
# A pose is {bone: {term: degrees}} plus optional "lift" (root, world units).
#   spine (root/torso/head): lean (+ forward), turn (+ toward the character's
#     left), tilt (+ left shoulder down)
#   limbs: flex (+ forward; on a shin + is the knee bend, heel back), abduct
#     (+ away from the body), twist (+ same sense on both sides).
# Terms are converted per side so a left and a right limb read alike and the
# mirror is a sign flip by construction. assert_conventions() proves the
# signs on the built rig before any take is baked. A crouch `lift` is sized to
# the rig's legs (revision 3: hip to ankle 1.36); a hop or a bob is air.

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
        if bone in ("lift", "hop"):
            out[bone] = _lerp(a.get(bone, 0.0), b.get(bone, 0.0), u)
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
        if bone in ("lift", "hop"):
            continue
        pb = arm.pose.bones[bone]
        pb.rotation_mode = "XYZ"
        pb.rotation_euler = _euler_for(bone, terms)
    lift = pose.get("lift", 0.0)
    if lift:
        # root points up: bone-local Y is world Z.
        arm.pose.bones["root"].location = (0.0, lift, 0.0)
    if REACH["elbow"] or REACH["wrist"]:
        # A reach style: the elbow slides down the upper arm and the wrist down the forearm
        # (bone-local Y runs down the bone), where the stretched arm pieces end.
        for side in ("l", "r"):
            arm.pose.bones[side + "Fore"].location = (0.0, REACH["elbow"], 0.0)
            arm.pose.bones[side + "Wrist"].location = (0.0, REACH["wrist"], 0.0)
    bpy.context.view_layer.update()


def ground_hop(arm, pose: dict):
    """Stand the lowest sole on the dirt, then lift by the pose's `hop` (air, world units)."""
    ground_support(arm)
    hop = pose.get("hop", 0.0)
    if hop:
        root = arm.pose.bones["root"]
        matrix = root.matrix.copy()
        matrix.translation.z += hop
        root.matrix = matrix
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
             lThigh=limb(26, 10), rThigh=limb(26, 10), lShin=limb(30), rShin=limb(30), lift=-0.09)),
    (1.00, K(torso=spine(25), head=spine(-4, 4), lUpper=limb(36, 24), rUpper=limb(36, 24), lFore=limb(50), rFore=limb(50),
             lThigh=limb(28, 10), rThigh=limb(28, 10), lShin=limb(32), rShin=limb(32), lift=-0.12)),
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


# A gait in body terms. The defaults are the shared run; a motion style overrides
# some of them (STYLE_POSES). `gallop` swings both arms together, twice a cycle.
GAIT = dict(lean=10.0, leanAmp=6.0, spine=0.0, turn=16.0, tilt=0.0, head=2.0, headTurn=6.0,
            armBase=0.0, armFwd=50.0, armBack=58.0, armAbduct=8.0, gallop=False,
            foreBase=8.0, foreFwd=36.0, foreBack=16.0, forePass=20.0,
            thighBase=0.0, thighFwd=54.0, thighBack=38.0, thighPass=8.0, thighAbduct=0.0,
            shinLead=10.0, shinBase=6.0, kick=52.0, kickPass=40.0,
            bounce=0.10, bouncePass=0.16, liftBase=0.0)


def _stride(amp: float, duration: float, gait: dict | None = None):
    """One gait cycle. Phase 0: left foot planted forward, right arm forward."""
    g = {**GAIT, **(gait or {})}

    def plant(lead: str):
        trail = "r" if lead == "l" else "l"
        sign = 1.0 if lead == "l" else -1.0
        if g["gallop"]:
            arms = {lead + "Upper": limb(g["armBase"] - g["armBack"] * amp, g["armAbduct"]),
                    trail + "Upper": limb(g["armBase"] - g["armBack"] * amp, g["armAbduct"]),
                    lead + "Fore": limb(g["foreBack"]), trail + "Fore": limb(g["foreBack"])}
        else:
            arms = {lead + "Upper": limb(g["armBase"] - g["armBack"] * amp, g["armAbduct"]),
                    trail + "Upper": limb(g["armBase"] + g["armFwd"] * amp, g["armAbduct"]),
                    lead + "Fore": limb(g["foreBack"]), trail + "Fore": limb(g["foreFwd"] * amp + g["foreBase"])}
        return K(torso=spine(g["lean"] + g["leanAmp"] * amp, sign * g["turn"] * amp, sign * g["tilt"] * amp),
                 spine=spine(g["spine"]), head=spine(g["head"], sign * g["headTurn"] * amp), **arms,
                 **{lead + "Thigh": limb(g["thighBase"] + g["thighFwd"] * amp, g["thighAbduct"]),
                    trail + "Thigh": limb(g["thighBase"] - g["thighBack"] * amp, g["thighAbduct"]),
                    lead + "Shin": limb(g["shinLead"]), trail + "Shin": limb(g["kick"] * amp + g["shinBase"])},
                 lift=g["bounce"] * amp + g["liftBase"])

    def passing(stance: str):
        swing = "r" if stance == "l" else "l"
        arm = g["armBase"] + g["armFwd"] * amp if g["gallop"] else g["armBase"] - 4
        return K(torso=spine(g["lean"] + 2 + g["leanAmp"] * amp, 0), spine=spine(g["spine"]), head=spine(g["head"], 0),
                 lUpper=limb(arm, g["armAbduct"]), rUpper=limb(arm, g["armAbduct"]),
                 lFore=limb(g["forePass"]), rFore=limb(g["forePass"]),
                 lThigh=limb(g["thighBase"] + g["thighPass"] * amp, g["thighAbduct"]),
                 rThigh=limb(g["thighBase"] + g["thighPass"] * amp, g["thighAbduct"]),
                 **{stance + "Shin": limb(g["shinLead"]), swing + "Shin": limb(g["kickPass"] * amp + g["shinBase"])},
                 lift=g["bouncePass"] * amp + g["liftBase"])

    return [(0.0, plant("l")), (duration * 0.25, passing("l")), (duration * 0.5, plant("r")), (duration * 0.75, passing("r"))]


RUN = _stride(1.0, RUN_DUR)
WALK = _stride(0.45, WALK_DUR)


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


# ---------------------------------------------------------------- motion styles
#
# One row per style id in data/art/clips.json. A style is this pose table's
# dimension, not a second one: its gait is GAIT with overrides, its idle and its
# signature beat are keys like IDLE, its batting stance and its windup are
# deltas on the shared swing and pitch keys that fade out before the contract
# keys (contact, release), so every hand, bat and release contract still holds.
# Idles and signatures stand on the dirt (ground_hop); `hop` is air.

def add_terms(pose: dict, delta: dict, w: float = 1.0) -> dict:
    """pose + w x delta, term by term (body terms, lift and hop)."""
    out = {b: (dict(v) if isinstance(v, dict) else v) for b, v in pose.items()}
    for bone, terms in delta.items():
        if not isinstance(terms, dict):
            out[bone] = out.get(bone, 0.0) + w * terms
            continue
        row = out.setdefault(bone, {})
        for k, v in terms.items():
            row[k] = row.get(k, 0.0) + w * v
    return out


# Weights: the stance delta is whole through the swing's ready and load keys and
# gone by Contact; the windup delta is whole through the leg lift and gone by Release.
def stance_weight(t: float) -> float:
    return 1.0 if t <= 0.15 else max(0.0, 1.0 - (t - 0.15) / 0.15)


def windup_weight(t: float) -> float:
    return 1.0 if t <= 0.18 else max(0.0, 1.0 - (t - 0.18) / 0.24)


STYLE_POSES = {
    # Rio: the harbor kid is the neutral toy, eager on the balls of the feet.
    "harbor-kid": dict(
        gait={},
        idle=[
            (0.00, K(torso=spine(8), head=spine(-2, 0), lUpper=limb(10, 14), rUpper=limb(10, 14), lFore=limb(16), rFore=limb(16),
                     lThigh=limb(12, 4), rThigh=limb(12, 4), lShin=limb(22), rShin=limb(22))),
            (0.50, K(torso=spine(10), head=spine(-4, 12), lUpper=limb(14, 16), rUpper=limb(14, 16), lFore=limb(22), rFore=limb(22),
                     lThigh=limb(18, 4), rThigh=limb(18, 4), lShin=limb(32), rShin=limb(32))),
            (1.00, K(torso=spine(8), head=spine(-2, 0), lUpper=limb(10, 14), rUpper=limb(10, 14), lFore=limb(16), rFore=limb(16),
                     lThigh=limb(12, 4), rThigh=limb(12, 4), lShin=limb(22), rShin=limb(22))),
            (1.50, K(torso=spine(10), head=spine(-4, -12), lUpper=limb(14, 16), rUpper=limb(14, 16), lFore=limb(22), rFore=limb(22),
                     lThigh=limb(18, 4), rThigh=limb(18, 4), lShin=limb(32), rShin=limb(32))),
        ],
        stance={},
        windup={}, kick=1.0,
        # Fist pump: the bat hand punches the sky, twice a second.
        signature=([
            (0.00, K(torso=spine(-4), head=spine(-10), rUpper=limb(150, 20), rFore=limb(40), lUpper=limb(20, 20), lFore=limb(70),
                     lThigh=limb(6), rThigh=limb(6), lShin=limb(10), rShin=limb(10))),
            (0.25, K(torso=spine(-8), head=spine(-16), rUpper=limb(172, 10), rFore=limb(4), lUpper=limb(26, 22), lFore=limb(80),
                     lThigh=limb(0), rThigh=limb(0), lShin=limb(2), rShin=limb(2), hop=0.18)),
        ], 0.5),
    ),
    # Vale: tall and showy. Chin up, hand on the hip, the prancing high-knee run.
    "pageant": dict(
        gait=dict(lean=2, leanAmp=2, spine=-4, head=-8, headTurn=10, turn=10, armBase=6, armFwd=30, armBack=30,
                  armAbduct=26, foreBase=20, foreFwd=10, foreBack=30, forePass=30, thighFwd=66, thighBack=30,
                  thighPass=22, kick=64, kickPass=62, bounce=0.14, bouncePass=0.22),
        idle=[
            (0.00, K(pelvis=spine(0, 0, 5), torso=spine(-2, 0, -8), spine=spine(-4), head=spine(-8, 14, 6),
                     lUpper=limb(-18, 50, 60), lFore=limb(95), rUpper=limb(4, 14), rFore=limb(26),
                     lThigh=limb(2, 3), rThigh=limb(14, -6), lShin=limb(4), rShin=limb(28))),
            (1.00, K(pelvis=spine(0, 0, 6), torso=spine(-2, 0, -9), spine=spine(-4), head=spine(-10, -4, 2),
                     lUpper=limb(-18, 52, 60), lFore=limb(98), rUpper=limb(6, 16), rFore=limb(34),
                     lThigh=limb(2, 3), rThigh=limb(16, -6), lShin=limb(4), rShin=limb(32))),
        ],
        stance=K(torso=spine(-3), spine=spine(-3), head=spine(-5), lThigh=limb(-6, -4), rThigh=limb(-6, -4),
                 lShin=limb(-10), rShin=limb(-10)),
        windup=K(lUpper=limb(0, 40), head=spine(-8), torso=spine(-3)), kick=1.3,
        # Crowd wave: the glove hand high, sweeping side to side, the hip swung the other way.
        signature=([
            (0.00, K(pelvis=spine(0, 0, 4), torso=spine(-4, 0, -6), head=spine(-10, 10, 8), lUpper=limb(160, 34), lFore=limb(30),
                     rUpper=limb(-14, 44, 60), rFore=limb(95), lThigh=limb(2), rThigh=limb(12, -4), lShin=limb(4), rShin=limb(24))),
            (0.45, K(pelvis=spine(0, 0, 4), torso=spine(-4, 0, -6), head=spine(-10, -6, 8), lUpper=limb(160, 4), lFore=limb(10),
                     rUpper=limb(-14, 44, 60), rFore=limb(95), lThigh=limb(2), rThigh=limb(12, -4), lShin=limb(4), rShin=limb(24))),
        ], 0.9),
    ),
    # Zig: small and fast. Pumping arms at ninety degrees, a hard lean, legs a blur; hops on the toes when still.
    "speed": dict(
        gait=dict(lean=24, leanAmp=6, head=-12, turn=10, armFwd=70, armBack=70, armAbduct=10, foreBase=80, foreFwd=10,
                  foreBack=92, forePass=86, thighFwd=72, thighBack=46, thighPass=24, kick=100, kickPass=92,
                  bounce=0.06, bouncePass=0.10),
        idle=[
            (0.00, K(torso=spine(14), head=spine(-8), lUpper=limb(24, 12), rUpper=limb(24, 12), lFore=limb(84), rFore=limb(84),
                     lThigh=limb(16, 6), rThigh=limb(16, 6), lShin=limb(28), rShin=limb(28))),
            (0.25, K(torso=spine(12), head=spine(-10, 6), lUpper=limb(30, 12), rUpper=limb(18, 12), lFore=limb(90), rFore=limb(80),
                     lThigh=limb(8, 6), rThigh=limb(8, 6), lShin=limb(14), rShin=limb(14), hop=0.10)),
            (0.50, K(torso=spine(14), head=spine(-8), lUpper=limb(24, 12), rUpper=limb(24, 12), lFore=limb(84), rFore=limb(84),
                     lThigh=limb(16, 6), rThigh=limb(16, 6), lShin=limb(28), rShin=limb(28))),
            (0.75, K(torso=spine(12), head=spine(-10, -6), lUpper=limb(18, 12), rUpper=limb(30, 12), lFore=limb(80), rFore=limb(90),
                     lThigh=limb(8, 6), rThigh=limb(8, 6), lShin=limb(14), rShin=limb(14), hop=0.10)),
            (1.00, K(torso=spine(14), head=spine(-8), lUpper=limb(24, 12), rUpper=limb(24, 12), lFore=limb(84), rFore=limb(84),
                     lThigh=limb(16, 6), rThigh=limb(16, 6), lShin=limb(28), rShin=limb(28))),
            (1.25, K(torso=spine(12), head=spine(-10, 6), lUpper=limb(30, 12), rUpper=limb(18, 12), lFore=limb(90), rFore=limb(80),
                     lThigh=limb(8, 6), rThigh=limb(8, 6), lShin=limb(14), rShin=limb(14), hop=0.10)),
            (1.50, K(torso=spine(14), head=spine(-8), lUpper=limb(24, 12), rUpper=limb(24, 12), lFore=limb(84), rFore=limb(84),
                     lThigh=limb(16, 6), rThigh=limb(16, 6), lShin=limb(28), rShin=limb(28))),
            (1.75, K(torso=spine(12), head=spine(-10, -6), lUpper=limb(18, 12), rUpper=limb(30, 12), lFore=limb(80), rFore=limb(90),
                     lThigh=limb(8, 6), rThigh=limb(8, 6), lShin=limb(14), rShin=limb(14), hop=0.10)),
        ],
        stance=K(torso=spine(12), head=spine(-8), lThigh=limb(14, 2), rThigh=limb(14, 2), lShin=limb(22), rShin=limb(22)),
        windup=K(torso=spine(8), lUpper=limb(0, -10)), kick=0.8,
        # Victory hops: both fists up, three quick hops.
        signature=([
            (0.00, K(torso=spine(4), head=spine(-12), lUpper=limb(150, 30), rUpper=limb(150, 30), lFore=limb(30), rFore=limb(30),
                     lThigh=limb(20), rThigh=limb(20), lShin=limb(36), rShin=limb(36))),
            (0.15, K(torso=spine(-6), head=spine(-18), lUpper=limb(170, 22), rUpper=limb(170, 22), lFore=limb(6), rFore=limb(6),
                     lThigh=limb(4), rThigh=limb(4), lShin=limb(10), rShin=limb(10), hop=0.35)),
        ], 0.3),
    ),
    # Brondo: wide and heavy. Arms held out by the bulk, a stomping side-to-side run.
    "brick": dict(
        gait=dict(lean=6, spine=-4, turn=6, tilt=7, armBase=4, armFwd=22, armBack=22, armAbduct=38, foreBase=30, foreFwd=10,
                  foreBack=30, forePass=34, thighAbduct=10, thighFwd=36, thighBack=24, thighPass=6, kick=34, kickPass=30,
                  shinLead=14, bounce=0.02, bouncePass=0.12, liftBase=-0.05),
        idle=[
            (0.00, K(torso=spine(0, 0, 2), spine=spine(-5), head=spine(-4), lUpper=limb(8, 36), rUpper=limb(8, 36),
                     lFore=limb(34), rFore=limb(34), lThigh=limb(10, 16), rThigh=limb(10, 16), lShin=limb(16), rShin=limb(16))),
            (1.00, K(torso=spine(0, 0, -2), spine=spine(-7), head=spine(-5, 6), lUpper=limb(10, 40), rUpper=limb(10, 40),
                     lFore=limb(40), rFore=limb(40), lThigh=limb(12, 16), rThigh=limb(12, 16), lShin=limb(18), rShin=limb(18))),
        ],
        stance=K(torso=spine(4), lThigh=limb(8, 12), rThigh=limb(8, 12), lShin=limb(14), rShin=limb(14)),
        windup=K(torso=spine(-8), lUpper=limb(0, 14)), kick=0.7,
        # Double flex: elbows out, fists up, chest out; pulse.
        signature=([
            (0.00, K(spine=spine(-6), torso=spine(-4), head=spine(-8), lUpper=limb(10, 86), rUpper=limb(10, 86),
                     lFore=limb(96), rFore=limb(96), lThigh=limb(10, 16), rThigh=limb(10, 16), lShin=limb(16), rShin=limb(16))),
            (0.35, K(spine=spine(-9), torso=spine(-6), head=spine(-12), lUpper=limb(14, 92), rUpper=limb(14, 92),
                     lFore=limb(118), rFore=limb(118), lThigh=limb(14, 16), rThigh=limb(14, 16), lShin=limb(22), rShin=limb(22))),
        ], 0.7),
    ),
    # Konga: the ape. Long arms hanging from a hunch, bowed legs, a knuckle gallop.
    "ape": dict(
        gait=dict(lean=28, leanAmp=4, spine=10, head=-34, turn=8, tilt=5, gallop=True, armBase=34, armFwd=30, armBack=30,
                  armAbduct=16, foreBase=6, foreFwd=4, foreBack=8, forePass=8, thighAbduct=14, thighBase=14, thighFwd=40,
                  thighBack=26, thighPass=10, shinLead=30, shinBase=30, kick=40, kickPass=30, bounce=0.14,
                  bouncePass=0.06, liftBase=-0.12),
        idle=[
            (0.00, K(torso=spine(28, 0, 4), spine=spine(10), head=spine(-32, 6), lUpper=limb(34, 16), rUpper=limb(30, 16),
                     lFore=limb(6), rFore=limb(8), lThigh=limb(18, 14), rThigh=limb(18, 14), lShin=limb(32), rShin=limb(32))),
            (1.00, K(torso=spine(28, 0, -4), spine=spine(10), head=spine(-32, -6), lUpper=limb(30, 16), rUpper=limb(34, 16),
                     lFore=limb(8), rFore=limb(6), lThigh=limb(18, 14), rThigh=limb(18, 14), lShin=limb(32), rShin=limb(32))),
        ],
        stance=K(torso=spine(14), spine=spine(4), head=spine(-2), lThigh=limb(10, 8), rThigh=limb(10, 8),
                 lShin=limb(14), rShin=limb(14)),
        windup=K(torso=spine(16), spine=spine(6), head=spine(-18)), kick=0.9,
        # Chest thump: hunched, the hands beat the chest in turn.
        signature=([
            (0.00, K(torso=spine(10), spine=spine(4), head=spine(-18), lUpper=limb(62, 18), rUpper=limb(40, 30),
                     lFore=limb(112), rFore=limb(70), lThigh=limb(16, 14), rThigh=limb(16, 14), lShin=limb(28), rShin=limb(28))),
            (0.20, K(torso=spine(8), spine=spine(2), head=spine(-22), lUpper=limb(40, 30), rUpper=limb(62, 18),
                     lFore=limb(70), rFore=limb(112), lThigh=limb(16, 14), rThigh=limb(16, 14), lShin=limb(28), rShin=limb(28))),
        ], 0.4),
    ),
    # Ashlord: the villain. Upright, chest out, fists on the hips, heavy boots planted; a slow marching run.
    "villain": dict(
        gait=dict(lean=0, leanAmp=2, spine=-4, head=-6, turn=6, armFwd=18, armBack=18, armAbduct=16, foreBase=30, foreFwd=30,
                  foreBack=34, forePass=34, thighFwd=44, thighBack=34, thighPass=4, kick=26, kickPass=24, shinLead=4,
                  bounce=0.03, bouncePass=0.06),
        idle=[
            (0.00, K(spine=spine(-5), torso=spine(-3), head=spine(-8, 0), lUpper=limb(-16, 48, 60), rUpper=limb(-16, 48, 60),
                     lFore=limb(96), rFore=limb(96), lThigh=limb(0, 10), rThigh=limb(0, 10), lShin=limb(2), rShin=limb(2))),
            (1.00, K(spine=spine(-6), torso=spine(-4), head=spine(-10, 14), lUpper=limb(-16, 50, 60), rUpper=limb(-16, 50, 60),
                     lFore=limb(98), rFore=limb(98), lThigh=limb(0, 10), rThigh=limb(0, 10), lShin=limb(2), rShin=limb(2))),
        ],
        stance=K(torso=spine(-4), spine=spine(-3), head=spine(-6), lThigh=limb(-8, 6), rThigh=limb(-8, 6),
                 lShin=limb(-14), rShin=limb(-14)),
        windup=K(torso=spine(-6), head=spine(-6), lShin=limb(-18)), kick=1.2,
        # The villain laugh: head back, arms thrown wide, the shoulders shaking.
        signature=([
            (0.00, K(spine=spine(-8), torso=spine(-8, 0, 2), head=spine(-26), lUpper=limb(30, 74), rUpper=limb(30, 74),
                     lFore=limb(24), rFore=limb(24), lThigh=limb(0, 10), rThigh=limb(0, 10), lShin=limb(2), rShin=limb(2))),
            (0.15, K(spine=spine(-10), torso=spine(-10, 0, -2), head=spine(-30), lUpper=limb(36, 80), rUpper=limb(36, 80),
                     lFore=limb(18), rFore=limb(18), lThigh=limb(0, 10), rThigh=limb(0, 10), lShin=limb(2), rShin=limb(2))),
        ], 0.3),
    ),
    # Fenn: the elder turtle. Stooped, hands clasped behind, a short quick shuffle.
    "turtle": dict(
        gait=dict(lean=20, leanAmp=2, spine=12, head=-24, turn=6, armBase=-34, armFwd=6, armBack=6, armAbduct=12,
                  foreBase=64, foreFwd=0, foreBack=64, forePass=64, thighFwd=28, thighBack=18, thighPass=6, kick=26,
                  kickPass=22, shinLead=14, shinBase=10, bounce=0.03, bouncePass=0.05, liftBase=-0.04),
        idle=[
            (0.00, K(torso=spine(18), spine=spine(12), head=spine(-24, 0), lUpper=limb(-40, -6, 70), rUpper=limb(-40, -6, 70),
                     lFore=limb(70), rFore=limb(70), lThigh=limb(8, 4), rThigh=limb(8, 4), lShin=limb(14), rShin=limb(14))),
            (1.00, K(torso=spine(20), spine=spine(12), head=spine(-20, 10, 4), lUpper=limb(-40, -6, 70), rUpper=limb(-40, -6, 70),
                     lFore=limb(72), rFore=limb(72), lThigh=limb(10, 4), rThigh=limb(10, 4), lShin=limb(18), rShin=limb(18))),
        ],
        stance=K(torso=spine(12), spine=spine(8), head=spine(-2), lThigh=limb(6), rThigh=limb(6), lShin=limb(8), rShin=limb(8)),
        windup=K(torso=spine(14), head=spine(-14)), kick=0.6,
        # Slow clap: stooped, the hands meet in front, twice a second.
        signature=([
            (0.00, K(torso=spine(16), spine=spine(10), head=spine(-18), lUpper=limb(44, 34), rUpper=limb(44, 34),
                     lFore=limb(62), rFore=limb(62), lThigh=limb(8, 4), rThigh=limb(8, 4), lShin=limb(14), rShin=limb(14))),
            (0.30, K(torso=spine(18), spine=spine(10), head=spine(-20), lUpper=limb(46, 8), rUpper=limb(46, 8),
                     lFore=limb(66), rFore=limb(66), lThigh=limb(8, 4), rThigh=limb(8, 4), lShin=limb(14), rShin=limb(14))),
        ], 0.6),
    ),
}

# Named default motion data; both hands are baked from this one source.
BASEBALL = jsonc.load(Path(__file__).resolve().parents[2] / "data/art/baseball-takes.json")
BASEBALL_TAKES = {row["id"]: row for row in BASEBALL["takes"]}

SCOOP = [
    (0.00, K(torso=spine(14, 4), head=spine(10), lUpper=limb(20, 10), rUpper=limb(22, 10), lFore=limb(24), rFore=limb(26),
             lThigh=limb(28, 8), rThigh=limb(24, 8), lShin=limb(24), rShin=limb(22), lift=-0.07)),
    (0.10, K(torso=spine(24, 2), head=spine(16), lUpper=limb(34, 8), rUpper=limb(38, 8), lFore=limb(38), rFore=limb(42),
             lThigh=limb(42, 10), rThigh=limb(38, 10), lShin=limb(36), rShin=limb(34), lift=-0.22)),
    (0.22, K(torso=spine(32, 0), head=spine(18), lUpper=limb(44, 6), rUpper=limb(48, 6), lFore=limb(48), rFore=limb(52),
             lThigh=limb(50, 12), rThigh=limb(46, 12), lShin=limb(44), rShin=limb(42), lift=-0.32)),
    (0.50, K(torso=spine(10, -4), head=spine(6), lUpper=limb(16, 12), rUpper=limb(14, 12), lFore=limb(18), rFore=limb(20),
             lThigh=limb(20, 4), rThigh=limb(18, 4), lShin=limb(16), rShin=limb(14), lift=-0.03)),
]

SLIDE = [
    (0.00, K(torso=spine(12), head=spine(4), lUpper=limb(10, 18), rUpper=limb(10, 18), lFore=limb(14), rFore=limb(16),
             lThigh=limb(20), rThigh=limb(12), lShin=limb(18), rShin=limb(14), lift=0.0)),
    (0.18, K(torso=spine(-30, 0, 6), head=spine(20), lUpper=limb(-28, 22), rUpper=limb(-48, 12), lFore=limb(14), rFore=limb(16),
             lThigh=limb(70, 12), rThigh=limb(84, -8), lShin=limb(58), rShin=limb(66), lift=-0.40)),
    (0.40, K(torso=spine(-10, 0, 4), head=spine(12), lUpper=limb(-10, 22), rUpper=limb(-20, 12), lFore=limb(14), rFore=limb(16),
             lThigh=limb(42, 6), rThigh=limb(30, -4), lShin=limb(38), rShin=limb(42), lift=-0.30)),
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
             lThigh=limb(68, 12), rThigh=limb(68, 12), lShin=limb(70), rShin=limb(70), lift=-0.25)),
    (HOLD, K(torso=spine(33), head=spine(-10), lUpper=limb(40, 18), rUpper=limb(40, 18), lFore=limb(60), rFore=limb(60),
             lThigh=limb(68, 12), rThigh=limb(68, 12), lShin=limb(70), rShin=limb(70), lift=-0.26)),
]

STEAL_LEAD = [
    (0.00, K(torso=spine(22, 12), head=spine(0, 14), lUpper=limb(28, 22), rUpper=limb(12, 28), lFore=limb(30), rFore=limb(30),
             lThigh=limb(42, 10), rThigh=limb(18, 6), lShin=limb(36), rShin=limb(20), lift=-0.25)),
    (HOLD, K(torso=spine(24, 12), head=spine(0, 16), lUpper=limb(30, 22), rUpper=limb(14, 28), lFore=limb(30), rFore=limb(30),
             lThigh=limb(44, 10), rThigh=limb(20, 6), lShin=limb(36), rShin=limb(20), lift=-0.26)),
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
    doc = jsonc.load(SWING_CATALOG)
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
# The shared Contact key is every style's Contact: the barrel that cuts the zone plane over the
# plate (SwingPresentation.BarrelCutsZonePlane) must not carry a style's stance.
if stance_weight(SWING_CONTACT) != 0.0:
    raise RuntimeError(f"a style's stance must be gone by Contact ({SWING_CONTACT}); stance_weight is "
                       f"{stance_weight(SWING_CONTACT)}")
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
# The hand mesh center in the forearm's frame at rest: down the bone from the elbow, and forward.
HAND_IN_FORE = Vector((0.0, body.joint_z("lFore") - body.ANATOMY["handCenter"][2], body.ANATOMY["handCenter"][1]))


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
    upper_len = ARM_UPPER_LEN + REACH["elbow"]
    shoulder = (arm.matrix_world @ arm.pose.bones[upper].head).copy()
    # Include the authored wrist articulation in the effector offset. The
    # solver remains two-bone, but the hands no longer freeze to the forearm.
    hand_in_fore = (arm.matrix_world @ arm.pose.bones[fore].matrix).inverted() @ center(HAND_MESH[fore])
    effector_len = hand_in_fore.length
    to_target = hand_target - shoulder
    reach = to_target.length
    max_reach = upper_len + effector_len - 1e-4
    if reach > max_reach:
        to_target = to_target.normalized() * max_reach
        reach = max_reach
    u = to_target.normalized()
    side = pole - shoulder
    v = (side - u * side.dot(u))
    if v.length < 1e-6:
        v = Vector((0.0, 0.0, -1.0)) - u * Vector((0.0, 0.0, -1.0)).dot(u)
    v.normalize()
    cos_a = max(-1.0, min(1.0, (upper_len ** 2 + reach ** 2 - effector_len ** 2) / (2 * upper_len * reach)))
    sin_a = math.sqrt(max(0.0, 1 - cos_a * cos_a))
    elbow = shoulder + (u * cos_a + v * sin_a) * upper_len
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
    keys = [(k, swing["legs"][k]) for k in times]
    # A style's batting stance: its delta on the ready and load keys, gone by Contact.
    stance = STYLE_POSES[ACTIVE["style"]]["stance"] if ACTIVE["style"] else {}
    if stance:
        keys = [(k, add_terms(pose, stance, stance_weight(k))) for k, pose in keys]
    apply_pose(arm, pose_at(keys, t, ease=False, loop=False, duration=SWING_FINISH))
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
    # Every captain's head, not only the neutral one: the build's biggest head (rig.json build.head.max).
    for label, scale in (("neutral", 1.0), ("max build", float(body.BUILD["head"]["max"]))):
        body.set_build({"head": scale})
        clearance = bat_head_clearance(grip, actual)
        body.set_build({"head": 1.0})
        if clearance < BAT_HEAD_CLEARANCE:
            raise RuntimeError(f"{clip} {bats}: the bat passes {clearance:+.3f} from the {label} head at {t:.4f}; "
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
                 mark=None, sheet_times=None, sink=0.0, custom=None, validate=None, view="three-quarter",
                 ground=False, contracts=(), style=None):
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
        # Idles and signature beats stand on the dirt: the lowest sole is grounded each frame, then `hop` lifts.
        self.ground = ground
        # The named per-frame contracts `validate` holds (the receipt lists them; cli art compares a style's with the shared take's).
        self.contracts = tuple(contracts)
        self.style = style

    @property
    def folder(self) -> str:
        """Where the take's files sit under the clip root: '' for a shared take, `styles/{id}` for a style's."""
        return f"{STYLE_FOLDER}/{self.style}" if self.style else ""

    @property
    def label(self) -> str:
        return f"{self.style}/{self.clip}" if self.style else self.clip


def validate_sockets(arm, clip, t, bats):
    """Every take, every frame: the glove binds sit on the wrists and the release sockets on the rendered palms,
    so a ball caught or thrown leaves the drawn hand (a reach style moves the joints; this holds them to the mesh)."""
    for side in ("l", "r"):
        if (arm.pose.bones[side + "Glove"].head - arm.pose.bones[side + "Wrist"].head).length > .001:
            raise RuntimeError(f"{clip} {bats} {side}Glove left the wrist at {t:.3f}")
        if (arm.pose.bones[side + "Release"].head - center(side + "Hand")).length > .002:
            raise RuntimeError(f"{clip} {bats} {side}Release left the palm at {t:.3f}")


def catch_validate(arm, t, bats):
    """The catch reaches up: at the hold both drawn hands are above the head's center."""
    if t < HOLD - 0.5 / FPS:
        return
    head = center("headMesh").z
    for hand in ("lHand", "rHand"):
        if center(hand).z < head:
            raise RuntimeError(f"catch {bats}: {hand} is below the head at the hold ({center(hand).z:.2f} vs {head:.2f})")


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


def _sha256(path: Path) -> str:
    import hashlib
    return hashlib.sha256(path.read_bytes()).hexdigest()


def bake(arm, take: Take, out_dir: Path, sheets: Path | None, resources: Path | None):
    """Pose, falsify and export one take (and its baked mirror). Returns the receipt rows, one per file."""
    import clay
    scene = bpy.context.scene
    scene.render.fps = FPS
    if arm.animation_data is None:
        arm.animation_data_create()
    arm.animation_data.action = None
    use_style(take.style)
    rest = rest_matrices(arm)
    frames = frame_times(take)
    matrices = {}
    snaps = {}
    locals_r = {}
    tiles = []
    folder = out_dir / take.folder
    folder.mkdir(parents=True, exist_ok=True)
    sheet_dir = (sheets / take.folder) if sheets is not None else None
    action_name = take.label.replace("/", ".")

    def want_tile(t):
        return sheet_dir is not None and any(abs(t - st) < 0.5 / FPS for st in take.sheet_times)

    def fail_sheet():
        if sheet_dir is not None and tiles:
            clay.sheet(tiles, sheet_dir / f"{take.clip}-FAILED.png", columns=min(5, len(tiles)))

    def check(t, bats):
        if take.validate is not None:
            take.validate(arm, t, bats)
        validate_sockets(arm, take.label, t, bats)

    for frame, t in frames:
        if take.custom is not None:
            take.custom(arm, t)
        else:
            pose = pose_at(take.keys, t, take.ease, take.loop, take.duration)
            apply_pose(arm, pose)
            if take.ground:
                ground_hop(arm, pose)
        review_equipment(take.clip)
        if want_tile(t):
            tiles.append(clay.render(sheet_dir / f"{take.clip}-{t:.2f}.png", take.view, 360, 480))
        try:
            check(t, batting_stance.BATS_RIGHT)
            validate_feet_on_ground(arm, take.label, t, take.sink)
        except Exception:
            fail_sheet()
            raise
        matrices[frame] = snapshot_matrices(arm)
        snaps[frame] = landmark_snapshot()
        locals_r[frame] = local_snapshot(arm)
    right = write_action(arm, action_name, frames, locals_r)
    outputs = [export_take(arm, right, take, folder, take.clip)]
    arm.animation_data.action = None

    if take.handed:
        locals_l = {}
        for frame, t in frames:
            reflect_pose(arm, matrices[frame], rest)
            review_equipment(take.clip, left=True)
            if want_tile(t):
                tiles.append(clay.render(sheet_dir / f"{take.clip}-L-{t:.2f}.png", clay.mirror_view(take.view), 360, 480))
            try:
                assert_reflected(snaps[frame], landmark_snapshot(), take.label, t)
                check(t, batting_stance.BATS_LEFT)
            except Exception:
                fail_sheet()
                raise
            locals_l[frame] = local_snapshot(arm)
        left = write_action(arm, action_name + "-L", frames, locals_l)
        outputs.append(export_take(arm, left, take, folder, take.clip + "-L"))
        arm.animation_data.action = None

    if sheet_dir is not None and tiles:
        clay.sheet(tiles, sheet_dir / f"{take.clip}.png", columns=min(5, len(tiles)))
    contracts = ["feet", "sockets", *take.contracts] + (["reflection"] if take.handed else [])
    rows = []
    for out in outputs:
        print("take", take.folder + "/" + out.name if take.folder else out.name, out.stat().st_size)
        if resources is not None:
            (resources / take.folder).mkdir(parents=True, exist_ok=True)
            shutil.copy2(out, resources / take.folder / out.name)
        rel = f"{take.folder}/{out.name}" if take.folder else out.name
        rows.append({"file": rel, "sha256": _sha256(out), "frames": len(frames), "contracts": sorted(contracts)})
    clear_pose(arm)
    use_style(None)
    return rows


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


def styled_windup(keys):
    """A style's windup: its pose delta and leg-kick height on the keys before Release, gone by Release."""
    style = ACTIVE["style"]
    if not style:
        return keys
    sp = STYLE_POSES[style]
    out = []
    for k in keys:
        w = windup_weight(k["t"])
        kick = 1.0 + (sp["kick"] - 1.0) * w
        feet = dict(k["feet"])
        x, y, z = feet["left"]
        feet["left"] = [x, y, 0.24 + (z - 0.24) * kick]
        out.append({**k, "pose": add_terms(k["pose"], sp["windup"], w), "feet": feet})
    return out


def baseball_frame(clip):
    row=BASEBALL_TAKES[clip];times=[k["t"] for k in row["keys"]]
    def custom(arm,t):
        keys=styled_windup(row["keys"])
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


def all_takes(style: str | None = None):
    """Every clip's take. With a style, the styled clips carry its gait, idle, stance, windup and signature beat
    (the stance and windup are applied in pose_swing_frame / baseball_frame while the style is active)."""
    sp = STYLE_POSES[style] if style else None
    gait = sp["gait"] if sp else None
    signature, signature_dur = sp["signature"] if sp else (CHEER, 0.8)
    return [
        Take("idle", sp["idle"] if sp else IDLE, duration=2.0, loop=True, ground=sp is not None),
        Take("field", FIELD, duration=2.0, loop=True, sink=0.2),
        # A style's cheer is its captain's signature beat (the home run and the win play Cheer).
        Take("cheer", signature, duration=signature_dur, loop=True, ground=sp is not None),
        Take("charm", CHARM, duration=1.2, loop=True),
        Take("walk", _stride(0.45, WALK_DUR, gait), duration=WALK_DUR, loop=True, sink=0.3),
        Take("run", _stride(1.0, RUN_DUR, gait), duration=RUN_DUR, loop=True, sink=0.3),
        Take("jump", JUMP, duration=JUMP_DUR, sink=0.2),
        *[Take(row["id"], [(k["t"],k["pose"]) for k in row["keys"]], duration=row["duration"],
               handed=True, mark=row["releaseAt"], custom=baseball_frame(row["id"]), validate=baseball_validate(row["id"]), sink=.3,
               view="three-quarter-right", contracts=("release",))
          for row in BASEBALL["takes"]],
        # Slap and charge (#613): both meet the ball at Contact and end on the held finish (#583).
        *[Take(clip, None, view="three-quarter-right", duration=SWING_FINISH, handed=True, ease=False, mark=SWING_CONTACT,
               custom=swing_frame(clip), validate=swing_validate(clip), sheet_times=SWINGS[clip]["times"], sink=0.2,
               contracts=("swing",))
          for clip in (SWING_SLAP, SWING_CHARGE)],
        Take("checkSwing", None, view="three-quarter-right", duration=HOLD, handed=True, custom=held_swing_frame(0.20), validate=None,
             sheet_times=[0.0], sink=0.2),
        Take("bunt", None, view="three-quarter-right", duration=HOLD, handed=True, custom=bunt_frame, validate=bunt_validate,
             sheet_times=[0.0], sink=0.6, contracts=("bunt",)),
        Take("miss", None, view="three-quarter-right", duration=HOLD, handed=True, custom=miss_frame, sheet_times=[0.0], sink=0.2),
        Take("catch", CATCH, duration=HOLD, validate=catch_validate, contracts=("catch",)),
        Take("dive", DIVE, duration=HOLD, sink=1.0),
        Take("crouch", CROUCH, duration=HOLD, sink=0.8),
        Take("stealLead", STEAL_LEAD, duration=HOLD, sink=0.8),
        Take("spin", SPIN, duration=HOLD),
        Take("scoop", SCOOP, duration=0.50, mark=0.22, sink=0.9),
        Take("slide", SLIDE, duration=0.40, mark=0.18, sink=1.2),
    ]


def style_takes(style: str):
    """A style's own takes: the styled clips, or every clip when its reach moves the joints."""
    owns_all = abs(float(STYLE_ROWS[style]["reachScale"]) - 1.0) > 1e-9
    takes = []
    for take in all_takes(style):
        if owns_all or take.clip in STYLED_CLIPS:
            take.style = style
            takes.append(take)
    return takes


TAKES = all_takes()
if set(STYLE_POSES) != set(STYLE_ROWS):
    raise RuntimeError(f"STYLE_POSES {sorted(STYLE_POSES)} must match clips.json styles {sorted(STYLE_ROWS)}")
if not set(STYLED_CLIPS) <= {t.clip for t in TAKES}:
    raise RuntimeError(f"clips.json styles.clips names a clip no take bakes: {sorted(set(STYLED_CLIPS) - {t.clip for t in TAKES})}")




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



def write_receipt(path: Path, rows: list):
    """Merge this bake's rows into the takes receipt: per file its SHA-256, frame count and the per-frame contracts
    it passed. cli art refuses a take whose bytes no receipt row vouches for."""
    import json
    doc = {"notes": "Written by tools/blender/hero_shared_takes.py; do not edit. One row per baked take file under "
                    "unity/Assets/Art/Animation/Clips: its SHA-256, frames and the per-frame contracts it passed. "
                    "cli art checks every catalog take against it.", "takes": []}
    if path.exists():
        doc["takes"] = json.loads(path.read_text()).get("takes", [])
    by_file = {row["file"]: row for row in doc["takes"]}
    for row in rows:
        by_file[row["file"]] = row
    doc["takes"] = [by_file[k] for k in sorted(by_file)]
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(doc, indent=2) + "\n")
    print("receipt", path, len(rows), "rows")


def main(argv):
    p = argparse.ArgumentParser()
    p.add_argument("--out", required=True)
    p.add_argument("--resources", default="")
    p.add_argument("--sheets", default="")
    p.add_argument("--only", default="", help="Clip ids to bake (default: every clip).")
    p.add_argument("--styles", default="all", help="'all' (shared and every style), 'none' (shared only), 'only' "
                                                   "(every style, no shared), or style ids.")
    p.add_argument("--receipt", default="", help="Receipt JSON (default: data/art/takes-receipt.json when --out is "
                                                 "the catalog clip folder, else <out>/takes-receipt.json).")
    p.add_argument("--blend", default="", help="Save editable rig, mesh, props and baked actions for inspection.")
    args = p.parse_args(argv)
    out = Path(args.out).resolve()
    out.mkdir(parents=True, exist_ok=True)
    resources = Path(args.resources).resolve() if args.resources else None
    sheets = Path(args.sheets).resolve() if args.sheets else None
    only = {s.strip() for s in args.only.split(",") if s.strip()}
    catalog = (REPO / "unity/Assets/Art/Animation/Clips").resolve()
    receipt = Path(args.receipt).resolve() if args.receipt else (
        REPO / "data/art/takes-receipt.json" if out == catalog else out / "takes-receipt.json")
    if args.styles == "all":
        shared, styles = True, list(STYLE_ROWS)
    elif args.styles == "none":
        shared, styles = True, []
    elif args.styles == "only":
        shared, styles = False, list(STYLE_ROWS)
    else:
        shared, styles = False, [s.strip() for s in args.styles.split(",") if s.strip()]
        unknown = [s for s in styles if s not in STYLE_ROWS]
        if unknown:
            raise RuntimeError(f"unknown styles {unknown}; clips.json has {sorted(STYLE_ROWS)}")
    arm = body.build_scene()
    add_review_equipment(arm)
    assert_conventions(arm)
    print("conventions ok")
    queue = (TAKES if shared else []) + [take for style in styles for take in style_takes(style)]
    rows = []
    for take in queue:
        if only and take.clip not in only:
            continue
        rows.extend(bake(arm, take, out, sheets, resources))
    write_receipt(receipt, rows)
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
