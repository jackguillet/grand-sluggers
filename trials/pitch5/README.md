# `pitch5` — the five-family library

The trial overlay the three unauthored pitch families are proposed in
([#818](https://github.com/jackguillet/grand-sluggers/issues/818)).
Mechanism: [#716](https://github.com/jackguillet/grand-sluggers/issues/716).
Decision: **PH-20-R1** — a new pitch's numbers live in a trial overlay until Jack has judged them
in a trial window.

The shipped library authors two families, the fastball and the changeup. `curveball`, `slider` and
`sinker` are library ids with no row: they stop the pitch by name and the SET selection skips them
(#810, #812). This overlay is the shipped `rules/pitching.json` with three rows added, so those
three fly — under this trial and nowhere else.

**Every number here is a proposal.** Nothing in it has been accepted, no sitting has happened, and
no register decision records a numeric target for any of it. The reasoning, the measured
relationships and the plots are in
[`docs/research/pitch-families-p1d.md`](../../docs/research/pitch-families-p1d.md).

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

One file. It is the **whole** shipped file — a trial writes whole files, never fields — plus three
keys under `families`. A test
(`PitchFamilyTrialScenarioTests.S113_…`) parses both copies, removes those three rows, and compares
what is left, so a shipped edit that forgets this overlay fails loudly instead of quietly making
the trial measure something else.

What that means in practice: the release point, the air-time clamps, `breakMaxFt`, the star shapes,
the stamina pool, the CPU's pitch mix and every other number in `pitching.json` are the shipped
ones. This trial measures three rows and nothing else.

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
