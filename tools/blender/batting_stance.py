"""Shared DCC targets for the camera-neutral batting stance.

The source catalog is written in Unity batter-local axes: +X crosses the
plate for a right-handed batter, +Y is up, and +Z faces the pitcher. Blender's
FBX basis maps those directions to (-X, -Z, +Y).

A rig's *rendered* eyes are not always the eyes this scene authors. Each caller
names its import basis so the DCC aims the landmark that Unity actually draws.
"""
from __future__ import annotations

import json
import math
from pathlib import Path

import bpy
from mathutils import Matrix, Quaternion, Vector


CATALOG = Path(__file__).resolve().parents[2] / "data/art/pose-clips/swing.json"


# How a rig's rendered eyes relate to the DCC eye landmark this module aims.
# A Generic character package (Elder Fenn) ships its authored eye meshes, so the
# landmark is the face Unity draws. SharedRig.TryBindDrop instead hides the
# shared blockout's eye meshes and rebuilds the face on the head bone's Unity
# +Z; the blockout builds that head facing Blender +Y, which FBX import turns
# into Unity -Z, so the drawn face is the reverse of the landmark.
EYES_AS_AUTHORED = 1
EYES_REVERSED_BY_IMPORT = -1


def _load_keys():
    rows = json.loads(CATALOG.read_text())["keys"]
    keys = [
        (
            round(float(row["t"]), 4),
            {
                "chest": Vector(row["chestForward"]).normalized(),
                "eyes": Vector(row["eyesForward"]).normalized(),
                "feet": Vector(row["feetAxis"]).normalized(),
            },
        )
        for row in rows
    ]
    keys.sort(key=lambda row: row[0])
    return keys


KEYS = _load_keys()


def unity_to_dcc(value, *, normalize: bool = True):
    value = Vector(value)
    converted = Vector((-value.x, -value.z, value.y))
    return converted.normalized() if normalize else converted


def span_at(t: float, times):
    """The authored span holding t, matching BattingStance.Interpolate in Sim."""
    t = min(max(float(t), times[0]), times[-1])
    for index in range(len(times) - 1):
        low, high = times[index], times[index + 1]
        if t > high and index < len(times) - 2:
            continue
        u = 0.0 if high - low <= 1e-9 else (t - low) / (high - low)
        return index, u
    return max(len(times) - 2, 0), 0.0


def target_at(t: float, *, eyes_basis: int = EYES_AS_AUTHORED):
    """Unity stance directions at t, converted to DCC axes.

    Between authored keys this interpolates exactly as BattingStance does in
    Sim, so a take baked on every frame agrees with the runtime contract at any
    sample time the gate picks, not only on the five authored keys.
    """
    index, u = span_at(t, [key for key, _ in KEYS])
    low, high = KEYS[index][1], KEYS[min(index + 1, len(KEYS) - 1)][1]
    row = {
        name: (low[name] + (high[name] - low[name]) * u).normalized()
        for name in low
    }
    row["eyes"] = row["eyes"] * float(eyes_basis)
    return {name: unity_to_dcc(value) for name, value in row.items()}


def rendered_center(name: str):
    deps = bpy.context.evaluated_depsgraph_get()
    ob = bpy.data.objects[name].evaluated_get(deps)
    mesh = ob.to_mesh()
    if not mesh.vertices:
        ob.to_mesh_clear()
        raise RuntimeError(f"{name} has no rendered vertices")
    points = [ob.matrix_world @ vertex.co for vertex in mesh.vertices]
    ob.to_mesh_clear()
    low = Vector(tuple(min(point[i] for point in points) for i in range(3)))
    high = Vector(tuple(max(point[i] for point in points) for i in range(3)))
    return (low + high) * 0.5


def flat(value):
    value = Vector((value.x, value.y, 0.0))
    if value.length_squared < 1e-10:
        raise RuntimeError("batting stance landmark collapsed in the ground plane")
    return value.normalized()


def visible_direction(front: str, center: str):
    return flat(rendered_center(front) - rendered_center(center))


def feet_axis(left: str, right: str):
    return flat(rendered_center(right) - rendered_center(left))


def _signed_yaw(current, target):
    current = flat(current)
    target = flat(target)
    return math.atan2(
        current.x * target.y - current.y * target.x,
        current.dot(target),
    )


def aim_bone_from_landmarks(arm_ob, bone_name: str, current, target):
    """Yaw one pose bone in armature space while keeping its head fixed."""
    bone = arm_ob.pose.bones[bone_name]
    matrix = bone.matrix.copy()
    correction = Quaternion((0.0, 0.0, 1.0), _signed_yaw(current, target))
    bone.rotation_mode = "QUATERNION"
    bone.matrix = Matrix.LocRotScale(
        matrix.translation,
        correction @ matrix.to_quaternion(),
        matrix.to_scale(),
    )
    bpy.context.view_layer.update()


def author_visible_stance(
    arm_ob,
    t: float,
    *,
    chest_front: str,
    chest_center: str,
    eye_left: str,
    eye_right: str,
    head_center: str,
    foot_left: str,
    foot_right: str,
    eyes_basis: int = EYES_AS_AUTHORED,
):
    """Align feet, visible chest, and visible eyes to the shared stance key."""
    target = target_at(t, eyes_basis=eyes_basis)
    aim_bone_from_landmarks(
        arm_ob, "root", feet_axis(foot_left, foot_right), target["feet"])
    aim_bone_from_landmarks(
        arm_ob, "torso", visible_direction(chest_front, chest_center), target["chest"])
    eyes = (rendered_center(eye_left) + rendered_center(eye_right)) * 0.5
    aim_bone_from_landmarks(
        arm_ob, "head", flat(eyes - rendered_center(head_center)), target["eyes"])


def validate_visible_stance(
    t: float,
    *,
    chest_front: str,
    chest_center: str,
    eye_left: str,
    eye_right: str,
    head_center: str,
    foot_left: str,
    foot_right: str,
    eyes_basis: int = EYES_AS_AUTHORED,
    minimum_dot: float = 0.995,
):
    target = target_at(t, eyes_basis=eyes_basis)
    eyes = (rendered_center(eye_left) + rendered_center(eye_right)) * 0.5
    actual = {
        "chest": visible_direction(chest_front, chest_center),
        "eyes": flat(eyes - rendered_center(head_center)),
        "feet": feet_axis(foot_left, foot_right),
    }
    for name, expected in target.items():
        dot = actual[name].dot(expected)
        if name == "feet":
            dot = abs(dot)
        if dot < minimum_dot:
            raise RuntimeError(
                f"{name} missed stance at {t:.2f}: dot {dot:.4f}; "
                f"actual {tuple(round(v, 4) for v in actual[name])}; "
                f"expected {tuple(round(v, 4) for v in expected)}"
            )
