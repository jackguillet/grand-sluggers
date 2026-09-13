# Baseball motion spec — shared default

This is the source of truth for the authored baseball body, motions and equipment. Gameplay outcomes and input timing remain owned by [gameplay-spec.md](gameplay-spec.md). When an animation disagrees with this document, fix the authoring source and rebake; do not compensate with runtime bone rotations.

Status: implemented and baked; automated geometry/import checks passed. Reference matching and the human look/gameplay gates are pending. Jack requested the rig revision and more human proportions on 2026-09-12. That supersedes the former prohibition on adding joints, while retaining one shared skeleton and one Blender motion pipeline.

## 1. Reference and scope

Peach in **Mario Super Sluggers (Wii, 2008)** is the motion reference for a regular swing, held charge and charged swing, ordinary pitch, charged pitch and bunt. Fielding throws use the same overarm delivery family. Our characters, equipment and exported assets remain original. Heart/Star skills are outside this default motion set.

Reference index: [Peach pitching gameplay](https://www.youtube.com/watch?v=N4RgZ-1jnfI), [Nintendo instruction booklet](https://www.mariomayhem.com/downloads/mario_instruction_booklets/Mario_Super_Sluggers_-_ML1_Manual_-_WII.pdf). A reference URL alone is not evidence that the motion was matched. Record observed beats and timestamps before calling reference comparison complete. Authored times below are Grand Sluggers' timing contract, not measurements claimed from Peach footage.

## 2. Body and joints

One `hero-shared` rig, revision 2. `data/art/rig.json` defines every joint, parent and rest endpoint in Blender coordinates: +Z up, −Y forward, +X anatomical left. No runtime mirror or negative root scale. Default stature is 4.81 units, head diameter 0.90 (5.34 heads tall), hips 2.13, knee 1.16, ankle 0.24, shoulder 3.50, elbow 2.48, wrist 1.60. These are art dimensions, not a claim of adult anatomical realism. Legs supply about 44% of height instead of the former short toy legs.

The chain separates pelvis, lower spine, chest, neck, clavicles, shoulders, elbows, wrists, hips, knees and ankles. Hands and shoes follow wrists and feet. A torso turn must not drag a planted foot. Wrist articulation must not move the elbow. All existing takes are rebaked against this hierarchy; mixing revision-1 clips and revision-2 bodies is invalid.

A captain remains body scale, palette and accessories. Role players share the body and take set. Future style variants start from the default authoring data and must pass the same geometry and handedness gates. No per-captain branch in HeroActor.

## 3. Motion phases and clocks

A regular swing starts ready, drives hips before hands, meets the ball at clip second 0.30, follows through at 0.50, and holds a balanced finish at 0.60. The charged swing exaggerates the coil, lead knee and finish. Holding charge samples the load; committing continues from the sample. The sim's existing timing warp still places Contact at the ball's arrival.

Pitch phases are gather, lead-knee balance, separation/stride, arm cock, release and follow-through. Normal and charged pitches share the 0.42 release marker; charge exaggerates the balance and coil, not the ball's rules. The throwing hand must be forward of the chest and above shoulder level at release, with the opposite foot leading. The glove stays on the opposite hand through the whole delivery.

The fielding throw gathers from the glove, steps with the glove-side foot, releases at 0.18, and follows through across the body. It uses the sim's throw clock for every position, including catcher, relay and pitcher covering a bag.

Bunt: square enough to see the pitcher, bend the knees, hold the barrel across the strike band. Bottom hand stays near the knob; upper hand slides toward the taper. Both hands must actually meet the bat. A bunt is held, not a small swing.

## 4. Equipment

Every defensive actor, including pitcher and catcher, wears a glove on the hand opposite `Character.Throws`. Batting handedness does not select the glove. The bare throwing hand supplies the release socket. A handed glove is a baked mesh pair, selected by wearing hand; its thumb must point inward. No mirrored runtime transforms.

The glove has an open wrist, concave pocket, four distinct finger stalls, a thumb, webbing and laces. Its origin is the wrist bind, not its bounding-box center. Both hand meshes use the same dimensions. Brown/gold gear changes finish without changing the wearing hand.

The bat has a knob, wrapped grip, continuous taper, barrel and rounded end. Keep the measured gameplay envelope: model Y −1.14 to +1.25, knob −1.14 to −0.96, grip −0.85, handle −1.00 to −0.10 at radius 0.08, barrel −0.15 to +1.25 with maximum radius 0.12; shared scale 1.28. The three named lathe profiles have seated overlaps so their materials retain the imported measurement contract. Polish must preserve the contact geometry. Equipment dimensions and slots precede asset baking.

## 5. Replicable authoring procedure

1. Record reference clips and visible key beats; distinguish measured reference facts from our timing choices.
2. Update the rig/motion/equipment JSON first. A schema or joint change requires a complete body, accessories and clip rebuild.
3. Author right-handed default takes in Blender. Solve two-hand grip in DCC; key wrists, feet and sockets. No runtime IK.
4. Reflect bone transforms and anatomical sides in Blender, bake the left files, and compare every rendered landmark within 0.001 units per frame.
5. Bake body, extras and all clips. Store byte-identical player copies. Inspect clay sheets with the actual equipment attached, both hands.
6. Run anatomy/geometry validation, sim tests, art validation and Unity compile. Run the rendered swing/motion matrix for every captain and both hands.
7. Commit the isolated worktree and build a standalone preview. Inspect the rendered window, then play pitching, normal/charged batting, bunting, catches and throws with keyboard/mouse and pads. Record failures; agent checks do not pass Jack's look or gameplay gate.
8. Only after human acceptance merge and deliver the merged standalone. Keep reference, source data and evidence together so the next character follows the same loop.

## 6. Acceptance scenarios

M-01: rig parent relationships isolate wrists, ankles and pelvis; neutral body is symmetric and meets the stated proportions.
M-02: every right/left take is an exact baked reflection, including hand and shoe meshes.
M-03: both hands stay on the swing handle, lead hand below top hand, every frame; the bat clears the head by at least 0.10 units.
M-04: charge hold endpoints are continuous with committed motion; event markers agree with the sim.
M-05: pitch and throw release from the bare hand with the opposite foot leading; glove stays seated on the wrist.
M-06: bunt hands meet the knob/taper and the barrel remains in the strike band.
M-07: every Bats/Throws combination selects the correct bat clip and opposite-hand glove; brown/gold equipment obeys the same rule.
M-08: all captain accessories remain attached and eyes/feet face the intended direction in Unity.
M-09: title → lineup → first pitch → played half in the standalone; both control schemes, then two pads. Human gate stays pending until Jack accepts.

## 7. Rebuild commands

Run from an isolated repository worktree with Blender on PATH. The `.blend` is an editable review artifact; the JSON and Python scripts are the reproducible authoring source. FBX takes are the shipped asset. Rebuilding the body alone is never sufficient after a hierarchy change.

```sh
python3 tools/blender/sync_swing_contract.py
blender -b -t 2 --python tools/blender/hero_shared_blockout.py -- --out unity/Assets/Art/Characters/SharedRig/hero-shared.fbx --resources unity/Assets/Resources/Art/Characters/SharedRig --clay /tmp/gs-motion-review/body
blender -b -t 2 --python tools/blender/hero_shared_extras.py -- --out unity/Assets/Art/Characters/SharedRig/extras.fbx --resources unity/Assets/Resources/Art/Characters/SharedRig --clay /tmp/gs-motion-review/extras
blender -b -t 2 --python tools/blender/hero_shared_takes.py -- --out unity/Assets/Art/Animation/Clips --resources unity/Assets/Resources/Art/Animation/Clips --sheets /tmp/gs-motion-review/takes --blend /tmp/gs-motion-review/default-motion.blend
python3 tools/blender/sync_swing_contract.py --check
dotnet test src/GrandSluggers.Sim.Tests/GrandSluggers.Sim.Tests.csproj -m:1 -nodeReuse:false -p:UseSharedCompilation=false
dotnet run --project src/GrandSluggers.Cli -- art
dotnet run --project src/GrandSluggers.Cli -- match
tools/unity-compile.sh
```

Use [screenshot-gate.md](screenshot-gate.md) for the Unity request-file capture and [local-player.md](local-player.md) for the standalone preview. Select the full seven-captain swing matrix; the capture now evaluates both hands independently of roster defaults.

For a future character variant, first duplicate the default take's data into a catalogued style slot and retain its event markers, handed pair and rig revision. Route selection through the motion catalog, then bake and validate both sides. The current release intentionally has one default; character-specific style selection is not implemented yet. Proportion changes already use the existing captain scale/palette/accessory system. Do not create another skeleton or put pose overrides in HeroActor.

## 8. Evidence and remaining review

The full .NET suite passed **887 tests**. The latest anatomy/catalog contract subset passed 16 tests after final validator changes. The Blender bake validated both handed clips frame by frame; the Unity capture passed **140 swing rows** (seven captains × two hands × normal/MAX × five beats). Art validation, CLI match and narrow Unity compile passed. These prove named relationships and imported geometry; they do not certify aesthetic quality, Peach fidelity or a played half.

[Recorded Unity measurements](images/default-motion/swing-matrix.json). Body comparison below uses the same Blender camera and material display: previous body left, revision 2 right.

![Shared body before and after](images/default-motion/body-before-after.png)

Current Unity rest and swing-contact evidence:

![Rio at rest](images/default-motion/rio-rest.png)
![Rio swing contact](images/default-motion/rio-contact.png)

Pending human review: compare the rhythm and poses of all six reference verbs against visible Peach footage, inspect glove seating and palm direction during live catches/throws, and play M-09 with keyboard/mouse and pads. No human gate has been passed by an agent. In particular, this work does not claim a frame-matched Peach reconstruction; recorded reference timestamps are still required before that claim can be made.
