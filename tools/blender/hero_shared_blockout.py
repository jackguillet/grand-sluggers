#!/usr/bin/env python3
"""Build the Grand Sluggers hero-shared body and export FBX.

One rig for every captain. Bone names from data/art/rig.json. The character
faces Blender -Y with its left hand at +X, so the FBX (axis_forward=-Z,
axis_up=Y, X reflected on Unity import) lands facing Unity +Z with lHand at
Unity -X: the side HeroActor points at `look`. The face and toes are authored
here; nothing rebuilds them at runtime. No caps: hats come later as accessories.

Silhouette.ToyScale and the per-captain root scale are applied in Unity.
Do not scale the FBX. Contract: docs/character-motion.md.
Stage 1 blocking (data/agent/dcc-stages.json): --clay scratchpad/takes/body.png, then --out.
"""
from __future__ import annotations

import argparse
import math
import shutil
import sys
from pathlib import Path

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import jsonc  # noqa: E402  (the one reader for data files with // notes)


RIG = jsonc.load(Path(__file__).resolve().parents[2] / "data/art/rig.json")
BONES = RIG["bones"]
ANATOMY = RIG["anatomy"]

# Material names are palette roles. Unity recolors by these names.
PALETTE = {
    "jersey": (0.86, 0.19, 0.16),
    "trim": (0.86, 0.19, 0.16),
    "gold": (1.0, 0.80, 0.25),
    "flesh": (0.95, 0.79, 0.64),
    "slack": (0.95, 0.95, 0.93),
    "ink": (0.08, 0.07, 0.07),
    "white": (1.0, 1.0, 1.0),
    "leather": (0.30, 0.18, 0.10),
}

# Landmarks the DCC validators and the Unity swing matrix read by name.
LANDMARKS = ("torsoMesh", "Stripe", "headMesh", "EyeL", "EyeR", "lHand", "rHand", "lShoe", "rShoe")

HEAD = Vector(ANATOMY["headCenter"])


def nuke():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for block in (bpy.data.meshes, bpy.data.armatures, bpy.data.materials, bpy.data.curves, bpy.data.actions):
        for item in list(block):
            block.remove(item)


def mat(name, color):
    m = bpy.data.materials.get(name)
    if m is not None:
        return m
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1.0)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (*color, 1.0)
        bsdf.inputs["Roughness"].default_value = 0.55
    return m


def mesh_prim(kind, name, loc, scale, material, rot=(0.0, 0.0, 0.0)):
    if kind == "uv_sphere":
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.5, location=loc, segments=28, ring_count=16)
    elif kind == "cylinder":
        bpy.ops.mesh.primitive_cylinder_add(radius=0.5, depth=1.0, location=loc, vertices=28)
    else:
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc)
    ob = bpy.context.active_object
    ob.name = name
    ob.scale = scale
    ob.rotation_euler = rot
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    for p in ob.data.polygons:
        p.use_smooth = True
    ob.data.materials.append(material)
    return ob


def join(name, pieces):
    """Join pieces into one object named `name`, origin at the world origin."""
    bpy.ops.object.select_all(action="DESELECT")
    for ob in pieces:
        ob.select_set(True)
    bpy.context.view_layer.objects.active = pieces[0]
    bpy.ops.object.join()
    ob = pieces[0]
    ob.name = name
    bpy.context.scene.cursor.location = (0.0, 0.0, 0.0)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    return ob


def add_bone(arm, name, head, tail, parent=None):
    b = arm.edit_bones.new(name)
    b.head = Vector(head)
    b.tail = Vector(tail)
    b.roll = 0.0
    b.use_connect = False
    if parent is not None:
        b.parent = arm.edit_bones[parent]
    return b


def skin(ob, arm_ob, bone):
    """Vertex-group skin, 100% one bone. Unity imports a SkinnedMeshRenderer."""
    ob.parent = arm_ob
    ob.parent_type = "OBJECT"
    vg = ob.vertex_groups.new(name=bone)
    vg.add(list(range(len(ob.data.vertices))), 1.0, "REPLACE")
    mod = ob.modifiers.new("Armature", "ARMATURE")
    mod.object = arm_ob
    mod.use_vertex_groups = True


