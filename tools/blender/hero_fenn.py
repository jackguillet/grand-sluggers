#!/usr/bin/env python3
"""Elder Fenn — cartoon Generic package, rebuilt from volumes.

Why the posed turtle is gone: a fused mid-action mesh cannot take weights.
Faces that span two bones tear. Heat-weight inverts the shell. A freeze is a
statue. Unity Generic wants mesh + armature + weights authored in the DCC
in the rest pose (Unity 6 Creating models for animation / Generic animations).

Construction (same as hero_shared_blockout.py):
  - Bone names from data/art/rig.json
  - Each piece is a closed volume, 100% one vertex group
  - Pieces overlap at joints so there is no hole
  - Shell lives only on `head`. Arms never own a shell vert.
  - Rest pose = idle. CharacterMotion flexes locally on that bind until an
    authored verb take is ready.
  - Batting takes solve both hands to Fenn's own `bat` socket. Socket local -Y
    is the shared handle-to-barrel convention; no shared-rig Euler drives him.
  - Body and takes export from this one scene with the same FBX space settings.
  - FBX Generic, axis_forward=-Z, albedo sidecar (URP Lit)

  /opt/homebrew/bin/blender --background --python tools/blender/hero_fenn.py -- \
    --out unity/Assets/Art/Characters/fenn/fenn.fbx \
    --albedo unity/Assets/Art/Characters/fenn/fenn-albedo.png \
    --resources unity/Assets/Resources/Art/Characters/fenn
"""
from __future__ import annotations

import argparse
import math
import shutil
import sys
from pathlib import Path

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import batting_stance


BONES = [
    "root", "torso", "head",
    "lUpper", "lFore", "rUpper", "rFore",
    "lThigh", "lShin", "rThigh", "rShin",
    "bat", "glove",
]

TILE = {
    "shell": (0, 3), "scute": (1, 3), "flesh": (2, 3), "cream": (3, 3),
    "scarf": (0, 2), "wood": (1, 2), "white": (2, 2), "ink": (3, 2),
    "cheek": (0, 1), "gold": (1, 1), "beak": (2, 1),
}

RGB = {
    "shell": (0.42, 0.58, 0.36),
    "scute": (0.30, 0.44, 0.26),
    "flesh": (0.46, 0.66, 0.40),
    "cream": (0.91, 0.86, 0.72),
    "scarf": (0.62, 0.38, 0.24),
    "wood": (0.46, 0.28, 0.14),
    "white": (0.98, 0.98, 0.96),
    "ink": (0.08, 0.07, 0.07),
    "cheek": (0.90, 0.52, 0.40),
    "gold": (0.92, 0.72, 0.28),
    "beak": (0.86, 0.74, 0.42),
}


def nuke():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for coll in (bpy.data.meshes, bpy.data.armatures, bpy.data.materials, bpy.data.images, bpy.data.actions):
        for item in list(coll):
            coll.remove(item)


def mat(name, color):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (*color, 1.0)
        bsdf.inputs["Roughness"].default_value = 0.62
    return m


def mesh_prim(kind, name, loc, scale, material, rot=(0.0, 0.0, 0.0), seg=28):
    if kind == "uv_sphere":
        bpy.ops.mesh.primitive_uv_sphere_add(
            radius=0.5, location=loc, segments=seg, ring_count=max(14, seg // 2)
        )
    elif kind == "cylinder":
        bpy.ops.mesh.primitive_cylinder_add(radius=0.5, depth=1.0, location=loc, vertices=seg)
    else:
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc)
    ob = bpy.context.active_object
    ob.name = name
    ob.scale = scale
    ob.rotation_euler = rot
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    for p in ob.data.polygons:
        p.use_smooth = True
    ob.data.materials.append(material)
    return ob


