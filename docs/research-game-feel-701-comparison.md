# Wii / GameCube comparison — first observation packet

Research child [#701](https://github.com/jackguillet/grand-sluggers/issues/701) of [#693](https://github.com/jackguillet/grand-sluggers/issues/693). September 14, 2026. Session: **gameplay research/documentation**. Depends on the foundation in [PR #704](https://github.com/jackguillet/grand-sluggers/pull/704), revision `67dac68`. No runtime values change.

## What this packet supports

Both inspected games make possession, a throw, and the receiving play legible as separate actions. The first Wii example is a direct grounder-to-first out; the GameCube example is a grounder with a force at second and an unsuccessful second throw to first. These are useful side-by-side examples of action vocabulary. They are **not matched experiments** from which to declare one game faster, more forgiving, or correctly scaled.

Jack accepted the packet's **provisional visual-lead recommendation** on September 14, 2026: Wii for on-screen readability, GameCube as a mechanics cross-check. It does not complete #701 or supply the full numerical contract required by F693-01/02. The [decision register](plan-game-feel-693.md) retains the five earlier accepted directions and this provisional visual lead; numerical reference selection and exact playable geometry remain open. The [annotation dataset](research/game-feel-701-observations.json) preserves event brackets, source identities, exclusions, and explicit unknowns.

## Watch these two plays

1. **Wii — [00:57–01:07, Mario Stadium](https://www.youtube.com/watch?v=3-5aQh-fxZk&t=57s).** Green Toad hits toward Boo at shortstop. Boo possesses the ball, the Throw prompt remains visible, and the released ball has a conspicuous blue trail. Green Paratroopa receives at first for an out. The camera stays with the result, then transitions to Daisy's at-bat. Look for the separation between holding, releasing, travel, and result recognition.
2. **GameCube — [03:10–03:18, Mario Stadium](https://www.youtube.com/watch?v=fdtUfRUjyAY&t=190s).** Luigi's grounder produces a force at second; Donkey Kong visibly gathers and throws toward Waluigi at first. Luigi is safe, and a run scores. Look for the second receiver becoming the next thrower, and for how the small white ball reads against the dirt. This is a received-ball transfer and a scoring play, so its overall duration is not a counterpart to the Wii routine out.

Sources are the original uploaders' recordings: Typhlosion4President (2016) and Sage & Tsuko Play (2015). Their titles, publication dates, and the latter's five-inning Exhibition description are recorded in the dataset. The inspected play segments show Mario Stadium; they do not establish every game setting.

## Timing observations, not tuning values

For the Wii play, the conservative event brackets are:

- Contact: **58.23–58.50 s** into the recording.
- Possession: **60.03–60.54 s**. The sampled ball is approaching before this interval and the possession prompt is visible afterward.
- Visible release: **61.09–61.20 s**.
- Reception/result recognition: **62.19–62.30 s**. This visual bracket does not resolve the hidden engine catch event separately from the emerging OUT signal.
- Next ready view: **65.53–66.04 s**. This names the visible pitching setup with its prompts, not a verified input-unlock frame.

Subtracting endpoint intervals gives **0.99–1.21 s of release-to-reception**, **3.23–3.85 s from reception/result to the next ready view**, and **7.03–7.81 s from contact to that view**. The first interval is a ball-travel observation; the second includes the result view and transition. Neither is a population range. Holding time includes player response and cannot be relabeled authored windup.

The GameCube return throw has a clear held-ball observation at **193.19 s**, a clearly separated ball by **193.60 s**, and an approaching ball at **193.99 s**. The SAFE signal begins by **194.20 s** and the receiving pose is visible by **194.30 s**. A safe signal is **not** a catch timestamp: a runner can be safe before the ball arrives. Keep reception uncertainty separate. The broad release/reception brackets in the dataset are sufficient to locate the action for review, not to calibrate a speed curve or rank the games.

All seconds above are **recording time**. Browser playback rate was 1, but the original recording's hardware, game revision, speed configuration, and mods are unknown. These clips therefore remain outside the verified stock timing-calibration set required by the foundation. We do not silently relax that exclusion rule to acquire a number.

## Accepted provisional visual lead — F693-01

**Accepted by Jack, September 14, 2026:** Wii Super Sluggers leads the way a baseball action reads on screen, with GameCube retained as a mechanics cross-check.

**Rationale for the accepted direction.** This is a design judgment based on Jack's stated priorities, not a measured finding that Wii is universally slower or better. The Wii example offers a concrete target for following the ball and registering the result from the couch. The GameCube example is useful for preserving the tactical distinction between a pickup, a feed, and a received-ball transfer. Existing controls and D1–D18 remain the starting point.

The alternative is to make GameCube the visual lead and use its inspected throw/result treatment as the starting reference. An explicit hybrid remains possible, but each borrowed relationship must name its source; averaging unrelated timings would leave no coherent reference to test.

The accepted **visual lead** sets the default reference for readability reviews. It approves no Nintendo dimensions, release seconds, throw curves, runner speeds, or blanket cross-game hybrid. Those parts of F693-01 and F693-02 still require the broader measurement packet and subsequent human decisions. Jack subsequently accepted under F693-02 that both infield and outfield dimensions may change independently. Exact dimensions and a coherent race budget are the next geometry review; flexibility does not require a shrink.

The Wii recording's black transition is descriptive evidence, not a proposal to add one. The project's existing restriction on full-screen interruptions remains in force; an approved result beat must use the project's presentation rules.

## What the proportions evidence does and does not say

The Wii sample displays wide gameplay content. The GameCube upload contains a narrower game image inside a wider frame. The browser viewport and media quality also changed during inspection. Compare proportions within the active game image; do not divide a character by the entire screenshot, black bars included.

The two throwers differ in body, pose, position, and camera phase. Pixel size here cannot establish world-space body/basepath or fence/basepath ratios. No ratio is selected from these screenshots. The existing GameCube stadium survey remains **community-reported evidence for a shallower outfield**, with basepath shrink still unestablished; see [foundation §3](research-game-feel-693.md#3-geometry-and-proportions).

## Reproduction and coverage

Open each timestamped source, verify the title and play, set normal playback, and use the visible seek/frame-step controls. Read the media timestamp, allow the new image to settle, then identify the event. Do not record a loading spinner, old frame, or recommendation panel as the sought game state. Keep the last clearly-before and first clearly-after observation and round bounds outward. For duration A→B use `[B.low − A.high, B.high − A.low]`.

The Wii frame-step increment observed was approximately 1/30 s. GameCube increments varied between approximately 1/60 and 1/30 s during inspection. Adaptive delivery makes a keypress count unsuitable as an engine-frame index. The dataset stores actual inspected media times and makes no claim of unique simulation cadence. Hidden controller commands remain unknown. No remote media was downloaded; durable evidence consists of source locators and annotations, not an unlicensed video copy.

Current coverage is **one annotated Wii direct out and one annotated GameCube force/return attempt**, plus source-screening observations. It does not meet the proposed minimum sample in [foundation §7](research-game-feel-693.md#7-measurement-protocol). No percentile, average game pace, routine-out probability, or universal throw duration is estimated.

Before numerical reference selection, #701 still needs documented stock configurations; comparable ordinary SS/2B grounders; short/long and pickup/received-ball throw strata; running with hands/dash; liner/fly/wall/relay/double-play/sac-fly coverage; routine versus exceptional reset intervals; and ground-plane/projection measurements. #702 must make the corresponding Harbor events measurable without changing their values. These are existing work items, not reasons to ask Jack to approve research again.

## Validation and acceptance

Validate JSON parsing, unique source/play/event ids, ordered event bounds, source references, and duration arithmetic. The evidence remains `exploratory-observations-not-calibration`, with an empty accepted-target list. Runtime tests are not rerun for this documentation-only packet; the foundation records the existing 1,030-test baseline. No standalone build, gameplay gate, or look gate is passed here.
