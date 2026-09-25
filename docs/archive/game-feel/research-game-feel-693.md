# Field proportions and the readable baseball race

> **Historical.** A finished report, kept for its evidence and reasoning; the contract is [gameplay-spec §0.2 (D19)](../../spec/00-decisions.md) and [the decision plan](../../decisions/plan-game-feel-693.md). Where they disagree, the contract is right.

## Decision status

This is the research and design foundation for [#693](https://github.com/jackguillet/grand-sluggers/issues/693), serving #209 and the flight/fielding work in #564/#566. It responds to Jack's sitting on `850dd95`: contact and throws are too fast, and the field may need to be smaller. The implementation audit below uses `eff10d9`, not the sitting revision.[^10] Jack's direction on September 14, 2026 is **compare Wii Super Sluggers and GameCube Superstar Baseball before choosing**.

The recommended decision unit is a **whole baseball race**: contact, defensive read, pursuit, possession, player decision, release, ball travel, receiver, runner arrival. World dimensions, character proportions, ball motion, and framing must support that race together. This document establishes research findings and proposed acceptance methods; it does not approve new gameplay coefficients or declare reference parity. The [execution plan](../../decisions/plan-game-feel-693.md) tracks what is established, what needs measurement, and who can accept the result.

Three conclusions are supported now:

1. A smaller outfield deserves a controlled comparison. The GameCube community survey reports a materially shallower Mario Stadium than Harbor. That evidence does not establish a proportional reduction of the basepaths, pitcher distance, or characters.
2. A single mph setting cannot reproduce the reference. GameCube research describes contact-dependent flight behavior, throw deceleration, situational throw types, and runner acceleration. Harbor has different simplifications, including class-dependent flight time stretching.
3. Numerical similarity and perceptual similarity require separate evidence. A geometric out can pass while the player cannot see the scoop or judge the throw. A flattering camera can conceal a bad race. Both the sim trace and a continuous standalone play must be assessed.

The recommended next experiment compares the unchanged Harbor control with a clock-only candidate and a separately compressed outfield candidate. A full diamond rescale remains an option if measurements of basepath/body proportions support it. No scale factor or target seconds are selected in this report.

## 1. Evidence rules

Every numeric claim has one of five statuses:

- **Primary documented:** Nintendo's booklet describes the mechanic, or MLB supplies a real-world dimension. This proves only what the source actually states.
- **Community reported:** first-party community datamining or experimental research describes an implementation. Preserve its game version, units, caveats, and revision. This is useful evidence, not Nintendo documentation or an independently reproduced result.
- **Harbor measured / code-derived:** a named revision, formula, fixture, or trace supports the number. A headless result is not an observed player experience.
- **Proposed:** an experiment or design contract to evaluate. It must not be called a measured reference value.
- **Unresolved:** evidence is missing, ambiguous, or contradictory. Use an explicit unknown, never a plausible default.

The source register in §10 distinguishes the two games. The existing [reference teardown](../reference/research-sluggers.md) remains useful for mechanics. For scale and pace, its earlier figures must be read with this provenance rule. In particular, GameCube raw exit values such as 145–150 are not Harbor mph; GameCube timing cannot silently become Wii timing.

No matched frame-count dataset for the Wii and GameCube games has yet been produced for #693. The Wii video in §10 was visually inspected at approximately 01:00, where a close infield view shows the player-controlled fielder, ball, dirt, and bags. That is qualitative framing evidence, not a world-space or timing calibration. The GameCube longplay is a candidate source; its introductory/cinematic material is excluded from measurement. Neither a video seek bar nor a single screenshot supplies simulation frames.

The follow-on [#701 observation packet](research-game-feel-701-comparison.md) adds explicitly bounded recording-time observations from Wii and GameCube Mario Stadium plays. It preserves unknown capture configuration, distinguishes a direct out from a force/return scoring play, and remains outside the verified stock-calibration set. It does not supersede the sampling or evidence rules below.

## 2. Comparing the references

### Nintendo Wii: Mario Super Sluggers

The Nintendo booklet provides a useful account of player ownership. Sideways controls include choosing a throw destination, moving and switching the fielder, dashing, and relaying. Its other control styles change how much fielding and baserunning are automatic. It also describes close-play prompts at third/home and chemistry-assisted buddy actions. Measurements must therefore record the control style and whether an action was automatic. Mixing these modes would contaminate decision-delay comparisons. [^1]

The booklet does not provide a calibrated basepath length, character height, throw-speed curve, or ball-physics coefficients. Neither the current project teardown nor the sources inspected here establishes those Wii values. The existing Wii exhibition video is a strong candidate for observation; it must be annotated before its timing can govern implementation. [^1][^8]

**What Wii can lead today:** the established couch verbs and readable action vocabulary, subject to the project's existing adaptations. **What it cannot yet lead numerically:** field scale, release-to-reception seconds, contact-to-scoop seconds, and body-to-diamond ratios. The game's identity alone does not fill those gaps.

### Nintendo GameCube: Mario Superstar Baseball

The original booklet describes button-controlled batting, pitching, throwing, running, dashing, and character abilities. It documents chemistry's effect on throws. These qualitative relationships are supported without importing a competitive mod's settings or assuming Wii mechanics. [^2]

The community's reverse-engineering work provides a much stronger numerical starting point than was found for Wii. It reports stadium dimensions, running frame ranges, throw algorithms, and batted-ball behavior. Several pages explicitly leave important parts unconfirmed, and some explanatory equations contain inconsistencies. Preserve that uncertainty rather than translating their values directly into Harbor's feet/seconds. [^3][^4][^5][^6][^7]

**What GameCube can lead today:** hypotheses about relationships and a repeatable measurement protocol. **What still requires confirmation:** precise units and integration for throws, playable-space/body proportions, and whether its pacing is the experience Jack prefers over Wii.

### Choosing a combination

Compare ordinary Mario Stadium plays in each game before special abilities, chemistry boosts, stars, or hazards. Keep separate columns of evidence for each title; do not average them into an imaginary reference game.

A combination is defensible only when each borrowed subsystem names its source and the seams pass the same race tests. For example, adopting GameCube-inspired throw deceleration while retaining Wii-inspired buddy actions is a design proposal. It must preserve visible differences between a routine throw, a boosted throw, and a relay. The result must remain playable with the current two-pad and keyboard/mouse scheme.

Existing decisions D1–D18 remain in force until explicitly superseded. In particular, D7's pitch-pace hold survives this research. A field change that shortens mound-to-plate distance must not inadvertently shorten pitch flight or move the swing window. Reference selection for #693 is not permission to reopen every earlier control decision.

## 3. Geometry and proportions

### Real baseball is an anchor, not the visual target

MLB specifies a 90-foot infield square and a 60-foot-6-inch pitching distance. Harbor uses those dimensions. [^9] They are useful unit anchors; they do not prove that a toy baseball game should preserve regulation scale.

At `eff10d9`, the semantic geometry owner is `Diamond.cs`: baseline 90 ft, mound 60.5 ft, first/third `(±63.64, 63.64)`, second `(0, 127.28)`. `ParkDiamond.cs` draws the dirt, mound, bags, and ground dressing from this system; it is not the sole owner of baseball distances. Harbor's park data supplies 330/400/330-ft fences and a 12-ft wall. A change confined to `ParkDiamond` or the Blender kit would leave baseball on the old diamond.

The resulting code-derived Harbor ratios are:

- Mound distance / basepath: **0.6722**.
- Center fence / basepath: **4.4444**; foul-line fence / basepath: **3.6667**.
- Second-base distance / basepath: approximately **1.4142**.
- Authored 4-ft bag width / basepath: **0.0444**. This is visual bag width, not the 6-ft cover radius or the runner's tag-safe radius.

### What the GameCube survey supports

The community stadium survey reports Mario Stadium's center wall at **100 m**, its foul poles at approximately **80.5 m**, and a field that is not left/right symmetric. Those convert arithmetically to about **328.1 ft** and **264.1 ft**. Harbor's corresponding distances are about **21.9%** and **25.0%** longer. These are comparisons to community-reported dimensions, not measurements of Nintendo's physical feet. [^3]

That supports testing **outfield depth independently**. It does not prove the basepaths are 80% of regulation, nor that scaling every Harbor object by 0.8 reproduces the reference. The inspected source text did not establish the basepath/body ratio. Wii field dimensions remain unresolved for this decision. The [#701 geometry follow-up](research-game-feel-701-geometry.md) now records a Wii running survey whose equal-speed interpretation suggests shallower wall/basepath ratios, together with movement-model sensitivity and a pending research candidate. It does not establish absolute feet or basepath/body scale.

### Measure two kinds of proportion

**Playable-space ratios** describe the sim: fence/basepath, mound/basepath, infield depth/basepath, catch radius/basepath, fielder distance-to-intercept/basepath, and runner travel/basepath. Prefer dimensionless ratios so game units do not masquerade as feet.

**Projected proportions** describe what a player sees: full body height / active gameplay viewport height; ball diameter / viewport height; glove and bag size near an interaction; and the fraction of the screen occupied by the decisive play. Record pose, camera phase, depth, aspect ratio, crop, and the identity of the character. Separate body height from an extra such as horns or a crown.

A ground-plane homography can compare bags and field markings within one stable camera view. It cannot turn a raised head or an airborne ball into a reliable ground distance. Measuring a near batter against a distant fielder without correcting perspective is invalid. Do not infer feet from Mario's assumed canonical height.

For Harbor, audit Rio, Vale, Zig, Brondo, Konga, Ashlord, and Elder Fenn, plus one role player of each body type. The research can compare proportions; implementation remains in separate gameplay, presentation, and art sessions. Preserve the one rig and original identities. A captain-specific shrink is never a way to fit an approved shot.

## 4. The ball has a path and a clock

### Harbor's current flight

`batting.exit` starts at `61 + 3.7 × power` mph. Before pitch, item, star, stamina, or on-base factors, Bat/Power 5 produces **79.5 mph**. The contact columns produce slap sour/nice/perfect **59.625 / 75.525 / 79.5 mph**, and full-charge sour/nice/perfect **75.525 / 89.04 / 99.375 mph**. These are resolver inputs, not average rendered velocities.

`BallFlight` integrates at 120 samples per physics second with gravity **32.174 ft/s²**, quadratic drag coefficient **0.0019**, and a launch height of **2.5 ft**. It applies bounce, skid, rolling, and wall rules, then timestamps the samples using a class-dependent scale: **1.0 for liners; 1.65 for dirt contacts and other air contacts**. Runners and fielders meet that timestamped path on live-play time. The selected scale follows the coarse launch classifier, not a fresh arbitrary scale at each bounce.

For a fixed path and scale `s`, visible velocity is physics velocity divided by `s`; visible acceleration is physics acceleration divided by `s²`. An unmodified 79.5-mph contact therefore starts at about **116.6 ft/s** on the 1.0 clock, versus **70.7 ft/s** on the 1.65 clock, before drag and projection. The effective gravitational term on the stretched clock is about **11.82 ft/s²**. These are derived explanations of the current implementation, not proposed Nintendo coefficients.

A scalar exit-speed change affects more than pace: carry, apex, classification around the 74-mph liner threshold, rebound energy, bobble/knockback inputs, and which body can reach the ball. For launches near the liner boundary, an exit reduction can switch the time scale as well. Increasing time stretch preserves the integrated path but grants fielders and runners more live time. Neither operation is equivalent to slowing a video.

### What GameCube research adds

Community research reports different unadjusted exit ranges for slap/charge and sour/nice/perfect contacts, plus a contact-dependent gravity term. Perfect charge has a zero added-gravity entry while nice charge does not. It also reports air-speed retention of **0.996 per frame** and base gravity **0.00275** in its internal representation. These are unverified engine quantities here, not ft/s². Its 45-degree component example allocates half the speed to each axis; that is not a standard unit-vector decomposition, so the underlying routine needs inspection before porting the example as physics. [^4]

The stadium research distinguishes first-bounce coefficients from later bounces and rolling retention. For Mario Stadium it reports vertical retention **0.44** at the first bounce, horizontal retention **0.66** at the first bounce, and rolling retention **0.999**. Harbor's fixed rolling deceleration and its ordinary/skid paths are different models. Those coefficients alone cannot be transplanted without their update cadence and surrounding integration. [^3]

The design implication is to measure **trajectory shape and phase timings**: first bounce, apex, arrival at infield depth, possession, wall contact, and rest. Contact quality should affect the kind of opportunity, not merely how fast the same curve plays. A hopper, rope, pop, fly, wall carom, and homer must remain visually distinguishable and geometrically consistent.

For Wii these numerical flight relationships are unresolved. Do not cite the GameCube wiki as evidence that Wii uses them.

Jack accepted F693-04’s direction on September 14, 2026: a hard liner may reward the fielder’s existing position over post-contact reaction while remaining readable. Grounders and flies must retain different defensive opportunities. This establishes intent, not approval of Harbor’s current fast-liner clock or an unmeasured Nintendo coefficient; exact trajectory/timing bounds still require the comparison.

## 5. Throws, runners, and the race budget

### Harbor's current throw formula

`InPlay.ThrowSec(d, throw)` returns:

`0.22 + d / max(32, 100 × SpeedMul)` seconds.

The arm multiplier is `0.85 + 0.03 × Field`; Field 5 is **1.0**, Field 3 is **0.94**, and Field 10 is **1.15**. Chemistry and abilities modify the result. A neutral Field-5 throw has formula durations of **0.67 s at 45 ft**, **1.12 s at 90 ft**, **1.4928 s at 127.28 ft**, **2.22 s at 200 ft**, and **3.22 s at 300 ft**. These exclude CPU possession-to-command delay and any subsequent wait for an uncovered receiver.

Crucially, `LivePlaySystem.Field.cs` sets `ThrowDur` to that whole duration and linearly interpolates the ball from hand to destination using `ThrowT / ThrowDur`. There is no separate stationary 0.22-second release phase in this path. At 90 ft the actual horizontal travel rate is therefore about **80.36 ft/s**, not 100 ft/s following a 0.22-second hold. Calling `releaseSec` an observed windup would misdescribe current behavior.

The CPU also waits `max(0.08, 0.35 − 0.02 × Field) × difficultyReaction` after possession before its command. For Field 5 at NORMAL that is **0.25 s**. A human's decision time comes from the human. A test must not insert CPU timing into the human's opportunity budget.

### Reference throw relationships

GameCube's community research separates arm power, situational throw type, chemistry/smash modifiers, and quick-transfer abilities. It reports **10 frames for Daisy versus 30 for Wario** to release after receiving a throw, and notes Quick Throw does not apply to fielding a ground ball. It also says throws slow in flight, with the slowdown formula and speed-to-time conversion still unconfirmed. These are reasons to measure transfer and travel separately, not permission to call its raw 0.500–0.800 table m/frame or mph. [^5]

The Wii booklet documents relays and high-speed buddy tosses, but does not establish release or deceleration coefficients. [^1] Preserve distinct ordinary/boosted/relay observations for both games. A highlight featuring a special throw cannot set ordinary throw speed.

Jack's F693-03 direction, accepted September 14, 2026, is **quick release and readable travel**, qualified by “but not too quick” and a requirement for the throw to visibly register. The Wii/GameCube comparison must establish a readable ordinary transfer/release and follow-through as well as travel time. This is not permission to use an instant launch, adopt a special Quick Throw frame count, or approve a duration without the comparison.

### Harbor's runners

`RunnerSystem.BagSec` is `clamp(3.55 − 0.12 × Run, 2.45, 3.65)`. Bag-to-bag time without dash is **3.43 s at Run 1**, **2.95 s at Run 5**, and **2.45 s at Run 10**. Full dash multiplies speed by **1.12**, making the Run-5 bag interval approximately **2.634 s**. The batter has a **0.5-second** start delay. Actual home-to-first time includes the handed batter's box position and path; `0.5 + 2.95` is only a 90-ft reference calculation.

GameCube research reports a **31-frame** post-contact delay before batter acceleration. Its speed-50 home-to-first estimates are **220–244 frames right-handed** and **202–224 left-handed**, with the lower endpoint using perfect mashing. At an explicitly assumed 60 simulation frames/s these would be **3.667–4.067 s** and **3.367–3.733 s**. These are the source's estimates with a named box position, not universal Mario or Wii times. Its stamina/acceleration prose and equations disagree in places; that prevents treating the displayed full formula as a verified port. [^6]

This suggests a meaningful comparison of start, acceleration, dash, handedness, and arrival. It does not justify slowing runners simply to restore an out after slowing throws. Runner speed has already been observed as the wrong suspected culprit in #693.

### Budget every decisive event

Use contact as `t = 0` and record:

- `t_read`: fielder allowed to move; `t_possession`: ball acquired at the actual interception.
- `t_command`: player/CPU commits the throw; `t_release`: ball separates from the hand.
- `t_reception`: receiver legally acquires the ball; `t_bag_ready`: receiver is in a legal force position.
- `t_runner_arrival`: runner reaches the legal safe boundary; `t_tag`: actual glove/body contact for a tag.

For a force, the useful comparison is runner arrival against the instant both possession and legal bag coverage exist. Define `forceMargin = t_runner_arrival − t_force_ready`; positive favors the defense. A tie remains safe under the current spec. Tags and close-play prompts retain their own rules. An early ball arrival to an empty bag is not an out.

The read delay and ball flight overlap. Do not add `flight + reaction + pursuit` as if they were all sequential. Find the first reachable interception on the timestamped path. After possession, decision, transfer, flight, cover, and tag readiness determine the rest of the play.

For S-31 the out must remain geometric, but the test also needs a measurable visible opportunity. Jack accepted the F693-05 direction of **reliable routine defense** on September 14, 2026: clean, reasonably prompt ordinary execution should normally win, while difficult circumstances create tight races. Define the permissible margin band only after reference annotation and the human comparison; the direction does not establish numerical bounds. Pair it with S-32 (fast runner/weak arm), S-33 (human never throws), the double-play chain, sac fly, and the outfield throw home. A fixed outcome for one fixture is insufficient.

## 6. Three experiments and what each would prove

### A. Keep geometry; alter time and flight behavior

Hold the current diamond, body scale, and park geometry constant. Evaluate separately: a slower ordinary throw curve, a true release phase with the same or deliberately revised total budget, and contact-class clocks. Each candidate names which quantity it changes and why.

This is the cheapest causal experiment. It answers whether readable travel fixes the complaint at current proportions. It cannot establish that the characters and park now resemble the reference. Slower ball motion can increase defensive reach on a hit while decreasing the defense's advantage on a throw; report both effects.

### B. Compress the outfield separately

Keep basepaths and mound distance fixed; test an outfield depth/starting-position profile informed by measured fence/basepath ratios. Re-evaluate fence crossings, doubles, relays, coverage, and projected character size. A deeper ball can become a homer if the wall moves inward even when its path is unchanged.

The GameCube survey is evidence for considering this option, not approval to type 264/328/264 into Harbor. Its asymmetry and missing validated basepath ratio matter. It also does not imply that new parks should be built.

### C. Rescale the whole playable space

Only pursue this if measured infield/body ratios warrant it. Declare which distances scale and which remain intentional toy exaggerations. A 20% reduction in distance with unchanged throw speed reduces the travel part of throw time by 20%, making the ball arrive sooner. Because Harbor derives runner speed from baseline / bag time, coherently scaling runner paths and baseline can preserve their bag times while accelerating the defense's throw advantage.

For an ideal uniform transformation `x' = kx` and `t' = qt`, matching trajectories requires `v' = (k/q)v` and `a' = (k/q²)a`. Quadratic drag in the current form requires its coefficient to transform as `drag' = drag/k` under uniform spatial scaling; dimensionless restitution stays unchanged. Distances, launch height, wind speed, rolling deceleration, reaction/transfer times, and thresholds each need a declared treatment. This is a dimensional-analysis check, not a recipe to scale every toy or a proposed runtime global multiplier.

A uniform rescale of bodies, ball, field, and camera can preserve the same screen image. It would change the units without changing the feel. To change toy proportions intentionally, state which dimensionless ratios change and verify the associated gameplay and visual consequences.

## 7. Measurement protocol

### Reference capture

Use a normal Mario Stadium match for each title. Record title, region/revision if known, hardware/emulator, emulator version and speed setting, active mods/codes, aspect ratio, capture rate, unique simulation-frame cadence, control mode, player count, team/characters, handedness, stats, superstar status, chemistry, difficulty, inning, outs, runners, and contact type. Unknown configuration disqualifies exact claims that depend on it; it need not disqualify a qualitative observation.

Project Rio provides instrumentation and selectable game modes. Record every enabled change; a modern modded tournament is not automatically the stock GameCube reference. Never obtain game binaries as part of this research. A lawful local copy or suitable original gameplay recording is sufficient. [^7]

Annotate contact, first fielder movement, bounce, possession, command if visible, release, reception, runner departure/arrival, apex, wall event, and next-ready state. Record frame indices and an uncertainty interval for occluded events. At a verified 60 unique frames/s, ±1 frame on each endpoint yields up to ±2/60 s uncertainty on a duration. A 60-fps upload with duplicate frames is not 60 unique simulation frames.

Do not combine live-ball simulation seconds, freeze time, replay time, and wall-clock menu delays. Report playable phase duration and spectator duration separately. Do not infer command time from an unseen button press. A bat swing or throw animation is not automatically the sim event.

Jack accepted F693-07’s direction on September 14, 2026: brisk routine beats, **on the slower side**, with extra emphasis for big moments and a Mario comparison before setting durations. Annotate dead-ball/result recognition, brief character reaction, camera reset, and next-ready state in both games. Separate routine outs/hits from home runs, great catches, and inning-ending plays. A single post-play interval cannot represent all of these; the direction does not approve shorter live decision windows or a change to D7.

As a **proposed minimum screening sample**, collect ten usable ordinary SS/2B grounders, ten long outfield throws, and ten home-to-first runs per title, spread across slow/middle/fast or weak/middle/strong archetypes. Add at least three clean examples per title of a liner, pop/fly, wall carom, relay, double play, and sac fly. This is a feasibility minimum, not a powered statistical sample. Report counts, ranges, and individual cases; avoid p95 claims from tiny cells. Increase samples when the uncertainty could change the decision.

Freeze the inclusion/exclusion rules before choosing favorable clips. Exclude cuts through decisive events, unknown playback speed, cinematic intros, replays presented as live play, and special effects from the ordinary-play set. Log exclusions with reasons. Prefer matched in-game fixtures where available; otherwise stratify instead of presenting unlike plays as matched pairs.

### Harbor capture

Retain the sim revision and a hash of the actual rules and park data, not only a video filename. Use `cli match --trace` and the existing scenario harness. Preserve contact inputs, seed, roster, hands, difficulty, commands, and each event timestamp. The current trace lacks enough metadata to recover every one of those facts; extend the existing trace in its own gameplay child rather than infer them from a caption.

Run deterministic CPU cases first to localize physical relationships, then replay the same rule profile with human ownership. Compare 1P batting, 1P fielding, and both halves of 1v1. Check keyboard/mouse for player 1 and two physical pads. Difficulty can alter CPU choices and reaction, not the ball's physical clock or a human's body.

For projected proportions and pacing, use the standalone in a separate presentation session. Preserve the camera phase and full continuous play. No change in this research commit has been viewed in the standalone, and no player gate is passed here.

### Required measurements before selecting a target

The decision packet must contain: basepath/body and fence/basepath ratios for both titles; ordinary short/long throw durations and release segments; contact-to-possession for a hopper; time and apex for a liner and a fly; running intervals with handedness/dash; and contact-to-next-ready pacing. Each target records its source range, uncertainty, selected Harbor interval, reason for deviation, and accepting human.

If a number cannot be measured, Jack may explicitly choose a **Harbor-authored cartoon** instead. Record that choice and a playable acceptance interval. Missing evidence is not license for an agent to invent reference precision.

## 8. Durable implementation rails

**Geometry ownership.** Baseball landmarks and gameplay distances belong in one sim-owned data contract. `Diamond`, pursuit/cover starts, field bounds, fair/foul tests, runner paths, and all park drawing consumers must agree. A geometry migration starts by moving the current values into that contract with parity checks; it must not simultaneously tune them. Blender consumes the same approved geometry through the existing kit workflow. No second field definition in Unity or Python.

**Motion ownership.** Continue to use the one `BattedBall` trajectory and one throw model for prediction, visible position, receiver tests, and CPU decisions. If release or deceleration is introduced, one named model must expose duration and sampling. Unity cannot stretch the shown throw while the sim judges its old arrival. Preserve monotonic progress, exact endpoints, units, minimum-speed behavior, and uncovered-bag handling.

**Timing ownership.** Use explicit units and event definitions. A number that changes interception or arrival belongs in `data/rules/`; camera cuts, framing, and presentation holds belong in `data/feel/`. An authored animation marker must be mapped to the same release/contact event; do not pose bodies in C# or bury baseball delays in a clip.

**Evidence ownership.** Extend `PlayTrace`/`cli match`, the scenario suite, and existing catalog validators. Store small reference annotations, source locators, and accepted budget records with the docs. Raw recordings remain linked evidence rather than new game assets. Missing accepted bounds must remain pending and must never load as zero or as a guessed fallback.

**Change ownership.** One child and one session kind per implementation. Shared feel work is serial. Gameplay establishes the rules; presentation assesses the named shot; art updates approved kit/body slots only when required. Each change updates the affected source-of-truth paragraphs and measurement record. Couch verbs/cameras require `HowToPlay.cs` and `docs/how-to-play.md` together.

**Regression ownership.** A new profile is tested across throw distance, arm, chemistry, abilities, runner speed, dash, hand, hit class, field position, and seat. Record both fixed contact inputs and fixed tactical scenarios. `FlightFixtures.Landing` solves for exit velocity to retain carry; that is useful for tactical geometry but can hide an exit/flight change if it is the only test.

No schema or runtime loader for an approved feel profile is introduced by this research. The plan names the contract that must be implemented and tested after the quantities are understood. That avoids turning a speculative document into a second configuration system.

## 9. Baseline and falsification

The retained [baseline record](../../research/game-feel-693-baseline.json) gives formula checks, the seed-7 Harbor trace summary, and their provenance. It is descriptive, never a tuning input. The CLI run is a three-inning scheduled match that continued through extras to five innings and ended Ember Court 6, Spark All-Stars 3. It is not S-29 and it is not a human half.

A read-only replay of the existing S-31/S-32 helper adds a narrow race snapshot. These fixtures inherit Vale's **Crystal Rink**, not Harbor. With seed 1, an Ashlord shortstop (Field 3), and the inverse-solved 118-ft, 4° / −18° contact, the solver supplies **106.9 mph**. S-31 (Cinder, Run 5) first reports possession at **1.0333 s**, `Throwing` at **1.4833 s**, and the end of that throw at **2.8000 s**. The existing runner predictor gives **3.4583 s**, a **0.6583-s** arrival margin. S-32 (Dart, Run 9) reports the same initial possession, holds without a throw, and ends as a single; its predicted runner arrival is **2.8495 s**.

These observations are quantized to the helper's 1/60-s tick. `Throwing` is a state flag, not a separately observed command or animated release; throw end is the helper's transition detector. The 0.45 s from possession to the first throw flag includes the existing model's readiness/decision behavior, not a newly measured animation budget. Predicted runner arrival is counterfactual for the retired batter. The margin is not an approved feel band or spare time that can safely be spent without testing other plays. [Reproduction instructions](../../research/game-feel-693-reproduce.md) retain the probe; #702 must replace this limited observation with explicit events and a Harbor fixture.

The existing S-29 test is **50 games**, constructed from five captain pairs in both home/away orders using seeds 1–5. It is not fifty distinct seeds and is not Harbor-only: `Match.Exhibition` selects the home captain's park when none is specified. Its current assertions allow mean away and home runs **1.8–5**, doubles fewer than singles, mean homers at most **2 per game**, and nonzero aggregate strikeouts/walks. At the audited baseline, the spec headline still said **2–5**, while its #667 note explained the 1.8 floor. Jack resolved F693-06 on September 14, 2026 by retaining **1.8–5 as the regression guardrail**. The spec now matches the existing test; this decision changes no runtime coefficient and does not declare the eventual scoring experience accepted.

Keep the existing cohort for regression and add a Harbor-only cohort for this decision. Predeclare a disjoint validation seed set before tuning. A score distribution can conceal a broken throw race, while an attractive isolated grounder can conceal broken fly coverage. Require both event budgets and outcome distributions. Do not change the S-29 band after seeing a candidate's failures without an explicit design decision.

The closing human test is a continuous standalone path: Call time → How to play → return to play → finish a half, including a readable hopper/scoop/throw race and a watchable outfield throw. Supplement with the named play matrix for events not encountered naturally. Jack assesses whether he can see possession, decide, follow the ball, and understand safe/out at couch distance. S-29, a build, and a launch cannot close #693, #534, #346, or a look gate on their own.

## 10. Sources

Accessed September 14, 2026. Nintendo manuals are primary publications hosted by archival mirrors. The mechanics wiki is community-authored original research; its caveats are retained. No Nintendo assets are added to the game.

[^1]: Nintendo, *Mario Super Sluggers Instruction Booklet*, 2008, printed pp. 5–10; [original booklet scan](https://www.mariomayhem.com/downloads/mario_instruction_booklets/Mario_Super_Sluggers_-_ML1_Manual_-_WII.pdf). Supports qualitative control/ability comparisons; does not supply numeric scale or physics.
[^2]: Nintendo, *Mario Superstar Baseball Instruction Booklet*, 2005, In-Game Controls / Chemistry; [original booklet scan](https://www.gamesdatabase.org/Media/SYSTEM/Nintendo_GameCube/Manual/formated/Mario_Superstar_Baseball_-_2005_-_Nintendo.pdf). Supports the GameCube control and chemistry relationships.
[^3]: Mario Superstar Baseball research community, *Stadiums*, revision 890, February 14, 2026; [permanent revision](https://mariobaseball.miraheze.org/wiki/Stadiums?oldid=890). Community dimensions and surface coefficients, with approximate distances and some broken figure links. Underlying coordinate samples were not reproduced here.
[^4]: Mario Superstar Baseball research community, *Batting Mechanics*, revision 482, February 14, 2026; [permanent revision](https://mariobaseball.miraheze.org/wiki/Batting_Mechanics?oldid=482). Contact-dependent flight research. Internal units and the component example require verification.
[^5]: Mario Superstar Baseball research community, *Throwing Mechanics*, revision 382, February 14, 2026; [permanent revision](https://mariobaseball.miraheze.org/wiki/Throwing_Mechanics?oldid=382). Credits Roeming for throw-speed discovery. Explicitly leaves time conversion and deceleration unresolved; includes reported Quick Throw frame examples.
[^6]: Mario Superstar Baseball research community, *Running Mechanics*, revision 426, February 14, 2026; [permanent revision](https://mariobaseball.miraheze.org/wiki/Running_Mechanics?oldid=426). Source-estimated handedness/mashing intervals and startup delay. Some formula/prose inconsistencies remain.
[^7]: Project Rio developers, [Project Rio](https://www.projectrio.online/) and [Game Modes](https://www.projectrio.online/gamemode). Instrumentation and mod configuration context. The linked [Roeming batting calculator](https://roeming.github.io/MSSB_Batting_Calculator/) is a follow-up implementation lead, not an independently validated source of Harbor coefficients.
[^8]: Typhlosion4President, *Mario Super Sluggers – Exhibition Mode – Mario Stadium*, [gameplay recording](https://www.youtube.com/watch?v=3-5aQh-fxZk&t=60s). Qualitative view inspected at approximately 01:00. Capture cadence/control configuration still need annotation. WishingTikal, *Mario Superstar Baseball FULL GAME Longplay (Gamecube) 4k*, [candidate recording](https://www.youtube.com/watch?v=Y47r3r7JmeQ). Introductory/cinematic samples excluded; matched live-play dataset pending.
[^9]: MLB, [Field Dimensions](https://www.mlb.com/glossary/rules/field-dimensions). Regulation baseline and pitching distance only.
[^10]: Grand Sluggers `eff10d9`: `Diamond.cs`, `ParkDiamond.cs`, `BallFlight.cs`, `BattedBall.cs`, `InPlay.cs`, `Runner.cs`, `LivePlaySystem.Field.cs`, `AtBatResolver.cs`, `PlayTrace.cs`, `data/rules/{batting,flight,fielding,running}.json`, `data/parks/harbor-diamond.json`, `FieldingScenarioTests`, `AtBatScenarioTests.S29`, and `FlightFixtures`. Repository source is authoritative for the current implementation, not proof of reference parity.
