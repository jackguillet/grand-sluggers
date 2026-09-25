#!/usr/bin/env python3
"""Every captain side by side on the one rig: the body look-gate still (DCC half).

Each captain is hero-shared at its root scale (Silhouette.SharedRootScale) and
its build (the head / arms / torso shape keys, Silhouette.Build), in its
faction palette (Colors.cs, painted by material role as SharedRig paints it)
and in flat black, at the turnaround camera and at gameplay distance. Blender
units are world feet here. Captains stand in ladder order, shortest first.

  tools/blender-run.sh -b --python tools/blender/hero_lineup.py -- \
    --out scratchpad/stills [--repo <tree>] [--prefix dcc-lineup]

Writes {prefix}-turnaround.png (palette front and 3/4, labelled, then flat
black front), {prefix}-gameplay.png (palette, the plate camera's distance to
the mound, 1:1 pixels) and {prefix}-gameplay-black.png (flat black, same
camera, unlabelled: name them blind). `--repo` renders another checkout's body
and data (a before tree) with this script. Stage 5 still; Jack passes look.
"""
from __future__ import annotations

import argparse
import math
import re
import sys
from pathlib import Path

import bpy
import numpy as np
from mathutils import Vector

HERE = Path(__file__).resolve()

# Silhouette.ToyScale and SharedRootScale: the same arithmetic as the sim.
TOY_SCALE = 1.18

# Silhouette-bible turnaround: 14 ft out, 5.5 up, at the chest, vertical FOV 32.
TURN_DIST, TURN_Y, TURN_LOOK, TURN_FOV = 14.0, 5.5, 3.2, 32.0
TILE_W, TILE_H = 600, 960
# Gameplay distance: the plate camera (data/feel/shots.json "plate") to the
# pitcher is about sixty feet, six up, vertical FOV 50, full 1080p frame.
GAME_DIST, GAME_Y, GAME_LOOK, GAME_FOV = 60.0, 6.0, 2.6, 50.0
GAME_W, GAME_H = 1920, 1080
GAME_SPACING = 5.0

PALETTE_BG = (0.55, 0.62, 0.66)
BLACK_BG = (0.86, 0.86, 0.84)


def root_scale(p):
    """Unity root scale (x, y, z) for proportions p: Silhouette.SharedRootScale."""
    h, w = float(p["height"]), float(p["width"])
    return ((h * 0.45 + w * 0.55) * TOY_SCALE, h * TOY_SCALE, (h * 0.55 + w * 0.45) * TOY_SCALE)


def srgb_to_linear(c):
    return tuple(x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4 for x in c)


def lerp(a, b, u):
    return tuple(x + (y - x) * u for x, y in zip(a, b))


class Palette:
    """Faction colors read from unity/Assets/Scripts/Runtime/Colors.cs, painted by role as SharedRig.MaterialFor."""

    def __init__(self, colors_cs: Path):
        text = colors_cs.read_text()
        self.named = {}
        for name, hexv in re.findall(r"Color (\w+) = Hex\(0x([0-9A-Fa-f]{6})\)", text):
            v = int(hexv, 16)
            self.named[name] = (((v >> 16) & 255) / 255, ((v >> 8) & 255) / 255, (v & 255) / 255)
        for name, a, b, c in re.findall(r"Color (\w+) = new Color\(([\d.]+)f, ([\d.]+)f, ([\d.]+)f\)", text):
            self.named[name] = (float(a), float(b), float(c))
        self.body = self._switch(text, "Body")
        self.accent = self._switch(text, "Accent")
        skin = text[text.index("SkinTone(string faction)"):]
        skin = skin[:skin.index("};")]
        self.skin = {}
        for keys, value in re.findall(r"((?:\"\w+\"(?: or )?)+) => (\w+)", skin):
            for k in re.findall(r"\"(\w+)\"", keys):
                self.skin[k] = self.named[value]
        self.skin_default = self.named[re.search(r"_ => (\w+)", skin).group(1)]

    def _switch(self, text, method):
        block = text[text.index(f"Color {method}(string faction)"):]
        block = block[:block.index("default:")]
        out = {}
        for faction, value in re.findall(r"case \"(\w+)\": return ([^;]+);", block):
            m = re.match(r"new Color\(([\d.]+)f, ([\d.]+)f, ([\d.]+)f\)", value)
            out[faction] = (float(m.group(1)), float(m.group(2)), float(m.group(3))) if m else self.named[value]
        return out

    def roles(self, faction):
        body = self.body[faction]
        return {
            "jersey": body,
            "trim": self.accent[faction],
            "flesh": self.skin.get(faction, self.skin_default),
            "slack": lerp((1, 1, 1), body, 0.12),
            "leather": lerp(body, (0, 0, 0), 0.38),
            "gold": self.named["Gold"],
            "ink": (0.08, 0.07, 0.07),
            "white": (1, 1, 1),
        }


