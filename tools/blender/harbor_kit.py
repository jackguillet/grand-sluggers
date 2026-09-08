#!/usr/bin/env python3
"""Harbor kit meshes: dugout, wall, crowd, home plate, bag. Bind in HarborKit; missing file keeps primitives.

Unity Generic, axis_forward -Z, axis_up Y.
Names: dugout-1b, dugout-3b, wall-panel, fan-stand, fan-sit, home-plate, bag.
"""
from __future__ import annotations

import argparse
import math
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix


# Keep in sync with src/GrandSluggers.Sim/HarborDugout.cs
HALF_ALONG = 32.0
HALF_DEEP = 5.2
PIT = 3.2
STAIR_COUNT = 4
STAIR_DEPTH = 0.70
FIELD_STAIR_RUN = 1.2
# Keep in sync with HarborInfield.BagSize / HomeSet.PlateW / ParkDiamond (feet).
BAG_SIZE = 4.0
# Keep in sync with HarborWall / HarborPostcard / HarborDugout.
LEFT_FENCE = 330.0
CENTER_FENCE = 400.0
RIGHT_FENCE = 330.0
FOUL_DEG = 45.0
HOME_RADIUS = 34.0
WRAP_SEGS = 120
WALL_H = 26.0
WALL_THICK = 3.4
DUGOUT_X = 70.0
DUGOUT_Z = 40.0
DUGOUT_PAD = 14.0
DUGOUT_CLEAR_X = DUGOUT_X + HALF_DEEP + DUGOUT_PAD
DUGOUT_CLEAR_Z = DUGOUT_Z + HALF_ALONG + 10.0
DUGOUT_R = math.hypot(DUGOUT_CLEAR_X, DUGOUT_CLEAR_Z)
DUGOUT_SPRAY = math.degrees(math.atan2(DUGOUT_CLEAR_X, DUGOUT_CLEAR_Z))
# OBR 2.02: 17″ front, 8½″ shoulders, point at origin (catcher).
PLATE_HALF_W = 17.0 / 12.0 / 2.0
PLATE_FRONT = 17.0 / 12.0
PLATE_SHOULDER = 8.5 / 12.0
PATH_WIDTH = 8.0
PATH_CORNER = 14.0
PATH_Y = 0.26
PATH_THICK = 0.24
HOME_PACKED_R = 16.0
MOUND_R = 9.2
MOUND_H = 0.98
MOUND_TABLE_R = 2.2


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


def stairs(name, conc, y_lip, sign):
    """Steps down from field into the pit at one end."""
    step_h = PIT / STAIR_COUNT
    pieces = []
    for i in range(STAIR_COUNT):
        z_c = -step_h * (i + 0.5)
        y_c = y_lip + sign * (0.15 + i * STAIR_DEPTH)
        pieces.append(
            prim("cube", name + "S" + str(i), (0, y_c, z_c),
                 (HALF_DEEP * 2 - 1.0, STAIR_DEPTH + 0.04, step_h), conc)
        )
    return pieces


