#!/usr/bin/env python3
"""Harbor kit meshes: dugout, wall, crowd, home plate, bag. Bind in HarborKit; missing file keeps primitives.

Unity Generic, axis_forward -Z, axis_up Y.
Names: dugout-1b, dugout-3b, wall-panel, fan-stand, fan-sit, home-plate, bag.

Stages (docs/agent-rails.md §6, data/agent/dcc-stages.json). One-shotting a kit
mesh is a patch. The next prompt names the stage it continues.
  1 blocking  diamond / wall ring volumes   --clay scratchpad/takes
  2 fill      kit slots                     --clay
  3 motion    — (Harbor has no takes)
  4 export    FBX into the catalog slot     --out
  5 still     tools/dcc-still.sh harbor + tools/still-gate.sh
Existing flags --out and --clay still run. This script does not retarget a rig.
"""
from __future__ import annotations

import argparse
import math
import re
import shutil
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector

REPO = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(REPO / "tools"))  # tools/ is not a package
import jsonc  # noqa: E402  (the one reader for data files with // notes)

PARK_ID = "harbor-diamond"


def sim_consts(cls: str) -> dict:
    """The Sim's own numeric `const` table for one class (src/GrandSluggers.Sim/<cls>.cs).

    The dress the decision keeps in feet (path width, bag pads, home pad, the dugout) lives as
    `const` in Sim, not in data. The kit reads that table rather than copying it, so the kit and
    the game cannot drift. A name that stops being a numeric const raises here, not in the still.
    """
    text = (REPO / "src" / "GrandSluggers.Sim" / (cls + ".cs")).read_text()
    pat = r"\bconst\s+(?:float|double|int)\s+(\w+)\s*=\s*(-?\d+(?:\.\d+)?)[fd]?\s*;"
    return {m.group(1): float(m.group(2)) for m in re.finditer(pat, text)}


# The diamond the sim plays (data/rules/infield.json), the edge it ends at
# (data/rules/boundary.json) and the park's fence (data/parks/harbor-diamond.json).
# The next geometry change rebakes with no code edit.
INFIELD = jsonc.load(REPO / "data" / "rules" / "infield.json")
BOUNDARY = jsonc.load(REPO / "data" / "rules" / "boundary.json")
PARK = jsonc.load(REPO / "data" / "parks" / (PARK_ID + ".json"))
CORNER = float(INFIELD["cornerFt"])
SECOND = float(INFIELD["secondFt"])
MOUND_FT = float(INFIELD["moundFt"])
INNER_HALF = float(INFIELD["innerHalfFt"])
BACK_ARC = float(INFIELD["backArcFt"])
DIAMOND = sim_consts("ParkDiamond")
DUGOUT = sim_consts("HarborDugout")

HALF_ALONG = DUGOUT["HalfAlong"]
HALF_DEEP = DUGOUT["HalfDeep"]
PIT = DUGOUT["PitDepth"]
STAIR_COUNT = int(DUGOUT["StairCount"])
STAIR_DEPTH = DUGOUT["StairDepth"]
FIELD_STAIR_RUN = DUGOUT["FieldStairRun"]
# Keep in sync with HarborInfield.BagSize / HomeSet.PlateW / ParkDiamond (feet).
BAG_SIZE = 4.0
# The park's fence (D15: the drawn wall is the fence the flight clips against).
LEFT_FENCE = float(PARK["leftFenceFt"])
CENTER_FENCE = float(PARK["centerFenceFt"])
RIGHT_FENCE = float(PARK["rightFenceFt"])
# The wall ring: HOME_RADIUS and WRAP_SEGS are the kit's own (HarborKitScriptTests); the dugout is read above.
FOUL_DEG = 45.0
HOME_RADIUS = 34.0
WRAP_SEGS = 120
WALL_H = float(PARK["fenceHeightFt"])
WALL_THICK = 3.4
DUGOUT_X = DUGOUT["X"]
DUGOUT_Z = DUGOUT["Z"]
DUGOUT_PAD = float(BOUNDARY["dugoutPadFt"])
DUGOUT_CLEAR_X = DUGOUT_X + HALF_DEEP + DUGOUT_PAD
DUGOUT_CLEAR_Z = DUGOUT_Z + HALF_ALONG + 10.0
DUGOUT_R = math.hypot(DUGOUT_CLEAR_X, DUGOUT_CLEAR_Z)
DUGOUT_SPRAY = math.degrees(math.atan2(DUGOUT_CLEAR_X, DUGOUT_CLEAR_Z))
# OBR 2.02: 17″ front, 8½″ shoulders, point at origin (catcher).
PLATE_HALF_W = 17.0 / 12.0 / 2.0
PLATE_FRONT = 17.0 / 12.0
PLATE_SHOULDER = 8.5 / 12.0
PATH_WIDTH = DIAMOND["PathWidth"]
BAG_PAD_R = DIAMOND["BagPadR"]
PATH_Y = 0.26
PATH_THICK = 0.24
HOME_PACKED_R = DIAMOND["HomePackedR"]
SKIN_SEGS = int(DIAMOND["SkinLoopSegs"])
KIT_CLAY = ("home-plate", "bag", "mound", "fan-stand")
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
    gate = 3.5
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


