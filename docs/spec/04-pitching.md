# 4. Pitching

## 4.1 The four verbs

Same shape as the swing: tap / charge / modifier / star. Booklet-confirmed contract.

| Verb | Input | Ball |
| --- | --- | --- |
| Normal | Tap and release RT | Pitcher's base fastball, easiest control |
| Charge | Hold RT to MAX (`pitchChargeSeconds` 0.55), release inside the MAX band (`chargeMaxHoldSeconds` 0.5) | +mph. Released inside the first 0.25 s of MAX (`pitching.release.niceBandSec`) = **Nice!** (+5%, `niceMul`; `PitchCommand.Nice`). Past the band the charge decays (`chargeOverchargeDecay`) toward a normal pitch. A charged pitch takes only 10% of the break (`flight.breakDampedMul`; reference: charge and changeup are "essentially straight") ✅ P1 |
| Cycle pitch | **West in SET, before the charge** | Steps the selected ordinary family — Fastball → second → third → Fastball, skipping what the active table does not author (§3, §4.3). One press is one step. The family **locks the tick the charge arms** and nothing is re-polled at release; every SET starts on Fastball. The changeup is a family in this cycle, not a held modifier: −20% mph, hangs then dumps late (§4.3), 10% of the break ⚠️ **sitting 1** |
| Break | Stick L/R **after release** | Ball bends toward that side of the screen. Direction only (magnitude ignored); how fast the bend reaches full is the arm's **Control** (`flight.breakRatePerSec` × per-point, `PitchFlight.BreakStep`; §2, P3-a). Capped at half a zone (`breakMaxFt` 0.46) ✅ P1 |
| Star | **LT held** as RT is let go (§12, PH-16-R10, R11, R17) | The pitcher's Star Pitch (§13). Costs its price even if hit; unaffordable, the ordinary pitch of the selected family at the same timing |

Which ordinary family this delivery throws is chosen in SET before the charge and locked at the arm edge (§3, PH-02-R3/R4/R5): the sim owns the step and the mound reads it; there is no West changeup modifier, and the row above is retired with it. ⚠️ **sitting 1**

## 4.2 Location

