# P2-c — ordinary swings ignore the stick, behind a trial switch

Child [#855](https://github.com/jackguillet/grand-sluggers/issues/855) of the Phase 2 tracker
[#803](https://github.com/jackguillet/grand-sluggers/issues/803). Decisions: **PH-12** (option C,
direction-accepted September 20, 2026: remove the extra directional input that shapes ordinary hits;
timing, contact position, pitch shape and swing type decide the flight) and **PH-18** (the CPU batter
stays inside human limits: no human shapes an ordinary swing with the stick, so the CPU does not
either).

Everything here was measured from the sim on this child's branch, with `main` at `e14bc513` as the
"before". Nothing here is accepted. Sitting 2 is Jack's, and it has not happened.

## The switch

`batting.geometryOnly`, one bool at the top of `batting.json`. `false` in `data/` and in the code
default; `true` in `trials/pitch5/rules/batting.json`, which changes nothing else.
`AtBatResolver.StickShapesContact(bunt, starSwing, rules)` is the one question every reader asks.

- **Off, the shipped root.** Stick L/R at contact adds `spray.stickDeg` (12°) to the direction, and
  stick U/D takes `launch.stickDeg` (12°) off the launch per unit, on every swing. The CPU batter
  draws `Gauss() × cpu.spraySigmaDeg` (12°) and `Gauss() × cpu.launchAimSigma` (0.45) for every
  ordinary swing. This is bit for bit what shipped.
- **On, `trials/pitch5`.** A swing that is **neither a bunt nor a Star Swing** reads both aims as 0.
  Nothing else in the resolver moves: the timing pull and push, the zone spread, the pitch-height
  term, the Power and charge loft, the launch noise, the out-of-zone spread and the sour foul pull are
  all as they were. The resolver's rng draws do not move either, because the two stick terms never
  drew. The CPU batter passes 0 / 0 on an ordinary swing and **does not draw** the two Gaussians.
- **What the switch does not touch.** A bunt keeps its stick direction until P4-b (PH-14-R5); its
  launch is its own band and never read the stick's U/D on either root. A Star Swing keeps both stick
  terms until Phase 6. The CPU's Star Swing and sac bunt draw exactly as today. The stick still walks
  the box and Down still recenters in SET (PH-09). `SwingInputIntent` still captures the stick; only
  the resolver ignores it.

## What the switch removes: the invariance table

One pitch at the zone center, on time (error 0 frames), box centered, seed 7, Rio (Power from the
roster), a Pitch-5 arm, Harbor, the ordinary bat. The crossing X for each contact zone is found by
asking the resolver along the barrel. Exit mph / launch ° / spray °, computed by `AtBatResolver`
through the real `SwingInputIntent.Capture`, both roots in one process. The swing is on time so the
overlay's other key (`window.shared`) cannot enter: with the stick centered the two roots are the
same ball.

| swing | contact | stick | shipped exit / launch / spray | trial exit / launch / spray |
| --- | --- | --- | --- | --- |
| quick | Perfect | center | 86.9 / 16.4 / 3.0 | 86.9 / 16.4 / 3.0 |
| quick | Perfect | up | 86.9 / 4.4 / 3.0 | 86.9 / 16.4 / 3.0 |
| quick | Perfect | down | 86.9 / 28.4 / 3.0 | 86.9 / 16.4 / 3.0 |
| quick | Perfect | left | 86.9 / 16.4 / -9.0 | 86.9 / 16.4 / 3.0 |
| quick | Perfect | right | 86.9 / 16.4 / 15.0 | 86.9 / 16.4 / 3.0 |
| quick | Nice | center | 82.6 / 16.4 / 6.7 | 82.6 / 16.4 / 6.7 |
| quick | Nice | up | 82.6 / 4.4 / 6.7 | 82.6 / 16.4 / 6.7 |
| quick | Nice | down | 82.6 / 28.4 / 6.7 | 82.6 / 16.4 / 6.7 |
| quick | Nice | left | 82.6 / 16.4 / -5.3 | 82.6 / 16.4 / 6.7 |
| quick | Nice | right | 82.6 / 16.4 / 18.7 | 82.6 / 16.4 / 6.7 |
| quick | Sour | center | 65.2 / 10.8 / 8.4 | 65.2 / 10.8 / 8.4 |
| quick | Sour | up | 65.2 / 10.8 / 8.4 | 65.2 / 10.8 / 8.4 |
| quick | Sour | down | 65.2 / 10.8 / 8.4 | 65.2 / 10.8 / 8.4 |
| quick | Sour | left | 65.2 / 10.8 / -3.6 | 65.2 / 10.8 / 8.4 |
| quick | Sour | right | 65.2 / 10.8 / 56.1 | 65.2 / 10.8 / 8.4 |
| charged | Perfect | center | 108.6 / 18.9 / 3.0 | 108.6 / 18.9 / 3.0 |
| charged | Perfect | up | 108.6 / 6.9 / 3.0 | 108.6 / 18.9 / 3.0 |
| charged | Perfect | down | 108.6 / 30.9 / 3.0 | 108.6 / 18.9 / 3.0 |
| charged | Perfect | left | 108.6 / 18.9 / -9.0 | 108.6 / 18.9 / 3.0 |
| charged | Perfect | right | 108.6 / 18.9 / 15.0 | 108.6 / 18.9 / 3.0 |
| charged | Nice | center | 97.3 / 18.9 / 6.7 | 97.3 / 18.9 / 6.7 |
| charged | Nice | up | 97.3 / 6.9 / 6.7 | 97.3 / 18.9 / 6.7 |
| charged | Nice | down | 97.3 / 30.9 / 6.7 | 97.3 / 18.9 / 6.7 |
| charged | Nice | left | 97.3 / 18.9 / -5.3 | 97.3 / 18.9 / 6.7 |
| charged | Nice | right | 97.3 / 18.9 / 18.7 | 97.3 / 18.9 / 6.7 |
| charged | Sour | center | 82.6 / 10.8 / 8.4 | 82.6 / 10.8 / 8.4 |
| charged | Sour | up | 82.6 / 10.8 / 8.4 | 82.6 / 10.8 / 8.4 |
| charged | Sour | down | 82.6 / 10.8 / 8.4 | 82.6 / 10.8 / 8.4 |
| charged | Sour | left | 82.6 / 10.8 / -3.6 | 82.6 / 10.8 / 8.4 |
| charged | Sour | right | 82.6 / 10.8 / 56.1 | 82.6 / 10.8 / 8.4 |

How to read it:

- On the shipped root, stick up or down moves a Perfect or Nice launch by exactly 12°, and stick left
  or right moves the spray by exactly 12°. Under the trial every column is the stick-center row.
- The exit never moved on either root. The stick never touched the exit.
- A **sour** ball never read the stick's U/D on either root: sour contact is forced into the topper
  or pop band. On the shipped root the sour row still read L/R, and "right" at 56.1° is the cheap
  sour pull throwing an already-pulled ball past the chalk. Under the trial that ball stays at 8.4°.
- The table is a sample. S-128 asserts the whole grid: nine sticks (center, four sides, four
  diagonals) × both batting hands × quick and charged × Perfect / Nice / Sour × errors −2 / 0 / +2
  frames × three seeds. That is 972 resolutions with the `AtBatResult` equal field for field, and
  no stored double.

## S-29 under `trials/pitch5`, before and after

Fifty three-inning CPU-vs-CPU games, five captain pairs both ways, seeds 1–5
(`GRAND_SLUGGERS_TRIAL=trials/pitch5 cli match --cohort s29`). The accepted guardrail (F693-06,
1.8–5 mean runs per side) is the **shipped root's**. There is no trial band and this report proposes
none. **Re-reported, never tuned.**

"Before" is `main` at `e14bc513`: the P2-b overlay (three families, `cpu.humanInputs`,
`window.shared`). Rerun here, it reproduces P2-b's published pair exactly (2.02 / 2.32). "After" adds
`geometryOnly`.

| per game (all 50) | before | after |
| --- | --- | --- |
| mean runs, **home** | 2.02 | **2.30** |
| mean runs, **away** | 2.32 | **2.12** |
| most runs in a game | 9 | 9 |
| singles | 5.00 | 5.18 |
| doubles | 1.76 | 1.96 |
| triples | 0.32 | 0.54 |
| home runs | 1.76 | 1.50 |
| ground outs | 5.00 | 4.12 |
| fly outs | 8.90 | 9.56 |
| balls in play (the six kinds above) | 22.74 | 22.86 |
| **fouls** | **3.70** | **2.20** |
| strikeouts | 3.70 | 3.36 |
| walks | 0.76 | 0.78 |
| swings and misses | 10.70 | 11.00 |
| called strikes | 11.72 | 11.10 |

### The batted-ball mix

Every ordinary (not bunt, not Star Swing) CPU swing that made contact over the same 50 games, fair
and foul together. The class is the flight's shape (`AtBatResult.Class`): "grounder" is topper +
grounder + chopper; "fly" is fly + wall + homer. Direction is for fair balls only, by batting hand:
pull is more than 15° toward the pull line, opposite is more than 15° toward the other line, and
center is between them.

| | before | after |
| --- | --- | --- |
| ordinary contact (count) | 1285 | 1203 |
| Perfect / Nice / Sour | 56.4 / 36.4 / 7.2 % | 60.7 / 33.3 / 6.1 % |
| **grounder** | 21.8 % | **15.1 %** |
| **liner** | 43.0 % | **53.5 %** |
| **fly** | 28.4 % | **25.9 %** |
| **pop** | 6.8 % | **5.5 %** |
| **foul** (share of contact) | 17.2 % (221) | **11.0 % (132)** |
| pull / center / opposite (fair) | 30.0 / 37.3 / 32.7 % | 28.9 / 39.5 / 31.6 % |
| non-bunt CPU swings with 0 / 0 aims | 0 of 2010 | 1913 of 1940 (the other 27 are Star Swings) |

What moved, and why:

- **Fouls fell by 40 %** (221 → 132; 3.70 → 2.20 per game). The CPU's 12° spray σ pushed a share of
  balls past the chalk. That source is gone. The fouls that remain come from timing and the zone's
  own spread.
- **The launch distribution narrowed.** The CPU's launch aim (σ 0.45 × 12° ≈ 5.4°) is gone, so fewer
  balls land at either end: grounders fall from 21.8 % to 15.1 % and flies from 28.4 % to 25.9 %, and
  liners rise from 43.0 % to 53.5 %. The mean did not shift, because the aim was centered on 0; its
  spread did. **Grounders, liners, flies, pops and fouls all still happen.** PH-12 asks for that
  ("distinct geometric grounder / liner / fly / foul outcomes"), and it is a finding for sitting 2,
  not something this child tuned.
- **Direction is still timing's.** Pull / center / opposite barely moved (30.0 / 37.3 / 32.7 →
  28.9 / 39.5 / 31.6). The spray aim was noise around the timing pull, not a direction of its own.