def build_home_plate(chalk):
    """One rounded chalk slab. Point at origin; FieldKit turns FBX -Z toward the mound."""
    mesh = bpy.data.meshes.new("home-plate")
    ob = bpy.data.objects.new("home-plate", mesh)
    bpy.context.collection.objects.link(ob)
    bm = bmesh.new()
    w = PLATE_HALF_W
    verts2d = [(-w, PLATE_FRONT), (w, PLATE_FRONT), (w, PLATE_SHOULDER), (0.0, 0.0), (-w, PLATE_SHOULDER)]
    face_z = 0.22  # FieldKit's AuthoredPlateFaceY; the rest of the slab sits in dirt.
    bottom = [bm.verts.new((x, y, 0.0)) for x, y in verts2d]
    top = [bm.verts.new((x, y, face_z)) for x, y in verts2d]
    bm.faces.new(bottom)
    bm.faces.new(list(reversed(top)))
    n = len(bottom)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((bottom[i], bottom[j], top[j], top[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    # Round the playing-face perimeter into the side of this same solid.
    # Keep the buried bottom's five corners as the exact footprint datum.
    perimeter = [e for e in bm.edges if all(abs(v.co.z - face_z) < 1e-6 for v in e.verts)]
    bmesh.ops.bevel(bm, geom=perimeter, offset=0.035, segments=5, profile=0.5, affect="EDGES")
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    for face in bm.faces:
        face.smooth = 0.001 < face.normal.z < 0.999
    # A ledge is a second shell or geometry above the flat playing face.
    assert all(e.is_manifold for e in bm.edges), "plate must be one closed slab"
    seen, pending = set(), [next(iter(bm.verts))]
    while pending:
        v = pending.pop()
        if v in seen:
            continue
        seen.add(v)
        pending.extend(e.other_vert(v) for e in v.link_edges)
    assert len(seen) == len(bm.verts), "plate contains a separate rim or shell"
    assert abs(max(v.co.z for v in bm.verts) - face_z) < 1e-6, "plate rises above its playing face"
    for face in bm.faces:
        if face.normal.z > 0.999:
            assert all(abs(v.co.z - face_z) < 1e-6 for v in face.verts), "plate has a horizontal ledge below its playing face"
    for x, y in verts2d:
        assert any((v.co - Vector((x, y, 0))).length < 1e-6 for v in bm.verts), "plate footprint moved"
    bm.to_mesh(mesh)
    bm.free()
    ob.data.materials.append(chalk)
    return origin_world(ob)


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
    """Yellow shaft + fair-facing grate. HarborKit dresses these in world; this is the kit slot."""
    h = 72.0
    shaft = prim("cylinder", "PoleShaft", (0, 0, h * 0.5), (1.24, 1.24, h), gold)
    ball = prim("uv_sphere", "PoleBall", (0, 0, h), (1.6, 1.6, 1.6), gold)
    # Thin in Y: after FBX, HarborKit LookRotation(fair) puts that axis toward the diamond.
    screen = prim("cube", "PoleScreen", (0, 0.9, 45.0), (5.6, 0.22, 38.0), gold)
    pole = join_in_place("foul-pole", [shaft, ball, screen])
    return origin_world(pole)


def build_warning_track(dirt):
    """One radial slab. HarborKit instances around the fence. Local +Y = along wall, +X = radial."""
    track = prim("cube", "warning-track", (0, 0, 0.11), (22.0, 15.0, 0.22), dirt)
    return origin_world(track)


def _ray_circle_far(ox, oz, dx, dz, cx, cz, r):
    fx, fz = ox - cx, oz - cz
    b = fx * dx + fz * dz
    c = fx * fx + fz * fz - r * r
    disc = b * b - c
    if disc < 0:
        return 0.0
    s = math.sqrt(disc)
    t = max(-b - s, -b + s)
    return t if t > 0.01 else 0.0


def _angular_dist(a, b):
    d = a - b
    while d > math.pi:
        d -= 2 * math.pi
    while d < -math.pi:
        d += 2 * math.pi
    return abs(d)


def _inner_on_ray(x, z):
    """ParkDiamond.InnerOnRay: the grass diamond's edge on the ray from its center."""
    center = SECOND * 0.5
    dx, dz = x, z - center
    l1 = abs(dx) + abs(dz)
    if l1 < 1e-6:
        return 0.0, center
    s = INNER_HALF / l1
    return dx * s, center + dz * s


def _outer_at(ang):
    """ParkDiamond.OuterAt: thin home legs, bag pads, and the mound-centered back arc."""
    center = SECOND * 0.5
    ux, uz = math.cos(ang), math.sin(ang)
    ix, iz = _inner_on_ray(ux, center + uz)
    r_path = math.hypot(ix, iz - center) + PATH_WIDTH
    r_home = _ray_circle_far(0, center, ux, uz, 0, 0, HOME_PACKED_R)
    r_back = _ray_circle_far(0, center, ux, uz, 0, MOUND_FT, BACK_ARC)
    first = (CORNER, CORNER)
    second = (0.0, SECOND)
    third = (-CORNER, CORNER)
    r_bag = max(_ray_circle_far(0, center, ux, uz, bx, bz, BAG_PAD_R) for bx, bz in (first, second, third))
    r_front = max(r_path, r_home, r_bag)
    u = (_angular_dist(ang, -math.pi / 2) - math.pi / 4) / (math.pi / 4)
    blend = _smoothstep(u)
    r = max(r_front + (max(r_back, r_front) - r_front) * blend, r_bag)
    return ux * r, center + uz * r


def infield_outline():
    """(inner, outer) loops of the dirt ring: the outline ParkDiamond.OuterVerts draws in Unity."""
    outer = [_outer_at(i * 2 * math.pi / SKIN_SEGS) for i in range(SKIN_SEGS)]
    inner = [_inner_on_ray(x, z) for x, z in outer]
    return inner, outer


def build_infield_dirt(dirt):
    """The dirt ring the sim draws, around the grass diamond. Inner grass shows through.

    Outline is ParkDiamond's: home legs PathWidth wide, a BagPadR pad at each bag, the home pad,
    and the 1B–2B–3B back arc (infield.json backArcFt) from the rubber. Height matches
    ParkDiamond.PathY / PathThick so the ring sits on the lawn, not in it.
    """
    inner, outer = infield_outline()
    n = len(outer)
    top, bot = PATH_Y + PATH_THICK * 0.5, PATH_Y - PATH_THICK * 0.5
    mesh = bpy.data.meshes.new("infield-dirt")
    ob = bpy.data.objects.new("infield-dirt", mesh)
    bpy.context.collection.objects.link(ob)
    bm = bmesh.new()
    it = [bm.verts.new((x, y, top)) for x, y in inner]
    ot = [bm.verts.new((x, y, top)) for x, y in outer]
    ib = [bm.verts.new((x, y, bot)) for x, y in inner]
    ob_ = [bm.verts.new((x, y, bot)) for x, y in outer]
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((it[i], ot[i], ot[j], it[j]))
        bm.faces.new((ib[j], ob_[j], ob_[i], ib[i]))
        bm.faces.new((ot[i], ob_[i], ob_[j], ot[j]))
        bm.faces.new((it[j], ib[j], ib[i], it[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    ob.data.materials.append(dirt)
    return origin_world(ob)


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
    print("diamond", "baselineFt", INFIELD["baselineFt"], "cornerFt", CORNER, "secondFt", SECOND,
          "moundFt", MOUND_FT, "backArcFt", BACK_ARC, "pathWidth", PATH_WIDTH, "bagPad", BAG_PAD_R,
          "homePad", HOME_PACKED_R, "dugout", DUGOUT_X, DUGOUT_Z, "fence", LEFT_FENCE, CENTER_FENCE, RIGHT_FENCE, "wallH", WALL_H)
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
    build_home_plate(chalk)
    build_bag(chalk, navy)
    hill = mat("hill", (0.66, 0.44, 0.26))
    build_mound(dirt, hill)
    build_foul_pole(gold, chalk)
    build_warning_track(dirt)
    build_infield_dirt(dirt)


def diamond_tile(clay, path: Path, width: int, height: int):
    """Overhead of the diamond the kit bakes: infield-dirt, bag at 1B/2B/3B, mound on the rubber
    distance, plate at home — all from data/rules/infield.json. Layout proof only: the stand-ins are
    removed before export, and Unity places the bag and the mound itself (FieldKit)."""
    clay.setup(width, height)
    scene = bpy.context.scene
    stand_ins = []
    for name, loc in (
        ("bag", (CORNER, CORNER, 0.0)),
        ("bag", (0.0, SECOND, 0.0)),
        ("bag", (-CORNER, CORNER, 0.0)),
        ("mound", (0.0, MOUND_FT, 0.0)),
        ("home-plate", (0.0, 0.0, 0.0)),
    ):
        src = bpy.data.objects[name]
        dup = src.copy()
        dup.name = "diamond-" + name
        dup.location = loc
        scene.collection.objects.link(dup)
        stand_ins.append(dup)
    shown = {"infield-dirt"} | {o.name for o in stand_ins}
    for o in bpy.data.objects:
        if o.type == "MESH":
            o.hide_render = o.name not in shown
    cam_data = bpy.data.cameras.new("diamond-cam")
    cam_data.type = "ORTHO"
    # Fixed frame: the 80-ft dirt (home pad to back arc, ±backArcFt wide) fits inside it; a
    # 90-ft diamond would overrun it, so the still itself reads the size change.
    cam_data.ortho_scale = 230.0
    cam = bpy.data.objects.new("diamond-cam", cam_data)
    scene.collection.objects.link(cam)
    cam.location = (0.0, 59.0, 300.0)
    cam.rotation_euler = (0.0, 0.0, 0.0)
    previous = scene.camera
    scene.camera = cam
    scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)
    scene.camera = previous
    for o in stand_ins:
        bpy.data.objects.remove(o, do_unlink=True)
    bpy.data.objects.remove(cam, do_unlink=True)
    bpy.data.cameras.remove(cam_data)
    return path


def clay_check(folder: Path):
    """Named DCC still of origin-centered kit pieces (docs/agent-rails.md §4)."""
    sys.path.insert(0, str(Path(__file__).resolve().parent))
    import clay
    folder = folder.resolve()
    folder.mkdir(parents=True, exist_ok=True)
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    missing = [name for name in KIT_CLAY if name not in bpy.data.objects]
    if missing:
        raise SystemExit("harbor clay: missing " + ",".join(missing))
    tiles = []
    for name in KIT_CLAY:
        for o in meshes:
            o.hide_render = o.name != name
        tiles.append(clay.render(folder / f"kit-{name}.png", "three-quarter", 360, 480))
    tiles.append(diamond_tile(clay, folder / "kit-diamond.png", 360, 480))
    for o in meshes:
        o.hide_render = False
    out = clay.sheet(tiles, folder / "harbor-kit.png", columns=len(tiles))
    print("clay", out)
    return out


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
    p.add_argument("--clay", default="", help="Folder for the named DCC kit still.")
    p.add_argument("--resources", default="",
                   help="Player copy folder (Assets/Resources/...); the same bytes as --out.")
    args = p.parse_args(argv)
    build()
    if args.clay:
        clay_check(Path(args.clay))
    out = Path(args.out).resolve()
    export_fbx(out)
    if args.resources:
        dest = Path(args.resources).resolve() / out.name
        dest.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(out, dest)
        print("resources", dest)


if __name__ == "__main__":
    argv = sys.argv
    if "--" in argv:
        argv = argv[argv.index("--") + 1 :]
    else:
        argv = argv[1:]
    main(argv)
