---
description: Session split, load the agent spec, do not mix play and look, do not pass human gates.
alwaysApply: true
---

# Agentic rails

Contract: `docs/agent-rails.md`. Playbook: `docs/playbook.md`.

- Declare the session kind (gameplay / presentation / art) and stay in its file list. Mixing them is a patch: shrinking a mesh to save a camera, putting an out in Unity, posing in C#.
- Gameplay owns `data/rules/` and the sim. Presentation owns cameras, HUD, and the book. Art owns one catalog slot and a Blender script. 3D is Python; a `.blend` is not the source.
- Load `docs/agent-rails.md` (#647). Once R2 (#649) ships, load `data/agent/debug-protocol.json` for the kind you are in. Append a signature when you repair a novel failure.
- End the session with the artifact of its kind: `cli match` / scenarios, a named shot, or still PNGs. Do not rebuild the `.app` as proof of look.
- Agents do not pass human gates (#346, #209 sittings, #188). A critic files diffs. Jack passes.
- Do not start a prompt-to-game engine, a second skeleton, Meshy heroes, PhysX/NavMesh baseball, or Unity gameplay skills as the sim.
