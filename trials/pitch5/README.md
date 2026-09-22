# `pitch5` — the five-family library, and the CPU that throws it

The trial overlay the three unauthored pitch families are proposed in
([#818](https://github.com/jackguillet/grand-sluggers/issues/818)) **and the CPU pitcher converts to
human inputs in** ([#823](https://github.com/jackguillet/grand-sluggers/issues/823)).
Mechanism: [#716](https://github.com/jackguillet/grand-sluggers/issues/716).
Decisions: **PH-20-R1** — a new pitch's numbers live in a trial overlay until Jack has judged them
in a trial window; **PH-18-R1** — the CPU pitcher converts to human inputs *in the same trial as the
new shapes*, and the shipped CPU does not change before then.

The shipped library authors two families, the fastball and the changeup. `curveball`, `slider` and
`sinker` are library ids with no row: they stop the pitch by name and the SET selection skips them
(#810, #812). This overlay is the shipped `rules/pitching.json` with three rows added, so those
three fly — under this trial and nowhere else.

It is also where `cpu.humanInputs` is `true`, so under this overlay the CPU pitcher builds its pitch
from the rubber, the cycle presses, the charge and a stick it actually holds, instead of solving a
plate endpoint with a height no hand can input. On the shipped root the switch is `false` and the
CPU is bit for bit the one that shipped.

**Every number here is a proposal.** Nothing in it has been accepted, no sitting has happened, and
no register decision records a numeric target for any of it. The reasoning, the measured
relationships and the plots are in
[`docs/research/pitch-families-p1d.md`](../../docs/research/pitch-families-p1d.md) (the shapes) and
[`docs/research/cpu-pitcher-p1g.md`](../../docs/research/cpu-pitcher-p1g.md) (the CPU).

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
it does today, and the shipped game is bit for bit the game it was.

## What is in it

```
trials/pitch5/rules/pitching.json    overrides data/rules/pitching.json
```

One file. It is the **whole** shipped file — a trial writes whole files, never fields — plus two
named additions: three keys under `families`, and the CPU switch with its trial weights. A test
(`PitchFamilyTrialScenarioTests.S113_…`) parses both copies, puts both additions back, and compares
what is left, so a shipped edit that forgets this overlay fails loudly instead of quietly making
the trial measure a third thing.

What that means in practice: the release point, the air-time clamps, `breakMaxFt`, the star shapes,
the stamina pool, the CPU's locations in feet, its scatter, its pickoff read, its Star chance and
every other number in `pitching.json` are the shipped ones. This trial measures three shapes and one
CPU, and nothing else.

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

## What has not been decided

Whether any of these numbers is right. That is Jack's, in the trial window (sitting 1), and the
mound is not wired to a seat until P1-f. Until then this overlay is a thing to measure, not a thing
to play.
