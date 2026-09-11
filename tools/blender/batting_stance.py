"""Shared DCC targets for the camera-neutral batting stance.

The source catalog is written in Unity batter-local axes: +X crosses the
plate for a right-handed batter, +Y is up, and +Z faces the pitcher. Blender's
FBX basis maps those directions to (-X, -Z, +Y).
"""
from __future__ import annotations

import json
import math
from pathlib import Path

import bpy
from mathutils import Matrix, Quaternion, Vector


CATALOG = Path(__file__).resolve().parents[2] / "data/art/pose-clips/swing.json"


def _load_keys():
    rows = json.loads(CATALOG.read_text())["keys"]
    return {
        round(float(row["t"]), 4): {
            "chest": Vector(row["chestForward"]).normalized(),
            "eyes": Vector(row["eyesForward"]).normalized(),
            "feet": Vector(row["feetAxis"]).normalized(),
        }
        for row in rows
    }


KEYS = _load_keys()


def unity_to_dcc(value, *, normalize: bool = True):
    value = Vector(value)
    converted = Vector((-value.x, -value.z, value.y))
    return converted.normalized() if normalize else converted


def target_at(t: float):
    key = round(float(t), 4)
    if key not in KEYS:
        raise KeyError(f"batting stance has no authored key at {t:.4f}")
    row = KEYS[key]
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
):
    """Align feet, visible chest, and visible eyes to the shared stance key."""
    target = target_at(t)
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
    minimum_dot: float = 0.995,
):
    target = target_at(t)
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