def read_captains(repo: Path, jsonc):
    captains = []
    for path in sorted((repo / "data/characters").glob("*.json")):
        doc = jsonc.load(path)
        if isinstance(doc, dict) and doc.get("captain") and doc.get("proportions"):
            captains.append({"id": doc["id"], "name": doc.get("name", doc["id"]), "faction": doc["faction"],
                             "proportions": doc["proportions"], "bodyClass": doc.get("bodyClass", "")})
    return captains


def camera(name, loc, look, fov_deg, w, h):
    ob = bpy.data.objects.get(name)
    if ob is None:
        ob = bpy.data.objects.new(name, bpy.data.cameras.new(name))
        bpy.context.collection.objects.link(ob)
    ob.location = loc
    ob.rotation_euler = (Vector(look) - Vector(loc)).to_track_quat("-Z", "Y").to_euler()
    ob.data.sensor_fit = "VERTICAL"
    ob.data.angle_y = math.radians(fov_deg)
    scene = bpy.context.scene
    scene.camera = ob
    scene.render.resolution_x = w
    scene.render.resolution_y = h
    scene.render.resolution_percentage = 100
    return ob


def setup_render(flat: bool, background):
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "FLAT" if flat else "STUDIO"
    scene.display.shading.color_type = "MATERIAL"
    scene.display.shading.show_shadows = False
    scene.display.shading.show_cavity = False
    scene.display.shading.show_object_outline = False
    scene.display_settings.display_device = "sRGB"
    scene.view_settings.view_transform = "Standard"
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.film_transparent = True
    if scene.world is None:
        scene.world = bpy.data.worlds.new("lineup")
    scene.world.color = srgb_to_linear(background)


def paint(roles: dict | None):
    """roles None: flat black silhouettes."""
    for m in bpy.data.materials:
        key = m.name.split(".")[0]
        if key.startswith("label"):
            continue
        color = (0.0, 0.0, 0.0) if roles is None else srgb_to_linear(roles.get(key, (0.5, 0.5, 0.5)))
        m.diffuse_color = (*color, 1.0)


def label(text: str, loc, size: float):
    curve = bpy.data.curves.new("label-" + text, "FONT")
    curve.body = text
    curve.align_x = "CENTER"
    curve.size = size
    ob = bpy.data.objects.new("label-" + text, curve)
    bpy.context.collection.objects.link(ob)
    ob.location = loc
    ob.rotation_euler = (math.radians(90), 0, 0)
    mat = bpy.data.materials.get("label") or bpy.data.materials.new("label")
    mat.diffuse_color = (0.02, 0.02, 0.02, 1.0)
    curve.materials.append(mat)
    return ob


def render(path: Path):
    bpy.context.scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)
    img = bpy.data.images.load(str(path), check_existing=False)
    w, h = img.size
    px = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)
    bpy.data.images.remove(img)
    return px


def over(canvas, layer):
    a = layer[..., 3:4]
    canvas[..., :3] = layer[..., :3] * a + canvas[..., :3] * (1 - a)
    return canvas