def uv_tile(ob, key):
    col, row = TILE[key]
    me = ob.data
    if not me.uv_layers:
        me.uv_layers.new(name="UVMap")
    layer = me.uv_layers[0]
    pad = 0.04
    cu = (col + 0.5) / 4.0
    cv = (row + 0.5) / 4.0
    for loop in me.loops:
        layer.data[loop.index].uv = (cu, cv)


def add_bone(arm, name, head, tail, parent=None):
    b = arm.edit_bones.new(name)
    b.head = Vector(head)
    b.tail = Vector(tail)
    b.use_connect = False
    if parent is not None:
        b.parent = arm.edit_bones[parent]
    return b


def skin(ob, arm_ob, bone):
    """Vertex-group skin, not bone-parent. Unity imports SkinnedMeshRenderer."""
    ob.parent = arm_ob
    ob.parent_type = "OBJECT"
    vg = ob.vertex_groups.new(name=bone)
    vg.add(list(range(len(ob.data.vertices))), 1.0, "REPLACE")
    mod = ob.modifiers.new("Armature", "ARMATURE")
    mod.object = arm_ob
    mod.use_vertex_groups = True
    mod.use_deform_preserve_volume = False


def write_albedo(path: Path):
    size = 1024
    n = 4
    tile = size // n
    img = bpy.data.images.new("fenn-albedo", width=size, height=size, alpha=False)
    px = [0.04, 0.04, 0.05, 1.0] * (size * size)
    for key, (col, row) in TILE.items():
        r, g, b = RGB[key]
        x0, y0 = col * tile, row * tile
        for y in range(y0 + 6, y0 + tile - 6):
            for x in range(x0 + 6, x0 + tile - 6):
                i = (y * size + x) * 4
                px[i : i + 4] = [r, g, b, 1.0]
        if key == "shell":
            cr, cg, cb = RGB["scute"]
            for cy, cx in ((0.35, 0.35), (0.65, 0.38), (0.48, 0.68)):
                cxp = int(x0 + cx * tile)
                cyp = int(y0 + cy * tile)
                rad = tile // 7
                for y in range(cyp - rad, cyp + rad):
                    for x in range(cxp - rad, cxp + rad):
                        if 0 <= x < size and 0 <= y < size and (x - cxp) ** 2 + (y - cyp) ** 2 <= rad * rad:
                            i = (y * size + x) * 4
                            px[i : i + 4] = [cr, cg, cb, 1.0]
    img.pixels = px
    path.parent.mkdir(parents=True, exist_ok=True)
    img.filepath_raw = str(path)
    img.file_format = "PNG"
    img.save()
    print("albedo", path, path.stat().st_size)


def build_armature():
    arm_data = bpy.data.armatures.new("fenn-data")
    arm_ob = bpy.data.objects.new("fenn", arm_data)
    bpy.context.collection.objects.link(arm_ob)
    bpy.context.view_layer.objects.active = arm_ob
    bpy.ops.object.mode_set(mode="EDIT")

    # Blender Z-up, faces +Y. Standing idle. Arms hang so CharacterMotion X flexes.
    add_bone(arm_data, "root", (0, 0, 0), (0, 0, 0.22))
    add_bone(arm_data, "torso", (0, 0.10, 0.95), (0, 0.14, 1.85), "root")
    add_bone(arm_data, "head", (0, 0.38, 2.15), (0, 0.50, 3.20), "torso")
    add_bone(arm_data, "lUpper", (-0.90, 0.16, 1.70), (-1.05, 0.26, 1.12), "torso")
    add_bone(arm_data, "lFore", (-1.05, 0.26, 1.12), (-1.10, 0.38, 0.62), "lUpper")
    add_bone(arm_data, "rUpper", (0.90, 0.16, 1.70), (1.05, 0.26, 1.12), "torso")
    add_bone(arm_data, "rFore", (1.05, 0.26, 1.12), (1.10, 0.38, 0.62), "rUpper")
    add_bone(arm_data, "lThigh", (-0.48, 0.12, 0.88), (-0.52, 0.20, 0.46), "root")
    add_bone(arm_data, "lShin", (-0.52, 0.20, 0.46), (-0.54, 0.34, 0.12), "lThigh")
    add_bone(arm_data, "rThigh", (0.48, 0.12, 0.88), (0.52, 0.20, 0.46), "root")
    add_bone(arm_data, "rShin", (0.52, 0.20, 0.46), (0.54, 0.34, 0.12), "rThigh")
    add_bone(arm_data, "bat", (1.20, 0.48, 0.68), (1.20, 0.55, 0.05), "rFore")
    add_bone(arm_data, "glove", (-1.20, 0.48, 0.68), (-1.20, 0.55, 0.22), "lFore")

    bpy.ops.object.mode_set(mode="OBJECT")
    return arm_ob, arm_data


