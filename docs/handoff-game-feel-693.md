# Game feel research handoff — #693 / #708

Prepared September 15, 2026 because Jack is nearly out of session usage. **Continue the existing work; do not restart the decision interview.** Session kind: **Gameplay research/documentation**. Last design commit at preparation: `59baf8c` (`docs: simplify fielding to arcade range and visual glove motion`); see the September 15 update in [Next work](#next-work--parity-first-explicit-prototype-dependencies) for the current state.

## Start here

- Existing dedicated worktree: `/Users/jack/repos/grand-sluggers/scratchpad/wt-708-compact-proposal`.
- Branch: `codex/708-compact-proposal`. Reuse this worktree for this same child issue after checking its branch/status; never edit the primary checkout. Do not reset or overwrite work that appeared after this handoff.
- Parent [#693](https://github.com/jackguillet/grand-sluggers/issues/693); research child [#708](https://github.com/jackguillet/grand-sluggers/issues/708); draft [PR #709](https://github.com/jackguillet/grand-sluggers/pull/709). PR was verified OPEN, draft, base `main`, head `codex/708-compact-proposal` during handoff preparation. Recheck live status before acting.
- Work is stacked after the #702 measurement work at `59f3762` / draft [#707](https://github.com/jackguillet/grand-sluggers/pull/707). Do not flatten, merge, close or pass human gates as part of resuming research.
- **No compact profile has been simulated or played. No runtime tuning or art changes are authorized by this packet.** Accepted numbers are authored calibration trials, not Nintendo measurements or approved shipping defaults.

Read this file, current `AGENTS.md`, [agent rails](agent-rails.md), and the current direction at the top of [the #708 report](research-game-feel-708.md#current-direction--simple-arcade-fielding) first. Then read the relevant portions of [the decision plan](plan-game-feel-693.md) and [gameplay spec](gameplay-spec.md). Load `data/agent/debug-protocol.json` for the session; it contains comments, so a bare Python `json.loads` is not its loader.

## Jack's objective and review preferences

Establish lasting, researched and tracked game-feel rules for a compact cartoon toy baseball game. Compare **both Mario Superstar Baseball (GameCube) and Mario Super Sluggers (Wii)** before choosing between differing reference behaviors. Current provisional policy in the report is Wii-led visual readability with GameCube mechanics cross-checks; matched numerical fidelity remains unproven.

Preserve doubles, triples and relays without a huge outfield. Field dimensions, pursuit speed, ball travel and reach must be calibrated together. Throws should release promptly but visibly register and travel readably, on the slower side of the proposed readable range. Routine fielding should be reliable. Chemistry and special abilities matter.

Jack originally asked to approve required decisions **one at a time, with context**. The session then became far too microscopic. His latest correction is the most important instruction for the successor:

> honestly i think this is too detailed for our game. at the end of the day it is still a cartoon toy baseball game. we want the fielding mechanics to look good (glove pocket facing the ball visually) but i don't think it needs to be calculated

This has already been recorded, committed, pushed and reflected in the trackers at `59baf8c`. **Do not ask him to approve that correction again.** Bring meaningful player-facing tradeoffs, not each hidden coefficient, glove surface, or implementation detail. Consolidate routine research and tuning autonomously within the approved scope.

## Current overriding fielding contract

Decision **`F693-02-arcade-fielding-simplification`**, candidate key `arcadeFieldingSimplification`:

- Fielding opportunities use explicit character fielding range, actual ball position/trajectory, action readiness and baseball legality. The player owns positioning and jump/action timing.
- The glove should visually meet the ball with its pocket facing it. A bad visual wrist angle is a presentation defect, not another gameplay miss or skill check. Do not teleport the ball to disguise a mismatch.
- No required glove mesh collision, pocket/rim/back eligibility, contact-surface normals or detailed glove incidence calculation. Preserve the existing shared rig, authored takes and simulation clock.
- Keep routine reliability, difficulty/defensive-quality error chance, nearby bobbles versus continuing deflections, brief stun, reliable same-error recovery and special-hit exceptions.
- Simple reproducible gameplay contact context will select response/direction. **The exact simplified mapping remains open.** Do not silently introduce a severity lottery, automatic base award or tactical outcome.

Four historical decisions are now `superseded-by-user-direction`: `F693-02-continuing-error-retention-curve`, `F693-02-error-contact-obstruction-basis`, `F693-02-glove-contact-surface`, and `F693-02-glove-catch-sides`. Their old text and evidence remain for audit. The old collider-driven `r = .80 - .30*c` and surface-normal incidence formula are not implementation requirements. The **50–80% continuing retention bounds remain trial anchors**. The old glove-side question was retired, not accepted.

Some historical section prose and queue question wording describe an earlier proposal. Always read the actual accepted/corrected payload and current amendment; do not treat a question string as the final decision. In particular, continuing deflection angular variation is **±15°, not ±30°**.

## Where the complete decisions live

- [#708 report](research-game-feel-708.md): rationale, accepted scope, corrections, research limitations and historical decisions.
- [Candidate JSON](research/game-feel-708-candidates.json): exact decision IDs, acceptance evidence, trial values, amendments and `decisionQueue`. This is the detailed handoff; do not reconstruct values from chat fragments.
- [Derived JSON](research/game-feel-708-derived.json): generated arithmetic, not simulation evidence.
- [Reproduction/checker](../tools/compact-field-report.py): regenerates and checks the dataset.
- [#693 plan](plan-game-feel-693.md) and [gameplay spec](gameplay-spec.md): broader status and governing design.
- [Original #693 research](research-game-feel-693.md), [#701 comparison](research-game-feel-701-comparison.md), [geometry](research-game-feel-701-geometry.md), [proportions](research-game-feel-701-proportions.md), and [race traces](race-traces.md): evidence and validation procedure.
- Immutable history: [#693 tracker archive](research/game-feel-693-tracker-history.md) and [#708 tracker archive](research/game-feel-708-tracker-history.md). Do not rewrite these to hide corrections.

## Accepted anchors to preserve

This is orientation, not a substitute for the detailed scope and exceptions in the report/JSON.

- **Space:** C80 leads: 80-foot basepaths; 232 / 280 / 232-foot fences; unchanged character stature; 12-foot wall initially. C70 remains an unselected alternative. Preserve pitch-flight pacing when moving the mound.
- **Runners/throws:** middle runner nominal 2.95 seconds per bag plus .50-second batter startup. Ordinary throw release .30 seconds; neutral middle-arm 80-foot flight .90 seconds. Long throws have moderate range-dependent slowdown; each relay leg requires its own command. Good/bad pair chemistry modifies travel by 1.30x/.90x speed. Snap Throw, Laser and .25-second throw-buffer/cancel rules have explicit accepted scopes in the dataset.
- **Pursuit:** Run-5 ordinary top speed 18 ft/s, one profile across positions/hit classes with character differences. Reads from contact: outfield .40, infield .25, pitcher .35, catcher .45 seconds. Acceleration/braking/turn and analog rules already exist in the dataset; do not restart those approvals.
- **Dash:** only a character with the Ball Dash ability automatically receives the approved 20% speed increase while holding a secure live ball. No activation button, universal field dash, timer or cooldown. Current roster assignment is unresolved. Runner dash is separate.
- **Ratings:** the later accepted split supersedes summary-only Fielding. Arm controls throwing; Fielding is the hands/recovery input, seeded from current Field for parity. Reach is separately authored; neither rating controls glove positioning or grants an ability.
- **Recoil/specials:** routine clean pickups/catches have no generic added pause. Ordinary retained-ball recoil and special recovery stack for the same impact, preserving defensive-quality value. Special hit contracts may override ordinary behavior explicitly. Shared recovery rails do not approve individual special attack values, dislodging or multi-hit exceptions.
- **Jump:** one press, fixed normal arc, player-owned takeoff, limited air correction, no hold-height or double jump. Trial root rise 2 ft over .60 sec; throw readiness waits for landing and other locks. Full buffer/action rules are already recorded.
- **Errors:** qualified difficult handling can fail based on difficulty and defensive quality; routine opportunities stay reliable. Trial `p = .10*D*(1-.80*H)`, capped at 10%, with D/H mapping still open. One seeded draw per genuine attempt. An awkward in-between hop is the first difficulty source; a hard-hit label alone does not qualify.
- **Recovery:** contacted handling failures cause a shared .40-second stun; helpers and ball remain live. Defense quality reduces frequency rather than shortening this stun. Same-error recovery is reliable across fielders/distance/bounces until possession/dead-ball end. Untouched misses do not earn this protection or a handling stun.
- **Error direction:** contact-led with uniform angular variation, local ±30°, continuing ±15°. No frame/selection rerolls.
- **Nearby bobble trial:** initial vertical speed zero; gravity from actual contact position; horizontal speed `min(.20*incoming horizontal speed, 6 ft/s)`. Ground vertical retention .35, rebound ceiling 6 inches, settle at actual ground impact if predicted next rebound is at most 3 inches. Retain 90% horizontal speed per actual ground impact; supported rolling deceleration 6 ft/s². No forced airborne snap or fixed scatter endpoint.
- **Continuing deflection trial:** retain 50–80% incoming horizontal speed and apply the same factor to signed vertical speed; then use shared ordinary batted-ball ground response. Exact simplified contact factor is unresolved. Do not inherit the nearby-bobble caps, and do not interpret shared response as approval of legacy bounce/friction numbers.

## Next work — parity first, explicit prototype dependencies

**Review fixes authorized September 15, 2026.** Jack asked to implement all four review recommendations. The eight later approvals remain: roughly 6-foot stand-up reach; earned-only human dive; deliberate CPU dive that may miss; .60-to-.465-second quality-scaled dive recovery; Arm/Fielding split; .0040 drag trial; one movement profile for covering bodies starting without a read; and live-ball CPU dive commitment while ordinary pursuit keeps the route planner. No approval is being reopened. See the [current report](research-game-feel-708.md) and exact candidate payloads for scopes and evidence.

The parity slice is next and may proceed at current values. Arm/Fielding and independent reach are seeded from the current rules; no third independent defensive-quality stat or roster rebalance is introduced. Existing expectations must remain unchanged. Geometry moves into its shared data owner at current values first. Reuse this child worktree only for the research; implementation follows the plan's one-child/one-worktree discipline.

**The compact contract is not complete.** The [sequenced plan](plan-game-feel-693.md#implementation-plan--sequenced-september-15-2026) and candidate `implementationReadiness` name four open mappings: awkward-hop difficulty D, normalized Fielding quality H, local-versus-continuing failed handling, and continuing speed/direction. Implement simple reproducible trial rules and representative scenarios before declaring prototype completion. No glove geometry or new severity roll. Routine choices do not need another microscopic approval interview; material gameplay tradeoffs still go to Jack one at a time.

Other dependencies remain visible: retained-ball recoil trigger speeds, ordinary bounce/roll/wall calibration, per-character reach, and applicable status/special/throw-before-cover behavior. Deferral can bound a prototype; it does not waive complete-contract validation. Separate parity migration, prototype completion, whole-race validation, presentation/kit alignment, human acceptance and explicit default promotion.

**Corrected evidence:** drag is not neutral to grounders. In the production model, the 80-mph, 8-degree example reaches 100 feet at 1.566 s versus 1.752 s, with first bounce at 115.8 versus 102.3 ft. P5 Heat Swing carries about 282 ft but hits the 12-foot center wall at about 3 ft high. Keep the approved .0040 trial and star power; evaluate actual fence crossings and coupled grounder races. No all-fly-caught, increased-doubles or globally-homerless conclusion is established by these probes.

**Reproduction:** `dotnet run --project tools/game-feel-flight-probes -- --check` verifies source-tagged production-model evidence from [fixed inputs](research/game-feel-708-flight-inputs.json), with [results](research/game-feel-708-flight-derived.json). `--write` regenerates after review. The Python arithmetic check is separate and does not execute trajectories. No whole compact game or human gate has been validated.

The governing gameplay spec now records the new approvals and labels the conflicting summary-only architecture historical. Candidate approval evidence and immutable tracker history remain preserved. `F693-02-error-response-mapping` is implementation-design work; zero human questions are pending.

**Reference limits:** still no matched Wii/GameCube bobble, error, dive or catch-reach measurements. The earlier clip attempt stopped at pre-roll advertising. New evidence is our production flight model, not Nintendo measurement. The original compare-both-reference objective remains open for stronger observations.

## Validation and publishing workflow

Use this worktree and stage explicit paths only. Confirm current branch/status before every commit. The recurring design-edit set is the spec, plan, report, candidate JSON, derived JSON and checker; edit only those that actually need a change. No `git add -A` or `git add .`.

```sh
python3 tools/compact-field-report.py
python3 tools/compact-field-report.py --check
dotnet run --project tools/game-feel-flight-probes -- --check
git diff --check
```

The Python check verifies three spatial profiles and authored arithmetic; the .NET probe check executes production ball trajectories. Neither verifies complete gameplay. For documentation-only handoff work, no runtime tests, Unity build or standalone launch is warranted. Do not add runtime tuning to make the research feel complete. Human gates stay human; R3/#693/#708 remain open.

After real design updates, keep #693, #708 and draft #709 coherent with current immutable commit links. Fetch fresh bodies first; do not overwrite other edits with old `/tmp` copies. Use `gh ... --body-file` for multiline edits. The latest issue section begins **“Latest direction / correction — simple arcade fielding”**, not the former “Latest approval” heading. PR #709 has a leading overriding-direction paragraph and a “Next research action” segment; preserve their meaning. Its long historical body was about 42,664 characters at the last design update, so avoid appending the whole conversation.

Old `/tmp` editing/sync scripts are historical conveniences, not safe replayable migrations. Do not rerun them blindly. Network operations may need sandbox escalation. No approval review rejection was outstanding at handoff.

No subagents or new tasks unless requested. No merge, issue closure, runtime implementation or human-gate claim is implied by this handoff. The next agent should acknowledge the latest simplification and continue the research, not spend Jack's remaining usage on another intake interview.
