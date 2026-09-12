# Handoff — batting stance: reversed lefty feet, swapped hands

Written 2026-09-11. Everything below is measured, not inferred. Where a previous
conclusion was wrong it is called out, because two of them cost real time.

## Where things stand

| | |
| --- | --- |
| `main` | `1291c0a` — green, 560 tests, `unity-compile.sh` OK |
| working branch | `codex/left-handed-stance` @ `e82aa9e`, pushed, **not merged** |
| worktree | `/private/tmp/wt-gs-hand` |
| branch tests | **567 passed, 2 failed** — the 2 failures are deliberate |
| primary checkout | `/Users/jack/repos/grand-sluggers` on `main` @ `1291c0a` |
| standalone running | `main-1291c0ad7a` (built by `tools/local-player.py`) |
| Unity editor | not running |

The branch is gate-work only. Jack asked for **gate checks first, fix second**,
so the checks that expose the defects are committed and the defects themselves
are untouched. Do not merge this branch until the fixes land with it — it turns
`main` red by design.

## The contract (Jack's words, plus his reference photo)

> Right-handed batters start with the bat hovering above their right shoulder.
> The feet face the plate. The left hand is below the right.
> For left-handed batters, the bat starts hovering above the left shoulder,
> feet face the plate, right hand is below left.

The bottom hand on the handle is the **lead** hand — the one opposite the
batting side.

## Two independent defects

| defect | who | where it lives | caught by |
| --- | --- | --- | --- |
| feet reversed | left-handed only (zig, konga, ashlord) | rendered body | signed feet dot in the matrix |
| hands swapped | **all seven** | authored Sim data | `LeadHandRidesUnderTheTopHand` |

### 1. Feet — left-handed only

`BattingStance` authors this **correctly**: `FeetAxis` is `(0,0,1)` at every key
and `MirrorX` leaves Z alone, so both batters set their feet the same way down
the pitch line. Handedness turns the chest and swaps the low hand; it does not
reverse the feet. `BattingStanceHandednessTests` pins that and passes.

The rendered left-handed body disagrees. Measured at ready, normal power:

| captain | bats | signed `feetAlongPitch` | matrix rows |
| --- | --- | --- | --- |
| rio, vale, brondo, fenn | R | **+1.00** | 8/8 |
| zig, konga, ashlord | L | **−1.00** | 4/8 |

12 failures: 3 captains x 2 powers x ready and load. Chest and eyes are correct
for all seven.

**The fix is in the rendered mirror path, not the data.** Start at
`HeroActor.MirrorBoundSwing()` — it mirrors thighs and shins through
`MirrorLocal`, which swaps the two legs' rotations and negates Y and Z. A true
reflection swaps left/right foot identity, which is exactly what makes
`footR - footL` flip sign. The authored contract says it should not flip.

### 2. Hands — all seven captains

`SwingPresentation.Keys` has the two hands swapped. At `LoadAt`:

    LeftHand  (0.300, 2.727, 0.512)
    RightHand (0.080, 2.522, 0.326)   <- right hand is the LOW hand

A right-handed batter needs the left hand low. `Mirror()` reproduces the error
flipped, so left-handers get it too.

**There is a second problem in the same table: the hand order flips mid-swing.**

| key | left y | right y | lower | RHB needs |
| --- | --- | --- | --- | --- |
| LoadAt | 2.727 | 2.522 | right | left |
| LaunchAt | 2.356 | 2.240 | right | left |
| ApproachAt | 1.729 | 1.798 | **left** | left |
| ContactAt | 1.856 | 1.914 | **left** | left |
| FollowThroughAt | 2.195 | 2.155 | right | left |

A two-handed grip cannot swap hands mid-swing. So this is **not** a blanket
"swap every key" fix — Approach and Contact are already the right way up.
Swapping only Load, Launch and Follow makes all five agree, but that needs
checking against the baked take rather than being assumed.

**Important:** these keys are described in-file as *"Evaluated rendered-hand
centers... in shared-root space"* — they are the expected measurements of the
baked `swing.fbx`, not the drivers of it. Changing the numbers alone will make
the Sim contract disagree with the FBX, and the matrix checks both hands meet
the physical handle (`leftHandleContact` / `rightHandleContact`). Expect this to
require re-authoring the take in `tools/blender/hero_shared_swing.py` and
re-baking, then updating these keys to match. That is look-gated work.

## What is committed on the branch

