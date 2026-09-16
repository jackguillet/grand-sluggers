# Issue #708 tracker history through 4e49b32

Preserved verbatim from the issue description before condensing its current status on September 14, 2026. Historical pending states are superseded by the current decision register.

---

Parent: #693. Session kind: gameplay research/documentation. Stacked after #702 / draft #707. No gameplay tuning, Unity presentation, Blender, or standalone acceptance in this step.

Prepare R3's complete compact-field decision packet: unchanged control plus two coherent spatial candidates, body/basepath ratios, mound/fence/fielding-start geometry, runner/possession/release/travel/catch/read response budgets, flight/roll/wall constraints, accepted scoring gates, evidence confidence, and implementation order. Separate measured Mario evidence from authored trial values and analytical predictions; do not present an unimplemented candidate as a simulation result.

Deliver a version-controlled report and machine-readable candidate records with reproducible arithmetic and a geometry comparison illustration. Address compact-outfield doubles/triples/relays explicitly and preserve the accepted common pursuit profile, responsive movement, visible read, quick but visible release/travel, moderate ball assistance, and D7. Compare Wii and GameCube evidence before recommending.

Present Jack one decision at a time. First review concerns the lead spatial candidate for prototyping; other proposed numerical timing values remain pending until reviewed individually. Approval of a trial profile cannot pass the human gameplay/look gate. Subsequent implementation belongs to separate children, starting with shared geometry ownership and parity.


### R3 spatial decision packet — #708 / draft #709

