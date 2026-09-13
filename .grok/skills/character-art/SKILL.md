---
name: character-art
description: Change the shared body, a captain's extras, or a take (Blender scripts, clips.json, extras.json, skins.json, stills). Use when a character looks or moves wrong.
---

# Character art

Contract: `docs/character-motion.md`. Stills: `docs/screenshot-gate.md`. Dual stills: `data/agent/dual-stills.json` (#651).

## Stop

Do not add a bone, a second rig, a procedural pose in C#, a runtime mirror, a cap, or a per-captain branch. Do not declare look done from tests, the DCC bake, or a rebuilt `.app`. File both stills, spawn look-critic, **stop**. Jack passes look.

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

1. Edit the script or table.
2. Capture the **DCC still**: `tools/dcc-still.sh body|extras|takes [clip]|harbor`. Named PNG: `scratchpad/stills/dcc-body.png` (or `dcc-extras.png` / `dcc-{clip}.png` / `dcc-harbor-kit.png`).
3. `dotnet test`, `dotnet run --project src/GrandSluggers.Cli -- art`, `tools/unity-compile.sh` print OK. These are not a still.
4. Capture the **in-game still**: `tools/still-gate-character.sh {id}` (Harbor kit: `tools/still-gate.sh`). Named PNGs: `scratchpad/stills/char-{id}-rest.png` and `char-{id}-pose.png`.
5. Swing or stance change: also run the Unity swing matrix (`docs/screenshot-gate.md`).
6. Link both PNGs in the PR. Spawn **look-critic** (read-only). It files diffs; it cannot mark #188 done. **Stop**.
