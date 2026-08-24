#!/usr/bin/env python3
"""Build the Grand Sluggers hero-shared blockout (Rio / Harbor kid) and export FBX.

Bone names match data/art/rig.json. Unity Generic rig, character faces -Z.
ToyScale 1.18 is applied in Unity — do not bake it here.
"""
from __future__ import annotations

import argparse
import math
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


def nuke():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for block in (bpy.data.meshes, bpy.data.armatures, bpy.data.materials, bpy.data.curves):
        for item in list(block):
            block.remove(item)


def mat(name, color):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (*color, 1.0)
        bsdf.inputs["Roughness"].default_value = 0.55
    return m


def mesh_prim(kind, name, loc, scale, material, rot=(0.0, 0.0, 0.0)):
    if kind == "uv_sphere":
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.5, location=loc, segments=24, ring_count=16)
    elif kind == "cylinder":
        bpy.ops.mesh.primitive_cylinder_add(radius=0.5, depth=1.0, location=loc, vertices=24)
    else:
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc)
    ob = bpy.context.active_object
    ob.name = name
    ob.scale = scale
    ob.rotation_euler = rot
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    ob.data.materials.append(material)
    return ob


def add_bone(arm, name, head, tail, parent=None):
    b = arm.edit_bones.new(name)
    b.head = Vector(head)
    b.tail = Vector(tail)
    b.use_connect = False
    if parent is not None:
        b.parent = arm.edit_bones[parent]
    return b


def skin(ob, arm_ob, bone):
    """Vertex-group skin, not bone-parent. Unity imports this as SkinnedMeshRenderer."""
    ob.parent = arm_ob
    ob.parent_type = "OBJECT"
    vg = ob.vertex_groups.new(name=bone)
    vg.add(list(range(len(ob.data.vertices))), 1.0, "REPLACE")
    mod = ob.modifiers.new("Armature", "ARMATURE")
    mod.object = arm_ob
    mod.use_vertex_groups = True