def build_scene():
    nuke()
    mats = {k: mat(k, RGB[k]) for k in RGB}
    arm_ob, arm_data = build_armature()
    pieces = []

    def add(kind, name, loc, scale, key, bone, rot=(0.0, 0.0, 0.0)):
        ob = mesh_prim(kind, name, loc, scale, mats[key], rot)
        uv_tile(ob, key)
        pieces.append((ob, bone))
        return ob

    # Cream body. Unique package is not Silhouette-squashed — author the cut here.
    add("uv_sphere", "Hip", (0.00, 0.10, 0.90), (1.55, 1.25, 0.95), "cream", "root")
    add("uv_sphere", "torsoMesh", (0.00, 0.22, 1.50), (1.38, 1.12, 1.22), "cream", "torso")
    add("uv_sphere", "Belly", (0.00, 0.62, 1.38), (1.12, 0.68, 0.95), "cream", "torso")
    add("uv_sphere", "Scarf", (0.00, 0.40, 1.98), (1.42, 1.12, 0.36), "scarf", "torso")
    add("uv_sphere", "Knot", (0.00, 0.98, 1.82), (0.38, 0.26, 0.22), "scarf", "torso")

    # Face in front of the shell-brim. All of this is 100% head.
    add("uv_sphere", "headMesh", (0.00, 0.62, 2.48), (1.72, 1.58, 1.58), "flesh", "head")
    add("uv_sphere", "Snout", (0.00, 1.28, 2.32), (0.98, 0.68, 0.64), "flesh", "head")
    add("uv_sphere", "Beak", (0.00, 1.56, 2.18), (0.48, 0.32, 0.26), "beak", "head")
    add("uv_sphere", "CheekL", (-0.55, 1.08, 2.32), (0.48, 0.38, 0.38), "cheek", "head")
    add("uv_sphere", "CheekR", (0.55, 1.08, 2.32), (0.48, 0.38, 0.38), "cheek", "head")
    add("uv_sphere", "WhiteL", (-0.34, 1.28, 2.62), (0.42, 0.15, 0.48), "white", "head")
    add("uv_sphere", "WhiteR", (0.34, 1.28, 2.62), (0.42, 0.15, 0.48), "white", "head")
    add("uv_sphere", "EyeL", (-0.34, 1.38, 2.72), (0.20, 0.10, 0.24), "ink", "head")
    add("uv_sphere", "EyeR", (0.34, 1.38, 2.72), (0.20, 0.10, 0.24), "ink", "head")
    add("cube", "BrowL", (-0.36, 1.22, 2.82), (0.34, 0.08, 0.08), "ink", "head", rot=(0, 0, math.radians(8)))
    add("cube", "BrowR", (0.36, 1.22, 2.82), (0.34, 0.08, 0.08), "ink", "head", rot=(0, 0, math.radians(-8)))
    add("uv_sphere", "Mouth", (0.00, 1.32, 2.10), (0.48, 0.12, 0.10), "ink", "head")

    add("uv_sphere", "shell", (0.00, -0.38, 2.68), (2.10, 1.65, 1.22), "shell", "head")
    add("uv_sphere", "shellBrim", (0.00, 0.12, 2.22), (2.10, 1.48, 0.28), "shell", "head")
    add("cylinder", "ScuteA", (0.00, -0.82, 3.10), (0.68, 0.68, 0.14), "scute", "head", rot=(math.radians(28), 0, 0))
    add("cylinder", "ScuteB", (-0.58, -0.58, 2.88), (0.46, 0.46, 0.12), "scute", "head", rot=(math.radians(30), 0, math.radians(-22)))
    add("cylinder", "ScuteC", (0.58, -0.58, 2.88), (0.46, 0.46, 0.12), "scute", "head", rot=(math.radians(30), 0, math.radians(22)))

    add("uv_sphere", "lUpperMesh", (-0.96, 0.20, 1.42), (0.62, 0.62, 0.90), "flesh", "lUpper")
    add("uv_sphere", "lForeMesh", (-1.08, 0.30, 0.86), (0.52, 0.52, 0.68), "flesh", "lFore")
    add("uv_sphere", "lHand", (-1.12, 0.42, 0.52), (0.48, 0.42, 0.36), "flesh", "lFore")
    add("uv_sphere", "rUpperMesh", (0.96, 0.20, 1.42), (0.62, 0.62, 0.90), "flesh", "rUpper")
    add("uv_sphere", "rForeMesh", (1.08, 0.30, 0.86), (0.52, 0.52, 0.68), "flesh", "rFore")
    add("uv_sphere", "rHand", (1.12, 0.42, 0.52), (0.48, 0.42, 0.36), "flesh", "rFore")

    add("uv_sphere", "lThighMesh", (-0.50, 0.16, 0.68), (0.68, 0.68, 0.68), "flesh", "lThigh")
    add("uv_sphere", "lShinMesh", (-0.54, 0.28, 0.30), (0.56, 0.56, 0.44), "flesh", "lShin")
    add("uv_sphere", "lFoot", (-0.54, 0.56, 0.12), (0.68, 0.95, 0.28), "cream", "lShin")
    add("uv_sphere", "rThighMesh", (0.50, 0.16, 0.68), (0.68, 0.68, 0.68), "flesh", "rThigh")
    add("uv_sphere", "rShinMesh", (0.54, 0.28, 0.30), (0.56, 0.56, 0.44), "flesh", "rShin")
    add("uv_sphere", "rFoot", (0.54, 0.56, 0.12), (0.68, 0.95, 0.28), "cream", "rShin")

    add("cylinder", "Cane", (1.20, 0.50, 0.30), (0.10, 0.10, 1.15), "wood", "bat")
    add("uv_sphere", "CaneKnob", (1.20, 0.50, 0.86), (0.18, 0.18, 0.18), "wood", "bat")
    add("uv_sphere", "CaneTip", (1.20, 0.50, -0.24), (0.13, 0.13, 0.13), "gold", "bat")

    bpy.context.view_layer.objects.active = arm_ob
    bpy.ops.object.mode_set(mode="OBJECT")
    for ob, bone in pieces:
        skin(ob, arm_ob, bone)

    missing = [n for n in BONES if n not in arm_data.bones]
    if missing:
        raise RuntimeError("missing bones: " + ",".join(missing))
    print("pieces", len(pieces))
    return arm_ob


