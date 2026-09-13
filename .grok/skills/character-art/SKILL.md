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

## Distill (from failed stills)

After a sitting or a failed still: **file** the child under the epic that owns the lie, **append** a `data/agent/debug-protocol.json` row in the same PR as the fix, and on the **second** firing **promote** the signature to a validator or a scenario. Do not wait for a third. A procedural lesson (how to look, how to bake) goes here, not only the PR body.

### Bat through the head — `swing-*-max-load` (#623, `bat-through-head`)

The charge windup still `swing-{rio,brondo}-max-load` put the barrel through the skull. The DCC validator checked hands on the handle, not bat versus head. The sim test that required the loaded barrel to rise pushed it upward. The take baked, tested, and captured green with the bat inside the head.

Lesson: **extend the falsifier**, do not loosen it. `BatHeadClearance` refuses a take that misses it; sim samples both takes, both hands, and the whole charge-up (`SwingPresentationTests.TheBatClearsTheHeadOnEverySampleOfBothTakesAndTheWholeChargeUp`). Do not pose the bat in C#. Tune the windup key in `hero_shared_takes.py` / `data/art/swing-takes.json`. Do not shrink the head to save the still.
