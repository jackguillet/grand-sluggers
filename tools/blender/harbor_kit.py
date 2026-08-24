#!/usr/bin/env python3
"""Harbor kit meshes: dugout, wall panel, crowd. Bind in HarborKit; missing file keeps primitives.

Unity Generic, axis_forward -Z, axis_up Y. Names: dugout-1b, dugout-3b, wall-panel, fan-stand, fan-sit.
"""
from __future__ import annotations

import argparse
import math
import sys
from pathlib import Path

import bpy


HALF_ALONG = 6.2
HALF_DEEP = 4.6


def nuke():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for block in (bpy.data.meshes, bpy.data.armatures, bpy.data.materials, bpy.data.curves):
        for item in list(block):
            block.remove(item)


def mat(name, color):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (*color, 1.0)
        bsdf.inputs["Roughness"].default_value = 0.55
    return m


def prim(kind, name, loc, scale, material, rot=(0.0, 0.0, 0.0)):
    if kind == "uv_sphere":
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.5, location=loc, segments=18, ring_count=12)
    elif kind == "cylinder":
        bpy.ops.mesh.primitive_cylinder_add(radius=0.5, depth=1.0, location=loc, vertices=16)
    else:
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc)
    ob = bpy.context.active_object
    ob.name = name
    ob.scale = scale
    ob.rotation_euler = rot
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    ob.data.materials.append(material)
    return ob


def join_in_place(name, pieces):
    bpy.ops.object.select_all(action="DESELECT")
    for ob in pieces:
        ob.select_set(True)
    bpy.context.view_layer.objects.active = pieces[0]
    bpy.ops.object.join()
    pieces[0].name = name
    return pieces[0]


def bevel(ob, width=0.06, segs=2):
    bpy.ops.object.select_all(action="DESELECT")
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    mod = ob.modifiers.new("bev", "BEVEL")
    mod.width = width
    mod.segments = segs
    mod.limit_method = "NONE"
    bpy.ops.object.modifier_apply(modifier="bev")


def build_dugout(name, wood, roof, gold, pad, post, flip_x):
    field_x = -HALF_DEEP
    back_x = HALF_DEEP
    y0 = -HALF_ALONG
    y1 = HALF_ALONG
    pieces = [
        prim("cube", name + "Pad", (0, 0, 0.10), (HALF_DEEP * 2 + 1.2, HALF_ALONG * 2 + 0.8, 0.20), pad),
        prim("cylinder", name + "PostFH", (field_x, y0 + 0.45, 2.02), (0.52, 0.52, 4.05), post),
        prim("cylinder", name + "PostFF", (field_x, y1 - 0.45, 2.02), (0.52, 0.52, 4.05), post),
        prim("cylinder", name + "PostBH", (back_x, y0 + 0.45, 2.10), (0.52, 0.52, 4.20), post),
        prim("cylinder", name + "PostBF", (back_x, y1 - 0.45, 2.10), (0.52, 0.52, 4.20), post),
        prim("cube", name + "Back", (back_x, 0, 2.05), (0.42, HALF_ALONG * 2 - 0.5, 4.0), wood),
        prim("cube", name + "End", (0, y1, 2.0), (HALF_DEEP * 2 - 0.2, 0.38, 3.9), wood),
        prim("cube", name + "Roof", (0, 0, 4.32), (HALF_DEEP * 2 + 1.8, HALF_ALONG * 2 + 1.4, 0.28), roof),
        prim("cube", name + "Ridge", (0, 0, 4.52), (1.1, HALF_ALONG * 2 + 0.6, 0.22), roof),
        prim("cube", name + "Fascia", (field_x, 0, 4.18), (0.34, HALF_ALONG * 2 + 0.5, 0.30), gold),
        prim("cube", name + "Rail", (field_x, 0, 1.02), (0.24, HALF_ALONG * 2 - 0.9, 0.32), gold),
        prim("cube", name + "Bench", (back_x - 1.55, 0, 0.88), (1.5, HALF_ALONG * 2 - 2.0, 0.24), wood),
        prim("cylinder", name + "LegH", (back_x - 1.55, y0 + 1.6, 0.39), (0.28, 0.28, 0.78), post),
        prim("cylinder", name + "LegF", (back_x - 1.55, y1 - 1.6, 0.39), (0.28, 0.28, 0.78), post),
    ]
    bevel(pieces[7], 0.08, 2)
    dug = join_in_place(name, pieces)
    if flip_x:
        bpy.ops.object.select_all(action="DESELECT")
        dug.select_set(True)
        bpy.context.view_layer.objects.active = dug
        dug.scale[0] = -1.0
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        bpy.ops.object.mode_set(mode="EDIT")
        bpy.ops.mesh.select_all(action="SELECT")
        bpy.ops.mesh.flip_normals()
        bpy.ops.object.mode_set(mode="OBJECT")
    bpy.ops.object.select_all(action="DESELECT")
    dug.select_set(True)
    bpy.context.view_layer.objects.active = dug
    bpy.context.scene.cursor.location = (0.0, 0.0, 0.0)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    dug.location = (0.0, 0.0, 0.0)
    return dug