def export_fbx(out: Path):
    out.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(
        filepath=str(out),
        use_selection=False,
        object_types={"ARMATURE", "MESH"},
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        bake_anim=False,
        armature_nodetype="NULL",
        primary_bone_axis="Y",
        secondary_bone_axis="X",
        axis_forward="-Z",
        axis_up="Y",
        apply_scale_options="FBX_SCALE_ALL",
        bake_space_transform=True,
        path_mode="AUTO",
    )
    print("exported", out, out.stat().st_size)


def clear_pose(arm_ob):
    bpy.context.view_layer.objects.active = arm_ob
    bpy.ops.object.mode_set(mode="POSE")
    for bone in arm_ob.pose.bones:
        bone.rotation_mode = "XYZ"
        bone.rotation_euler = (0.0, 0.0, 0.0)
        bone.location = (0.0, 0.0, 0.0)
        bone.scale = (1.0, 1.0, 1.0)
    bpy.context.view_layer.update()
    bpy.ops.object.mode_set(mode="OBJECT")


def make_action(arm_ob, name, keys):
    """Author one take on the same armature and rest basis as fenn.fbx."""
    action = bpy.data.actions.new(name)
    if arm_ob.animation_data is None:
        arm_ob.animation_data_create()
    arm_ob.animation_data.action = action
    clear_pose(arm_ob)
    bpy.context.view_layer.objects.active = arm_ob
    bpy.ops.object.mode_set(mode="POSE")
    for frame, bone_name, degrees in keys:
        bone = arm_ob.pose.bones.get(bone_name)
        if bone is None:
            raise RuntimeError("missing action bone: " + bone_name)
        bone.rotation_mode = "XYZ"
        bone.rotation_euler = tuple(math.radians(value) for value in degrees)
        bone.keyframe_insert(data_path="rotation_euler", frame=frame)
    bpy.ops.object.mode_set(mode="OBJECT")
    return action


