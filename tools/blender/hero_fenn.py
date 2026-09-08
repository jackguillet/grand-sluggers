#!/usr/bin/env python3
"""Elder Fenn as a cartoon Generic package.

Fat volumes, bone names from data/art/rig.json, 100% vertex groups per piece.
The posed PBR turtle GLB is a source, not the player mesh. bind=skinned.

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


BONES = [
    "root", "torso", "head",
    "lUpper", "lFore", "rUpper", "rFore",
    "lThigh", "lShin", "rThigh", "rShin",
    "bat", "glove",
]

# Atlas tiles (col, row) in a 4x4 grid. Row 0 is UV v=0 (image bottom).
TILE = {
    "shell": (0, 3),
    "scute": (1, 3),
    "flesh": (2, 3),
    "cream": (3, 3),
    "scarf": (0, 2),
    "wood": (1, 2),
    "white": (2, 2),
    "ink": (3, 2),
    "cheek": (0, 1),
    "gold": (1, 1),
    "beak": (2, 1),
    "pad": (3, 1),
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
    "pad": (0.55, 0.42, 0.28),
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
        spec = bsdf.inputs.get("Specular IOR Level") or bsdf.inputs.get("Specular")
        if spec:
            spec.default_value = 0.12
    return m


def mesh_prim(kind, name, loc, scale, material, rot=(0.0, 0.0, 0.0), seg=28):
    if kind == "uv_sphere":
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.5, location=loc, segments=seg, ring_count=max(14, seg // 2))
    elif kind == "cylinder":
        bpy.ops.mesh.primitive_cylinder_add(radius=0.5, depth=1.0, location=loc, vertices=seg)
    elif kind == "cone":
        bpy.ops.mesh.primitive_cone_add(radius1=0.5, depth=1.0, location=loc, vertices=seg)
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
    u0 = col / 4.0 + pad
    v0 = row / 4.0 + pad
    u1 = (col + 1) / 4.0 - pad
    v1 = (row + 1) / 4.0 - pad
    cu, cv = (u0 + u1) * 0.5, (v0 + v1) * 0.5
    # Solid swatch: every loop samples the tile center so PaintAuthored cannot hit the void.
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
    ob.parent = arm_ob
    ob.parent_type = "OBJECT"
    vg = ob.vertex_groups.new(name=bone)
    vg.add(list(range(len(ob.data.vertices))), 1.0, "REPLACE")
    mod = ob.modifiers.new("Armature", "ARMATURE")
    mod.object = arm_ob
    mod.use_vertex_groups = True
    mod.use_deform_preserve_volume = True


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
            # Darker scute discs inside the shell swatch so the dome is not a flat fill.
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

    # Blender Z-up. Faces +Y (Unity -Z after FBX axis_forward='-Z').
    # Short wide turtle. Feet on z=0. Unique packages do not get Silhouette squash.
    add_bone(arm_data, "root", (0, 0, 0), (0, 0, 0.22))
    add_bone(arm_data, "torso", (0, 0.12, 0.95), (0, 0.16, 1.88), "root")
    add_bone(arm_data, "head", (0, 0.42, 2.15), (0, 0.55, 3.25), "torso")
    add_bone(arm_data, "lUpper", (-0.92, 0.18, 1.72), (-1.08, 0.28, 1.12), "torso")
    add_bone(arm_data, "lFore", (-1.08, 0.28, 1.12), (-1.12, 0.40, 0.62), "lUpper")
    add_bone(arm_data, "rUpper", (0.92, 0.18, 1.72), (1.08, 0.28, 1.12), "torso")
    add_bone(arm_data, "rFore", (1.08, 0.28, 1.12), (1.12, 0.40, 0.62), "rUpper")
    add_bone(arm_data, "lThigh", (-0.48, 0.12, 0.88), (-0.52, 0.20, 0.46), "root")
    add_bone(arm_data, "lShin", (-0.52, 0.20, 0.46), (-0.54, 0.34, 0.12), "lThigh")
    add_bone(arm_data, "rThigh", (0.48, 0.12, 0.88), (0.52, 0.20, 0.46), "root")
    add_bone(arm_data, "rShin", (0.52, 0.20, 0.46), (0.54, 0.34, 0.12), "rThigh")
    add_bone(arm_data, "bat", (1.22, 0.48, 0.68), (1.22, 0.55, 0.02), "rFore")
    add_bone(arm_data, "glove", (-1.22, 0.48, 0.68), (-1.22, 0.55, 0.22), "lFore")

    bpy.ops.object.mode_set(mode="OBJECT")
    return arm_ob, arm_data


def build_scene():
    nuke()
    mats = {k: mat(k, RGB[k]) for k in RGB}

    arm_ob, arm_data = build_armature()
    pieces = []

    def add(kind, name, loc, scale, key, bone, rot=(0.0, 0.0, 0.0), seg=28):
        ob = mesh_prim(kind, name, loc, scale, mats[key], rot, seg)
        uv_tile(ob, key)
        pieces.append((ob, bone))
        return ob

    # Cream plastron is the body. Face sits in front of the shell-brim.
    add("uv_sphere", "Hip", (0.00, 0.10, 0.90), (1.58, 1.28, 0.98), "cream", "root")
    add("uv_sphere", "torsoMesh", (0.00, 0.22, 1.52), (1.42, 1.15, 1.28), "cream", "torso")
    add("uv_sphere", "Belly", (0.00, 0.62, 1.38), (1.18, 0.70, 1.00), "cream", "torso")

    add("uv_sphere", "headMesh", (0.00, 0.62, 2.48), (1.78, 1.62, 1.62), "flesh", "head")
    add("uv_sphere", "Snout", (0.00, 1.28, 2.32), (1.02, 0.72, 0.68), "flesh", "head")
    add("uv_sphere", "Beak", (0.00, 1.58, 2.18), (0.52, 0.34, 0.28), "beak", "head")
    add("uv_sphere", "CheekL", (-0.58, 1.08, 2.32), (0.50, 0.40, 0.40), "cheek", "head")
    add("uv_sphere", "CheekR", (0.58, 1.08, 2.32), (0.50, 0.40, 0.40), "cheek", "head")
    add("uv_sphere", "WhiteL", (-0.36, 1.28, 2.62), (0.44, 0.16, 0.50), "white", "head")
    add("uv_sphere", "WhiteR", (0.36, 1.28, 2.62), (0.44, 0.16, 0.50), "white", "head")
    add("uv_sphere", "EyeL", (-0.36, 1.38, 2.72), (0.22, 0.10, 0.26), "ink", "head")
    add("uv_sphere", "EyeR", (0.36, 1.38, 2.72), (0.22, 0.10, 0.26), "ink", "head")
    add("cube", "BrowL", (-0.38, 1.24, 2.82), (0.36, 0.08, 0.08), "ink", "head", rot=(0, 0, math.radians(8)))
    add("cube", "BrowR", (0.38, 1.24, 2.82), (0.36, 0.08, 0.08), "ink", "head", rot=(0, 0, math.radians(-8)))
    add("uv_sphere", "Mouth", (0.00, 1.34, 2.10), (0.52, 0.12, 0.10), "ink", "head")

    # Shell-as-brim sits on the BACK of the head so the face is the picture.
    add("uv_sphere", "shell", (0.00, -0.38, 2.68), (2.20, 1.72, 1.28), "shell", "head")
    add("uv_sphere", "shellBrim", (0.00, 0.18, 2.22), (2.20, 1.55, 0.28), "shell", "head")
    add("cylinder", "ScuteA", (0.00, -0.85, 3.12), (0.72, 0.72, 0.16), "scute", "head", rot=(math.radians(28), 0, 0))
    add("cylinder", "ScuteB", (-0.62, -0.62, 2.88), (0.48, 0.48, 0.12), "scute", "head", rot=(math.radians(30), 0, math.radians(-22)))
    add("cylinder", "ScuteC", (0.62, -0.62, 2.88), (0.48, 0.48, 0.12), "scute", "head", rot=(math.radians(30), 0, math.radians(22)))

    add("uv_sphere", "Scarf", (0.00, 0.42, 1.98), (1.48, 1.18, 0.38), "scarf", "torso")
    add("uv_sphere", "Knot", (0.00, 1.02, 1.82), (0.40, 0.28, 0.24), "scarf", "torso")

    add("uv_sphere", "lUpperMesh", (-0.98, 0.22, 1.42), (0.64, 0.64, 0.92), "flesh", "lUpper")
    add("uv_sphere", "lForeMesh", (-1.10, 0.32, 0.86), (0.54, 0.54, 0.70), "flesh", "lFore")
    add("uv_sphere", "lHand", (-1.14, 0.44, 0.52), (0.50, 0.44, 0.38), "flesh", "lFore")
    add("uv_sphere", "rUpperMesh", (0.98, 0.22, 1.42), (0.64, 0.64, 0.92), "flesh", "rUpper")
    add("uv_sphere", "rForeMesh", (1.10, 0.32, 0.86), (0.54, 0.54, 0.70), "flesh", "rFore")
    add("uv_sphere", "rHand", (1.14, 0.44, 0.52), (0.50, 0.44, 0.38), "flesh", "rFore")

    add("uv_sphere", "lThighMesh", (-0.50, 0.16, 0.68), (0.70, 0.70, 0.70), "flesh", "lThigh")
    add("uv_sphere", "lShinMesh", (-0.54, 0.28, 0.30), (0.58, 0.58, 0.46), "flesh", "lShin")
    add("uv_sphere", "lFoot", (-0.54, 0.58, 0.12), (0.70, 0.98, 0.30), "cream", "lShin")
    add("uv_sphere", "lToe", (-0.54, 0.92, 0.10), (0.40, 0.36, 0.20), "cream", "lShin")
    add("uv_sphere", "rThighMesh", (0.50, 0.16, 0.68), (0.70, 0.70, 0.70), "flesh", "rThigh")
    add("uv_sphere", "rShinMesh", (0.54, 0.28, 0.30), (0.58, 0.58, 0.46), "flesh", "rShin")
    add("uv_sphere", "rFoot", (0.54, 0.58, 0.12), (0.70, 0.98, 0.30), "cream", "rShin")
    add("uv_sphere", "rToe", (0.54, 0.92, 0.10), (0.40, 0.36, 0.20), "cream", "rShin")

    add("cylinder", "Cane", (1.22, 0.50, 0.28), (0.11, 0.11, 1.22), "wood", "bat")
    add("uv_sphere", "CaneKnob", (1.22, 0.50, 0.88), (0.20, 0.20, 0.20), "wood", "bat")
    add("uv_sphere", "CaneTip", (1.22, 0.50, -0.28), (0.14, 0.14, 0.14), "gold", "bat")

    bpy.context.view_layer.objects.active = arm_ob
    bpy.ops.object.mode_set(mode="OBJECT")
    for ob, bone in pieces:
        skin(ob, arm_ob, bone)

    missing = [n for n in BONES if n not in arm_data.bones]
    if missing:
        raise RuntimeError("missing bones: " + ",".join(missing))

    zs = []
    for ob, _ in pieces:
        for v in ob.data.vertices:
            zs.append((ob.matrix_world @ v.co).z)
    print("pieces", len(pieces), "z", round(min(zs), 3), round(max(zs), 3))
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


def pose_limb(arm_ob, bone="rUpper", euler=(math.radians(90), 0.0, math.radians(-8))):
    bpy.context.view_layer.objects.active = arm_ob
    bpy.ops.object.mode_set(mode="POSE")
    b = arm_ob.pose.bones.get(bone)
    if b is None:
        raise RuntimeError("no bone " + bone)
    b.rotation_mode = "XYZ"
    b.rotation_euler = euler
    bpy.context.view_layer.update()
    bpy.ops.object.mode_set(mode="OBJECT")


def clear_pose(arm_ob):
    bpy.context.view_layer.objects.active = arm_ob
    bpy.ops.object.mode_set(mode="POSE")
    for b in arm_ob.pose.bones:
        b.rotation_mode = "XYZ"
        b.rotation_euler = (0.0, 0.0, 0.0)
        b.location = (0.0, 0.0, 0.0)
    bpy.ops.object.mode_set(mode="OBJECT")


def build(out: Path, albedo: Path, resources: Path | None = None):
    build_scene()
    write_albedo(albedo)
    export_fbx(out)
    if resources is not None:
        resources.mkdir(parents=True, exist_ok=True)
        shutil.copy2(out, resources / out.name)
        shutil.copy2(albedo, resources / albedo.name)
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