def background(h, w, color):
    canvas = np.ones((h, w, 4), dtype=np.float32)
    canvas[..., :3] = color
    return canvas


def save(px, path: Path):
    h, w = px.shape[:2]
    img = bpy.data.images.new(path.stem, width=w, height=h, alpha=True)
    img.pixels = px.ravel().tolist()
    img.filepath_raw = str(path)
    img.file_format = "PNG"
    img.save()
    bpy.data.images.remove(img)
    print("still", path)


def main(argv):
    p = argparse.ArgumentParser()
    p.add_argument("--out", required=True)
    p.add_argument("--repo", default=str(HERE.parents[2]), help="Checkout whose body script and data are drawn.")
    p.add_argument("--prefix", default="dcc-lineup")
    p.add_argument("--beat", default="idle", help="idle, run, stance, windup or signature: each captain in its motion style's beat.")
    args = p.parse_args(argv)
    repo = Path(args.repo).resolve()
    out = Path(args.out).resolve()
    out.mkdir(parents=True, exist_ok=True)
    work = out / (args.prefix + "-tiles")
    work.mkdir(parents=True, exist_ok=True)

    sys.path.insert(0, str(repo / "tools"))
    sys.path.insert(0, str(repo / "tools/blender"))
    import jsonc  # noqa: E402
    import hero_shared_blockout as body  # noqa: E402
    import hero_shared_takes as takes  # noqa: E402

    palette = Palette(HERE.parents[2] / "unity/Assets/Scripts/Runtime/Colors.cs")
    captains = read_captains(repo, jsonc)
    arm = body.build_scene()
    has_build = hasattr(body, "set_build")

    styles = getattr(takes, "STYLE_POSES", None)
    # A captain moves in its body class's style (data/rules/body-classes.json motionStyle).
    class_file = repo / "data/rules/body-classes.json"
    class_style = {r["id"]: r["motionStyle"] for r in jsonc.load(class_file)["classes"]} if class_file.exists() else {}
    by_body = {c["id"]: class_style.get(c.get("bodyClass", "")) for c in captains} if styles else {}

    def place(c, beat):
        """Pose the captain in its motion style's beat (the shared take on a tree without styles), at unit scale."""
        arm.scale = (1.0, 1.0, 1.0)
        where = arm.location.copy()
        arm.location = (0.0, 0.0, 0.0)
        style = by_body.get(c["id"]) if styles else None
        if has_build:
            try:
                body.set_build(body.build_scale(c["proportions"]), reset=True)
            except TypeError:  # a tree before the style channels
                body.set_build(body.build_scale(c["proportions"]))
        if styles:
            takes.use_style(style)
            body.set_build(body.build_scale(c["proportions"]))
        sp = styles[style] if style else None
        if beat == "idle":
            pz = takes.pose_at(sp["idle"] if sp else takes.IDLE, 0.0, True, True, 2.0)
            takes.apply_pose(arm, pz)
            if sp:
                takes.ground_hop(arm, pz)
        elif beat == "run":
            keys = takes._stride(1.0, takes.RUN_DUR, sp["gait"]) if sp else takes.RUN
            takes.apply_pose(arm, takes.pose_at(keys, takes.RUN_DUR * 0.1, True, True, takes.RUN_DUR))
        elif beat == "stance":
            takes.pose_swing_frame(arm, 0.075, "swing-charge")
        elif beat == "windup":
            takes.baseball_frame("pitch-charge")(arm, 0.0)
        elif beat == "signature":
            keys, dur = sp["signature"] if sp else (takes.CHEER, 0.8)
            pz = takes.pose_at(keys, dur / 2, True, True, dur)
            takes.apply_pose(arm, pz)
            if sp:
                takes.ground_hop(arm, pz)
        else:
            raise RuntimeError(f"unknown beat {beat}")
        if styles:
            takes.ACTIVE["style"] = None  # the pose is set; keep its build on the mesh
        scale = root_scale(c["proportions"])
        arm.scale = (scale[0], scale[2], scale[1])  # Unity (x, y, z) -> Blender (x, z, y)
        arm.location = where
        bpy.context.view_layer.update()

    def pose(c, beat="idle"):
        place(c, beat)
        deps = bpy.context.evaluated_depsgraph_get()
        top = 0.0
        for ob in bpy.data.objects:
            if ob.type != "MESH" or ob.name.startswith("label") or ob.hide_render:
                continue
            ev = ob.evaluated_get(deps)
            mesh = ev.to_mesh()
            top = max([top] + [(ev.matrix_world @ v.co).z for v in mesh.vertices])
            ev.to_mesh_clear()
        return top



    tops = {}
    for c in captains:
        tops[c["id"]] = pose(c)
    captains.sort(key=lambda c: tops[c["id"]])
    rio = tops.get("rio")
    print("LADDER", " ".join(f"{c['id']}={tops[c['id']]:.2f}ft" + (f"({tops[c['id']] / rio:.2f}xRio)" if rio else "")
                            for c in captains))

    # ---------------------------------------------------------- turnaround tiles
    rows = []
    for view, flat in (("front", False), ("three-quarter", False), ("front", True)):
        setup_render(flat, BLACK_BG if flat else PALETTE_BG)
        if view == "front":
            loc = (0.0, -TURN_DIST, TURN_Y)
        else:
            a = math.radians(40)
            loc = (TURN_DIST * math.sin(a), -TURN_DIST * math.cos(a), TURN_Y)
        tiles = []
        for c in captains:
            arm.location = (0, 0, 0)
            pose(c, args.beat)
            paint(None if flat else palette.roles(c["faction"]))
            tag = None if flat else label(f"{c['name']}  {tops[c['id']]:.2f} ft", (0, -1.6, -0.2), 0.34)
            if tag is not None and view != "front":
                tag.rotation_euler.z = math.radians(40)  # face the 3/4 camera
            camera("lineup-cam", loc, (0, 0, TURN_LOOK), TURN_FOV, TILE_W, TILE_H)
            px = render(work / f"{c['id']}-{view}-{'black' if flat else 'palette'}.png")
            tiles.append(over(background(TILE_H, TILE_W, BLACK_BG if flat else PALETTE_BG), px))
            if tag is not None:
                bpy.data.objects.remove(tag, do_unlink=True)
        rows.append(np.concatenate(tiles, axis=1))
    # Blender image rows run bottom-to-top: the first row goes on top.
    save(np.concatenate(rows[::-1], axis=0), out / f"{args.prefix}-turnaround.png")

    # ---------------------------------------------------------- gameplay distance
    for flat in (False, True):
        setup_render(flat, BLACK_BG if flat else PALETTE_BG)
        camera("lineup-cam", (0.0, -GAME_DIST, GAME_Y), (0.0, 0.0, GAME_LOOK), GAME_FOV, GAME_W, GAME_H)
        canvas = background(GAME_H, GAME_W, BLACK_BG if flat else PALETTE_BG)
        n = len(captains)
        for i, c in enumerate(captains):
            pose(c, args.beat)
            arm.location = ((i - (n - 1) / 2) * GAME_SPACING, 0, 0)  # +X is screen right from -Y
            paint(None if flat else palette.roles(c["faction"]))
            over(canvas, render(work / f"{c['id']}-game-{'black' if flat else 'palette'}.png"))
        arm.location = (0, 0, 0)
        # Crop to the band the toys stand in, at 1:1 pixels (rows run bottom-to-top).
        band = canvas[int(GAME_H * 0.40):int(GAME_H * 0.64), int(GAME_W * 0.25):int(GAME_W * 0.75)]
        save(band, out / f"{args.prefix}-gameplay{'-black' if flat else ''}.png")


if __name__ == "__main__":
    argv = sys.argv
    argv = argv[argv.index("--") + 1:] if "--" in argv else argv[1:]
    try:
        main(argv)
    except Exception:
        import traceback
        traceback.print_exc()
        sys.stdout.flush()
        sys.exit(1)
