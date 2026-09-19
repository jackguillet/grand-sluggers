# Tutorials — every mechanic has a playable lesson

Status: **first six lessons implemented in preview; expansion and human learning gates pending**, September 19, 2026. Gameplay child #772 implements the catalog and six headless lessons; the Tutorials screen is #774, with pointer correction #776. Three-success mastery is #778 and its separate presentation child. Tracker: [#770](https://github.com/jackguillet/grand-sluggers/issues/770). This is gameplay foundation work serving #209 and #342, before generating more artwork. The reference is Super Sluggers' approachable party baseball; the lessons teach Grand Sluggers' own accepted rules and controls.

## Product contract

Every player-facing mechanic must have a discoverable, repeatable tutorial in a **Tutorials** section. Teach individual actions (throw a strike, throw a changeup, make a slap hit, dive) and combinations (scoop → throw → force out; feed second → receive → throw first). A controls page supports the lesson; it does not replace doing the play.

A new or changed mechanic must update its tutorial coverage in the same PR: stable mechanic id, lesson id, setup, player-owned action, success and failure evidence, rule/profile dependencies, regression scenario, and implementation state. A planned lesson is tracked debt, never completed coverage. During migration, existing gaps have named owners; new mechanics cannot quietly add gaps. Before a mechanic is presented as fully taught, its playable lesson must ship and pass the human learning check.

Tutorials support the existing Harbor Exhibition slice. They do not unlock Challenge, extra parks, new characters, another input toolkit, or an art-generation pass. Keep the existing characters, field, cameras, and input path; improve them in their owning sessions when a learning check exposes a failure.

## What exists at the reviewed revision

Reviewed `05471a60`, including PR #769, against the current GitHub tracker. This is a source review and headless baseline, not a standalone sitting.

