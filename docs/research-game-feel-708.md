# Compact field proposal — first numerical decision

September 14, 2026. [#708](https://github.com/jackguillet/grand-sluggers/issues/708), under [#693](https://github.com/jackguillet/grand-sluggers/issues/693). **Gameplay research/documentation. No runtime tuning.** Stacked after the #702 measurement work at `59f3762` / draft [#707](https://github.com/jackguillet/grand-sluggers/pull/707).

**Accepted first trial: C80, an 80-foot diamond and 232 / 280 / 232-foot fences. Jack approved on September 14, 2026.** Compare C70, a 70-foot diamond with 203 / 245 / 203-foot fences, as the stronger alternative. Preserve character stature in both. C80 is an approved original spatial trial; C70 remains an unselected alternative. Neither is measured Nintendo geometry or approved shipping defaults.

This packet starts R3's numerical review. It supplies the coordinated spatial proposal, its race dependencies, analytical sensitivities and outstanding decisions. **It is not yet a complete implementable game contract.** Jack chooses one decision at a time; selecting a spatial trial does not approve its future movement, throwing, flight or presentation coefficients. R3 stays open until those quantities have also been reviewed together and validated.

Machine-readable [candidate inputs](research/game-feel-708-candidates.json), [derived arithmetic](research/game-feel-708-derived.json), and [reproduction script](../tools/compact-field-report.py) accompany this report. No compact profile has been simulated or played.

![Same-scale ground plans and nominal character proportions](research/game-feel-708-comparison.png)

## Accepted decision — lead spatial trial

**F693-02-spatial-trial — accepted by Jack, September 14, 2026:** C80 leads the subsequent numerical design and prototype. Jack replied “approve.” to the recommendation, which explicitly reserved running and throwing times for separate review. Character sizes stay unchanged. This does not accept the remaining runtime coefficients or pass a human gate.

- **C80, recommended:** 80-foot basepaths, 53.78-foot mound distance, 232 / 280 / 232-foot fences. The basepath is 11.1% shorter and center field 30% closer than the historical control. Unchanged bodies are 12.5% larger relative to the basepath. This makes a substantial outfield change while making the smaller of the two infield changes.
- **C70, stronger compact alternative:** 70-foot basepaths, 47.06-foot mound distance, 203 / 245 / 203-foot fences. The basepath is 22.2% shorter and center field 38.75% closer. Unchanged bodies are 28.6% larger relative to the basepath. This offers a stronger toy-like proportion, with more pressure on infield spacing, catch coverage, footwork and short-throw readability.
- **C0, historical control:** 90-foot basepaths, 60.5-foot mound, 330 / 400 / 330-foot fences. Retain it as the measured comparison, not as an assertion that its current feel is correct.

C80 is recommended because it delivers most of the proposed outfield compression while making a smaller simultaneous change to the infield/body relationship. That is a design judgment about calibration risk, not evidence that C80 is more faithful to Mario. C70 remains a meaningful comparison if C80 still looks too spread out. Neither is guaranteed to work, and the final dimensions may move after full-race tests and Jack's sitting.

The proposed choice includes the coherent spatial conventions below for a trial. It does **not** pass the human look/play gate, settle exact visual ball size, choose a pursuit penalty, authorize a game build with incomplete tuning, or select every later number by implication.

## What the Mario comparison can establish

The [Wii field survey](https://www.reddit.com/r/MarioSuperSluggers/comments/xdvwn9) reports one Pianta taking 3.65 seconds from third to home and 10.49 / 12.86 / 10.51 seconds from the three wall locations to home. Equal effective speed gives conditional wall/basepath ratios of approximately **2.874 / 3.523 / 2.879**. The proposed **2.9 / 3.5 / 2.9** rounds that pattern. The original author's absolute feet assume a 90-foot basepath; that assumption is not a measurement. Fielding versus baserunning speed, route, acceleration and input uncertainty could change the inferred ratio. We have not independently repeated the experiment. The [#701 geometry packet](research-game-feel-701-geometry.md) retains sensitivity cases rather than treating the ratio as exact.

The [GameCube coordinate account](https://www.reddit.com/r/MarioBaseball/comments/nt2n3v) reports roughly 100 units to center and 18.4 to the mound. The later stadium survey uses meters, but its independence from the earlier report is unverified. Project Rio exposes useful memory addresses, not the actual stock base coordinates in the inspected header. Neither source establishes a matched Wii/GameCube body/basepath scale. Therefore this packet does not select 80 or 70 feet by converting a purported Mario stature. Both basepaths are authored exploration points.

The [Wii play around 00:58](https://www.youtube.com/watch?v=3-5aQh-fxZk&t=58s) has previously annotated throw flight of **0.99–1.21 seconds**. The [GameCube force/return attempt around 03:10](https://www.youtube.com/watch?v=fdtUfRUjyAY&t=190s) distinguishes release, SAFE, reception and reset. Those clips support keeping event phases visible; they do not supply a common speed coefficient because distance, character, input and ability conditions are unmatched. See the [full comparison and annotations](research-game-feel-701-comparison.md).

Additional discovery leads during #708 were the original [Project Rio stadium mapping post](https://www.reddit.com/r/MarioBaseball/comments/suzucf) and [relative wall heights post](https://www.reddit.com/r/MarioBaseball/comments/svxabc). Search exposed their titles, but direct retrieval failed and their diagrams were not inspected. They are **excluded from numerical support** here; no value has been read from them or inferred from their titles.

Jack's accepted reference policy remains: Wii leads provisional visual readability; GameCube cross-checks mechanics. Both have been considered, but matched world geometry and full-race numerical fidelity remain unresolved. The proposal must be judged as an original game trial until stronger evidence exists.

## Spatial contract proposed for the trial

Use feet, home at `(0,0)`, +Z toward center, +X toward first. Preserve a 90-degree fair wedge. Fit the existing circular fence arc through the left, center and right posts; do not interpolate two straight segments into a pointed center field. Both compact candidates share wall/basepath ratios 2.9 / 3.5 / 2.9, versus the control's 3.6667 / 4.4444 / 3.6667. They differ in field size relative to the unchanged toys.

For the first migration, preserve the control's rounded bag centers exactly: `(63.64,63.64)`, `(0,127.28)`, `(-63.64,63.64)`. Trial centers multiply these by `B/90`, with home fixed. Thus the nominal basepath and coordinate-derived distance differ slightly, as they already do in the control; changing coordinate precision is not hidden inside a parity migration. Scale mound distance by the same factor, provisionally retaining Harbor's mound/basepath relationship. Keep the plate, batter/stance and catcher group at their current local scale. Moving the mound must preserve the existing D7 pitch-flight durations; it cannot silently speed up the pitch.

For initial defensive starts, multiply 1B/2B/3B/SS positions by `B/90`; pitcher follows the mound and catcher stays at Z = −15 feet. For LF/CF/RF, preserve bearing and the current starting-radius / fence-radius-at-that-bearing fraction. This lets outfield starts follow the actual arc rather than borrowing the infield shrink factor. All nine resulting coordinates are in the derived dataset. CF starts at Z = 305 / 213.5 / 186.8125 feet for C0 / C80 / C70. Starts remain a calibration variable; these trial positions are not evidence of balanced interception.

Preserve the **12-foot wall height** initially. Lowering both wall height and wall distance would add another home-run/rebound variable. This preservation is a control choice, not a claim about Mario's walls. Keep body size, equipment, 4-foot visible bags, 10-foot dirt path widths, 12-foot bag pads, 18-foot home pad, 15-foot warning track and 36-foot foul apron at their current sizes for comparison. Scale the inner grass diamond and rear dirt-arc radius by `B/90`; keep the mound's local shape and height. A later kit task must verify joins, fair/foul coverage, collision/dress agreement and clearances. These documentation diagrams do not edit the park or certify its geometry renders correctly.

Preserving toy-related lengths while reducing field distances is deliberate: uniformly shrinking everything would leave the desired proportions unchanged. In C80, Rio's nominal 5.108-foot head-top is **6.39%** of a basepath; in C70 it is **7.30%**, versus **5.68%** in C0. Ashlord is **10.22% / 11.68%**, versus **9.08%**. The dataset includes every captain. These are rest markers from the [#701 proportions audit](research-game-feel-701-proportions.md), not measured animated bounds or projected screen heights.

**Reach needs its own accounting.** For example, leaving a 13-foot catch-assist radius unchanged changes its basepath fraction from 14.44% to 16.25% or 18.57%. That can erase gaps even with slower pursuit. Separate physical body/glove reach, catch assistance, scoop thresholds, tag reach, cover eligibility and bag occupancy. Neither multiplying all radii by `B/90` nor leaving them all untouched is approved by this spatial choice. Record the coverage change before accepting the complete runtime contract. Moderate drawn-ball assistance must not enlarge those judgments.

## Next decision — runner elapsed pace

**F693-05-runner-clock — pending.** Recommend preserving the existing runner elapsed pace as the C80 calibration anchor. For a middle-speed Run-5 character without dash, that means a nominal **2.95 seconds per straight basepath**, plus the existing **0.5-second batter startup**: approximately **3.45 seconds from contact to first** in the simplified model. The historical S-31 projection is 3.458 seconds because actual paths and starting geometry matter. These are not measured C80 results.

On an 80-foot path, maintaining that interval reduces ordinary linear speed from 30.51 to **27.12 ft/s**. The alternative is keeping current feet per second: the same simplified first-base race drops to **3.12 seconds**, removing about **0.33 seconds** from the defense's total opportunity. That may feel more hectic and leaves less time for the visibly registering release/travel Jack wants. Preserving elapsed time gives us a stable starting race while shrinking the field; it does not prove defense is balanced or guarantee extra bases.

Preserve current stat differences and the explicit dash schedule for this comparison. Rounding, slide/reversal geometry and resulting multi-base arrival times still require full traces. This decision does not select fielding speed, slow a runner to rescue one out, introduce a new stamina mechanic or approve exact animation cadence. Later runner changes must return with evidence and a named decision.

The [Wii survey](https://www.reddit.com/r/MarioSuperSluggers/comments/xdvwn9) reports **3.65 seconds for Pianta from third to home**, but its starting/input protocol is not a matched ordinary Run-5 first-base test. GameCube's community running-mechanics research describes acceleration, dash and stamina; the inspected evidence does not establish a matched middle-speed elapsed target for both titles. We should not label 3.45 seconds a measured Mario match. The recommendation is a provisional Harbor calibration anchor to compare with stronger reference and standalone evidence.

**Question for Jack:** retain this approximately 3.45-second ordinary first-base pace for the compact trial? Running and throw/fielding budgets remain separate decisions.

## Coordinate time with space

The two compact candidates should be compared against the **same intended elapsed pace**, so the spatial preference does not accidentally become a choice between a fast and slow game. The next decision will propose whether today's runner elapsed pace should anchor that comparison. It is pending; current values are observations, not newly accepted targets.

At present, a Run-5 runner has a nominal 2.95-second bag interval, before batter startup, rounding and other path effects. Keeping that interval makes straight running speed **30.51 / 27.12 / 23.73 feet per second** across C0 / C80 / C70. That preserves the distance/time relation without making a smaller diamond automatically faster. The observed S-31 projected first arrival is 3.458 seconds, not exactly `2.95 + 0.5`; use actual runner paths and contact/arrival marks. Preserve character differences and explicit dash behavior during comparison, then review any proposed change openly.

**Jack's compact-field/slower-pursuit observation is mathematically sound, but only for the matched distance.** A constant-speed center fielder covering the control's remaining 95 feet to its center wall at 18.3 ft/s takes 5.191 seconds. At the proposed starts, C80 has 66.5 feet remaining and C70 58.1875 feet. Speeds of **12.81 / 11.21 ft/s** preserve that simplified rear-chase time.

Those speeds are **sensitivities, not selected profiles**. To preserve an equally scaled routine infield chase instead, the control's 30.5 ft/s would become **27.11 / 23.72 ft/s**. Applying the outfield-derived sensitivity to the infield would make the same proportionally scaled infield chase about **2.116 times as long**. The control currently gets two different responses through its class/position modifiers; Jack's accepted common-profile direction removes that shortcut. Read, starting position, ball arrival, reach and speed must be solved together. Acceleration and moving interception make the actual problem more complex. No constant-speed arithmetic here proves the new game will work.

This is why the next implementation cannot simply shrink the park and slow every character. It must expose the routine-grounder and deep-gap races at once. Preserve responsive control; do not manufacture a triple through extra frozen time, automatic awards or a hidden outfielder-only speed rule.

## Race budget and remaining numerical choices

The authoritative race is:

`contact → read/pursuit → actual possession → player command → release → ball at target → eligible receiver possession → geometric out/tag`

The batter/runner clock overlaps it. Read, flight and pursuit also overlap; do not sum them as if each starts after the previous one finishes. An early command may overlap transfer when the agreed input system permits it. An uncovered target is not a reception. Relay legs each need their own release and reception.

The [#702 baseline](research-game-feel-702.md) supplies useful complete reference races:

- S-31 at Crystal Rink: possession **1.033 s**, release **1.483 s**, covered reception **2.800 s**, projected retired Run-5 runner arrival **3.458 s**. The resulting **0.658 s** margin is a weak-arm named fixture, not an average-arm universal budget.
- Harbor fixed 84 mph / 8° / −12° grounder: possession **1.617 s**, release **1.967 s**, covered reception **3.217 s**. The tactical Harbor counterpart matches S-31 but solves a different exit speed. Retain both comparisons.
- Scripted human relay: releases/receptions **1.183 → 1.867 s** and **1.883 → 2.667 s**. Those extremely short between-leg events are observations to review against visible transfer, not newly accepted motion timing.

The ordinary throw law currently adds `.22` seconds to a distance-dependent duration, but samples travel over the whole duration. **That intercept is not an existing stationary windup.** The proposed release budget must represent a real release phase, retain player command ownership and account for both arm stat and short/long path lengths. Compare the resulting flight with the annotated Mario clips without claiming the clips establish feet per second. Selecting the compact geometry does not select this release duration.

For each trial, finish these quantities before treating the profile as implementable:

1. **Runner clock:** first-base startup and arrival, subsequent bags, rounding, reversal, slide and dash. First review the elapsed anchor, then resolve any material deviation separately.
2. **Ordinary defense and throws:** define an average-arm routine-grounder fixture, its command policy, possession deadline, release interval and distance-dependent flight. Require a readable positive defensive margin with correct prompt execution; difficult hits and fast runners may be close. Document CPU latency separately from human command time.
3. **Common pursuit:** speed by character stat, visible read, acceleration, stopping/turning, neutral-stick assistance, handoff coast and explicit abilities. Existing accepted directions do not silently approve their current coefficients. Cover movement must also agree with the race; do not make a receiver teleport ready.
4. **Ball motion:** fixed exit/launch trajectories and tactical gap/line/fly/hopper/wall paths; carry, hang, first bounce, rebound, rolling speed/distance, interception height and wall-crossing height. Shorten space without turning every strong hit into a homer or every deep fly into an automatic catch. Class boundaries and prediction use the same physics as the live ball.
5. **Coverage:** physical reach versus assist radii, scoop/catch windows, bag occupancy, tags and slide geometry. Report their world/body/field ratios and actual out margins.
6. **Presentation:** release marker and visual handoff, ball-size assistance bounds, camera response and routine/big-play result duration. These are separate implementation owners and later decisions; D7 remains unchanged.

There is no defensible single shrink factor for the ball model: infield and outfield contract by different factors while wall height and bodies remain fixed. For a uniform mathematical transform `x′ = kx, t′ = qt`, velocity scales by `k/q`, acceleration by `k/q²`, and quadratic-drag coefficient by `1/k`. Those relations are a diagnostic, not a solution for this field. Do not modify gravity alone or regenerate only inverse-carry fixtures and call the physics preserved.

## Proving doubles, triples and relays survive

Use full traces for a clean routine grounder, a hard liner through a positioned/mispositioned defense, a gap hit with a double opportunity, a corner/wall retrieval with a triple opportunity, a direct-versus-cutoff throw home, a fly/tag-up, and an uncovered receiver. Each names roster/hands/stats, contact input, initial alignment, player commands and expected opportunity. Include slow/middle/fast characters, both seat configurations and unchanged special-action rules. A triple opportunity need not guarantee a triple: better routes and decisions should change the outcome through geometry.

For every deep play report contact-to-possession, each throw leg, runner arrival at second/third/home, covered reception and out/tag margin. Compare the same fixed exit/launch inputs across C0 and the compact candidate to reveal changed fence crossings and retrieval. Then use tactical fixtures to investigate equivalent gap/wall intent, recording the solved exit speed. Do not replace the fixed-input evidence with tactical matches.

Retain the accepted **1.8–5 home and away mean runs**, independently for S-29, Harbor calibration and Harbor validation. The #702 control currently misses Harbor calibration away at **1.38** and Harbor validation home at **1.60**. Pooling the sets cannot pass either failure. Report singles, doubles, triples, homers, multiple-out plays and relay use alongside means; their baseline counts are observations, not imposed frequency targets. The inspected seed sets are regressions now. Predeclare another seed set before using it as unseen confirmation; do not repeatedly inspect and tune to it.

## Implementation order and review boundary

Continue Jack's decisions in the order recorded in the candidate JSON: spatial lead, runner elapsed anchor, throw budget, common movement/coverage budget, contact-class ball budget, then individual numerical presentation choices. Present a concrete recommendation, evidence limits and consequences for each; do not reopen accepted design intent. If a later budget makes the selected spatial trial infeasible, bring back the affected decision with measured failures.

Before runtime calibration, finish this packet's pending quantities and review the combined contract. Create separate implementation children. **First migrate existing geometry into the shared data owner at unchanged values and prove parity.** Audit consumers across sim bags/paths, cover, classifications/CPU thresholds, fair/foul and wall geometry, plus kit/presentation adapters. Preserve control behavior before tuning. A separate presentation/kit owner must consume the same geometry; this research session does not edit their assets or cameras.

Then implement one coherent sim calibration with the accepted full profile and all trace/cohort comparisons. Update the governing spec for deliberate rule changes. No experimental player-facing settings menu, per-hit speed exceptions, forced outcomes or unrelated scoring patch. Presentation and any art follow serially. Any new live visual baseline must contain the #691 possession-scale correction from #700; this research stack's historical runtime predates it.

Jack's final acceptance is a named standalone preview, not this schematic or a green headless suite. Inspect pad and keyboard, one and two players, small and large captains, routine plays and deep races. Record the running revision and remaining gates. #693 stays open through that acceptance; selecting C80 or C70 is permission to develop a trial, not a declaration of finished feel.

## Reproduction and verification

Run `python3 tools/compact-field-report.py --check` to compare committed arithmetic with the candidate inputs and source hashes. Use `MPLCONFIGDIR=/tmp/gs708-mpl python3 tools/compact-field-report.py --figure` to regenerate the diagram. It is an analytical schematic with identical world scale, not an in-game camera or art mockup. The lower bars compare nominal head-top/basepath ratios; they do not predict screen pixels.

Validation for this documentation step checks fence endpoints, preserved mound ratio, derived candidate geometry/body/time sensitivities, accepted spatial-trial scope, pending runner-clock status and repository links. No new runtime coefficient, simulation result, Unity build or human acceptance is claimed. The #702 tests and 150-game measurements remain historical evidence at their named revisions, not tests of C80/C70.
