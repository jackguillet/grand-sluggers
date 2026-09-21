# Pitching and hitting decision plan

Tracker: [#803](https://github.com/jackguillet/grand-sluggers/issues/803), serving #209 and coordinating #534. Session kind: **Gameplay research/documentation**. Baseline: `05471a6`. Research: [reference comparison](research-pitching-hitting.md). Canonical structured record: [pitching-hitting-decisions.json](research/pitching-hitting-decisions.json).

## Current state

Research foundation prepared; **all 20 broad directions are accepted. Starting repertoires are assigned to all 25 existing characters. Pitch selection cycles on RB/Tab, resets to Fastball each pitch and locks at charge start (PH-02-R1–R5, PH-15-R1–R4). PH-05-R1 makes charge power trade against available steering correction while retaining characteristic movement. PH-08-R1 makes peak-power loss fatigue’s primary effect; a small steering penalty remains tentative. PH-08-R2 keeps fatigue per character for the match with no in-game recovery or swap reset; PH-08-R3 removes extra fatigue from conceded hits/home runs/runs in the target design. PH-14-R1 makes contact quality govern bunt softness and control; PH-14-R2/R3 add a first-/third-base-side preference adjustable until contact, shown by bat angle; PH-14-R4/R5 select directional hold-to-bunt (LT/J third, RT/L first) and release-all-to-withdraw. PH-13-R1 sets East/G cancellation and direct uncommitted swing-to-bunt conversion, discarding charge. PH-15-R5 separates batting Contact and Power; PH-15-R6 separates pitching Velocity, Movement, Control and Endurance; PH-15-R7 gives Contact spatial forgiveness only with shared hitter timing. PH-11-R1 makes charged swings trade spatial forgiveness for power with unchanged timing. PH-09-R1 preserves normal box movement while loading a swing. PH-16-R1 keeps ordinary timing windows for Star Pitches; replacement effects for timing-reducing skills remain open. PH-16-R2 permits ability-specific Star Swing contact-area changes with actual contact required. PH-16-R3 makes missed Star Swings spend their full normal resource cost. PH-16-R4 gives each team one shared Star Pitch/Swing resource pool. PH-16-R5 grants both teams completed-plate-appearance Star gains plus performance bonuses. PH-16-R6 gives both teams a usable starting Star reserve. PH-16-R7 accepts tiered ability costs; PH-16-R8 reserves the highest-cost specials for captains. PH-16-R9 gives each character one Star Pitch and one Star Swing. PH-16-R10 uses a held modifier with the normal pitch/swing action for specials. PH-16-R11 locks special intent at action-button release. PH-16-R12 uses ordinary fallback without Star spend when unaffordable; Star-counter red-flash feedback is a candidate. PH-16-R13 retains ordinary charge tradeoffs alongside special effects. Remaining detailed contracts and tuning stay open; no new mechanics are implemented or numerical tuning targets accepted.** Jack loves Super Sluggers and is open to more robustness. That is a brief to explore, not blanket approval for a precision simulator. Existing gameplay-spec D4, D6, D7, D12 and D13 remain in force. The older #534 “match exactly for now” scope is now open for discussion; PH-12 selects a target change to the direct stick spray/loft rules in gameplay-spec §§5.3–5.4; reconcile that shipping contract before implementation. PH-13 additionally selects a deliberate pre-commit swing cancel; its binding and commitment boundary must be specified and taught before implementation. Runtime remains unchanged.

This follows the #693/#708 tracking pattern: stable IDs, alternatives, recommendations, scoped human choices, evidence and separate implementation/play gates. It also carries forward that tracker's lesson: **ask about material gameplay tradeoffs one at a time; do not create a chain of glove-microphysics-style approvals for routine derivations.**

## How we use this together

1. Discuss the next ready question using a short player-facing example and two or three real alternatives. The broad direction round is complete. Next scoped discussion: **PH-16-R14**, decide whether plate-level chemistry grants modest Star-earning bonuses (recommended), modest pitching/hitting attribute bonuses, or no plate-level bonus. None is selected yet; existing fielding chemistry is outside this choice. PH-16-R13 accepts ordinary charge tradeoffs alongside special effects. PH-16-R12 accepts ordinary fallback without Star spend, with clear feedback; a red flash at the Star count remains a presentation candidate. PH-16-R11 samples special intent at accepted action-button release; ordinary family locking stays at charge start. PH-16-R10 accepts a held modifier with the normal action, preserving charge/release. PH-16-R9 accepts one Star Pitch and one Star Swing per character, separate from ordinary pitches. PH-16-R8 resolves the tentative access idea: highest-cost specials are captain-exclusive. PH-16-R7 accepts a small set of cost tiers; illustrative abilities, tier count and prices remain unselected. PH-16-R6 gives both teams a usable starting Star reserve. PH-16-R5 accepts modest completed-plate-appearance gains for both teams plus performance bonuses. PH-16-R4 accepts one shared Star Pitch/Swing pool per team. PH-16-R3 makes a missed Star Swing spend its full normal resource cost. PH-16-R2 permits individually authored Star Swing contact-area changes, following Jack’s correction to option 2. PH-16-R1 keeps the ordinary timing window for Star Pitches, with replacement effects still unselected. PH-09-R1 preserves normal horizontal movement during uncommitted swing loading. PH-08-R3 accepts pitching-effort-only fatigue with no conceded-outcome surcharge. PH-08-R2 keeps fatigue for the match with no rest recovery or swap reset. Contact and charged swings now change spatial forgiveness only under PH-15-R7/PH-11-R1; exact regions and curves remain open. PH-14-R4 accepts hold-to-bunt with geometric contact and release-to-withdraw. PH-14-R2 separates first-/third-base-side preference from earned contact quality. PH-08-R1 accepts fatigue primarily lowering peak power, with a small steering-correction penalty only a candidate for comparison. Repertoires and the cycling flow are recorded under PH-02/PH-15. PH-14-R1 now resolves Jack’s earlier contact-quality follow-up; the original question and proposal remain in its history. Refine the accepted directions without reopening them or treating unresolved author proposals as selected.
2. Record Jack's answer in the JSON: selected option (or a clearly described custom answer), accepted scope, exact qualification, local date, author and a conversation/issue reference or quoted answer. Recommendation never counts as selection.
3. Append history rather than erase a prior choice. Record supersession explicitly; link the replacement and named gameplay-spec decision affected.
4. Direction acceptance selects intent only. Numeric targets stay null until a later scoped trial is accepted with units, conditions, rationale and evidence limits.
5. Keep this readable plan synchronized with the canonical JSON and summarize current state in the GitHub tracker. The research report supplies evidence; gameplay-spec remains the shipping contract. Reconcile any changed spec rule before implementation.
6. Open bounded implementation children only after their contract is ready. Link exact decision IDs, scenario requirements, PRs, tested revision and remaining human gate. Do not pre-file twenty speculative implementation tasks.

Status progression: `open → direction-accepted → trial-accepted → implemented → validated → human-accepted`. `deferred`, `rejected` and `superseded` preserve alternatives/history. For nonnumeric decisions, record that a numeric trial is not applicable before moving to implementation. Automated validation never fills `human_acceptance`.

## Discussion order

- **Round 1 — the core duel:** PH-01, then PH-09 (hitting coverage) and PH-02 (pitch vocabulary), followed by PH-03/04 (location and steering). These determine which later choices are meaningful.
- **Round 2 — commitments and tactics:** PH-05/06, PH-10–16, PH-08, PH-17/18. Consolidate consequences of accepted choices rather than ask Jack to approve every coefficient.
- **Round 3 — measurable trials and acceptance:** PH-07 pace, PH-19 feedback/teaching and PH-20 delivery. Existing D7 requires a re-sit before pitch-speed tuning. Determine the acceptance process early even though its evidence is collected last.

## Decision register

PH-01–20 are **DIRECTION ACCEPTED**. Detailed contracts, numeric trials, implementation and human acceptance remain open. The recommendation is the author's proposal. Only Jack’s explicitly recorded answer selects an option; each acceptance statement is a proposed falsifier rather than a passed gate. Source IDs resolve in the research report.

### PH-01 — What should added depth primarily ask the player to do?

**Decision — Jack, September 20, 2026: A+C.** “Both 1 and 3.” Pursue Mario-style tactical depth together with reliability, consistency and clear teaching. Exact sequencing remains open. This selects no precision-control expansion, specific mechanic or numerical target. Evidence: this task’s PH-01 reply; full provenance is in the canonical JSON.

Area: Direction. Depends on: none. Evidence: WII, SMB-EI, SHOW-H.

- **A — Mario-style tactical depth:** Preserve positioning and charge; deepen pitch mixing, reads and situational choices.
- **B — More precise execution:** Add target/reticle or release demands; more control, more practice required.
- **C — Clarity and reliability first:** Keep the current choice set and improve consistency, teaching and feedback before expansion.

**Recommendation:** A, with clarity and reliability required throughout. It best matches the stated affection for Sluggers; still only a proposal.

**Existing contract:** Existing gameplay-spec principles remain; this choice authorizes a direction, not new controls or coefficients.

**Acceptance:** A new player can put balls in play; a practiced player can explain a tactical advantage beyond simply reacting faster.

### PH-02 — How many ordinary pitch families should each pitcher have?

**Refinement PH-02-R4 — Jack, September 21, 2026: single-button cycling.** Reply “2” selects one button that advances through the three ordinary pitches and wraps, before charging. Selection does not throw the pitch; charge start locks the family under PH-02-R3. Preserve common charge/release and live steering. Exact binding, order, default/persistence, feedback and simultaneous-input precedence remain open. Respect PH-06 information limits and bag/pickoff controls. No runtime change or passed playtest. Full provenance is in the canonical JSON.

**Refinement PH-02-R5 — Jack, September 21, 2026: cycling flow with Fastball reset.** Reply “1” accepts RB / Tab cycling Fastball → second pitch → third pitch → Fastball in accepted repertoire order, starting on Fastball for every new pitch. South / Space / left click charges/releases; retain the existing Enter equivalent and lock the family at charge start. Show the available pitches in order without highlighting or labeling the active selection on the shared screen. Keep stick movement, D-pad bag/pickoff inputs and live steering. Reconcile the prior West/changeup-through-release contract and paired HowToPlay/docs before implementation. Exact layout and input/state edge handling remain contract work; no runtime/presentation change, numeric tuning target or human gate passed. Original proposal and resolution are in the canonical JSON.

**Refinement PH-02-R3 — Jack, September 21, 2026: select before charging.** Reply “1” selects the ordinary pitch before loading; starting the common charge/release action locks its type for the delivery. Later selection/modifier inputs cannot swap the family mid-charge. This locks the type, not live steering after release or the accepted power/control tradeoff. Exact selection bindings, default/persistence, feedback and pitcher cancellation/rearming remain open; PH-13’s hitter cancel does not select a pitcher cancel. Preserve PH-06’s ordinary-play information limits. No runtime change, tuning target or passed playtest. Full provenance and the commitment contract are in the canonical JSON.

**Refinement PH-02-R2 — Jack, September 21, 2026: all five families.** Reply “1” selects fastball (speed pressure, relatively straight), changeup (slower delivery that punishes early swings), curveball (pronounced arc/drop), slider (sideways coverage challenge), and sinker (faster dipping alternative to the curveball). Each pitcher still gets three; charge modifies and stars remain separate. These are accepted game roles, not measured reference-game behavior. This resolves earlier library-open statements while preserving their history. Repertoire composition, character assignments, selection inputs, detailed weaknesses, trajectories and tuning remain open. Preserve the accepted horizontal batter control and live steering; no guaranteed hit class or outcome. No runtime change, numeric trial or human gate passed. Full provenance is in the canonical JSON.

**Refinement PH-02-R1 — Jack, September 21, 2026: three pitches.** Reply “1” selects exactly three ordinary pitches per pitcher, drawn from a shared library with different character repertoires. Charging modifies a pitch; Star Pitches are outside the count. This resolves the original count-open statement below while preserving that earlier decision’s history. Library membership, individual pitch purposes/weaknesses, character assignments, inputs and tuning remain open. No runtime change, numeric tuning trial or passed playtest. Full provenance and the count contract are in the canonical JSON.

**Decision — Jack, September 20, 2026: B.** Reply “2” selects a small set of distinct ordinary pitches, each with a clear purpose and weakness. Exact pitch count/names, character differences, inputs, location, steering and tuning remain open. This accepts the direction only; no implementation or human playtest gate is passed. Evidence: this task’s PH-02 reply; full provenance is in the canonical JSON.

Area: Pitching. Depends on: PH-01. Evidence: GC, WII, SPORTS, SMB-EI.

- **A — Current compact vocabulary:** Normal, charged and changeup with steering; lowest teaching cost.
- **B — Small authored repertoire:** Add clearly distinct ordinary pitch shapes, with character-specific strengths and weaknesses.
- **C — Broad baseball repertoire:** Many named pitches and detailed matchup selection; largest complexity and balance cost.

**Recommendation:** B if PH-01 favors added tactics; define each pitch by a distinct response it demands before choosing a count or names.

**Existing contract:** Current PitchFlight has fastball/changeup base shapes; stars are separate. No new pitch count is selected.

**Acceptance:** Each pitch has a readable trajectory, tactical use, counterplay and reason not to throw it every time.

### PH-03 — How does the pitcher choose location?

**Decision — Jack, September 20, 2026: A.** Reply “1” selects horizontal mound positioning plus pitch-defined height. This does not add broad target selection or a freely aimed two-dimensional target. Exact shapes and numbers remain open; post-release steering is decided separately in PH-04. No runtime change or human playtest acceptance. Evidence: this task’s PH-03 reply; full provenance is in the canonical JSON.

Area: Pitching. Depends on: PH-01, PH-02. Evidence: WII, SMB-EI, SHOW-P.

- **A — Mound and shape:** Keep horizontal mound positioning and pitch-defined height.
- **B — Coarse zone intent:** Choose a broad region before delivery; execution and shape determine the actual crossing.
- **C — Free two-dimensional target:** Place an exact intended endpoint; increases precision and aiming load.

**Recommendation:** Compare A and B first. Choose together with PH-04 so aiming plus steering does not grant unlimited correction.

**Existing contract:** Human pitching currently has no free vertical aim. CPU endpoint targeting is a baseline asymmetry to assess in PH-18.

**Acceptance:** Both seats can intentionally seek a strike or chase pitch without the shared screen revealing the exact endpoint.

### PH-04 — How much control remains after release?

**Decision — Jack, September 20, 2026: A.** Reply “1” selects live left/right steering after release, with bounded, readable break and a fair chance for the hitter to respond. This does not select a release lock or early-only steering cutoff. Exact break strength, timing, per-pitch/charge differences and numerical limits remain open. Direction only; no runtime change or passed playtest. Evidence: this task’s PH-04 reply; full provenance is in the canonical JSON.

Area: Pitching. Depends on: PH-02, PH-03. Evidence: GC, WII.

- **A — Live lateral steering:** Keep the familiar post-release duel; requires bounded break and a fair response opportunity.
- **B — Committed shape:** Release locks the path; easier prediction and clearer pitch identity.
- **C — Bounded early steering:** Allow a limited correction then commit; adds a timing boundary to teach.

**Recommendation:** A as the first comparison baseline; evaluate B/C only against an observed readability or fairness problem.

**Existing contract:** Current break is accumulated direction input, with charge/changeup damping. New caps and cutoffs remain unset.

**Acceptance:** A legal late action can fool a committed swing but cannot create an unreadable unavoidable strike for a waiting hitter.

### PH-05 — What makes a pitch well executed?

**Refinement PH-05-R1 — Jack, September 21, 2026: less steering correction for more power.** Reply “1” selects reduced room for the player to correct the pitch’s natural path as charge power rises. Characteristic pitch movement and live steering remain; this does not flatten a charged breaking pitch or select more sensitive inputs. Exact correction bounds, charge curve, family/character differences and fatigue interaction remain open. Preserve readable steering and D7; no random misses, separate accuracy minigame, runtime change, numeric trial or passed playtest. Full provenance and proposed validation requirements are in the canonical JSON.

**Decision — Jack, September 20, 2026: B.** Reply “2” selects a stronger power-versus-control tradeoff using the same charge/release controls. Pursuing more power makes the pitch harder to control. The form and strength of that challenge, affected pitches, charge/release details, fatigue interaction and numbers remain open; no separate accuracy minigame or particular random-error model is selected. Preserve PH-03 mound/shape location and PH-04 live steering. Direction only; no runtime change or passed playtest. Evidence: this task’s PH-05 reply; full provenance is in the canonical JSON.

Area: Pitching. Depends on: PH-03, PH-04. Evidence: WII, SMB-EI, SHOW-P.

- **A — Existing charge/release:** Preserve a simple timing commitment with visible benefits and fatigue costs.
- **B — Power versus command:** Use the same action to trade strength against controllability; adds a tactical risk.
- **C — Separate accuracy task:** Meter, pulse or gesture execution; greater precision cost every pitch.

**Recommendation:** A initially; consider B if it creates a clear choice. Avoid a second execution minigame unless PH-01 selects that emphasis.

**Existing contract:** No charge timing, Nice multiplier or stamina number changes with this direction.

**Acceptance:** A player can distinguish a poor release from a poor location choice; maximum power is not universally best.

### PH-06 — What may the opponent see before committing?

**Decision — Jack, September 20, 2026: A.** Reply “1” selects body and ball cues in ordinary play: read the windup, charge, release and actual ball movement without explicit opponent pitch-type or destination hints. Retain a readable response opportunity on the shared screen. Stronger training/assistance guidance remains a separate open choice under PH-17/PH-19; exact cues and timing are unselected. Direction only; no presentation/runtime change or passed playtest. Evidence: this task’s PH-06 reply; full provenance is in the canonical JSON.

Area: Shared duel. Depends on: PH-03, PH-04. Evidence: WII, SMB-EI, SHOW-P.

- **A — Body and ball tells:** Show preparation and trajectory but keep exact aim private.
- **B — Broad intent tells:** Also reveal a pitch family or coarse region; easier reads, less deception.
- **C — Explicit trajectory aid:** Show target/arrival guidance in training or named assistance; strongest help.

**Recommendation:** A for the ordinary duel; C is useful in training. Choose any competitive assistance openly in PH-17.

**Existing contract:** One shared screen and both controller seats must be considered. No new overlay is approved.

**Acceptance:** Two people on one screen retain a mind game; a learner can identify the cue that gave them a chance to respond.

### PH-07 — What reaction and between-pitch pace should we target?

**Decision — Jack, September 21, 2026: A, qualified.** “1. if anything, it can slow down a hair to highlight character animations, highlights, changing sides, special ability clips, etc.” Keep the current pace pending replay; if anything, allow slightly more time for character animations, highlights, changing sides and special-ability clips. Treat this as a preference for modest breathing room in the overall rhythm, not an accepted pitch-flight slowdown. Evaluate flight/reaction clocks separately from animation and transition durations. Coordinate the existing F693-07 deliberate dead-ball pacing direction. D7’s named standalone re-sit still precedes numeric pitch-speed tuning. Exact durations, clip cadence and presentation treatment remain open; no runtime/presentation/art change or passed playtest. Evidence: this task’s PH-07 reply; full provenance is in the canonical JSON.

Area: Shared duel. Depends on: PH-04, PH-09, PH-11. Evidence: WII, GC, Legacy.

- **A — Keep current pace:** Re-sit existing timing before changing the budget.
- **B — Faster duel:** Increase urgency after measured evidence; raises accessibility demands.
- **C — Selected middle ground:** Tune pitch families and reset rhythm to an explicit measured budget.

**Recommendation:** A until the gameplay-spec D7 re-sit; then compare matched captures and authored trial intervals.

**Existing contract:** D7 is a HOLD, not superseded by this brief. Air time, visual tell time, input lead and reset duration are separate clocks.

**Acceptance:** Record release, readable cue, swing command and plate arrival; Jack compares pad, keyboard and two-pad play before numeric acceptance.

### PH-08 — What should fatigue change during a short game?

**Refinement PH-08-R3 — Jack, September 21, 2026: fatigue costs pitching effort only.** Reply “1” selects no extra stamina penalty for conceding hits, home runs or runs. Preserve ordinary/high-effort costs, Endurance, match-long persistence and the previously accepted fatigue effects. The target design supersedes the inspected baseline home-run/run-allowed surcharges; reconcile gameplay-spec §4.7 and rule data before implementation. Exact action, family and special costs and response curves remain open. No runtime change, numeric trial or passed playtest. Full provenance is in the canonical JSON.

**Refinement PH-08-R2 — Jack, September 21, 2026: fatigue lasts for the match.** Reply “1” selects per-character fatigue persistence without in-game stamina recovery. Moving a pitcher to another position, resting while a teammate pitches, and returning to the mound preserve that character’s accumulated fatigue; swaps never reset it. Preserve effort-driven costs, Endurance differences, primary peak-power decline and the tentative-only secondary steering penalty. Swap eligibility/frequency, pools, costs and curves are not selected here; no new batting/fielding fatigue effect is implied. No runtime change, numeric trial or passed playtest. Full provenance is in the canonical JSON.

**Refinement PH-08-R1 — Jack, September 21, 2026: power decline first; steering penalty tentative.** “definitely 1, maybe a bit of 2” accepts gradual loss of available peak pitching power as the primary fatigue effect. Keep a small secondary reduction in steering correction as a candidate to compare, not a mandatory accepted penalty. Preserve visible effort-driven fatigue and predictable steering. Exact power response, correction penalty, costs, recovery and curves remain open; coordinate any combined cost with PH-05-R1 and character traits without assuming a stacking formula. Respect D7 before changing flight timing. No runtime change, numeric trial or passed playtest. The exact qualification and tentative consideration are in the canonical JSON.

**Decision — Jack, September 20, 2026: B.** Reply “2” selects gradual, clearly visible effort-driven fatigue. Ordinary pitching stays relatively economical; repeatedly pushing for power wears the pitcher down faster, with gradual decline. Exact pools, costs, curve, affected abilities, recovery/substitution details and penalties remain open. Coordinate accumulated fatigue with PH-05’s immediate power/control tradeoff and PH-15’s character strengths; no stacking formula is selected. Reconcile the existing fatigue contract before implementation. Direction only; no runtime change or passed playtest. Evidence: this task’s PH-08 reply; full provenance is in the canonical JSON.

Area: Pitching. Depends on: PH-02, PH-05. Evidence: GC, SMB-EI.

- **A — Current pitch-use resource:** Keep per-character fatigue and swaps; audit costs and tells.
- **B — Visible effort economy:** Make high-effort choices the main cost, with gradual readable degradation.
- **C — Minimal fatigue influence:** Keep the duel stable; removes some substitution and resource strategy.

**Recommendation:** Compare A/B after the pitch vocabulary is chosen; no invisible sudden loss of control.

**Existing contract:** Existing stamina costs stack; a changeup adds 3 to the ordinary cost of 4. No new fatigue thresholds selected.

**Acceptance:** The player predicts when to change pitchers and understands why a tired delivery differs; swapping does not erase history.

### PH-09 — What does the hitter position to cover the ball?

**Refinement PH-09-R1 — Jack, September 21, 2026: normal movement while loading.** Reply “1” selects no charge-related slowdown of horizontal box movement during an uncommitted ordinary swing load. Preserve batter-linked coverage, the spatial charge/power tradeoff, overcharge, PH-13-R1 commitment/cancel/rearming and existing height/recenter rules. Committed-swing movement, authored transitions, normal speed/acceleration values and character movement modifiers are not selected here. No runtime change, numeric trial or passed playtest. Full provenance is in the canonical JSON.

**Decision — Jack, September 20, 2026: A.** Reply “1” selects the batter-linked horizontal cursor. Keep horizontal box movement and the existing height/recenter contracts. This does not add high/low aiming, an independent cursor, numerical tuning or a passed playtest. Evidence: this task’s PH-09 reply; full provenance is in the canonical JSON.

Area: Hitting. Depends on: PH-01. Evidence: WII, GC, SHOW-H, SMB-EI.

- **A — Batter-linked cursor:** Keep horizontal box movement and readable coverage; height comes from the existing contract.
- **B — Position plus height intent:** Add a coarse high/low adjustment; more coverage decisions without a free cursor.
- **C — Independent two-dimensional cursor:** Precise barrel aiming; strongest execution demand and largest control change.

**Recommendation:** A first, with B as the smallest expansion to compare if pitch variety needs vertical counterplay. Do not assume C is necessary.

**Existing contract:** Gameplay-spec D4/D12 apply. An independent cursor or persistent box position requires an explicit supersession.

**Acceptance:** Inside/outside and high/low balls have understandable responses for both hands and small/large captains.

### PH-10 — How should timing and barrel position determine contact quality?

**Decision — Jack, September 20, 2026: A.** Reply “1” selects placement as the primary quality determinant and timing as the direction determinant. Centered contact can still produce strong deliberate pull/opposite-field hits; contact weakens near the timing-window edges, and outside the window is a miss. Retain the timing-rim penalty concept rather than continuously penalizing all nonideal timing. Exact window widths, edge bands and penalty amounts remain open; D4/D13 remain in force. Direction only; no runtime change or passed playtest. Evidence: this task’s PH-10 reply; full provenance is in the canonical JSON.

Area: Hitting. Depends on: PH-09. Evidence: GC, WII, SHOW-H, Legacy.

- **A — Preserve the existing split:** Position primarily controls quality; timing controls direction/whiffs, with the current rim exception audited.
- **B — Continuous combined quality:** Timing and spatial error both reduce quality; expressive but may punish one mistake twice.
- **C — Timing-led assistance:** Timing drives the accessible mode with explicit spatial assistance; less direct coverage control.

**Recommendation:** A as the baseline. Make the current timing-rim quality demotion explicit before deciding to retain or replace it.

**Existing contract:** Gameplay-spec D4/D13 govern. Code demotes one tier at the outer timing rim; do not claim timing never affects quality today.

**Acceptance:** Controlled early/late and centered/off-center trials explain quality and direction without guaranteed hits or arbitrary outs.

### PH-11 — What is the tradeoff between a quick swing and charging?

**Refinement PH-11-R1 — Jack, September 21, 2026: power costs placement forgiveness.** Reply “1” selects reduced spatial placement forgiveness for charged swings, with an unchanged swing timing window. The same swing rule applies to every hitter, alongside spatial-only Contact and independent Power. Preserve geometric contact, timing-led direction and shared timing-edge weakening, charge buildup/overcharge and PH-13-R1 commitment/cancel/rearming. Exact regions, quality boundaries, charge response, overcharge interaction and visible representation remain open. No runtime change, numeric trial or passed playtest. Full provenance is in the canonical JSON.

**Decision — Jack, September 20, 2026: A.** Reply “1” selects one button for quick and charged swings: quick release favors contact; holding/releasing favors power with greater commitment; holding too long loses charge. Quick swings retain a useful role with two strikes and difficult pitches. Exact fill/MAX/decay, forgiveness, power and commitment values remain open. Preserve PH-09/PH-10 and D13. Direction only; no runtime change or passed playtest. Evidence: this task’s PH-11 reply; full provenance is in the canonical JSON.

Area: Hitting. Depends on: PH-09, PH-10. Evidence: GC, WII, SMB-EI.

- **A — One hold/release grammar:** Keep quick and charged versions of one action, including overcharge.
- **B — Explicit contact/power choices:** Separate intent selection; easier intent clarity but more inputs.
- **C — Minimal charging:** Emphasize timing/coverage with little load management; changes the Mario rhythm.

**Recommendation:** A, provided quick swings retain useful situational advantages. Compare forgiveness, power and commitment as a package.

**Existing contract:** Existing fill/MAX/decay values are a snapshot, not approved new targets; preserve D13 until explicitly amended.

**Acceptance:** Quick swings remain useful with two strikes and awkward pitches; waiting fully charged is not a dominant strategy.

### PH-12 — How deliberately can the hitter shape a batted ball?

**Decision — Jack, September 20, 2026: C.** Reply “3” selects no extra directional input for shaping ordinary hits. Timing, contact position, pitch shape and swing type determine flight. Preserve horizontal batter movement (PH-09), timing-led pull/opposite-field direction and contact quality (PH-10), and quick/charged swings (PH-11). No preselected grounder/liner/lift approach. This changes the target for gameplay-spec §§5.3–5.4 direct stick spray/loft; reconcile the spec before implementation. Batter movement, SET recenter and pitching steering remain, and bunt placement is separately open. Exact flight coefficients and human acceptance remain open. Evidence: this task’s PH-12 reply; full provenance is in the canonical JSON.

**Accepted validation scope:** hold batter position, swing timing, pitch, swing/charge state and seed fixed; changing only direct spray/loft intent must not change the ordinary hit. Moving the batter may still change actual contact, without a second stick-based spray bonus. Keep grounders/liners/flies/fouls distinct. Coordinate flight distributions with #693/#708/#715 and update the paired book/tutorial instructions in their implementation scopes.

Area: Hitting. Depends on: PH-09, PH-10. Evidence: GC, SHOW-H.

- **A — Current bounded influence:** Timing pulls/pushes; stick shifts direction and loft, with contact constraining the result.
- **B — Preselected approach:** Choose grounder/liner/lift intent before commitment; more tactical clarity.
- **C — Contact geometry alone:** Remove extra trajectory intent; simpler inputs but fewer deliberate situational choices.

**Recommendation:** A first; retain meaningful influence without letting a stick command guarantee a hit class.

**Existing contract:** Current Up lowers launch and Down lifts. Coordinate changed exit/launch distributions with #693/#708/#715.

**Acceptance:** The same intent can succeed or fail because of actual contact; grounders, liners, flies and fouls remain distinct.

### PH-13 — What happens when the hitter changes their mind?

**Refinement PH-13-R1 — Jack, September 21, 2026: direct load-to-bunt transition and cancel flow.** Reply “1” accepts East / G canceling an uncommitted load and discarding charge. A directional bunt hold also discards an uncommitted load and squares up directly, without a separate cancel. Releasing the swing button commits the ordinary swing; neither cancel nor bunt can interrupt it afterward. After cancel/conversion, releasing the old hold cannot swing; another ordinary swing requires a fresh press after release, with no banked charge. Exact same-tick arbitration and authored transition timing remain contract work. No runtime change, numeric tuning or passed playtest. Original proposal and accepted scope are preserved in the canonical JSON.

**Decision — Jack, September 20, 2026: B.** Reply “2” selects a deliberate cancel before swing commitment, allowing the player to abandon a charge and take the pitch. Committed swings follow through. Cancel remains distinct from releasing to swing; its input, exact cutoff, priority and rearming behavior remain open. This does not select automatic hesitation detection or mid-swing rescue. Preserve PH-09–PH-12 and geometric ball/strike judgment. Direction only; no runtime change or passed playtest. Evidence: this task’s PH-13 reply; full provenance is in the canonical JSON.

**Validation requirements:** cancel before commitment must take the pitch without a later button release accidentally swinging; cancel after commitment cannot undo the swing or judgment. Teach and verify cancellation versus release, take and bunt withdrawal on both schemes and both seats. Exact binding and boundary decisions precede implementation.

Area: Hitting. Depends on: PH-11. Evidence: WII, SHOW-H.

- **A — Existing commitment:** Keep taking and committed swings; make release semantics clear.
- **B — Deterministic check/cancel:** Add a taught cancel boundary before commitment; more plate discipline control.
- **C — Assisted checking:** Infer hesitation and rescue some swings; easier but risks surprising judgments.

**Recommendation:** A initially; B only if Jack wants checking as a core verb. Avoid hidden random check-swing outcomes.

**Existing contract:** A release-based charge scheme needs an explicit distinction between canceling a load and swinging. No new binding selected.

**Acceptance:** Take, load-cancel, bunt withdrawal and swing cannot conflict; balls/strikes follow the same geometric zone.

### PH-14 — How much depth belongs in bunting?

**Refinement PH-14-R4 — Jack, September 21, 2026: hold to bunt.** Reply “1” selects holding the bunt action while positioning the bat; actual ball/bat contact resolves the bunt without a separate timed swing press. Releasing before contact withdraws the bat, with no ordinary swing or stale latched bunt. Holding alone does not guarantee contact. Preserve side changes until contact, bat-angle readability, quality-driven softness/control, live defense and two-strike foul-bunt rules. Exact bindings, transition/rearming precedence, animation timing and response curves remain open. Ordinary swing timing/charge remains separate. No runtime change, numeric trial or passed playtest.

**Refinement PH-14-R5 — Jack, September 21, 2026: two directional holds.** Reply “1” accepts LT / J holding a third-base-side bunt and RT / L holding a first-base-side bunt. Change held side before contact; latest press wins if both are held. Release all bunt holds to withdraw. Left stick / WASD retains positioning. This replaces the target West / V / Ctrl bunt verb; reconcile paired HowToPlay/docs/diagrams before implementation. Audit trigger modifiers, same-tick arbitration and live-play rearming; exact transition details and curves remain open. No runtime change or human gate passed. Original proposal provenance is preserved in the canonical JSON.

**Refinement PH-14-R3 — Jack, September 21, 2026: side changes until contact.** Reply “1” allows changing first-/third-base-side preference while squared up until contact. The bat angle visibly shows the current choice to the defense. Contact fixes the outgoing ball; later inputs cannot redirect it. Preserve quality-driven softness/control and no guaranteed landing. Exact bindings, contact-action/withdrawal behavior, defaults, animation transitions and response curves remain open. The cue belongs to authored presentation work; no runtime/art change, numeric trial or human gate passed. Full provenance is in the canonical JSON.

**Refinement PH-14-R2 — Jack, September 21, 2026: explicit bunt-side preference.** Reply “option 2.” selects first-base-side or third-base-side intent, with softness and accuracy earned through PH-14-R1 contact quality. A good bunt toward either side must not inherently require off-center contact. The preference biases the outgoing ball, not an exact landing spot, fair ball or safe result. Keep live defense, two-strike foul-bunt rules and horizontal coverage. This is bunt-specific; PH-12 still removes extra direction shaping from ordinary swings. Exact bindings, commitment timing, defaults, presentation and numerical curves remain open. No runtime change or passed playtest.

**Clarification preserved:** Jack asked “how would option 1 work?” The assistant explained that an off-center contact-to-direction mapping would need designing and could couple deliberate placement to poorer contact. Baseline `AtBatResolver.cs:121–128` suppresses bunt timing spray and uses supplied direction plus spread; it does not prove that proposed mapping. The assistant revised its recommendation to option 2 before Jack chose it. Full provenance is in the canonical JSON.

**Refinement PH-14-R1 — Jack, September 21, 2026: softness and control from contact quality.** Reply “1” accepts the bunt-specific response: good contact favors a soft controlled bunt; poor contact can pop up, travel too hard or go foul according to actual contact/pitch geometry. Better contact does not simply mean more swing-style exit power. Preserve live defense, legal contact and the two-strike foul-bunt rule; no guaranteed safe/out or automatic sacrifice. Exact response curves, directional placement controls and numbers remain open. This resolves the earlier quality proposal; no runtime change, numeric trial or passed playtest. Full provenance and proposed validation are in the canonical JSON.

**Decision — Jack, September 20, 2026: A.** “1. Bunt quality should be determined by contact quality?” The option selection accepts simple situational bunting: square up, make contact, readable foul risk and live defense. Preserve two-strike foul-bunt rules. The contact-quality question is recorded separately below; no new response curve, placement input, timing task or number is accepted. Direction only; no runtime change or passed playtest.

**Original contact-quality proposal — direction accepted September 21 through PH-14-R1:** use actual contact quality with a bunt-specific response: good contact deadens and controls the ball; poor contact can produce a pop, excessive pace or a foul according to actual contact and pitch geometry. Avoid mapping better bunt quality to ordinary swing power or guaranteeing a safe/out result. The baseline already uses quality/height for bunt pops (`AtBatResolver.cs:107–114`) and inherits swing quality/power for exit speed (`:81–89`); that is not proof of an appropriate dedicated bunt-quality curve. Exact response mapping and any placement input remain open. Evidence and Jack’s wording are preserved in the canonical JSON.

Area: Hitting. Depends on: PH-09, PH-13. Evidence: GC, WII, SMB-EI.

- **A — Simple situational bunt:** Preserve existing square/contact behavior and teach placement and foul risk.
- **B — Richer bunt control:** Add placement or timing demands for sacrifice/drag intent; increases state and input work.
- **C — Defer expansion:** Keep the current verb while first settling ordinary swings.

**Recommendation:** A/C during initial design, then test B only against a concrete situational need.

**Existing contract:** Existing S-18/19 and two-strike foul-bunt rules remain; do not create a guaranteed CPU exploit.

**Acceptance:** A bunt is a live ball with an earned outcome; both humans and CPU can defend it and a foul bunt with two strikes retires the batter.

### PH-15 — How much should characters, hands and gear change the duel?

**Refinement PH-15-R7 — Jack, September 21, 2026: Contact changes placement forgiveness only.** Reply “1” selects a more forgiving spatial contact area/sweet spot for higher Contact, with the same swing timing window across hitters. Contact does not add character-specific timing assistance. Preserve geometric contact, timing-led direction and shared timing-edge weakening, independent Power, bunt-specific quality response and fixed human assistance. Exact region sizes, shapes, quality boundaries and visible representation remain open. The shared quick/charged swing tradeoff is still PH-11 contract work. No runtime change, numeric trial or passed playtest. Full provenance is in the canonical JSON.

**Refinement PH-15-R6 — Jack, September 21, 2026: four separate pitching attributes.** Reply “1” selects independently authored Velocity (pitch speed), Movement (natural break), Control (player steering correction) and Endurance (resistance to fatigue). Natural break and player-added correction remain distinct within shared controls. Preserve pitch-family identities, the charge/correction tradeoff, primary peak-power fatigue with a tentative-only secondary steering penalty, and equal CPU action limits. Exact ratings, formulas, gear, display, migration and interactions remain open. D7 still precedes speed tuning. No runtime change, numeric trial or passed playtest. Full provenance is in the canonical JSON.

**Refinement PH-15-R5 — Jack, September 21, 2026: separate Contact and Power.** Reply “1” selects independently authored batting Contact (contact reliability/forgiveness) and Power (hitting strength), preserving the shared player-controlled geometric contact model. Exact effects on contact regions or timing, rating scale, character values, gear interactions and migration from the existing Bat rating remain open. No automatic hit probability, new aim control or HUD is selected. Preserve PH-10, PH-17 and bunt-specific quality response. No runtime change, numeric trial or passed playtest. Full provenance is in the canonical JSON.

**Refinement PH-15-R3 — Jack, September 21, 2026: individual role-player repertoires.** Reply “2” selects an authored fastball-plus-two repertoire per role player instead of automatic faction-captain inheritance. Combinations may repeat; this does not require a unique pair for every character. Shared controls and the three-pitch limit remain, and role players gain no captain Star Pitches. Exact assignments, traits and tuning remain open. No runtime change or passed playtest. Full provenance is in the canonical JSON.

**Refinement PH-15-R4 — Jack, September 21, 2026: all 18 starting role-player assignments accepted.** Reply “1” accepts the complete reviewed set below; each player gets a fastball plus the listed pair. Existing hands are unchanged. These are starting design assignments, not proven strengths or implemented mechanics. Traits, detailed pitch behavior, selection controls and tuning remain open. Full assignments, original proposal provenance and acceptance evidence are in the canonical JSON.

- **Spark League:** Nico — Changeup + Curveball; Pip — Changeup + Slider; Marlow — Curveball + Sinker; Gull — Slider + Sinker.
- **Royal Rink:** Frost — Curveball + Slider; Lace — Changeup + Sinker; Pewter — Curveball + Sinker.
- **Carnival Crew:** Dart — Changeup + Slider; Jester — Changeup + Curveball.
- **Goldrush:** Boom — Slider + Sinker; Hex — Changeup + Curveball; Nugget — Changeup + Sinker.
- **Canopy Clan:** Vine — Curveball + Slider; Moss — Curveball + Sinker; Basil — Changeup + Sinker.
- **Ember Keep:** Cinder — Slider + Sinker; Grit — Changeup + Sinker; Soot — Curveball + Sinker.

This set was accepted as one batch. Detailed rationales and existing throwing hands are in the canonical JSON; no hand, trait, numeric strength or captain special changes are selected.

**Refinement PH-15-R1 — Jack, September 21, 2026: universal fastball.** Reply “1” selects a fastball for every pitcher plus exactly two character-specific choices from changeup, curveball, slider and sinker. Keep shared controls/contact rules; charge modifies a pitch and stars remain outside the count. Universal access does not imply identical fastball strengths. Character assignments, traits, inputs and tuning remain open; no runtime change or passed playtest. Full provenance and composition contract are in the canonical JSON.

**Refinement PH-15-R2 — Jack, September 21, 2026: starting captain assignments accepted.** Reply “1” selects the seven assignments below, with a fastball added to each pair. These accept repertoire membership and its intended character roles, not exact traits or proven balance. Role-player assignments, controls, detailed weaknesses and tuning remain open. No runtime change or passed playtest. The original proposal and its resolution remain in the canonical JSON.

- **Rio Sparks:** Changeup + Curveball. Balanced timing and movement choices.
- **Queen Vale:** Curveball + Slider. Two different breaking shapes for a pitching specialist.
- **Zig:** Changeup + Slider. Timing changes and lateral movement for a tricky repertoire.
- **Brondo:** Changeup + Sinker. Slow/fast contrast with a dipping alternative.
- **Konga:** Curveball + Sinker. Contrasting arcing and faster dipping shapes.
- **Ashlord:** Slider + Sinker. Lateral and dipping movement alongside the fastball.
- **Elder Fenn:** Changeup + Curveball. Patient timing changes and a pronounced arc; shares Rio’s families, with traits to distinguish later.

Seven captains share six possible pairs, so Rio and Fenn share pitch families; distinct traits remain a later proposal. PH-15-R4 separately accepts the role-player assignments above.

**Decision — Jack, September 20, 2026: B.** Reply “2” selects distinct pitch repertoires and recognizable pitching/hitting traits within shared controls and contact rules. Differences add matchup strategy while preserving one model to learn. Exact repertoires, trait definitions, character assignments, numerical strengths and gear interactions remain open. No unique control scheme, separate character contact resolver, new captain or second rig. Direction only; no runtime change or passed playtest. Evidence: this task’s PH-15 reply; full provenance is in the canonical JSON.

Area: Roster. Depends on: PH-02, PH-10. Evidence: GC, WII.

- **A — Shared verbs, stat strengths:** One core model with readable differences in power, coverage, break and endurance.
- **B — Distinct repertoires/traits:** Different tools inside the same model; more identity and matchup teaching.
- **C — Highly specialized mechanics:** Character-specific control or resolution rules; highest maintenance and fairness cost.

**Recommendation:** A with selected B traits if the chosen repertoire supports them. Preserve the shared rig and systems.

**Existing contract:** No new captain, second skeleton or one-captain resolver. Equipment and chemistry must be included in balance trials.

**Acceptance:** Rio and Ashlord, both hands, contact/power archetypes and relevant gear all follow the same taught rules.

### PH-16 — How should stars and chemistry affect plate decisions?

**Refinement PH-16-R13 — Jack, September 21, 2026: shared charge tradeoffs for specials.** Reply “1” selects more power with less steering correction for charged Star Pitches, and more power with less spatial forgiveness for charged Star Swings, alongside their ability effects. Ability-specific contact-area changes remain allowed. Preserve characteristic movement, ordinary timing windows, geometric contact, charge/overcharge behavior and release-time commitment. Exact formulas, bounds, ability-area composition and character/fatigue interactions remain open; no charge-dependent resource price or numeric multiplier is selected. D7 and D6 remain. No runtime change, numeric trial or passed playtest. Full provenance is in the canonical JSON.

**Refinement PH-16-R12 — Jack, September 21, 2026: ordinary fallback; Star-counter feedback candidate.** Jack replied “1, maybe the indication is where the star count is shown. it flashes red or something.” An unaffordable released special performs the ordinary action at the intended timing, spends no Stars and clearly indicates unavailability. Pitching uses the already selected ordinary family; no blocked action or later upgrade. The proposed Star-counter location and red flash remain tentative presentation details. Exact feedback, resource ordering and input edges remain open; use ordinary charge/contact behavior. No runtime or presentation change, numeric trial or passed playtest. Full provenance is in the canonical JSON.

**Refinement PH-16-R11 — Jack, September 21, 2026: choose special at release.** Reply “1” selects checking the held modifier at accepted normal action-button release, allowing changes while charging but locking the ordinary/special attempt afterward. Later modifier changes cannot upgrade or downgrade that attempt. This is the input release, not a later animation marker or physical ball release. Ordinary pitch-family locking stays at charge start. Bindings, charge effects, resource validation/deduction, insufficient-resource behavior and same-tick arbitration remain open. Preserve cancellation/rearming, full miss cost and actual contact. No runtime change, numeric trial or passed playtest. Full provenance is in the canonical JSON.

**Refinement PH-16-R10 — Jack, September 21, 2026: modifier plus normal action.** Reply “1” selects holding a special modifier while using the usual pitch/swing action, preserving the charge-and-release rhythm instead of a dedicated special action button. Exact bindings, special-intent sampling/locking, special charge effects, insufficient-resource behavior, deduction and cancellation/bunt interactions remain open. Preserve shared commitment rules, full miss cost, team resources and role-appropriate special ownership. Eventual controls require paired HowToPlay/docs changes. No runtime change, numeric trial or passed playtest. Full provenance is in the canonical JSON.

**Refinement PH-16-R9 — Jack, September 21, 2026: one special per side of the duel.** Reply “1” selects one Star Pitch and one Star Swing per character, separate from the three ordinary pitches, with direct activation rather than selecting among multiple specials. This does not require a unique effect for every character or choose particular assignments, bindings or charge interactions. Preserve captain-only highest-cost specials, lower-cost role-player specials, ability-specific contact areas and normal Star Pitch timing. Ability designs and prices remain open. No runtime change, numeric trial or passed playtest. Full provenance is in the canonical JSON.

**Refinement PH-16-R8 — Jack, September 21, 2026: captain-exclusive highest-cost specials.** Reply “1” reserves the highest-cost tier for captains, with role players using lower-cost specials and no highest-tier exceptions. This resolves PH-16-R7’s tentative access idea; its original evidence remains in history. Eligibility does not grant every highest-tier special to every captain. Specific abilities, assignments, loadouts, tier count and prices remain open. Preserve ordinary-play usefulness, shared resource rules, readable counterplay and D6. No runtime change, numeric trial or passed playtest. Full provenance is in the canonical JSON.

**Refinement PH-16-R7 — Jack, September 21, 2026: cost tiers accepted; captain access tentative.** After illustrative examples, Jack replied “okay sure. and maybe captains have access to the higher cost ones”. This accepts a small set of ability cost tiers based on strength and utility. The captain-access suggestion remains tentative; it does not yet exclude role players or assign abilities. Wide Barrel, Hook Pitch and Quake Swing were explanatory examples, not accepted designs; neither three tiers nor exact prices were selected. Tier count, costs and assignments remain open. Preserve full normal miss cost, shared pools, opening access and counterplay. No runtime change, numeric trial or passed playtest. Full provenance is in the canonical JSON.

**Refinement PH-16-R6 — Jack, September 21, 2026: usable starting Star reserves.** Reply “1” selects starting resources for both teams so each can use a special from the opening plate appearance. This permits early use without requiring it or making every ability affordable. Exact starting balances, capacity, prices, eligible opening options and later reset rules remain open. Preserve shared team pools, the accepted earning policy, full miss cost and supplementary special frequency. No runtime change, numeric trial or passed playtest. Full provenance is in the canonical JSON.

**Refinement PH-16-R5 — Jack, September 21, 2026: steady gains plus performance bonuses.** Reply “1” selects modest Star gains for both teams after completed plate appearances, plus bonuses for successful plays. Each team receives gains in its own shared pool. Exact base/bonus amounts, bonus events, stacking/cap rules and completion-edge cases remain open. This does not select real-time or per-pitch regeneration, starting resources, prices or a score-deficit multiplier. Preserve supplementary special frequency. No runtime change, numeric trial or passed playtest. Full provenance is in the canonical JSON.

**Refinement PH-16-R4 — Jack, September 21, 2026: one shared team Star pool.** Reply “1” selects one pool per team for both Star Pitches and Star Swings. Spending on pitching leaves less for batting and vice versa; opposing teams have independent balances. Capacity, starting amount, earning events/rates, prices, resets and UI remain open. Preserve supplementary special frequency, full missed-swing cost and accepted Star Pitch/Swing rules. No runtime change, numeric trial or passed playtest. Full provenance is in the canonical JSON.

**Refinement PH-16-R3 — Jack, September 21, 2026: full cost on a missed Star Swing.** Reply “1” selects the attempted ability’s full normal resource cost even on a whiff, with no miss discount/refund. Preserve ability-specific contact areas and actual geometric contact. Exact prices, deduction/reservation timing, pre-commit cancellation, foul policy and earning rates remain open. Reconcile the inspected baseline flat miss cost wherever it differs from the normal ability price before implementation. No runtime change, numeric trial or passed playtest. Full provenance is in the canonical JSON.

**Refinement PH-16-R2 — Jack, September 21, 2026: ability-specific contact-area changes.** Jack corrected the initial “1” with “i mean 2. depending on the ability, it can impact contact area”. Option 2 is authoritative: an individual Star Swing may change its spatial contact area, including extra forgiveness; this is not a universal bonus. Actual geometric contact remains required. Eligible abilities, region shapes/sizes, quality boundaries, visible representation, costs and Contact/charge interactions need separate review. No timing-window change, automatic hit, guaranteed outcome or specific ability implementation is approved. The initial reply and correction are preserved in the canonical JSON. No runtime change, numeric trial or passed playtest.

**Refinement PH-16-R1 — Jack, September 21, 2026: Star Pitches keep normal timing.** Reply “1” selects the ordinary hitter timing window for Star Pitches, with challenge coming from readable ball behavior such as movement or speed changes. Different arrival times still require correct timing; the allowable window itself is not narrowed. Reconcile timing-reducing Star Pitches in gameplay-spec §13 and ability data, and review replacement effects separately before implementation. No specific replacement, speed/shape value, cost, presentation, Star Swing effect or park modifier is selected. Preserve D6, CPU fairness and D7 before speed tuning. No runtime change, numeric trial or passed playtest. Full provenance is in the canonical JSON.

**Decision — Jack, September 20, 2026: A.** Reply “1” selects stars and chemistry as supplements to the ordinary pitching/hitting duel. Ordinary play remains satisfying on its own; specials create occasional tactical opportunities while retaining readable counterplay and never guaranteeing hits or outs. Exact rates, costs, bonuses, effects and frequency remain open; D6 and existing special-ability restrictions remain. Direction only; no runtime change or passed playtest. Evidence: this task’s PH-16 reply; full provenance is in the canonical JSON.

Area: Resources. Depends on: PH-02, PH-10, PH-15. Evidence: GC, WII.

- **A — Keep specials supplementary:** Ordinary baseball stays complete; specials briefly bend a readable rule.
- **B — More central resource tactics:** Increase star/chemistry influence; adds draft and at-bat resource decisions.
- **C — Ordinary duel first:** Evaluate baseline with specials off, then restore existing special interactions.

**Recommendation:** C for isolation, A for integration; B requires explicit acceptance of how much the resource dominates.

**Existing contract:** Gameplay-spec D6 prohibits free homers; no full-screen blinds or automatic outs. Existing systems remain until changed deliberately.

**Acceptance:** Each special retains a response and an understandable resource cost, including a miss; baseline balance is tested without specials.

### PH-17 — How should different skill levels share the same game?

**Decision — Jack, September 21, 2026: C.** Reply “2” selects one fixed challenge: everyone uses identical difficulty and forgiveness within the shared batter-linked cursor model. The conversation offered two choices; its option 2 maps to canonical C, not B. No optional or per-seat mechanical assistance is selected. PH-19 still decides teaching and post-pitch feedback; PH-18 decides CPU information/commitment policy. Exact shared tuning and reconciliation of existing difficulty multipliers remain open. Direction only; no runtime change or passed playtest. Evidence: this task’s PH-17 reply; full provenance is in the canonical JSON.

Area: Accessibility. Depends on: PH-09, PH-10. Evidence: GC, SMB4, SHOW-H.

- **A — Shared rules with explicit assists:** Tune forgiveness and teaching while keeping common trajectory and judgment rules.
- **B — Separate hitting interfaces:** Timing/position/zone modes; more preference coverage, more testing and fairness work.
- **C — One fixed challenge:** Simplest balance surface; least support for mixed-skill couch play.

**Recommendation:** A. Decide whether assists are per-seat and visible before numbers; do not silently rubber-band outcomes.

**Existing contract:** Existing difficulty multipliers are baseline. Keyboard/mouse remains P1; pad 2 remains a real second gamepad.

**Acceptance:** A new player and an experienced friend can play together and understand what help each receives.

### PH-18 — What information and commitment rules may CPU players use?

**Decision — Jack, September 21, 2026: A.** Reply “1” selects human-equivalent CPU limits: the same legal actions, available cues and committed swings that late legal steering can fool. No future-input knowledge, hidden destination access or privileged post-commit correction. Exact observation model, timing and CPU skill tuning remain open; selectable difficulty is not newly accepted and PH-17’s fixed human forgiveness remains. Audit current live/headless behavior and endpoint aiming before implementation. No simulation shortcut is approved by this choice. Direction only; no runtime change or passed playtest. Evidence: this task’s PH-18 reply; full provenance is in the canonical JSON.

Area: CPU. Depends on: PH-03, PH-04, PH-06. Evidence: Legacy.

- **A — Shared legal actions and bounded reads:** Use the same action limits, explicit observations and commitment windows; difficulty changes skill.
- **B — Documented simulation shortcuts:** Permit limited headless approximations with separate validation of live fairness.
- **C — Current policy pending audit:** Retain baseline until the live/headless input asymmetries are fully traced.

**Recommendation:** A as the target; audit C first. Any B shortcut must not become a hidden advantage against a human.

**Existing contract:** Current live CPU commits about .30 s before plate arrival, while final crossing judges contact; endpoint aiming also differs from human inputs.

**Acceptance:** Late legal steering can defeat a committed CPU read; CPU cannot use future inputs, reserved human actions or impossible location access.

### PH-19 — What feedback teaches the duel without giving it away?

**Decision — Jack, September 21, 2026: A.** Reply “1” selects brief causal feedback after the pitch, explaining timing, contact placement or charge when relevant, with optional deeper practice detail. Preserve PH-06’s ordinary-play information rules and PH-17’s identical forgiveness. Feedback must describe actual outcomes: MAX is charge, and contact-quality words require actual contact. Exact copy, layout, duration and practice interface remain open; coordinate #770 and the paired HowToPlay/docs changes. Direction only; no runtime/presentation change or passed playtest. Evidence: this task’s PH-19 reply; full provenance is in the canonical JSON.

Area: Understanding. Depends on: PH-06, PH-10, PH-11. Evidence: SMB4, SHOW-H.

- **A — Brief causal feedback:** Explain timing, coverage and charge after the pitch, with optional deeper practice detail.
- **B — Continuous aim coaching:** Show substantial guidance during flight; easier learning, weaker deception.
- **C — Minimal HUD explanation:** Rely on body, ball and sound; strongest immersion but harder diagnosis.

**Recommendation:** A supported by readable body/ball cues. Training can expose more; ordinary play should not disclose private pitch targets.

**Existing contract:** MAX describes charge; hit-quality words follow actual contact. Coordinate tutorials with #770 and paired HowToPlay/docs changes.

**Acceptance:** A stranger can name why a miss or weak hit occurred and deliberately improve on the next attempt without external instructions.

### PH-20 — What evidence will make the chosen system ready to ship?

**Decision — Jack, September 21, 2026: A.** Reply “1” selects small serial playable steps after required baseline checks: define and test each connected mechanic, then have Jack judge the named standalone revision. Preserve D7 and all applicable evidence prerequisites, scoped numeric trials and human acceptance. No broader baseline-only milestone is required by default. Keep gameplay, presentation/book/tutorial and art scopes separate; resolve the remaining detailed contracts before opening implementation children. This is delivery direction acceptance, not approval of an implementation, merge, coefficient or passed playtest. Evidence: this task’s PH-20 reply; full provenance is in the canonical JSON.

Area: Delivery. Depends on: PH-01. Evidence: Legacy.

- **A — Serial trials and standalone sitting:** Approve concepts, then numeric trials, then test and let Jack accept actual play.
- **B — Large combined redesign:** Evaluate many coupled changes at once; harder to attribute improvement or regression.
- **C — Evidence-only first milestone:** Measure current play before any new mechanic trial; slower expansion with a clearer baseline.

**Recommendation:** A, beginning with C for D7 and identified evidence gaps. Do not convert a recommendation or successful unit test into human acceptance.

**Existing contract:** Existing #346/#534 gates stay open. One implementation child per bounded worktree; presentation and art remain separate.

**Acceptance:** Named revision, both schemes, two pads, both hands, representative captains, ordinary/star cases, reproducible tests and Jack’s dated acceptance.

## Proposed sequence after the direction round

The order below organizes the accepted small-step approach; it does not select unresolved mechanics or numerical targets. Each step gets a concrete contract and relevant evidence before implementation. Keep the existing IDs and append refinements to their history rather than starting another broad questionnaire.

1. **Define the ordinary pitching duel (PH-02–06, PH-15, PH-18).** Use three ordinary pitches per pitcher from the accepted fastball/changeup/curveball/slider/sinker library; refine their readable shapes, tactical purposes and weaknesses, then implement the accepted RB/Tab cycling flow with Fastball reset under PH-02-R4/R5 only after its input/state contract is complete, with pitch family locked at charge start under PH-02-R3, within the accepted fastball-plus-two composition and PH-15-R2/R4 assignments for all 25 existing characters. Charge modifies a pitch and stars remain outside the count. Consolidate mound/shape location, live steering, power/control behavior and equal CPU action limits into one reviewable contract. Audit current CPU endpoint targeting before deriving fair controls. Gather required baseline evidence, including the named D7 sitting before any pitch-speed tuning.
2. **Define contact and swing commitment (PH-09–14, PH-17).** Preserve the batter-linked cursor, placement-led quality and timing-led direction. Use PH-13-R1 cancel/conversion/rearm behavior and settle same-tick arbitration; remove direct ordinary-hit spray/loft from the target contract, and specify the accepted contact-quality-driven bunt response under PH-14-R1 plus the PH-14-R2 first-/third-base-side preference, apply PH-14-R3 side changes until contact, use PH-14-R4 held geometric contact and release-to-withdraw, use PH-14-R5 directional holds, then validate same-tick swing/bunt arbitration and PH-13-R1 rearming. Reconcile existing difficulty multipliers with fixed forgiveness. Resolve the changed gameplay-spec sections before code; retain geometric judgments and live defense.
3. **Integrate effort, character traits and specials (PH-05, PH-08, PH-15–16).** Trial immediate power/control costs and accumulated fatigue together so their interaction is understandable. Check distinct character strengths and supplementary specials against the ordinary duel, including counterplay. Pool sizes, costs and strengths remain proposals until scoped trial acceptance.
4. **Present and judge each playable step (PH-07, PH-19–20).** Schedule separate presentation/book/tutorial work alongside the mechanic it teaches: actual causal feedback, readable animation and modest breathing room for highlights, side changes and special abilities. Keep pitch travel, reaction time and presentation durations distinct. Validate the named revision, then Jack judges the standalone with the required schemes, two pads, both hands and representative ordinary/special cases before the next coupled step.

Do not postpone all teaching and playtests until step 4: its delivery requirements apply to every earlier playable step. No speculative implementation issues are opened by this sequence. The unresolved pitch specifications, input bindings, fatigue curves, bunt response curves and placement, timing values and presentation treatments remain visible work, even though all 20 direction rows are accepted.

## Work and acceptance checklist

- [x] Inspect previous #693/#708 tracker conventions and existing #534 scope.
- [x] Research both Mario titles separately using original manuals.
- [x] Compare Super Mega Baseball, MLB The Show 25 and Wii Sports with edition-specific sources.
- [x] Map the inspected Harbor baseline and preserve source hashes.
- [x] Seed stable decision IDs with alternatives, dependencies and proposed acceptance.
- [x] Resolve PH-01: tactical depth plus reliability/teaching (Jack, September 20).
- [x] Resolve PH-09: keep the batter-linked horizontal cursor (Jack, September 20).
- [x] Resolve PH-02: a small set of distinct ordinary pitches with clear purposes and weaknesses (Jack, September 20).
- [x] Resolve PH-03: horizontal mound positioning plus pitch-defined height (Jack, September 20).
- [x] Resolve PH-04: live left/right steering after release with readable limits (Jack, September 20).
- [x] Resolve PH-05: stronger power-versus-control tradeoff within charge/release (Jack, September 20).
- [x] Resolve PH-06: body and ball cues for ordinary play (Jack, September 20).
- [x] Resolve PH-10: placement-led quality, timing-led direction with edge weakening and outside-window misses (Jack, September 20).
- [x] Resolve PH-11: one-button quick/charged swings with overcharge and useful quick swings (Jack, September 20).
- [x] Resolve PH-12: no extra directional input for ordinary hit shaping (Jack, September 20).
- [x] Resolve PH-13: deliberate cancel before swing commitment; committed swings follow through (Jack, September 20).
- [x] Resolve PH-14: simple situational bunting; contact-quality follow-up recorded separately (Jack, September 20).
- [x] Resolve PH-15: distinct repertoires/traits within shared controls and contact rules (Jack, September 20).
- [x] Resolve PH-16: stars/chemistry supplement the ordinary duel and preserve counterplay (Jack, September 20).
- [x] Resolve PH-08: gradual visible fatigue driven mainly by effort (Jack, September 20).
- [x] Resolve PH-17: one fixed challenge with identical difficulty and forgiveness (Jack, September 21; presented option 2 = canonical C).
- [x] Resolve PH-18: human-equivalent CPU action limits, bounded reads and committed swings (Jack, September 21).
- [x] Resolve PH-19: brief causal post-pitch feedback with optional deeper practice detail (Jack, September 21).
- [x] Resolve PH-07: keep pace pending replay, with slight breathing room for animations and game moments if needed; D7 remains (Jack, September 21).
- [x] Resolve PH-20: small serial playable steps after required baseline checks, with Jack’s standalone acceptance (September 21).
- [x] Consolidate the proposed sequence and unresolved contracts after the broad direction round.
- [x] Refine PH-02-R1: three ordinary pitches per pitcher from a shared library, character-specific selections, charge modifier and stars outside the count (Jack, September 21).
- [x] Refine PH-02-R2: five-family library (fastball, changeup, curveball, slider, sinker) and proposed game roles (Jack, September 21).
- [x] Refine PH-15-R1: every pitcher has a fastball plus two character-specific pitches (Jack, September 21).
- [x] Refine PH-15-R2: accept the seven captain starting repertoires, including shared Rio/Fenn families (Jack, September 21).
- [x] Refine PH-15-R3: individually authored role-player ordinary repertoires (Jack, September 21).
- [x] Refine PH-15-R4: accept all 18 role-player starting repertoires as reviewed (Jack, September 21).
- [x] Refine PH-02-R3: select pitch type before charge and lock it at charge start; preserve common charge/release and live steering (Jack, September 21).
- [x] Refine PH-02-R4: one button cycles through the three ordinary pitches before charge (Jack, September 21).
- [x] Refine PH-02-R5: RB/Tab cycling, Fastball reset every pitch, shared charge/release and no active-selection disclosure (Jack, September 21).
- [x] Refine PH-05-R1: charge power reduces player steering correction while retaining characteristic pitch movement and live steering (Jack, September 21).
- [x] Refine PH-08-R1: fatigue primarily lowers peak power; a small secondary steering penalty remains tentative (Jack, September 21).
- [x] Refine PH-14-R1: actual contact quality governs bunt softness and control; earlier quality proposal resolved (Jack, September 21).
- [x] Refine PH-14-R2: explicit first-/third-base-side bunt preference with contact-quality execution (Jack, September 21).
- [x] Refine PH-14-R3: side changes while squared up until contact; bat angle shows intent (Jack, September 21).
- [x] Refine PH-14-R4: hold-to-bunt with geometric contact; release before contact withdraws (Jack, September 21).
- [x] Refine PH-14-R5: LT/J third-side, RT/L first-side, latest press wins, release all withdraws, left stick/WASD positions (Jack, September 21).
- [x] Refine PH-13-R1: East/G cancel, direct load-to-bunt conversion, release commitment and fresh-press rearming (Jack, September 21).
- [x] Refine PH-15-R5: independently authored batting Contact and Power (Jack, September 21).
- [x] Refine PH-15-R6: independently authored pitching Velocity, Movement, Control and Endurance (Jack, September 21).
- [x] Refine PH-15-R7: Contact grants spatial placement forgiveness only; shared hitter timing window (Jack, September 21).
- [x] Refine PH-11-R1: charged swings trade spatial placement forgiveness for power with unchanged timing (Jack, September 21).
- [x] Refine PH-08-R2: fatigue persists per character for the match without recovery or swap reset (Jack, September 21).
- [x] Refine PH-08-R3: pitching-effort-only fatigue; no extra cost from hits/home runs/runs allowed (Jack, September 21).
- [x] Refine PH-09-R1: normal horizontal box movement while loading; no charging slowdown (Jack, September 21).
- [x] Refine PH-16-R1: Star Pitches keep the ordinary timing window; replacement effects need separate review (Jack, September 21).
- [x] Refine PH-16-R2: ability-specific Star Swing contact-area changes; corrected option 2 (Jack, September 21).
- [x] Refine PH-16-R3: full normal resource cost on a missed Star Swing (Jack, September 21).
- [x] Refine PH-16-R4: one shared Star Pitch/Swing resource pool per team (Jack, September 21).
- [x] Refine PH-16-R5: completed-plate-appearance gains for both teams plus performance bonuses (Jack, September 21).
- [x] Refine PH-16-R6: usable starting Star reserves for both teams (Jack, September 21).
- [x] Refine PH-16-R7: tiered ability costs; higher-cost captain access remains tentative (Jack, September 21).
- [x] Refine PH-16-R8: highest-cost specials are captain-exclusive; prior tentative idea resolved (Jack, September 21).
- [x] Refine PH-16-R9: one Star Pitch and one Star Swing per character (Jack, September 21).
- [x] Refine PH-16-R10: held special modifier plus normal pitch/swing action (Jack, September 21).
- [x] Refine PH-16-R11: special intent locks at accepted action-button release (Jack, September 21).
- [x] Refine PH-16-R12: ordinary fallback without Star spend; Star-counter red flash remains a candidate (Jack, September 21).
- [x] Refine PH-16-R13: ordinary charge tradeoffs alongside special effects (Jack, September 21).
- [ ] Refine PH-16-R14 plate-level chemistry effect, then consolidate remaining contracts; compare the tentative fatigue steering effect.
- [ ] Collect matched reference observations and the required D7 standalone re-sit.
- [ ] Accept explicitly scoped numerical trials and reconcile any superseded spec decisions.
- [ ] Implement serial gameplay children; separate presentation/book/tutorial and conditional art children.
- [ ] Run changed-mechanic scenarios, appropriate full regression/cohort checks and visual verification.
- [ ] Jack accepts the named standalone revision with both schemes and two pads.

A research-documentation merge may complete the foundation deliverable; it does not close the design tracker or #534/#346. No gameplay modification, standalone delivery, or passed human gate is claimed by this packet.
