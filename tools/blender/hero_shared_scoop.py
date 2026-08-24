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

# data/art/pose-clips/scoop.json — Unity parent-space euler offsets (degrees).
KEYS = [
    (0.00, {
        "torso": (18, 8, 0), "head": (14, 4, 0),
        "lUpper": (22, 8, 24), "lFore": (26, 0, 0),
        "rUpper": (26, -10, -24), "rFore": (30, 0, 0),
        "lThigh": (52, 12, 0), "lShin": (38, 0, 0),
        "rThigh": (44, -10, 0), "rShin": (34, 0, 0),
        "glove": (30, 0, 0),
    }),
    (0.10, {
        "torso": (58, 4, 0), "head": (22, 2, 0),
        "lUpper": (48, 10, 8), "lFore": (52, 0, 0),
        "rUpper": (55, -12, -6), "rFore": (56, 0, 0),
        "lThigh": (70, 14, 0), "lShin": (58, 0, 0),
        "rThigh": (64, -12, 0), "rShin": (54, 0, 0),
        "glove": (56, 0, 0),
    }),
    (0.22, {
        "torso": (72, 0, 0), "head": (28, 0, 0),
        "lUpper": (52, 8, 6), "lFore": (58, 0, 0),
        "rUpper": (58, -10, -4), "rFore": (62, 0, 0),
        "lThigh": (78, 16, 0), "lShin": (62, 0, 0),
        "rThigh": (72, -14, 0), "rShin": (58, 0, 0),
        "glove": (62, 0, 0),
    }),
    (0.50, {
        "torso": (10, -8, 0), "head": (6, -4, 0),
        "lUpper": (18, 6, 20), "lFore": (20, 0, 0),
        "rUpper": (16, -8, -18), "rFore": (24, 0, 0),
        "lThigh": (28, 6, 0), "lShin": (22, 0, 0),
        "rThigh": (22, -6, 0), "rShin": (18, 0, 0),
        "glove": (24, 0, 0),
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
