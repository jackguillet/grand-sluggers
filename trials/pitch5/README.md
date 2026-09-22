# `pitch5` — the duel trial: the five-family library, the CPU that throws it, and the one window

**This overlay is now the Phase 1 *and* Phase 2 duel trial.** Jack runs one overlay at a time, and
sitting 1 is on this one, so the batting window joined it rather than forcing a second build. Two
files, two sittings: sitting 1 judges the mound (the three shapes and the CPU), sitting 2 judges the
plate (the one timing window, and ordinary swings that ignore the stick at contact).

The trial overlay the three unauthored pitch families are proposed in
([#818](https://github.com/jackguillet/grand-sluggers/issues/818)), **the CPU pitcher converts to
human inputs in** ([#823](https://github.com/jackguillet/grand-sluggers/issues/823)) **and the one
shared timing window is proposed in**
([#844](https://github.com/jackguillet/grand-sluggers/issues/844)).
Mechanism: [#716](https://github.com/jackguillet/grand-sluggers/issues/716).
Decisions: **PH-20-R1** — a new pitch's numbers live in a trial overlay until Jack has judged them
in a trial window; **PH-18-R1** — the CPU pitcher converts to human inputs *in the same trial as the
new shapes*, and the shipped CPU does not change before then; **PH-10-R1** — one shared timing
window, starting from 9 frames, judged in a trial window (with **PH-11-R1**, **PH-15-R7**,
**PH-17**).

The shipped library authors two families, the fastball and the changeup. `curveball`, `slider` and
`sinker` are library ids with no row: they stop the pitch by name and the SET selection skips them
(#810, #812). This overlay is the shipped `rules/pitching.json` with three rows added, so those
three fly — under this trial and nowhere else.

It is also where `cpu.humanInputs` is `true`, so under this overlay the CPU pitcher builds its pitch
from the rubber, the cycle presses, the charge and a stick it actually holds, instead of solving a
plate endpoint with a height no hand can input. On the shipped root the switch is `false` and the
CPU is bit for bit the one that shipped.

And it is where `batting.window.shared` is `true`. Under this overlay every hitter, a quick swing
and a charged swing, and EASY / NORMAL / HARD alike are judged in **one window of 9 frames** at
60 Hz (±4.5 frames, 150 ms), instead of slap 9 / charge 7 ± (Contact − 5) × 0.4 × the difficulty
rung. On the shipped root the switch is `false` and the split window is bit for bit the one that
shipped.

And it is where `batting.geometryOnly` is `true` ([#855](https://github.com/jackguillet/grand-sluggers/issues/855),
PH-12, PH-18): an ordinary swing — not a bunt, not a Star Swing — ignores the stick at contact, so
timing, contact, pitch height and the swing decide the ball, and the CPU batter draws no aim for it.
The stick still walks the box. Report: [`docs/research/stick-shaping-p2c.md`](../../docs/research/stick-shaping-p2c.md).

**Every number here is a proposal.** Nothing in it has been accepted, no sitting has happened, and
no register decision records a numeric target for any of it *except* the window's 9 frames, which
PH-10-R1 records as a **trial start value** (`trial-accepted`) and not as a shipping number. The
reasoning, the measured relationships and the plots are in
[`docs/research/pitch-families-p1d.md`](../../docs/research/pitch-families-p1d.md) (the shapes),
[`docs/research/cpu-pitcher-p1g.md`](../../docs/research/cpu-pitcher-p1g.md) (the CPU) and
[`docs/research/plate-window-p2b.md`](../../docs/research/plate-window-p2b.md) (the window).

## Running it

```
GRAND_SLUGGERS_TRIAL=trials/pitch5 dotnet run --project src/GrandSluggers.Cli -- art
GRAND_SLUGGERS_TRIAL=trials/pitch5 dotnet run --project src/GrandSluggers.Cli -- match --seed 7
```

and, once the mound is wired to a seat (P1-f):

```
python3 tools/local-player.py --trial trials/pitch5
```

The same binary, the same seed, one variable — so the control and the trial are two runs to diff
rather than two tables inside one run. Every `cli` run prints the root and the overlay it read on
stderr, which is how a trace is attributed after the fact.

Unset, this folder does nothing at all. A checked-in trial must not touch a run that did not ask
for it: without the variable the three rows do not exist, `Of("slider")` stops by name exactly as
it does today, `batting.window.shared` is `false`, and the shipped game is bit for bit the game it
was.

## What is in it

```
trials/pitch5/rules/pitching.json    overrides data/rules/pitching.json
trials/pitch5/rules/batting.json     overrides data/rules/batting.json
```

Two files, each the **whole** shipped file — a trial writes whole files, never fields — with named
changes and nothing else.

`pitching.json` carries two named additions: three keys under `families`, and the CPU switch with
its trial weights. A test (`PitchFamilyTrialScenarioTests.S113_…`) parses both copies, puts both
additions back, and compares what is left, so a shipped edit that forgets this overlay fails loudly
instead of quietly making the trial measure a third thing.

`batting.json` changes exactly **two keys**: `window.shared` and `geometryOnly`, each `false` →
`true`. Every other number in it — `frames`, `slapFrames`, `chargeFrames`, `framesPerContact`,
`floorFrames`, `squareFraction`, `leadSec`, the cursor, the exit table, the launch bands, both
`stickDeg`s, the CPU batter's rolls — is the shipped one, and `SharedWindowScenarioTests.S126_…`
restores both keys and deep-compares the two files so a shipped edit that forgets this copy fails
the same way.

What that means in practice: the release point, the air-time clamps, `breakMaxFt`, the star shapes,
the stamina pool, the CPU's locations in feet, its scatter, its pickoff read, its Star chance and
every other number in `pitching.json` are the shipped ones; the cursor, the exit table and the CPU
batter's rolls in `batting.json` are too. This trial measures three shapes, one CPU, one window and
one stick rule, and nothing else.

## The CPU switch and its weights

`cpu.humanInputs: true`. Under it the CPU's pitch is built from the inputs a hand has and nothing
else — the five rules are in [gameplay-spec §4.8](../../docs/gameplay-spec.md). Each count row keeps
its shipped `normal` / `charge` / `changeup` / `break` weights (inert here) and adds a `families`
block plus `chargeChance` and `steerChance`. Weights are **shares**, filtered at run time to the
slots this pitcher can select, so a row may weight a family nobody on the roster owns.

| row (location) | fastball | changeup | curveball | slider | sinker | `chargeChance` | `steerChance` | why |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `even` (edge) | 40 | 15 | 12 | 18 | 15 | 0.20 | 0.30 | Balanced, leaning to the family that finishes *on* the corner: the slider sweeps farthest, so it arrives on the black rather than starting there. |
| `ahead` (waste) | 20 | 24 | 26 | 20 | 10 | 0.15 | 0.45 | "Low" is now a family, not an aim: the two biggest droppers, curveball and changeup, carry the chase pitch, and the fastball is halved. The highest steer chance in the table — this is the count the CPU is trying to miss the zone late. |
| `behind` (middle-in) | 44 | 6 | 6 | 14 | 30 | 0.30 | 0.15 | Needs a strike: the two fastest families with the least late movement to mislocate — fastball and sinker take three quarters of the row — and the lowest steer chance, because steering off a middle-in intent is how a 3-1 becomes a walk. |
| `runnerTwoOuts` (middle) | 38 | 10 | 10 | 20 | 22 | 0.40 | 0.25 | "Middle, fast" without being batting practice: the fast half of the library, with enough sweep that a middle intent is not a meatball. The charge share is the shipped row's. |

The shipped file authors the same fields with the **port of today's exclusive mix** — fastball takes
every verb that was not the changeup, `chargeChance` = charge / total, `steerChance` = break / total
— so the trial's starting point is written down rather than invented. Those values are inert while
the switch is off, and `CpuPitcherScenarioTests.S120_…` holds them to the arithmetic.

## The three rows

| | `curveball` | `slider` | `sinker` |
| --- | --- | --- | --- |
| role (PH-02-R2) | pronounced arc and drop | sideways movement that challenges coverage | a faster dipping alternative to the curveball |
| `mph` / `chargeMph` | 72.5 / 4 | 78 / 5 | 81 / 6 |
| `dropFt` | 0.9 | 0.3 | 0.55 |
| `sweepFt` (+ = glove side) | +0.32 | +0.58 | −0.2 |
| `sweepFrom` | 0.42 | 0.45 | 0.55 |
| `offSpeed` | true | false | false |
| `staminaCost` | 2 | 2 | 1 |

`sweepFt` and `sweepFrom` are the natural-sweep fields the flight gained in #818 (spec §4.2): feet
the crossing ends off the straight line from the rubber, toward the pitcher's glove side, mirrored
by the throwing hand, and where in the flight that movement starts to show. They are not scaled by
the stick and not damped by a charge; the player's steering adds to them under its own unchanged
cap. The shipped rows author `sweepFt 0`, so the shipped flight is unchanged.

## The window switch

`batting.window.shared: true`. Under it `AtBatResolver.ContactWindowFrames` is

```
frames = window.frames (9)            one number for every hitter and both swings
       × the Star Pitch's batterWindowMul (PH-16-R18 removes this later, not here)
       × the park's nightContactWindowMul (not selected; stays)
       floored at window.floorFrames (5)
```

and the rung's `humanWindowMul` is **not** in it: one fixed challenge is PH-17, so EASY, NORMAL and
HARD judge a pad's swing in the same window. What that removes, compared to the shipped root:

| | shipped root | `trials/pitch5` |
| --- | --- | --- |
| quick swing, Contact 5 | 9.0 frames | 9.0 frames |
| charged swing, Contact 5 | 7.0 | 9.0 |
| quick swing, Contact 2 | 7.8 | 9.0 |
| quick swing, Contact 10 | 11.0 | 9.0 |
| the same swing on EASY / HARD | ×1.3 / ×0.9 | unchanged |

The spatial half of the plate is untouched: the cursor still narrows for a charge (`cursor.chargeMul`
0.8, PH-11-R1) and still widens with Contact (`cursor.scalePerContact` 0.04, PH-15-R7). A charge
still costs something and a better-Contact hitter is still more forgiving — in the oval, which is
where those two decisions put it. The CPU batter's timing error, `(11 − Contact) × 0.62 × the rung`,
is its execution skill and not the window; it is unchanged here and is P2-g's question.

The frames table computed from the sim, the S-29 cohort before and after, and what is *not* claimed
are in [`docs/research/plate-window-p2b.md`](../../docs/research/plate-window-p2b.md).

## What has not been decided

Whether any of these numbers is right. The mound is Jack's in the trial window (sitting 1), and the
mound is not wired to a seat until P1-f. The window is Jack's in **sitting 2**: whether 9 is right,
whether a quick and a charged swing feeling the same is what he wants, and whether the difficulty
rungs should have lost the window. Until then this overlay is a thing to measure, not a thing to
play.