def build_dugout(name, wood, roof, gold, pad, post, conc, well, mesh_mat, flip_x):
    """MLB pit: rail is the hip wall (−X), pit behind toward +X, roof over the bench.
    +Y is home after Unity yaw. Stairs and mesh gate at the home end."""
    field_x = -HALF_DEEP
    back_x = HALF_DEEP
    y_bag = -HALF_ALONG
    y_home = HALF_ALONG
    along = HALF_ALONG * 2
    deep = HALF_DEEP * 2
    rail_y = 4.2
    roof_y = rail_y + 3.0
    gate = 5.2
    mesh_y1 = y_home - gate
    mesh_along = along - gate - 0.4
    mesh_mid = (y_bag + mesh_y1) * 0.5
    pieces = [
        prim("cube", name + "Floor", (0.2, 0, -PIT), (deep + 0.5, along + 0.5, 0.22), pad),
        prim("cube", name + "BackWall", (back_x, 0, (-PIT + roof_y) * 0.5),
             (0.38, along + 0.2, roof_y + PIT), well),
        prim("cube", name + "EndBag", (0, y_bag, (-PIT + roof_y) * 0.5),
             (deep + 0.2, 0.34, roof_y + PIT), well),
        prim("cube", name + "EndHome", (0, y_home, (-PIT + rail_y) * 0.5),
             (deep + 0.2, 0.34, rail_y + PIT), well),
        prim("cube", name + "Sill", (field_x, mesh_mid, -PIT + 0.35),
             (0.42, mesh_along, 0.7), pad),
        prim("cube", name + "RailPad", (field_x, mesh_mid, rail_y),
             (0.58, mesh_along + 0.3, 0.48), pad),
        prim("cube", name + "Fascia", (field_x - 0.28, mesh_mid, rail_y + 0.42),
             (0.16, mesh_along + 0.15, 0.62), gold),
        # Roof over the bench only — not a slab on the warning track.
        prim("cube", name + "Roof", (back_x - 1.4, 0, roof_y),
             (deep * 0.55, along + 0.4, 0.24), roof),
        prim("cube", name + "Bench", (back_x - 1.45, 0, -PIT + 0.82),
             (1.55, along - 4.0, 0.22), wood),
        prim("cylinder", name + "LegH", (back_x - 1.45, y_home - 3.0, -PIT + 0.38),
             (0.22, 0.22, 0.72), post),
        prim("cylinder", name + "LegF", (back_x - 1.45, y_bag + 3.0, -PIT + 0.38),
             (0.22, 0.22, 0.72), post),
    ]
    n_bars = 11
    bar_h = rail_y - 0.4 + PIT - 0.5
    bar_z = -PIT + 0.5 + bar_h * 0.5
    for i in range(n_bars):
        t = i / (n_bars - 1) if n_bars > 1 else 0.5
        yy = y_bag + 0.4 + t * (mesh_along - 0.8)
        pieces.append(
            prim("cube", name + "MeshV" + str(i), (field_x - 0.02, yy, bar_z),
                 (0.06, 0.08, bar_h), mesh_mat)
        )
    pieces.append(
        prim("cube", name + "MeshH0", (field_x - 0.02, mesh_mid, -PIT + 1.15),
             (0.05, mesh_along - 0.6, 0.06), mesh_mat)
    )
    pieces.append(
        prim("cube", name + "MeshH1", (field_x - 0.02, mesh_mid, rail_y - 0.85),
             (0.05, mesh_along - 0.6, 0.06), mesh_mat)
    )
    pieces.extend(stairs(name + "H", conc, y_home, -1))
    bevel(pieces[7], 0.06, 2)
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


def _norm_spray(s):
    s = s % 360.0
    if s > 180.0:
        s -= 360.0
    if s < -180.0:
        s += 360.0
    return s


def _post(r, spray):
    rad = math.radians(spray)
    return r * math.sin(rad), r * math.cos(rad)


def _fence_at(spray):
    t = max(0.0, min(1.0, (spray + FOUL_DEG) / (FOUL_DEG * 2.0)))
    spray = -FOUL_DEG + t * 2.0 * FOUL_DEG
    lf, cf, rf = _post(LEFT_FENCE, -FOUL_DEG), _post(CENTER_FENCE, 0.0), _post(RIGHT_FENCE, FOUL_DEG)
    ax, az = lf
    bx, bz = cf
    cx, cz = rf
    d = 2.0 * (ax * (bz - cz) + bx * (cz - az) + cx * (az - bz))
    if abs(d) < 1e-6:
        return CENTER_FENCE
    a2, b2, c2 = ax * ax + az * az, bx * bx + bz * bz, cx * cx + cz * cz
    ux = (a2 * (bz - cz) + b2 * (cz - az) + c2 * (az - bz)) / d
    uz = (a2 * (cx - bx) + b2 * (ax - cx) + c2 * (bx - ax)) / d
    r2 = (ux - bx) ** 2 + (uz - bz) ** 2
    rad = math.radians(spray)
    sx, sz = math.sin(rad), math.cos(rad)
    b = sx * ux + sz * uz
    disc = b * b - (ux * ux + uz * uz - r2)
    if disc < 0:
        return CENTER_FENCE
    root = math.sqrt(disc)
    return max(b + root, b - root)


def _smoothstep(u):
    u = max(0.0, min(1.0, u))
    return u * u * (3.0 - 2.0 * u)


def wall_radius(spray):
    s = _norm_spray(spray)
    a = abs(s)
    if a <= FOUL_DEG:
        return _fence_at(s)
    pole = _fence_at(FOUL_DEG if s >= 0 else -FOUL_DEG)
    knots = (
        (FOUL_DEG, pole),
        (DUGOUT_SPRAY, DUGOUT_R),
        (95.0, DUGOUT_CLEAR_X),
        (180.0, HOME_RADIUS),
    )
    if a <= knots[0][0]:
        return knots[0][1]
    for i in range(len(knots) - 1):
        a0, r0 = knots[i]
        a1, r1 = knots[i + 1]
        if a > a1:
            continue
        u = 0.0 if a1 - a0 < 1e-6 else (a - a0) / (a1 - a0)
        return r0 + (r1 - r0) * _smoothstep(u)
    return knots[-1][1]