- `src/GrandSluggers.Sim/Training.cs` has five broad lesson categories plus Free Practice, choice/skip, progress flags, and a runner-on-first setup for a double play. Earlier training work (#16, #71, #224, #234) is closed; reuse it.
- `unity/Assets/Scripts/Runtime/TrainingDirector.cs` connects that session to the existing at-bat/fielding loop. Title entry and a Practice description already exist. The proposed Tutorials section expands that product rather than adding a competing mode.
- `src/GrandSluggers.Sim.Tests/ScenarioHarness.cs` prepares runners/outs and replays seat commands through the sim, but lives in the test assembly and depends on xUnit. Extract reusable setup/command concepts into production Sim; do not make Unity reference test code.
- `Match.StationRunner`, `Match.SetOuts`, `LivePlaySystem`, typed outcomes, and play/race traces provide useful foundations for reproducible lessons and evidence.
- Current progress is too broad for this requirement. `RecordPitch` counts in-zone pitches and MAX charges without a separate changeup objective. `RecordSwing` accepts any non-miss for its contact flag. `RecordTurnTwo` checks the aggregate ground-out/two-outs result without establishing the player's two commands. `Choose` resets only some counters/flags. These are migration targets, not verified learning gates.
- There is no data catalog relating every mechanic to a lesson, no coverage validator, and no general lesson setup/reset/objective lifecycle in the inspected Practice path. A passing scenario test is not a playable tutorial.

## One sim; a controlled exercise around it

The lesson supplies a situation, the CPU supplies an opportunity, and the player supplies the skill. The ordinary sim decides contact, catches, arrivals, tags, and outs.

**Setup.** Resolve the active content root/profile; choose Harbor, a supported lineup and handedness, count/outs, runners, possession or pitch/contact setup, seed, and teaching seat. Use validated sim setup APIs outside a live play. A fielding-only exercise may start from an authored ball-in-play state; a hitting exercise must earn contact from a real pitch. Do not search repeatedly for a lucky random outcome when the lesson starts.

**CPU actors.** A named lesson policy may take pitches, throw a specified pitch to a target, put a reproducible grounder into play, run a prescribed route, or cover the receiving bag. Scripts submit normal commands or use validated setup fixtures. They can constrain the opponent and teammates; they cannot catch, throw, dive, swing, or advance for the teaching seat and then credit the player. Demo mode may control that seat, clearly marked as a demonstration, with completion disabled.

**Player attempt.** Use the normal commands, seat ownership, active rules, abilities, and input bindings. The required action is explicit. If assistance normally scoops a grounder, a lesson about the throw can accept assisted possession; a lesson about manual pursuit must require manual movement evidence. A dive lesson must require the player's dive command and its actual catch, never a neutral-stick automatic dive. Any introductory help must be visible and followed by an attempt with the ordinary assistance policy.

**Evidence.** A named objective evaluator reads typed sim facts and the teaching seat's accepted command history. Counts/captions alone are insufficient. State whether the objective is contact, a fair hit, a catch, an out, or a particular sequence. Feedback explains the decisive miss (outside the zone, late swing, missed catch, wrong bag, runner beat the throw). Numeric tolerances come from the active rules or an explicitly authored lesson target, never a hidden easier baseball model.

**Lifecycle.** Select → brief → optional demonstration → ready → attempt → feedback → retry / next / exit. Retry restores all relevant state: count, outs, runners, ball, fielders, selection, pending commands, recovery/status timers, stars/stamina, RNG, and per-attempt evidence. Every tutorial passes only after three distinct successful attempts. A failure does not remove an earlier success. Persist 0/3, 1/3, 2/3, and 3/3 by lesson revision/profile; retries and leaving/reopening a lesson keep earned progress. Duplicate input/results, CPU-only attempts, and demonstrations never add credit. Old single-success completion is retired through a lesson revision bump. No prior play evidence can satisfy the next attempt. Support immediate retry, pause, skipping, and direct lesson selection; no long compulsory sequence. Save learning progress separately from match state, with lesson-version identity so changed objectives can be marked for refresh without erasing unrelated progress.

**Transfer.** First offer a stable setup; then a small authored set of variations (side, pitch location, runner speed, character/handedness) and free practice. Expose prerequisites as recommendations, not mandatory locks. A player should recognize the same action in Exhibition afterward.

## First six lessons

These acceptance contracts are exercised by `TutorialSessionTests` on shipped and C80 roots. They are headless lessons; the legacy Practice screen does not yet expose them. “Slap hit” names the game's existing uncharged swing (the brief's “slap shot”). Existing S-ids are reusable regression anchors; add lesson-specific tests for setup, ownership, failure and retry.

1. **T-P01 — Throw a strike.** Human pitches; CPU takes. Reset to a fresh count, a consistent batter and neutral pitcher stamina. Pass on the player's delivery producing a called strike through the normal plate crossing. An out-of-zone delivery fails even if another CPU policy would chase it. Anchors: spec §4.4, S-01/S-02.
2. **T-P03 — Throw a changeup.** Human pitches; CPU takes; show the active changeup binding. Pass only when a player-commanded changeup crosses the intended in-zone target through the real flight. A fastball strike cannot pass. Teach the timing contrast against an ordinary pitch without inventing a second speed curve. Anchors: §4.1–4.4 and the existing changeup flight tests.
3. **T-B01 — Make a slap hit.** CPU repeats an ordinary hittable strike; human bats. Pass on a player-commanded uncharged, non-bunt swing producing fair contact. A charged hit, a foul, a miss, or CPU contact cannot pass. Feedback separates cursor placement and timing. Anchors: §5.1–5.3, S-07/S-08/S-09.
4. **T-F01 — Field a ground ball.** Start a verified routine grounder with a human teaching glove. Teach taking control, moving to the live ball and the current scoop action. Pass on the player's required movement/scoop evidence and secure ground possession; a subsequent throw is a separate step. A fly catch cannot pass. Anchors: §8.2–8.3, S-31/S-33, S-94–S-99.
5. **T-F05 — Dive for an out.** Present a reproducible catchable airborne ball near the dive reach, with sufficient reaction opportunity under the active profile. Pass on the human dive command and an airborne catch out by that diver. A ground-ball pickup, automatic assisted dive, standing catch, or missed dive cannot pass. Teach recovery as part of the result. Anchors: §8.3–8.4 and the catch/dive/recovery suites.
6. **T-D02 — Turn a double play.** Runner on first, no outs, an authored makeable SS grounder, CPU runner and covering fielders. Human scoops, chooses/throws to second, then chooses/throws to first after the receiver handoff. Pass on both human-issued throws, the correct runner identities and two geometric force outs in order. One out, an unrelated two-out play, or an automatically supplied second throw cannot pass. Anchors: §10.4, S-40–S-50. Verify feasibility for each supported profile; do not retune an unmakeable race to rescue the lesson.

