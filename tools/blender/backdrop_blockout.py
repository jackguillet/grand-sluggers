"""Rough backdrop blockouts for the parks (WD-03 A, WD-10 A; #1156).

Reads data/art/backdrops.json and, for each park row (or the ones named by --park), builds its shapes as plain primitives -
a box, a cone, a cylinder or a sphere at a bearing and distance from home, base on the ground, facing home - and exports them
as one FBX into the row's Assets slot and its Resources copy for the standalone player. The rows are the source: a new shape
is a row, not a Blender session. Rough is enough; this is a greybox for Jack's park sittings, not park art.

Unity is feet, +Z out to centre field, +X toward right. DCC -> Unity is (bx, by, bz) -> (-bx, bz, -by), the Harbor kit's
convention (tools/blender/README.md), so a Unity ground point (x, z) at height y is DCC (-x, -z, y).

    tools/blender-run.sh -b --python tools/blender/backdrop_blockout.py -- --repo . [--park coconut-cove] [--clay scratchpad/backdrops]
"""
from __future__ import annotations

import argparse
import json
import math
import re
import shutil
import sys
from pathlib import Path

import bpy


def read_jsonc(path: Path):
    text = path.read_text()
    return json.loads(re.sub(r"^\s*//.*$", "", text, flags=re.M))


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    for block in (bpy.data.meshes, bpy.data.materials):
        for item in list(block):
            block.remove(item)


def material(hex_color: str):
    name = "Mat" + hex_color
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    r, g, b = (int(hex_color[i:i + 2], 16) / 255.0 for i in (1, 3, 5))
    mat.diffuse_color = (r, g, b, 1.0)
    return mat


def add_shape(index: int, shape: dict):
    kind = shape["kind"]
    w, h, d = shape["sizeFt"]
    bearing = math.radians(shape["bearingDeg"])
    pitch = math.radians(shape.get("pitchDeg", 0.0))
    if kind == "box":
        bpy.ops.mesh.primitive_cube_add(size=1.0)
    elif kind == "cone":
        bpy.ops.mesh.primitive_cone_add(vertices=24, radius1=0.5, radius2=0.0, depth=1.0)
    elif kind == "cylinder":
        bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=0.5, depth=1.0)
    elif kind == "sphere":
        bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=12, radius=0.5)
    else:
        raise SystemExit(f"backdrop shape {index}: unknown kind {kind}")
    obj = bpy.context.active_object
    obj.name = f"Shape{index}"
    obj.scale = (w, d, h)
    # Tip it back about its width, then turn it to face home: the depth axis runs along the bearing.
    obj.rotation_mode = "XYZ"
    obj.rotation_euler = (pitch, 0.0, math.pi - bearing)
    # Base on the ground: the rotated shape's vertical extent, halved.
    up = abs(h * math.cos(pitch)) + abs(d * math.sin(pitch))
    x = shape["distanceFt"] * math.sin(bearing)
    z = shape["distanceFt"] * math.cos(bearing)
    obj.location = (-x, -z, up / 2.0)
    obj.data.materials.append(material(shape["color"]))
    return obj


def export_fbx(out: Path):
    out.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    bpy.ops.export_scene.fbx(
        filepath=str(out),
        use_selection=False,
        object_types={"MESH"},
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        bake_anim=False,
        axis_forward="-Z",
        axis_up="Y",
        apply_scale_options="FBX_SCALE_ALL",
        bake_space_transform=True,
        path_mode="AUTO",
    )


def clay(out_dir: Path, park: str):
    """An overhead workbench still of the blockout, home at the bottom, for the author's own look."""
    out_dir.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.color_type = "MATERIAL"
    scene.render.resolution_x, scene.render.resolution_y = 1200, 800
    # Home plate and the field's reach, for scale: a white disc at home, a grey ring at 300 ft (not exported).
    bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=12, depth=1, location=(0, 0, 0))
    bpy.context.active_object.data.materials.append(material("#FFFFFF"))
    bpy.ops.mesh.primitive_torus_add(major_radius=300, minor_radius=3, location=(0, 0, 0))
    bpy.context.active_object.data.materials.append(material("#808080"))
    cam_data = bpy.data.cameras.new("Clay")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = 1800
    cam_data.clip_end = 10000
    cam = bpy.data.objects.new("Clay", cam_data)
    scene.collection.objects.link(cam)
    cam.location = (0, -450, 1200)
    cam.rotation_euler = (0, 0, math.pi)
    scene.camera = cam
    scene.render.filepath = str(out_dir / f"{park}.png")
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    ap = argparse.ArgumentParser()
    ap.add_argument("--repo", default=".")
    ap.add_argument("--park", action="append")
    ap.add_argument("--clay")
    args = ap.parse_args(argv)
    repo = Path(args.repo).resolve()
    rows = read_jsonc(repo / "data/art/backdrops.json")["backdrops"]
    for row in rows:
        if args.park and row["park"] not in args.park:
            continue
        clear_scene()
        for i, shape in enumerate(row["shapes"]):
            add_shape(i, shape)
        slot = repo / "unity" / row["slot"]
        export_fbx(slot)
        player = repo / "unity/Assets/Resources" / (row["resources"] + ".fbx")
        player.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(slot, player)
        print(f"backdrop {row['park']}: {len(row['shapes'])} shapes -> {row['slot']} + Resources/{row['resources']}.fbx")
        if args.clay:
            clay(Path(args.clay), row["park"])


main()
