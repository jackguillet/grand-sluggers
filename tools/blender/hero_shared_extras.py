#!/usr/bin/env python3
"""Captain extras as accessory meshes on hero-shared. One kit, not six skeletons.

Object names match data/art/skins.json extras. Play parents them to Head/Torso/Shin.
"""
from __future__ import annotations

import argparse
import math
import sys
from pathlib import Path

import bpy


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
        bsdf.inputs["Roughness"].default_value = 0.5
    return m


def prim(kind, name, loc, scale, material, rot=(0.0, 0.0, 0.0)):
    if kind == "uv_sphere":
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.5, location=loc, segments=20, ring_count=12)
    elif kind == "cylinder":
        bpy.ops.mesh.primitive_cylinder_add(radius=0.5, depth=1.0, location=loc, vertices=20)
    else:
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc)
    ob = bpy.context.active_object
    ob.name = name
    ob.scale = scale
    ob.rotation_euler = rot
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    ob.data.materials.append(material)
    return ob


def join(name, pieces):
    bpy.ops.object.select_all(action="DESELECT")
    for ob in pieces:
        ob.select_set(True)
    bpy.context.view_layer.objects.active = pieces[0]
    bpy.ops.object.join()
    pieces[0].name = name
    bpy.ops.object.origin_set(type="ORIGIN_GEOMETRY")
    pieces[0].location = (0.0, 0.0, 0.0)
    bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)
    return pieces[0]


def build():
    nuke()
    gold = mat("gold", (1.0, 0.80, 0.25))
    ice = mat("ice", (0.85, 0.95, 1.0))
    glass = mat("glass", (0.2, 0.85, 0.55))
    flesh = mat("flesh", (0.95, 0.79, 0.64))
    trim = mat("trim", (0.86, 0.19, 0.16))
    jersey = mat("jersey", (0.86, 0.19, 0.16))
    sash_c = mat("sash", (0.75, 0.92, 1.0))
    sneaker = mat("sneaker", (0.96, 0.96, 0.96))
    ember = mat("ember", (1.0, 0.45, 0.12))
    ink = mat("ink", (0.08, 0.07, 0.07))

    dome = prim("uv_sphere", "Dome", (0, 0, 0.22), (1.18, 1.18, 0.72), trim)
    disc = prim("cylinder", "BrimDisc", (0, 0.12, 0.0), (1.95, 1.65, 0.08), gold, rot=(math.radians(12), 0, 0))
    join("brim", [dome, disc])

    cl = prim("uv_sphere", "CheekL", (-0.48, 0.18, -0.08), (0.42, 0.42, 0.42), flesh)
    cr = prim("uv_sphere", "CheekR", (0.48, 0.18, -0.08), (0.42, 0.42, 0.42), flesh)
    join("cheeks", [cl, cr])

    shoe = prim("cube", "Shoe", (0, 0.18, 0.0), (0.72, 1.05, 0.42), sneaker)
    toe = prim("uv_sphere", "Toe", (0, 0.42, -0.02), (0.62, 0.55, 0.38), sneaker)
    stripe = prim("cube", "Stripe", (0, 0.12, 0.12), (0.4, 0.55, 0.12), trim)
    join("sneakers", [shoe, toe, stripe])

    sash = prim("cube", "sash", (0.08, 0.22, 0.0), (1.15, 0.1, 0.18), sash_c, rot=(0, 0, math.radians(18)))

    band = prim("cylinder", "Band", (0, 0, 0.0), (0.95, 0.95, 0.18), ice)
    p0 = prim("cube", "P0", (0, 0, 0.32), (0.16, 0.16, 0.42), ice)
    p1 = prim("cube", "P1", (0.28, 0, 0.26), (0.12, 0.12, 0.28), ice)
    p2 = prim("cube", "P2", (-0.28, 0, 0.26), (0.12, 0.12, 0.28), ice)
    join("crown", [band, p0, p1, p2])

    prim("cylinder", "neck", (0, 0, 0.0), (0.42, 0.42, 0.85), flesh)

    gl = prim("cylinder", "GogL", (-0.32, 0.22, 0.0), (0.58, 0.58, 0.14), glass, rot=(math.radians(90), 0, 0))
    gr = prim("cylinder", "GogR", (0.32, 0.22, 0.0), (0.58, 0.58, 0.14), glass, rot=(math.radians(90), 0, 0))
    br = prim("cube", "GogBridge", (0, 0.2, 0.0), (0.32, 0.12, 0.1), trim)
    join("goggles", [gl, gr, br])

    prim("cube", "cube-chest", (0, 0.2, 0.0), (1.65, 1.15, 1.15), jersey)
    prim("cube", "brick-jaw", (0, 0.18, -0.12), (1.22, 0.85, 0.48), flesh)

    sn = prim("uv_sphere", "SnoutBall", (0, 0.28, -0.08), (1.05, 0.95, 0.7), flesh)
    nl = prim("uv_sphere", "NostrilL", (-0.18, 0.48, 0.02), (0.18, 0.18, 0.18), ink)
    nr = prim("uv_sphere", "NostrilR", (0.18, 0.48, 0.02), (0.18, 0.18, 0.18), ink)
    join("snout", [sn, nl, nr])

    prim("uv_sphere", "belly", (0, 0.28, -0.1), (1.28, 1.12, 0.95), jersey)

    hl = prim("cylinder", "HornL", (-0.42, -0.05, 0.28), (0.28, 0.28, 0.85), trim, rot=(0, 0, math.radians(22)))
    hr = prim("cylinder", "HornR", (0.42, -0.05, 0.28), (0.28, 0.28, 0.85), trim, rot=(0, 0, math.radians(-22)))
    tl = prim("uv_sphere", "TipL", (-0.62, -0.05, 0.68), (0.32, 0.32, 0.32), trim)
    tr = prim("uv_sphere", "TipR", (0.62, -0.05, 0.68), (0.32, 0.32, 0.32), trim)
    join("horns", [hl, hr, tl, tr])

    cape = prim("cube", "Cape", (0, -0.35, -0.05), (1.5, 0.16, 1.55), trim)
    flare = prim("cube", "CapeFlare", (0, -0.42, -0.45), (1.7, 0.14, 0.55), trim)
    join("cape", [cape, flare])

    el = prim("uv_sphere", "EmberL", (-0.22, 0.18, 0.0), (0.28, 0.2, 0.28), ember)
    er = prim("uv_sphere", "EmberR", (0.22, 0.18, 0.0), (0.28, 0.2, 0.28), ember)
    join("ember-eyes", [el, er])

    for o in list(bpy.data.objects):
        if o.type != "MESH":
            continue
        bpy.ops.object.select_all(action="DESELECT")
        o.select_set(True)
        bpy.context.view_layer.objects.active = o
        bpy.ops.object.origin_set(type="ORIGIN_GEOMETRY")
        o.location = (0.0, 0.0, 0.0)
        bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)


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
