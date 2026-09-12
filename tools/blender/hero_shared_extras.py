#!/usr/bin/env python3
"""Captain extras and common props for hero-shared. One kit, not six skeletons.

Each extra is authored on the body where it belongs (Blender scene space, the
character facing -Y) with its origin at the world origin, and rendered to a
clay check. Unity drops the piece at the rig root and reparents it to the
socket bone named in data/art/extras.json, keeping the world pose, so nothing
here assumes how the importer orients a bone's local frame. Props (bat-wood,
glove-brown, baseball) keep their authored model origins.

No caps. Hats return later as accessories on the head socket.
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
import hero_shared_blockout as body  # noqa: E402

HEAD = body.HEAD

EXTRA_COLORS = {
    "ice": (0.85, 0.95, 1.0),
    "glass": (0.2, 0.85, 0.55),
    "sash": (0.75, 0.92, 1.0),
    "sneaker": (0.96, 0.96, 0.96),
    "ember": (1.0, 0.45, 0.12),
    "wood": (0.45, 0.28, 0.12),
    "grip": (0.18, 0.12, 0.08),
    "stitch": (0.86, 0.18, 0.16),
    "cream": (0.96, 0.93, 0.86),
}

# id -> socket bone. The same list is data/art/extras.json; the catalog test
# checks they agree.
SOCKETS = {
    "cheeks": "head",
    "sneakers": "lShin",
    "sash": "torso",
    "crown": "head",
    "neck": "torso",
    "goggles": "head",
    "cube-chest": "torso",
    "brick-jaw": "head",
    "snout": "head",
    "belly": "torso",
    "horns": "head",
    "cape": "torso",
    "ember-eyes": "head",
    "shell": "head",
    "staff": "torso",
}

PROPS = ("bat-wood", "glove-brown", "baseball")


def prim(kind, name, loc, scale, material, rot=(0.0, 0.0, 0.0)):
    return body.mesh_prim(kind, name, loc, scale, material, rot)


def join(name, pieces, authored_origin=None):
    bpy.ops.object.select_all(action="DESELECT")
    for ob in pieces:
        ob.select_set(True)
    bpy.context.view_layer.objects.active = pieces[0]
    bpy.ops.object.join()
    ob = pieces[0]
    ob.name = name
    bpy.context.scene.cursor.location = authored_origin if authored_origin is not None else (0.0, 0.0, 0.0)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    ob.location = (0.0, 0.0, 0.0)
    bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)
    return ob


def build_extras(m):
    """Extras in scene space on the body. Returns {id: object}."""
    out = {}
    y = HEAD.y
    z = HEAD.z

    cl = prim("uv_sphere", "CheekL", (0.52, y - 0.54, z - 0.25), (0.42, 0.42, 0.42), m["flesh"])
    cr = prim("uv_sphere", "CheekR", (-0.52, y - 0.54, z - 0.25), (0.42, 0.42, 0.42), m["flesh"])
    out["cheeks"] = join("cheeks", [cl, cr])

    # Authored on the left shin; the piece is symmetric so rShin takes the same mesh.
    shoe = prim("cube", "Shoe", (0.42, -0.36, 0.14), (0.86, 1.25, 0.46), m["sneaker"])
    toe = prim("uv_sphere", "Toe", (0.42, -0.86, 0.12), (0.72, 0.62, 0.44), m["sneaker"])
    stripe = prim("cube", "Stripe", (0.42, -0.30, 0.32), (0.50, 0.62, 0.12), m["trim"])
    out["sneakers"] = join("sneakers", [shoe, toe, stripe])

    out["sash"] = prim("cube", "sash", (0.06, -0.60, 2.32), (1.15, 0.10, 0.18), m["sash"],
                       rot=(0, math.radians(18), 0))

    band = prim("cylinder", "Band", (0, y, z + 0.76), (0.95, 0.95, 0.18), m["ice"])
    p0 = prim("cube", "P0", (0, y, z + 1.06), (0.16, 0.16, 0.42), m["ice"])
    p1 = prim("cube", "P1", (0.28, y, z + 0.99), (0.12, 0.12, 0.28), m["ice"])
    p2 = prim("cube", "P2", (-0.28, y, z + 0.99), (0.12, 0.12, 0.28), m["ice"])
    out["crown"] = join("crown", [band, p0, p1, p2])

    out["neck"] = prim("cylinder", "neck", (0, -0.05, 3.22), (0.42, 0.42, 0.85), m["flesh"])

    gl = prim("cylinder", "GogL", (0.42, y - 0.84, z + 0.14), (0.84, 0.84, 0.16), m["glass"], rot=(math.radians(90), 0, 0))
    gr = prim("cylinder", "GogR", (-0.42, y - 0.84, z + 0.14), (0.84, 0.84, 0.16), m["glass"], rot=(math.radians(90), 0, 0))
    br = prim("cube", "GogBridge", (0, y - 0.84, z + 0.14), (0.36, 0.12, 0.12), m["trim"])
    out["goggles"] = join("goggles", [gl, gr, br])

    out["cube-chest"] = prim("cube", "cube-chest", (0, -0.05, 2.28), (1.65, 1.15, 1.15), m["jersey"])
    out["brick-jaw"] = prim("cube", "brick-jaw", (0, y - 0.34, z - 0.62), (1.22, 0.48, 0.85), m["flesh"])

    sn = prim("uv_sphere", "SnoutBall", (0, y - 0.78, z - 0.20), (1.05, 0.70, 0.95), m["flesh"])
    nl = prim("uv_sphere", "NostrilL", (0.18, y - 1.18, z - 0.10), (0.18, 0.18, 0.18), m["ink"])
    nr = prim("uv_sphere", "NostrilR", (-0.18, y - 1.18, z - 0.10), (0.18, 0.18, 0.18), m["ink"])
    out["snout"] = join("snout", [sn, nl, nr])

    out["belly"] = prim("uv_sphere", "belly", (0, -0.50, 1.92), (1.28, 0.95, 1.12), m["jersey"])

    hl = prim("cylinder", "HornL", (0.48, y + 0.06, z + 0.66), (0.28, 0.28, 0.85), m["trim"], rot=(0, math.radians(22), 0))
    hr = prim("cylinder", "HornR", (-0.48, y + 0.06, z + 0.66), (0.28, 0.28, 0.85), m["trim"], rot=(0, math.radians(-22), 0))
    tl = prim("uv_sphere", "TipL", (0.65, y + 0.06, z + 1.05), (0.32, 0.32, 0.32), m["trim"])
    tr = prim("uv_sphere", "TipR", (-0.65, y + 0.06, z + 1.05), (0.32, 0.32, 0.32), m["trim"])
    out["horns"] = join("horns", [hl, hr, tl, tr])

    cape = prim("cube", "Cape", (0, 0.62, 2.00), (1.50, 0.16, 1.55), m["trim"])
    flare = prim("cube", "CapeFlare", (0, 0.68, 1.42), (1.70, 0.14, 0.55), m["trim"])
    out["cape"] = join("cape", [cape, flare])

    el = prim("uv_sphere", "EmberL", (0.30, y - 0.98, z + 0.12), (0.30, 0.22, 0.30), m["ember"])
    er = prim("uv_sphere", "EmberR", (-0.30, y - 0.98, z + 0.12), (0.30, 0.22, 0.30), m["ember"])
    out["ember-eyes"] = join("ember-eyes", [el, er])

    # Fenn: the shell is the brim. Green shell (jersey role), darker scutes (leather role).
    dome = prim("uv_sphere", "Shell", (0, y + 0.42, z + 0.55), (2.10, 1.65, 1.22), m["jersey"])
    lip = prim("uv_sphere", "ShellBrim", (0, y - 0.02, z + 0.27), (2.10, 1.48, 0.28), m["jersey"])
    sa = prim("cylinder", "ScuteA", (0, y + 0.66, z + 1.02), (0.68, 0.68, 0.14), m["leather"], rot=(math.radians(-28), 0, 0))
    sb = prim("cylinder", "ScuteB", (0.58, y + 0.58, z + 0.80), (0.46, 0.46, 0.12), m["leather"], rot=(math.radians(-30), 0, math.radians(22)))
    sc = prim("cylinder", "ScuteC", (-0.58, y + 0.58, z + 0.80), (0.46, 0.46, 0.12), m["leather"], rot=(math.radians(-30), 0, math.radians(-22)))
    out["shell"] = join("shell", [dome, lip, sa, sb, sc])

    # Fenn: the cane, slung across the back so it never collides with gear.
    cane = prim("cylinder", "Cane", (0, 0.62, 2.20), (0.10, 0.10, 1.30), m["wood"], rot=(0, math.radians(30), 0))
    knob = prim("uv_sphere", "CaneKnob", (0.325, 0.62, 2.76), (0.20, 0.20, 0.20), m["wood"])
    tip = prim("uv_sphere", "CaneTip", (-0.325, 0.62, 1.64), (0.14, 0.14, 0.14), m["gold"])
    out["staff"] = join("staff", [cane, knob, tip])

    missing = [k for k in SOCKETS if k not in out]
    extra = [k for k in out if k not in SOCKETS]
    if missing or extra:
        raise RuntimeError(f"extras/sockets mismatch: missing {missing} extra {extra}")
    return out


def build_props(m):
    """Common props. bat-wood keeps its measured origin: handle Y -1.00..-0.10,
    grip -0.85, barrel Y -0.15..1.25 at radius 0.12. Do not recenter it."""
    handle = prim("cylinder", "BatHandle", (0, 0, -0.55), (0.16, 0.16, 0.9), m["grip"])
    barrel = prim("cylinder", "BatBarrel", (0, 0, 0.55), (0.24, 0.24, 1.4), m["wood"])
    knob = prim("uv_sphere", "BatKnob", (0, 0, -1.05), (0.22, 0.22, 0.18), m["wood"])
    bat = join("bat-wood", [handle, barrel, knob], authored_origin=(0.0, 0.0, 0.0))
    palm = prim("uv_sphere", "Palm", (0, 0, 0), (0.7, 0.55, 0.42), m["leather"])
    web = prim("cube", "Web", (0, 0.12, 0.22), (0.55, 0.12, 0.42), m["leather"])
    thumb = prim("cylinder", "Thumb", (-0.32, 0.05, 0.08), (0.18, 0.18, 0.55), m["leather"], rot=(0, math.radians(28), 0))
    fingers = prim("cube", "Fingers", (0.12, 0.18, 0.06), (0.48, 0.22, 0.42), m["leather"])
    glove = join("glove-brown", [palm, web, thumb, fingers])
    # Diameter 1 (uv_sphere r=0.5 × scale 1) — Unity PrimitiveType.Sphere rest.
    # BallView localScale is Baseball.ApparentScale / ToyMesh.BaseballRestDiameter.
    ball = prim("uv_sphere", "BallBody", (0, 0, 0), (1.0, 1.0, 1.0), m["cream"])
    seam = prim("cube", "Seam", (0, 0, 0), (0.12, 0.94, 0.12), m["stitch"])
    baseball = join("baseball", [ball, seam])
    for ob in (glove, baseball):
        bpy.ops.object.select_all(action="DESELECT")
        ob.select_set(True)
        bpy.context.view_layer.objects.active = ob
        bpy.ops.object.origin_set(type="ORIGIN_GEOMETRY")
        ob.location = (0.0, 0.0, 0.0)
        bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)
    return {"bat-wood": bat, "glove-brown": glove, "baseball": baseball}


def clay_check(extras, folder: Path):
    import clay
    body_objects = [o for o in bpy.data.objects if o.type == "MESH" and o.name not in extras]
    tiles = []
    for name in sorted(extras):
        for other, ob in extras.items():
            ob.hide_render = other != name
        for prop in PROPS:
            if prop in bpy.data.objects:
                bpy.data.objects[prop].hide_render = True
        tiles.append(clay.render(folder / f"extra-{name}.png", "three-quarter", 300, 400))
    for ob in extras.values():
        ob.hide_render = False
    return clay.sheet(tiles, folder / "extras.png", columns=5)


def build(out: Path, resources: str, clay_folder: str):
    arm_ob = body.build_scene()
    m = {k: body.mat(k, v) for k, v in {**body.PALETTE, **EXTRA_COLORS}.items()}
    extras = build_extras(m)
    if clay_folder:
        sys.path.insert(0, str(Path(__file__).resolve().parent))
        print("clay", clay_check(extras, Path(clay_folder).resolve()))
    props = build_props(m)
    keep = set(extras) | set(props)
    for ob in list(bpy.data.objects):
        if ob.name not in keep:
            bpy.data.objects.remove(ob, do_unlink=True)
    out.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(
        filepath=str(out),
        use_selection=False,
        object_types={"MESH"},
        use_mesh_modifiers=True,
        bake_anim=False,
        **body.FBX_AXES,
    )
    names = sorted(o.name for o in bpy.data.objects if o.type == "MESH")
    print("exported", out, out.stat().st_size, "meshes", ",".join(names))
    body.copy_to_resources(out, resources)


def main(argv):
    p = argparse.ArgumentParser()
    p.add_argument("--out", required=True)
    p.add_argument("--resources", default="")
    p.add_argument("--clay", default="")
    args = p.parse_args(argv)
    build(Path(args.out).resolve(), args.resources, args.clay)


if __name__ == "__main__":
    argv = sys.argv
    if "--" in argv:
        argv = argv[argv.index("--") + 1 :]
    else:
        argv = argv[1:]
    main(argv)
