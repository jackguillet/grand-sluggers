# Handoff — batting stance: lead side (feet and hands)

Written 2026-09-11, superseding the earlier handoff on `codex/left-handed-stance`.
Everything below is measured, not inferred. Where the earlier handoff was wrong
it is called out, because acting on it would have sent the next session into
the rendered mirror path for a defect that lives in the authored take.

## Where things stand

| | |
| --- | --- |
| `main` | `1291c0a` — green, 560 tests, `unity-compile.sh` OK |
| gate branch | `codex/left-handed-stance` @ `d050231` — gate checks only, turns `main` red by design |
| fix branch | `claude/fix-handoff-md-issues-aed83e` @ see `git log`, on top of `d050231` |
| branch tests | **574 passed, 0 failed** |
| `unity-compile.sh` | OK (`UNITY_PACKAGE_ASSEMBLIES` pointed at the primary checkout's `unity/Library/ScriptAssemblies`) |
| Blender bakes | `swing.fbx` and Fenn's `chargeSwing` / `swing` re-baked, every per-frame falsifier green |
| Unity swing matrix | **56/56, `ok: true`** on the fix branch with both new checks live — see **Matrix run** |
| Jack's look gate | **open** — see **Look gate** below |

Merge the fix branch, which carries the gate checks with it, so `main` is never
red. Do not merge `codex/left-handed-stance` on its own.

## The contract (Jack's words, plus his reference photo)

> Right-handed batters start with the bat hovering above their right shoulder.
> The feet face the plate. The left hand is below the right.
> For left-handed batters, the bat starts hovering above the left shoulder,
> feet face the plate, right hand is below left.

Stated once for both hands: **the side opposite the batting hand leads.** That
hand holds the knob end of the handle under the top hand, and that foot stands
nearer the pitcher. A left-handed batter is the right-handed pose reflected
across the plate line. `BattingStance.LeadSide(bats)` names it in the Sim and
every check below is written against it, never against "left" or "right".

## What was actually wrong — measured in Blender

`tools/blender/hero_shared_swing.py` builds the blockout, authors the stance
and solves the hands per frame. Evaluating the right-handed take headlessly
and converting DCC to Unity shared-root axes with `(−bx, bz, −by)` gives, at
ready (`t=0`):

| | before (`d050231`) | after (fix branch) |
| --- | --- | --- |
| chest (Stripe − torso) | (1, 0, 0) — on the plate | (1, 0, 0) |
| left toe direction | (−0.95, 0, −0.33) — **away from the plate** | (0.95, 0, 0.33) |
| right toe direction | (−0.99, 0, −0.16) — **away from the plate** | (0.99, 0, 0.16) |
| rShoe − lShoe | (0, 0, +1) — **right foot toward the pitcher** | (0, 0, −1) — left foot forward |
| lHand along the handle from the knob | 0.57 ft (top hand) | 0.35 ft (knob hand) |
| rHand along the handle from the knob | 0.35 ft (knob hand) | 0.57 ft (top hand) |

The same numbers hold on every key; the toes stay within 35° of the plate
direction through follow-through on the fixed take.

### 1. Feet — every captain, not left-handed only

`batting_stance.feet_axis` was `rShoe − lShoe`, aimed at the catalog's
`feetAxis` `(0,0,1)`. That yaws the root until the **right** foot is toward
the pitcher, which on a right-handed batter is the hips turned out of the box
with both toes pointing away from the plate. The torso is aimed separately on
its own bone, so the chest still landed on the plate — twisted half a turn over
the hips. Chest, eyes, and the bat were right; the lower body was backwards.

`HeroActor.MirrorBoundSwing` reflects that pose exactly for a left-handed
batter (the gate reads chest −1, feet −1: a clean mirror), so lefties stood
the same way. Jack saw it on Ashlord, whose block shoes show a toe direction;
Rio's dropped sneakers do not. The earlier handoff's "right-handers confirmed
correct" was the upper body.

**Fix:** `feetAxis` now means *from the back foot to the lead foot*, which
points at the pitcher from either box. `BattingStance.Key.FeetAxis` documents
it, `batting_stance.feet_axis(left, right, bats)` aims the root at it, and
`HeroActor.TryRenderedBattingStance` reports the same line. The rendered mirror
path is untouched and correct.

### 2. Hands — every captain

`SwingPresentation.Keys` had `LeftHand` / `RightHand` swapped on **all five
keys**. The earlier handoff read hand height and concluded Approach and
Contact were "already the right way up" and that the order flipped mid-swing.
That is an artifact: the barrel comes level at approach, so height stops
saying which hand is low. Measured **along the handle from the grip socket**,
the right hand sat nearest the knob on every key (0.22–0.35 ft) and the left
hand above it (0.57–0.71 ft). A plain swap of the two hands at every key is a
consistent two-handed grip.

Fenn authors his own grip in `hero_fenn.py` and had the right hand at the knob
too (`grip + 0.04` vs `0.20`).

**Fix:** hands swapped in `SwingPresentation.Keys`, `HAND_TARGETS` in
`hero_shared_swing.py`, and Fenn's two targets; both takes re-baked. No key
position changed, only which hand owns it, so the IK solve lands on the same
points (`missed = 0.0` on every frame).

## What is falsified now

| layer | check |
| --- | --- |
| Sim (`BattingStanceHandednessTests`) | `LeadFootStandsTowardThePitcherForBothHands` (signed FeetAxis on four keys), `LeadHandHoldsTheKnobEndThroughTheWholeSwing` (`SwingPresentation.HandAlongHandle` on 101 samples, both hands), `LeadHandRidesUnderTheTopHand` at load, `LeadSideIsOppositeTheBattingHand` |
| Sim (`SwingPresentationTests`) | the ready/load stance test dropped its `Math.Abs` on the feet |
| DCC (`batting_stance.validate_visible_stance`) | feet dot is signed; with `arm_ob` each toe must be within 45° of the plate direction. `hero_shared_swing.py` also raises if the left hand is not nearest the knob on any frame |
| Unity (`StillCapture` swing matrix) | signed back-to-lead feet dot, and the drawn `lHand` / `rHand` stack read by lead side |

`lHand`, `lShoe` **are** the batter's left. The blockout places them at Blender
−X with the character facing +Y; the FBX import X reflection cancels against
the `axis_forward="-Z"` export facing. The earlier "landmark names are
reflected" story is wrong and is what produced a hand check that agreed with
its own bug.

## Files changed on the fix branch

- `src/GrandSluggers.Sim/SwingPresentation.cs` — keys swapped, `HandAlongHandle`, `BattingStance.LeadSide`, `FeetAxis` doc
- `src/GrandSluggers.Sim.Tests/BattingStanceHandednessTests.cs`, `SwingPresentationTests.cs`
- `tools/blender/batting_stance.py` — lead-side feet axis, signed validate, toe check
- `tools/blender/hero_shared_swing.py`, `tools/blender/hero_fenn.py` — hand targets, falsifiers
- `unity/Assets/Scripts/Runtime/HeroActor.cs` — back-to-lead feet line
- `unity/Assets/Scripts/Runtime/StillCapture.cs` — lead-side hand check, messages
- `unity/Assets/Art/Animation/Clips/swing.fbx` + Resources copy
- `unity/Assets/Art/Characters/fenn/fenn-chargeSwing.fbx`, `fenn-swing.fbx` + Resources copies (body, idle, pose untouched)

## Measuring the take without Unity

This is what settled it in minutes and is worth keeping:

```sh
/opt/homebrew/bin/blender -b --python measure_take.py
```

where the script imports `hero_shared_swing`, calls `hero.build_scene()`,
then per key sets `pose_at(t)`, `batting_stance.author_visible_stance(...)`,
`solve_rendered_hands(...)`, and prints `rendered_center` of `lShoe`, `rShoe`,
`Stripe`, `torsoMesh`, `lHand`, `rHand` converted with `(−bx, bz, −by)`. Toe
direction is shoe center minus the shin bone tail. Read this before reasoning
about chirality from a still or a renderer name.

## Matrix run

Launching the GUI editor with `-executeMethod` runs the capture on open
without clicking the menu:

```sh
GS_STILL_REQUEST_FILE=/path/to/swing-request.json \
  /Applications/Unity/Hub/Editor/6000.5.9f1/Unity.app/Contents/MacOS/Unity \
  -projectPath /path/to/worktree/unity \
  -executeMethod GrandSluggers.EditorTools.StillGateMenu.CaptureRequestFile \
  -logFile /path/to/unity-editor.log &
```

Request file:

```json
{"shots":["swing-matrix"],"swingCaptains":["rio","vale","brondo","fenn","zig","konga","ashlord"],"hudOff":true,"width":1920,"height":1080}
```

Results land in `unity/Temp/gs-still-done.json`, PNGs in `unity/Temp/gs-stills/`.
With `unity/Library` already imported the whole run, launch to done file, took
thirty seconds.

Run on the fix branch (this worktree, fresh `unity/Library`, import plus
capture took about two minutes): **`ok: true`, 56/56**, with the signed
back-to-lead feet check and the lead-hand check both live. The JSON is filed
here as `fix-lead-side.json`. Normal-power rows:

| captain | bats | ready: back-to-lead z | ready: lHand y / rHand y | low hand at ready |
| --- | --- | --- | --- | --- |
| rio | R | +0.996 | 2.530 / 2.699 | left |
| vale | R | +1.000 | 3.486 / 3.718 | left |
| brondo | R | +1.000 | 2.699 / 2.879 | left |
| fenn | R | +1.000 | 1.745 / 1.839 | left |
| zig | L | +1.000 | 1.679 / 1.574 | right |
| konga | L | +1.000 | 3.898 / 3.654 | right |
| ashlord | L | +1.000 | 4.318 / 4.048 | right |

On the gate-only branch the same rows read −1.000 for the three lefties (and
+1.000 for the righties only because the line was right-minus-left on a take
with the right foot forward). At contact the *top* hand is the lower one by a
few hundredths on every captain — the barrel is level, so height says nothing
there; the Sim reads the knob hand along the handle instead.

Unity clears `unity/Temp/` when the editor quits, so copy the PNGs and the
done file out before quitting.

Hard-won notes (still true):

- **Exactly one editor instance.** Two on the same project wedges the capture
  silently. Check with `pgrep -x Unity` first.
- A fresh worktree imports the whole project on first open; budget ten minutes
  before the capture starts.
- `osascript -e 'tell application "Unity" to quit'` deleted `unity/Temp/`
  and then hung with the process alive. From a non-interactive session
  System Events keystrokes are refused ("osascript is not allowed to send
  keystrokes"), so Cmd+Q is not available there; `pkill -x Unity` (SIGTERM)
  quit it cleanly both times once the capture had finished.
- Unity rewrites some `.fbx.meta` files on import. Do not commit that churn —
  `git checkout -- unity/Assets` before staging.
- Tinting hand meshes to tell them apart: `material.color` does nothing under
  URP, set `_BaseColor`. Do not use red on rio — its jersey is red.

## Look gate (Jack)

One assembled comparison, same framing before and after, both hands:
`look-gate-before-after.png` in this folder (also attached to the PR). Before
is `main` `1291c0a`; after is the fix branch. Rio (bats R) and Ashlord (bats L)
at normal ready and contact.

The one question: **do the toes point at the plate, and is the lead hand — the
side opposite the batting hand — the low hand on the handle?** Rio: left foot
forward, left hand low. Ashlord: right foot forward, right hand low.

Agents do not pass this gate. Merge only after Jack says the stance reads.

Fenn's batting takes changed under `Art/Characters/fenn/`; his body, idle and
pose did not. `docs/screenshot-gate.md` still asks for `char-fenn-rest` and
`char-fenn-pose` before a player rebuild.

## Still open, unrelated to this work

- Another session pushed `c75c8a4` straight to `main` mid-work with an
  unverified fix that did not actually clear the gate. Same pattern as the
  `add all` commit that once blocked every branch in the repo.
- Worktrees with uncommitted work, preserved on purpose: `wt-merge-537-538`
  (166 files), `wt-pitch-hit-motion` (17), `wt-grand-sluggers-review-0910`
  (untracked `docs/reviews/`, `tools/review-probe/`).
