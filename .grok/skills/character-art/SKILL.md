---
name: character-art
description: Change the shared body, a captain's extras, or a take (Blender scripts, clips.json, extras.json, skins.json, stills). Use when a character looks or moves wrong.
---

# Character art

Contract: `docs/character-motion.md`. Stills: `docs/screenshot-gate.md`. Dual stills: `data/agent/dual-stills.json` (#651). Stages: `data/agent/dcc-stages.json` (#653). Load with `cli stages`.

## Stop

Do not add a bone, a second rig, a procedural pose in C#, a runtime mirror, a cap, or a per-captain branch. Do not declare look done from tests, the DCC bake, or a rebuilt `.app`. File both stills, spawn look-critic, **stop**. Jack passes look.

**One-shotting a captain extra or a Harbor kit mesh is banned.** Walk the stages. The next prompt names the stage it continues.

## Stages

Art sessions walk named checkpoints. Save after each (the script edit + the still). Do not jump to export or still to "just make the still pass."

| # | Stage | Character | Harbor kit | Checkpoint |
| --- | --- | --- | --- | --- |
| 1 | **blocking** | `hero_shared_blockout.py` silhouette | diamond / wall ring volumes | `--clay` sheet (`scratchpad/takes/body.png` / `harbor-kit.png`) |
| 2 | **fill** | extras from `hero_shared_extras.py` + `extras.json` | kit slots from `harbor_kit.py` | `--clay` sheet (`extras.png` / `harbor-kit.png`) |
| 3 | **motion** | takes from `hero_shared_takes.py` | — (Harbor has no takes) | `--sheets` (`{clip}.png`) |
| 4 | **export** | FBX into the catalog slot | FBX into the catalog slot | `--out` |
| 5 | **still** | DCC still + in-game character still (R4) | in-game park still | `scratchpad/stills/` dual stills |

Existing bake flags (`--clay`, `--sheets`, `--out`) still run. This rail does not retarget the rig or add a take. A `.blend` is a cache, not the source.

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

Name the stage. Stop at its checkpoint. The next prompt continues from that stage.

1. **blocking** — edit `hero_shared_blockout.py` (Harbor: diamond / wall ring volumes). `--clay`. Checkpoint: `scratchpad/takes/body.png` (Harbor: `harbor-kit.png`).
2. **fill** — extras / kit slots. `--clay`. Checkpoint: `extras.png` / `harbor-kit.png`.
3. **motion** — takes (skip for Harbor). `--sheets`. Checkpoint: `{clip}.png`.
4. **export** — `--out` into the catalog slot. Do not skip here from a one-shot mesh.
5. **still** — dual stills (R4):
   1. Capture the **DCC still**: `tools/dcc-still.sh body|extras|takes [clip]|harbor`. Named PNG: `scratchpad/stills/dcc-body.png` (or `dcc-extras.png` / `dcc-{clip}.png` / `dcc-harbor-kit.png`).
   2. `dotnet test`, `dotnet run --project src/GrandSluggers.Cli -- art`, `tools/unity-compile.sh` print OK. These are not a still.
   3. Capture the **in-game still**: `tools/still-gate-character.sh {id}` (Harbor kit: `tools/still-gate.sh`). Named PNGs: `scratchpad/stills/char-{id}-rest.png` and `char-{id}-pose.png`.
   4. Swing or stance change: also run the Unity swing matrix (`docs/screenshot-gate.md`).
   5. Link both PNGs in the PR. Spawn **look-critic** (read-only). It files diffs; it cannot mark #188 done. **Stop**.

## Distill (from failed stills)

After a sitting or a failed still: **file** the child under the epic that owns the lie, **append** a `data/agent/debug-protocol.json` row in the same PR as the fix, and on the **second** firing **promote** the signature to a validator or a scenario. Do not wait for a third. A procedural lesson (how to look, how to bake) goes here, not only the PR body.

### Bat through the head — `swing-*-max-load` (#623, `bat-through-head`)

The charge windup still `swing-{rio,brondo}-max-load` put the barrel through the skull. The DCC validator checked hands on the handle, not bat versus head. The sim test that required the loaded barrel to rise pushed it upward. The take baked, tested, and captured green with the bat inside the head.

Lesson: **extend the falsifier**, do not loosen it. `BatHeadClearance` refuses a take that misses it; sim samples both takes, both hands, and the whole charge-up (`SwingPresentationTests.TheBatClearsTheHeadOnEverySampleOfBothTakesAndTheWholeChargeUp`). Do not pose the bat in C#. Tune the windup key in `hero_shared_takes.py` / `data/art/swing-takes.json`. Do not shrink the head to save the still.

### Bat hidden behind the head at ready — `swing-*-normal-ready` (#560, `bat-behind-head-at-ready`)

From the plate SET the ready barrel sat inside the head disk. The take met `SwingPresentation.Keys` and `BatHeadClearance` — 3D-beside is not the same as beside on the plate camera. Tuning `shots.json` cannot pull the barrel out inside the SET constraints (`PlateIsBehindHomeLookingAtThePitcher`).

Lesson: **lean the ready key out**, do not retune the plate camera and do not shrink the head. `PlateLoadedBesideDeg` is the falsifier (`SwingPresentationTests.TheLoadedBarrelSitsBesideTheHeadOnThePlateCamera`). The charge MAX windup already stood beside (#623); ready has to as well.
