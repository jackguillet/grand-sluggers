#!/usr/bin/env python3
"""Swing take on the hero-shared armature. Contact at 0.30s (MoveBones.SwingContact).

Keys match data/art/pose-clips/swing.json so the FBX is the same cut, not a T-pose.
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
CONTACT = 0.30

# data/art/pose-clips/swing.json — Unity parent-space euler offsets (degrees).
KEYS = [
    (0.00, {
        "torso": (8, -32, 4), "head": (6, -18, 0),
        "lUpper": (-22, 30, 38), "lFore": (24, 0, 0),
        "rUpper": (-118, -54, -64), "rFore": (32, 0, 0),
        "lThigh": (14, 0, 0), "lShin": (18, 0, 0),
        "rThigh": (-10, 12, 0), "rShin": (14, 0, 0),
        "bat": (132, 16, 20),
    }),
    (0.15, {
        "torso": (12, 6, -6), "head": (6, 4, 0),
        "lUpper": (-8, 16, 28), "lFore": (28, 0, 0),
        "rUpper": (-62, -28, -36), "rFore": (26, 0, 0),
        "lThigh": (20, 0, 0), "lShin": (20, 0, 0),
        "rThigh": (-16, 18, 0), "rShin": (18, 0, 0),
        "bat": (64, 52, 14),
    }),
    (0.30, {
        "torso": (18, 88, -12), "head": (10, 32, 0),
        "lUpper": (38, -62, 12), "lFore": (40, 0, 0),
        "rUpper": (22, 92, 34), "rFore": (8, 0, 0),
        "lThigh": (20, 0, 0), "lShin": (20, 0, 0),
        "rThigh": (-24, 24, 0), "rShin": (26, 0, 0),
        "bat": (-12, 96, 6),
    }),
    (0.50, {
        "torso": (12, 108, -18), "head": (14, 40, 0),
        "lUpper": (56, -90, -14), "lFore": (22, 0, 0),
        "rUpper": (8, 112, 44), "rFore": (6, 0, 0),
        "lThigh": (12, 0, 0), "lShin": (16, 0, 0),
        "rThigh": (-14, 20, 0), "rShin": (22, 0, 0),
        "bat": (-82, 186, 24),
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


def key_swing(arm_ob):
    scene = bpy.context.scene
    scene.render.fps = FPS
    # Blender / FBX takes are 1-based. t=0 is frame 1 so Unity t=0.30 is Contact.
    scene.frame_start = 1
    scene.frame_end = 1 + int(round(DURATION * FPS))
    scene.frame_current = 1

    action = bpy.data.actions.new("swing")
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
    key_swing(arm)
    hero.export_fbx(Path(args.out).resolve(), anim=True)
    print("swing contact frame", 1 + int(round(CONTACT * FPS)), "fps", FPS)


if __name__ == "__main__":
    argv = sys.argv
    if "--" in argv:
        argv = argv[argv.index("--") + 1 :]
    else:
        argv = argv[1:]
    main(argv)