def wall_point(spray):
    r = wall_radius(spray)
    rad = math.radians(_norm_spray(spray))
    return r * math.sin(rad), r * math.cos(rad)


def build_wall_ring(pad, cap):
    """Full padded loop at home origin. Blender XY = Unity XZ. HarborKit drops at world 0."""
    n = WRAP_SEGS
    h, thick = WALL_H, WALL_THICK
    half = thick * 0.5
    mesh = bpy.data.meshes.new("wall-ring")
    ob = bpy.data.objects.new("wall-ring", mesh)
    bpy.context.collection.objects.link(ob)
    bm = bmesh.new()
    inner_b, inner_t, outer_b, outer_t = [], [], [], []
    for i in range(n):
        spray = -180.0 + 360.0 * i / n
        x, y = wall_point(spray)
        r = math.hypot(x, y)
        ux, uy = (x / r, y / r) if r > 1e-6 else (0.0, 1.0)
        ix, iy = x - ux * half, y - uy * half
        ox, oy = x + ux * half, y + uy * half
        inner_b.append(bm.verts.new((ix, iy, 0.0)))
        inner_t.append(bm.verts.new((ix, iy, h)))
        outer_b.append(bm.verts.new((ox, oy, 0.0)))
        outer_t.append(bm.verts.new((ox, oy, h)))
    cap_verts = []
    for i in range(n):
        x, y = wall_point(-180.0 + 360.0 * i / n)
        r = math.hypot(x, y)
        ux, uy = (x / r, y / r) if r > 1e-6 else (0.0, 1.0)
        cap_verts.append(bm.verts.new((x + ux * (half + 0.35), y + uy * (half + 0.35), h + 0.45)))
    bm.verts.ensure_lookup_table()
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((inner_t[i], outer_t[i], outer_t[j], inner_t[j]))
        bm.faces.new((inner_b[i], inner_t[i], inner_t[j], inner_b[j]))
        bm.faces.new((outer_t[i], outer_b[i], outer_b[j], outer_t[j]))
        bm.faces.new((outer_b[i], inner_b[i], inner_b[j], outer_b[j]))
        bm.faces.new((outer_t[i], cap_verts[i], cap_verts[j], outer_t[j]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    ob.data.materials.append(pad)
    for poly in ob.data.polygons:
        poly.use_smooth = True
    return origin_world(ob)


def origin_world(ob):
    bpy.ops.object.select_all(action="DESELECT")
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    bpy.context.scene.cursor.location = (0.0, 0.0, 0.0)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    ob.location = (0.0, 0.0, 0.0)
    return ob


def build_home_plate(chalk, navy):
    """MLB pentagon, Harbor-fat. Point at origin (catcher). Front +Y = pitcher after FBX."""
    mesh = bpy.data.meshes.new("home-plate")
    ob = bpy.data.objects.new("home-plate", mesh)
    bpy.context.collection.objects.link(ob)
    bm = bmesh.new()
    w = PLATE_HALF_W
    verts2d = [(-w, PLATE_FRONT), (w, PLATE_FRONT), (w, PLATE_SHOULDER), (0.0, 0.0), (-w, PLATE_SHOULDER)]
    bottom = [bm.verts.new((x, y, 0.0)) for x, y in verts2d]
    top = [bm.verts.new((x, y, 0.22)) for x, y in verts2d]
    bm.faces.new(bottom)
    bm.faces.new(list(reversed(top)))
    n = len(bottom)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((bottom[i], bottom[j], top[j], top[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bmesh.ops.bevel(bm, geom=bm.edges, offset=0.05, segments=2, affect="EDGES")
    bm.to_mesh(mesh)
    bm.free()
    ob.data.materials.append(chalk)

    rim_mesh = bpy.data.meshes.new("home-plate-rim")
    rim = bpy.data.objects.new("home-plate-rim", rim_mesh)
    bpy.context.collection.objects.link(rim)
    bm = bmesh.new()
    scale = 0.86
    inner = [(x * scale, y * scale + 0.08) for x, y in verts2d]
    outer_v = [bm.verts.new((x, y, 0.225)) for x, y in verts2d]
    inner_v = [bm.verts.new((x, y, 0.225)) for x, y in inner]
    n = len(outer_v)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((outer_v[i], outer_v[j], inner_v[j], inner_v[i]))
    geom = bmesh.ops.extrude_face_region(bm, geom=bm.faces)
    for v in [e for e in geom["geom"] if isinstance(e, bmesh.types.BMVert)]:
        v.co.z += 0.04
    bm.to_mesh(rim_mesh)
    bm.free()
    rim.data.materials.append(navy)
    plate = join_in_place("home-plate", [ob, rim])
    return origin_world(plate)


def build_bag(chalk, navy):
    """Stuffed canvas pillow, diamond-aligned. HarborKit instances at 1B/2B/3B."""
    mesh = bpy.data.meshes.new("bag")
    ob = bpy.data.objects.new("bag", mesh)
    bpy.context.collection.objects.link(ob)
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts:
        v.co.x *= BAG_SIZE
        v.co.y *= BAG_SIZE
        v.co.z *= 0.52
        v.co.z += 0.26
    bmesh.ops.rotate(
        bm, verts=bm.verts, cent=(0, 0, 0),
        matrix=Matrix.Rotation(math.radians(45), 3, "Z"),
    )
    bmesh.ops.bevel(bm, geom=bm.edges, offset=BAG_SIZE * 0.12, segments=5, profile=0.7, affect="EDGES")
    puff_r = BAG_SIZE * 0.58
    for v in bm.verts:
        if v.co.z > 0.18:
            r = math.hypot(v.co.x, v.co.y)
            v.co.z += 0.18 * max(0.0, 1.0 - (r / puff_r) ** 2)
    bm.normal_update()
    bm.to_mesh(mesh)
    bm.free()
    ob.data.materials.append(chalk)
    for poly in ob.data.polygons:
        poly.use_smooth = True

    curve = bpy.data.curves.new("bag-piping", "CURVE")
    curve.dimensions = "3D"
    curve.bevel_depth = 0.055
    curve.bevel_resolution = 2
    curve.fill_mode = "FULL"
    spline = curve.splines.new("BEZIER")
    r = BAG_SIZE * 0.52
    pts = [(r, 0.0), (0.0, r), (-r, 0.0), (0.0, -r)]
    spline.bezier_points.add(len(pts) - 1)
    spline.use_cyclic_u = True
    for i, (x, y) in enumerate(pts):
        p = spline.bezier_points[i]
        p.co = (x, y, 0.58)
        p.handle_left_type = "VECTOR"
        p.handle_right_type = "VECTOR"
    piping = bpy.data.objects.new("bag-piping", curve)
    bpy.context.collection.objects.link(piping)
    piping.data.materials.append(navy)
    bpy.ops.object.select_all(action="DESELECT")
    piping.select_set(True)
    bpy.context.view_layer.objects.active = piping
    bpy.ops.object.convert(target="MESH")
    piping = bpy.context.active_object
    bag = join_in_place("bag", [ob, piping])
    return origin_world(bag)


def build_mound(dirt, hill):
    """Smooth dirt hill with a flat rubber table. Not stacked cylinders.

    Origin at ground center. Rubber is a Play primitive on ParkDiamond.RubberY.
    Height matches ParkDiamond.MoundH so the crown meets the rubber.
    """
    segs = 32
    table_rings = 4
    slope_rings = 10
    mesh = bpy.data.meshes.new("mound")
    ob = bpy.data.objects.new("mound", mesh)
    bpy.context.collection.objects.link(ob)
    bm = bmesh.new()

    def z_at(r):
        if r <= MOUND_TABLE_R:
            return MOUND_H
        if r >= MOUND_R:
            return 0.0
        u = (r - MOUND_TABLE_R) / (MOUND_R - MOUND_TABLE_R)
        s = u * u * (3.0 - 2.0 * u)
        return MOUND_H * (1.0 - s)

    radii = [MOUND_TABLE_R * (i / max(1, table_rings - 1)) for i in range(table_rings)]
    for i in range(1, slope_rings + 1):
        radii.append(MOUND_TABLE_R + (MOUND_R - MOUND_TABLE_R) * (i / slope_rings))

    rings = []
    for r in radii:
        z = z_at(r)
        row = []
        for s in range(segs):
            a = 2.0 * math.pi * s / segs
            row.append(bm.verts.new((r * math.cos(a), r * math.sin(a), z)))
        rings.append(row)

    for i in range(len(rings) - 1):
        inner, outer = rings[i], rings[i + 1]
        for s in range(segs):
            t = (s + 1) % segs
            bm.faces.new((inner[s], inner[t], outer[t], outer[s]))

    bot = bm.verts.new((0.0, 0.0, 0.0))
    lip = rings[-1]
    for s in range(segs):
        t = (s + 1) % segs
        bm.faces.new((bot, lip[t], lip[s]))

    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    for f in bm.faces:
        f.smooth = True
    bm.to_mesh(mesh)
    bm.free()
    ob.data.materials.append(hill)
    return origin_world(ob)


def build_foul_pole(gold, chalk):
    shaft = prim("cylinder", "PoleShaft", (0, 0, 26.0), (1.7, 1.7, 52.0), gold)
    ball = prim("uv_sphere", "PoleBall", (0, 0, 52.0), (2.2, 2.2, 2.2), gold)
    screen = prim("cube", "PoleScreen", (0, -0.4, 38.0), (7.0, 0.18, 16.0), chalk)
    pole = join_in_place("foul-pole", [shaft, ball, screen])
    return origin_world(pole)


def build_warning_track(dirt):
    """One radial slab. HarborKit instances around the fence. Local +Y = along wall, +X = radial."""
    track = prim("cube", "warning-track", (0, 0, 0.11), (22.0, 15.0, 0.22), dirt)
    return origin_world(track)


def build_infield_dirt(dirt):
    """Rounded diamond ring at home origin. Inner grass shows through.

    Height matches ParkDiamond.PathY / PathThick so the ring sits on the lawn,
    not in it (a 0.16-ft slab at z=0.11 z-fights the grass and vanishes).
    """
    home = (0.0, 0.0)
    first = (63.64, 63.64)
    second = (0.0, 127.28)
    third = (-63.64, 63.64)
    inset = PATH_CORNER
    pieces = []

    def segment(name, a, b):
        ax, ay = a
        bx, by = b
        dx, dy = bx - ax, by - ay
        span = math.hypot(dx, dy)
        ux, uy = dx / span, dy / span
        sx, sy = ax + ux * inset, ay + uy * inset
        ex, ey = bx - ux * inset, by - uy * inset
        mx, my = (sx + ex) * 0.5, (sy + ey) * 0.5
        length = math.hypot(ex - sx, ey - sy)
        ang = math.atan2(uy, ux)
        return prim("cube", name, (mx, my, PATH_Y), (length, PATH_WIDTH, PATH_THICK), dirt, rot=(0, 0, ang))

    pieces.append(segment("DirtH1", home, first))
    pieces.append(segment("Dirt12", first, second))
    pieces.append(segment("Dirt23", second, third))
    pieces.append(segment("Dirt3H", third, home))
    for name, pos, radius in (
        ("Dirt1", first, PATH_CORNER),
        ("Dirt2", second, PATH_CORNER),
        ("Dirt3", third, PATH_CORNER),
        ("DirtH", home, HOME_PACKED_R),
    ):
        d = radius * 2.0
        pieces.append(prim("cylinder", name, (pos[0], pos[1], PATH_Y), (d, d, PATH_THICK), dirt))
    ring = join_in_place("infield-dirt", pieces)
    return origin_world(ring)


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
    dirt = mat("dirt", (0.58, 0.40, 0.24))
    well = mat("well", (0.50, 0.34, 0.20))
    conc = mat("conc", (0.62, 0.60, 0.56))
    post = mat("post", (0.28, 0.22, 0.16))
    jersey = mat("jersey", (0.86, 0.19, 0.16))
    flesh = mat("flesh", (1.0, 0.80, 0.68))
    cap = mat("cap", (1.0, 0.80, 0.25))
    chalk = mat("chalk", (0.96, 0.91, 0.80))
    navy = mat("navy", (0.06, 0.18, 0.42))
    mesh_mat = mat("mesh", (0.08, 0.10, 0.12))
    build_dugout("dugout-1b", wood, roof, gold, pad, post, conc, well, mesh_mat, flip_x=False)
    build_dugout("dugout-3b", wood, roof, gold, pad, post, conc, well, mesh_mat, flip_x=True)
    build_wall(pad, gold)
    build_fan("fan-stand", sit=False, jersey=jersey, flesh=flesh, cap=cap)
    build_fan("fan-sit", sit=True, jersey=jersey, flesh=flesh, cap=cap)
    build_home_plate(chalk, navy)
    build_bag(chalk, navy)
    hill = mat("hill", (0.66, 0.44, 0.26))
    build_mound(dirt, hill)
    build_foul_pole(gold, chalk)
    build_warning_track(dirt)
    build_infield_dirt(dirt)


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
