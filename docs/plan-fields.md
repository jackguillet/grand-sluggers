# Fields decision plan

Tracker: [#814](https://github.com/jackguillet/grand-sluggers/issues/814), serving #209 and building on [#713](https://github.com/jackguillet/grand-sluggers/issues/713) (the park override rail). Session kind: **Gameplay research/documentation**. Baseline: `d0c6e12c`. Research: [research-fields.md](research-fields.md). Code maps: [sim](research/fields-code-map-sim.md), [presentation](research/fields-code-map-presentation.md). Measured baseline: [fields-park-baseline.json](research/fields-park-baseline.json). Canonical structured record: [fields-decisions.json](research/fields-decisions.json).

## Current state

Research and maps are done. **FD-01 is accepted: rails first, proven on one park, then the other parks one at a time as greyboxes; no park art. FD-02 is accepted: a park's effect is noticeable, in a direction it declares first. The engineering rails are accepted as a set: FD-09 B (hazard pattern library), FD-12 B (diamond-relative positions), FD-16 B (one field kit with slots), FD-17 C (greybox first, one park in art at a time). FD-03 is accepted: a park may override the ball's environment (the #713 list); gravity, the time scales, the plate and the infield stay global. FD-04 is accepted: the ground has a small effect on bodies with control kept (B); full traction (C) is held as a trial candidate for the greybox sitting. FD-05 is accepted: the ground is a map of zones from the shared diamond (B). FD-06 is accepted: the fence may be a free polyline with a height per point and a material per span (C); the three-post arc stays the default. The other 9 decisions are open. Nothing is implemented. No number is accepted.** Next: FD-07. Jack's brief, September 21, 2026: treat Harbor as the default; give the other fields a unique look, possible hazards, and qualities (size, air density, ground material, slickness); build the rails and the engineering process before the artwork.

This plan follows the #693 and #803 pattern: stable ids, options, a recommendation, a scoped human choice, then evidence. It keeps one lesson from both: **ask about material tradeoffs one at a time, and do not ask Jack to approve routine derivations.**

## How we use this together

1. We discuss one decision at a time, in the order below. Jack answers with an option id or his own words.
2. The answer goes into the JSON: option, accepted scope, date, author, and the quote. A recommendation is never a selection.
3. History is appended, never erased. A replaced choice is marked `superseded` with a link.
4. A direction selects intent only. `numeric_targets` stays `null` until a scoped trial is accepted with units, conditions and limits.
5. This file, the JSON and the tracker stay in step. The spec ([gameplay-spec.md](gameplay-spec.md) §6, §14, §16) stays the shipping contract; a changed rule is reconciled there before code.
6. An implementation child is filed only when its contract is ready. It names its decision ids, its rails (FR ids), its scenario ids and its banned files.

Status: `open → direction-accepted → trial-accepted → implemented → validated → human-accepted`, plus `deferred`, `rejected`, `superseded`. Automated checks never fill `human_acceptance`.

## The frame

**A park is Harbor plus the differences it names.** Harbor names none. It stays the control park and the only calibrated park until a decision says otherwise (FD-13).

Three findings from the maps set the work:

| # | Finding | Evidence |
| --- | --- | --- |
| 1 | Today a park can change only its three fence posts, its fence height, its wind, one night number, and its hazards. `surface` changes no play. All ball physics and all body motion are global. | [sim map](research/fields-code-map-sim.md) §1–§2, §5 |
| 2 | Hazards are older than Phase P. Each is decided once, at the preview, from the ball's landing point. No body ever touches one. The warp exit and the frozen drop are rolls. Four of eleven types do nothing. Chompers are a code literal behind a park-id string. | sim map §3 |
| 3 | There are two diamonds on screen. Harbor draws the kit. The other five draw an older primitive diamond with different bags, dirt, chalk and mound, and they do not draw the foul rail the ball hits. Looks are five private `ParkView` methods with literal positions and colors. | [presentation map](research/fields-code-map-presentation.md) §1–§2 |

One measurement sets the stakes. With the same matchup and seeds, **today's parks already move runs by up to 1.35× Harbor and home runs by up to 1.45×**, and nobody chose those numbers. S-29 pools all six parks, and 70 % of its games are not at Harbor.

| Park (shipped root) | Runs / game | × Harbor | HR / game | × Harbor | 3B / game | Ground-rule 2B / game |
| --- | --- | --- | --- | --- | --- | --- |
| Harbor Diamond | 3.90 | 1.00 | 1.24 | 1.00 | 0.30 | 0.10 |
| Crystal Rink | 5.10 | 1.31 | 1.80 | 1.45 | 0.26 | 0.80 |
| Funfair Park | 5.28 | 1.35 | 1.78 | 1.44 | 0.28 | 0.58 |
| Rooftop City | 4.04 | 1.04 | 1.42 | 1.15 | 0.14 | 0.14 |
| Canopy Yard | 4.02 | 1.03 | 1.58 | 1.27 | 0.10 | 0.12 |
| Ember Keep | 4.34 | 1.11 | 1.28 | 1.03 | 0.60 | 0.38 |

Fifty three-inning day games per park, CPU both sides. Read a factor within about 0.15 of 1.0 as noise. The C80 rows and the limits are in the [baseline file](research/fields-park-baseline.json). The two 8-ft parks give six to eight times Harbor's ground-rule doubles; that is the clearest single effect.

## Lever matrix

What a field can vary, where it lives today, and where this plan proposes it lives. "Decides" names the decision that opens the lever.

| Lever | Today | Consumer today | Proposed home | Decides |
| --- | --- | --- | --- | --- |
| Fence distance (L / C / R) | per park | `AtBatResolver.FenceAt` | unchanged | — |
| Fence height | per park, one number (D15) | `FieldBounds`, `BattedBall.FenceClearFt` | per fence point (FD-06 C); D15 is reconciled first | FD-06 ✅ |
| Fence shape | circle through three posts | `AtBatResolver.RoundFence` | a free polyline, pole to pole; the three-post arc is the default | FD-06 ✅ |
| Wall material (carom, climbable) | global `flight.wall`; `climb_wall` is a park-wide flag | `BallFlight.cs:183`, `ParkHazards.CanClamber` | wall-material row per fence span | FD-06 ✅ |
| Wind speed and direction | per park | `BallFlight.cs:30` | unchanged | — |
| Wind exposure (`windMul`) | global 0.35 | `BallFlight.cs:44` | park environment | FD-03 |
| Air density (drag) | global 0.0019 (C80 0.0040) | `BallFlight.cs:118` | park environment, as a multiplier on the root's drag | FD-03 |
| Ground roll (friction, rest speed) | global 22 / 1.4 | `BallFlight.cs:90` | ground row | FD-03, FD-05 |
| Ground bounce (restitution, horizontal, skid) | global 0.48 / 0.82 | `BallFlight.cs:159` | ground row | FD-03, FD-05 |
| Loose-ball grounds (overthrow decel, bobble decel) | global, two more models | `LivePlaySystem.Field.cs:3059-3126` | read the same ground row | FD-05 |
| Body traction (brake, turn, slide) | global (#718 response law, `running.bags`); the law is off on the shipped root (0 / 0) | `LivePlaySystem.Field.cs:2231` | ground row: small multipliers on the response law and on slide / overrun (FD-04 B). Full traction is a held trial | FD-04 ✅ |
| Foul territory (offset, flare, rail height, backstop) | `HarborWall` literals for every park | `FieldBounds.cs:105-188` | park boundary parameters | FD-07 |
| Outfield depth (fielder starts) | one global set | `Diamond.Positions` | stays global unless FD-07 says C | FD-07 |
| Hazards | per park, eleven types, thin | `ParkHazards` (`Fielding.cs:645`) | pattern library + park instances | FD-08, FD-09, FD-19 |
| Hazards on or off | always on | — | a match option (an empty instance list) | FD-10 |
| Night | one park number + two code rules | `Match.Night` | declared night block | FD-11 |
| Gravity, time scales, plate, infield, foul-line angle | global | — | **stay global** | — |
| Look: kit, ground, wall skin, backdrop, light, sky, audio | code per park id | `ParkView`, `Look.Rig*` | kit slots in `data/art/parks.json` | FD-16 |

## Rails

The engineering contract. These are patterns the repo already uses; each row names the precedent. Jack accepted the four rail decisions as a set on September 21, 2026 (FD-09, FD-12, FD-16, FD-17). The table is the working contract for implementation children; a rail that depends on an open decision (FR-07 on FD-08, FR-10's band on a trial) waits for it.

| Id | Rail | Precedent | Falsifier |
| --- | --- | --- | --- |
| FR-01 | **Override with fallback.** A park file names only what differs from Harbor. The match plays on one resolved table: `content.Rules.AtLevel(difficulty).AtPark(park)`. No consumer reads a park field to pick a physics number. | `RulesTable.AtLevel` (`Rules.cs:38`), the one derived table a match already has | A test proves Harbor's resolved table equals the global table bit for bit. S-29, the Harbor cohorts and the seed-7 game do not move in the parity PR. |
| FR-02 | **Closed libraries, authored rows, no silent fallback.** Ground ids, wall-material ids and hazard types are closed sets. Each id has one authored row. An id with no row stops the load and names the id. | Pitch families, D20 (#810) | `cli art` fails on an unknown id and on an id with no row. |
| FR-03 | **Strict park schema.** Unknown park fields are refused, like the rule tables. Dead fields are removed or wired (`notes`, `nightOnly`, `dayOnly`, `periodSec`, `tag`, `faction`). | `RulesValidation.UnknownFields` (`Rules.cs:111`) | A misspelled field fails the load. |
| FR-04 | **No park id in code.** No rule, light, color, copy line or list is chosen by an id string. Lists of parks come from the catalog. | Spec §14 already says it; `ParkHazards.ChompFly` and `ParkView` break it | A source test finds no park-id literal outside data and tests. |
| FR-05 | **One geometry owner, no Harbor name on a shared rule.** The boundary (fence, foul wrap, rail, backstop) is a park-neutral type built from park parameters. Harbor's values are the defaults. Sim and kit read the same type. | F693 "one geometry owner feeds sim and kit"; D15 | The drawn wall equals the flight wall on every segment, in every park, asymmetric parks included. |
| FR-06 | **Parity first.** Every rail lands with no behavior change. A number moves only in a later PR that a decision authorized. | #710 → #711 parity slice; P1-a / P1-b | A parity PR changes seals only. |
| FR-07 | **Hazards are live geometry.** A hazard acts on the thing that touches it (this ball, this body), when it touches it, for a stated time. No play-wide flag from a landing point. No roll. Each effect emits a typed event. | AGENTS.md "decided by geometry, never by a roll"; typed `PlayEvent`s (P0) | One scenario per pattern proves the outcome from positions. Unity reads the event, not a caption. |
| FR-08 | **Patterns, not one-offs.** The sim implements a few hazard patterns. A hazard type is a data row that picks a pattern. A ninth park needs rows, not code. | "Rails, not patches": the next park must not invent it again | A new type that fits a pattern lands with zero sim code. |
| FR-09 | **Both roots, one authoring.** Park positions are written relative to the diamond and the fence, so the shipped root and a trial root place a hazard in the same spot. A shipped park edit that leaves the trial behind fails by name. | #716 whole-file rule; #730 / #732 (absolute feet that do not scale) | `CompactGeometryTests` key-equality stays green; no hazard coordinate is a second copy. |
| FR-10 | **Measure every park against Harbor.** `cli match --cohort park-factors` (from `tools/park-factors.py`) reports run and home-run factors on predeclared seeds, both roots. Each park declares its intent before it gets numbers. | Harbor cohorts (F693); "no expectation edited to make it pass" | A park PR carries the report. A factor outside the accepted band (FD-02) is a finding, not a tuning target for that PR. |
| FR-11 | **Lever probes.** Each lever has a fixed-input row: the same ball at Harbor and at the park, one difference, asserted. | `ParkSlowRowsTests` row pattern | A lever with no row is not shipped. |
| FR-12 | **Seat parity.** The CPU plans on the same resolved park the ball flies in. Every hazard scenario runs on the CPU seat and on the human seat. | P5 / P6 scenario pairs | Paired rows agree. |
| FR-13 | **One field kit, slots per park.** One park-neutral builder draws the diamond, lines, bags, mound, wall and rail from the geometry owner. A park fills named slots. Empty slots draw a complete greybox from data. The `ParkView` fallback diamond and the per-park methods retire. | art-rails.md rule 4; `HarborKit` | Harbor fills the slots with no visual change first. Every park then draws the same bags, chalk and rail. |
| FR-14 | **Greybox before art, one park at a time.** Order per park: rules green → greybox playable → greybox sitting (Jack) → DCC stages → dual stills. Stills can request a park and night. | #37; dcc-stages and dual-stills (Harbor lane only today) | `StillRequest` carries `park` and `night`. The stage catalog has a park lane. No mesh is commissioned before the greybox sitting. |
| FR-15 | **Tell, stamp, lesson.** Every park rule that can decide a play has a tell before it acts, a typed event when it acts, and a Practice lesson. | AGENTS.md "Tutorials grow with gameplay"; `PlayStamp` | `cli tutorials` lists a lesson per hazard pattern and per ground that changes the ball. |
| FR-16 | **Seals move on purpose.** A PR that touches a sealed source says so, regenerates in the known order (flight probe `--write`, then `compact-field-report.py`), and states that only hashes moved. `Park` gains members with defaults so positional callers still compile. | #708 packet mechanics; PR #736 | Both `--check` runs pass in CI. |

## Park identity matrix

**Proposal only.** It shows that the levers can give each park a character. Every cell is a direction, not a number. "=" means Harbor's default. Nothing here is accepted until its decision is, and FD-02 bounds how far any cell may push results.

| Park | Size | Air | Ground | Wall | Hazard pattern | Night | Intent against Harbor |
| --- | --- | --- | --- | --- | --- | --- | --- |
| **Harbor Diamond** | 330 / 400 / 330, symmetric | = (sea breeze out to right-center) | grass, dirt infield | 12 ft padded | none | look only | the reference: 1.00 |
| Crystal Rink | a little short, symmetric | cold and dense: less carry | ice: the ball runs long and stays low | 8 ft glass boards: lively carom, more ground-rule doubles | status volume (freezers) | blackout: tighter contact window | fewer home runs, more balls to the wall, doubles |
| Funfair Park | lopsided: short left, deep right | wind out: more carry | grass | 8 ft, striped | ball redirect (warp cans); timed mover (train) | catch stealers (chompers) | high-scoring, pull-side home runs |
| Rooftop City | short lines | thin air and a strong crosswind | tar: hard, fast, high hop | 12 ft billboards as a tall segment; reward targets | solid bodies (AC units) | look only | home runs and odd caroms; wind reads |
| Canopy Yard | smallest | humid and heavy, little wind | soft dirt: slow roll, dead bounce | 12 ft vine wall, climbable segment | ball redirect (barrels); solid bodies (trees) | look only | few extra-base hits on the ground; robbed home runs |
| Ember Keep | deepest | hot and thin: more carry into a deep park | ash: slow roll, dead bounce | 10 ft stone: hard carom | status volumes (lava, breath) | breath reaches farther | triples and gap hits, few home runs |
| Haunt Manor, Cruise Deck, Playroom | — | — | — | — | input disruption, tilt, smear | — | deferred: each needs a pattern the library does not have (FD-09) |

## Hazard pattern matrix

| Pattern | Acts on | Types today | What the sim does today | What the pattern needs |
| --- | --- | --- | --- | --- |
| Status volume | a body inside it, for a stated time | `freeze_volume`, `lava_pit`, `fire_breath` | landing point in the disc slows **every** chaser for the **whole** play (×0.45) and adds a 0.4 drop roll | per-body touch test in the tick, a duration, no roll, CPU route cost, a tell on the body |
| Ball redirect | a ball on the ground that enters it | `warp_pipe`, `barrel` | moves the preview landing to a **random** other can; the live ball does not move | live entry, a told exit, exit speed and direction, the ball re-enters play as a typed event |
| Solid body | the ball (carom) and bodies (route around) | `ac_unit`, `tree`, `statue` | nothing | a collider in the flight clip and in the route planner |
| Timed mover | the ball and bodies, on a clock | `train` | nothing; `periodSec` is not read | a position from the play clock, told ahead |
| Catch stealer | a ball in the air | chompers (code literal, Funfair id) | a fly that lands in the disc at night is an out | data instances, a tell, and a decision on whether an out with no glove fits the game |
| Reward target | the batting team's stars | `billboard` | landing in the disc gives a star; `tag` is ignored | the target on the wall segment it is drawn on |
| Wall trait | a fielder at the wall | `climb_wall` | a park-wide flag: +6 ft catch radius anywhere, rob to 28 ft | a property of a wall segment (FD-06), not a hazard |
| Visibility | the batter's window | `nightContactWindowMul` | one park number at night | a night-block field (FD-11) |

## Gap audit

Grouped by the epic that fixes it. Lines are from the maps at `d0c6e12c`.

**F1 — Schema and catalog (Gameplay).** Unknown park fields are dropped silently (`ContentValidation.cs:42-47`). `notes`, `nightOnly`, `dayOnly`, `periodSec` have no C# member. `faction` and `tag` are read by nothing. The park list is a code literal in five places (`ExhibitionPick.cs:10`, `Teams.cs:37`, `CarnivalFront.cs:229`, `tools/game-feel-scale-probes/Program.cs:19`, `HarborWallTests.cs:16`). An unknown park id falls back to Harbor silently (`Match.cs:151-186`). docs/parks.md:80 describes a warning channel that does not exist.

**F2 — Geometry owner (Gameplay, then Presentation).** `FieldBounds.Build` takes the foul wrap, rail and backstop from `HarborWall` literals (`FieldBounds.cs:105-188`, `HarborWall.cs:21-32,114,183`). The validator ties every park's fence to Harbor's 4.2-ft rail (`ContentValidation.cs:317`). The drawn wall mirrors right field onto left (`HarborWall.cs:99-101`); three parks are lopsided. The drawn foul rail ramps to fence height while the sim rail stays 4.2 ft to the pole (`HarborWall.cs:177-191` vs `FieldBounds.cs:159`). The `FieldBounds` cache key lacks any new boundary input (`FieldBounds.cs:115`). #732 already owns the 36-ft and 95-ft absolutes under #730: **coordinate, do not collide.**

**F3 — Environment table (Gameplay).** All of `flight.json` is global (`BallFlight.cs:79-194`). Three separate ground models exist (`flight.roll`, `overthrow.decelFtPerSec2`, `handling.bobble*`). `Diamond`, `ParkDiamond` and `RunnerAi.cs:82` read `Rules.Default`, not the match's table, and 118 `Rules.Or(rules)` call sites fall back to it; any of them would play Harbor's air in another park. The grass / dirt split is one radial lip (`Fielding.cs:332`).

**F4 — Hazard runtime (Gameplay).** Every row of the pattern matrix. Also: `ParkHazards.ChompFly` tests `park.Id` (`Fielding.cs:679`); `EmberNightFireMul` is a fielding rule with a park's name; `drops.frozen` is a roll (`Match.cs:702`); `WarpIfPipe` rolls the exit (`Fielding.cs:701`); Unity reads none of `Frozen`, `Warped`, `Chomped`. Spec §14 says "1.2 s" and "tilt"; neither exists.

**F5 — Measurement (Gameplay).** S-29 pools six parks with no per-park band (`AtBatScenarioTests.cs:690`). The CLI has no night flag, so no gate measures a night rule. There is no park-factor cohort.

**F6 — Field kit (Presentation).** Two diamonds (`ParkView.cs:181-273` vs `HarborKit`). Five per-park dress methods and eleven light rigs chosen by id (`ParkView.cs:48-173`, `Look.cs:296-484`). The foul rail is not drawn outside Harbor. `StarMeter` stands on Harbor's dugout in every park (`StarMeter.cs:24`). Rooftop draws two AC units that are not in data (`ParkView.cs:829`). Warp and barrel draw the radius but the sim reach adds an 8-ft pad nobody sees. `data/art/parks.json` rows are `{id, slot, placed}` only; `cli art` checks only that a row exists (`Art.cs:237`).

**F7 — Look gates (Presentation / Art).** `StillRequest` has no `park` and no `night`. The stage catalog and the dual-stills catalog have a Harbor lane only, enforced by their validators (`DccStages.cs:59-92`, `DualStills.cs:17-34`). `harbor_kit.py` reads no data and seven of its constants have drifted (`WALL_H 26` against the sim's 12). No debug-protocol row covers a non-Harbor park. The `field` shot aims at z 380 and is tested at Harbor only.

**F8 — Legibility (Presentation + Gameplay setup).** Field pick copy is a per-id switch (`CarnivalFront.cs:225`). No hazard has a tell, a stamp or a lesson; docs/tutorials.md lists park hazard lessons as blocked by #37.

## Epic sequence

Serial by default. Each epic is filed only when the decisions it needs are accepted. Scenario ids for this phase use the block `SF-01 …` so they cannot collide with the pitching and hitting work.

| Epic | Kind | Delivers | Needs | Rails |
| --- | --- | --- | --- | --- |
| **F0** Reconcile the standing orders | docs | Scope line in AGENTS.md, roadmap.md and art-rails.md: **done with FD-01**. Still owed: the parks.md and spec §14 corrections listed in the research report | FD-01 ✅ | — |
| **F1** Schema and catalog | Gameplay | strict park schema, dead fields resolved, park list from the catalog, unknown id is an error | FD-01 | FR-03, FR-04, FR-06, FR-16 |
| **F2** Geometry owner | Gameplay | park-neutral boundary type; Harbor's numbers as defaults; lopsided parks draw true; parity. Then the polyline fence: one distance per bearing, vertices kept in the clip polygon, height per point, material per span; D15 reconciled in the spec first | FD-06 ✅, FD-07; coordinate #732 | FR-05, FR-06 |
| **F3** Environment table | Gameplay | `AtPark`; ground and wall-material libraries, ground rows carrying ball fields and body multipliers; every park still names nothing; then one lever at a time as a trial. The body effect needs the response law on (C80 today) | FD-03, FD-04, FD-05 | FR-01, FR-02, FR-06, FR-11 |
| **F4** Hazard runtime | Gameplay | pattern library; live touch tests; typed events; CPU reads hazards; rolls removed | FD-08, FD-09, FD-10, FD-11, FD-14, FD-19 | FR-07, FR-08, FR-09, FR-12 |
| **F5** Measurement | Gameplay | `park-factors` cohort, night flag in the CLI, declared park intents | FD-02, FD-13 | FR-10 |
| **F6** Field kit | Presentation | one builder, kit slots, Harbor refilled with no visual change, data-driven greybox for every park, light and sky as data | FD-16 | FR-13, FR-04 |
| **F7** Look gates | Presentation / Art process | park and night in `StillRequest`, park lane in stages and dual stills, named park shots, greybox sitting checklist | FD-17 | FR-14 |
| **F8** Legibility | Presentation + Gameplay setup | field card from data, tells and stamps from typed events, a lesson per pattern and per ground | FD-15 | FR-15 |
| **F9** The proving park | Gameplay, then Presentation, then Art | the first non-Harbor park through every gate, with no code that names it | FD-18, FD-02 | all |

F1, F2 and F5 can run beside the pitching and hitting children if their file lists do not overlap (`Models.cs`, `Rules.cs`, `Match.cs` are shared: serialize those). F6 and F7 touch no sim rule and can start after F2. Park art starts only inside F9, after the greybox sitting.

## Standing orders this plan touches

| Order | Where | What the plan does |
| --- | --- | --- |
| "Do not start: extra parks as products (#37)" | AGENTS.md, roadmap rule 4 | Kept. FD-01 (accepted) names rails and greybox as allowed work, one park at a time. Park art stays behind #37 and F9's gates. |
| "Other parks stay JSON until Exhibition is the reason people stay" | AGENTS.md Art | Same. The plan makes the JSON true and the greybox honest; it commissions no mesh. |
| Phase D order: Crystal as a kit, then one gimmick park | roadmap.md | FD-18 recommends the same first park. |
| #713 sequencing: "after the compact contract is validated" | #713 | Parity rails (F1, F2, F3 parity, F6, F7) move no number and can precede 3e. Any park number waits for FD-13. |
| #730 / #732 own hazard radii, `FoulOffset`, `flareStart` | #730 boundary | F2 and F4 are filed with those numbers banned until #732 closes, or as its named successor. |
| Sealed evidence | #708 packet, CI `--check` | FR-16. |

## Discussion order

- **Round 1 — scope and strength:** FD-01, then FD-02. These decide whether the rest is worth asking.
- **Round 2 — the engineering rails:** FD-09, FD-12, FD-16, FD-17. None changes how the game plays. One answer can accept all four recommendations.
- **Round 3 — what a park may change:** FD-03, FD-04, FD-05, FD-06, FD-07.
- **Round 4 — hazards:** FD-08, FD-19, FD-10, FD-11, FD-14.
- **Round 5 — learning and order:** FD-13, FD-15, FD-18.

## Decision register

FD-01 to FD-06, FD-09, FD-12, FD-16 and FD-17 are **DIRECTION ACCEPTED**. The other 9 are **OPEN**. The recommendation is the author's proposal. Only Jack's recorded answer selects an option. Each acceptance line is a proposed falsifier, not a passed gate. Source ids resolve in the [research report](research-fields.md#sources).

### FD-01 — What does the fields phase authorize?

**Decision — Jack, September 21, 2026: A, then B.** "A first, then B park by park." Build the rails at Harbor parity and prove them on one second park as a greybox. Then bring the other listed parks to a playable greybox one at a time. No park art: it stays behind #37, Phase T and each park's greybox sitting. This selects no first park (FD-18), no lever, no hazard rule and no number. The scope line is in [AGENTS.md](../AGENTS.md), [roadmap.md](roadmap.md) and [art-rails.md](art-rails.md). Full provenance is in the canonical JSON.

Area: Direction. Depends on: none. Evidence: MW-MSS, SMB-TG, MH-STAD.

- **A — Rails only, proven on one park:** Build the park rails at Harbor parity. Prove them on one second park as a greybox. No park art. Smallest scope; one proof may miss a rail a different park needs.
- **B — Rails, then every listed park as a playable greybox:** Build the rails. Give all six park files their qualities and hazards in the sim. Draw each as a data-driven greybox. Art stays behind #37 and Phase T. More balance work before C80 is promoted.
- **C — Rails and park art together:** Parks become products now. Conflicts with AGENTS.md, roadmap Phase D and #37.

**Recommendation:** A first, then B park by park. Rails and greybox only. AGENTS.md keeps 'extra parks as products (#37)' on the do-not-start list; this phase changes that line only to name the rails work as allowed.

**Existing contract:** AGENTS.md 'Do not start: extra parks as products (#37)'. Roadmap rule 4 and Phase D ('three good parks beat six ugly ones'). #713 is the existing park override rail issue. art-rails.md rule 4: parks are kits, other parks wait for #37.

**Acceptance:** A written scope line in AGENTS.md and roadmap.md that an agent can obey without asking. No park art is commissioned by this decision.

### FD-02 — How strong may a park's effect on results be?

**Decision — Jack, September 21, 2026: B.** Reply "b." selects a noticeable park effect in a direction each park declares before it gets numbers: about 25 % on runs and about 50 % on home runs is the intended size. Those figures describe the direction; they are not accepted numeric targets. Exact bounds, per-event bands, seeds and cohort size stay open until a scoped trial. Today's unplanned factors (Crystal 1.31, Funfair 1.35) are findings, not targets, and no park is tuned now (FD-13). Full provenance is in the canonical JSON.

Area: Direction. Depends on: FD-01. Evidence: SAVANT-PF, NATHAN-SC, MH-STAD.

- **A — Subtle:** A park moves runs and home runs by about 10 percent against Harbor. Safe for balance. A player may not feel the park.
- **B — Noticeable:** A park moves runs by up to about 25 percent and home runs by up to about 50 percent, in a named direction. Real MLB parks span 0.83 to 1.25 on runs and 0.76 to 1.27 on home runs; 22 of 28 sit within 6 percent on runs. A player changes tactics by park.
- **C — Wild:** A park may double a rate. Parks feel like modes. Every balance gate becomes per-park.

**Recommendation:** B, with the direction declared per park before any number is chosen. The band is measured as a park factor against Harbor on the same seeds.

**Existing contract:** S-29 is a mixed-park guardrail (1.8–5 mean runs per side, F693); 35 of its 50 games are not at Harbor. Harbor cohorts are the calibration reference. No per-park band exists. Measured today, unplanned: Crystal 1.31 and Funfair 1.35 on runs, 1.45 and 1.44 on home runs (docs/research/fields-park-baseline.json).

**Acceptance:** Each park has a declared intent (for example 'fewer home runs, more triples') and a measured factor that lands inside the accepted band on predeclared seeds. S-29 and the Harbor cohorts stay green.

### FD-03 — Which physical qualities may a park override?

**Decision — Jack, September 21, 2026: A.** Reply "a" selects the #713 list: air drag, wind exposure, roll friction, rest speed, bounce and wall carom, as overrides with fallback to the global table. Gravity, the time scales, the plate, the foul-line angle and the infield stay global. Body traction and outfield depth are not opened here (FD-04, FD-07). No value is selected; the #702 race rows are re-run per park before any number is accepted. Full provenance is in the canonical JSON.

Area: Environment. Depends on: FD-01. Evidence: NATHAN-SC, NATHAN-CARRY, MH-STAD, SHOW-SZ.

- **A — The #713 list:** Air drag, wind exposure, roll friction, rest speed, bounce, wall carom. Gravity and the time scales stay global.
- **B — The #713 list plus bodies:** Also fielder and runner traction (FD-04) and outfield depth (FD-07).
- **C — Anything in flight.json:** Maximum freedom. A park could change gravity or the play clock. Breaks the one-clock rule.

**Recommendation:** A as the first slice, B only through FD-04 and FD-07. Gravity, timeScale, linerTimeScale, dirtTimeScale, plate geometry and the infield stay global in every option we recommend.

**Existing contract:** #713 names the candidates in data/rules/flight.json. The GameCube game varied first-bounce and rolling factors by park (MH-STAD). Spec §16: the infield is one global set; 'outfield depth, the foul area and the park environment vary per park (#713)'.

**Acceptance:** A park file names only what differs from Harbor. A test proves Harbor's resolved environment equals the global table bit for bit.

### FD-04 — Does a slick or soft ground act on bodies, or only on the ball?

**Decision — Jack, September 21, 2026: B, with C held for the sitting.** First reply: "I think B, maybe even C." After the B / C comparison, reply "b" selects a small body effect: a ground row may scale the fielder's start, brake and cut-back through the #718 response law, and the runner's slide and overrun. The body always goes where the stick points; a routine grounder stays a routine out in every park. **C (full traction) is a named trial candidate, judged by feel at the proving park's greybox sitting**; the ground row is built so C is an increment on the same rail. Dependency: the shipped root has `chase.accelSec` 0 / `brakeSec` 0, so the body effect exists only where the response law is on (C80 today). "Players do not skate" and the #693 reliable-routine-defense anchor stay in force. The author's recommendation was A. No multiplier is selected. Full provenance is in the canonical JSON.

Area: Environment. Depends on: FD-03. Evidence: MW-PIG, BYB-WP.

- **A — Ball only:** Ice makes a grounder run long. Fielders and runners move the same in every park. Keeps the accepted movement contract (#718) identical everywhere.
- **B — Ball, plus a small body effect:** Ground also scales the fielder's brake and turn (the #718 response law) and the runner's slide. No skating, no loss of control. Needs per-park race checks.
- **C — Full traction model:** Bodies slip. Breaks 'players do not skate' and multiplies every race calibration by the number of grounds.

**Recommendation:** A for the first slice. Re-open B only after a sitting says ice does not feel like ice.

**Existing contract:** docs/parks.md Crystal Rink: 'we do not make players skate'. #718 response law and #761 coast/brake are one movement contract for every park. fielding.chase.frozenMul 0.45 is the only ordinary park status.

**Acceptance:** The same routine grounder is a routine out in every park. A probe shows the ball's roll differs by ground while the body's speed curve does not (option A).

### FD-05 — Is the ground one material per park, or a map of zones?

**Decision — Jack, September 21, 2026: B.** Reply "b" selects zones from the shared diamond: infield dirt, outfield, warning track and foul apron each name a ground id from a closed library; a park overrides a zone. The ball, the three loose-ball ground models and the FD-04 body multipliers all read the zone. The exact zones, their boundaries (the lip and the track are #730 / #732 geometry) and the ground ids are contract work. No painted regions. No ground value is selected. Full provenance is in the canonical JSON.

Area: Environment. Depends on: FD-03. Evidence: BROSNAN, MH-STAD.

- **A — One ground per park:** A park names one ground id. Simple. An ice park has an ice infield.
- **B — Zones from the shared diamond:** Infield dirt, outfield, warning track and foul apron each name a ground id. The zones are the ones the diamond already has (infield lip, track). A park overrides a zone's ground.
- **C — Painted regions:** A park draws free shapes with their own ground. Most expressive. Hardest to read and to test.

**Recommendation:** B. The zones already exist as geometry, so no new shape language is needed. The ground library is a closed table like the pitch families (D20).

**Existing contract:** Roll friction is one number today (flight.json roll.friction 22). `surface` is a label with no gameplay effect (#713). The infield lip and the warning track are existing geometry.

**Acceptance:** A ground id with no authored row stops the load and names the id. A ball crossing from dirt to grass changes its roll at the lip, in the trace.

### FD-06 — What shape may the outfield fence take?

**Decision — Jack, September 21, 2026: C.** Reply "C" selects a free polyline: points from pole to pole, each with a height, so a park can have notches, porches and alleys. A span names a wall material, so B's benefits (a tall wall, a low corner, a climbable section) are included. **The three-post arc stays the default**: a park that lists no points plays and draws exactly as today. The author's recommendation was B. Contract work, not selected: the point format (FD-12 units), the validator's limits, and how the fielder-depth rule, the C80 migration, the track, the poles, the drawn wall and the `field` shot follow a free shape. Proposed guardrail: one fence distance per bearing from home, so `AtBatResolver.FenceAt` stays a function and its consumers keep working (`FieldBounds.Build` already clips the flight against a polygon sampled from it). **D15 changes form** and is reconciled in the spec before code: "one number" becomes "the drawn wall equals the flight wall on every span". Full provenance is in the canonical JSON.

Area: Geometry. Depends on: FD-01. Evidence: MLB-FD, SI-WALLS, WP-GM, MH-STAD.

- **A — Three posts, one height:** Today's rule: the circle through the left, center and right posts, one fence height. Asymmetry only through the posts.
- **B — Posts plus wall segments:** Keep the three-post arc. Add named segments by bearing that change the height or the wall material (a tall left wall, a low corner, a climbable section).
- **C — Free polyline:** The fence is a list of points with a height each. Notches, porches and alleys. The fielder depth rule, the cameras and the kit must all follow a free shape.

**Recommendation:** B. It gives a park a landmark wall without changing how depth, cameras and the C80 migration read the fence.

**Existing contract:** D15: fenceHeightFt is one number for the flight and the drawn wall. `AtBatResolver.RoundFence` / `FenceAt` is the three-post circle. Canopy's `climb_wall` hazard is a wall property written as a hazard.

**Acceptance:** The drawn wall equals the flight wall on every segment (the HarborWallTests rule, for every park). A segment's carom and rob behavior come from its material row.

### FD-07 — May foul territory and outfield depth differ by park?

Area: Geometry. Depends on: FD-01. Evidence: FG-FOUL, FG-FOULHFA.

- **A — Shared in every park:** Every park keeps Harbor's foul wrap and the global fielder starts. Least work. #732 already shows the wrap cutting fair territory when fences shrink.
- **B — Foul territory per park, depth shared:** Extract the wrap from HarborWall into park parameters. Parity first. A park may then ask for a wide or tight foul area.
- **C — Foul territory and outfield depth per park:** Also let a park stand its outfield deeper or shallower (#730 decision 3 left this to #713).

**Recommendation:** B now, C when a park's fence shape needs it. The extraction is the largest single rail and it is needed even if every park keeps Harbor's numbers.

**Existing contract:** `FieldBounds.Of(park)` builds the foul walls from `HarborWall` (FoulOffset 36 ft, flare start 95 ft, 4.2 ft rail). #732 owns those absolutes under #730. fielders.json is one global set (#730 decision 3).

**Acceptance:** No Harbor-named class decides a rule in another park. Every park plays bit-identical until it names a difference.

### FD-08 — What fairness contract must every hazard obey?

Area: Hazards. Depends on: FD-01. Evidence: MW-YP, MW-WC, MTA, SSB, PR-RULES.

- **A — Geometric, fixed and told:** A hazard is a fixed body or volume. Its effect is decided by geometry. The player sees it before it acts. No roll. A warp has a fixed, learnable exit.
- **B — Geometric with seeded variety:** As A, but a hazard may choose among told outcomes with the match seed (a warp picks one of two exits). The choice shows before the ball arrives.
- **C — Party chaos:** Hazards may surprise. Matches the reference most closely. Breaks 'a play is decided by geometry, never by a roll'.

**Recommendation:** A, with B allowed only where the choice is shown early enough to act on.

**Existing contract:** AGENTS.md: a play is decided by geometry, never by a roll or a caption. Today every hazard is decided once, at the preview, from the ball's landing point (Fielding.cs:69-90). The warp exit is rng.Next (Fielding.cs:701). A slowed play adds a 0.4 drop roll (Match.cs:702). The slow lasts the whole play for every chaser. The reference's most criticized hazards are random ball output, lost visibility and stuns with no counter; fixed geometry gets the praise.

**Acceptance:** Both seats and the CPU read the same hazard facts. A scenario per hazard pattern proves the outcome from positions alone. A stranger can say why the hazard did what it did.

### FD-09 — How are hazards built: one rule per hazard, or a small library of patterns?

**Decision — Jack, September 21, 2026: B.** Accepted with FD-12, FD-16 and FD-17 as one set ("accept all four recommendations"). A closed pattern library; a hazard type is a data row; no silent fallback. The pattern list is a starting proposal: which patterns ship, the fairness contract (FD-08), placement (FD-19), the catch stealer's place in the game and every number stay open. Full provenance is in the canonical JSON.

Area: Hazards. Depends on: FD-08. Evidence: MH-STAD, MW-MSS.

- **A — One rule per hazard type:** Each new hazard is new sim code. Fast for the first few. The ninth park invents everything again.
- **B — A closed pattern library:** A few patterns (status volume, ball redirect, solid body, timed mover, catch stealer, reward target, wall trait). A hazard type is a data row that picks a pattern and its numbers. A park lists instances.
- **C — Scripted hazards:** A park carries its own script. Most freedom. No validator can reason about it.

**Recommendation:** B. It is the pitch-family rail (D20) applied to hazards: a closed library, authored rows, no silent fallback.

**Existing contract:** ParkHazards (Fielding.cs:645-729) is a set of type-string tests. statue, train, ac_unit and tree have no sim effect. climb_wall is a park-wide flag. Chompers are a code literal behind a park-id test (Fielding.cs:670-683). Hazard numbers sit in fielding.json 'park' under park names (emberNightFireMul).

**Acceptance:** An unknown hazard type or a type with no authored row fails `cli art`. A new hazard that fits a pattern needs no new sim code.

### FD-10 — Can the players turn park hazards off?

Area: Hazards. Depends on: FD-08. Evidence: MPT, MTA, MSC, MSBL, SSB.

- **A — No toggle:** The park is the park. Harbor is the no-hazard choice. One rule set to balance.
- **B — A match option: hazards off:** Any park can be played for its size, air and ground alone. Doubles the states to balance and test.
- **C — Day is clean, night has hazards:** Time of day is the toggle. Ties FD-11 to this choice.

**Recommendation:** B, default on, built after the hazard runtime. With hazards as a list of instances the switch is an empty list, and the park keeps its size, air and ground. Later Mario sports games and Smash all added this switch; the one game that removed hazards was called too plain.

**Existing contract:** Title has a night toggle. Exhibition options are innings and difficulty (title X / LB). No hazard switch is documented for either Mario baseball game; Harbor is the clean park today.

**Acceptance:** A friend on the couch can always play any park with no hazard. The park-factors report covers both states.

### FD-11 — What may night change?

Area: Hazards. Depends on: FD-08. Evidence: MW-MSS, MW-PIG, MPT, THT-TWI.

- **A — Presentation only:** Night is light, sky and fireworks. Rules never change.
- **B — A declared rule layer per park:** A park may carry a `night` block that overrides named fields or adds hazard instances (Crystal's window, Ember's breath reach, Funfair's chompers). Everything else is the day park.
- **C — Night is a second park:** Day and night are separate park files. Doubles the authoring and the two-root mirroring.

**Recommendation:** B. It is what the data already half does (`nightContactWindowMul`), made general and validated.

**Existing contract:** nightContactWindowMul is a park field (Crystal 0.85) read by ParkHazards.ContactWindowMul; the reference has no such at-bat rule and lost visibility is the most criticized pattern. Ember's night reach is fielding.park.emberNightFireMul 1.6. Funfair's chompers are night-only code. nightOnly / dayOnly are in every park file and read by nothing. The CLI cannot run a night game.

**Acceptance:** No code path names a park id or the word night to pick a number. The night block is validated like the day block.

### FD-12 — How does a park file stay correct on both data roots (shipped and C80)?

**Decision — Jack, September 21, 2026: B.** Accepted as part of the rails set. Positions are authored relative to the diamond and the fence, inside the #716 whole-file rule. The unit scheme is contract work and must reproduce the accepted by-zone migration (F693-04-park-migration). #730 / #732 keep their numbers until they close. No park file changes by this decision. Full provenance is in the canonical JSON.

Area: Rails. Depends on: FD-01. Evidence: code maps only.

- **A — Two whole files per park, kept in step by hand:** Today's rule (#716: whole files, never fields). Every park edit is made twice. Twelve files for six parks.
- **B — Positions in diamond-relative units:** A hazard or a wall segment is placed by bearing and by a fraction of the basepath or of the fence at that bearing. One authoring serves both roots; the trial keeps only what it truly changes.
- **C — Wait for promotion:** Do no park work until one root is the default. Simplest. Blocks the phase on #715 3e.

**Recommendation:** B for positions, inside #716's whole-file rule. It removes the class of bug #730 and #732 found (absolute feet that do not scale).

**Existing contract:** Spec §16 trial overlays: whole files, override never add. F693-04-park-migration: fences scale 0.70, wall heights unscaled, hazards scaled by the zone they sit in. trials/c80/parks carries all six parks.

**Acceptance:** A park edit that forgets the trial copy fails a test by name. A hazard sits at the same place relative to the bags on both roots.

### FD-13 — When does per-park balance start, and against what?

Area: Rails. Depends on: FD-02. Evidence: SAVANT-PF.

- **A — Harbor is the only calibrated park; others are measured, not tuned:** Every park reports its factor against Harbor. Nothing is tuned to a park until C80 is promoted. Park numbers stay trial anchors.
- **B — Tune each park as it lands:** Each park ships inside its band. Rework when C80 promotes or the flight budget moves.
- **C — Tune on C80 only:** Treat the trial as the future default. Shipped parks stay as they are.

**Recommendation:** A. It follows #713's caution: once parks override physics the home-run economy is per-park and the work multiplies by six.

**Existing contract:** F693: Harbor-specific cohorts plus the mixed-park S-29 guardrail, which has no per-park band and plays 35 of 50 games away from Harbor. `cli match --cohort s29|harbor-calibration|harbor-validation`. #713: 'keep Harbor as the only thing calibrated against until the compact profile has been through whole-race validation'.

**Acceptance:** A `park-factors` report exists for both roots and is part of the evidence for every park PR. No expectation is edited to make a park pass.

### FD-14 — Must the CPU know the park?

Area: Rails. Depends on: FD-08. Evidence: code maps only.

- **A — Yes: one shared reading:** The CPU fielder's route, the CPU runner's margins and the CPU batter read the same resolved park the ball does. A CPU glove walks around a freezer or accepts the slow knowingly.
- **B — No: the CPU plays Harbor everywhere:** Cheapest. The CPU walks into hazards and misjudges slow ground. Hazards become a human-only advantage.

**Recommendation:** A. It is the seat-parity rule the rest of the game already follows.

**Existing contract:** CPU tables live in cpu.json and running.json `cpu`. The route planner and the makeable margin read the flight path; the code map records what they read of hazards.

**Acceptance:** A scenario pair per pattern: the CPU seat and the human seat get the same outcome from the same positions.

### FD-15 — How does a player learn what a park does?

Area: Legibility. Depends on: FD-02, FD-08. Evidence: SHOW-SZ, SMB-TG.

- **A — The field card:** The field pick shows size, air, ground and hazard as icons with one line each, from the park data.
- **B — Card plus in-park tells:** Also a wind flag, a ground that looks like its row, a hazard tell before it acts, and a stamp when a park rule decides a play.
- **C — Card, tells and a lesson per hazard:** Also a Practice lesson for every hazard pattern and every ground that changes the ball (AGENTS.md: every player-facing mechanic needs a tutorial).

**Recommendation:** C, delivered in that order. The card and the tells are Presentation children; the lessons are Gameplay setup plus a Presentation child.

**Existing contract:** docs/tutorials.md contract. `PlayStamp` stamps come from typed events. Field pick is part of the carnival front (#248).

**Acceptance:** A stranger picks a park, reads its card, and can say after one inning what the park did to a ball.

### FD-16 — What is a park kit, and what is shared?

**Decision — Jack, September 21, 2026: B.** Accepted as part of the rails set. One park-neutral field kit; parks fill named slots; empty slots draw a complete greybox from data. Harbor fills the slots first with no visual change; the `ParkView` fallback diamond and dress methods retire after that. The slot schema is contract work. No art is commissioned. Full provenance is in the canonical JSON.

Area: Presentation. Depends on: FD-01. Evidence: code maps only.

- **A — Copy HarborKit per park:** Each park gets its own builder. Fast start. Six builders drift.
- **B — One field kit, park parts in slots:** The diamond, lines, bags, mound and wall builder become one park-neutral kit driven by park data. A park fills named slots: wall skin, backdrop, stands, light rig, sky, ground materials, hazard actors, audio bed, postcard.
- **C — Hand-placed scenes per park:** Each park is its own Unity scene. Breaks 'one geometry owner feeds sim and kit'.

**Recommendation:** B. Harbor becomes the first park to fill the slots, with no visual change, before any second park is drawn.

**Existing contract:** Two diamonds exist: HarborKit for Harbor, an older primitive diamond in ParkView for the rest (different bags, dirt, chalk, mound; no foul rail). Five looks are private ParkView methods; eleven light rigs are chosen by id (Look.cs:296-484). data/art/parks.json rows are {id, slot, placed}. art-rails.md rule 4 already says parks are kits, not ParkView methods.

**Acceptance:** `cli art` lists every park's slots and what is missing. A park with empty slots draws a complete greybox from its data, not an error and not Harbor's dress.

### FD-17 — What gates a park's look, and in what order?

**Decision — Jack, September 21, 2026: C.** Accepted as part of the rails set. Rules green → greybox playable → Jack's greybox sitting → DCC stages and dual stills; one park in art at a time. `StillRequest` gains `park` and `night`; the stage and still catalogs gain a park lane. Agents do not pass the sitting or the look gate. Full provenance is in the canonical JSON.

Area: Presentation. Depends on: FD-16. Evidence: code maps only.

- **A — Art when ready:** No fixed order. A park may get meshes before its rules are proven.
- **B — Greybox first, with its own gate:** A park must pass: rules green, greybox playable, a greybox sitting by Jack. Only then do the DCC stages (blocking, fill, export, still) start, with named park shots as dual stills.
- **C — B, plus one park at a time:** As B, and only one park may be in the art stages at once.

**Recommendation:** C. It is 'rails before artwork' and 'three good parks beat six ugly ones' written as a gate.

**Existing contract:** StillRequest has no park and no night. The DCC stage catalog and the dual-stills catalog have a Harbor lane only, fixed by their validators. harbor_kit.py reads no data; seven constants have drifted. #37: three parks at trailer quality before unlocking the rest.

**Acceptance:** A park's PR trail shows the gates in order. No park mesh is commissioned for a park whose greybox sitting has not happened.

### FD-18 — Which park proves the rails first?

Area: Roster. Depends on: FD-01, FD-09. Evidence: MW-PIG.

- **A — Crystal Rink:** Proves ground (ice), wall material (glass boards), a status volume and a night rule. Roadmap D2 already names it. Its hazard is the one pattern the sim already carries.
- **B — Ember Keep:** Proves size (the deep park), hot air and status volumes. Little new ground behavior.
- **C — Funfair Park:** Proves ball redirects and a timed mover. The hardest hazards first; the environment rails get little exercise.

**Recommendation:** A. It exercises the most environment levers with the least new hazard code, and it matches the roadmap.

**Existing contract:** Roadmap Phase D: D2 Crystal Rink as a kit, D3 one gimmick park (Funfair or Ember).

**Acceptance:** The proving park needs no code that names it.

### FD-19 — Where may a hazard sit?

Area: Hazards. Depends on: FD-08. Evidence: MW-MSS, MH-STAD.

- **A — Outfield and foul corners only:** The reference's habit: almost every hazard is in the outfield, and the GameCube barrels never roll into the infield. Routine infield plays are the same in every park.
- **B — Anywhere except the running lanes, the mound-to-plate lane and the bags:** Allows shallow ball redirects that can take a routine grounder (the reference's pipes). Keeps runners and the at-bat clean.
- **C — Anywhere:** A hazard may sit on a basepath. No reference does this.

**Recommendation:** B, as a validator rule, so a park file cannot place a hazard on a lane.

**Existing contract:** Funfair's cans sit at z 55–95 and Canopy's barrels at z 58–102, inside or near the infield. No reference game has a runner hazard in normal play. No validator checks a hazard's place today.

**Acceptance:** cli art refuses a hazard whose disc crosses a running lane, a bag pad, the mound or the plate area, on both data roots.
