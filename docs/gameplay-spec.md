# Gameplay spec — how every play behaves

This is the **source of truth for baseball behavior** in Grand Sluggers. When the code and this document disagree, the code is wrong. When this document is silent, file a child issue under the play epic and add the rule here in the same PR.

The bar is *Mario Super Sluggers* (Wii, 2008): a play is decided by **where the ball is, where the runner is, and what the player pressed** — never by a dice roll that a caption then narrates. The user always owns the verb. CPU fills the seat the human did not take, and it fills it with the same rules the human plays by.

Companion docs: [systems.md](systems.md) (chemistry, stars, gear, parks), [how-to-play.md](how-to-play.md) (couch buttons), [research-sluggers.md](research-sluggers.md) (the reference teardown), [roadmap.md](roadmap.md) (the order we build this in). Feel numbers stay in `data/feel/`. Rule numbers move to `data/rules/` (section 16).

Status tags used throughout, checked against the sim and Unity client at `f09cad1` (2026-09-12):

| Tag | Meaning |
| --- | --- |
| ✅ | Shipped and behaves as written |
| ⚠️ | Exists but diverges (line reference given) |
| ❌ | Missing, or resolved by a roll / caption instead of geometry |

Every ❌ and ⚠️ is collected in Appendix A with the file and line. Every rule that matters has a scenario id (`S-xx`) in Appendix B; the scenario list is the acceptance test for the roadmap epics.

---

## 0. Principles

1. **Sim owns baseball. Unity presents.** Every out, safe, strike, ball, foul, and advance is decided in `GrandSluggers.Sim` from positions and times. Unity may animate a verdict; it may not produce one. ✅ P0: `LivePlaySystem` owns the live ball from contact to Time — gloves, catches, throws, the relay chain, the close-play race, the bobble, and the steal phase — from one `Tick` command per frame carrying both pads; `InPlayDirector` translates the pads, mirrors the state, and plays the cues.
2. **Geometry decides. Rolls only add noise, never outcomes.** A stat changes speed, range, window, or accuracy. It does not roll "out or single" (`Fielding.cs:146-161` ❌). Randomness is allowed on *inputs* (a CPU's timing error, a bad-chemistry throw's lateral error), never on *results*.
3. **One clock, one body.** A runner has one position and one speed. A throw has one duration used both to fly the ball and to judge the bag. A fielder has one speed whether a human or CPU holds the stick. ⚠️ Today there are two runner speeds, four throw-speed formulas, and two glove speeds (Appendix A).
4. **The user owns the verb; CPU covers the rest.** Dead stick means the CPU plays that seat *by these same rules*. A human never gets an auto-out and never gets robbed of one by a script.
5. **Every play type is a scene with a name.** Grounder, chopper, liner, pop, fly, wall ball, homer, bunt — each has a fielder, a runner rule, a throw rule, a camera, and a stamp (section 7).
6. **Rails, not patches.** A new rule lives in a named system with a `data/rules/*.json` number and a scenario test. Special-casing one play, one seat, or one captain is a patch (AGENTS.md).
7. **Two seconds of illegal physics, then baseball.** Star skills and items bend one rule for a beat. They never auto-resolve a play, blind the other player, or make a home run free.
8. **Readable at couch distance.** A stranger should be able to say why they were out. If the reason is not visible on the field (a tag, a bag, a catch), it is not a rule we ship.

### 0.1 Decisions where Sluggers and the current game differ

The reference teardown ([research-sluggers.md](research-sluggers.md), "Mechanics teardown") settles most questions. Where the current game shipped something the reference does not have, this is the call:

