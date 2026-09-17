# What scales when the diamond shrinks — research packet (#730)

**Phase 1 of #730. Research and options only. No decision is made here, no rule is changed, and no runtime edit is proposed.**

Subjects: #725 (fielder start spots), #728 (the infield lip), #729 (ground dress), #732 (three absolute distances), and the hazard-radius question raised on PR #731.

Every figure below comes from the production classifier with counterfactual rule tables, written by `tools/game-feel-scale-probes` into `docs/research/game-feel-730-scale-derived.json` and re-verifiable with `--check`. Nothing here is a simulation, a rate, or a human gate.

---

## Why these are one question

The compact profile does not scale uniformly, and that is deliberate:

| | scale |
| --- | --- |
| infield (basepath 80 / 90) | **0.889** |
| outfield (fences) | **0.700** |

Every subject here is a length in feet that has to pick one of those, or a third thing. Answering them separately produces rules that disagree, so they are researched together.

**The lip is the hinge.** It is the infield/outfield boundary for the defence, the pop/fly line for the ball, and the seam the hazard-zone rule reads. Everything else is measured against whatever it becomes.

---

## Subject 1 — the infield lip (#728)

`flight.classes.infieldLipFt` = **155**, unchanged by the C80 trial.

### Who reads it

| Consumer | What it decides |
| --- | --- |
| `Fielding.cs:230-231` `OutfieldGrass` | whether a body is an infielder or an outfielder |
| `BattedBall.cs:215` | a fly landing inside it is a **pop** |
| `InPlay.cs:522` | whether the ball counts as held on the infield (gates the runner-hold rule) |
| `ParkDiamond.cs:416` | a dress assertion that the warning track stays outside it |

`OutfieldGrass` feeds the infielder/outfielder split that `infieldAirMul` / `outfieldAirMul` key off — the S-29 lever from #636. So this number quietly sorts the defence.

### The options, measured

Pop and fly counts over the same grid (6 parks × Power 1-10 × 4 qualities × 2 charge states × 2 lifts × 9 sprays = 17,280 classifications), in migrated parks:

| Option | Lip | Pop | Fly | of compact centre | of compact pole | hazards in a foreign zone |
| --- | --- | --- | --- | --- | --- | --- |
| shipped game, for reference | 155 | **489** | 4103 | 38.8% | 47.0% | — |
| A — unchanged | 155.00 | **976** | 3494 | 55.4% | 66.8% | 4 |
| B — basepath ×0.889 | 137.78 | 652 | 3818 | 49.2% | 59.4% | 3 |
| C — fence ×0.700 | 108.50 | **99** | 4371 | 38.8% | 46.8% | 0 |

### Two findings that decide this

**1. There is a hard floor, and option C is below it.**

The lip tells the sim who is an infielder. A middle infielder stands 125.25 ft from home. Set the lip below that and `OutfieldGrass` calls him an outfielder.