- **Runs.** Home went up and away went down (2.02 / 2.32 → 2.30 / 2.12). Both are inside the range
  the shipped band uses. The CPU's stream is reseeded from the first ordinary swing on, so the two
  columns are different games, not paired plays. Do not read the swap between home and away as a
  side effect. It is an observation, not a claim that anything is fixed.

### In-process and CLI agree

The S-127 repair lets the same cohort run from a catalog loaded on the overlay in a process rooted
at the shipped data. That run (`RaceCohort.Run` on `ContentCatalog.Load(new DataRoot(data,
trials/pitch5))`, and the same loop in a scratch probe) gives **2.30 home / 2.12 away**, and every
per-game outcome count above equals the CLI's. The in-process figures and the CLI figures agree to
the game. On the "before" side the in-process run could not exist (that is the bug), so the probe
there ran with `GRAND_SLUGGERS_TRIAL` set, like the CLI, and it reproduces 2.02 / 2.32.

## The shipped root did not move

- `cli match --seed 7` on the shipped root is **byte-identical** to `main` at `e14bc513` (`cmp`, no
  output).
- The trial log for `--seed 7` diverges on the first pitch of the game. Cinder swings without the two
  aim draws, and his single is still a single, but the play after contact reads a different stream
  ("Rocket daze!" and the pickoff that followed are gone). The two logs never line up again. Both
  reach a Final (Ember Court 4–3 before, 5–3 after).
