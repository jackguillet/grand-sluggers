#!/usr/bin/env python3
"""Drop a character GLB into the shared-rig slot.

The source is a posed toy (often no skeleton). We do **not** auto-weight it to
hero-shared limbs — that explodes in Unity when MoveBones poses the chain.
Default bind is **skinned**: bones are fitted *inside this mesh*, then heat-weighted
so MoveBones can swing / scoop / run without shredding a posed toy. Pass
`--bind rigid` only for a statue.

  /opt/homebrew/bin/blender --background --python tools/blender/drop_character.py -- \
    --src /path/to/hero.glb --id fenn \
    --out unity/Assets/Art/Characters/fenn/fenn.fbx \
    --resources unity/Assets/Resources/Art/Characters/fenn/fenn.fbx \
    --portrait unity/Assets/Resources/Art/fenn-hero.jpg

Rotate a captain in: character JSON + skins.json mesh/bind=rigid + this drop.
Missing FBX keeps SharedRig primitives.
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
    body.name = "body"
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
    """Place hero-shared bones *inside* this mesh. A T-pose humanoid in a
    crouched toy is what shredded Fenn — arms weighted to bones in the shell.
    """
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

    print("fit head", tuple(head_c), "torso", tuple(torso_c))
    print("fit larm", tuple(larm_c), "rarm", tuple(rarm_c))
    print("fit lfoot", tuple(lfoot_c), "rfoot", tuple(rfoot_c))

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
    return arm_ob


def skin_mesh(body, arm_ob):
    """Heat weights with bones already inside the toy. Rest pose = this mesh."""
    for mod in list(body.modifiers):
        body.modifiers.remove(mod)
    for g in list(body.vertex_groups):
        body.vertex_groups.remove(g)
    bpy.ops.object.select_all(action="DESELECT")
    body.select_set(True)
    arm_ob.select_set(True)
    bpy.context.view_layer.objects.active = arm_ob
    bpy.ops.object.parent_set(type="ARMATURE_AUTO")
    bpy.context.view_layer.objects.active = body
    try:
        bpy.ops.object.vertex_group_limit_total(limit=4)
    except Exception:
        pass
    print("skin groups", [g.name for g in body.vertex_groups])


def finish_rig(arm_ob, body):
    """Bone roll so local X is a swing axis. Outward normals so URP Lit does not show the inside."""
    bpy.context.view_layer.objects.active = arm_ob
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.armature.select_all(action="SELECT")
    try:
        bpy.ops.armature.calculate_roll(type="GLOBAL_POS_Z")
    except Exception as ex:
        print("roll skip", ex)
    bpy.ops.object.mode_set(mode="OBJECT")
    bpy.context.view_layer.objects.active = body
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.normals_make_consistent(inside=False)
    bpy.ops.object.mode_set(mode="OBJECT")


def rigid_parent(body, arm_ob):
    """Mesh follows the actor root. No limb weights."""
    for mod in list(body.modifiers):
        body.modifiers.remove(mod)
    for g in list(body.vertex_groups):
        body.vertex_groups.remove(g)
    body.parent = arm_ob
    body.parent_type = "OBJECT"
    body.parent_bone = ""
    body.location = (0.0, 0.0, 0.0)
    body.rotation_euler = (0.0, 0.0, 0.0)


def albedo_image(body):
    """Principled Base Color map, else the largest color image (not the ORM pack)."""
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
    p.add_argument("--faces", type=int, default=28000)
    p.add_argument("--bind", default="skinned", choices=("skinned", "rigid"))
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

    body = join_meshes()
    body.name = args.id
    stand_on_origin(body)
    faces = decimate(body, args.faces)
    print("faces after decimate", faces, "verts", len(body.data.vertices))
    arm = fit_armature(body)
    finish_rig(arm, body)
    if args.bind == "rigid":
        rigid_parent(body, arm)
    else:
        skin_mesh(body, arm)
    albedo = save_albedo(body, out.parent / (args.id + "-albedo.png"), size=1024)
    export_fbx(out)
    if args.resources:
        dest = Path(args.resources)
        dest.parent.mkdir(parents=True, exist_ok=True)
        if dest.resolve() != out.resolve():
            shutil.copy2(out, dest)
        if albedo and albedo.is_file():
            shutil.copy2(albedo, dest.parent / albedo.name)
        print("WROTE", dest)
    if args.portrait:
        render_portrait(Path(args.portrait))
    print("WROTE", out)


if __name__ == "__main__":
    main()
