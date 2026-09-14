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

**State:** geometry flexibility accepted by Jack on September 14, 2026: **both infield and outfield dimensions may change independently** to achieve the intended reference-informed proportions. The current 90-foot basepaths and existing fence depths are not locked. Exact dimensions, ratios, and calibration remain pending.

**Still pending:** measure fence/basepath, mound/basepath, body/basepath, and projected body/ball sizes, then present a coherent geometry proposal for Jack’s review. Compare the unchanged baseline with independently adjusted infield/outfield candidates. Current Harbor fences are larger than the GameCube survey; a basepath reduction is not yet established. This direction approves no shrink factor, does not require either area to shrink, and is not proof that the Wii infield is smaller. Shorter basepaths affect both running and throwing; outfield depth affects pursuit, long throws, and carry. Preserve reliable routine defense, D7 pitch pace, and shared geometry across sim and presentation.

**Acceptance:** one geometry owner feeds sim and kit; bags, cover, fair/foul, paths, wall collisions, and shots agree. Separate approved toy exaggerations from distances that decide plays. Do not choose a scale to accommodate one captain or one camera. Coordinate the #691 hold-ball scaling correction before collecting the final visual baseline.

### F693-03 — Ordinary throw shape and transfer

**State:** design intent accepted by Jack on September 14, 2026: **quick release and readable travel, with enough transfer/release motion for the throw to visibly register**. Jack's qualification is explicit: “but not too quick … let's refer to mario for this.” Responsiveness must preserve recognizable possession, transfer, release, and follow-through rather than imply an instant launch.

**Still pending:** compare ordinary throws in Wii Super Sluggers and GameCube Superstar Baseball before choosing transfer/release durations, travel intervals, or motion curves. Include short infield throws, long outfield throws, and ground-ball pickup versus received-ball transfer. Keep chemistry, abilities, lobs, and relays separate. This direction selects neither game as the numerical reference and approves no new coefficient. Harbor's current `releaseSec` is part of a linearly sampled total duration, not a stationary release segment.

**Acceptance:** at ordinary gameplay speed and couch distance, the transfer/release visibly registers and ball travel can be followed, assessed against the annotated Mario comparison. One sim model samples and predicts the same throw; tests distinguish command/release/reception; third/home tags and uncovered bags remain live geometry. #558 owns authored takes; it must coordinate any marker change after the sim event is decided.

### F693-04 — Contact-class motion

**State:** design intent accepted by Jack on September 14, 2026: **hard liners may reward existing positioning over post-contact reaction**. A well-struck liner may pass before a fielder can reposition, while its path remains readable. Grounders retain the pickup-and-throw sequence; flies allow pursuit and catch judgment. Do not flatten all contact speeds to provide a recovery opportunity on every hit.

**Still pending:** Wii/GameCube-informed bounds for hopper/liner/pop/fly flight, bounce, roll, wall carom, and carry. No current speed, time scale, class threshold, or guaranteed catch/miss is approved. Judge catchability from the shared trajectory and the fielder’s position/reach. This does not add a defensive-positioning control or change D18 assistance. The current 1.0/1.65 clock split and 74-mph class threshold remain baseline observations, not accepted targets.

**Acceptance:** fixed-input trajectories and tactical fixtures both pass. Contact-to-scoop, apex/hang, class separation, and wall behavior are within accepted intervals. Do not solve the complaint by hiding a rope with a fly camera or trading a physics change for an opaque CPU delay.

### F693-05 — Runner and defensive opportunity

**State:** design intent accepted by Jack on September 14, 2026: **reliable routine defense**. On an ordinary grounder with a clean pickup, an average runner, and an ordinary arm, reasonably prompt correct execution should normally retire the runner with a readable margin. Tight races arise from fast runners, deep pickups, weak arms, bobbles, or hesitation. Outcomes still follow ball/runner/glove geometry; this is not a guaranteed-out rule.

**Still pending:** reference-informed timing intervals and human validation of “reasonably prompt” and “readable.” No runner slowdown or other coefficient is selected. Record startup, acceleration, bag intervals, handedness, dash, pursuit, read, legal bag coverage, and decision time. A direction decision does not close the measurement or play gate.

**Acceptance:** a Run-5 routine grounder can be retired by geometry and is readable; fast/weak-arm variants remain meaningful; an unthrown human ball never becomes an automatic out. The human owns both legs of a double play. Throws and runs are assessed against the same live clock.

### F693-06 — Scoring regression band

**State:** accepted by Jack on September 14, 2026: retain **1.8–5 mean runs per side** as the S-29 regression guardrail. This applies separately to the home and away averages over the existing 50-game cohort, not to each game's score. The earlier 2–5 spec headline is reconciled to the existing test; no runtime coefficient or test threshold changes. This is a regression tolerance, not proof of reference fidelity or an accepted final scoring experience.

**Acceptance:** preserve the existing 50-game mixed-park cohort; add a Harbor-only cohort and predeclared disjoint validation seeds. Report per-side means and play kinds, fixture event budgets, and seat parity. Score alone cannot pass feel.

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

[#702](https://github.com/jackguillet/grand-sluggers/issues/702), gameplay child of #693 under #209. Extend the existing `PlayTrace`/`cli match` observation path with revision/profile hashes, contact inputs, seats, difficulty, character/hand information, commands, release/possession/receiver events, and per-leg timing. Version the trace format and preserve deterministic replay. Do not change gameplay coefficients in this step.

Add a small budget/annotation contract with source status, units, bounds, uncertainty, fixture, decision ID, and acceptance state. Its validator rejects an accepted target with missing provenance/bounds, reversed intervals, mixed units, unknown references, duplicate IDs, or a pending record treated as an active default. Use existing catalog/validation conventions; do not add a second sim or game configuration pipeline.

Tests must cover both force and tag timelines, overlapping read/pursuit, multiple throws, an uncovered receiver, and identical commands in both seat configurations. Include fixed-exit/launch trajectories alongside `FlightFixtures.Landing` fixtures. Extend the retained S-31/S-32 state-transition snapshot into a complete event budget and add a Harbor counterpart; the existing fixture inherits Crystal Rink, and the seed-7 Harbor match is not S-31. Record baseline S-29 and the Harbor-only cohort without retuning either.

### R3 — Choose the contract, then calibrate serially

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
