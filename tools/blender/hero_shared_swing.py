#!/usr/bin/env python3
"""Swing take on the hero-shared armature. Contact at 0.30s (MoveBones.SwingContact).

Targets match data/art/pose-clips/swing.json after FBX handedness conversion.
"""
from __future__ import annotations

import argparse
import importlib.util
import math
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import batting_stance


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

# Evaluated rendered-hand centers and socket grip in Unity shared-root space.
# The same numbers drive SwingPresentation; conversion here is position-safe
# (it changes basis without normalizing the authored distance).
HAND_TARGETS = {
    0.00: {"lFore": (0.300, 2.727, 0.512), "rFore": (0.080, 2.522, 0.326)},
    0.15: {"lFore": (0.416, 2.356, -0.235), "rFore": (0.143, 2.240, 0.016)},
    0.24: {"lFore": (0.366, 1.729, -0.547), "rFore": (0.129, 1.798, -0.140)},
    0.30: {"lFore": (0.458, 1.856, -0.622), "rFore": (0.124, 1.914, -0.257)},
    0.50: {"lFore": (-0.242, 2.195, -0.576), "rFore": (-0.134, 2.155, -0.290)},
}
GRIP_TARGETS = {
    0.00: (0.273, 2.215, 0.226),
    0.15: (0.326, 2.013, 0.249),
    0.24: (0.049, 1.761, 0.071),
    0.30: (-0.069, 1.872, -0.149),
    0.50: (0.060, 2.032, -0.073),
}

HAND_MESH = {"lFore": "lHand", "rFore": "rHand"}
ARM_PARENT = {"lFore": "lUpper", "rFore": "rUpper"}
HANDLE_HOLD_FROM_GRIP = 0.46
HANDLE_LENGTH = 0.85 * 1.28


def load_blockout():
    path = Path(__file__).resolve().parent / "hero_shared_blockout.py"
    spec = importlib.util.spec_from_file_location("hero_shared_blockout", path)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def deg(v):
    return tuple(math.radians(x) for x in v)


def rendered_center(name):
    deps = bpy.context.evaluated_depsgraph_get()
    ob = bpy.data.objects[name].evaluated_get(deps)
    mesh = ob.to_mesh()
    points = [ob.matrix_world @ v.co for v in mesh.vertices]
    ob.to_mesh_clear()
    lo = Vector((min(v.x for v in points), min(v.y for v in points), min(v.z for v in points)))
    hi = Vector((max(v.x for v in points), max(v.y for v in points), max(v.z for v in points)))
    return (lo + hi) * 0.5


def reflect_centerline(matrix):
    reflect = Matrix.Diagonal((-1, 1, 1, 1))
    return reflect @ matrix @ reflect


def solve_rendered_hands(arm_ob, targets):
    controls = []
    for fore_name, target in targets.items():
        control = bpy.data.objects.new("ik-" + fore_name, None)
        bpy.context.collection.objects.link(control)
        control.location = target
        constraint = arm_ob.pose.bones[fore_name].constraints.new("IK")
        constraint.target = control
        constraint.chain_count = 2
        constraint.iterations = 128
        controls.append((fore_name, control, constraint))

    # The rigid hand center sits beyond the forearm tail and off its centerline.
    # Feed that evaluated offset back into the IK target until the mesh, rather
    # than an abstract bone tip, reaches the authored point.
    for _ in range(16):
        bpy.context.view_layer.update()
        for fore_name, control, _ in controls:
            actual = rendered_center(HAND_MESH[fore_name])
            control.location += targets[fore_name] - actual

    bpy.context.view_layer.update()
    solved = {}
    for fore_name, _, _ in controls:
        solved[ARM_PARENT[fore_name]] = arm_ob.pose.bones[ARM_PARENT[fore_name]].matrix.copy()
        solved[fore_name] = arm_ob.pose.bones[fore_name].matrix.copy()
    for fore_name, control, constraint in controls:
        arm_ob.pose.bones[fore_name].constraints.remove(constraint)
        bpy.data.objects.remove(control, do_unlink=True)
    for name in ("lUpper", "rUpper", "lFore", "rFore"):
        bone = arm_ob.pose.bones[name]
        bone.rotation_mode = "QUATERNION"
        bone.matrix = solved[name]
    bpy.context.view_layer.update()