def build(out: Path):
    nuke()
    jersey = mat("jersey", (0.86, 0.19, 0.16))
    trim = mat("trim", (0.86, 0.19, 0.16))
    gold = mat("gold", (1.0, 0.80, 0.25))
    flesh = mat("flesh", (0.95, 0.79, 0.64))
    slack = mat("slack", (0.95, 0.95, 0.93))
    ink = mat("ink", (0.08, 0.07, 0.07))
    white = mat("white", (1.0, 1.0, 1.0))
    sneaker = mat("sneaker", (0.96, 0.96, 0.96))

    arm_data = bpy.data.armatures.new("hero-shared-data")
    arm_ob = bpy.data.objects.new("hero-shared", arm_data)
    bpy.context.collection.objects.link(arm_ob)
    bpy.context.view_layer.objects.active = arm_ob
    bpy.ops.object.mode_set(mode="EDIT")

    # Blender Z-up. Character faces +Y (Unity -Z after FBX axis_forward='-Z').
    add_bone(arm_data, "root", (0, 0, 0), (0, 0, 0.25))
    add_bone(arm_data, "torso", (0, 0, 1.15), (0, 0, 2.55), "root")
    add_bone(arm_data, "head", (0, 0.05, 3.35), (0, 0.05, 4.55), "torso")
    add_bone(arm_data, "lUpper", (-0.95, 0, 2.45), (-0.95, 0, 1.55), "torso")
    add_bone(arm_data, "lFore", (-0.95, 0, 1.55), (-0.95, 0, 0.85), "lUpper")
    add_bone(arm_data, "rUpper", (0.95, 0, 2.45), (0.95, 0, 1.55), "torso")
    add_bone(arm_data, "rFore", (0.95, 0, 1.55), (0.95, 0, 0.85), "rUpper")
    add_bone(arm_data, "lThigh", (-0.42, 0, 1.05), (-0.42, 0, 0.45), "root")
    add_bone(arm_data, "lShin", (-0.42, 0, 0.45), (-0.42, 0, 0.08), "lThigh")
    add_bone(arm_data, "rThigh", (0.42, 0, 1.05), (0.42, 0, 0.45), "root")
    add_bone(arm_data, "rShin", (0.42, 0, 0.45), (0.42, 0, 0.08), "rThigh")
    add_bone(arm_data, "bat", (1.15, 0.15, 0.90), (1.15, 0.15, 0.20), "rFore")
    add_bone(arm_data, "glove", (-1.15, 0.15, 0.90), (-1.15, 0.15, 0.20), "lFore")

    bpy.ops.object.mode_set(mode="OBJECT")

    pieces = []

    def add(kind, name, loc, scale, material, bone, rot=(0.0, 0.0, 0.0)):
        ob = mesh_prim(kind, name, loc, scale, material, rot)
        pieces.append((ob, bone))
        return ob

    add("uv_sphere", "Hip", (0, 0, 1.05), (1.42, 1.12, 1.00), slack, "root")
    add("uv_sphere", "torsoMesh", (0, 0.05, 2.28), (1.42, 0.98, 1.85), jersey, "torso")
    add("cube", "Stripe", (0, 0.52, 2.35), (0.32, 0.08, 1.20), gold, "torso")
    add("uv_sphere", "headMesh", (0, 0.08, 4.05), (1.72, 1.72, 1.72), flesh, "head")
    add("uv_sphere", "EyeL", (-0.32, 0.78, 4.18), (0.28, 0.12, 0.36), ink, "head")
    add("uv_sphere", "EyeR", (0.32, 0.78, 4.18), (0.28, 0.12, 0.36), ink, "head")
    add("uv_sphere", "lUpperMesh", (-0.95, 0, 2.00), (0.52, 0.52, 1.05), jersey, "lUpper")
    add("uv_sphere", "lForeMesh", (-0.95, 0, 1.20), (0.42, 0.42, 0.78), flesh, "lFore")
    add("uv_sphere", "lHand", (-0.95, 0.08, 0.78), (0.38, 0.32, 0.32), flesh, "lFore")
    add("uv_sphere", "rUpperMesh", (0.95, 0, 2.00), (0.52, 0.52, 1.05), jersey, "rUpper")
    add("uv_sphere", "rForeMesh", (0.95, 0, 1.20), (0.42, 0.42, 0.78), flesh, "rFore")
    add("uv_sphere", "rHand", (0.95, 0.08, 0.78), (0.38, 0.32, 0.32), flesh, "rFore")
    add("uv_sphere", "lThighMesh", (-0.42, 0, 0.78), (0.56, 0.56, 0.85), slack, "lThigh")
    add("uv_sphere", "lShinMesh", (-0.42, 0, 0.32), (0.46, 0.46, 0.55), slack, "lShin")
    add("cube", "lShoe", (-0.42, 0.28, 0.12), (0.62, 0.95, 0.38), sneaker, "lShin")
    add("uv_sphere", "rThighMesh", (0.42, 0, 0.78), (0.56, 0.56, 0.85), slack, "rThigh")
    add("uv_sphere", "rShinMesh", (0.42, 0, 0.32), (0.46, 0.46, 0.55), slack, "rShin")
    add("cube", "rShoe", (0.42, 0.28, 0.12), (0.62, 0.95, 0.38), sneaker, "rShin")

    bpy.context.view_layer.objects.active = arm_ob
    bpy.ops.object.mode_set(mode="OBJECT")
    for ob, bone in pieces:
        skin(ob, arm_ob, bone)

    missing = [n for n in BONES if n not in arm_data.bones]
    if missing:
        raise RuntimeError("missing bones: " + ",".join(missing))

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
    print("exported", out)


def main(argv):
    p = argparse.ArgumentParser()
    p.add_argument("--out", required=True)
    args = p.parse_args(argv)
    build(Path(args.out).resolve())


if __name__ == "__main__":
    argv = sys.argv
    if "--" in argv:
        argv = argv[argv.index("--") + 1 :]
    else:
        argv = argv[1:]
    main(argv)
