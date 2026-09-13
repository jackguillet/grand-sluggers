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
import json
import math
import shutil
import sys
from pathlib import Path

import bpy
from mathutils import Vector


RIG = json.loads((Path(__file__).resolve().parents[2] / "data/art/rig.json").read_text())
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


def jersey_mesh(material):
    """One continuous tailored jersey; graded spine/chest skin weights replace
    a stack of disconnected torso ellipsoids. Rings are in the shared bind."""
    rings=[(2.28,.42,.29),(2.42,.44,.30),(2.66,.43,.30),(2.90,.52,.33),
           (3.15,.61,.345),(3.38,.59,.32),(3.54,.43,.26),(3.66,.19,.18)]
    vertices=[(rx*math.cos(i*2*math.pi/32),-.02+ry*math.sin(i*2*math.pi/32),z)
              for z,rx,ry in rings for i in range(32)]
    faces=[(j*32+i,j*32+(i+1)%32,(j+1)*32+(i+1)%32,(j+1)*32+i)
           for j in range(len(rings)-1) for i in range(32)]
    faces.extend([tuple(reversed(range(32))),tuple((len(rings)-1)*32+i for i in range(32))])
    mesh=bpy.data.meshes.new("jersey-surface");mesh.from_pydata(vertices,[],faces);mesh.update()
    ob=bpy.data.objects.new("torsoMesh",mesh);bpy.context.collection.objects.link(ob)
    mesh.materials.append(material)
    for poly in mesh.polygons:poly.use_smooth=True
    return ob


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

    add("uv_sphere", "Hip", (0, 0, 2.13), (1.00, 0.65, 0.62), "slack", "pelvis")
    pieces.append((jersey_mesh(mats["jersey"]), "torso"))
    add("cube", "Stripe", (0, -0.365, 3.12), (0.16, 0.04, 0.76), "gold", "torso")
    add("uv_sphere", "NeckMesh", (0, -0.05, 3.80), (0.36, 0.36, 0.55), "flesh", "neck")
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
    sym("uv_sphere", "Shoulder", (.72,0,3.45), (.46,.46,.46), "jersey", "Upper")
    sym("uv_sphere", "UpperMesh", (.72,0,2.99), (.40,.42,1.12), "jersey", "Upper")
    sym("uv_sphere", "Elbow", (.72,0,2.48), (.31,.31,.31), "flesh", "Fore")
    sym("uv_sphere", "ForeMesh", (.72,0,2.05), (.32,.34,1.02), "flesh", "Fore")
    sym("uv_sphere", "Hand", tuple(ANATOMY["handCenter"]), (.32,.28,.32), "flesh", "Wrist")
    sym("uv_sphere", "Thumb", (.57,-.08,1.49), (.14,.17,.22), "flesh", "Wrist")
    sym("uv_sphere", "ThighMesh", (.36,0,1.65), (.52,.56,1.15), "slack", "Thigh")
    sym("uv_sphere", "Knee", (.36,0,1.16), (.38,.39,.39), "slack", "Shin")
    sym("uv_sphere", "ShinMesh", (.36,0,.71), (.36,.40,1.02), "slack", "Shin")
    sym("uv_sphere", "Shoe", tuple(ANATOMY["shoeCenter"]), (.44,.76,.32), "leather", "Foot")

    bpy.context.view_layer.objects.active = arm_ob
    bpy.ops.object.mode_set(mode="OBJECT")
    for ob, bone in pieces:
        skin(ob, arm_ob, bone)
        if ob.name == "torsoMesh":
            chest=ob.vertex_groups["torso"];waist=ob.vertex_groups.new(name="spine")
            for v in ob.data.vertices:
                weight=max(0.0,min(1.0,(v.co.z-2.65)/.45))
                chest.add([v.index],weight,"REPLACE")
                waist.add([v.index],1-weight,"REPLACE")

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
