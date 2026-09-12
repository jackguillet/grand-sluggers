#!/usr/bin/env python3
"""Build the Grand Sluggers hero-shared body and export FBX.

One rig for every captain. Bone names from data/art/rig.json. The character
faces Blender -Y with its left hand at +X, so the FBX (axis_forward=-Z,
axis_up=Y, X reflected on Unity import) lands facing Unity +Z with lHand at
Unity -X: the side HeroActor points at `look`. The face and toes are authored
here; nothing rebuilds them at runtime. No caps: hats come later as accessories.

Silhouette.ToyScale and the per-captain root scale are applied in Unity.
Do not scale the FBX. Contract: docs/character-motion.md.
"""
from __future__ import annotations

import argparse
import math
import shutil
import sys
from pathlib import Path

import bpy
from mathutils import Vector


BONES = [
    "root",
    "torso",
    "head",
    "lUpper",
    "lFore",
    "rUpper",
    "rFore",
    "lThigh",
    "lShin",
    "rThigh",
    "rShin",
    "bat",
    "glove",
]

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

HEAD = Vector((0.0, -0.08, 4.05))


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

    # Z-up, faces -Y, left at +X. Limb bones hang straight down with roll 0:
    # local X = world X, local Y = down the bone, local Z = world +Y (behind).
    add_bone(arm_data, "root", (0, 0, 0), (0, 0, 0.25))
    add_bone(arm_data, "torso", (0, 0, 1.15), (0, 0, 2.55), "root")
    add_bone(arm_data, "head", (0, -0.05, 3.35), (0, -0.05, 4.55), "torso")
    add_bone(arm_data, "lUpper", (0.95, 0, 2.45), (0.95, 0, 1.55), "torso")
    add_bone(arm_data, "lFore", (0.95, 0, 1.55), (0.95, 0, 0.85), "lUpper")
    add_bone(arm_data, "rUpper", (-0.95, 0, 2.45), (-0.95, 0, 1.55), "torso")
    add_bone(arm_data, "rFore", (-0.95, 0, 1.55), (-0.95, 0, 0.85), "rUpper")
    add_bone(arm_data, "lThigh", (0.42, 0, 1.05), (0.42, 0, 0.45), "root")
    add_bone(arm_data, "lShin", (0.42, 0, 0.45), (0.42, 0, 0.08), "lThigh")
    add_bone(arm_data, "rThigh", (-0.42, 0, 1.05), (-0.42, 0, 0.45), "root")
    add_bone(arm_data, "rShin", (-0.42, 0, 0.45), (-0.42, 0, 0.08), "rThigh")
    add_bone(arm_data, "bat", (-1.15, -0.15, 0.90), (-1.15, -0.15, 0.20), "rFore")
    add_bone(arm_data, "glove", (1.15, -0.15, 0.90), (1.15, -0.15, 0.20), "lFore")
    bpy.ops.object.mode_set(mode="OBJECT")
    return arm_ob, arm_data


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

    add("uv_sphere", "Hip", (0, 0, 1.05), (1.42, 1.12, 1.00), "slack", "root")
    add("uv_sphere", "torsoMesh", (0, -0.05, 2.28), (1.42, 0.98, 1.85), "jersey", "torso")
    add("cube", "Stripe", (0, -0.52, 2.35), (0.32, 0.08, 1.20), "gold", "torso")

    # Face on the front (-Y). Eyes are the stance landmark; whites sit behind them.
    add("uv_sphere", "headMesh", tuple(HEAD), (1.72, 1.72, 1.72), "flesh", "head")
    for side, sx in (("L", 1.0), ("R", -1.0)):
        add("uv_sphere", "White" + side, (sx * 0.30, HEAD.y - 0.78, HEAD.z + 0.12), (0.42, 0.42, 0.42), "white", "head")
        add("uv_sphere", "Eye" + side, (sx * 0.30, HEAD.y - 0.94, HEAD.z + 0.12), (0.22, 0.22, 0.22), "ink", "head")
        add("cube", "Brow" + side, (sx * 0.30, HEAD.y - 0.80, HEAD.z + 0.38), (0.38, 0.12, 0.08), "ink", "head")
        add("uv_sphere", "Ear" + side, (sx * 0.86, HEAD.y, HEAD.z + 0.03), (0.28, 0.28, 0.28), "flesh", "head")
    add("uv_sphere", "Mouth", (0, HEAD.y - 0.80, HEAD.z - 0.28), (0.42, 0.16, 0.18), "ink", "head")

    # No cap. Hats return later as accessories on the head socket.

    sym("uv_sphere", "UpperMesh", (0.95, 0, 2.00), (0.64, 0.64, 1.05), "jersey", "Upper")
    sym("uv_sphere", "ForeMesh", (0.95, 0, 1.18), (0.52, 0.52, 0.82), "flesh", "Fore")
    sym("uv_sphere", "Hand", (0.95, -0.10, 0.72), (0.48, 0.40, 0.38), "flesh", "Fore")
    sym("uv_sphere", "ThighMesh", (0.42, 0, 0.78), (0.68, 0.68, 0.88), "slack", "Thigh")
    sym("uv_sphere", "ShinMesh", (0.42, 0, 0.32), (0.54, 0.54, 0.58), "slack", "Shin")
    sym("cube", "Shoe", (0.42, -0.32, 0.12), (0.70, 1.05, 0.42), "leather", "Shin")

    bpy.context.view_layer.objects.active = arm_ob
    bpy.ops.object.mode_set(mode="OBJECT")
    for ob, bone in pieces:
        skin(ob, arm_ob, bone)

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
