#!/usr/bin/env python3
"""Drop a unique character into a Grand Sluggers package.

Art is made in Blender. A posed GLB with no skeleton is a *source*, not a
Unity skin. We do **not** heat-weight a fused toy onto bones — that shreds
the mesh the first time a limb rotates.

Default bind is **segmented**: split the mesh into rigid pieces (shell, head,
arms, legs) and parent each piece to a named socket from data/art/rig.json.
Limbs rotate independently. The shell cannot invert.

  /opt/homebrew/bin/blender --background --python tools/blender/drop_character.py -- \
    --src /path/to/hero.glb --id fenn --bind segmented \
    --out unity/Assets/Art/Characters/fenn/fenn.fbx \
    --resources unity/Assets/Resources/Art/Characters/fenn/fenn.fbx \
    --portrait unity/Assets/Resources/Art/fenn-hero.jpg

--bind skinned  only when the source already has painted weights (quality path).
--bind rigid    statue (debug).
--keep-weights  keep imported vertex groups (do not strip a previous drop).

Rotate in: character JSON + skins.json mesh/bind=segmented + this drop.
Rotate out: delete the JSON rows and Assets/Art/Characters/{id}/.
Missing FBX keeps SharedRig primitives.
"""
from __future__ import annotations

import argparse
import math
import shutil
import sys
from collections import defaultdict
from pathlib import Path

import bmesh
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

