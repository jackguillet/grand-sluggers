---
name: character-art
description: Change the shared body, a captain's extras, or a take (Blender scripts, clips.json, extras.json, skins.json, stills). Use when a character looks or moves wrong.
---

# Character art

Contract: `docs/character-motion.md`. Stills: `docs/screenshot-gate.md`.

## Stop

Do not add a bone, a second rig, a procedural pose in C#, a runtime mirror, a cap, or a per-captain branch. Do not declare look done from tests, the DCC bake, or a rebuilt `.app`.

## Where a change goes

| Wrong thing | Fix in |
| --- | --- |
| a body proportion | `Silhouette.Proportions` (root scale) or `tools/blender/hero_shared_blockout.py` |
| a face, toe, landmark | `hero_shared_blockout.py` |
| a captain's hat, snout, cape | `hero_shared_extras.py` + `data/art/extras.json` + `data/art/skins.json` |
| a pose or timing | `hero_shared_takes.py` pose table; markers in `Motion.Clips` and `data/art/clips.json` |
| the swing grip or stance | `SwingPresentation.Keys` / `data/art/batting-stance.json`, then re-bake |
| which hand plays which file | `Motion.ClipFile` (do not special-case a captain) |

## Loop

1. Edit the script or table. Bake with `--clay` / `--sheets` and look at the sheet yourself.
2. `dotnet test`, `dotnet run --project src/GrandSluggers.Cli -- art`, `tools/unity-compile.sh` print OK.
3. Swing or stance change: run the Unity swing matrix (`docs/screenshot-gate.md`, `-executeMethod`), both hands.
4. Capture `tools/still-gate-character.sh {id}`; copy PNGs to `scratchpad/`; assemble before/after side by side; **stop**. Jack passes look.