def build_armature():
    arm_data = bpy.data.armatures.new("hero-shared-data")
    arm_ob = bpy.data.objects.new("hero-shared", arm_data)
    bpy.context.collection.objects.link(arm_ob)
    bpy.context.view_layer.objects.active = arm_ob
    bpy.ops.object.mode_set(mode="EDIT")

    # Data owns the rest hierarchy; all takes use this same armature.
    for joint in RIG["joints"]:
        add_bone(arm_data, joint["name"], joint["head"], joint["tail"], joint["parent"])
    bpy.ops.object.mode_set(mode="OBJECT")
    return arm_ob, arm_data


JOINT = {j["name"]: j for j in RIG["joints"]}


def joint_z(name: str, end: str = "head") -> float:
    return float(JOINT[name][end][2])


# The four-head toy's jersey: a round belly over short legs. Each ring is
# (z, half-width, half-depth), from the belt to the neck, on the shared bind.
JERSEY_RINGS = [(1.70, .50, .38), (1.88, .59, .45), (2.12, .65, .49), (2.42, .67, .50),
                (2.72, .66, .48), (3.02, .63, .45), (3.26, .60, .42), (3.44, .50, .35),
                (3.58, .20, .20)]
JERSEY_AXIS_Y = -0.02


def _ring_depth(z: float) -> float:
    rings = JERSEY_RINGS
    if z <= rings[0][0]:
        return rings[0][2]
    for (z0, _, d0), (z1, _, d1) in zip(rings, rings[1:]):
        if z <= z1:
            return d0 + (d1 - d0) * (z - z0) / (z1 - z0)
    return rings[-1][2]


def jersey_mesh(material):
    """One continuous tailored jersey; graded spine/chest skin weights replace
    a stack of disconnected torso ellipsoids. Rings are in the shared bind."""
    rings = JERSEY_RINGS
    vertices=[(rx*math.cos(i*2*math.pi/32),JERSEY_AXIS_Y+ry*math.sin(i*2*math.pi/32),z)
              for z,rx,ry in rings for i in range(32)]
    faces=[(j*32+i,j*32+(i+1)%32,(j+1)*32+(i+1)%32,(j+1)*32+i)
           for j in range(len(rings)-1) for i in range(32)]
    faces.extend([tuple(reversed(range(32))),tuple((len(rings)-1)*32+i for i in range(32))])
    mesh=bpy.data.meshes.new("jersey-surface");mesh.from_pydata(vertices,[],faces);mesh.update()
    ob=bpy.data.objects.new("torsoMesh",mesh);bpy.context.collection.objects.link(ob)
    mesh.materials.append(material)
    for poly in mesh.polygons:poly.use_smooth=True
    return ob


def stripe_mesh(material, low: float, high: float, half_width: float = 0.09, thick: float = 0.035):
    """The gold chest stripe, laid on the jersey's front surface from `low` to
    `high` so it follows the belly instead of floating off it."""
    steps = 12
    vertices = []
    for i in range(steps + 1):
        z = low + (high - low) * i / steps
        front = JERSEY_AXIS_Y - _ring_depth(z) - 0.012
        for x in (-half_width, half_width):
            vertices.append((x, front, z))
            vertices.append((x, front + thick, z))
    faces = []
    for i in range(steps):
        a = i * 4
        b = a + 4
        faces += [(a, a + 2, b + 2, b), (a + 1, b + 1, b + 3, a + 3),
                  (a, b, b + 1, a + 1), (a + 2, a + 3, b + 3, b + 2)]
    faces += [(0, 1, 3, 2), (steps * 4, steps * 4 + 2, steps * 4 + 3, steps * 4 + 1)]
    mesh = bpy.data.meshes.new("stripe-surface")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    ob = bpy.data.objects.new("Stripe", mesh)
    bpy.context.collection.objects.link(ob)
    mesh.materials.append(material)
    return ob