LIMB_PIECES = [
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
    body.name = "body"
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    return body


def strip_rig():
    """Previous drops leave a fitted armature and heat groups. Those are not art."""
    for ob in list(bpy.data.objects):
        if ob.type == "ARMATURE":
            bpy.data.objects.remove(ob, do_unlink=True)
    for ob in bpy.data.objects:
        if ob.type != "MESH":
            continue
        for mod in list(ob.modifiers):
            ob.modifiers.remove(mod)
        for g in list(ob.vertex_groups):
            ob.vertex_groups.remove(g)


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


def decimate(body, target_faces=40000):
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
    if (b.tail - b.head).length < 0.04:
        b.tail = b.head + Vector((0.0, 0.0, 0.12))
    b.use_connect = False
    if parent is not None:
        b.parent = arm.edit_bones[parent]
    return b


def pct(vals, p):
    s = sorted(vals)
    i = int(max(0, min(len(s) - 1, p * (len(s) - 1))))
    return s[i]


def centroid(points):
    n = max(1, len(points))
    x = sum(p.x for p in points) / n
    y = sum(p.y for p in points) / n
    z = sum(p.z for p in points) / n
    return Vector((x, y, z))


def world_verts(body):
    mw = body.matrix_world
    return [mw @ v.co for v in body.data.vertices]


def fit_armature(body):
    """Named sockets inside THIS mesh. Not Rio's T-pose."""
    verts = world_verts(body)
    xs = [v.x for v in verts]
    zs = [v.z for v in verts]
    z10, z35, z50, z65, z82 = pct(zs, 0.10), pct(zs, 0.35), pct(zs, 0.50), pct(zs, 0.65), pct(zs, 0.82)
    x12, x88 = pct(xs, 0.12), pct(xs, 0.88)
    head_pts = [v for v in verts if v.z >= z82]
    torso_pts = [v for v in verts if z35 <= v.z <= z65 and abs(v.x) < max(0.4, 0.45 * (x88 - x12))]
    larm_pts = [v for v in verts if v.x <= x12 and z35 <= v.z <= z82]
    rarm_pts = [v for v in verts if v.x >= x88 and z35 <= v.z <= z82]
    lhand_pts = [v for v in verts if v.x <= pct(xs, 0.08) and v.z <= z50]
    rhand_pts = [v for v in verts if v.x >= pct(xs, 0.92) and v.z <= z50]
    lfoot_pts = [v for v in verts if v.x < 0 and v.z <= z10]
    rfoot_pts = [v for v in verts if v.x >= 0 and v.z <= z10]
    head_c = centroid(head_pts) if head_pts else Vector((0, 0, z82))
    torso_c = centroid(torso_pts) if torso_pts else Vector((0, 0, z50))
    larm_c = centroid(larm_pts) if larm_pts else Vector((x12, 0, z50))
    rarm_c = centroid(rarm_pts) if rarm_pts else Vector((x88, 0, z50))
    lhand_c = centroid(lhand_pts) if lhand_pts else larm_c + Vector((0, 0, -0.6))
    rhand_c = centroid(rhand_pts) if rhand_pts else rarm_c + Vector((0, 0, -0.6))
    lfoot_c = centroid(lfoot_pts) if lfoot_pts else Vector((-0.4, 0, 0.05))
    rfoot_c = centroid(rfoot_pts) if rfoot_pts else Vector((0.4, 0, 0.05))
    hip_z = max(0.15, (torso_c.z + lfoot_c.z) * 0.5)
    lhip = Vector((lfoot_c.x, lfoot_c.y, hip_z))
    rhip = Vector((rfoot_c.x, rfoot_c.y, hip_z))
    lknee = (lhip + lfoot_c) * 0.5
    rknee = (rhip + rfoot_c) * 0.5
    neck = Vector((head_c.x, head_c.y, (torso_c.z + head_c.z) * 0.5))
    l_sh = Vector(((torso_c.x + larm_c.x) * 0.5, (torso_c.y + larm_c.y) * 0.5, larm_c.z))
    r_sh = Vector(((torso_c.x + rarm_c.x) * 0.5, (torso_c.y + rarm_c.y) * 0.5, rarm_c.z))

    print("fit head", tuple(round(c, 3) for c in head_c), "torso", tuple(round(c, 3) for c in torso_c))
    print("fit larm", tuple(round(c, 3) for c in larm_c), "rarm", tuple(round(c, 3) for c in rarm_c))
    print("fit lfoot", tuple(round(c, 3) for c in lfoot_c), "rfoot", tuple(round(c, 3) for c in rfoot_c))

    arm_data = bpy.data.armatures.new("hero-shared-data")
    arm_ob = bpy.data.objects.new("hero-shared", arm_data)
    bpy.context.collection.objects.link(arm_ob)
    bpy.context.view_layer.objects.active = arm_ob
    bpy.ops.object.mode_set(mode="EDIT")
    add_bone(arm_data, "root", (0, 0, 0), (0, 0, 0.2))
    add_bone(arm_data, "torso", torso_c + Vector((0, 0, -0.35)), torso_c + Vector((0, 0, 0.45)), "root")
    add_bone(arm_data, "head", neck, head_c + Vector((0, 0, 0.2)), "torso")
    add_bone(arm_data, "lUpper", l_sh, larm_c, "torso")
    add_bone(arm_data, "lFore", larm_c, lhand_c, "lUpper")
    add_bone(arm_data, "rUpper", r_sh, rarm_c, "torso")
    add_bone(arm_data, "rFore", rarm_c, rhand_c, "rUpper")
    add_bone(arm_data, "lThigh", lhip, lknee, "root")
    add_bone(arm_data, "lShin", lknee, lfoot_c, "lThigh")
    add_bone(arm_data, "rThigh", rhip, rknee, "root")
    add_bone(arm_data, "rShin", rknee, rfoot_c, "rThigh")
    add_bone(arm_data, "bat", rhand_c, rhand_c + Vector((0.0, 0.15, -0.45)), "rFore")
    add_bone(arm_data, "glove", lhand_c, lhand_c + Vector((0.0, 0.15, -0.45)), "lFore")
    bpy.ops.object.mode_set(mode="OBJECT")
    missing = [n for n in BONES if n not in arm_data.bones]
    if missing:
        raise RuntimeError("missing bones: " + ",".join(missing))
    return arm_ob, {
        "z_hip": hip_z,
        "z_neck": neck.z,
        "z_knee_l": lknee.z,
        "z_knee_r": rknee.z,
        "x_arm_l": larm_c.x,
        "x_arm_r": rarm_c.x,
        "l_sh": l_sh,
        "r_sh": r_sh,
        "larm_c": larm_c,
        "rarm_c": rarm_c,
        "lhand_c": lhand_c,
        "rhand_c": rhand_c,
        "lknee": lknee,
        "rknee": rknee,
    }


def finish_roll(arm_ob):
    bpy.context.view_layer.objects.active = arm_ob
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.armature.select_all(action="SELECT")
    try:
        bpy.ops.armature.calculate_roll(type="GLOBAL_POS_Z")
    except Exception as ex:
        print("roll skip", ex)
    bpy.ops.object.mode_set(mode="OBJECT")


def dist2(a, b):
    d = a - b
    return d.x * d.x + d.y * d.y + d.z * d.z


def assign_vertices(body, fit):
    """Protruding limbs only. The shell/core stays torso so it never tears."""
    verts = world_verts(body)
    xs = [v.x for v in verts]
    zs = [v.z for v in verts]
    x_arm_l = pct(xs, 0.10)
    x_arm_r = pct(xs, 0.90)
    z_head = pct(zs, 0.86)
    z_hip = fit["z_hip"]
    z_knee_l = fit["z_knee_l"]
    z_knee_r = fit["z_knee_r"]
    larm_c = fit["larm_c"]
    rarm_c = fit["rarm_c"]
    lhand_c = fit["lhand_c"]
    rhand_c = fit["rhand_c"]
    l_sh = fit["l_sh"]
    r_sh = fit["r_sh"]

    names = []
    counts = defaultdict(int)
    for v in verts:
        name = "torso"
        if v.z >= z_head and abs(v.x) < max(abs(x_arm_l), abs(x_arm_r)) * 0.72:
            name = "head"
        elif v.x <= x_arm_l and v.z >= z_hip * 0.85:
            name = "lFore" if dist2(v, lhand_c) < dist2(v, l_sh) * 0.85 or v.z < larm_c.z - 0.15 else "lUpper"
        elif v.x >= x_arm_r and v.z >= z_hip * 0.85:
            name = "rFore" if dist2(v, rhand_c) < dist2(v, r_sh) * 0.85 or v.z < rarm_c.z - 0.15 else "rUpper"
        elif v.z <= z_hip:
            if v.x < 0:
                name = "lShin" if v.z <= z_knee_l else "lThigh"
            else:
                name = "rShin" if v.z <= z_knee_r else "rThigh"
        names.append(name)
        counts[name] += 1
    print("assign", dict(counts))
    return names


def outward_normals(mesh, origin):
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    flip = 0
    for f in bm.faces:
        to_f = f.calc_center_median() - origin
        if to_f.length > 1e-6 and to_f.dot(f.normal) < 0:
            flip += 1
    if bm.faces and flip > len(bm.faces) * 0.5:
        bmesh.ops.reverse_faces(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()


def split_pieces(body, assignment, id_):
    """One rigid mesh per socket. Mixed faces go to every group that owns a vert (overlap, no holes)."""
    src = body.data
    uv_src = src.uv_layers.active
    materials = list(src.materials)
    origin = Vector((0.0, 0.0, TARGET_HEIGHT * 0.45))

    faces_by = defaultdict(list)
    for poly in src.polygons:
        names = {assignment[i] for i in poly.vertices}
        for name in names:
            if name in LIMB_PIECES:
                faces_by[name].append(poly)

    pieces = []
    for name in LIMB_PIECES:
        polys = faces_by.get(name) or []
        if len(polys) < 4:
            print("piece skip", name, "faces", len(polys))
            continue
        used = []
        old_to_new = {}
        for poly in polys:
            for i in poly.vertices:
                if i not in old_to_new:
                    old_to_new[i] = len(used)
                    used.append(src.vertices[i].co.copy())
        faces = []
        face_uvs = []
        for poly in polys:
            faces.append([old_to_new[i] for i in poly.vertices])
            if uv_src:
                face_uvs.append([uv_src.data[li].uv.copy() for li in poly.loop_indices])
        mesh = bpy.data.meshes.new(id_ + "_" + name)
        mesh.from_pydata(used, [], faces)
        mesh.update()
        if face_uvs:
            uv = mesh.uv_layers.new(name="UVMap")
            loop_i = 0
            for fu in face_uvs:
                for u in fu:
                    uv.data[loop_i].uv = u
                    loop_i += 1
        for mat in materials:
            mesh.materials.append(mat)
        outward_normals(mesh, origin)
        ob = bpy.data.objects.new(id_ + "_" + name, mesh)
        bpy.context.collection.objects.link(ob)
        ob.matrix_world = body.matrix_world.copy()
        pieces.append((name, ob))
        print("piece", name, "verts", len(used), "faces", len(faces))
    if not any(n == "torso" for n, _ in pieces):
        raise RuntimeError("segmented drop produced no torso piece")
    return pieces


def parent_piece(ob, arm, bone_name):
    mw = ob.matrix_world.copy()
    ob.parent = arm
    ob.parent_type = "BONE"
    ob.parent_bone = bone_name
    ob.matrix_world = mw


def rigid_parent(body, arm_ob):
    for mod in list(body.modifiers):
        body.modifiers.remove(mod)
    for g in list(body.vertex_groups):
        body.vertex_groups.remove(g)
    body.parent = arm_ob
    body.parent_type = "OBJECT"
    body.parent_bone = ""
    body.location = (0.0, 0.0, 0.0)
    body.rotation_euler = (0.0, 0.0, 0.0)


def keep_painted_skin(body, arm_ob):
    """Quality path: source already has groups. Parent to the armature, keep weights."""
    has = {g.name for g in body.vertex_groups}
    if not any(n in has for n in LIMB_PIECES):
        return False
    body.parent = arm_ob
    body.parent_type = "ARMATURE"
    if not any(m.type == "ARMATURE" for m in body.modifiers):
        mod = body.modifiers.new("Armature", "ARMATURE")
        mod.object = arm_ob
    print("keep painted groups", [g.name for g in body.vertex_groups])
    return True


def albedo_image(body):
    for slot in body.material_slots:
        mat = slot.material
        if not mat or not mat.use_nodes:
            continue
        bsdf = next((n for n in mat.node_tree.nodes if n.type == "BSDF_PRINCIPLED"), None)
        if bsdf is None:
            continue
        sock = bsdf.inputs.get("Base Color")
        if sock and sock.links:
            node = sock.links[0].from_node
            if node.type == "TEX_IMAGE" and node.image:
                return node.image
    colored = []
    for img in bpy.data.images:
        if not img.has_data or img.size[0] < 16:
            continue
        colored.append(img)
    colored.sort(key=lambda i: i.size[0] * i.size[1], reverse=True)
    return colored[0] if colored else None


def save_albedo(body, dest: Path, size=1024):
    img = albedo_image(body)
    if img is None:
        print("no albedo image")
        return None
    dest.parent.mkdir(parents=True, exist_ok=True)
    work = img.copy()
    work.scale(size, size)
    work.filepath_raw = str(dest)
    work.file_format = "PNG"
    work.save()
    print("WROTE", dest, work.size[0], "x", work.size[1])
    return dest


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
    path.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.render.render(write_still=True)


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--src", required=True)
    p.add_argument("--id", default="body")
    p.add_argument("--out", required=True)
    p.add_argument("--resources", default="")
    p.add_argument("--portrait", default="")
    p.add_argument("--faces", type=int, default=40000)
    p.add_argument("--bind", default="segmented", choices=("segmented", "skinned", "rigid"))
    p.add_argument("--keep-weights", action="store_true")
    argv = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else sys.argv[1:]
    args = p.parse_args(argv)

    src = Path(args.src)
    out = Path(args.out)
    if not src.is_file():
        raise SystemExit("missing source " + str(src))

    nuke()
    suffix = src.suffix.lower()
    if suffix in (".glb", ".gltf"):
        bpy.ops.import_scene.gltf(filepath=str(src))
    elif suffix == ".fbx":
        bpy.ops.import_scene.fbx(filepath=str(src))
    elif suffix == ".obj":
        bpy.ops.wm.obj_import(filepath=str(src))
    else:
        raise SystemExit("unsupported drop format " + suffix + " (glb/gltf/fbx/obj)")

    if not args.keep_weights:
        strip_rig()

    body = join_meshes()
    body.name = args.id
    stand_on_origin(body)
    faces = decimate(body, args.faces)
    print("faces after decimate", faces, "verts", len(body.data.vertices))

    arm, fit = fit_armature(body)
    finish_roll(arm)

    bind = args.bind
    existing = out.parent / (args.id + "-albedo.png")
    albedo = save_albedo(body, existing, size=1024)

    if bind == "rigid":
        rigid_parent(body, arm)
        print("bind rigid (statue)")
    elif bind == "skinned" and args.keep_weights and keep_painted_skin(body, arm):
        print("bind skinned (painted weights)")
    else:
        if bind == "skinned":
            print("skinned requested without painted weights — using segmented")
        assignment = assign_vertices(body, fit)
        pieces = split_pieces(body, assignment, args.id)
        for name, ob in pieces:
            parent_piece(ob, arm, name)
        bpy.data.objects.remove(body, do_unlink=True)
        print("bind segmented pieces", len(pieces))

    export_fbx(out)
    if args.resources:
        dest = Path(args.resources)
        dest.parent.mkdir(parents=True, exist_ok=True)
        if dest.resolve() != out.resolve():
            shutil.copy2(out, dest)
        albedo_name = args.id + "-albedo.png"
        src_albedo = out.parent / albedo_name
        if src_albedo.is_file():
            shutil.copy2(src_albedo, dest.parent / albedo_name)
        print("WROTE", dest)
    if args.portrait:
        render_portrait(Path(args.portrait))
    print("WROTE", out, "bind", bind)


if __name__ == "__main__":
    main()