def build_actions(arm_ob):
    idle = make_action(arm_ob, "idle", [
        (1, "torso", (0, 0, 0)),
        (1, "head", (0, 0, 0)),
        (13, "torso", (4, 0, 0)),
        (13, "head", (0, 6, 0)),
        (25, "torso", (0, 0, 0)),
        (25, "head", (0, 0, 0)),
    ])
    pose = make_action(arm_ob, "pose", [
        (1, "rUpper", (0, 0, 0)),
        (1, "rFore", (0, 0, 0)),
        (1, "torso", (0, 0, 0)),
        (10, "rUpper", (90, 0, -8)),
        (10, "rFore", (24, 0, 0)),
        (10, "torso", (-6, 8, 0)),
    ])
    return idle, pose


def export_take(path: Path, arm_ob, action, first_frame: int, last_frame: int):
    """Export a take without an FBX import/re-export round trip."""
    path.parent.mkdir(parents=True, exist_ok=True)
    arm_ob.animation_data.action = action
    scene = bpy.context.scene
    old_start, old_end, old_frame = scene.frame_start, scene.frame_end, scene.frame_current
    scene.frame_start = first_frame
    scene.frame_end = last_frame
    scene.frame_set(first_frame)
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(
        filepath=str(path),
        use_selection=False,
        object_types={"ARMATURE", "MESH"},
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        bake_anim=True,
        bake_anim_use_all_bones=True,
        bake_anim_use_all_actions=False,
        bake_anim_use_nla_strips=False,
        bake_anim_force_startend_keying=True,
        bake_anim_step=1.0,
        bake_anim_simplify_factor=0.0,
        armature_nodetype="NULL",
        primary_bone_axis="Y",
        secondary_bone_axis="X",
        axis_forward="-Z",
        axis_up="Y",
        apply_scale_options="FBX_SCALE_ALL",
        bake_space_transform=True,
        path_mode="AUTO",
    )
    scene.frame_start, scene.frame_end = old_start, old_end
    scene.frame_set(old_frame)
    print("exported take", action.name, path, path.stat().st_size)


def _target(name: str):
    ob = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(ob)
    ob.empty_display_type = "PLAIN_AXES"
    ob.empty_display_size = 0.14
    return ob


def _key_target(ob, frame: float, value):
    ob.location = value
    ob.keyframe_insert(data_path="location", frame=frame)


