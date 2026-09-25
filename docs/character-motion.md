# Characters and motion — the contract

One rig. One body script. One motion source. Handedness is baked, not computed. This document replaces the runtime parts of [character-package.md](character-package.md); that file now records the deferred unique-package path.

Living spec: `data/art/rig.json`, `data/art/clips.json`, `data/art/extras.json`, `data/art/skins.json`, `tools/blender/hero_shared_takes.py`, and `dotnet run --project src/GrandSluggers.Cli -- art`.

## Why

Before this contract the same swing lived in five places: procedural eulers in `MoveBones`, a second procedural set in `CharacterMotion`, `data/art/pose-clips/*.json` sampled at runtime, a Blender bake, and a hardcoded quaternion switch in `HeroActor`. Left-handed batters were produced at runtime by negating Euler Y/Z on each bone after sampling, a trick that depends on bone-axis conventions and was wrong more than once. Fenn was a separate pipeline (own mesh, controller, manifest, validator, bind capture) that failed its own look gate. Bodies were built three ways (Blender FBX, Unity primitives, Unity package bind) with per-captain `if (brick) ... else if (speed)` geometry in C#.

That is not a rail. It is five patches that agree by accident.

## The rules

1. **One rig.** `hero-shared` from `tools/blender/hero_shared_blockout.py`. Bone names in `data/art/rig.json` are the contract. There is no second skeleton and no unique package. Every captain, Fenn included, is this rig.
2. **A captain is data.** Its `proportions` (Height and Width are the root scale; Head, Arms and Torso are the build: shape keys on the one mesh, `Silhouette.Build`), `teamName` and `signatureBat` in `data/characters/<id>.json`, read through `Silhouette.Proportions`, and a faction palette. `data/art/extras.json` slots stay; skins list none until extras read as toys (#687). No per-captain geometry in C#.
3. **One motion source: Blender takes.** Every `Motion.Verb` plays an FBX clip baked by `tools/blender/hero_shared_takes.py` from one pose table. C# contains no Euler angles for any body part. `SwingPresentation` and `BattingStance` remain the *contract* the swing take is authored against and measured by; they never drive bones.
4. **Handedness is baked.** A handed take is authored right-handed and reflected across the sagittal plane in Blender, exactly, into `{clip}-L.fbx`. Both files are validated per frame against the same handed contract. Runtime picks the file by `Character.Bats` or `Character.Throws`. There is no runtime mirroring of any bone, socket, or sample.
5. **The sim owns the clock.** A clip is sampled at a time the sim computes: world time for loops, verb time for one-shots, `LoadSampleAt(charge)` for a held load. Markers (`Contact`, `Release`, `FootPlant`) are at the same seconds the sim uses. The swing is the one take the sim time-warps (D13, #612): for a press inside the timing window `AtBatMotion.SwingClipTime` compresses load → contact so the `Contact` mark lands on the ball's plate time, then plays the follow-through at the take's own speed; outside the window the take plays at its natural 0.50 s. The warp only changes when a key is shown, never its order or its pose. Presentation never advances a clip on its own. At the `Contact` key the barrel cuts the zone plane (the plate front) over the plate, inside the contact band, for every captain, style and hand (`SwingPresentation.BarrelCutsZonePlane`): the stride and the extension carry the hands toward the pitcher from the batter's spot just behind the box middle.
6. **Bodies are kinematic.** No Rigidbody, no PhysX on a character. The ball is the sim's. Squash and stretch are scale on the presentation wrapper, never on bones. Lift (jump arc, run bob, crouch) is baked into the take's `root` bone.
7. **Placeholders, not crashes; validators, not hope.** Missing body FBX: a capsule and a validator error. Missing clip: the idle clip, then bind pose, and a validator error. `cli art` fails before a build does.
8. **Look is Jack's.** DCC falsifiers, Sim tests, and the Unity swing matrix prove the contract. Dual stills in `docs/screenshot-gate.md` (`tools/dcc-still.sh` + `tools/still-gate-character.sh`) are the pictures. A look-critic files; Jack passes. Agents do not.

## Axes, stated once

| Space | Forward | Up | Character's left | Note |
| --- | --- | --- | --- | --- |
| Unity | +Z | +Y | −X | `HeroActor` faces `look` with `LookRotation`; the body must face +Z at identity |
| Blender scene | −Y | +Z | +X | What `hero_shared_blockout.py` authors |
| Batter-local (contract) | +Z = pitcher | +Y | — | `SwingPresentation`, `BattingStance`, `HomeSet`; +X crosses the plate for a right-handed batter |

FBX export is `axis_forward="-Z", axis_up="Y", bake_space_transform=True`. Unity's import reflects X. Net map, DCC → Unity: `(bx, by, bz) → (−bx, bz, −by)`. Inverse, Unity → DCC: `(ux, uy, uz) → (−ux, −uz, uy)` (`batting_stance.unity_to_dcc`). The body is authored facing −Y so that it lands facing +Z; the face and toes are on the side Unity draws forward. Nothing rebuilds a face at runtime.

Limb bones hang along −Z in Blender with roll 0, so their local frames are: X = world X, Y = down the bone, Z = world +Y (behind the character). The pose table in `hero_shared_takes.py` is written in body terms (`flex` forward, `abduct` outward, `twist`) and converted to bone-local Eulers per side by the script, so a pose reads the same for the left and the right limb and the mirror is a sign flip by construction.

## Rig

The source hierarchy and dimensions are now `data/art/rig.json` revision 2. The
[baseball motion spec](baseball-motion-spec.md) owns the default motion phases,
body proportions, equipment fit and repeatable authoring procedure.

`root → pelvis → spine → torso → neck → head`, with paired clavicle / upper arm /
forearm / wrist chains and thigh / shin / foot chains. `lGlove` / `rGlove` are
wrist binds; `lRelease` / `rRelease` are the rendered palm centers. `bat` remains
the animated grip socket under `rWrist`; the left clip keys it in the other hand.
The legacy `glove` bone stays in the shared chain for file compatibility, while
equipment attaches to the anatomical wrist sockets.

The neutral body (revision 3) is a toy about four heads tall: 4.80 units with a
1.20-unit head, short legs (hip 1.60, knee 0.90), a long round jersey weighted
across spine/chest, almost no neck, and arms of the shared reach. Joint names,
parents and rest endpoints are data. Every clip must contain the revised joints;
`cli art` rejects old skeleton clips even when their player copies are
byte-identical. `anatomy.knee` and `anatomy.chest` are the rest landmarks the
strike zone reads (`Silhouette.KneeFt` / `ChestFt`, world feet per captain).

**Build.** A captain's head, arms and torso are shape keys on the body pieces,
never a joint, so every take stays shared. `data/art/rig.json` `build` names each
channel's `neutral`, `gain`, `min` and `max`; the scale is
`1 + gain · (proportion / neutral − 1)`, and `{channel}+` / `{channel}-` are the
keys authored at max and min. The head grows about the neck top, the arms thicken
about the bone line (mitts grow about their centers), the torso widens about the
body axis. `Silhouette.Build` is the one function: `SharedRig` sets the weights
from it, and the swing contract, the stills and the ladder measure from it. The
takes bake checks the bat against the head at the build's max; `cli art` refuses
a captain whose build falls outside the keys.

Mesh landmarks remain `torsoMesh`, `Stripe`, `headMesh`, `EyeL`, `EyeR`, `lHand`,
`rHand`, `lShoe`, `rShoe`. Hands are weighted to wrists and shoes to feet.

## Verbs and clips

`Motion.Verb` is the presentation vocabulary the directors speak. `data/art/clips.json` is the file list. `Motion.ClipFor(verb, hand)` is the only mapping.

| Verb | Clip | Clock | Handed | Marker |
| --- | --- | --- | --- | --- |
| Idle, Field, Cheer, Charm | idle / field / cheer / charm | world (loop) | no | |
| Walk, Run | walk / run | ground (loop): the phase advances with distance covered, `Gait.Advance` | no | FootPlant 0; the root yaw is `BodyFacing` (gameplay-spec §8.2); run at or above the body's run floor (`Gait.RunFloorFt`), the walk take below it and on the backpedal |
| Jump, Clamber | jump | verb | no | FootPlant 0.55 |
| ChargePitch | pitch-charge at `LoadSampleAt(charge)` | charge | yes | |
| ThrowPitch | pitch or pitch-charge at `LoadedClipTime` | verb | yes | Release 0.42 |
| Throw | throw | verb | yes | Release 0.18 |
| ChargeSwing | swing-charge at `SwingPresentation.HeldLoadAt(charge)` | charge | yes | |
| Swing | swing-slap or swing-charge (the resolver's charge test) at `AtBatMotion.SwingClipTime` | verb | yes | Contact 0.30, held finish 0.60 |
| CheckSwing, Bunt, Miss | checkSwing / bunt (bunt-pull, bunt-push by the held side) / miss | verb (hold) | yes | |
| LetGo | swing-letgo | verb, from `LetGoStartAt(charge)` | yes | |
| Catch, Dive, Crouch, StealLead, Spin | catch / dive / crouch / stealLead / spin | verb (hold) | no | |
| Scoop | scoop | verb | no | Contact 0.22 |
| Slide | slide | verb | no | FootPlant 0.18 |
| CatcherThrow | catcherThrow: receive in the crouch, transfer, plant, release, follow-through | verb, release warped to the sim's preparation | yes | Release 0.30 |
| Tag | tag: the glove hand sweeps low in front of the bag | verb (hold) | yes | |
| SlideHeadFirst | slideHeadFirst: flat on the belly, both hands to the bag; reserved, nobody plays it yet | verb | no | FootPlant 0.18, the feet-first slide's clock |
| TurnBack | turnBack: brake, pivot, push off back | verb, for 0.30 s after a runner reverses | no | |

**Catalog first, then the take.** A clip row may name a `standIn`: an authored clip it plays until its own take lands. The row still states the contract its take must meet (length, marker, hand); until then every file, marker and hold is the stand-in's (`Motion.Played`). `cli art` refuses a stand-in that is not an authored clip, a row whose `standIn` differs from `Motion.Clips`, and a stand-in slot that already has files. The take that fills a slot drops its `standIn` in the same change. No motion style owns a stand-in slot.

The fielding takes carry the reference's relationships as bake contracts (#558). The scoop at Contact has the glove within 0.45 of the dirt and 0.6 ahead of the feet, a base wider than the shoulders and the bare hand over the glove. The jump stands on the dirt at take-off and landing, its soles rise `Motion.JumpPeak` at the top key, and the glove reaches over the head and the bare hand. The catch hold has both hands above the head.

A held load samples the one-shot at `LoadSampleAt(charge) = NormalLoadAt · (1 − charge)`: MAX holds the full coil at 0, a tap starts from the half load. The committed verb then samples `LoadedClipTime(poseT, loadAt, eventAt)`, which is monotonic and lands the marker exactly at `eventAt`. One function for pitch and swing.

The squared bunt shows its side (PH-14-R3): `data/art/baseball-takes.json` `bunt.sides` turns the barrel about the vertical by `yawDeg` (positive carries the barrel end toward the pitcher, so the face points at the pull field) and adds a body-term delta.
They are baked as `bunt-pull` and `bunt-push` for both hands, and the bake checks each barrel is its side's.

A cancelled load lets go (PH-13-R1): `letGo` walks the charge take from the full coil (`fromAt`) to the no-charge stance (`toAt`) linearly by `returnAt`, then settles by `settle`, so `Motion.LetGoStartAt(charge)` starts a partial load on its own held pose.

### Motion styles (CH-12)

A body class moves in its own **motion style**. A style is a dimension of the one pose table in `hero_shared_takes.py` (`STYLE_POSES`), not a second motion system. Rows live in `data/art/clips.json` `styles`. The table has room for about fifteen styles, so role players can get their own later.

- **Styled clips.** `styles.clips` lists them: `idle`, `walk`, `run`, `swing-slap` and `swing-charge` (the batting stance), `pitch` and `pitch-charge` (the windup), and `cheer`. A style's `cheer` is its captain's **signature beat**, played on the existing Cheer path (a home run, the win). Every other clip is the shared take.
- **Files.** The bake writes each style's takes to `Clips/styles/{id}/{clip}[-L].fbx`, for both hands, with the same per-frame contracts. `Motion.ClipFor(verb, hand, style)` returns `{id}/{file}` when the style owns the clip. Otherwise it returns the shared file.
- **Stance and windup.** These are deltas on the shared swing and pitch keys. The stance delta is whole through the ready and load keys and gone by Contact. The windup delta, including the leg-kick height, is whole through the leg lift and gone by Release. So every hand, bat, head-clearance and release contract still holds, and the bake refuses a style that breaks one.
- **Build channels.** `reach` (arm length) and `boots` (shoe size) are style channels in `rig.json` `build`. Their scale comes from the style (`reachScale`, `bootsScale`), not from the proportions. `boots` moves no joint. `reach` stretches the arm pieces, and **the style's takes move the elbow and the wrist down the bone** (`reach_offsets`). So a style with reach owns every clip: the bake solves each swing, bunt, throw and catch hand contract at the longer arm, and a socket contract (glove on the wrist, release on the palm) runs on every frame of every take.
- **Receipt.** The bake writes `data/art/takes-receipt.json`: one row per file, with its SHA-256, frame count and the contracts it passed. `cli art` refuses a take whose bytes no row vouches for, and a style take that passed fewer contracts than its shared take.
- **Who wears which.** A body moves in the style its body class names (`data/rules/body-classes.json` `motionStyle`, read through `ArtCatalog.StyleOf`). A role player plays its captain's class unless it names its own. `cli art` refuses a class whose `motionStyle` is not a style row, and a style no class plays unless the row marks it `reserved`. A body with no class plays the shared takes.
- **Stride.** `runCycle` and `walkCycle` are rig units of ground per loop. `Gait.CycleFt` scales them by the body's root height. The loop's phase advances with the ground the body covers on the sim's positions, so stride rate follows ground speed (SC-21).

### The two swings (#613)

- **Slap** (`swing-slap`): no windup. It starts on the ready key (hands by the back shoulder, bat standing up beside the head so the plate SET can see the hover, #560), a compact arc, the held finish.
- **Charge** (`swing-charge`): the hold samples its windup (0.00 = MAX: hands high and back, the bat wrapped, the lead knee up; 0.075 = the ready key at no charge), then a bigger arc and a bigger finish.
- A committed swing plays the charge take when `ChargeFeel.IsCharge(charge)`, the same test that narrows the window, so the take and the judgment are always the same swing. A slap starts at 0; a charge continues from the held windup.
- The numbers are data: `data/art/swing-takes.json` holds per key the rendered hand centers, the grip socket, the barrel direction (Unity batter-local, right-handed) and the legs in body terms. The takes script solves every frame to it; `SwingPresentation.SlapKeys` / `ChargeKeys` are generated by `tools/blender/sync_swing_contract.py` and a test holds them equal. Approach (0.24) and contact (0.30) are the measured contract of [research-batting.md](research-batting.md) and are shared by both takes.
- **Held finish (#583).** Both takes end on a finish key at `Motion.SwingFinish` 0.60. The batter keeps it after the take: through the STRIKE stamp until SET on a dead ball, and through the contact freeze until the batter-runner is `feel.swingFinishStepFt` (2.5 ft) out of the box on contact (`AtBatMotion.PresentsSwing`). No blend back to ready in between.
- **The bat never passes through the head (#623).** Revision 2 uses a smaller head and raises ready/finish to shoulder height. The charge windup keeps the bat above and behind the hands, with independently keyed wrists; the geometry gate remains mandatory. Every frame of both takes, and every held charge, keeps the physical bat's surface at least `batHeadClearance` (0.10 rig units) from the head. Three gates hold it: the takes script measures the rendered head against the physical bat on every frame of both hands and refuses the take; `SwingPresentation.HeadClearance` checks the catalog keys with a conservative head (the crouch `Lift` per key, plus the stance yaw's slack) in `SwingPresentationTests`; the Unity swing matrix fails any beat where the drawn bat enters the drawn head. Body proportions scale the bat and the head together, so rig-space clearance holds for every captain.
- `checkSwing` holds the slap at 0.20 and `miss` holds the slap's follow-through.

Handed clips: `swing-slap`, `swing-charge`, `pitch`, `pitch-charge`, `throw`, `checkSwing`, `bunt`, `miss`. Their left files are `{clip}-L.fbx`. A right-handed batter or thrower plays the unsuffixed file.

Clips are exported armature-only (no mesh), one take per file, 60 fps, linear keys, from the same scene as the body. Player copies live under `Assets/Resources/Art/Animation/Clips/` and must be byte-identical to the authoring copy; the test enforces it.

## Runtime

`HeroActor` is a thin player:

- `SharedRig.Spawn` instantiates the body FBX, applies the palette by material name, hangs extras listed on the skin (none until they read as toys), and returns the bone chain. No primitives except the missing-file capsule.
- Hierarchy: `HeroActor` (position, yaw) → `body` wrapper (`Silhouette.SharedRootScale`, bounce, squash) → FBX root → armature node (`Animator`, no controller). Takes are armature-only files, so their curve paths start at the armature node; the Animator sits there. Scale and bounce live on the wrapper.
- `ClipPlayer` is a two-slot `AnimationMixerPlayable` in manual update. Each tick: resolve `(clip, time)` from `(verb, hand, clocks)`, crossfade if the clip changed, `SetTime`, `Evaluate(0)`. `SnapTick` is the same call with a zero fade. Nothing slerps toward a target.
- Props: the common bat under the `bat` socket with the one authored model-to-socket bind; the appropriate baked glove under `lGlove` / `rGlove`, selected only by the throwing hand. Ball attachment uses the glove pocket or bare-palm release socket.
- Measurement helpers for the swing matrix live in `HeroActor.Evidence.cs` and read rendered meshes, never bone-local axes.

## Extras

`data/art/extras.json` rows: `id`, `bone`, `hides`. Meshes are named pieces in `Assets/Art/Characters/SharedRig/extras.fbx` from `tools/blender/hero_shared_extras.py`, authored on the body in rig space; Unity drops each piece at the rig root and reparents it to its bone keeping the world pose. Attaching is one loop. **Skins list none** until extras read as toys, not geometry junk (#687). Role players already listed none. A missing piece draws nothing and the validator says so. There are no caps: heads are bare until hats return as accessories.

## Pitch, throw and bunt authoring

`data/art/baseball-takes.json` is the default right-handed pose and foot-target
source. Normal and charged pitches share Release 0.42 but have distinct load
poses. The charge take includes the normal ready key at 0.09; charge selection
uses the existing `ChargeFeel.IsCharge` threshold. The follow-through lasts to
0.70. Throws release at 0.18. A DCC two-bone leg solve plants the authored ankle
targets, with flat feet independent of the shins. Wrist keys articulate the palm;
release sockets must follow the rendered hand within 0.002 units on every frame.

Bunt uses measured knob/taper hand targets and a level barrel. Both hands must
land within 0.02 units of their assigned points. Swings include wrist flex in
the two-bone arm effector; planted sole adjustment happens before hand solving.
Nothing in these solves runs in Unity.

`data/art/baseball-equipment.json` specifies the bat profile, glove dimensions,
mesh pair IDs and pocket location. `baseball_equipment.py` authors the actual
props used both in `extras.fbx` and the clay sheets. Every glove has a concave
pocket, individual fingers, thumb, woven web and open cuff. Right-worn versions
are reflected meshes with corrected winding; runtime transforms remain positive.

## Authoring loop

```bash
# body + extras (rarely)
/opt/homebrew/bin/blender -b --python tools/blender/hero_shared_blockout.py -- --out unity/Assets/Art/Characters/SharedRig/hero-shared.fbx --resources unity/Assets/Resources/Art/Characters/SharedRig
/opt/homebrew/bin/blender -b --python tools/blender/hero_shared_extras.py -- --out unity/Assets/Art/Characters/SharedRig/extras.fbx --resources unity/Assets/Resources/Art/Characters/SharedRig
# regenerate the C# measured bat contract after editing swing JSON
python3 tools/blender/sync_swing_contract.py
# every clip, both hands, validated per frame, with clay contact sheets
/opt/homebrew/bin/blender -b --python tools/blender/hero_shared_takes.py -- --out unity/Assets/Art/Animation/Clips --resources unity/Assets/Resources/Art/Animation/Clips --sheets scratchpad/takes
```

The takes script refuses to export a take that misses its contract: hands off the handle, lead hand not at the knob, chest or toes off the plate, a left file that is not the exact reflection of its right file (every rendered landmark within 1 mm after reflection), a marker not at its second. It renders a clay contact sheet per clip so the author looks before Unity does.

## Gates

| Lane | Proves | Command |
| --- | --- | --- |
| DCC bake | takes meet the contract on every frame, both hands | the takes script exits non-zero otherwise |
| DCC still | clay/sheet before import (`dcc-*.png`) | `tools/dcc-still.sh` |
| Sim tests | the contract itself (hand order, feet, plate crossing, clocks, catalog ↔ verbs) | `dotnet test` |
| Art catalog | every verb has a clip file, both hands where handed, player copies identical, extras resolve | `cli art` |
| Narrow compile | Runtime and Editor still compile | `tools/unity-compile.sh` |
| Unity swing matrix | rendered hands on the handle, stance, barrel cutting the zone plane over the plate, all captains, both hands | `Grand Sluggers → Capture Swing Matrix` / `-executeMethod` (see `docs/screenshot-gate.md`) |
| In-game still | rest + pose (and park shots) HUD-off | `tools/still-gate-character.sh` / `still-gate.sh` |
| Look | it reads as a toy | Jack, from both stills. A look-critic files; it cannot mark #188 done. |

## What is gone

`MoveBones.Evaluate` and every procedural pose, `CharacterMotion`, `PoseClip` and `data/art/pose-clips/`, the `HeroActor` pose switch, `MirrorBoundSwing` / `MirrorArms` / `MirroredBatSocket`, `SharedRig` primitive bodies and `Face()`, the package runtime (`CharacterPackage`, `character-packages.json`, package import validation, controller sync), Fenn's package FBX set, `EYES_REVERSED_BY_IMPORT`.

## What is not in scope yet

Painted unique meshes, secondary motion (cape, ears), blend trees, IK at runtime, motion matching. Each is a later slot on this rig, not a reason to grow a second one.