# ------------------------------------------------------------ build (CH-04)
#
# A captain's head, arms and torso are shape keys on this one mesh, never a
# joint: `<channel>+` is the piece at the channel's max scale, `<channel>-` at
# its min (data/art/rig.json build). Unity sets the weights from
# Silhouette.Build; the bones, the takes and the sockets do not move.

BUILD = RIG["build"]
BUILD_CHANNELS = ("head", "arms", "torso")
HEAD_PIVOT = Vector(BUILD["head"]["pivot"])


def _build_head(world: Vector, s: float, piece: str) -> Vector:
    return HEAD_PIVOT + (world - HEAD_PIVOT) * s


def _build_torso(world: Vector, s: float, piece: str) -> Vector:
    return Vector((world.x * s, JERSEY_AXIS_Y + (world.y - JERSEY_AXIS_Y) * s, world.z))


def _build_arms(world: Vector, s: float, piece: str) -> Vector:
    x0 = ANATOMY["handCenter"][0] * (1.0 if piece.startswith("l") else -1.0)
    if piece[1:] in ("Hand", "Thumb"):
        hand = Vector((x0, ANATOMY["handCenter"][1], ANATOMY["handCenter"][2]))
        return hand + (world - hand) * s
    return Vector((x0 + (world.x - x0) * s, world.y * s, world.z))


BUILD_SHAPE = {"head": _build_head, "arms": _build_arms, "torso": _build_torso}


def build_channel(piece: str, bone: str):
    """Which build channel shapes a piece: by its bone, so a new piece joins by where it hangs."""
    if bone == "head":
        return "head"
    if bone in ("torso", "spine", "neck", "pelvis"):
        return "torso"
    if bone[1:] in ("Upper", "Fore", "Wrist"):
        return "arms"
    return None


def add_build_keys(ob, channel: str):
    row = BUILD[channel]
    shape = BUILD_SHAPE[channel]
    ob.shape_key_add(name="Basis", from_mix=False)
    world = ob.matrix_world.copy()
    inverse = world.inverted()
    for suffix, s in (("+", float(row["max"])), ("-", float(row["min"]))):
        key = ob.shape_key_add(name=channel + suffix, from_mix=False)
        key.slider_min = 0.0
        key.slider_max = 1.0
        for i, v in enumerate(ob.data.vertices):
            key.data[i].co = inverse @ shape(world @ v.co, s, ob.name)


def build_weights(scales: dict) -> dict:
    """Shape-key values for {channel: scale}: the same arithmetic as Silhouette.BuildWeights."""
    out = {}
    for channel in BUILD_CHANNELS:
        row = BUILD[channel]
        s = float(scales.get(channel, 1.0))
        up = max(0.0, (s - 1.0) / (float(row["max"]) - 1.0))
        down = max(0.0, (1.0 - s) / (1.0 - float(row["min"])))
        out[channel + "+"] = min(1.0, up)
        out[channel + "-"] = min(1.0, down)
    return out


def build_scale(proportions: dict) -> dict:
    """{channel: scale} for a captain's proportions: the same arithmetic as Silhouette.Build."""
    out = {}
    for channel in BUILD_CHANNELS:
        row = BUILD[channel]
        ratio = float(proportions[channel]) / float(row["neutral"])
        out[channel] = 1.0 + float(row["gain"]) * (ratio - 1.0)
    return out


def set_build(scales: dict):
    """Pose every body piece's build keys for {channel: scale} (the lineup and the bake's max-head check)."""
    weights = build_weights(scales)
    for ob in bpy.data.objects:
        keys = ob.data.shape_keys if ob.type == "MESH" and ob.data.shape_keys else None
        if keys is None:
            continue
        for block in keys.key_blocks:
            if block.name in weights:
                block.value = weights[block.name]
    bpy.context.view_layer.update()


