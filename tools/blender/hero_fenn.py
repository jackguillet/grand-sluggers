#!/usr/bin/env python3
"""Skin Elder Fenn to the shared hero armature and export Unity FBX + portrait.

Source GLB has no skeleton. Bones match data/art/rig.json / hero_shared_blockout.py.
Do not add a second skeleton — this is a unique mesh on the shared chain.
"""
from __future__ import annotations

import argparse
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector


BONES = [
    "root",
    "torso",
    "head",
    "lUpper",
    "lFore",
    "rUpper",
    "rFore",
    "lThigh",
    "lShin",
    "rThigh",
    "rShin",
    "bat",
    "glove",
]

TARGET_HEIGHT = 4.40


def nuke():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for coll in (
        bpy.data.meshes,
        bpy.data.armatures,
        bpy.data.materials,
        bpy.data.images,
        bpy.data.cameras,
        bpy.data.lights,
        bpy.data.actions,
    ):
        for item in list(coll):
            coll.remove(item)


def world_bounds(obs):
    mins = Vector((1e9, 1e9, 1e9))
    maxs = Vector((-1e9, -1e9, -1e9))
    for ob in obs:
        if ob.type != "MESH":
            continue
        for corner in ob.bound_box:
            w = ob.matrix_world @ Vector(corner)
            mins.x, mins.y, mins.z = min(mins.x, w.x), min(mins.y, w.y), min(mins.z, w.z)
            maxs.x, maxs.y, maxs.z = max(maxs.x, w.x), max(maxs.y, w.y), max(maxs.z, w.z)
    return mins, maxs


def join_meshes():
    meshes = [ob for ob in bpy.data.objects if ob.type == "MESH"]
    if not meshes:
        raise RuntimeError("no mesh after import")
    bpy.ops.object.select_all(action="DESELECT")
    for ob in meshes:
        ob.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    if len(meshes) > 1:
        bpy.ops.object.join()
    body = bpy.context.view_layer.objects.active
    body.name = "fenn"
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    return body


def stand_on_origin(body):
    """Feet on Z=0, facing +Y, height TARGET_HEIGHT. Shared rig is Z-up in Blender."""
    bpy.ops.object.select_all(action="DESELECT")
    body.select_set(True)
    bpy.context.view_layer.objects.active = body

    mins, maxs = world_bounds([body])
    size = maxs - mins
    up_axis = max(range(3), key=lambda i: size[i])
    if up_axis == 1:
        body.rotation_euler = (math.radians(90), 0, 0)
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
        mins, maxs = world_bounds([body])
        size = maxs - mins

    height = max(size.z, 1e-4)
    s = TARGET_HEIGHT / height
    body.scale = (s, s, s)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    mins, maxs = world_bounds([body])
    body.location -= Vector(((mins.x + maxs.x) * 0.5, (mins.y + maxs.y) * 0.5, mins.z))
    bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)


def decimate(body, target_faces=28000):
    faces = len(body.data.polygons)
    if faces <= target_faces:
        return faces
    mod = body.modifiers.new("Decimate", "DECIMATE")
    mod.ratio = max(0.02, target_faces / float(faces))
    bpy.context.view_layer.objects.active = body
    bpy.ops.object.modifier_apply(modifier=mod.name)
    return len(body.data.polygons)


def add_bone(arm, name, head, tail, parent=None):
    b = arm.edit_bones.new(name)
    b.head = Vector(head)
    b.tail = Vector(tail)
    b.use_connect = False
    if parent is not None:
        b.parent = arm.edit_bones[parent]
    return b


def build_armature():
    arm_data = bpy.data.armatures.new("hero-shared-data")
    arm_ob = bpy.data.objects.new("hero-shared", arm_data)
    bpy.context.collection.objects.link(arm_ob)
    bpy.context.view_layer.objects.active = arm_ob
    bpy.ops.object.mode_set(mode="EDIT")
    add_bone(arm_data, "root", (0, 0, 0), (0, 0, 0.25))
    add_bone(arm_data, "torso", (0, 0, 1.15), (0, 0, 2.55), "root")
    add_bone(arm_data, "head", (0, 0.05, 3.35), (0, 0.05, 4.55), "torso")
    add_bone(arm_data, "lUpper", (-0.95, 0, 2.45), (-0.95, 0, 1.55), "torso")
    add_bone(arm_data, "lFore", (-0.95, 0, 1.55), (-0.95, 0, 0.85), "lUpper")
    add_bone(arm_data, "rUpper", (0.95, 0, 2.45), (0.95, 0, 1.55), "torso")
    add_bone(arm_data, "rFore", (0.95, 0, 1.55), (0.95, 0, 0.85), "rUpper")
    add_bone(arm_data, "lThigh", (-0.42, 0, 1.05), (-0.42, 0, 0.45), "root")
    add_bone(arm_data, "lShin", (-0.42, 0, 0.45), (-0.42, 0, 0.08), "lThigh")
    add_bone(arm_data, "rThigh", (0.42, 0, 1.05), (0.42, 0, 0.45), "root")
    add_bone(arm_data, "rShin", (0.42, 0, 0.45), (0.42, 0, 0.08), "rThigh")
    add_bone(arm_data, "bat", (1.15, 0.15, 0.90), (1.15, 0.15, 0.20), "rFore")
    add_bone(arm_data, "glove", (-1.15, 0.15, 0.90), (-1.15, 0.15, 0.20), "lFore")
    bpy.ops.object.mode_set(mode="OBJECT")
    missing = [n for n in BONES if n not in arm_data.bones]
    if missing:
        raise RuntimeError("missing bones: " + ",".join(missing))
    return arm_ob