def build_wall(pad, cap):
    body = prim("cube", "WallBody", (0, 0, 4.4), (17.2, 1.45, 8.8), pad)
    lip = prim("cube", "WallCap", (0, 0.12, 8.92), (17.2, 1.85, 0.32), cap)
    bevel(body, 0.10, 2)
    wall = join_in_place("wall-panel", [body, lip])
    bpy.context.scene.cursor.location = (0.0, 0.0, 0.0)
    bpy.ops.object.select_all(action="DESELECT")
    wall.select_set(True)
    bpy.context.view_layer.objects.active = wall
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    wall.location = (0.0, 0.0, 0.0)
    return wall


def build_fan(name, sit, jersey, flesh, cap):
    if sit:
        body = prim("cylinder", name + "Body", (0, 0, 0.78), (0.72, 0.72, 1.20), jersey)
        head = prim("uv_sphere", name + "Head", (0, 0.04, 1.48), (0.64, 0.64, 0.64), flesh)
        visor = prim("cube", name + "Cap", (0, 0.18, 1.62), (0.55, 0.42, 0.08), cap)
    else:
        body = prim("cylinder", name + "Body", (0, 0, 0.92), (0.70, 0.70, 1.55), jersey)
        head = prim("uv_sphere", name + "Head", (0, 0.04, 1.78), (0.68, 0.68, 0.68), flesh)
        visor = prim("cube", name + "Cap", (0, 0.20, 1.92), (0.58, 0.44, 0.08), cap)
    fan = join_in_place(name, [body, head, visor])
    bpy.ops.object.select_all(action="DESELECT")
    fan.select_set(True)
    bpy.context.view_layer.objects.active = fan
    bpy.context.scene.cursor.location = (0.0, 0.0, 0.0)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    fan.location = (0.0, 0.0, 0.0)
    return fan


def build():
    nuke()
    wood = mat("wood", (0.42, 0.26, 0.12))
    roof = mat("roof", (0.14, 0.32, 0.20))
    gold = mat("gold", (1.0, 0.80, 0.25))
    pad = mat("pad", (0.16, 0.42, 0.28))
    post = mat("post", (0.28, 0.22, 0.16))
    jersey = mat("jersey", (0.86, 0.19, 0.16))
    flesh = mat("flesh", (1.0, 0.80, 0.68))
    cap = mat("cap", (1.0, 0.80, 0.25))
    build_dugout("dugout-1b", wood, roof, gold, pad, post, flip_x=False)
    build_dugout("dugout-3b", wood, roof, gold, pad, post, flip_x=True)
    build_wall(pad, gold)
    build_fan("fan-stand", sit=False, jersey=jersey, flesh=flesh, cap=cap)
    build_fan("fan-sit", sit=True, jersey=jersey, flesh=flesh, cap=cap)


def export_fbx(out: Path):
    out.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="SELECT")
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
    names = sorted(o.name for o in bpy.data.objects if o.type == "MESH")
    print("exported", out, "meshes", ",".join(names))


def main(argv):
    p = argparse.ArgumentParser()
    p.add_argument("--out", required=True)
    args = p.parse_args(argv)
    build()
    export_fbx(Path(args.out).resolve())


if __name__ == "__main__":
    argv = sys.argv
    if "--" in argv:
        argv = argv[argv.index("--") + 1 :]
    else:
        argv = argv[1:]
    main(argv)