def make_batting_action(arm_ob, name: str, poses):
    """Drive Fenn's own rig to a two-hand grip and authored bat socket path.

    Each pose is (frame, stance_time, grip, handle_to_barrel, torso_z_degrees). IK exists
    only in this DCC scene; export_take bakes the evaluated Generic bone curves.
    The shipped runtime receives no constraint or shared-rig Euler dependency.
    """
    action = bpy.data.actions.new(name)
    if arm_ob.animation_data is None:
        arm_ob.animation_data_create()
    arm_ob.animation_data.action = action
    clear_pose(arm_ob)

    grip_target = _target(name + "-grip")
    barrel_target = _target(name + "-barrel")
    left_target = _target(name + "-left-hand")
    right_target = _target(name + "-right-hand")
    left_pole = _target(name + "-left-elbow")
    right_pole = _target(name + "-right-elbow")
    helpers = [grip_target, barrel_target, left_target, right_target, left_pole, right_pole]

    for frame, stance_time, grip_value, direction_value, torso_z in poses:
        bpy.context.scene.frame_set(frame)
        for bone_name in ("root", "torso", "head"):
            bone = arm_ob.pose.bones[bone_name]
            bone.rotation_mode = "XYZ"
            bone.rotation_euler = (0.0, 0.0, 0.0)
            bone.location = (0.0, 0.0, 0.0)
        grip = Vector(grip_value)
        direction = Vector(direction_value).normalized()
        # Hands stack up the handle from its authored grip origin. Their IK
        # targets are Fenn-local measurements, independent of Rio's arm axes.
        _key_target(grip_target, frame, grip)
        _key_target(barrel_target, frame, grip + direction * 3.0)
        # Fenn bats right, so the lead (left) hand holds the knob end and the
        # right hand stacks above it.
        _key_target(left_target, frame, grip + direction * 0.04)
        _key_target(right_target, frame, grip + direction * 0.20)
        _key_target(left_pole, frame, (-1.75, 0.02, 1.30))
        _key_target(right_pole, frame, (1.75, 0.02, 1.30))

        torso = arm_ob.pose.bones["torso"]
        torso.rotation_mode = "XYZ"
        torso.rotation_euler = (0.0, 0.0, math.radians(torso_z))
        head = arm_ob.pose.bones["head"]
        head.rotation_mode = "XYZ"
        head.rotation_euler = (0.0, 0.0, math.radians(-torso_z * 0.35))
        bpy.context.view_layer.update()
        batting_stance.author_visible_stance(
            arm_ob, stance_time,
            chest_front="Belly", chest_center="torsoMesh",
            eye_left="EyeL", eye_right="EyeR", head_center="headMesh",
            foot_left="lFoot", foot_right="rFoot",
            bats=batting_stance.BATS_RIGHT,
        )
        for bone_name in ("root", "torso", "head"):
            bone = arm_ob.pose.bones[bone_name]
            bone.rotation_mode = "QUATERNION"
            bone.keyframe_insert(data_path="rotation_quaternion", frame=frame)

    constraints = []
    for bone_name, target, pole in (
        ("lFore", left_target, left_pole),
        ("rFore", right_target, right_pole),
    ):
        ik = arm_ob.pose.bones[bone_name].constraints.new("IK")
        ik.name = name + "-two-hand-grip"
        ik.target = target
        ik.pole_target = pole
        ik.chain_count = 2
        ik.iterations = 64
        ik.use_tail = True
        constraints.append((arm_ob.pose.bones[bone_name], ik))

    bat = arm_ob.pose.bones["bat"]
    copy = bat.constraints.new("COPY_LOCATION")
    copy.name = name + "-grip-origin"
    copy.target = grip_target
    copy.target_space = "WORLD"
    copy.owner_space = "WORLD"
    constraints.append((bat, copy))
    track = bat.constraints.new("DAMPED_TRACK")
    track.name = name + "-barrel-axis"
    track.target = barrel_target
    # Package socket contract: local -Y runs from the grip toward the barrel.
    track.track_axis = "TRACK_NEGATIVE_Y"
    constraints.append((bat, track))
    return action, helpers, constraints