def build_scene():
    nuke()
    mats = {k: mat(k, v) for k, v in PALETTE.items()}
    arm_ob, arm_data = build_armature()
    pieces = []

    def add(kind, name, loc, scale, key, bone, rot=(0.0, 0.0, 0.0)):
        ob = mesh_prim(kind, name, loc, scale, mats[key], rot)
        pieces.append((ob, bone))
        return ob

    def sym(kind, name, loc, scale, key, bone, rot=(0.0, 0.0, 0.0)):
        """Left piece at +X on the l-bone, right piece mirrored on the r-bone."""
        x, y, z = loc
        rx, ry, rz = rot
        add(kind, "l" + name, (x, y, z), scale, key, "l" + bone, (rx, -ry, -rz))
        add(kind, "r" + name, (-x, y, z), scale, key, "r" + bone, (rx, -ry, -rz))

    hip = joint_z("pelvis")
    shoulder = joint_z("lUpper")
    elbow = joint_z("lFore")
    wrist = joint_z("lWrist")
    knee = joint_z("lShin")
    ankle = joint_z("lFoot")
    neck = joint_z("neck")
    x_arm = JOINT["lUpper"]["head"][0]
    x_leg = JOINT["lThigh"]["head"][0]
    hand = Vector(ANATOMY["handCenter"])

    add("uv_sphere", "Hip", (0, 0, hip + 0.02), (1.12, 0.82, 0.70), "slack", "pelvis")
    pieces.append((jersey_mesh(mats["jersey"]), "torso"))
    pieces.append((stripe_mesh(mats["gold"], joint_z("torso") - 0.15, joint_z("lClavicle") - 0.12), "torso"))
    add("uv_sphere", "NeckMesh", (0, -0.05, neck + 0.06), (0.46, 0.46, 0.30), "flesh", "neck")
    d = ANATOMY["headDiameter"]
    add("uv_sphere", "headMesh", tuple(HEAD), (d, d, d), "flesh", "head")
    face = d / 1.72
    for side, sx in (("L", 1.0), ("R", -1.0)):
        def face_piece(kind, name, offset, scale, role):
            add(kind, name + side, tuple(HEAD + Vector(offset) * face), tuple(v * face for v in scale), role, "head")
        face_piece("uv_sphere", "White", (sx*.30,-.78,.12), (.42,.42,.42), "white")
        face_piece("uv_sphere", "Eye", (sx*.30,-.94,.12), (.22,.22,.22), "ink")
        face_piece("cube", "Brow", (sx*.30,-.80,.38), (.38,.12,.08), "ink")
        face_piece("uv_sphere", "Ear", (sx*.86,0,.03), (.28,.28,.28), "flesh")
    add("uv_sphere", "Mouth", tuple(HEAD + Vector((0,-.80,-.28))*face), tuple(v*face for v in (.42,.16,.18)), "ink", "head")
    # No cap. Sculpted toy segments overlap at the anatomical pivots. Wrists
    # and feet have independent skin groups, so they can articulate naturally.
    # Toy weight: short thick legs, round mitts, big shoes; the arms keep the
    # shared reach so every take's hands land where they did.
    sym("uv_sphere", "Shoulder", (x_arm, 0, shoulder - 0.04), (.52, .52, .52), "jersey", "Upper")
    sym("uv_sphere", "UpperMesh", (x_arm, 0, (shoulder + elbow) / 2), (.46, .48, (shoulder - elbow) * 1.08), "jersey", "Upper")
    sym("uv_sphere", "Elbow", (x_arm, 0, elbow), (.35, .35, .35), "flesh", "Fore")
    sym("uv_sphere", "ForeMesh", (x_arm, 0, (elbow + wrist) / 2), (.37, .39, (elbow - wrist) * 1.14), "flesh", "Fore")
    sym("uv_sphere", "Hand", tuple(hand), (.40, .35, .40), "flesh", "Wrist")
    sym("uv_sphere", "Thumb", (hand.x - .18, hand.y, hand.z + .03), (.16, .19, .24), "flesh", "Wrist")
    sym("uv_sphere", "ThighMesh", (x_leg, 0, (hip + knee) / 2), (.62, .64, (hip - knee) * 1.30), "slack", "Thigh")
    sym("uv_sphere", "Knee", (x_leg, 0, knee), (.44, .45, .45), "slack", "Shin")
    sym("uv_sphere", "ShinMesh", (x_leg, 0, (knee + ankle) / 2), (.47, .50, (knee - ankle) * 1.18), "slack", "Shin")
    sym("uv_sphere", "Shoe", tuple(ANATOMY["shoeCenter"]), (.62, 1.00, .38), "leather", "Foot")

    bpy.context.view_layer.objects.active = arm_ob
    bpy.ops.object.mode_set(mode="OBJECT")
    for ob, bone in pieces:
        skin(ob, arm_ob, bone)
        if ob.name == "torsoMesh":
            chest=ob.vertex_groups["torso"];waist=ob.vertex_groups.new(name="spine")
            low = joint_z("torso") - 0.25
            for v in ob.data.vertices:
                weight=max(0.0,min(1.0,(v.co.z-low)/.50))
                chest.add([v.index],weight,"REPLACE")
                waist.add([v.index],1-weight,"REPLACE")
        channel = build_channel(ob.name, bone)
        if channel is not None:
            add_build_keys(ob, channel)

    missing = [n for n in BONES if n not in arm_data.bones]
    if missing:
        raise RuntimeError("missing bones: " + ",".join(missing))
    names = {ob.name for ob, _ in pieces}
    lost = [n for n in LANDMARKS if n not in names]
    if lost:
        raise RuntimeError("missing landmarks: " + ",".join(lost))
    return arm_ob



