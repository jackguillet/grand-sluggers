---
description: Session split, load the agent spec, do not mix play and look, do not pass human gates.
alwaysApply: true
---

# Agentic rails

Contract: `docs/agent-rails.md`. Playbook: `docs/playbook.md`.

- Declare the session kind (gameplay / presentation / art) and stay in its file list. Mixing them is a patch: shrinking a mesh to save a camera, putting an out in Unity, posing in C#.
- Gameplay owns `data/rules/` and the sim. Presentation owns cameras, HUD, and the book. Art owns one catalog slot and a Blender script. 3D is Python; a `.blend` is not the source.
- Start from "Start here" in `AGENTS.md`. Load `data/agent/debug-protocol.json` for the kind you are in; look up `docs/agent-rails.md` (#647) when the work needs it. Append a signature only when you repair a novel failure, in the same PR as the fix; a repeat or a PR name is not a row. On the second firing, promote it to a test.
- What a PR owes (`docs/agent-rails.md` §1.2): never run the full test suite locally; run `tools/test-fast.sh <Classes you touched>`. Done = it compiles, the CI breakage suite is green, and the human gates that apply are noted. The full test suite (Full tests on GitHub too), balance, cohorts, flight probes, evidence seals and C80 parity run only when Jack asks for a balance pass; a tuning PR does not trigger them. A feature PR does not reseal, edit a `trials/` twin, or edit a decision register or ledger. A behavior change updates its rule in `docs/gameplay-spec.md` in the same PR, with no PR-number provenance. A procedural lesson grows `.grok/skills/character-art/` or the spec, not only the PR body.
- End the session with the artifact of its kind: `cli match` / scenarios (`--trace` for tick JSON of ball / runner / glove / bag), a named shot, or dual stills in `scratchpad/stills/` (`tools/dcc-still.sh` + `tools/still-gate-character.sh`). Art walks `data/agent/dcc-stages.json` (`cli stages`): blocking → fill → motion → export → still. One-shotting a captain extra or a kit mesh is a patch. Do not rebuild the `.app` as proof of look.
- Agents do not pass human gates (#346, #209 sittings, #188). A look-critic files diffs. It cannot mark #188 done. Jack passes.
- Do not start a prompt-to-game engine, a second skeleton, Meshy heroes, PhysX/NavMesh baseball, or Unity gameplay skills as the sim.
