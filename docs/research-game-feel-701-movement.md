# Compact field: movement consistency decision

[#701](https://github.com/jackguillet/grand-sluggers/issues/701), parent [#693](https://github.com/jackguillet/grand-sluggers/issues/693). September 14, 2026. Gameplay research/documentation. Companion to the [accepted compact-field direction](research-game-feel-701-geometry.md) and [decision register](plan-game-feel-693.md). The [audit dataset](research/game-feel-701-movement.json) retains formula inputs, source hashes, conditions, and the accepted design direction. No runtime change.

## What the current game already separates

At `dc86fb9`, fielding and baserunning use the same character Run stat through **separate formulas**. The seven audited source files are byte-identical to the foundation's `eff10d9` baseline. With no dash, ability, or frozen status:

- Base fielding speed is `21 + 1.9 × Run` ft/s. At Run 1 / 5 / 10 this is **22.9 / 30.5 / 40.0 ft/s**.
- Baserunning speed is `90 / clamp(3.55 − 0.12 × Run, 2.45, 3.65)` ft/s: **26.24 / 30.51 / 36.73 ft/s** at those same stats. The batter's 0.5-second start delay is separate from moving speed.
- An outfielder with a non-grounder hit preview receives a **0.6 multiplier**: **13.74 / 18.30 / 24.00 ft/s**. An infielder with a non-grounder, non-liner preview receives **0.45**: **10.305 / 13.725 / 18.000 ft/s**. Infield liners use the base speed.

Source owners: [Fielding.cs](../src/GrandSluggers.Sim/Fielding.cs) `ChaseSpeedFt`/`AirMul`, [Runner.cs](../src/GrandSluggers.Sim/Runner.cs) `BagSec`/`SpeedFtPerSec`, and their existing rule tables. The choice is not whether to create separate offense/defense tuning; that separation already exists.

**Predicate matters:** `AirMul` checks the hit preview's `Grounder` and `Line` flags and assigned position, not the ball's current height. Retaining that preview after a bounce retains the multiplier at those call sites. Do not describe this as a verified speed change exactly at landing. Loose-ball/base-speed paths and abilities must be annotated separately. These are source-derived values, not observed averages or evidence that a particular play feels slow.

## Why this is a design decision

For the same ordinary input, a Run-5 outfielder can receive 30.5 ft/s for a grounder preview and 18.3 ft/s for a non-grounder preview. Current gameplay-spec §8.1 explicitly permits these modifiers; §8.2 documents their historical role in catch coverage and S-29 scoring. They cannot be silently removed as a cleanup or treated as an implementation accident.

Jack's compact-field direction allows shorter pursuit distances and correspondingly slower movement. His subsequent consistency decision below settles that ordinary pursuit should not additionally change because of hit classification or assigned fielding position.

The inspected GameCube [Fielding Mechanics, revision 410](https://mariobaseball.miraheze.org/wiki/Fielding_Mechanics?oldid=410) describes ordinary fielding velocity through character speed, sprint, and Ball Dash modifiers. Its listed formula contains no hit-class multiplier. The separately documented [Running Mechanics, revision 426](https://mariobaseball.miraheze.org/wiki/Running_Mechanics?oldid=426) includes baserunning acceleration, stamina, and dash. This supports investigating consistent ordinary pursuit and separate baserunning tuning. It is community reverse-engineering, not proof that every GameCube movement state shares one speed. Wii's corresponding numeric contract remains unverified. Previously retrieved source caches were re-inspected; web retrieval failed this turn. No new stock-game measurement or acceleration curve is claimed.

## Accepted direction — F693-02 pursuit consistency

**Accepted by Jack, September 14, 2026:** give each character one ordinary pursuit movement profile across grounders, liners, and flies, independent of their assigned fielding position. Keep explicit dash, abilities, and status effects, plus meaningful character speed differences. Fielding can still be slower than baserunning. This direction concerns physical movement, not identical animation poses or a new controller scheme.

The intended benefit is that the player can learn how their chosen character moves. Distinct catch opportunities would come from ball path/hang, positioning, route, and reach within the compact geometry, rather than a hit-class multiplier on the character's legs. The tradeoff is substantial: compact geometry, ball flight, and coverage must be recalibrated together to preserve the accepted routine defense, hard-liner behavior, deep hits, and scoring guardrail.

**Alternative considered, not selected:** retain deliberate hit-class/position movement modifiers. They allow coverage of liners and flies to be adjusted separately, but the same character can respond at different travel speeds across otherwise similar inputs. If chosen, each modifier still needs a stated gameplay reason and comparison; S-29 alone cannot justify it.

The accepted direction selects no top speed, acceleration, reaction lockout, catch radius, or dimension. Current §8.2 reaction rules, D7, D16–D18, dash ownership, and geometric outcomes remain in force until separately addressed. Numerical C1 field ratios remain unaccepted.

## Implementation and evidence boundary

Do not simply set the existing multipliers to 1. That would speed current airborne-hit pursuit substantially without supplying the compact-field movement contract. With consistency accepted, migrate to a complete named profile through the existing sim/rule owners, then compare it with the unchanged baseline. The movement predictor and actual stepping must agree: [FieldingPursuit.cs](../src/GrandSluggers.Sim/FieldingPursuit.cs) currently predicts using constant speed, while ordinary stick and `StepToward` paths integrate `speed × dt` after movement eligibility. A future acceleration choice therefore needs shared prediction and integration, not an animation-only easing curve.

The #702 trace contract should distinguish input, movement eligibility, first displacement, movement mode/modifiers, actual velocity, possession, and the remaining throw/runner events. Compare the same character/input across grounder, liner, fly, post-bounce, loose-ball, and possession transitions, plus infield/outfield assignments and both seats. Gameplay fixtures must retain hard-liner positioning, short-pop catches, ordinary infield outs, and live gap/wall/relay opportunities. The standalone review must check control response and footfalls as well as race duration.

## Next human choice — F693-02 movement weight

**Recommendation, awaiting Jack:** ordinary pursuit should have a brief build-up to running speed and quick course corrections, with little residual drift when the desired movement changes. Slower sustained speed can preserve chase time in the compact field while the character responds promptly once movement is allowed. This is a proposed feel direction, not a measured Nintendo acceleration curve or an approved duration.

**Alternative:** heavier momentum, with more time required to build speed, brake, and reverse. Routes become more committed, and a wrong first step costs more recovery time. That can express weight, but also reduces the player's ability to adjust under a fly or recover toward a rolling ball. Neither option guarantees an interception or changes the accepted hard-liner positioning requirement.

The current implementation separates several things that this decision must not conflate:

- **Physical movement:** ordinary stick integration directly applies `stick × speed × dt`; `StepToward` directly applies speed toward the target. There is no ordinary pursuit acceleration/braking ramp in these paths. Changing the intended direction changes the displacement direction immediately after movement eligibility.
- **Visible facing:** [BodyFacing.cs](../src/GrandSluggers.Sim/BodyFacing.cs) smooths observed velocity and turns the rendered heading at a bounded rate. That presentation behavior does not slow or steer the sim's ground position. A new physical acceleration model must be shared by prediction and stepping, then represented faithfully by the authored motion.
- **Reaction eligibility:** the existing position-based post-contact lockout is separate from acceleration after movement begins. This choice does not remove it, extend it, or approve using extra lockout to imitate weight.
- **Neutral stick:** [FieldAssist.cs](../src/GrandSluggers.Sim/FieldAssist.cs) permits automatic pursuit when the glove has no ball and the stick is neutral. Releasing the stick therefore is not a universal stop command. Judge braking when the active movement intent actually requests a stop or changes direction; do not silently change the assistance contract.
- **Handoff and special actions:** the previous glove's existing 0.2-second handoff coast, dives, dashes, and other named actions are separate states. This recommendation selects neither new timings for those states nor a new movement verb.

The ordinary physical response should follow the accepted per-character consistency rule across hit classes and assigned positions. Do not introduce hidden per-hit acceleration penalties after removing per-hit speed modifiers. Character differentiation remains; no new agility stat, heavyweight exception, or second movement system is selected here.

Before numeric acceptance, compare starting from rest, a 90-degree correction, a reversal, arrival at a catch plant, and transition into/out of assisted pursuit. Keep initial position, stat, input, and target equal while varying the response profile; then include actual grounder/liner/fly/wall plays and fast/slow characters. Record command/eligibility, first displacement, velocity, correction time/distance, possession, and the runner/throw budget. Track physical motion separately from rendered facing and footfalls. Unknown controller input in retrospective Mario videos cannot establish input-to-motion latency; verified input captures or an explicitly Harbor-authored response target will be needed.

This is the next pending taste choice only. Top speeds, acceleration/braking numbers, field dimensions, reaction rules, and all human play gates remain open. No runtime values change in this packet.

## Validation

Recompute the audit rows from the retained rule inputs, check source-file hashes and parity with `eff10d9`, verify local links, and verify the accepted design direction remains separate from the empty accepted numerical targets. This packet is documentation only; no runtime test rerun, standalone build, or human gate pass is claimed.
