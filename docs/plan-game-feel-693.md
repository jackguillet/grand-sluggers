# Game feel decision plan

## Scope and ownership

[#693](https://github.com/jackguillet/grand-sluggers/issues/693) remains open through research, reference comparison, implementation, and Jack's race re-sit. It serves #209; flight and fielding lineage stays #564/#566. The research foundation is [Field proportions and the readable baseball race](research-game-feel-693.md). Baseline: `eff10d9`; sitting: `850dd95`. Session kind for this foundation is **gameplay research/documentation**. Runtime tuning, presentation, and art are separate work items.

Jack's selection policy is **compare Wii and GameCube before choosing**. This supersedes an assumption that Wii automatically supplies every numeric coefficient. It does not supersede the existing control scheme, D7 pitch-pace hold, or human gates.

## Current deliverable

- [x] Read #693, its parent issues, the sitting issue, gameplay spec, agent rails, and debug protocol.
- [x] Verify the original booklet descriptions and inspect community research with version-specific citations.
- [x] Audit Harbor's geometry, runner, flight, throw, test, and trace owners at a named revision.
- [x] Name the dimensional relationships and event boundaries that implementation must preserve.
- [x] Retain a machine-readable baseline summary and source-file hashes.
- [x] Define staged follow-ups, alternative experiments, and human acceptance.
- [ ] Obtain a matched, annotated Wii/GameCube measurement set.
- [ ] Select a reference or explicit hybrid and accept target intervals.
- [ ] Implement and view the calibrated game.

The first six items are a research foundation, not completion of #693. A docs merge must not use a closing keyword for #693.

## Decision register

### F693-01 — Reference selection

**State:** provisional visual lead accepted by Jack on September 14, 2026: **Wii Super Sluggers leads on-screen readability; GameCube Superstar Baseball remains a mechanics cross-check**. This follows review of the first two reference clips. Numerical reference selection and target intervals remain open. Compare stock Mario Stadium conditions in both titles, recording mode, hands, stats, chemistry, stars, and capture cadence before numerical calibration.

**First observation packet:** [Wii / GameCube comparison](research-game-feel-701-comparison.md), with a [source and event dataset](research/game-feel-701-observations.json). One Wii direct grounder out and one GameCube force/return attempt support a provisional **visual-lead** discussion; they are not a matched stock-calibration sample. Jack accepted the provisional visual-lead recommendation. This selects the default reference for readability reviews, with existing controls and presentation restrictions preserved. It selects no dimensions or coefficients, and does not complete this row or #701.

**Acceptance:** each selected relationship identifies its game and source; deliberate Harbor deviations have reasons and a named human decision. Existing D1–D18 are either preserved or explicitly superseded one by one.

### F693-02 — Playable geometry versus visible proportions

**State:** geometry flexibility accepted by Jack on September 14, 2026: **both infield and outfield dimensions may change independently** to achieve the intended reference-informed proportions. The current 90-foot basepaths and existing fence depths are not locked. Jack subsequently selected the C80 spatial trial below; final shipping dimensions and calibration remain pending.

**Spatial trial accepted — F693-02-spatial-trial, September 14, 2026:** Jack approved **C80: 80-foot basepaths, 232 / 280 / 232-foot fences, unchanged character sizes**, as the first trial. See [#708’s packet](research-game-feel-708.md). Running, throws, pursuit/coverage, ball motion, final shipping dimensions and human acceptance remain open.

**Remaining evidence:** improve matched reference fence/basepath, mound/basepath, body/basepath, and projected body/ball measurements, then finish the full runtime contract. Compare the unchanged baseline with independently adjusted infield/outfield candidates. Current Harbor fences are larger than the GameCube survey; a basepath reduction is not yet established. The initial flexibility decision selected no shrink factor; Jack subsequently specified a compact outfield as the desired feel. Neither decision establishes a smaller Wii infield or accepts numerical dimensions. Shorter basepaths affect both running and throwing; outfield depth affects pursuit, long throws, and carry. Preserve reliable routine defense, D7 pitch pace, and shared geometry across sim and presentation.

**Current evidence and accepted deep-hit direction:** the [geometry packet](research-game-feel-701-geometry.md) records a conditional Wii estimate near 2.87 / 3.52 / 2.88 wall/basepath ratios and the separate GameCube coordinate lineage. C1 (2.9 / 3.5 / 2.9) is an incomplete Harbor-authored research candidate, not accepted geometry. Jack accepted on September 14, 2026: preserve doubles, triples, and relays **within a compact, cartoon-feeling outfield**. Do not make the outfield huge to preserve chase time. His slower-outfielder/smaller-field example directs coordinated space/time calibration; it does not prescribe a position-specific movement penalty. Tune pursuit, starting positions, carry/bounce/roll, running, and throws together, preserving responsive control and character differences. This selects no hit frequency, dimension, or coefficient and does not substitute for the full geometry/body and race-budget review.

**Pursuit consistency accepted by Jack, September 14, 2026:** one ordinary pursuit movement profile per character across hit types and assigned fielding positions, retaining explicit dash/ability/status effects, character speed differences, and separate baserunning tuning. The [movement audit](research-game-feel-701-movement.md) records the existing formulas and modifiers. This supersedes the current §8.1 class/position modifiers as the target design; replace them only within a complete compact-field calibration, not by simply setting them to 1. Exact speeds, acceleration, reaction rules, and numerical dimensions remain unselected.

**Movement weight accepted by Jack, September 14, 2026:** a brief build-up to running speed with quick course corrections and little residual drift when movement intent changes; see the [movement report](research-game-feel-701-movement.md#accepted-direction--f693-02-movement-weight). This selects the lighter, responsive direction, not numerical acceleration/braking timings, and changes neither reaction eligibility nor neutral-stick assistance. Compare actual physical movement separately from rendered facing before numerical acceptance.

**Post-contact read accepted by Jack, September 14, 2026:** retain a brief, visibly communicated read before ordinary pursuit, followed by the accepted responsive movement. The [movement report](research-game-feel-701-movement.md#accepted-direction--f693-02-post-contact-read) records GameCube’s community-reported control lockouts and Harbor’s separate eligibility/view-change clocks. This selects no numerical delay, per-position schedule, difficulty change, or camera-dependent sim gate. Do not use extended freezing to manufacture extra-base opportunities; numerical timing and continuous standalone acceptance remain open.

**Ball readability accepted by Jack, September 14, 2026:** moderate, bounded visual size assistance for distant moving balls, supported by contrast/trails, with a believable glove-fitting held size and smooth possession transition. Jack explicitly qualified approval: “make it moderate.” Enlargement must not become a prominent growth effect or an obvious size jump. The [proportions audit](research-game-feel-701-proportions.md) retains Wii screen-space brackets, all-seven-character nominal Harbor head-top/basepath calculations, and GameCube exclusions; reference world-space body/basepath remains unresolved. This accepts no numerical diameter or scaling bound and leaves ball physics and judgments unchanged.

**Acceptance:** one geometry owner feeds sim and kit; bags, cover, fair/foul, paths, wall collisions, and shots agree. Separate approved toy exaggerations from distances that decide plays. Do not choose a scale to accommodate one captain or one camera. The #691 possession-related character-scale correction merged in #700 at `08eab5b`; collect the final visual baseline from a named build containing it, rather than this research branch’s older runtime baseline.

### F693-03 — Ordinary throw shape and transfer

**State:** design intent accepted by Jack on September 14, 2026: **quick release and readable travel, with enough transfer/release motion for the throw to visibly register**. Jack's qualification is explicit: “but not too quick … let's refer to mario for this.” Responsiveness must preserve recognizable possession, transfer, release, and follow-through rather than imply an instant launch.

**Still pending:** compare ordinary throws in Wii Super Sluggers and GameCube Superstar Baseball to refine the selected release trial and choose travel intervals and motion curves. Include short infield throws, long outfield throws, and ground-ball pickup versus received-ball transfer. Keep chemistry, abilities, lobs, and relays separate. The design-intent approval alone selected no numerical reference or coefficient; the subsequent ordinary release trial below is now accepted. Harbor's current `releaseSec` is part of a linearly sampled total duration, not a stationary release segment.

**Acceptance:** at ordinary gameplay speed and couch distance, the transfer/release visibly registers and ball travel can be followed, assessed against the annotated Mario comparison. One sim model samples and predicts the same throw; tests distinguish command/release/reception; third/home tags and uncovered bags remain live geometry. #558 owns authored takes; it must coordinate any marker change after the sim event is decided.

**Accepted trial anchor — F693-03-release-clock, September 14, 2026:** [0.30-second ordinary command-to-release](research-game-feel-708.md#accepted-decision--ordinary-throw-release), immediate motion onset with a visible transfer, excluding human decision time and subsequent ball flight. This selects the clean ordinary baseline, not special/stat release adjustments, buffering or recovery penalties; it is not a measured Mario duration.

**Accepted trial anchor — F693-03-travel-clock, September 14, 2026:** [0.90-second ordinary infield flight over 80 feet](research-game-feel-708.md#accepted-decision--ordinary-infield-ball-travel), proportional to distance for the neutral middle arm, preserving arm differences; 1.20 seconds including the accepted release. This is an infield trial anchor, not an accepted whole-field curve.

**Accepted direction — F693-03-long-throws, September 14, 2026:** [moderate arm-dependent loss of pace on very long direct throws](research-game-feel-708.md#accepted-direction--long-throws-and-relay-usefulness), so a well-positioned relay can sometimes be faster while direct throws remain available. Jack explicitly added **character chemistry must come into play**. Comfortable range, timing curve and chemistry modifiers remain to be selected.

**Accepted trial anchor — F693-03-good-chemistry, September 14, 2026:** [retain the existing 1.30× good-pair travel-speed boost](research-game-feel-708.md#accepted-decision--chemistry-on-direct-and-relay-throws), once per actual thrower/receiver pair, equally for direct and relay legs; no extra whole-chain bonus or shortening of the 0.30-second ordinary release. Negative chemistry, special stacking and relay continuation/hold/retarget controls also require explicit review before implementation.

**Next review — F693-03-long-throw-numbers:** [160-foot middle-arm comfortable range with gradual long-range loss](research-game-feel-708.md#next-decision--comfortable-range-and-long-throw-timing), ±5 feet per Field point; 0.60 s extra neutral flight at 80 feet beyond range with quadratic growth; pair chemistry applied once to flight. Pending original trial profile. See the derived neutral/strong/weak/chemistry/direct/relay comparisons and their idealized assumptions.

### F693-04 — Contact-class motion

**State:** design intent accepted by Jack on September 14, 2026: **hard liners may reward existing positioning over post-contact reaction**. A well-struck liner may pass before a fielder can reposition, while its path remains readable. Grounders retain the pickup-and-throw sequence; flies allow pursuit and catch judgment. Do not flatten all contact speeds to provide a recovery opportunity on every hit.

**Still pending:** Wii/GameCube-informed bounds for hopper/liner/pop/fly flight, bounce, roll, wall carom, and carry. No current speed, time scale, class threshold, or guaranteed catch/miss is approved. Judge catchability from the shared trajectory and the fielder’s position/reach. This does not add a defensive-positioning control or change D18 assistance. The current 1.0/1.65 clock split and 74-mph class threshold remain baseline observations, not accepted targets.

**Acceptance:** fixed-input trajectories and tactical fixtures both pass. Contact-to-scoop, apex/hang, class separation, and wall behavior are within accepted intervals. Do not solve the complaint by hiding a rope with a fly camera or trading a physics change for an opaque CPU delay.

### F693-05 — Runner and defensive opportunity

**State:** design intent accepted by Jack on September 14, 2026: **reliable routine defense**. On an ordinary grounder with a clean pickup, an average runner, and an ordinary arm, reasonably prompt correct execution should normally retire the runner with a readable margin. Tight races arise from fast runners, deep pickups, weak arms, bobbles, or hesitation. Outcomes still follow ball/runner/glove geometry; this is not a guaranteed-out rule.

**Accepted numerical trial anchor — F693-05-runner-clock, September 14, 2026:** [preserve the existing runner elapsed pace for C80](research-game-feel-708.md#accepted-decision--runner-elapsed-pace): nominal Run-5 2.95-second straight bag interval plus 0.5-second batter startup (about 3.45 seconds to first without dash), with current stat differences and dash schedule. Derive movement speed from the shorter path. Exact path/rounding/slide arrivals still require validation. This is a Harbor trial anchor, not a measured Mario target.

**Still pending:** reference-informed timing intervals and human validation of “reasonably prompt” and “readable.” The accepted running anchor preserves elapsed pace on the shorter field; it does not approve slowing the runner clock to rescue an out. Other race coefficients remain pending. Record startup, acceleration, bag intervals, handedness, dash, pursuit, read, legal bag coverage, and decision time. A direction decision does not close the measurement or play gate.

**Acceptance:** a Run-5 routine grounder can be retired by geometry and is readable; fast/weak-arm variants remain meaningful; an unthrown human ball never becomes an automatic out. The human owns both legs of a double play. Throws and runs are assessed against the same live clock.

### F693-06 — Scoring regression band

**State:** accepted by Jack on September 14, 2026: retain **1.8–5 mean runs per side** as the S-29 regression guardrail. This applies separately to the home and away averages over the existing 50-game cohort, not to each game's score. The earlier 2–5 spec headline is reconciled to the existing test; no runtime coefficient or test threshold changes. This is a regression tolerance, not proof of reference fidelity or an accepted final scoring experience.

**Acceptance:** preserve the existing 50-game mixed-park cohort; add a Harbor-only cohort and predeclared disjoint validation seeds. Report per-side means and play kinds, fixture event budgets, and seat parity. Score alone cannot pass feel.

### F693-06-H — Harbor-specific scoring scope

**State: accepted by Jack on September 14, 2026.** Apply **1.8–5 mean runs per side** independently to Harbor-specific calibration and validation cohorts, checking home and away separately, while retaining the existing mixed-park S-29 guardrail. Individual games may fall outside the band.

[The #702 baseline report](research-game-feel-702.md) remains unchanged evidence: mixed parks 1.90 home / 1.92 away, Harbor calibration 1.90 / 1.38, Harbor validation 1.60 / 2.30. Harbor calibration's away mean and validation's home mean fall below the newly accepted target. They remain calibration work; approval of the target does not pass the baseline or select a gameplay coefficient. Preserve compact proportions, reliable routine defense, readable throws, and extra-base/relay opportunity. Geometry decides outcomes, and Jack retains the standalone feel gate.

### F693-07 — Spectator pacing and couch acceptance

**State:** design intent accepted by Jack on September 14, 2026: **brisk routine beats, on the slower/more deliberate side**, with additional emphasis for big moments. After a routine dead-ball result, give the result and a brief character reaction enough time to register before returning to play. Jack explicitly requires comparison with Mario; do not interpret brisk as an immediate reset.

**Still pending:** compare Wii and GameCube result-to-next-ready intervals at normal speed, distinguishing routine plays from home runs, great catches, and inning-ending plays. Selected durations and the standalone human gate remain open. Record simulation time separately from hit freeze, camera cut/settle, possession readability, decision time, stamps, and next-ready delay. This direction concerns completed plays; it does not shorten live player decisions, introduce full-screen interruptions, or reopen D7’s separate pitch-pace hold.

**Acceptance:** continuous standalone play with keyboard/mouse, one pad, and two pads; both halves and ownership roles; representative small and large captains; HUD-off diagnostic clips as well as the actual HUD. Jack watches the race and finishes the book-to-half path. No agent can sign this row off.

## Human review handoff

All seven initial decision areas now have a recorded direction or constraint. This does not accept their numerical targets or pass their human gates. The next human review should present measured field/body proportions and a coherent candidate race budget, with sources, uncertainties, and alternatives. Continue #701 measurement and #702 trace work under the existing authorization; do not ask Jack to choose unsupported dimensions or repeat an accepted direction. Present each remaining substantive choice one at a time with context.

## Execution sequence

### R1 — Annotate the two reference games (#701)

[#701](https://github.com/jackguillet/grand-sluggers/issues/701), research/documentation child of #693 under #209. No runtime or art edits. Follow research §7's capture manifest, event definitions, uncertainty, exclusions, and proposed screening sample. Prefer ordinary plays in comparable parks; label any special or modded footage. Get the raw frame/coordinate evidence behind the GameCube stadium claims where possible. Independently check throw units and motion; preserve unknowns if the evidence does not resolve them.

Deliver a small version-controlled annotation dataset, exact clip locators, source revisions, a comparison for F693-01–05 and F693-07, and a recommendation with tradeoffs. A screenshot does not count as elapsed-time evidence. Existing retrospective clips may lack enough metadata and must be marked accordingly. Exit is a reviewable comparison, not Jack's approval.

### R2 — Make Harbor's race measurable (#702)

The #702 implementation and reproduction contract is [race-traces.md](race-traces.md). Its version 2 observations preserve full throw/receiver chains and runner retirement evidence; the evidence catalog rejects unresolved active defaults. This is draft instrumentation until its PR merges. Numeric design choices and human acceptance remain open.

[#702](https://github.com/jackguillet/grand-sluggers/issues/702), gameplay child of #693 under #209. Extend the existing `PlayTrace`/`cli match` observation path with revision/profile hashes, contact inputs, seats, difficulty, character/hand information, commands, release/possession/receiver events, and per-leg timing. Version the trace format and preserve deterministic replay. Do not change gameplay coefficients in this step.

Add a small budget/annotation contract with source status, units, bounds, uncertainty, fixture, decision ID, and acceptance state. Its validator rejects an accepted target with missing provenance/bounds, reversed intervals, mixed units, unknown references, duplicate IDs, or a pending record treated as an active default. Use existing catalog/validation conventions; do not add a second sim or game configuration pipeline.

Tests must cover both force and tag timelines, overlapping read/pursuit, multiple throws, an uncovered receiver, and identical commands in both seat configurations. Include fixed-exit/launch trajectories alongside `FlightFixtures.Landing` fixtures. Extend the retained S-31/S-32 state-transition snapshot into a complete event budget and add a Harbor counterpart; the existing fixture inherits Crystal Rink, and the seed-7 Harbor match is not S-31. Record baseline S-29 and the Harbor-only cohort without retuning either.

### R3 — Choose the contract, then calibrate serially

**In progress — #708:** the [compact-field decision packet](research-game-feel-708.md) proposes C80 (80-ft paths, 232/280/232-ft fences) and C70 (70-ft paths, 203/245/203-ft fences), with unchanged bodies and coordinated spatial conventions. **Jack accepted C80 as the lead spatial trial on September 14, 2026**; C70 remains unselected. The packet records existing race evidence, incompatible IF/OF time-preserving speed sensitivities, and the remaining one-at-a-time numerical decisions. It is not yet a complete runtime contract; no candidate has been simulated. F693-02-spatial-trial and F693-05-runner-clock are accepted. F693-03-release-clock is also accepted (0.30-second ordinary command-to-release). F693-03-travel-clock is accepted (0.90-second flight over 80 feet). Long-throw/relay direction is accepted with chemistry explicitly included. Good-pair chemistry treatment is accepted. Next review is long-range numerical pace and the remaining negative-chemistry/relay control decisions. Remaining throw, pursuit/coverage, ball-motion and presentation values stay pending.

Decision review on #693 first. Jack resolves F693-01–06 from the evidence. The review artifact contains the candidate profile's complete quantities, intervals, invariants, and deviations, so approval concerns a concrete game contract.

Then create one gameplay implementation child per coherent change. If geometry moves, first migrate existing values into the shared data owner and prove parity. Implement the accepted clock/path relationships in named sim systems and `data/rules/`, leaving presentation and Blender to their owners. Compare unchanged control A against each candidate with identical declared inputs and seeds. Report effects instead of repairing each failed fixture with a private exception.

Do not add an experimental player-facing settings menu. Research profiles belong in the existing data/test workflow, and the shipped game has the selected contract. A candidate that changes an existing rule requires a corresponding spec decision and scenario update before it can be accepted.

### R4 — Present the approved race

Separate presentation child, after the sim contract is coherent. Use named shots, existing HUD/event systems, and `data/feel/`; test 1P and 1v1 with the same geometry. Coordinate #690 event stamps, #691 body scaling, #645 fly shadow, and #570 book/presentation work. No duplicate repairs in #693.

If the accepted dimensions require kit work, file a separate art child under #188, tied to #693. Consume approved geometry through `harbor_kit.py`; walk DCC stages and dual stills. If #558 needs release/scoop timing changes, agree the marker mapping before rebaking. Neither art nor a camera moves a sim outcome.

### R5 — Standalone re-sit and closure

Commit the candidate and use `python3 tools/local-player.py --preview /absolute/path/to/worktree` when Jack is ready to try it. Identify preview revision and data profile; confirm the window renders. Preserve the current game while preparing evidence and do not restart it in the background.

Jack assesses F693-07. File each new sitting finding under its owning epic and link it to #693. Append a debug-protocol row when the real repair is made; do not record an untested recommendation as a verified fix. After approval/merge, use the normal local-player delivery and a skeptic pass on the named path. Close #693 only after its complete observable behavior and human race gate pass; #346/#534 keep their own gates.

## Review packet checklist

- Source ledger with separate Wii/GC records and preserved uncertainty.
- Full accepted profile or an explicitly pending decision; no unexplained constants.
- Geometry ratios, contact-class paths, runner intervals, and short/long/relay throw segments.
- S-31/32/33 and double-play/sac-fly/tag/steal/wall/liner regression results.
- Fixed-input tests plus tactical tests; no carry-solver-only validation.
- Existing S-29 and Harbor-only cohort, disjoint validation seeds, explicit band decision.
- Continuous standalone comparison, both schemes and seats, exact build/data revision.
- Named human acceptance, remaining findings, and links to their owning issues.

## Validation of this research foundation

At `eff10d9`, `cli protocol` loaded all 23 entries successfully. The existing .NET sim suite passed **1,030 tests** with zero failures/skips. `cli match --home rio --away ashlord --park harbor-diamond --seed 7 --trace ...` completed and its trace was summarized in the baseline record. Existing compiler/analyzer warnings were present. The first sandboxed test attempt failed on process sockets; the successful suite ran with the required host access.

These checks establish a current-code baseline. No gameplay coefficient, Unity camera, mesh, clip, or couch verb changes in this foundation, so no standalone preview was built. Reference calibration and every human feel/look gate remain open.