## Coverage backlog

The first six ids are **implemented in the headless runner**. The other ids remain planned or blocked; `cli tutorials` is the current coverage report. Existing broad Practice behavior is partial reuse, not tutorial completion. This is the initial inventory; the catalog implementation must reconcile it against every player-facing spec section, control verb, ability and enabled feature. No unsupported future mechanic is enabled by appearing here.

- **Pitching:** T-P01 strikes; T-P02 aim/location and balls versus strikes; T-P03 changeup; T-P04 charge/MAX; T-P05 break after release; T-P06 rubber positioning; T-P07 stamina/pitcher substitution; T-P08 pickoff; T-P09 star pitch (scope dependent).
- **Batting:** T-B01 slap contact; T-B02 cursor/sweet spot; T-B03 early/late direction; T-B04 charged swing; T-B05 box positioning; T-B06 launch direction; T-B07 bunt and foul-bunt risk; T-B08 recognize/take a ball; T-B09 star swing (scope dependent).
- **Fielding:** T-F01 manual pursuit/ground pickup; T-F02 assistance and taking control; T-F03 throw to a named bag; T-F04 fly/liner catch; T-F05 dive; T-F06 jump; T-F07 relay/cutoff and receiver handoff; T-F08 buffered throw, retarget and cancel; T-F09 recovery/bobble/loose-ball chase; T-F10 coverage and an uncovered bag; T-F11 wall carom; T-F12 buddy jump/wall rob (ability dependent); T-F13 Ball Dash; T-F14 Snap Throw; T-F15 Laser Throw; T-F16 other enabled fielding abilities, split into one lesson per distinct action/effect during the catalog audit. Profile-specific abilities remain profile-specific.
- **Outs and decisions:** T-D01 force versus tag; T-D02 two-throw ground-ball double play; T-D03 fielder's choice/which out to take; T-D04 doubled-off runner and return throw; T-D05 tag/rundown; T-D06 close-play offense and defense; T-D07 third-out/scoring consequences; T-D08 triple-play opportunity, after prerequisite lessons exist.
- **Running:** T-R01 select one runner/send/hold/return; T-R02 all-runner commands and avoiding shared bags; T-R03 dash/rounding; T-R04 slide; T-R05 fly-ball return and tag-up; T-R06 arm steal and release timing; T-R07 multi-runner steal/steal home; T-R08 defend a steal with the catcher. T-R01/T-R02 follow #688's resolved contract rather than concealing its gap.
- **Team and game literacy:** T-G01 lineup/positions/handedness; T-G02 chemistry on actual throw pairs; T-G03 stars and resources; T-G04 counts/outs/innings/fair-foul; T-G05 controller roles and a second pad; T-G06 pause/book/restart and controller recovery/calibration. These may use guided interactions in the existing screens; they need not pretend to be live-ball drills.
- **Enabled special interactions:** T-X01 items, one exercise per distinct enabled item; T-X02 counterplay and fielding a special hit; T-X03 park effects, only when those parks become product scope. Keep pending special/status design explicitly blocked; #715's ordinary-loop validation exclusion is not a tutorial waiver or permission to invent those rules.

