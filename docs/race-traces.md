# Race observations for #693 / #702

Session kind: **Gameplay — measurement infrastructure**. This extends `PlayTrace`, the scenario harness, `ContentDataValidator`, and `cli match`. It changes no gameplay coefficient. Jack's accepted directions and unresolved numerical choices remain in [D19](spec/00-decisions.md#02-field-proportions-and-race-calibration--d19) and the [decision register](plan-game-feel-693.md#decision-register).

## Version 2 contract

Enable `Match.Tracing` or `LivePlaySystem.Recording` **before** beginning the fixture. A `PlayTraceLog` contains the match seed, captains, park, effective input identity, and individual plays. Each live trace carries its original inning/half/count, batter and pitcher (ids, hands, stats, abilities), seats, difficulty, park geometry, submitted commands, ticks, ordered event marks, and completed outcome. A pitch without a live-ball interval has no fabricated contact or travel timestamps. Enabling recording halfway through a play produces a partial observation, not a complete replay fixture.

`identity.inputsJson` freezes the effective rules (including difficulty overrides), feel table, park, teams/order/equipment configuration, pairwise chemistry, star skills, night/mercy/innings settings. `sha256` hashes its UTF-8 bytes. `build` is the assembly informational version; `moduleId` identifies the compiled module. An informational version alone does **not** prove a clean checkout. The reproduction report also records source revision and file hashes. No trace identity reads RNG state or gameplay files during a tick.

Distances are **feet**, simulation times **seconds**, observed velocity **feet/second**. Zero is live-play start: batted-ball contact, or the start of the catcher's/pickoff live interval. A steal's preceding pitch and runner head start are not silently included in a contact-to-bag budget. Tick commands retain their submitted delta, pads, source, and effect-in-flight flag. Ownership filtering remains in `LiveSeats`; the trace does not reinterpret an offense press as a defensive command.

Version 1 dumps remain readable and identify as version 1 when the version is absent. Unknown versions are rejected. Unavailable research bounds remain JSON `null`. Existing sim command values such as an undefined fence clearance can round-trip as named nonfinite strings; those strings are **not valid numerical evidence bounds**.

## Events and race arithmetic

Marks contain an ordinal, command index, simulation time, throw-leg id where applicable, actor/bag, runner state, and event geometry. The ordinal distinguishes multiple operations within one tick, including receiving and immediately relaying a ball. The sim still decides every outcome.

- `Possession` is the actual acquisition call, including a later reception or loose-ball recovery. Use the **first** batted-ball possession for contact-to-possession, and keep subsequent possessions in the chain.
- `ThrowRelease` is `BeginThrow` starting the sim's linear travel. It records the thrower, intended receiver, actual target, arm/chemistry multiplier and total duration. The current `releaseSec` is inside that duration; **there is no separately observed stationary release phase**. Rendered hand release is unobserved here.
- `ThrowTargetReached` records the first tick the travel clock reaches its duration, even if an effect postpones resolution. The nominal target time is release plus duration; the observed tick can be later.
- `UncoveredWait` records the first failed receiver-coverage check for the leg. It is separate from both target arrival and `Reception`. A waiting lob can become a `LooseBall` with no reception. Cutoff throws use bag 0 and retain the cutoff's position.
- `Reception` records the receiver's **pre-snap** geometry and coverage test. A receiver in reach is not automatically an out: possession, runner state, force eligibility or tag geometry still decide the play. Per-tick `coverage` records assigned cover proximity and possession proximity separately from `forceAtBag`.
- `RunnerArrival` records the runner tick's arrival notification. `lowerT` and `t` bound that observation interval; `runner.lastTouchAt` retains the sim's touch clock where available. A run-through followed by returning to first generates two notifications. Use the first legal touch for the initial race, and retain the return as a separate event.
- `RunnerAward` distinguishes a verdict placing a runner on a bag from a tick observing a physical touch (including close-play resolution).
- `Out` records the actual retirement, its type, bag, runner's retained position/state, and glove geometry. The runner may be removed from the next tick when the match commits. For an out at a bag, `predictedRunnerAt` is the current sim's remaining-distance query plus elapsed time. It is a **counterfactual projection**, not an actual arrival by a retired body; it is not a second simulated outcome.

A routine force budget is contact → first possession → release → target arrival → covered reception → verdict. Compare that with the runner's actual first touch, or explicitly labeled projection if retired. Do not add pursuit and flight as successive durations: the flight continues while fielders read and pursue. Do not add the receiver's coverage delay twice when it overlaps the throw.

## Movement evidence

Every tick includes all nine assigned fielders: character identity, position, selected/human-owned state, read eligibility time, nominal pursuit speed, preview freeze and held-dash flags, handoff coast, cutoff/backup assignments, and selected-body dive/jump/recoil/swap timers. `observedVx/Vz` is displacement between adjacent samples divided by elapsed time. Its first sample is null; it can include a legal catch/receiver snap. It is not an inferred controller signal or a new acceleration model.

