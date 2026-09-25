# Appendix A — Gap audit (code at `f09cad1`; every row closed by `a15f5f5`; A.9 reopened one row on 2026-09-13, closed by PR #644)

> Archived from `docs/gameplay-spec.md`. Every row below is closed; this is history, not a rule.


Kept as the record of what was wrong on the morning of 2026-09-12 and which epic or PR fixed it. The line numbers are the morning's; the ✅ note is the resolution.

Grouped by the epic that fixes them (roadmap.md, Phase P). Line numbers from the two code maps taken on 2026-09-12.

## A.1 Pitch and swing contract (P1)

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
| 10 | `AtBatResolver.cs:13-23, 151-157` | Foul = spray > 45°; `CheapFoulPull` 40% teleport | §5.6 | ✅ P2 #590 (fair / foul by where the ball lands or is touched; the cheap pull survives as the `batting.foul` spray rule — an input, not an outcome) |
| 11 | `AtBatResolver.cs:119-122` vs `Fielding.cs:340-346` | Two homer launch bands | §5.6 | ✅ P2 #588 (`BattedBallClass.Homer` from the fence crossing is the one rule; `HomeRunLikely` reads it) |
| 12 | `AtBatResolver.cs:241-256`, `Match.cs:811-819` | HBP unreachable; `FinishHitByPitch` ignores `inZone` — ✅ P1a body geometry in world feet (S-16, S-17); the rubber-walk reach is P1b | §4.6 |
| 13 | `AtBatFeel.cs` vs `HomeSet.BatterWalk` | Cursor moves 1.85 ft/unit, body 2.4 ft/unit — ✅ P1a (both `HomeSet.BatterWalk`) | §4.6 |
| 14 | `AtBatFeel.cs:217-221`, `AtBatResolver.cs:52` | Bunt judged at the press; bypasses the oval — ✅ P1a (S-19) | §5.8 |
| 15 | `Match.cs:773-779` | Foul bunt with 2 strikes not a K — ✅ P1a (S-18); `FinishFoul` skips `AfterPitch` — stays until P6 retires the pickoff roll | §5.8, §5.6 |
| 16 | `AtBatDirector.cs:171, 173-174, 235, 288` | Stick-down both aims launch and resets the box; SET press dropped / −65-frame miss — ✅ P1a (S-14, S-15; Down resets in SET only) | §5.4, §3 |
| 17 | `Match.cs:558, 574` | Box walk reset every pitch — ✅ P1a persisted it across the at-bat; D12 (#607) recenters it after every pitch through `Match.AfterPitch` (S-16) | §3 |
| 18 | `Match.cs:38-39, 87, 205, 602-622, 654, 658, 1289` | Team stamina, flat costs, threshold ×4, swap +35 — ✅ P1c (per-pitcher pools, table costs, JSON star cost, swap trades gloves, S-25, S-26) | §4.7 |
| 19 | `Match.cs:648-668` | CPU pitcher aims center, nested type rolls, dead `TimingErrorFrames` — ✅ P1c (location-by-count table, S-27; the field is removed) | §4.8 |
| 20 | `Match.cs:672-676, 698-726` | `CpuBatter.Swing` arms steals; forced \|err\| ≥ 3.2 vs a human — ✅ P1 (`CpuBatter.Swing` is pure, S-28; the steal roll is `CpuBatter.ArmSteal`, a SET verb, until P6 #568 moves it into the runner AI) | §5.9, §11.6 |
| 21 | `AtBatResolver.cs:232, 264-281`; `Models.cs:105, 123, 139` | Discarded `pitchStat`; 1.12 divide-out; dead `ChargePitch`, `Strike`, `TimingErrorFrames` — ✅ P1c (the divide-out and the pitch's timing field are gone; `AtBatInput.ChargePitch` is live in the pitch factor) | cleanup |
| 22 | `Fielding.cs:472-476` | Park id string special-cased for the window — ✅ P7 (`Park.NightContactWindowMul`, a park data field); the window itself is gone on both roots since F4-d (#895, FD-11-R2) | §14 |
| 23 | `star-skills.json` vs `FieldAbilities.cs:117-152`, `AtBatResolver.cs:220-226`, `Match.cs:1289` | JSON dead; `staff-swing` 1.08 vs 1.10; `staminaCost` ignored — ✅ P1c (`StarSkillTable`, copies deleted) | §13 |

## A.2 Flight and field (P2)

| # | Where | What | Spec |
| --- | --- | --- | --- |
| 24 | `BallFlight.cs:34` | Scalar downrange wind | §6.1 — ✅ P2 |
| 25 | `BallFlight.cs` | No fence, wall, or foul line in the trajectory | §6.1, §7.9 — ✅ P2 (`FieldBounds.Of`, `BattedBall`) |
| 26 | `BallFlight.cs:68, 116-119` | Landing guards in two time bases | §6.1 — ✅ P2 |
| 27 | `Fielding.cs:327-346` | Three separate class bands | §6.2 — ✅ P2 (`BattedBallClass`) |
| 28 | `Fielding.cs:420-432` | Positions by roster order | §8.1 — ✅ P2 (`Team.Gloves`) |

## A.3 Runner model (P3)

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

## A.4 Fielding decides by geometry (P4) — ✅ closed by P4 (#566)

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

## A.5 Outs, double plays, Time (P5) — ✅ closed by P5 (#567)

| # | Where | What | Spec | Closed |
| --- | --- | --- | --- | --- |
| 48 | `Match.cs:870-879` | Synthetic DP from fabricated throws | §10.4 | ✅ P3 (Complete reads the bodies) |
| 49 | `LivePlaySystem.cs:344` | `Retire` result discarded after committing caption/flags | §10.4 | ✅ P5 (`ApplyThrow`, the close-play verdict) |
| 50 | `Match.cs:899-976` | Outcome inferred from bookkeeping deltas; three `goto case Single` | §10.6 | ✅ P3 |
| 51 | `Match.cs:908, 1030-1031` | Control flow on caption text | §15 | ✅ P3 |
| 52 | `Match.cs:44, 932`, `InPlayDirector.cs:1215` | `ClosePlaySafe` stale across plays | §9.6 | ✅ P3 / P5 (one verdict, written once) |
| 53 | `ClosePlay.cs`, `InPlayDirector.cs:1157-1223` | Mash on every unforced 3B/home throw | §9.6 | ✅ P5 (`ClosePlay.WithinMargin`) |
| 54 | `InPlay.cs:402, 416-417` | Tag reach 14 ft; home is a safe bag for the batter | §10.3 | ✅ P5 (4 ft + ability, judged through the frame; the batter's plate was closed in P3) |
| 55 | `LivePlaySystem.cs:307-313` | `ThrowArrived` sets HasBall/CatchMade unconditionally | §10.2 | ✅ scripted harness command only (tests); the live ball lands every throw through `OnThrowLanded` |
| 56 | `InPlayDirector.cs:150-152` | Rest fallback bypasses `CommitInPlay` | §10.6 | ✅ P3 (the sim commits every ball) |
| 57 | `Match.cs:556-559` vs `:575-584` | Walk-off only on the take path | §1 | ✅ P3 |
| 58 | `PlayStamp.cs:29` | Triple play is a label only | §10.7 | ✅ P5 |
| 59 | — | Extra innings, mercy, ground-rule double, foul fly catch, rundown missing (the ERROR stamp landed with P4) | §1, §7.11, §9.7 | ✅ P3 / P4 / P5 (rundown) |

## A.6 Steals and pickoffs (P6) — ✅ closed by P6

| # | Where | What | Spec | Closed |
| --- | --- | --- | --- | --- |
| 60 | `Match.cs:338-350`, `Baserunning.cs:25, 46-49` | Double steal impossible; no steal of home | §11.1 | ✅ `StartStealAt` arms one body; `StealTarget(3) == 4` |
| 61 | `StealThrow.cs:59-64`, `ActorDirector.cs:441-446` | Lead as a time credit; runner leaves at commit; no perfect steal | §11.2 | ✅ `StealBreak`: the break at release on the pitch's clock, the perfect arm off the windup clock; `RunnerRemainSec` is gone |
| 62 | `Match.cs:480-503, 1376-1393` | Pickoff arms a steal; a miss awards the base | §11.4 | ✅ `Match.BeginPickoff`: the pickoff is a live runner play; a sailed throw is live |
| 63 | `Match.cs:1421-1481` | Random pickoffs on every pitch (≤ 72%) | §4.5, D3 | ✅ gone with P3; the CPU read (`CpuPitcher.PickoffBag`) is a rate, never an out |
| 64 | `Match.cs:1319-1323`, `StealThrow.cs:96-115` | Pickoff resolved with the steal race; real pickoff race unreachable | §11.4 | ✅ one live ball for both; `GunSteal` / `ResolveStealThrow` / `ApplySteal` deleted |
| 65 | `StealThrow.cs:23` | No cover at bags 1 and 4; chem computed defender-vs-runner | §11.4 | ✅ `StealThrow.CoverPos` covers every bag; `BeginThrowToBag` rolls the thrower–cover pair |

## A.7 Architecture (P0 / #512) — ✅ closed by P0

| # | Where | What | Now |
| --- | --- | --- | --- |
| 66 | `InPlayDirector.cs` (1249 lines) | Relay chain, close play, CPU catch timing, steal phase, glove speeds are Unity-side baseball | `LivePlaySystem.Field.cs` owns them; `InPlayDirector.cs` translates pads, mirrors state, plays cues. The bobble and the CPU catcher's release roll on `Match._rng` (S-92). | ✅ |
| 67 | `data/feel/table.json` | `throwEase`, `chargeDecay`, `inPlayCommitSeconds` read only by tests; `runHz` shadowed by `Motion.RunHz` | Removed. | ✅ |
| 68 | everywhere in §16 | ~150 rule constants in C# with no data hook | `data/rules/*.json` + `RulesTable` + validator; the rows above name their section. Rule numbers still *shaped* like the old code (the infield roll, the lead credit, the CPU rolls) are tabled as shipped and marked for their epic. | ✅ |

The stale close-play verdict (A.5 #52) is gone with `Match.ClosePlaySafe` (the contest's verdict is applied to the body in the play and nothing else reads it); the wrong-clock arrival inputs (A.5 #55, §9.1) are P3's `RunnerSystem.ArrivalSec`. The mash's ±0.25 s gate (A.5 #53) stays P5's: today the contest runs whenever the ball is at third or home before an unforced runner still coming, and that runner's body waits for the verdict.

---

## A.8 Evening sitting (2026-09-12) — ✅ all closed the same day

| # | Found | What | Spec | Closed by |
| --- | --- | --- | --- | --- |
| 69 | #606 | Mini diamond drew three booleans; runner positions were one call away | §15 | ✅ #622 |
| 70 | #607 | Box persisted across the at-bat (reference) — Jack's call: recenter every pitch | §3, D12 | ✅ #620 |
| 71 | #608 | Drawn Harbor wall 26 ft over an 8 ft sim fence; carom at an invisible plane | §6.1, §7.9, D15 | ✅ #616 (Harbor 12 ft, one number, gate) |
| 72 | #609 | 2.4 s outfield read (P7) in real seconds, on the human glove, unscaled | §8.2 | ✅ #619 (0.83 s; S-29 held by `chase.outfieldAirMul` 0.6) |
| 73 | #610 | Camera swooped to the bag on every throw; reference cuts once on contact, bag cam only on a close play | §15, D14 | ✅ #618 (cuts, per-shot blend, 0.25 s hold) |
| 74 | #611, #576 | Every glove's yaw pinned to the ball while the run cycle played | §8.2 | ✅ #615 (facing from velocity, dt-scaled, backpedal named) |
| 75 | #612 | Press judged at press + 0.30 s; reference judges at the plate and warps the take | §5.3, D13 | ✅ #617 (`window.leadSec` 0.10, `humanWindowMul` until #887) |
| 76 | #613, #583 | One swing take, no windup, no finish | §5.1 | ✅ #621 (`swing-slap` / `swing-charge`, held finish; look gate open) |
| 77 | #623 | The new takes put the bat through the head at MAX load | §5.1 | ✅ #624 |

## A.9 Skeptic pass (#634, 2026-09-13) — closed by PR #644

| # | Found | What | Spec | Status |
| --- | --- | --- | --- | --- |
| 78 | #634 → #640 | See note *78, What* below. | §4.5, §8.7, §11.4 (S-69) | ✅ #640 (PR #644): one cover read per play (`LivePlaySystem.CoverBallX`), no body covers two bags, the CPU rundown (inside `running.rundown.rangeFt`) throws ahead at the last makeable moment and never lazily at a body it races, the CPU runner reverses only when the way back is open; `StealScenarioTests.S69` un-skipped, a Run 2 / 3 / 4 theory added |

Notes to the table above:

**78, What.** `InPlay.CoverMap` picks second's cover by the ball's side; `LivePlaySystem.Field.cs` `InitRunnerGloves` reads it at the rubber and `BeginThrowToBag` / the rundown read it at the thrower, and `TickCoverBags` walks only the first — on a pickoff at first the throw to second hangs as a lob (`fielding.throw.lobMaxSec`) while the runner who broke walks in; no runner, Run 2 to 9, is ever picked off

## A.10 Fields audit (#814, code at `d0c6e12c`) — open

Two read-only maps hold every line: [sim](../research/fields-code-map-sim.md), [presentation](../research/fields-code-map-presentation.md). The rows are grouped by the epic that fixes them ([plan-fields.md](../plan-fields.md), [implementation map](../plan-fields-implementation.md)).

| # | Epic | What | Where | Spec | Status |
| --- | --- | --- | --- | --- | --- |
| 79 | F1 | Unknown park fields are dropped with no error; `notes`, `nightOnly`, `dayOnly`, `periodSec` have no C# member; `faction` and `tag` are read by no rule | `ContentValidation.cs:42-47, 508-540`; `Models.cs:186-211` | §16 | ✅ F1-a (#820, PR #831): the park read is strict and names the file and the key; `notes` is declared on the loader's row and stays off `Park`; `nightOnly`, `dayOnly` and `periodSec` are removed from all twelve files; `faction` gained a reader (the home-park map). `tag` is still Unity's only |
| 80 | F1 | The park list and the home-park map are code literals; an unknown park id falls back to Harbor silently | `ExhibitionPick.cs:10`, `Teams.cs:37`, `CarnivalFront.cs:229`, `Match.cs:151-186` | §0.3 | ✅ F1-a (#820, PR #831): the list and its order come from each park's `pickOrder`, the home park from its `faction`; an unknown id throws. `CarnivalFront`'s per-id copy is F8-a's and is named as the one allowed exception beside `ParkHazards.ChompFly` (F4-a) |
| 81 | F2 | The foul wrap (36 ft), backstop (−36 ft), rail (4.2 ft) and flare (95 ft) are `HarborWall` literals used for every park; the validator ties every fence to Harbor's rail | `FieldBounds.cs:105-188`, `HarborWall.cs:21-32, 114, 183`, `ContentValidation.cs:317` | §6.1 | ✅ F2-a #826 (PR #833): `boundary.json` through `ParkBoundary`; `FieldBounds` and the fence floor name no park's class; the cache keys on the edge. Values unmoved (#732 still owns 36 / 95); `SF-08` holds vertex for vertex on both roots |
| 82 | F2 | The drawn wall mirrors right field onto left; the drawn rail ramps where the sim rail stays 4.2 ft | `HarborWall.cs:99-101, 177-191` vs `FieldBounds.cs:159` | §6.1, D15 | See note *82, Status* below. |
| 83 | F3 | All ball physics is one global table; three separate loose-ball ground models; `surface` changes no play | `BallFlight.cs:79-194`, `LivePlaySystem.Field.cs:3059-3126` | §6.1, §14 | See note *83, Status* below. |
| 84 | F3 | `Diamond`, `ParkDiamond` and `RunnerAi` read the process-wide `Rules.Default`; 118 call sites fall back to it | `Diamond.cs:15-52`, `ParkDiamond.cs:15,24,420`, `RunnerAi.cs:82` | §16 | See note *84, Status* below. |
| 85 | F4 | Every hazard is decided once from the landing point; the slow is play-wide; four types are inert | `Fielding.cs:69-90, 645-729` | §14 | See note *85, Status* below. |
| 86 | F4 | Two rolls: the warp exit on the world, `drops.frozen` on the result | `Fielding.cs:701`, `Match.cs:702` | principle 2, §14 | ⚠️ the warp exit (F4-c: the live ball must travel there) / ✅ `drops.frozen`: the park's use retired by F4-b (#896, FD-08-R1, `SF-22`); the heart swing's use (a special) is unchanged |
| 87 | F4 | Chompers: code literals behind a park id; Ember's night reach is a fielding rule with a park's name | `Fielding.cs:670-683`, `fielding.json` `park` | §14 | See note *87, Status* below. |
| 88 | F4 | Four status volumes sit on a running lane on the shipped root, and four on `trials/c80` (one on second base's pad). Inert today, because no body touches a hazard | Crystal `(40,70)`, `(-45,90)`; Ember `(38,78)`, `(-42,96)`; map §5 Q1 | §0.3 (FD-19) | See note *88, Status* below. |
| 89 | F5 | S-29 pools six parks with no per-park band; `cli match` has no night flag | `AtBatScenarioTests.cs:690`, `Cli/Program.cs:102` | §14 | ⚠️ night flag ✅ #828 (PR #834): `cli match --night`, and `cli match --cohort park-factors` reports every catalog park day and night against Harbor on both roots (`SF-30`, a report, not a gate). S-29 still pools six parks with no per-park band — whether it becomes Harbor-only plus a factor check is FD-13's open tuning question (map §5 Q11) |
| 90 | F6 | Two diamonds are drawn; the foul rail is drawn only at Harbor; looks and eleven light rigs are chosen by park id; two Rooftop AC units are not in data | `ParkView.cs:48-273, 829`, `Look.cs:296-484`, `HarborKit.cs:86-99` | §15, §0.3 | See note *90, Status* below. |
| 91 | F7 | ~~`StillRequest` has no `park` and no `night`~~; the stage and dual-still catalogs have a Harbor lane only; `harbor_kit.py` reads no data and seven constants have drifted | `StillRequest.cs:15-34`, `DccStages.cs:59-92`, `DualStills.cs:17-34`, `harbor_kit.py:42-61` | §0.3 | ⚠️ · `StillRequest` ✅ #829 (PR #830): optional `park` (refused by name unless it is in the catalog) and `night`, honoured by `StillCapture` and handed back after the batch, named in the PNG away from the default park, and `tools/still-gate.sh --park <id> [--night]`. The two catalogs are F7-b |
| 92 | F8 | No hazard has a tell, a stamp or a lesson; Unity reads none of `Frozen`, `Warped`, `Chomped`; field-pick copy is a per-id switch | `Match.cs:1556, 1584, 1607`, `CarnivalFront.cs:225` | §15, principle 9 | ❌ |

Notes to the table above:

**82, Status.** ✅ mirror: F2-b #845 (PR #848) — the loop walks both sides on the park's own fence, every drawn vertex is on the clip polygon (`SF-05`, 1e-9 ft, both roots), the three symmetric parks are bit-identical. ✅ ramp: F2-b2 #873 (PR #875), FD-06-R2 — each drawn span stands at its flight segment's top: the rail is hip-high to each pole and the wall steps up at the pole, drawn as a vertical step (`FieldKit.Wall`); `RampStartZ` and `TaperIsARamp` are gone, and `SF-05` claims the whole rail on both roots. ⚠️ look gate (Jack) at the poles

**83, Status.** ✅ the air (#827: `RulesTable.AtPark`, the park file's optional `environment`; no park names one) / ✅ the libraries and the zone map (F3-b #846, PR #850:
`grounds.json` and `walls.json` as named rows, every row today's `flight.json` number; a park's optional `zones` block over a map derived from `surface`; `surface` validated against the library instead of a set in the validator; `GroundZones.ZoneAt` reads the existing lip, track and chalk and moves none of them; `GroundLibraryTests`) / ✅ the readers (F3-c #856):
the batted ball's roll and bounces, the overthrow and the local bobble read the row of the zone under the ball through `GroundZones.RowAt`, the carom the row of the segment's material through `WallMaterial.OfSegment`; `flight.roll` / `bounce` / `skid` / `wall` and the four fielding keys are retired on both roots; at equal rows every path is the pre-move one to the bit (`GroundReadTests`).
`surface` still changes no play, now only because every row is equal — the first unequal row is a trial (F9-a)

**84, Status.** ✅ the flight readers (#827: the second client and the Unity directors take `Match.Rules`; `ParkEnvironmentTests.TheFlightReadsTheTableItIsHandedAndNotTheProcessDefault` is the audit that catches the next one) / ✅ the resolvers (F3-a2 #838, PR #840:
`Match` builds `AtBatResolver` and `FieldingResolver` on `_rules` and every call it makes reads the same table, so the first flight of a play sees the park's air; `LivePlaySystem` already read `_match.Rules`; `ParkEnvironmentTests.SF01_EveryResolverInTheMatchHoldsTheMatchsResolvedTable` walks both roots, six parks and four rungs, and `…SF10_ASeededSwingThroughTheMatchFliesTheParksAir` is the seeded swing through the real path) / global by decision:
the infield and the fielder starts (`Diamond`, `ParkDiamond`, `RunnerAi`)

**85, Status.** ⚠️ partial — ✅ F4-a (#847): the dispatch is a closed pattern library (`data/rules/hazards.json`, `HazardPattern`), not a ladder of type-string tests, and the four inert types carry `decoration`, an authored row that says so (map §5 Q6). ✅ F4-b (#896, FR-07, FD-08-R2): the status volume is a per-body touch test in the live tick with a stated time (`slowSec` 3.0 s, Jack's number, not yet played) and a typed event (`BodySlowed`); the play-wide flag is gone from the park.
✅ The CPU route costs a volume and goes around it when that is cheaper than the slow (FD-14, `SF-26`). ✅ The redirect and the reward target are live on the ball, and the chompers are redirects (FD-09-R2). ✅ Solid bodies and the timed mover are live (F4-f). Every hazard type acts on the live ball or body.

**87, Status.** ✅ F4-a (#847): the three mouths are `chomper` rows in `data/parks/funfair-park.json` at the literal's own places, migrated on `trials/c80` by the accepted zone rule, and the reach is `fireBreath.nightRadiusMul` in `hazards.json`. `fielding.park` keeps only `shellWarpChance`, which is a star swing's flag. Shipped root bit-identical; the trial's Funfair night row moves, reported in PR. ✅ F4-d (#895, FD-11, FD-11-R2):
the mouths are Funfair's night block, read only through the one resolution (`PlayedPark.Of`); the reach stays the breath's own night number in its library row, which FD-11-R2 keeps; Funfair's and Ember's night games are the games they were (SF-25)

**88, Status.** ✅ F4-e (#862, FD-19-R1, Jack 2026-09-22 answered §5 Q1): the five shipped rows behind the eight moved outward along their own bearings to the smallest whole-foot place that clears both roots, sizes kept, the trial rows the migration of the moved rows — Crystal (53, 93), (−49, 97), (10, 187); Ember (49, 100), (−45, 104) (§14 table). The validator refuses the next one (`SF-23`)

**90, Status.** ⚠️ · two diamonds ✅ F6-a (#859, PR #863): one `FieldKit` draws the diamond, rail and wall at all six parks from the geometry owner; `HarborKit` adds Harbor's dress to it; `ParkView`'s fallback diamond, its fence and the kit-less Harbor branch are gone; the foul rail and backstop are drawn at every park. Harbor unchanged is a look gate at review. · the old dress backstops and dugouts ✅ F6-a2 (#881):
the pre-kit backstop, side panels and ledges the five dresses still built inside the kit's backstop (implementation map finding 29) are removed, with the fallback dress's `Lip`, and so are their old dugout benches and awnings (Jack at the sheets: "looks like we also need to get rid of those old dugouts"); two source rows refuse the next one (`FieldKitSourceTests.NoDressPieceStandsInsideTheKitsBackstop`, `…NoDressPieceStandsInADugout`); the before / after sheets are Jack's look gate.
· the eleven rigs and the colors by id ✅ F6-c: rows of `data/art/looks.json` and each kit row's `palette`, named by the park's slots; `Look.Rig*` is gone. · the dress by id and the two AC units ✅ F6-d: the dress is named builders picked by slot, the hazards toys picked by type row with a ring at the sim's disc, and the two literal AC units are gone (Rooftop draws the one its data names); the Funfair train stands at its data spot, not a code one
