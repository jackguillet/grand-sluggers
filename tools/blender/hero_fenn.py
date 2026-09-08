#!/usr/bin/env python3
"""Elder Fenn: keep the authored turtle, skin it to posed named bones.

The posed GLB/FBX is the identity (shell, face, scarf, cane). Bones are
placed *in that rest pose* and each vert gets one bone at 100% — no heat
weight, no sphere replacement, no rigid statue.

  /opt/homebrew/bin/blender --background --python tools/blender/hero_fenn.py -- \
    --src scratchpad/fenn-src/fenn-source.fbx \
    --out unity/Assets/Art/Characters/fenn/fenn.fbx \
    --albedo scratchpad/fenn-src/fenn-albedo.png \
    --resources unity/Assets/Resources/Art/Characters/fenn
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
    "root", "torso", "head",
    "lUpper", "lFore", "rUpper", "rFore",
    "lThigh", "lShin", "rThigh", "rShin",
    "bat", "glove",
]


def nuke():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for coll in (bpy.data.meshes, bpy.data.armatures, bpy.data.actions):
        for item in list(coll):
            coll.remove(item)


def add_bone(arm, name, head, tail, parent=None):
    b = arm.edit_bones.new(name)
    b.head = Vector(head)
    b.tail = Vector(tail)
    b.use_connect = False
    if parent is not None:
        b.parent = arm.edit_bones[parent]
    return b


def import_turtle(src: Path):
    bpy.ops.import_scene.fbx(filepath=str(src))
    mesh = next(o for o in bpy.data.objects if o.type == "MESH")
    # Drop the T-pose armature that came with the statue — it does not match this pose.
    for o in list(bpy.data.objects):
        if o.type == "ARMATURE":
            bpy.data.objects.remove(o, do_unlink=True)
    mesh.parent = None
    mesh.matrix_parent_inverse.identity()
    mesh.name = "fenn"
    return mesh


def build_armature():
    """Bones sit in the crouched turtle, not a T-pose."""
    arm_data = bpy.data.armatures.new("fenn-data")
    arm_ob = bpy.data.objects.new("fenn", arm_data)
    bpy.context.collection.objects.link(arm_ob)
    bpy.context.view_layer.objects.active = arm_ob
    bpy.ops.object.mode_set(mode="EDIT")

    add_bone(arm_data, "root", (0.0, 0.0, 0.0), (0.0, 0.0, 0.22))
    add_bone(arm_data, "torso", (0.0, 0.08, 1.35), (0.0, 0.05, 2.45), "root")
    add_bone(arm_data, "head", (-0.1, -0.35, 3.15), (-0.14, -0.55, 4.55), "torso")
    add_bone(arm_data, "lUpper", (-0.85, -0.08, 2.25), (-1.50, -0.20, 1.15), "torso")
    add_bone(arm_data, "lFore", (-1.50, -0.20, 1.15), (-1.80, -0.13, 0.38), "lUpper")
    add_bone(arm_data, "rUpper", (0.85, 0.06, 2.28), (1.50, 0.10, 1.18), "torso")
    add_bone(arm_data, "rFore", (1.50, 0.10, 1.18), (1.56, -0.15, 0.50), "rUpper")
    add_bone(arm_data, "lThigh", (-0.48, 0.18, 1.00), (-0.62, 0.26, 0.42), "root")
    add_bone(arm_data, "lShin", (-0.62, 0.26, 0.42), (-0.70, 0.30, 0.04), "lThigh")
    add_bone(arm_data, "rThigh", (0.48, -0.18, 0.98), (0.68, -0.40, 0.40), "root")
    add_bone(arm_data, "rShin", (0.68, -0.40, 0.40), (0.75, -0.52, 0.04), "rThigh")
    add_bone(arm_data, "bat", (1.20, 0.90, 1.70), (1.40, 2.10, 1.40), "rFore")
    add_bone(arm_data, "glove", (-1.80, -0.13, 0.38), (-1.95, -0.10, 0.10), "lFore")

    bpy.ops.object.mode_set(mode="OBJECT")
    return arm_ob, arm_data


def hard_skin(mesh, arm_ob):
    """One bone per vert. Shell/head never share an arm weight."""
    mw = mesh.matrix_world
    bpy.context.view_layer.objects.active = arm_ob
    bpy.ops.object.mode_set(mode="POSE")
    bones = {b.name: b for b in arm_ob.pose.bones}
    bpy.ops.object.mode_set(mode="OBJECT")

    centers = {}
    for name, pb in bones.items():
        head = arm_ob.matrix_world @ pb.bone.head_local
        tail = arm_ob.matrix_world @ pb.bone.tail_local
        centers[name] = (head + tail) * 0.5

    limb = ["lUpper", "lFore", "rUpper", "rFore", "lThigh", "lShin", "rThigh", "rShin"]

    while mesh.vertex_groups:
        mesh.vertex_groups.remove(mesh.vertex_groups[0])
    groups = {n: mesh.vertex_groups.new(name=n) for n in BONES}

    assign = {n: [] for n in BONES}
    for i, v in enumerate(mesh.data.vertices):
        p = mw @ v.co
        if p.y > 0.95 and p.x > 0.25 and p.z < 2.4:
            name = "bat"
        elif p.z >= 1.9:
            # Helmet-shell + face + brim. Never an arm.
            name = "head"
        elif abs(p.x) < 1.4:
            if p.z >= 0.9:
                name = "torso"
            else:
                name = min(["lThigh", "lShin", "rThigh", "rShin"],
                           key=lambda n: (p - centers[n]).length)
        else:
            name = min(["lUpper", "lFore", "rUpper", "rFore"],
                       key=lambda n: (p - centers[n]).length)
        assign[name].append(i)

    for name, idxs in assign.items():
        if idxs:
            groups[name].add(idxs, 1.0, "REPLACE")
        print("group", name, len(idxs))

    mesh.parent = arm_ob
    mesh.parent_type = "OBJECT"
    for mod in list(mesh.modifiers):
        mesh.modifiers.remove(mod)
    arm_mod = mesh.modifiers.new("Armature", "ARMATURE")
    arm_mod.object = arm_ob
    arm_mod.use_vertex_groups = True
    arm_mod.use_deform_preserve_volume = True


def export_fbx(out: Path):
    out.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(
        filepath=str(out),
        use_selection=False,
        object_types={"ARMATURE", "MESH"},
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        bake_anim=False,
        armature_nodetype="NULL",
        primary_bone_axis="Y",
        secondary_bone_axis="X",
        axis_forward="-Z",
        axis_up="Y",
        apply_scale_options="FBX_SCALE_ALL",
        bake_space_transform=True,
        path_mode="AUTO",
    )
    print("exported", out, out.stat().st_size)


def pose_preview(arm_ob):
    bpy.context.view_layer.objects.active = arm_ob
    bpy.ops.object.mode_set(mode="POSE")
    for name, euler in {
        "rUpper": (math.radians(70), 0.0, math.radians(-10)),
        "rFore": (math.radians(20), 0.0, 0.0),
        "lThigh": (math.radians(30), 0.0, 0.0),
    }.items():
        b = arm_ob.pose.bones[name]
        b.rotation_mode = "XYZ"
        b.rotation_euler = euler
    bpy.context.view_layer.update()
    bpy.ops.object.mode_set(mode="OBJECT")


def build(src: Path, out: Path, albedo: Path, resources: Path | None = None):
    nuke()
    mesh = import_turtle(src)
    arm_ob, arm_data = build_armature()
    hard_skin(mesh, arm_ob)
    missing = [n for n in BONES if n not in arm_data.bones]
    if missing:
        raise RuntimeError("missing bones: " + ",".join(missing))
    export_fbx(out)
    if albedo.is_file():
        dest = out.parent / "fenn-albedo.png"
        if albedo.resolve() != dest.resolve():
            shutil.copy2(albedo, dest)
        print("albedo", dest, dest.stat().st_size)
    if resources is not None:
        resources.mkdir(parents=True, exist_ok=True)
        shutil.copy2(out, resources / out.name)
        alb = out.parent / "fenn-albedo.png"
        if alb.is_file():
            shutil.copy2(alb, resources / alb.name)
        print("resources", resources)


def main(argv):
    p = argparse.ArgumentParser()
    p.add_argument("--src", required=True)
    p.add_argument("--out", required=True)
    p.add_argument("--albedo", required=True)
    p.add_argument("--resources", default="")
    args = p.parse_args(argv)
    res = Path(args.resources).resolve() if args.resources else None
    build(Path(args.src).resolve(), Path(args.out).resolve(), Path(args.albedo).resolve(), res)


if __name__ == "__main__":
    argv = sys.argv
    if "--" in argv:
        argv = argv[argv.index("--") + 1 :]
    else:
        argv = argv[1:]
    main(argv)
