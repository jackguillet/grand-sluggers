#!/usr/bin/env python3
"""Scoop take on the hero-shared armature. Contact at 0.22s (glove on the dirt).

Keys match data/art/pose-clips/scoop.json so the FBX is a pick, not a T-pose.
"""
from __future__ import annotations

import argparse
import importlib.util
import math
import sys
from pathlib import Path

import bpy


FPS = 60
DURATION = 0.50
CONTACT = 0.22

# Authored on the FBX bind (bones along their length). Capsule JSON eulers
# of torso X=72 laid this mesh on its side. Contact is a crouch pick.
KEYS = [
    (0.00, {
        "torso": (12, 6, 0), "head": (10, 4, 0),
        "lUpper": (18, 6, 10), "lFore": (22, 0, 0),
        "rUpper": (20, -8, -10), "rFore": (24, 0, 0),
        "lThigh": (28, 8, 0), "lShin": (22, 0, 0),
        "rThigh": (24, -8, 0), "rShin": (20, 0, 0),
        "glove": (24, 0, 0),
    }),
    (0.10, {
        "torso": (22, 2, 0), "head": (16, 2, 0),
        "lUpper": (32, 8, 6), "lFore": (38, 0, 0),
        "rUpper": (36, -8, -6), "rFore": (42, 0, 0),
        "lThigh": (42, 10, 0), "lShin": (34, 0, 0),
        "rThigh": (38, -10, 0), "rShin": (32, 0, 0),
        "glove": (42, 0, 0),
    }),
    (0.22, {
        "torso": (28, 0, 0), "head": (18, 0, 0),
        "lUpper": (40, 6, 4), "lFore": (48, 0, 0),
        "rUpper": (44, -8, -4), "rFore": (52, 0, 0),
        "lThigh": (48, 12, 0), "lShin": (40, 0, 0),
        "rThigh": (44, -12, 0), "rShin": (38, 0, 0),
        "glove": (52, 0, 0),
    }),
    (0.50, {
        "torso": (8, -4, 0), "head": (6, -2, 0),
        "lUpper": (14, 4, 12), "lFore": (16, 0, 0),
        "rUpper": (12, -6, -12), "rFore": (18, 0, 0),
        "lThigh": (18, 4, 0), "lShin": (14, 0, 0),
        "rThigh": (16, -4, 0), "rShin": (12, 0, 0),
        "glove": (18, 0, 0),
    }),
]


def load_blockout():
    path = Path(__file__).resolve().parent / "hero_shared_blockout.py"
    spec = importlib.util.spec_from_file_location("hero_shared_blockout", path)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def deg(v):
    return tuple(math.radians(x) for x in v)


def key_scoop(arm_ob):
    scene = bpy.context.scene
    scene.render.fps = FPS
    scene.frame_start = 1
    scene.frame_end = 1 + int(round(DURATION * FPS))
    scene.frame_current = 1

    action = bpy.data.actions.new("scoop")
    arm_ob.animation_data_create()
    arm_ob.animation_data.action = action

    bpy.context.view_layer.objects.active = arm_ob
    bpy.ops.object.mode_set(mode="POSE")
    for pb in arm_ob.pose.bones:
        pb.rotation_mode = "XYZ"

    for t, pose in KEYS:
        frame = 1 + int(round(t * FPS))
        scene.frame_set(frame)
        for name, euler in pose.items():
            if name not in arm_ob.pose.bones:
                continue
            pb = arm_ob.pose.bones[name]
            pb.rotation_mode = "XYZ"
            pb.rotation_euler = deg(euler)
            pb.keyframe_insert(data_path="rotation_euler", frame=frame)

    for layer in action.layers:
        for strip in layer.strips:
            for bag in strip.channelbags:
                for fc in bag.fcurves:
                    for kp in fc.keyframe_points:
                        kp.interpolation = "LINEAR"

    scene.frame_set(1 + int(round(CONTACT * FPS)))
    bpy.ops.object.mode_set(mode="OBJECT")


def main(argv):
    p = argparse.ArgumentParser()
    p.add_argument("--out", required=True)
    args = p.parse_args(argv)
    hero = load_blockout()
    arm = hero.build_scene()
    key_scoop(arm)
    hero.export_fbx(Path(args.out).resolve(), anim=True)
    print("scoop contact frame", 1 + int(round(CONTACT * FPS)), "fps", FPS)


if __name__ == "__main__":
    argv = sys.argv
    if "--" in argv:
        argv = argv[argv.index("--") + 1 :]
    else:
        argv = argv[1:]
    main(argv)
