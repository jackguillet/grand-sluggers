# Tutorials — every mechanic has a playable lesson

Status: **103 lessons implemented in the catalog and runner; two explicit coverage gaps remain; standalone learning gates pending**, September 19, 2026. Gameplay child #772 implements the catalog and six headless lessons; the Tutorials screen is #774, with pointer correction #776. Three-success mastery is #778; its counter/Continue/save presentation is #780. Tracker: [#770](https://github.com/jackguillet/grand-sluggers/issues/770). This is gameplay foundation work serving #209 and #342, before generating more artwork. The reference is Super Sluggers' approachable party baseball; the lessons teach Grand Sluggers' own accepted rules and controls.

## Product contract

Every player-facing mechanic must have a discoverable, repeatable tutorial in a **Tutorials** section. Teach individual actions (throw a strike, throw a changeup, make a slap hit, dive) and combinations (scoop → throw → force out; feed second → receive → throw first). A controls page supports the lesson; it does not replace doing the play.

A new or changed mechanic must update its tutorial coverage in the same PR: stable mechanic id, lesson id, setup, player-owned action, success and failure evidence, rule/profile dependencies, regression scenario, and implementation state. A planned lesson is tracked debt, never completed coverage. During migration, existing gaps have named owners; new mechanics cannot quietly add gaps. Before a mechanic is presented as fully taught, its playable lesson must ship and pass the human learning check.

Tutorials support the existing Harbor Exhibition slice. They do not unlock Challenge, extra parks, new characters, another input toolkit, or an art-generation pass. Keep the existing characters, field, cameras, and input path; improve them in their owning sessions when a learning check exposes a failure.

## Original Practice baseline

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

**Lifecycle.** Select → brief → optional demonstration → ready → attempt. After either success or failure, immediately reset and begin the next attempt while progress is below 3/3, without a confirmation or briefing. At 3/3, show completion with replay / next / exit. Guided lessons use the same loop. Retry restores all relevant state: count, outs, runners, ball, fielders, selection, pending commands, recovery/status timers, stars/stamina, RNG, and per-attempt evidence. Every tutorial passes only after three distinct successful attempts. A failure does not remove an earlier success. Persist 0/3, 1/3, 2/3, and 3/3 by lesson revision/profile; retries and leaving/reopening a lesson keep earned progress. Duplicate input/results, CPU-only attempts, and demonstrations never add credit. Old single-success completion is retired through a lesson revision bump. No prior play evidence can satisfy the next attempt. Support immediate retry, pause, skipping, and direct lesson selection; no long compulsory sequence. Save learning progress separately from match state, with lesson-version identity so changed objectives can be marked for refresh without erasing unrelated progress.

**Transfer.** First offer a stable setup; then a small authored set of variations (side, pitch location, runner speed, character/handedness) and free practice. Expose prerequisites as recommendations, not mandatory locks. A player should recognize the same action in Exhibition afterward.

## First six lessons

These acceptance contracts are exercised by `TutorialSessionTests` on the shipped root. They are headless lessons; the legacy Practice screen does not yet expose them. “Slap hit” names the game's existing uncharged swing (the brief's “slap shot”). Existing S-ids are reusable regression anchors; add lesson-specific tests for setup, ownership, failure and retry.

