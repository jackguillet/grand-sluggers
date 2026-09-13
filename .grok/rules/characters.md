---
description: One rig, one body script, one takes script. No poses in C#, no runtime mirroring, no second skeleton.
globs: unity/Assets/Art/Characters/**,unity/Assets/Art/Animation/**,tools/blender/**,data/art/*.json,docs/character-motion.md
alwaysApply: false
---

# Characters

Contract: `docs/character-motion.md`. Procedure: `.grok/skills/character-art/`. Stills: `docs/screenshot-gate.md`.

- Every captain is `hero-shared` + extras from `data/art/extras.json`. Unique packages are deferred.
- Every verb is a take from `tools/blender/hero_shared_takes.py`. Do not put an Euler angle for a body part in C#.
- A left-handed batter or thrower plays `{clip}-L.fbx`, the baked reflection. Do not mirror a bone, socket, or sample at runtime.
- The takes script must refuse a take that misses its contract. Extend the falsifier, do not loosen it.
- No caps until hats return as accessories.
- Walk named DCC stages (`data/agent/dcc-stages.json`, `cli stages`): blocking → fill → motion → export → still. One-shotting a captain extra is a patch.
- Look is a human gate: DCC still (`dcc-*.png` from `tools/dcc-still.sh`) and in-game still (`char-{id}-rest.png` / `char-{id}-pose.png`) in `scratchpad/stills/` before a player rebuild. A look-critic files; Jack passes. Tests, the bake, and a rebuilt `.app` are not a still.
