# Spec provenance archive

> The issue numbers, PR numbers, slice tags, dates and acceptance notes that `docs/spec/` carried before its rules were rewritten to say only what the game does.
> Each entry is the original line, under the file and heading it came from. This is history, not a rule; the rule is the line in `docs/spec/`.

## docs/spec/00-decisions.md

### 0. Principles

2. **Geometry decides. Rolls only add noise, never outcomes.** A stat changes speed, range, window, or accuracy. It does not roll "out or single" (the roll at `Fielding.cs:146` was deleted by P4 #597 ✅). Randomness is allowed on *inputs* (a CPU's timing error, a bad-chemistry throw's lateral error), never on *results*. A park hazard's surprise is an input of this kind (D21, FD-08-R1):

   a seeded draw may decide when a hazard fires, which exit it picks or where it sends the ball; it never awards an out, a hit, a drop or a catch. ✅ F4-b (#896): the park's `fielding.drops.frozen` roll is retired — a glove a status volume slowed is decided by the glove and the ball (`SF-22`); the heart swing's use of the table is a special's and is unchanged.

3. **One clock, one body.** A runner has one position and one speed. A throw has one duration used both to fly the ball and to judge the bag. A fielder has one speed whether a human or CPU holds the stick. ✅ P3 #595 (one `bagSec` for every runner), P4 #597 (one `InPlay.ThrowSec`; the catcher's gun and Unity's throw flight read it), P4 (one glove speed for both seats).

9. **Every mechanic is teachable by playing it.** Requirement added September 19, 2026: a Tutorials section must provide repeatable scenarios for individual actions and combined plays, and each new or changed mechanic must carry tutorial coverage. [tutorials.md](../tutorials.md) defines the contract and planned migration from existing Practice. Controlled setup/CPU behavior may create an opportunity; the human performs the skill and the ordinary sim decides the result.

### 0.1 Decisions where Sluggers and the current game differ

| D4 | Contact quality | **Cursor decides quality, timing decides direction.** Sour / nice / perfect by where the ball meets the cursor; early pulls, late pushes; outside the window is a whiff. | Booklet plus the Superstar datamine (five bat zones, 9-frame slap / 7-frame charge window[^d4-window]). ✅ P1 #586 |

| D5 | Close plays | Button prompt at **third and home only**, only when the throw and the runner arrive together. | Sluggers booklet wording. Superstar Baseball used a body-check roll instead; we take the prompt. ✅ P5 #598 |

| D7 | Pitch pace | **Wait.** Shipped `AirSeconds` stays ~0.69–1.28 s (meat ~0.85–1.0; changeup ~0.25 s longer, #668). The Superstar-derived reference is ~0.6–0.75 s. Do not retune `arcadeScale` / `airMinSec` / `airMaxSec` from the 2026-09-13 sitting — that game predates the changeup hang/dump and `leadSec` 0.18. Jack calls it in the #346 sitting on current main (#209): keep / speed up / middle. | Not measured in Sluggers. ⏳ open: Jack 2026-09-14 (option 4) |

| D8 | Innings | 3 / 6 / 9 (not 1 / 3 / 5 / 7 / 9). Extra innings up to +3. Mercy 10 at the end of an inning. | Party default; the reference cap and mercy are copied. ✅ P3 #596 |

| D11 | Flat bag-cover speed | Historical shipped rule; **superseded as the #693 target by F693-02-coverage-budget**: use the shared character movement profile, starting at contact without a read. | Superstar datamine: constant cover speed starting 14 frames after the hit. ✅ P0 #571 (`running.json`) |

| D12 | Box position between pitches | **Recenters after every pitch.** Down still recenters early in SET. | Jack's call (sitting 2026-09-12, #607). The reference persists the box with a reset button; overridden for readability. ✅ #620 |

| D13 | When you press to swing | **The window is centered on the ball reaching the plate** (minus a small authored lead), and the swing take is time-warped so the bat meets the ball inside the window. Outside the window the take plays at its natural length and misses. | Superstar datamine: "the timing of the contact is constant, the animation is lengthened / shortened to make contact." Replaces the fixed press + 0.30 s plane (#612). ✅ #617 |

| D14 | In-play camera | **One cut on contact** to the in-play view that follows the ball; **the bag camera only for a close play** at third or home (and, optionally, once on a steal throw). No swoop to the bag on ordinary throws. ✅ #610: `PlayCamera.LiveBeat` has no throw beat; `diamond` / `diamond-line` / `diamond-fly` / `tag` / `throw` are blend 0 (a cut); `PlayCamera.CameraHold` keeps a target `cameraHoldSeconds` (0.25). ✅ #665: a liner is `diamond-line`, not the fly pull-back. | Both booklets document one cut on contact and a base-locked camera only for the close play (#610). |

| D15 | Fence height | See note *D15, Decision* below. | Sitting 2026-09-12: the drawn Harbor wall was 26 ft over an 8 ft sim fence (#608). |

| D17 | Fielder lock | **No lock verb.** A hand-off never takes the human's body while it still has a route to the ball, so a lock has nothing to prevent. Revisit only if a sitting shows a mid-run swap. | Superstar (hold L) and Power Pros (hold R1) needed one because their auto-switch re-evaluates by distance. ✅ by omission; the route guard is `TryHandoffOutfield` on the ground (S-96, #633) and in the air (S-97's caught liner, #636) |

| D18 | The human starts on the glove? | **No, in Exhibition.** The CPU runs the play glove until the stick passes the take threshold or LB is pressed; a dead stick catches but never throws. Training scoop drills start you on it. | #83 and #209: a human never gets an auto-out, never gets robbed of one. Sluggers Remote-only mode auto-pursues and auto-throws; with the Nunchuk you move. ✅ code (`FieldAssist`) |

| D20 | What is a pitch type? | See note *D20, Decision* below. | **Supersedes** the older rule in §4.3 and Appendix A.1 row 2 — "pitch type strings are retired… an unknown type flies as a fastball" — which was right when there were two hard-coded shapes and becomes a silent wrong pitch as soon as a third family exists (PH-02-R2, PH-02-R1). Curveball / slider / sinker were unauthored until P1-d's numeric trial; Jack accepted the `trials/pitch5` window on September 22, 2026 ("trial was good.") and #860 authored them in the shipped table. |

| D21 | What is a park? | See note *D21, Decision* below. | Jack, 2026-09-21, [#814](https://github.com/jackguillet/grand-sluggers/issues/814): [plan-fields.md](../plan-fields.md) FD-01 … FD-19, [research-fields.md](../research-fields.md). The GameCube game varied fence, wall, bounce and roll per park; real parks vary air, wind, wall and foul ground. Today a park can change only its fence posts, fence height, wind, one night number and its hazards. |

**D15, Decision.** **One number**: the park's `fenceHeightFt` drives both the flight clip and the drawn wall; a gate asserts they match. The Harbor value is Jack's call (recommended 12 ft). ✅ #608: `HarborWall.OutfieldHeight(park)` is the park field; Harbor ships at 12 ft. **Amended by D21 (FD-06, 2026-09-21):** a park's fence may become a polyline with a height per point. The rule then reads *the drawn wall equals the flight wall on every span*.

A park that lists no points keeps the one number. ✅ F2-c (#874): the polyline is built and the drawn loop draws each span between the flight's own vertices at the flight's top there (`HarborWallTests.SF05_APolylineParkDrawsEveryPointAndEverySpanAtTheFlightsTop`); no park names points, so every park keeps the one number. ✅ F2-b2 (#873, PR #875; FD-06-R2):

**D16, Why.** Genre: every game says "nearest" and every review complains about mid-run swaps (Sluggers, Super Mega Baseball 4, The Show). Superstar's own rule is by hit class and area with an explicit yield rule. Ours is the route planner (§8.2) plus the event rule (§8.9). ✅ code since P4 / #615; a liner that bounces in the gap and rolls to the wall uses the roll, not the bounce (#667)

**D20, Decision.** **A family id from the closed library** (`PitchFamily`: fastball, changeup, curveball, slider, sinker). `PitchCommand.Type` *is* that id; there is no `Changeup` bool beside it. Every family's numbers are an authored row in `pitching.json` `families`, and one shape function evaluates every row. An id with no authored row, and an id outside the library, both **stop the pitch and name themselves**. Break stays a stick verb and charge stays a modifier: neither is a family. ✅ #810 (`PitchFamilyTable.Of`, `PitchFlight.Shape`)

[^d4-window]: The 9 / 7 split quoted here is **the reference's window, and the one that shipped before #860**. Since #860 the shipped rule is PH-10-R1 (with PH-11-R1, PH-15-R7 and PH-17): one 9-frame window for every hitter, both swings and every difficulty, which Jack accepted the `trials/pitch5` window on September 22, 2026 ("trial was good.") (§5.3, §16). The split and its switch were removed by #887. D4 itself — cursor decides quality, timing decides direction — is untouched by that.

### 0.2 Field proportions and race calibration — D19 (#693)

## 0.2 Field proportions and race calibration — D19 (#693)

**The compact field is the game (3e, Jack, 2026-09-22).** The ordinary loop plays on the C80 contract below; there is no second profile. Where a decision paragraph in this section still says "trial", "the C80 copy" or "the shipped table keeps", the game now plays the C80 value, and the older number is only the value a switch takes when it is off. Specials and special statuses are outside this contract (they play as authored), and the throw-before-cover rule is unchanged.

**Research gate; first spatial trial selected, remaining targets pending.** Before changing field dimensions, ordinary throw/exit speeds, flight time scales, runner clocks, or catch/pursuit distances to match Sluggers, load [the #693 research](../research-game-feel-693.md) and [decision plan](../plan-game-feel-693.md). Jack's direction is to compare **Wii and GameCube before choosing**.

After the first comparison, Jack accepted **Wii as the provisional visual lead, with GameCube as a mechanics cross-check** on September 14, 2026 (F693-01). This governs readability reviews; numerical reference selection and target values remain pending. Identify each value as primary documented, community reported, Harbor measured/code-derived, proposed, or unresolved. GameCube raw engine values are not Wii measurements or Harbor mph.

Under F693-02, Jack accepted on September 14, 2026 that **both infield and outfield dimensions may change independently** to achieve reference-informed proportions. Existing 90-foot basepaths and fence depths are not fixed constraints. Jack subsequently specified a **compact, cartoon-feeling outfield that preserves doubles, triples, and relays**: do not enlarge the outfield merely to create extra-base opportunities.

Under **F693-02-spatial-trial**, Jack approved **C80: 80-foot basepaths, 232 / 280 / 232-foot fences and unchanged character sizes** on September 14, 2026, as the **first trial**, following the [#708 comparison](../research-game-feel-708.md). This is an original spatial trial, not verified Nintendo dimensions or final shipping geometry.

Under F693-02 pursuit consistency, Jack accepted on September 14, 2026 **one ordinary pursuit movement profile per character across hit types and assigned fielding positions**. Keep explicit dash, ability, and status modifiers and meaningful character speed differences; fielding and baserunning remain separately tunable. This supersedes §8.1’s hit-class/position speed multipliers as the target design.

Under F693-02 movement weight, Jack accepted on September 14, 2026 **a brief build-up to running speed, quick direction corrections, and little residual drift when movement intent changes**. This is the ordinary pursuit target after movement eligibility, consistent across hit types and assigned fielding positions for each character. Numerical acceleration/braking timings remain pending reference comparison and playtesting.

Under F693-02 post-contact read, Jack accepted on September 14, 2026 **a brief, visibly communicated read of the hit before ordinary pursuit becomes available, followed by the accepted responsive movement**. Calibrate its length against ball travel, remaining defensive opportunity, and the separate camera handoff.

Under F693-02 ball readability, Jack accepted on September 14, 2026 **moderate, bounded visual size assistance for distant moving balls**, alongside contrast and readable trails, with a believable glove-fitting held size and smooth possession transition. His explicit qualification is “make it moderate”: enlargement supports tracking without becoming a prominent growth effect or causing distracting size changes.

Exact diameters, distance response, and smoothing remain pending comparison and playtesting; current baseline sizes are not thereby approved. This changes presentation intent only: ball center, trajectory, travel time, catch coverage, and physical judgments remain sim-owned. Preserve stable character stature through possession under #691.

Preserve D1–D18, including D7's pitch-pace hold, until a numbered decision explicitly supersedes them. S-31/32/33, the double-play/relay/tag/sac-fly families, fixed-input trajectories, and the scoring cohorts must be assessed together. Under F693-06, Jack accepted retaining S-29's **1.8–5 mean runs per side** regression guardrail on September 14, 2026. Apply it separately to the home and away means across the existing cohort; individual games may fall outside the band.

Under F693-06-H, Jack also accepted on September 14, 2026 the same **1.8–5 mean runs per side** target independently for each Harbor-specific calibration and validation cohort, separately home and away, while retaining S-29. The #702 baseline misses the new floor for Harbor calibration away (1.38) and Harbor validation home (1.60); the complete compact-field calibration must address those gaps. No coefficient is selected by this acceptance.

It does not establish reference fidelity or human acceptance. Jack accepts target relationships and the standalone race; agents do not pass #693 or its linked human gates.

Under F693-05, Jack accepted **reliable routine defense** on September 14, 2026: a clean ordinary grounder against an average runner and ordinary arm should normally become an out after reasonably prompt correct execution, with a readable margin. Fast runners, deep pickups, weak arms, bobbles, and hesitation create the tight races. This is calibration intent, never an automatic out or a permission to slow runners to rescue a fixture. Numeric opportunity/margin bounds and human validation remain pending.

Under **F693-05-runner-clock**, Jack approved on September 14, 2026 preserving the current runner elapsed pace as C80’s calibration anchor: the existing stat-to-bag formula, **2.95-second nominal straight bag interval at Run 5**, **0.5-second batter startup**, and current stat differences/dash schedule. This yields approximately **3.45 seconds to first without dash** in the simplified model; exact starting/rounding/slide paths remain to be validated.

Under F693-03, Jack accepted **quick release and readable travel, with a visibly registering transfer/release** on September 14, 2026. Quick does not mean instant: possession, transfer, release, and follow-through must remain recognizable at ordinary gameplay speed and couch distance. Compare ordinary Wii and GameCube throws to set the timing; special quick-transfer or boosted throws cannot define the ordinary baseline.

Under **F693-03-release-clock**, Jack approved **0.30 seconds from an accepted ordinary throw command to ball release** on September 14, 2026, as the clean-possession, ordinary-arm trial baseline. Motion starts immediately and the ball stays in hand until actual release; this is not an idle input delay. Human decision time, pickup/read, recovery, follow-through and flight are separate. Stat/ability adjustments and pre-possession buffering are not selected by this scalar.

Under **F693-03-travel-clock**, Jack approved **0.90 seconds of ball flight over 80 feet** on September 14, 2026, proportional to horizontal distance for ordinary infield throws with a neutral middle arm, while preserving character arm differences. The accepted 0.30-second release is separate, giving 1.20 seconds from ordinary command to target over 80 feet. Target arrival does not itself grant reception or an out.

Under **F693-03-long-throws**, Jack accepted on September 14, 2026 **moderate, smooth, arm-dependent loss of pace on very long direct throws**, allowing a well-positioned relay to be faster while keeping useful direct throws available. Jack explicitly added that **character chemistry can come into play here**: the complete direct/relay calibration must consider the actual thrower/receiver relationships alongside arm strength, distance, transfer and coverage.

Under **F693-03-good-chemistry**, Jack approved on September 14, 2026 retaining **1.30× travel speed for a good thrower–receiver pair**, evaluated independently for each actual direct or relay leg. Divide otherwise-calibrated flight duration by 1.30 once; keep the ordinary 0.30-second release and apply no extra relay-chain or range bonus. An 80-foot ordinary good-pair flight is about 0.692 s, or 0.992 s including release.

Under **F693-03-long-throw-numbers**, Jack approved the ordinary long-throw trial profile on September 14, 2026: comfortable range `R = 160 + 5*(Field-5)` feet; base speed `v = (80/0.90)*(0.85+0.03*Field)` ft/s; neutral flight `T = distance/v + 0.60*(max(0,distance-R)/80)^2` seconds. Apply the accepted good-pair factor once (`T/1.30`), then add the separate 0.30-second ordinary release. The range is a gradual timing transition, not a forced cutoff or maximum distance.

Under **F693-03-negative-chemistry**, Jack approved on September 14, 2026 replacing bad chemistry's extra random slant with **0.90× travel speed per actual bad thrower–receiver pair**. Divide the full otherwise-calibrated flight by 0.90 once, including long-range loss; retain the separate 0.30-second ordinary release. Remove the chemistry-only random lateral miss, not general Field-dependent accuracy, bobbles or special effects.

Under **F693-03-relay-ownership**, Jack approved on September 14, 2026 **one deliberate throw command per human relay leg**, with early-input buffering for the next receiver. A cutoff holds without an onward command; a queued command can be canceled or retargeted before onward release motion starts. Selecting a bag or continuing to hold the first button does not authorize another throw.

Under **F693-03-snap-throw**, Jack approved on September 14, 2026 a **0.22-second release after clean teammate-throw reception**, replacing Snap Throw's universal 1.22× flight-speed boost. Ground pickups, batted-ball catches and loose recoveries retain the ordinary 0.30-second release. Eligibility belongs to uninterrupted possession begun by a clean received throw; a delayed command can use it, but a bobble/drop clears it. Required recovery finishes before release starts.

Under **F693-03-laser-throw**, Jack approved on September 14, 2026 **1.25× travel speed on actual throws home when a live unresolved runner is on third or on the third-home segment**, replacing Laser's universal 1.45× bonus. Check eligibility at release-motion start and lock it for that throw; advancing and returning runners on that segment qualify, scored/retired runners do not.

Under **F693-03-input-buffer**, Jack approved on September 14, 2026 a **0.25-second early onward-throw buffer**, measured in active simulation time from a deliberate press until release-motion readiness. Execute immediately once the expected receiver has secure possession and completed required recovery within the window; otherwise expire. Already-ready commands have no added wait. A command at exactly .25 seconds is valid, and expiry cannot interrupt an already-started release.

Under **F693-03-throw-cancel**, Jack approved on September 14, 2026 **fresh RB / period cancels a queued defensive throw**, while existing bag selectors retarget it before release motion without refreshing its .25-second age. Cancellation clears the pending throw and retains selected-only target state; a fresh throw command is required afterward. Queue feedback distinguishes selected from queued and clears immediately on cancel/expiry.

Under **F693-02-pursuit-speed**, Jack approved on September 14, 2026 **18 ft/s ordinary pursuit top speed at Run 5**, with `speed(Run) = (21 + 1.9*Run) * (18/30.5)` to preserve existing relative character differences. Use the accepted common per-character profile across assigned positions and hit classes; replace legacy IF/OF class multipliers only during coherent calibration. This chooses top speed, not read, acceleration/braking, dash/status, carrying/coverage behavior or a full-race outcome.

Accepted baserunning elapsed anchors remain unchanged. Actual routine/deep-hit races, scoring and human feel remain required. ✅ #718 (slice 1): the `c80` copy carries `fielding.chase` 12.4 + 1.12 × Run, floor 4.72 — 18.0 ft/s at Run 5, the spread kept to half a percent.

Under **F693-02-outfield-read-clock**, Jack approved on September 14, 2026 a **0.40-second base ordinary pursuit read from contact for LF/CF/RF**, replacing the .83-second target. No extra camera-completion delay. Preserve contact-clock eligibility, the existing airborne hang cap, and human-glove versus CPU-driven ownership; human neutral-stick assistance uses human timing and glove switches do not restart the read.

Under **F693-02-infield-read-clock**, Jack approved on September 14, 2026 a **shared 0.25-second base ordinary pursuit read from contact for 1B/2B/SS/3B**. Preserve the contact-clock ownership, existing airborne cap and held-direction behavior described for the outfield; no new camera wait or universal catch/action lockout. Pitcher/catcher timing stays separate, and CPU difficulty multipliers remain baseline assumptions pending validation.

Under **F693-02-pitcher-read-clock**, Jack approved on September 14, 2026 a **0.35-second base pitcher pursuit read from contact**, preserving shared contact-clock ownership, airborne cap and held-direction semantics. Do not add it after the camera cut or pitching follow-through, invent a second fixed delivery-recovery timer, or grant comebacker immunity. Any real action-readiness constraint is separately traced and combined as a prerequisite rather than blindly summed.

Under **F693-02-catcher-read-clock**, Jack approved on September 14, 2026 a **0.45-second base catcher pursuit read after batted contact**. Start at contact, with the shared ownership/airborne-cap semantics and no extra stand-up or camera wait. Preserve actual catcher placement and bunt routes. Do not add this delay to pitch receiving, steal throws, received fielding throws at home or tags; the separate steal-release clock still needs full-contract review. This accepts a calibration anchor, not runtime tuning or a passed play gate.

Under **F693-02-pursuit-acceleration**, Jack approved on September 14, 2026 a **linear .20-second build-up from rest to ordinary full pursuit speed**. Movement starts when eligible and requested, without extra standstill or precharging during the read. Each character reaches their own accepted top speed in the same duration across positions/hit classes. Preserve existing physical movement through selection/ownership changes; one sim response must govern prediction and stepping.

Under **F693-02-pursuit-braking**, Jack approved on September 14, 2026 **linear ordinary braking from full speed to rest in .10 seconds**, using each character's ordinary top speed divided by .10 as braking magnitude. Lower initial speeds stop sooner. Assisted fixed-target routes plan braking before arrival; no snap-back, added post-arrival wait or enlarged reach. Neutral-stick assistance and geometric moving catches remain. Reversal/turning, handoff coast and other movement states require separate review. Runtime and human validation remain pending.

Under **F693-02-pursuit-reversal**, Jack approved on September 14, 2026 a **full opposite-direction pursuit response using the accepted .10-second brake followed immediately by .20-second acceleration**, from ordinary full speed. No extra pivot pause or uninterruptible sequence; changed intent acts from current physical velocity. Selection does not reset velocity or read. Catch geometry stays active, and authored facing does not impose a sim timer. Angled corrections follow separately. This authored trial remains pending runtime and human validation.

Under **F693-02-pursuit-angled-turn**, Jack approved on September 14, 2026 **continuous ordinary angled correction through the shared brake/start velocity response**: move toward requested velocity at `V/.10` while speed decreases and `V/.20` while it increases, splitting at the minimum-speed point without dwell. This yields about .115 seconds / 92.4% minimum speed at 45 degrees and .212 seconds / 70.7% at 90 degrees from full speed, continuously matching the accepted reversal.

Under **F693-02-pursuit-analog-response**, Jack approved on September 14, 2026 **linear proportional ordinary pursuit speed across the active radial stick range beyond neutral**: half usable range requests half ordinary speed, full range full speed. Feed requested velocity through the accepted physical response; preserve deliberate steady fine input and total speed cap. Neutral stick retains assistance.

Numerical neutral/ownership boundaries and calibration remain separate; do not globally remap aiming or add a walk modifier. Runtime and human validation remain pending. ✅ #718 (slice 4): on the `c80` copy the asked velocity is the calibrated stick's unit direction × `(magnitude − stick.leaveMag) / (1 − leaveMag)`, capped at 1 (a diagonal past the ring still asks full speed), fed through the response law like any other want (`PursuitStick.Read`, `LivePlaySystem.ReadPursuitStick`).

Under **F693-02-pursuit-neutral-boundary**, Jack approved on September 14, 2026 **manual pursuit entry at calibrated radial magnitude >=.20 and return to assistance at <=.15**, retaining the current owner between them. Manual target speed uses the accepted linear remap from .15 to 1. Apply per seat/device after arming; retain intent ownership through glove selection and each body's physical velocity. Returning to assistance may continue pursuit.

Calibration, arming and device lifecycle remain separate; one documented input coordinate and one remap are required. Runtime and human validation remain pending. ✅ #718 (slice 4): `fielding.stick.enterMag` 0.20 / `leaveMag` 0.15 in the `c80` copy, radial magnitude of the centre-subtracted stick, the owner kept between the gates; at 0 / 0 (shipped) the read is the Manhattan gate at `feel.fieldAssistStick`.

Under **F693-02-pursuit-calibration-policy**, Jack approved on September 14, 2026 **a valid released-stick pursuit calibration established outside live baseball and held fixed through the match**, per actual bound device. Recalibration is explicit while paused and adopted only on valid samples; failure retains the prior valid profile. No silent live center learning, copied profile on controller replacement or automatic widening of .20/.15.

Exact sample criteria, arming lifecycle and couch implementation remain separate. Runtime and human validation remain pending. ✅ #718 (slice 4): `StickCalibration` per bound device (`LivePadInput.Device`, `LivePlaySystem.FieldStick(device)`), sampled by the client on an input clock and adopted only through `TryAdopt` / `PursuitStick.Recalibrate`; a fresh device is on the identity profile (a digital stick's), `DeviceReplaced` invalidates it, a refusal keeps the prior.

Under **F693-02-pursuit-arming**, Jack approved on September 14, 2026 **a valid profile plus one neutral observation <=.15 on defensive-role entry, controller binding/recovery or successful recalibration**, without extra neutral dwell. Retain pursuit readiness through that defensive half, including pitches/contact, glove changes and ordinary pause with the same valid device. Held direction acts at the accepted read deadline, not before.

Keep unready recovery visible and preserve the existing running-versus-paused resume policy, physical velocity and other verb guards. Calibration sample criteria, runtime and human validation remain pending. ✅ #718 (slice 4): `PursuitStick.Armed` — a valid profile plus one read at or under `leaveMag`; owed again by `EnterDefense` (every new half, from `BeginLive`), `DeviceRecovered`, `DeviceReplaced` and a successful `Recalibrate`; kept through the half.

Under **F693-02-pursuit-calibration-samples**, Jack approved on September 14, 2026 **a .50-second valid released-stick calibration window with mean radial center offset <=.10 and each sample <=.02 from that mean**, inclusive. Use the documented normalized device coordinate before center subtraction/remap and a non-live input clock, not paused sim time. Adopt only a complete valid window; failure preserves the prior profile.

This is calibration only, with no added dwell on routine neutral checks. Real controller, runtime and human validation remain pending. ✅ #718 (slice 4): `fielding.stick.calibrationSec` 0.50, `centerOffsetMax` 0.10, `sampleSpreadMax` 0.02 in both roots, inclusive; `StickCalibration.Sample(x, y, clockSec)` takes the client's clock, `Complete` spans the window, `TryAdopt` spends a complete window whether it is adopted or refused.

Under **F693-02-field-dash-peak**, Jack approved on September 14, 2026 **1.20x ordinary pursuit peak speed** for fielding dash (Run-5: 18 to 21.6 ft/s), preserving character speed ratios across positions/hit classes. This supersedes the 1.35 multiplier as the target trial only. Duration/recovery, entry/exit response and CPU/assist activation remain separately reviewed; no read bypass, catch-reach or pickup/throw bonus is implied.

Section 8.1's two-second fade is not present in the inspected held-boost path and remains an explicit contract/runtime reconciliation item. No runtime tuning or human gate is passed here. ✅ #718 (slice 3): superseded as accepted — the `c80` copy carries `dash.chaseMul` 1.0, so there is no universal fielding sprint on the trial; the shipped table keeps ×1.35 on East held, with no fade, as it always had.

Under **F693-02-ball-dash-carrier**, Jack approved on September 14, 2026 **automatic 1.20x ordinary carrying speed only for Ball Dash ability holders with secure live-ball possession**. No activation input, burst timer, stamina or cooldown. This supersedes the universal sprint peak above and the two-second proposal Jack approved immediately before redirecting the design. Before possession, ordinary pursuit applies; the separate baserunning dash remains as approved.

Use actual ownership for both seats and CPU, with shared movement/forecasting and no catch-reach or throw bonus. Ordinary carry base/response and ability holders remain separately reviewed in the [decision report](../research-game-feel-708.md#current-dash-revision--passive-ball-carrier-ability). Runtime/help still describe the earlier universal sprint and require coordinated later migration; no runtime or human gate is passed here. ✅ #718 (slice 3):

Under **F693-02-ordinary-carry-speed**, Jack approved on September 14, 2026 **ordinary live-ball carrying top speed equal to ordinary pursuit top speed**, without a generic possession penalty/bonus. Preserve the character speed curve: Run-5 carries at 18 ft/s, or 21.6 with the separately accepted passive Ball Dash. Apply the same carry context to both human seats, CPU movement and carry-versus-throw forecasts; no retained contact-class multiplier or universal held-button sprint.

This chooses top speed only: carrying response, pickup/throw recovery, coverage, roster/status and full-race validation remain open. No runtime or human gate is passed here. ✅ #718 (slice 3): there is no carry multiplier of its own in either table — a body carries at the pursuit speed it was asked for, and only a Ball Dash holder at more.

Under **F693-02-carry-movement-response**, Jack approved on September 14, 2026 **the ordinary acceleration/braking/continuous-turn law for eligible carrying, with rates based on unboosted character speed even during Ball Dash**. Use `a=V/.20`, `b=V/.10`; Ball Dash changes only the requested speed cap to `1.20V`. Ordinary start/stop/reverse remain .20/.10/.30 seconds; boosted full-speed equivalents are .24/.12/.36.

Preserve actual velocity through eligible possession/intent changes and share prediction/stepping across seats and CPU. This does not remove pickup/recoil/throw restrictions, grant movement during a throw, precharge a ramp or reset contact read. Recovery/eligibility, coverage/roster/status and actual play validation remain open; no runtime or human gate is passed here. ✅ #718 (slice 2):

Under **F693-02-clean-ground-pickup-readiness**, Jack approved on September 14, 2026 **zero generic post-possession pause for routine clean ground/loose pickups with no active exceptional recovery**. Preserve visible acquisition and authoritative secure ownership; movement continues through the accepted response, and a valid deliberate/buffered command may start ordinary .30-second release at readiness.

Queue expiry/cancel and loss/bobble ordering still apply; ground pickups do not qualify for Snap Throw. This grants no instantaneous pickup, extra reach, automatic throw or bypass of recoil/dives/other recovery. Exceptional recovery and real play validation remain open; no runtime or human gate is passed here. ✅ #720 (slice 1):

Under **F693-02-ground-pickup-recoil-basis**, Jack approved on September 15, 2026 **deterministic ordinary retained-ball ground-pickup recoil based on actual incoming ball speed at acquisition and Field rating**, replacing original launch/contact-quality strength as the recoil input. Sample incoming world-space speed on the active gameplay clock immediately before possession stops/attaches the ball, respecting bounce/wall event order.

At fixed Field slower arrival cannot worsen recoil; at fixed arrival higher Field cannot worsen it; routine arrivals have zero recovery. This changes neither bobble RNG nor special-effect rules. Onset/curve/cap, Field scaling, physical reaction and action permissions remain separately reviewed; no runtime or human gate is passed here. ✅ #720 (slice 1):

Under **F693-02-ground-pickup-recoil-cap**, Jack approved on September 15, 2026 **a .20-second maximum for ordinary retained-ball ground-pickup recoil, explicitly allowing authored special hits to exceed this cap**. Routine arrivals remain zero; lesser qualifying impacts may recover sooner. Use one active-sim ordinary recovery interval from secure acquisition; physical settling and required animation do not append separate waits.

Special effects have their own reviewed conditions/durations, not a global .20 clamp; ordinary Perfect/Nice/charged contact alone grants no exemption. Special/ordinary composition, numeric onset/curve/Field mapping and physical/action recovery remain pending. Existing .25 input buffer and .30 ordinary release remain unchanged; no runtime or human gate is passed here. ✅ #720 (slice 1):

Under **F693-02-special-recovery-composition**, Jack approved on September 15, 2026 **adding ordinary Field-dependent recoil and authored special recovery contributions from the same pickup/impact**, preserving the better fielder’s shorter ordinary recovery. Cap the ordinary portion at .20; special and total recovery may exceed it. Count each contribution once and only for actions it actually restricts; do not add another animation-tail wait.

Under **F693-02-recoil-field-shaping**, Jack approved on September 15, 2026 **a shared incoming-speed onset, with Field reducing ordinary recoil duration after speed severity is bounded**. Structure: `.20*S(arrivalSpeed)*F(Field)`, with monotonic `S` in [0,1], zero through shared onset, and positive decreasing Field factors at most 1. Bound severity before applying Field so high severity does not flatten different factors at a common cap; add applicable special recovery afterward.

Numeric onset/full-severity speeds, S curve and Field factors remain separately reviewed. No stat changes, frame rounding, unreviewed minimum recovery, runtime or human gate is selected here. ✅ #720 (slice 1): `FieldingResolver.RecoilSeverity` bounds S first, `RecoilHandsFactor` applies after — `RecoilWeight = S × F`; Hands 1 / 5 / 10 pay 0.20 / 0.16 / 0.11 at full severity.

Under **F693-02-recoil-field-factors**, Jack approved on September 15, 2026 **`F(Field)=1-.05*(Field-1)` for validated Field ratings 1–10**, a linear five-percentage-point reduction per rating above 1. Apply it only to ordinary capped speed severity: maximum ordinary recoil .20/.16/.12/.11 seconds at Field1/5/9/10, proportionally less for lesser severity. Add applicable special recovery afterward; do not shorten the special or .30 release with this factor. Existing ratings remain unchanged.

Numerical speed anchors/severity curve, physical response and action permissions remain separate; no runtime or human gate is passed here. ✅ #720 (slice 1): `recoil.handsCutPerPoint` 0.05 in both roots, on the `Hands` trait (#712) — `1 − 0.05 × (Hands − 1)`, 1 / 0.80 / 0.55 at Hands 1 / 5 / 10.

Under **F693-02-recoil-severity-curve**, Jack approved on September 15, 2026 **`S(v)=clamp((v-v0)/(v1-v0),0,1)`**: linear ordinary recoil severity between two shared incoming-speed anchors, zero at/below onset and full at/above the upper speed. Require `v1>v0>=0`; apply `.20*S(v)*F(Field)` then applicable same-impact special recovery.

Numerical `v0/v1` remain explicitly unselected pending event-sided arrival-speed evidence and coherent ball-motion calibration; launch mph and the old energy threshold are not substitutes. Physical recoil and action permissions remain separate; no runtime or human gate is passed here. ✅ #720 (slice 1): `S = clamp((v − onset) / (full − onset), 0, 1)` with the anchors measured on the `c80` copy itself (48 `cli match` runs, 464 ground pickups):

Under **F693-02-ordinary-recoil-actions**, Jack approved on September 15, 2026 **blocking commanded locomotion/steering and throw-release starts during ordinary retained-ball recoil, while preserving secure possession and valid geometric forces/tags**. Actual legal contact can complete an out during recovery; absent contact, loss of possession or existing safe rules cannot. The live world and .25-second throw buffer/cancel/target management continue.

Current intent resumes through actual velocity at readiness, without precharging acceleration or a Ball Dash bypass. This does not select physical recoil displacement, permissions for special effects/dives/bobbles or movement during committed throws. No runtime or human gate is passed here. ✅ #720 (slice 1):

Under **F693-02-ordinary-recoil-displacement**, Jack approved on September 15, 2026 **modest physical pushback on qualifying ordinary retained-ball ground pickups**, including possible loss of bag/tag contact, and emphasized **special hits that impact the fielder**. Routine zero-recoil pickups add no shove. Use incoming horizontal ball travel for impact direction and actual integrated body/glove movement for contact; preserve acquisition-time legal outs and do not snap back to a bag.

Preserve coherent existing velocity and the accepted recovery interval. Impacting specials may author stronger displacement and longer recovery; this does not assign knockback to every special, select unlimited force or define impulse composition. Exact ordinary distance/velocity response and individual specials remain pending; no runtime or human gate is passed here. ✅ #720 (slice 1):

Under **F693-02-ordinary-recoil-distance-cap**, Jack approved on September 15, 2026 **a one-foot maximum added ordinary impact displacement for the initial trial**, with zero on routine pickups and less on weaker qualifying impacts. The ceiling applies to the ordinary impact contribution, not total body travel from existing momentum or special impacts. Special hits that strike the fielder may exceed it under their own finite authored bounds.

Preserve actual motion and geometric contact; do not clamp the body to a one-foot radius or force the maximum distance into every recovery. Exact severity/Field mapping, velocity response and special physical composition remain pending. No runtime or human gate is passed here. ✅ #720 (slice 1): the skid is `w²` feet with w ≤ 1 — one foot at Hands 1 on a rocket, 0.42 ft for vale (Hands 8), 0.30 ft at Hands 10 (`RecoilSkidFt`; the caps test measures 1.000 ft live).

Under **F693-02-ordinary-recoil-motion-profile**, Jack approved on September 15, 2026 **a brief impact kick that slows linearly to zero, with better Fielding reducing skid distance as well as recovery time**. Set `w=S(arrivalSpeed)*F(Field)`, retain `T=.20w` seconds, and use initial added speed `K=10w` ft/s with velocity `K*(1-t/T)` along incoming horizontal direction for `0<=t<=T`, giving `D=w²` feet.

Special composition, incoming-speed anchors and runtime/standalone verification remain open. ✅ #720 (slice 1): `recoil.kickFtPerSec` 10 in both roots — `K = 10 w`, `T = 0.20 w`, `v(t) = K (1 − t / T)`, integrated exactly per frame (`K [(t₁ − t₀) − (t₁² − t₀²) / 2T]`), so the skid is `w²` to the last decimal; the body's own locomotion brakes through `TickIdleBrakes` (`chase.brakeSec` 0.10) and the intent resumes from the actual velocity at readiness. No frame rounding, no minimum.

Under **F693-02-special-impact-motion-composition**, Jack approved on September 15, 2026 **concurrent ordinary and authored special pushback from the same retained-ball pickup/impact**, each counted once in one actual body/glove path. Combine velocity contributions as vectors with their own clocks/directions/bounds; retain ordinary Field-dependent motion and its one-foot component ceiling without clamping the combined special effect to one foot.

Under **F693-02-special-impact-field-resistance**, Jack approved on September 15, 2026 **up to 20% Fielding resistance to a special's physical pushback and associated impact recovery**. Use `R(Field)=1-.20*(Field-1)/9` for ratings 1–10, applied once to the special component before composition; retain ordinary recoil's separate factor.

Under **F693-02-special-pushback-possession**, Jack approved on September 15, 2026 **retaining an already-secured ball through pure special pushback, with no additional random drop roll**. Existing failed acquisition and bobble outcomes remain unchanged. An explicitly authored dislodging attack can cause a real loose-ball transition under its own reviewed trigger, resistance and recovery contract; no attack or drop probability is assigned here.

Under **F693-02-special-pushback-actions**, Jack approved on September 15, 2026 **ordinary recoil permissions for pure special pushback**: block commanded steering and throw-release starts through the applicable recovery, while retaining valid held-ball force/tag evaluation and responsive buffer/cancel/target management. A force resolves at legal possession-and-bag contact, without waiting for runner arrival; later pushback cannot reverse the out.

Under **F693-02-mixed-status-action-readiness**, Jack approved on September 15, 2026 **restoring each action when its normal prerequisites hold and no active effect still restricts it**. Expiring one source clears only its restrictions; unrelated lingering effects do not impose a blanket character lock. First construct the approved same-impact ordinary-plus-special recovery sum, then evaluate that restriction alongside other reviewed statuses.

Under **F693-02-repeated-impact-recovery**, Jack approved on September 15, 2026 **starting each distinct physical impact's recovery at its actual arrival**, with an action blocked through the latest applicable end among active intervals. Do not queue the new full duration after the unfinished wait or let a weaker hit truncate an older restriction. Within each qualifying event, ordinary and Field-resisted special recovery still add before overlapping with distinct events.

Under **F693-02-special-impact-repeat-eligibility**, Jack approved on September 15, 2026 **one physical-impact application per fielder per special activation by default**, with deliberately multi-hit exceptions requiring explicit review. Continued contact, genuine re-entry, bounce, drop/reacquisition or control switching cannot retrigger that activation's physical impact on the same fielder.

Under **F693-02-clean-air-catch-readiness**, Jack approved on September 15, 2026 **zero generic added pause after a clean routine airborne batted-ball catch by a grounded fielder**. Once possession is securely established and no applicable recovery blocks the action, eligible movement or a valid ordinary .30-second throw release may begin while play remains live.

Under **F693-02-grounded-air-catch-recoil**, Jack approved on September 15, 2026 **reusing ordinary recoil response and Fielding benefits for qualifying hard airborne batted-ball catches by grounded fielders**, while routine fly catches retain zero added delay. At equal normalized severity/Field, use the accepted `.20w` recovery, `10w` initial horizontal kick and `w²`-foot displacement, with secure possession and completed catch outs preserved.

Actual incoming speed drives severity; airborne trigger anchors and whether they can share ground values remain unselected pending measurements that include vertical descent and routine-fly controls. Do not create recoil solely from a hit label, invent a lateral kick for purely vertical arrival or add a drop roll. Jump/dive/landing and reception contracts remain separately reviewed; no runtime or human gate is passed here. ✅ #720 (slice 2):

Under **F693-02-jump-catch-throw-readiness**, Jack approved on September 15, 2026 **actual landing before starting the ordinary throw release after a normal jumping catch, with zero generic extra pause after a clean landing**. Require secure possession, live play, landing and completion of applicable throw restrictions; landing neither restarts nor cancels another recovery.

✅ #719 (slice 3): on the `c80` copy a jumping catch (`CatchJump` with the body `Airborne`) releases nothing until the landing — `ThrowPress` drops a South in the air except in the last `throw.relayBufferSec`, where it is remembered and fires at the first grounded frame; a grounded catch adds nothing.

Under **F693-02-jump-air-control**, Jack approved on September 15, 2026 **limited horizontal correction during a normal jump while preserving actual takeoff momentum**. Neutral intent retains horizontal drift in free space; active correction changes velocity gradually without an instant stop/reverse, added lift or extended airtime. The same bounds apply before/after a catch and across human/CPU/assisted intent; possession or selection changes cannot reset motion or replenish correction.

Under **F693-02-normal-jump-input-profile**, Jack approved on September 15, 2026 **one consistent normal-jump vertical profile per eligible press**, without hold-for-height, early-release shortening, catch reset or automatic repeat from holding through landing. Horizontal correction remains separate and cannot add height/airtime. An uninterrupted jump retains its vertical clock through catch or miss; actual external impacts/collisions remain governed by their own reviewed rules.

Consistency does not select equal reach across characters or erase jump abilities. Numerical arc, takeoff latency/early-input handling, character variation and exceptional jumps remain separately reviewed; no runtime or human gate is passed here. ✅ #719 (slice 3): `fielding.catch.jumpAirSec` 0 shipped (the arm window) / **0.60** c80 — one airborne clock per fresh press (`TickJumpPress`, `Takeoff`, `TickJumpClock`), no hold, no cut, no repeat while held, kept through catch or miss.

Under **F693-02-normal-jump-takeoff-ownership**, Jack approved on September 15, 2026 **starting a normal jump promptly from eligible input rather than scheduling takeoff to the ball or catch window**. The player owns timing and can jump too early or late; actual catch geometry remains authoritative. Any later-reviewed anticipation is fixed relative to input, not ball prediction.

Respect existing control/read/recovery restrictions, and do not add a human-side automatic jump takeover or retime an active jump after a changed ball path. Numerical startup, early-input buffering and physical arc remain separately reviewed; catch arming timers do not establish takeoff timing. No runtime or human gate is passed here. ✅ #719 (slice 3):

Under **F693-02-normal-jump-arc-trial**, Jack approved on September 15, 2026 a baseline level-ground normal jump with **2 feet of body-root rise, .60 seconds airborne and a .30-second apex**. Use `h=4H*u*(1-u)` with `H=2`, `u=t/.60`; character vertical acceleration is approximately -44.44 ft/s², independent of ball physics. The airborne clock begins at actual takeoff and excludes any separately reviewed anticipation and subsequent throw release. Preserve horizontal momentum:

neutral ordinary Run5 travel is 10.8 feet, with no added lunge. Root rise is not absolute glove height, catch radius or fence clearance; legacy reach allowances require separate reconciliation. Character/ability variation, startup, numerical air response and exceptional collisions/impacts remain separately reviewed. This is an authored calibration anchor, not a measured Mario arc or a passed runtime/human gate. ✅ #719 (slice 3):

Under **F693-02-normal-jump-air-response-trial**, Jack approved on September 15, 2026 **10% of ordinary ground acceleration and braking rates for active midair correction**, using the accepted velocity-segment turn law. Neutral resolved intent coasts. Ordinary speed `V` gives `a_air=.10*(V/.20)` and `b_air=.10*(V/.10)`; analog and eligible ordinary/carry requested caps remain in force.

Movement restrictions suppress commanded correction; vertical timing stays unchanged. These are authored calibration anchors, with reference comparison, runtime implementation and human gates still open. ✅ #719 (slice 3): `catch.jumpAirResponseMul` 0.10 — `Respond` scales the airborne glove's ramp and brake rates; the tests read 1.62 ft from rest and 7.56 ft against a reversal at Run 5 to within the frame sum.

Under **F693-02-normal-jump-startup-trial**, Jack approved on September 15, 2026 **zero added gameplay startup after an eligible normal-jump press is accepted**. Physical takeoff and airborne steering begin at that simulation event; the uninterrupted level-ground arc peaks .30 seconds later and lands at .60. Preserve actual position/velocity and all eligibility restrictions. This selects no artificial crouch/windup wait and promises no zero hardware/render latency.

Authored launch must agree with physical takeoff, without an invisible airborne glove while the body remains grounded; a genuine grounded anticipation would require revisiting this trial. Buffering ineligible input and exceptional profiles remain separate. This is an authored calibration anchor, not measured Mario timing or a passed runtime/human gate. ✅ #719 (slice 3): zero startup — `Takeoff` on the press frame, `JumpT` set to the airtime for the client's pose.

Under **F693-02-normal-jump-input-buffer**, Jack approved on September 15, 2026 a **.10-second inclusive buffer for a fresh grounded normal-jump press blocked by temporary read/recovery restrictions**. Start once at the first fully eligible instant while valid, using actual position/velocity and the full .60-second arc; already eligible input has zero added startup. Airborne presses do not queue a landing hop, and holding does not refresh age.

Bind the request to its fielder/owner; clear it on support, possession, control, role/play or device changes, defensive cancel, or a conflicting accepted exclusive action. An existing pending/committed throw/dive prevents buffering. Age uses active baseball time, freezes in pause and never refreshes from ball prediction or changing restrictions. This is separate from throw buffering and old catch-arming windows. No runtime or human gate is passed here. ✅ #719 (slice 3):

Under **F693-02-normal-jump-character-profile**, Jack approved on September 15, 2026 **the same ordinary 2-foot root rise and .60-second airtime across characters**, with a .30-second apex. Do not scale ordinary vertical motion by body size, Field or Run or introduce a separate ordinary jump stat. Preserve actual body/glove geometry, Run-dependent horizontal motion and separately accepted Field benefits; equal root lift is not equal absolute glove reach.

Exceptional jump abilities may differ through their own reviewed contracts. Actual glove/catch geometry and legacy reach reconciliation remain mandatory before implementation; this establishes neither a roster reach ranking nor measured Mario parity. No runtime, asset or human gate changes here. ✅ #719 (slice 3): one rise and one airtime for every character; the legacy `catch.jumpReachFt` 8 is **0** in the `c80` copy — the jump is the arc, not eight feet of horizontal reach.

Under **F693-02-normal-jump-catch-input**, Jack approved on September 15, 2026 **jump as the only required action press for a normal jumping catch**, with eligible actual ball–glove contact completing acquisition. No second catch press or continued hold is required. Positioning, takeoff timing and limited steering remain player-owned; a jump alone does not guarantee possession. Preserve special-hit acquisition/possession restrictions, the physical arc and landing-before-throw readiness.

Selection changes cannot transfer the jump or invalidate a legal physical interception solely because its body is deselected. Actual catch geometry/tolerances and other catch types remain separately reviewed. No runtime or human gate changes here. ✅ #719 (slice 3): `PlayerCaught` takes the ball on the jump alone while the body is in the air; no second press.

Under **F693-02-character-catch-range**, Jack directed on September 15, 2026 that **characters have an explicit catch range within which they can move their glove to meet the ball; Fielding must not control glove positioning or response**. This supersedes the unaccepted Field-dependent tracking proposal. Ordinary legal in-range catches should have corresponding visible glove placement without a new low-Field response failure.

**Current #693 target amendments — September 15, 2026; approved research trials, not shipped behavior:**

- **F693-02-catch-reach-envelope:** ordinary stand-up reach is a roughly 6-foot middle-character C80 trial, authored independently of ratings. The parity migration first seeds each character's current reach unchanged. The compact trial separately applies reviewed reach; per-character variation remains open. Preserve the separate dirt pad and loose-scoop scope, not a new glove collider. ✅ #719 (slice 1):

  It blocks move/throw readiness and overlaps the .40-second handling stun by taking the later end; it does not revoke a completed legal catch. No auto-success or error roll manufactured to force a CPU miss. ✅ #719 (slice 2): `fielding.catch.autoDive` 1 shipped / **0** c80 — at 0 `FlyCatch.AutoDive` fires neither behind the dead stick nor for the CPU on its own; `catch.diveRecoverySec` 0 / **0.60** with `diveRecoveryFieldCut` 0 / **0.025** (`FieldingResolver.DiveRecoverySec`:

- **F693-02-cpu-dive-intent-policy:** use live ball position/motion for dive commitment, not resolved future trajectory. Ordinary pursuit retains its existing route planner. Define and test the exact commitment predicate within this boundary; no missed dive quota or random failure is implied. ✅ #719 (slice 2):

**Historical — superseded in part by F693-02-defensive-trait-mapping.** Under **F693-02-fielding-rating-role**, Jack approved on September 15, 2026 **displayed Fielding as a summary of explicit defensive traits and abilities, with those underlying traits determining gameplay**. Changing only the summary must not change behavior or grant an ability. Final trait categories, score weights, handling/error rules and roster values remain pending.

Under **F693-02-handling-error-opportunities**, Jack approved on September 15, 2026 **reliable routine catches/pickups, with handling errors limited to explicitly defined difficult or disrupted acquisition situations**. Low handling alone must not add a generic random failure to an ordinary legal in-range play.

Specific difficulty triggers, handling response, resolution method and physical error outcomes remain unselected; candidate awkward-hop or disrupted-acquisition examples are not automatic triggers. Preserve retained-ball recoil/pure-pushback possession rules, distinguish acquisition failure from later dislodging, and keep throw accuracy separate. Existing bobble/drop consumers require explicit migration review; no runtime or human gate changes here. ✅ #721 (slice 1):

Under **F693-02-ordinary-handling-error-chance**, Jack directed on September 15, 2026 **a small error chance based on degree of difficulty and the character's underlying defensive quality**, superseding the briefly approved deterministic-resolution proposal. Higher difficulty increases risk; better relevant defense reduces it. Keep reliable routine acquisition and the reviewed difficult-opportunity gate, independent catch range/glove placement.

Chance bounds, trait/difficulty curves, attempt boundaries and failure outcomes remain pending; no runtime or human gate changes here. ✅ #721 (slice 1): one draw per qualifying take (`Match.RollHandling`, the seeded stream), only when the chance is above 0; `HandlingChance` records it; the failed take resolves before possession (the ball never attaches).

Under **F693-02-ordinary-handling-error-cap**, Jack approved on September 15, 2026 a **10% maximum error chance per qualifying difficult ordinary acquisition attempt**, with routine legal catches/pickups at zero. This is a final ordinary probability ceiling, not a blanket rate for every difficult play or a per-play cap. Resolve one result per genuine attempt; repeated frames/callbacks cannot reroll it. Special-hit probabilities remain separately reviewed.

Difficulty/defense curves, fresh-attempt boundaries and physical failure outcomes remain pending; no runtime or human gate changes here. ✅ #721 (slice 1): `handling.chanceCap` 0.10 in both roots; `FieldingResolver.HandlingErrorChance` clamps to it whatever the inputs; live, authored Hands 1 on the hardest ball seen faces 9.4 %.

Under **F693-02-ordinary-handling-chance-curve**, Jack approved on September 15, 2026 the ordinary trial curve **`p=.10*D*(1-.80*H)`**, where normalized difficulty `D` and underlying handling quality `H` lie in `[0,1]`. Full-difficulty weak/middle/strong risk is 10%/6%/2%; half difficulty gives 5%/3%/1%. Routine/unqualified cases stay zero, with no additive error floor. These coordinates are not displayed Fielding values or roster assignments.

Physical-to-D and trait-to-H mappings, ordinary modifier composition and failure outcomes remain pending; preserve independent glove/range behavior and the final 10% ordinary cap. This is an authored numerical anchor, not measured Mario probability or a passed runtime/human gate. ✅ #721 (slice 1): `p = chanceCap × D × (1 − handsCut × H)` with `handsCut` 0.80; H is the Hands trait plus the glove's help as the shipped roll counted it, 1 → 0 and 10 → 1 (`HandlingQuality`):

Under **F693-02-awkward-hop-difficulty-source**, Jack approved on September 15, 2026 **an awkward in-between hop at actual ground-ball acquisition as the first ordinary handling-difficulty source**. Routine rolls, low micro-bounces and clean short/long hops remain outside this source. Identify a real prior ground bounce and the actual acquisition-side ball/body context; neither a Ground sample, original hit label, high speed nor weak handling alone qualifies a play.

This can apply to a legally live ordinary batted ball after bouncing regardless of its initial trajectory class. Keep physical difficulty separate from handling quality and preserve existing movement/input ownership. Numerical hop phase/height/speed bands, normalized difficulty mapping, additional difficulty contexts and physical failure outcomes remain pending. This is a qualitative design choice, not measured Wii/GC logic or an implemented detector. ✅ #721 (slice 1):

Under **F693-02-ordinary-bobble-outcome**, Jack approved on September 15, 2026 **a modest visible loose-ball bobble nearby as the default failed ordinary awkward-hop acquisition outcome**. Begin continuous deflection at actual ball/glove contact, resolve failure before secure-possession benefits, and require actual recovery by an eligible defender. Do not teleport the ball, guarantee a safe call/base award or undo an earlier completed out.

Preserve contact-based baseball rules, retained-ball recoil/pure-pushback contracts and input-buffer aging/invalidation. Scatter trajectory, fumbler reaction, reacquisition eligibility and repeated-attempt rules remain pending; special-hit outcomes are reviewed separately. This is an authored outcome direction, not measured Mario scatter/recovery or a runtime/human gate pass. ✅ #721 (slice 1):

Under **F693-02-bobble-stun**, Jack directed on September 15, 2026 **a brief stun after an ordinary bobble, then resumption of pursuit and physical pickup**. This supersedes the unaccepted movement-available recovery proposal. The fumbling character cannot actively run/steer, initiate new jump/dive actions or secure the ball during the reaction; the ball and other eligible defenders remain live. Track the restriction on that character across selection/ownership changes.

Expiry restores eligibility under applicable independent gates, not automatic possession or a fresh error roll. Duration, handling dependence, entry braking/residual motion, committed-motion interactions, buffering and repeated-attempt rules remain pending. Do not interpret the direction as an instantaneous physical reset or adopt legacy fumble values without review. No runtime or human gate changes here. ✅ #721 (slice 1):

Under **F693-02-uniform-bobble-stun**, Jack directed on September 15, 2026 **the same ordinary bobble stun duration across characters**, declining the proposed handling-based reduction. Handling still reduces error probability through the approved curve; it does not also shorten this reaction. Do not scale the ordinary stun by displayed Fielding, Run or body size. Shared numerical duration remains pending. This does not equalize travel/pickup time or change accepted recoil and special-effect resistance contracts. No runtime or human gate changes here.

Under **F693-02-bobble-stun-duration**, Jack approved on September 15, 2026 a **shared .40-second ordinary bobble stun trial**, beginning at actual failed acquisition on active gameplay time. Pause freezes the timer; selection/ownership changes cannot reset it. At the deadline, ordinary action eligibility returns only if independent restrictions permit, with actual pursuit/contact still required.

Do not add a generic stand-up/read delay, grant possession or create a new error roll merely because the timer expired. Handling, displayed Fielding, Run and body size do not scale the duration. Entry motion, scatter, buffering and fresh-attempt rules remain pending. This is an authored calibration anchor, not measured Mario timing or a passed runtime/human gate. ✅ #721 (slice 1):

Under **F693-02-grounded-bobble-braking**, Jack approved on September 15, 2026 **ordinary locomotion braking within the .40-second grounded bobble stun**. Use ordinary top speed `V=(21+1.9*Run)*18/30.5` and braking `b=V/.10`; entry speed `u` slows without reversal, stopping in `u/b` over `u²/(2b)` absent collision. Run-5 full speed stops in .10 seconds over .90 feet. The stop and stun begin together, with no extra .10-second delay, steering/acquisition permission or new recoil impulse.

Under **F693-02-bobble-recovery-reliability**, Jack approved on September 15, 2026 **reliable legal recovery of the same ordinary bobble, without another ordinary error roll from that fumble**. The original fielder must satisfy the stun and other gates; an eligible helper can recover sooner through actual contact. Preserve the originating bobble through its loose recovery phase so scatter bounces, switching, range re-entry and repeated callbacks cannot manufacture a fresh awkward-hop roll.

This grants neither automatic interception nor a global one-error-per-play cap. Independently reviewed effects retain their rules, and later distinct events after genuine recovery follow their own contracts. General attempt boundaries and additional exceptions remain pending. No runtime or human gate changes here. ✅ #721 (slice 1):

Under **F693-02-contact-led-random-bobble**, Jack directed on September 15, 2026 **contact-based general bobble direction with a small random directional variation**, superseding the unaccepted no-random-direction proposal. Use actual incoming ball/contact evidence for the baseline and resolve a retained directional variation once per failed-acquisition event through authoritative seeded randomness.

Under **F693-02-bobble-direction-spread**, Jack selected on September 15, 2026 **up to 30 degrees either side of the contact-derived horizontal local-bobble direction**, a 60-degree total spread. This replaces the proposed ±15-degree bound. Retain one event-side sampled offset; distribution, contact baseline/fallback and other trajectory values remain pending. The angle does not set distance, speed or vertical motion. ✅ #721 (slice 1): `handling.bobbleSpreadDeg` 30 — the ball's horizontal travel turned by one seeded draw (`Match.RollSpreadDeg`); a ball with no travel spills away from the body.

Under **F693-02-expanded-ordinary-error-outcomes**, Jack directed on September 15, 2026 that **ordinary errors can also let the ball get past the defender or deflect onward into the outfield, especially on hard-hit infield balls**. A local bobble remains one outcome, not a universal nearby-stop rule. Preserve real contact and continuous motion; a hard-hit/infield label alone does not qualify a new error roll. Actual trajectory and coverage determine the destination and runner result.

The classifier, retained-motion physics and inheritance of stun, braking, recovery reliability and directional randomness for these new branches remain pending. An untouched geometric miss is not automatically a stat error; catcher passed-pitch/scoring rules are outside this batted-ball decision. No runtime or human gate changes here. ✅ #721 (slice 2):

Under **F693-02-error-outcome-selection**, Jack approved on September 15, 2026 **current ball motion and actual contact as the basis for local versus continuing ordinary error outcomes**, without an independent severe-error roll. A knockdown can remove speed; a glancing touch can leave the ball travelling; an untouched geometric miss retains its trajectory under existing rules.

Preserve the approved handling-error eligibility/chance and directional variation, with no glove/range manipulation or automatic failure from a hard-hit/infield label. Numerical classification, retained-speed physics and new-branch reaction/recovery/randomness inheritance remain pending. No assigned outfield destination, base award, runtime change or human gate pass. ✅ #721 (slice 2):

Under **F693-02-continuing-error-reaction**, Jack approved on September 15, 2026 **the same .40-second reaction for a qualifying contacted ordinary handling failure that sends the ball onward**, with **no added error stun for an untouched geometric miss**. Actual touch alone does not qualify a failure; preserve the reviewed handling opportunity and failed result.

Start at failed contact, keep grounded ordinary braking within the timer, and do not scale or refresh it by handling quality or escape distance. Helpers remain live; selection cannot bypass the affected character's gate. Untouched misses retain any independent dive/landing/special restrictions. Continuing-error recovery eligibility, directional variation and airborne/committed motion remain separately reviewed. No runtime or human gate changes here. ✅ #721 (slice 2):

Under **F693-02-continuing-error-recovery**, Jack approved on September 15, 2026 **reliable legal recovery of the same continuing ordinary contacted handling error**, including another defender collecting it in the outfield. Distance, travel time, ordinary bounces and defender/selection changes do not add a new same-origin ordinary error roll. An untouched earlier miss grants no protection; the next actual first handling attempt retains its routine/difficult opportunity rules.

Require actual contact/readiness, retain independent reviewed effects, and end protection when the loose recovery phase genuinely ends. This is not a universal one-error-per-play cap or generic loose-ball immunity. Directional inheritance and numerical continuation physics remain pending; no runtime or human gate changes here. ✅ #721 (slice 2):

Under **F693-02-continuing-error-direction**, Jack directed on September 15, 2026 a **narrower ±15-degree horizontal random-variation trial for continuing deflections**, replacing the proposed shared ±30-degree limit. Local bobbles retain ±30 degrees. A continuing ball loses speed at contact but preserves more of its incoming direction than a substantially stopped bobble.

The contact-derived baseline must honor that forward bias; its exact mapping and total contact-plus-random deviation remain pending. Untouched misses receive no error-angle change. Retain one seeded directional result per actual failed-contact event. Numerical speed loss, distribution, baseline/fallback and vertical response remain open; no runtime or human gate changes here. ✅ #721 (slice 2):

Under **F693-02-continuing-error-speed-retention**, Jack approved on September 15, 2026 an initial **50–80% retention of actual pre-contact horizontal speed for continuing ordinary deflections**. Glancing contact retains more; stronger interruption within this branch retains less. Exact contact mapping and branch thresholds remain pending. The lower bound is not a floor for every failure: substantial knockdowns can use the local-bobble branch.

Use event-side speed, not bat exit speed, with no independent speed/severity roll or handling multiplier. Directional variation preserves the resulting horizontal magnitude. This is not an energy percentage or a complete vertical/contact model; no runtime or human gate changes here. ✅ #721 (slice 2):

Under **F693-02-uniform-error-direction**, Jack selected on September 15, 2026 **an even angular distribution within the accepted limits**, replacing the proposed triangular weighting. Local bobbles sample uniformly within ±30 degrees; continuing deflections within ±15 degrees around their reviewed contact-derived baselines. Equal-width angle intervals are equally likely, with no side/tactical bias.

Retain one authoritative seeded result per failed-contact event and no redraw for outcome classification or recovery. Untouched misses receive no error-angle modifier. This changes neither error chance nor speed, stun or recovery rules. Contact mapping and vertical physics remain pending; no runtime or human gate changes here. ✅ #721 (slice 1):

`(rng × 2 − 1) × spread`, one draw per failed contact, uniform; four bobbling seeds give four different offsets inside ±30° (`TheBobblesDirectionIsOneUniformDrawInsideTheSpread`). The ±15° continuing band is slice 2's. ✅ #721 (slice 2): the ±15° continuing band, one uniform draw per failed contact (`TheErrorsDirectionIsOneUniformDrawInsideItsSpread`: the four seeds that fail on the hot liner get past with four different offsets inside ±15°).

Under **F693-02-local-bobble-vertical-shape**, Jack approved on September 15, 2026 that an ordinary nearby bobble **spill down from actual glove contact with a low ground rebound**, without a default upward pop. Preserve real contact height and continuous motion; no fixed-height reset, ground snap or fake secured possession. Ball motion and the .40-second stun proceed independently, and eligible contact can recover before settling, including by a helper.

Keep uniform ±30-degree local direction and reliable same-origin recovery. This qualitative shape does not govern continuing deflections, untouched balls or specials. The rebound ceiling is accepted below; exact contact response, horizontal speed and settling distance remain pending; no runtime or human gate changes here. ✅ #721 (slice 1):

Under **F693-02-local-bobble-rebound-ceiling**, Jack approved on September 15, 2026 a **six-inch (0.50-foot) maximum rise for ordinary local-bobble ground rebounds**. Measure ball-center rise from supported ground-contact height to apex; do not cap initial contact height or absolute world height. Weaker impacts can rebound less or settle; never add energy to reach the ceiling.

This does not select restitution, a minimum bounce or a fixed bounce count, and does not apply to continuing deflections, untouched balls or specials. Recovery and the .40-second stun retain their independent rules. This is an authored trial, not measured Mario physics; runtime and human validation remain open. ✅ #721 (slice 1): `handling.bobbleReboundCapFt` 0.50 — the rebound speed is capped at √(2 g × 0.5), never raised to it.

Under **F693-02-local-bobble-restitution**, Jack approved on September 15, 2026 **35% of actual downward vertical impact speed as upward rebound speed** for ordinary local bobbles, reduced as needed by the accepted six-inch ceiling. The percentage describes vertical speed, not height or horizontal speed. Use shared authoritative ball physics with continuous contact; no new randomness or handling multiplier. The .40-second stun and eligible recovery remain independent.

The settling threshold, post-glove velocity, horizontal response and complete collision integration remain open. This is an authored trial, not measured Wii/GC restitution; no runtime or human gate changes here. ✅ #721 (slice 1): `handling.bobbleRestitution` 0.35 of the downward speed at each ground impact, then the ceiling.

Under **F693-02-local-bobble-settling**, Jack selected on September 15, 2026 a **three-inch (0.25-foot) predicted-rebound settling threshold**, replacing the unaccepted one-inch recommendation. At actual ground impact, apply the accepted restitution and ceiling, then settle vertical motion if the predicted next rise is three inches or less. Preserve horizontal motion under its separately reviewed collision/rolling response.

Do not snap an airborne ball to ground, require a bounce, stop the whole ball or award possession. The same-error identity, independent .40-second stun and eligible recovery persist through settling. This is an authored local-bobble trial; other ball/special contexts retain separate contracts and runtime validation remains open. ✅ #721 (slice 1): `handling.bobbleSettleFt` 0.25 — the rebound is zeroed when its predicted rise is three inches or less; the horizontal roll goes on.

Under **F693-02-local-bobble-glove-release**, Jack approved on September 15, 2026 that actual failed contact already classified as an ordinary local knockdown **absorbs vertical momentum to zero, then immediately falls under shared gravity**. Preserve the real contact position and height, with zero added hold and no secured-possession state. This does not zero horizontal motion or force continuing deflections into the local branch.

Subsequent local ground impacts retain the approved 35% restitution, six-inch ceiling and three-inch settling cutoff. The .40-second stun and eligible recovery remain independent. Horizontal response and complete collision integration remain open; this is an authored trial with runtime/reference/human validation still required. ✅ #721 (slice 1): `_looseVY` 0 at the contact, gravity from the next frame, the real contact position and height kept.

Under **F693-02-local-bobble-horizontal-cap**, Jack approved on September 15, 2026 a **six-foot-per-second ceiling on outgoing horizontal speed at ordinary local-bobble contact**. Weaker spills can be slower or stationary; no minimum kick is implied. Apply after actual physical outcome classification, preserving contact origin and the approved local direction. This is an event-side horizontal magnitude, not a total speed or fixed scatter distance.

Contact-speed mapping and ground response remain pending; continuing deflections and specials retain separate contracts. No runtime or human gate changes here. ✅ #721 (slice 1): `handling.bobbleCapFtPerSec` 6 — `s = min(0.20 × s_in, 6)`; a hot ball leaves at exactly 6.0 in the tests.

Under **F693-02-local-bobble-horizontal-retention**, Jack approved on September 15, 2026 **20% retention of actual pre-contact world-space horizontal speed, capped at six ft/s**, for ordinary local bobbles. Classify the physical contact outcome before applying `s_out=min(.20*s_in,6)`. Preserve contact-derived direction plus uniform ±30-degree variation, actual contact origin and zero initial vertical speed with immediate gravity.

Zero input stays zero, with no minimum kick, extra speed roll or handling modifier. Continuing deflections retain their separate response. Ground response and complete trajectory validation remain open; no runtime or human gate changes here. ✅ #721 (slice 1): `handling.bobbleRetain` 0.20 of the ball's actual horizontal speed the frame before the take.

Under **F693-02-local-bobble-ground-horizontal**, Jack selected on September 15, 2026 **90% retention of current horizontal speed at each actual ordinary local-bobble ground impact**, replacing the pending 80% recommendation. Preserve heading on flat stationary ground and apply once per real collision, including a collision that settles vertically. Never reapply per grounded frame or invent a landing for an already supported release.

Vertical response, error identity, independent stun and eligible recovery stay intact. Continuous rolling friction remains pending. This is an authored trial; runtime and human validation remain open. ✅ #721 (slice 1): `handling.bobbleGroundRetain` 0.90 once per real impact, the settling impact included.

Under **F693-02-local-bobble-rolling-deceleration**, Jack approved on September 15, 2026 **six ft/s² of horizontal deceleration while an ordinary local bobble rolls supported on flat ground**. Preserve heading and integrate continuously to zero: from rolling speed `s`, stop time is `s/6` and travel is `s²/12`. A 5.4-ft/s entry stops in .90 seconds over 2.43 feet if untouched; airborne travel is additional.

Apply the 90% impact retention once per actual landing, then rolling deceleration only over supported time. No reversal, fixed-distance teleport, wait-until-stopped pickup or new error roll. Other ball contexts retain separate contracts; full runtime/reference/human validation remains open. ✅ #721 (slice 1): `handling.bobbleDecelFtPerSec2` 6 while supported: a six-ft/s spill rests in 1.0 s over 3.0 ft of roll, 3.2 ft with the spill's drift in the tests.

Under **F693-02-continuing-error-vertical-retention**, Jack approved on September 15, 2026 that a continuing ordinary deflection **scales signed vertical speed by the identical contact-dependent 50–80% factor used horizontally**. Preserve rising/falling sign at actual contact, continuous position and the approved forward-biased horizontal heading with uniform ±15-degree variation. Gravity acts immediately; no separate vertical roll or added pop.

Local bobbles retain their different drop response. Exact contact-factor mapping and continuing ground response remain pending; this is an authored trial with runtime/reference/human validation still open. ✅ #721 (slice 2): the same factor on the signed vertical speed — a rising ball keeps rising (`TheHotLinersFailedTakeGetsPastAndIsRecoveredWithoutASecondRoll` reads the ball above its contact height), gravity from the next sample.

Under **F693-02-continuing-error-ground-response**, Jack approved on September 15, 2026 that continuing ordinary deflections **use shared ordinary batted-ball ground physics from their actual deflected state**, without added error-specific braking or local-bobble caps. Equivalent physical states on the same surface use the same bounce/roll response; keep error history separate so recovery of the same contacted error remains reliable.

Do not restore the original bat trajectory or invent a landing for an already supported ball. This selects a response family, not legacy numerical defaults; ordinary ground calibration remains F693-04 work. Runtime/reference/human validation remains open. ✅ #721 (slice 2):

Under **F693-02-arcade-fielding-simplification**, Jack directed on September 15, 2026 that fielding remain **simple cartoon baseball: resolve opportunities from explicit character fielding range, ball trajectory and readiness; make the glove pocket face the ball through presentation**. Glove mesh collision, pocket/rim/back eligibility, surface normals and detailed obstruction geometry are not required gameplay gates.

**Historical — F693-02-continuing-error-retention-curve; superseded as a required implementation contract by F693-02-arcade-fielding-simplification.** Previously, Jack approved on September 15, 2026 a **linear progression from 80% to 50% retained speed as normalized obstruction increases within continuing contacts**: `r=.80-.30*c`, giving 65% at midpoint. Apply the same factor to horizontal and signed vertical speed.

The physical obstruction metric and local/continuing boundaries remain unselected; the strongest continuing endpoint does not mean complete blockage. No added random severity/speed roll or handling multiplier. This is an authored interpolation, with runtime/reference/human validation still open. ✅ #721 (slice 2): the linear 0.80 → 0.50 progression is the trial anchor used, over the arcade obstruction (`1 − dist / window`) rather than a glove-geometry incidence.

**Historical — F693-02-error-contact-obstruction-basis; superseded as a required implementation contract by F693-02-arcade-fielding-simplification.** Previously, Jack approved on September 15, 2026 **three-dimensional contact incidence** as the obstruction basis: how squarely pre-contact ball motion relative to the moving contact surface meets its outward normal. For valid approaching nonzero relative velocity `u` and unit normal `n`, raw incidence is `clamp(-dot(normalize(u),n),0,1)`.

**Historical — F693-02-glove-contact-surface; superseded as a required implementation contract by F693-02-arcade-fielding-simplification.** Previously, Jack approved on September 15, 2026 a **smooth concave pocket with rounded rim as the glove contact-shape family**, matching the authored visible glove and remaining separate from character reach. Use the existing equipment/motion sources for a reviewed authoritative surface; decorative mesh detail does not directly define baseball collisions.

Under F693-04, Jack accepted **hard liners rewarding existing positioning over post-contact reaction** on September 14, 2026. A readable hard liner may pass before a fielder can reposition; grounders and flies retain their distinct pickup/throw and pursuit/catch opportunities. Do not make every contact uniformly forgiving. Catchability remains geometric, with no new positioning verb or change to D18 assistance. Reference-informed speed, trajectory, and timing bounds remain pending.

Under F693-07, Jack accepted **brisk routine dead-ball beats on the slower/more deliberate side**, with more emphasis for big moments, on September 14, 2026. The result and a brief reaction must register before the reset. Compare Wii and GameCube pacing before selecting durations. This applies after a play ends; live player decisions, D7’s pitch-pace hold, and the existing on-field presentation contract remain in force. Timing targets and human acceptance remain pending.

### 0.3 Fields — D21 (#814)

## 0.3 Fields — D21 (#814)

**Directions accepted; nothing built; no number accepted.** Before changing a park file's schema, the field boundary, the flight table's reach, a hazard, night, or how a park is drawn, read this section and the FD rows the change names in [the decision plan](../plan-fields.md). [The fields research](../research-fields.md) and [the implementation map](../plan-fields-implementation.md) are reference. Jack accepted all nineteen directions and one refinement on September 21, 2026.

- **Harbor is the default.** A park is Harbor plus the differences it names (FD-01, FD-03). The match plays on one resolved table, derived the way `RulesTable.AtLevel` derives the difficulty rung. Harbor's resolved table equals the global table bit for bit. No rule is chosen by a park id. ✅ the table (#827):

  The chompers no longer name a park: ✅ F4-a (#847) made them `chomper` rows in Funfair's own file and moved `ParkHazards` onto the hazard library's patterns, so nothing in `GrandSluggers.Sim` picks a rule by a park id.

  Full traction is a held trial, judged at the first greybox sitting. The body effect exists only where the response law is on. ✅ the rails, at parity: the zone map and the libraries (F3-b #846), the ball and both loose-ball models read the zone (F3-c #856), the body multipliers on every row, all 1.0 (F3-d #857). ✅ the first ground that differs: Crystal's `ice` row (F9-a, above).

  ✅ the fence (F2-c #874, FD-06-R1, FD-12): an optional `fence.points` block, one distance per bearing from home, each point a bearing, a fraction of the park's own three-post fence at that bearing and a height, each span a `walls.json` material; `AtBatResolver.FenceAt` reads it and every reader of `FenceAt` follows; Crystal is the one park that names one: its own three posts in thirteen points, every span `glass` (F9-a) (§6.1, §16). ✅ foul territory and depth (F2-d):

- **Night** (FD-11, FD-11-R2). Night keeps the stadium lights: play visibility is the day's. A park's night block may name hazard instances and look fields (the view outside the stadium) and nothing that changes the at-bat, the flight, the ground or the bodies; it is validated like the day block. Harbor's night is a look only. ✅ the block and the one resolution (F4-d, #895):

  T-H01 a grounder on Crystal's ice, T-H02 a fly past a freezer (a human catch with no slow on the catcher passes; a slowed catcher fails), T-H03 a grounder into a warp can and T-H04 a liner off a tree (the player's own glove takes the ball after the redirect or carom; the assistance's take, or a take before the hazard acts, fails). The mover (T-H05), the reward (T-H06) and the wall trait (T-H07) are planned rows, debt on #814.

## docs/spec/02-stats.md

### 2. Stats, in numbers

**#693 design amendment (F693-02-character-catch-range, September 15, 2026):** the target catch range is an explicit property independent of displayed Fielding; Field must not determine glove positioning/response. The historical Field-driven catch-radius model above needs reconciliation before implementation. The later F693-02-defensive-trait-mapping supersedes summary-only Fielding: Arm controls throwing and Fielding controls hands/recovery.

The four stats are shown on the character card. **Contact and Power are ratings of their own** (PH-15-R5), authored beside `bat` the way `arm` and `hands` are authored beside `field`: optional keys in `data/characters/`, 1–10 when present, and **an absent or 0 key tracks `Bat`**, so a roster that authors neither behaves exactly as it did before the split. `Stats.ContactAuthored` / `PowerAuthored` say which it was; neither flag is serialized. No shipped character authors a value today, and the values are Jack's to choose later. ✅ P2-a (#837)

- **Contact is spatial forgiveness** (PH-15-R7): it scales the cursor's barrel (§5.2), so a crossing further from the center still finds the bat. It does **not** buy per-character timing assistance. Since #860 it does not touch the timing window, and the barrel is all it does (§5.3; #887 removed the split window that widened with Contact). ✅ P2-b (#844), shipped #860. The CPU batter's timing error is a separate thing that also reads Contact: it is that bat's *execution*, not its window (§5.9), and whether it should read Contact at all is P2-g's question.

**Velocity, Movement, Control and Endurance are ratings of their own** (PH-15-R6), authored beside `pitch` the same way: optional `velocity` / `movement` / `control` / `endurance` keys in `data/characters/`, 1–10 when present, and **an absent or 0 key tracks `Pitch`**, so a roster that authors none behaves exactly as it did before the split. `Stats.VelocityAuthored` … `EnduranceAuthored` say which it was; no flag is serialized. No shipped character and no trial overlay authors a value; the values are Jack's to choose later. ✅ P3-a (#890)

**Ordinary pitch repertoire (PH-02-R1/R2, PH-15-R1/R2/R4, #807).** Beside the stats, every character carries an ordered repertoire of exactly three ordinary pitches: the **fastball every pitcher throws**, then two *different* families drawn from Changeup / Curveball / Slider / Sinker, in the order the decision register accepts (`PH-15-R2` for the seven captains, `PH-15-R4` for the eighteen role players).

`ContentDataValidator` **requires** the field on every row — the character loader ignores unknown keys, so a misspelled one has to surface as missing — and refuses a count other than two, the same family twice, a listed `fastball`, and any id outside the four (`PitchFamily`, `Repertoire`; `RepertoireTests` holds all 25 rows to the register). ✅ #807 — **membership only**; §4.3 says what flies.

Family ids are a **different namespace from the Star Pitch ids** in `data/abilities/star-skills.json`, which already spells two of its own skills `fastball` and `changeup` (§13). A character's `starPitch` is never resolved against its repertoire and the two sets are never validated against each other: Boom's star pitch is spelled `fastball` while his ordinary repertoire is slider + sinker. ✅ #807

## docs/spec/03-at-bat.md

### 3. The at-bat state machine

**FLIGHT, What may happen.** Ball travels rubber → plate in `AirSeconds` (≈0.69–1.28 shipped; D7 **wait** — called in the #346 sitting before retuning). The floor `pitching.flight.airMinSec` scales with the mound: 0.78 s × 53.78 / 60.5 ≈ 0.69 s, so on the 53.78-ft mound a pitch reaches the floor only above ~109 mph (it was ~96 mph at 0.78 s) and every real pitch keeps its speed difference. Pitcher steers break (stick L/R).

- **The ordinary pitch is selected in SET, before the charge, and the charge locks it** (PH-02-R3/R4/R5). One button cycles the pitcher's three ordinary families in repertoire order — fastball → second → third → fastball (§2, §4.3) — and a family with no authored row in the active `pitching.families` table is **skipped**, so zero, one or two presses always land on a pitch that can fly (since #860 the shipped table authors all five, so every pitcher cycles three:

  Cycling before the pitcher-ready beat is allowed — a selection is not a delivery — but not while the swap pick is open. ⚠️ **sitting 1**: `PitchSelectionState` / `PitchSelection.Advance` beside `ChargeButton`, read through `Match.SelectPitch` / `Match.FamilyAt` against the current pitcher and the match's own table (S-101 … S-106, #812). The mound reads it every SET frame since #825:

  ✅ shipped since #860 (Jack accepted the `trials/pitch5` window on September 22, 2026 ("trial was good."); `CpuPitcher.PitchByInputs`, S-115 … S-120). #887 removed the `pitching.cpu.humanInputs` switch and the endpoint model behind it.

- **The box recenters after every pitch** (D12, #607); Down recenters early in SET; the pitcher's rubber persists. ✅ #607: every pitch's finish returns through `Match.AfterPitch`, which calls `ResetBatter` (take, swing and miss, foul, strikeout, walk, HBP, ball in play); `BatterContactOffsetX` still latches the walk the swing used (S-16 under D12).

## docs/spec/04-pitching.md

### 4.1 The four verbs

Same shape as the swing: tap / charge / modifier / star. Booklet-confirmed contract (issue #534).

| Cycle pitch | **West in SET, before the charge** | Steps the selected ordinary family — Fastball → second → third → Fastball, skipping what the active table does not author (§3, §4.3). One press is one step. The family **locks the tick the charge arms** and nothing is re-polled at release; every SET starts on Fastball. The changeup is a family in this cycle, not a held modifier: −20% mph, hangs then dumps late (§4.3), 10% of the break ⚠️ **sitting 1** (#825) |

Which ordinary family this delivery throws is chosen in SET before the charge and locked at the arm edge (§3, PH-02-R3/R4/R5): the sim step shipped in #812 and the mound reads it in #825, which retired the West changeup modifier and the row above with it. ⚠️ **sitting 1**

### 4.2 Location

  The arm rides on the delivery (`PitchCommand.Throws`, stamped by `Match.PreparePitch` from the pitcher). The sweep is **not scaled by the stick** and **not damped by a charge** (PH-05-R1); the player's steering **adds** to it under its own unchanged `breakMaxFt` cap, so the worst legal lateral movement is sweep + break. ✅ #818 (`PitchFlight.SweepShiftFt`; order of composition: shape → sweep → stick → star).

### 4.3 Pitch shapes

The hang is the row's `hangRate` (well below 1 so Y stays at or above the fastball until `hangUntil`); the dump is `dumpRate` (the rest of the drop to `dropFt`), and the validator refuses a row whose hang and dump never reach the aim in flight. A hangRate near 1 is a fade and is not a changeup (#668).

| Curveball | ✅ authored (#860; accepted in `trials/pitch5`, proposed by #818) | slower than the slider, faster than the changeup | The library's biggest `hump` then its longest drop; a small glove-side `sweepFt`; `offSpeed`. Numbers and measured margins: [`docs/research/pitch-families-p1d.md`](../research/pitch-families-p1d.md) |

| Slider | ✅ authored (#860; accepted in `trials/pitch5`, proposed by #818) | between the sinker and the curveball | The flattest of the three and the big glove-side `sweepFt`. Numbers: the P1-d report |

| Sinker | ✅ authored (#860; accepted in `trials/pitch5`, proposed by #818) | the fastest after the fastball | Rides the fastball's height longest, then dips; a small **arm-side** (negative) `sweepFt`. Numbers: the P1-d report |

✅ #810 (D20; `PitchFamilyTable.Of`, `PitchFlight.Shape`, `Training.PitchesOf(rules)` is the **loaded** table's authored rows in library order — all five on the shipped root, fastball and changeup on a table that leaves the three optional rows null; a practice session and the `GrandSluggers.Play` sandbox read it off the catalog they loaded, never the code defaults. ✅ #888, S-133).

**The shared ordinary pitch library** is five families, ids lowercase and closed: `fastball`, `changeup`, `curveball`, `slider`, `sinker` (`PitchFamily`, PH-02-R2). A character throws the fastball plus the two its repertoire names (§2, PH-15-R1). Since #810 these ids are **both** roster membership *and* what flies:

an authored `"slider"` in `data/characters/` is a family that character owns, and a `PitchCommand` spelling `"slider"` is that same family flying from its `pitching.json` row (authored since #860); a table without that row still stops loudly rather than throwing a fastball. The library is still not the star-pitch namespace (`data/abilities/star-skills.json` spells two of its own skills `fastball` and `changeup`; the two sets are never resolved against each other).

✅ #807 for the ids and the 25 assignments, #810 for the table they resolve against.

**Authored is a fact about the data, not about the code** (#818). The three rows are nullable: `pitching.families.curveball` (and `.slider`, `.sinker`) is present in the shipped table since #860 and has no code default, and `IsAuthored`, `Authored` and `Of` all read the table they are asked. A JSON `null` is not a way to be unauthored — absence is; a `null` there is refused as "must be an object". The validator checks every authored row, shipped or trial: the hang must reach the aim in flight, and a row that sweeps must say when the sweep starts.

**On the shipped root all five families are authored, fly and can be selected** (#860). Jack accepted the `trials/pitch5` window on September 22, 2026 ("trial was good."), and #860 moved the three rows #818 proposed there into `data/rules/pitching.json` unchanged.

✅ #810 built the table and moved the two original families onto it without changing one flight; ✅ #818 gave the other three numbers in the trial overlay first, together with the natural-sweep term; P1-c shipped the West selection before the charge and the charge-start lock (PH-02-R3/R4/R5) and #825 wired the mound to it.

### 4.4 Strike zone and judgment

- **Take in zone** = called strike. **Take outside** = ball. Judged at the plate crossing of the shown trajectory. ✅ (`StrikeZoneGeometry.Contains(Point(u=1))`; issue #535 covers any residual divergence)

  since #818 the crossing carries the family's own drop and natural sweep, so a ring there names the selected family on a screen both seats read, which is not the rubber tell. **Practice and the Tutorials keep the full crossing ring, through the flight** (PH-19, `SetTells.Locator`, gated on `TrainingOn || TutorialOn`): there the shape is the lesson and there is no opponent to leak it to. ⚠️ **sitting 1** (#825)

### 4.7 Stamina and the pitcher swap

- Costs (`data/rules/pitching.json` `stamina`): normal 4, charge +3, break +1, the family's own `families.<id>.staminaCost` on top (fastball 0, changeup 3 — §4.3, #810), star = the skill's `staminaCost` (`data/abilities/star-skills.json`, 8–22, read at runtime). **Only a pitch costs the arm** (PH-08-R3): a hit, a home run or a run allowed costs nothing past the pitch that was thrown. ✅ P1 (S-25)

  Confirming resets the pitch selection to the fastball — a new arm is a new repertoire and that path does not re-enter SET (§3, #825). ✅ P1 (S-26, #582)

### 4.8 CPU pitcher

**The CPU's pitch is built from the inputs a human has and nothing else** (PH-18, PH-18-R1, #823). Jack accepted the `trials/pitch5` window on September 22, 2026 ("trial was good."), together with the new shapes (P1-d) and the West verb (P1-f); #860 shipped it, and #887 removed the `pitching.cpu.humanInputs` switch and the endpoint model it kept reachable (an aimed crossing height and four exclusive verbs, what the CPU did before #860; the retired S-114 held it). ✅ #823, shipped #860

2. **No vertical intent exists.** Height is whatever the family gives (PH-03, §4.2): the crossing is the zone center less the row's `dropFt`, and nothing else in the model touches it. There is no vertical spread (`locations.middleYSpreadFt` went with the endpoint model, #887).

**And it is drawn as a held stick, not as a snap** (#825). The client starts a steered CPU delivery's drawn bend at 0 and walks it with the same `PitchFlight.BreakStep` a hand's hold walks, in the plan's direction, every flight frame — so the ball arrives at the command's `BreakReach` at the plate instead of leaving the hand already bent. The **judged** crossing is unchanged:

**Scatter is a legal mistake.** The Gaussian lands on the CPU's own rubber intent, in X only — there is no vertical input to miss in — and TIRED still widens it by `tiredScatterMul`. Fatigue lays no random miss on the delivery itself, the CPU's or a human's (PH-08-R1, §4.7). The rubber is the location on every pitch, so there is no separate walk to roll for (`rubberWalkChance` / `rubberWalkMax` went with the endpoint model, #887) — and, as a consequence, the CPU's rubber moves between most pitches, which the CPU batter's mistrack read (§5.9) sees. Reported, not tuned.

The per-family weights and `steerChance` in the shipped file are the **accepted** ones (#860, PH-20-R1): the numbers #823 proposed in `trials/pitch5`, with a rationale per row in [`docs/research/cpu-pitcher-p1g.md`](../research/cpu-pitcher-p1g.md), which Jack played and accepted. `chargeChance` is the port of the retired exclusive mix's charge share (the trial kept it). Every row weights all five families (S-120).

**Which row, and where** (both unchanged by #860):

## docs/spec/05-batting.md

### 5.1 The four verbs

| Slap | Tap and release RT | Full swing, widest window, base power. Plays the `swing-slap` take: no windup, compact (#613) |

| Charge | Hold to MAX (`swingChargeSeconds` 0.45), release in the MAX band | The same window as a slap (#860, PH-10-R1); the charge narrows the **cursor** instead, §5.2, PH-11-R1. More power (up to ×1.35 at MAX). Past the band the charge decays Plays the `swing-charge` take: the hold shows its windup, a bigger arc (#613) |

Charge at MAX is a *charge* tell (rings line up; the swing shows MAX, the pitch its booklet word "Nice!"). Words about the contact — PERFECT / NICE / SOUR — come only from the typed zone once the bat meets the ball; a miss shows STRIKE (#578). ✅ P1

A cancelled load returns the body to the charge take's rest at zero charge; an authored let-go take is Art (#943). Lesson T-B10 teaches it.

### 5.2 The cursor (sweet spot) — quality

- The **Contact** rating scales the barrel (`scalePerContact` 0.04 per point from 5) — this is the spatial forgiveness of PH-15-R7 and the only thing Contact is meant to buy; a charge **narrows** it (`chargeMul` 0.8; reference: charge zones are smaller than slap zones). **Runners on base never change the barrel**: there is no plate-level chemistry (PH-16-R14; #891 removed the good-chemistry slap widening, `buddiesOnBase.widen*`). ✅ P1 (S-11, S-30); ✅ P2-e (S-144); ✅ P2-a reads `Stats.Contact` (+ the bat's `contactMod`) rather than `Bat` (S-122)

- **This is the whole of what a charge costs and what Contact buys** (PH-11-R1, PH-15-R7): the timing half of both is gone, because every swing is judged in the one window of §5.3. The cursor numbers here did not move with #860. ✅ P2-b (S-125, #844)

- **The drawn oval is the judged oval** (P2-d, #889). One sim function, `SweetSpot.Oval`, returns what the client draws for a swing: the center from the box walk (`WorldCenter`) and the half-extents — tip, handle and the zone's half height — from `SweetSpot.SwingBarrel`, the barrel the resolver judges with (Contact + the bat's `contactMod` clamped 1–10, the charge as it stands with the Charge Bat as a MAX).

  `AtBatDirector.ShowCursor` hands the match's batter, bat, effective charge and box to it and `StrikeZone` draws the result; the client no longer clamps Contact or calls `BarrelScale` itself. Pinned across Contact 1–10, quick and charged, both hands, every bat and good-chemistry runners on base (which change nothing since #891, S-144), through the resolver: a crossing just inside the drawn line is Nice and just outside it is Sour. ✅ P2-d (S-134)

### 5.3 Timing — the window and direction

`err` = (press time − (ball-plate time − `window.leadSec`)) in frames at 60 Hz (D13, #612 / #670; `leadSec` 0.18, `AtBatMotion.SwingErrorFrames`). The take's `Contact` mark (`Motion.SwingContact`, 0.30 into the take) is an animation contract, not the judgment:

Outside the window the take plays at its natural 0.50 s and the bat misses the ball honestly. The warp is read at the press from the same window number the resolver judges (`Match.SwingWindowFrames`). ✅ #612 (S-07, S-08, S-09; `SwingPresentationTests`)

The take is the swing that is judged (#613): a charge (`ChargeFeel.IsCharge`, the same test that narrows the window) plays `swing-charge`, anything else `swing-slap`; both share the Contact mark and the measured approach and contact keys, so the warp above is one rule for both. Both end on a held finish at 0.60 that stays up through the STRIKE stamp until SET, or through the contact freeze until the batter-runner is `feel.swingFinishStepFt` out of the box (#583). ✅ #613 (`SwingPresentationTests`, `MotionTests`, the swing matrix `finish` beat)

- **Window** — **one window for everyone** (PH-10-R1, #844). ✅ P2-b (S-125 … S-127; `AtBatResolver.ContactWindowFrames`), shipped #860. The window is a total width: the bat is on the plane when |err| ≤ half of it. Outside it the bat is not on the plane: **miss**, strike. `window.frames` is the whole window for every hitter, a quick swing and a charged swing alike, and every human rung. **No Star Pitch changes the window** (PH-16-R1, PH-16-R18):

  a Star Pitch's challenge is its speed and its path, which the hitter can see, and it is judged in the same window as the plain pitch (S-190, S-191). The 5-frame floor still applies. No park or night term (FD-11-R2, F4-d #895): Crystal's night multiplier is gone on both roots.

  Jack accepted the `trials/pitch5` window on September 22, 2026 ("trial was good."), and #860 turned it on in `data/rules/batting.json`. The title's difficulty line reads `3 INNINGS · NORMAL · CPU SKILL` (#876), because the rung changes the CPU and never a pad's window.

  - **Retired by #887: the split window that played before #860** (`batting.window.shared: false`). Slap 9 frames, charge 7 frames (the reference), + (Contact − 5) × 0.4, × skill × park × the difficulty rung's `cpu.json` `humanWindowMul` for a pad's swing (EASY 1.3 / NORMAL 1.0 / HARD 0.9), floored at 5. The switch, `slapFrames`, `chargeFrames`, `framesPerContact` and `humanWindowMul` are gone from the data and the code; a table that still authors one is refused by name (S-126).

- **The stick at contact — an ordinary hit is geometry only** (PH-12, #855). ✅ P2-c (S-13, S-128 … S-132; `AtBatResolver.StickShapesContact`), shipped #883. A swing that is neither a bunt nor a Star Swing ignores the stick at contact: its direction is the timing, the zone's spread, the out-of-zone spread and the sour pull above, and nothing else. **PH-12** option C (direction-accepted September 20, 2026): timing, contact position, pitch shape and swing type decide the flight.

  A **bunt** reads its held side and never the stick (§5.8), and a **Star Swing** keeps both stick terms until Phase 6 reviews each one, so `spray.stickDeg` and `launch.stickDeg` stay authored. The CPU batter follows (§5.9). Jack played the P2-c stick trial in the `trials/pitch5` window (preview `4b47ad4e`) and accepted it on September 22, 2026 ("approve all"; PH-12 human-accepted), and #883 turned it on in `data/rules/batting.json` and retired `trials/pitch5`.

  - **Retired by #887: the stick on every swing, what played before #883** (`batting.geometryOnly: false`). Stick L/R at contact shifted the whole range by ±12° (`spray.stickDeg`) on every swing, and stick U/D moved the launch (§5.4). The switch is gone from the data and the code; a table that still authors it is refused by name (S-126).

### 5.4 Height and the stick

- **Launch** = base by **Power** (`Stats.Power` plus the bat's `powerMod`) and charge (`launch.loftBaseDeg`, `loftPerPower`, `charge.loftDeg`) **+** the pitch height (`perFtOfHeight` per foot the crossing sits above the zone center: a low pitch launches lower) **+**, on a Star Swing only (until Phase 6), stick U/D at contact (`stickDeg`): **Up = over the top = grounder**, **Down = under = lift**. The reference maps up→grounder, down→fly; that was the rule for every swing until #883, and #887 removed the switch that kept it reachable. ✅ P1 (S-06)

- **An ordinary swing's launch has no stick term** (since #883; §5.3, PH-12): it is the Power and charge loft, signed pitch height and the noise. Ordinary sour contact uses the same continuous formula. A low pitch still launches lower than a high one and a charged swing still lofts more than a quick one (S-129); a Star Swing without an authored launch keeps the term until Phase 6 (S-131). ✅ P2-c (S-13, S-128, S-129)

- Sluggers' "scatter hit" (right stick at contact) was our stick L/R until #883. The reference guide calls it unreliable. An ordinary swing has no scatter hit at all (PH-12); a bunt and a Star Swing keep the stick L/R.

### 5.5 Exit velocity

- **No `buddies` term.** Good-chemistry runners on base used to multiply a charged swing's exit (×1.10 / 1.25 / 1.50, `buddiesOnBase.*Mul`) and widen a slap (§5.2). Jack removed plate-level chemistry (PH-16-R14): runners on base change neither the barrel nor the exit. ✅ P2-e (#891, S-144)

- The Charge Bat gives a manual-MAX charge for free and keeps the narrow charge zones and the charge window off; it is never worse than a manual charge. ✅ P1 (S-30). Since #860 its **window** clause is moot — nobody has a charge window to be spared — and its **spatial** clause is the whole item: it keeps the wide slap zones on a MAX charge, which is exactly the half PH-11-R1 says a charge trades. ✅ P2-b (S-30, S-125)

### 5.8 Bunt

- Bunt fielding: P, C, 1B, 3B charge (§7.3). Runner rules: sac bunt is a live play, not a table. ✅ #625: the square is a typed fact on the swing (`SwingCommand.SquareSec`, how long the bat had been squared at the plate time); the defense reads it before the pitch (§7.3). No CPU fielder yet moves by the side.

**Client** (P4-c). West square toward third, North toward first, at the Input System's bunt button press point, on each seat's own pad; West no longer bunts. At the plate a squared bat is `PlateButtons.HeldBuntAtPlate`. The batter card names the held side (`BUNT 3B` / `BUNT 1B`, `BroadcastHud.BuntTell`) for a human and for the CPU. The bat does not yet angle toward the side: the one `bunt` take has no side, so the bat-angle cue is Art (#943). Lessons T-B07 (either side, two strikes) and T-B11 (first-base side, runner on first).

✅ Sim. ✅ P4-c client: bindings, leak guards, editor gates (compiled; not run: Personal Unity cannot batchmode), book pair, lessons. ⚠️ The bat-angle cue (#943). ⚠️ Human gate: sitting 4.

### 5.9 CPU batter

**Audit, #892 (what the CPU batter read before the commit read).** `DecideLeadSec` names *when* the CPU commits and `AtBatMotion.CommitCpuSwing` clamps the bat to that instant, but it never decided *what* was read: `CpuBatter.Swing` read `PitchFlight.Crossing(pitch)` (u = 1) of whatever command it got.

both read the hidden destination. The commit read closes the second case and makes the first explicit. The timing source is unchanged and already keys to **Contact**, not Bat: σ = (11 − Contact) × `errorFramesPerBatStat` × `timingSigmaMul` (P2-b re-reads it; #892 decides nothing).

| Runner on 1st, 0 outs, Bat ≤ 5, trailing by ≤ 2 | | Sac bunt 35% — ✅ #625: the square is read **at SET** (`CpuBatter.SquaresBunt`, once per pitch, the tell a human pitcher sees before the pitch, §7.3); at the plate plane a strike is bunted and a ball is taken with the corners already in (`batting.cpu.sacBuntSquareSec` is the headless square; the client passes its own clock). The side is drawn with the square (§5.8) |

**Which trait each CPU read takes** (PH-15-R5; the rows above name `Bat` because it is the number a reader recognizes, and every trait equals `Bat` until one is authored). ✅ P2-a (#837, S-122)

**The CPU batter's aims** (PH-12, PH-18, #855). No human's stick shapes an ordinary swing (since #883), so the CPU batter holds none either: an ordinary CPU swing passes 0 / 0 and draws neither Gaussian (PH-18). A CPU Star Swing still draws a spray aim `Gauss() × spraySigmaDeg` (12°) and a launch aim `Gauss() × launchAimSigma` (0.45), and the sac bunt draws no aim and no timing error: it is the held bunt toward the side it drew with the square (§5.8).

Until #883 every ordinary CPU swing drew both aims; that reseeded every shipped game from its first ordinary CPU swing, so `cli match --seed 7` and the S-29 cohort moved with #883 ([`promote-stick.md`](../research/promote-stick.md)). #887 removed the switch and moved nothing. ✅ P2-c (S-132)

**The CPU batter's timing σ is execution skill, not the window** (#844). `(11 − Contact) × errorFramesPerBatStat × timingSigmaMul` is how far off the ball this bat *arrives*; `batting.window` is how far off the ball a swing may be and still meet it. They are two different things that both count frames, and the one window (#860) touches only the second: every hitter is judged in it, and the CPU still swings with its own error inside it.

## docs/spec/06-ball-flight.md

### 6.1 Flight

  Gravity, the shared stretch and the plate height are global in every park (D21), and the ground and the wall are not in this block (F3-b, F3-c). ✅ #827 (`RulesTable.AtPark` → `FlightRules.WithEnvironment`; `ParkEnvironmentTests.SF10_ThickerAirIsAShorterCarryOffTheSameLaunch`, `…SF10_AParkAtWindMulZeroFliesTheStillAirPath`). ✅ the reach of that table is whole (F3-a2, #838, PR #840):

- Bounce: restitution 0.48, horizontal 0.82; shallow impacts skid (0.28 / 0.93), blended by incoming angle as above. Roll friction 22 ft/s². Rest at 1.4 ft/s. Wall carom: normal × 0.48, along the wall × 0.82. ✅ Every one of these is a ground row or the wall row since F3-c (#856): `data/rules/grounds.json` (the same numbers in `grass`, `dirt`, `ice` and `ash`) and `walls.json` `padded`, read off the zone under the ball and the segment it meets (next bullet); `flight.json` no longer carries them.

- **The ground under the ball (FD-05).** ✅ F3-b (#846) built the two closed libraries the numbers above will move into — `grounds.json` (`grass`, `dirt`, `ice`, `ash`) and `walls.json` (`padded`) — and the zone map that says which row a point is standing on:

  a park's ground is `infieldDirt`, `outfield`, `warningTrack` and `foulApron`, taken from the shared diamond's own geometry (the lip, the track, the chalk), each naming a row, each derived from the park's `surface` unless the park's optional `zones` block overrides it. **Every row is today's number and no park overrides a zone**, so the ball rolls, hops and caroms exactly as it did. ✅ **F3-c (#856) moved the reads.**

  the overthrow (a sailed throw, a lob dropped at an uncovered bag, the shipped fumble's scatter — `BallFlight.OverthrowTick`) slows at the row's `overthrow.decelFtPerSec2`, and the local bobble (#721, `BallFlight.LocalBobbleTick`) rebounds at the row's `bobble.restitution`, keeps `bobble.groundRetain` of its roll per impact and slows at `bobble.decelFtPerSec2`; the third ground model, the deflected ball, was already the batted ball (`BallFlight.Continue`).

  Above, between the poles → home run. Bounce then over → ground-rule double. Any touch of a foul wall, or over one, → foul, dead. ✅ P2 ⚠️ #814 audit: that last sentence holds only for a ball not yet decided. A ball already fair caroms off the rail and stays live, and a fair ball that then clears the rail is a ground-rule double (`BattedBall.cs:131-170`); the code is the intended rule and this sentence is too coarse.

  The drawn rail ramps from 4.2 ft to the fence from 95 ft out while the sim rail stays 4.2 ft to the pole (`HarborWall.cs:177-191`, `FieldBounds.cs:159`), and the drawn wall loop mirrors right field onto left (`HarborWall.cs:99-101`), so a lopsided park draws a wall the ball does not meet. ✅ F2-a (#826, PR #833):

  the foul wrap, rail, flare, backstop and dugout pad are no longer Harbor's literals — they are `boundary.json` (§16) read through a `ParkBoundary` value type, `FieldBounds` and the park validator's fence floor name no park's class, and the polygon cache keys on the whole edge so a per-park foul area cannot be served a stale polygon. Every value is the one that shipped and the polygon is unchanged vertex for vertex (`SF-08`); the flare and the offset stay #732's to choose.

  ✅ F2-b (#845, PR #848): **the mirror is gone**. The drawn loop walks the whole boundary — each side's own fence through `FenceAt`, that side's foul wrap and the backstop — on the spray grid `FieldBounds` samples, so every drawn vertex lies on the clip polygon (worst 5e-15 ft) and a lopsided park draws its own left field (`SF-05`). The three symmetric parks draw bit-identical loops, so nothing changed on screen; the drawn-loop cache now keys on the whole edge, as the polygon cache does.

  ✅ F2-b2 (#873, PR #875): **the ramp is gone.** §5 **Q5** was answered (Jack, September 22, 2026, FD-06-R2): the sim's rail is the rule. Each drawn span now stands at the top of the flight segment it lies on, read at each of its two ends (`HarborWall.FlightSpan` / `SpanTops`, read from `FieldBounds.Of(park)`, not recomputed), and a drawn vertex at the tallest segment that meets there (`HarborWall.Height`).

  Before, from 95 ft out (`RampStartZ`) the drawn rail rose to the fence by a smoothstep, and the last 31–34 ft of rail before each pole (11–12 ft on the compact field) was drawn at full fence height, because "outfield" meant within half a degree of the line. No flight, boundary or rule number moved. ⚠️ Look gate at review (Jack): the foul poles at Harbor and the five parks. ✅ **F2-c (#874): the polyline fence** (FD-06 C, FD-06-R1, FD-12 B).

- **Ball ground shadow** (#645): a dark translucent disk follows the live ball's current X/Z through flight, bounces and throws; held and hidden balls have no disk. It clears the field skins and follows the mound. Both seats and every captain use the same presentation, including HUD-off. The yellow/red landing ring remains the predicted catch/landing cue.

  In `data/feel/table.json`, `ballShadow` starts at **5 ft diameter** at ground level, shrinks linearly to **3 ft at 60 ft high**, and stays at least 3 ft; **0.65 opacity**, **0.04 ft lift** above the highest flat skin or mound. These are original readability values, not measured Sluggers dimensions. ⚠️ Implementation and geometry tests on the #645 branch; Jack's standalone visibility gate remains open. Reference:

## docs/spec/07-play-types.md

### 7. Play types — what happens on each

- Camera: `diamond` 45° on the dirt under the ball; a liner sits between that and the fly (`diamond-line`); fly pulls back to `diamond-fly`; a throw does not cut behind the thrower (`data/feel/shots.json`). ✅ #665

### 7.3 Bunt

- Defense tell: the batter squares at the bunt press (§5.8); 1B and 3B **crash** (charge 25 ft toward the plate), 2B covers 1B, SS covers 2B, P and C charge the triangle. ✅ #625 (`BuntDefense`, `fielding.bunt`):

- **Fields**: earliest of P / C / 1B / 3B. ✅ #625 (`FieldingResolver.BuntPursuitPositions`; the routes start from the square's bodies, so the crashing corner beats the pitcher to a bunt down its line that would have been the pitcher's from the rest spots).

- **Runners**: batter runs; forced runners go (sac). Runner on 3rd with a squeeze: goes at contact only if the offense sent them (`send 3B`), else holds. ✅ #625 (`BallSituation.Bunt`: the CPU runner from third holds until a glove has the ball; the send is the human's stick).

- **Throw**: 1B (batter) by default. Lead runner if the bunt is popped or too hard and the margin is makeable. Runner from 3rd on a squeeze → home only if inside 30 ft. ✅ #625 (`LivePlaySystem.CpuDecide`, the bunt rows ahead of §8.8 rule 1: home only from inside `fielding.bunt.squeezeHomeFt`, the lead force only on a bunt at or above `hardExitMph`, else first; a popped bunt is a pop, §5.8, and the doubled-off race is its row). The throw to first lands in the second baseman's glove: the bunt cover map (`BuntDefense.CoverMap`) is the diamond's with the middle behind the crash.

### 7.6 Line drive

- **Caught**: out. The glove on the live ball before the bounce is the catch, not a scoop at the landing ring (#666). Runners who left the bag are **doubled off** if the fielder throws to that bag (or steps on it) before they return (§10.5). Runners at the read step are safe if they return in time — that is the tension.

- **Not caught**: it bounces or skids from its actual impact; the outfielder whose route meets the roll earliest chases it (D16, #667), not the body nearest the bounce; runners as §7.5.

### 7.9 Wall ball / carom

- A fly or liner that meets the fence below fence height caroms (restitution 0.48, angle mirrored) and drops at the base of the wall. The outfielder plays the carom (route to the first reachable point on the post-carom path). ✅ P2 (`BattedBallClass.Wall`; `LiveEvent.WallCarom` is the thump the client plays; S-58). ✅ The carom's two numbers are the row of the wall material the segment is made of (FD-06, F3-c #856):

  `walls.json` `padded` (0.48 / 0.82); the same carom off the `padded` row at two values follows each (`GroundReadTests.SF12_…`). ✅ Per span (F2-c #874):

- Rob: in the window at the wall, West (jump) with Super Jump / Clamber / Buddy Jump can catch a ball that would clear the fence by ≤ the ability's rob height (§8.4). ✅ P2 (`FlyCatch.CanRob` against `BattedBall.FenceClearFt`; S-56, S-57). ⚠️ F2-c (#874): `BattedBall` still measures that clearance against the park's `fenceHeightFt`, not the top of the span the ball crossed. No park names points, so nothing plays differently; the first park whose spans stand at other heights (or the child that makes a span robbable) moves that read to the crossing's top.

### 7.11 Foul ball

- Stamp FOUL when dead, within the count hold (S-24b). The play never hangs: a foul is a dead-ball result the sim commits itself (#575). ✅ P2

## docs/spec/08-fielding.md

### 8.1 Bodies and positions

- Nine positions from `Diamond.Positions` (feet), seven of them read from `fielders.json` (#725); the pitcher is the rubber `infield.json` names and the catcher is `HomeSet.CatcherZ`. Defensive alignment is the lineup's glove diamond (Offense / Defense Setup), not roster order. ✅ P2: `Team.Gloves` carries the diamond and `FieldingResolver.Assign(team, pitcher)` reads it; after a pitcher swap (§4.7) the old pitcher takes the vacated glove (S-26). Preset teams with no diamond stand in roster order.

- Speed in the field: `chase = 12.4 + Run × 1.12` ft/s (floor 4.72; a Run-5 body runs 18.0 ft/s), **one formula** for human and CPU, with a 0.20-s ramp from rest and a 0.10-s brake (§0.2). ✅ P4 (`FieldingResolver.ChaseSpeedFt` with the dash multiplier; the stick table is gone). An **outfielder on a ball hit in the air** (fly, liner, pop, wall ball) runs it × `fielding.chase.outfieldAirMul` (**0.6**, #609) — human stick and CPU alike.

  An **infielder under a fly or a pop** runs at the one speed (`fielding.chase.infieldAirMul` **1.0**; it was 0.45 on the full-size field, #636). An infielder on a liner (its own clock: the rope gets past the glove or it does not, §7.6), balls on the dirt, carries and loose balls run the one speed. ✅ #609, #636 (`ChaseSpeedFt(fielder, pos, preview)`). ✅ #719 (slice 1, Jack's scope addition of 2026-09-17):

  the `c80` copy carries both multipliers at **1.0** — one pursuit profile per body, in the air as on the dirt (F693-02 pursuit consistency); the shipped table keeps 0.6 / 0.45. ✅ #715 (3d, 2026-09-18):

  Every step the body takes is slowed — chase, carry, cover, cutoff and backup walks, a runner's run along his path — and nothing else about it (reach, read, throw, catch). The planner does not foresee it: the preview, the CPU's route and every arrival estimate read the full speed. Burrow is never slowed. ✅ F4-b (#896, `StatusVolumeTests`).

  The **heart swing** (a special, §13) still slows every chaser × 0.45 for the whole play, exactly as shipped; a pursuit step it already slows is not slowed again by a volume (one slow, never two). There is no universal East-held sprint (`dash.chaseMul` 1.0). ✅ #718 (slice 3, F693-02-ball-dash-carrier):

  The pursuit planner charges half the ramp of the zone the body starts in, so the plan and the body agree. Each multiplier scales a time, never the rated speed, the asked velocity or the heading: the body always goes where the stick points, and a number above 1 only makes it slower to answer. The law's switch is still the table's (`accelSec` or `brakeSec` above 0); the game plays it on (0.20 / 0.10), so the ground rows can act on a fielder (implementation map finding 8). ✅ F3-d (#857):

### 8.2 Who is on the ball

- On contact the sim plans a route per candidate to the **landing** (fly) or the **first reachable point** (roller) and picks the earliest meet, then shortest travel (`FieldingPursuit.Better`). A liner still up that this body cannot catch at the plant uses the roll after the hop, not the bounce: RF starting closer to a right-center hop does not beat CF to the wall. Ties: CF over corners, SS over 2B, infielder over pitcher. ✅ #667

- **A ball nobody can reach.** When no sample is reachable, an infield body (P, C, 1B, 2B, 3B, SS) plays the ball where it passes closest to it, at catch height before the bounce or scoop height after it, while the ball is still coming. It never runs out to where the ball will come to rest; the outfield takes the ball on the grass (§8.9). Once the ball has passed, the body chases the smallest miss. An outfielder has nobody behind it and always chases the smallest miss, which is the roll (#667).

- **Reaction lockout** after contact before a body moves, by position: P 0.35, C 0.45, the infield four 0.25, **OF 0.40** (the reference's frames were P 0.42, C 0.67, infield 0.25–0.30, OF 0.83). The camera cut to the diamond happens at 0.42 (`feel.contactCutSeconds`, ✅ P8). Data (`fielding.reaction`). ✅ #718 (slice 1): the `c80` copy carries the four read clocks — P 0.35, C 0.45, the infield four 0.25, OF **0.40** — the largest single change in the compact profile. ✅ P4:

  every body, the human's stick glove included; the pursuit planner counts the lockout in its routes, so the glove picked at contact is the one whose body gets there first. The infield lockouts, the one glove speed (§8.1) and the CPU throw delay (§8.8) are the numbers the §10.4 double-play rows were tuned on; neither P7 nor #609 moved them.

  - **Who waits what** (sitting 2026-09-12, #609): the lockout is play seconds after the crack. The **human seat's glove waits the reference** at every difficulty rung; every CPU-driven body waits it × `cpu.reactionMul` (easy 1.4, normal 1, hard 0.8). A **ball in the air caps the ordinary read at its hang; pitcher delivery recovery keeps its full minimum** (its landing instant), so no body is still frozen when the ball comes down; a ball on the dirt passes no cap.

    ✅ #609 (`FieldingResolver.ReactionLockouts(rules, mul, airHangSec)` / `CpuReactionLockouts`, `LivePlaySystem.ReadyAt`; `OutfieldReadTests`: CF moves at 0.83 s on a routine fly for either seat, LF moves at 0.83 s on the quickest liner that reaches its grass (1.43 s at Harbor — a 1.1 s liner lands on the dirt), 0.83 s for the human glove and 1.17 / 0.83 / 0.67 s for the CPU glove on easy / normal / hard).

  - **#636 (D17 in the air) moved it once more.** With the hand-off honouring the infielder's route in the air, the infield ran back under every short fly past the lip (0.20 → 1.18 outs a game on balls landing 155–200 ft) and the fifty seeds fell from 2.62 / 2.58 (main at #642) to 1.92 / 1.76. The lever is the infield's reach under the stretched clock:

- **Facing** (sitting 2026-09-12, #611 / #576): a body **turns and runs**. Moving faster than a walk (`CartoonJuice.WalkFtPerSec`), it faces its own velocity; planted or holding, it faces the ball (a ball within `feel.faceBallMinFt` 3 ft — overhead or in the glove — keeps the heading, so the ball dropping through the plant never turns it round); releasing a throw, it faces the target. The one named short-range case is the **backpedal**:

  A spin-check glove is not posed as a spin while it waits (the ability is the extra-base check, §13). ✅ #611 (`BodyFacing` / `BodyHeading`, drawn by `HeroActor.Place`; `BodyFacingTests`, S-576 counts heading changes under a routine fly: fewer than 3 after arriving inside the catch radius, planted for the last 0.4 s). A head look-at while running is not built.

### 8.5 Throws

  the number is data, the scenario is the contract. ✅ #722 (slice 1, F693-03-long-throw-numbers): the clock is `releaseSec + [dist / (baseFtPerSec × arm) + longThrowLossSec × (max(0, dist − range) / 80)²] / (chem × ability)` with `range = comfortableRangeFt + rangePerArmFt × (Arm − 5)`, one function for the live ball and both CPU estimates.

- **Chemistry** (systems.md, reference): good ×1.30 speed, purple laser, never to the cutoff. Bad: **slow, not random** — every bad-pair throw flies at × 0.90 and none slants (`chem.slantChance` 0; the full-size game slanted one in five, ×0.70 with a 10–14 ft miss). The roll is on the *input* (the throw's accuracy), the outcome is still the ball missing the cover. ✅ P4 (`fielding.chem.slant*`; the boolean `Error` and the Single conversion are gone). ✅ #722 (slice 1):

- **Relay by time**: there is no forced-relay distance (`fielding.throw.onTheFlyFt` 9999); the CPU relays when the relay arrives first by more than its rung's `relayBiasSec` (0.3 / 0.1 / 0 s), and the CPU's margin for such a bag counts the cutoff's reaction and arm. ✅ #722 (slice 2, decision 6 of #730):

- Abilities: Laser ×1.25 on a throw home with a live runner on third only; Snap Throw a 0.22-s release after a clean received throw, at ×1.0. ✅ ✅ #723 (F693-03-laser-throw, F693-03-snap-throw):

### 8.6 Errors

  the play is live and the runner gains that time. It **never converts an out into a caption** and is not by itself the error. ✅ P4 (`LiveEvent.Bobble`; the resolver conversion and the Unity re-roll are gone). ✅ #720 (slice 1): the knockback that rode beside the bobble — `fielding.knockback`, a stop off the contact's energy — is the shipped rule; the `c80` copy reads the ball's speed instead (`fielding.recoil`, the decisions above). ✅ #721 (slice 1):

- **Drop** (star effects: the heatball, the phony swing, the heart swing's frozen glove): a fixed drop chance on the catch is allowed for *skills* (burn-hop, phony) because the skill is the two-second rule; it is never allowed for plain baseball. ✅ P4 (`Match.RollDrop`, `fielding.drops`). ✅ F4-b (#896, FD-08-R1, `SF-22`):

- Stamp ERROR ✅ P4 (`PlayStamp.Error` on the hit a sailed throw allowed). The ERROR tell at the sail ✅ P8: `LiveEvent.ThrowSailed` pops the small ERROR sticker the moment the throw skips past (`PlayStamp.LiveTell`, the same rail as the small SAFE). ✅ #690: that live tell is the stamp; `ShowsAtTime` does not overlay a second ERROR card.

### 8.7 Cover, cutoff, relay, backup

  ✅ #718 (slice 1): two switches in `fielding.cover` — `lockoutMul` and `chaseSpeedWeight`, 1 / 0 shipped, which is D11 as written above; 0 / 1 with `startSec` 0 in the `c80` copy, so cover, cutoff and backup bodies walk at their own pursuit speed from contact with no read (F693-02-coverage-budget: covering is a known assignment to a fixed spot, not the recognition of where a ball went).

  `FieldingResolver.CoverSpeedFt`, one function for the live walks, the CPU's cover-arrival estimate and the bunt square's cover walk. **One read per play** ✅ #640 (PR #644):

  when second's cover is the one who would back up first or third, the pitcher backfills. Outfielders not on the ball back up the throw 60 ft behind its target. **On a bunt** (the batter squared, or the ball a bunt, §7.3) the map is `BuntDefense.CoverMap`: 2B first, SS second, the corners their own bags when not the glove, P backfilling; a cover already walking on the square keeps walking through the crack with no start delay. ✅ #625.

- **Relay**: each human relay leg needs its own throw command — the cutoff holds until RT is pressed, and an early press is remembered 0.25 s of active play and fires at the catch; the CPU continues by the decision table from the cutoff's spot with the cutoff's own arm. ✅ P4. ✅ #723 (F693-03-relay-ownership, -input-buffer, -throw-cancel):

### 8.8 CPU fielder decisions

✅ #625 adds the **bunt's rows** ahead of rule 1 when the ball is a bunt (§7.3): home on a squeeze only from inside `fielding.bunt.squeezeHomeFt` (30) and only when makeable or inside the close margin; the lead force only when the bunt left the bat at or above `hardExitMph` (36, too hard for the sac) and is makeable; otherwise the batter at first if makeable, else hold. The two-out rule and the doubled-off race stay ahead of it.

✅ #722 (slice 2) adds: the throw the table judges a bag on is the leg the CPU would take — straight or through the cutoff, on the one clock with the real arms (`LivePlaySystem.PlanThrow`) — and the throw-in with nothing makeable takes the same leg. The runner's read (§9.9) is the same plan.

### 8.9 Control: who you are on defense

a liner over the shortstop belongs to the outfielder whose route reaches it, and a slow roller belongs to the charging corner, not the pitcher who is closer but locked out longer. (Superstar assigns by hit class and area and yields to an outfielder who is within 20 frames of the infielder; the route planner subsumes both rules.) ✅ `FieldingPursuit.Choose`, S-31 / S-32, S-100 (#667).

**Taking the glove (D18).** In Exhibition the CPU runs the play glove until the human *takes* it: the calibrated stick passes `fielding.stick.enterMag` (0.20) on an armed seat, or LB is pressed (✅ #718 slice 4: the take is manual pursuit's entry, so on the `c80` copy it is the calibrated radial `fielding.stick.enterMag` 0.20 of an armed seat; LB takes regardless).

After a hand-off the previous body keeps its momentum for `chase.handoffCoastSec` (0.2 s, the FIFA "move assistance" idea) so the swap does not jerk; the new body answers the stick at once; a throw's release is not a hand-off that coasts (the thrower stays put, §8.5), and neither is a body in its dive — inside the dive's arm window, or still owing its recovery — because a diver is on the ground and its last frame is the lunge, not a run (`DiveHandoffCoastTests`, #715).

The stick take is not a re-pick either: it takes the body wearing the ring. ✅ events (`HandGloveTo` call sites), the route guard on the ground and the coast (`TryHandoffOutfield`, `TickHandoffCoast`, S-96, S-97, #633), the same guard in the air (#636: a liner SS reaches past the lip stays SS's and SS catches it, S-97's second row, both seats; the S-29 band is held by the infield's reach under the stretched clock, `chase.infieldAirMul` §8.1, not by giving the liner away).

This is the genre's "one button to nearest" plus direction, so the shortstop can be taken off a liner up the middle without cycling (the Superstar mod added exactly that). ✅ `FieldAssist.SwapGlove`, `SwitchHint`; the lock and the dead press with the ball on the batted-ball play, S-99 (#633); ✅ one rule for the runner play too (§11.3, §11.4):

refused with the ball in the catcher's or the receiver's glove, dead while the throw flies, live on a loose ball under the same lock; S-99 on the runner play in `StealScenarioTests` (#637, PR #642).

**No lock (D17), no auto-throw.** There is no hold-to-lock verb (Superstar's L, Power Pros' R1) because nothing auto-switches by distance. There is no auto-throw for the human seat at any difficulty (Sluggers Remote-only mode throws for you; we do not): the CPU throws only for CPU-owned gloves (#579). Difficulty changes the CPU's lockouts and margins, never who you are.

**What is not decided.** Whether the human may *refuse* the receiver hand-off (today the ring goes to the receiver at release; once the receiver has the ball the stick steers them and Select is dead until the next throw or a loose ball) — S-98 pins today's behavior. Whether Sluggers itself hands off mid-play is UNVERIFIED (the booklet only documents the switch button); the emulator study was **skipped by decision on 2026-09-13** — the genre rule and the sitting decide.

Tracking: #633 shipped S-94 … S-99 in the harness with the ground route guard and the coast; #636 (the guard in the air — shipped, the band re-held by `chase.infieldAirMul`), #637 (Select on the runner play — shipped: `SelectTakes` is the one predicate both plays read), #634 (the sixteen unnamed scenario ids).

## docs/spec/09-baserunning.md

### 9.4 Dash, slide, rounding

  The slide's length is `slideFt` × the `body.slideMul` of the ground the bag stands on, for the automatic slide and the forced one alike (`RunnerSystem.SlideFt`, FD-04 B); ✅ F3-d (#857), **1.0** on every row. It is not behind the response law, so it acts on both roots.

  The run-through's length is `overrunFt` × the `body.overrunMul` of the ground first stands on (`RunnerSystem.OverrunFt`, FD-04 B); ✅ F3-d (#857), **1.0** on every row, on both roots. Every other body stops on the bag it is stopping at; a runner who overruns any bag is FD-04 C, held.

### 9.5 Fly balls and tag-ups

  ✅ #732 (decision 5 of #730): **a tag-up is a race, not a distance.** At the catch — once, `RunnerAiContext.AtCatch` — a runner on third or second also goes when `margin(next)` (§9.9, the arm and the relay read since #722) clears `running.cpu.tagUpHomeMarginSec` / `tagUpThirdMarginSec` plus the rung's `runnerMarginSec`.

### 9.7 Rundowns

  a throw that races nobody to its bag, with every moving body ≥ 80% of the way to a bag, is a lazy lob (reference). ✅ P5, reread by #640 (PR #644):

### 9.9 CPU baserunner decisions

| Fly ball | hold; tag-up rules §9.5 | ✅ #732: the tag-up is `margin(next) > tagUp*MarginSec + runnerMarginSec`, read once at the catch |

the glove on (or the route to) the ball, `running.cpu.reactionSec` 0.35, then `InPlay.ThrowSec` over the distance. ✅ #722 (slice 2): the runner reads the defense's own plan through `BallSituation.ThrowClock` — as much of the thrower's arm (`cpu.*.runnerReadsArm`), of the relay the fielder would take (`runnerReadsRelay`) and of the pair chemistry (`readsChemistry`) as the rung reads. Easy reads half the arm and no relay; normal and hard read the arm, the relay and the pair chemistry in full.

## docs/spec/10-outs.md

### 10.4 Double plays (ground ball)

| Runner on 1st, bunt popped up | C / P | catch | 1B (double off) | S-49 ✅ #625 (`BuntScenarioTests`: the squared bunt popped 29 ft out is the catcher's; the throw back to first lands in 2B's glove, the bunt cover) |

### 10.5 Doubled off and the tag-up DP

- **A firm catch is firm.** Once the batted ball is caught in the air it stays caught for the rest of the play. A glove that loses the ball afterwards — a throw that sails, a lob nobody covers, an item that knocks it loose — is possession changing, not the batted ball coming down. The retouch is owed only by a body that was off the bag **at the catch** (§9.5), and is judged **once**, on that catch. S-55b. ✅ P5 (`LivePlaySystem.UpdateFly`; #692).

## docs/spec/11-steals.md

### 11.3 Catcher throw play (after a take or a miss)

- **LB on the runner play** is the §8.9 rule, shared with the batted-ball play (`LivePlaySystem.SelectTakes`, `FieldAssist.SwapGlove`): with the ball in the catcher's (or the receiver's) glove a press is refused — the ring and the ball stay put and the throw still goes where the right stick says; while the throw flies it is dead; on a loose ball (a sailed pickoff, S-71) it takes the body the stick names under the one `chase.swapLockSec` lock, which also holds the nearest-body hand-off and the CPU walk, as on a batted ball. ✅ #637 / PR #642 (S-99 on the runner play, `StealScenarioTests`).

### 11.5 Scenarios

| Pickoff at 1st, runner departed during SET | Existing position and return direction survive; tag at 1B or 2B / rundown by geometry | S-69 ✅ (#640, PR #644: the one cover read; the Run-8 body is tagged at second on the throw ahead, a Run 2–4 body through the rundown) |

| Pickoff throw sails (bad chem; a bad pair never slants, #722, and no pickoff of 400 sails with Vale on the mound, so the S-71 and S-99 rows give the pitcher an authored Arm of 1, #715) | Ball live; runner advances (ERROR) | S-71 ✅ |

## docs/spec/12-chemistry-stars-items.md

### 12. Chemistry, stars, items in play

- **Items are dormant** (PH-16-R15, #891). The on-deck item offer is removed: a good-chemistry on-deck hitter no longer offers an item, and nothing else does, so no item is thrown in a game (S-145).

  The item code and data stay, not deleted — `ErrorItems`, `batting.items`, `Match.ApplyOffenseItem` / `ThrowItem` / `CpuWouldThrowItem`, `LivePlaySystem.LandItem` / `TickItems`, the tutorial `item-effect` objective and the clients' item controls — and wake only when a new item source sets `AtBatResult.ChemistryItemOffered`. The item lessons (T-X01, T-I-banana, T-I-rocket, T-I-pow) are blocked on #891. The rule below is how an item plays **if** one is offered.

## docs/spec/14-parks.md

### 14. Parks in play

**What a park changes today.** Its three fence posts, `fenceHeightFt`, `windMph`, `windDeg`, and its `hazards` — by day, and in its optional `night` block at night (§6.1, `Models.cs:186-211`; F4-d #895: the night window `nightContactWindowMul` is gone on both roots, FD-11-R2). `surface` is read by the validator and by presentation only; it changes no play. ⚠️ D21 gives it a meaning through ground zones.

**How a hazard is decided today.** `ParkHazards` runs once, in `FieldingResolver.Preview`, against the ball's **landing point**, for the ball redirect, the reward target and the catch stealer. ⚠️ D21: hazards act live on the ball or the body that touches them. ✅ F4-b (#896, FR-07): the **status volume** is live — each frame, before anybody moves, the live ball tests every fielder and every live runner where the last frame left it against the park's volumes (`ParkHazards.StatusVolumes`, `BodySlows`); nothing reads a landing point to slow a body.

**What decides which rule runs.** ✅ F4-a (#847, FD-09, FR-08): a **closed pattern library**. `data/rules/hazards.json` carries one authored row per hazard type — the twelve ids of `HazardType`, the spelling a park file uses — and each row names its **pattern** from the closed set `statusVolume | ballRedirect | rewardTarget | catchStealer | wallTrait | decoration` (`HazardPattern`) plus the numbers that are the type's own.

`HazardLibraryTests` carries the pre-#847 type-string code verbatim as an oracle and compares the two over every hazard in every park, by day and by night, including the warp's seeded draw.

| The same three | ✅ F4-b (#896, FD-08-R1): the park's `fielding.drops.frozen` roll (0.4) is **retired**. A glove a volume slowed is decided by the glove and the ball (`SF-22`). The table's one remaining reader is the heart swing's frozen glove (a special, outside D21), left exactly as it was. Removing the park's draw changes the count of draws on the match's one stream at Crystal and Ember, which reseeds the plays after it (measured below). | ✅ retired for park hazards |

**Freeze volume, lava pit, fire breath (statusVolume), What happens today.** ✅ F4-b (#896; FR-07, FD-08-R2): **live, per body**. A body — fielder or runner — whose position at the start of a frame is inside the disc (the instance's radius × the row's `nightRadiusMul` at night) and was not at the last read has touched it: it runs at `fielding.chase.frozenMul` (0.45, unchanged, "3. keep.")

**Where a hazard may stand.** ✅ F4-e (#862; FD-19 B, FD-19-R1; `SF-23`). The content validator — so `cli art` and every `ContentCatalog.Load` — refuses a hazard whose disc crosses one of the four running lanes (home–first, first–second, second–third, third–home), the mound-to-plate lane, a bag pad, the mound or the plate area, on both data roots, and names the park file, the hazard's index and type, and every piece of ground it crosses with the depth in feet, deepest first (`HazardPlacement`,

it is how near a *ball* must land for the can to take it (#732), not ground a body stands on, and FD-19 keeps shallow ball hazards legal. **The disc is the one the instance plays, by day and at night** (✅ F4-d #895, map finding 31): a day instance stands at night too, so it is measured at the larger of its radius and its night disc (`ParkHazards.NightDiscFt`, its row's `nightRadiusMul`), and a night-block instance at its night disc; where the two differ the refusal names both.

**The eight volumes moved (FD-19-R1, Jack, 2026-09-22).** Four status volumes sat on a running lane on the shipped root and four on `trials/c80`, one of them on second base's pad; nobody noticed, because no body touches a hazard. They are **five shipped rows**: Crystal's deep volume had to move for its trial twin's sake, and Crystal's first volume's twin moved with it.

**Night** (FD-11 B, FD-11-R2). ✅ F4-d (#895): night keeps the stadium lights and changes no rule of the at-bat, the flight, the ground or the bodies. A park file may carry an optional `night` block that names hazard instances only (validated like the day's, by the type library, the strict schema and the FD-19 placement rule; any other key is refused by name); the look at night is not a park key but the night row of the park's sky and light in `data/art/looks.json` (F6-c).

"definitely drop it"), so a Crystal night plays its day game for game. ✅ #828 (PR #834): `cli match --night` plays a night game and `cli match --cohort park-factors` reports every park day and night; a report, not a gate (FD-13).

**Hazards off** (FD-10 B, `SF-24`). ✅ F4-h (#858): a match option, default on — `Match(..., hazards: false)`, the same last argument on both `Match.Exhibition` overloads, and `cli match --hazards off` (the header line prints `hazards on` / `hazards off`). Off, the match plays the catalog's park with every **hazard instance** removed and every other member the same value: size, fence, walls, air, wind, ground zones, foul territory and depth.

`statusVolume`, `ballRedirect`, `rewardTarget` and `catchStealer` go; `wallTrait` stays (a climbable span is a property of the wall, FD-06, so a Clamber fielder still robs at Canopy) and `decoration` stays (it does nothing in play, and the kit still draws it). That split is contract work FD-10 left open and F4-h wrote, confirmed by Jack (FD-10-R1: "4. a", September 22, 2026); it lives in that one list. **Night:** the switch removes instances only. ✅ F4-d (#895):

on the shipped root (measured on `11fbbe2e`, after the promotions in #871 and #886) the run factor with hazards off reads Canopy 1.00 → 0.95, Crystal 1.06 → 1.05 by day and 1.07 → 1.06 at night, where its night window still applies, Ember 1.10 → 1.04 and 1.13 → 1.04, and Funfair 1.16 → 1.22 and 1.17 → 1.22. Every factor, runs and home runs, on both roots, moves by at most 0.09, inside the 0.15 the report itself calls noise at fifty games a cell. Rooftop does not move at all, on either root:

**Measured.** With the same matchup and seeds, the shipped parks run from 1.00 (Harbor) to 1.35 (Funfair) on runs and to 1.45 (Crystal) on home runs, and the order changes under `trials/c80` ([fields-park-baseline.json](../research/fields-park-baseline.json), one matchup, fifty seeds, day only). ✅ #828 (PR #834):

## docs/spec/15-presentation.md

### 15. Presentation contract per play

every stamp in the table is `PlayStamp.Label(PlayEvent)` over the typed outcome — the outs made, `DefensiveFeat` (buddy jump, the wall robs, a plain JUMP, a DIVE, set at the catch), `Error`, `FieldersChoice`, `RunnerResult` — and the client calls nothing else; the contact word is `PlayStamp.ContactTell` over the typed zone and the release tell is MAX / Nice! alone (#578). ✅ #690: one tell per live event, when it happens, at a named `BroadcastHud` anchor — never a center card.

✅ P8 (cameras), D14 (#610): the in-play beat is `PlayCamera.LiveBeat` over typed live state — steals and pickoffs use ordinary live ball-follow, a home run smashes at the crack, every other hit holds the SET shot for `contactCutSeconds` (0.42, `table.json`, the §8.2 cut), then the close play, the rundown, and the class read off the typed hit (`AtBatResult.Class`). **An ordinary throw is not a beat**:

the follow stays on the ball; chemistry tints that ball, never a predicted line through the dirt (#689). `PlayCamera.LiveFraming` translates the named shot onto the bag, the batter, or the dirt under the ball and carries the shot's `blend` (`Framing.Blend`; 0 is a cut, which the rig honors on the live path), and `PlayCamera.CameraHold` keeps a target (a beat, and a close-play bag) for `cameraHoldSeconds` (0.25, `table.json`) so a flicker does not re-aim (`CameraDirector.Live`;

the client owns no Vector3). A routine grounder is SET, one cut, one follow, SET. ✅ #665: a liner cuts to `diamond-line` (same 45°, between hopper and fly), never `diamond-fly` or `diamond-grounder`. ✅ P8 (bodies): `PlayOutcome.Bodies` is every glove and live runner where it stood at Time (`LivePlaySystem.BodiesNow`, read before the field resets); the result beat draws those, so the catching fielder stays where the catch happened and a third out does not swap the defense on screen (#574).

✅ #606 (HUD): the mini diamond draws every live runner at `Runner.Pip` — the segment from the last bag toward the next and the fraction run along it (`Baserunning.PathPip`), the batter-runner included, a return as the fraction falling, the run-through as a fraction past 1 on home → first — through `BroadcastHud.Scorebug.Runners` and `Baserunning.DiamondPip(from, to, u)`; a seated runner is fraction 0, on the bag. The selected runner keeps the larger pip; one pad and two draw the same.

| Liner | `diamond-line` (a cut) after the contact cut, same 45° as the hopper, closer than `diamond-fly` so the rope reads vs a bounce (#665) | `solidFreeze` | OUT (DIVE / JUMP) at the glove when caught; OUT at each bag as it happens; SINGLE / DOUBLE / TRIPLE at Time |

| Throw | no camera move (D14): the follow stays on the ball; nothing cuts behind the thrower. Chemistry tints the ball trail, never a predicted line through the dirt (#689) | — | (the arrival decides) |

**One field kit** (FD-16, FR-13, FR-05; ✅ F6-a #859, PR #863). Every park draws the same diamond, rail and wall, read from the geometry owner.

Unity has no EditMode test assembly, so the drawn loop is pinned through the sim loop it is read from (`HarborWallTests`, `SF-05`). The dress no longer builds a backstop or a dugout (✅ F6-a2 #881):

## docs/spec/16-data-tables.md

### 16. Data tables (rails)

Only entry points (the CLI, the Unity bootstrap, tools, tests) and the one shared diamond (`Diamond`, `ParkDiamond`'s dress, `ParkBoundary.Default`) read the process table found from the data root, and a process that finds no readable root stops. **Any new rule number is a field here with a validator, never a C# literal.** The infield joined them in `infield.json` (#711):

every park shares one diamond, so it is one global set loaded once and read-only thereafter — nothing may change it mid-run, because tests execute in parallel. The ground that dresses that diamond joined it in the same file (#729):

the drawn grass diamond (`innerHalfFt`) and the back arc of the dirt (`backArcFt`) are measured from the bags and the rubber, so they travel with them — a deliberate exception to "presentation stays in `data/feel/`", because a picture measured from geometry is not free to stay behind when the geometry moves. The seven fielder starts followed in `fielders.json` (#725), for the same reason and on the same terms:

### Trial overlays (#716)

## Trial overlays (#716)

**Retired:** `trials/c80` (#715–#723). The compact profile — the 80-ft field, the heavier ball, and the reads, clocks and throws derived to match them — became the shipped game when Jack decided to promote it on September 22, 2026 (3e, the ordinary loop only; §0.2 holds its contract). Every value it carried is in `data/`, and the folder was deleted.

**Retired:** `trials/pitch5` (P1-d, P1-g, P2-b, P2-c). It carried the duel trial until #860 moved it into `data/rules/` after Jack accepted the window on September 22, 2026 ("trial was good."), and then the stick trial (`batting.geometryOnly: true`, #855) until #883 moved that too after Jack accepted it the same day ("approve all"). Its last file would have equalled the shipped one, so #883 deleted the folder. Reports: [`docs/research/promote-duel-trial.md`](../research/promote-duel-trial.md), [`docs/research/promote-stick.md`](../research/promote-stick.md).

**Park files (`data/parks/*.json`) are held to that standard.** ✅ F1-a (#820, PR #831): a key a park file does not declare is an error that names the file and the key, in the park object and in a hazard row alike (`ContentDataValidator.UnknownKeys`, the shape `RulesValidation.UnknownFields` uses), on the shipped root and on a trial root. The dead fields are resolved:

✅ F3-a (#827) — `RulesTable.AtPark(park)` beside `AtLevel`, and with it the first park field this section owns, an optional **`environment`** block (`dragMul`, a multiplier on the root's `flight.drag`, refused at or below 0 and above 4; `windMul`, which replaces `flight.windMul`, refused outside [0, 1]; both optional, absent meaning the global number, and an unknown key inside the block refused by the same strict read).

✅ **F3-b (#846, PR #850)** brought two of those libraries and the park's second optional block, **`zones`** (`infieldDirt`, `outfield`, `warningTrack`, `foulApron`; each optional, each naming a row in `grounds.json`; an unknown key or an unauthored id refused by the same strict read).

Which zone a point is in is the geometry the field already had — the `flight.classes.infieldLipFt` lip, `ParkDiamond.TrackWidth` inside `AtBatResolver.FenceAt`, the ±45° chalk — **read, not moved** (#730 / #732 keep those numbers), and `GroundZones.ZoneAt(x, z)` is the one function F3-c and F3-d call. The map is resolved from the park beside the table rather than hung on it, the way `ParkBoundary` resolves the edge:

`AtPark` hands back the global table *itself* for a park that names no air (`SF-01`), and a per-park map on that table would have made every park's table a copy. What is still open: a span naming its own wall material (F2-c). ✅ F3-c (#856) moved the readers onto the rows: the ball and both loose-ball models read the zone under the ball (`GroundZones.RowAt`) and the carom the segment's material (`WallMaterial.OfSegment`), and the flight's and the fielding table's copies are gone.

✅ **F2-c (#874)** brought the park's third optional block, **`fence`** (FD-06 C, FD-06-R1, FD-12 B): `points`, from the left-field pole to the right, each `{ bearingDeg, fenceFrac, heightFt, material }` — the bearing in the spray frame, the distance as a fraction of the park's own three-post fence at that bearing (fence-relative, so the one block serves both roots and lands where the migration rule puts it, within the posts' own rounding), the top in feet (unscaled),

a material with no row (`SF-03`) or one on the right-field pole's point; an unknown key in the block or a point. It is carried onto `Park` as the last, defaulted member, so a park that names none writes nothing into the trace identity. **No shipped or trial park names one.** ✅ **F4-d (#895)** brought the fourth optional block, **`night`** (FD-11 B, FD-11-R2):

| `flight.json` | gravity, drag, the shared `timeScale` (§6.1), plate height, `windMul`, sample rate; `classes` (the §6.2 table: topper / grounder / chopper / liner bands, the chopper's hop, the infield lip), `deadBall` (homer trot, foul flight hold). The `carry` hit bands are gone (P4): the bodies decide the bases. `bounce`, `skid`, `roll` and `wall` are gone too (F3-c #856): they are the ground rows in `grounds.json` and the wall row in `walls.json`, read off the zone under the ball and the segment it meets, on both roots |

**pitching.json, Sections.** `speed` (the Pitch-stat coefficient every family shares), **`families`** (one named row per pitch family, all five authored since #860 — `mph`, `chargeMph`, `hump`, `hangUntil`, `hangRate`, `dumpRate`, `dropFt`, `sweepFt`, `sweepFrom`, `breakDamped`, `staminaCost`, `offSpeed`; §4.2, §4.3, #810, #818), `release` (Nice!

band and ×), `flight` (release hand, `AirSeconds` scale and clamps, break cap / ramp / damping / rate), `starShapes` (heat, prism, charm, phony, cask wobble), `stamina` (the pool, the pitch costs every family shares, TIRED threshold, tired aim wobble, the CPU swap lead), `cpu` (the CPU pitcher's table, §4.8, PH-18-R1, #823:

rolls; and `pickoff`). #887 removed `humanInputs`, the four exclusive-verb weights, `rubberWalkChance`, `rubberWalkMax` and `locations.middleYSpreadFt` with the endpoint model that read them. The rubber walk distance is geometry (`HomeSet.PitcherWalk`)

**batting.json, Sections.** `window` (**`frames`**, the one window for every hitter, swing and rung (§5.3, PH-10-R1, #844), then floor, square fraction, `leadSec` the square press before the ball's plate time, D13; #887 removed the `shared` switch, slap / charge frames and per-contact), no stick switch (#887 removed `geometryOnly`:

**grounds.json, Sections.** The closed **ground library** (FD-05, F3-b #846): one named row per ground a park's zone may name — `grass`, `dirt`, `ice`, `ash`, exactly the ids a `surface` has always been allowed to be. Each row carries `roll` (`friction`, `restSpeed`), `bounce` (`restitution`, `horizontal`, `minVy`), the whole `skid` block (its incoming-impact blend band included:

a `Dictionary` would bypass the reflective range walk, the unknown-key refusal and the JSON = code parity test (FR-02). A ground with no row is a stop that names the id and the file (`SF-03`), in a park's `surface`, in a park's `zones`, or asked for in code. **This is the only copy** (F3-c #856):

the batted ball, the overthrow and the local bobble read the row of the zone the ball is in through `GroundZones.RowAt`, and `GroundLibraryTests` pins every row to the shipped numbers written in the test (FD-05). `trials/c80` carries no copy — no zone differs yet, so an overlay would be answering a question nobody asked. The first unequal row is a trial (Crystal, F9-a). ✅ F3-d (#857):

**walls.json, Sections.** The closed **wall-material library** (FD-06, F3-b #846): one named row per material a fence span may name. One row, `padded`, carrying what `flight.wall` carried (0.48 / 0.82) — every span of every park is that wall today, and since F3-c it is the only copy: the carom reads the row of the segment's material (`WallMaterial.OfSegment`, `padded` for every segment until F2-c). No `climbable`, no `robFt`:

how fast it slows is the ground row's, F3-c), `catcher` (where the catcher holds the ball behind the plate, the CPU release), `chem` (good speed, the slant), `bobble` (the glove's help to Hands and the fumble's stun), `recoil` (the impact recoil off the ball's speed, #720), `handling` (the awkward hop, the chance curve and the local bobble's spill, stun, ceiling and settle, #721;

the bobble's restitution, ground retain and rolling deceleration are the ground row's since F3-c), `park` (`shellWarpChance` alone since #847 — a star swing's flag, not a park hazard's number)

**hazards.json, Sections.** One authored row per hazard type (§14, FD-09, FR-02, FR-08; F4-a, #847): `freezeVolume`, `lavaPit`, `fireBreath`, `warpPipe`, `barrel`, `billboard`, `climbWall`, `chomper`, `statue`, `train`, `acUnit`, `tree`.

5.6 on `trials/c80`) and, on the three `statusVolume` rows only, `slowSec` (how long a touch slows the body that made it — **3.0** on both roots, Jack's number, FD-08-R2, F4-b #896; `[Optional, Positive]`: every volume must author it and no other row may). `nightOnly` left the rows with F4-d (#895): an instance is night-only because its park authors it in its `night` block.

A number its pattern never reads is refused, the way a sweep with no start is. **The numbers moved, they were not chosen**: #730 / #732 still own the pad and the multiplier, and `fielding.chase.frozenMul`, `fielding.drops.frozen` and `stars.gains.billboard` stayed where they were because a star swing sets the same slow. `trials/c80` carries a copy for the pad's sake

**cpu.json, Sections.** `level` (the default rung) and the `easy` / `normal` / `hard` rungs: timing-σ ×, reaction × (CPU batter σ and tracking, close-play reaction, catcher release, the fielder's throw delay), mistrack ×, makeable margin (§8.8), perfect-steal chance and pickoff chance (§11.6, §4.5), `runnerMarginSec` (§9.9). No rung touches a pad's swing window (PH-17; the title names the rung as CPU SKILL, #876; #887 removed `humanWindowMul`).

**infield.json, Sections.** `baselineFt`, `moundFt`, `cornerFt`, `secondFt` — the diamond every park shares, read by `Diamond` (#711) — plus `innerHalfFt`, `backArcFt`, the ground that dresses it, read by `ParkDiamond` (#729). The corners are authored, not derived: a 90-ft baseline rotated 45° is 63.6396…, and the diamond has always played at a rounded 63.64.

Outfield depth and the foul area are **not** here — those vary per park (#713; accepted as D21, FD-07, not built) — and neither is the park environment, which is the park file's own optional `environment` block (FD-03, #827)

**boundary.json, Sections.** `foulOffsetFt` (36), `flareStartFt` (95), `railHeightFt` (4.2), `backstopZFt` (−36), `dugoutPadFt` (18) — where the field ends, read through `ParkBoundary` by the flight's clip polygon (`FieldBounds`), the drawn kit (`HarborWall`) and the park validator's fence floor (#826). These were `HarborWall` constants until F2-a: a class named after one park decided the foul wrap of all six, and the edge could not be changed without editing code (FD-07, FR-05).

One global set for now, like `infield.json`, and a value type rather than five `const`s so that a park may name its own foul area later (F2-d) and the polyline fence (F2-c) has somewhere to land; the polygon cache keys on the resolved edge, so a second edge cannot be served the first one's polygon. **The numbers moved, they were not chosen**: #730 / #732 own the offset and the flare until they close.

`trials/c80` deliberately carries no copy — whether the compact profile scales the offset or the flare is #732's question, and an overlay would answer it by accident. `railHeightFt` is authored as 4.2 and the geometry stands at `(float)4.2`, which is what the `float` constant it replaced always gave (`ParkBoundary.RailTopFt`); widening it moves the polygon and belongs to whoever owns the value

**fielders.json, Sections.** `first`, `second`, `third`, `short`, `left`, `center`, `right` — where each glove stands with nobody on, in feet from home, read by `Diamond.Positions` (#725). `xFt` is signed (the left side of the field is negative), `zFt` is out from home. Seven, not nine: the pitcher stands on `infield.moundFt` and the catcher on `HomeSet.CatcherZ`, and naming either here would be a second source of truth.

One global set for all six parks, outfield included (#730 decision 3) — per-park depth is new behaviour and stays with #713 (accepted as D21, FD-07: the three outfield starts only, not built). A trial does not scale these: the infield four take the basepath scale, and the outfield three keep each body's bearing and its fraction of the fence **at that bearing**, through the circular fence arc (`AtBatResolver.FenceAt`).

## docs/spec/appendix-b-scenarios.md

### Appendix B — Scenario matrix (acceptance)

**Coverage on `f82f948` + #634.** Every row S-01 … S-93 carries its id in a test name in `src/GrandSluggers.Sim.Tests` (plus S-24b, S-55b and S-58b), and S-94 … S-99 are named by `ControlScenarioTests` since #633. The #634 skeptic pass re-read the sixteen rows the `a15f5f5` note listed as unnamed (S-02, S-08, S-12, S-38, S-49, S-52, S-53, S-54, S-61, S-63, S-69, S-70, S-71, S-74, S-78, S-81):

every one already carried its id; four were not their row and were rewritten or tightened — S-49 (a 2B catch at 63 ft and a tag at second; now a bunt pop the catcher takes and the force back at first), S-38 (a comebacker; now a grounder to SS met inside `running.cpu.infieldBackFt`), S-52 and S-69 (an out only if one happened; now the out and the arrival that made it). S-69 (#640) is green again on PR #644: the one cover read of a runner play and the geometric rundown.

S-97's in-air twin (the liner SS reaches past the lip) is green on both seats since #636. S-49 with the square (the crashing corners, the catcher's pop, the force back into 2B's glove) is `BuntScenarioTests` (#625).

S-101 … S-106 (plus S-106b) are named by `PitchSelectionScenarioTests` since #812, and S-107 … S-113 by `PitchFamilyTrialScenarioTests` since #818 — those seven measure the trial overlay `trials/pitch5` and assert relationships only, so a proposed number may move without editing a test.

S-114 … S-120 are named by `CpuPitcherScenarioTests` since #823 and read the same overlay the same way, with S-114 and S-120 standing on the shipped root; S-113's second clause was widened in #823, because the overlay now carries the CPU switch beside the three family rows.

S-124 … S-127 are named by `SharedWindowScenarioTests` since #844 and read the same overlay the same way, with S-124 standing on the shipped root; S-10 and S-30 gained trial halves in `AtBatScenarioTests`, and S-113's and S-120's overlay-file lists gained `rules/batting.json`, because the overlay is now the Phase 1 + Phase 2 duel trial. **#860** promoted that trial (Jack accepted the `trials/pitch5` window on September 22, 2026 ("trial was good.")):

every row with a shipped half and a trial half now asserts the accepted contract on the shipped root, and each former shipped half asserts the switch's **off path**, built in the test (`SwitchOffPaths`, or a copied root with one key changed) and never read from shipped data; rows that pinned "shipped root unchanged" now pin "shipped equals the accepted trial". **#887** removed the three switches (`batting.window.shared`, `pitching.cpu.humanInputs`, `batting.geometryOnly`) and their off paths:

the off-path halves are gone, S-114 and S-124 are retired rows, S-127 holds the repaired in-zone read with a catalog built in the test, and S-120 and S-126 now hold that a table authoring a removed key is refused by name. S-134 … S-137 are named by `CursorOvalScenarioTests` since #889 and stand on the shipped root. S-141 … S-143 are named by `CpuReadScenarioTests` on the shipped root; S-04 and S-28 have rows there too. Nothing in the matrix is open.

### B.1 Pitch and swing

| S-07 | Bat 5 slap, ball at the cursor center, press at plate − 0.18 s (err 0, D13 / #670) | | Perfect, straight to CF ± 8° |

| S-10 | Contact 1 / 5 / 10, quick and charged, at the window's edge; then a charmball | | All six swings are judged in the **same** window (`window.frames`): just inside its half none misses, just outside all do. The charmball is judged in the same window (PH-16-R18). No frame count is stored: the row reads the table it asserts about. The split window's Bat 1 charged + charmball half (the floor doing the work) was retired by #887. ✅ P2-b (#844); moved #860 — Jack accepted the `trials/pitch5` window on September 22, 2026 ("trial was good.") |

| S-13 | Stick up at contact, an ordinary swing | | The same swing with the stick up and with it centered gives the **same** launch, exactly — and the same ball (#883). No launch is stored. The off-path half (stick up ~12° lower, `launch.stickDeg`) was retired by #887. ✅ P2-c (#855). Moved #883 — Jack accepted the stick trial in the `trials/pitch5` window on September 22, 2026 ("approve all"). |

| S-30 | Charge Bat vs manual MAX vs a quick swing | | **Spatial clause:** the ball 0.4 ft toward the tip is Nice on a manual charge and Perfect with the Charge Bat, and the bat is never worse than a manual MAX for power. **Window:** all three swings share one window — the bat's "no window penalty" is moot because nobody has one — so just inside its half none misses and just outside all do. The split-window half (the bat kept the slap window) was retired by #887. ✅ P2-b (#844); moved #860 — Jack accepted the `trials/pitch5` window on September 22, 2026 ("trial was good.") |

| S-101 | All 25 shipped repertoires (both hands, captains and role players), under a table that authors all five families | 0 / 1 / 2 / 3 cycle presses in SET | Slot 0 / 1 / 2 / 0 — fastball, second, third, fastball in repertoire order, and the wrap (PH-02-R4/R5). ✅ #812 (`PitchSelectionScenarioTests`) |

| S-102 | Two presses, then the charge arms | Cycle pressed on every held frame and on the release frame; a quick release and a MAX release | The presses after the arm edge are ignored; the committed family is the one that was showing when the charge armed (PH-02-R3). ✅ #812 |

| S-103 | Fastball showing | Cycle and the charge press on one tick; and a press-and-release on one tick | Cycle first, then lock: the *next* family is locked and thrown. No press is dropped. ✅ #812 |

| S-104 | A real match: a delivery, then a foul, then a dead pickoff, then a swap to a pitcher with a different repertoire | — | Every SET entry and the new arm start at slot 0, unlocked, fastball; the commit tick resets by itself; `Match.SelectPitch` / `FamilyAt` follow the current pitcher and the match's own table, never `Rules.Default` (PH-02-R5, PH-15-R1). ✅ #812 |

| S-105 | Shipped root (fastball and changeup authored, #810): Rio (changeup + curveball), Vale (curveball + slider), then all 25 | Twelve cycle presses | An unauthored family is skipped — Rio cycles fastball ↔ changeup, Vale never leaves the fastball — and no press, for any repertoire, ever selects a family that cannot fly. ✅ #812 |

| S-106 | Third pitch selected, charge held | The swap pick opens mid-hold (the button stops accepting and discards the charge), then a press while it is open, then the pick closes; and a cycle press before the pitcher-ready beat | The lock releases and the slot is kept; the press while the pick is open is ignored, not banked; cycling before the ready beat moves the selection and arms nothing. No pitcher cancel is added (PH-02-R3). ✅ #812 |

| S-106b | Third pitch locked by a live charge, then the SET frame that would have seen the release returns early (the button disarms, the selection step does not run) | A cycle press on the next tick, then a fresh charge | The stale lock is read as unlocked: the press cycles instead of being swallowed, the next charge locks afresh (`LockedThisTick`) and throws what it locked. A lock lives only while the charge is armed; a genuinely armed charge still ignores presses. ✅ #812 |

| S-107 | The shipped root (the trial overlay `trials/pitch5` until #883 retired it), loaded in process; every Pitch stat, charge and release | — | Speed order fastball > sinker > slider > curveball > changeup, and every new family between the two shipped ones — so its air time is between theirs and no pitch leans on `airMinSec` / `airMaxSec` (D7 untouched). ✅ #818 (`PitchFamilyTrialScenarioTests`); the values are accepted and shipped (#860) **#860:** the overlay no longer carried `pitching.json`, so S-107 … S-112 read the shipped rows — the accepted ones — and hold them to the same roles. |

| S-108 | Same overlay, middle of the rubber, no aim, no stick, no charge, both throwing hands | — | Every family in the library crosses **inside the strike zone** with a ball's radius to spare, and a batter standing where the box starts them has the crossing inside the nice oval, both batting hands (PH-03: nobody aims height, so an ordinary pitch has to be a strike). ✅ #818 |

| S-109 | Same overlay, the whole flight sampled | — | Three different height paths: the curveball carries the library's biggest `hump` and is the only family whose path climbs out of the hand; it travels farthest from peak to plate and drops the most over the last two fifths of the three; the sinker stays nearest the fastball's height through the first three quarters and then leaves it by more than either of the others; the slider strays least from its own chord. Every authored row still finishes its drop in flight. ✅ #818 |

| S-110 | Same overlay, both throwing hands, all 25 shipped pitchers | — | Sweep order slider > curveball > 0 toward the **glove side**, sinker toward the **arm side**, fastball and changeup exactly 0; the glove side is +X for a right-hander (first base's own sign). The sweep term mirrors **bit for bit** between the hands at every point of the flight, and so does the crossing from the middle of the rubber; off the rubber's middle the hands mirror about the unswept line to ~1e-12, because the flight rounds `base + sweep` once per hand. ✅ #818 |

| S-112 | Same overlay, both throwing hands, no stick and full stick | Charge 0 and charge 1 | With no stick the crossing is the same bits charged and uncharged, at every point of the flight: a charge never touches the family's own movement. With full stick the steering is `breakMaxFt` uncharged and `breakMaxFt × breakDampedMul` charged (PH-05-R1, PH-15-R6). ✅ #818 |

| S-114 | **Retired by #887.** It held the endpoint model on the switch's off path (`cpu.humanInputs` false): an aimed crossing height and four exclusive verbs, the same command stream per seed. The switch and the model are gone; the seed-7 log before and after #860 is in [`docs/research/promote-duel-trial.md`](../research/promote-duel-trial.md), and #887's seed-7 log is byte-identical to main's. | — | — |

| S-116 | The overlay, all 25 pitchers, 60 pitches each; then a table that authors two families with all five weighted (built in the test by dropping the three rows #860 shipped) | — | See note *S-116, Expect* below. |

| S-117 | The overlay, all 25 arms; then a stick held one frame at a time | — | `\|BreakX\|` is exactly `SteerDir × BreakReach(Pitch, airSec)` and never exceeds it; holding the stick frame by frame through `BreakStep` arrives at the same number for every Pitch stat and flight length. The bound bites at the air-time floor (`BreakReach(1, airMinSec)` < 1) and no real pitch leans on that floor, so on today's numbers every steered CPU pitch does reach the whole ±1 — **measured, not a target**. ✅ shipped #860 (the overlay now resolves to the shipped rows). ✅ #823 |

| S-118 | The overlay, the *even* row (0-0) and the *ahead* row (0-2), 4000 pitches each | — | Charge and steer occur at their rows' `chargeChance` / `steerChance` (± 0.04) and **co-occur at the product** (± 0.03): a charged pitch is still steerable, which the shipped model's four exclusive verbs never allowed (S-114). ✅ shipped #860 (the overlay now resolves to the shipped rows). ✅ #823 |

| S-119 | The overlay at 0-2 — S-27's twin — then the even row | 100 pitches each | ≥ 30 % cross outside the zone, and ≥ 30 % are outside **by X**: with no vertical intent left the waste has to come from the rubber, and the split by X vs by Y is reported (no ordinary family drops out of the zone on its own, so the whole waste is the rubber's). The even row still puts fewer than 10 of 100 down the middle. ✅ shipped #860 (the overlay now resolves to the shipped rows). ✅ #823 |

| S-124 | **Retired by #887.** It held the split window on the switch's off path (`batting.window.shared` false): slap / charge ± (Contact − 5) × per-contact, × star × park × rung, floored, over a 1 200-row grid, and the window half of S-122. The split window and its keys are gone. | — | — |

| S-125 | The shipped root (#860), every rung | — | See note *S-125, Expect* below. |

| S-126 | The shipped `batting.json`; a copied data root with one key or number changed | `geometryOnly`, `window.shared`, `slapFrames`, `chargeFrames`, `framesPerContact` added back one at a time; `frames` 4 | `trials/pitch5` is gone (#883). The shipped file authors none of the removed keys (#887), and a table that adds one back is refused by name ("not a rule this table owns"), never read. A window under its own floor is refused by name. ✅ P2-b; ✅ P2-c widened it; moved #860 ("trial was good.") and #883 ("approve all"), both September 22, 2026; switches removed #887 |

| S-128 | The shipped root (`geometryOnly`, since #883), loaded in process; one fixed pitch and crossing, one fixed timing error, one fixed box, one fixed seed | An ordinary swing — quick and charged, a right- and a left-handed hitter, Perfect, Nice and Sour contact — with the stick centered, up, down, left, right and on the four diagonals | See note *S-128, Expect* below. |

| S-129 | The shipped root | Early vs late; a low vs a high crossing; quick vs charged; a pitch in vs out of the zone | Timing and contact still shape the ball without the stick: early pulls and late pushes, mirrored by the batting hand (PH-10); a low pitch launches lower than a high one (`perFtOfHeight`); a charged swing lofts more than a quick one (`charge.loftDeg`, with the noise draw held); the out-of-zone spread still applies. ✅ P2-c. Moved #883 — Jack accepted the stick trial in the `trials/pitch5` window on September 22, 2026 ("approve all"). |

| S-131 | The shipped root | A captain's Star Swing (no authored launch) with the stick centered, up, down, left and right | The Star Swing still follows the stick: up launches lower than center, left and right move the spray. **Owner: Phase 6** — PH-12's scope is ordinary hits, and each Star Swing is reviewed there. ✅ P2-c. Moved #883 — Jack accepted the stick trial in the `trials/pitch5` window on September 22, 2026 ("approve all"). |

| S-144 | Both roots: a hand-built hitter at Contact 1 … 10, both hands, no bat and every bat item, with no runners and with one, two and three good-chemistry runners on base; charge 0 / just under the line / on the line / MAX; crossings across the barrel; one seed | `SweetSpot.Oval`, then the resolver | **No plate-level chemistry** (PH-16-R14): with good-chemistry runners on base a swing's oval and barrel scale are the ones with none, and the resolver returns the same quality, exit, launch, spray and carry for every crossing. ✅ P2-e (#891, `PlateChemistryScenarioTests`) |

| S-145 | Both roots: every character at the plate with every other on deck; then 5 three-inning CPU games (the S-29 pairs, one way, seed 1) | `ChemistryTable.ChemistryItemOffered`; `Match.AutoPlayGame` | **No item offer is ever made** (PH-16-R15): the table offers nothing for any batter / on-deck pair, the good-chemistry pairs that offered before included; in the games no at-bat result carries an offer and no item is thrown. ✅ P2-e (#891, `PlateChemistryScenarioTests`) |

**S-29, Expect.** Mean runs per side **1.8–5**, separately for home and away across the cohort (F693-06, retained by Jack September 14, 2026); doubles < singles; HR ≤ 2 per game mean; strikeouts and walks both present. ✅ P7 (`AtBatScenarioTests.S29`, in CI). #609: 2.2 / 2.6 runs, 3.2 singles, 1.9 doubles, 1.5 HR, 3.5 K, 2.9 BB per game on the shipped seeds, held by `fielding.chase.outfieldAirMul` with the outfield read at the reference 0.83 s (P7 shipped 2.5 / 2.7 with a 2.4 s read). #636:

2.6 / 2.3 runs, 3.0 singles, 1.7 doubles, 1.7 HR, 3.7 K, 2.8 BB with D17 in the air, held by `fielding.chase.infieldAirMul` 0.45 on flies and pops (main before it read 2.6 / 2.6; D17 alone 1.9 / 1.8). #667: 1.94 / 1.88 runs, 3.94 singles, 1.40 doubles, 1.28 HR, 3.18 K, 2.84 BB — CF meeting the wall instead of RF chasing the bounce converted doubles to singles; the test floor is 1.8 so the sitting is not undone to hold 2.2.

**S-111, Expect.** Positive coverable margin in every case, both throwing hands and both batting hands — the reach (walk in the flight that is left, plus the nice oval's half-width at the crossing's own height) exceeds the lateral distance to the crossing. The uncharged case is always the harder one, because a charge buys speed by spending steering (PH-04). Margins are in the report. ✅ #818

**S-113, Expect.** **Shipped (#860):** all five families are authored, in library order, and the CPU runs on human inputs; the SET cycle walks slots 0 → 1 → 2 → 0 for every repertoire, landing only on families that can fly. **Off path** (the code-default table, whose three optional rows are null): `curveball`, `slider` and `sinker` are unauthored, `Of` stops by name and names `pitching.json`, `Authored` is fastball + changeup, and so is `Training.PitchesOf` that table (#888).

✅ #818, amended #823; moved #860 — Jack accepted the `trials/pitch5` window on September 22, 2026 ("trial was good."); the overlay rows dropped when #883 retired `trials/pitch5`

**S-115, Expect.** `AimX` and `AimY` are 0 on every CPU pitch; `Throws` is the pitcher's; the rubber is inside the legal ±1 and is what `PitcherOffsetX` now holds; the crossing lands on the horizontal intent to 1e-9 unless the walk ran into the clamp, in which case it misses **short**. Height is the family's `dropFt` below the zone center and nothing else touches it (a Star's own shape excepted, §13). Both arms pitched, and the same delivery from the wrong arm would miss its intent by exactly twice the family's sweep — which is what makes the stamp-before-solve load-bearing. ⚠️ trial. ✅ #823

**S-116, Expect.** The family thrown is always in that pitcher's repertoire **and** authored; the press count is 0 / 1 / 2 and replaying those presses through `PitchSelection.Advance` from a SET reset lands on the same family. Across the roster every family in the library comes up, so the weights are not collapsing onto the fastball. Under the two-family table the presses only ever reach the fastball and the changeup. ✅ shipped #860 (the overlay now resolves to the shipped rows). ✅ #823

**S-120, Expect.** #887 removed `cpu.humanInputs` and the keys only the endpoint model read (`rubberWalkChance`, `rubberWalkMax`, `locations.middleYSpreadFt`, each row's `normal` / `charge` / `changeup` / `break`): a table that still authors one is refused by name. The shipped rows carry the accepted weights: every row weights every family in the library, and `chargeChance` / `steerChance` are chances.

A weight for a family the active table cannot fly is **valid** — weights are filtered at run time, not validated against a roster — and never reaches the mound. A row that weights nothing at all is a **validation error** by name. A row that weights only families *this pitcher* cannot select is not an error: it falls back to the fastball with zero presses, stated here and tested. ✅ #823; moved #860 — Jack accepted the `trials/pitch5` window on September 22, 2026 ("trial was good.")

**S-125, Expect.** The window is exactly `window.frames` on EASY, NORMAL and HARD; since #887 `ContactWindowFrames` takes no hitter, bat or charge at all, so the 2 700-row roster grid it once ran is the signature. No star pitch multiplies it (PH-16-R18) and the floor is still the last step (shown by a fixture table whose window sits on the floor); the crystal rink at night is Harbor's window (FD-11-R2, F4-d #895). The spatial half is untouched:

the barrel still narrows for a charge and still widens with Contact, and two hitters with the same Bat differ in the oval. ✅ P2-b Moved #860 — Jack accepted the `trials/pitch5` window on September 22, 2026 ("trial was good.").

**S-127, Expect.** **The cross-root read #844 pinned stays repaired** (#855): `AtBatResolver.PitchInZone` → `StrikeZoneGeometry.Contains` → `PitchFlight.Point` read the table they are handed, so the catalog's answer is its own crossing's and differs from the process-wide one; the game finishes in process.

Until #887 this row ran the S-29 cohort, recorded and never gated, from the stick switch's off path; its figures stay in [`docs/research/plate-window-p2b.md`](../research/plate-window-p2b.md), [`docs/research/stick-shaping-p2c.md`](../research/stick-shaping-p2c.md) and [`docs/research/promote-stick.md`](../research/promote-stick.md). ✅ P2-b; ✅ P2-c (#855); reshaped #887

**S-128, Expect.** The whole `AtBatResult` (exit, launch, spray, carry, class, foul) is **identical** across every stick for each case: equality of the resolver's output, not a stored double. The row is not vacuous: a Star Swing on the same resolver still moves with the stick (S-131). ✅ P2-c (`StickShapingScenarioTests`). Moved #883 — Jack accepted the stick trial in the `trials/pitch5` window on September 22, 2026 ("approve all").

**S-132, Expect.** An ordinary CPU swing carries 0 / 0 aims, and the command — charge, timing error, box — is composed in the test from a `Random` of the same seed replayed in order with both Gaussians skipped (the second call proves the count), never a stored double; a CPU Star Swing still carries drawn aims, and a sac bunt carries its side and none. The off-path half (the command with both aims, as played before #883) was retired by #887. ✅ P2-c. Moved #883 — Jack accepted the stick trial in the `trials/pitch5` window on September 22, 2026 ("approve all").

**S-133, Expect.** **Training offers the loaded table's families, in library order (#888, PH-02-R1, PH-15-R1).** On the shipped root a practice session offers all five: fastball, changeup, curveball, slider, sinker. On the copied root that authors two it offers fastball and changeup, from the same code. The code defaults are a table like any other and offer their two. Training is not in a match, so seed 7 and S-29 do not move. ✅ #888

**S-134, Expect.** The drawn oval is the judged oval: its center is `WorldCenter(box)`; every point of its outline is at ellipse distance 1 from the resolver's barrel; the resolver calls a crossing at 0.98 of the drawn tip, top, handle and bottom **Nice** and at 1.02 **Sour**. The oval's outline is the same points the client drew before for the same barrel. ✅ P2-d (#889, `CursorOvalScenarioTests`)

### B.2 Grounders and fielding

| S-35 | Bad-chem throw to 1B, σ big (a bad pair is slow, never slanted, #722) | 100 seeds | 0 errors and every throw at ×0.90; a throw that did miss the cover by > 6 ft would be live, ERROR, batter to 2B |

### B.4 Flies, liners, tag-ups

✅ P5 for S-51 … S-55 and S-55b (`OutsScenarioTests`; S-54 runs with the roster's Field-8 arm and three runner speeds; S-55b is the caught fly a lost relay must not re-read, #692); ✅ P2 for S-56 … S-59 (`FlightScenarioTests`).

| S-55b | Runner on 3rd tags on a caught fly; the relay then loses the ball (sail, uncovered lob, or item) | CPU runners | The catch is the out and is never re-read as a drop: no second retouch, so no force back at the bag he legally left. His fate from there is geometry — he scores, or the recovered ball beats him to the plate (#692) |

### B.8 Control: who you are (§8.9)

| S-96 | Human took 2B on a roller they can reach (the compact row is the 150 ft / 9° hopper 2B meets a foot onto the grass, #715) | Ball reaches the grass before 2B meets it | No hand-off while 2B's route still reaches (or the ball is inside their reach); 2B scoops on the grass |

| S-97 | See note *S-97, Setup* below. | Ball lands on the grass past them; the stick goes dead. The reachable liner: dead stick throughout. The intercept: South on the rope | Hand-off to the outfielder by route the first frame the chase runs with SS having no route; SS coasts 0.2 s; nobody teleports. The reachable liner: the ring never leaves SS, SS catches it, FlyOut (#636). The intercept and the CF rope: FlyOut; a liner that has touched the dirt is a scoop, never FlyOut (#666) |

| S-99 | Human presses Select with the stick pointing at CF, then again inside 0.7 s | — | First press takes CF; second is ignored (lock); holding the ball, Select does nothing — on the runner play too: the catcher on a steal keeps the ring and the ball and the throw goes to the armed bag; a sailed pickoff's loose ball takes Select under the lock (#637, PR #642) |

| S-100 | Liner that lands in right-center (RF starts closer to the bounce) and rolls to the wall; the same gap in left-center. Catchable fly to RC. Both seats | Dead stick | CF (or the body whose rolling route meets it earliest) wears the glove from the grass hand-off and scoops at the wall, not RF because RF started closer. A catchable fly to RC still picks by the landing. Same for 1P and 1v1. Never a per-frame re-pick (#667) |

**S-97, Setup.** Human took SS (Select) on a liner over their head that nobody catches; and, on both seats, a liner SS reaches whose plant is 2 ft past the lip (the 80-ft diamond has no such ball: SS reaches no liner planted past the 137.78 ft lip, nearest miss 2.8 ft; the row is the deepest rope SS reaches, planted 132.9 ft out, with a tripwire for the day one exists, #715); and a liner on the glove in the air (SS intercept, CF that reaches)

### B.7 Determinism and seats

| S-90 | Same seed + same recorded commands → identical `PlayEvent` stream, CPU seat and human seat (#512) |

| S-93 | Seat ownership is a function of (half, home/away, seated controllers) and nothing else (§0.4): the batting human's pad never reaches a glove, so a CPU defense scoops and throws with the offense pad live (S-31 with the human on offense, both halves, HOME and AWAY); the human on defense still owns the throw (S-33); in 1v1 the other controller owns the gloves every half (#579) |

### B.9 Fields (§0.3, D21) — SF-01, SF-02, SF-05, SF-06, SF-07, SF-08, SF-10, SF-11, SF-13, SF-14, SF-23, SF-24, SF-30, the hazard half of SF-03, SF-12 (the material half and the per-span read) and the sim half of SF-04 built (F1-a #820 / PR #831, F2-a #826 / PR #833, F2-b #845 / PR #848, F2-c #874, F3-a #827 / PR #832, F3-a2 #838 / PR #840, F3-c #856, F3-d #857, F4-a #847, F4-e #862, F4-h #858, F5-a #828 / PR #834); the rest ❌

## B.9 Fields (§0.3, D21) — SF-01, SF-02, SF-05, SF-06, SF-07, SF-08, SF-10, SF-11, SF-13, SF-14, SF-23, SF-24, SF-30, the hazard half of SF-03, SF-12 (the material half and the per-span read) and the sim half of SF-04 built (F1-a #820 / PR #831, F2-a #826 / PR #833, F2-b #845 / PR #848, F2-c #874, F3-a #827 / PR #832, F3-a2 #838 / PR #840, F3-c #856, F3-d #857, F4-a #847, F4-e #862, F4-h #858, F5-a #828 / PR #834); the rest ❌

| SF-01 | F3 | Harbor's resolved table (`AtPark`) equals the global table bit for bit, on both roots; S-29, the Harbor cohorts and seed 7 do not move in the parity PR — ✅ #827 (`ParkEnvironmentTests.SF01_EveryParksResolvedTableIsTheGlobalTableOnBothRoots`, `…SF01_TheMatchAtHarborPlaysTheCatalogsOwnTable`: the resolved table is the same reference, so there is nothing to compare), and ✅ every resolver in the match holds that reference (F3-a2 #838, PR #840: `…SF01_EveryResolverInTheMatchHoldsTheMatchsResolvedTable`, both roots × six parks × four rungs) |

| SF-02 | F1 | A park file with an unknown field, an unknown park id, or a misspelled key stops the load and names it — ✅ F1-a (#820, PR #831), `ParkSchemaTests.SF02_…`: an unknown park key, an unknown hazard key, a misspelled key, a missing or duplicated `pickOrder`, two parks with one faction, and an unknown id at `Match.Slice` / `Match.Exhibition` / `Challenge.MakeMatch`; both roots load |

| SF-08 | F2 | The boundary built from park parameters at Harbor's defaults equals today's polygon for all six parks — ✅ F2-a #826 (PR #833), `BoundaryTests.SF08_TheBoundaryFromTheTableIsTodaysPolygon`, six parks × both roots, vertex for vertex against the pre-move code path |

| SF-11 | F3 | The same grounder on two grounds: the roll changes where the ball crosses a zone boundary, in the trace — ✅ F3-c (#856), `GroundReadTests.SF11_TheSameGrounderRollsFartherOnTheSlickerOutfieldAndChangesItsLossAtTheLip`, both roots on a fixture root with unequal rows: read off the path's own samples, each step loses the friction of the zone of the sample it starts from, so the loss changes at the first sample past the lip, and the slicker outfield rolls farther |

| SF-14 | F3 | The three loose-ball ground models read the same zone row — ✅ F3-c (#856), `GroundReadTests.SF14_TheBattedRollTheOverthrowAndTheBobbleReadTheRowOfTheSamePoint`: the batted ball's roll, the overthrow and the local bobble dropped at one point read that point's zone row, and all three follow when the point crosses the lip, both roots on a fixture root with unequal rows |

| SF-22 | F4 | No result by chance: a catch by a slowed fielder is decided by the glove and the ball, never by `drops.frozen` — ✅ F4-b (#896), `StatusVolumeTests.SF22_ASlowedFieldersCatchIsDecidedByTheGloveAndTheBallWithNoDropRollDrawn`, both roots: over twelve seeds a fly to a short stop who stands in a fixture freeze volume from the crack is caught every time, and the live ball draws nothing from the match's stream (a counting stream in place of `_rng`); the heart swing, the special that still owns the table, draws the drop roll on the same ball, so the probe sees a draw when there is one |

| SF-25 | F4 | Night blocks at parity: Ember's reach and Funfair's chompers behave as before the move; Crystal's window is dropped (FD-11-R2) ✅ F4-d (#895) |

F4-a (#847), `HazardLibraryTests.SF03_AParkHazardTypeOutsideTheLibraryStopsTheLoadAndNamesIt` (both roots), `…SF03_ALibraryTypeWithNoAuthoredRowStopsTheLoadAndNamesIt` (the D20 message, from the table and from every park that names the type), `…SF03_ATrialCopyMissingARowIsRefusedByTheWholeFileRule` (the overlay's stricter net gets there first) and `…SF03_ARowWithAnUnknownPatternStopsTheLoadAndNamesIt` (both roots). The grounds and the wall materials:

F3-b (#846, PR #850), `GroundLibraryTests.SF03_…` — a `surface` with no row and each of the four `zones` ids with no row stop the load and name the bad id, the park file it was written in and the library file the rows live in, on the shipped root and on `trials/c80` (the trial's park file, against the shipped library it resolves to); a ground or a wall material asked for in code is the same stop by name, and an unknown key inside `zones` is refused by the strict park read

**SF-04, Expect.** No park-id string literal decides a rule, a light, a color, a copy line or a list (source test over `src/` and `unity/Assets/Scripts`) — sim half ✅ F1-a (#820, PR #831) and ✅ F4-a (#847), `ParkSchemaTests.SF04_TheOnlyParkIdLiteralInSimRuleCodeIsTheOneDefault`: `ExhibitionPick.DefaultPark` and nothing else.

**SF-05, Expect.** The drawn wall equals the flight wall on every span, in every park, lopsided parks included (D15 as amended) — ✅ F2-b #845 (PR #848), `HarborWallTests.SF05_TheDrawnWallIsTheFlightWallOnEverySpan` over `content.ParkPickOrder` on both roots and `SF05_ALopsidedParkDrawsItsOwnLeftFieldWall` on a fixture: every drawn vertex lies on the clip polygon within 1e-9 ft (worst measured 5e-15), a fair span is drawn at the fence top and a foul span at the rail top.

✅ F2-b2 #873 (PR #875, FD-06-R2): the row claims the whole rail. Every drawn span lies on one flight segment and stands at that segment's top at each of its ends, every drawn vertex stands at the tallest segment it lies on, and the step from the fence to the rail is on the pole vertex on each side (`IsPole`, where the kit stands the pole), with the next vertex into foul already at the rail.

**SF-06, Expect.** A park that lists no fence points plays and draws bit-identical to the three-post circle — ✅ F2-c (#874), `PolylineFenceTests.SF06_EveryCatalogParkPlaysAndDrawsTheThreePostFenceBitForBit`:

**SF-07, Expect.** A polyline fence keeps its vertices in the clip polygon; a ball off a notch caroms by that span's normal; the fence has one distance per bearing from home — ✅ F2-c (#874), `PolylineFenceTests` on a fixture with a porch, a notch and a short alley, both roots:

**SF-10, Expect.** The same fly at two drags: carry differs, launch and gravity do not — ✅ the air, on a park built in the test (#827: `ParkEnvironmentTests.SF10_ThickerAirIsAShorterCarryOffTheSameLaunch`, `…SF10_AParkAtWindMulZeroFliesTheStillAirPath`), and ✅ on the path the game plays — a seeded swing through `Match` / `LivePlaySystem` at a fixture park with `dragMul` 2.0 carries shorter in the at-bat's ball, the fielding preview's and the live path alike (F3-a2 #838, PR #840:

`…SF10_ASeededSwingThroughTheMatchFliesTheParksAir`). ✅ the zone-row half (F3-c #856): on a fixture root with unequal rows the same fly lands on two outfield rows, the carry to the first landing is the same bits and each hop follows the row it lands on (`GroundReadTests.SF10_TheSameFlyCarriesTheSameAndHopsOffTheRowItLandsOn`, both roots)

**SF-12, Expect.** The same carom off two wall materials: normal and tangent speed follow the span's row — ✅ the material half (F3-c #856), `GroundReadTests.SF12_TheSameCaromFollowsThePaddedRowOfEachTable`: the same roll into the fence off the `padded` row at two values (two tables), the normal and tangential speeds read off the samples follow each, on both roots. ✅ the per-span read (F2-c #874), `PolylineFenceTests.SF12_EachSpanAsksTheLibraryForItsOwnMaterialsRow`:

**SF-13, Expect.** The same body on two grounds with the response law on: start, brake and cut-back differ, top speed and heading do not; the routine grounder (S-31's ball) is still an out. With the law off, the body is identical — ✅ F3-d (#857), `BodyGroundTests` on fixture rows (not shipped numbers) with the law on at the trial's 0.2 / 0.1: `SF13_StartBrakeAndCutBackDifferOnTwoGrounds` (the stick glove from rest, through a 90° cut and a reversal:

**SF-20, Expect.** Status volume: a body that enters is slowed for the row's time and no other body is; leaving and re-entering restarts it as the row says — ✅ F4-b (#896), `StatusVolumeTests`, both roots: `SF20_AFielderWhoRunsIntoAVolumeIsSlowedForThreeSecondsAndNoOtherBodyIs` (the centre fielder crosses a fixture volume on a shallow fly:

**SF-23, Expect.** A hazard whose disc or path crosses a running lane, a bag pad, the mound or the plate area is refused, on both roots — ✅ F4-e (#862), `HazardPlacementTests`: `SF23_EveryHazardClearsTheLanesPadsMoundAndPlate` (all 26 hazards of all six parks on each root, by their own radius; the validator silent on both), `SF23_AVolumeOnALanePadMoundOrPlateIsRefusedByName` (a volume on each of the five lanes, each of the three pads, the mound and the plate, both roots:

**SF-24, Expect.** Hazards off: the match has no hazard instance and no hazard event; every other park field is unchanged — ✅ F4-h #858, `HazardsOffTests`: `SF24_AHazardsOffMatchHasNoHazardInstanceAndEveryOtherParkMemberUnchangedOnBothRoots` (every park in the pick order, both roots:

**SF-30, Expect.** `cli match --cohort park-factors` reports run and home-run factors against Harbor on predeclared seeds, both roots, hazards on and off, day and night. A report, not a gate. ✅ #828 (PR #834), `ParkFactorsCohortTests`: determinism over two runs, every catalog park, the control park at 1.00 by definition, day and night both present, the root named — and no row asserts a factor. ✅ hazards on and off (F4-h #858):

## docs/gameplay-spec.md

### Gameplay spec — how every play behaves

Status tags used throughout, first checked against `f09cad1` (2026-09-12 morning) and re-checked against `a15f5f5` (2026-09-12 evening, after Phase P #562–#570 and the sitting batches #606–#613 shipped). Older ✅ tags name the PR that closed them; [Appendix A](archive/gameplay-spec-appendix-a.md), now archived, keeps the original audit rows as the record.

From 2026-09-22 a rule says what the game does, not who built it: a behavior change updates its rule here in the same PR, with a bare tag and no PR numbers ([agent-rails.md](agent-rails.md) §1.2). Commit history keeps who did what. Existing provenance stays.
