# Character package

How a unique toy gets into Grand Sluggers and stays a toy: painted, addable, removable, limbs that move.

This is the contract. `data/art/skins.json` + `drop_character.py` + a Unity prefab are the living fill.

## What we will not do

- Auto-weight a posed GLB onto a T-pose humanoid (bones in the shell). That shreds the mesh.
- Drive a unique rest pose with Rio’s MoveBones eulers (`Q(e) * bind`). That folds the turtle through itself.
- Freeze the mesh to stop tearing. That is a statue.
- Rely on embedded FBX Standard materials in URP. They render white.

## The unit is a package, not an FBX string

```
unity/Assets/Art/Characters/{id}/
  {id}.fbx              mesh + armature, rest pose = idle
  {id}-albedo.png       1024 base color
  {id}.mat / {id}.prefab   editor import (playable unit)
unity/Assets/Resources/Art/Characters/{id}/   player copies of fbx + albedo
data/characters/{id}.json
data/art/skins.json     mesh + bind: skinned
```

Rotate in: JSON + drop script. Rotate out: clear `mesh` (primitives) or delete the folder.

## Blender

Art is made in Blender. Unrigged GLB is allowed as a *source*; the exporter builds a Generic armature whose **bone names match** `data/art/rig.json`, placed **inside this mesh**, heat-weighted, normals outward, bone roll `GLOBAL_POS_Z` so local X is flexion.

```bash
/opt/homebrew/bin/blender --background --python tools/blender/drop_character.py -- \
  --src /path/to/hero.glb --id {id} --bind skinned \
  --out unity/Assets/Art/Characters/{id}/{id}.fbx \
  --resources unity/Assets/Resources/Art/Characters/{id}/{id}.fbx \
  --portrait unity/Assets/Resources/Art/{id}-hero.jpg
```

`--bind rigid` is a statue (debug only).

A human rigger can open the FBX in Blender, weight-paint, and re-export. Same bone names. That is the quality path.

## Runtime

- SharedRig primitives and `hero-shared` extras still use **MoveBones** (Rio’s axes, `Q(e)*bind`).
- A `bind: skinned` drop uses **CharacterMotion**: local flexion `bind * Q(e)`. Idle / walk / run / swing / pitch / scoop / throw / slide. Authored clips on this rig still win when present.
- Albedo is a sidecar PNG assigned as URP Lit. Never trust the embedded FBX material.

## Authored clips (later, same slot)

Name Blender actions `idle` `walk` `run` `swing` `pitch` `scoop` `throw` `slide` on **this** armature. Export as `Assets/Art/Animation/Clips/{id}.fbx` only when the clip is shared; per-character takes live next to the body as `{id}-{verb}.fbx` when we need unique motion. Until then CharacterMotion is the limb rail.

## Tools we do not need yet

| Tool | When |
| --- | --- |
| Auto-Rig Pro (Blender, paid) | A rigger is weight-painting a dozen unique toys |
| Mixamo / AccuRIG | Humanoid scans only — not a turtle |
| Unity Animation Rigging | IK polish after clips exist |
| glTFast | If we preview GLB in-editor; FBX remains the player format |

Nothing extra is required to drop Fenn or the next GLB.
