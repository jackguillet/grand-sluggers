"""Headless clay renders and contact sheets for authoring checks.

Workbench, studio light, material colors. Front is Blender -Y (Unity +Z), so
the front camera stands at -Y and looks at the chest. Sheets are tiled with
numpy so nothing outside Blender is needed.
"""
from __future__ import annotations

from pathlib import Path

import bpy
import numpy as np
from mathutils import Vector


VIEWS = {
    # name: (camera location, look-at, lens)
    "front": ((0.0, -14.0, 3.0), (0.0, 0.0, 2.4), 45),
    "left": ((14.0, 0.0, 3.0), (0.0, 0.0, 2.4), 45),
    "right": ((-14.0, 0.0, 3.0), (0.0, 0.0, 2.4), 45),
    "three-quarter": ((9.0, -11.0, 4.0), (0.0, 0.0, 2.4), 45),
    "three-quarter-right": ((-9.0, -11.0, 4.0), (0.0, 0.0, 2.4), 45),
    "back": ((0.0, 14.0, 3.0), (0.0, 0.0, 2.4), 45),
}


def mirror_view(name: str) -> str:
    """The same view from the other side, for a reflected take."""
    return {"three-quarter": "three-quarter-right", "three-quarter-right": "three-quarter",
            "left": "right"}.get(name, name)


def _camera(name: str):
    ob = bpy.data.objects.get("clay-cam")
    if ob is None:
        cam = bpy.data.cameras.new("clay-cam")
        ob = bpy.data.objects.new("clay-cam", cam)
        bpy.context.collection.objects.link(ob)
    loc, look, lens = VIEWS[name]
    ob.location = loc
    ob.rotation_euler = (Vector(look) - Vector(loc)).to_track_quat("-Z", "Y").to_euler()
    ob.data.lens = lens
    return ob


def setup(width: int = 360, height: int = 480):
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "MATERIAL"
    scene.display.shading.show_shadows = True
    scene.render.resolution_x = width
    scene.render.resolution_y = height
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False


def render(path: Path, view: str = "three-quarter", width: int = 360, height: int = 480):
    setup(width, height)
    scene = bpy.context.scene
    scene.camera = _camera(view)
    path.parent.mkdir(parents=True, exist_ok=True)
    scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)
    return path


def _pixels(path: Path):
    img = bpy.data.images.load(str(path), check_existing=False)
    w, h = img.size
    px = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)
    bpy.data.images.remove(img)
    return px


def sheet(tiles: list[Path], out: Path, columns: int = 4):
    """Tile rendered PNGs left-to-right, top-to-bottom into one PNG."""
    if not tiles:
        return None
    images = [_pixels(t) for t in tiles]
    h, w = images[0].shape[:2]
    rows = (len(images) + columns - 1) // columns
    canvas = np.zeros((rows * h, columns * w, 4), dtype=np.float32)
    canvas[..., 3] = 1.0
    for i, px in enumerate(images):
        r, c = divmod(i, columns)
        # Blender image rows run bottom-to-top.
        y0 = (rows - 1 - r) * h
        canvas[y0:y0 + h, c * w:(c + 1) * w] = px
    img = bpy.data.images.new("clay-sheet", width=columns * w, height=rows * h, alpha=True)
    img.pixels = canvas.ravel().tolist()
    out.parent.mkdir(parents=True, exist_ok=True)
    img.filepath_raw = str(out)
    img.file_format = "PNG"
    img.save()
    bpy.data.images.remove(img)
    return out