def key_bat_direction(arm_ob, direction, grip, frame):
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
        grip,
        correction @ matrix.to_quaternion(),
        matrix.to_scale(),
    )
    bat.keyframe_insert(data_path="rotation_quaternion", frame=frame)
    bat.keyframe_insert(data_path="location", frame=frame)


def point_segment_distance(point, start, end):
    axis = end - start
    u = 0 if axis.length_squared < 1e-8 else max(0, min(1, (point - start).dot(axis) / axis.length_squared))
    return (point - (start + axis * u)).length


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
        for pb in arm_ob.pose.bones:
            pb.rotation_mode = "XYZ"
            pb.rotation_euler = (0, 0, 0)
            pb.location = (0, 0, 0)
        for name, euler in pose.items():
            if name not in arm_ob.pose.bones:
                continue
            pb = arm_ob.pose.bones[name]
            pb.rotation_mode = "XYZ"
            pb.rotation_euler = deg(euler)
            if name not in {"torso", "head", "lUpper", "lFore", "rUpper", "rFore", "bat"}:
                pb.keyframe_insert(data_path="rotation_euler", frame=frame)
        bpy.context.view_layer.update()

        batting_stance.author_visible_stance(
            arm_ob, t,
            chest_front="Stripe", chest_center="torsoMesh",
            eye_left="EyeL", eye_right="EyeR", head_center="headMesh",
            foot_left="lShoe", foot_right="rShoe",
        )
        targets = {
            name: batting_stance.unity_to_dcc(value, normalize=False)
            for name, value in HAND_TARGETS[t].items()
        }
        solve_rendered_hands(arm_ob, targets)
        for name in ("root", "torso", "head", "lUpper", "lFore", "rUpper", "rFore"):
            arm_ob.pose.bones[name].keyframe_insert(data_path="rotation_quaternion", frame=frame)

        grip = batting_stance.unity_to_dcc(GRIP_TARGETS[t], normalize=False)
        key_bat_direction(arm_ob, BARREL_DIRECTIONS[t], grip, frame)

    for layer in action.layers:
        for strip in layer.strips:
            for bag in strip.channelbags:
                for fc in bag.fcurves:
                    for kp in fc.keyframe_points:
                        kp.interpolation = "LINEAR"

    scene.frame_set(1 + int(round(CONTACT * FPS)))
    bpy.ops.object.mode_set(mode="OBJECT")

    for t, direction in BARREL_DIRECTIONS.items():
        scene.frame_set(1 + int(round(t * FPS)))
        bpy.context.view_layer.update()
        bat = arm_ob.pose.bones["bat"]
        actual = -(bat.matrix.to_3x3() @ Vector((0, 1, 0))).normalized()
        unity = Vector(direction).normalized()
        expected = Vector((-unity.x, -unity.z, unity.y)).normalized()
        if actual.dot(expected) < 0.999:
            raise RuntimeError(f"bat direction missed at {t:.2f}: {actual} vs {expected}")
        grip = bat.head
        handle_end = grip + actual * HANDLE_LENGTH
        for hand in (rendered_center("lHand"), rendered_center("rHand")):
            distance = point_segment_distance(hand, grip, handle_end)
            if distance > 0.30:
                raise RuntimeError(f"rendered hand missed handle at {t:.2f}: {distance:.3f}")
        batting_stance.validate_visible_stance(
            t,
            chest_front="Stripe", chest_center="torsoMesh",
            eye_left="EyeL", eye_right="EyeR", head_center="headMesh",
            foot_left="lShoe", foot_right="rShoe",
        )


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
