# Measured race baseline — #702, September 14, 2026

The [version-2 trace contract](race-traces.md) is implemented in the #702 draft stack. The [machine-readable report](research/game-feel-702-baseline.json) contains 11 fixture observations and all 150 cohort games with effective-input identities. [tools/race-report.py](../tools/race-report.py) validates those identities and derives the report from exported traces and cohort logs. No gameplay or feel coefficient changed. No human gate passed.

## What the measurements establish

The existing S-31 grounder at **Crystal Rink** reaches the shortstop's glove at **1.033 s**, starts its throw at **1.483 s**, and reaches its covered receiver at first at **2.800 s**. The remaining-distance query projects the retired Run-5 batter's first-base arrival at **3.458 s**: a **0.658 s** defensive margin. That arrival is a counterfactual projection, explicitly labeled in the trace. The actual out occurs at 2.800 s. The thrower's Field stat is 3, so this named scenario is not evidence that every average-arm routine play has that margin.

The paired S-32 fixture uses Run-9 Dart on the same tactical grounder: possession remains **1.033 s**, the CPU elects not to throw, and the first touch of first is observed at **2.850 s** (sim touch **2.849539 s**). Returning after the run-through produces another arrival notification at **3.517 s**. S-33 one-player and versus human defense both acquire the ball at 1.033 s and do not invent a throw on a dead stick.

A scripted human defense relay produces two distinct releases/receptions: **1.183 → 1.867 s** to second, then **1.883 → 2.667 s** to first. Both outs retain their runner state and timing query. The separate cutoff test verifies bag-0 reception and its receiver's pre-snap coverage geometry. These are fixture demonstrations, not frequency targets or proof that relay opportunities survive a compact redesign.

The Harbor fixed-input grounder (**84 mph / 8° / −12°**) is possessed at **1.617 s**, released at **1.967 s**, and received at **3.217 s**. The tactical **118-ft / 4° / −18°** fixture solves for **106.9 mph**, and matches S-31's 1.033 / 1.483 / 2.800 s timeline at Harbor. They answer different questions. Changing a flight table must expose the fixed input's changed path; regenerating the tactical fixture alone could conceal it.

The read/movement fixture records a ball already traveling while fielders remain in their read window, followed by pursuit while the ball is still airborne. A separate human-input prefix records initial stick intent, eligibility, displacement, reversal, pause and resume. No camera or controller-latency estimate is inferred from those sim clocks.

Two **fault fixtures**, excluded from the production baseline interpretation, temporarily delay cover walking. In one, a throw reaches its target at **2.500 s** but is not received until **3.083 s**. In the other, that leg has no reception and becomes loose at **4.000 s**, followed by another attempted throw. Their modified effective inputs are identified in the report. This demonstrates why target arrival cannot stand in for reception or an out.

## Scoring and extra-base observations

Every cohort contains the same five captain pairs, both home/away orders, and 50 completed games. Seeds and cohort policy were committed at `769a9c0604d2baaa6e6fd4b36035c7f5136c9851` before running these measurements.

- **Existing S-29, mixed parks, seeds 1–5:** mean runs **1.90 home / 1.92 away**. Totals: 187 singles, 72 doubles, 12 triples, 64 home runs; 9 multiple-out plays. Both means satisfy the accepted **1.8–5** S-29 band.
- **Harbor calibration, seeds 1–5:** mean runs **1.90 home / 1.38 away**. Totals: 174 singles, 55 doubles, 12 triples, 58 home runs; 12 multiple-out plays.
- **Harbor validation, seeds 1001–1005:** mean runs **1.60 home / 2.30 away**. Totals: 191 singles, 67 doubles, 16 triples, 56 home runs; 8 multiple-out plays.

The two Harbor cohorts together average **1.75 home / 1.84 away**, but the individual cohorts show substantial variation. These are observations from predeclared seeds, not confidence intervals, a causal explanation, or an excuse to tune outcomes directly. Both sets have now been inspected; they are fixed regression sets, not future unseen evidence. The raw game rows preserve roster/order/park identity so later analysis can distinguish matchups from field effects.

Passing the mixed-park S-29 check does not establish Harbor's own scoring pace. The existing accepted F693-06 decision explicitly applies to the mixed-park cohort. **No Harbor-specific scoring band is currently accepted**, so the lower Harbor means are a design-review finding, not a newly declared failing gate. Doubles/triples counts also do not establish the quality of their opportunities: compact-field proposals still need the full retrieval/throw/runner races and Jack's sitting.

## Next human decision — F693-06-H, Harbor scoring scope

**Pending; recommendation only:** apply the same **1.8–5 mean runs per side** target to Harbor-specific calibration and validation cohorts, checking home and away separately while retaining the existing mixed-park S-29 guardrail. Continue reporting each cohort and the kinds of plays; do not meet the target through forced outcomes or by expanding the outfield against the accepted direction.

Why ask now: the mixed-park pass can hide a different pace in the park Jack actually plays. This recommendation adds a Harbor-specific design target; it does not authorize any particular tuning value or declare the current game human-approved. The alternative is to retain the band only for mixed parks and leave Harbor's scoring reports diagnostic. Jack has not selected between these scopes yet.

No next numerical geometry, running, release/travel, or visual target is approved by this report. The full compact geometry/body/race proposal remains R3. The accepted moderate ball assistance remains a separate presentation decision.

## Verification and reproduction

- Full solution suite at instrumentation revision `769a9c0`: **1,048 passed**.
- After review corrections at `14ea6a5468f7ea5977ae288ad645cbb7e1fe68f0`: **26 affected trace/evidence/cutoff tests passed**, including the added movement-prefix test. Exported fixture identities name this revision.
- The 150 cohort games ran the committed `769a9c0` build. The later changes concern observation geometry, runner-award marks, and tests; the gameplay tables and outcome logic remain unchanged.
- `cli match --home rio --away ashlord --park harbor-diamond --seed 7 --trace ...` completed: Ember Court **6**, Spark All-Stars **3**, in five innings after the scheduled three tied. The trace includes every play; this is not a standalone sitting.
- Unity's narrow C# compatibility check passed using the primary checkout's existing package-assembly cache. No Unity import, player build, visual inspection, or human gate pass is claimed.
- Final catalog: **18 evidence/content tests passed** with the Harbor proposal still pending. The CLI rejects cohort overrides; regenerating the report from its raw exports produces identical output.
- Version-2 JSON round-trips; serialized commands replay the same fixture geometry; traced/untraced full-match outcomes agree. Source hashes and effective-input digests are preserved in the report.

Use the commands in [race-traces.md](race-traces.md#reproduce), then:

```sh
python3 tools/race-report.py --traces /tmp/gs702-traces \
  --cohort /tmp/gs702-s29.json \
  --cohort /tmp/gs702-harbor-calibration.json \
  --cohort /tmp/gs702-harbor-validation.json \
  --output /tmp/gs702-baseline.json
```

The raw exports are reproducible local artifacts; the committed summary preserves event marks, race legs, source hashes, fixture contexts, and individual cohort game results. Rebuilding at another revision changes build/module identity; compare effective inputs and the actual measurements, rather than expecting a raw-file hash to ignore a new build.
