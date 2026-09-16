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

**Accepted trial anchor — F693-03-good-chemistry, September 14, 2026:** [retain the existing 1.30× good-pair travel-speed boost](research-game-feel-708.md#accepted-decision--chemistry-on-direct-and-relay-throws), once per actual thrower/receiver pair, equally for direct and relay legs; no extra whole-chain bonus or shortening of the 0.30-second ordinary release. Negative chemistry is accepted below; relay ownership is accepted below; special stacking and exact input-buffer/cancel details still require review before implementation.

**Accepted trial profile — F693-03-long-throw-numbers, September 14, 2026:** [160-foot middle-arm comfortable range with gradual long-range loss](research-game-feel-708.md#accepted-decision--comfortable-range-and-long-throw-timing), ±5 feet per Field point; 0.60 s extra neutral flight at 80 feet beyond range with quadratic growth; pair chemistry applied once to flight. Accepted original trial profile, not a tested game or verified Mario formula. See the derived neutral/strong/weak/chemistry/direct/relay comparisons and their idealized assumptions.

**Accepted trial anchor — F693-03-negative-chemistry, September 14, 2026:** [predictable 0.90× bad-pair travel speed instead of the additional random slant](research-game-feel-708.md#accepted-decision--bad-chemistry-and-routine-reliability), ordinary release unchanged. Jack explicitly accepted this deliberate deviation from Mario-style occasional off-target chemistry throws. General accuracy/errors remain separately governed.

**Accepted control direction — F693-03-relay-ownership, September 14, 2026:** [one deliberate command per onward relay leg, with early-input buffering](research-game-feel-708.md#accepted-decision--ownership-of-the-onward-relay-throw). Hold without a command; allow cancel/retarget before release motion starts. Accepted control direction; buffer duration and cancel mapping remain separate pending details.

**Accepted trial ability — F693-03-snap-throw, September 14, 2026:** [0.22-second release after cleanly receiving a teammate throw, replacing the 1.22× flight-speed boost](research-game-feel-708.md#accepted-decision--snap-throw-as-a-quick-transfer). Accepted trial ability definition; ordinary pickups remain at 0.30 seconds and chemistry affects flight only. Laser and exact input-buffer/cancel details follow individually.

**Accepted trial ability — F693-03-laser-throw, September 14, 2026:** [25% travel-speed bonus on actual throws home with a runner on third or between third and home](research-game-feel-708.md#accepted-decision--laser-as-a-home-plate-specialty), replacing the universal 45% bonus. Accepted authored trial; ordinary release remains, chemistry stacks once and no bonus passes through the entire relay chain.

**Accepted input trial — F693-03-input-buffer, September 14, 2026:** [0.25-second maximum age for an early onward-throw command](research-game-feel-708.md#accepted-decision--how-long-an-early-throw-press-remains-valid). Execute immediately when secure possession/recovery permits; expire otherwise. Accepted numerical trial. Cancel/retarget mapping and feedback follow separately.

**Accepted control direction — F693-03-throw-cancel, September 14, 2026:** [RB / period cancels a queued defensive throw; existing bag selectors retarget it](research-game-feel-708.md#accepted-decision--cancel-and-retarget-a-queued-throw). Accepted contextual binding with explicit item-input precedence and selected-versus-queued feedback; no timer refresh on retarget.

**Accepted movement trial — F693-02-pursuit-speed, September 14, 2026:** [18 ft/s ordinary Run-5 pursuit, scaling the existing relative stat curve](research-game-feel-708.md#accepted-decision--ordinary-pursuit-top-speed). Accepted top-speed trial; read, acceleration/braking, dash/status and coverage follow separately. Compare both routine-defense cost and compact deep-chase gains; no candidate simulation is claimed.

**Accepted read trial — F693-02-outfield-read-clock, September 14, 2026:** [0.40-second base outfield pursuit read from contact](research-game-feel-708.md#accepted-decision--outfield-post-contact-read), with no extra camera wait. Accepted authored trial shorter than the reported GameCube read; retain contact-clock ownership and existing airborne cap. Infield/pitcher/catcher timing follows separately.

**Accepted read trial — F693-02-infield-read-clock, September 14, 2026:** [shared 0.25-second read for 1B/2B/SS/3B](research-game-feel-708.md#accepted-decision--the-four-base-infielders-read). Accepted authored trial close to existing .25–.30 values; pitcher and catcher follow individually. Preserve contact-clock/cap semantics and readable action before the current fielding-view cut.

**Accepted read trial — F693-02-pitcher-read-clock, September 14, 2026:** [0.35-second base pitcher pursuit read from contact](research-game-feel-708.md#accepted-decision--pitcher-post-contact-read). Accepted authored trial between the base infield and current pitcher timing; no extra camera/follow-through timer. Validate comebackers, bunts and delivery-to-field motion. Catcher follows separately.

**Accepted read trial — F693-02-catcher-read-clock, September 14, 2026:** [0.45-second base catcher pursuit read after batted contact](research-game-feel-708.md#accepted-decision--catcher-post-contact-read). Accepted authored trial; no added rise/camera delay and no new wait on steal throws or home receptions. Validate bunt/short-pop races with the preserved catcher start.

**Accepted movement trial — F693-02-pursuit-acceleration, September 14, 2026:** [linear 0.20-second ordinary pursuit build-up from rest](research-game-feel-708.md#accepted-decision--ordinary-pursuit-acceleration). Accepted authored trial, same duration across characters at their respective top speeds; movement starts when eligible, without extra standstill. Braking and turning follow separately.

**Accepted movement trial — F693-02-pursuit-braking, September 14, 2026:** [linear stop from ordinary full speed in .10 seconds](research-game-feel-708.md#accepted-decision--ordinary-pursuit-braking). Accepted authored trial; constant braking magnitude per character, shorter stopping time from partial speed, and planned assisted arrival. Neutral stick retains assisted pursuit; no new stationary-catch prerequisite.

**Accepted movement trial — F693-02-pursuit-reversal, September 14, 2026:** [full opposite-direction reversal using the accepted brake and start](research-game-feel-708.md#accepted-decision--full-pursuit-reversal). Accepted authored trial: .10-second full-speed brake, then .20-second acceleration in the opposite direction, without a pivot pause. Angled corrections follow separately.

**Accepted movement trial — F693-02-pursuit-angled-turn, September 14, 2026:** [continuous angled correction through shared brake/start rates](research-game-feel-708.md#accepted-decision--angled-pursuit-corrections). Accepted authored trial: full-speed 45-degree turn about .115 seconds with 92% minimum speed; 90-degree about .212 seconds with 71% minimum speed. Continuous extension to the accepted reversal, no angle threshold or added stop.

**Accepted movement trial — F693-02-pursuit-analog-response, September 14, 2026:** [linear speed across the active radial stick range](research-game-feel-708.md#accepted-decision--fine-analog-pursuit). Accepted authored trial for fine positioning through the accepted response; numerical neutral boundary, drift handling and assistance transition follow separately. Existing per-axis filtering/recentering requires an explicit pursuit-input audit.

**Accepted movement trial — F693-02-pursuit-neutral-boundary, September 14, 2026:** [manual pursuit at .20 radial tilt, assistance at .15](research-game-feel-708.md#accepted-decision--neutral-and-manual-pursuit-boundary). Accepted calibrated-input thresholds with retained ownership between them and manual speed mapped from .15. Calibration/arming and drift behavior follow separately; this is not a global input remap.

**Accepted movement policy — F693-02-pursuit-calibration-policy, September 14, 2026:** [released-stick calibration outside live play, fixed through the match](research-game-feel-708.md#accepted-decision--stable-controller-calibration). Accepted policy with explicit paused recalibration; sample acceptance and arming details remain open. Audit correction: StickPlay recenter magnitude is radial; the separate FieldAssist takeover test is L1.

**Accepted movement policy — F693-02-pursuit-arming, September 14, 2026:** [neutral once at role/device recovery boundaries, retained readiness through the half](research-game-feel-708.md#accepted-decision--pursuit-readiness-and-held-direction). Accepted policy: valid calibrated neutral <=.15 without extra dwell, held direction works at contact, other verb guards and recovery pause behavior remain explicit.

**Accepted input trial — F693-02-pursuit-calibration-samples, September 14, 2026:** [.50-second sample window, .10 maximum center offset, .02 maximum sample deviation](research-game-feel-708.md#accepted-decision--calibration-sample-acceptance). Accepted numerical preset for requested non-live calibration, not routine arming. Failed sampling retains the prior profile; real controller validation remains open.

**Accepted replacement — F693-02-ball-dash-carrier, September 14, 2026:** [automatic 20% Ball Dash](research-game-feel-708.md#current-dash-revision--passive-ball-carrier-ability), only for ability holders securely carrying the ball, without activation or burst/cooldown. This supersedes universal fielding sprint and its two-second proposal, preserving their history. Carry base is accepted separately below; response and roster allocation remain separate.

**Accepted movement trial — F693-02-ordinary-carry-speed, September 14, 2026:** [ordinary carrying top speed equals ordinary pursuit top speed](research-game-feel-708.md#accepted-decision--ordinary-carrying-speed), with the accepted Ball Dash factor applied only to eligible carriers. Run-5 carries at 18 or 21.6 ft/s in the trial. Numerical response and complete races remain separate.

**Accepted movement trial — F693-02-carry-movement-response, September 14, 2026:** [shared carrying response with unchanged ordinary rates](research-game-feel-708.md#accepted-decision--carrying-movement-response). Ball Dash raises the target cap only: .24-second start/.12 stop/.36 full reversal at its full speed. Possession changes preserve actual velocity; recovery and movement eligibility remain explicit.

**Accepted readiness trial — F693-02-clean-ground-pickup-readiness, September 14, 2026:** [zero generic post-possession pause on routine clean ground pickups](research-game-feel-708.md#accepted-decision--clean-ground-pickup-readiness). Preserve visible acquisition, secure ownership and exceptional recovery; a valid command starts the existing .30-second ordinary release at readiness.

**Accepted recovery basis — F693-02-ground-pickup-recoil-basis, September 15, 2026:** [actual incoming pickup speed plus Field rating](research-game-feel-708.md#accepted-decision--retained-ball-ground-pickup-recoil-basis), deterministic, with zero recovery in the routine region. Threshold/duration/Field curve, physical reaction and special cases remain separate.

**Accepted recovery cap — F693-02-ground-pickup-recoil-cap, September 15, 2026:** [.20-second ordinary retained-ball recovery ceiling](research-game-feel-708.md#accepted-decision--retained-ball-ground-pickup-recovery-cap). Jack explicitly requires special-hit exceptions: authored special recovery may exceed .20. Not a fixed pause: routine pickups remain zero. Trigger/curve/Field mapping and physical/action recovery remain separate; .25 buffer/.30 release remain intact.

**Accepted composition — F693-02-special-recovery-composition, September 15, 2026:** [add ordinary and special recovery from the same impact](research-game-feel-708.md#accepted-decision--ordinary-and-special-recovery-composition), preserving the ordinary Fielding time difference. Prior unaccepted overlap proposal remains in history. Unrelated/repeated effects, special values and per-action permissions remain separate.

**Accepted recoil shape — F693-02-recoil-field-shaping, September 15, 2026:** [shared incoming-speed onset; Field reduces duration after severity is capped](research-game-feel-708.md#accepted-decision--fieldings-role-in-ordinary-recoil), preventing the .20 cap from erasing stat differences at high severity. Numerical thresholds/curves/factors remain unselected.

**Accepted recoil factors — F693-02-recoil-field-factors, September 15, 2026:** [five-percentage-point factor reduction per Fielding point above 1](research-game-feel-708.md#accepted-decision--numerical-fielding-recovery-factors). Full-severity ordinary recoil .20/.16/.11 at Field1/5/10. Ratings, onset/full-severity speed and severity curve remain separate.

**Accepted severity curve — F693-02-recoil-severity-curve, September 15, 2026:** [linear severity between shared onset/full-severity incoming speeds](research-game-feel-708.md#accepted-decision--ordinary-recoil-severity-curve). Numerical speed anchors stay null pending event-sided arrival evidence and coherent ball-motion calibration; choose shape only.

**Accepted recoil permissions — F693-02-ordinary-recoil-actions, September 15, 2026:** [block commanded steering/throw starts; preserve valid force/tag contacts and input management](research-game-feel-708.md#accepted-decision--actions-during-ordinary-recoil). Ordinary recoil only; special permissions remain separate.

**Accepted physical recoil — F693-02-ordinary-recoil-displacement, September 15, 2026:** [modest physical skid on qualifying hard pickups](research-game-feel-708.md#accepted-decision--physical-displacement-during-ordinary-recoil), including actual bag/tag contact consequences. Jack emphasized special hits that impact the fielder; their stronger effects remain explicitly available.

**Accepted ordinary displacement ceiling — F693-02-ordinary-recoil-distance-cap, September 15, 2026:** [one-foot maximum added ordinary impact displacement](research-game-feel-708.md#accepted-decision--ordinary-pushback-distance-ceiling), separate from pre-existing movement and special impact bounds.

**Accepted ordinary skid response — F693-02-ordinary-recoil-motion-profile, September 15, 2026:** [brief impact kick with steady slowing and smaller skids for stronger Fielding](research-game-feel-708.md#accepted-decision--ordinary-skid-response). `D=w²` feet and `K=10w` ft/s over `.20w` seconds, with existing locomotion braking retained.

**Accepted special motion composition — F693-02-special-impact-motion-composition, September 15, 2026:** [combine ordinary and special motion concurrently from the same impact](research-game-feel-708.md#accepted-decision--combining-ordinary-and-special-impact-motion), with individual bounds/clocks and the accepted additive action recovery.

**Accepted special impact resistance — F693-02-special-impact-field-resistance, September 15, 2026:** [up to 20% Fielding resistance to the special physical impact itself](research-game-feel-708.md#accepted-decision--fielding-resistance-to-special-impacts), separate from ordinary recoil and non-impact statuses.

**Accepted special-pushback possession — F693-02-special-pushback-possession, September 15, 2026:** [retain secure possession through pure pushback; explicitly author dislodging exceptions](research-game-feel-708.md#accepted-decision--possession-during-special-pushback). Existing acquisition/bobble outcomes remain separate.

**Accepted pure-special-pushback actions — F693-02-special-pushback-actions, September 15, 2026:** [ordinary recoil permissions for pure special pushback](research-game-feel-708.md#accepted-decision--actions-during-pure-special-pushback): delay steering/throw starts, preserve valid held-ball contact and input management.

**Accepted mixed-status readiness — F693-02-mixed-status-action-readiness, September 15, 2026:** [recover actions individually as their restrictions end](research-game-feel-708.md#accepted-decision--action-readiness-with-multiple-statuses), preserving the accepted same-impact recovery sum and other active restrictions.

**Accepted repeated-impact recovery — F693-02-repeated-impact-recovery, September 15, 2026:** [start each distinct impact recovery at its own arrival](research-game-feel-708.md#accepted-decision--recovery-from-repeated-physical-impacts), taking the latest applicable end without queuing full durations after unfinished waits. Same-impact ordinary/special addition stays intact.

**Accepted special repeat eligibility — F693-02-special-impact-repeat-eligibility, September 15, 2026:** [one physical hit per fielder per special activation by default](research-game-feel-708.md#accepted-decision--repeat-hits-from-the-same-special), with explicitly reviewed multi-hit exceptions.

**Accepted standing fly-catch readiness — F693-02-clean-air-catch-readiness, September 15, 2026:** [zero extra pause after a clean standing fly catch](research-game-feel-708.md#accepted-decision--readiness-after-a-clean-standing-fly-catch), preserving secure acquisition, ordinary release and live runner rules.

**Accepted grounded hard-air-catch recoil — F693-02-grounded-air-catch-recoil, September 15, 2026:** [reuse ordinary recoil response for qualifying hard airborne catches by grounded fielders](research-game-feel-708.md#accepted-decision--impact-recoil-on-grounded-airborne-catches). Routine catches retain zero added delay; airborne trigger speeds and whether they share ground anchors require evidence.

**Accepted jump-catch throw readiness — F693-02-jump-catch-throw-readiness, September 15, 2026:** [land before starting a normal throw after a jumping catch](research-game-feel-708.md#accepted-decision--throwing-after-a-jumping-catch), with no generic extra clean-landing pause.

**Accepted normal-jump air control — F693-02-jump-air-control, September 15, 2026:** [limited horizontal correction with preserved takeoff momentum](research-game-feel-708.md#accepted-decision--control-during-a-normal-jump), same policy before/after a catch. Numerical limits remain coupled to airtime.

**Accepted normal-jump input profile — F693-02-normal-jump-input-profile, September 15, 2026:** [consistent jump per eligible press, without hold-for-height or release-to-shorten](research-game-feel-708.md#accepted-decision--normal-jump-press-and-hold-behavior). Numerical arc and character variation remain pending; the rechecked Mario manuals do not establish measured arcs.

**Accepted normal-jump takeoff ownership — F693-02-normal-jump-takeoff-ownership, September 15, 2026:** [start promptly from eligible input instead of scheduling to the ball](research-game-feel-708.md#accepted-decision--who-determines-normal-jump-takeoff-timing). Numerical startup and any early-input buffer remain separate.

**Accepted normal-jump arc — F693-02-normal-jump-arc-trial, September 15, 2026:** [2-foot root rise over .60 seconds, with a midpoint apex](research-game-feel-708.md#accepted-decision--normal-jump-height-and-airtime-trial). Authored baseline trial; actual glove reach, character/ability variation and matched reference measurements remain pending.

**Accepted normal-jump air response — F693-02-normal-jump-air-response-trial, September 15, 2026:** [10% ground acceleration/braking rates for active midair correction](research-game-feel-708.md#accepted-decision--normal-jump-air-response-strength). Preserve neutral drift and actual velocity; the Run5 .60-second correction envelope is at most 3.24 feet absent external effects.

**Accepted normal-jump startup — F693-02-normal-jump-startup-trial, September 15, 2026:** [zero added gameplay delay before an eligible normal jump](research-game-feel-708.md#accepted-decision--normal-jump-startup-timing). Physical and visible takeoff must agree; device latency and buffering ineligible input remain separate.

**Accepted normal-jump input buffer — F693-02-normal-jump-input-buffer, September 15, 2026:** [.10-second grounded early-input grace across temporary read/recovery restrictions](research-game-feel-708.md#accepted-decision--slightly-early-normal-jump-input). No airborne queue or ball scheduling; explicit expiry, cancellation and ownership boundaries.

**Accepted ordinary character jump profile — F693-02-normal-jump-character-profile, September 15, 2026:** [same 2-foot/.60-second ordinary vertical profile across characters](research-game-feel-708.md#accepted-decision--ordinary-jump-differences-between-characters). Preserve actual body/glove reach and movement/stat differences; exceptional abilities and catch geometry remain separately reviewed.

**Accepted normal-jump catch input — F693-02-normal-jump-catch-input, September 15, 2026:** [one jump press, with no separate catch press on eligible actual contact](research-game-feel-708.md#accepted-decision--catching-during-a-normal-jump). Retain the existing input relationship through physical-jump work; no guaranteed possession or new geometric tolerance.

**Superseded proposal — F693-02-normal-jump-glove-tracking:** [Field-dependent glove response was not accepted](research-game-feel-708.md#historical-proposal--field-dependent-glove-adjustment-superseded). Jack rejected Fielding affecting glove positioning.

**Accepted catch-range direction — F693-02-character-catch-range, September 15, 2026:** [explicit character range supports glove placement, independent of displayed Fielding](research-game-feel-708.md#accepted-direction--character-catch-range-without-field-driven-glove-positioning). Shape/dimensions and matching authored motion remain pending.

**Accepted Fielding rating role — F693-02-fielding-rating-role, September 15, 2026:** [Fielding summarizes named defensive traits/abilities](research-game-feel-708.md#accepted-decision--what-the-fielding-rating-represents). Error rules, weights and migration of approved Field-based throwing/recoil remain separate required decisions.

**Accepted handling-error scope — F693-02-handling-error-opportunities, September 15, 2026:** [reliable routine acquisition, errors only in reviewed difficult/disrupted contexts](research-game-feel-708.md#accepted-decision--when-handling-errors-can-happen). No trigger catalog, probabilities or error-resolution model selected.

**Superseded resolution — F693-02-ordinary-handling-resolution:** [deterministic limits were briefly approved then revised by Jack](research-game-feel-708.md#historical-proposal--consistent-handling-limits-superseded). Do not implement that historical proposal.

**Accepted ordinary error-chance direction — F693-02-ordinary-handling-error-chance, September 15, 2026:** [small chance based on difficulty and underlying defensive quality](research-game-feel-708.md#accepted-direction--difficulty-and-defense-determine-error-chance). Routine reliability remains; exact curves, opportunity identity and failure outcomes remain pending.

**Accepted ordinary error ceiling — F693-02-ordinary-handling-error-cap, September 15, 2026:** [10% ceiling per qualifying difficult ordinary acquisition attempt](research-game-feel-708.md#accepted-decision--maximum-ordinary-handling-error-chance). Specials remain separate; this is neither a blanket chance nor an observed per-game rate.

**Accepted ordinary chance curve — F693-02-ordinary-handling-chance-curve, September 15, 2026:** [linear difficulty and handling response: 10% / 6% / 2% at full difficulty](research-game-feel-708.md#accepted-decision--difficulty-and-handling-probability-curve). Physical difficulty and underlying trait mappings remain unselected.

**Accepted first difficulty source — F693-02-awkward-hop-difficulty-source, September 15, 2026:** [awkward in-between hop at actual ground-ball acquisition](research-game-feel-708.md#accepted-decision--awkward-hop-as-the-first-difficulty-source). Numerical bands and mapping remain pending; clean rolls/short/long hops remain outside this source.

**Accepted ordinary error outcome — F693-02-ordinary-bobble-outcome, September 15, 2026:** [modest visible loose-ball bobble requiring recovery](research-game-feel-708.md#accepted-decision--ordinary-bobble-outcome). Scatter, reaction and fresh-attempt rules remain pending.

**Accepted correction — F693-02-bobble-stun, September 15, 2026:** [brief stun, then resume pursuit and pickup](research-game-feel-708.md#accepted-direction--brief-bobble-stun). Supersedes the unaccepted movement-available recovery proposal; duration, entry motion and fresh-attempt rules remain pending.

**Accepted correction — F693-02-uniform-bobble-stun, September 15, 2026:** [same ordinary stun duration across characters](research-game-feel-708.md#accepted-direction--shared-ordinary-bobble-stun). Jack declined handling-based shortening; error probability retains its handling benefit.

**Accepted duration — F693-02-bobble-stun-duration, September 15, 2026:** [shared .40-second ordinary stun trial](research-game-feel-708.md#accepted-decision--ordinary-bobble-stun-duration). Authored anchor, not measured Mario timing; total recovery still includes physical pursuit/pickup.

**Accepted grounded motion — F693-02-grounded-bobble-braking, September 15, 2026:** [ordinary braking within the stun](research-game-feel-708.md#accepted-decision--grounded-bobble-braking). Full ordinary speed stops in .10 seconds, concurrently with the .40-second stun; grounded locomotion only.

**Accepted recovery — F693-02-bobble-recovery-reliability, September 15, 2026:** [reliable legal recovery of the same ordinary bobble](research-game-feel-708.md#accepted-decision--reliable-ordinary-bobble-recovery). No repeat ordinary roll from the same fumble; physical eligibility and independently reviewed effects still apply.

**Accepted correction — F693-02-contact-led-random-bobble, September 15, 2026:** [contact-led direction with small random variation](research-game-feel-708.md#accepted-direction--contact-led-random-bobble). Supersedes the unaccepted no-random-direction proposal; retain one authoritative seeded result per actual bobble.

**Accepted spread — F693-02-bobble-direction-spread, September 15, 2026:** [±30 degrees horizontally, 60-degree total spread](research-game-feel-708.md#accepted-decision--horizontal-bobble-direction-spread). Jack increased the proposed bound; distribution and trajectory values remain pending.

**Accepted outcome expansion — F693-02-expanded-ordinary-error-outcomes:** [local bobbles, balls getting past and continuing deflections](research-game-feel-708.md#accepted-direction--expanded-ordinary-error-outcomes). Especially review hard-hit infield balls; new-branch reaction/recovery/randomness inheritance is unselected.

**Accepted outcome basis — F693-02-error-outcome-selection, September 15, 2026:** [current ball motion and contact determine the consequence](research-game-feel-708.md#accepted-decision--selecting-the-ordinary-error-outcome). No independent severity roll; numerical classifier and retained-motion model remain pending.

**Accepted reaction — F693-02-continuing-error-reaction, September 15, 2026:** [same .40-second contacted-failure stun, no added untouched-miss stun](research-game-feel-708.md#accepted-decision--reaction-to-a-continuing-ordinary-error). Recovery and directional randomness remain pending.

**Accepted recovery extension — F693-02-continuing-error-recovery, September 15, 2026:** [reliable recovery of the same continuing handling error](research-game-feel-708.md#accepted-decision--recovery-of-a-continuing-ordinary-error). An untouched miss grants no protection; the later first handling attempt keeps its normal rules.

**Accepted narrower direction — F693-02-continuing-error-direction, September 15, 2026:** [±15-degree continuing-deflection variation](research-game-feel-708.md#accepted-decision--directional-variation-of-a-continuing-ordinary-error). Continuing contact slows the ball while preserving more incoming direction; local bobbles retain ±30 degrees.

**Accepted speed bounds — F693-02-continuing-error-speed-retention, September 15, 2026:** [50–80% of actual pre-contact horizontal speed](research-game-feel-708.md#accepted-decision--continuing-deflection-speed-retention). Contact mapping, branch thresholds and vertical response remain pending.

**Accepted distribution — F693-02-uniform-error-direction, September 15, 2026:** [uniform angular variation](research-game-feel-708.md#accepted-decision--uniform-error-direction-distribution). Jack selected even probability within local ±30-degree and continuing ±15-degree limits, superseding the triangular proposal.

**Accepted shape — F693-02-local-bobble-vertical-shape, September 15, 2026:** [downward spill and low bounce](research-game-feel-708.md#accepted-decision--local-bobble-vertical-shape). Shape only; exact bounce height, speed and distance remain pending.

**Accepted ceiling — F693-02-local-bobble-rebound-ceiling, September 15, 2026:** [six-inch maximum ground rebound](research-game-feel-708.md#accepted-decision--local-bobble-rebound-ceiling). Weaker impacts can rebound less or settle. Authored trial; reference measurements remain missing.

**Accepted bounce strength — F693-02-local-bobble-restitution, September 15, 2026:** [35% vertical speed retention at ground impact](research-game-feel-708.md#accepted-decision--local-bobble-bounce-strength). Settling cutoff and post-glove velocity remain open.

**Accepted settling — F693-02-local-bobble-settling, September 15, 2026:** [settle when the next rebound would rise three inches or less](research-game-feel-708.md#accepted-decision--local-bobble-settling). Jack increased the pending one-inch recommendation. Evaluate at actual ground collision; preserve horizontal motion and same-error recovery.

**Accepted glove release — F693-02-local-bobble-glove-release, September 15, 2026:** [absorb vertical motion, then drop immediately under gravity](research-game-feel-708.md#accepted-decision--local-bobble-release-from-the-glove). Actual contact height; zero hold or possession.

**Accepted horizontal ceiling — F693-02-local-bobble-horizontal-cap, September 15, 2026:** [six-foot-per-second maximum local spill](research-game-feel-708.md#accepted-decision--local-bobble-horizontal-speed-ceiling). No minimum kick; contact mapping and ground response remain pending.

**Accepted local retention — F693-02-local-bobble-horizontal-retention, September 15, 2026:** [20% incoming horizontal speed, capped at six ft/s](research-game-feel-708.md#accepted-decision--local-bobble-horizontal-speed-retention). Actual pre-contact speed; zero input stays zero.

**Accepted ground impact — F693-02-local-bobble-ground-horizontal, September 15, 2026:** [90% horizontal retention at actual ground impact](research-game-feel-708.md#accepted-decision--local-bobble-horizontal-response-at-ground-impact). Jack increased the pending 80% proposal; apply once per real impact.

**Accepted rolling — F693-02-local-bobble-rolling-deceleration, September 15, 2026:** [six-ft/s-per-second slowdown](research-game-feel-708.md#accepted-decision--local-bobble-rolling-slowdown). A 5.4-ft/s roll stops in .90 seconds over 2.43 feet if untouched; ground-only arithmetic.

**Accepted continuing vertical response — F693-02-continuing-error-vertical-retention, September 15, 2026:** [same 50–80% factor for signed vertical and horizontal speed](research-game-feel-708.md#accepted-decision--continuing-deflection-vertical-response). Preserve rising/falling direction; exact factor mapping remains open.

**Accepted continuing ground response — F693-02-continuing-error-ground-response, September 15, 2026:** [shared ordinary batted-ball response](research-game-feel-708.md#accepted-decision--continuing-deflection-ground-response). Actual deflected state; preserve reliable recovery. Numerical ordinary ground calibration remains pending.

**Current correction — F693-02-arcade-fielding-simplification, September 15, 2026:** [simple range/readiness-based arcade fielding](research-game-feel-708.md#current-direction--simple-arcade-fielding). Jack rejected the level of glove simulation: pocket facing is visual; detailed surface/normal and catch-side rules are retired. The prior collider-dependent curve is not a required runtime formula; speed bounds and other feel anchors remain trials.

**Superseded history:** F693-02-continuing-error-retention-curve, F693-02-error-contact-obstruction-basis, F693-02-glove-contact-surface and the unaccepted F693-02-glove-catch-sides. Preserve their records for provenance, not as implementation requirements.

**Research complete — F693-02-arcade-fielding-validation, September 15, 2026:** [the consolidated simplified contract](research-game-feel-708.md#consolidated-simplified-fielding-contract) states the three representative plays end to end and separates retained anchors from superseded microphysics and open mappings. No glove microdecision awaits approval. Simple response mapping, traits and complete race validation remain open.

**Accepted reach trial — F693-02-catch-reach-envelope, September 15, 2026:** [re-author the ordinary stand-up catch reach to roughly 6 feet](research-game-feel-708.md#accepted-decision--re-author-the-catch-reach). The compact alley is 30% narrower against unchanged reach and pursuit, so today's 13-foot radius closed it at 2.17 seconds and the third-to-short hole at 1.07; 6 feet reopens those to 2.56 and 1.46 and is the only option the visible glove can meet. Stand-up magnitude only. Dive, jump and scoop reach are still +8 / +8 / +4 feet and are now the dominant reach, and `10 + 0.6 × Field` cannot survive this decision. No rules file or runtime change.

**Accepted dive direction — F693-02-dive-jump-scoop-reach, September 15, 2026:** [the dive is earned, and it costs something](research-game-feel-708.md#accepted-decision--the-dive-is-earned-and-it-costs-something). The runtime audit found the dive fires automatically for any fielder the seat is not steering and for every CPU fielder, so 6 feet plus an automatic 8 gave 14 feet of passive coverage, more than the 13 just removed. Jack removed the assistance dive entirely: a dive happens only on a deliberate press, passive coverage is the accepted 6 feet in the air and 10 on the dirt, and an earned dive reaches 14. He also added a dive recovery delay before throwing or moving, so dives are a last resort rather than a default — new behaviour, since `DiveT` today is only an arm window that gates nothing. Duration, caught-versus-missed cost and character variation are open under F693-02-dive-recovery-cost; CPU dive intent is open under F693-02-cpu-dive-intent.

**Correction, September 15, 2026:** an earlier note said the seat steers one fielder while the other eight dive automatically. The sim runs exactly one active glove, `GlovePos`, steered by the seat or driven by `ChaseGlove` assistance on a dead stick and handed on as the play develops. The automatic dive was therefore two cases, not eight: a human glove whose stick is neutral, and the entire CPU defence unconditionally. The 14-foot finding, the coverage arithmetic and both accepted decisions are unaffected.

**Accepted CPU dive direction — F693-02-cpu-dive-intent, September 15, 2026:** [the CPU dives deliberately](research-game-feel-708.md#accepted-decision--the-cpu-dives-deliberately). The CPU defence reached the rim only through `AutoDive`, so removing it left the CPU with no dive at all, and the dive is worth a flat 16-foot band of every gap. The CPU glove now gets deliberate dive intent: it chooses, it can miss, it pays the same recovery delay, and it reaches the same 14 feet a seat does. Jack explicitly declined the convert-only option, so a CPU dive that fails is required behaviour, not a defect. Intent policy, lookahead budget and difficulty scaling are open under F693-02-cpu-dive-intent-policy.

**Accepted dive recovery trial — F693-02-dive-recovery-cost, September 15, 2026:** [0.60 seconds on a narrow curve](research-game-feel-708.md#accepted-decision--a-060-second-dive-recovery-on-a-narrow-curve). Derived from the packet's own anchors: the illustrative routine race leaves a 0.50-second margin and a dive delay is spent inside it, so 0.60 seconds is the shortest value at which an unnecessary dive costs the out. Recovery narrows 2.5% per defensive-quality point to 0.465 seconds at the highest — deliberately narrower than the recoil curve, so even the best defender only reaches a dead heat on a dive they did not need. Quality comes from an explicit defensive trait, never displayed Fielding. Caught and missed dives cost the same, and the delay overlaps the 0.40-second handling stun rather than adding to it, a recorded departure from the additive precedent in F693-02-special-recovery-composition. The CPU pays the same. The defensive-trait migration, now consumed by both the error chance and this curve, blocks implementation and is queued as F693-02-defensive-trait-mapping.

**Accepted rating architecture — F693-02-defensive-trait-mapping, September 15, 2026:** [Arm splits out](research-game-feel-708.md#accepted-decision--arm-splits-out). The required inventory found `Stats.Field` driving four distinct jobs — arm, hands, reach and three CPU-only reaction timings — and that the displayed Fielding number was simultaneously the arm rating and the hands rating, so a cannon-armed catcher with stone hands could not exist. There are now two defensive ratings: **Arm** for throw speed, comfortable range and accuracy, and **Fielding** for hands — recoil duration, the handling error chance and dive recovery. Reach belongs to neither. Arm is seeded from each character's current Field value, so every accepted throw anchor evaluates identically on day one. Schema, the `Teams.Tools` roster-building sum, the HUD row, the CPU reaction group and deliberate per-character Arm values are queued as F693-02-arm-rating-migration. Jump, loose-ball scoop and the 4-foot dirt pad are unchanged.

**Required migration — F693-02-defensive-trait-migration:** inventory every current/approved Field consumer and map it explicitly to underlying traits before implementation, including old bobble/drop paths and approved throwing/recoil anchors.

**Pending special completion — F693-02-special-attack-contracts:** individual attack lifetimes/values/permissions, repeated impulse composition, dislodging/multi-hit exceptions and independent-attack chains must be reviewed before implementation; returning to routine fielding does not approve these gaps.

The parent issue description was condensed after preserving its full history through 9ca738e in the [versioned tracker archive](research/game-feel-693-tracker-history.md); current statuses live here and in #708.

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

**In progress — #708:** the [compact-field decision packet](research-game-feel-708.md) proposes C80 (80-ft paths, 232/280/232-ft fences) and C70 (70-ft paths, 203/245/203-ft fences), with unchanged bodies and coordinated spatial conventions. **Jack accepted C80 as the lead spatial trial on September 14, 2026**; C70 remains unselected. The packet records existing race evidence, incompatible IF/OF time-preserving speed sensitivities, and the remaining one-at-a-time numerical decisions. It is not yet a complete runtime contract; no candidate has been simulated. F693-02-spatial-trial and F693-05-runner-clock are accepted. F693-03-release-clock is also accepted (0.30-second ordinary command-to-release). F693-03-travel-clock is accepted (0.90-second flight over 80 feet). Long-throw/relay direction is accepted with chemistry explicitly included. Good-pair chemistry treatment is accepted. The long-range numerical profile is accepted. Bad-pair chemistry treatment is accepted. Relay control direction is accepted. Snap Throw is accepted. Laser is accepted. The input-buffer window is accepted. Cancel/retarget controls and feedback are accepted. Ordinary pursuit speed is accepted. Outfield read is accepted. Base-infielder read is accepted. Pitcher read is accepted. Catcher read is accepted. Ordinary pursuit acceleration is accepted. Ordinary pursuit braking is accepted. Full reversal is accepted. Angled turning is accepted. Analog shaping is accepted. Neutral/assistance boundaries are accepted. Calibration policy is accepted. Pursuit arming is accepted. Calibration sample criteria are accepted. Passive 20% Ball Dash is accepted and supersedes universal sprint peak/duration. Ordinary carrying top speed is accepted. Carrying response is accepted. Clean ground-pickup readiness is accepted. Retained-ball recoil basis is accepted. The .20-second ordinary cap is accepted with special-hit exceptions. Same-impact additive composition is accepted. Field-dependent recoil shaping is accepted. Numerical Field factors are accepted. Severity curve is accepted. Ordinary recoil action permissions are accepted. Physical recoil displacement is accepted, especially for impacting specials. The one-foot ordinary displacement ceiling is accepted. The coupled ordinary skid response is accepted. Same-impact ordinary/special physical composition is accepted. Fielding resistance to the special impact itself is accepted. Possession during pure special pushback is accepted. Its action permissions are accepted. Action readiness with multiple statuses is accepted. Recovery timing across distinct repeated impacts is accepted. Repeat-hit eligibility within one special activation is accepted. Clean standing-air-catch readiness is accepted. Ordinary recoil response on hard airborne catches by grounded fielders is accepted. Throw readiness after a normal jumping catch is accepted. Normal-jump air control is accepted. Normal-jump press/hold behavior is accepted. Normal-jump takeoff timing ownership is accepted. The 2-foot/.60-second normal-jump arc trial is accepted. The 10% normal-jump air-response trial is accepted. Zero added normal-jump startup is accepted. The .10-second grounded jump-input buffer is accepted. The shared ordinary character jump profile is accepted. One-press normal-jump catching is accepted. Jack superseded the unaccepted Field-dependent glove proposal with explicit character catch range and no Field-driven glove positioning. Displayed Fielding as a summary of underlying defensive traits is accepted. Handling-error eligibility is accepted. Jack replaced deterministic resolution with a small chance based on difficulty and underlying defensive quality. The 10% ordinary probability ceiling is accepted. The linear difficulty/handling probability curve is accepted. The first awkward-hop difficulty source is accepted. The ordinary loose-ball bobble outcome is accepted. Jack replaced the movement-available recovery proposal with a brief bobble stun followed by resumed pursuit. Jack declined handling-based shortening and selected a shared ordinary stun duration. The shared .40-second stun trial is accepted. Grounded ordinary braking within the stun is accepted. Reliable recovery without another ordinary roll from the same bobble is accepted. Jack selected contact-led direction with a small random variation, superseding the no-random-direction proposal. Jack selected a 30-degree horizontal spread either side and expanded ordinary error outcomes to include balls getting past and continuing deflections. Contact/motion-based outcome selection is accepted. The contacted/untouched reaction distinction is accepted. Reliable recovery across continuing ordinary errors is accepted. Jack narrowed continuing deflections to a ±15-degree random-variation trial with speed loss and greater preservation of incoming direction. The 50–80% horizontal-speed retention band is accepted. Jack selected uniform angular sampling within each accepted limit, superseding triangular weighting. The local-bobble downward spill and low rebound shape is accepted. The six-inch local rebound ceiling is accepted. The 35% vertical speed retention trial is accepted. Jack selected a three-inch predicted-rebound settling threshold. The zero-vertical-speed local glove release with immediate gravity is accepted. The six-foot-per-second local horizontal spill ceiling is accepted. The 20% incoming horizontal-speed retention trial for local bobbles is accepted. Jack selected 90% horizontal retention at actual ground impact. The six-ft/s-per-second local rolling slowdown is accepted. Shared horizontal/vertical retention for continuing deflections is accepted. Shared ordinary ground response after continuing deflections is accepted. Jack subsequently simplified fielding to range/readiness-based arcade gameplay with visually correct glove facing. Detailed glove surfaces, incidence calculations, catch-side policy and mandatory geometry-dependent interpolation are superseded. Next work is consolidated reference/feel validation, not another glove microdecision; explicit consumer migration remains required. Individual special contracts, evidence-based trigger speeds, other recovery, coverage and ball calibration remain pending; the separate catcher steal-release clock remains a full-contract audit item. The arcade-fielding validation is now research complete. Jack accepted a re-authored 6-foot ordinary stand-up catch reach, superseding both today's absolute 13 feet and a basepath-scaled 11.56; dive, jump and scoop reach are now the dominant reach, and Jack then made the dive earned-only with a recovery delay before throwing or moving, leaving the delay's numbers and CPU dive intent as the named follow-ups. Remaining throw, pursuit/coverage, ball-motion and presentation values stay pending.

Decision review on #693 first. Jack resolves F693-01–06 from the evidence. The review artifact contains the candidate profile's complete quantities, intervals, invariants, and deviations, so approval concerns a concrete game contract.

Then create one gameplay implementation child per coherent change. If geometry moves, first migrate existing values into the shared data owner and prove parity. Implement the accepted clock/path relationships in named sim systems and `data/rules/`, leaving presentation and Blender to their owners. Compare unchanged control A against each candidate with identical declared inputs and seeds. Report effects instead of repairing each failed fixture with a private exception.

Do not add an experimental player-facing settings menu. Research profiles belong in the existing data/test workflow, and the shipped game has the selected contract. A candidate that changes an existing rule requires a corresponding spec decision and scenario update before it can be accepted.

### R3 next work items — sequenced September 15, 2026

Seven rows remain in the #708 queue and no human decision is pending. The fielding contract is decided end to end but **has never been run**: roughly ninety-five accepted anchors exist only as authored arithmetic, and none of them has met the others. The sequence below is ordered by that fact, not by queue order.

**1. Flight budget — `F693-04-flight-budget`. Design, expect one or two decisions.**
The last remaining design input, and the one that decides whether the fielding numbers are right. Everything accepted so far describes what the defence *can* cover; the flight budget decides what it *has* to cover. Contact-class flight, bounce, roll and wall play inside 232 / 280 / 232 feet. The coverage arithmetic is explicitly contingent on it — the closure tables state a ceiling, and how many real batted balls land inside the open window is a flight question. **Do this before implementation**, because it can move reach and pursuit, and moving them after they are in code is more expensive.

**2. Coverage budget — `F693-02-coverage-budget`. Research-heavy, perhaps one decision.**
Cover and cutoff movement, receiver readiness, and pickup/recovery against complete races. Catch reach, its original headline item, is now answered. What remains is whether a receiver is actually at the bag when the throw lands on a field this size, which is a relay and double-play question rather than a fielding one.

**3. Implementation slice one — the fielding contract in code.**
Where this packet stops being documentation. It covers `F693-02-arm-rating-migration` (schema, seeding, the `Teams.Tools` decision, the HUD row, the CPU reaction group), the authored reach property replacing `10 + 0.6 × Field`, dive ownership and the recovery delay, the handling error chance on the seeded rail, and `F693-02-cpu-dive-intent-policy`. Because Arm is seeded from Field, **no roster authoring is needed for this slice** — the split is numerically free on day one. Follow R3's existing rule: migrate values into the shared data owner and prove parity before changing behaviour.

**4. Whole-race validation — the traces R2 built the instrumentation for.**
A C80 profile exercising reach, dive, delay, throws, pursuit and the runner clock at once, across a routine grounder, a gap ball, a relay and a steal. **Expect accepted trial numbers to move here.** They were each derived soundly and in isolation; several were derived from the same 0.50-second routine margin, which means they are coupled and have never been tested together. Report effects rather than repairing each failed fixture with a private exception, per R3.

**5. Presentation budget — `F693-07-presentation-budget`.** R4's work, after the sim contract is coherent, not before.

**6. Standalone re-sit — R5.** Jack's look and play gate, still open, as is the human gameplay gate.

**Deliberately deferred.** `F693-02-special-attack-contracts` is large, independent of the ordinary loop and better judged once the loop is playable; the accepted shared recovery rails do not approve any individual special, so nothing is being inferred while it waits. `F693-02-movement-response` sits behind the same validation. Neither blocks the sequence above.

If the accepted dimensions require kit work, that remains a separate art child under #188 as described in R4.

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

The [full #708 tracker history through 4e49b32](research/game-feel-708-tracker-history.md) is preserved verbatim before condensing its growing issue description. Use current approval states in this register and the candidate JSON.
