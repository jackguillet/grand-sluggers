---
description: Unique captains are DCC Generic packages. Do not heat-weight posed GLBs.
globs: unity/Assets/Art/Characters/**,tools/blender/**,data/art/skins.json,docs/character-package.md
alwaysApply: false
---

# Characters

Spec: `docs/character-package.md`. Procedure: `.grok/skills/character-art/`. Stills: `docs/screenshot-gate.md`.

- A posed unrigged GLB is a **source**, not a player mesh.
- Unique anatomy: Unity **Generic** + clips on **that** armature. Not Mixamo. Not Rio `MoveBones` / `swing.fbx`.
- Do not heat-weight a posed mesh. Do not freeze a SkinnedMeshRenderer to hide tearing.
- URP Lit from sidecar `{id}-albedo.png`. Embedded FBX Standard is white.
- Look is a human gate: `char-{id}-rest.png` and `char-{id}-pose.png` before a player rebuild. Tests are not a still.
