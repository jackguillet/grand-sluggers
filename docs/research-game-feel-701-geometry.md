# Field proportions — evidence and the deep-hit decision

Research child [#701](https://github.com/jackguillet/grand-sluggers/issues/701), parent [#693](https://github.com/jackguillet/grand-sluggers/issues/693). September 14, 2026. Gameplay research/documentation; no runtime change. Companion to the [first observation packet](research-game-feel-701-comparison.md) and [decision register](plan-game-feel-693.md). Raw reports, calculations, and hypothetical sensitivity inputs live in the [geometry dataset](research/game-feel-701-geometry.json).

## Finding

A Wii community experiment gives a useful **candidate for a more compact outfield relative to the diamond**. Its equal-speed interpretation is approximately **2.87 basepaths to left field, 3.52 to center, and 2.88 to right**, compared with Harbor's audited **3.67 / 4.44 / 3.67**. That is about 21% less home-to-wall distance at the same basepath length. This is conditional arithmetic from a reported experiment, not an independently reproduced Nintendo measurement or an accepted Harbor target.

The result says nothing about how long a basepath should be relative to a character. Both infield and outfield remain independently adjustable under Jack's accepted direction. A 21% reduction in home-to-wall distance is also not a 21% reduction in every fielder's pursuit distance, field area, or play duration.

## Wii: retain the raw times, discard the assumed feet

In an [original field survey](https://www.reddit.com/r/MarioSuperSluggers/comments/xdvwn9/my_crude_attempt_at_measuring_the_size_of_the/) published September 14, 2022, **therevd0g** reports timing Pianta from third to home at **3.65 s**, then from Mario Stadium's left pole, center wall, and right pole to home at **10.49 / 12.86 / 10.51 s**. The author assumes a 90-foot basepath to publish distances. Neither the feet nor that basepath assumption is established by the experiment. Repeated trials, precise endpoints, stock configuration, dash, route, and starting motion are not documented.

Our calculation divides each wall time by 3.65. This removes the assumed unit conversion, but **does not remove the running-model assumption**. Let `q = effective wall-run speed / effective base-run speed`. Then `wall distance / basepath = q × wall time / base time`, provided the reported paths actually join those landmarks. Acceleration, different fielding/baserunning movement, dash, and timing endpoints can change effective speed. The source does not establish `q = 1`.

## How fragile is the estimate?

These are deliberately chosen stress cases, **not measured error bars or confidence intervals**:

- At `q = 0.9`, center is **3.17 basepaths**; at `q = 1.1`, it is **3.88**. Both are shallower than Harbor's 4.44, but the source does not bound `q` to that interval.
- With equal speeds and an independently assumed **±0.15 s** endpoint error on each reported duration, center spans **3.34–3.72**. This checks timing sensitivity only; it does not cure movement-model bias. Numerator and denominator endpoints are taken in opposite directions for the extrema.
- A center-field run effectively **26.1% faster** than the base run would produce Harbor's existing 4.44 ratio from the same reported times. The corresponding pole thresholds are about **27.6% / 27.3%**. Therefore this report alone cannot rule out Harbor-like geometry.

The three wall estimates share one base-run denominator and one method. They are correlated evidence, not three independent confirmations. The tiny left/right difference cannot establish stadium asymmetry.

## GameCube: a different measurement lineage

The June 5, 2021 original modding report, [“Mario Stadium is (kinda) MLB standard”](https://www.reddit.com/r/MarioBaseball/comments/nt2n3v), describes home at `(0,0)`, mound at `(0,18.4)`, and center depth of 100 coordinate units, interpreting the units as meters. Without that interpretation, the reported **center/mound ratio is 5.435**, versus Harbor's **6.612**. Thus this account also motivates investigating outfield depth independently of an infield landmark. It does not give base coordinates or a body/basepath ratio, and it does not establish equivalence with the Wii experiment.

The later [community stadium survey, revision 890](https://mariobaseball.miraheze.org/wiki/Stadiums?oldid=890), reports 100 m center and approximately 80.5 m poles. Its relationship to the earlier account is unverified; do not count their agreement as independent replication. The foundation's conversions to feet remain conditional comparisons to community-reported units.

Project Rio's public [GameData.h at revision 6b9748a](https://github.com/ProjectRio/ProjectRio-Modding/blob/6b9748a87c9383c4eb5b2cf79cc1e7836c3cfb3d/Game%20Data/GameData.h#L33940) identifies `base_MoundCoordinates` at `0x807B5A44` and [`baseCoordsForRunning`](https://github.com/ProjectRio/ProjectRio-Modding/blob/6b9748a87c9383c4eb5b2cf79cc1e7836c3cfb3d/Game%20Data/GameData.h#L34016) at `0x807B6034`. These are useful reproduction leads. **Memory addresses are not coordinate values**; inspecting the header does not reproduce a stock game's field dimensions. We obtained no game binary, assets, or live memory capture.

## Visual screening

The already cataloged Wii recording at **00:05** is a park-selection miniature, not an eligible gameplay geometry view. At **00:35**, the stadium introduction has obstructing team graphics and an incomplete diamond; at **00:40**, the view is low and centered on the pitcher. The GameCube introduction at **00:10** lacks all four bag centers; **00:15** is the pitching setup. None supplies the four same-plane landmarks needed for the intended square-diamond homography. No pixel-derived field or body ratio is claimed from these frames. Intros remain excluded from live event timing.

## Candidate experiment and the next human decision

**Proposed research candidate C1, not accepted:** rounded wall/basepath ratios **2.9 / 3.5 / 2.9**, against control C0 **3.6667 / 4.4444 / 3.6667**. C1 is a Harbor-authored trial motivated by the Wii report, not a claim of exact Wii geometry. Basepath/body scale, mound placement, wall height, wall shape between sampled directions, fielding starts, carry, and every clock remain unresolved. This partial candidate must not enter runtime data as a complete profile. For orientation only, retaining 90-foot basepaths would express C1 as 261 / 315 / 261 feet; that illustration does not select 90 feet.

Before completing C1, Jack needs to decide the **deep-hit experience** under F693-02:

- **Recommended: preserve playable deep hits and extra-base decisions.** Strong ordinary contact can reach gaps or the wall and create doubles, triples, relay choices, and throws home; home runs remain distinct rewards for especially good power/contact combinations. This names opportunities, not guaranteed bases or a per-hit result. A compact field may require coordinated carry and pursuit calibration to preserve them.
- **Alternative: emphasize home-run opportunities.** More strong deep contact is intended to clear the wall, accepting fewer live deep-field races. This still must satisfy the retained scoring guardrail; the same runs-per-side average can conceal very different hit distributions.

This is a pending taste decision, not a measured finding about either Mario game's home-run frequency. The inspected samples cannot estimate that frequency. Reliable routine infield defense, hard-liner positioning, visible throws, and D7 remain accepted constraints under either choice.

After that choice, complete the geometry/body comparison and #702 race budget before requesting dimensions or coefficients. Compare identical fixed exit/launch inputs for fence crossings and catch opportunities, then matched tactical fixtures for gap retrieval, a wall carom, relay, and a throw home. Record extra-base opportunity and home-run frequency separately from S-29's scoring mean, using a declared Harbor cohort and held-out seeds. Do not hide a fence change by using only carry-preserving fixtures. Exact dimensions and Jack's standalone acceptance remain open.

## Validation

The dataset preserves raw reported times, derived dimensionless ratios, assumed sensitivity cases, source revisions, and pending candidate status. Recompute all ratios and stress cases from those inputs; verify reference ids and that accepted targets remain empty. These are documentation checks, not runtime or human feel validation. No new gameplay test run or standalone build is claimed.
