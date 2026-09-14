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

**State:** open; Jack decides. Compare stock Wii Mario Stadium and stock GameCube Mario Stadium, recording mode, hands, stats, chemistry, stars, and capture cadence. Numerical coverage in a wiki is not sufficient to choose the game's feel. Deliver the comparable play clips and event intervals before recommending Wii-led, GameCube-led, or a named hybrid.

**Acceptance:** each selected relationship identifies its game and source; deliberate Harbor deviations have reasons and a named human decision. Existing D1–D18 are either preserved or explicitly superseded one by one.

### F693-02 — Playable geometry versus visible proportions

**State:** unresolved. Candidates: current geometry; independently compressed outfield; coherent full playable-space rescale. Measure fence/basepath, mound/basepath, body/basepath, and projected body/ball sizes. Current Harbor fences are larger than the GameCube survey; the same reduction of the basepaths is not established.

**Acceptance:** one geometry owner feeds sim and kit; bags, cover, fair/foul, paths, wall collisions, and shots agree. Separate approved toy exaggerations from distances that decide plays. Do not choose a scale to accommodate one captain or one camera. Coordinate the #691 hold-ball scaling correction before collecting the final visual baseline.

### F693-03 — Ordinary throw shape and transfer

**State:** unresolved. Harbor's `releaseSec` is part of a linearly sampled total duration, not a stationary release segment. Compare real transfer, flight, deceleration, and reception. Ordinary, chemistry, ability, lob, and relay behavior need separate observations.

**Acceptance:** one sim model samples and predicts the same throw; tests distinguish command/release/reception; third/home tags and uncovered bags remain live geometry. #558 owns authored takes; it must coordinate any marker change after the sim event is decided.

### F693-04 — Contact-class motion

**State:** unresolved. Compare hopper/liner/pop/fly flight, bounce, roll, wall carom, and carry separately. Current 1.0/1.65 clock split and 74-mph class threshold make an exit change nonlinear in the player experience.

**Acceptance:** fixed-input trajectories and tactical fixtures both pass. Contact-to-scoop, apex/hang, class separation, and wall behavior are within accepted intervals. Do not solve the complaint by hiding a rope with a fly camera or trading a physics change for an opaque CPU delay.

### F693-05 — Runner and defensive opportunity

**State:** unresolved; no runner slowdown selected. Record startup, acceleration, bag intervals, handedness, dash, pursuit, read, legal bag coverage, and decision time.

**Acceptance:** a Run-5 routine grounder can be retired by geometry and is readable; fast/weak-arm variants remain meaningful; an unthrown human ball never becomes an automatic out. The human owns both legs of a double play. Throws and runs are assessed against the same live clock.

### F693-06 — Scoring regression band

**State:** open discrepancy. S-29's spec headline says 2–5 runs; the current test and #667 note use 1.8–5. Record the intended authoritative band explicitly before calibration. Do not silently retune the game or rewrite the band to fit a candidate.

**Acceptance:** preserve the existing 50-game mixed-park cohort; add a Harbor-only cohort and predeclared disjoint validation seeds. Report per-side means and play kinds, fixture event budgets, and seat parity. Score alone cannot pass feel.

### F693-07 — Spectator pacing and couch acceptance

**State:** pending gameplay candidates. Record simulation time separately from hit freeze, camera cut/settle, possession readability, decision time, stamps, and next-ready delay. Preserve D7 until Jack's separate pitch re-sit resolves it.

**Acceptance:** continuous standalone play with keyboard/mouse, one pad, and two pads; both halves and ownership roles; representative small and large captains; HUD-off diagnostic clips as well as the actual HUD. Jack watches the race and finishes the book-to-half path. No agent can sign this row off.

## Execution sequence

### R1 — Annotate the two reference games (#701)

[#701](https://github.com/jackguillet/grand-sluggers/issues/701), research/documentation child of #693 under #209. No runtime or art edits. Follow research §7's capture manifest, event definitions, uncertainty, exclusions, and proposed screening sample. Prefer ordinary plays in comparable parks; label any special or modded footage. Get the raw frame/coordinate evidence behind the GameCube stadium claims where possible. Independently check throw units and motion; preserve unknowns if the evidence does not resolve them.

Deliver a small version-controlled annotation dataset, exact clip locators, source revisions, a comparison for F693-01–05, and a recommendation with tradeoffs. A screenshot does not count as elapsed-time evidence. Existing retrospective clips may lack enough metadata and must be marked accordingly. Exit is a reviewable comparison, not Jack's approval.

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
