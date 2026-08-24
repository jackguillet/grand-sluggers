#!/usr/bin/env python3
"""Pitch take on the hero-shared armature. Release at 0.42s (MoveBones.PitchRelease).

Keys match data/art/pose-clips/pitch.json so the FBX is a throw toward home, not a T-pose.
From the plate the arm goes back, high-kicks, then comes forward at Release.
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
RELEASE = 0.42

# data/art/pose-clips/pitch.json — Unity parent-space euler offsets (degrees).
KEYS = [
    (0.00, {
        "torso": (-16, 28, 4), "head": (8, 14, 0),
        "lUpper": (22, 10, 32), "lFore": (28, 0, 0),
        "rUpper": (-132, 48, -58), "rFore": (42, 0, 0),
        "lThigh": (62, 8, 0), "lShin": (36, 0, 0),
        "rThigh": (-12, -6, 0), "rShin": (14, 0, 0),
    }),
    (0.18, {
        "torso": (-10, 16, 2), "head": (6, 8, 0),
        "lUpper": (28, 6, 26), "lFore": (24, 0, 0),
        "rUpper": (-48, 22, -28), "rFore": (28, 0, 0),
        "lThigh": (72, 10, 0), "lShin": (18, 0, 0),
        "rThigh": (8, -4, 0), "rShin": (16, 0, 0),
    }),
    (0.42, {
        "torso": (16, -28, -8), "head": (10, -10, 0),
        "lUpper": (14, -10, 18), "lFore": (12, 0, 0),
        "rUpper": (108, -56, 24), "rFore": (4, 0, 0),
        "lThigh": (36, 4, 0), "lShin": (14, 0, 0),
        "rThigh": (16, -8, 0), "rShin": (22, 0, 0),
    }),
    (0.50, {
        "torso": (18, -32, -8), "head": (12, -12, 0),
        "lUpper": (12, -6, 16), "lFore": (14, 0, 0),
        "rUpper": (118, -48, 28), "rFore": (4, 0, 0),
        "lThigh": (22, 2, 0), "lShin": (12, 0, 0),
        "rThigh": (12, -6, 0), "rShin": (18, 0, 0),
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


def key_pitch(arm_ob):
    scene = bpy.context.scene
    scene.render.fps = FPS
    scene.frame_start = 1
    scene.frame_end = 1 + int(round(DURATION * FPS))
    scene.frame_current = 1

    action = bpy.data.actions.new("pitch")
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

    scene.frame_set(1 + int(round(RELEASE * FPS)))
    bpy.ops.object.mode_set(mode="OBJECT")


def main(argv):
    p = argparse.ArgumentParser()
    p.add_argument("--out", required=True)
    args = p.parse_args(argv)
    hero = load_blockout()
    arm = hero.build_scene()
    key_pitch(arm)
    hero.export_fbx(Path(args.out).resolve(), anim=True)
    print("pitch release frame", 1 + int(round(RELEASE * FPS)), "fps", FPS)


if __name__ == "__main__":
    argv = sys.argv
    if "--" in argv:
        argv = argv[argv.index("--") + 1 :]
    else:
        argv = argv[1:]
    main(argv)