- Walk the rubber with stick L/R during SET/WINDUP (`WalkPitcher`, ±1). The release point moves with the body; the ball's crossing moves by the **same world distance** as the body, once: `HomeSet.PitcherWalk` (2.4 ft per unit) for the body, the hand, and the crossing, on every seat. The human's `AimX` is 0; the CPU's aim is compensated for its walk (`AimForCrossing`). ✅ P1 (S-16)
- **Vertical location** is a pitch property, not a stick: normal/charge cross mid-zone (`PitchFlight.PlateY` = the zone center); the changeup crosses `shapes.changeupDropFt` (0.9) lower; break pitches cross mid and drift; the human moves height by pitch choice and by letting a changeup dump. Stick U/D is *not* an aim axis during SET. Every shape crosses exactly at its aim (the fastball's hump is mid-flight). ✅ P1
- Post-release break moves the crossing by at most **half the zone width** (`flight.breakMaxFt` 0.46 of 0.92); the drift grows late (`breakLateFrom` 0.55), a small early bend (`breakEarly`) is only for the eye and is gone at the plate. ✅ P1
- **Natural sweep** is the family's own lateral movement and is *not* the stick (PH-15-R6): a row's `sweepFt` is the feet its crossing ends off the straight line from the rubber, and `sweepFrom` (0..1) is where in the flight it starts to show, after which it grows as the **square** of the share of flight that is left — nothing early, all of itself at the plate. It is **mirrored by the throwing hand**:
  positive is the pitcher's **glove side**, which is **+X for a right-hander** and −X for a left-hander. That sign is derived from the diamond and lives in one place (`PitchFlight.GloveSideSign`): first base is at `infield.cornerFt` on +X (a `[Positive]` rule), and a right-hander on the rubber faces home with first base on the glove hand's side — so a right-hander's glove-side sweep runs away from a right-handed batter.
  The arm rides on the delivery (`PitchCommand.Throws`, stamped by `Match.PreparePitch` from the pitcher). The sweep is **not scaled by the stick** and **not damped by a charge** (PH-05-R1); the player's steering **adds** to it under its own unchanged `breakMaxFt` cap, so the worst legal lateral movement is sweep + break. ✅ (`PitchFlight.SweepShiftFt`; order of composition: shape → sweep → stick → star).
  Both shipped rows author `sweepFt` 0 and `sweepFrom` 1 ("never begins"), so no shipped flight moves; the three families that use it are a trial, below.
- Tired pitcher (§4.7): less room for the stick's break. The crossing is never moved at random (PH-08-R1).

## 4.3 Pitch shapes

Every shape is a **row in `data/rules/pitching.json` `families`** (`PitchFamilyTable`, one named row per family), and **one** function evaluates every row (`PitchFlight.Shape`, called by `PitchFlight.Point(u)`); the strike zone, the aim tell, the cursor, and the CPU batter read the u=1 sample (`PitchFlight.Crossing`, `SetTells.Locator`).
A row carries the family's base `mph` and its own `chargeMph`, its height path (`hump`, `hangUntil`, `hangRate`, `dumpRate`, `dropFt`), its natural sweep (`sweepFt`, `sweepFrom`; §4.2), whether the stick's break is damped for it (`breakDamped`), its extra `staminaCost`, and whether it is `offSpeed` (§5.2's sour-slap pop band and §5.9's fooled-late CPU batter). Time to plate `AirSeconds(mph)` ≈ 0.85 (charged) – 1.23 (changeup, 0.80× the 86 mph meat, ~0.25 s extra), Sluggers pace.
The hang is the row's `hangRate` (well below 1 so Y stays at or above the fastball until `hangUntil`); the dump is `dumpRate` (the rest of the drop to `dropFt`), and the validator refuses a row whose hang and dump never reach the aim in flight. A hangRate near 1 is a fade and is not a changeup.

| Family | Row in `families` | Speed | Path |
| --- | --- | --- | --- |
| Fastball | ✅ authored | `mph` 86 + Pitch×k + `chargeMph` 8 | Straight (`hangUntil` 1, `hangRate` 1), mild mid-flight `hump` 0.35, `dropFt` 0 |
| Changeup | ✅ authored | `mph` 68.8 (0.80×), `chargeMph` 3 | Flat until `hangUntil` 0.62 at `hangRate` 0.22, then dumps at `dumpRate` 2.4 to `dropFt` 0.9 below the crossing height. `breakDamped`, `offSpeed`, `staminaCost` 3 |
| Curveball | ✅ authored | slower than the slider, faster than the changeup | The library's biggest `hump` then its longest drop; a small glove-side `sweepFt`; `offSpeed`. Numbers and measured margins: [`docs/research/pitch-families-p1d.md`](../research/pitch-families-p1d.md) |
| Slider | ✅ authored | between the sinker and the curveball | The flattest of the three and the big glove-side `sweepFt`. Numbers: the P1-d report |
| Sinker | ✅ authored | the fastest after the fastball | Rides the fastball's height longest, then dips; a small **arm-side** (negative) `sweepFt`. Numbers: the P1-d report |
| Break (stick) | — not a family | the family's | Adds lateral drift that grows late (u>0.55), signed by stick, on top of whatever family flew |
| Star pitches | — not a family | per skill | The family + the skill shape (§13); the *shape* is data, the effect on the batter is the skill rule |

`PitchCommand.Type` **is** the family id, and the id is resolved through the library on every read of the pitch — the flight, the speed and the stamina. An id the library does not have (the retired trajectory strings `"curve"`, `"slide"`, and anything else) and a library id with no authored row (`curveball`, `slider`, `sinker`) are **different errors and both stop the pitch by name**; neither flies as a fastball. Break is a stick verb and charge is a modifier, so neither is a family.
✅ (D20; `PitchFamilyTable.Of`, `PitchFlight.Shape`, `Training.PitchesOf(rules)` is the **loaded** table's authored rows in library order — all five on the shipped root, fastball and changeup on a table that leaves the three optional rows null; a practice session and the `GrandSluggers.Play` sandbox read it off the catalog they loaded, never the code defaults. ✅, S-133).

**The shared ordinary pitch library** is five families, ids lowercase and closed: `fastball`, `changeup`, `curveball`, `slider`, `sinker` (`PitchFamily`, PH-02-R2). A character throws the fastball plus the two its repertoire names (§2, PH-15-R1). These ids are **both** roster membership *and* what flies:
an authored `"slider"` in `data/characters/` is a family that character owns, and a `PitchCommand` spelling `"slider"` is that same family flying from its `pitching.json` row; a table without that row still stops loudly rather than throwing a fastball. The library is still not the star-pitch namespace (`data/abilities/star-skills.json` spells two of its own skills `fastball` and `changeup`; the two sets are never resolved against each other).
✅ for the ids, the 25 assignments and the table they resolve against.

This child does **not** make the flight check a pitcher's repertoire. Today every pitcher, human or CPU, may throw a changeup whatever their roster row says; taking that away moves behaviour and the CPU's `_rng` draws, and it belongs with selection (P1-e / P1-f).

**Authored is a fact about the data, not about the code**. The three rows are nullable: `pitching.families.curveball` (and `.slider`, `.sinker`) is present in the shipped table and has no code default, and `IsAuthored`, `Authored` and `Of` all read the table they are asked. A JSON `null` is not a way to be unauthored — absence is; a `null` there is refused as "must be an object". The validator checks every authored row, shipped or trial: the hang must reach the aim in flight, and a row that sweeps must say when the sweep starts.

**On the shipped root all five families are authored, fly and can be selected**. The three newer rows live in `data/rules/pitching.json` beside the fastball and the changeup.
✅ The table carries the two original families without changing one flight, the other three with the natural-sweep term, and the selection before the charge with the charge-start lock (PH-02-R3/R4/R5) that the mound reads.
So West cycles fastball → second → third for every repertoire. **The off path** — a table with no row for a family (the code defaults, a fixture, a trial that drops a row) — still refuses it: no speed, no sweep, no stamina cost, no input, and a request for one is a loud stop that names the family.

## 4.4 Strike zone and judgment

- Zone is a fixed world rectangle over the plate (`StrikeZoneGeometry`: half-width 0.92 ft, bottom 1.45, top 3.65). It does not scale with the batter body (arcade, readable). ✅
- **Take in zone** = called strike. **Take outside** = ball. Judged at the plate crossing of the shown trajectory. ✅ (`StrikeZoneGeometry.Contains(Point(u=1))`)
- **Swing and miss** = strike regardless of location. ✅
- The white frame on screen *is* the zone. The frame never lies. ✅ (`PitchJudgmentGate`)
- **The SET ring is the rubber, not the crossing** (PH-06, PH-06-R1). In ordinary play the pale ring on the pitching seat sits at the pitcher's own rubber walked into world feet (`SetTells.RubberRing`: X = `PitcherOffsetX × HomeSet.PitcherWalk`, Y = `StrikeZoneGeometry.CenterY`), and it **hides at release** — in flight the ball is the cue. It may not be drawn from `PitchFlight.Crossing`:
  the crossing carries the family's own drop and natural sweep, so a ring there names the selected family on a screen both seats read, which is not the rubber tell. **Practice and the Tutorials keep the full crossing ring, through the flight** (PH-19, `SetTells.Locator`, gated on `TrainingOn || TutorialOn`): there the shape is the lesson and there is no opponent to leak it to. ⚠️ **sitting 1**

## 4.5 Pitcher throws, commitment and balks

- Before holding the pitch button, the pitcher can select and throw to any base, including home or an empty destination ahead of a runner. It is a live throw with the ordinary cover, flight and tag rules. A runner standing on a bag is safe; the throw itself never awards an out.
- Beginning the pitch-button hold begins the visible windup and commits the pitcher to deliver. Commitment starts before MAX and cannot be cleared by resetting position or cancelling charge. The charge can be held indefinitely; there is no forced-release timer. Releasing lets the pitch proceed and the catcher defend a steal.
- Attempting a pitcher throw while committed and still holding the ball is a **balk** with runners aboard: dead ball, every runner advances one base from their last touched bag, no out from the attempt, no pitch counted, no plate appearance completed, and no count change. A runner on third scores, including a walk-off. The next SET is uncommitted. With no runners there is no base award and the pitcher remains committed.
- Once the pitch is airborne, a pickoff command cannot recall it or manufacture a balk. Catcher target selection and buffered throw input belong to the defense’s forthcoming possession (§11.3).
- A legal pitcher throw and subsequent rundown use the existing runner positions. A base throw does not cause an uncommanded runner to break. A sailed throw is a live loose ball.
- CPU pickoff decisions use the same commitment gate and seeded situation read; no roll awards an out. CPU and human commands share the live throw and tag system.

## 4.6 Hit by pitch

- If the pitch's plate-plane point lies inside the batter's body circle (`batting.hbp.bodyRadiusFt` 0.45, world feet, centered where the batter body actually is, including box walk, at the natural crossing height) **and the batter did not swing**, the batter is hit: first base, forced runners advance, ball dead, stamp HIT BY PITCH. Balls/strikes unchanged. ✅ P1 (S-16, S-17)
- The batter body and the cursor move by the **same** world distance per box unit (`HomeSet.BatterWalk`). ✅ P1
- A human pitcher can reach the body by walking the rubber fully toward the batter's side plus full break, with the box centered. CPU pitchers reach it only through scatter (rare, ≈1 per game at Pitch ≤ 4). ✅ P1 (S-16)

## 4.7 Stamina and the pitcher swap

- Stamina is **per pitcher** (each character carries their own pool for the match, `Match.StaminaOf`), pool = `poolBase` 60 + **Endurance** × `poolPerPitch` 6 (Endurance tracks Pitch until authored, §2). ✅ P1 (S-25), P3-a
- Costs (`data/rules/pitching.json` `stamina`): normal 4, charge +3, break +1, the family's own `families.<id>.staminaCost` on top (fastball 0, changeup 3 — §4.3), star = the skill's `staminaCost` (`data/abilities/star-skills.json`, 8–22, read at runtime). **Only a pitch costs the arm** (PH-08-R3): a hit, a home run or a run allowed costs nothing past the pitch that was thrown. ✅ (S-25)
- **Fatigue is the character's for the match** (PH-08-R2). Nothing restores a pool in the game: not a swap, not a half on a glove, not a teammate pitching. An arm that leaves the mound and comes back takes it with exactly what it had left (S-146).
- **Fatigue is gradual** (PH-08, PH-08-R1): the fade runs from 0 at a pool of `fadeFrom` 50 to 1 at empty (`StaminaRules.Fade`). The arm loses fade × `exhaustedMph` 10 mph, and its break is scaled from 1 down to `tiredBreakMul` 0.6 (less steering room, stamped in `PreparePitch`). A pool of 40 loses 2 mph and keeps 0.92 of its break; 25 loses 5 mph and keeps 0.8; empty loses 10 and keeps 0.6.
  Both ends lie inside the air-time range the game already threw, so pace stays inside D7. **Fatigue is never a random miss** (PH-08-R1, PH-05-R1): a tired or exhausted arm crosses where it was aimed and steered. The pool is allowed below zero. ✅ P1 (S-25), P3-c (S-147 … S-149)
- Below `tiredBelow` 25 the arm is **TIRED**: the sweat and card tell (`BroadcastHud.ArmLine(stamina, rules)`) and the CPU's swap bunt button. It is a label, not a step; the fade is the effect. Below 0 it is exhausted.
- **Swap** (LB during SET): pick any fielder as the new pitcher (`SwapPitcher(who)`; the best displayed Pitch when nobody is named — the aggregate, not a rating, §2); the old pitcher takes that glove (the defense order is the match's, `Match.DefenseRoster`, and the swap trades the two slots). Each character's pool is their own, so a fresh arm is fresh. The swap costs no time-out. Once per half-inning (`CanSwapPitcher`). LB before charging opens **Arrange defense**, a window over the field.
  Its lineup-style diamond includes all nine players. Inspection shows PITCH / BAT / FIELD / RUN, throwing hand, repertoire, remaining stamina and good / poor / neutral chemistry with teammates. Stick / d-pad moves spatially just like position setup; focus inspects. RT picks a source; choosing a second position swaps the two occupants immediately. Picking the same position or East cancels a pending pick. Done closes the window; East with no pending pick also closes. Completed swaps are kept.
  LB, or Quick swap to mound, remains a shortcut for the focused player; it leaves the window open. A used pitcher allowance disables further P swaps while the other positions remain editable. Browsing never edits the team. The window consumes both seats’ baseball inputs and suspends the pre-contact clock; opening/closing cannot leak a pitch, pickoff or stored charge.
  Confirming resets the pitch selection to the fastball — a new arm is a new repertoire and that path does not re-enter SET (§3). ✅ (S-26)
- CPU swaps at TIRED with a lead ≥ `cpuSwapLead` 3 or at exhaustion always (`CpuConsidersSwap`). ✅ P1

## 4.8 CPU pitcher

A decision table, not nested rolls (`pitching.json` `cpu`, `Match.CpuPitch`). Evaluated once per SET from (count, outs, runners, batter hand, own stamina, stars). ✅ P1 (S-27)

**The CPU's pitch is built from the inputs a human has and nothing else** (PH-18, PH-18-R1). There is no endpoint model (an aimed crossing height and four exclusive verbs) and no switch that reaches one. ✅

The contract, five rules (`CpuPitcher.PitchByInputs`, S-115 … S-120):

1. **Location is the rubber.** The row's location is a *horizontal* intent in world feet at the plate — *edge* the away or in corner, *waste* outside it, *middle-in* toward the batter, *middle* the center line — and the body walks the rubber until the family's own crossing lands on that intent. The crossing is affine in the rubber (§4.2: the walk moves the release and the aim target by the same world distance), so the solve is exact and not a search:
   `r = (intent − X₀) / HomeSet.PitcherWalk`, where `X₀` is the same delivery's crossing from the middle of the rubber. `AimX` and `AimY` are **always 0**. **The throwing hand is stamped before the solve**, so a left-hander's natural sweep is compensated the right way (P1-d's finding: the retired endpoint model solved before `PreparePitch` stamped `Throws`, which was harmless only because every family it threw swept 0).
   The walk is clamped to the legal rubber range, ±1, the same clamp `WalkPitcher` enforces for a hand on the stick; a walk that runs into it misses short, on the near side, the way an arm out of rubber does.
2. **No vertical intent exists.** Height is whatever the family gives (PH-03, §4.2): the crossing is the zone center less the row's `dropFt`, and nothing else in the model touches it. There is no vertical spread (`locations.middleYSpreadFt` went with the endpoint model).
3. **Family is presses.** Each count row carries **named per-family weights** (`families.fastball` … `families.sinker`), filtered at run time to the slots this pitcher can actually *select* — in the repertoire **and** authored (§3, `PitchSelection.IsSelectable`) — renormalised, and rolled once.
   The choice is then expressed as the 0 / 1 / 2 cycle presses it is, walked through the same `PitchSelection.Advance` from the same SET reset a player walks, so **the CPU cannot select what a hand cannot reach**. A row that weights nothing this pitcher can select falls back to the fastball with no presses — the one family every pitcher throws (PH-15-R1) and the one every SET starts on. A row that weights nothing **at all** is a broken table and stops the load by name.
4. **Charge and steer are modifiers, not verbs** (PH-02-R1). Two independent rolls per row, `chargeChance` and `steerChance`: a charged pitch is still steerable, damped by `breakDampedMul` exactly as a human's is (§4.1). Nice! stays a roll on charged pitches; Star stays as it was (§13 — a star's own shape still moves the ball, for this arm as for a human's, and the solve lands on the intent anyway because the same wobble is in `X₀`).
5. **Steer is what a held stick reaches.** A hand accumulates break at `BreakStep`'s rate over the flight; the CPU has no frames to hold, so it takes the same total in one step: `BreakX = ±PitchFlight.BreakReach(Control, airSec)` = `±min(1, rate(Control) × airSec)`, never an instant ±1 no arm could get to. **Measured on the shipped numbers**:
   the slowest roster arm (Pitch 3) bends the stick to the cap in 0.55 s, well inside its shortest flight, so today every steered CPU pitch does reach the whole ±1 — the rule is what changed, not the number. The slowest *legal* arm (Pitch 1) bends to the cap in 0.80 s; on the 53.78-ft mound its hardest pitch flies 0.754 s, so it would reach 0.94. The bound bites hardest at the air-time floor (`BreakReach(1, airMinSec)` = 0.86).

**And it is drawn as a held stick, not as a snap**. The client starts a steered CPU delivery's drawn bend at 0 and walks it with the same `PitchFlight.BreakStep` a hand's hold walks, in the plan's direction, every flight frame — so the ball arrives at the command's `BreakReach` at the plate instead of leaving the hand already bent. The **judged** crossing is unchanged:
it reads the command the sim built, and only the drawn point takes the ticked value. **The CPU's rubber walk is drawn as a walk too**: the delivery is solved at the top of SET, so the body walks toward its rubber at the hand's own rate over SET rather than appearing there on the release frame. ⚠️ **sitting 1**

**Scatter is a legal mistake.** The Gaussian lands on the CPU's own rubber intent, in X only — there is no vertical input to miss in — and TIRED still widens it by `tiredScatterMul`. Fatigue lays no random miss on the delivery itself, the CPU's or a human's (PH-08-R1, §4.7). The rubber is the location on every pitch, so there is no separate walk to roll for (`rubberWalkChance` / `rubberWalkMax` went with the endpoint model) — and, as a consequence, the CPU's rubber moves between most pitches, which the CPU batter's mistrack read (§5.9) sees. Reported, not tuned.

The per-family weights and `steerChance` in the shipped file are the **accepted** ones (PH-20-R1), with a rationale per row in [`docs/research/cpu-pitcher-p1g.md`](../research/cpu-pitcher-p1g.md). `chargeChance` is the charge share of the retired exclusive mix. Every row weights all five families (S-120).

**Which row, and where**:

| Situation | Location | Star |
| --- | --- | --- |
| 0-0, 1-0, 1-1 | Zone edges (corner picked by batter hand: away) | 5% (captain, ≥1 star) |
| Ahead 0-2, 1-2 | Just off the zone (waste), then edge | 15% |
| Behind 2-0, 3-0, 3-1 | Middle-in, safe | 0% |
| Runner on with 2 outs | Middle, fast (pitch-out never) | 0% |
| TIRED | Whatever the table says, then §4.7 noise | |
| Pickoff | Before the pitch: 3% / 6% / 10% by difficulty when a runner is on (×3 when it sees a pip armed in SET, D3); lead runner, or 1B on first-and-third (66%) | ✅ P6 |

Row choice: a runner on with two outs first; then two strikes with at most one ball is *ahead*; two or more balls with at most one strike is *behind*; every other count (0-0, 1-0, 1-1, 0-1, 2-1, 2-2, 3-2) reads the *even* row.
Locations are horizontal feet from the frame (`cpu.locations`): *edge* = `edgeInsetFt` inside the away corner (by batter hand) `edgeAwayChance` of the time, the in corner otherwise; *waste* = `wasteOutFt` outside on the away side; *middle-in* = `middleInFt` toward the batter; *middle* = the center line. A charge is MAX with a Nice! release `niceChance` of the time.
Scatter σ = (11 − **Control**) × `scatterFtPerPitchStat` (0.10 ft; P7 raised it from 0.055 so the CPU arm walked a batter about three times a game, S-29) on the rubber intent, never the center; × `tiredScatterMul` when TIRED. ✅ P1. The `TimingErrorFrames` on `PitchCommand` was dead and is removed. ✅ P1