Prepared [the report](https://github.com/jackguillet/grand-sluggers/blob/a900ddc/docs/research-game-feel-708.md), [same-scale comparison](https://github.com/jackguillet/grand-sluggers/blob/a900ddc/docs/research/game-feel-708-comparison.png), machine-readable candidate records and reproducible arithmetic at `a900ddc`. Jack accepted C80 (80-ft bases, 232/280/232-ft fences, unchanged characters) as the first spatial trial on September 14, 2026. C70 remains unselected. Neither compact candidate has been simulated.

F693-02-spatial-trial is accepted. F693-05-runner-clock is also accepted. F693-03-release-clock is also accepted. F693-03-travel-clock is also accepted. Long-throw/relay behavior is now accepted with chemistry explicitly included; good-pair chemistry treatment is now accepted; the long-range numerical profile is now accepted; negative chemistry is now accepted; relay ownership direction is now accepted; Snap Throw is now accepted; Laser is now accepted; the input-buffer window is now accepted; cancel/retarget controls are now accepted; ordinary pursuit speed is now accepted; outfield read is now accepted; base-infielder read is now accepted; pitcher read is now accepted; catcher read is now accepted; ordinary pursuit acceleration is now accepted; ordinary pursuit braking is now accepted; full pursuit reversal is now accepted; angled pursuit correction is now accepted; fine analog pursuit response is now accepted; neutral/manual pursuit boundaries are now accepted; stable pursuit calibration policy is now accepted; carrying movement response is accepted; clean ground-pickup readiness is the next pending decision. Remaining running-path details, special throw interactions, common pursuit/read/coverage, flight/roll/wall and presentation numbers remain separate pending decisions. #708's full-contract work and #693's human gate remain open. Analytical speed sensitivities expose the routine-defense/deep-chase coupling; no derived speed is an approved coefficient.

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

Subsequently accepted by Jack, F693-02-pursuit-calibration-policy: establish a valid released-stick center outside live play and keep the device profile fixed through the match. Recalibration is explicit while paused, adopted only on valid samples; failure retains the prior profile. Never silently learn steady live input as center or widen .20/.15. Calibration sample criteria, arming/held-at-SET and reconnect lifecycle remain separate decisions before implementation. Any new couch action needs its own presentation/book work.

Audit correction: StickPlay.Mag is Euclidean, so its .32/.45 center/recenter regions are radial, not L1 as prior prose stated. FieldAssist takeover and the stillness delta use axis sums. Earlier tracker wording is corrected; no accepted design number changes. Controls also samples a vector for center learning but child axes for movement, requiring a consistent processor/coordinate audit.

The GameCube booklet explicitly warns about off-center controls establishing incorrect startup neutral and describes a released-control reset. This supports the principle, not our match-long scope; Wii live calibration remains unverified. Validation: report/check, neutral acceptance and analog-origin linkage, 68 local links and whitespace pass. No runtime/input edits or human gate pass. #693/#708 remain open.


### Calibration accepted; pursuit readiness next — `8257bd4` / #709

Jack approved F693-02-pursuit-calibration-policy: valid released-stick calibration outside live play, fixed per device through the match, explicit paused recalibration only, with failed samples retaining the previous valid profile. No live center learning or automatic neutral widening. Spec/register/report/candidate provenance updated.

Subsequently accepted by Jack, F693-02-pursuit-arming: valid profile plus one neutral <=.15 observation at defensive-role entry, device recovery or successful recalibration; no extra dwell if already neutral. Retain readiness through the half, including pitches/contact, camera cuts, selection and ordinary pause. Held direction then moves at the accepted read deadline, never before it. Keep off-center recovery in the existing ready/recovery flow until valid neutral; preserve existing running-versus-paused resume policy. Other verb guards and calibration sample criteria remain separate.

Audit: AtBatDirector.BeginSet calls Controls.CatchPlay every pitch, and recovery/BeginMatch also call it. Pursuit readiness must be retained separately inside the existing input owner rather than globally deleting those guards. Both seats/device identity and sim velocity remain independent. Nintendo evidence supports released-control neutral calibration but does not establish this lifecycle.

Validation: report/check, calibration provenance, arming examples, 73 local links and whitespace pass. #693's entire previous 62,634-character body is preserved verbatim in docs/research/game-feel-693-tracker-history.md before its current-status condensation. No runtime/input/UI edits, simulation or human gate pass. Both issues remain open.


### Readiness accepted; calibration samples next — `273a499` / #709

Jack approved F693-02-pursuit-arming: valid calibration and neutral <=.15 at role/device/calibration boundaries, retained readiness through the defensive half and held direction honored at the read deadline. No extra dwell or global removal of other verb guards; recovery pause behavior remains explicit. Spec/register/report/candidate provenance updated.

Subsequently accepted by Jack, F693-02-pursuit-calibration-samples: .50-second valid released-stick window with mean radial center offset <=.10 and every sample <=.02 from that mean. Inclusive bounds, complete-window validation and monotonic non-live input time while baseball remains paused. This is for calibration only, not routine neutral checks. Learn center only; do not infer range endpoints from rest samples. Failed/interrupted sampling retains the old valid profile; a first-time invalid device stays outside live readiness. No silent threshold widening or trimmed excursions.

Synthetic windows verify good data, excessive offset/variation, insufficient elapsed duration and inclusive boundaries. They are not actual controller measurements or a required polling cadence. GameCube reset guidance supports released controls, not these authored values. Real pad noise/offset, processing, two-seat identity and couch retry behavior remain validation work.

Validation: report/check, arming approval, synthetic window arithmetic, 73 local links and whitespace pass. No runtime/input/UI edits, candidate simulation, hardware capture or human gate pass. #693/#708 remain open.

### Calibration samples accepted; field dash peak next — `17c908f` / #709

Jack approved F693-02-pursuit-calibration-samples: a .50-second valid released-stick calibration window, mean radial center offset <=.10 and every sample within .02 of that mean. Inclusive bounds, complete-window stability and monotonic non-live input time; failed sampling preserves the prior valid profile. Calibration only, with no extra routine arming delay. These authored bounds still require real controller validation.

Subsequently accepted by Jack, F693-02-field-dash-peak: 1.20x ordinary pursuit peak speed, taking Run-5 from 18 to 21.6 ft/s. Peak only; duration/availability, physical response and CPU/assist ownership follow separately. No read bypass, catch-reach or pickup/throw bonus. Nintendo manuals establish deliberate dash controls, not a measured 1.20 multiplier.

The inspected held-dash paths apply dash.chaseMul=1.35 while EastHeld, but spec section 8.1 says two seconds then fades. This discrepancy is explicitly open for the next schedule decision; neither timing behavior is accepted here. A constant-speed 60-foot sensitivity is 3.333 seconds ordinary versus 2.778 at peak, excluding reads, acceleration, turns and availability; it does not authorize a dash lasting that entire route.

Validation: report/check, approval provenance, synthetic sample and dash arithmetic, 78 local report links and whitespace pass. [Current report](https://github.com/jackguillet/grand-sluggers/blob/17c908f/docs/research-game-feel-708.md). No runtime/input/UI edits, candidate simulation or human gate pass. #693/#708 remain open.

### Field dash peak accepted; burst duration next — `3c7537d` / #709

Jack approved F693-02-field-dash-peak: 1.20x ordinary fielding pursuit peak speed (Run-5: 18 to 21.6 ft/s), preserving character ratios across positions/hit classes. Peak only; no catch-reach, pickup/throw bonus or contact-read bypass. Duration/recovery, physical response and CPU/assist ownership remain separate.

Historical proposal, subsequently approved then reopened by Jack’s carrier-ability correction, F693-02-field-dash-duration: a maximum two-second continuous dash window, including build-up, then request ordinary running even if the button remains held. No below-ordinary exhaustion penalty is selected. Recovery/re-arming and exact entry/exit rates follow separately.

Use active sim time, starting with eligible available pursuit and movement intent; SET/read cannot consume or precharge it. Pause/recovery freezes the clock; after activation, stops/turns do not freeze the deadline. Early release ends the requested boost without implying instant refill. Keep the clock on the actual fielder; selection cannot reset it. At the deadline request ordinary movement without snapping physical velocity. Spec 8.1 has a two-second fade but inspected held-boost paths lack it; duration approval alone will not close that runtime/response discrepancy.

At Run-5, two seconds at constant ordinary/peak speed covers 36/43.2 feet, a 7.2-foot difference within the window. Entry/turns and residual exit travel are excluded; this is not a whole-play advantage or simulated catch. Alternatives 1.5/2.5 seconds give ideal 5.4/9-foot differences. Manuals establish deliberate dash controls, not two-second timing or recharge; this is an authored Harbor trial.

Validation: report/check, dash-peak approval provenance, duration arithmetic, 82 local report links and whitespace pass. No runtime/input/UI edits, candidate simulation, standalone build or human gate pass. [Current report](https://github.com/jackguillet/grand-sluggers/blob/3c7537d/docs/research-game-feel-708.md). #693/#708 remain open.

### Dash scope reopened for passive carrier ability — `49a87bb` / #709

Jack approved the two-second burst, then redirected dash toward automatic speed only for characters with the Dash ability while carrying the ball. The prior universal sprint peak/duration scopes are reopened, with their approval history preserved. Suspend sprint recovery decisions; other accepted anchors remain intact.

Subsequently accepted by Jack, F693-02-ball-dash-carrier: replacing universal activated sprint with passive Ball Dash, only for an ability holder with secure live-ball possession. No activation input, burst timer or cooldown. Recommend 1.20x ordinary carry speed as the first trial; prior pursuit-speed approval does not automatically approve this different scope. Ordinary carrying base/response and roster allocation remain separate.

Community Ball Dash descriptions distinguish possession-only ability behavior in both Mario games from the ordinary dash controls cited earlier; exact magnitude/transition remains unmeasured. No current character among the 25 inspected records assigns Dash/Ball Dash. Add no holder or second ability silently. Before possession, retain ordinary pursuit. Same eligibility for both seats and CPU, with CPU carry-versus-throw forecasts matching actual motion. Check tags, rundowns, unassisted forces and relays so carrying does not routinely replace throwing.

Validation: report/check, 86 local report links and whitespace pass. Prior arithmetic remains historical; no carrier candidate simulation, runtime/roster/input/UI change or human gate pass. [Current proposal](https://github.com/jackguillet/grand-sluggers/blob/49a87bb/docs/research-game-feel-708.md#current-dash-revision--passive-ball-carrier-ability). Both issues remain open.

### Passive Ball Dash accepted; ordinary carrying speed next — `cdb4058` / #709

Jack approved F693-02-ball-dash-carrier: automatic 1.20x ordinary carrying speed only for an ability holder securely possessing the live ball, for human and CPU fielders alike. No activation input, burst timer, stamina or cooldown. This supersedes universal fielding sprint and its earlier two-second approval; preserve their history. Ordinary pursuit before possession and separate baserunning anchors remain intact.

Subsequently accepted by Jack, F693-02-ordinary-carry-speed: ordinary carrying top speed equal to each character’s ordinary pursuit top speed, with no generic possession penalty/bonus. Run-5 would carry at 18 ft/s, or 21.6 with the accepted Ball Dash ability. Top speed only; transitions, recovery, coverage and ability holders remain separately reviewed.

For 40 feet at steady speed, ordinary/Ball Dash carry is 2.222/1.852 seconds versus .30 release + .45 flight for an ordinary neutral Field-5 throw. These omit pickup, thinking, acceleration/turn/brake, receiver coverage and catch/tag geometry; they do not decide an out. The manual carry path can retain an air Preview while CPU movement/forecast uses the base overload; implementation must use shared explicit carry state and verify parity, not patch one case.

Community Ball Dash descriptions support the ability identity, not a measured 1.0 ordinary carry/pursuit ratio or 20% magnitude. All 25 character records still have no Dash holder; allocation remains a separate decision.

Validation: report/check, Ball Dash approval provenance, carrying arithmetic, 88 local report links and whitespace pass. No runtime/roster/input/UI edits, candidate simulation, standalone build or human gate pass. [Current report](https://github.com/jackguillet/grand-sluggers/blob/cdb4058/docs/research-game-feel-708.md). Both issues remain open.

### Ordinary carrying speed accepted; shared response next — `bc62635` / #709

Jack approved F693-02-ordinary-carry-speed: ordinary carrying top speed equals each character’s ordinary pursuit speed, without a generic possession penalty/bonus. Run-5 is 18 ft/s, or 21.6 with the separately accepted passive Ball Dash. Same carrying context for both seats, CPU movement and forecasts. Response, recovery, coverage and roster remain separate.

Subsequently accepted by Jack, F693-02-carry-movement-response: the accepted pursuit acceleration/braking/continuous-turn law while carrying, with rates based on unboosted character speed even during Ball Dash. The ability raises the requested cap only. Ordinary start/stop/reverse remain .20/.10/.30 seconds; at Ball Dash peak they become .24/.12/.36. Preserve actual velocity through eligible possession and intent changes.

At Run-5 use acceleration 90 ft/s² and braking 180 ft/s²: Ball Dash full stop covers 1.296 feet. In otherwise unrestricted same-direction movement, ordinary-to-Dash takes .04 seconds and Dash-to-ordinary .02. These are consequences of the shared physical law, not added timers or permission to move during a throw. Pickup/recoil/throw eligibility and recovery remain unresolved; no ramp precharge while movement is barred. Shared prediction and actual stepping must agree across seats/CPU.

Alternative: boost response rates by 20% too, retaining .20/.10/.30 at the higher speed; that would add a response-rate benefit beyond the speed bonus. Recommend unchanged base rates. These values extend authored trial rules, not measured Mario handling.

Validation: report/check, carrying-speed approval provenance, ordinary/boosted response arithmetic, 90 local report links and whitespace pass. No runtime/input/UI/roster changes, candidate simulation, standalone build or human gate pass. [Current report](https://github.com/jackguillet/grand-sluggers/blob/bc62635/docs/research-game-feel-708.md). Both issues remain open.

### Carrying response accepted; clean ground-pickup readiness next — `4e49b32` / #709

Jack approved F693-02-carry-movement-response: shared ordinary acceleration/braking/continuous-turn law for eligible carrying, with rates based on unboosted character speed during Ball Dash. Ordinary start/stop/reverse remain .20/.10/.30 seconds; boosted equivalents are .24/.12/.36. Preserve actual velocity and share prediction/stepping. Recovery/eligibility, coverage and roster remain separate.

Next pending: F693-02-clean-ground-pickup-readiness: recommend zero added generic pause after a routine clean ground pickup becomes securely possessed, with no active exceptional recovery. Continue eligible carrying or begin the ordinary .30-second release when a valid deliberate/buffered command is ready. This is not zero-time acquisition or an automatic throw.

Keep visible scoop/secure-possession agreement, real fair/foul and geometry, and queue cancellation/expiry. Bobble/loss/invalidation wins at a shared event boundary before possession-based ability or command eligibility. A retained ball can still have recoil: current TakeBattedBall/ArmRecoil and RecoilT distinguish this from routine readiness. Dives, bobbles, hard-hit recoil, air catches/receptions and movement during a throw remain separate. No recovery thresholds change.

Synthetic examples: possession at 1.00 with command .80 starts release at 1.00 and separates at 1.30; command .70 expires; a fresh 1.05 command separates at 1.35. Ground/loose pickups do not qualify for Snap Throw. These are arithmetic examples, not candidate traces or Mario measurements.

Validation: report/check, carrying-response approval provenance, synthetic pickup/buffer/release arithmetic, 93 local report links and whitespace pass. No runtime/input/UI/roster edits, candidate simulation, standalone build or human gate pass. [Current report](https://github.com/jackguillet/grand-sluggers/blob/4e49b32/docs/research-game-feel-708.md). Both issues remain open.

### Arcade fielding consolidated; catch-reach envelope next — `e2a2e4b` / #709

F693-02-arcade-fielding-validation is research complete. The report now states the simplified contract end to end across three representative plays: a routine grounder to short (0.25-second read, pursuit toward 18 ft/s, in-range acquisition, no handling roll, 0.30-second release, 0.90-second 80-foot flight); an awkward in-between hop (the one accepted difficulty source, `p = .10*D*(1-.80*H)` capped at 10%, one seeded draw, local bobble numbers, ±30° contact-led direction, 0.40-second shared stun, reliable same-error recovery); and a hard infield ball that escapes (50–80% shared horizontal/vertical retention, shared ordinary ground response, ±15° direction, no inherited bobble caps and no assigned destination). Retained anchors, superseded microphysics and open mappings are separated in one table.

Reference comparison: both recorded plays — the Wii shortstop grounder around 00:58 and the GameCube force-and-return around 03:10 — are clean fielding. Neither reference packet contains a bobble, a deflection or a gap ball, so the error behaviour has no matched Wii or GameCube observation in either direction. A September 15, 2026 attempt reopened the Wii clip page and was abandoned in pre-roll advertising before any play was inspected; nothing was measured or inferred from it.

New reach accounting. Infield starts scale with the basepath, so infield gaps shrink 11.1%, but outfield starts hold their fraction of the fence radius and the fence shrank 30% at center, so the left–center alley falls from 122.98 to 86.08 feet while reach and pursuit do not change. Spending the accepted 0.40/0.25-second reads, 0.20-second build-up and 18 ft/s on straight-line lateral interception, the hang at which two neighbours cover the whole line between them is: C0 alley 3.19 s and C80 alley 2.17 s at today's 13-foot radius, 2.25 s scaled, 2.45 s at 8 feet, 2.56 s at 6 feet, 2.89 s at zero; C0 third-to-short 1.25 s versus C80 1.07 s at 13 feet and 1.46 s at 6 feet. Reach is a real but partial lever — deleting it entirely recovers about 0.7 of the lost second. The C80 arc is also nearer, so an alley ball hangs less; a no-drag same-launch-angle ball scales hang with the square root of carry, putting the control-equivalent near 2.67 s rather than 3.19. This is straight-line arithmetic, a ceiling on coverage rather than a hit rate, and is not a simulation.

Next pending: F693-02-catch-reach-envelope. Keep today's absolute 13-foot stand-up radius, scale it with the basepath to 11.56 feet, or re-author a smaller envelope the visible glove can actually meet, with 6 feet as the candidate trial. The third is the only option consistent with the accepted glove-meets-the-ball contract, since 13 feet is about two and a half Rio head-heights; it also turns marginal outs into hits and promotes the 8-foot dive and jump additions into the dominant reach. Restoring the alley by slowing outfielders is unavailable under the accepted one-profile pursuit decision, and matching the control's closure by spreading the outfield would need the control's absolute 123-foot gap. Whichever magnitude wins, the live `10 + 0.6 × Field` formula contradicts F693-02-character-catch-range and cannot carry over. No value is selected. No Wii or GameCube catch reach has been measured; video supplies no world scale.

Validation: report/check and whitespace pass; the checker now derives and verifies per-profile closure arithmetic, monotonicity in reach and the quoted headline seconds. No runtime, asset, roster, input or UI edits, candidate simulation, standalone build or human gate pass. [Current report](https://github.com/jackguillet/grand-sluggers/blob/e2a2e4b/docs/research-game-feel-708.md). Both issues remain open.

### Six-foot catch reach accepted; dive/jump/scoop reach next — `677a514` / #709

Jack accepted F693-02-catch-reach-envelope on September 15, 2026. Offered the three options from the reach accounting, he chose to re-author the ordinary stand-up catch reach to roughly **6 feet** for a middle character on C80, over keeping today's absolute 13 feet and over scaling it with the basepath to 11.56. Reach becomes about what the visible glove covers from a planted stance — 7.5% of a basepath instead of 16.25% — which is the only option consistent with the accepted glove-meets-the-ball contract, since 13 feet is about two and a half Rio head-heights and no authored glove reaches its rim.

On the derived closure table that reopens the third-to-short hole from 1.07 to 1.46 seconds, short-to-second to 2.09, and the left-center alley from 2.17 to 2.56. Routine plays are unaffected, because a routine fielder runs to the ball rather than reaching for it; what moves is the margins, and marginal plays that would be outs at 13 feet become hits. This is straight-line lateral interception arithmetic, a ceiling on coverage rather than a hit rate, and is not a simulation.

Accepted scope: the ordinary stand-up magnitude only, as a trial anchor rather than a shipping default or a measured Mario reach. It does not select dive, jump, scoop-pad or ability reach, still +8 / +8 / +4 feet in the current runtime and therefore now the dominant reach in the stack. It does not select per-character variation, beyond the already accepted rule that its source must be an explicit property and not displayed Fielding, so `10 + 0.6 × Field` cannot survive this decision. Full-race validation must still report what the smaller envelope does to actual doubles, triples and infield singles.

Next pending: F693-02-dive-jump-scoop-reach. With the stand-up envelope at 6 feet, the dive, jump and scoop additions are each larger than the radius they extend, which inverts the intended relationship and makes them the governing reach for every marginal play. Research the envelope and matching authored poses before putting a number to Jack. The explicit per-character reach source, the flight budget and outfield starting spread remain open behind it.

Validation: report/check and whitespace pass; the checker now verifies the accepted reach is one of the tabulated options and that the quoted closure consequences match the derived lead-profile table. No runtime, rules-file, asset, roster, input or UI edits, candidate simulation, standalone build or human gate pass. [Current report](https://github.com/jackguillet/grand-sluggers/blob/677a514/docs/research-game-feel-708.md). Both issues remain open.

### Dive audited, then made earned-only with a recovery delay — `b7a8353` / #709

Research first. Reading the runtime rather than only the rules file changed the shape of F693-02-dive-jump-scoop-reach: the dive was not a player action for most of the defence. `FlyCatch.AutoDive` fires behind the dead-stick branch in `LivePlaySystem.Field.cs` and unconditionally on the CPU branch, performing the lunge itself, so on a nine-player defence the seat steers one fielder while the other eight dive automatically, as does the whole opposing defence. The dirt scoop pad is likewise automatic — a grounder inside `radius + windowPadFt` is taken with no button — while the jump is armed only by the West press and the 3.5-foot loose-ball scoop is independent of catch radius.

That made 6 feet of accepted stand-up radius plus an automatic 8-foot dive into **14 feet of passive coverage, more than the 13 feet the reach decision had just removed**. On the derived closure table the left-center alley would have closed at 2.11 seconds rather than 2.17, and the third-to-short hole at 1.01 rather than 1.07, making F693-02-catch-reach-envelope cosmetic for every fielder the seat is not personally steering. Each addition was also larger than the radius it extended.

Jack accepted the strictest option on September 15, 2026 and added a cost to it: "Dive ever only on a press. But, there should be a 'delay' if you dive (before throwing or moving) so that dives are used as a last resort to reach a ball, not spammed on every play."

The assistance dive is removed. `AutoDive` must stop firing behind the dead-stick branch, so no fielder reaches the rim without someone choosing to. Passive coverage becomes exactly the accepted 6 feet in the air and 10 feet on the dirt, never 14, and an earned dive still reaches 14. Separately, a dive now carries a recovery delay before the diver can throw or move. That is **new behaviour, not a retune**: `DiveT` in the current runtime is only an arm window that widens the catch window and gates nothing, so there is no existing dive cost to adjust. The delay is a cost of the dive itself and applies whether or not the ball was caught; a dive that secures the ball still records the out, and the delay is paid after the catch rather than being a reason to drop it. Routine clean catches and pickups keep their accepted zero added pause, and the dive is an explicit exception to it, consistent with the earlier readiness decisions that all excluded dives by name.

Accepted scope is dive ownership and the existence of a commitment cost. No delay duration, rules-file value or runtime change is selected. Jump, loose-ball scoop and the 4-foot dirt scoop pad are unchanged, the last still deferred.

Next pending: F693-02-dive-recovery-cost — the delay duration, whether a caught dive costs the same as a missed one, character variation and composition with the accepted .40-second handling stun. Jack's earlier remark that Fielding could indicate "a unique dive ability" makes dive recovery its natural home, recorded as a direction to discuss rather than a selected mechanism. Also pending: F693-02-cpu-dive-intent — with the assistance dive gone, whether a CPU or unselected fielder gets deliberate dive intent paying the same commitment cost, or genuinely never dives. Jack accepted that cost with it stated, but the CPU side needs its own contract rather than being settled by omission.

Validation: report/check and whitespace pass; the checker verifies the effective automatic reach against the accepted stand-up radius, the quoted closure seconds against the derived lead-profile table, that no option lets an automatic dive out-reach an earned one, and that the accepted direction leaves passive reach below the radius it replaced. No runtime, rules-file, asset, roster, input or UI edits, candidate simulation, standalone build or human gate pass. [Current report](https://github.com/jackguillet/grand-sluggers/blob/b7a8353/docs/research-game-feel-708.md). Both issues remain open.

### Correction on the one-glove model, and who dives now — `e42feda`+ / #709

**Correction.** The previous entry and the report section it archived said that on a nine-player defence the seat steers one fielder while the other eight dive automatically. That is wrong about how the sim is built, and it is corrected here rather than by editing the earlier entry. There is exactly **one active glove**, `GlovePos`, driven by the seat's stick or by `ChaseGlove` assistance when the stick is dead, and handed to another position by `TryHandoffOutfield` / `TryHandoffLoose` as the play develops; the other fielders are positioned, not chasing. The automatic dive was therefore two real cases, not eight: a **human glove whose stick is neutral at that instant**, and the **entire CPU defence, unconditionally**. The 14-foot finding is unaffected and if anything stronger, since the CPU case was unconditional. The coverage arithmetic is unaffected because the two-neighbour model computes the nearest-fielder case, which is exactly what the handoff produces. Both accepted decisions stand.

Research for the two follow-ups. The CPU defence drives the same single glove a seat would and reaches the rim **only** through `FlyCatch.AutoDive`; there is no other path in the code. Removing it leaves the CPU with no dive at all rather than a weaker one. On the accepted anchors the dive is worth a flat **16-foot band** of every gap — eight feet either side of the hardest point — until the gap closes on pursuit alone: the left-center alley at a 1.6-second hang is 18.5 feet open with an earned dive and 34.5 without, the short-second hole at 1.6 seconds is 1.7 against 17.7, and the third-short hole at 1.0 second is 0.5 against 16.5. That sets how hard the computer is to hit against and whether the opposing defence ever produces a highlight catch.

Noted for Jack without deciding it: "dive ever only on a press" is about the dive being a deliberate act with a cost, not about requiring a physical controller. A CPU that chooses to dive, can miss, and pays the same delay honours that; a CPU that dives for free does not; and a CPU that dives only on balls it converts is the old automatic dive wearing a delay, handing back the passive rim coverage the last two decisions removed.

F693-02-dive-recovery-cost is recorded as research in progress rather than a queued question. Its neighbours in the packet are the accepted .40-second handling stun, ordinary retained-ball recoil at .20/.16/.11 seconds, the .60-second normal-jump arc and the existing .50-second dive arm window; a last-resort commitment has to cost more than a recoil and land nearer the stun. Its three shape questions — caught versus missed cost, character variation as the natural home for Jack's "unique dive ability" remark, and composition with the handling stun — are to be consolidated into one proposed trial, sequenced after the CPU answer because a CPU that dives changes what the delay has to be worth.

Validation: report/check and whitespace pass; the checker now verifies the contested band against the accepted earned dive and every quoted open-window figure against the derived lead-profile geometry, and holds the correction on the record. No runtime, rules-file, asset, roster, input or UI edits, candidate simulation, standalone build or human gate pass. Both issues remain open.

### Deliberate CPU dive accepted at the same cost — `f39dde9` / #709

Jack accepted F693-02-cpu-dive-intent on September 15, 2026, choosing deliberate CPU dive over never diving and over diving only on balls the CPU converts.

The CPU-driven glove gets deliberate dive intent: it chooses to dive, **it can miss**, and it pays the same recovery delay a seat pays. Same rules, different agency. Its dive reaches the same 14 feet a seat's does and no further, and it may not reach the rim through a wider window or through a lookahead the seat has no equivalent of. The assistance dive stays removed, so a human glove on a neutral stick still does not dive.

The declined option is part of the decision and is recorded as such. A CPU that dives only on balls it will convert is the automatic dive wearing a delay: it would hand back the passive rim coverage F693-02-catch-reach-envelope and F693-02-dive-jump-scoop-reach removed, and reduce the commitment cost to decoration. **A CPU dive that fails and leaves the ball live is therefore required behaviour, not a defect**, and no branch may convert a CPU dive by construction.

Accepted scope is that the CPU may dive deliberately under the same cost. No intent policy is selected. Queued as F693-02-cpu-dive-intent-policy: when the CPU spends a dive, the discipline not to spend it on a ball it could walk to, how far ahead it may look when deciding, and whether willingness varies with difficulty or needs a budget to avoid looking robotic. Lookahead is the line between a deliberate dive and a psychic one and needs an explicit budget rather than whatever the prediction code happens to expose. Any randomness in CPU dive intent runs on the project seeded rail and replays identically. A trace with every CPU dive removed must reopen the 16-foot band, confirming the intent is doing the work rather than a hidden reach.

F693-02-dive-recovery-cost remains research in progress, sequenced after this because a CPU that dives changes what the delay has to be worth.

Validation: report/check and whitespace pass; the checker holds that a CPU dive may not out-reach a seat dive and that the assistance dive stays removed. No runtime, rules-file, asset, roster, input or UI edits, candidate simulation, standalone build or human gate pass. [Current report](https://github.com/jackguillet/grand-sluggers/blob/f39dde9/docs/research-game-feel-708.md). Both issues remain open.

### Dive recovery accepted at .60 seconds on a narrow curve — `0962cfe` / #709

The duration was derived rather than chosen. The packet already carried the anchor: the illustrative routine race in `throwReleaseProposal.arithmeticOnlyExample` reaches a covered reception at 2.95 seconds against a 3.45-second runner, and a dive delay is spent between possession and release, so it comes out of that .50-second margin. That turns "last resort, not spammed" into arithmetic — a dive taken on a ball the fielder could have fielded standing should not still produce the out. At .20 seconds an unnecessary dive is a comfortable out, at .40 it is an out barely, at .50 a dead heat and at .60 a hit. A necessary dive is unaffected by the reasoning, since a ball at the rim had no standing play to lose.

Jack accepted F693-02-dive-recovery-cost on September 15, 2026, choosing character variation over a shared recovery with a named dive ability and over a shared recovery alone. **Dive recovery is .60 seconds at the lowest defensive quality, narrowing 2.5% per quality point to .465 seconds at the highest.** The curve is deliberately narrower than the packet house recoil curve: at the recoil curve's 5% per point the best defender reaches .33 seconds and an unnecessary dive is a comfortable out again, handing that character back the spam the decision removed. At 2.5% even the best defender only reaches a dead heat. Quality is supplied by an explicit defensive trait under the accepted summary architecture, never by the displayed Fielding number, exactly as the handling error chance is sourced. The dive is therefore where the defensive rating finally earns its keep in ordinary play, reach, glove positioning and tracking speed all having been ruled out.

Two composition rules were proposed rather than asked and are accepted with it. A caught dive and a missed dive cost the same, because the delay is a cost of the dive rather than a penalty for failing, and charging the miss extra would double-punish a failed play and quietly reintroduce a failure lottery. When a dive reaches the ball and then fails its handling roll the result is **the longer** of the dive delay and the .40-second stun rather than their sum; adding would give 1.00 second, longer than anything else in the packet by a wide margin. **This departs from the additive precedent in F693-02-special-recovery-composition** and is recorded explicitly rather than buried so it can be revisited. The CPU pays the same delay on the same curve, per F693-02-cpu-dive-intent.

Accepted scope is the trial duration, its curve and the two composition rules. No trait list or trait-to-quality mapping is selected. The explicit defensive-trait migration is now consumed by **both** the handling error chance and this curve and blocks implementation; it is queued as F693-02-defensive-trait-mapping.

Validation: report/check and whitespace pass; the checker rebuilds the duration table from the accepted race example, rebuilds every quality row from the base and rate, holds the rows positive and strictly decreasing, keeps the accepted curve narrower than the recoil curve, and holds that the best defender cannot reach a comfortable out on an unnecessary dive. No runtime, rules-file, asset, roster, input or UI edits, candidate simulation, standalone build or human gate pass. [Current report](https://github.com/jackguillet/grand-sluggers/blob/0962cfe/docs/research-game-feel-708.md). Both issues remain open.

### Arm splits out of the Fielding rating — `c376249` / #709

The inventory first, since it was required before implementation. `Stats.Field` drives four distinct jobs. **Arm:** throw speed `0.85 + 0.03 x Field`, comfortable range `160 ft ± 5 ft per point`, and throw accuracy `sigma = (11 - Field) x 0.35 ft` — three consumers with two accepted anchors on them. **Hands:** retained-ball recoil duration, the handling error chance quality term and dive recovery — three consumers with three accepted decisions on them. **Reach:** `10 + 0.6 x Field`, already superseded by F693-02-catch-reach-envelope and F693-02-character-catch-range. And three **CPU-only** reaction timings — `InPlay.ThrowReactionSec`, `StealThrow.CpuReleaseSec` and `ClosePlay.CpuReactionSec` — every one of them multiplied by `cpu.active.reactionMul`, which makes them difficulty scaling wearing a character stat. `Teams.Tools` and the HUD rows are roster building and display, not gameplay.

The finding: **the displayed Fielding number was simultaneously the arm rating and the hands rating**, so a cannon-armed catcher with stone hands could not exist, and neither could a slick middle infielder with a noodle arm. The accepted throw anchors sit squarely on the arm half, so whatever replaced Field had to keep feeding `armSpeedPerFieldPoint 0.03` and `rangeFeetPerFieldPoint 5` or F693-03-long-throw-numbers would break.

Jack accepted F693-02-defensive-trait-mapping on September 15, 2026, splitting **Arm** out over keeping one defensive rating and over splitting Arm, Hands and Dive three ways. There are two defensive ratings. Arm governs throw speed, comfortable range and throw accuracy. Fielding becomes the hands rating and finally means one coherent thing: recoil duration at 5% per point, the handling error chance quality term, and dive recovery at .60 seconds narrowing 2.5% per point. **Reach belongs to neither.**

Every character's Arm is **seeded at their current Field value**, so every accepted throw anchor evaluates identically the day the split lands. The split is numerically free at that moment and only permits deliberate divergence afterwards. The same trick does not work for reach, which is why reach had to be re-authored rather than migrated.

Four knock-on effects are recorded rather than assumed. The content schema gains an Arm rating, validated 1..10 like the others, and existing character files need it or a seeded default. **`Teams.Tools` sums Pitch + Bat + Field + Run for roster building, so whether Arm joins that sum is a deliberate choice rather than an automatic one** — it changes team-building balance. The HUD and CLI rows gain an A, and the displayed Fielding number now means hands only, a readability change for anyone used to reading it as general defence. The three CPU-only reaction timings still read Field; they are reaction rather than throwing, so the default is that they follow Fielding, but whether they should be character-driven at all stays open through migration.

Accepted scope is the architecture and which consumer belongs to which rating. No roster numbers, schema edit, migration code, `Teams.Tools` decision, HUD change or runtime change is selected. Queued as F693-02-arm-rating-migration.

Validation: report/check and whitespace pass; the checker holds every inventory row to a named group, holds that exactly two consumers are blocked waiting on a trait, that the arm rows still carry the accepted throw coefficients, that every CPU-only row shows the difficulty multiplier, and that each accepted rating claims exactly its inventory group. No runtime, rules-file, asset, roster, input or UI edits, candidate simulation, standalone build or human gate pass. [Current report](https://github.com/jackguillet/grand-sluggers/blob/c376249/docs/research-game-feel-708.md). Both issues remain open.

### Flight budget measured, then the ball's drag raised — `6a0e537` / #709

Research first, from the game's own contact and flight model rather than from reasoning about it. Exit speed is `(61 + 3.7 x Power)` mph scaled by quality — slap .75 / .95 / 1.00, charge .95 / 1.12 / 1.25 — and launch is `16 + (Power - 5) + 2.5 if charged + 6 per foot of pitch height - 12 x stick`, clamped 3 to 52 with plus or minus 7 degrees of noise. Flight is a 120 Hz Euler integration at gravity 32.174 and drag .0019. Carry figures were cross-checked against `BallFlight.CarryFeet` on seven probes and agreed within 0.7 foot; the counterfactuals vary only the drag constant in that same integrator.

**C80 as specified was a home-run derby.** Measured by the ball's height crossing the fence line rather than its carry, since the 12-foot wall catches wall-scrapers: a Power-3 bat, the weakest on the roster, crossed centre field at 12.2 feet on a charged perfect swing, and a Power-10 bat crossed the 232-foot pole at 17.1 feet on ordinary uncharged line-drive contact with no lift at all. On the control field those same swings carry 296 and 269 feet against a 330-foot pole and a 400-foot centre, both comfortably in play. The compact park did not change the ball, and the ball was what had become wrong.

**The doubles engine is the liner, not the fly,** and it survives the shrink. `linerTimeScale 1.0` keeps anything under 22 degrees at 74 mph or more on the real clock, hanging 1.8 to 3.3 seconds against the accepted 2.56-second alley closure, while `timeScale 1.65` leaves flies hanging 4.3 to 8.5 seconds and always caught if they stay in the park. So gap hits come from line drives through a split that already exists, and the risk is inverted from how it looked: liners are not in danger of being caught, they are in danger of clearing the fence instead of falling in the gap.

**The ball is global, not Harbor's.** All six parks are still at control scale, poles 312 to 338 feet and centres 378 to 408, and drag, gravity and the exit table are shared.

Jack accepted F693-04-flight-budget on September 15, 2026, raising **drag from .0019 to a .0040 trial** over cutting exit velocity by a fifth and over raising the wall. The exit table and the 12-foot wall are untouched. Ordinary contact now stays in the park — a nice slap with lift carries 180 feet and a Power-10 nice slap becomes a 206-foot liner rather than a 269-foot home run. A charged squared-up mid-power swing clears the pole at 248 feet but not centre, the biggest bat clears centre at 304, and star swings clear centre reliably at 282 to 304 feet at Power 5 and 339 to 360 at Power 10 — the role a star swing should have, and a case the original probe set did not cover. Gap doubles go up rather than down, because liners land in front of the fence while still hanging under the closure.

It is the surgical lever: drag acts on carry through the air and barely touches a ground ball, so exit speeds, grounder arrival times, the infield races, the awkward-hop difficulty source and every accepted fielding anchor are unaffected. Cutting exit velocity would have reopened all of them.

**Required coupling, recorded rather than left to be discovered.** Drag is global and the other five parks are still at control scale. At .0040 the best ordinary swing carries 304 feet against their 330-foot poles, so those parks would have **no home runs at all**. The drag change and the park migration must land together; shipping drag first would make the game homerless everywhere except a migrated Harbor. Queued as F693-04-park-migration.

Accepted scope is the lever and its trial value, as a trial anchor rather than a shipping default or a measured Mario carry. The exact value within roughly .0035 to .0045 stays open, and the heavier-ball look it produces has to be judged on screen rather than by arithmetic.

Validation: report/check and whitespace pass; the checker holds that every park is still at control scale, that every measured lever shortens every probe, that the accepted lever keeps ordinary contact in the park while still letting a charged squared-up swing leave it, that the exit table is untouched, and that every park but the lead profile's still needs migrating. No runtime, rules-file, asset, roster, input or UI edits, candidate simulation, standalone build or human gate pass. [Current report](https://github.com/jackguillet/grand-sluggers/blob/6a0e537/docs/research-game-feel-708.md). Both issues remain open.
