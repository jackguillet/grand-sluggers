---
name: character-art
description: Change the shared body, a captain's extras, or a take (Blender scripts, clips.json, extras.json, skins.json, stills). Use when a character looks or moves wrong.
---

# Character art

Contract: `docs/character-motion.md`. Stills: `docs/screenshot-gate.md`. Dual stills: `data/agent/dual-stills.json` (#651). Stages: `data/agent/dcc-stages.json` (#653). Load with `cli stages`.

## Stop

Do not add a bone, a second rig, a procedural pose in C#, a runtime mirror, a cap, or a per-captain branch. Do not declare look done from tests, the DCC bake, or a rebuilt `.app`. File both stills, spawn look-critic, **stop**. Jack passes look.

**One-shotting a captain extra or a Harbor kit mesh is banned.** Walk the stages.

## Process startup

Use `tools/blender-run.sh` (also used by `tools/dcc-still.sh`). On macOS, the agent sandbox can return no Metal devices and Blender can crash before Python starts. Run with approved GPU access outside the sandbox; a failed preflight means stop and change execution permissions, not retry. Preserve the user's existing Blender window. See `docs/editor-startup.md`.

## Stages

Art sessions walk named checkpoints. Name the stage, stop at its checkpoint, and save (the script edit + the still). The next prompt names the stage it continues. Do not jump to export or still to "just make the still pass." Existing bake flags (`--clay`, `--sheets`, `--out`) still run. A `.blend` is a cache, not the source.

1. **blocking**: `hero_shared_blockout.py` silhouette (Harbor: diamond / wall ring volumes). `--clay`. Checkpoint: `scratchpad/takes/body.png` (Harbor: `harbor-kit.png`).
2. **fill**: extras from `hero_shared_extras.py` + `extras.json` (Harbor: kit slots from `harbor_kit.py`). `--clay`. Checkpoint: `extras.png` / `harbor-kit.png`.
3. **motion**: takes from `hero_shared_takes.py` (Harbor has no takes). `--sheets`. Checkpoint: `{clip}.png`. The takes script refuses a take that misses its contract; extend the falsifier, do not loosen it. A left-handed batter or thrower plays `{clip}-L.fbx`, the baked reflection.
4. **export**: `--out` into the catalog slot. Do not skip here from a one-shot mesh.
5. **still**: dual stills (R4):
   1. Capture the **DCC still**: `tools/dcc-still.sh body|extras|takes [clip]|harbor`. Named PNG: `scratchpad/stills/dcc-body.png` (or `dcc-extras.png` / `dcc-{clip}.png` / `dcc-harbor-kit.png`).
   2. `tools/test-fast.sh <Classes you touched>`, `dotnet run --project src/GrandSluggers.Cli -- art`, `tools/unity-compile.sh` print OK. These are not a still.
   3. Capture the **in-game still**: `tools/still-gate-character.sh {id}` (Harbor kit: `tools/still-gate.sh`). Named PNGs: `scratchpad/stills/char-{id}-rest.png` and `char-{id}-pose.png`.
   4. Swing or stance change: also run the Unity swing matrix (`docs/screenshot-gate.md`).
   5. Link both PNGs in the PR. Spawn **look-critic** (read-only). It files diffs; it cannot mark #188 done. **Stop**.

## Where a change goes

| Wrong thing | Fix in |
| --- | --- |
| a body proportion | `Silhouette.Proportions` (root scale) or `tools/blender/hero_shared_blockout.py` |
| a face, toe, landmark | `hero_shared_blockout.py` |
| a captain's hat, snout, cape | catalog slot in `extras.json` / `hero_shared_extras.py`. Do not list it on a skin until it reads as a toy (#687) |
| a pose or timing | `hero_shared_takes.py` pose table; markers in `Motion.Clips` and `data/art/clips.json` |
| the swing grip or stance | `SwingPresentation.Keys` / `data/art/batting-stance.json`, then re-bake |
| which hand plays which file | `Motion.ClipFile` (do not special-case a captain) |

## Distill (from failed stills)

After a sitting or a failed still: **file** the child under the epic that owns the lie, **append** a `data/agent/debug-protocol.json` row in the same PR as the fix, and on the **second** firing **promote** the signature to a validator or a scenario. Do not wait for a third. A procedural lesson (how to look, how to bake) goes here, not only the PR body.

### Bat through the head — `swing-*-max-load` (#623, `bat-through-head`)

The charge windup still `swing-{rio,brondo}-max-load` put the barrel through the skull. The DCC validator checked hands on the handle, not bat versus head. The sim test that required the loaded barrel to rise pushed it upward. The take baked, tested, and captured green with the bat inside the head.

Lesson: **extend the falsifier**, do not loosen it. `BatHeadClearance` refuses a take that misses it; sim samples both takes, both hands, and the whole charge-up (`SwingPresentationTests.TheBatClearsTheHeadOnEverySampleOfBothTakesAndTheWholeChargeUp`). Do not pose the bat in C#. Tune the windup key in `hero_shared_takes.py` / `data/art/swing-takes.json`. Do not shrink the head to save the still.

### Bat hidden behind the head at ready — `swing-*-normal-ready` (#560, `bat-behind-head-at-ready`)

From the plate SET the ready barrel sat inside the head disk. The take met `SwingPresentation.Keys` and `BatHeadClearance` — 3D-beside is not the same as beside on the plate camera. Tuning `shots.json` cannot pull the barrel out inside the SET constraints (`PlateIsBehindHomeLookingAtThePitcher`).

Lesson: **lean the ready key out**, do not retune the plate camera and do not shrink the head. `PlateLoadedBesideDeg` is the falsifier (`SwingPresentationTests.TheLoadedBarrelSitsBesideTheHeadOnThePlateCamera`). The charge MAX windup already stood beside (#623); ready has to as well.

### Extras that read as geometry junk — skins list none (#687, `extras-are-geometry-junk`)

Brondo and Konga grew huge circles/squares at their feet (`cube-chest`, `snout`, `belly` on oversized `extras.fbx`). Ashlord's cape was an orange rectangle on his back. Jack's sitting: identity is palette + `Silhouette.Proportions` only.

Lesson: **leave extras off the skin** until they read as toys. Catalog slots stay. Do not shrink Ashlord to hide the cape. Do not bring caps back as geometry (#557). `cli art` fails any skin that lists an extra (`ArtCatalogTests.NoSkinListsAnExtraUntilTheyReadAsToys`).