FBX_AXES = dict(
    add_leaf_bones=False,
    armature_nodetype="NULL",
    primary_bone_axis="Y",
    secondary_bone_axis="X",
    axis_forward="-Z",
    axis_up="Y",
    apply_scale_options="FBX_SCALE_ALL",
    bake_space_transform=True,
    path_mode="AUTO",
)


def export_fbx(out: Path, *, anim: bool = False, armature_only: bool = False):
    out.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(
        filepath=str(out),
        use_selection=False,
        object_types={"ARMATURE"} if armature_only else {"ARMATURE", "MESH"},
        use_mesh_modifiers=True,
        bake_anim=anim,
        bake_anim_use_all_bones=True,
        bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=False,
        bake_anim_force_startend_keying=True,
        bake_anim_step=1.0,
        bake_anim_simplify_factor=0.0,
        **FBX_AXES,
    )
    print("exported", out, out.stat().st_size)


def copy_to_resources(out: Path, resources: str):
    if not resources:
        return
    res = Path(resources).resolve()
    res.mkdir(parents=True, exist_ok=True)
    shutil.copy2(out, res / out.name)
    print("resources", res / out.name)


def main(argv):
    p = argparse.ArgumentParser()
    p.add_argument("--out", required=True)
    p.add_argument("--resources", default="", help="Player copy folder; byte-identical.")
    p.add_argument("--clay", default="", help="Folder for clay check renders.")
    args = p.parse_args(argv)
    build_scene()
    out = Path(args.out).resolve()
    export_fbx(out, anim=False)
    copy_to_resources(out, args.resources)
    if args.clay:
        sys.path.insert(0, str(Path(__file__).resolve().parent))
        import clay
        folder = Path(args.clay).resolve()
        tiles = [clay.render(folder / f"body-{v}.png", v) for v in ("front", "three-quarter", "left", "back")]
        clay.sheet(tiles, folder / "body.png", columns=4)
        print("clay", folder / "body.png")


if __name__ == "__main__":
    argv = sys.argv
    if "--" in argv:
        argv = argv[argv.index("--") + 1 :]
    else:
        argv = argv[1:]
    main(argv)