1. **T-P01 — Throw a strike.** Human pitches; CPU takes. Reset to a fresh count, a consistent batter and neutral pitcher stamina. Pass on the player's delivery producing a called strike through the normal plate crossing. An out-of-zone delivery fails even if another CPU policy would chase it. Anchors: spec §4.4, S-01/S-02.
2. **T-P03 — Throw a changeup by the cycle.** Human pitches; CPU takes. The binding it shows is the pre-charge cycle (RB / Tab): every SET starts on Fastball, so **one press** selects the changeup and the charge locks it (#825, PH-02-R3/R4/R5). The setup names **hex** (fastball, changeup, curveball) as the pitcher: the home captain vale throws curveball / slider and owns no changeup, so the lesson was unwinnable by the cycle until #876 named its pitcher. Pass only when a player-commanded changeup crosses the intended in-zone target through the real flight. A fastball strike cannot pass. Teach the timing contrast against an ordinary pitch without inventing a second speed curve. Anchors: §4.1–4.4 and the existing changeup flight tests.
3. **T-B01 — Make a slap hit.** CPU repeats an ordinary hittable strike; human bats. Pass on a player-commanded uncharged, non-bunt swing producing fair contact. A charged hit, a foul, a miss, or CPU contact cannot pass. Feedback separates cursor placement and timing. Anchors: §5.1–5.3, S-07/S-08/S-09.
4. **T-F01 — Field a ground ball.** Start a verified routine grounder with a human teaching glove. Teach taking control, moving to the live ball and the current scoop action. Pass on the player's required movement/scoop evidence and secure ground possession; a subsequent throw is a separate step. A fly catch cannot pass. Anchors: §8.2–8.3, S-31/S-33, S-94–S-99.
5. **T-F05 — Dive for an out.** Present a reproducible catchable airborne ball near the dive reach, with sufficient reaction opportunity under the active profile. Pass on the human dive command and an airborne catch out by that diver. A ground-ball pickup, automatic assisted dive, standing catch, or missed dive cannot pass. Teach recovery as part of the result. Anchors: §8.3–8.4 and the catch/dive/recovery suites.
6. **T-D02 — Turn a double play.** Runner on first, no outs, an authored makeable SS grounder, CPU runner and covering fielders. Human scoops, chooses/throws to second, then chooses/throws to first after the receiver handoff. Pass on both human-issued throws, the correct runner identities and two geometric force outs in order. One out, an unrelated two-out play, or an automatically supplied second throw cannot pass. Anchors: §10.4, S-40–S-50. Verify feasibility for each supported profile; do not retune an unmakeable race to rescue the lesson.

## Coverage backlog

Implemented lessons now cover plate decisions, throws and relays, running and steals, guided menus, named star skills, and chemistry throws. The item lessons are blocked: items are dormant since #891 removed the on-deck item offer (PH-16-R15). The exact remaining ids stay planned or blocked; `cli tutorials` is the current coverage report. Existing broad Practice behavior is partial reuse, not tutorial completion. This is the initial inventory; the catalog implementation must reconcile it against every player-facing spec section, control verb, ability and enabled feature. No unsupported future mechanic is enabled by appearing here.

- **Pitching:** T-P01 strikes; T-P02 aim/location and balls versus strikes; T-P03 changeup by the cycle; T-P04 charge/MAX; T-P05 break after release; T-P06 rubber positioning; T-P07 stamina/pitcher substitution; T-P08 pickoff; T-P09 star pitch; T-P10 the third pitch by the cycle (#876): since #860 every pitcher throws Fastball plus two authored families, so **two presses** select the third slot. Human pitches (vale: fastball, curveball, slider); CPU takes. The objective `third-slot-strike` is the **slot**, not a family: the pass needs a player-commanded, non-star pitch of the play's own pitcher's third family (`Repertoire[2]`) and a called strike, so it holds for any repertoire. A fastball or second-pitch strike fails with `use-third-pitch`. `TutorialSessionTests` drives the real cycle (`Match.SelectPitch`) on both profiles.
- **Batting:** T-B01 slap contact; T-B02 cursor/sweet spot; T-B03 early/late direction; T-B04 charged swing; T-B05 box positioning; T-B06 launch direction (retired by #883: an ordinary swing ignores the stick, PH-12); T-B07 bunt and foul-bunt risk; T-B08 recognize/take a ball; T-B09 star swing; T-B10 cancel a swing (PH-13-R1: load, East / G, take the high ball); T-B11 bunt toward first (PH-14-R5: hold RT / L with a runner on first, the bunt fair toward first).
- **Fielding:** T-F01 manual pursuit/ground pickup; T-F02 assistance and taking control; T-F03 throw to a named bag; T-F04 fly/liner catch; T-F05 dive; T-F06 jump; T-F07 relay/cutoff and receiver handoff; T-F08 buffered throw, retarget and cancel; T-F09 recovery/bobble/loose-ball chase; T-F10 coverage and an uncovered bag; T-F11 wall carom; T-F12 buddy jump/wall rob (ability dependent); T-F13 Ball Dash; T-F14 Snap Throw; T-F15 Laser Throw; T-F16 other enabled fielding abilities, split into one lesson per distinct action/effect during the catalog audit. Profile-specific abilities remain profile-specific.
- **Outs and decisions:** T-D01 force versus tag; T-D02 two-throw ground-ball double play; T-D03 fielder's choice/which out to take; T-D04 doubled-off runner and return throw; T-D05 tag/rundown; T-D06 close-play offense and defense; T-D07 third-out/scoring consequences; T-D08 triple-play opportunity, after prerequisite lessons exist.
- **Running:** T-R01 select one runner/send/hold/return; T-R02 all-runner commands and avoiding shared bags; T-R03 dash/rounding; T-R04 slide; T-R05 fly-ball return and tag-up; T-R06 arm steal and release timing; T-R07 multi-runner steal/steal home; T-R08 defend a steal with the catcher. T-R01/T-R02 follow #688's resolved contract rather than concealing its gap.
- **Star lessons and prices:** every Star Pitch and Star Swing lesson (T-P09, T-B09, T-SP-*, T-SS-*, T-X02) and T-G03 must fund the named special at its price on the lesson's own team: the ability's tier price from `stars.json` `tiers`, plus `costs.guestCaptainSurcharge` when the character is a captain playing for another captain's team (§12). `cli tutorials` checks it on the active profile. At the prices (low 1, mid 2, top 3, surcharge 1) a guest captain's top special costs 4, more than the reserve of 3, so the six guest-captain top-tier lessons start at 5 Stars. A special released without enough Stars is the ordinary action and spends nothing (PH-16-R12); **T-G03-U When the stars run out** teaches it: Vale (charmball) on a pool of 0, the player holds LB / Q as the pitch is let go, and success is the play's own typed record — one Star Pitch `StarRequest` not afforded, the ordinary pitch thrown, the pool unchanged; a release without the modifier fails (`star-not-asked`), and the CPU and a demonstration earn nothing (S-203). A lesson's match starts both pools on the one `startingReserve`, as every match does; a setup's `startingStars` / `opponentStars` can only raise its side's pool, and only `poolStars` sets the teaching side's pool exactly (the validator lets T-G03-U alone use it below the price). **The verb** (PH-16-R10, R11, R17): every star lesson asks the player to hold LB / Q as South / Space is let go — no North, no arming — and each moved its revision with the verb (T-P09, T-B09, T-SP-*, T-SS-*, T-G03 are revision 3); the evidence is still the player's own release (`TutorialSession.Pitch` / `Swing` with `Human`), which the client builds from the modifier read at the release. T-G03 still earns before it spends: the ordinary strikeout adds its bonus and the plate-appearance base to a pool that started on the reserve.
- **Team and game literacy:** T-G01 lineup/positions/handedness; T-G02 chemistry on actual throw pairs; T-G03 stars and resources; T-G04 counts/outs/innings/fair-foul; T-G05 controller roles and a second pad; T-G06 pause/book/restart and controller recovery/calibration. These may use guided interactions in the existing screens; they need not pretend to be live-ball drills.
- **Enabled special interactions:** T-X01 items, one exercise per distinct enabled item; T-X02 counterplay and fielding a special hit; T-X03 park effects, only when those parks become product scope. Keep pending special/status design explicitly blocked; #715's ordinary-loop validation exclusion is not a tutorial waiver or permission to invent those rules.

## Coverage that stays current

The versioned catalog lives in `data/tutorials/`: `mechanics.json` is the independent inventory, `lessons.json` carries lesson/setup definitions, and `migration.json` explicitly owns initial coverage debt. `TutorialCatalog` loads/validates it in Sim; `cli tutorials` and CI run the coverage gate. Avoid a growing `PracticeLesson` switch as the content registry. The catalog must carry:

- Stable mechanic and lesson ids, title/category and lesson revision.
- Spec/control/ability references and regression scenario/test references.
- State: planned, blocked (reason + issue), implemented (automated evidence), or human-verified (build/profile + recorded sitting). Keep mechanical feasibility and human learning acceptance separate.
- Profile support (`shipped`, or a later trial's explicit id), prerequisites and required roster capabilities. Since 3e (2026-09-22) the C80 profile is the shipped game; lessons that were C80-only are shipped lessons, and the full-size-only fumble lesson T-F09-S is retired with the rule it taught.
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

Continue the ordinary-loop sitting on the promoted C80 game and the Harbor Exhibition gates while building this foundation. Tutorials can expose bad rules or unreadable plays; file those under their owning epics. Artwork follows a stable, learnable game.

## Running and verifying the core (#772)

`dotnet run --project src/GrandSluggers.Cli -- tutorials` validates and reports the active profile. Implemented means headless; it does not mean a human learning gate passed. Each item/star and each distinct field ability has its own inventory reference, so new runtime content cannot disappear into a generic Special lesson.

`TutorialSession` accepts ordinary pitch, swing and field-pad commands. It recreates the seeded match on retry, keeps progress by lesson revision/profile, rejects CPU credit, and reports typed feedback. `Recording()` captures literal inputs plus the effective gameplay-input hash. `cli tutorials --replay recording.json` replays that recording and rejects a changed lesson revision/profile/input identity. Replays are diagnostic evidence, not proof a person learned the mechanic.

Regression command (a trial profile, when one exists, runs in its own process because the diamond is process-wide):

```bash
dotnet test src/GrandSluggers.Sim.Tests --filter FullyQualifiedName~Tutorial
```

The authored grounder reuses S-40's 118-foot/4-degree/-18-degree opportunity. The dive is a 120-mph, 16-degree, straight-ahead liner. These are lesson inputs, resolved through production flight and rules. They are not replacement trajectories or easier catch windows. Positive dive tests steer and commit; a dead-stick assisted dive fails.

## Standalone teaching flow (#774)

Guided team/menu lessons T-G01, T-G05, T-G06, T-G06-R, and T-G06-C use the ordinary Exhibition stadium, Select, lineup, and Call time screens. The runner credits accepted roster, order, glove, seat-binding, pause/book/restart, recovery, and calibration transitions in that order within each attempt; three separate successes are required. T-G05 needs two distinct physical gamepads. T-G06-R needs an active pad to be lost and the same logical seat recovered. T-G06, T-G06-R, and T-G06-C start in a prepared Harbor SET so the relevant menu or active controller is available immediately. T-G06-C is c80-only because shipped pursuit does not offer Reset stick. These are automated and narrow-compile verified; standalone physical-controller learning is still a human gate. T-G01 uses explicit pick-and-swap in the batting bar and field diamond: pointer clicks or South/Space choose the source and target. Only completed swaps credit order/glove changes; inspection, navigation, a first pick and cancellation do not. The ordinary setup, CPU policy, three-success progression and supported profiles are unchanged. Both keyboard/mouse and controller use the same accepted transitions; LineupScreensTests checks seat isolation, independent cards and readiness, cancellation and preserved roster/glove permutations. Both human players must ready their teams before Exhibition starts; CPU readiness is automatic. T-G05 still requires two physical pads; model checks do not pass that human gate. Human transfer to Exhibition remains pending.

Title West / F opens the lesson list; each available lesson has a brief, attempt, specific feedback, retry, next, and return to lessons. Free practice stays available. The teaching seat follows the player's role (away for batting, home for pitching/fielding), even with a second pad connected. Saved checkmarks use profile + lesson id + revision. Menus freeze attempts; Call time and How to play remain available. Presentation delegates outcomes to `TutorialSession` and reuses the normal Harbor cameras, actors, input, and live-play view.

The first six are available for preview, not human-accepted. Standalone pad/keyboard learning and transfer to Exhibition remain the gate in #770/#774. No art is added by this slice.


## Expansion sequence after the three-success rail

All remaining mechanics already have stable catalog entries. Implement the accepted ordinary rules in these groups, with a gameplay child and then a presentation child for each. Each lesson needs a reproducible opportunity, player-owned command evidence, specific failure feedback, and three separate successes. Coverage is not complete just because an entry exists.

1. **Pitching and batting fundamentals:** location/balls, charge/MAX, break, rubber/box positioning, cursor contact, early/late direction, bunt/foul-bunt and taking a ball (T-P02/P04–P06; T-B02–B08). Reuse plate commands and the real crossing/contact resolver.
2. **Fielding and throws:** takeover, named bags, fly/liner catch, jump, receiver handoff/cutoff, buffering/retarget/cancel, loose balls/recovery and uncovered bags (T-F02–F04/F06–F10). Reuse live glove/throw state, with separate single-action lessons before combined plays.
3. **Running and steals:** individual/all-runner orders, dash/rounding, slides, fly return/tag-up, steals and catcher defense (T-R01–R08). Control opposing throws/runners to produce repeatable opportunities; the teaching seat owns its runner or catcher.
4. **Outs and decisions:** force/tag distinctions, choosing the out, doubled-off runners, rundowns, close plays, scoring at the third out, then triple plays (T-D01/D03–D08). Prerequisites come from the earlier verbs; outcomes require the right runner/bag identities.
5. **Team and game literacy:** substitutions/stamina, lineup/handedness, chemistry, resources, counts/innings, two-controller roles and recovery/pause/book (T-P07, T-G01–G06). Repeated guided decisions in the existing screens count as attempts; a controls-page visit does not.
6. **Abilities and special interactions:** individual enabled field abilities, star moves, item use/counterplay, wall plays and pickoffs. Keep catalog profile/dependency restrictions explicit. Mechanics with unresolved design remain blocked; extra parks and deferred modes are not unlocked by tutorial work.

Human learning gates remain pending while Jack is unavailable. Continue mapping, headless implementation and agent UI checks; do not claim human acceptance or merge gated presentation to bypass the sitting.


## Plate fundamentals batch (#782)

Seven existing catalog entries now have production-runner exercises. All inherit the three-success rule, revision/profile progress and CPU/demo exclusion.

- **T-P04 MAX pitch:** ordinary, non-changeup/non-star pitch released at effective charge 1.0, with a real called strike. A partial/decayed charge or ball fails.
- **T-P05 break:** normal, uncharged pitch with at least 0.5 normalized accumulated break, followed by a called strike. Charging or holding changeup cannot substitute for normal break.
- **T-P06 rubber movement:** move at least 0.15 normalized rubber units from center and throw a called strike. A central strike alone does not teach repositioning. Thresholds live in the authored setup, not in Unity.
- **T-B02 sweet spot:** ordinary slap with typed Perfect contact and fair territory. A fair sour/nice contact does not complete this goal.
- **T-B04 MAX swing:** full effective charge 1.0 and fair contact; MAX with a miss/foul fails.
- **T-B07 bunt:** start with two strikes, hold either bunt side (LT / J or RT / L) through the pitch, and bunt fair. The held bunt has no timed press (§5.8): the command is `SwingCommand.HeldBunt` with the held side. Setup establishes the count through two ordinary taken strikes. Retry restores that count; foul-bunt/strikeout risk stays governed by the existing rules. Revision 2: the verb moved from West / V to the triggers, so earlier progress does not carry.
- **T-B08 take a ball:** the authored ordinary fastball has normalized aimY 2 (2 × PlateScaleY above the zone center), validated outside the zone and clear of either batter's body. A human-ready attempt takes the real delivered pitch; swinging or squaring fails. Merely advancing the lesson clock without resolving a pitch times out and earns nothing.
- **T-B10 cancel a swing** (PH-13-R1; objective `cancel-take`, setup `T-B10`, the T-B08 high ball): the client hands every plate tick of the teaching seat to `TutorialSession.Plate`, which steps the same `PlateButtons.Advance` Exhibition uses. The pass needs the session's own evidence that East / G discarded an **armed** load (a bunt trigger's conversion is not the explicit cancel), a take at the plate, and a called ball. A take with nothing loaded (`load-then-cancel`), a swing or a bunt (`cancel-swung`) or a chased strike fails. The released cancelled hold commits nothing (the sim's must-release). Plate ticks are recorded and replayed with the attempt.
- **T-B11 bunt toward first** (PH-14-R2 … R5; objective `bunt-first-fair`, setup `T-B11`: the T-B07 middle fastball with no strikes and a runner on first): the held bunt with `BuntSide.First` and fair contact whose spray is toward first (positive). The third-base side (`use-first-side`), a swing or a withdrawn bat (`use-bunt`), a held bat off the ball (`bunt-miss`), a foul (`foul-bunt`) or a fair bunt toward third (`bunt-wrong-side`) fails. The side leans the ball and never places it, so the last failure is real.

`TutorialPlateObjectives` evaluates normal commands and typed results. Setup policy validates CPU pitch geometry, count and movement thresholds. New headless tests execute all nine lessons (with T-B10 and T-B11) three times and replay their inputs. This is implementation evidence, not a human learning gate.

## Plate lesson presentation (#784)

The standalone browser now groups the 13 implemented lessons by category, with left/right or A/D to switch categories and up/down or W/S to select lessons. Pointer tabs use the same authored hit rectangles as rendering. A category displays at most six lessons per page; selection crosses page boundaries and pointer arrows select adjacent pages. Free practice has its own Free play tab. Next follows the whole implemented catalog across category boundaries; returning to lessons keeps the current lesson selected.

The seven plate briefs describe the controlled setup, the required action, and both input schemes. Feedback distinguishes undercharging, failing to bend or reposition, missing the sweet spot, bunting foul with two strikes, and chasing the high ball. Three-success progress, saved partial attempts and profile isolation are shared with the earlier lessons. Human learning and controller checks remain pending.

## Ordinary plate decisions (#786)

Six more exercises bring the implemented catalog to 19 lessons. Timing directions and launch directions (the launch pair retired by #883) have separate stable lesson ids so a player earns three successes in each action, rather than passing a combined lesson by repeating only one side.

- **T-P02 called ball:** a real TakeBall earns the attempt. A called strike or hit-by-pitch does not. The controlled batter takes; rubber movement creates the opportunity using ordinary pitch geometry.
- **T-B03 pull / T-B03-L push:** ordinary fair slap contact with at least one frame of early/late input respectively, and a real spray on that handedness-relative side. Neutral timing or opposite-side contact does not count. No absolute left/right assumption.
- **T-B05 box positioning:** CPU aimX 0.4 produces a crossing 0.74 ft off center. Move at least 0.2 normalized box units and produce Perfect fair slap contact; moving without meeting the sweet spot is insufficient. The minimum movement and authored CPU pitch are validated together. Default box center cannot pass.
- **T-B06 grounder / T-B06-F fly — retired (#883).** They asked for the stick held up or down at contact and a dirt or fly ball. Since #883 an ordinary swing ignores the stick (PH-12), so the held stick made no different ball; the two lessons, their setups, the `grounder-fair` / `fly-fair` objectives and mechanic `batting.06` were removed. Timing, contact and pitch height still decide the launch.

These use existing rules, not altered timing windows or manufactured outcomes. The six setups reset identically on retry; replay and CPU/demo exclusion remain mandatory. The human learning gate is still pending.

## Ordinary plate decision presentation (#788)

The six exercises from #786 have standalone titles, goals, authored setup explanations, controls for both schemes, and corrective feedback. Early and late timing remain distinct lessons, as do grounder and fly launch. The book explicitly says Down/S recenters during SET but controls loft during the pitch. The browser now contains 19 implemented lessons; batting exercises span two six-row pages. Three-success progress and CPU scenarios use the shared runner unchanged.

Agent checks verify rendering and navigation separately from the pending physical-pad and human learning/transfer gates. No artwork or baseball rules change in this presentation child.

## Fielding fundamentals (#790)

Seven headless exercises use the existing Harbor live-play rules and the same three-success `TutorialProgress` ledger. Each retry rebuilds the seeded match, clears command receipts, and replays literal pad input. One rule profile, the shipped one. `implemented` here is automated evidence; pad and keyboard coaching, physical standalone sitting, and Exhibition transfer remain human gates.

- **T-F02 takeover:** the 118-ft, 4°, −18° routine grounder starts on an assisted glove. The player steers that glove and secures the ball on the dirt. Immediate manual takeover is valid. The receipt needs actual manual glove movement and possession by that glove; if assisted pursuit moves the glove after the player's last steering step, the pickup is `assisted-pickup`. A manually positioned glove can wait still for the ball. T-F01 uses the same corrected ownership check and advances to revision 3 so earlier mastery is refreshed.
- **T-F03 / T-F03-2 / T-F03-3 / T-F03-H named throws:** the same grounder supplies secure possession. The player arms first/second/third/home with D-pad or number key, then presses South. `ThrowPop` from a human release (or a human-buffered release) records the bag. Success waits for a live receiver holding the ball at the named bag; the first-base batter force may complete on that same reception frame, so its real `ThrowOutAtFirst` is the receipt. Choosing another bag is `wrong-bag`; a release that never arrives is `throw-not-received`; arming without South earns nothing. Second, third and home teach throwing to a named target without promising an out when no runner is there.
- **T-F04 ordinary aerial catch:** an 88-mph, 32° center fly. The controlled glove needs a human South press on the actual airborne take and a completed geometric catch out with no dive or jump feat. A CPU stand-up catch or South after the bounce fails as `no-aerial-out`.
- **T-F06 jump catch:** a 245-ft, 34° center fly with Basil in center. The player presses West on the selected glove; a neutral pursuit stick on the press is valid. The jump is its physical arc. The human press must actually arm/take off, and the completed play must record that fielder's `Jump` catch out. An early leap that misses, an assisted catch without West, or South alone fails as `no-jumping-out`.

The test runner drives ordinary `LivePadInput` and checks three earned attempts, wrong/dead/CPU input, receiver timing across later nonhuman ticks, demonstrations, retries, and exact deterministic replay on both profiles. These fixtures author opportunities, not catches, throws, outs, or changed rule windows.

## Fielding teaching flow (#791)

The standalone browser presents the seven field fundamentals from #790 with separate brief, goal, setup, controls and feedback for each. The four named bags remain separate lessons, so each requires three successful throws. Fielding now spans two pages. Copy distinguishes a throw received at a bag from an out, a catch in the air from a scoop after a bounce, and a jump catch from a standing catch or dive.

The existing live-field input, camera and feedback path serves every exercise. How to play includes the same bag, catch and jump controls for both schemes. Agent UI verification is separate from the pending human learning/transfer and physical-controller gates.

## September 19 expansion verification

The prepared scenarios use the ordinary rules and typed outcomes. Three-success persistence is shared by every runnable lesson; guided screen lessons use ordered receipts from the real screens. Profile-only lessons remain visible only in their supported profile. The catalog keeps specific unfinished features (such as Spin Check) separate when a narrower lesson becomes playable.

Standalone preview `8dd6520468` was rendered and exercised on keyboard: Call time began directly in prepared Harbor SET, its real menu → book → restart sequence reached 2/3 and then 3/3 through Continue; Time a steal armed the first runner and reached second for 1/3; Earn and spend stars progressed from an ordinary strikeout to the next batter's star pitch for 1/3. The pitcher picker showed the candidate and stamina beneath coaching, and confirmation earned progress. The preview returned to title with automatic input selection. These are agent smoke checks, not Jack's human learning gate or physical two-pad/recovery/calibration acceptance.

At that preview checkpoint the full regression suite passed 1,678 tests, catalog/protocol/match CLI checks passed, and the Unity narrow compiler plus GUI player build succeeded. Subsequent fielding/ability integration passed 361 focused tests under both shipped and C80 profiles. Later commits require their own final validation; these results are revision-scoped evidence, not a blanket pass for future changes.

## Remaining-tutorial expansion (#794–#800)

The catalog now separates the real subskills: named-bag throws; relay, queue, retarget and cancel; wall and ordinary bobble recovery; uncovered-bag reception; force home, double-off, rundown, both sides of a close play and a caught-fly triple play; individual/all-runner orders, dash, slide, rounding, early fly return, tag-up, steals and catcher defense; counts, foul/fair and half changes; guided lineup, seat and menu work; every named star pitch/swing, item, and supported fielding reach ability. Each runnable entry uses the shared three-success progression, repeatable CPU setup, typed outcomes, and human-command evidence.

Tutorials do not change the accepted rules to manufacture a win. The triple play requires a real catch and both return throws; rounding requires the runner's actual turn and dash; Bobble recovery requires a real ordinary handling error followed by a human scoop. Assisted pursuit after an isolated steering frame cannot earn manual recovery credit. A multi-pitch count or foul/fair sequence is one success, and failures preserve earlier successes.

Outstanding coverage is explicit in the catalog. Spin Check is not connected to live runner decisions, so its extra-base commitment and geometry contract must be resolved before a lesson can teach it (#799). Park hazards remain blocked: Harbor has none and extra parks are outside current product scope (#37). The item lessons (T-X01, T-I-banana, T-I-rocket, T-I-pow) are blocked on #891: no at-bat offers an item any more (PH-16-R15), so none can be earned; their setups and the `item-effect` objective stay for a future item source. T-D07 teaches a third force out at home with no run. T-D07-T now stages a real runner crossing home before a third force at second cancels it; T-D07-C contrasts it with a real earlier crossing that counts before a later nonforce third tag.

All new work remains in dedicated local branches. Jack's learning, physical controller and transfer-to-Exhibition gates remain pending. No artwork was added.

Preview `27c7dea9d2` (101 lessons before the scoring extension) passed the full 1,744-test suite; 439 focused tutorial/book/control checks also passed under both shipped and C80. Catalog, protocol, Harbor match, art-slot validation and the Unity narrow compiler passed. The standalone rendered the revised counts, recovery, fielding, running and item pages without clipping, including keyboard/controller copy. The half-inning lesson was exercised through 1/3 and 2/3, then its saved 3/3 completion was verified after restarting into this preview. Main remained `c7cd3e01`. These checks do not pass the human learning or physical two-pad gates.

Final expansion verification: **103 implemented lessons across the supported profiles (97 shipped, 102 C80)**. The 103-lesson integration passed all **1,748** full-suite tests. After tightening the scoring tag to require third base, the final source passed **443** focused tutorial/book/control checks in each profile. Catalog validation passes in both profiles; protocol, Harbor match, asset-slot validation, derived report and Unity compilation passed. The final standalone preview **`28d5be9733`** built and rendered at title with automatic input selection. Its scoring briefs and the new scoring book page were inspected in the preceding preview with identical presentation; both keyboard and controller text fit. No human success is claimed for the scoring drills.

Only **T-A-spin-check (#799)** and **T-X03 (#37)** remain unrunnable for the specific feature/scope reasons above. The final notes commit corrects stale recovery catalog descriptions and records this evidence; it does not alter the running preview's gameplay. No merge or source upload was performed after automatic approval review rejected publication; local branches and their commits remain available for review.


## Immediate attempt loop — #802 / #804 (September 20, 2026)

Presentation revision `d88999de7a` saves earned progress and immediately rebuilds/begins the same lesson after success or failure below 3/3. Gameplay and guided lessons share the repeat decision. Only completion at 3/3 opens the result actions; the first briefing and deliberate replay remain available. The reset frame consumes no gameplay input, charge state starts unarmed, and a failed-attempt correction remains beside the controls in the next attempt.

Verification on that revision:
- All 1,756 full-suite tests passed on shipped data. 430 tutorial/book/input regression checks passed on shipped data and 430 passed with the C80 overlay. The new mixed success/failure sequence verifies retained progress, fresh matches/evidence, and the third-success stop; guided checks require the full ordered action sequence again on each attempt.
- Unity C# compilation, tutorial catalog, debug protocol, art catalog, derived report checks, and `cli match --seed 7` passed.
- The Mac standalone preview built and rendered. Keyboard/mouse `T-P03` returned directly to live SET after a wrong pitch at saved 1/3, retaining the correction and count. `T-P06` also reset after a missed objective and a timeout. `T-SP-heatball` advanced from 0/3 through 1/3 and 2/3 without a Continue press or briefing, then stopped on LESSON COMPLETE at 3/3 after the third Q + Space pitch. Call time still allowed leaving an unfinished lesson. These UI checks are agent evidence, not a human learning gate.
- Separate sitting-found child #806 records the pre-existing title navigation footer disappearing after a pitch; no unrelated repair is included here.
- Preview left at title; primary checkout and human/controller gates remain unchanged. Physical pads and live guided repetition were not exercised in this sitting.

## Field lessons — F8-c (#814)

A setup may name a `park` and `night`. The lesson plays at that park with its hazards on, from the setup's seed, so the opportunity repeats. One lesson per hazard pattern and per ground that changes the ball:

| Lesson | Park | Opportunity | Passes | Fails |
| --- | --- | --- | --- | --- |
| T-H01 | Crystal Rink | grounder onto the ice | manual ground possession | assistance's pickup, dead pad |
| T-H02 | Crystal Rink | fly behind the freezer at (7, 131) from 2B | the player's catch, no slow on the catcher | the straight run through the freezer (`slowed`), no catch |
| T-H03 | Funfair Park | grounder into the warp can at (18, 49) | the player's glove takes it after it leaves the other can | the assistance's take, a take before the redirect |
| T-H04 | Canopy Yard | liner off the tree at (35, 217) | the player's glove takes it after the carom | the assistance's take, a take before the carom |
| T-H05 | Funfair Park | the train (timed mover) | planned | |
| T-H06 | Rooftop City | a billboard star (batting) | planned | |
| T-H07 | Canopy Yard | the Clamber wall | planned | |

Regression: `HazardLessonTests` earns each implemented lesson with a scripted human pad and fails it with a dead pad; T-H02 also fails the straight run. The mover, reward and wall-trait mechanics are migration debt on #814. The standalone learning gate is Jack's.

Setup-flow coverage: T-G01 and T-G05 begin at stadium and then use captain selection; their accepted roster/order/glove and distinct-seat objectives, CPU policy, retries and profile support are unchanged. Match settings is covered by ExhibitionSettingsTests and LineupScreensTests (ownership, unavailable Items, ready reset and back preservation); a guided settings objective remains planned debt under #962. Standalone two-controller learning remains a human gate.

T-P07 uses the in-game pitcher window: inspect the candidate’s four stats, repertoire, remaining ARM and teammate chemistry before confirming. Setup/CPU policy remains `pitcher-swap` with a tired starter; only the player-confirmed fresh-arm substitution can satisfy `tired-pitcher-swap`. Browsing or cancelling earns nothing. The shipped profile uses the existing objective. `PitcherSwapTests`, `TutorialSetTests`, `TutorialSessionTests` and the two pitching-seat cases in `AtBatInputGate` cover inspection, confirmation and input isolation; physical two-pad readability and learning transfer remain human gates.

### Steal departure and pitcher commitment coverage

T-R06 uses player-timed departure and the continuous pre-contact runner clock; T-R07 keeps the delayed double-steal opportunity, and T-R08 requires the human catcher’s throw command and a real tag after transfer and flight. T-P08 also covers pitching.11: a legal throw before charge can earn success at receiver possession, while beginning charge and then attempting the pickoff produces the real typed balk and fails the lesson. Charge input is recorded and replayed. PitchSetupTests exercises return during windup/flight, all-base targets, indefinite charge, balk awards, pause and catcher input buffering. Standalone controls and coaching use the adapters described below. Human learning acceptance remains pending.

The presentation adapters route pitch-charge presses into T-P08’s commitment input and show `balk-after-charge` as a retryable failure. T-R06/07 show actual departure/return positions and the race inset; T-R08 uses the same release-aligned throw view as Exhibition. Coaching explains immediate departure. From catcher possession, the ordinary live-play camera follows the ball through transfer, throw and reception. Human learning/transfer acceptance remains pending.