## Coverage that stays current

The versioned catalog lives in `data/tutorials/`: `mechanics.json` is the independent inventory, `lessons.json` carries lesson/setup definitions, and `migration.json` explicitly owns initial coverage debt. `TutorialCatalog` loads/validates it in Sim; `cli tutorials` and CI run the coverage gate. Avoid a growing `PracticeLesson` switch as the content registry. The catalog must carry:

- Stable mechanic and lesson ids, title/category and lesson revision.
- Spec/control/ability references and regression scenario/test references.
- State: planned, blocked (reason + issue), implemented (automated evidence), or human-verified (build/profile + recorded sitting). Keep mechanical feasibility and human learning acceptance separate.
- Profile support (`shipped`, `c80`, or later explicit ids), prerequisites and required roster capabilities; no assumption that C80 is already the default.
- Setup and CPU policy references, teaching role, required commands, objective evaluator, failure/retry conditions and deterministic seed/variation set.
- Scheme-aware instruction/feedback keys sourced from the same control/copy owners as Exhibition, plus progress version and tracking issue.

The validator must reject duplicate/dangling ids, unknown objective/setup policies, missing profile/ability requirements, and implemented lessons lacking a runnable fixture. The coverage report joins an **independent mechanic inventory** (spec plus control/ability/content catalogs) to lessons: validating only lessons already in the tutorial file cannot detect a newly added mechanic. Uncovered shipped mechanics fail the gate unless they are explicitly in the initial migration backlog with an owning issue; new omissions cannot silently join that allowlist. An unmapped mechanic, blocked lesson, and completed lesson remain visibly different states.

Unit/scenario tests must include successful input, no input, wrong input, CPU-only success, failed setup, retry after partial success, and profile/seat/character variations. A success predicate must fail when the player does not perform the skill. Timeouts end an attempt with a reason and a retry; they never synthesize success.

## Delivery order and session boundaries

1. **Gameplay — coverage catalog.** Audit all existing mechanics into the inventory; validate references and migration gaps; add a report/CI gate. Establish the six first lesson contracts against the active profiles. No Unity screen change.
2. **Gameplay — reusable lesson runner.** Move reusable scenario setup into production Sim, add controlled CPU policies, command attribution, typed objectives and complete reset. Deliver the six headless lessons with success and failure traces. Use one shared sim and table root, no per-lesson outcome overrides.
3. **Presentation — Tutorials section.** Expand existing Practice entry into categories and direct lesson selection; implement brief/feedback/retry/next/exit/progress with the existing input and HUD owners. Update `HowToPlay.cs` and `docs/how-to-play.md` together. Exercise both schemes and teaching-seat mappings. This is a separate child and worktree.
4. **Gameplay + separate presentation children — expand coverage.** Fill running, throws/relays, remaining outs, team concepts and enabled abilities in the existing catalog. A new mechanic brings its lesson as part of its definition of done; unresolved specials wait for their rule contracts.
5. **Human learning gate.** In the Mac standalone, Jack selects an unfamiliar lesson, completes and retries it with keyboard/mouse and a pad, then performs the same action in Exhibition without external instructions. Check two-pad ownership where supported, small/large captains and both hands. Record build, data profile and remaining findings. Agents do not pass this gate.

Continue the ordinary-loop C80 sitting and Harbor Exhibition gates while building this foundation. Tutorials can expose bad rules or unreadable plays; file those under their owning epics. Artwork follows a stable, learnable game.

## Running and verifying the core (#772)

`dotnet run --project src/GrandSluggers.Cli -- tutorials` validates and reports the active profile. Implemented means headless; it does not mean a human learning gate passed. Each item/star and each distinct field ability has its own inventory reference, so new runtime content cannot disappear into a generic Special lesson.