| # | Question | Decision | Why |
| --- | --- | --- | --- |
| D1 | Lead-offs | **None.** Runners stand on the bag. `Lead01`, the lead stick verb, and the mini-diamond lead pips are retired. ✅ P3 | Neither Sluggers nor Superstar Baseball has leads. Leads are what made random pickoffs "necessary" and what made the steal a time credit instead of a race. |
| D2 | Steal jump | Armed runner breaks at **release**; armed inside the first 0.25 s of the windup is a **perfect steal** and breaks 0.4 s before release. ✅ P6 | Superstar Baseball frame data (frame 40 vs frame 15 of the windup). |
| D3 | Pickoff | A runner on the bag is always safe. A pickoff catches an armed runner who **already broke** (an early arm breaks on the pitcher's first motion, including a pickoff motion). ✅ P6 | Reference: "a pure pick-off can never get a runner out". This is the mind game, not a roll. |
| D4 | Contact quality | **Cursor decides quality, timing decides direction.** Sour / nice / perfect by where the ball meets the cursor; early pulls, late pushes; outside the window is a whiff. | Booklet plus the Superstar datamine (five bat zones, 9-frame slap / 7-frame charge window). |
| D5 | Close plays | Button prompt at **third and home only**, only when the throw and the runner arrive together. | Sluggers booklet wording. Superstar Baseball used a body-check roll instead; we take the prompt. |
| D6 | Five-star free homer | **No.** | Superstar Baseball had it; Sluggers dropped it. |
| D7 | Pitch pace | Keep ≈ 0.85–1.10 s to the plate for now; the reference is closer to 0.6–0.75 s. Human parity gate (#534) decides. | Derived from Superstar speeds, not measured in Sluggers. |
| D8 | Innings | 3 / 6 / 9 (not 1 / 3 / 5 / 7 / 9). Extra innings up to +3. Mercy 10 at the end of an inning. | Party default; the reference cap and mercy are copied. |
| D9 | Infield fly, balk, dropped third strike, intentional walk, DH | None. | Neither game has them. |
| D10 | Steal of home | Legal (armed from third). ✅ P6 | Nothing in the reference forbids it; the catcher's zero-length throw makes it rare. |
| D11 | Flat bag-cover speed | Keep (it is how the reference moves covers), but as a data number. | Superstar datamine: constant cover speed starting 14 frames after the hit. |
| D12 | Box position between pitches | **Recenters after every pitch.** Down still recenters early in SET. | Jack's call (sitting 2026-09-12, #607). The reference persists the box with a reset button; overridden for readability. |
| D13 | When you press to swing | **The window is centered on the ball reaching the plate** (minus a small authored lead), and the swing take is time-warped so the bat meets the ball inside the window. Outside the window the take plays at its natural length and misses. | Superstar datamine: "the timing of the contact is constant, the animation is lengthened / shortened to make contact." Replaces the fixed press + 0.30 s plane (#612). |
| D14 | In-play camera | **One cut on contact** to the in-play view that follows the ball; **the bag camera only for a close play** at third or home (and, optionally, once on a steal throw). No swoop to the bag on ordinary throws. | Both booklets document one cut on contact and a base-locked camera only for the close play (#610). |
| D15 | Fence height | **One number**: the park's `fenceHeightFt` drives both the flight clip and the drawn wall; a gate asserts they match. The Harbor value is Jack's call (recommended 12 ft). ✅ #608: `HarborWall.OutfieldHeight(park)` is the park field; Harbor ships at 12 ft. | Sitting 2026-09-12: the drawn Harbor wall was 26 ft over an 8 ft sim fence (#608). |

---

## 1. Match rules

| Rule | Spec | Status |
| --- | --- | --- |
| Sides | 9 v 9. Positions P, C, 1B, 2B, 3B, SS, LF, CF, RF. | ✅ |
| Innings | 3 (party default), 6, 9. Selected on the title (Tab). | ✅ |
| Home / away | Away bats the top. Home bats the bottom. 1P: controller 1 picks HOME or AWAY. | ✅ |
| Walk-off | Bottom of the last inning (or later) ends the moment the home team leads. Bottom is skipped if home leads after the top of the last. | ✅ P3: the take path and the swing path both call `EndIfWalkOff` (S-80) |
| Extra innings | Tied after the last scheduled inning → play full innings until a lead after a complete inning (or a walk-off). Cap at scheduled + 3; a tie at the cap is a tie. | ✅ P3 (`match.extraInningsCap`, S-81) |
| Mercy | Optional (default **on** in Exhibition). 10-run lead after the trailing side has batted in inning 3 (or later) ends the game. Off for 3-inning games. | ✅ P3 (`match.mercy`: `runs` 10, `fromInning` 3, `minScheduledInnings` 6; `Match(mercy:)`; S-82) |
| Designated hitter | None. The pitcher bats. | ✅ |
| Lineup | Nine, set in Offense / Defense Setup. No substitutions except the pitcher swap (§4.7). | ✅ |
| Count | 4 balls = walk. 3 strikes = strikeout. Foul with 2 strikes stays 2 strikes **except a bunt**, which is strike three (§5.8). | ✅ P1 (S-18) |
| Hit by pitch | A pitch that meets the batter's body while the batter does not swing awards first base (§4.6). | ✅ P1 geometry (S-16, S-17); the rubber-walk reach is P1 part b |
| Balk, intentional walk, dropped third strike, check swing, infield fly, appeal plays | Not in the game. | ✅ by omission |
| Ground-rule double | A fair ball that bounces on the field then leaves it over the fence: batter and all runners advance exactly two bases from where they started. | ✅ P2 (`BattedBall.GroundRule`, `PlayOutcome.GroundRuleDouble`; S-59) |
| Ball out of play (foul territory beyond the wall / into the stands) | Foul ball, dead. | ✅ P2 (over a foul wall or the backstop is `SampleEvent.Stands`) |
| Stars | Shared 0–5 per team. §12. | ✅ |
| Ties in geometry | Tie at a bag goes to the runner. | ✅ (`InPlay.ForceOnBag`) |

**Scoring on the third out.** A run counts if the runner touched home *before* the third out, unless the third out is a force or the batter-runner retired before first. Both timers are already tracked by the live play; the rule is a comparison at `Complete`. ✅ P3 (`Runner.ScoredAt` against the third out's play time; S-78, S-79).

---

## 2. Stats, in numbers

Stats are 1–10 per character (`data/characters/`). They drive *only* the quantities below. Nothing else may read a stat. Formulas here are the contract; the exact coefficients are the `data/rules/` tables in §16 and are tunable without touching this document.

| Stat | Drives | Not allowed to drive |
| --- | --- | --- |
| **Pitch** | Fastball mph, break amount, changeup drop, stamina pool, CPU aim scatter | Whether a batter misses |
| **Bat** | Contact window width, sweet-spot size, exit-velo cap (power), CPU timing error | Whether a fielder catches |
| **Field** | Chase speed in the field, catch radius, jump/dive window, throw speed, throw accuracy, bobble chance, CPU throw decision delay | Whether a runner is out |
| **Run** | Sprint speed on the bases and out of the box, dash, CPU send aggression, close-play reaction | Anything about the ball |

The four stats are shown on the character card. Internally `Bat` splits into `contact` (window) and `power` (exit velo) with the same value unless a bat item modifies one (`data/bats/`). ✅

---

## 3. The at-bat state machine

One pitch is one cycle. Times are seconds; the feel numbers are `data/feel/table.json` and are quoted for orientation.

```
SET ──(pitch commit)──▶ WINDUP ──(release @0.42)──▶ FLIGHT ──▶ JUDGE ──┬─▶ DEAD (take/miss/foul/K/BB/HBP) ─▶ STAMP ─▶ SET
                                                                        └─▶ LIVE (contact) ─▶ ... ─▶ COMPLETE ─▶ STAMP ─▶ SET
   ▲                                                                                                                       │
   └────────────────────────────── pickoff (SET only) ─▶ PICKOFF PLAY ─▶ STAMP ────────────────────────────────────────────┘
```

| Phase | What may happen | Who acts | Exits |
| --- | --- | --- | --- |
| **SET** | Pitcher walks the rubber, batter walks the box, offense arms steals (D-pad runner + L3) and all-advance, defense arms a pickoff, either side arms a star. Pitcher readiness beat `pitcherReadySeconds` (0.55). Runners stand on their bags (D1). | Both | Pitch commit (release of South at the mound). Pickoff (bag + South). Call time. |
| **WINDUP** | Delivery animation. Batter may still walk the box and start a charge. A steal armed inside the first 0.25 s is a **perfect steal** (D2). Runners with a steal armed break at **release** (perfect: 0.4 s before). | Both | Release at `Motion.PitchRelease` (0.42). |
| **FLIGHT** | Ball travels rubber → plate in `AirSeconds` (≈0.85–1.10, D7). Pitcher steers break (stick L/R). Batter may swing at any moment; the press is judged against the ball's plate time less `batting.window.leadSec` (D13), and inside the window the take is warped so its Contact mark lands on the ball. Stealing runners run at ⅔ speed until the ball reaches the plate. | Both | Ball crosses the plate plane (take) or bat plane meets ball (swing). |
| **JUDGE** | One function, one frame: strike/ball, swing/miss, contact quality, HBP. Fair / foul is the live ball's call (§5.6): every batted ball goes LIVE. | Sim | Dead or live. |
| **DEAD** | Count updates. A runner who broke makes the pitch a **catcher throw play** (§11.3), the same live ball as LIVE with the ball already in the catcher's glove. A pickoff in SET is the same play with the pitcher's throw in the air (§11.4). | Sim, then catcher seat | Stamp. |
| **LIVE** | Ball in play. Fielders, runners, throws, tags, forces (§7–11). | Both | `Time` (§10.6). |
| **STAMP** | Result named on the field. Scoring, outs, bag placement already applied. Hold `afterOutSeconds` / `afterCountSeconds`. | Sim | SET, half change, or game over. |

Rules:

- **The judged pitch is the shown pitch.** The strike/ball/contact verdict is computed from the same trajectory the batter sees, including in-flight break. The CPU batter commits at the decision instant (plate − `batting.window.leadSec` − `batting.cpu.decideLeadSec`) from the trajectory as it stands then, exactly like a human who has pressed; the judgment reads the final crossing (`AtBatMotion.CpuDecisionTime`, `CommitCpuSwing`). ✅ P1 (S-04)
- **A swing before release is a swing.** It resolves as an early miss (strike); the take plays at its natural length (Contact 0.30 s after the press, D13). A press during SET is ignored (it is not a swing yet): the hold still builds a charge, the release does not commit (`ChargeButton.Advance(commits: false)`). ✅ P1 (S-14, S-15)
- **The box recenters after every pitch** (D12, #607); Down recenters early in SET; the pitcher's rubber persists. ⚠️ Today the box persists across the at-bat (`Match.NextBatter`, P1 S-16) — S-16 is re-expressed under D12.
- **Nothing advances baseball while a seat is disconnected** (how-to-play.md, two controllers). ✅

---

## 4. Pitching

### 4.1 The four verbs

Same shape as the swing: tap / charge / modifier / star. Booklet-confirmed contract (issue #534).

| Verb | Input | Ball |
| --- | --- | --- |
| Normal | Tap and release South | Pitcher's base fastball, easiest control |
| Charge | Hold South to MAX (`pitchChargeSeconds` 0.55), release inside the MAX band (`chargeMaxHoldSeconds` 0.5) | +mph. Released inside the first 0.25 s of MAX (`pitching.release.niceBandSec`) = **Nice!** (+5%, `niceMul`; `PitchCommand.Nice`). Past the band the charge decays (`chargeOverchargeDecay`) toward a normal pitch. A charged pitch takes only 10% of the break (`flight.breakDampedMul`; reference: charge and changeup are "essentially straight") ✅ P1 |
| Changeup | West held through release | −20% mph, hangs then dumps late (§4.3). 10% of the break ✅ P1 |
| Break | Stick L/R **after release** | Ball bends toward that side of the screen. Direction only (magnitude ignored); how fast the bend reaches full is the Pitch stat (`flight.breakRatePerSec` × per-stat, `PitchFlight.BreakStep`). Capped at half a zone (`breakMaxFt` 0.46) ✅ P1 |
| Star | North armed + South | Captain star pitch (§13). Costs a star even if hit |

### 4.2 Location

- Walk the rubber with stick L/R during SET/WINDUP (`WalkPitcher`, ±1). The release point moves with the body; the ball's crossing moves by the **same world distance** as the body, once: `HomeSet.PitcherWalk` (2.4 ft per unit) for the body, the hand, and the crossing, on every seat. The human's `AimX` is 0; the CPU's aim is compensated for its walk (`AimForCrossing`). ✅ P1 (S-16)
- **Vertical location** is a pitch property, not a stick: normal/charge cross mid-zone (`PitchFlight.PlateY` = the zone center); the changeup crosses `shapes.changeupDropFt` (0.9) lower; break pitches cross mid and drift; the human moves height by pitch choice and by letting a changeup dump. Stick U/D is *not* an aim axis during SET. Every shape crosses exactly at its aim (the fastball's hump is mid-flight). ✅ P1
- Post-release break moves the crossing by at most **half the zone width** (`flight.breakMaxFt` 0.46 of 0.92); the drift grows late (`breakLateFrom` 0.55), a small early bend (`breakEarly`) is only for the eye and is gone at the plate. ✅ P1
- Tired pitcher (§4.7): a random wobble of the crossing on every pitch, visible as a shaky streak.

### 4.3 Pitch shapes

All shapes are `data/rules/pitching.json` curves, evaluated by `PitchFlight.Point(u)`; the strike zone, the aim tell, the cursor, and the CPU batter read the u=1 sample (`PitchFlight.Crossing`, `SetTells.Locator`). Time to plate `AirSeconds(mph)` ≈ 0.85 (charged) – 1.10 (changeup), Sluggers pace.

| Shape | Speed | Path |
| --- | --- | --- |
| Fastball | base + Pitch×k + charge | Straight, mild drop |
| Changeup | 0.80× | Flat until u≈0.6, then dumps below the crossing height by up to one zone-half |
| Break (stick) | fastball | Adds lateral drift that grows late (u>0.55), signed by stick |
| Star pitches | per skill | Fastball + skill shape (§13); the *shape* is data, the effect on the batter is the skill rule |

Pitch **type strings** (`"curve"`, `"slider"`) are retired: break is a stick verb, not a type. ✅ P1 (`PitchFlight` has two shapes; an unknown type flies as a fastball; `Training.CorePitches` is fastball / changeup).

### 4.4 Strike zone and judgment

- Zone is a fixed world rectangle over the plate (`StrikeZoneGeometry`: half-width 0.92 ft, bottom 1.45, top 3.65). It does not scale with the batter body (arcade, readable). ✅
- **Take in zone** = called strike. **Take outside** = ball. Judged at the plate crossing of the shown trajectory. ✅ (`StrikeZoneGeometry.Contains(Point(u=1))`; issue #535 covers any residual divergence)
- **Swing and miss** = strike regardless of location. ✅
- The white frame on screen *is* the zone. The frame never lies. ✅ (`PitchJudgmentGate`)

### 4.5 Pickoff (SET only)

- Defense arms a bag (D-pad / 1–3) and presses South during SET. The pitcher turns and throws to that bag; the covering fielder (1B / SS / 3B) takes it. The pitch clock resets; the count does not change.
- A runner **on the bag** is safe. Always. No play, a small "back" beat, no stamp. (Reference: a pure pickoff never retires a runner.) ✅ P6 (`PlayKind.Pickoff`: the count stands, `PlayStamp.Shows` is false; `Match.BeginPickoff` never opens a live ball when nobody broke).
- A runner who has **broken** (an early-armed steal breaks on the pitcher's first motion — a pickoff motion counts, D3) is now between bags: the receiver at the bag throws ahead of them or chases; a **rundown** (§9.7) or a tag at the next bag decides it by geometry. That is the whole point of the pickoff: it punishes arming the steal too early. ✅ P6: the pickoff is a **live runner play** (`LivePlaySystem.RunnerPlay`) — the pitcher's throw to the named bag is the one throw model (§8.5) from the rubber, every runner armed in SET (`StealArm.Set`) has broken toward their next bag at full speed (no head start: the motion is the break), and P5's tag, rundown, and Time rules finish it. A pickoff throw that sails is live and the runner takes the bag (S-71, the ERROR). Stamp PICKED OFF (`RunnerPlayResult.PickedOff`).
- **No random pickoffs.** A runner is never retired on a pitch by a roll. ✅ P3 / P6 (`ResolvePickoff` is gone; the CPU pickoff is a read at SET, never an out by itself, S-72).
- CPU pitcher attempts a pickoff 3–10% of SETs with a runner on (by difficulty), lead runner by default, 1B on first-and-third (§4.8). A failed CPU pickoff is a wasted beat, not a base. ✅ P6 (`Match.CpuPickoffBag`: `cpu.*.pickoffChance`, × `running.cpu.pickoffSeenArmMul` (3) when a steal pip armed in SET is showing — a perfect arm is the windup's and is never seen in SET; first on the corners by `running.cpu.pickoffFirstOnCornersChance`).

### 4.6 Hit by pitch

- If the pitch's plate-plane point lies inside the batter's body circle (`batting.hbp.bodyRadiusFt` 0.45, world feet, centered where the batter body actually is, including box walk, at the natural crossing height) **and the batter did not swing**, the batter is hit: first base, forced runners advance, ball dead, stamp HIT BY PITCH. Balls/strikes unchanged. ✅ P1 (S-16, S-17)
- The batter body and the cursor move by the **same** world distance per box unit (`HomeSet.BatterWalk`). ✅ P1
- A human pitcher can reach the body by walking the rubber fully toward the batter's side plus full break, with the box centered. CPU pitchers reach it only through scatter (rare, ≈1 per game at Pitch ≤ 4). ✅ P1 (S-16)

### 4.7 Stamina and the pitcher swap

- Stamina is **per pitcher** (each character carries their own pool for the match, `Match.StaminaOf`), pool = `poolBase` 60 + Pitch × `poolPerPitch` 6. ✅ P1 (S-25)
- Costs (`data/rules/pitching.json` `stamina`): normal 4, charge +3, changeup 3, break +1, star = the skill's `staminaCost` (`data/abilities/star-skills.json`, 8–22, read at runtime), homer allowed +6, each run allowed +2. ✅ P1 (S-25)
- Below `tiredBelow` 25 = **TIRED**: −`tiredMph` 6, break × `tiredBreakMul` 0.6, crossing wobble σ `tiredWobbleFt` 0.25 (sampled once per pitch in `PreparePitch`), sweat and card tell (`BroadcastHud.ArmLine(stamina, rules)`). Below 0: −`exhaustedMph` 10, wobble σ `exhaustedWobbleFt` 0.45. The pool is allowed below zero. ✅ P1 (S-25)
- **Swap** (Select / R during SET): pick any fielder as the new pitcher (`SwapPitcher(who)`; the best Pitch stat when nobody is named); the old pitcher takes that glove (the defense order is the match's, `Match.DefenseRoster`, and the swap trades the two slots). Each character's pool is their own, so a fresh arm is fresh. The swap costs no time-out. Once per half-inning (`CanSwapPitcher`). On the pad: Select opens a visible pick (`PitcherSwapPick`, the card reads SWAP → glove name), the stick or d-pad steps through every fielder, Select confirms, East closes; the changeup hold reads CHANGE on the card (`BroadcastHud.PitcherExtra`). ✅ P1 (S-26, #582)
- CPU swaps at TIRED with a lead ≥ `cpuSwapLead` 3 or at exhaustion always (`CpuConsidersSwap`). ✅ P1

### 4.8 CPU pitcher

A decision table, not nested rolls (`pitching.json` `cpu`, `Match.CpuPitch`). Evaluated once per SET from (count, outs, runners, batter hand, own stamina, stars). ✅ P1 (S-27)

| Situation | Location target | Pitch mix (normal / charge / changeup / break) | Star |
| --- | --- | --- | --- |
| 0-0, 1-0, 1-1 | Zone edges (corner picked by batter hand: away) | 45 / 20 / 15 / 20 | 5% (captain, ≥1 star) |
| Ahead 0-2, 1-2 | Just off the zone (waste), then edge | 20 / 15 / 35 / 30 | 15% |
| Behind 2-0, 3-0, 3-1 | Middle-in, safe | 60 / 30 / 5 / 5 | 0% |
| Runner on with 2 outs | Middle, fast | 50 / 40 / 0 / 10 (pitch-out never) | 0% |
| TIRED | Whatever the table says, then §4.7 noise | | |
| Pickoff | Before the pitch: 3% / 6% / 10% by difficulty when a runner is on (×3 when it sees a pip armed in SET, D3); lead runner, or 1B on first-and-third (66%) | | ✅ P6 |

Row choice: a runner on with two outs first; then two strikes with at most one ball is *ahead*; two or more balls with at most one strike is *behind*; every other count (0-0, 1-0, 1-1, 0-1, 2-1, 2-2, 3-2) reads the *even* row. Locations are feet from the frame (`cpu.locations`): *edge* = `edgeInsetFt` inside a corner, the away corner (by batter hand) `edgeAwayChance` of the time; *waste* = `wasteOutFt` outside on the away side; *middle-in* = `middleInFt` toward the batter at mid-height; *middle* = center ± `middleYSpreadFt`. A charge is MAX with a Nice! release `niceChance` of the time; a break is the stick held one way. The CPU walks the rubber before `rubberWalkChance` of its pitches (up to `rubberWalkMax`) — a real verb the batter may mistrack (§5.9). Aim scatter σ = (11 − Pitch) × `scatterFtPerPitchStat` (0.10 ft; P7 raised it from 0.055 so the CPU arm walks a batter about three times a game, S-29) around the *target*, never the center; × `tiredScatterMul` when TIRED. ✅ P1. The `TimingErrorFrames` on `PitchCommand` was dead and is removed. ✅ P1

---

## 5. Batting

### 5.1 The four verbs

| Verb | Input | Swing |
| --- | --- | --- |
| Slap | Tap and release South | Full swing, widest window, base power. Plays the `swing-slap` take: no windup, compact (#613) |
| Charge | Hold to MAX (`swingChargeSeconds` 0.45), release in the MAX band | Narrower window (×0.78), more power (up to ×1.35 at MAX). Past the band the charge decays Plays the `swing-charge` take: the hold shows its windup, a bigger arc (#613) |
| Bunt | Hold West through the pitch | Batter squares at the press; contact when the ball reaches the bat (§5.8) |
| Star | North armed + South | Captain star swing (§13). Costs a star even on a miss |

Charge at MAX is a *charge* tell (rings line up; the swing shows MAX, the pitch its booklet word "Nice!"). Words about the contact — PERFECT / NICE / SOUR — come only from the typed zone once the bat meets the ball; a miss shows STRIKE (#578). ✅ P1

### 5.2 The cursor (sweet spot) — quality

The reference model (D4): the bat is a hitbox along the swing plane, split into **five zones** — sour / nice / perfect / nice / perfect — and the ball meets one of them by *where it crosses*, not by when you pressed. The cursor on screen is that hitbox drawn on the plate.

- A gold oval **follows the batter**, never the pitch (booklet-confirmed). Box walk moves it in X by the same world distance as the body; its Y is the zone center. The oval is the perfect+nice zones; the rim is sour. ✅ P1 (`SweetSpot`, world feet, `batting.cursor`)
- The oval is **tall enough that any strike is hittable**: its nice half-height is the zone half-height, and the sour rim is the barrel's rectangle `rimFraction` beyond it, so the corners of the frame are on the bat with the box centered. Quality is by the ellipse distance from the center: along X the bat barrel (nice half-axis `niceTipFt` 1.05 toward the tip, `niceHandleFt` 0.75 toward the hands — the handle side is shorter), along Y a ball at the top or bottom of the zone is at best nice. The perfect heart is `perfectFraction` (0.42) of the oval. The client draws exactly this outline (`SweetSpot.Outline`). ✅ P1 (S-05, S-06)
- Bat (contact) stat scales the barrel (`scalePerContact` 0.04 per point from 5); a charge **narrows** it (`chargeMul` 0.8; reference: charge zones are smaller than slap zones). Good-chemistry runners on base widen a slap's barrel (×1.05 / 1.10 / 1.20 for 1 / 2 / 3, `buddiesOnBase.widen*`). ✅ P1 (S-11, S-30)
- Vertical placement is *earned* by the pitch choice on the mound (a changeup dumps under the center; a high charged fastball rides over it). A **sour slap on a changeup or a charged pitch is a pop-up** (reference rule). That is the pitcher-vs-batter game. ✅ P1 (S-12)

| Zone | Slap exit (× base) | Charge exit (× base) | Tell |
| --- | --- | --- | --- |
| Perfect | 1.00 | 1.25 | PERFECT flash, crack, `solidFreeze` |
| Nice | 0.95 | 1.12 | NICE |
| Sour | 0.75 | 0.95 | dull thud; forced pop/topper by timing |
| Off the bat | miss | miss | whiff |

The two columns are `batting.quality.slap` / `.charge`, interpolated by the effective charge (a decayed overcharge lands between them). The perfect charge is the ×1.25 of §5.5. Reference numbers behind the ratios: slap sour 100–130 / nice 140–145 / perfect 145–150; charge sour 140–150 / nice 162–177 / perfect 160–170, with the perfect charge carrying because it gets no added gravity.

### 5.3 Timing — the window and direction

`err` = (press time − (ball-plate time − `window.leadSec`)) in frames at 60 Hz (D13, #612; `leadSec` 0.10, `AtBatMotion.SwingErrorFrames`). The take's `Contact` mark (`Motion.SwingContact`, 0.30 into the take) is an animation contract, not the judgment: for a press inside the window the take is warped so the mark lands on the ball's plate time (`AtBatMotion.SwingContactSec`, `SwingClipTime`) — load → contact is compressed onto the span from the press to the plate, the follow-through plays at the take's own speed, the keys keep their order and the take never plays backward; a press inside the window but after the ball is on the plate (only a widened window) lands Contact at the press. Outside the window the take plays at its natural 0.50 s and the bat misses the ball honestly. The warp is read at the press from the same window number the resolver judges (`Match.SwingWindowFrames`). ✅ #612 (S-07, S-08, S-09; `SwingPresentationTests`)

The take is the swing that is judged (#613): a charge (`ChargeFeel.IsCharge`, the same test that narrows the window) plays `swing-charge`, anything else `swing-slap`; both share the Contact mark and the measured approach and contact keys, so the warp above is one rule for both. Both end on a held finish at 0.60 that stays up through the STRIKE stamp until SET, or through the contact freeze until the batter-runner is `feel.swingFinishStepFt` out of the box (#583). ✅ #613 (`SwingPresentationTests`, `MotionTests`, the swing matrix `finish` beat)

- **Window**: slap **9 frames**, charge **7 frames** (reference), + (contact − 5) × 0.4, × skill multipliers (`star-skills.json` `batterWindowMul`) × the park's × the difficulty rung's `cpu.json` `humanWindowMul` for a pad's swing only (EASY 1.3 / NORMAL 1.0 / HARD 0.9, printed on the title's difficulty line), **floored at 5 frames** (`batting.window`). The window is a total width: the bat is on the plane when |err| ≤ half of it. Outside it the bat is not on the plane: **miss**, strike. The Charge Bat keeps the slap window. ✅ P1 (S-08, S-09, S-10, S-30; `AtBatResolver.ContactWindowFrames`)
- Inside the window, timing does **not** change quality (D4). It changes **direction**: early contact **pulls**, late contact **pushes** (opposite field). Linear across the window: earliest frame ≈ 55° toward the pull line (`spray.timingDeg`), center ≈ straight at second, latest ≈ 55° toward the opposite line. Stick L/R at contact shifts the whole range by ±12° (`spray.stickDeg`). The zone adds its spread (`spray.*SpreadDeg`). ✅ P1 (S-07, S-08)
- The batter's handedness mirrors the map. A right-handed batter who is early hits toward 3B. ✅ P1
- Quality still moves slightly with timing only through the *rim*: the outermost (1 − `squareFraction`) of each half-window reduces the cursor zone by one (perfect → nice, nice → sour) because the bat is not square. Never two zones; sour stays sour. ✅ P1

### 5.4 Height and the stick

- **Launch** = base by power and charge (`launch.loftBaseDeg`, `loftPerPower`, `charge.loftDeg`) **+** the pitch height (`perFtOfHeight` per foot the crossing sits above the zone center: a low pitch launches lower) **+** stick U/D at contact (`stickDeg`): **Up = over the top = grounder**, **Down = under = lift**. The reference maps up→grounder, down→fly and that is the rule. The same axis does **not** reset the box: Down-reset is a SET verb only, before the windup. ✅ P1 (S-06, S-13)
- Launch noise is ±`noiseDeg`/2. Sour contact is forced to a band: the topper band (`topperMinDeg`..) when early, the pop band (`popMinDeg`..) when late or on the §5.2 pop-up rule. ✅ P1 (S-12). The five-band probability table (topper / grounder / liner / fly / pop by zone × swing × stick) is the P2 batted-ball class table; until then the class is read from the launch.
- Sluggers' "scatter hit" (D-pad at contact) is our stick L/R above. The reference guide calls it unreliable; ours is deterministic.

### 5.5 Exit velocity

`exit = base(power) × zone × charge × starSwing × buddies × pitch`.

- `base(power)` = `batting.exit.baseMph` + Power × `batting.exit.mphPerPower` (61 + 3.7 × Power; P7 lifted the base from 57 for the S-29 band and left the slope, so the whole lineup hits harder and the power hitter's homer stays inside the ≤ 2 per game mean).

- `zone × charge`: the §5.2 table — 0 → the slap column; MAX → the charge column (**×1.25** on a perfect; reference: charge perfect 160–170 vs slap perfect 145–150, with less gravity). Below MAX interpolates; past the band the charge decays and the exit slides back toward the slap column. ✅ P1 (S-11, S-30)
- `buddies`: good-chemistry runners on base — **×1.10 / 1.25 / 1.50 on a charged swing only** (`buddiesOnBase.*Mul`), and the slap-zone widening in §5.2. ✅ P1
- `pitch` (`batting.pitchFactor`): a charged pitch met with sour contact ×0.6; met with a perfect charge ×1.1 (reference "pitch type impact"). A high-Pitch arm dampens non-perfect contact per stat point above 5 (nice ×0.9, sour ×0.75 at Pitch 10) — the reference's hidden "cursed ball" made visible as the Pitch stat. ✅ P1
- The Charge Bat gives a manual-MAX charge for free and keeps the narrow charge zones and the charge window off; it is never worse than a manual charge. ✅ P1 (S-30)
- Pull / push hitters (`data/characters/` optional `hitType`): ×1.05 to the named side, ×0.9 to the other. Optional; default mid.

### 5.6 Fair, foul, home run

- A ball is **foul** if it *lands* (or is first touched by a fielder) in foul territory, or rolls foul before passing a base without being touched. It is **fair** if it lands fair past the bases, or is touched fair, or leaves the park between the poles. The chalk is geometry, not a spray cutoff. ✅ P2: the untouched path's verdict is `BattedBall.Foul`, decided where the ball first lands past the bags, where it crosses the bag circle (90 ft) on a roll, where it rests, or where it touches a foul wall (S-20 … S-23). The wind bends the path, so the landing spray is not the spray at contact. There is no spin in the flight: a roll only curves with the wind. Touch-by-a-fielder is the live ball's call (P2 part b).
- **Home run**: the flight crosses the fence line above fence height between the poles. One rule, used by the flight, the fielding preview, and the landing ring. ✅ P2 (`BattedBall.HomeRun` from the fence crossing; the launch bands are gone).
- A ball that hits the wall is live (§7.9). A ball that bounces over is a ground-rule double (§1). ✅ P2
- **Foul ball** with fewer than 2 strikes adds a strike. With 2 strikes, nothing (except bunt). Runners return. The ball still flies so the camera can chase it; the stamp says FOUL when it lands. ✅ P2: a foul is a live ball the sim plays out (`LivePlaySystem`, `FairFoulCall`); it commits as `PlayKind.Foul` through `FinishInPlay` at the untouched path's verdict (plus the `flight.deadBall` beat) or at a touch on foul ground, and goes through `AfterPitch` like a take or a miss.
- A foul fly can be **caught** for an out (§7.11). ✅ P2 (S-24; the touch before the landing mark is the catch).

### 5.7 Hit by pitch — see §4.6.

### 5.8 Bunt

- Hold West before the pitch: the batter squares (pose tell for the defense). Contact is judged when the ball reaches the bat plane, same clock as a swing (no `leadSec` because the bat is already there, `AtBatMotion.SwingErrorFrames(bunt)`). ✅ P1
- Quality: the window and the cursor apply; a bunt off the bat is a miss. A sour bunt, or a crossing more than `bunt.popAboveCenterFt` above the zone center, is a **bunt pop** (the pop band). ✅ P1 (S-19)
- Ball: exit ×0.42, launch 3–12°, spray toward the stick side ±14° (timing does not steer a bunt). Never a home run. ✅
- **Foul bunt with two strikes is a strikeout.** ✅ P1 (S-18)
- Bunt fielding: P, C, 1B, 3B charge (§7.3). Runner rules: sac bunt is a live play, not a table.

### 5.9 CPU batter

A table (`batting.json` `cpu`, `Match.CpuSwing`), evaluated when the ball crosses the plate plane (same instant a human's swing would be judged), from the **final** trajectory. `CpuSwing` has no side effects: the box and the swing are the returned command. ✅ P1 (S-28)

| Pitch | Count | Action |
| --- | --- | --- |
| In zone, middle third | any | Swing. Charge if count is 2-0, 3-1, 3-0 (Bat ≥ 6) or a runner is in scoring position with < 2 outs (Bat ≥ 7) |
| In zone, edge | < 2 strikes | Swing 65%, take 35% |
| In zone, edge | 2 strikes | Swing (protect) |
| Out of zone, near | < 2 strikes | Chase (18 − Bat) % |
| Out of zone, near | 2 strikes | Chase (30 − Bat) % |
| Out of zone, far | any | Take |
| Runner on 1st, 0 outs, Bat ≤ 5, trailing by ≤ 2 | | Sac bunt 35% |
| Star | ≥ 1 star, captain, runner on or 2 strikes | 20% |

**Tracking** (the reference model): the CPU batter's guess is the last crossing this offense saw; after release it re-reads the ball and centers the cursor on it `trackPerfectChance` (0.55) of the time, else the box stays at the guess plus a fixed offset (`mistrackMinFt` 0.11 – 0.41 ft, × the rung's `mistrackMul`, worse on easy). **If the pitcher moved on the rubber since the last pitch, the mistrack chance rises sharply** (`mistrackMovedChance` 0.7 vs `mistrackChance` 0.3). That is why walking the rubber is a real verb against the CPU. Timing error σ = (11 − Bat) × `errorFramesPerBatStat` 0.62 frames × the rung's `timingSigmaMul`; when it did not track, a changeup pulls it `fooledMinFrames` 4–9 frames late and a charged pitch that many early. Zone classes: *middle* is the inner `middleFraction` of the frame; *near* is outside by at most `nearFt`. ✅ P1 (S-28)

**No forced-miss clamp against a human pitcher**: the human's meatball is punished by the same table; difficulty is a σ multiplier and the mistrack table (`data/rules/cpu.json` `difficulty` 0.8 / 1.0 / 1.3). ✅ P1 part a: `CpuSwingVsHuman`, its `batting.cpu.vsHuman` numbers, and the `cpuVsHumanTake` / `cpuVsHumanMiss` feel rolls are deleted; one table whoever pitches. ⚠️ `CpuSwing` must not arm steals (side effect, `Match.cs` `CpuSwing`) — that is the runner AI (§11.6, P6).

Charge vs slap by archetype (reference, `cpu.archetype`): balanced 50%, power 80%, speed 30%, technique 10% — derived from the character's Bat/Run split (Bat − Run ≥ `splitStat` = power, Run − Bat ≥ `splitStat` = speed, both ≥ `techniqueMin` = technique). ✅ P1

---

## 6. Ball flight and the field

### 6.1 Flight

- 3-D ballistic with drag taken relative to a directional park wind (`Park.WindMph` + `WindDeg`, `flight.windMul` of the flag reading), integrated at 120 Hz. ✅ P2 (`BallFlight.Trajectory(exit, launch, spray, park)`; every `Sample` carries X / Z / height and an event).
- Sample times are stretched by `TimeScale` (1.65) so gloves can get under a fly. Carry does not change. Pitches and throws are **not** stretched. **The stretched sample clock is the play clock**: `LivePlaySystem.ElapsedSeconds` advances on it, the ball sits at `PointAt(path, ElapsedSeconds)`, and every glove, runner, and throw is judged against it — one clock (§0.3). ✅ P2 (explicit in `BallFlight` and `FlightRules.TimeScale`; the one-clock test in `FlightScenarioTests`).
- **The stretch is per shape** (P7): a fly or pop runs on `flight.timeScale` (1.65, the watchable hang); a **liner** runs on `flight.linerTimeScale` (**1.0** — a rope is not stretched, so it gets past the glove or it does not, §7.6); a ball on the dirt (topper, grounder, chopper, bunt) runs on `flight.dirtTimeScale` (1.65, the scoop-and-race the infield was tuned on). The shape is read at the crack (`BattedBallClasses.ByLaunch`), so one contact has one clock for its whole flight, bounces and roll included. ✅ P7 (`FlightRules.TimeScaleFor`).
- Bounce: restitution 0.48, horizontal 0.82; liners (14–22°) skid (0.28 / 0.93). Roll friction 22 ft/s². Rest at 1.4 ft/s. Wall carom: normal × 0.48, along the wall × 0.82 (`flight.wall`). ✅ (`data/rules/flight.json`)
- **Fence**: the path is clipped by the park boundary (`FieldBounds.Of(park)`): the outfield fence between the poles is the circle through the park's L / C / R posts (`RoundFence`) at `fenceHeightFt`; the foul wraps are the diamond kit's hip rail (36 ft off each line, flaring to the pole) and the round backstop (36 ft behind the plate). The drawn wall is the same number (D15, `HarborWall.OutfieldHeight(park)`, `HarborWallTests`). Below fence height → wall carom (§7.9). Above, between the poles → home run. Bounce then over → ground-rule double. Any touch of a foul wall, or over one, → foul, dead. ✅ P2
- **Foul lines / backstop / side walls** are the field boundary; the ball cannot leave the park except over the fence or into foul stands (dead). ✅ P2 (property test: no sample of a fair trajectory lies outside the polygon except above the fence).
- **Landing mark**: the first ground contact of the clipped path; the ring is where a glove has to be. Wall plant if the ball meets the wall or clears the fence first (`BattedBall.HangT` is that instant; the catch window sits on it). ✅ (`FlyCatch.ChaseTarget`)
- **Landing guards** share one time base: the ground does not exist before `flight.landing.firstGrassMinSec` of play time, and every landing fact (hang, carry, the ring) reads the sample events, not a second height threshold. ✅ P2

### 6.2 Batted-ball classes

Class is a function of launch angle and exit velocity at contact, used by fielding assignment, cameras, and runner AI. One table (`data/rules/flight.json`):

| Class | Launch | Exit | Typical |
| --- | --- | --- | --- |
| Topper | < 3° | any | Weak roller in front of the plate |
| Grounder | 3–10° (and 10–14° under 74 mph) | any | Infield hop |
| Chopper | < 14° with a first bounce inside 30 ft and bounce height > 3 ft | ≥ 70 | High bounce, slow to the glove |
| Liner | 10–22° | ≥ 74 (`linerMinExitMph`; P7 lowered it from 78) | Rope; not stretched (§6.1) |
| Fly (infield / pop) | > 22° (or 14–22° under 74 mph) | first landing < 155 ft | Pop-up, long hang |
| Fly (outfield) | > 22° (or 14–22° under 74 mph) | landing ≥ 155 ft | Routine / deep by carry |
| Wall ball | fly or liner that meets the fence below fence height | | Carom |
| Home run | crosses the fence above height | | Dead, runners circle |
| Bunt | bunt verb | | Dribbler in the triangle |
| Foul | lands / touched in foul territory | | Dead unless caught |

✅ P2: `BattedBallClass` and `BattedBall.Of` (`flight.classes`). `BattedBallClasses.ByLaunch` is the read at the crack (topper / grounder / liner / fly) the cameras use; the flight refines it (chopper by the first hop, pop by the landing, wall and homer by the fence). `AtBatResult.Class` and `FieldingPreview.Class` carry the shape (topper … homer, bunt) with `Foul` beside it as the chalk; `BattedBall.Class` is the spec's row (Foul when the untouched path is foul). A bunt popped up (§5.8) is a pop, not a bunt. The pursuit pool is `FieldingResolver.PursuitPool(class, foul)`. The chopper row reads "< 14°" rather than "3–14°": launched from the bat's height, no ball at 3° and 70 mph first bounces inside 30 ft — the chopper is a ball driven down, and it becomes reachable once §5.4's launch bands (P1) include the topper band below 3°.

---

## 7. Play types — what happens on each

Each subsection is one scene: **who fields**, **what runners do**, **the throw**, **the camera and stamp**. "Runners" means offense-controlled runners; a human presses, CPU follows §9.9. Fielder decisions are §8.8 for CPU; a human fielder has the verbs in how-to-play.

Common to all live plays:

- **Batter always runs** on fair contact. ✅
- **Forced runners run** on a grounder (they have no choice). Unforced runners hold on the bag until the ball is through or fielded, then go/hold by the send rule (a human) or the margin table (CPU, §9.9). ✅ P3 (`Runner.Forced`, `RunnerAi`); the read step is presentation.
- **On a fly / liner**, all runners hold on the bag until the catch or the drop (tag-up rule §9.5). ✅ P3 (`FlyState`, per runner).
- **The throw** goes where the fielder names (human) or where the decision table says (CPU, §8.8). The out is judged when the ball arrives (§10). ✅ for named bags.
- Camera: `diamond` 45° on the dirt under the ball; fly pulls back to `diamond-fly`; a throw does not cut behind the thrower (`data/feel/shots.json`). ✅
- Stamp: OUT / SINGLE / DOUBLE / TRIPLE / HOME RUN / DOUBLE PLAY / TRIPLE PLAY / FOUL / ERROR when the play is dead. ✅ P4: ERROR is the throw that skipped past its cover (`PlayOutcome.Error`, `PlayStamp.Error`); it stamps on the hit it allowed, never on an out.

### 7.1 Grounder to an infielder (routine)

- **Fields**: the infielder whose planned route meets the ball earliest (`FieldingPursuit.Choose`) — 1B/2B/SS/3B, P on comebackers, C on toppers. Gloves scoop by touching the ball on the dirt (no button). ✅
- **Runners**: batter to first; forced runners go; unforced hold at the read step, then advance only if the throw goes elsewhere and they can beat a relay (§9.9).
- **Throw**: nobody on → 1B. Runner on 1st → 2B for the force (then 1B if time, §10.4). Runners on 1st and 2nd → 3B if the fielder is 3B/SS near the bag, else 2B. Loaded → home if the fielder is inside 60 ft of the plate, else 2B. Human names the bag; the default follows this table. ✅ default bags; ✅ P4: the CPU throw is the decision table (§8.8) from the live bodies; the roll is gone and nothing is force-fed at hang (the resolver's `Kind` is only what the flight decides alone — a homer, a foul, a chomp — or `PlayKind.InPlay`).
- **Out**: force at the bag if the ball (in a glove on the bag) arrives before the runner. Tie to runner. Bobble (§8.6) adds time; it does not decide.
- Stamp OUT (one out) / FORCE OUT caption / SINGLE if the runner beats it (a fielder's choice or an error — stamp ERROR if the throw sailed).

### 7.2 Slow roller / topper

- **Fields**: P or C, or a charging 3B/1B. Bare-hand pose if the fielder is running toward home.
- **Runners**: batter races; forced runners usually safe (the throw goes to 1B by default because the force at 2B is not makeable — the decision table computes margins, §8.8).
- **Throw**: 1B unless a runner on 3rd is going home and the fielder is inside 45 ft (then home).
- Beat the throw → infield single (stamp SINGLE). ✅ P4 (S-32): the arrival compare, never a roll.

### 7.3 Bunt

- Defense tell: the batter squares at the West press; 1B and 3B **crash** (charge 25 ft toward the plate), 2B covers 1B, SS covers 2B, P and C charge the triangle. ❌
- **Fields**: earliest of P / C / 1B / 3B.
- **Runners**: batter runs; forced runners go (sac). Runner on 3rd with a squeeze: goes at contact only if the offense sent them (`send 3B`), else holds.
- **Throw**: 1B (batter) by default. Lead runner if the bunt is popped or too hard and the margin is makeable. Runner from 3rd on a squeeze → home only if inside 30 ft.
- Bunt pop-up caught → out; runners who left are doubled off if the fielder throws back (§10.5).
- Stamp BUNT + SINGLE / OUT. ✅ stamp exists.

### 7.4 Chopper

- High first bounce (§6.2). The fielder waits at the second hop (`Rolling`, first reachable point) or charges to take it on the short hop — the human chooses by stick; CPU charges if the runner's margin at 1B is < 0.3 s.
- Runners: as grounder. Choppers are the classic infield single.

### 7.5 Grounder through the infield (to the outfield)

- Infielder misses (no route reaches) → outfield hand-off: LF/CF/RF charge the roll (`HandoffToOutfield`). ✅
- **Runners**: batter rounds first and reads the outfielder (send to 2B if the pickup is deep and the arm is weak, §9.9). Runner on 1st → 3rd if the ball is to RF/CF and picked up beyond 200 ft, else 2B. Runner on 2nd → home unless the ball is hit to LF shallow and the arm is strong. Runner on 3rd scores. All by the margin formula, not a table. ✅ P3 (`RunnerAi.Margin` at contact, at the pickup, at each throw, at each bag).
- **Throw**: outfielder throws to the base *ahead* of the lead runner if makeable, else to the **cutoff** (§8.7) to hold the batter at first. A throw home goes through the cutoff unless the arm can reach on the fly.
- Runner thrown out at a base = tag (unforced) or force (batter at 2B when an outfielder throws there? no — the batter is only forced at 1B). ✅ tag/force distinction.
- Stamp SINGLE / DOUBLE; OUT at a bag stamps OUT with the caption naming the throw.

### 7.6 Line drive

- **Fields**: the infielder or outfielder on the line if a route meets the ball above 0.75 ft before the first bounce (catch window ≈ hang − 0.25). Liners are a **jump or dive** verb inside the window; a straight-at-you liner is a South catch.
- **Caught**: out. Runners who left the bag are **doubled off** if the fielder throws to that bag (or steps on it) before they return (§10.5). Runners at the read step are safe if they return in time — that is the tension.
- **Not caught**: it skids; the nearest fielder chases the live ball; runners as §7.5.
- Presentation: `solidFreeze` on the crack; the liner has the short hang so a dive is possible (Super Mega Baseball's rule; ours via the liner's own stretch, `flight.linerTimeScale` 1.0 — ✅ P7). Before P7 every liner was stretched like a fly and the outfield reached four of five of them (S-29 read 0.7 runs a side).

### 7.7 Pop-up (infield fly)

- **Fields**: the infielder or catcher whose route reaches the landing earliest; the P never takes a pop if anyone else can. Camera pulls back (`diamond-fly`).
- **Runners**: hold at the bag (CPU) or wherever the human puts them. No infield fly rule: a **dropped pop** is live, and forced runners are forced. CPU runners stay on the bag on a pop so a drop costs at most the force at the lead bag.
- **Caught**: out. Send after the catch = tag-up (rarely wise).
- **Dropped** (bobble, §8.6): live; the fielder picks up and throws by the grounder table.

### 7.8 Fly ball (outfield)

- **Fields**: LF/CF/RF by route to the landing; CF has priority on a tie. Runs to the **landing**, not the ball. ✅ (`FlyCatch.ChaseTarget`). Jump/dive windows from Field and ability (§8.4).
- **Runners**: hold; tag-up on the catch if sent (§9.5). A runner on 3rd with < 2 outs tags on any fly caught ≥ 200 ft from the plate (CPU rule; human decides).
- **Caught**: out (routine / DIVE / JUMP stamp). Throw after the catch to the bag ahead of a tagging runner; the margin decides (§10.2).
- **Dropped**: live, runners go by the outfield-single rule.
- **Sac fly**: the runner from 3rd scores if home arrival < throw arrival. It is a live throw, can be an out. ✅ P3 (the body tags at the catch and races the throw; `AdvanceTagUp` and the 230 ft literal are gone).

### 7.9 Wall ball / carom

- A fly or liner that meets the fence below fence height caroms (restitution 0.48, angle mirrored) and drops at the base of the wall. The outfielder plays the carom (route to the first reachable point on the post-carom path). ✅ P2 (`BattedBallClass.Wall`; `LiveEvent.WallCarom` is the thump the client plays; S-58).
- Runners: this is the **double / triple** scene. Batter reads the carom; runner on 1st scores on a carom to the gap with < 2 outs if the margin says so. ✅ P3: the bodies take what the carom and the arm give (S-58 asserts second or third, never a dead double).
- Rob: in the window at the wall, West (jump) with Super Jump / Clamber / Buddy Jump can catch a ball that would clear the fence by ≤ the ability's rob height (§8.4). ✅ P2 (`FlyCatch.CanRob` against `BattedBall.FenceClearFt`; S-56, S-57).

### 7.10 Home run

- Fence crossed above fence height between the poles. Dead ball. Batter and all runners circle at trot speed; scoring is immediate (the throw cannot happen). Stamp HOME RUN / GRAND SLAM. ✅ (the crossing is the one homer rule, P2)
- The ball keeps flying into the stands (presentation). Night: fireworks (Harbor).

### 7.11 Foul ball

- Foul grounder / foul pop: dead when it lands or leaves the field, **unless a fielder catches it** (foul fly out: C, 1B, 3B, LF, RF near the lines). ✅ P2: the foul pool is `FieldingResolver.FoulPursuitPositions`, routed by the same planner; the call is made where the ball lands, rolls foul, rests, leaves, or is first touched (`BattedBall.DecidedT`, `LivePlaySystem.Call`). A touch on fair ground before the roll went foul makes it a fair ball and the play goes on.
- Runners return. A caught foul fly is a fly ball for tag-up purposes. ✅ (it is `PlayKind.FlyOut`)
- Stamp FOUL when dead, within the count hold (S-24b). The play never hangs: a foul is a dead-ball result the sim commits itself (#575). ✅ P2

### 7.12 Strikeout / walk / HBP with runners

- Strikeout: dead. A runner stealing on the pitch is a **catcher throw play** (strike-em-out-throw-em-out DP, §11.5). ✅ steal throw after a miss.
- Walk: batter to first; only forced runners advance. ✅
- HBP: as walk. ✅ placement; ⚠️ geometry.

---

## 8. Fielding

### 8.1 Bodies and positions

- Nine positions from `Diamond.Positions` (feet). Defensive alignment is the lineup's glove diamond (Offense / Defense Setup), not roster order. ✅ P2: `Team.Gloves` carries the diamond and `FieldingResolver.Assign(team, pitcher)` reads it; after a pitcher swap (§4.7) the old pitcher takes the vacated glove (S-26). Preset teams with no diamond stand in roster order.
- Speed in the field: `chase = 21 + Run × 1.9` ft/s, **one formula** for human and CPU. ✅ P4 (`FieldingResolver.ChaseSpeedFt` with the dash multiplier; the stick table is gone).
- Frozen (park hazard) ×0.45. Dash (East held) ×1.35 for 2 s then fades.

### 8.2 Who is on the ball

- On contact the sim plans a route per candidate to the **landing** (fly) or the **first reachable point** (roller) and picks the earliest meet, then shortest travel (`FieldingPursuit.Better`). Ties: CF over corners, SS over 2B, infielder over pitcher. ✅
- Pools: infield dirt → P, C, 1B, 2B, 3B, SS. Air → LF, CF, RF, SS, 2B (+ C, 1B, 3B on pops in their sector). Grass → LF, CF, RF. ✅
- **Reaction lockout** after contact before a body moves, by position (reference frames → seconds): P 0.42, C 0.67, 1B 0.27, 2B 0.25, 3B 0.30, SS 0.28, **OF 2.4** (P7: the outfielder's read; 0.83 let the outfield reach every fly and liner on the stretched clock, S-29). The camera cut to the diamond happens at 0.42 (`feel.contactCutSeconds`, ✅ P8). Data (`fielding.reaction`). ✅ P4: every body, the human's stick glove included; the pursuit planner counts the lockout in its routes, so the glove picked at contact is the one whose body gets there first. The infield lockouts, the one glove speed (§8.1) and the CPU throw delay (§8.8) are the numbers the §10.4 double-play rows were tuned on and P7 left them alone. **Sitting 2026-09-12 (#609): the lockout is real seconds, never longer than the ball's remaining hang, applies to the human glove at the reference length only, and difficulty scales the CPU's; S-29 is held with another lever, not the read.**
- **Facing** (sitting 2026-09-12, #611 / #576): a body **turns and runs**. Moving faster than a walk (`CartoonJuice.WalkFtPerSec`), it faces its own velocity; planted or holding, it faces the ball (a ball within `feel.faceBallMinFt` 3 ft — overhead or in the glove — keeps the heading, so the ball dropping through the plant never turns it round); releasing a throw, it faces the target. The one named short-range case is the **backpedal**: the glove under a fly still in the air, inside `feel.backpedalFt` (12 ft) of the plant and moving away from the ball, faces the ball (walk cycle, not the sprint). The turn is `feel.bodyTurnDegPerSec` (720) × the frame's seconds — the same heading at 30 and 60 fps. Every body (fielders, runners, the batter-runner) turns by the one rule; the pitcher on the rubber and the batter in the box are pinned to home. A spin-check glove is not posed as a spin while it waits (the ability is the extra-base check, §13). ✅ #611 (`BodyFacing` / `BodyHeading`, drawn by `HeroActor.Place`; `BodyFacingTests`, S-576 counts heading changes under a routine fly: fewer than 3 after arriving inside the catch radius, planted for the last 0.4 s). A head look-at while running is not built.
- The **YOU** ring names the glove; Select/R swaps to the pulsing next-nearest. Dead stick = CPU runs that glove. ✅

### 8.3 Catch

- **Fly**: catch if the glove is inside the catch radius of the landing (or plant) when the ball is in the window `[hang − 0.48 − extra, hang + 0.14 + extra/2]`. Human presses South in the window; CPU catches automatically at the ball's arrival. Outside the window or radius = drop / falls in. ✅ (`FlyCatch`)
- Catch radius = 10 + Field × 0.6 (+ ability). A jump adds 8 ft of reach and a window bonus; a dive adds 8 ft along the lunge (10 ft) and only below 7.5 ft ball height. ✅
- **Roller**: standing on the ball scoops it, no button. ✅ Bobble check on scoop (§8.6).
- **Liner**: same as fly with the short window.
- **CPU catch is geometric**: the glove must be inside the radius at the window. It is **never force-fed at hang because a roll said out**. ✅ P4: the CPU glove takes a fly only by `FlyCatch.AutoCatch` (under the plant, in the window, and at the wall only inside its rob height, §8.4) and a roller only by touching it; a drop is rolled only for a star effect (heatball, phony swing, frozen) on the one seeded stream.
- **Loose ball**: a fumble, an overthrow, or a lob nobody came for leaves the ball on the ground in nobody's glove; it rolls to a stop (`fielding.overthrow`) and the nearest body chases it. A loose ball is picked up by touching it (`fielding.chase.looseScoopFt`), never by the catch radius.

### 8.4 Jump, dive, wall rob, buddy jump

| Verb | Input | Effect |
| --- | --- | --- |
| Jump | West in the window (arms through it) | +8 ft reach up; can rob a ball ≤ 4 ft over the fence at the wall |
| Super Jump (ability) | same | rob ≤ 18 ft over; window +0.16 |
| Clamber (ability, wall parks) | same at the wall | rob ≤ 28 ft over; window +0.12 |
| Buddy Jump | West with a good-chem partner planted within 26 ft | rob ≤ 18 ft; both bodies |
| Dive | East tap | 10 ft lunge toward the ball, +8 ft reach, ball < 7.5 ft |
| Grow / Lick (ability) | passive | +6 / +3 ft catch radius, window +0.08 |

✅ all windows exist (`FlyCatch`, `FieldAbilities`). ✅ P2: the rob heights are `fielding.catch.jumpRobFt / superJumpRobFt / clamberRobFt / buddyJumpRobFt`, judged against the ball's clearance over the fence at the crossing (`FlyCatch.CanRob`, S-56 / S-57).

### 8.5 Throws

- **One throw model.** `throwSec = 0.22 + dist / (100 × arm × chem × ability)` ft/s, with `arm = 0.85 + Field × 0.03`. This one number flies the ball *and* judges the bag, for every arm on the field — the catcher's gun on a steal included (§11.3). ✅ P4: `InPlay.ThrowSec` over `ThrowResult.SpeedMul` = arm × chemistry × ability (`fielding.throw`); `StealThrow.CatcherThrowSec` is the same clock over the plate-to-bag distance. The base was the reference 56 ft/s (38 mph) until P4; at that speed a routine grounder to short could not retire a Run-5 batter (S-31), so the base is 100 ft/s (68 mph): the number is data, the scenario is the contract.
- **Accuracy**: lateral error σ = (11 − Field) × 0.35 ft. A throw that lands more than 6 ft from the cover is **not caught** — it skips past, the ball is live, runners take what the pickup gives them (stamp ERROR). ✅ P4: `ThrowResult.LateralFt` is sampled on the throw, `InPlay.ThrowLanding` puts the ball there, and the receiver's reach (`fielding.cover.radiusFt`) decides the catch where it lands (S-35).
- **Chemistry** (systems.md, reference): good ×1.30 speed, purple laser, never to the cutoff. Bad: **20% of throws are "slanted"** — ×0.70 speed with a 10–14 ft lateral miss (an error by the rule above); the other 80% are ordinary. The roll is on the *input* (the throw's accuracy), the outcome is still the ball missing the cover. ✅ P4 (`fielding.chem.slant*`; the boolean `Error` and the Single conversion are gone).
- **Situational speed**: a throw to an **uncovered bag hangs as a lob until the cover arrives**, and if nobody comes inside `fielding.throw.lobMaxSec` it drops at the bag, live. This is how "the receiver must be on the bag" reads on screen. ✅ P4. The lazy lob to a bag nobody can beat (×0.35, reference) is presentation and is not modelled.
- **Reach on the fly**: a throw longer than `fielding.throw.onTheFlyFt` (200 ft) goes through the cutoff on the line (§8.7); the CPU's margin for such a bag counts the cutoff's reaction and arm.
- Abilities: Laser ×1.45, Snap Throw ×1.22. ✅
- The thrower's body: after the throw the fielder **stays where they are**; the receiver at the bag is whoever covers (§8.7). ✅ P4: the YOU ring hands to the receiver at release and follows that body's walk to the bag; the thrower's body stays put (`FieldAssist.AfterThrowPos` is gone).
- You may **arm a bag before the catch**; the throw fires on South after the catch. ✅

### 8.6 Errors

- **Bobble**: on a scoop, chance = f(ball energy, hands) — hard-hit balls to weak gloves. A bobble is a 0.58 s fumble with the ball scattered ≤ 6.5 ft, loose on the ground; the play is live, the glove is out of it for the fumble and then chases, and the runner gains that time. It **never converts an out into a caption** and is not by itself the error. ✅ P4 (`LiveEvent.Bobble`; the resolver conversion and the Unity re-roll are gone).
- **Throwing error**: the lateral miss above. ✅ P4.
- **Drop** (star effects, frozen): a fixed drop chance on the catch is allowed for *skills* (burn-hop, phony) because the skill is the two-second rule; it is never allowed for plain baseball. ✅ P4 (`Match.RollDrop`, `fielding.drops`).
- Stamp ERROR ✅ P4 (`PlayStamp.Error` on the hit a sailed throw allowed). The ERROR tell at the sail ✅ P8: `LiveEvent.ThrowSailed` pops the small ERROR sticker the moment the throw skips past (`PlayStamp.LiveTell`, the same rail as the small SAFE), and the play's stamp follows at Time.

### 8.7 Cover, cutoff, relay, backup

- **Cover**: on contact each non-fielding infielder walks to the bag they cover — 1B covers first (2B covers first if 1B is fielding), 2B/SS cover second (whichever is not fielding; with both free, the one away from the ball's side), 3B third, C home, P backfills any bag whose cover is the glove. Cover moves at a **flat cover speed** starting 0.23 s after contact (constant, stat-independent; D11) — `fielding.cover`. ✅ P4 (`InPlay.CoverMap`). Outfielders not on the ball back up the throw 60 ft behind its target.
- **Cutoff**: on a throw longer than the arm's fly reach (§8.5), the cutoff is the infielder nearest the line between the fielder and the target who is neither the glove nor the bag's cover (SS for LF/CF, 2B or 1B for RF). Geometric. ✅ P4 (`InPlay.CutoffFor`). LB / X with no bag = throw to the cutoff on the line to the armed bag (home by default). ✅ verb.
- **Relay**: a throw to the cutoff continues automatically to the armed bag (human) or by the decision table from the cutoff's spot (CPU) with the cutoff's own arm. ✅ P4.
- **Backup**: the pitcher behind first and home, the outfielder nearest the spot behind second and third, runs to the backup spot 60 ft past the target on the throw line (`InPlay.BackupSpot`, `InPlay.BackupPos`). It decides where an overthrow stops: the loose ball rolls on and the nearest body — the backup, when they are there — picks it up. ✅ P4.

### 8.8 CPU fielder decisions

Computed at the moment the fielder gains the ball, from live positions. For each candidate bag `b`, `margin(b) = runnerArrival(b) − (throwSec(b) + release 0.25)`. A play is *makeable* if `margin > 0.15` (tie band). Choose in this order:

1. **Force at the lead forced bag** if makeable and outs < 2 → throw there; if the receiver then has a makeable relay to the next bag behind, chain it (double play, §10.4).
2. **Home** if a runner is going home and makeable (tag).
3. **Third** if the runner from 2nd is going and makeable (tag).
4. **First** if the batter is makeable.
5. Otherwise **hold**: throw to the bag ahead of the lead runner's next advance (2B on a single with nobody on) or to the cutoff. Never a wild "throw to first" that lets a runner score behind it.

Outfielders: 1) home if makeable and a run is at stake (score within 2 or < 2 outs), 2) third, 3) second, 4) cutoff. Fielder reaction delay before the throw = 0.35 − Field × 0.02 s.

Difficulty (`cpu.json`): margin threshold 0.30 / 0.15 / 0.05 and reaction 1.4× / 1.0× / 0.8×.

✅ P4 (`LivePlaySystem.CpuDecide`): the table runs once per possession, after the reaction (`fielding.reaction.throw*` × `cpu.reactionMul`), from the live bodies and `RunnerSystem.ArrivalSec`; rule 1 is the lead force ahead of a forced runner (second, third, home), the batter at first is rule 4; a tag candidate is a runner whose body is bound for the bag; "makeable" is `margin > cpu.makeableMarginSec`; a bag beyond the arm's fly reach is judged through the cutoff (§8.5, §8.7). The receiver of a caught throw runs the table again after its own reaction, which is the double-play chain. An infielder with nothing makeable holds and Time comes (§10.6); an outfielder throws in to the cutoff or to the bag ahead of the lead runner.

✅ P5 adds to the same table: after a catch, a body off its start bag is a force back there ahead of rule 1 (`RunnerSystem.ReturnSec` against the throw, §10.5); with **two outs** any makeable out ends the inning, so the shortest makeable throw goes (S-50); the **plate is worth the throw** whenever the ball can land inside the close margin of the body (`running.close.marginSec`), even short of the tie band — the mash or the tag at the plate decides, a run is never conceded by a glove holding the ball; a play the table chose at a bag inside `fielding.throw.unassistedFt` (8) of the glove is made by **stepping on it**, not a throw (S-41, S-46); and a body caught between bags is run at (§9.7).

✅ P6 adds: **second** joins home and third as a tag candidate (a body bound for second unforced — the stealer, the batter rounding first) and every tag bag is **worth the throw** inside the close margin, not only the plate (the catcher throws on a close steal rather than conceding it; the tag at the bag decides). Among the tag bags the lead body is played unless a trailing body's margin is better by `running.steal.cpuTrailPreferSec` (0.3, the double steal, §11.3). The play at a bag is made by **whoever gets the ball there first**: the glove's own legs (`fielding.chase`) or a throw to a cover who must be at the bag to take it (`fielding.cover` walk; a lob waits, §8.5) — so the catcher walks to the plate on a steal of home instead of lobbing at a bag nobody covers, and a first baseman who fields it near the bag takes it themselves. `margin` is read against that arrival.

---

## 9. Baserunning

### 9.1 The runner model

- Each runner (including the batter-runner) is an object with **position on the basepath** (bag index + feet along the segment), **velocity**, **state** (`OnBag`, `Advancing`, `Returning`, `Stealing`, `Sliding`, `Out`, `Scored`), a **destination bag**, and a **forced** flag snapshotted at contact. ✅ P3 (`Runner`, `RunnerSystem`; `Match.Runners`, `Match.BatterRunner`; the three nullable slots and `RunnerState` are gone, and Complete re-seats the same objects instead of rebuilding them).
- Speed: `bagSec = 3.55 − Run × 0.12` (clamped 2.45–3.65) per 90 ft, **one formula** for every segment (`running.bagSec`). The batter-runner starts **0.5 s after contact** from where they stood in the box (reference: 31 frames); the box is a few feet closer to first for a left-handed batter, so they arrive about 0.1 s sooner by our geometry (the reference's 0.5 s is its box placement, not ours). Dash ×1.12 at full mash. ✅ P3 (`RunnerSystem.BagSec` / `SpeedFtPerSec`; `homeToFirst` and `bagToBag` are gone).
- **Runners cannot pass each other** (a trailing runner inside 27 ft of the runner ahead stops behind them, `running.bagSec.noPassFt`). ✅ P3
- A runner's position is what every tag, force, and arrival uses. No closed-form "beats" re-derivation. ✅ P3 (`RunnerSystem.ArrivalSec(runner, bag)` is the sim query P4's fielder reads; `BatterBeatsThrow`, `RunnerBeatsTag`, `RelayBeats` are gone).

### 9.2 No leads (D1)

- Runners stand on the bag until contact, a steal break (§11.2), or a send. There is no lead stick, no lead pip, no pickoff risk from standing there. The stick-toward-a-bag verb during SET **arms a steal for the selected runner** (same as L3), which keeps the couch map simple: point at the bag you want, press to go. ✅ P3 (`Runner.StealArmed`; P6 lands the break).
- `Lead01`, `TakeLead`, `ReturnToBag`'s walk-back, `LeadSpot`, `MiniLead`, the Unity lead rates, the lead pips, and the Lead chapter of how-to-play are retired. ✅ P3. The random pickoff on a walking lead (`ResolvePickoff`, `pitching.cpu.pickoff`) went with them (D3): a runner on the bag is always safe; a pickoff plays only on an armed runner.

### 9.3 Send / hold per runner

- **All-advance (LB / `,`)**: every runner's destination = next bag (and the next after that if they arrive and it is open). **All-return (RB / `.`)**: every runner returns to the last bag. **Freeze** (both / `/`): hold where they are. A tap of the opposite button halts (reference). ✅ P3 (`LivePadInput.AllAdvance / AllReturn / Freeze` through the live ball's `Tick`; the tap-halt is the press edge).
- **Per-runner**: D-pad selects a runner (right 1B, up 2B, left 3B, down = batter-runner); stick toward the next bag sends that runner; stick back returns that runner; halt freezes that runner. ✅ P3 (`Match.SendRunnerAt`, `ReturnToBagAt`, `HaltAt`; the same verbs before the pitch select and arm).
- Forced runners cannot be held on a grounder once the batter reaches first (they are forced off). A human "hold" on a forced runner is ignored until the force is removed (batter out at first). ✅ P3 (`Runner.Forced` against the live force chain).
- The **batter-runner** is selectable (down / 4 while running) and obeys the same send/return: round first and go, or stop at the bag. ✅ P3

### 9.4 Dash, slide, rounding

- Dash: mash South, +0.28 per press to 1.0, decays 0.5/s; ×1.12 speed at full. ✅
- Slide: automatic on the last 12 ft into a bag when a tag is threatened (throw armed to that bag or a glove with the ball within 20 ft) and the runner is stopping at that bag; a runner rounding never slides (reference). West/South near the bag forces it. A slide shrinks the tag reach by 2 ft; it does not change arrival time. ✅ P3 (`running.bags.slideFt / slideThreatFt / slideReachCutFt`; `InPlay.Touches(sliding:)`).
- Rounding: a runner heading past a bag runs a shallow arc (presentation) and reaches the next bag at the same `bagSec` — no time penalty in the arcade rule. ✅ P3
- Overrun first: the batter-runner may run through first base and is safe from a tag while returning directly, unless they turn toward second (then live). ✅ P5 (`Runner.OverrunFt`, `running.bags.overrunFt` 12: the body runs through on the line from home and comes straight back, holding the bag and untouchable the whole way; Time waits for the return; sent on while out there they turn back at once and are live). Every other body stops on the bag it is stopping at.

### 9.5 Fly balls and tag-ups

- On a catchable fly/liner the game **sends every runner back to their bag** (reference: automatic return on a fly). A human can override with a send, at the doubled-off risk. ✅ P3 (`FlyState.InAir` holds every runner but the batter, who runs to first and waits there).
- Runners **cannot leave until the ball is firmly caught** (post-bobble). After the catch, a runner on the bag may advance (**tag up**). A runner off the bag at the catch must return and touch before advancing; if the defense throws to that bag and the ball beats them back, they are out (doubled off, §10.5). ✅ P3 (`Runner.LeftEarly`; `ThrowVerdict.DoubledOff`).
- All-advance pressed *before* the catch means "tag and go on the catch" — the runner waits on the bag and leaves at the catch. ✅ P3 (`Runner.TagAndGo`, per runner; LB before the pitch arms it for the coming fly).
- A lone runner cannot cross home on a fly with < 2 outs until the catch/drop resolves (reference restriction; keeps a dropped fly honest). ✅ P3 (the body is held a foot short of the plate).
- CPU: runner on 3rd tags on a caught fly ≥ 200 ft with < 2 outs; runner on 2nd tags to third on a fly to RF ≥ 250 ft; else holds. Batter-runner on a fly stays near first until the drop/catch. ✅ P3 (`running.cpu.tagThirdMinCarryFt / tagSecondMinCarryFt`).

### 9.6 Close plays

- A close play is a **geometric** condition: the throw arrives within ±`closeMargin` (0.25 s) of the runner at a tag bag (3B or home; a force is never close-played). Only then does the **mash contest** run: the icon appears, first press after the icon wins, CPU reacts at `0.20 + (10 − stat) × 0.032`. Outside the margin the geometry decides and no icon appears. ✅ P5 (`ClosePlay.WithinMargin`, `running.close.marginSec` 0.25): the ball on the bag **ahead of the body** by no more than the margin runs the mash; the body **in ahead of the ball** by no more than the margin is safe on the bag (§10.3) and pops the small SAFE, no contest; further out either way the geometry decides silently — the tag at the bag, or the runner in. Once one seat has pressed and the clock is past that press the other seat can only be later, so a seat that never presses loses to the CPU's reaction (S-75). The verdict is written once (the out is recorded, or the body is placed on the bag; the caption follows the record; `ClosePlaySafe` is gone).
- Stamp SAFE (small) / OUT.

### 9.7 Rundowns

- A runner caught between bags (a fielder with the ball inside 20 ft on the path, the runner not on a bag) is in a rundown: the fielder runs the runner toward the bag they came from; covering fielders take throws; the runner may reverse (stick). The tag rule (§10.3) ends it. A rundown ends by tag, by the runner reaching a bag, or by a throw that misses (runner advances). ✅ P5 (`running.rundown.rangeFt` 20; `LivePlaySystem.ReadRundown` flags the body each frame, `Runner.InRundown`; `LiveEvent.Rundown` cues it). A glove standing on the bag a body is still closing on is not a rundown: it waits there and the tag at the bag decides (§10.3). A body that stops or turns away is caught.
- CPU runner in a rundown reverses each time the ball is thrown. CPU fielders throw when the runner is inside 8 ft of a covered bag and run at them otherwise; when every runner is ≥ 80% of the way to a bag the throw is a lazy lob (reference). ✅ P5 (`RunnerAi`: a throw to the bag ahead turns the body back, a throw to the bag behind sends it on; `LivePlaySystem.TickCpuRundown`: `running.rundown.throwWithinFt` 8 to a bag whose cover is inside the cover radius, the chase at the one glove speed otherwise, `lazyLobFraction` 0.8 / `lazyLobSpeedMul` 0.5). A CPU glove with nothing makeable on the table runs at a stray body wherever it stands: a runner frozen on the path is never left standing (Time needs every body on a bag, §10.6).
- Runners stay on the basepath (no running off the line to dodge — the reference exploit is closed by clamping the runner to the path). ✅ (the bodies are one-dimensional on the path).

### 9.8 Scoring and the play's end — see §10.6.

### 9.9 CPU baserunner decisions

Evaluated at contact, at every fielder touch, and at every throw release (events, not per frame). For each runner, `margin(next) = throwArrival(next) − runnerArrival(next)` using the fielder currently on (or nearest) the ball, their arm, and a reaction of 0.35 s.

| Situation | Go if | Notes |
| --- | --- | --- |
| Forced on a grounder | always | |
| Unforced on a grounder to the infield | margin(next) > 0.4 and the ball is not in front of them | A runner on 2nd does not run at a grounder to SS/3B in front of them |
| Runner on 3rd, grounder, < 2 outs | infield back (fielder ≥ 110 ft from home) or margin(home) > 0.3 | "Contact play" with 2 outs: always go |
| Hit through / to the outfield | margin(next) > 0.5 − Run × 0.03 | Aggression by Run; two outs: +0.3 (go more) |
| Batter-runner rounding first | margin(2B) > 0.6 − Run × 0.03 | Reads the pickup: ball behind the outfielder = go |
| Fly ball | hold; tag-up rules §9.5 | |
| Score situation | trailing by ≥ 3 in the last inning: thresholds −0.2 | Aggressive when desperate |
| Steal | §11.6 | Not in the swing function. A body on its steal segment never turns back on the catcher's read (only the rundown reverses it, §9.7) ✅ P6 |

Reference shape for the "go" rule: keep going if time-to-bag < throw-time − 0.33 s (−0.5 s on easy), with a bonus once past 40% of the segment; otherwise turn back with a 12–20% chance of a mistake. ✅ P3 (`RunnerAi`, `running.cpu`: the thresholds above, `commitFraction` 0.4 for the turn-back, the mistake roll left out — no roll decides a runner). Difficulty adds `cpu.*.runnerMarginSec` (+0.15 easy, −0.15 hard) to every threshold. `throwArrival` is the defense's live read: the glove on (or the route to) the ball, `running.cpu.reactionSec` 0.35, then `InPlay.ThrowSec` over the distance.

---

## 10. Outs

### 10.1 The five ways

| Out | Condition |
| --- | --- |
| Strikeout | Three strikes (§1) |
| Catch | A fly, liner, pop, or foul fly held (no bobble) before it touches the ground or wall |
| Force | A fielder holding the ball touches a bag that a forced runner has not yet reached, or tags the forced runner |
| Tag | A fielder holding the ball touches a runner who is not on a bag (or who is off the bag they must return to) |
| Throw-out at first | The force at first: ball in the glove on the bag before the batter's foot |

Everything else (caught stealing, picked off, doubled off, appeal) is a tag or a force. ✅ P5: these five are the only paths to `Outs++` (`Match.RecordOut` through `RetireLiveRunner`; the recorders are `RecordCatchOut`, `TryForce`, `ApplyThrow`, `TryTag`, and the close-play verdict). `Retire`'s result gates every caption and flag: a retire that fails narrates nothing. ✅ P6: caught stealing and picked off are the same tags, made on a live runner play (§11.3, §11.4); the old steal race is gone.

### 10.2 Arrival

`ballArrival(bag)` = release time + `throwSec` (§8.5), then the receiver must be inside 6 ft of the bag (cover). If nobody covers, the ball skips past: live. `runnerArrival(bag)` = from the runner's position and speed. Out iff `ballArrival + 0` < `runnerArrival` **and** the receiver is on the bag (force) or tags (unforced). Tie → runner. ✅ P5 (`LivePlaySystem.OnThrowArrived` / `ArrivalVerdict` from the bodies: a forced body short of the bag is out, a body on it beat the throw, an unforced body inside the reach is tagged, one further out is waited for and the tag rule runs frame by frame). The scripted harness command `ThrowArrived` (tests only) still states the arrival it wants; no runtime path uses it.

### 10.3 Tag geometry

- A tag is a glove with the ball inside **reach** of the runner: `TagReachFt` = 4 ft (+2 with Lick / Grow, `fielding.abilities.tagReachBonusFt`); the runner is not touching a bag (`TagSafeRadiusFt` **1.5**). Home plate is a bag for a runner coming home, **not** for the batter leaving the box. ✅ P5 (`InPlay.TagReachFt`, `InPlay.Touches(fielder:)`). The safe radius moved from the 3.5 of the first draft to 1.5: the slide takes 2 ft off the reach (§9.4), and a safe radius wider than the slid reach would make every slide untaggable; the table validates `tagSafeRadiusFt ≤ tagReachFt − slideReachCutFt`. The tag is judged **through the frame** (`InPlay.TagWithinFrame`): a body that crossed the reach on its way to the bag was tagged before it touched, however short the step, so a 60 Hz body cannot skip the four-foot window. A tag records the bag the glove stands on (or 0 in the field).
- Human: have the ball, touch the runner (walk into them). South is not required. ✅
- The runner on a bag is safe. A runner who overran second or third is off the bag and taggable. Overrun first is protected (§9.4). ✅ P5 (`Runner.OverrunProtected`; nobody overruns second or third — a body stops on the bag it is stopping at, and one that rounds is off it and live).
- A glove that walks to a bag a body is bound for **waits there** and the tag at the bag decides (the catcher at the plate on a steal of home, the first baseman on a pickoff throw); it leaves once nobody is bound there. ✅ P6 (`LivePlaySystem.PlayStandsAt`).

### 10.4 Double plays (ground ball)

The classic turn: force at second, throw to first. Each leg is its own throw with its own arrival test; **one press is one throw**. The human throws both (how-to-play: "you throw both"). CPU chains by §8.8 rule 1.

| Scenario | Ball to | First throw | Second throw | Notes | Id |
| --- | --- | --- | --- | --- | --- |
| Runner on 1st, grounder to SS | SS | 2B (2B covers) | 1B | 6-4-3 | S-40 |
| Runner on 1st, grounder to 2B | 2B | 2B (SS covers) | 1B | 4-6-3; if 2B is within 8 ft of the bag they step on it themselves (unassisted force), then throw | S-41 |
| Runner on 1st, grounder to 3B | 3B | 2B | 1B | 5-4-3 | S-42 |
| Runner on 1st, grounder to 1B near the bag | 1B | step on 1B (batter out) **then** 2B — now a **tag** because the force is removed | — | 3-6 tag; the runner may stop and return, and a rundown can start (§9.7) | S-43 |
| Runner on 1st, grounder to 1B away from the bag | 1B | 2B | 1B (P or 2B covers first) | 3-6-1 / 3-6-3 | S-44 |
| Runner on 1st, comebacker | P | 2B | 1B | 1-6-3 / 1-4-3 | S-45 |
| Runners on 1st and 2nd, grounder to 3B near the bag | 3B | step on 3B | 1B (or 2B if the batter is slow — the CPU picks the best makeable margin) | 5-3 / 5-4-3 around-the-horn | S-46 |
| Bases loaded, grounder to an infielder inside 60 ft | fielder | home (force) | 1B | 2-3 / 6-2-3; the catcher is the cover at home | S-47 |
| Bases loaded, grounder to 1B on the bag | 1B | step on 1B (batter out) | home — now a **tag** at the plate | 3-2 tag; the runner from 3rd can hold | S-48 |
| Runner on 1st, bunt popped up | C / P | catch | 1B (double off) | S-49 |
| Runner on 1st, 2 outs | any | the **first makeable out** ends the inning; the CPU prefers the shorter throw | — | No DP attempt with 2 outs | S-50 |

Rules that fall out of geometry, and must not be tabled:

- The second throw is only an out if it beats the batter; a slow turn is a **fielder's choice** (one out, batter safe at first). Caption and stamp FIELDER'S CHOICE (✅ P8: `PlayStamp.FieldersChoice` from `PlayOutcome.FieldersChoice`; the first draft stamped OUT). ✅ P5 (the synthetic DP went with P3's Complete; `ApplyThrow` records the out first and narrates only what was recorded; `PlayOutcome.FieldersChoice` and the caption "Fielder's choice." come from the same typed facts; a chain of outs captions "Double play." / "Triple play!" ahead of its last decision; `PlayStamp.Label(PlayEvent)` reads the typed outs). One press is one throw on the human seat; the CPU steps on a force bag inside `fielding.throw.unassistedFt` instead of throwing (S-41, S-46).
- The force at second is removed the moment the batter is retired at first; any later play on that runner is a tag.
- The receiver must be on the bag: if the cover has not arrived (slow SS), the ball waits in the air — the out is late. That is how a **fast runner beats a DP**.
- A **neighborhood play** does not exist; the foot must be on the bag (6 ft occupancy radius is the arcade tolerance).
- Mini diamond updates as each out lands. ✅

### 10.5 Doubled off and the tag-up DP

- Liner or fly caught with a runner off the bag: the fielder throws to (or steps on) that bag; if the ball arrives before the runner returns, the runner is out. This is an **appeal-less force back**. Works at every bag. S-51..S-53.
- Fly ball, runner tags and goes, throw beats them: **tag** at the next bag (never a force). S-54 (sac fly thrown out at home).
- Pop-up dropped on purpose with runners on: no infield fly rule; forced runners must go. CPU runners stay on the bag, so the drop is a force at the lead bag only. S-55.

✅ P5: the CPU reads the doubled-off race ahead of its table (`RunnerSystem.ReturnSec` against the throw to the start bag, or a walk onto it inside `fielding.throw.unassistedFt`); a human sends a runner into the doubled-off risk with the stick on that runner (LB on a ball in the air is tag-and-go, §9.5). A body owing a retouch is not settled: Time waits for it (§10.6).

### 10.6 When the play ends (Time)

`Time` is true when: three outs; **or** the ball is held by a fielder on the infield (inside the dirt / grass lip, `flight.classes.infieldLipFt` — the 100 ft of the first draft put 2B and SS on the grass) and not thrown, **and** every live runner is on a bag or out, for `TimeOnBagSec` (1.0). Nobody left to play on (every runner out or home) is Time wherever the ball is, and so is a ball lying at rest that nobody picked up once every body has settled. A home run ends at the crossing plus the trot. ✅ P3 (`InPlay.Time` over the bodies; a CPU outfielder holding a ball with everyone settled throws it in, §8.8 rule 5).

At `Complete`: runs = runners who crossed home before the third out (with the §1 force exception), outs already recorded, bags = where each runner stands. **No table placement.** ✅ P3 (`Match.SettleRunners`; `AdvanceHit`, `AdvanceTagUp`, `OccupiedDestBag`, `BatterDestBag`, and the `goto case Single` reclassifications are gone; walks, hits by pitch, homers, and ground-rule doubles are the only placements by rule). The stamp reads the bodies: an out on the play stamps OUT (the batter safe at first behind it stamps FIELDER'S CHOICE, §10.4); no out, the batter's bag names the hit. Every CPU ball — `cli match`, `AutoPlay`, a cold `FinishAtBat` — runs through the same live ball (`Match.RunLive`), so there is one path.

### 10.7 Triple play

Three outs on one live ball by the rules above (liner, double off, double off; or force, force, tag). Stamp TRIPLE PLAY. ✅ P5: reachable through the forces alone (runners on first and second, a hard grounder beside third: step on third, the force at second, the throw to first — `OutsScenarioTests`), stamped from the typed outs, captioned "Triple play!".

---

## 11. Steals and the catcher

### 11.1 Arming

- Select a runner (D-pad) and press L3 / Z — or push the stick toward the next bag — during SET or WINDUP: that runner's steal is armed (tell: a crouch and a purple STEAL pip). ✅ both arms (P0). All-return (RB) cancels before the windup. **Any number of runners may be armed** — a double steal is two arms. ✅ P6 (`Match.StartStealAt` arms one body and leaves the rest; stick back on a runner takes only that arm off; `Runner.StealArm` records SET / windup / perfect).
- Targets: 1st→2nd, 2nd→3rd, **3rd→home** (D10). ✅ P6 (`Baserunning.StealTarget(3) == 4`).
- A steal into an occupied bag is not offered unless that runner is also armed (double steal). ✅ P6 (`Baserunning.CanSteal(…, nextRunnerArmed)`); an arm whose bag holds an unarmed body drops at the pitch (`Match.BreakArmedRunners`).
- A perfect arm's pip shows once the windup starts, never in SET: the CPU pitcher's pickoff read sees only SET arms (§4.5).

### 11.2 The jump (D2)

- Armed runners **break at release** (0.42 into the delivery) from the bag at `bagSec` speed, ×⅔ while the pitch is still in the air, full speed once it reaches the plate (reference). ✅ P6 (`StealBreak`, `running.steal.airSpeedMul` 0.6667): the break is a body on the pitch's own clock — when the ball is dead in the catcher's glove (or live off the bat) every runner who broke stands `StealBreak.HeadStartFt` up the path (`airSpeedMul × speed × (AirSeconds + perfectEarlySec)`), and runs at full speed from there. There is no time credit and nothing rolls.
- **Perfect steal**: armed inside the first 0.25 s of the windup → breaks 0.4 s before release; the pip turns gold. Arming *before* the windup (in SET) is the ordinary steal. Arming late in the windup still breaks at release; after release it is too late (nothing arms). ✅ P6 (`StealBreak.ArmFor(windupSec)`, `running.steal.perfectWindowSec` 0.25, `perfectEarlySec` 0.4; the client passes the windup clock, headless callers arm in SET).
- **Early break risk (D3)**: a runner armed in SET breaks on the pitcher's **first motion** — if that motion is a pickoff, they are caught between bags (§11.4). This is the pitcher's read on a runner who armed too early: the CPU pitcher's pickoff rate rises when it sees the pip. ✅ P6 (`StealBreak.BreaksOnPickoff`: SET arms only).
- A runner returns on a foul (dead) and on a home run (trot). On a ball in play the steal simply becomes running. ✅ P6 (S-64: the body starts the live ball on the path with its head start, phase Stealing, forced or not).
- The race by the numbers: a Run-9 body armed perfect on a fastball is at second in ≈ 1.65 s from the catch; the CPU catcher's release (≈ 0.35 s) plus the gun from the plate (0.22 + 130 ft over the arm) is ≈ 1.8–1.9 s for a Field-5 arm and ≈ 1.4 s for a Field-8 arm released at once. Ordinary steals belong to burners; a Run ≤ 5 body from the bag is tagged by ≈ 0.5 s (S-60, S-62, S-70).

### 11.3 Catcher throw play (after a take or a miss)

- The ball is in the catcher's glove at plate crossing + 0.05. The defense (human catcher seat, or CPU) arms a bag and presses South; the throw is a normal throw (§8.5) from the plate with the catcher's arm. Release delay: human = press time; CPU = `0.42 − Field × 0.014 ± 0.10`. ✅ P6: the steal is a **live runner play** (`LivePlaySystem.RunnerPlay`, `Match.RunStealPlay` headless, Unity `Phase.StealThrow` ticking the same commands): the catcher holds the ball `fielding.catcher.behindPlateFt` (3) behind the plate, the cover of every bag a body is bound for is on it (the middle infielder breaks to the bag on the pitch), the throw is `BeginThrowToBag` with the pair's chemistry (thrower and cover, never the runner), the human presses (a South with nothing armed throws to the lead body's bag), the CPU's first decision runs at its release (`StealThrow.CpuReleaseSec`, one seeded stream) through the §8.8 table.
- The out is a **tag** at the bag: ball arrival + receiver on the bag + tag reach vs runner arrival (§10.3). With two runners stealing the catcher picks one (human) or the lead runner unless the trailing runner's margin is ≥ 0.3 better (CPU). ✅ P6 (`ArrivalVerdict` / `TryTag` at the bag, the mash at third or home inside the margin (§9.6); `running.steal.cpuTrailPreferSec`).
- On a **walk** or **HBP** the runner from 1st is entitled to 2nd — no play on them; other runners' steals are live. ✅ P6 (`PlaceByWalk` seats the forced bodies; an unforced body that broke still runs its play, S-63).
- With a runner on third watching a steal of second the free middle infielder cuts `running.steal.cutInFrontFt` (25) in front of the bag on the throw line: the cutoff verb (LB with nothing armed) sends the throw to them and they hold for the play at the plate (S-66); the cover at second can return it home too.
- Stamp STOLEN BASE / CAUGHT STEALING. ✅ A strikeout with the runner caught is two outs on one pitch: DOUBLE PLAY (S-62). Time seats the bodies and completes the pitch's event (`Match.FinishRunnerPlay`): a runner out is CAUGHT STEALING, a runner who took a bag is a STOLEN BASE, a body back on its bag leaves the pitch as it was; a run that crossed counts by §1.

### 11.4 Pickoff play (SET)

- Pitcher throws to the armed bag. A runner on the bag is safe; no race (D3). ✅ P6
- A runner who broke on the pickoff motion (armed in SET) is between bags: the receiver throws ahead or chases; the runner may keep going or come back (stick). It resolves as a tag at either bag or a rundown (§9.7). ✅ P6 (`Match.BeginPickoff` → `LivePlayCommand.BeginPickoff`; the same live ball as §11.3 with the pitcher's throw already in the air; `RunnerPlayResult.PickedOff`, stamp PICKED OFF).
- Pickoff at 2nd: the live cover map's middle infielder (`InPlay.CoverMap`). At 3rd: 3B. At 1st: 1B. Home: the catcher (the plate is theirs). ✅ P6 (`StealThrow.CoverPos` covers every bag).

### 11.5 Scenarios

| Scenario | Rule | Id |
| --- | --- | --- |
| Straight steal of 2nd, take | Catcher throw to 2B, tag | S-60 ✅ |
| Steal of 2nd, swing and miss | Same; the batter's body does not block | S-61 ✅ |
| Strikeout + caught stealing | Two outs on one pitch; stamp DOUBLE PLAY | S-62 ✅ |
| Steal of 2nd, ball four | Runner entitled to 2B; no play (an unforced runner's steal is live) | S-63 ✅ |
| Steal of 2nd, ball in play | Steal becomes running; forced anyway | S-64 ✅ |
| Double steal 1st & 2nd | Catcher picks; lead runner default | S-65 ✅ |
| Double steal 1st & 3rd (delayed) | Runner on 1st goes; catcher throws to 2B → runner on 3rd may break for home when the throw passes the mound (stick); the SS/2B can cut the throw (cutoff verb, `running.steal.cutInFrontFt`) or the cover at 2B returns it home | S-66 ✅ |
| Steal of home | Catcher receives, walks to the plate, tags; the runner needs a perfect steal and a slow pitch (changeup) to have a chance | S-67 ✅ |
| Pickoff at 1st, runner not armed | Back, no play, no stamp | S-68 ✅ |
| Pickoff at 1st, runner armed in SET | Runner broke on the motion; tag at 1B or 2B / rundown by geometry | S-69 ✅ |
| Perfect steal (armed 0.2 s into the windup), average catcher | Runner breaks 0.4 s early; safe at 2B against a Field-5 catcher, out against the roster's best arm (Field 8; the row's Field 9 is not on any roster) with a Nice release | S-70 ✅ |
| Pickoff throw sails (bad chem) | Ball live; runner advances (ERROR) | S-71 ✅ |
| CPU never picks off a runner at random | | S-72 ✅ |

✅ P6 (`StealScenarioTests`): every row runs headlessly on the seats it names; the CPU catcher and the human catcher drive the same live ball.

### 11.6 CPU steal decisions

At SET, for each runner with an open next bag: `P(steal) = base(Run) × situation`, base = 0 for Run ≤ 4, 0.06 at Run 6, 0.16 at Run 8, 0.25 at Run 10; ×1.5 with 2 outs, ×0.5 with the captain slugger up, ×0 with a runner already armed ahead of them (no double steal into a body), ×0 when trailing by ≥ 5. Perfect-steal chance 0 / 20 / 40 / 50% by difficulty (reference); otherwise the CPU arms in SET and is exposed to the pickoff. Evaluated once per at-bat (not per pitch), as a runner-AI event — not inside `CpuSwing`. ✅ P6 (`RunnerAi.StealPlan`, `running.cpu.stealMinRun / stealBaseRun6 / 8 / 10 / stealTwoOutsMul / stealCaptainUpMul / stealTrailingRuns`, `cpu.*.perfectStealChance`; `Match.CpuArmSteal` runs it once per at-bat on the one seeded stream; the old `stealChance` roll is gone).

---

## 12. Chemistry, stars, items in play

Mechanics are in systems.md. The play contract:

- **Chemistry throw** modifies throw speed and accuracy (§8.5). That is its only in-play effect. A bad throw *looks* bad before it lands (muddy trail) so the room can yell.
- **Buddy Jump / Buddy Throw** are verbs with windows (§8.4, §8.7), not rolls.
- **Star meter**: gains are events (`data/rules/stars.json` `gains`, meter max 5): single 0.4, extra-base hit 0.8, home run 1, strikeout 0.8, out 0.35 / live out 0.4, stolen base 0.35, **double play 1**, **robbed homer 1**, billboard 1. Spend `costs.own` 1 (`costs.guestCaptain` 2; a missed star swing costs 1 for everyone). ✅ P7: every gain and cost is read from the table; the double play (two outs on one live ball) and the robbed homer (a leap that takes a ball clearing the fence, §8.4) are events of their own.
- **MVP** (`stars.json` `mvp`, `Match.Mvp`): a **walk-off hit** names its hitter ahead of the points (the last event of the game, a hit in the home half that turned a tie or a deficit into the lead); else the most points on either roster — HR 10, winning pitcher 5 (the arm of the side that last took the lead, credited once the game is over), go-ahead RBI 5 (on top of the RBI), robbed homer / buddy jump 5, K 3, RBI 3, hit / walk / HBP / SB 1, close play won 2 (the runner called safe or the glove that tagged, §9.6), item that mattered 2 (it landed and the batter reached), **putout 1** (the glove that forced, tagged, or caught a runner live, the catcher on a caught stealing). The "took over the diamond" / "kept the line moving" lines read `mvp.tookOverAt` 8 / `mvp.keptMovingAt` 4. ✅ P7 (no literal in `Match.cs`; a run scored is not a point of its own).
- **Items** after contact (banana / rocket / POW): each is a *field* effect with geometry and seconds (`batting.items`), thrown at a body and landing after `flySec` 0.9 s of play time — a **banana is a peel on the grass** at the aimed body's feet where it lands, lying there `peelSec` 6 s, and *any* body inside `peelRadiusFt` 5 of it slips for `slipSec` 0.8 (the aimed body first, since it is standing on it); a **rocket dazes the body it was aimed at** for `dazeSec` 0.8 when it lands (North smashes it in the air, the only defense); a **POW keeps every ball on the dirt** hopping, unscoopable, for `powHopSec` 0.8. They add time; the geometry then decides the play. A body foiled while holding the ball drops it loose. No roll decides an item: the **CPU offense throws its offered item when it would matter** — when the batter's slack at first against the glove's throw from the landing is under `cpuThrowMarginSec` 0.3 (the §9.9 read) — a POW with a runner on and the ball on the dirt, a peel at the glove going for a ball on the dirt, a rocket at the body under a ball in the air (`ErrorItems.CpuPick`). ✅ P7 (`LivePlaySystem.LandItem` / `TickItems`; the 40% auto-throw roll and the rocket's daze roll are gone).

---

## 13. Star skills — the two-second rule

A skill bends one rule for ≤ 2 s and then baseball resumes. The bend is one of: the ball's path (element / break / decoy), the batter's window (status), the fielder's body (status / payload), or the terrain (area). Values live in `data/abilities/star-skills.json` and are **read at runtime** (`StarSkillTable`, `ContentCatalog.StarSkills`; `speedMul`, `staminaCost`, `batterWindowMul`, `exitVeloMul`, `launchDeg`, validated by `ContentDataValidator`), not re-typed. ✅ P1: the C# copies are deleted; `staff-swing` is 1.08 as the JSON says.

| Skill | Bend | Then |
| --- | --- | --- |
| Heatball | +15% speed, the catcher's glove smokes; a caught fly from a heat-swing has a burn-hop (drop chance 35% for 2 s) | baseball |
| Charmball | Batter window ×0.75 | |
| Prismball | Late break, ghost images | |
| Phonyball | Decoy path; 40% of non-perfect contact whiffs | |
| Caskball | Slow, knockback on the catch (0.55 s) | |
| Skullball | +20% speed, window ×0.80 | |
| Fogball | Window ×0.78 | |
| Heat / Furnace swing | Exit ×1.15 / ×1.25; a burn patch / lava strip where it lands slows the fielder ×0.45 for 2 s | |
| Heart swing | The nearest fielder pauses 0.8 s | |
| Shell / Staff swing | Infield chaos: the first bounce is randomized ±30° | |
| Phony swing | Decoy ball; the real one shows at the apex | |
| Cask swing | Fragments: two decoy balls fall with it | |
| Role players | Star fastball / change / breaker; star grounder / fly / line | |

Star skills cannot produce a free home run; the exit multipliers are capped so a Perfect charged star swing at Bat 10 clears Harbor's 400 only with a Perfect. ✅ by tuning.

---

## 14. Parks in play

Harbor has no hazard. Others tick hazard ids (`ParkHazards`): freeze volumes (×0.45 speed 1.2 s), warp cans (grounder exits another can), chompers (night: a fly into the mouth is a fly out), lava/fire (slow), tilt (grounders drain to a line), blackout (window ×0.85). Each is a *field* rule with geometry, documented in parks.md. The contact-window park rule is a park data field, `nightContactWindowMul` in `data/parks/*.json` (Crystal Rink 0.85, every other park 1; validated in (0, 1]), read by `ParkHazards.ContactWindowMul` — never a park-id string in code. ✅ P7.

---

## 15. Presentation contract per play

For each play class the camera, the stamp, and the hold are data (`data/feel/shots.json`, `table.json`). The sim emits typed `PlayEvent`s; Unity may not infer the play from caption text. ✅ P8 (stamps): every stamp in the table is `PlayStamp.Label(PlayEvent)` over the typed outcome — the outs made, `DefensiveFeat` (buddy jump, the wall robs, a plain JUMP, a DIVE, set at the catch), `Error`, `FieldersChoice`, `RunnerResult` — and the client calls nothing else; the contact word is `PlayStamp.ContactTell` over the typed zone and the release tell is MAX / Nice! alone (#578). ✅ P0: `PlayOutcome` carries the outs made (type, bag, runner, fielder), every runner placement, the batter's bag, error, and fielder's choice; `InPlay.ThrowToBag` decides a `ThrowVerdict` and captions are narrated from it last (`InPlay.Narrate`). No rule reads caption text.

✅ P8 (cameras): the in-play beat is `PlayCamera.LiveBeat` over typed live state — the runner play sits on its bag, a home run smashes at the crack, every other hit holds the SET shot for `contactCutSeconds` (0.42, `table.json`, the §8.2 cut), then the close play, the throw, the rundown, and the class read off the typed hit (`AtBatResult.Class`) — and `PlayCamera.LiveFraming` translates the named shot onto the bag, the batter, or the dirt under the ball (`CameraDirector.Live`; `HoldInPlay` / `ThrowTo` are gone, the client owns no Vector3). ✅ P8 (bodies): `PlayOutcome.Bodies` is every glove and live runner where it stood at Time (`LivePlaySystem.BodiesNow`, read before the field resets); the result beat draws those, so the catching fielder stays where the catch happened and a third out does not swap the defense on screen (#574).

| Class | Camera | Freeze | Stamp |
| --- | --- | --- | --- |
| Pitch / take / miss | `mound` (1P pitching) / `plate` | — | BALL / STRIKE / STRIKE OUT / WALK / HIT BY PITCH |
| Grounder | `diamond` follows the dirt after the contact cut | `solidFreeze` on the crack | OUT (DIVE) / FIELDER'S CHOICE / SINGLE / DOUBLE PLAY / TRIPLE PLAY / ERROR / BUNT |
| Liner / fly / wall | `diamond-fly` after the contact cut | `solidFreeze` | OUT (DIVE / JUMP / BUDDY JUMP) / SINGLE / DOUBLE / TRIPLE / DOUBLE PLAY / ERROR |
| Home run | `smash` on the batter at the crack for `smashHold`, then `diamond-fly` with the ball | `smashFreeze` + `smashHold` | HOME RUN / GRAND SLAM |
| Steal / pickoff | `throw` on the play's bag (`LivePlaySystem.RunnerPlayBag`) | — | STOLEN BASE / CAUGHT STEALING / PICKED OFF / ERROR; a strikeout plus a caught stealing is DOUBLE PLAY |
| Throw | `throw` on the bag it is going to; nothing cuts behind the thrower | — | (the arrival decides) |
| Close play | `tag` on the bag | — | SAFE (small, mid-play) / OUT |
| Rundown | `diamond` follows the ball between the bags | — | OUT / SAFE by the tag rule |
| Throw that sails | the follow stays on the loose ball | — | ERROR (small, mid-play at the sail); the play's stamp at Time |
| Star | skill VFX, scorebug mutes 2 s | — | — |

---

## 16. Data tables (rails)

Presentation stays in `data/feel/`. Rules live in `data/rules/` (✅ P0), loaded by `RulesTable` into `ContentCatalog.Rules` the way `FeelTable` is loaded: the JSON is the source of truth, the C# initializers are only the load fallback for a field a file does not name, and `ContentDataValidator` (so `cli art` and every `ContentCatalog.Load`) refuses a missing file, an unknown field, or a value outside its declared range (`[Positive]`, `[Chance]`, `[Signed]`). A test pins the JSON to the code defaults field for field. Every helper that reads a number takes the table it is handed (`Match.Rules`, `ContentCatalog.Rules`); a caller with no catalog falls back to the table found from the data root. **Any new rule number is a field here with a validator, never a C# literal.** Structural geometry stays constant: `Diamond`, the plate frame (`PitchFlight.PlateY`, `PlateScaleX/Y`), and the 45° foul line the wall and stands meshes share.

Files and the sections each owns (P0 moved the numbers that existed; later epics add fields, not literals):

| File | Sections |
| --- | --- |
| `pitching.json` | `speed` (base mph per shape, Pitch coefficient, charge mph, changeup charge, star ×), `release` (Nice! band and ×), `flight` (release hand, `AirSeconds` scale and clamps, break cap / ramp / damping / rate), `shapes` (fastball hump, changeup hang / dump / drop), `starShapes` (heat, prism, charm, phony, cask wobble), `stamina` (costs, TIRED threshold, swap restore, tired aim wobble), `cpu` (the CPU pitcher's rolls as shipped, with `pickoff`; §4.8 replaces them with a table in P1 part c). The rubber walk distance is geometry (`HomeSet.PitcherWalk`) |
| `batting.json` | `window` (slap / charge frames, per-contact, floor, square fraction, `leadSec` the square press before the ball's plate time, D13), `charge` (loft), `quality` (`slap` / `charge` exit columns by zone, energy ×), `exit`, `launch` (loft, height, stick, noise, topper and pop bands), `bunt` (exit, launch, spray, pop height), `spray` (zone spread, stick, timing), `foul` (sour pull past the chalk, until P2), `homer` (launch band), `cursor` (barrel half-axes, perfect and rim fractions, contact scale, charge narrowing), `hbp` (body radius, world feet), `star` (phonyball whiff, star launches), `buddiesOnBase` (charged power ×, slap widen ×), `pitchFactor` (charged pitch vs sour / perfect charge, high-Pitch damping), `items` (the item's flight, the peel's radius and life on the grass, the CPU's throw margin, the slip / daze / hop seconds — §12; no roll), `cpu` (the CPU batter's rolls as shipped; §5.9's tracking table lands in P1 part c) |
| `flight.json` | gravity, drag, `timeScale` / `linerTimeScale` / `dirtTimeScale` (the stretch per shape, §6.1), plate height, `windMul`, sample rate; `bounce`, `skid`, `roll`, `wall` (carom restitution / tangential), `landing` (the one landing guard), `classes` (the §6.2 table: topper / grounder / chopper / liner bands, the chopper's hop, the infield lip), `deadBall` (homer trot, foul flight hold). The `carry` hit bands are gone (P4): the bodies decide the bases |
| `fielding.json` | `chase` (the one glove speed, swap lock, the loose-ball scoop reach), `reaction` (the lockout per position, the CPU throw delay), `cover` (flat cover speed and start, D11; the cover radius a throw must land inside; the backup distance), `dash` (chase ×, buddy toss, kick, dive lunge, item smash), `catch` (radius, windows, reaches, ability windows, jump/dive arm times), `drops` (star effects only), `wallPlant`, `abilities` (catch, range, and throw bonuses; the Lick / Grow tag reach), `throw` (the one throw model: release, base speed, arm, lateral σ, the lob wait, the fly reach, the unassisted step-on distance), `overthrow` (how a missed throw rolls), `catcher` (where the catcher holds the ball behind the plate, the CPU release), `chem` (good speed, the slant), `bobble`, `knockback`, `park` |
| `running.json` | `bagSec` (the one speed: base, per Run, clamps, dash, the batter's start delay, the no-pass gap), `bags` (occupy radius, tag reach, tag-safe radius, `timeOnBagSec`, the slide, the run-through at first), `close` (the close-play margin, icon delay, CPU reaction), `rundown` (range, the throw distance, the lazy lob), `steal` (the jump, D2: the air speed fraction, the perfect window and its early break; the CPU catcher's trailing-runner preference; the cut in front of second), `stick`, `dash` (mash per press), `cpu` (the §9.9 thresholds; the §11.6 steal table; the pickoff read's pip multiplier and the corners chance) |
| `stars.json` | `meterMax`, `gains` per event (the double play and the robbed homer included), `costs`, `starting` (chemistry scores and the starting-meter thresholds), `mvp` (the §12 point table and the MVP line thresholds) |
| `cpu.json` | `level` (the default rung) and the `easy` / `normal` / `hard` rungs: a pad's swing window × (`humanWindowMul` 1.3 / 1.0 / 0.9, §5.3, shown on the title), timing-σ ×, reaction × (CPU batter σ and tracking, close-play reaction, catcher release, the fielder's throw delay), mistrack ×, makeable margin (§8.8), perfect-steal chance and pickoff chance (§11.6, §4.5), `runnerMarginSec` (§9.9). Normal is ×1 everywhere so the tables read as written. A match plays at one rung (`Match.Difficulty`, `RulesTable.AtLevel`): the title picks it next to the innings (X / LB), `cli match --difficulty`, every other table shared. ✅ P7 |
| `match.json` | `extraInningsCap`, `mercy` (`runs`, `fromInning`, `minScheduledInnings`) — §1 |

Feel values that were dead or shadowed (`throwEase`, `chargeDecay`, `inPlayCommitSeconds`, `runHz`) are removed from `table.json` and `FeelTable` (✅ P0), and `cpuVsHumanTake` / `cpuVsHumanMiss` with the forced-miss clamp (✅ P1); `fieldAssistStick` is the one stick-take threshold and `FieldAssist` reads it (the duplicate `FieldAssist.StickTake` constant is gone).

---

## Appendix A — Gap audit (code at `f09cad1`)

Grouped by the epic that fixes them (roadmap.md, Phase P). Line numbers from the two code maps taken on 2026-09-12.

### A.1 Pitch and swing contract (P1)

| # | Where | What | Spec |
| --- | --- | --- | --- |
| 1 | `AtBatFeel.cs:127-152`, `AtBatDirector.cs:109` | Sweet-spot oval has a fixed Y covering only [1.83, 2.97] of a [1.45, 3.65] zone; 3-step overlap — ✅ P1a (`SweetSpot` in world feet, five zones, S-05 … S-12) | §5.2 |
| 2 | `AtBatDirector.cs:222-224` | Human pitch type hard-coded fastball, `AimY` 0; `curve`/`slider` unreachable — ✅ P1b (normal / charge / changeup / break / Nice! from the pad; types retired) | §4.1, §4.3 |
| 3 | `AtBatDirector.cs:224`, `Match.cs:667`, `PitchFlight.cs:45` | Rubber walk moves the crossing 1.35× for a human, 0.35× for CPU — ✅ P1b (`HomeSet.PitcherWalk` once, both seats) | §4.2 |
| 4 | `PitchFlight.cs:58-63` | Full break moves the crossing 1.8 ft (zone half-width 0.92) — ✅ P1b (`breakMaxFt` 0.46, `BreakStep`) | §4.2 |
| 5 | `AtBatDirector.cs:245-246, 311-314` | CPU batter judges at launch, before in-flight steering — ✅ P1b (decides at `CpuDecisionTime` from the live trajectory, S-04) | §3 |
| 6 | `AtBatResolver.cs:9-11, 48-51` | Perfect band 1.0 frame, no window floor — ✅ P1a (`ContactWindowFrames`, S-08 … S-10) | §5.3 |
| 7 | `AtBatResolver.cs:54-55` | Off-center Perfect demotes to Cheap — ✅ P1a (one tier at the rim, never two) | §5.3 |
| 8 | `AtBatResolver.cs:73-75` | Charge ×1.12 max; Charge Bat pinned to ×1.10 — ✅ P1a (`quality.charge` column, S-11, S-30) | §5.1, §5.5 |
| 9 | `ChemistryTable.cs:96-105` | Buddies-on-base × applies to every contact — ✅ P1a (charged swings only; slap widen) | §5.5 |
| 10 | `AtBatResolver.cs:13-23, 151-157` | Foul = spray > 45°; `CheapFoulPull` 40% teleport | §5.6 |
| 11 | `AtBatResolver.cs:119-122` vs `Fielding.cs:340-346` | Two homer launch bands | §5.6 |
| 12 | `AtBatResolver.cs:241-256`, `Match.cs:811-819` | HBP unreachable; `FinishHitByPitch` ignores `inZone` — ✅ P1a body geometry in world feet (S-16, S-17); the rubber-walk reach is P1b | §4.6 |
| 13 | `AtBatFeel.cs` vs `HomeSet.BatterWalk` | Cursor moves 1.85 ft/unit, body 2.4 ft/unit — ✅ P1a (both `HomeSet.BatterWalk`) | §4.6 |
| 14 | `AtBatFeel.cs:217-221`, `AtBatResolver.cs:52` | Bunt judged at the press; bypasses the oval — ✅ P1a (S-19) | §5.8 |
| 15 | `Match.cs:773-779` | Foul bunt with 2 strikes not a K — ✅ P1a (S-18); `FinishFoul` skips `AfterPitch` — stays until P6 retires the pickoff roll | §5.8, §5.6 |
| 16 | `AtBatDirector.cs:171, 173-174, 235, 288` | Stick-down both aims launch and resets the box; SET press dropped / −65-frame miss — ✅ P1a (S-14, S-15; Down resets in SET only) | §5.4, §3 |
| 17 | `Match.cs:558, 574` | Box walk reset every pitch — ✅ P1a (persists across the at-bat, S-16) | §3 |
| 18 | `Match.cs:38-39, 87, 205, 602-622, 654, 658, 1289` | Team stamina, flat costs, threshold ×4, swap +35 — ✅ P1c (per-pitcher pools, table costs, JSON star cost, swap trades gloves, S-25, S-26) | §4.7 |
| 19 | `Match.cs:648-668` | CPU pitcher aims center, nested type rolls, dead `TimingErrorFrames` — ✅ P1c (location-by-count table, S-27; the field is removed) | §4.8 |
| 20 | `Match.cs:672-676, 698-726` | `CpuSwing` arms steals; forced \|err\| ≥ 3.2 vs a human — ✅ P1 (`CpuSwing` is pure, S-28; the steal roll is `CpuArmSteal`, a SET verb, until P6 #568 moves it into the runner AI) | §5.9, §11.6 |
| 21 | `AtBatResolver.cs:232, 264-281`; `Models.cs:105, 123, 139` | Discarded `pitchStat`; 1.12 divide-out; dead `ChargePitch`, `Strike`, `TimingErrorFrames` — ✅ P1c (the divide-out and the pitch's timing field are gone; `AtBatInput.ChargePitch` is live in the pitch factor) | cleanup |
| 22 | `Fielding.cs:472-476` | Park id string special-cased for the window — ✅ P7 (`Park.NightContactWindowMul`, a park data field) | §14 |
| 23 | `star-skills.json` vs `FieldAbilities.cs:117-152`, `AtBatResolver.cs:220-226`, `Match.cs:1289` | JSON dead; `staff-swing` 1.08 vs 1.10; `staminaCost` ignored — ✅ P1c (`StarSkillTable`, copies deleted) | §13 |

### A.2 Flight and field (P2)

| # | Where | What | Spec |
| --- | --- | --- | --- |
| 24 | `BallFlight.cs:34` | Scalar downrange wind | §6.1 — ✅ P2 |
| 25 | `BallFlight.cs` | No fence, wall, or foul line in the trajectory | §6.1, §7.9 — ✅ P2 (`FieldBounds.Of`, `BattedBall`) |
| 26 | `BallFlight.cs:68, 116-119` | Landing guards in two time bases | §6.1 — ✅ P2 |
| 27 | `Fielding.cs:327-346` | Three separate class bands | §6.2 — ✅ P2 (`BattedBallClass`) |
| 28 | `Fielding.cs:420-432` | Positions by roster order | §8.1 — ✅ P2 (`Team.Gloves`) |

### A.3 Runner model (P3)

| # | Where | What | Spec |
| --- | --- | --- | --- |
| 29 | `Models.cs:276-330`, `Match.cs:28-33` | No runner position/velocity/batter-runner | §9.1 — ✅ P3 (`Runner`) |
| 30 | `Match.cs:1236-1254` | `SetBag` wipes runner state | §9.1 — ✅ P3 (`Runner.Seat` on the same object) |
| 31 | `InPlay.cs:53-58, 82-83, 499-500` | Two speed curves; all runners move at home-to-first speed | §9.1 — ✅ P3 (`running.bagSec`) |
| 32 | `InPlay.cs:372-393`, `Match.cs:1139-1223` | Advance by `PlayKind` table; runner on 3rd scores on any grounder; 2nd never scores on a single | §7, §9.9 — ✅ P3 (`RunnerAi`, `SettleRunners`) |
| 33 | `Match.cs:299-336` | Send/return only global | §9.3 — ✅ P3 |
| 34 | `Models.cs:320`, `ActorDirector.cs:360` | `Sliding` never consulted | §9.4 — ✅ P3 (`RunnerPhase.Sliding`, the reach cut) |
| 35 | `Match.cs:986` | Sac fly = carry > 230 literal, no throw | §7.8 — ✅ P3 |
| 36 | `Models.cs:276-330`, `Diamond.cs:46-52`, `Baserunning.cs:71-77`, `ActorDirector.cs:411-453` | Lead-off system to retire (D1) | §9.2 — ✅ P3 |

P3 also closed A.4 #40's fourth clock (the flat `fielding.throw.flight*` flight the client played): the live throw flies on `InPlay.ThrowSec`, because bodies now race it. The `arm` term of §8.5 and the resolver's roll (#37, #38) stay P4's.

### A.4 Fielding decides by geometry (P4) — ✅ closed by P4 (#566)

| # | Where | What | Spec |
| --- | --- | --- | --- |
| 37 | `Fielding.cs:146-161` | Infield out/hit is a stat roll — ✅ gone; `FieldingResolver.Resolve` names only what the flight decides alone or `PlayKind.InPlay` | §7.1, §8.8 |
| 38 | `InPlayDirector.cs:371-403` | Glove force-fed at hang with no distance check — ✅ the CPU catch is `FlyCatch.AutoCatch` at the radius in the window | §8.3 |
| 39 | `Fielding.cs:113-116, 130-133, 152-160` | Drop/bobble converts out→single as a caption — ✅ a bobble is a loose ball; drops are star effects only | §8.6 |
| 40 | `InPlay.cs:63-67`, `StealThrow.cs:42-53`, `InPlayDirector.cs:745, 862-887` | Four throw-speed formulas; verdict ignores flight time — ✅ one clock, the catcher's gun included | §8.5 |
| 41 | `ChemistryTable.cs:85-94` | 25% error roll; `LateralFt` never read — ✅ the slant is a lateral miss the receiver's reach judges | §8.5 |
| 42 | `InPlayDirector.cs:182-192` vs `Fielding.cs:312-313` | Two glove speeds — ✅ one | §8.1 |
| 43 | `InPlayDirector.cs:340-355` | Cover speed is a Unity literal — ✅ `fielding.cover` with the start delay | §8.7 |
| 44 | `Fielding.cs:412-418` | Cutoff hard-coded SS→2B — ✅ `InPlay.CutoffFor` by the line | §8.7 |
| 45 | `FieldAssist.cs:59-63`, `InPlayDirector.cs:749-768, 806-814` | Thrower teleports to the bag — ✅ the body stays; the YOU ring hands to the receiver | §8.5 |
| 46 | `MatchDirector.cs:762-796` | Unity re-rolls the bobble with an ad-hoc seed — ✅ closed by P0 | §8.6 |
| 47 | `InPlayDirector.cs:1225-1232` | `LiveKind` returns HR/3B/2B from carry mid-flight — ✅ `PlayKind.InPlay` until Complete | §7 |

P4 left to the client: the "E" tell on the thrower's body (`LiveEvent.ThrowSailed`) and the bobble puff (`LiveEvent.Bobble`) are cues the sim raises; `InPlayDirector` plays a dust puff and releases the ball for both (✅ P8: the sail also pops the small ERROR tell, §8.6). The lazy lob to a bag nobody can beat (§8.5, reference) is not modelled.

### A.5 Outs, double plays, Time (P5) — ✅ closed by P5 (#567)

| # | Where | What | Spec | Closed |
| --- | --- | --- | --- | --- |
| 48 | `Match.cs:870-879` | Synthetic DP from fabricated throws | §10.4 | P3 (Complete reads the bodies) |
| 49 | `LivePlaySystem.cs:344` | `Retire` result discarded after committing caption/flags | §10.4 | P5 (`ApplyThrow`, the close-play verdict) |
| 50 | `Match.cs:899-976` | Outcome inferred from bookkeeping deltas; three `goto case Single` | §10.6 | P3 |
| 51 | `Match.cs:908, 1030-1031` | Control flow on caption text | §15 | P3 |
| 52 | `Match.cs:44, 932`, `InPlayDirector.cs:1215` | `ClosePlaySafe` stale across plays | §9.6 | P3 / P5 (one verdict, written once) |
| 53 | `ClosePlay.cs`, `InPlayDirector.cs:1157-1223` | Mash on every unforced 3B/home throw | §9.6 | P5 (`ClosePlay.WithinMargin`) |
| 54 | `InPlay.cs:402, 416-417` | Tag reach 14 ft; home is a safe bag for the batter | §10.3 | P5 (4 ft + ability, judged through the frame; the batter's plate was closed in P3) |
| 55 | `LivePlaySystem.cs:307-313` | `ThrowArrived` sets HasBall/CatchMade unconditionally | §10.2 | scripted harness command only (tests); the live ball lands every throw through `OnThrowLanded` |
| 56 | `InPlayDirector.cs:150-152` | Rest fallback bypasses `CommitInPlay` | §10.6 | P3 (the sim commits every ball) |
| 57 | `Match.cs:556-559` vs `:575-584` | Walk-off only on the take path | §1 | P3 |
| 58 | `PlayStamp.cs:29` | Triple play is a label only | §10.7 | P5 |
| 59 | — | Extra innings, mercy, ground-rule double, foul fly catch, rundown missing (the ERROR stamp landed with P4) | §1, §7.11, §9.7 | P3 / P4 / P5 (rundown) |

### A.6 Steals and pickoffs (P6) — ✅ closed by P6

| # | Where | What | Spec | Closed |
| --- | --- | --- | --- | --- |
| 60 | `Match.cs:338-350`, `Baserunning.cs:25, 46-49` | Double steal impossible; no steal of home | §11.1 | `StartStealAt` arms one body; `StealTarget(3) == 4` |
| 61 | `StealThrow.cs:59-64`, `ActorDirector.cs:441-446` | Lead as a time credit; runner leaves at commit; no perfect steal | §11.2 | `StealBreak`: the break at release on the pitch's clock, the perfect arm off the windup clock; `RunnerRemainSec` is gone |
| 62 | `Match.cs:480-503, 1376-1393` | Pickoff arms a steal; a miss awards the base | §11.4 | `Match.BeginPickoff`: the pickoff is a live runner play; a sailed throw is live |
| 63 | `Match.cs:1421-1481` | Random pickoffs on every pitch (≤ 72%) | §4.5, D3 | gone with P3; the CPU read (`CpuPickoffBag`) is a rate, never an out |
| 64 | `Match.cs:1319-1323`, `StealThrow.cs:96-115` | Pickoff resolved with the steal race; real pickoff race unreachable | §11.4 | one live ball for both; `GunSteal` / `ResolveStealThrow` / `ApplySteal` deleted |
| 65 | `StealThrow.cs:23` | No cover at bags 1 and 4; chem computed defender-vs-runner | §11.4 | `StealThrow.CoverPos` covers every bag; `BeginThrowToBag` rolls the thrower–cover pair |

### A.7 Architecture (P0 / #512) — ✅ closed by P0

| # | Where | What | Now |
| --- | --- | --- | --- |
| 66 | `InPlayDirector.cs` (1249 lines) | Relay chain, close play, CPU catch timing, steal phase, glove speeds are Unity-side baseball | `LivePlaySystem.Field.cs` owns them; `InPlayDirector.cs` translates pads, mirrors state, plays cues. The bobble and the CPU catcher's release roll on `Match._rng` (S-92). |
| 67 | `data/feel/table.json` | `throwEase`, `chargeDecay`, `inPlayCommitSeconds` read only by tests; `runHz` shadowed by `Motion.RunHz` | Removed. |
| 68 | everywhere in §16 | ~150 rule constants in C# with no data hook | `data/rules/*.json` + `RulesTable` + validator; the rows above name their section. Rule numbers still *shaped* like the old code (the infield roll, the lead credit, the CPU rolls) are tabled as shipped and marked for their epic. |

The stale close-play verdict (A.5 #52) is gone with `Match.ClosePlaySafe` (the contest's verdict is applied to the body in the play and nothing else reads it); the wrong-clock arrival inputs (A.5 #55, §9.1) are P3's `RunnerSystem.ArrivalSec`. The mash's ±0.25 s gate (A.5 #53) stays P5's: today the contest runs whenever the ball is at third or home before an unforced runner still coming, and that runner's body waits for the verdict.

---

## Appendix B — Scenario matrix (acceptance)

Each scenario is a headless sim test: set the state, script the inputs (human seat commands or "CPU"), assert the outcome **and** the reason (which out type, which bag, which runner). A scenario is green only when it passes for the human seat *and* the CPU seat where both exist. Unity's job is to show it; the gate is `dotnet test`, then a sitting.

### B.1 Pitch and swing

| Id | Setup | Input | Expect |
| --- | --- | --- | --- |
| S-01 | Any | Take, pitch crosses inside the frame | Called strike |
| S-02 | Any | Take, pitch crosses outside the frame by 0.01 ft | Ball |
| S-03 | Any | Swing, miss, pitch outside | Strike |
| S-04 | Human pitcher steers full break | CPU batter | CPU decision uses the final crossing; a pitch steered out of the zone is a take at (100 − chase)% |
| S-05 | Charged fastball high in the zone (Y 3.4) | Perfect timing, box centered | Contact (not an automatic miss) with reduced quality |
| S-06 | Changeup dumps to Y 1.6 | Perfect timing | Contact, grounder bias |
| S-07 | Bat 5 slap, ball at the cursor center, press at plate − 0.10 s (err 0, D13) | | Perfect, straight to CF ± 8° |
| S-08 | Bat 5 slap, cursor center, press 4 frames before plate − 0.10 (inside the 9-frame window); press at plate − 0.17 / plate − 0.03 (±4.2 frames, the rim) | | Perfect, pulled ≈ 45°; the rim pulls / pushes at Nice |
| S-09 | Bat 5 slap, cursor center, err +5 frames; press at plate + 0.05 (+9) or plate − 0.02 (+4.8, past the 4.5-frame half) | | Miss |
| S-10 | Bat 1 charged + charmball | | Window ≥ 5 frames |
| S-11 | Bat 5 charge, ball 0.4 ft toward the bat tip from center | | Nice, charge exit ≈ ×1.12 |
| S-12 | Sour slap on a changeup | | Pop-up band forced |
| S-13 | Stick up at contact | | Launch lower than stick-center by ~12° |
| S-14 | Press during SET | | No swing; the batter is still able to swing on the pitch |
| S-15 | Press 0.1 s before release | | Early miss, strike |
| S-16 | Walk the box 1.0, take a pitch at body X | | HBP, first base, count unchanged |
| S-17 | Same, swing | | Strike, no HBP |
| S-18 | Bunt, 2 strikes, foul | | Strikeout |
| S-19 | Bunt, pitch high | | Bunt pop (oval applies) |
| S-20 | Ball at 44° spray, 400 ft | | Fair, home run |
| S-21 | Ball at 46° spray, 400 ft | | Foul |
| S-22 | Grounder at 40° that rolls foul before 1B untouched | | Foul |
| S-23 | Grounder at 40° that passes 1B fair then rolls foul | | Fair |
| S-24 | Foul pop, C under it | CPU | Out |
| S-25 | Pitcher stamina 20 | | −6 mph, wobble present; TIRED tell |
| S-26 | Swap pitcher | | New pitcher's own pool; old pitcher on the vacated glove |
| S-27 | CPU pitcher, 0-2 | 100 pitches | ≥ 30% cross outside the zone |
| S-28 | CPU batter vs human middle-middle normal pitch | 100 pitches | Perfect rate > 0; no forced-miss clamp |
| S-29 | 3-inning CPU-vs-CPU, 50 seeds across the captain pairs (a single pair can be a mismatch by design: Pitch 9 vs Bat 4) | `cli match` / `AutoPlayGame` | Mean runs per side 2–5; doubles < singles; HR ≤ 2 per game mean; strikeouts and walks both present. ✅ P7 (`AtBatScenarioTests.S29`, in CI; 2.5 / 2.7 runs, 4.7 singles, 2.5 doubles, 1.3 HR, 3.5 K, 3.0 BB per game on the shipped seeds). A blowout still happens on a mismatch pair (12 runs by one side in 50 games): the mercy rule does not run in a 3-inning game (§1) |
| S-30 | Charge Bat vs manual MAX | | Charge Bat ≥ manual MAX power, no window penalty |

### B.2 Grounders and fielding

| Id | Setup | Input | Expect |
| --- | --- | --- | --- |
| S-31 | Nobody on, routine grounder to SS, Run 5 batter | CPU | Force at 1B, out; throw arrival < runner arrival by geometry |
| S-32 | Same, Run 9 batter (the roster's fastest), SS Field 3 with no throw ability (every Field-2 glove carries Laser) | CPU | Infield single (arrival compare), no roll |
| S-33 | Same as S-31, human SS never throws | Dead stick then no South | CPU runs the glove; ball scooped; no throw unless the human presses South; batter safe when Time |
| S-34 | Grounder to SS, human arms 3B with nobody on, throws | | Ball to 3B; batter safe at 1B; caption names the wasted throw |
| S-35 | Bad-chem throw to 1B, σ big | 100 seeds | Some throws miss the cover by > 6 ft → live, ERROR, batter to 2B |
| S-36 | Runner on 2nd, grounder to 3B in front of them | CPU runner | Runner holds; 3B throws to 1B |
| S-37 | Runner on 2nd, grounder to 2B behind them | CPU runner | Runner goes to 3B if margin > 0.4 |
| S-38 | Runner on 3rd, infield in, grounder to SS, < 2 outs | CPU runner | Holds; SS throws to 1B |
| S-39 | Runner on 3rd, 2 outs, any grounder | CPU runner | Goes on contact |

### B.3 Double plays (§10.4) — S-40 … S-50 as tabled, each asserting: two outs only if both arrivals win; one out + FIELDER'S CHOICE otherwise; force removed after the batter is retired first (S-43, S-48 are tags). ✅ P5 (`OutsScenarioTests`, every row on the CPU seat and on the human seat; a throw is an out only when the body it was for was short of the bag as it landed).

### B.4 Flies, liners, tag-ups

✅ P5 for S-51 … S-55 (`OutsScenarioTests`; S-54 runs with the roster's Field-8 arm and three runner speeds); ✅ P2 for S-56 … S-59 (`FlightScenarioTests`).

| Id | Setup | Input | Expect |
| --- | --- | --- | --- |
| S-51 | Runner on 1st sent on contact, liner to SS caught | CPU SS | SS steps on 1B or throws: runner doubled off if arrival wins |
| S-52 | Runner on 2nd off the bag, liner to CF caught, throw to 2B | | Doubled off / safe by arrival |
| S-53 | Runner on 3rd holding on the bag, liner caught | | One out; runner stays |
| S-54 | Runner on 3rd tags on a 220 ft fly to LF (arm Field 9), goes | | Throw home; out or safe by arrival; a close-play icon only if within 0.25 s |
| S-55 | Bases loaded, pop to SS dropped on purpose | CPU runners | Runners on bags; force at home only |
| S-56 | Fly 12 ft over the fence, CF Super Jump in the window at the wall | West | Robbed, out |
| S-57 | Same, no ability | West | Home run |
| S-58 | Fly hits the wall below the top | | Carom; live; batter to 2B by geometry |
| S-58b | Fly crosses the fence line at `fenceHeightFt` ± 0.5 ft (D15) | | +0.5 home run; −0.5 carom off the padding you see |
| S-59 | Bounce then over the fence | | Ground-rule double: every runner +2 |

### B.5 Steals and pickoffs — S-60 … S-72 as tabled in §11.5. ✅ P6 (`StealScenarioTests`).

### B.6 Close plays, rundowns, Time, scoring

✅ P5 for S-73 … S-76 (`OutsScenarioTests`); ✅ P3 for S-77 … S-79 (`RunnerScenarioTests`) and S-80 … S-82 (`InningsScenarioTests`).

| Id | Setup | Input | Expect |
| --- | --- | --- | --- |
| S-73 | Throw home arrives 0.5 s before the runner | | Out, no icon |
| S-74 | Throw home arrives 0.1 s before the runner | offense mashes first | Safe; icon shown |
| S-75 | Same, nobody presses | | CPU reaction table decides |
| S-76 | Runner off 1st with the ball in 1B's glove 10 ft away | stick back / forward | Rundown; ends by tag, bag, or overthrow |
| S-77 | Single to CF with runners on 1st and 3rd; runner from 3rd scores, runner from 1st holds at 2nd | | Play ends after 1 s on bags; score +1 |
| S-78 | Third out is a force at 2B while the runner from 3rd crossed home 0.2 s earlier | | No run |
| S-79 | Third out is a tag at 3B after the run crossed | | Run counts |
| S-80 | Home leads after the top of the last inning | | Bottom skipped; game over |
| S-81 | Tie after the last inning | | Extra innings to the cap |
| S-82 | 10-run lead after the trailing side bats in the 3rd of a 6-inning game | | Mercy |

### B.7 Determinism and seats

| Id | Expect |
| --- | --- |
| S-90 | Same seed + same recorded commands → identical `PlayEvent` stream, CPU seat and human seat (#512) |
| S-91 | Every scenario above runs without Unity scene objects |
| S-92 | No `PlayEvent` is produced by a `System.Random` outside the sim's `_rng` |
| S-93 | Seat ownership is a function of (half, home/away, seated controllers) and nothing else (§0.4): the batting human's pad never reaches a glove, so a CPU defense scoops and throws with the offense pad live (S-31 with the human on offense, both halves, HOME and AWAY); the human on defense still owns the throw (S-33); in 1v1 the other controller owns the gloves every half (#579) |