def export_batting_take(path: Path, arm_ob, name: str, poses, first_frame: int, last_frame: int):
    action, helpers, constraints = make_batting_action(arm_ob, name, poses)
    try:
        for frame, stance_time, *_ in poses:
            bpy.context.scene.frame_set(frame)
            bpy.context.view_layer.update()
            batting_stance.validate_visible_stance(
                stance_time,
                chest_front="Belly", chest_center="torsoMesh",
                eye_left="EyeL", eye_right="EyeR", head_center="headMesh",
                foot_left="lFoot", foot_right="rFoot",
                bats=batting_stance.BATS_RIGHT,
                arm_ob=arm_ob,
            )
        export_take(path, arm_ob, action, first_frame, last_frame)
    finally:
        for bone, constraint in constraints:
            bone.constraints.remove(constraint)
        for helper in helpers:
            bpy.data.objects.remove(helper, do_unlink=True)
        arm_ob.animation_data.action = None
        clear_pose(arm_ob)


def build(out: Path, albedo: Path, resources: Path | None = None):
    arm_ob = build_scene()
    write_albedo(albedo)
    export_fbx(out)
    idle, pose = build_actions(arm_ob)
    idle_path = out.with_name(out.stem + "-idle.fbx")
    pose_path = out.with_name(out.stem + "-pose.fbx")
    export_take(idle_path, arm_ob, idle, 1, 25)
    export_take(pose_path, arm_ob, pose, 1, 10)
    charge_swing_path = out.with_name(out.stem + "-chargeSwing.fbx")
    swing_path = out.with_name(out.stem + "-swing.fbx")
    scene = bpy.context.scene
    old_fps, old_fps_base = scene.render.fps, scene.render.fps_base
    scene.render.fps = 100
    scene.render.fps_base = 1.0
    # Blender faces +Y; its +Z becomes Unity +Y and +Y becomes Unity -Z. FBX
    # also reflects X, so these DCC directions pre-reflect the desired Unity
    # path. The socket still moves only through Fenn's own authored rig.
    export_batting_take(charge_swing_path, arm_ob, "chargeSwing", [
        (1, 0.00, (0.02, 0.55, 1.42), (0.18, -0.42, 0.89), 0),
        (101, 0.00, (0.20, 0.30, 1.62), (0.18, -0.42, 0.89), -8),
    ], 1, 101)
    export_batting_take(swing_path, arm_ob, "swing", [
        (1, 0.00, (0.20, 0.30, 1.62), (0.18, -0.42, 0.89), -8),
        (16, 0.15, (0.13, 0.50, 1.50), (0.10, 0.78, 0.62), -3),
        (25, 0.24, (0.08, 0.65, 1.42), (-0.4315, 0.9022, 0.005), 3),
        (31, 0.30, (0.06, 0.68, 1.40), (-0.7790, 0.6264, 0.0275), 7),
        (51, 0.50, (-0.05, 0.55, 1.52), (0.54, 0.78, 0.31), 2),
    ], 1, 51)
    scene.render.fps, scene.render.fps_base = old_fps, old_fps_base
    if resources is not None:
        resources.mkdir(parents=True, exist_ok=True)
        shutil.copy2(out, resources / out.name)
        shutil.copy2(albedo, resources / albedo.name)
        shutil.copy2(idle_path, resources / idle_path.name)
        shutil.copy2(pose_path, resources / pose_path.name)
        shutil.copy2(charge_swing_path, resources / charge_swing_path.name)
        shutil.copy2(swing_path, resources / swing_path.name)
        print("resources", resources)


def main(argv):
    p = argparse.ArgumentParser()
    p.add_argument("--out", required=True)
    p.add_argument("--albedo", required=True)
    p.add_argument("--resources", default="")
    args = p.parse_args(argv)
    res = Path(args.resources).resolve() if args.resources else None
    build(Path(args.out).resolve(), Path(args.albedo).resolve(), res)


if __name__ == "__main__":
    argv = sys.argv
    if "--" in argv:
        argv = argv[argv.index("--") + 1 :]
    else:
        argv = argv[1:]
    main(argv)
