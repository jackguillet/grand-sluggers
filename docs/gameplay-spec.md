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

1. **Sim owns baseball. Unity presents.** Every out, safe, strike, ball, foul, and advance is decided in `GrandSluggers.Sim` from positions and times. Unity may animate a verdict; it may not produce one. ⚠️ Today Unity's `InPlayDirector` decides relay chains, close plays, CPU catch timing, and the steal phase.
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
| D1 | Lead-offs | **None.** Runners stand on the bag. `Lead01`, the lead stick verb, and the mini-diamond lead pips are retired. | Neither Sluggers nor Superstar Baseball has leads. Leads are what made random pickoffs "necessary" and what made the steal a time credit instead of a race. |
| D2 | Steal jump | Armed runner breaks at **release**; armed inside the first 0.25 s of the windup is a **perfect steal** and breaks 0.4 s before release. | Superstar Baseball frame data (frame 40 vs frame 15 of the windup). |
| D3 | Pickoff | A runner on the bag is always safe. A pickoff catches an armed runner who **already broke** (an early arm breaks on the pitcher's first motion, including a pickoff motion). | Reference: "a pure pick-off can never get a runner out". This is the mind game, not a roll. |
| D4 | Contact quality | **Cursor decides quality, timing decides direction.** Sour / nice / perfect by where the ball meets the cursor; early pulls, late pushes; outside the window is a whiff. | Booklet plus the Superstar datamine (five bat zones, 9-frame slap / 7-frame charge window). |
| D5 | Close plays | Button prompt at **third and home only**, only when the throw and the runner arrive together. | Sluggers booklet wording. Superstar Baseball used a body-check roll instead; we take the prompt. |
| D6 | Five-star free homer | **No.** | Superstar Baseball had it; Sluggers dropped it. |
| D7 | Pitch pace | Keep ≈ 0.85–1.10 s to the plate for now; the reference is closer to 0.6–0.75 s. Human parity gate (#534) decides. | Derived from Superstar speeds, not measured in Sluggers. |
| D8 | Innings | 3 / 6 / 9 (not 1 / 3 / 5 / 7 / 9). Extra innings up to +3. Mercy 10 at the end of an inning. | Party default; the reference cap and mercy are copied. |
| D9 | Infield fly, balk, dropped third strike, intentional walk, DH | None. | Neither game has them. |
| D10 | Steal of home | Legal (armed from third). | Nothing in the reference forbids it; the catcher's zero-length throw makes it rare. |
| D11 | Flat bag-cover speed | Keep (it is how the reference moves covers), but as a data number. | Superstar datamine: constant cover speed starting 14 frames after the hit. |

---

## 1. Match rules

| Rule | Spec | Status |
| --- | --- | --- |
| Sides | 9 v 9. Positions P, C, 1B, 2B, 3B, SS, LF, CF, RF. | ✅ |
| Innings | 3 (party default), 6, 9. Selected on the title (Tab). | ✅ |
| Home / away | Away bats the top. Home bats the bottom. 1P: controller 1 picks HOME or AWAY. | ✅ |
| Walk-off | Bottom of the last inning (or later) ends the moment the home team leads. Bottom is skipped if home leads after the top of the last. | ✅ walk-off; ⚠️ only the take path calls `EndIfWalkOff` (`Match.cs:556-559` vs `:575-584`) |
| Extra innings | Tied after the last scheduled inning → play full innings until a lead after a complete inning (or a walk-off). Cap at scheduled + 3; a tie at the cap is a tie. | ❌ |
| Mercy | Optional (default **on** in Exhibition). 10-run lead after the trailing side has batted in inning 3 (or later) ends the game. Off for 3-inning games. | ❌ (documented in systems.md, not in sim) |
| Designated hitter | None. The pitcher bats. | ✅ |
| Lineup | Nine, set in Offense / Defense Setup. No substitutions except the pitcher swap (§4.7). | ✅ |
| Count | 4 balls = walk. 3 strikes = strikeout. Foul with 2 strikes stays 2 strikes **except a bunt**, which is strike three (§5.8). | ⚠️ foul bunt (`Match.cs:773-779`) |
| Hit by pitch | A pitch that meets the batter's body while the batter does not swing awards first base (§4.6). | ⚠️ unreachable geometry (`AtBatResolver.cs:241-256`) |
| Balk, intentional walk, dropped third strike, check swing, infield fly, appeal plays | Not in the game. | ✅ by omission |
| Ground-rule double | A fair ball that bounces on the field then leaves it over the fence: batter and all runners advance exactly two bases from where they started. | ❌ (no fence in flight, `BallFlight.cs`) |
| Ball out of play (foul territory beyond the wall / into the stands) | Foul ball, dead. | ✅ (spray > 45° is dead) |
| Stars | Shared 0–5 per team. §12. | ✅ |
| Ties in geometry | Tie at a bag goes to the runner. | ✅ (`InPlay.ForceOnBag`) |

**Scoring on the third out.** A run counts if the runner touched home *before* the third out, unless the third out is a force or the batter-runner retired before first. Both timers are already tracked by the live play; the rule is a comparison at `Complete`. ❌ (`FinishInPlay` places runners by table).

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
| **FLIGHT** | Ball travels rubber → plate in `AirSeconds` (≈0.85–1.10, D7). Pitcher steers break (stick L/R). Batter may swing at any moment; the bat reaches the plane 0.30 after release of the button. Stealing runners run at ⅔ speed until the ball reaches the plate. | Both | Ball crosses the plate plane (take) or bat plane meets ball (swing). |
| **JUDGE** | One function, one frame: strike/ball, swing/miss, contact quality, foul/fair, HBP. | Sim | Dead or live. |
| **DEAD** | Count updates. Steal in progress resolves as a **catcher throw play** (§11). Pickoff resolves as a pickoff play. | Sim, then catcher seat | Stamp. |
| **LIVE** | Ball in play. Fielders, runners, throws, tags, forces (§7–11). | Both | `Time` (§10.6). |
| **STAMP** | Result named on the field. Scoring, outs, bag placement already applied. Hold `afterOutSeconds` / `afterCountSeconds`. | Sim | SET, half change, or game over. |

Rules:

- **The judged pitch is the shown pitch.** The strike/ball/contact verdict is computed from the same trajectory the batter sees, including in-flight break. ⚠️ CPU batter decides at launch before the human steers (`AtBatDirector.cs:245-246, 311-314`). Fix: CPU batter decides at the same plate-plane instant as a human would, from the final trajectory (S-05).
- **A swing before release is a swing.** It resolves as an early miss (strike) with the bat arriving 0.30 s after the press. A press during SET is ignored (it is not a swing yet). ⚠️ SET press silently dropped or resolved as a −65-frame miss (`AtBatDirector.cs:173-174, 235`).
- **Box and rubber positions persist** across pitches of the same at-bat; Down resets. ⚠️ `ResetBatter()` after every pitch (`Match.cs:558, 574`).
- **Nothing advances baseball while a seat is disconnected** (how-to-play.md, two controllers). ✅

---

## 4. Pitching

### 4.1 The four verbs

Same shape as the swing: tap / charge / modifier / star. Booklet-confirmed contract (issue #534).

| Verb | Input | Ball |
| --- | --- | --- |
| Normal | Tap and release South | Pitcher's base fastball, easiest control |
| Charge | Hold South to MAX (`pitchChargeSeconds` 0.55), release inside the MAX band (`chargeMaxHoldSeconds` 0.5) | +mph. Released inside the first 0.25 s of MAX = **Nice!** (+5% on top). Past the band the charge decays (`chargeOverchargeDecay`) toward a normal pitch. A charged pitch takes only 10% of the break (reference: charge and changeup are "essentially straight") |
| Changeup | West held through release | −20% mph, hangs then dumps late (§4.3). 10% of the break |
| Break | Stick L/R **after release** | Ball bends toward that side of the screen. Direction only (magnitude ignored); how fast the bend reaches full is the Pitch stat. Capped at half a zone |
| Star | North armed + South | Captain star pitch (§13). Costs a star even if hit |

### 4.2 Location

- Walk the rubber with stick L/R during SET/WINDUP (`WalkPitcher`, ±1). The release point moves with the body; the ball's crossing moves by the **same world distance** as the body, once. ⚠️ Human crossing moves 1.35× offset, CPU 0.35× (`AtBatDirector.cs:224`, `Match.cs:667`, `PitchFlight.cs:45`).
- **Vertical location** is a pitch property, not a stick: normal/charge cross mid-zone; changeup crosses low; break pitches cross mid and drift; the human moves height by pitch choice and by letting a changeup dump. Stick U/D is *not* an aim axis during SET. ⚠️ `AimY` hard-coded 0 (`AtBatDirector.cs:224`) — acceptable for the human because height is a pitch property, but the CPU must use the same rule (§4.8).
- Post-release break moves the crossing by at most **half the zone width** (0.46 ft of 0.92). ⚠️ Full break moves it 1.8 ft, twice the zone half-width (`PitchFlight.cs:58-63`).
- Tired pitcher (§4.7): a random wobble of the crossing on every pitch, visible as a shaky streak.

### 4.3 Pitch shapes

All shapes are `data/rules/pitching.json` curves, evaluated by `PitchFlight.Point(u)`; the strike zone reads the u=1 sample. Time to plate `AirSeconds(mph)` ≈ 0.85 (charged) – 1.10 (changeup), Sluggers pace.

| Shape | Speed | Path |
| --- | --- | --- |
| Fastball | base + Pitch×k + charge | Straight, mild drop |
| Changeup | 0.80× | Flat until u≈0.6, then dumps below the crossing height by up to one zone-half |
| Break (stick) | fastball | Adds lateral drift that grows late (u>0.55), signed by stick |
| Star pitches | per skill | Fastball + skill shape (§13); the *shape* is data, the effect on the batter is the skill rule |

Pitch **type strings** (`"curve"`, `"slider"`) are retired: break is a stick verb, not a type. ⚠️ Types exist in code, unreachable from the mound seat (`AtBatResolver.cs:264-281`, `AtBatDirector.cs:222`).

### 4.4 Strike zone and judgment

- Zone is a fixed world rectangle over the plate (`StrikeZoneGeometry`: half-width 0.92 ft, bottom 1.45, top 3.65). It does not scale with the batter body (arcade, readable). ✅
- **Take in zone** = called strike. **Take outside** = ball. Judged at the plate crossing of the shown trajectory. ✅ (`StrikeZoneGeometry.Contains(Point(u=1))`; issue #535 covers any residual divergence)
- **Swing and miss** = strike regardless of location. ✅
- The white frame on screen *is* the zone. The frame never lies. ✅ (`PitchJudgmentGate`)

### 4.5 Pickoff (SET only)

- Defense arms a bag (D-pad / 1–3) and presses South during SET. The pitcher turns and throws to that bag; the covering fielder (1B / SS / 3B) takes it. The pitch clock resets; the count does not change.
- A runner **on the bag** is safe. Always. No play, a small "back" beat, no stamp. (Reference: a pure pickoff never retires a runner.) ✅ for a glued runner today.
- A runner who has **broken** (an early-armed steal breaks on the pitcher's first motion — a pickoff motion counts, D3) is now between bags: the receiver at the bag throws ahead of them or chases; a **rundown** (§9.7) or a tag at the next bag decides it by geometry. That is the whole point of the pickoff: it punishes arming the steal too early. ❌ Today a pickoff arms a *steal* on the runner and a miss awards the base (`Match.cs:480-503, 1376-1393`).
- **No random pickoffs.** A runner is never retired on a pitch by a roll. ❌ (`ResolvePickoff`, `Match.cs:1421-1481`, up to 72% per pitch with a lead.)
- CPU pitcher attempts a pickoff 3–10% of SETs with a runner on (by difficulty), lead runner by default, 1B on first-and-third (§4.8). A failed CPU pickoff is a wasted beat, not a base.

### 4.6 Hit by pitch

- If the pitch's plate-plane point lies inside the batter's body circle (radius ≈ 0.45 ft, centered where the batter body actually is, including box walk) **and the batter did not swing**, the batter is hit: first base, forced runners advance, ball dead, stamp HIT BY PITCH. Balls/strikes unchanged.
- The batter body and the sweet-spot oval move by the **same** world distance per box unit. ⚠️ Cursor moves 1.85 ft/unit, body 2.4 ft/unit (`AtBatFeel.cs`, `HomeSet.BatterWalk`).
- A human pitcher can reach the body by walking the rubber fully toward the batter's side plus break. CPU pitchers reach it only through scatter (rare, ≈1 per game at Pitch ≤ 4). ⚠️ unreachable (`AtBatResolver.cs:250-256`).

### 4.7 Stamina and the pitcher swap

- Stamina is **per pitcher** (each character carries their own pool for the match), pool = 60 + Pitch×6.
- Costs (`data/rules/pitching.json`): normal 4, charge +3, changeup 3, break +1, star = the skill's `staminaCost` (`data/abilities/star-skills.json`, 8–22), homer allowed +6, each run allowed +2.
- Below 25 = **TIRED**: −6 mph, −40% break, crossing wobble σ 0.25 ft, sweat and card tell. Below 0: −10 mph, wobble σ 0.45 ft. ⚠️ Team pool, flat costs, `staminaCost` JSON never read (`Match.cs:38-39, 1289`).
- **Swap** (Select / R during SET): pick any fielder as the new pitcher; the old pitcher takes that glove. Each character's pool is their own, so a fresh arm is fresh. The swap costs no time-out. Once per half-inning. ⚠️ Adds +35 to a team pool and auto-picks (`Match.cs:602-622`).
- CPU swaps at TIRED with a lead ≥ 3 or at 0 always.

### 4.8 CPU pitcher

A decision table, not nested rolls. Evaluated once per SET from (count, outs, runners, batter Bat, own stamina, stars).

| Situation | Location target | Pitch mix (normal / charge / changeup / break) | Star |
| --- | --- | --- | --- |
| 0-0, 1-0, 1-1 | Zone edges (corner picked by batter hand: away) | 45 / 20 / 15 / 20 | 5% (captain, ≥1 star) |
| Ahead 0-2, 1-2 | Just off the zone (waste), then edge | 20 / 15 / 35 / 30 | 15% |
| Behind 2-0, 3-0, 3-1 | Middle-in, safe | 60 / 30 / 5 / 5 | 0% |
| Runner on with 2 outs | Middle, fast | 50 / 40 / 0 / 10 (pitch-out never) | 0% |
| TIRED | Whatever the table says, then §4.7 noise | | |
| Pickoff | Before the pitch: 3% / 6% / 10% by difficulty when a runner is on; lead runner, or 1B on first-and-third (66%) | | |

Aim scatter σ = (11 − Pitch) × 0.055 ft around the *target*, not the center. ⚠️ CPU always targets dead center; type via three nested rolls (`Match.cs:648-668`). The `TimingErrorFrames` on `PitchCommand` is dead and is removed.

---

## 5. Batting

### 5.1 The four verbs

| Verb | Input | Swing |
| --- | --- | --- |
| Slap | Tap and release South | Full swing, widest window, base power |
| Charge | Hold to MAX (`swingChargeSeconds` 0.45), release in the MAX band | Narrower window (×0.78), more power (up to ×1.35 at MAX). Past the band the charge decays |
| Bunt | Hold West through the pitch | Batter squares at the press; contact when the ball reaches the bat (§5.8) |
| Star | North armed + South | Captain star swing (§13). Costs a star even on a miss |

Charge at MAX is the "Nice!" tell (rings line up). ✅ ⚠️ Charge adds only ×1.12 power (`AtBatResolver.cs:73-75`), too small to be a choice.

### 5.2 The cursor (sweet spot) — quality

The reference model (D4): the bat is a hitbox along the swing plane, split into **five zones** — sour / nice / perfect / nice / perfect — and the ball meets one of them by *where it crosses*, not by when you pressed. The cursor on screen is that hitbox drawn on the plate.

- A gold oval **follows the batter**, never the pitch (booklet-confirmed). Box walk moves it in X; its Y is the batter's natural contact height. The oval is the perfect+nice zones; the rim is sour.
- The oval is **tall enough that any strike is hittable**: it spans the zone height plus a rim. Quality is by distance from the center along X (bat barrel: tip side sour, sweet spot perfect, handle side sour, asymmetric per body — the handle side is shorter) and along Y (a ball at the top or bottom of the zone is at best nice). ❌ Today the oval covers only Y ∈ [1.83, 2.97] of a zone spanning [1.45, 3.65]; the top 0.68 ft and bottom 0.38 ft of every strike are an automatic miss (`AtBatFeel.cs:127-152`), and the overlap is a 3-step quantization.
- Bat (contact) stat scales the oval; a charge **narrows** the perfect and nice zones (reference: charge zones are smaller than slap zones). Good-chemistry runners on base widen the slap zones (×1.05 / 1.10 / 1.20 for 1 / 2 / 3).
- Vertical placement is *earned* by the pitch choice on the mound (a changeup dumps under the center; a high charged fastball rides over it). A **sour slap on a changeup or a charged pitch is a pop-up** (reference rule). That is the pitcher-vs-batter game.

| Zone | Slap exit (× base) | Charge exit (× base) | Tell |
| --- | --- | --- | --- |
| Perfect | 1.00 | 1.15 | PERFECT flash, crack, `solidFreeze` |
| Nice | 0.95 | 1.12 | NICE |
| Sour | 0.75 | 0.95 | dull thud; forced pop/topper by timing |
| Off the bat | miss | miss | whiff |

Reference numbers behind the ratios: slap sour 100–130 / nice 140–145 / perfect 145–150; charge sour 140–150 / nice 162–177 / perfect 160–170, with the perfect charge carrying because it gets no added gravity.

### 5.3 Timing — the window and direction

`err` = (bat-plane time − ball-plate time) in frames at 60 Hz, bat plane = press + 0.30 s (`Motion.SwingContact`).

- **Window**: slap **9 frames**, charge **7 frames** (reference), ± (contact − 5) × 0.4, × skill multipliers, **floored at 5 frames**. Outside the window the bat is not on the plane: **miss**, strike. ⚠️ Today the window is 4.8–9.75 frames with a 1.0-frame "perfect" *timing* band sampled once per render frame — a coin flip at 60 fps (`AtBatResolver.cs:9-11`); no floor.
- Inside the window, timing does **not** change quality (D4). It changes **direction**: early contact **pulls**, late contact **pushes** (opposite field). Linear across the window: earliest frame ≈ 55° toward the pull line, center ≈ straight at second, latest ≈ 55° toward the opposite line. Stick L/R at contact shifts the whole range by ±12°.
- The batter's handedness mirrors the map. A right-handed batter who is early hits toward 3B.
- Quality still moves slightly with timing only through the *rim*: the earliest and latest frame reduce the cursor overlap by one zone (perfect → nice) because the bat is not square. Never two zones. ⚠️ Perfect → Cheap (`AtBatResolver.cs:54-55`).

### 5.4 Height and the stick

- **Launch** = base by pitch height (low pitch → lower launch) **+** stick U/D at contact: **Up = over the top = grounder** (probability mass to the lowest band), **Down = under = lift** (to the highest band). The reference maps up→grounder, down→fly; this matches the current sign (`AtBatResolver.cs:90-91`) and is now the rule. The same axis must **not** also reset the box (`AtBatDirector.cs:171, 288` ⚠️ — Down-reset is a SET verb only, before the windup).
- Launch is drawn from five bands (topper / grounder / liner / fly / pop) with probabilities by zone × swing × stick, `data/rules/batting.json`. Sour contact is forced to the topper (early) or pop (late) band. ✅ shape; ⚠️ single formula with ±7° noise.
- Sluggers' "scatter hit" (D-pad at contact) is our stick L/R above. The reference guide calls it unreliable; ours is deterministic.

### 5.5 Exit velocity

`exit = base(power) × zone × charge × starSwing × buddies × pitch`.

- `charge`: 0 → slap; MAX → **×1.25** (reference: charge perfect 160–170 vs slap perfect 145–150, with less gravity). Below MAX interpolates; past the band it decays. ⚠️ ×1.12 max (`AtBatResolver.cs:73-75`).
- `buddies`: good-chemistry runners on base — **×1.10 / 1.25 / 1.50 on a charged swing** (reference), and the slap-zone widening in §5.2. ⚠️ Applies to every contact including cheap (`ChemistryTable.cs:96-105`).
- `pitch`: a charged pitch met with sour contact ×0.6; met with a perfect charge ×1.1 (reference "pitch type impact"). A high-Pitch arm dampens non-perfect contact (nice ×0.9, sour ×0.75 at Pitch 10) — the reference's hidden "cursed ball" made visible as the Pitch stat.
- The Charge Bat gives a manual-MAX charge for free and keeps the narrow charge zones off; it is never worse than a manual charge. ⚠️ Pins ×1.10, below the manual ×1.12 (`AtBatResolver.cs:73-75`).
- Pull / push hitters (`data/characters/` optional `hitType`): ×1.05 to the named side, ×0.9 to the other. Optional; default mid.

### 5.6 Fair, foul, home run

- A ball is **foul** if it *lands* (or is first touched by a fielder) in foul territory, or rolls foul before passing a base without being touched. It is **fair** if it lands fair past the bases, or is touched fair, or leaves the park between the poles. The chalk is geometry, not a spray cutoff. ⚠️ Foul = spray > 45° at contact; a 400 ft fly at 44° is a homer, a 46° pop is foul (`AtBatResolver.cs:13-23`).
- **Home run**: the flight crosses the fence line above fence height between the poles. One rule, used by the flight, the fielding preview, and the landing ring. ⚠️ Two mismatched launch bands (18–38° vs 16–40°) (`AtBatResolver.cs:119-122`, `Fielding.cs:340-346`); no fence in the trajectory.
- A ball that hits the wall is live (§7.9). A ball that bounces over is a ground-rule double (§1).
- **Foul ball** with fewer than 2 strikes adds a strike. With 2 strikes, nothing (except bunt). Runners return. The ball still flies so the camera can chase it; the stamp says FOUL when it lands. ✅ ⚠️ `FinishFoul` skips `AfterPitch` so no steal/pickoff resolution on a foul.
- A foul fly can be **caught** for an out (§7.11). ❌ Foul flights are never fielded.

### 5.7 Hit by pitch — see §4.6.

### 5.8 Bunt

- Hold West before the pitch: the batter squares (pose tell for the defense). Contact is judged when the ball reaches the bat plane, same clock as a swing (no 0.30 offset because the bat is already there). ⚠️ Judged at the press (`AtBatFeel.cs:217-221`).
- Quality: timing tiers apply; a bunt still needs oval overlap (a high pitch popped up is a bunt pop). ⚠️ Bypasses the oval (`AtBatResolver.cs:52`).
- Ball: exit ×0.42, launch 3–12°, spray toward the stick side ±14°. Never a home run. ✅
- **Foul bunt with two strikes is a strikeout.** ❌
- Bunt fielding: P, C, 1B, 3B charge (§7.3). Runner rules: sac bunt is a live play, not a table.

### 5.9 CPU batter

A table, evaluated when the ball crosses the plate plane (same instant a human's swing would be judged), from the **final** trajectory.

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

**Tracking** (the reference model): during the windup the CPU batter *guesses* the crossing (50–80% "same as last pitch") and walks the box toward it; after release it re-reads the ball with a chance to track perfectly or with a fixed offset (0.11–0.41 ft, worse on easy). **If the pitcher moved on the rubber since the last pitch, the mistrack chance rises sharply** (reference: 30–95%). That is why walking the rubber is a real verb against the CPU. Timing error σ = (11 − Bat) × 0.62 frames; fooled by a changeup / charge it swings 4–9 frames early / late.

**No forced-miss clamp against a human pitcher**: the human's meatball is punished by the same table; difficulty is a σ multiplier and the mistrack table (`data/rules/cpu.json` `difficulty` 0.8 / 1.0 / 1.3). ❌ `CpuSwingVsHuman` forces |err| ≥ 3.2 and takes 32% of meatballs (`Match.cs:698-726`). `CpuSwing` must not arm steals (side effect, `Match.cs:672-676`) — that is the runner AI (§11.6).

Charge vs slap by archetype (reference): balanced 50%, power 80%, speed 30%, technique 10% — derived from the character's Bat/Run split.

---

## 6. Ball flight and the field

### 6.1 Flight

- 3-D ballistic with drag and a directional park wind (`Park.WindMph` + `WindDeg`), integrated at 120 Hz. ⚠️ 2-D with scalar downrange wind (`BallFlight.cs:34`).
- Sample times are stretched by `TimeScale` (1.65) so gloves can get under a fly. Carry does not change. Pitches and throws are **not** stretched. ✅ The same stretched clock is the clock fielders and runners run on — one clock (§0.3). ✅ by accident today; make it explicit.
- Bounce: restitution 0.48, horizontal 0.82; liners (14–22°) skid (0.28 / 0.93). Roll friction 22 ft/s². Rest at 1.4 ft/s. ✅ (constants → `data/rules/flight.json`)
- **Fence**: the path is clipped by the outfield fence polygon (from the park's L/C/R distances, `RoundFence`). Below fence height → wall carom (§7.9). Above → home run. Bounce then over → ground-rule double. ❌
- **Foul lines / backstop / side walls** are the field boundary; the ball cannot leave the park except over the fence or into foul stands (dead). ⚠️ `FieldBounds` clamps gloves; the ball itself rolls through the wall.
- **Landing mark**: the first ground contact of the clipped path; the ring is where a glove has to be. Wall plant if the ball hits the wall first. ✅ (`FlyCatch.ChaseTarget`)

### 6.2 Batted-ball classes

Class is a function of launch angle and exit velocity at contact, used by fielding assignment, cameras, and runner AI. One table (`data/rules/flight.json`):

| Class | Launch | Exit | Typical |
| --- | --- | --- | --- |
| Topper | < 3° | any | Weak roller in front of the plate |
| Grounder | 3–10° | any | Infield hop |
| Chopper | 3–14° with a first bounce inside 30 ft and bounce height > 3 ft | ≥ 70 | High bounce, slow to the glove |
| Liner | 10–22° | ≥ 78 | Rope; catchable inside 1.2 s |
| Fly (infield / pop) | > 22° | first landing < 155 ft | Pop-up, long hang |
| Fly (outfield) | > 22° | landing ≥ 155 ft | Routine / deep by carry |
| Wall ball | fly or liner that meets the fence below fence height | | Carom |
| Home run | crosses the fence above height | | Dead, runners circle |
| Bunt | bunt verb | | Dribbler in the triangle |
| Foul | lands / touched in foul territory | | Dead unless caught |

⚠️ `IsGrounder < 14°`, `IsLine 14–22°`, `HomeRunLikely` bands are three separate hard-coded checks (`Fielding.cs:327-346`).

---

## 7. Play types — what happens on each

Each subsection is one scene: **who fields**, **what runners do**, **the throw**, **the camera and stamp**. "Runners" means offense-controlled runners; a human presses, CPU follows §9.9. Fielder decisions are §8.8 for CPU; a human fielder has the verbs in how-to-play.

Common to all live plays:

- **Batter always runs** on fair contact. ✅
- **Forced runners run** on a grounder (they have no choice). Unforced runners hold at a **read step** (a few feet off the bag, leaning) until the ball is through or fielded, then go/hold by the send rule. ❌ (`OccupiedDestBag` table)
- **On a fly / liner**, all runners hold near the bag until the catch or the drop (tag-up rule §9.5). ✅ hold; ⚠️ tag-up is one global flag.
- **The throw** goes where the fielder names (human) or where the decision table says (CPU, §8.8). The out is judged when the ball arrives (§10). ✅ for named bags.
- Camera: `diamond` 45° on the dirt under the ball; fly pulls back to `diamond-fly`; a throw does not cut behind the thrower (`data/feel/shots.json`). ✅
- Stamp: OUT / SINGLE / DOUBLE / TRIPLE / HOME RUN / DOUBLE PLAY / TRIPLE PLAY / FOUL / ERROR when the play is dead. ✅ ⚠️ ERROR stamp missing.

### 7.1 Grounder to an infielder (routine)

- **Fields**: the infielder whose planned route meets the ball earliest (`FieldingPursuit.Choose`) — 1B/2B/SS/3B, P on comebackers, C on toppers. Gloves scoop by touching the ball on the dirt (no button). ✅
- **Runners**: batter to first; forced runners go; unforced hold at the read step, then advance only if the throw goes elsewhere and they can beat a relay (§9.9).
- **Throw**: nobody on → 1B. Runner on 1st → 2B for the force (then 1B if time, §10.4). Runners on 1st and 2nd → 3B if the fielder is 3B/SS near the bag, else 2B. Loaded → home if the fielder is inside 60 ft of the plate, else 2B. Human names the bag; the default follows this table. ✅ default bags; ❌ CPU out decided by a roll (`Fielding.cs:146-161`), Unity then force-feeds the glove at hang (`InPlayDirector.cs:397-403`).
- **Out**: force at the bag if the ball (in a glove on the bag) arrives before the runner. Tie to runner. Bobble (§8.6) adds time; it does not decide.
- Stamp OUT (one out) / FORCE OUT caption / SINGLE if the runner beats it (a fielder's choice or an error — stamp ERROR if the throw sailed).

### 7.2 Slow roller / topper

- **Fields**: P or C, or a charging 3B/1B. Bare-hand pose if the fielder is running toward home.
- **Runners**: batter races; forced runners usually safe (the throw goes to 1B by default because the force at 2B is not makeable — the decision table computes margins, §8.8).
- **Throw**: 1B unless a runner on 3rd is going home and the fielder is inside 45 ft (then home).
- Beat the throw → infield single (stamp SINGLE). ✅ possible; ⚠️ resolved by the roll.

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
- **Runners**: batter rounds first and reads the outfielder (send to 2B if the pickup is deep and the arm is weak, §9.9). Runner on 1st → 3rd if the ball is to RF/CF and picked up beyond 200 ft, else 2B. Runner on 2nd → home unless the ball is hit to LF shallow and the arm is strong. Runner on 3rd scores. All by the margin formula, not a table. ❌
- **Throw**: outfielder throws to the base *ahead* of the lead runner if makeable, else to the **cutoff** (§8.7) to hold the batter at first. A throw home goes through the cutoff unless the arm can reach on the fly.
- Runner thrown out at a base = tag (unforced) or force (batter at 2B when an outfielder throws there? no — the batter is only forced at 1B). ✅ tag/force distinction.
- Stamp SINGLE / DOUBLE; OUT at a bag stamps OUT with the caption naming the throw.

### 7.6 Line drive

- **Fields**: the infielder or outfielder on the line if a route meets the ball above 0.75 ft before the first bounce (catch window ≈ hang − 0.25). Liners are a **jump or dive** verb inside the window; a straight-at-you liner is a South catch.
- **Caught**: out. Runners who left the bag are **doubled off** if the fielder throws to that bag (or steps on it) before they return (§10.5). Runners at the read step are safe if they return in time — that is the tension.
- **Not caught**: it skids; the nearest fielder chases the live ball; runners as §7.5.
- Presentation: `solidFreeze` on the crack; the liner has the short hang so a dive is possible (Super Mega Baseball's rule; ours via `TimeScale`).

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
- **Sac fly**: the runner from 3rd scores if home arrival < throw arrival. It is a live throw, can be an out. ❌ (`AdvanceTagUp`, carry > 230 ft literal, `Match.cs:986`)

### 7.9 Wall ball / carom

- A fly or liner that meets the fence below fence height caroms (restitution 0.48, angle mirrored) and drops at the base of the wall. The outfielder plays the carom (route to the first reachable point on the post-carom path).
- Runners: this is the **double / triple** scene. Batter reads the carom; runner on 1st scores on a carom to the gap with < 2 outs if the margin says so.
- Rob: in the window at the wall, West (jump) with Super Jump / Clamber / Buddy Jump can catch a ball that would clear the fence by ≤ the ability's rob height (§8.4). ✅ windows; ❌ no fence in the flight.

### 7.10 Home run

- Fence crossed above fence height between the poles. Dead ball. Batter and all runners circle at trot speed; scoring is immediate (the throw cannot happen). Stamp HOME RUN / GRAND SLAM. ✅
- The ball keeps flying into the stands (presentation). Night: fireworks (Harbor).

### 7.11 Foul ball

- Foul grounder / foul pop: dead when it lands or leaves the field, **unless a fielder catches it** (foul fly out: C, 1B, 3B, LF, RF near the lines).
- Runners return. A caught foul fly is a fly ball for tag-up purposes.
- Stamp FOUL when dead. ✅ dead stamp; ❌ never fielded.

### 7.12 Strikeout / walk / HBP with runners

- Strikeout: dead. A runner stealing on the pitch is a **catcher throw play** (strike-em-out-throw-em-out DP, §11.5). ✅ steal throw after a miss.
- Walk: batter to first; only forced runners advance. ✅
- HBP: as walk. ✅ placement; ⚠️ geometry.

---

## 8. Fielding

### 8.1 Bodies and positions

- Nine positions from `Diamond.Positions` (feet). Defensive alignment is the lineup's glove diamond (Offense / Defense Setup), not roster order. ⚠️ Assigned by roster list order (`Fielding.cs:420-432`).
- Speed in the field: `chase = 21 + Run × 1.9` ft/s, **one formula** for human and CPU. ⚠️ Human glove uses `18 + Run×1.8` (`InPlayDirector.cs:184`).
- Frozen (park hazard) ×0.45. Dash (East held) ×1.35 for 2 s then fades.

### 8.2 Who is on the ball

- On contact the sim plans a route per candidate to the **landing** (fly) or the **first reachable point** (roller) and picks the earliest meet, then shortest travel (`FieldingPursuit.Better`). Ties: CF over corners, SS over 2B, infielder over pitcher. ✅
- Pools: infield dirt → P, C, 1B, 2B, 3B, SS. Air → LF, CF, RF, SS, 2B (+ C, 1B, 3B on pops in their sector). Grass → LF, CF, RF. ✅
- **Reaction lockout** after contact before a body moves, by position (reference frames → seconds): P 0.42, C 0.67, 1B 0.27, 2B 0.25, 3B 0.30, SS 0.28, OF 0.83. The camera cut to the diamond happens at 0.42. Data (`fielding.json`). ❌ (no lockout; gloves move on frame 1)
- The **YOU** ring names the glove; Select/R swaps to the pulsing next-nearest. Dead stick = CPU runs that glove. ✅

### 8.3 Catch

- **Fly**: catch if the glove is inside the catch radius of the landing (or plant) when the ball is in the window `[hang − 0.48 − extra, hang + 0.14 + extra/2]`. Human presses South in the window; CPU catches automatically at the ball's arrival. Outside the window or radius = drop / falls in. ✅ (`FlyCatch`)
- Catch radius = 10 + Field × 0.6 (+ ability). A jump adds 8 ft of reach and a window bonus; a dive adds 8 ft along the lunge (10 ft) and only below 7.5 ft ball height. ✅
- **Roller**: standing on the ball scoops it, no button. ✅ Bobble check on scoop (§8.6).
- **Liner**: same as fly with the short window.
- **CPU catch is geometric**: the glove must be inside the radius at the window. It is **never force-fed at hang because a roll said out**. ❌ (`InPlayDirector.cs:397-403`)

### 8.4 Jump, dive, wall rob, buddy jump

| Verb | Input | Effect |
| --- | --- | --- |
| Jump | West in the window (arms through it) | +8 ft reach up; can rob a ball ≤ 4 ft over the fence at the wall |
| Super Jump (ability) | same | rob ≤ 18 ft over; window +0.16 |
| Clamber (ability, wall parks) | same at the wall | rob ≤ 28 ft over; window +0.12 |
| Buddy Jump | West with a good-chem partner planted within 26 ft | rob ≤ 18 ft; both bodies |
| Dive | East tap | 10 ft lunge toward the ball, +8 ft reach, ball < 7.5 ft |
| Grow / Lick (ability) | passive | +6 / +3 ft catch radius, window +0.08 |

✅ all windows exist (`FlyCatch`, `FieldAbilities`). ❌ the rob height is meaningless until the flight has a fence.

### 8.5 Throws

- **One throw model.** `throwSec = 0.22 + dist / (56 × arm × chem × ability)` ft/s, with `arm = 0.85 + Field × 0.03`. This one number flies the ball *and* judges the bag. ⚠️ Four formulas: `InPlay.ThrowSec`, `StealThrow.CatcherThrowSec`, the Unity `_throwDur = max(0.55, spec)`, and `RelayBeats` re-deriving from hang (`InPlay.cs:63-67`, `StealThrow.cs:42-53`, `InPlayDirector.cs:745, 862-887`).
- **Accuracy**: lateral error σ = (11 − Field) × 0.35 ft. A throw that lands more than 6 ft from the cover is **not caught** — it skips past, the ball is live, runners take the extra base (stamp ERROR). ⚠️ `LateralFt` is computed and never read (`ChemistryTable.cs:91-92`).
- **Chemistry** (systems.md, reference): good ×1.30 speed, purple laser, never to the cutoff. Bad: **20% of throws are "slanted"** — ×0.70 speed with a 10–14 ft lateral miss (an error by the rule above); the other 80% are ordinary. The roll is on the *input* (the throw's accuracy), the outcome is still the ball missing the cover. ⚠️ Today the 25% roll is a boolean `Error` that the resolver converts to a Single (`ChemistryTable.cs:85-94`, `Fielding.cs:152-160`).
- **Situational speed** (reference): a throw to a bag nobody can beat is a lazy lob (×0.35) — presentation of a non-play; a throw to an **uncovered bag slows to a lob until the cover arrives**, and if nobody is coming it drops at the bag (live). This is how "the receiver must be on the bag" reads on screen.
- Abilities: Laser ×1.45, Snap Throw ×1.22. ✅
- The thrower's body: after the throw the fielder **stays where they are** (or drifts to back up); the receiver at the bag is whoever covers (§8.7). ⚠️ The human glove teleports to the destination bag at release (`FieldAssist.AfterThrowPos`, `InPlayDirector.cs:749-768`). Fix: the human's YOU ring hands to the receiver; the body does not move.
- You may **arm a bag before the catch**; the throw fires on South after the catch. ✅

### 8.6 Errors

- **Bobble**: on a scoop or catch, chance = f(ball energy, hands) — hard-hit balls to weak gloves. A bobble is a 0.58 s fumble with the ball scattered ≤ 6.5 ft; the play is live and the runner gains that time. It **never converts an out into a caption**. ⚠️ Turns `GroundOut` into `Single` in the resolver (`Fielding.cs:152-160`); Unity re-rolls with an ad-hoc seed (`MatchDirector.cs:762-796`).
- **Throwing error**: the lateral miss above.
- **Drop** (star effects, frozen): a fixed drop chance on the catch is allowed for *skills* (burn-hop, phony) because the skill is the two-second rule; it is never allowed for plain baseball.
- Stamp ERROR and an "E" tell on the body. ❌

### 8.7 Cover, cutoff, relay, backup

- **Cover**: on contact each non-fielding infielder walks to the bag they cover — 1B covers first (2B covers first if 1B is fielding), 2B/SS cover second (whichever is not fielding), 3B third, C home, P backfills any abandoned bag and backs up first on a ball to the right side and home on a throw home. Cover moves at a **flat cover speed** starting 0.23 s after contact (reference: constant, stat-independent; D11) — a data number, not `28` in Unity (`InPlayDirector.cs:340-355` ⚠️). Outfielders not on the ball go to support spots or back up the throw 60 ft behind its target.
- **Cutoff**: on an outfield throw home or to third, the cutoff is the infielder on the line between the fielder and the target (SS for LF/CF, 2B for RF; 1B for a throw home from RF). Geometric, not "SS then 2B" (`Fielding.cs:412-418` ⚠️). LB / X with no bag = throw to the cutoff. ✅ verb.
- **Relay**: a throw to the cutoff continues automatically to the armed bag (human) or to the decision-table bag (CPU) with the cutoff's own arm.
- **Backup**: the pitcher / the outfielder behind a bag runs to the backup spot on a throw. Presentation-only at first, but it decides where an overthrow stops (§8.5).

### 8.8 CPU fielder decisions

Computed at the moment the fielder gains the ball, from live positions. For each candidate bag `b`, `margin(b) = runnerArrival(b) − (throwSec(b) + release 0.25)`. A play is *makeable* if `margin > 0.15` (tie band). Choose in this order:

1. **Force at the lead forced bag** if makeable and outs < 2 → throw there; if the receiver then has a makeable relay to the next bag behind, chain it (double play, §10.4).
2. **Home** if a runner is going home and makeable (tag).
3. **Third** if the runner from 2nd is going and makeable (tag).
4. **First** if the batter is makeable.
5. Otherwise **hold**: throw to the bag ahead of the lead runner's next advance (2B on a single with nobody on) or to the cutoff. Never a wild "throw to first" that lets a runner score behind it.

Outfielders: 1) home if makeable and a run is at stake (score within 2 or < 2 outs), 2) third, 3) second, 4) cutoff. Fielder reaction delay before the throw = 0.35 − Field × 0.02 s.

Difficulty (`cpu.json`): margin threshold 0.30 / 0.15 / 0.05 and reaction 1.4× / 1.0× / 0.8×.

❌ None of this exists; the CPU out is the roll at `Fielding.cs:146-161` and the relay chain is Unity's.

---

## 9. Baserunning

### 9.1 The runner model

- Each runner (including the batter-runner) is an object with **position on the basepath** (bag index + feet along the segment), **velocity**, **state** (`OnBag`, `Advancing`, `Returning`, `Stealing`, `Sliding`, `Out`, `Scored`), a **destination bag**, and a **forced** flag snapshotted at contact. ❌ Today: three `RunnerState` slots with `Lead01`, no position, no batter-runner (`Models.cs:276-330`), state wiped by `SetBag` (`Match.cs:1236-1254`).
- Speed: `bagSec = 3.55 − Run × 0.12` (clamped 2.45–3.65) per 90 ft, **one formula** for every segment. The batter-runner starts **0.5 s after contact** from where they stood in the box (reference: 31 frames; a left-handed batter reaches first ≈ 0.5 s sooner because the box is closer). Dash ×1.12 at full mash. ⚠️ Two curves (`InPlay.cs:53-58, 82-83`), and `RunFeet` positions all runners at home-to-first speed.
- **Runners cannot pass each other** (a trailing runner inside 27 ft of the runner ahead stops behind them). ❌
- A runner's position is what every tag, force, and arrival uses. No closed-form "beats" re-derivation. ⚠️ (`RelayBeats`, `BatterBeatsThrow`)

### 9.2 No leads (D1)

- Runners stand on the bag until contact, a steal break (§11.2), or a send. There is no lead stick, no lead pip, no pickoff risk from standing there. The stick-toward-a-bag verb during SET now **arms a steal for the selected runner** (same as L3), which keeps the couch map simple: point at the bag you want, press to go.
- `Lead01`, `TakeLead`, `ReturnToBag`, `LeadSpot`, `MiniLead`, the Unity lead rates and the `Lead` chapter of how-to-play are retired in the same PR. ⚠️ all shipped (`Models.cs:276-330`, `Diamond.cs:46-52`, `Baserunning.cs:71-77`, `ActorDirector.cs:411-453`).

### 9.3 Send / hold per runner

- **All-advance (LB / `,`)**: every runner's destination = next bag (and the next after that if they arrive and it is open). **All-return (RB / `.`)**: every runner returns to the last bag. **Freeze** (both / `/`): hold where they are. A tap of the opposite button halts (reference). ✅ global flags.
- **Per-runner**: D-pad selects a runner (right 1B, up 2B, left 3B, down = batter-runner); stick toward the next bag sends that runner; stick back returns that runner; halt freezes that runner. ⚠️ Halt exists; send/return are only global (`Match.SendAll`).
- Forced runners cannot be held on a grounder once the batter reaches first (they are forced off). A human "hold" on a forced runner is ignored until the force is removed (batter out at first).
- The **batter-runner** is selectable (down / 4 while running) and obeys the same send/return: round first and go, or stop at the bag.

### 9.4 Dash, slide, rounding

- Dash: mash South, +0.28 per press to 1.0, decays 0.5/s; ×1.12 speed at full. ✅
- Slide: automatic on the last 12 ft into a bag when a tag is threatened (throw armed to that bag or a glove within 20 ft with the ball) and the runner is stopping at that bag; a runner rounding never slides (reference). West/South near the bag forces it. A slide shrinks the tag reach by 2 ft; it does not change arrival time. ⚠️ `Sliding` posed, never consulted (`Models.cs:320`).
- Rounding: a runner heading past a bag runs a shallow arc (presentation) and reaches the next bag at the same `bagSec` — no time penalty in the arcade rule.
- Overrun first: the batter-runner may run through first base and is safe from a tag while returning directly, unless they turn toward second (then live).

### 9.5 Fly balls and tag-ups

- On a catchable fly/liner the game **sends every runner back to their bag** (reference: automatic return on a fly). A human can override with a send, at the doubled-off risk.
- Runners **cannot leave until the ball is firmly caught** (post-bobble). After the catch, a runner on the bag may advance (**tag up**). A runner off the bag at the catch must return and touch before advancing; if the defense throws to that bag and the ball beats them back, they are out (doubled off, §10.5).
- All-advance pressed *before* the catch means "tag and go on the catch" — the runner waits on the bag and leaves at the catch. ✅ (`SendAll` tag-up) ⚠️ one global flag, no per-runner.
- A lone runner cannot cross home on a fly with < 2 outs until the catch/drop resolves (reference restriction; keeps a dropped fly honest).
- CPU: runner on 3rd tags on a caught fly ≥ 200 ft with < 2 outs; runner on 2nd tags to third on a fly to RF ≥ 250 ft; else holds. Batter-runner on a fly stays near first until the drop/catch.

### 9.6 Close plays

- A close play is a **geometric** condition: the throw arrives within ±`closeMargin` (0.25 s) of the runner at a tag bag (3B or home; a force is never close-played). Only then does the **mash contest** run: the icon appears, first press after the icon wins, CPU reacts at `0.20 + (10 − stat) × 0.032`. Outside the margin the geometry decides and no icon appears. ⚠️ Today the contest runs on every unforced throw to 3B/home regardless of margin (`ClosePlay.cs`, `InPlayDirector.cs:1157-1223`), and its verdict is written twice (`ClosePlaySafe` stale, `Match.cs:44, 932`).
- Stamp SAFE (small) / OUT.

### 9.7 Rundowns

- A runner caught between bags (a fielder with the ball inside 20 ft on the path, the runner not on a bag) is in a rundown: the fielder runs the runner toward the bag they came from; covering fielders take throws; the runner may reverse (stick). The tag rule (§10.3) ends it. A rundown ends by tag, by the runner reaching a bag, or by a throw that misses (runner advances).
- CPU runner in a rundown reverses each time the ball is thrown. CPU fielders throw when the runner is inside 8 ft of a covered bag and run at them otherwise; when every runner is ≥ 80% of the way to a bag the throw is a lazy lob (reference). ❌
- Runners stay on the basepath (no running off the line to dodge — the reference exploit is closed by clamping the runner to the path).

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
| Steal | §11.6 | Not in the swing function |

Reference shape for the "go" rule: keep going if time-to-bag < throw-time − 0.33 s (−0.5 s on easy), with a bonus once past 40% of the segment; otherwise turn back with a 12–20% chance of a mistake. ❌ (`OccupiedDestBag` table.) Difficulty scales thresholds ±0.15.

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

Everything else (caught stealing, picked off, doubled off, appeal) is a tag or a force. ✅ tag/force/catch exist; ❌ several are decided upstream by rolls and tables (Appendix A).

### 10.2 Arrival

`ballArrival(bag)` = release time + `throwSec` (§8.5), then the receiver must be inside 6 ft of the bag (cover). If nobody covers, the ball skips past: live. `runnerArrival(bag)` = from the runner's position and speed. Out iff `ballArrival + 0` < `runnerArrival` **and** the receiver is on the bag (force) or tags (unforced). Tie → runner. ✅ predicate (`InPlay.ForceOnBag`); ⚠️ inputs are the wrong clocks.

### 10.3 Tag geometry

- A tag is a glove with the ball inside **reach** of the runner: `TagReachFt` = 4 ft (+2 with Lick / Grow); the runner is not touching a bag (`TagSafeRadiusFt` 3.5). Home plate is a bag for a runner coming home, **not** for the batter leaving the box. ⚠️ 14 ft reach (`InPlay.cs:402`), home safe for the batter (`:416-417`).
- Human: have the ball, touch the runner (walk into them). South is not required. ✅
- The runner on a bag is safe. A runner who overran second or third is off the bag and taggable. Overrun first is protected (§9.4).

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

- The second throw is only an out if it beats the batter; a slow turn is a **fielder's choice** (one out, batter safe at first). Caption FIELDER'S CHOICE, stamp OUT. ⚠️ A synthetic DP is fabricated when no live throw happened (`Match.cs:870-879`); a retire that fails still commits the caption (`LivePlaySystem.cs:344`).
- The force at second is removed the moment the batter is retired at first; any later play on that runner is a tag.
- The receiver must be on the bag: if the cover has not arrived (slow SS), the ball waits in the air — the out is late. That is how a **fast runner beats a DP**.
- A **neighborhood play** does not exist; the foot must be on the bag (6 ft occupancy radius is the arcade tolerance).
- Mini diamond updates as each out lands. ✅

### 10.5 Doubled off and the tag-up DP

- Liner or fly caught with a runner off the bag: the fielder throws to (or steps on) that bag; if the ball arrives before the runner returns, the runner is out. This is an **appeal-less force back**. Works at every bag. S-51..S-53.
- Fly ball, runner tags and goes, throw beats them: **tag** at the next bag (never a force). S-54 (sac fly thrown out at home).
- Pop-up dropped on purpose with runners on: no infield fly rule; forced runners must go. CPU runners stay on the bag, so the drop is a force at the lead bag only. S-55.

### 10.6 When the play ends (Time)

`Time` is true when: three outs; **or** the ball is held by a fielder inside the infield (within 100 ft of the plate) and not thrown, **and** every live runner is on a bag or out, for `TimeOnBagSec` (1.0). A home run ends at the crossing plus the trot. ✅ (`InPlay.Time`) ⚠️ the "held by an infielder" clause is missing, so an outfielder holding the ball with everyone standing on bags ends the play (fine) but a runner dancing off a bag keeps it alive forever (fixed by rundown rules, §9.7).

At `Complete`: runs = runners who crossed home before the third out (with the §1 force exception), outs already recorded, bags = where each runner stands. **No table placement.** ❌ (`FinishInPlay` `AdvanceHit` / `Advance` / `AdvanceTagUp`, and three `goto case Single` reclassifications, `Match.cs:899-976`.)

### 10.7 Triple play

Three outs on one live ball by the rules above (liner, double off, double off; or force, force, tag). Stamp TRIPLE PLAY. ✅ stamp; ❌ reachable only through the label.

---

## 11. Steals and the catcher

### 11.1 Arming

- Select a runner (D-pad) and press L3 / Z — or push the stick toward the next bag — during SET or WINDUP: that runner's steal is armed (tell: a crouch and a purple STEAL pip). All-return (RB) cancels before the windup. **Any number of runners may be armed** — a double steal is two arms. ⚠️ `StartSteal` cancels every other runner (`Match.cs:338-350`).
- Targets: 1st→2nd, 2nd→3rd, **3rd→home** (D10). ⚠️ `Baserunning.StealTarget(3) == 0` (`Baserunning.cs:25`).
- A steal into an occupied bag is not offered unless that runner is also armed (double steal).

### 11.2 The jump (D2)

- Armed runners **break at release** (0.42 into the delivery) from the bag at `bagSec` speed, ×⅔ while the pitch is still in the air, full speed once it reaches the plate (reference).
- **Perfect steal**: armed inside the first 0.25 s of the windup → breaks 0.4 s before release; the pip turns gold. Arming *before* the windup (in SET) is the ordinary steal. Arming late in the windup still breaks at release.
- **Early break risk (D3)**: a runner armed in SET breaks on the pitcher's **first motion** — if that motion is a pickoff, they are caught between bags (§11.4). This is the pitcher's read on a runner who armed too early: the CPU pitcher's pickoff rate rises when it sees the pip.
- A runner returns on a foul (dead) and on a home run (trot). On a ball in play the steal simply becomes running.
- ⚠️ Today `RunnerRemainSec` models a lead as a time credit and the runner leaves at commit (`StealThrow.cs:59-64`).

### 11.3 Catcher throw play (after a take or a miss)

- The ball is in the catcher's glove at plate crossing + 0.05. The defense (human catcher seat, or CPU) arms a bag and presses South; the throw is a normal throw (§8.5) from the plate with the catcher's arm. Release delay: human = press time; CPU = `0.42 − Field × 0.014 ± 0.10`. ✅ (`StealThrow`, Unity `Phase.StealThrow`)
- The out is a **tag** at the bag: ball arrival + receiver on the bag + tag reach vs runner arrival (§10.3). With two runners stealing the catcher picks one (human) or the lead runner unless the trailing runner's margin is ≥ 0.3 better (CPU).
- On a **walk** or **HBP** the runner from 1st is entitled to 2nd — no play on them; other runners' steals are live.
- Stamp STOLEN BASE / CAUGHT STEALING. ✅

### 11.4 Pickoff play (SET)

- Pitcher throws to the armed bag. A runner on the bag is safe; no race (D3).
- A runner who broke on the pickoff motion (armed in SET) is between bags: the receiver throws ahead or chases; the runner may keep going or come back (stick). It resolves as a tag at either bag or a rundown (§9.7). ❌ Currently the steal race is reused (`Match.cs:1319-1323`), a miss awards the base (`:1376-1393`), and the cover lookup returns "" for bags 1 and 4 (`StealThrow.cs:23`).
- Pickoff at 2nd: SS covers. At 3rd: 3B. At 1st: 1B. Home: none.

### 11.5 Scenarios

| Scenario | Rule | Id |
| --- | --- | --- |
| Straight steal of 2nd, take | Catcher throw to 2B, tag | S-60 |
| Steal of 2nd, swing and miss | Same; the batter's body does not block | S-61 |
| Strikeout + caught stealing | Two outs on one pitch; stamp DOUBLE PLAY | S-62 |
| Steal of 2nd, ball four | Runner entitled to 2B; no play | S-63 |
| Steal of 2nd, ball in play | Steal becomes running; forced anyway | S-64 |
| Double steal 1st & 2nd | Catcher picks; lead runner default | S-65 |
| Double steal 1st & 3rd (delayed) | Runner on 1st goes; catcher throws to 2B → runner on 3rd may break for home when the throw passes the mound (stick); the SS/2B can cut the throw and return it home (cutoff verb) | S-66 |
| Steal of home | Catcher receives, tags; the runner needs a perfect steal and a slow pitch (changeup) to have a chance | S-67 |
| Pickoff at 1st, runner not armed | Back, no play, no stamp | S-68 |
| Pickoff at 1st, runner armed in SET | Runner broke on the motion; tag at 1B or 2B / rundown by geometry | S-69 |
| Perfect steal (armed 0.2 s into the windup), average catcher | Runner breaks 0.4 s early; safe at 2B against a Field-5 catcher, out against Field 9 with a Nice release | S-70 |
| Pickoff throw sails (bad chem) | Ball live; runner advances | S-71 |
| CPU never picks off a runner at random | | S-72 |

### 11.6 CPU steal decisions

At SET, for each runner with an open next bag: `P(steal) = base(Run) × situation`, base = 0 for Run ≤ 4, 0.06 at Run 6, 0.16 at Run 8, 0.25 at Run 10; ×1.5 with 2 outs, ×0.5 with the captain slugger up, ×0 with a runner already armed ahead of them (no double steal into a body), ×0 when trailing by ≥ 5. Perfect-steal chance 0 / 20 / 40 / 50% by difficulty (reference); otherwise the CPU arms in SET and is exposed to the pickoff. Evaluated once per at-bat (not per pitch), as a runner-AI event — not inside `CpuSwing`.

---

## 12. Chemistry, stars, items in play

Mechanics are in systems.md. The play contract:

- **Chemistry throw** modifies throw speed and accuracy (§8.5). That is its only in-play effect. A bad throw *looks* bad before it lands (muddy trail) so the room can yell.
- **Buddy Jump / Buddy Throw** are verbs with windows (§8.4, §8.7), not rolls.
- **Star meter**: gains are events (hit +0.5, extra-base +1, K +0.5, DP +1, robbed HR +1, park feature +1; `data/rules/stars.json`). Spend 1 (2 for a guest captain; a missed star swing costs 1 for everyone). ✅ (values are literals in `Match.cs`.)
- **MVP** (reference algorithm, `stars.json`): walk-off homer → hitter; walk-off hit → hitter; else points — HR 10, winning pitcher 5, go-ahead RBI 5, robbed homer / buddy jump 5, K 3, RBI 3, hit / walk / HBP / SB 1, close play won 2, item that mattered 2. ⚠️ ad-hoc points (`Match.cs:384` etc.).
- **Items** after contact (banana / rocket / POW): each is a *field* effect with geometry — a peel at a spot (a fielder who steps on it slips 0.8 s), a rocket at a body (dazed 0.8 s if hit; the fielder may smash it with North), a POW on the dirt (every ball on the ground hops once). They add time; they do not "convert an out to a single". ⚠️ Banana converts a would-be out (`Match.cs:634`, systems.md); CPU auto-throws on a 40% roll.

---

## 13. Star skills — the two-second rule

A skill bends one rule for ≤ 2 s and then baseball resumes. The bend is one of: the ball's path (element / break / decoy), the batter's window (status), the fielder's body (status / payload), or the terrain (area). Values live in `data/abilities/star-skills.json` and are **read at runtime**, not re-typed. ⚠️ JSON is dead data; `FieldAbilities.cs:117-152` and `AtBatResolver.cs:220-226` hold copies (`staff-swing` already disagrees: 1.08 vs 1.10).

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

Harbor has no hazard. Others tick hazard ids (`ParkHazards`): freeze volumes (×0.45 speed 1.2 s), warp cans (grounder exits another can), chompers (night: a fly into the mouth is a fly out), lava/fire (slow), tilt (grounders drain to a line), blackout (window ×0.85). Each is a *field* rule with geometry, documented in parks.md. The contact-window park rule must be a park data field, not a park-id string in code (`Fielding.cs:472-476` ⚠️).

---

## 15. Presentation contract per play

For each play class the camera, the stamp, and the hold are data (`data/feel/shots.json`, `table.json`). The sim emits typed `PlayEvent`s; Unity may not infer the play from caption text. ⚠️ Captions are load-bearing (`Match.cs:908, 1030-1031`).

| Class | Camera | Freeze | Stamp |
| --- | --- | --- | --- |
| Pitch / take / miss | `mound` (1P pitching) / `plate` | — | BALL / STRIKE / STRIKE OUT / WALK / HIT BY PITCH |
| Grounder | `diamond` follows the dirt | `solidFreeze` on the crack | OUT / SINGLE / DOUBLE PLAY / ERROR |
| Liner / fly | `diamond-fly` | `solidFreeze` | OUT (DIVE / JUMP) / SINGLE / DOUBLE / TRIPLE |
| Home run | `smash` override | `smashFreeze` + `smashHold` | HOME RUN / GRAND SLAM |
| Steal / pickoff | `throw` to the bag | — | STOLEN BASE / CAUGHT STEALING / PICKED OFF |
| Close play | bag cam | — | SAFE / OUT |
| Star | skill VFX, scorebug mutes 2 s | — | — |

---

## 16. Data tables (rails)

Presentation stays in `data/feel/`. Rules move to `data/rules/` with a validator in `ContentValidation` and defaults in code only as a load fallback. Proposed files and the numbers each owns (all currently hard-coded; lines in Appendix A):

| File | Owns |
| --- | --- |
| `pitching.json` | base mph per shape, Pitch coefficient, charge mph, changeup ratio, break cap (zone halves), `AirSeconds` scale and clamps, release point, stamina pool and costs, TIRED thresholds and wobble, CPU pitcher table (§4.8) |
| `batting.json` | window base and per-contact slope, charge window ×, perfect band, floor, tier boundaries, tier exit ×, tier spray spread, timing→spray, stick spray/launch degrees, oval size and falloff, bunt ×, HBP body radius, charge power curve, buddies-on-base ×, CPU batter table (§5.9) |
| `flight.json` | gravity, drag, TimeScale, bounce, roll, batted-ball class bands, fence height, foul geometry |
| `fielding.json` | chase speed, catch radius, jump/dive reach and windows, ability bonuses, throw speed and accuracy, chem ×, bobble curve, cover/cutoff rules, CPU fielder margins and reaction (§8.8) |
| `running.json` | `bagSec` curve, start delay, dash, read-step feet, slide distance, tag reach, bag radii, `TimeOnBagSec`, close margin and CPU reaction, rundown thresholds, CPU runner table (§9.9), CPU steal table (§11.6) |
| `stars.json` | meter gains per event, costs |
| `cpu.json` | difficulty multipliers applied over the tables above |

Feel values already in `table.json` that are dead or shadowed (`throwEase`, `chargeDecay`, `inPlayCommitSeconds`, `runHz`) are removed or made live in the same PR that adds `data/rules/`.

---

## Appendix A — Gap audit (code at `f09cad1`)

Grouped by the epic that fixes them (roadmap.md, Phase P). Line numbers from the two code maps taken on 2026-09-12.

### A.1 Pitch and swing contract (P1)

| # | Where | What | Spec |
| --- | --- | --- | --- |
| 1 | `AtBatFeel.cs:127-152`, `AtBatDirector.cs:109` | Sweet-spot oval has a fixed Y covering only [1.83, 2.97] of a [1.45, 3.65] zone; 3-step overlap | §5.2 |
| 2 | `AtBatDirector.cs:222-224` | Human pitch type hard-coded fastball, `AimY` 0; `curve`/`slider` unreachable | §4.1, §4.3 |
| 3 | `AtBatDirector.cs:224`, `Match.cs:667`, `PitchFlight.cs:45` | Rubber walk moves the crossing 1.35× for a human, 0.35× for CPU | §4.2 |
| 4 | `PitchFlight.cs:58-63` | Full break moves the crossing 1.8 ft (zone half-width 0.92) | §4.2 |
| 5 | `AtBatDirector.cs:245-246, 311-314` | CPU batter judges at launch, before in-flight steering | §3 |
| 6 | `AtBatResolver.cs:9-11, 48-51` | Perfect band 1.0 frame, no window floor | §5.3 |
| 7 | `AtBatResolver.cs:54-55` | Off-center Perfect demotes to Cheap | §5.3 |
| 8 | `AtBatResolver.cs:73-75` | Charge ×1.12 max; Charge Bat pinned to ×1.10 | §5.1, §5.5 |
| 9 | `ChemistryTable.cs:96-105` | Buddies-on-base × applies to every contact | §5.5 |
| 10 | `AtBatResolver.cs:13-23, 151-157` | Foul = spray > 45°; `CheapFoulPull` 40% teleport | §5.6 |
| 11 | `AtBatResolver.cs:119-122` vs `Fielding.cs:340-346` | Two homer launch bands | §5.6 |
| 12 | `AtBatResolver.cs:241-256`, `Match.cs:811-819` | HBP unreachable; `FinishHitByPitch` ignores `inZone` | §4.6 |
| 13 | `AtBatFeel.cs` vs `HomeSet.BatterWalk` | Cursor moves 1.85 ft/unit, body 2.4 ft/unit | §4.6 |
| 14 | `AtBatFeel.cs:217-221`, `AtBatResolver.cs:52` | Bunt judged at the press; bypasses the oval | §5.8 |
| 15 | `Match.cs:773-779` | Foul bunt with 2 strikes not a K; `FinishFoul` skips `AfterPitch` | §5.8, §5.6 |
| 16 | `AtBatDirector.cs:171, 173-174, 235, 288` | Stick-down both aims launch and resets the box; SET press dropped / −65-frame miss | §5.4, §3 |
| 17 | `Match.cs:558, 574` | Box walk reset every pitch | §3 |
| 18 | `Match.cs:38-39, 87, 205, 602-622, 654, 658, 1289` | Team stamina, flat costs, threshold ×4, swap +35 | §4.7 |
| 19 | `Match.cs:648-668` | CPU pitcher aims center, nested type rolls, dead `TimingErrorFrames` | §4.8 |
| 20 | `Match.cs:672-676, 698-726` | `CpuSwing` arms steals; forced \|err\| ≥ 3.2 vs a human | §5.9, §11.6 |
| 21 | `AtBatResolver.cs:232, 264-281`; `Models.cs:105, 123, 139` | Discarded `pitchStat`; 1.12 divide-out; dead `ChargePitch`, `Strike`, `TimingErrorFrames` | cleanup |
| 22 | `Fielding.cs:472-476` | Park id string special-cased for the window | §14 |
| 23 | `star-skills.json` vs `FieldAbilities.cs:117-152`, `AtBatResolver.cs:220-226`, `Match.cs:1289` | JSON dead; `staff-swing` 1.08 vs 1.10; `staminaCost` ignored | §13 |

### A.2 Flight and field (P2)

| # | Where | What | Spec |
| --- | --- | --- | --- |
| 24 | `BallFlight.cs:34` | Scalar downrange wind | §6.1 |
| 25 | `BallFlight.cs` | No fence, wall, or foul line in the trajectory | §6.1, §7.9 |
| 26 | `BallFlight.cs:68, 116-119` | Landing guards in two time bases | §6.1 |
| 27 | `Fielding.cs:327-346` | Three separate class bands | §6.2 |
| 28 | `Fielding.cs:420-432` | Positions by roster order | §8.1 |

### A.3 Runner model (P3)

| # | Where | What | Spec |
| --- | --- | --- | --- |
| 29 | `Models.cs:276-330`, `Match.cs:28-33` | No runner position/velocity/batter-runner | §9.1 |
| 30 | `Match.cs:1236-1254` | `SetBag` wipes runner state | §9.1 |
| 31 | `InPlay.cs:53-58, 82-83, 499-500` | Two speed curves; all runners move at home-to-first speed | §9.1 |
| 32 | `InPlay.cs:372-393`, `Match.cs:1139-1223` | Advance by `PlayKind` table; runner on 3rd scores on any grounder; 2nd never scores on a single | §7, §9.9 |
| 33 | `Match.cs:299-336` | Send/return only global | §9.3 |
| 34 | `Models.cs:320`, `ActorDirector.cs:360` | `Sliding` never consulted | §9.4 |
| 35 | `Match.cs:986` | Sac fly = carry > 230 literal, no throw | §7.8 |
| 36 | `Models.cs:276-330`, `Diamond.cs:46-52`, `Baserunning.cs:71-77`, `ActorDirector.cs:411-453` | Lead-off system to retire (D1) | §9.2 |

### A.4 Fielding decides by geometry (P4)

| # | Where | What | Spec |
| --- | --- | --- | --- |
| 37 | `Fielding.cs:146-161` | Infield out/hit is a stat roll | §7.1, §8.8 |
| 38 | `InPlayDirector.cs:371-403` | Glove force-fed at hang with no distance check | §8.3 |
| 39 | `Fielding.cs:113-116, 130-133, 152-160` | Drop/bobble converts out→single as a caption | §8.6 |
| 40 | `InPlay.cs:63-67`, `StealThrow.cs:42-53`, `InPlayDirector.cs:745, 862-887` | Four throw-speed formulas; verdict ignores flight time | §8.5 |
| 41 | `ChemistryTable.cs:85-94` | 25% error roll; `LateralFt` never read | §8.5 |
| 42 | `InPlayDirector.cs:182-192` vs `Fielding.cs:312-313` | Two glove speeds | §8.1 |
| 43 | `InPlayDirector.cs:340-355` | Cover speed is a Unity literal (flat is correct, D11; the number belongs in `running.json` and the start delay is missing) | §8.7 |
| 44 | `Fielding.cs:412-418` | Cutoff hard-coded SS→2B | §8.7 |
| 45 | `FieldAssist.cs:59-63`, `InPlayDirector.cs:749-768, 806-814` | Thrower teleports to the bag | §8.5 |
| 46 | `MatchDirector.cs:762-796` | Unity re-rolls the bobble with an ad-hoc seed | §8.6 |
| 47 | `InPlayDirector.cs:1225-1232` | `LiveKind` returns HR/3B/2B from carry mid-flight | §7 |

### A.5 Outs, double plays, Time (P5)

| # | Where | What | Spec |
| --- | --- | --- | --- |
| 48 | `Match.cs:870-879` | Synthetic DP from fabricated throws | §10.4 |
| 49 | `LivePlaySystem.cs:344` | `Retire` result discarded after committing caption/flags | §10.4 |
| 50 | `Match.cs:899-976` | Outcome inferred from bookkeeping deltas; three `goto case Single` | §10.6 |
| 51 | `Match.cs:908, 1030-1031` | Control flow on caption text | §15 |
| 52 | `Match.cs:44, 932`, `InPlayDirector.cs:1215` | `ClosePlaySafe` stale across plays | §9.6 |
| 53 | `ClosePlay.cs`, `InPlayDirector.cs:1157-1223` | Mash on every unforced 3B/home throw | §9.6 |
| 54 | `InPlay.cs:402, 416-417` | Tag reach 14 ft; home is a safe bag for the batter | §10.3 |
| 55 | `LivePlaySystem.cs:307-313` | `ThrowArrived` sets HasBall/CatchMade unconditionally | §10.2 |
| 56 | `InPlayDirector.cs:150-152` | Rest fallback bypasses `CommitInPlay` | §10.6 |
| 57 | `Match.cs:556-559` vs `:575-584` | Walk-off only on the take path | §1 |
| 58 | `PlayStamp.cs:29` | Triple play is a label only | §10.7 |
| 59 | — | Extra innings, mercy, ground-rule double, ERROR stamp, foul fly catch, rundown missing | §1, §7.11, §9.7 |

### A.6 Steals and pickoffs (P6)

| # | Where | What | Spec |
| --- | --- | --- | --- |
| 60 | `Match.cs:338-350`, `Baserunning.cs:25, 46-49` | Double steal impossible; no steal of home | §11.1 |
| 61 | `StealThrow.cs:59-64`, `ActorDirector.cs:441-446` | Lead as a time credit; runner leaves at commit; no perfect steal | §11.2 |
| 62 | `Match.cs:480-503, 1376-1393` | Pickoff arms a steal; a miss awards the base | §11.4 |
| 63 | `Match.cs:1421-1481` | Random pickoffs on every pitch (≤ 72%) | §4.5, D3 |
| 64 | `Match.cs:1319-1323`, `StealThrow.cs:96-115` | Pickoff resolved with the steal race; real pickoff race unreachable | §11.4 |
| 65 | `StealThrow.cs:23` | No cover at bags 1 and 4; chem computed defender-vs-runner | §11.4 |

### A.7 Architecture (P0 / #512)

| # | Where | What |
| --- | --- | --- |
| 66 | `InPlayDirector.cs` (1249 lines) | Relay chain, close play, CPU catch timing, steal phase, glove speeds are Unity-side baseball |
| 67 | `data/feel/table.json` | `throwEase`, `chargeDecay`, `inPlayCommitSeconds` read only by tests; `runHz` shadowed by `Motion.RunHz` |
| 68 | everywhere in §16 | ~150 rule constants in C# with no data hook |

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
| S-07 | Bat 5 slap, ball at the cursor center, err 0 | | Perfect, straight to CF ± 8° |
| S-08 | Bat 5 slap, cursor center, err −4 frames (early, inside the 9-frame window) | | Perfect, pulled ≈ 45° |
| S-09 | Bat 5 slap, cursor center, err +5 frames (outside the window) | | Miss |
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
| S-29 | 3-inning CPU-vs-CPU, 50 seeds | `cli match` | Mean runs per side 2–5; doubles < singles; HR ≤ 2 per game mean |
| S-30 | Charge Bat vs manual MAX | | Charge Bat ≥ manual MAX power, no window penalty |

### B.2 Grounders and fielding

| Id | Setup | Input | Expect |
| --- | --- | --- | --- |
| S-31 | Nobody on, routine grounder to SS, Run 5 batter | CPU | Force at 1B, out; throw arrival < runner arrival by geometry |
| S-32 | Same, Run 10 batter, SS Field 2 | CPU | Infield single (arrival compare), no roll |
| S-33 | Same as S-31, human SS never throws | Dead stick then no South | CPU runs the glove; ball scooped; no throw unless the human presses South; batter safe when Time |
| S-34 | Grounder to SS, human arms 3B with nobody on, throws | | Ball to 3B; batter safe at 1B; caption names the wasted throw |
| S-35 | Bad-chem throw to 1B, σ big | 100 seeds | Some throws miss the cover by > 6 ft → live, ERROR, batter to 2B |
| S-36 | Runner on 2nd, grounder to 3B in front of them | CPU runner | Runner holds; 3B throws to 1B |
| S-37 | Runner on 2nd, grounder to 2B behind them | CPU runner | Runner goes to 3B if margin > 0.4 |
| S-38 | Runner on 3rd, infield in, grounder to SS, < 2 outs | CPU runner | Holds; SS throws to 1B |
| S-39 | Runner on 3rd, 2 outs, any grounder | CPU runner | Goes on contact |

### B.3 Double plays (§10.4) — S-40 … S-50 as tabled, each asserting: two outs only if both arrivals win; one out + FIELDER'S CHOICE otherwise; force removed after the batter is retired first (S-43, S-48 are tags).

### B.4 Flies, liners, tag-ups

| Id | Setup | Input | Expect |
| --- | --- | --- | --- |
| S-51 | Runner on 1st sent on contact, liner to SS caught | CPU SS | SS steps on 1B or throws: runner doubled off if arrival wins |
| S-52 | Runner on 2nd off the bag, liner to CF caught, throw to 2B | | Doubled off / safe by arrival |
| S-53 | Runner on 3rd holding on the bag, liner caught | | One out; runner stays |
| S-54 | Runner on 3rd tags on a 220 ft fly to LF (arm Field 9), goes | | Throw home; out or safe by arrival; a close-play icon only if within 0.25 s |
| S-55 | Bases loaded, pop to SS dropped on purpose | CPU runners | Runners on bags; force at home only |
| S-56 | Fly 12 ft over the fence, CF Super Jump in the window at the wall | West | Robbed, out |
| S-57 | Same, no ability | West | Home run |
| S-58 | Fly hits the wall 8 ft up | | Carom; live; batter to 2B by geometry |
| S-59 | Bounce then over the fence | | Ground-rule double: every runner +2 |

### B.5 Steals and pickoffs — S-60 … S-72 as tabled in §11.5.

### B.6 Close plays, rundowns, Time, scoring

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