- The #708 evidence (`game-feel-708-derived.json`, `game-feel-708-flight-derived.json`) and the
  pitch-family seal (`pitch-families-p1d.json`) differ **only in source-hash lines**
  (`AtBatResolver.cs`, `Match.cs`, `Rules.cs`, `batting.json`, the flight file's own hash). The
  pitch-family file also picked up a `Models.cs` hash that was already stale on `main`. No measured
  value moved.
- S-132 pins the CPU on the shipped root without a stored double. The test rebuilds today's
  `CpuSwing` draw by draw from a `Random` of the same seed on the running platform, and calls it
  twice, so a draw added or dropped anywhere fails the second call.

## Surfaces that still present the stick as shaping the ball (read-only)

These are correct for the shipped root, which still has the stick. They move when the shipped root
flips, and that is a Presentation child's work after sitting 2. Nothing here was changed.

- **Tutorial objectives.** `TutorialPlateObjectives.cs:56` (`grounder-fair` requires
  `LaunchAim ≥ minMovement01` and a ball on the dirt) and `:58` (`fly-fair` requires the stick down
  and a fly shape). These are lessons **T-B06** and **T-B06-F**. Under the trial the hold is still
  required, but it no longer makes the grounder or the fly, so the lesson would pass or fail by the
  pitch and the timing alone.
- **Lesson rows.** `data/tutorials/lessons.json` T-B06 / T-B06-F (both occurrences). In
  `data/tutorials/mechanics.json`, `batting.06` "launch direction", and `batting.03` "early/late
  direction", whose source is `control:batting/Spray`.
- **Lesson copy.** `HowToPlay.Tutorials.cs:26, 41, 42, 70, 96, 97, 217, 218` ("Hit a grounder" /
  "Lift a fly ball", "Up tops the ball; down lifts it", the hold-up / hold-down hints).
- **T-G04-F "Hit foul, then fair".** `HowToPlay.TutorialRemaining.cs:14-16` and `HowToPlay.cs:279,
  285` tell the player to "aim left and swing early to pull the first ball foul". Under the trial
  the early swing still pulls, but the stick adds nothing.
- **Control rows.** `RoleTables.cs:65` ("Spray — Stick L/R at contact") and `:115` ("Spray — A/D at
  contact").
- **The book.** `docs/how-to-play.md:120` ("Stick L/R at contact — spray"), `:122` ("Stick L/R at
  contact nudges the direction; stick up tops it, stick down lifts it") and `:203` (the grounder /
  fly lessons). `HowToPlay.cs` carries the book pair's control rows through `RoleTables`.
- **Unity.** `AtBatDirector.cs:490` has a code comment, "Stick U/D aims launch here". No Unity tell
  draws the aim. The swing capture (`SwingInputIntent.Capture`) is correct on both roots and stays.
- **The Raylib debug client.** `src/GrandSluggers.Play/Game.cs:330` passes a spray aim. It is not a
  player surface, and the resolver ignores the aim under the trial.

## What is not claimed

- **No human acceptance.** Sitting 2 has not happened. This child may merge because the shipped
  root is unchanged and the overlay is inert unless named.
- **No claim that ordinary hits feel right without the stick.** That is sitting 2's question: swing
  with the stick held each way and centered, and the ball should go where the timing and the contact
  put it; bunts should still steer; grounders, liners, flies and fouls should all still happen.
- **No trial band.** The S-29 figures and the batted-ball mix are observations. Nothing was tuned
  and no test asserts them.
- **Nothing about Star Swings.** They keep both stick terms (S-131). Each is Phase 6's.
- **Nothing about bunt direction.** It stays on the stick (S-130) until P4-b.
- **No retirement of T-B06 / T-B06-F or of the book copy.** Those follow the shipped flip.
- **Nothing about the CPU's timing σ** (P2-g) or the cursor helper (P2-d).
- **`spray.stickDeg` and `launch.stickDeg` stay authored.** Bunts and Star Swings read them. No
  shipped number moved.
- **Not repaired here:** three other in-zone reads still use the process-wide table: `SetTells.InZone`,
  `Training`'s practice pitch, and Unity `AtBatDirector`'s `CpuSwing` call. Each agrees on the shipped
  root and under `GRAND_SLUGGERS_TRIAL`. They belong to their owners and are listed in the
  debug-protocol row.