- `unity/Assets/Scripts/Runtime/StillCapture.cs`
  - feet dot is **signed** (was `Mathf.Abs`, which made −1.00 score identically
    to +1.00 — that is how a fully green 56/56 run hid the lefty defect)
  - lead-hand check: the hand opposite the batting side must sit lower
- `src/GrandSluggers.Sim.Tests/BattingStanceHandednessTests.cs` — 9 tests, 7 pass,
  2 fail on purpose (`LeadHandRidesUnderTheTopHand` for both hands)
- `scratchpad/validation/left-handed-stance/` — gate JSON, stills, this file

## Two wrong turns — do not repeat them

**1. "The landmark names are reflected, so `lHand` is the batter's right."**
Wrong. `hero_shared_blockout.py:141` places `lHand` at Blender **X −0.95** with
the character facing **+Y**, so it is anatomically the left hand in DCC. The FBX
exports `axis_forward="-Z"`, so the character faces Unity **−Z**, and that facing
flip **cancels** the import X reflection. `lHand` is the batter's left hand in
Unity, as named. Acting on the reflection story produced a hand check that
compared the drawn stack against the authored key — which agreed with the bug by
construction and passed all seven captains.

**2. "All seven captains have the lead foot wrong."** Wrong, and it came from the
same bad assumption. Jack confirmed right-handers look correct; they are the
anchor for the feet sign. Only the hands are wrong on all seven.

When chirality is in question, read `tools/blender/hero_shared_blockout.py` for
the authored side and the export axes. Do not reason from the renderer name.

## Running the swing matrix (this is fiddly)

Personal Unity cannot `-batchmode`. The editor must be open **on the worktree
under test** — an editor open on the primary checkout will measure `main`.

```sh
cat > /private/tmp/wt-gs-hand/swing-request.json <<'JSON'
{"shots":["swing-matrix"],"swingCaptains":["rio","vale","brondo","fenn","zig","konga","ashlord"],"hudOff":true,"width":1920,"height":1080}
JSON

GS_STILL_REQUEST_FILE=/private/tmp/wt-gs-hand/swing-request.json \
  /Applications/Unity/Hub/Editor/6000.5.9f1/Unity.app/Contents/MacOS/Unity \
  -projectPath /private/tmp/wt-gs-hand/unity &
```

Then menu **Grand Sluggers → Capture Request File**. Results land in
`unity/Temp/gs-still-done.json`, PNGs in `unity/Temp/gs-stills/`.

Hard-won notes:

- **Exactly one editor instance.** Two on the same project wedges the capture
  silently — stills stop being written and no done file appears. Check with
  `pgrep -x Unity` before clicking.
- **A fresh editor launch is the reliable path.** Clicking the menu while Play is
  already running only stops Play; the queued `delayCall` is often lost. If no
  done file appears within ~60s, quit and relaunch rather than clicking again.
- `osascript -e 'tell application "Unity" to quit'` is frequently ignored.
  Sending **Cmd+Q** via System Events works.
- Unity rewrites some `.fbx.meta` files on import. Do not commit that churn —
  `git checkout -- unity/Assets` before staging.
- Tinting hand meshes to tell them apart: `material.color` does nothing under
  URP, set `_BaseColor`. And do not use red on rio — its jersey is red.

## Suggested order for the next session

1. **Hands first.** It is Sim data plus a Blender re-bake, and
   `LeadHandRidesUnderTheTopHand` falsifies it without the editor. Resolve the
   mid-swing flip at the same time — all five keys should agree on which hand is
   low.
2. **Then the lefty feet**, in the rendered mirror path.
3. **One matrix run over all seven captains**, expecting `ok: true` with the
   signed feet check and the lead-hand check both live.
4. **Then Jack's look gate.** Present it as a single assembled comparison he can
   just look at — both hands, labelled, before and after in the same framing. He
   has asked for this explicitly; do not hand him file paths or a raw JSON.
5. Merge the fixes and the gate checks **together**, so `main` is never red.

## Still open, unrelated to this work

- Another session pushed `c75c8a4` straight to `main` mid-work with an
  unverified fix that did not actually clear the gate. Same pattern as the
  `add all` commit that once blocked every branch in the repo.
- Worktrees with uncommitted work, preserved on purpose: `wt-merge-537-538`
  (166 files), `wt-pitch-hit-motion` (17), `wt-grand-sluggers-review-0910`
  (untracked `docs/reviews/`, `tools/review-probe/`).
