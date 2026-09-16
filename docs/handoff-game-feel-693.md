# Game feel research handoff — #693 / #708

Prepared September 15, 2026 because Jack is nearly out of session usage. **Continue the existing work; do not restart the decision interview.** Session kind: **Gameplay research/documentation**. Last design commit at preparation: `59baf8c` (`docs: simplify fielding to arcade range and visual glove motion`); see the September 15 update in [Next work](#next-work--one-decision-is-now-waiting) for the current state.

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
- **Fielding rating:** broad summary of explicit defensive traits/abilities; it does not control glove positioning, tracking speed or a hidden reach roll. The old `Field` consumers still require an explicit trait migration plan before implementation.
- **Recoil/specials:** routine clean pickups/catches have no generic added pause. Ordinary retained-ball recoil and special recovery stack for the same impact, preserving defensive-quality value. Special hit contracts may override ordinary behavior explicitly. Shared recovery rails do not approve individual special attack values, dislodging or multi-hit exceptions.
- **Jump:** one press, fixed normal arc, player-owned takeoff, limited air correction, no hold-height or double jump. Trial root rise 2 ft over .60 sec; throw readiness waits for landing and other locks. Full buffer/action rules are already recorded.
- **Errors:** qualified difficult handling can fail based on difficulty and defensive quality; routine opportunities stay reliable. Trial `p = .10*D*(1-.80*H)`, capped at 10%, with D/H mapping still open. One seeded draw per genuine attempt. An awkward in-between hop is the first difficulty source; a hard-hit label alone does not qualify.
- **Recovery:** contacted handling failures cause a shared .40-second stun; helpers and ball remain live. Defense quality reduces frequency rather than shortening this stun. Same-error recovery is reliable across fielders/distance/bounces until possession/dead-ball end. Untouched misses do not earn this protection or a handling stun.
- **Error direction:** contact-led with uniform angular variation, local ±30°, continuing ±15°. No frame/selection rerolls.
- **Nearby bobble trial:** initial vertical speed zero; gravity from actual contact position; horizontal speed `min(.20*incoming horizontal speed, 6 ft/s)`. Ground vertical retention .35, rebound ceiling 6 inches, settle at actual ground impact if predicted next rebound is at most 3 inches. Retain 90% horizontal speed per actual ground impact; supported rolling deceleration 6 ft/s². No forced airborne snap or fixed scatter endpoint.
- **Continuing deflection trial:** retain 50–80% incoming horizontal speed and apply the same factor to signed vertical speed; then use shared ordinary batted-ball ground response. Exact simplified contact factor is unresolved. Do not inherit the nearby-bobble caps, and do not interpret shared response as approval of legacy bounce/friction numbers.

## Next work — one decision is now waiting

**Updated September 15, 2026.** `F693-02-arcade-fielding-validation` is **research complete**. The consolidated contract and the reach/coverage accounting are now the two new sections at the top of [the #708 report](research-game-feel-708.md#consolidated-simplified-fielding-contract). **`F693-02-catch-reach-envelope` was put to Jack and accepted the same day:** the ordinary stand-up catch reach is re-authored to roughly **6 feet** for a middle character on C80, superseding both today's absolute 13 feet and a basepath-scaled 11.56. Do not reopen it. Its full context, options, arithmetic and accepted scope are in `catchReachCoverageResearch` in the candidate JSON and the per-profile `catchReachCoverage` blocks in the derived JSON.

**`F693-02-dive-jump-scoop-reach` was put to Jack and accepted the same day.** The runtime audit found the dive fires automatically for any fielder the seat is not steering and for every CPU fielder, so 6 feet plus an automatic 8 was 14 feet of passive coverage — more than the 13 just removed. Jack removed the assistance dive entirely: **a dive happens only on a deliberate press**, passive coverage is the accepted 6 feet in the air and 10 on the dirt, and an earned dive reaches 14. He also directed that **a dive carries a recovery delay before the diver can throw or move**, so dives are a last resort rather than a default. That is new behaviour — `DiveT` today is only an arm window that widens the catch window and gates nothing. Do not reopen either part. The jump stays armed by the West press, the loose-ball scoop stays independent of catch radius, and the 4-foot dirt pad is unchanged and still deferred.

**Correction on the record:** an earlier draft said the seat steers one fielder while the other eight dive automatically. The sim runs exactly **one active glove**, `GlovePos`, steered by the seat or driven by `ChaseGlove` assistance on a dead stick and handed on by `TryHandoffOutfield` / `TryHandoffLoose`. The automatic dive was two cases, not eight: a human glove whose stick is neutral, and the entire CPU defence unconditionally. The 14-foot finding, the coverage arithmetic and both accepted decisions are unaffected. Do not re-derive this from the older wording.

**`F693-02-cpu-dive-intent` was put to Jack and accepted the same day.** The CPU defence reached the rim only through `AutoDive`, so removing it left it with no dive at all, and the dive is worth a flat 16-foot band of every gap. The CPU glove now gets **deliberate dive intent**: it chooses, **it can miss**, it pays the same recovery delay a seat pays, and it reaches the same 14 feet and no further. The assistance dive stays removed, so a human glove on a neutral stick still does not dive. Jack explicitly declined the convert-only option — **a CPU dive that fails and leaves the ball live is required behaviour, not a defect**, and anything that converts CPU dives by construction reintroduces the passive rim coverage two decisions removed. Do not reopen this.

`decisionQueue` has exactly one `next-human-decision`: **`F693-02-dive-recovery-cost`**, consolidated and put to Jack on September 15, 2026. The duration was derived rather than chosen — the illustrative routine race in `throwReleaseProposal.arithmeticOnlyExample` leaves a 0.50-second margin and a dive delay is spent inside it, so "last resort, not spammed" means an unnecessary dive should not still produce the out. **Proposed trial 0.60 seconds**, the shortest value at which the deterrent is unambiguous and above the 0.40-second handling stun. Two parts of the shape are proposed rather than asked: caught and missed dives cost the same, and the delay and the handling stun **overlap rather than adding** — that departs from the additive precedent in `F693-02-special-recovery-composition` and is flagged deliberately so it can be rejected on sight. The question actually put to Jack is **whether recovery varies by character**, and if so that the curve must be narrower than the recoil curve, because on the recoil curve the best defender reaches 0.33 s and an unnecessary dive is a comfortable out again. Context is in `diveRecoveryCostResearch`. **If no answer is recorded, it is still open — put it to him again rather than choosing for him.**

Open behind it: `F693-02-cpu-dive-intent-policy` (when the CPU spends a dive, how far ahead it may look — lookahead is the line between deliberate and psychic and needs an explicit budget — and difficulty scaling), and the explicit per-character reach source, since `10 + 0.6 × Field` is now contradicted by two accepted decisions and blocks implementation.

The two rows that came straight out of the dive answer: `F693-02-dive-recovery-cost` for the delay's duration, caught-versus-missed cost and character variation — Jack's earlier "unique dive ability" remark makes dive recovery the natural home for Fielding, which is worth raising when you get there — and `F693-02-cpu-dive-intent` for the cost he accepted with his eyes open, that eight of nine defenders and the whole opposing defence now stop reaching the rim. Research both before asking, and do not let the CPU side be settled by omission.

Then:

1. Review remaining meaningful dependencies together before proposing the next player-facing decision. Pending queue rows cover dive/jump/scoop reach, the explicit per-character reach source replacing `10 + 0.6 × Field`, defensive-trait migration, individual special attacks, movement/coverage, flight/bounce/roll/walls, and presentation/readability. The flight budget decides how much of the reopened window is actually used.
2. Return to Jack only when there is a material gameplay choice, with context and a recommendation. Continue one such decision at a time. Preserve the compact-field goal and whole-race validation requirements.

**Reference limits:** no matched Mario bobble/error/contact measurements exist in this packet, and no Mario catch reach has been measured — video supplies no world scale, so a reach figure cannot honestly be read from either reference. A September 15, 2026 attempt reopened the Wii clip page and was abandoned in pre-roll advertising before any play was inspected; nothing was measured or inferred from it. Manuals establish controls, not exact physics constants. Earlier throw clips and community geometry accounts are conditional evidence, not a calibrated Wii/GameCube equivalence. No C80 play evidence exists. Historical #702 tests are not a compact-field simulation. Never describe an arithmetic check as gameplay or look verification.

## Validation and publishing workflow

Use this worktree and stage explicit paths only. Confirm current branch/status before every commit. The recurring design-edit set is the spec, plan, report, candidate JSON, derived JSON and checker; edit only those that actually need a change. No `git add -A` or `git add .`.

```sh
python3 tools/compact-field-report.py
python3 tools/compact-field-report.py --check
git diff --check
```

These verify three spatial profiles and authored arithmetic, not gameplay. For documentation-only handoff work, no runtime tests, Unity build or standalone launch is warranted. Do not add runtime tuning to make the research feel complete. Human gates stay human; R3/#693/#708 remain open.

After real design updates, keep #693, #708 and draft #709 coherent with current immutable commit links. Fetch fresh bodies first; do not overwrite other edits with old `/tmp` copies. Use `gh ... --body-file` for multiline edits. The latest issue section begins **“Latest direction / correction — simple arcade fielding”**, not the former “Latest approval” heading. PR #709 has a leading overriding-direction paragraph and a “Next research action” segment; preserve their meaning. Its long historical body was about 42,664 characters at the last design update, so avoid appending the whole conversation.

Old `/tmp` editing/sync scripts are historical conveniences, not safe replayable migrations. Do not rerun them blindly. Network operations may need sandbox escalation. No approval review rejection was outstanding at handoff.

No subagents or new tasks unless requested. No merge, issue closure, runtime implementation or human-gate claim is implied by this handoff. The next agent should acknowledge the latest simplification and continue the research, not spend Jack's remaining usage on another intake interview.
