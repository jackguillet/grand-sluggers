---
name: character-art
description: Drop, rig, import, and verify Grand Sluggers characters (GLB, FBX, Fenn, bind, albedo, Unique Generic packages, Animator). Use when adding or changing a captain mesh, skins.json bind, drop_character.py, or character stills.
---

# Character art

Living spec: `docs/character-package.md`. Stills: `docs/screenshot-gate.md` (character rest + pose). Research: `docs/research-ai-characters.md`.

## Stop

Do not drop an unrigged posed GLB into `Assets/`. Do not heat-weight it. Do not freeze a SkinnedMeshRenderer to hide tearing. Do not play Rio `MoveBones` / `swing.fbx` on a unique rest pose. Do not declare look done from tests or a rebuilt `.app`.

A posed GLB is a **source**.

## Classify

- **Shared** (Rio six, role players): `hero-shared` + extras. Humanoid/shared clips. Stop here unless this ticket authors a package.
- **Unique Generic** (Fenn, future unique captains): own mesh, own rest pose, bone **names** from `data/art/rig.json`. Clips on **this** armature.

## Source

1. Inspect **untextured clay** (holes, intersections, pose). Color hides defects.
2. Riggable pose: **T-pose or A-pose**, or an armature with **painted** weights already in the file.
3. If the mesh is mid-action with no skeleton: **stop**. For Fenn, keep the turtle and skin it (`tools/blender/hero_fenn.py --src` the posed FBX). Bones go in that rest pose; shell/head never share an arm weight. Do not replace him with spheres. Mixamo/AccuRIG are humanoid only.
4. Rio six stay on `hero-shared` + extras until they are packages.

## Import

1. Export **FBX** (mesh + skeleton + clips). Not GLB as the player asset.
2. Unity Rig tab: **Generic**, Root node set, Avatar from this model. Humanoid only for T-pose bipeds.
3. Sidecar `{id}-albedo.png` → URP Lit (`_BaseMap`). Embedded Standard stays white.
4. Catalog: `data/characters/{id}.json` + `skins.json` `mesh` + `bind: skinned` (quality), `rigid` (posed authored mesh, statue), or `segmented` (Blender-authored pieces only — never a Python split of a posed GLB). Fenn is **rigid** until painted weights.
5. Prefab. Animator Controller when clips exist (`idle` `walk` `run` `swing` `pitch` `scoop` `throw` `slide` on this armature).

## Verify (then stop)

1. `dotnet test` and `dotnet run --project src/GrandSluggers.Cli -- art` print `OK`.
2. Capture with `tools/still-gate-character.sh {id}` (or menu **Grand Sluggers → Capture Character Stills**). Copy PNGs from `unity/Temp/gs-stills/` to `scratchpad/stills/`. **Do not pass the look gate.**
3. Do not rebuild the Mac player as proof.

Remove: delete JSON rows + `Assets/Art/Characters/{id}/` + Resources copy. Missing FBX keeps primitives.