| | floor |
| --- | --- |
| if the start spots stay where they are | **125.25 ft** |
| if the start spots scale by basepath (#725) | **111.33 ft** |

Option C at 108.50 ft fails both. Measured: at that lip the outfield roster becomes `2B, SS, LF, CF, RF`. That is not a taste judgement — it moves two infielders onto the outfield air multiplier.

**2. Neither candidate rule preserves what the lip governs.**

The lip's job is the pop/fly line. The value that leaves the compact game with the shipped game's pop count is **129.2 ft** — a scale of 0.833, between the two candidates and equal to neither.

129.2 ft also clears both floors.

### What the options do not select

None of these picks a rule for the *other* subjects, and none makes the lip a fraction of the fence rather than absolute feet. Re-authoring it as a fraction is a larger change and should be decided on its own.

---

## Subject 2 — infielder start spots (#725)

`Diamond.Positions` hardcodes `1B (78,72)`, `2B (42,118)`, `3B (-78,72)`, `SS (-42,118)`.

| Position | shipped | un-migrated on C80 | scaled by basepath |
| --- | --- | --- | --- |
| 1B → first | 16.62 ft | **26.41 ft** | 14.77 ft |
| 3B → third | 16.62 ft | **26.41 ft** | 14.77 ft |
| 2B → second | 43.01 ft | 42.28 ft | 38.23 ft |
| SS → second | 43.01 ft | 42.28 ft | 38.23 ft |

The corners break; the middle infield does not, because it already plays too deep for the shrink to reach it.

### The choice the packet's own rule does not resolve

`spatialPolicy.infieldStarts` says to scale by `basepath/90`. That gives **14.77 ft** at the corners — not the 16.62 ft they play at today.

So the packet's rule preserves *position as a fraction of the diamond*, not *fielder-to-bag distance*. Those are different rules. They differ by 1.85 ft, which is a third of the 6-ft stand-up reach #719 authors.

| Option | 1B → bag | preserves |
| --- | --- | --- |
| A — leave (what ships today) | 26.41 ft | nothing |
| B — scale by basepath | 14.77 ft | position as a fraction of the diamond |
| C — preserve the gap | 16.62 ft | the fielder-to-bag distance |

### Coupling

This decides the lip's floor: 125.25 ft under A, 111.33 ft under B or C.

---

## Subject 3 — outfielder start spots (#725)

`LF (-110,250)`, `CF (0,305)`, `RF (110,250)` — further out than every migrated fence.

| Position | start radius | bearing | Harbor fence at that bearing | fraction | if the fraction is preserved | feet beyond the compact wall today |
| --- | --- | --- | --- | --- | --- | --- |
| LF | 273.13 | −23.75° | 379.16 → 265.71 | 0.7203 | **191.40** | 7.42 |
| CF | 305.00 | 0° | 400.00 → 280.00 | 0.7625 | **213.50** | 25.00 |
| RF | 273.13 | +23.75° | 379.16 → 265.71 | 0.7203 | **191.40** | 7.42 |

`spatialPolicy.outfieldStarts` says to preserve bearing and the fence-at-bearing fraction, which is the right-hand column. `FieldBounds.Clamp` currently pins them to the warning track instead, which is why extra-base hits collapse in a trial run.

Note the fence at LF/RF's bearing is 379.16, not the 330-ft pole — the fence is a circular arc through the three posts, so the fraction must be computed through `AtBatResolver.FenceAt` rather than against a pole distance.

---

## Subject 4 — the foul rate down the lines — **not a scaling subject**

`HarborWall.FoulOffset` = 36 ft and `flareStart` = 95 ft are hardcoded and do not scale. An earlier revision of this packet, #732 and the PR #731 review all named them as the cause of a measured shift in the foul rate. **That attribution is wrong, and the correction is the finding.**

The shift is real. Down-the-line contact (|spray| > 40°), same grid:

| | called foul |
| --- | --- |
| control | 582 / 1920 — **30.3%** |
| compact | 794 / 1920 — **41.4%** |

But those two constants do not cause it. Measured by patching them directly and re-running the whole grid:

| `FoulOffset` | `flareStart` | compact foul share |
| --- | --- | --- |
| 36 (shipped) | 95 (shipped) | 0.4135 |
| 36 | 66.5 (×0.70) | **0.4135** |
| 25.2 (×0.70) | 66.5 (×0.70) | **0.4135** |

Identical to four decimals. The reason is geometric: `FoulWall` flares the rail *outward* near home and converges it to zero offset at the pole, and these balls — 0.1° inside a 45° foul line — only ever interact with the rail near the pole, where the offset is ~0 whatever the constants say.

**So the foul-rate rise is a consequence of the poles moving in, not an un-migrated absolute anyone can choose to scale.** It is an effect to report in 3d, not a number to decide here.

Two things follow:

- **#732 should drop the foul wrap** from its list of absolutes. It was the headline item there and it does not belong.
- **The observation that shaped PR #731's result still stands, with a different cause.** Pulled contact that clears a wall can still be scored `Foul` for crossing the hip rail near the pole. That is compact geometry doing it, not a constant that failed to migrate.

`HarborWall.HipHeight` is 4.2 ft — a vertical, correctly unscaled for the same reason wall heights are, and range-guarded to 3.2–5.5 ft at `HarborWall.cs:206`.

## Subject 5 — the tag-up gates (#732)

`cpu.TagThirdMinCarryFt` = 200, `cpu.TagSecondMinCarryFt` = 250. Read at `RunnerAi.cs:104` and `:106`.

| Park | 250 ft ÷ compact pole | 200 ft ÷ compact centre |
| --- | --- | --- |
| canopy-yard | **1.15** | 0.75 |
| crystal-rink | 1.12 | 0.74 |
| ember-keep | 1.05 | 0.70 |
| funfair-park | 1.14 | 0.73 |
| harbor-diamond | 1.08 | 0.71 |
| rooftop-city | 1.12 | 0.74 |

250 ft is past **every** compact pole. A carry that long is a home run or a wall ball, not a catchable fly, so the tag-from-second branch becomes unreachable.

This removes a scoring path rather than shifting one, which is why it is the most likely of all these numbers to be misread in a 3d race report.

---

## Subject 6 — the cutoff threshold (#732)

`fielding.throw.onTheFlyFt` = 200. A throw shorter than this needs no relay (`LivePlaySystem.Field.cs:1439`).

Share of fair ground inside 200 ft of home:

| Park | shipped | compact |
| --- | --- | --- |
| harbor-diamond | 28.2% | **57.5%** |
| canopy-yard | 31.4% | **64.0%** |
| ember-keep | 27.1% | 55.1% |
| crystal-rink | 30.3% | 61.8% |
| funfair-park | 29.4% | 60.0% |
| rooftop-city | 30.0% | 61.1% |

The cutoff man stops existing over half the compact field. #722's exit criterion asks for the relay break-even distance to be stated; it cannot be stated honestly while this is unscaled.

---

## Subject 7 — hazard radii (raised on #731)

PR #731 left radii unscaled, reasoning that a barrel is a physical object.

### The reasoning does not describe the code

| Code | What it shows |
| --- | --- |
| `Fielding.cs:566-570` | a barrel's capture test is `radius + PipeReachPadFt` (8 ft) |
| `Fielding.cs:539-541` | a `fire_breath` radius is multiplied by `EmberNightFireMul` **1.6 at night** |
| `Fielding.cs:80` | a freeze triggers on where the **ball** lands, then slows a fielder who need not be near it |
| `Fielding.cs:597` | a `climb_wall` radius is **never read** — only the type is tested |

`radius` is a trigger zone, not an object's size.

### The options

Fair territory falls to 0.70² = **49%** of shipped, so an unscaled radius roughly doubles its share of the field. Scaling by **0.70** preserves the share exactly.

| Hazard | share shipped | unscaled | ×0.70 |
| --- | --- | --- | --- |
| ember-keep `fire_breath` r16 | 0.69% | **1.41%** | 0.69% |
| rooftop `billboard` r12 | 0.43% | 0.88% | 0.43% |
| ember-keep `lava_pit` r12 | 0.39% | 0.79% | 0.39% |
| canopy `tree` r10 | 0.31% | 0.64% | 0.31% |

Ember's fire breath is ×1.6 at night, so unscaled it reaches **3.6%** of fair territory after dark.

Note that preserving share requires the *fence* factor for every hazard, including infield ones. That deliberately disagrees with the zone rule used for hazard positions.

### The wrinkle that decides how much this matters

For barrels and pipes the radius is not what catches the ball:

| Rule | Canopy barrel radius | capture disc |
| --- | --- | --- |
| unscaled | 5.00 | **13.00 ft** |
| ×0.70 | 3.50 | 11.50 ft |

Scaling the radius shrinks the real catch by 11.5% on a field that lost 30%. **`PipeReachPadFt` dominates it**, and that value is itself in #732. The same is true of `EmberNightFireMul` for fire breath.

So this decision and those two rules-file values have to be made in one pass, or the chosen rule will not do what it looks like it does.

---

## Subject 8 — ground dress (#729)

`ParkDiamond.InnerHalf` = 50, `BackR` = 92. `spatialPolicy.groundDress` says to scale both by basepath and to preserve path width, bag pads, home pad, warning track and foul apron in feet.

**Presentation only, verified:** `flight.dirtTimeScale` is selected by `shape.OnTheDirt()` — a batted-ball class, not a ground position (`Rules.cs:828`) — and `ParkDiamond.OnDirt` has one consumer outside its own file, a wall-loop assertion (`HarborWall.cs:284`).

One coupling: `ParkDiamond.cs:416` asserts the warning track sits outside the lip, and `ParkDiamondTests:70` asserts `GrassZ1(Harbor) > InfieldLipFt`. Move the lip without the dress and those fail — correctly.

---

## What Phase 2 has to decide, in order

1. **The lip.** Everything else reads off it. The floor and the pop-preserving value are both measured above.
2. **Infielder starts.** Sets the lip's floor, and the fraction-vs-distance question is open.
3. **Outfielder starts**, and whether outfield depth becomes per-park (#713's axis).
4. **Hazard radii**, together with `PipeReachPadFt` and `EmberNightFireMul`.
5. **The tag gates and the cutoff**, before #718 and #722 measure anything against them.
6. **The dress**, judged on screen rather than by arithmetic.

## Vertical numbers are explicitly out of scope

`jumpRobFt` 4, `superJumpRobFt` 18, `clamberRobFt` 28 and `buddyJumpRobFt` 18 all compare against `FenceClearFt`, a **height** (`BattedBall.cs:149,171`). Leaving them unscaled beside unscaled walls is consistent, and "bodies did not shrink" does hold for them.

---

# Phase 2 — accepted decisions, September 16, 2026

Seven decisions, taken one at a time. **Two of them stopped being scaling questions.**

| # | Decision | Accepted |
| --- | --- | --- |
| 1 | infield lip | **137.78 ft**, basepath |
| 2 | infielder starts | scale by basepath — 1B at (69.33, 64.00) |
| 3 | outfielder starts | one global set, scaled — CF at (0, 213.50) |
| 4 | hazard radii | scale radii **and** the reach pad by 0.70 |
| 5 | tag-up gates | **retired** — a runner goes when it can beat the throw |
| 6 | relay threshold | **retired** — relay when it arrives first |
| 7 | ground dress | basepath for `InnerHalf` / `BackR`, rest preserved in feet |

`F693-04-foul-wrap` is withdrawn; see Subject 4.

## 1 — the lip: 137.78 ft

The fence scale was not declined, it was **impossible**. At 108.50 ft the sim calls 2B and SS outfielders, and it still fails the floor after the start spots scale.

Between the two survivors, basepath won because three of the lip's four consumers are geometry. **Pops rise 489 → 652, and that is reported rather than tuned away.** The 129.2 ft that would preserve today's pop count was declined as fitting a number to an economy the compact game is meant to change.

## 2 and 3 — the start spots

Infielders scale by basepath, so the infield keeps its shape exactly: each corner stays 18.5% of the basepath from its bag. Preserving the gap in feet would have put them proportionally further out, at 20.8%.

Outfielders keep one global set for all six parks, scaled — which is what happens today, so depth-as-a-fraction goes on varying by park as it already does. CF returns to 76% of centre-field depth and stops being pinned to the warning track. Per-park depth is new behaviour and stays with #713.

These values match the figures the #708 packet already cited for C80, computed independently here.

## 4 — hazard radii, and the pad with them

Radius is a trigger zone, not an object's size. Scaling radii by 0.70 preserves each hazard's share of the field exactly, because fair area scales by 0.70².

**The pad scales too**, and that is the part that matters: it is larger than a barrel's radius, so scaling the radius alone would shrink a barrel's real catch from 13.00 ft to 11.50 ft — 11.5% on a field that lost 30%. With `PipeReachPadFt` at 5.60 the capture disc is 9.10 ft, exactly 0.70 of today. `EmberNightFireMul` is dimensionless and does not change.

## 5 and 6 — two numbers retired rather than rescaled

Jack declined all four scaling options for the tag-up gates: **a tag-up is a race, not a distance.** A CPU runner should go when it judges it can beat the throw.

The machinery is already there and already used everywhere else. `RunnerAi.Margin` is `throwArrival − runnerArrival`; `ThrowArrivalSec` measures from the glove's actual position, so how far the thrower is from the bag is in the model; and `cpu.*.runnerMarginSec` already tunes judgement by difficulty, easy +0.15 s against hard −0.15 s. **The tag-up branch is the only CPU runner decision still reading a carry distance.**

One gap: `ThrowArrivalSec` passes `null` for the thrower, so `Arm` does not yet reach the estimate. #712 shipped `Arm`; wiring it in is what makes throw power count, which the direction requires.

The same reasoning retires the relay threshold. Relay when the two-leg throw arrives first, otherwise throw through. #722's exit criterion asks for the relay break-even distance — it now falls out of the throw model rather than being authored, and varies by arm.

## 7 — the dress, decided without escalation

Phase 1 verified the dress has no gameplay effect, so this took the packet's own rule rather than Jack's time. `InnerHalf` 44.44, `BackR` 81.78; path width, bag pads, home pad, warning track and foul apron stay in feet. It moves with the lip, because `ParkDiamond.cs:416` and `ParkDiamondTests:70` tie them together.

## What Phase 3 hands on

| Issue | Carries |
| --- | --- |
| #728 | the lip at 137.78 |
| #725 | both sets of start spots |
| #729 | the dress |
| #732 | the tag-up and relay retirements, and `PipeReachPadFt` |
| #717 / a follow-up | hazard radii in the trial overlay |

**No runtime change was made here.** These are recorded decisions; the implementations are the issues above.
