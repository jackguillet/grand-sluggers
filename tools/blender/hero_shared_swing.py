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
from mathutils import Matrix, Vector


FPS = 60
DURATION = 0.50
CONTACT = 0.30

# data/art/pose-clips/swing.json — parent-space Euler offsets (degrees).
# The hand solve holds the palms 0.21–0.28 ft apart around one grip. The
# socket-to-barrel vector travels upward about 10 degrees from approach to contact.
KEYS = [
    (0.00, {
        "torso": (8, -28, 4), "head": (4, -12, 0),
        "lUpper": (-154.70, -1.03, 80), "lFore": (-56.41, 0, 0),
        "rUpper": (61.05, 50.21, 95.48), "rFore": (-143.30, 0, 0),
        "lThigh": (12, 0, 0), "lShin": (16, 0, 0),
        "rThigh": (-8, 10, 0), "rShin": (12, 0, 0),
        "bat": (7.35, -7.27, -158.19),
    }),
    (0.15, {
        "torso": (10, 8, -5), "head": (5, 2, 0),
        "lUpper": (-91.41, -112.21, 45.66), "lFore": (-84.28, 0, 0),
        "rUpper": (28.76, 114.82, 53.66), "rFore": (-121.03, 0, 0),
        "lThigh": (18, 0, 0), "lShin": (18, 0, 0),
        "rThigh": (-14, 16, 0), "rShin": (16, 0, 0),
        "bat": (166.52, 26.27, 34.63),
    }),
    (0.24, {
        "torso": (14, 52, -8), "head": (7, 18, 0),
        "lUpper": (11.86, -52.93, -76.80), "lFore": (-1.28, 0, 0),
        "rUpper": (36.53, 158.11, 45.80), "rFore": (-110.01, 0, 0),
        "lThigh": (20, 0, 0), "lShin": (20, 0, 0),
        "rThigh": (-22, 22, 0), "rShin": (24, 0, 0),
        "bat": (103.04, 46.96, 36.09),
    }),
    (0.30, {
        "torso": (16, 72, -10), "head": (8, 26, 0),
        "lUpper": (10.77, -49.04, -76.15), "lFore": (-18.87, 0, 0),
        "rUpper": (35.32, 176.10, 49.25), "rFore": (-119.92, 0, 0),
        "lThigh": (20, 0, 0), "lShin": (20, 0, 0),
        "rThigh": (-24, 24, 0), "rShin": (26, 0, 0),
        "bat": (71.06, 34.68, 32.35),
    }),
    (0.50, {
        "torso": (10, 96, -14), "head": (12, 34, 0),
        "lUpper": (24.60, -42.75, -93.93), "lFore": (-1.22, 0, 0),
        "rUpper": (21.38, 187.18, 60.09), "rFore": (-123.96, 0, 0),
        "lThigh": (12, 0, 0), "lShin": (16, 0, 0),
        "rThigh": (-14, 20, 0), "rShin": (22, 0, 0),
        "bat": (140.33, 12.53, 10.86),
    }),
]

# Batter-local Unity directions. Blender's FBX export changes handedness, so
# the DCC target is (-X, -Z, +Y). The common bat's +Y model axis binds to the
# socket's -Y bone axis; the authored socket rotation remains the sole owner of
# the barrel direction through the take.
BARREL_DIRECTIONS = {
    0.00: (-0.18, 0.89, 0.42),
    0.15: (-0.10, 0.62, -0.78),
    0.24: (0.4315, 0.005, -0.9022),
    0.30: (0.7790, 0.0275, -0.6264),
    0.50: (-0.54, 0.31, -0.78),
}


def load_blockout():
    path = Path(__file__).resolve().parent / "hero_shared_blockout.py"
    spec = importlib.util.spec_from_file_location("hero_shared_blockout", path)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def deg(v):
    return tuple(math.radians(x) for x in v)


def key_bat_direction(arm_ob, direction, frame):
    """Aim the authored bat bone; never patch its world rotation in Unity."""
    bat = arm_ob.pose.bones["bat"]
    # Preserve the keyed Euler as the roll seed, then rotate only enough to put
    # socket -Y on the imported handle-to-barrel direction.
    bpy.context.view_layer.update()
    current = -(bat.matrix.to_3x3() @ Vector((0, 1, 0))).normalized()
    unity = Vector(direction).normalized()
    target = Vector((-unity.x, -unity.z, unity.y)).normalized()
    correction = current.rotation_difference(target)
    matrix = bat.matrix.copy()
    bat.rotation_mode = "QUATERNION"
    bat.matrix = Matrix.LocRotScale(
        matrix.translation,
        correction @ matrix.to_quaternion(),
        matrix.to_scale(),
    )
    bat.keyframe_insert(data_path="rotation_quaternion", frame=frame)


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
            if name != "bat":
                pb.keyframe_insert(data_path="rotation_euler", frame=frame)
        key_bat_direction(arm_ob, BARREL_DIRECTIONS[t], frame)

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
