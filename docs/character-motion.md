# Characters and motion — the contract

One rig. One body script. One motion source. Handedness is baked, not computed. This document replaces the runtime parts of [character-package.md](character-package.md); that file now records the deferred unique-package path.

Living spec: `data/art/rig.json`, `data/art/clips.json`, `data/art/extras.json`, `data/art/skins.json`, `tools/blender/hero_shared_takes.py`, and `dotnet run --project src/GrandSluggers.Cli -- art`.

## Why

Before this contract the same swing lived in five places: procedural eulers in `MoveBones`, a second procedural set in `CharacterMotion`, `data/art/pose-clips/*.json` sampled at runtime, a Blender bake, and a hardcoded quaternion switch in `HeroActor`. Left-handed batters were produced at runtime by negating Euler Y/Z on each bone after sampling, a trick that depends on bone-axis conventions and was wrong more than once. Fenn was a separate pipeline (own mesh, controller, manifest, validator, bind capture) that failed its own look gate. Bodies were built three ways (Blender FBX, Unity primitives, Unity package bind) with per-captain `if (brick) ... else if (speed)` geometry in C#.

That is not a rail. It is five patches that agree by accident.

## The rules

1. **One rig.** `hero-shared` from `tools/blender/hero_shared_blockout.py`. Bone names in `data/art/rig.json` are the contract. There is no second skeleton and no unique package. Every captain, Fenn included, is this rig.
2. **A captain is data.** `Silhouette.Proportions` (root scale), a faction palette, and a list of extras from `data/art/extras.json` placed on named bones. No per-captain geometry in C#.
3. **One motion source: Blender takes.** Every `Motion.Verb` plays an FBX clip baked by `tools/blender/hero_shared_takes.py` from one pose table. C# contains no Euler angles for any body part. `SwingPresentation` and `BattingStance` remain the *contract* the swing take is authored against and measured by; they never drive bones.
4. **Handedness is baked.** A handed take is authored right-handed and reflected across the sagittal plane in Blender, exactly, into `{clip}-L.fbx`. Both files are validated per frame against the same handed contract. Runtime picks the file by `Character.Bats` or `Character.Throws`. There is no runtime mirroring of any bone, socket, or sample.
5. **The sim owns the clock.** A clip is sampled at a time the sim computes: world time for loops, verb time for one-shots, `LoadSampleAt(charge)` for a held load. Markers (`Contact`, `Release`, `FootPlant`) are at the same seconds the sim uses. The swing is the one take the sim time-warps (D13, #612): for a press inside the timing window `AtBatMotion.SwingClipTime` compresses load → contact so the `Contact` mark lands on the ball's plate time, then plays the follow-through at the take's own speed; outside the window the take plays at its natural 0.50 s. The warp only changes when a key is shown, never its order or its pose. Presentation never advances a clip on its own.
6. **Bodies are kinematic.** No Rigidbody, no PhysX on a character. The ball is the sim's. Squash and stretch are scale on the presentation wrapper, never on bones. Lift (jump arc, run bob, crouch) is baked into the take's `root` bone.
7. **Placeholders, not crashes; validators, not hope.** Missing body FBX: a capsule and a validator error. Missing clip: the idle clip, then bind pose, and a validator error. `cli art` fails before a build does.
8. **Look is Jack's.** DCC falsifiers, Sim tests, and the Unity swing matrix prove the contract. A still in `docs/screenshot-gate.md` passes the look. Agents do not.

## Axes, stated once

| Space | Forward | Up | Character's left | Note |
| --- | --- | --- | --- | --- |
| Unity | +Z | +Y | −X | `HeroActor` faces `look` with `LookRotation`; the body must face +Z at identity |
| Blender scene | −Y | +Z | +X | What `hero_shared_blockout.py` authors |
| Batter-local (contract) | +Z = pitcher | +Y | — | `SwingPresentation`, `BattingStance`, `HomeSet`; +X crosses the plate for a right-handed batter |

FBX export is `axis_forward="-Z", axis_up="Y", bake_space_transform=True`. Unity's import reflects X. Net map, DCC → Unity: `(bx, by, bz) → (−bx, bz, −by)`. Inverse, Unity → DCC: `(ux, uy, uz) → (−ux, −uz, uy)` (`batting_stance.unity_to_dcc`). The body is authored facing −Y so that it lands facing +Z; the face and toes are on the side Unity draws forward. Nothing rebuilds a face at runtime.

Limb bones hang along −Z in Blender with roll 0, so their local frames are: X = world X, Y = down the bone, Z = world +Y (behind the character). The pose table in `hero_shared_takes.py` is written in body terms (`flex` forward, `abduct` outward, `twist`) and converted to bone-local Eulers per side by the script, so a pose reads the same for the left and the right limb and the mirror is a sign flip by construction.

## Rig

`root torso head lUpper lFore rUpper rFore lThigh lShin rThigh rShin bat glove`. `bat` hangs under `rFore`, `glove` under `lFore`; both are sockets. Batting takes key `bat` on every frame from the solved grip, in both hands' files. The glove is attached at runtime under the fielding forearm with one fixed offset; it is never animated.

Mesh landmarks the gates read by name: `torsoMesh`, `Stripe`, `headMesh`, `EyeL`, `EyeR`, `lHand`, `rHand`, `lShoe`, `rShoe`. `lHand` is the character's left hand.

## Verbs and clips

`Motion.Verb` is the presentation vocabulary the directors speak. `data/art/clips.json` is the file list. `Motion.ClipFor(verb, hand)` is the only mapping.

| Verb | Clip | Clock | Handed | Marker |
| --- | --- | --- | --- | --- |
| Idle, Field, Cheer, Charm | idle / field / cheer / charm | world (loop) | no | |
| Walk, Run | walk / run | world (loop) | no | FootPlant 0 |
| Jump, Clamber | jump | verb | no | FootPlant 0.55 |
| ChargePitch | pitch at `LoadSampleAt(charge)` | charge | yes | |
| ThrowPitch | pitch at `LoadedClipTime` | verb | yes | Release 0.42 |
| Throw | throw | verb | yes | Release 0.18 |
| ChargeSwing | swing at `LoadSampleAt(charge)` | charge | yes | |
| Swing | swing at `LoadedClipTime` | verb | yes | Contact 0.30 |
| CheckSwing, Bunt, Miss | checkSwing / bunt / miss | verb (hold) | yes | |
| Catch, Dive, Crouch, StealLead, Spin | catch / dive / crouch / stealLead / spin | verb (hold) | no | |
| Scoop | scoop | verb | no | Contact 0.22 |
| Slide | slide | verb | no | FootPlant 0.18 |

A held load samples the one-shot at `LoadSampleAt(charge) = NormalLoadAt · (1 − charge)`: MAX holds the full coil at 0, a tap starts from the half load. The committed verb then samples `LoadedClipTime(poseT, loadAt, eventAt)`, which is monotonic and lands the marker exactly at `eventAt`. One function for pitch and swing.

Handed clips: `swing`, `pitch`, `throw`, `checkSwing`, `bunt`, `miss`. Their left files are `{clip}-L.fbx`. A right-handed batter or thrower plays the unsuffixed file.

Clips are exported armature-only (no mesh), one take per file, 60 fps, linear keys, from the same scene as the body. Player copies live under `Assets/Resources/Art/Animation/Clips/` and must be byte-identical to the authoring copy; the test enforces it.

## Runtime

`HeroActor` is a thin player:

- `SharedRig.Spawn` instantiates the body FBX, applies the palette by material name, hangs extras from `extras.json`, and returns the bone chain. No primitives except the missing-file capsule.
- Hierarchy: `HeroActor` (position, yaw) → `body` wrapper (`Silhouette.SharedRootScale`, bounce, squash) → FBX root → armature node (`Animator`, no controller). Takes are armature-only files, so their curve paths start at the armature node; the Animator sits there. Scale and bounce live on the wrapper.
- `ClipPlayer` is a two-slot `AnimationMixerPlayable` in manual update. Each tick: resolve `(clip, time)` from `(verb, hand, clocks)`, crossfade if the clip changed, `SetTime`, `Evaluate(0)`. `SnapTick` is the same call with a zero fade. Nothing slerps toward a target.
- Props: the common bat under the `bat` socket with the one authored model-to-socket bind; the glove under the fielding forearm.
- Measurement helpers for the swing matrix live in `HeroActor.Evidence.cs` and read rendered meshes, never bone-local axes.

## Extras

`data/art/extras.json` rows: `id`, `bone`, `hides`. Meshes are named pieces in `Assets/Art/Characters/SharedRig/extras.fbx` from `tools/blender/hero_shared_extras.py`, authored on the body in rig space; Unity drops each piece at the rig root and reparents it to its bone keeping the world pose. Attaching is one loop. A captain skin lists extra ids; role players list none. A missing piece draws nothing and the validator says so. There are no caps: heads are bare until hats return as accessories.

## Authoring loop

```bash
# body + extras (rarely)
/opt/homebrew/bin/blender -b --python tools/blender/hero_shared_blockout.py -- --out unity/Assets/Art/Characters/SharedRig/hero-shared.fbx --resources unity/Assets/Resources/Art/Characters/SharedRig
/opt/homebrew/bin/blender -b --python tools/blender/hero_shared_extras.py -- --out unity/Assets/Art/Characters/SharedRig/extras.fbx --resources unity/Assets/Resources/Art/Characters/SharedRig
# every clip, both hands, validated per frame, with clay contact sheets
/opt/homebrew/bin/blender -b --python tools/blender/hero_shared_takes.py -- --out unity/Assets/Art/Animation/Clips --resources unity/Assets/Resources/Art/Animation/Clips --sheets scratchpad/takes
```

The takes script refuses to export a take that misses its contract: hands off the handle, lead hand not at the knob, chest or toes off the plate, a left file that is not the exact reflection of its right file (every rendered landmark within 1 mm after reflection), a marker not at its second. It renders a clay contact sheet per clip so the author looks before Unity does.

## Gates

| Lane | Proves | Command |
| --- | --- | --- |
| DCC bake | takes meet the contract on every frame, both hands | the takes script exits non-zero otherwise |
| Sim tests | the contract itself (hand order, feet, plate crossing, clocks, catalog ↔ verbs) | `dotnet test` |
| Art catalog | every verb has a clip file, both hands where handed, player copies identical, extras resolve | `cli art` |
| Narrow compile | Runtime and Editor still compile | `tools/unity-compile.sh` |
| Unity swing matrix | rendered hands on the handle, stance, barrel through the plate, all captains, both hands | `Grand Sluggers → Capture Swing Matrix` / `-executeMethod` (see `docs/screenshot-gate.md`) |
| Look | it reads as a toy | Jack, from a still |

## What is gone

`MoveBones.Evaluate` and every procedural pose, `CharacterMotion`, `PoseClip` and `data/art/pose-clips/`, the `HeroActor` pose switch, `MirrorBoundSwing` / `MirrorArms` / `MirroredBatSocket`, `SharedRig` primitive bodies and `Face()`, the package runtime (`CharacterPackage`, `character-packages.json`, package import validation, controller sync), Fenn's package FBX set, `EYES_REVERSED_BY_IMPORT`.

## What is not in scope yet

Painted unique meshes, secondary motion (cape, ears), blend trees, IK at runtime, motion matching. Each is a later slot on this rig, not a reason to grow a second one.