Read eligibility, cover walking, cutoff/backup movement, automatic assistance, special actions, and selected-glove steering have different existing conditions. A field named `pursuitSpeedFtSec` does not claim the body was pursuing at that speed: use the commands, role/timer flags, coverage, and measured displacement together. Camera handoff, rendered facing, pose, footfalls, and visual ball size are outside this gameplay trace. Unknown reference-video input cannot establish controller latency.

## Evidence catalog and sampling plans

[data/agent/race-evidence.json](../data/agent/race-evidence.json) is checked by `RaceEvidence` through the existing `ContentDataValidator` load/validate path. Sources need locators, status and uncertainty; fixtures need locators and a fixed-input/tactical/cohort kind; decisions need locators and state. Every budget references those ids and declares units, uncertainty, purpose, acceptance, and whether it is an active default. Missing provenance or units, duplicate ids, unknown references, nonfinite or inverted bounds, missing accepted bounds, and unresolved active defaults are errors. This catalog is observation/governance data: **no gameplay system loads a coefficient from it**.

The initial accepted record preserves F693-06: **1.8–5 mean runs per side**, separately home and away, on the existing S-29 cohort. The ordinary-throw numeric target is pending with null bounds. No Mario number has been promoted into an active default.

`RaceCohort` predeclares these plans before measurement:

- `s29`: the existing five captain pairs, both home/away orders, seeds 1–5, three innings and existing mixed-park selection. The original S-29 test is unchanged.
- `harbor-calibration`: the same pairs/orders/seeds, all at Harbor Diamond.
- `harbor-validation`: the same pairs/orders at Harbor, seeds **1001–1005**, disjoint from calibration.

Jack accepted F693-06-H on September 14, 2026: each Harbor cohort now has the same 1.8–5 mean-runs target, separately home and away, in addition to S-29. This is a calibration acceptance target; the baseline has documented misses and does not pass it. Once inspected, a validation cohort is a fixed regression cohort rather than fresh unseen evidence; reserve additional unseen seeds before evaluating future candidates. Reports retain each game's identity, score, outcome counts (including doubles and triples), and multiple-out plays. Those outcome counts do not establish relay opportunity rates or player enjoyment; the live fixtures and subsequent sitting own those questions.

The measured results, verification revisions, and subsequent accepted Harbor scoring decision are in [the #702 report](research-game-feel-702.md); its [dataset](research/game-feel-702-baseline.json) is derived by `tools/race-report.py`.

## Reproduce

Run from the #702 worktree at the revision named by the measurement report:

```sh
dotnet test src/GrandSluggers.Sim.Tests/GrandSluggers.Sim.Tests.csproj
GS_RACE_TRACE_OUTPUT=/tmp/gs702-traces dotnet test src/GrandSluggers.Sim.Tests/GrandSluggers.Sim.Tests.csproj --filter FullyQualifiedName~RaceTraceTests
dotnet run --project src/GrandSluggers.Cli -- match --home rio --away ashlord --park harbor-diamond --seed 7 --trace /tmp/gs702-match.json
dotnet run --project src/GrandSluggers.Cli -- match --cohort s29 > /tmp/gs702-s29.json
dotnet run --project src/GrandSluggers.Cli -- match --cohort harbor-calibration > /tmp/gs702-harbor-calibration.json
dotnet run --project src/GrandSluggers.Cli -- match --cohort harbor-validation > /tmp/gs702-harbor-validation.json
```

The fixture export includes S-31/S-32 at **Crystal Rink**, S-33 with one/versus human defense, a scripted two-leg relay, a Harbor fly, and both fixed-exit and fixed-tactical Harbor grounders. The fixed-input grounder stays **84 mph / 8° / −12°**. The tactical fixture asks `FlightFixtures.Landing` for **118 ft / 4° / −18°** and reports the solved exit velocity. Preserve both; the inverse solver must not conceal flight changes.

The `fault-uncovered-*` fixtures change cover start only in a temporary catalog copy to exercise waiting/reception and dropped-lob branches. Their effective identities disclose the perturbation. **They are validator fault fixtures, not calibration candidates or production measurements.** Production `data/rules/` is unchanged.

A serialized live command stream replays identically after the same seeded fixture setup; it is not an arbitrary mid-match save state. Reproduce preceding `BeginAtBat`, runner placement, and other match commands as the named test does. Full CPU matches reproduce from seed plus effective content and build. Tests compare traced and untraced outcomes, replay serialized commands, and check forces, tags, both relay legs, cutoff reception, uncovered waits/drops, overlapping flight/read/pursuit, and seat ownership. These checks do not pass Jack's readability, proportion, or standalone gameplay gates.
