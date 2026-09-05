# Character package

How a unique toy gets into Grand Sluggers and stays a toy: painted, addable, removable, limbs that move independently. Art is made in Blender.

This is the contract. `data/art/skins.json` + `tools/blender/drop_character.py` + a Unity Generic FBX are the living fill.

## What we will not do

- Heat-weight a posed GLB onto a T-pose humanoid. Bones in the shell shred the mesh.
- Drive a unique rest pose with Rio’s MoveBones eulers or shared clip FBX (`swing.fbx`). That folds the turtle through itself.
- Freeze the whole mesh to stop tearing. That is a statue.
- Rely on embedded FBX Standard materials in URP. They render white.
- Mixamo / AccuRIG as identity. Those are humanoid scans, not a turtle.

## The unit is a package, not an FBX string

```
data/characters/{id}.json
data/art/skins.json                 mesh + bind
unity/Assets/Art/Characters/{id}/
  {id}.fbx                          mesh pieces + named sockets, rest pose = idle
  {id}-albedo.png                   1024 base color (URP Lit)
  {id}.mat / {id}.prefab            editor import
unity/Assets/Resources/Art/Characters/{id}/   player copies of fbx + albedo
unity/Assets/Resources/Art/{id}-hero.jpg      portrait
```

**Add:** character JSON + skins.json row + drop script.
**Remove:** delete those rows and `Assets/Art/Characters/{id}/` (and the Resources copy). Missing FBX keeps SharedRig primitives — it must not crash.

Bone **names** (`data/art/rig.json`) are the contract: `torso` `head` `lUpper` `lFore` `rUpper` `rFore` `lThigh` `lShin` `rThigh` `rShin` `bat` `glove`. Rest pose, mesh, and weights are per character.

## Bind

| `skins.json` bind | What it is | When |
| --- | --- | --- |
| *(empty)* / `shared` | hero-shared + extras | Rio six and role players |
| `segmented` | Rigid pieces parented to sockets | Posed GLB/FBX with no painted weights. **Default auto path.** |
| `skinned` | SkinnedMeshRenderer, painted weights | Artist weight-painted in Blender. Quality path. Never the auto default. |
| `rigid` | Whole mesh frozen | Statue / debug only |

Segmented is how cartoon toys actually work: the shell does not deform; arms and legs rotate at the sockets. Connected heat-weights on a fused turtle is what inverted Fenn.

## Blender

```bash
/opt/homebrew/bin/blender --background --python tools/blender/drop_character.py -- \
  --src /path/to/hero.glb --id {id} --bind segmented \
  --out unity/Assets/Art/Characters/{id}/{id}.fbx \
  --resources unity/Assets/Resources/Art/Characters/{id}/{id}.fbx \
  --portrait unity/Assets/Resources/Art/{id}-hero.jpg
```

The drop:

1. Imports GLB / GLTF / FBX / OBJ.
2. Strips a previous fitted armature (re-drops are safe).
3. Stands the toy on Z=0 at catalog height.
4. Fits named sockets **inside this mesh**.
5. **Splits** the mesh into rigid pieces (shell stays torso; protruding limbs become arms/legs/head).
6. Parents each piece to its bone. Outward normals. Sidecar `{id}-albedo.png`.

`--bind skinned --keep-weights` only when the source already has painted groups you want to keep.

A rigger can open the FBX in Blender, join pieces, weight-paint, and re-export as `skinned`. Same bone names. That is the quality path.

## Runtime

- SharedRig primitives and `hero-shared` extras still use **MoveBones**.
- A unique package uses **CharacterMotion**: local flexion `bind * Q(e)` on this rest pose. Shared authored pose-clips and `swing.fbx` are not applied.
- Segmented pieces are MeshRenderers parented to bones at spawn. Rotating `lUpper` rotates the arm piece; the shell cannot invert.
- Albedo is a sidecar PNG assigned as URP Lit. Never trust the embedded FBX material.

## Authored clips (quality fill, same slot)

Name Blender actions `idle` `walk` `run` `swing` `pitch` `scoop` `throw` `slide` on **this** armature. Until those clips exist, CharacterMotion is the limb rail. Do not retarget Rio’s takes onto a unique rest pose.

## The original six

Rio, Vale, Zig, Brondo, Konga, Ashlord stay on `hero-shared` + extras until they are authored as packages. Role players still reuse the captain body type and must not grow captain extras.

## Tools

| Tool | Need it? |
| --- | --- |
| **Blender** | Yes. Source of truth. Already in the pipeline. |
| Weight painting in Blender | Only for `skinned` smooth deformation. Segmented does not need it. |
| Auto-Rig Pro (paid) | Optional later, if we paint a dozen unique fused meshes. Not required. |
| Mixamo / AccuRIG | No. Humanoid only. |
| Unity Animation Rigging | Optional later for IK (hand to bat). Not required for the rail. |
| glTFast | No. FBX remains the player format. |

Nothing extra is required to add Fenn or the next unique GLB.
