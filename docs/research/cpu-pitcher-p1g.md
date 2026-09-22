# The CPU pitcher on human inputs — the P1-g proposal

**Status: proposed, not accepted.** The switch is `false` on the shipped root and `true` in
`trials/pitch5`. The shipped CPU is bit for bit the one that shipped. Jack judges the trial side in
sitting 1, together with P1-d's shapes and P1-f's verb; that sitting is not possible yet, because
the mound is not wired to a seat until P1-f.

Issue [#823](https://github.com/jackguillet/grand-sluggers/issues/823), after
[#818 / #821](https://github.com/jackguillet/grand-sluggers/issues/818). Decisions: **PH-18**
(human-equivalent CPU limits), **PH-18-R1** (the CPU converts in the same trial as the new shapes,
no interim flat CPU), **PH-15-R1..R4** (repertoires), **PH-02-R3/R4/R5** (selection by presses),
**PH-03** (height is the family's), **PH-04** (bounded live steering), **PH-20-R1** (the trial
window). Spec: §4.8 (rewritten), §3, §16, Appendix B.1 S-114 … S-120.
Tests: `CpuPitcherScenarioTests`. Companion report: [`pitch-families-p1d.md`](pitch-families-p1d.md).

Run it:

```
GRAND_SLUGGERS_TRIAL=trials/pitch5 dotnet run --project src/GrandSluggers.Cli -- match --seed 7
GRAND_SLUGGERS_TRIAL=trials/pitch5 dotnet run --project src/GrandSluggers.Cli -- match --cohort s29
```

## What was wrong

`Match.CpuPitch` picked a point on the plate — an X **and a Y** — and then asked
`PitchFlight.AimForCrossing` to solve `AimX` / `AimY` so the ball arrived there. A human has no
vertical input at all (PH-03) and no plate-aim input of any kind: their location is the rubber they
walk to and the shape they choose. The CPU also got the whole ±1 of break instantly, where a hand
accumulates it a frame at a time, and it treated charge, changeup and break as four **exclusive**
verbs, so it could never charge a breaking pitch the way a player can.

PH-18-R1 says it converts in the same trial as the new shapes. This child builds that CPU behind
`pitching.cpu.humanInputs`.

## The five rules

Written out in [gameplay-spec §4.8](../gameplay-spec.md). In one paragraph each:

1. **Location is the rubber.** The count row's location becomes a *horizontal* intent in feet at the
   plate and the body walks the rubber until the family's own crossing lands on it. The crossing is
   affine in the rubber — the walk moves the release point and the aim target by the same world
   distance — so the solve is one subtraction and one divide, not a search:
   `r = (intent − X₀) / HomeSet.PitcherWalk`, with `X₀` the same delivery's crossing from the middle
   of the rubber. Everything the flight adds after the straight line (the family's sweep, the stick's
   shift, a Star's wobble) is identical at every rubber position, so it cancels. `AimX` and `AimY`
   are always 0. The walk is clamped to ±1, the legal range a hand has.
2. **No vertical intent.** The crossing height is the zone center less the family's `dropFt`. The
   old `middleYSpreadFt` term and the vertical half of the edge/waste targets are gone from this
   path; the field stays in the table for the shipped one.
3. **Family is presses.** The row's weights are filtered to the slots this pitcher can *select* — in
   the repertoire **and** authored — renormalised, rolled once, and then walked through the same
   `PitchSelection.Advance` cycle from the same SET reset a player walks. The CPU's choice is
   therefore always 0, 1 or 2 presses from the fastball.
4. **Charge and steer are modifiers.** Two independent rolls per row. A charged CPU pitch is still
   steerable, damped by `breakDampedMul` exactly as a human's is.
5. **Steer is what a held stick reaches.** `PitchFlight.BreakReach(Pitch, airSec)`, one public pure
   function so P1-f's Unity tick and the sim cannot disagree about how far a hold gets.

**The arm is stamped before the solve.** P1-d found that `CpuPitch` called `AimForCrossing` before
`PreparePitch` stamped `Throws`, which was harmless only because every shipped family sweeps 0. With
the trial's sweeps it is not harmless: the same delivery from the wrong arm misses its intent by
exactly twice the family's sweep — 1.16 ft for the slider, 0.64 for the curveball, 0.40 for the
sinker. `CpuPitchByInputs` builds the command with `Throws = Pitcher.Throws` from the start, and
`S115_AWrongArmWouldMissTheIntentByTwiceTheFamilysSweep` is the guard.

## The trial weights

Proposals. The rationale per row is in
[`trials/pitch5/README.md`](../../trials/pitch5/README.md); the table is repeated here so the
report stands alone.

| row (location) | fastball | changeup | curveball | slider | sinker | `chargeChance` | `steerChance` |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `even` (edge) | 40 | 15 | 12 | 18 | 15 | 0.20 | 0.30 |
| `ahead` (waste) | 20 | 24 | 26 | 20 | 10 | 0.15 | 0.45 |
| `behind` (middle-in) | 44 | 6 | 6 | 14 | 30 | 0.30 | 0.15 |
| `runnerTwoOuts` (middle) | 38 | 10 | 10 | 20 | 22 | 0.40 | 0.25 |

- `even` — balanced, leaning to the family that finishes *on* the corner rather than starting there:
  the slider sweeps farthest, so it arrives on the black.
- `ahead` — "low" is now a family, not an aim, so the two biggest droppers (curveball, changeup)
  carry the chase pitch and the fastball is halved. Highest steer chance in the table: this is the
  count the CPU is trying to miss the zone late.
- `behind` — needs a strike. The two fastest families with the least late movement to mislocate take
  three quarters of the row, and the steer chance is the lowest, because steering off a middle-in
  intent is how a 3-1 becomes a walk.
- `runnerTwoOuts` — "middle, fast" without being batting practice: the fast half of the library, with
  enough sweep that a middle intent is not a meatball. The charge share is the shipped row's.

The **shipped** file authors the same fields, inert, holding the port of today's exclusive mix:
`fastball = normal + charge + break`, `changeup = changeup`, `chargeChance = charge / total`,
`steerChance = break / total`. That is what the trial's `chargeChance` column still is; only the
steer chances were raised, because steering stopped being a verb that costs you the charge.

| row | shipped port | trial |
| --- | --- | --- |
| `even` | fb 85 / ch 15, charge 0.20, steer 0.20 | fb 40 / ch 15 / cu 12 / sl 18 / si 15, charge 0.20, steer 0.30 |
| `ahead` | fb 65 / ch 35, charge 0.15, steer 0.30 | fb 20 / ch 24 / cu 26 / sl 20 / si 10, charge 0.15, steer 0.45 |
| `behind` | fb 95 / ch 5, charge 0.30, steer 0.05 | fb 44 / ch 6 / cu 6 / sl 14 / si 30, charge 0.30, steer 0.15 |
| `runnerTwoOuts` | fb 100 / ch 0, charge 0.40, steer 0.10 | fb 38 / ch 10 / cu 10 / sl 20 / si 22, charge 0.40, steer 0.25 |

## The reach table

`BreakReach(Pitch, airSec) = min(1, breakRatePerSec × max(0.25, 1 + (Pitch − 5) × breakRatePerPitchStat) × airSec)`,
on the shipped `pitching.flight` (`breakRatePerSec` 2.4, `breakRatePerPitchStat` 0.12, air clamps
0.78 – 1.28 s). Feet on the plate are `BreakX × breakMaxFt` (0.46 ft), a tenth of that when charged.

| Pitch | rate (stick-units/s) | seconds to the cap | reach at `airMinSec` 0.78 | reach at `airMaxSec` 1.28 |
| --- | --- | --- | --- | --- |
| 1 | 1.248 | 0.801 | **0.973** | 1.000 |
| 2 | 1.478 | 0.677 | 1.000 | 1.000 |
| 5 | 2.400 | 0.417 | 1.000 | 1.000 |
| 10 | 3.840 | 0.260 | 1.000 | 1.000 |

**The bound is real and, on today's numbers, slack.** The shortest flight any *real* pitch has is
0.821 s (a Pitch-10 charged fastball at 103 mph; the 0.78 s floor is never reached — S-107 already
said no pitch leans on the clamps), and the slowest arm needs 0.801 s. So every steered CPU pitch
under the trial does reach the whole ±1, exactly as the shipped path handed out — **but it reaches
it because a hand holding from release would, not because the model gave it away**, and the margin
is 0.02 s at the tightest point across arms, 0.09 s for a Pitch-1 arm on its own hardest pitch
(0.891 s). Move `breakRatePerSec` below ~2.16, or shorten the flight, and the bound starts showing
on the field. `S117_…` asserts the relationship and the floor case, not the saturation.

This is the one place where "a legal CPU" is currently indistinguishable from the old CPU in the
output. It is reported, not tuned: nobody has accepted a break rate for this trial.

## S-29, shipped and trial

Fifty three-inning CPU-vs-CPU games, the five captain pairs both ways, seeds 1–5
(`cli match --cohort s29`). The accepted guardrail (F693-06) is **1.8 – 5 mean runs per side**,
applied separately to home and away. **Re-reported, never tuned.**

| | shipped root | `trials/pitch5` |
| --- | --- | --- |
| mean runs, **home** | **1.90** | **1.56** |
| mean runs, **away** | **1.92** | **2.08** |
| most runs in a game | 10 | 9 |
| singles / game | 3.74 | 4.72 |
| doubles / game | 1.44 | 1.28 |
| triples / game | 0.24 | 0.24 |
| home runs / game | 1.28 | 1.46 |
| strikeouts / game | 3.18 | 3.70 |
| **walks / game** | **2.86** | **0.98** |

The shipped column is byte-identical to `main`'s — the switch being off is the whole point.

**The trial CPU walks almost nobody, and that is why the home mean falls under the floor.** Walks per
game drop from 2.86 to 0.98, a two-thirds collapse, and the home mean lands at **1.56**, below the
accepted 1.8 floor; the away mean rises to 2.08 and stays inside the band. (Home means run low in
this cohort by construction — a three-inning game skips the home half when the home side leads.)

The cause is structural, not a tuning slip. Under the shipped endpoint model the CPU's Gaussian
scatter applies in **both** axes around a target that already sits on or off the corner, so a miss in
Y is an easy ball: the pitch simply crosses above or below the frame. Under the human-input model
**there is no Y at all** — every ordinary family crosses inside the zone vertically by construction
(P1-d's S-108) and the only way to be a ball is horizontal. Half the ways to miss disappeared with
the vertical aim, so the arm that used to walk a batter about three times a game now walks one, and
more balls are put in play (singles 3.74 → 4.72, strikeouts 3.18 → 3.70).

**Recorded, not tuned, and not asserted.** No trial band exists, because nobody accepted one; the
shipped S-29 test is untouched and green. If the sitting wants the walk back, the honest levers are
in the trial file and are all *legal* ones — a wider `scatterFtPerPitchStat`, a bigger `wasteOutFt`,
more weight on the families that leave the zone horizontally — and none of them is this child's to
pick.

A second, smaller consequence of the same rule: **the CPU's rubber moves between most pitches**,
because the rubber *is* the location. `RubberMovedSinceLastPitch` is what the CPU batter's mistrack
read keys off (§5.9), so the trial CPU batter mistracks more often against a trial CPU pitcher. That
is inside the numbers above.

## What moved, and what did not

- **The shipped root did not move.** `cli match --seed 7` is byte-identical before and after
  (empty `diff`), the S-29 cohort is identical, the #810 golden is unedited, and the #708 evidence
  files differ only in source-hash lines.
- **The trial root moved, on purpose.** `GRAND_SLUGGERS_TRIAL=trials/pitch5 cli match --seed 7`
  now differs from its own previous output from the first pitch: the CPU throws the three new
  families, so the at-bats diverge immediately. It runs to a Final.
- **The CPU's rubber now moves between most pitches**, because the rubber *is* the location. The CPU
  batter's mistrack read (§5.9) keys off `RubberMovedSinceLastPitch`, so the trial CPU batter
  mistracks more often against a trial CPU pitcher than it did. That is a consequence of the model,
  not a tuning choice, and it is part of what S-29 measures above.
- **The trial CPU's walk rate fell by two thirds and took the home S-29 mean under the floor.** The
  whole finding is in the S-29 section: losing the vertical aim lost half the ways to miss. Reported,
  not tuned. Nothing was changed to bring it back.
- **One debug-protocol row was appended**, `star-shape-moves-the-crossing`: a claim about where a
  pitch crosses can pass for hundreds of deliveries and then fail by exactly a `starShapes` value,
  because `PitchFlight.Point` applies the Star Pitch's own wobble after the family, the sweep and the
  stick. It cost a round trip here (expected 2.55 ft of height, got 3.10 — `caskballRise` 0.55). The
  rubber solve does not need the exemption, because the same wobble is in the rubber-0 probe and
  cancels; the height claim does.
- **S-113's second clause was widened.** It said the overlay is the shipped file plus three family
  rows; it is now the shipped file plus three family rows **and** the CPU switch with its weights.
  The test puts both additions back before comparing, so every other key in `pitching.json` is still
  guarded against a shipped edit that forgets the overlay.

## What is not claimed

- **No human acceptance.** Sitting 1 has not happened and cannot happen until P1-f wires the mound.
  Nothing here passes a gate.
- **No number here is accepted.** The weights, the steer chances, the break rate and the S-29 trial
  figures are all proposals or observations. `numeric_targets` in the register is untouched.
- **Nothing about the CPU batter.** `CpuSwing` is not in scope; the batting half of PH-18 is P2-g.
  Whether late legal steering can fool a committed CPU read is that child's question, not this one's.
- **Nothing about how Unity draws it.** P1-f decides whether the CPU's stick is ticked like a
  human's; `PitchFlight.BreakReach` exists so that when it is, the drawn bend and the sim's bend are
  the same number.
- **No claim that the trial CPU is better.** It is *legal*. Whether it is fun is Jack's.