def skin(body, arm_ob):
    bpy.ops.object.select_all(action="DESELECT")
    body.select_set(True)
    arm_ob.select_set(True)
    bpy.context.view_layer.objects.active = arm_ob
    bpy.ops.object.parent_set(type="ARMATURE_AUTO")
    body.parent = arm_ob
    body.parent_type = "OBJECT"


def save_textures(tex_dir: Path):
    tex_dir.mkdir(parents=True, exist_ok=True)
    saved = []
    for img in bpy.data.images:
        if not img.has_data or img.size[0] < 4:
            continue
        name = (img.name or "fenn").split(".")[0]
        name = "".join(ch if ch.isalnum() or ch in "-_" else "-" for ch in name)
        out = tex_dir / f"{name}.png"
        img.filepath_raw = str(out)
        img.file_format = "PNG"
        try:
            img.save()
            saved.append(out)
        except Exception as exc:
            print("texture skip", img.name, exc)
    return saved


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
        path_mode="COPY",
        embed_textures=True,
    )


def render_portrait(path: Path):
    cam_data = bpy.data.cameras.new("PortraitCam")
    cam = bpy.data.objects.new("PortraitCam", cam_data)
    bpy.context.scene.collection.objects.link(cam)
    bpy.context.scene.camera = cam
    cam.location = (0.0, -7.6, 2.35)
    direction = Vector((0.0, 0.0, 2.1)) - cam.location
    cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    cam_data.lens = 50

    key = bpy.data.lights.new("Key", "AREA")
    key.energy = 550
    key.size = 5
    key_ob = bpy.data.objects.new("Key", key)
    bpy.context.scene.collection.objects.link(key_ob)
    key_ob.location = (2.8, -3.2, 5.2)

    fill = bpy.data.lights.new("Fill", "AREA")
    fill.energy = 160
    fill.size = 7
    fill_ob = bpy.data.objects.new("Fill", fill)
    bpy.context.scene.collection.objects.link(fill_ob)
    fill_ob.location = (-3.4, 1.6, 2.8)

    world = bpy.context.scene.world or bpy.data.worlds.new("World")
    bpy.context.scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    if bg:
        bg.inputs[0].default_value = (0.78, 0.82, 0.86, 1)
        bg.inputs[1].default_value = 1.0

    scene = bpy.context.scene
    try:
        scene.render.engine = "BLENDER_EEVEE_NEXT"
    except Exception:
        scene.render.engine = "CYCLES"
        scene.cycles.samples = 24
    scene.render.resolution_x = 1024
    scene.render.resolution_y = 1024
    scene.render.filepath = str(path)
    scene.render.film_transparent = False
    scene.render.image_settings.file_format = "JPEG"
    scene.render.image_settings.quality = 92
    bpy.ops.render.render(write_still=True)


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--src", required=True)
    p.add_argument("--out", required=True)
    p.add_argument("--portrait", default="")
    p.add_argument("--faces", type=int, default=28000)
    argv = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else sys.argv[1:]
    args = p.parse_args(argv)

    src = Path(args.src)
    out = Path(args.out)
    if not src.is_file():
        raise SystemExit("missing source " + str(src))

    nuke()
    bpy.ops.import_scene.gltf(filepath=str(src))
    body = join_meshes()
    stand_on_origin(body)
    faces = decimate(body, args.faces)
    print("faces after decimate", faces, "verts", len(body.data.vertices))
    arm = build_armature()
    skin(body, arm)
    save_textures(out.parent)
    export_fbx(out)
    if args.portrait:
        render_portrait(Path(args.portrait))
    print("WROTE", out)


if __name__ == "__main__":
    main()
