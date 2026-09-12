"""Shared DCC targets for the camera-neutral batting stance.

The source catalog is written in Unity batter-local axes: +X crosses the
plate for a right-handed batter, +Y is up, and +Z faces the pitcher. Blender's
FBX basis maps those directions to (-X, -Z, +Y).

Every take here is authored right-handed and reflected in Unity for a
left-handed batter. The side opposite the batting hand leads: that foot stands
nearer the pitcher and that hand holds the knob end of the handle. The
catalog's feetAxis runs from the back foot to the lead foot, so it points at
the pitcher from either box; it is not "right minus left".

The body is authored facing -Y with its face on that side, so the eye
landmarks here are the eyes Unity draws.
"""
from __future__ import annotations

import json
import math
from pathlib import Path

import bpy
from mathutils import Matrix, Quaternion, Vector


CATALOG = Path(__file__).resolve().parents[2] / "data/art/batting-stance.json"


# Which hand the authored take bats with. The shared and package takes are
# right-handed; HeroActor mirrors the sampled pose for a left-handed batter.
BATS_RIGHT = "R"
BATS_LEFT = "L"
# "Feet face the plate": each toe within this cone of the plate direction.
FEET_FACE_PLATE_DOT = 0.7071


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


def target_at(t: float, bats: str = BATS_RIGHT):
    """Unity stance directions at t, converted to DCC axes.

    Between authored keys this interpolates exactly as BattingStance does in
    Sim, so a take baked on every frame agrees with the runtime contract at any
    sample time the gate picks, not only on the five authored keys. A
    left-handed batter is the reflection across the plate line: X flips, the
    pitcher stays where it is (BattingStance.At in Sim).
    """
    index, u = span_at(t, [key for key, _ in KEYS])
    low, high = KEYS[index][1], KEYS[min(index + 1, len(KEYS) - 1)][1]
    row = {
        name: (low[name] + (high[name] - low[name]) * u).normalized()
        for name in low
    }
    if bats == BATS_LEFT:
        row = {name: Vector((-value.x, value.y, value.z)) for name, value in row.items()}
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


def lead_side(bats: str = BATS_RIGHT) -> str:
    """The side opposite the batting hand leads the swing."""
    return BATS_LEFT if bats == BATS_RIGHT else BATS_RIGHT


def plate_direction(bats: str = BATS_RIGHT):
    """Across the plate from the batter's box, in DCC axes."""
    return unity_to_dcc((1.0 if bats == BATS_RIGHT else -1.0, 0.0, 0.0))


def feet_axis(left: str, right: str, bats: str = BATS_RIGHT):
    """From the back foot to the lead foot.

    Not right-minus-left: a right-handed batter leads with the left foot. The
    catalog's feetAxis points at the pitcher, and this signed line agrees with
    it only when the hips face the plate. Aiming the root at right-minus-left
    put the right foot forward on the right-handed take, turned the hips out
    of the box with the toes pointing away from the plate, and left the torso
    twisted half a turn to keep the chest on it.
    """
    lead, back = (left, right) if lead_side(bats) == BATS_LEFT else (right, left)
    return flat(rendered_center(lead) - rendered_center(back))


def toe_direction(arm_ob, shoe: str, shin: str):
    """Where a foot points: from the shin joint it hangs on to the shoe center."""
    joint = arm_ob.matrix_world @ arm_ob.pose.bones[shin].tail
    return flat(rendered_center(shoe) - joint)


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
    bats: str = BATS_RIGHT,
):
    """Align feet, visible chest, and visible eyes to the shared stance key.

    The root is yawed until the back-to-lead foot line meets the catalog's
    feetAxis, which turns the hips -- and the toes with them -- toward the
    plate. The chest and eyes are then aimed on their own bones.
    """
    target = target_at(t, bats)
    aim_bone_from_landmarks(
        arm_ob, "root", feet_axis(foot_left, foot_right, bats), target["feet"])
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
    bats: str = BATS_RIGHT,
    arm_ob=None,
    shin_left: str = "lShin",
    shin_right: str = "rShin",
):
    """Falsify the rendered stance against the catalog.

    The feet check is signed: an unsigned one scored hips turned out of the box
    the same as hips facing the plate, and the Unity swing matrix repeated the
    blind spot through a 56/56 run. With ``arm_ob`` each toe must also face the
    plate, which is the part of Jack's contract the foot line alone cannot see.
    """
    target = target_at(t, bats)
    eyes = (rendered_center(eye_left) + rendered_center(eye_right)) * 0.5
    actual = {
        "chest": visible_direction(chest_front, chest_center),
        "eyes": flat(eyes - rendered_center(head_center)),
        "feet": feet_axis(foot_left, foot_right, bats),
    }
    for name, expected in target.items():
        dot = actual[name].dot(expected)
        if dot < minimum_dot:
            raise RuntimeError(
                f"{name} missed stance at {t:.2f}: dot {dot:.4f}; "
                f"actual {tuple(round(v, 4) for v in actual[name])}; "
                f"expected {tuple(round(v, 4) for v in expected)}"
            )
    if arm_ob is None:
        return
    plate = plate_direction(bats)
    for shoe, shin in ((foot_left, shin_left), (foot_right, shin_right)):
        toe = toe_direction(arm_ob, shoe, shin)
        dot = toe.dot(plate)
        if dot < FEET_FACE_PLATE_DOT:
            raise RuntimeError(
                f"{shoe} does not face the plate at {t:.2f}: dot {dot:.4f}; "
                f"toe {tuple(round(v, 4) for v in toe)}; "
                f"plate {tuple(round(v, 4) for v in plate)}"
            )
