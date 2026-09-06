#!/usr/bin/env python3
"""Bake idle + pose takes on an existing Generic character FBX.

Does not remesh, heat-weight, or split. Clips are bone-local eulers on
THIS armature so Unity Generic + Animator can play them.

  /opt/homebrew/bin/blender --background --python tools/blender/package_clips.py -- \
    --src unity/Assets/Art/Characters/fenn/fenn.fbx --id fenn \
    --out-dir unity/Assets/Art/Characters/fenn \
    --resources unity/Assets/Resources/Art/Characters/fenn
"""
from __future__ import annotations

import argparse
import math
import shutil
import sys
from pathlib import Path

import bpy


def nuke():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for coll in (bpy.data.actions, bpy.data.armatures, bpy.data.meshes):
        for item in list(coll):
            coll.remove(item)


def armature():
    arms = [o for o in bpy.data.objects if o.type == "ARMATURE"]
    if not arms:
        raise RuntimeError("no armature in source — not a Generic package")
    return arms[0]


def clear_pose(arm):
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="POSE")
    for b in arm.pose.bones:
        b.rotation_mode = "XYZ"
        b.rotation_euler = (0.0, 0.0, 0.0)
        b.location = (0.0, 0.0, 0.0)
    bpy.ops.object.mode_set(mode="OBJECT")


def key_bone(arm, name, frame, euler):
    b = arm.pose.bones.get(name)
    if b is None:
        return
    b.rotation_mode = "XYZ"
    b.rotation_euler = euler
    b.keyframe_insert(data_path="rotation_euler", frame=frame)


def make_action(arm, name, keys):
    """keys: list of (frame, bone, (x,y,z) degrees)."""
    act = bpy.data.actions.new(name)
    if arm.animation_data is None:
        arm.animation_data_create()
    arm.animation_data.action = act
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="POSE")
    for b in arm.pose.bones:
        b.rotation_mode = "XYZ"
        b.rotation_euler = (0.0, 0.0, 0.0)
    for frame, bone, deg in keys:
        key_bone(arm, bone, frame, tuple(math.radians(c) for c in deg))
    bpy.ops.object.mode_set(mode="OBJECT")
    print("action", name)
    return act


def export_take(path: Path, arm, action):
    path.parent.mkdir(parents=True, exist_ok=True)
    arm.animation_data.action = action
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(
        filepath=str(path),
        use_selection=False,
        object_types={"ARMATURE", "MESH"},
        add_leaf_bones=False,
        bake_anim=True,
        bake_anim_use_all_actions=False,
        bake_anim_use_nla_strips=False,
        bake_anim_force_startend_keying=True,
        armature_nodetype="NULL",
        primary_bone_axis="Y",
        secondary_bone_axis="X",
        axis_forward="-Z",
        axis_up="Y",
        path_mode="COPY",
        embed_textures=False,
    )
    print("WROTE", path)


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--src", required=True)
    p.add_argument("--id", required=True)
    p.add_argument("--out-dir", required=True)
    p.add_argument("--resources", default="")
    argv = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else sys.argv[1:]
    args = p.parse_args(argv)
    src = Path(args.src)
    if not src.is_file():
        raise SystemExit("missing " + str(src))

    nuke()
    bpy.ops.import_scene.fbx(filepath=str(src))
    arm = armature()
    clear_pose(arm)

    idle = make_action(arm, "idle", [
        (1, "torso", (0, 0, 0)),
        (12, "torso", (4, 0, 0)),
        (12, "head", (0, 6, 0)),
        (24, "torso", (0, 0, 0)),
        (24, "head", (0, 0, 0)),
    ])
    pose = make_action(arm, "pose", [
        (1, "rUpper", (0, 0, 0)),
        (1, "rFore", (0, 0, 0)),
        (10, "rUpper", (90, 0, -8)),
        (10, "rFore", (24, 0, 0)),
        (10, "torso", (-6, 8, 0)),
    ])

    out = Path(args.out_dir)
    idle_path = out / (args.id + "-idle.fbx")
    pose_path = out / (args.id + "-pose.fbx")
    export_take(idle_path, arm, idle)
    export_take(pose_path, arm, pose)
    if args.resources:
        dest = Path(args.resources)
        dest.mkdir(parents=True, exist_ok=True)
        shutil.copy2(idle_path, dest / idle_path.name)
        shutil.copy2(pose_path, dest / pose_path.name)
        print("WROTE", dest)


if __name__ == "__main__":
    main()
