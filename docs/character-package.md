# Character package

How a unique toy gets into Grand Sluggers: painted, addable, removable, limbs that move. Art is made in **Blender**. Unity presents it.

This is the contract. Research and citations: [research-ai-characters.md](research-ai-characters.md). Procedure for agents: `.grok/skills/character-art/`. Stills: [screenshot-gate.md](screenshot-gate.md).

## Unity’s contract (do not skip)

A playable character is already **modeled, rigged, and skinned** in a DCC, then imported:

1. **Mesh** — UVs, albedo, feet on origin, facing −Z in Unity.
2. **Armature** — named sockets from `data/art/rig.json` (`torso` `head` `lUpper` `lFore` `rUpper` `rFore` `lThigh` `lShin` `rThigh` `rShin` `bat` `glove`).
3. **Skin** — painted weights, or rigid pieces **authored in Blender** (not a Python split of a posed GLB).
4. **Export FBX** — mesh + skeleton + clips. FBX is the player format, not GLB.
5. **Unity import** — **Generic** for unique anatomy (turtle, ape extras that are not a human T-pose). **Humanoid** only for T-pose bipeds that share Mixamo-style clips. Prefab + **Animator Controller**. Even one clip lives in a controller.

Bind pose and T-pose are not the same thing. A posed import can map bones and still be wrong. Do not retarget Rio’s `swing.fbx` onto a unique rest pose.

## What we will not do

- Drop an unrigged posed GLB into `Assets/` as the player mesh.
- Heat-weight a posed mesh onto fitted or T-pose bones. That shreds and inverts the shell.
- Freeze a SkinnedMeshRenderer to hide tearing. That is a statue.
- Drive a unique rest pose with Rio `MoveBones` eulers or shared clip FBX.
- Trust embedded FBX Standard materials in URP. They render white. Sidecar `{id}-albedo.png` + URP Lit.
- Mixamo / AccuRIG as identity for unique toys. Humanoid scans only.
- Declare look done from `dotnet test`, `unity-compile.sh`, or a rebuilt `.app`.

A posed GLB is a **source**. It is not a Unity character.

## The unit is a package

```
data/characters/{id}.json
data/art/skins.json                 mesh + bind
unity/Assets/Art/Characters/{id}/
  {id}.fbx                          Generic armature + mesh, rest pose = idle
  {id}-albedo.png                   1024 base color (URP Lit)
  {id}.mat / {id}.prefab            editor import
  {id}.controller                   manifest-built Animator Controller
unity/Assets/Resources/Art/Characters/{id}/   player copies of fbx + albedo
unity/Assets/Resources/Art/{id}-hero.jpg      portrait
data/art/character-packages.json    verb sources, clocks, markers, readiness
```

**Add:** character JSON + skins.json row + a **rigged** FBX (Blender, or a generator only after T/A-pose + auto-rig that you then inspect in clay).
**Remove:** delete those rows and `Assets/Art/Characters/{id}/` (and the Resources copy). Missing FBX keeps SharedRig primitives — it must not crash.

Bone **names** stay the contract so bat, glove, and cameras work. Rest pose, mesh, and weights are per character.

## Bind

| `skins.json` bind | What it is | When |
| --- | --- | --- |
| *(empty)* / `shared` | hero-shared + extras | Rio six and role players |
| `skinned` | SkinnedMeshRenderer, **painted** weights, Generic Avatar | Quality path for unique captains. Clips on **this** armature. |
| `segmented` | Rigid pieces **authored in Blender** parented to sockets | Stopgap only. A Python split of a posed GLB shredded Fenn's face. Do not do that again. |
| `rigid` | Whole mesh frozen | Statue / debug. Do not ship: MoveBones still runs and stick limbs poke out under the mesh. |

Quality fill: Blender actions named `idle` `pose` `walk` `run` `swing` `pitch` `scoop` `throw` `slide` on **this** armature (`tools/blender/package_clips.py` for idle/pose). Body and takes must use the same FBX axis, scale, and baked-space settings. Prefer exporting them from one DCC scene; an FBX import/re-export with different settings can resolve every bone name while writing an incompatible bind basis. Unity plays those Generic takes. Until a verb has a take, `CharacterMotion` local flexion is a stand-in, not the ship pipeline.

## Verb manifest

`data/art/character-packages.json` is the runtime contract. Every package declares every `MoveBones.Verb`; adding an authored run, pitch, scoop, or throw means filling that verb's `source`, `playerSource`, and imported `clip`, then changing `readiness` from `fallback` to `ready`. Runtime selection does not need a new character or verb switch.

- `loop` is true for idle/walk/run and false for one-shot plays.
- `clock` is `world` for locomotion, `pose` for timed plays, and `charge` for held pitch/swing loads.
- `markers` carry `Contact`, `Release`, and `FootPlant` at the same seconds used by the play event.
- `fallback` is explicit even on a ready slot. A missing runtime resource falls back to `character-motion`; validation rejects the missing ready import before a player build.

The authoring and `Resources` FBX copies are separate declared sources so the validation gate proves what ships. Each package also has tracked authoring and player `.controller` assets. `Grand Sluggers → Sync Character Package Controllers` derives their states from the ready rows and assigns the authoring controller to the package prefab.

`pose` is a deformation/still-gate take. It is not a gameplay verb and must not be marked as a complete swing.

## Runtime

- SharedRig primitives and `hero-shared` extras still use **MoveBones**.
- Unique packages must not play Rio authored pose-clips or `swing.fbx`.
- Unique packages select ready takes from their package manifest. The imported controller stays attached as the package contract; `HeroActor` restores the package's canonical local bind transforms before sampling a take and when returning to fallback, then samples on the play clock so contact and release remain synchronized with the ball.
- Albedo is a sidecar PNG assigned as URP Lit on import.

## The original six

Rio, Vale, Zig, Brondo, Konga, Ashlord stay on `hero-shared` + extras until they are authored as packages. `hero-shared` is the same cartoon fat-volume language as Fenn (named bones, 100% vertex groups). Role players reuse the captain body type and must not grow captain extras.

## Tools

| Tool | Need it? |
| --- | --- |
| **Blender** | Yes. Source of truth. |
| Weight painting / Rigify metarig | Yes for `skinned` unique anatomy. Rigify does not auto-skin a posed mesh; you place the metarig. |
| Meshy / Tripo auto-rig | Optional **base** after T/A-pose. Not the player asset until clay + extreme-pose pass. Humanoid/quadruped templates; unique turtles are not Mixamo. |
| Auto-Rig Pro (paid) | Optional if a human is painting many fused meshes. |
| Mixamo / AccuRIG | No for unique toys. Humanoid only. |
| Unity Animation Rigging | Optional later for IK (hand to bat). |
| glTFast | No. FBX remains the player format. |

## Acceptance

Idle alone is not the human test. The Unity validation gate rejects missing or duplicate sockets, an invalid Generic Avatar, unusable or mismatched skin/bind matrices, unresolved animation bindings, ready clips whose sampled skinned bounds collapse or leave the rest body, marker or loop mismatches, a controller without every ready take, and package assets that cannot load through `Resources`. The player build runs the same imported-package check.

A unique package is still not done until [screenshot-gate](screenshot-gate.md) **character stills** exist: rest (painted, not inverted) and a ~90° limb pose (shell intact). Imported weights and bindings cannot prove deformation or silhouette quality. Agents do not pass that gate.
