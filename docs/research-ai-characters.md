# Research: Unity characters and AI 3D (2026)

Dated **2026-09**. Citations, not product policy. The living contract is [character-package.md](character-package.md).

This is why posed-GLB drops shredded Fenn, and why `dotnet test` is not look.

## Unity’s Animation System

Unity 6.5: a character for Mecanim is already **modeled, rigged, and skinned** in a DCC (Blender / Maya / 3ds Max), then imported. Both types require that triad.

| Type | Requirement | Clips |
| --- | --- | --- |
| **Humanoid** | ≥15 bones in a loosely human layout, **T-pose**, Avatar mapping | Retarget across bipeds |
| **Generic** | Anything else (Unity’s example: teakettle to **dragon**). One **Root node**. No Configure Avatar | Authored **on that hierarchy**. Same bone names/transforms, or they do not play |

Sources: [Creating models for animation](https://docs.unity3d.com/6000.5/Documentation/Manual/UsingHumanoidChars.html), [Generic animations](https://docs.unity3d.com/6000.5/Documentation/Manual/GenericAnimations.html), [Rig tab](https://docs.unity3d.com/6000.5/Documentation/Manual/FBXImporter-Rig.html), [Configuring the Avatar](https://docs.unity3d.com/6000.5/Documentation/Manual/ConfiguringtheAvatar.html).

Bind pose ≠ T-pose. Unity samples the authored bind, automaps, then Enforce T-Pose. Bones can map and still report **Character not in T-Pose**. Retargeting a posed toy onto another character’s T-pose folds the mesh.

Runtime: **Animator** + **Animator Controller**. Even one clip lives in a controller. Humanoid also wants the Avatar on the component. Unique creatures (Unity’s octopus end-boss) get **their own clips**, not a shared humanoid library. [Animation from external sources](https://docs.unity3d.com/6000.5/Documentation/Manual/AnimationsImport.html).

Production interchange is **FBX** (mesh + skeleton + takes, including `name@clip.fbx`). glTF/GLB is not on Unity’s built-in model-format list. Skin weights are DCC-authored; the importer defaults to **four influences** and can drop the rest. [Preparing models](https://docs.unity3d.com/Manual/models-preparing.html), [Model file formats](https://docs.unity3d.com/6000.4/Documentation/Manual/3D-formats.html).

Auto-skinning in Unity’s docs is a **DCC** step, not something the Model Importer does to an unrigged GLB.

## AI 3D tools (what they actually rig)

Vendors overclaim. Scope:

| Tool | Rigs | Fenn-class unique turtle |
| --- | --- | --- |
| Mixamo | Humanoid T-pose. Optional tails/wings on a human shape. Not quadrupeds. | No |
| AccuRIG | Biped / humanoid. Can mask unused limbs. No unique topology. | No |
| Meshy auto-rig | Humanoid + quadruped. T/A-pose. Mixamo-compatible FBX. Official API refuses non-humanoid. Tutorial “Smart Rig (beta)” is **not** in the official rigging guide. | Not as a posed statue |
| Tripo Auto Rig | Seven named plans: biped, quadruped, hexapod, octopod, avian, serpentine, aquatic | Template match only |
| Rodin | **Unrigged** static mesh. T/A-pose flag prepares a humanoid to be rigged **elsewhere**. | No skeleton |
| Hunyuan Auto Rig | “Characters or animals,” **T-pose**, FBX/OBJ ≤60 MB, biped motion presets | Not unique extras |
| Rigify (Blender) | Control rig from a **user-placed metarig**. Does not auto-extract rest pose or paint weights from an arbitrary mesh. | Quality path if a human places the metarig |

Sources: [Meshy rigging](https://docs.meshy.ai/en/webapp/guides/3d-model/rigging), [Tripo rig](https://developers.tripo3d.ai/en/docs/animations-rig), [AccuRIG FAQ](https://actorcore.reallusion.com/learn-and-support/faq/accurig), [Rigify metarigs](https://docs.blender.org/manual/en/latest/addons/rigify/metarigs.html).

May 2026 production writeup (Unity 6.4 URP, 50 Meshy props, Steam Deck): auto-rig on **15 AI humanoid meshes → 5 usable, 10 failed topology (33%)**. Quote: *If your pipeline needs rigged characters, AI-generated meshes aren't there yet.* Props shipped; characters did not. [Chen / HackerNoon](https://hackernoon.com/i-shipped-50-ai-generated-3d-assets-into-a-unity-urp-pipeline-heres-what-actually-held-up).

Sept 2026 survey: characters are a **base**; deformation topology, skeleton, weights, and animation QA stay human. Game-ready pipeline: brief → generate → **untextured clay inspect** → retopo/bake → **rig after topology is accepted** → **extreme-pose test** (idle is not acceptance) → test in the target scene. [Wavect](https://wavect.io/blog/ai-3d-model-generators-game-development-2026/).

Meshy’s own 2026 auto-rig guide: a character generated **sitting / crouching / arms in** cannot separate shoulder verts from torso — **regenerate in T/A-pose**, do not “repair” the posed mesh. Tears at joints = triangulated AI topology; remesh to quads, then re-rig. [Meshy tutorial](https://www.meshy.ai/tutorials/character-auto-rigging-workflow).

GLB → URP: glTF packs roughness in G / metalness in B; URP Lit packed maps use R metallic, G occlusion, A smoothness. Wrong assignment = white or dead PBR. Magenta = missing/stripped shader. This project’s FBX Standard-in-URP case is **white** unless URP Lit + sidecar albedo. [glTF 2.0](https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html).

## How look is verified

Unity Test Framework Play Mode can run in a [standalone Player](https://docs.unity3d.com/6000.5/Documentation/Manual/test-framework/workflow-run-playmode-test-standalone.html). [Recorder](https://docs.unity3d.com/Packages/com.unity.recorder@5.1/manual/index.html) captures Editor Play only — **not** the shipped `.app`. A passing screenshot test does not prove weights or Avatar mapping.

This repo’s gate is [screenshot-gate.md](screenshot-gate.md). Agents do not pass it. A rebuilt Mac player is not a still.

## What this means here

Fenn’s source was a posed turtle GLB with no skeleton. Heat-weight + Rio eulers inverted the shell. Freeze made a statue. Segmented Python splits are a stopgap, not Unity’s ship path. Unique captains need a **Generic FBX** with an armature and weights (or Blender-authored rigid pieces), **clips on that rig**, URP Lit albedo, and **character stills**.
