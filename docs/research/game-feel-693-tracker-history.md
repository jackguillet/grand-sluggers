# Issue #693 tracker history through 9ca738e

Captured September 14, 2026, before condensing the parent issue description near its size limit. The body below is preserved verbatim. Historical proposals and pending statuses are superseded by later decisions in the [current register](../decisions/plan-game-feel-693.md) and [#708 report](../archive/game-feel/research-game-feel-708.md). This archive is provenance, not a new acceptance or runtime contract.

---

Parent: #564 / #566. Sitting: #534 re-sit `850dd95`. Serves #209. **Research before any number.** Exact-work: MLB vs Super Sluggers vs Harbor.

## Observed

Balls come off the bat too fast. Throws are definitely too fast. Consider making the whole field smaller. Research the proportions and speeds Mario Super Sluggers uses.

## Likely cause

Harbor is a 90 ft diamond (`ParkDiamond`). Exit velo is `data/rules/batting.json` quality columns. Throws are `fielding.throw.baseFtPerSec` **100** (68 mph) — P4 raised them from 56 ft/s so S-31 (grounder to SS vs Run-5) could retire the batter (`docs/gameplay-spec.md` §8.5). Super Sluggers plays on a **compressed** diamond with arcade throw/exit clocks, never measured here. Sitting: the race is lost because the ball and the throw are too fast for the 90 ft toy, not because the runner is slow.

Do **not** shrink Harbor as a one-off Vector3. Write the Sluggers relationships (base path vs throw time vs hopper time) first; then either scale the park in `ParkDiamond` / kit or slow `exit` / `throw.baseFtPerSec` so S-31 and S-29 still hold.

## Observable when fixed

A hopper to SS is a scoop and a race you can see. An OF throw home is watchable. Spec names the numbers (Sluggers-derived or a written Harbor cartoon). `cli match` S-29 stays in band. Jack re-sits the race.

## Files

Research note in the spec (D-new or §8.5 / §6). Then `data/rules/batting.json` / `fielding.json` / `ParkDiamond` / `harbor_kit.py` as the research decides. `HowToPlay` if distances are couch copy.

## Tests

Tests encode the **relationships** (throw time vs 90 ft vs runner), not “a mesh exists.” S-31 still an out or the spec says why not. S-29 band named.

## Standing order

Session kind: **gameplay** after the write-up. Exact work: research first. Human gate: Jack watches the race.

## Banned

A random shrink of Ashlord, retuning S-29 until one seed looks slow, PhysX, a second park, passing #534 from CI.

## Research and decision tracking — September 14, 2026

**Direction: compare Wii Super Sluggers and GameCube Superstar Baseball before choosing.** Treat this as the game’s long-term proportions/pace contract. Numeric calibration and the human race gate remain open.

- [x] Research foundation and current-code audit prepared in draft PR #704 (`9859671`; audited baseline `eff10d9`).
- [ ] #701 — matched reference captures, proportions, frame/event annotations, uncertainties, and comparison before reference selection.
- [ ] #702 — extend the existing trace/scenario path with reproducible full-race budgets and evidence validation; no coefficient tuning.
- [x] F693-06: Jack retained **1.8–5 mean runs per side** as the S-29 regression guardrail on September 14, 2026. Spec reconciled; no runtime/test threshold changes.
- [x] F693-05 design intent: Jack selected **reliable routine defense** on September 14, 2026. Clean, reasonably prompt ordinary execution should normally retire an average runner; difficult circumstances create tight races. Geometric outcomes remain authoritative. Numerical opportunity/margin bounds and human validation remain open.
- [x] F693-03 design intent: **quick release and readable travel, but not so quick that the throw fails to visibly register** (Jack, September 14, 2026). Compare ordinary Wii/GameCube transfer, release, follow-through, and flight before selecting timing; no instant release, numerical target, or reference winner is approved.
- [x] F693-04 design intent: Jack selected **hard liners rewarding existing positioning over post-contact reaction**, with readable paths and distinct grounder/fly opportunities (September 14, 2026). Reference-informed bounds remain pending; current fast-liner values are not accepted merely by this direction.
- [x] F693-07 design intent: **brisk routine beats on the slower/more deliberate side**, with extra emphasis for big moments (Jack, September 14, 2026). Compare Wii/GameCube result, reaction, reset, and next-ready timing before selecting durations; live play and D7 remain unchanged. Human acceptance is still pending.
- [ ] Jack resolves the remaining decisions in the version-controlled register and accepts explicit target intervals.
- [ ] Serial gameplay calibration, then separate presentation/conditional art work, then the standalone race re-sit.

Read [the research report](https://github.com/jackguillet/grand-sluggers/blob/codex/693-game-feel-rails/docs/research-game-feel-693.md) and [decision register/execution plan](https://github.com/jackguillet/grand-sluggers/blob/codex/693-game-feel-rails/docs/plan-game-feel-693.md). The draft adds gameplay-spec D19 and agent rails, a hashed machine-readable baseline, and reproduction instructions. Coordinate #558 takes, #691 body scaling, and the existing presentation owners.

Audit clarifications to the original hypotheses above: `Diamond.cs` owns semantic diamond geometry; `ParkDiamond` is a presentation consumer. The reported GameCube stadium dimensions support investigating a shallower **outfield**, not yet a measured proportional basepath shrink; Wii numeric dimensions remain unresolved. Current `releaseSec` is included in the whole linearly sampled throw duration. S-31 inherits Crystal Rink, not Harbor; its present throw-end/predicted-runner margin is about 0.66 s and is not an accepted feel band. S-29’s current 50-game mixed-park cohort allows 1.8–5. Jack accepted retaining that guardrail on September 14, 2026 (F693-06); the research PR reconciles the old 2–5 spec headline. This does not pass the final scoring/feel gate.

Baseline validation: 1,030 existing tests passed, 23 protocol entries loaded, seed-7 Harbor trace retained as a summary/digest, and S-31/S-32 observed through the existing helper. These are current-code evidence, not reference calibration or human acceptance. A documentation merge must not close this issue.

## Initial reference comparison — draft PR #706

The [Wii/GameCube observation packet](https://github.com/jackguillet/grand-sluggers/blob/codex/701-reference-comparison/docs/research-game-feel-701-comparison.md) is tracked in #701 and draft #706 (`8882531`, dependent on #704). It contains two annotated infield sequences and explicitly separates recording-time observations from verified stock calibration.

F693-01 provisional visual lead **accepted by Jack on September 14, 2026**: Wii for on-screen readability, with GameCube as a mechanics cross-check. Recorded in #706 at `209d446`, including the gameplay spec and decision register. This accepts no numerical targets or blanket hybrid; the larger reference sample and human gameplay acceptance remain pending. The five previously accepted directions remain in force.

F693-02 geometry flexibility **accepted by Jack on September 14, 2026**: both infield and outfield dimensions may change independently to achieve reference-informed proportions. Recorded in draft #706 at `09fa21a`, including gameplay-spec D19 and the decision register. Existing basepaths and fence depths are not locked; no exact dimension, shrink factor, or required reduction is approved. Preserve D7 pitch pace and reliable routine defense.

All seven initial direction areas are recorded. Continue the authorized #701 measurement and #702 trace work before presenting the next concrete field-proportion and race-budget choices to Jack, one at a time. Numerical targets and all human gameplay gates remain open.

## Geometry evidence and pending deep-hit decision — September 14, 2026

Draft #706 revision `97443ee` adds the [geometry report](https://github.com/jackguillet/grand-sluggers/blob/97443ee/docs/research-game-feel-701-geometry.md) and [raw reports/calculations](https://github.com/jackguillet/grand-sluggers/blob/97443ee/docs/research/game-feel-701-geometry.json). An original Wii Pianta running survey implies approximately 2.87 / 3.52 / 2.88 wall/basepath ratios only under equal effective running speeds; Harbor is 3.67 / 4.44 / 3.67. Explicit speed/timing sensitivity prevents treating the inferred ~21% compression as an established Nintendo dimension. The GameCube coordinate report and pinned Project Rio memory schema are recorded separately; no basepath/body ratio or stock coordinate capture is claimed.

Research candidate C1 (2.9 / 3.5 / 2.9) is Harbor-authored, incomplete, and not accepted. The then-pending F693-02 deep-hit choice is resolved by the accepted compact-outfield direction below. This selects no dimensions, frequencies, or coefficients. Full geometry/body evidence, #702 race budgets, numerical target review, and all human gates remain open.

Validation: source references, derived ratios, sensitivity arithmetic, pending/empty accepted-target states, existing timing arithmetic, local links, and diff whitespace passed. Documentation only; no runtime test rerun or standalone build.

## Accepted compact outfield and deep-field races — F693-02

Jack accepted on September 14, 2026: preserve doubles, triples, and relays **within a compact, cartoon-feeling outfield**. Do not make the outfield huge to preserve those opportunities. His example—smaller pursuit distances with slower fielders can preserve chase time—requires coordinated geometry/movement calibration, not a fixed-speed assumption that forces the park to grow.

Recorded in draft #706 at `dc86fb9`, including gameplay-spec D19, the decision register, geometry report, and dataset. Coordinate pursuit speed/acceleration and starts, carry/hang/bounce/roll, baserunning, and visible release/throw travel. Equal chase time alone does not preserve interception or the whole race. No blanket outfielder-only penalty, exact dimensions, C1 ratios, movement coefficients, or hit frequencies are approved. Preserve responsive control, character differences, reliable routine defense, and D7.

The deep-hit direction is now accepted. Complete compact geometry/body and race-budget candidates for the next numerical review; retain extra-base/relay opportunity separately from scoring averages. Documentation checks passed; no runtime changes or human gate pass.

## Movement audit and next decision — F693-02 pursuit consistency

Draft #706 at `6e9e90d` adds the [movement audit](https://github.com/jackguillet/grand-sluggers/blob/6e9e90d/docs/research-game-feel-701-movement.md) and hashed [formula dataset](https://github.com/jackguillet/grand-sluggers/blob/6e9e90d/docs/research/game-feel-701-movement.json). Seven source files match the foundation baseline. Fielding and baserunning already have separate formulas. Current pursuit additionally uses hit-class/position modifiers (Run-5 base 30.5 ft/s; non-grounder outfield preview 18.3; non-grounder/non-liner infield preview 13.725). These are preview predicates, not live ball-height predicates.

**Accepted by Jack:** one ordinary pursuit movement profile per character across hit types and fielding positions, retaining explicit dash/status/ability modifiers and separate baserunning tuning. This supersedes gameplay-spec §8.1 class/position multipliers as the target design; see the acceptance record below. Do not simply set the current multipliers to 1. The full compact geometry/flight/pursuit profile must preserve routine defense, liner positioning, live deep hits, and scoring.

#702 needs input, movement eligibility/first displacement, movement modes/modifiers, velocity, and possession alongside throw/runner events. Speeds, acceleration, reaction rules, dimensions, and numerical targets remain open. Formula/hash/parity/link/state checks passed; no runtime change or human gate pass.

## Pursuit consistency accepted — September 14, 2026

Jack answered yes to **one ordinary pursuit movement profile per character across hit types and assigned fielding positions**, retaining character speed differences and explicit dash/ability/status modifiers. Fielding and baserunning remain separately tunable. Recorded in draft #706 at `9a442a2`, including gameplay-spec D19, decision register, movement report, and dataset.

This supersedes §8.1 hit-class/position multipliers as the target design. Replace them only within the complete compact-field geometry/flight/pursuit calibration; do not simply set existing multipliers to 1. Preserve reliable routine defense, hard-liner positioning, doubles/triples/relays, visible throws, and the scoring guardrail. Numerical speeds, acceleration, reaction rules, dimensions, and the human gate remain open.

Documentation validation passed: accepted decision state, empty numerical targets, unchanged source hashes/formulas, local links, and diff whitespace. No runtime values changed.

## Next decision: ordinary movement weight — F693-02

Draft #706 revision `12d2725` extends the [movement audit](https://github.com/jackguillet/grand-sluggers/blob/12d2725/docs/research-game-feel-701-movement.md) and source-hashed dataset. Current ordinary physical movement applies speed directly after eligibility; smoothed rendered facing, reaction lockout, neutral-stick assisted pursuit, and previous-glove handoff coast are separate behaviors.

**Accepted by Jack:** a brief build-up to running speed with quick course corrections and little residual drift when movement intent changes. Heavier momentum was considered and not selected; see the acceptance record below. This selects no numerical duration, new stat, special-action timing, reaction-lockout change, or assistance change. The accepted consistent pursuit profile remains in force.

#702 should distinguish input/eligibility, first displacement, velocity and correction time/distance, possession, and the remaining race budget. Compare starts, turns, reversals, plant arrivals, and assist transitions before choosing response parameters. Unknown reference-video input cannot establish controller latency. Source hashes, acceptance/pending states, local links and diff checks pass. Documentation only; no runtime change or human gate pass.

## Movement weight accepted — September 14, 2026

Jack approved **a brief build-up to running speed, quick course corrections, and little residual drift when movement intent changes**. Recorded at `06b0868` in draft #706: gameplay-spec D19, decision register, movement report, and dataset.

This selects the lighter, responsive ordinary pursuit direction. It follows the accepted per-character consistency across hit types and assigned fielding positions. Numerical acceleration/braking and speed targets remain pending reference comparison and playtesting. Existing reaction eligibility, neutral-stick assistance, handoff coast, and special-action timings are unchanged. Physical prediction and stepping must share the response model; rendered facing and authored footfalls must reflect it.

Both movement directions are now accepted. Source hashes, decision states, empty numerical targets, document links, and whitespace checks passed. No gameplay coefficients or runtime code changed; all human gates remain open.

## Next decision: post-contact read — F693-02

Draft #706 at `2b0ff1a` adds the [post-contact read audit](https://github.com/jackguillet/grand-sluggers/blob/2b0ff1a/docs/research-game-feel-701-movement.md#next-human-choice--f693-02-post-contact-read). GameCube community research reports human/CPU outfield control at 50 frames after contact and the view change at 25 frames. Harbor's corresponding uncapped sim marks are 0.83 and 0.42 seconds, a nominal 0.41-second difference. These remain baseline/community evidence, not accepted timings or verified wall-clock latency. Wii's interval is unresolved.

**Accepted by Jack:** retain a brief visibly communicated read of the hit before pursuit, followed by the accepted responsive movement. Removing the deliberate read was considered and not selected; see the acceptance record below. No delay, per-position schedule, difficulty change, camera-dependent sim gate, or input-buffering policy is approved. Do not extend freezing to manufacture extra bases.

#702 must distinguish contact, input, eligibility and first displacement from the presentation handoff; preserve hang-cap, human difficulty invariance, and selection/contact-clock semantics until explicitly changed. Clock arithmetic, source hashes, accepted/pending states and local links pass. Documentation only; no runtime change or human gate pass.

## Post-contact read accepted — September 14, 2026

Jack accepted **a brief, visibly communicated read of the hit before ordinary pursuit, followed by the accepted responsive movement**. Recorded at `63abc97` in draft #706, including gameplay-spec D19, the decision register, movement report, and dataset.

Calibrate its duration against ball travel, remaining defensive opportunity, and the separate camera handoff. This does not accept Harbor's current 0.83-second outfield delay, choose a per-position schedule, add another pause after the view changes, or make sim eligibility depend on camera completion. Do not extend freezing to manufacture extra bases. Numerical timing, authored presentation, and standalone acceptance remain open; existing difficulty, assistance, and D7 constraints remain in force.

All three movement directions are now accepted: ordinary pursuit consistency, brief acceleration/quick corrections, and the visible post-contact read. Source hashes, read-clock arithmetic, decision states, empty numerical targets, document links, and whitespace checks passed. No runtime values changed or human gate passed.

## Sizing audit and next ball-readability decision

Draft #706 at `0e23222` adds the [proportions report](https://github.com/jackguillet/grand-sluggers/blob/0e23222/docs/research-game-feel-701-proportions.md) and [measurement dataset](https://github.com/jackguillet/grand-sluggers/blob/0e23222/docs/research/game-feel-701-proportions.json). Wii 01:00 silhouette brackets are about 7.0–8.6% of active image height for Boo and 13.3–16.0% for nearer Mario; these are pose/camera-dependent screen measurements, not world heights. GameCube's low-resolution contact frame and later black transition are excluded from numerical calibration. All-seven-character nominal Harbor rest head-top/basepath calculations and ball-render baseline values are retained with source hashes.

#691 is closed through merged #700 at `08eab5bba940f8ec5c5f020afac772ed16b8a367`; it fixes character scaling on possession, not baseball diameter. The new live visual baseline must name a build containing that correction. Reference world-space body/basepath ratios and exact dimensions remain unresolved.

**Accepted by Jack — F693-02 ball readability:** moderate bounded visual size assistance for distant moving balls, alongside contrast/trails, with a believable glove-fitting held size and smooth possession transition. Jack explicitly required moderation; see the acceptance record below. This is a Harbor policy, not a verified Mario scaling algorithm. Ball center, timing, physical judgments, and catch coverage remain sim-owned; no numerical diameter or camera-dependent physics is selected.

Validated source hashes, seven character marker ratios, raw pixel brackets, pending state, empty numerical targets, local links, and diff whitespace. Documentation only; no runtime, camera, model change, or human gate pass.

## Moderate ball-readability assistance accepted — September 14, 2026

Jack approved visual size assistance for distant moving balls with the explicit qualification: **“make it moderate.”** Recorded in draft #706 at `37f80e4`, including gameplay-spec D19, the decision register, proportions report, and dataset.

Use moderate, bounded enlargement alongside contrast/trails, with a believable glove-fitting held size and smooth possession transition. It should support tracking without becoming a prominent growth effect, dominating the character, or producing distracting size changes. Exact diameters, distance response, bounds, and smoothing remain pending comparison and playtesting; existing baseline sizes are not accepted by this direction.

Ball center, physical trajectory, travel time, catch coverage, and judgments remain sim-owned. Preserve #691's stable character stature on possession. Documentation validation passed: accepted direction and explicit qualification, empty numerical targets, source hashes, character ratios, local links, and whitespace. No runtime or visual sizes changed; human gates remain open.


## R2 measurement rail — draft #707, September 14, 2026

Implemented in [draft #707](https://github.com/jackguillet/grand-sluggers/pull/707), head `812a154`. The [contract](https://github.com/jackguillet/grand-sluggers/blob/812a154/docs/race-traces.md), [measured report](https://github.com/jackguillet/grand-sluggers/blob/812a154/docs/research-game-feel-702.md), and [dataset](https://github.com/jackguillet/grand-sluggers/blob/812a154/docs/research/game-feel-702-baseline.json) preserve 11 fixture observations and 150 cohort games. Stacked on #706/#704; not merged.

Version 2 adds effective-input/build identities, submitted commands, all fielders and movement gates, pre-snap receiver coverage, every throw leg, possession/reception/loose-ball events, retained runner retirement state, actual touches versus awarded bags, and explicitly labeled runner projections. The existing catalog validator rejects missing evidence, units, bounds, references, or unresolved active defaults. No gameplay, feel, or art coefficients changed.

S-31 (Crystal Rink) measures 1.033 s possession → 1.483 s sim release → 2.800 s covered reception; retired runner projection 3.458 s. S-32/33, fixed-input versus tactical Harbor trajectories, two-leg relay, cutoff, tags, uncovered waits/drops, and movement/read/pause distinctions are covered. Fault fixtures are excluded from calibration interpretation.

Verification: full solution 1,048 tests at `769a9c0`; 26 affected tests after observation review at `14ea6a5`; 18 final evidence/content checks. Three 50-game cohorts completed; Harbor seed-7 CLI trace completed; narrow Unity C# compatibility passed. Report regeneration is byte-identical. All human gates remain open.

### Accepted human decision: F693-06-H — Harbor-specific scoring scope

Jack approved on September 14, 2026: **1.8–5 mean runs per side** independently for Harbor-specific calibration and validation cohorts, separately home and away, while retaining the existing mixed-park S-29 guardrail. Recorded in draft #707 at `59f3762`, including gameplay-spec D19, the decision register, measured report, evidence catalog, and CLI report labeling.

The measured baseline remains unchanged. Harbor calibration away (1.38) and Harbor validation home (1.60) miss the newly accepted floor; these are outstanding calibration gaps, not passed gates. Individual games may fall outside the band. Preserve compact proportions, reliable routine defense, readable throws, and extra-base/relay opportunity; geometry decides outcomes. No gameplay coefficient or isolated scoring repair is selected by this acceptance. All human gates remain open.

All 18 evidence/content validation checks pass with accepted Harbor guardrails and the separate unresolved throw target still pending. Next numerical review remains the complete compact geometry/body/race proposal under R3.


### R3 spatial decision packet — #708 / draft #709

Prepared [the report](https://github.com/jackguillet/grand-sluggers/blob/a900ddc/docs/research-game-feel-708.md), [same-scale comparison](https://github.com/jackguillet/grand-sluggers/blob/a900ddc/docs/research/game-feel-708-comparison.png), machine-readable candidate records and reproducible arithmetic at `a900ddc`. Jack accepted C80 (80-ft bases, 232/280/232-ft fences, unchanged characters) as the first spatial trial on September 14, 2026. C70 remains unselected. Neither compact candidate has been simulated.

F693-02-spatial-trial is accepted. F693-05-runner-clock is also accepted. F693-03-release-clock is also accepted. F693-03-travel-clock is also accepted. Long-throw/relay behavior is now accepted with chemistry explicitly included; good-pair chemistry treatment is now accepted; the long-range numerical profile is now accepted; negative chemistry is now accepted; relay ownership direction is now accepted; Snap Throw is now accepted; Laser is now accepted; the input-buffer window is now accepted; cancel/retarget controls are now accepted; ordinary pursuit speed is now accepted; outfield read is now accepted; base-infielder read is now accepted; pitcher read is now accepted; catcher read is now accepted; ordinary pursuit acceleration is now accepted; ordinary pursuit braking is now accepted; full pursuit reversal is now accepted; angled pursuit correction is now accepted; fine analog pursuit response is now accepted; neutral/manual pursuit boundaries are now accepted; stable pursuit calibration policy is the next pending decision. Remaining running-path details, special throw interactions, common pursuit/read/coverage, flight/roll/wall and presentation numbers remain separate pending decisions. #708's full-contract work and #693's human gate remain open. Analytical speed sensitivities expose the routine-defense/deep-chase coupling; no derived speed is an approved coefficient.

Validation: arithmetic regeneration/check and local links passed; comparison diagram rendered and inspected; no gameplay values changed.


C80 acceptance is recorded in the gameplay spec, decision register and candidate data at `e13f383` / draft #709. Next recommendation: preserve current runner elapsed pace for the compact trial (Run-5 nominal 2.95-second bag interval plus 0.5-second batter startup, about 3.45 seconds to first; current stat differences and dash schedule). Jack subsequently approved this runner calibration anchor on September 14, 2026. Holding world speed instead would produce about 3.12 seconds in the simplified 80-foot model. These are analytical trial anchors, not compact simulation or measured Mario-equivalent results.


### Runner anchor accepted; ordinary release next — `090a14c` / #709

Jack approved retaining approximately 3.45 seconds from contact to first for a middle-speed runner without dash (2.95-second nominal straight bag interval + 0.5-second startup), with current stat differences and dash. Spec, register and candidate data now record F693-05-runner-clock as accepted for C80 calibration. Exact path arrivals and all human gates remain open.

Jack subsequently accepted F693-03-release-clock: a 0.30-second ordinary clean-possession command-to-release interval, with immediate visible motion onset. This is authored trial timing—not a measured Mario duration. Flight time will be reviewed separately; do not append the release delay blindly to current races. The report preserves source limitations, the existing authored 0.18-second marker, and explicitly hypothetical full-race arithmetic. No runtime values changed.


### Ordinary release accepted; infield travel next — `c710e66` / #709

Jack approved the 0.30-second ordinary release baseline on September 14, 2026. Jack subsequently approved 0.90 seconds of ordinary neutral-arm flight over 80 feet, proportional to distance in the infield: 40 ft / 0.45 s, 80 ft / 0.90 s, 120 ft / 1.35 s. Add the accepted release once; target arrival still requires eligible reception. This is an authored calibration anchor, not a measured Mario velocity or compact-game result.

Long-throw range and relay behavior remain a separate next decision; do not extrapolate the near-field rate as an approved whole-field curve. All full-race, scoring, implementation and human gates remain open. Report arithmetic and eleven local links checked; no runtime values changed.


### Infield travel accepted; long-throw behavior next — `57d1742` / #709

Jack approved the 0.90-second / 80-foot ordinary infield flight anchor on September 14, 2026, with proportional infield times and character arm differences. Release remains a separate accepted 0.30 seconds. Jack subsequently approved moderate, smooth, arm-dependent long-range loss of pace, explicitly adding character chemistry: direct throws remain available, while a well-positioned relay can sometimes be faster after paying its actual transfer/release cost. Exact comfortable range and slowdown follow numerically; relay continuation/hold/retarget ownership is tracked separately.

The report compares the current flat 200-foot CPU routing threshold with the actual human direct/cutoff branches, cites both Nintendo booklets for the cutoff verb, and separates that evidence from the proposed original long-throw behavior. Constant-speed relay break-even arithmetic is explicitly hypothetical. No candidate simulation, runtime tuning or human gate pass. Thirteen local links and all report arithmetic checks pass.


### Long-throw direction accepted with chemistry — `a3188f7` / #709

Jack: “absolutely agree. note that character chemistry can come into play here.” The spec, register and machine-readable decisions record that qualification. Direct-versus-relay calibration must include actual pair relationships as well as arm, distance and real transfer/coverage costs.

Jack subsequently approved retaining Harbor’s existing 1.30x good-chemistry travel-speed boost once per actual thrower–receiver pair, equally for compatible direct throws and each relay leg; retain the 0.30-second ordinary release and add no chain-wide bonus or separate unlimited-range reward. An 80-foot neutral flight of .90 s becomes .692 s (.992 s including release). Exact long-range curve, negative chemistry, special stacking and relay controls remain separate decisions. This 1.30x value is verified Harbor baseline code, not a claimed universal Mario law. Seventeen report links and all analytical examples check; no runtime tuning or compact simulation.


### Good-pair chemistry accepted; numerical range profile next — `ace360f` / #709

Jack approved the 30% travel-speed bonus per actual pair, with ordinary release unchanged. Jack subsequently approved this trial profile: middle-arm comfortable range 160 ft, varying 5 ft per Field point; extra neutral flight = .60*(max(0,distance-range)/80)^2 seconds. Keep the accepted infield rate and existing arm-speed mapping; chemistry divides full flight once by 1.30. This is an original trial curve, not a Nintendo measurement.

The committed report includes 30 distance/arm cases with neutral/good direct and four relay-pair combinations. At 240 ft, neutral middle direct is 3.60 s and the ideal midpoint relay is 3.50 s including an illustrative, unapproved .20-second decision gap. Strong-arm direct is about 3.05 s; middle good-pair direct about 2.84 s. Actual cutoff readiness, deeper parks, special effects, negative chemistry and human control remain validation/design work. Arithmetic regeneration, accepted-anchor checks and seventeen local links pass; no runtime tuning or compact-game simulation.

### Long-range numerical profile accepted; bad chemistry next — `6627f87` / #709

Jack approved the ordinary long-throw trial: comfortable range `R = 160 + 5*(Field-5)` feet; speed `v = (80/.90)*(.85+.03*Field)` ft/s; neutral flight `d/v + .60*(max(0,d-R)/80)^2` seconds. Apply good-pair flight /1.30 once and add the separate .30-second ordinary release. This is an accepted authored trial, not a measured Nintendo formula or a tested compact game.

Jack subsequently accepted F693-03-negative-chemistry: replace the chemistry-specific random slant with predictable .90x travel speed per actual bad pair, leaving ordinary release unchanged. An 80-foot middle-arm throw would take 1.00 second in flight, 1.30 including release. General accuracy, bobbles and special effects remain separately governed. This deliberately departs from the Wii booklet's occasional off-target chemistry throws; the alternative is to retain that volatility and calibrate its chance/severity. Current Harbor slant behavior is 20% chance, .70x speed and 10–14-foot lateral miss, not a verified Mario coefficient.

The subsequent acceptance is recorded below; relay ownership remains pending. Reproducible arithmetic, approval provenance, eighteen local links and whitespace checks pass. No runtime values changed; full calibration, relay controls and human gates remain open.

### Bad chemistry accepted; relay ownership next — `0c4e383` / #709

Jack answered yes to F693-03-negative-chemistry: predictable .90x travel speed per actual bad pair replaces chemistry's extra random slant. Divide full otherwise-calibrated flight by .90 once; ordinary release remains .30 seconds. General accuracy, bobbles and special effects remain separately governed. The deliberate departure from Wii's occasional off-target chemistry throws is recorded in the spec, report, register and candidate data. No runtime change is activated here.

Jack subsequently accepted F693-03-relay-ownership: each human relay leg needs its own deliberate throw command, with early-input buffering for the next receiver. Without an onward command the cutoff holds; allow cancellation or target changes before the onward release motion begins. The numeric buffer window and cancel-button mapping remain pending. Current code automatically continues only when a bag was explicitly armed; the proposal changes that convenience. Both Mario manuals document a cutoff choice, but the cited passages do not establish this exact onward-command/buffering contract.

The proposal preserves actual secure reception, recovery and the .30-second ordinary release per leg, and requires one-use commands tied to the expected receiver/play. Missed reception, changed receiver or seat, and play end invalidate the queue. Apply the ownership principle consistently to double plays and either human defensive seat; CPU defense still chooses its throws. Special-throw interactions follow as a separately tracked decision.

Validation: reproducible report regeneration/check, accepted-anchor provenance, chemistry arithmetic, nineteen local report links and whitespace checks pass. Full compact simulation, implementation and human gates remain open.

### Relay ownership accepted; Snap Throw next — `b2c3bee` / #709

Jack approved F693-03-relay-ownership: one deliberate throw command per human relay leg, with early-input buffering, hold without an onward command, and cancel/retarget before onward release motion. Actual secure possession and required recovery precede release; a queued command is consumed once for its expected receiver/play. This supersedes automatic armed-route continuation as the target rule. Exact buffer duration and cancel mapping remain pending and explicitly queued after the ability decisions.

Jack subsequently accepted F693-03-snap-throw: replace the existing universal 1.22x flight-speed bonus with a .22-second release after cleanly receiving a teammate throw, versus .30 ordinary. Flight still uses ordinary arm, long-range and pair chemistry. Ground pickups, batted-ball catches and loose recoveries retain ordinary release. The proposal keeps the existing Vale/Frost/Pip/Pewter assignments and makes the substantial balance change explicit. A Field-5 neutral 80-foot return would take 1.12 seconds command-to-target versus 1.20 without the ability; chemistry affects flight only. The benefit is .08 seconds per eligible release, not a free catch or automatic next throw.

The original GameCube booklet and player guide support the Quick Throw role in relays/double plays, not our exact .22-second value or received-only trigger. The report labels those as authored trial choices and discloses the lack of matched Wii/GameCube ability timing. Laser and exact buffer/cancel details follow individually.

Validation: report regeneration/check, accepted relay provenance, Snap Throw arithmetic, 22 local report links and whitespace checks pass. No runtime, roster or couch-control values changed; full calibration and human gates remain open.

### Snap Throw accepted; Laser next — `2a8486b` / #709

Jack answered sure to F693-03-snap-throw: .22-second release after cleanly receiving a teammate throw replaces the universal 1.22x flight-speed boost. Ordinary batted-ball catches and ground/loose pickups retain .30 seconds; uninterrupted possession and required recovery govern eligibility. Actual pair chemistry affects flight only. The spec, report, register and candidate data now record the accepted trial definition. Runtime, roster, authored motion and human gates remain unchanged.

Jack subsequently accepted F693-03-laser-throw: replace the universal 1.45x bonus with 1.25x travel speed on actual throws home with a live unresolved runner on third or the third-home segment. Check at release-motion start, lock for that throw, and preserve ordinary release. Apply pair chemistry once to full otherwise-calibrated flight, including the long-range contribution; no range expansion or whole-relay bonus. A cutoff feed gets no Laser merely because the final destination is home. Good-pair Laser combines to 1.625x travel speed.

The report cites the original GameCube player guide for the home-plate specialty, while labeling the exact coefficient, trigger boundaries and stacking as authored choices; matched Wii/GameCube Laser measurement remains unavailable. It documents the substantial reduction from the current universal ability and the need to validate sacrifice flies, tag-ups and scoring. Field-5 neutral 240-foot command-to-target is 2.94 seconds with Laser versus 3.60 ordinary; actual Field-3 Brondo is about 3.205 versus 3.932. These are trial arithmetic, not simulated outcomes.

Validation: report regeneration/check, Snap Throw acceptance provenance, nine Laser examples, 25 local links and whitespace checks pass. Exact input-buffer/cancel details follow the Laser decision. No runtime tuning or human gate passed.

### Laser accepted; early-input window next — `683c854` / #709

Jack approved F693-03-laser-throw: 1.25x travel speed on actual throws home with a live unresolved runner on third or the third-home segment, replacing the universal 1.45x bonus. Eligibility is checked at release-motion start and locked for the throw; pair chemistry applies once to full flight, ordinary release remains .30 seconds, and a cutoff feed gets no bonus merely because the ultimate destination is home. Spec, register, report and candidate records now mark the authored trial as accepted. Runtime and human acceptance remain open.

Jack subsequently accepted F693-03-input-buffer: remember a deliberate early onward-throw press for .25 seconds of active simulation time. Start release immediately when the intended receiver has secure possession and completed required recovery within that window; expire otherwise. This is input tolerance, not extra delay. A ready fielder acts immediately, and expiry cannot truncate an already-started release. A missed catch/bobble, receiver/seat change or play end clears the command. Exact cancel/retarget mapping and feedback remain a separate queued decision.

A clean .20-second early press works; a .30-second early press expires. A .20-second early press followed by .10 seconds of mandatory receiver recovery also expires rather than bypassing recovery. The report compares .15/.25/.40-second choices and explicitly states that neither Mario booklet provides this numeric window. Current throw-flight dispatch and armed automatic continuation are audited separately from the proposed queue.

Validation: reproducible report checks, Laser acceptance provenance, four buffer boundary/readiness examples, 26 local links and whitespace checks pass. No runtime control changes or human gate passed.

### Early-input window accepted; cancel controls next — `08c1ced` / #709

Jack answered yes to F693-03-input-buffer: .25-second maximum active-play input age until release-motion readiness. Execute immediately with secure possession and completed recovery inside that window, otherwise expire. Ready commands incur no added delay; buffer expiry never truncates a started release. Receiver/play/seat binding and invalidation remain mandatory. The spec, register, report and candidate data record this accepted numerical trial. Runtime and human responsiveness acceptance remain open.

Jack subsequently accepted F693-03-throw-cancel: fresh RB / period cancels a queued defensive throw; existing bag-selection controls retarget before release motion, without refreshing buffer age. Cancel clears the queued command but keeps the selected bag. A new throw press is required afterward. Distinguish selected-only from queued in the existing target indicator, including immediate state change on cancel/expiry. The exact visual layout remains the presentation owner's work.

The audit explicitly finds RB is not globally unused: offense all-return, pitch cycling and Charge+RB item input exist; the field input path can use Item for buddy toss. While a defensive queue exists, cancel consumes its press before conflicting defensive item/throw actions. Preserve no-queue role behavior and offense ownership. Use a typed defensive cancel, not the runner AllReturn flag. Once release motion starts, no undo or redirect. This mapping is authored for the current controls, not verified Mario behavior.

Validation: report regeneration/check, buffer approval provenance, boundary arithmetic, 29 local report links and whitespace checks pass. No runtime or couch-input edits. Shared movement/coverage and ball calibration remain after this control decision; #693/#708 and all human gates stay open.

### Cancel controls accepted; ordinary pursuit speed next — `a700f32` / #709

Jack answered yes to F693-03-throw-cancel: contextual fresh RB / period cancels a queued defensive throw; existing bag selectors retarget before release motion without refreshing the .25-second buffer. Cancellation retains selected-only target state and requires a fresh throw command afterward. Selected/queued feedback and item-input precedence, seat isolation and no-undo boundaries are recorded in the spec, register, report and candidate data. Runtime controls remain unchanged.

Jack subsequently accepted F693-02-pursuit-speed: ordinary Run-5 pursuit top speed 18 ft/s, with the existing relative stat curve scaled by 18/30.5 and the already-approved common profile across positions/hit classes. Run 1/5/9 becomes approximately 13.51/18/22.49 ft/s. Read, acceleration/braking, dash/status and coverage are explicitly separate next decisions; baserunning anchors remain unchanged.

The report exposes the incompatible sensitivities rather than claiming all old chase times survive: preserving scaled infield ground pursuit suggests 27.11 ft/s; preserving old CF-to-wall air pursuit suggests 12.81. At 18, the compact CF gap takes 3.69 seconds versus historical 5.19, while a hypothetical 18-to-16-foot infield chase grows from .59 to .89 seconds. These are constant-speed analytical examples excluding read, acceleration, interception, reach and pickup. The 16/18/20 alternatives and required routine-defense/deep-hit checks are documented. 18 ft/s is an authored trial, not measured Mario speed or a simulated game result.

Validation: report regeneration/check, cancel acceptance provenance, stat/travel/sensitivity arithmetic, 31 local links and whitespace checks pass. No runtime tuning, standalone build or human gate pass. Full movement/ball calibration and #693/#708 remain open.

### Ordinary pursuit speed accepted; outfield read next — `6cf0ed3` / #709

Jack answered yes to F693-02-pursuit-speed: 18 ft/s ordinary Run-5 top speed, scaling the existing Run curve by 18/30.5 and using the already-approved common profile across positions/hit classes. Relative character differences remain; baserunning anchors are unchanged. Read, acceleration/braking, dash/status and coverage are separate decisions. Spec, register, report and candidate data record the accepted trial; no runtime movement changed.

Jack subsequently accepted F693-02-outfield-read-clock: .40 seconds from contact for the base LF/CF/RF pursuit read, replacing .83. No additional camera wait. Retain contact-clock eligibility, human/CPU ownership structure and existing airborne hang cap; infield/pitcher/catcher timing follows separately. This is ordinary pursuit timing, not a new universal action freeze or permission to delay every catch.

The proposal deliberately shortens community-reported GameCube timing; Wii's matching interval remains unmeasured. Earlier eligibility adds .43 seconds, or up to 7.74 feet at 18 ft/s before response/path effects. Read plus the ideal CF-to-wall travel sensitivity is about 4.09 seconds for C80 versus historical 6.02. These are analytical comparisons, not simulated chases, and expose pressure on compact deep-hit opportunities. Full ball/read/pursuit/coverage calibration remains required.

Validation: report regeneration/check, pursuit-speed approval provenance, read/travel arithmetic, 34 local links and whitespace checks pass. No runtime or camera tuning, standalone build or human gate pass. #693/#708 remain open.

### Outfield read accepted; base-infielder read next — `763a5e5` / #709

Jack approved F693-02-outfield-read-clock: .40-second base pursuit read from contact for LF/CF/RF, no extra camera wait, with the documented contact-clock ownership and existing airborne cap. This is ordinary pursuit eligibility, not a new blanket action freeze or acceleration value. The spec, register, report and candidate data now record acceptance; runtime and human acceptance remain open.

Jack subsequently accepted F693-02-infield-read-clock: common .25-second read for 1B/2B/SS/3B. Current values are .27/.25/.28/.30, so 2B stays unchanged and the others gain .02–.05 seconds. At trial Run-5 top speed these gains correspond to at most .36/.00/.54/.90 feet before acceleration and path effects. This small change does not compensate for all of the slower pursuit cost. Pitcher and catcher are separately queued next, followed by response and coverage.

The report distinguishes the authored common timing from retained community GameCube rows and unresolved Wii timing. It preserves contact-clock ownership, airborne cap, held-direction response and separate action/cover paths. Current fielding-view timing can occur after infield eligibility; presentation must make early action legible without adding a sim camera gate.

Validation: report regeneration/check, outfield approval provenance, four-position arithmetic, 36 local links and whitespace checks pass. No runtime, camera or standalone changes; #693/#708 and human gates remain open.

### Base-infielder read accepted; pitcher read next — `c19ee44` / #709

Jack approved F693-02-infield-read-clock: shared .25-second base ordinary pursuit read from contact for 1B/2B/SS/3B, with the previously documented contact-clock, airborne-cap and ownership semantics. Pitcher and catcher remain separately reviewed. Spec, register, report and candidate data record acceptance. No runtime or camera changes are activated.

Subsequently accepted by Jack, F693-02-pitcher-read-clock: .35-second base pitcher pursuit read from contact, compared with current .42 and accepted base-infielder .25. Keep the same eligibility structure and actual geometric catches. Do not add the delay after follow-through or camera completion, create comebacker immunity, or introduce a separate fixed delivery-recovery timer. Any real action prerequisite must be traced separately rather than blindly summed. The authored delivery-to-field transition must agree with physical eligibility.

The proposal preserves the existing BuntDefense formation and live P/C/corner routes, requiring a consumer audit with the slower pursuit curve. The .07-second reduction gives only 1.26 feet of potential movement at Run-5 top speed before acceleration/path effects. The retained GameCube community report is about .417 seconds under a 60 Hz assumption; .35 is an authored shorter trial, with Wii timing still unmeasured. Catcher timing follows separately.

Validation: report regeneration/check, infield acceptance provenance, pitcher/read arithmetic, 41 local links and whitespace checks pass. No runtime tuning, standalone build or human gate pass. Full compact-game calibration and #693/#708 remain open.


### Pitcher read accepted; catcher read next — `e4a602f` / #709

Jack approved F693-02-pitcher-read-clock: .35-second base pitcher pursuit read from contact, without an added camera or follow-through delay. Shared contact-clock, airborne-cap and ownership semantics remain; actual action prerequisites are separately traced, not blindly summed. Preserve geometric catches and existing bunt formation. Spec, register, report and candidate data record acceptance.

Subsequently accepted by Jack, F693-02-catcher-read-clock: .45-second base catcher pursuit read after batted contact, down from .67. This leaves .10 seconds more than the pitcher and .20 more than base infielders. Start at contact, not after standing or a camera cut. Do not add this read to pitch receiving, steal throws, received fielding throws at home or tags. The separate catcher steal-release clock remains a full-contract audit item; its current numbers are not accepted here.

Keep the actual catcher start and existing bunt routes. The .22-second earlier read provides up to 3.96 feet of extra movement opportunity at Run-5 top speed, before acceleration/path effects. An illustrative unchanged 15-foot chase takes read plus top-speed travel from 1.16 seconds historically to 1.28 in the trial, so earlier eligibility alone does not prove faster bunt defense. Test real bunts, short pops, fouls, motion transitions and full races. The retained GameCube community report is about .667 seconds at an assumed 60 Hz; Wii catcher timing remains unmeasured. The .45 recommendation is an authored trial.

Validation: reproducible report/source hashes, acceptance provenance, catcher arithmetic, 45 local links and whitespace checks pass. No runtime change, candidate simulation, standalone build or human gate pass. #693/#708 and the full-contract calibration remain open.


### Catcher read accepted; acceleration next — `d7aaaad` / #709

Jack approved F693-02-catcher-read-clock: .45-second base catcher pursuit read after batted contact, without an added stand-up/camera wait or a new delay on pitch receiving, steal throws, received fielding throws at home or tags. Shared ownership, contact-clock and airborne-cap semantics apply; actual catcher start and bunt routes remain. The distinct catcher steal-release clock is still a full-contract audit item. Acceptance is recorded in the spec, register, report and candidate data.

Subsequently accepted by Jack, F693-02-pursuit-acceleration: a linear .20-second build-up from rest to ordinary full running speed, with movement starting on eligibility and active movement intent. Same duration across characters at their respective accepted top speeds, across positions/hit classes and manual/assisted/CPU pursuit. No precharge during a read or while idle; switching must retain an already moving fielder's physical state rather than reset the ramp or grant full speed. Braking, turning, partial-stick transitions, dash/status, carrying and coverage remain separate decisions.

Run-5 reaches 9 ft/s at .10 seconds and 18 ft/s at .20, traveling 1.80 feet during the full ramp versus 3.60 for an instant start. This adds .10 seconds to a long enough straight chase; an illustrative 16-foot chase is .989 instead of .889 seconds, excluding read, interception and pickup. Shared simulation prediction and stepping must integrate the response and eligibility boundaries consistently. Rendered facing alone cannot supply physical acceleration. The re-opened Nintendo manuals provide fielding controls without a calibrated ramp; .20 and the linear shape are authored trials, with matched GameCube/Wii acceleration evidence still unresolved.

Validation: report regeneration/check, source hashes, catcher acceptance provenance, ramp/character/timeline arithmetic, 48 local report links and whitespace checks pass. No runtime tuning, candidate simulation, standalone build or human acceptance. Full-contract work and #693/#708 stay open.


### Acceleration accepted; braking next — `41cc660` / #709

Jack approved F693-02-pursuit-acceleration: linear .20-second ordinary pursuit build-up from rest, with movement beginning when eligible and requested, same duration across characters at their respective speeds. No read precharge, extra standstill or selection-based velocity reset. Shared simulation prediction and stepping must agree. Spec, register, report and candidate data record acceptance.

Subsequently accepted by Jack, F693-02-pursuit-braking: linear stop from ordinary full speed in .10 seconds, using character braking magnitude V/.10. Run-5 stops over .90 feet from full speed, or .225 feet in .05 seconds from half speed. Assisted fixed-target routes plan braking before arrival using real velocity; no snap-back, extra catch reach or post-arrival wait. Neutral stick retains existing pursuit assistance. Moving catches remain geometric without a new zero-speed prerequisite. Turning, partial-stick transitions, dash/status, carrying/coverage and the existing handoff coast remain separate reviews.

The two official Mario fielding-control passages were re-inspected; neither supplies a numeric stop curve, and no matched measured stop is established. This is an authored trial. An illustrative 16-foot rest-to-rest route with .20 start/.10 stop takes 1.039 seconds excluding read and action, not a simulated catch. Full races and standalone feel still require validation.

Validation: report regeneration/check, approval provenance, source hashes, braking/partial-speed/arrival arithmetic, 51 local links and whitespace checks pass. No runtime tuning, candidate simulation, standalone build or human gate pass. #693/#708 remain open.


### Braking accepted; full reversal next — `7ed9ec8` / #709

Jack approved F693-02-pursuit-braking: linear .10-second stop from ordinary full speed, constant braking magnitude per character, shorter stops from lower speed and planned assisted arrival. Neutral-stick assistance and geometric moving catches remain. Spec, register, report and candidate data record acceptance.

Subsequently accepted by Jack, F693-02-pursuit-reversal: for sustained exact opposite-direction intent, use the accepted .10-second brake followed immediately by the accepted .20-second acceleration. No additional pivot pause. At Run-5 full speed, wrong-way runout is .90 feet; opposite movement begins after .10 seconds and reaches full speed at .30 seconds. This is physical response to input, not an uninterruptible .30-second command lock. Changed intent acts from current velocity; selection does not reset movement or the read. Angled course corrections follow individually and do not automatically inherit a full stop.

Current BodyFacing heading is presentation only. Prediction and stepping must share the physical response and timing boundaries; authored motion cannot add a pivot/camera wait. Catch geometry remains active. No matched measured GameCube/Wii reversal is established; .30 is derived from accepted Harbor trials. Full/partial-speed arithmetic, axes/diagonals, interrupted reversals, short-pop/liner/bunt misreads and rolling-ball replans are required validation cases.

Validation: report regeneration/check, braking provenance, reversal arithmetic, 54 local report links and whitespace checks pass. Source hashes now also cover BodyFacing, FieldAssist and FieldingPursuit. No runtime tuning, candidate simulation, standalone build or human gate pass. Full-contract work and #693/#708 remain open.


### Reversal accepted; angled correction next — `1ec5d4c` / #709

Jack approved F693-02-pursuit-reversal: .10-second full-speed brake followed immediately by .20-second acceleration in the opposite direction, without extra pivot pause or an uninterruptible input sequence. Existing velocity and geometric catches remain authoritative. Spec, register, report and candidate data record acceptance.

Subsequently accepted by Jack, F693-02-pursuit-angled-turn: continuously redirect velocity through the accepted braking/acceleration rates, with no angle threshold. Full-speed 45-degree correction completes in .115 seconds with 92.4% minimum speed; 90 degrees takes .212 seconds with 70.7%; exact reversal remains .300 seconds through zero. Small turns keep most speed, sharper turns lose more. Response starts immediately; completion time is not input latency.

The report specifies a straight segment between current and requested velocity, using V/.10 on its speed-decreasing portion and V/.20 afterward. The physical ground route curves; prediction and stepping share current velocity and integrate phase boundaries. The rule recovers accepted start/stop/reversal, is rotation-invariant and cannot exceed the cap when endpoint speeds respect it. No new catch-alignment requirement or facing/camera wait. Partial-stick, dash/status, handoff and coverage still follow separately.

Official GameCube/Wii control passages were re-opened; no measured physical angled-turn curve or matched known-input captures establish these numbers. This is authored trial arithmetic. Validation: report regeneration/check, reversal provenance, angle timing/minimum-speed/chord/cap checks, 58 local links and whitespace pass. No runtime tuning, candidate chase simulation, standalone build or human gate pass. #693/#708 remain open.


### Angled correction accepted; fine analog control next — `9ebf0f6` / #709

Jack approved F693-02-pursuit-angled-turn: continuous correction through the shared brake/start velocity response, preserving most speed on small turns and slowing more toward reversal, with no angle threshold, command lock or new catch-alignment requirement. Spec, register, report and candidate data record acceptance.

Subsequently accepted by Jack, F693-02-pursuit-analog-response: linear proportional speed across the active radial stick range beyond neutral. Quarter/half/full usable travel requests Run-5 4.5/9/18 ft/s. Half usable range means halfway from the neutral boundary to full tilt, not raw half tilt. Feed target velocity through accepted physical rates; rest-to-half takes .10 seconds, full-to-half .05. Numerical neutral radius is explicitly null and remains the next decision with assistance/drift handling.

Input audit: StickPlay filters individual axes below .32 and may relearn a steady input inside its .45 radial recenter region after .12 seconds; FieldAssist separately gates with |x|+|y| at .35. These baselines are not accepted thresholds and cannot supply a continuous radial range merely by adding another remap afterward. Preserve intentional small tilt through a contextual pursuit vector in the existing input owner; retain reviewed drift/held-at-SET protection, cap combined movement, and avoid changing base/throw/pitch aim. Neutral assistance remains; keyboard gets no new walk modifier.

No measured GameCube/Wii tilt-to-speed curve is established; this is authored control shaping. Validation: source hashes (now including StickPlay and feel table), turn approval, analog target/transition arithmetic, 63 local links and whitespace checks pass. No runtime/input edits, candidate simulation or human gate pass. #693/#708 remain open.


### Analog response accepted; neutral boundary next — `c4077b3` / #709

Jack approved F693-02-pursuit-analog-response: linear proportional pursuit speed over active radial stick travel, through the accepted physical response, preserving deliberate fine movement and neutral assistance. Spec, register, report and candidate data record acceptance; numerical neutralRadius remains null pending this next decision.

Subsequently accepted by Jack, F693-02-pursuit-neutral-boundary: on an armed calibrated pursuit vector, manual entry at radial magnitude >=.20, exit to existing assistance at <=.15, retaining the current owner between them. Manual speed maps from .15 to 1. Entry requests Run-5 1.06 ft/s; retained manual .18 requests .64 ft/s. After release, .18 stays assisted. No timed debounce or velocity reset; state is per seat/device and survives glove selection. Assistance may continue running on release, so this is not a stop command.

Input coordinates and single remapping are explicit. Unity processor documentation supports auditing ReadValue/device processing before choosing the pursuit input path; it does not establish the effective current axis curve. Numerical drift/center learning, held-at-SET and device-binding lifecycle follow next. .20/.15 are authored pad trials, not measured Mario thresholds or proof of drift tolerance.

Validation: report regeneration/check, analog approval provenance, neutral-state/target arithmetic, 65 local links and whitespace pass. No runtime/input change, candidate simulation, standalone build or human gate pass. #693/#708 remain open.


### Neutral boundary accepted; calibration policy next — `9ca738e` / #709

Jack approved F693-02-pursuit-neutral-boundary: manual entry >=.20, assisted exit <=.15, retained ownership between them on the calibrated radial coordinate. Manual speed remaps from .15; the accepted analog proposal now records neutralRadius=.15 and its decision provenance. Per-seat/device ownership and physical velocity remain distinct.

Next pending decision, F693-02-pursuit-calibration-policy: establish a valid released-stick center outside live play and keep the device profile fixed through the match. Recalibration is explicit while paused, adopted only on valid samples; failure retains the prior profile. Never silently learn steady live input as center or widen .20/.15. Calibration sample criteria, arming/held-at-SET and reconnect lifecycle remain separate decisions before implementation. Any new couch action needs its own presentation/book work.

Audit correction: StickPlay.Mag is Euclidean, so its .32/.45 center/recenter regions are radial, not L1 as prior prose stated. FieldAssist takeover and the stillness delta use axis sums. Earlier tracker wording is corrected; no accepted design number changes. Controls also samples a vector for center learning but child axes for movement, requiring a consistent processor/coordinate audit.

The GameCube booklet explicitly warns about off-center controls establishing incorrect startup neutral and describes a released-control reset. This supports the principle, not our match-long scope; Wii live calibration remains unverified. Validation: report/check, neutral acceptance and analog-origin linkage, 68 local links and whitespace pass. No runtime/input edits or human gate pass. #693/#708 remain open.