`TutorialSession` accepts ordinary pitch, swing and field-pad commands. It recreates the seeded match on retry, keeps progress by lesson revision/profile, rejects CPU credit, and reports typed feedback. `Recording()` captures literal inputs plus the effective gameplay-input hash. `cli tutorials --replay recording.json` replays that recording and rejects a changed lesson revision/profile/input identity. Replays are diagnostic evidence, not proof a person learned the mechanic.

Regression commands (run profiles in separate processes because the diamond is process-wide):

```bash
dotnet test src/GrandSluggers.Sim.Tests --filter FullyQualifiedName~Tutorial
GRAND_SLUGGERS_TRIAL=trials/c80 dotnet test src/GrandSluggers.Sim.Tests --filter FullyQualifiedName~Tutorial
```

The authored grounder reuses S-40's 118-foot/4-degree/-18-degree opportunity. The shipped dive is a 250-foot, 12-degree, -18-degree ball; C80 uses a 120-mph, 16-degree, straight-ahead liner. These are lesson inputs, resolved through production flight and rules. They are not replacement trajectories or easier catch windows. Positive dive tests steer and commit; a dead-stick assisted dive fails.

## Standalone teaching flow (#774)

Title West / F opens the lesson list; each available lesson has a brief, attempt, specific feedback, retry, next, and return to lessons. Free practice stays available. The teaching seat follows the player's role (away for batting, home for pitching/fielding), even with a second pad connected. Saved checkmarks use profile + lesson id + revision. Menus freeze attempts; Call time and How to play remain available. Presentation delegates outcomes to `TutorialSession` and reuses the normal Harbor cameras, actors, input, and live-play view.

The first six are available for preview, not human-accepted. Standalone pad/keyboard learning and transfer to Exhibition remain the gate in #770/#774. No art is added by this slice.


## Expansion sequence after the three-success rail

All remaining mechanics already have stable catalog entries. Implement the accepted ordinary rules in these groups, with a gameplay child and then a presentation child for each. Each lesson needs a reproducible opportunity, player-owned command evidence, specific failure feedback, and three separate successes. Coverage is not complete just because an entry exists.

1. **Pitching and batting fundamentals:** location/balls, charge/MAX, break, rubber/box positioning, cursor contact, early/late direction, launch direction, bunt/foul-bunt and taking a ball (T-P02/P04–P06; T-B02–B08). Reuse plate commands and the real crossing/contact resolver.
2. **Fielding and throws:** takeover, named bags, fly/liner catch, jump, receiver handoff/cutoff, buffering/retarget/cancel, loose balls/recovery and uncovered bags (T-F02–F04/F06–F10). Reuse live glove/throw state, with separate single-action lessons before combined plays.
3. **Running and steals:** individual/all-runner orders, dash/rounding, slides, fly return/tag-up, steals and catcher defense (T-R01–R08). Control opposing throws/runners to produce repeatable opportunities; the teaching seat owns its runner or catcher.
4. **Outs and decisions:** force/tag distinctions, choosing the out, doubled-off runners, rundowns, close plays, scoring at the third out, then triple plays (T-D01/D03–D08). Prerequisites come from the earlier verbs; outcomes require the right runner/bag identities.
5. **Team and game literacy:** substitutions/stamina, lineup/handedness, chemistry, resources, counts/innings, two-controller roles and recovery/pause/book (T-P07, T-G01–G06). Repeated guided decisions in the existing screens count as attempts; a controls-page visit does not.
6. **Abilities and special interactions:** individual enabled field abilities, star moves, item use/counterplay, wall plays and pickoffs. Keep catalog profile/dependency restrictions explicit. Mechanics with unresolved design remain blocked; extra parks and deferred modes are not unlocked by tutorial work.

Human learning gates remain pending while Jack is unavailable. Continue mapping, headless implementation and agent UI checks; do not claim human acceptance or merge gated presentation to bypass the sitting.
