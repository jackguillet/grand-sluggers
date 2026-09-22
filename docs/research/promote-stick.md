# Promote the accepted stick rule to the shipped root (#883)

Parent: #803. After P2-c (#855 / PR #865) and the duel promotion (#860 / PR #871). Session kind:
Gameplay. Decisions: **PH-12** (human-accepted) and **PH-18** (the CPU half).

Jack played window preview `4b47ad4e` on `trials/pitch5` on September 22, 2026. He was asked to sit
the P2-c stick trial, among three other items, and answered **"approve all"**. The register records
that sentence in commit `fcd3bf5f`. PH-20-R1 says only accepted numbers move to `data/rules/`. This
change moves the last key on trial, so it deliberately changes shipped behaviour.

## What moved

### Data

`data/rules/batting.json`: `geometryOnly` false → **true**. The comment beside the key changed with
it. No other key or number moved.
- `spray.stickDeg` (12°) and `launch.stickDeg` (12°) stay authored, because bunts and Star Swings
  read them.
- `cpu.spraySigmaDeg` and `cpu.launchAimSigma` stay authored. The CPU's Star Swing still draws them.

### Code default

`RulesTests.ShippedJsonEqualsTheCodeFallbackFieldForField` holds every shipped JSON value equal to
its code default in `Rules.cs` (the JSON = code parity rail). So `BattingRules.GeometryOnly`
defaults to **true**, as in the #860 precedent. Its doc comment now says the shipped side is on and
false is the off path.
- There is no logic change. No other file under `src/GrandSluggers.Sim` changed.
- A rules file that **omits** `geometryOnly` now falls back to on. The off path is reached only by
  writing `false`.

### `trials/pitch5` retired

After the flip, the overlay's one file (`rules/batting.json`) would equal the shipped file. So the
whole folder is deleted. Nothing is left on trial there.
- The tests that loaded it now read the shipped root, or build the off path.
- `tools/pitch-family-probes` already read the shipped root since #860. It needed no change. Its
  text names `trials/pitch5` only as the place Jack accepted the numbers.
- `docs/local-player.md` shows a sample refusal naming a window on `trials/pitch5`. A line under
  the sample now says the overlay is retired, and that there is nothing left to pass to
  `--trial trials/pitch5`.
- `trials/c80` has no batting overlay, so the compact root inherits the rule.

## S-29 on the shipped root: re-reported, never tuned

The cohort is 50 three-inning CPU-vs-CPU games: seeds 1–5, five captain pairs, both ways
(`cli match --cohort s29`, Release CLI built in each worktree). "Before" is origin/main `bbd982fe`.
The F693-06 band is 1.8–5 per side.

| Shipped root | Runs away / home | Walks / g | Strikeouts / g | Balls in play / g | Home runs / g | Fouls / g |
| --- | --- | --- | --- | --- | --- | --- |
| before | 2.48 / 2.08 | 0.80 | 3.64 | 23.10 | 1.76 | 3.78 |
| **after** | **2.18 / 2.26** | 0.78 | 3.42 | 23.02 | 1.50 | **2.24** |

Both sides are inside the band, and S-29 is green in the full suite.

**The shipped root now plays the accepted trial and nothing else.** Every per-game outcome of the
"after" cohort equals the `trials/pitch5` cohort on `bbd982fe`, game for game. Only the identity
block differs, because it names the data root. `cli match --seed 7` after is byte-identical to
`GRAND_SLUGGERS_TRIAL=trials/pitch5 cli match --seed 7` before (`cmp`, no output).

## S-29 on the c80 root

`GRAND_SLUGGERS_TRIAL=trials/c80 cli match --cohort s29`

| c80 | Runs away / home | Walks / g | Strikeouts / g | Balls in play / g | Home runs / g | Fouls / g |
| --- | --- | --- | --- | --- | --- | --- |
| before | 2.58 / 2.26 | 0.88 | 3.68 | 21.92 | 1.74 | 3.40 |
| **after** | **3.06 / 2.94** | 0.86 | 3.84 | 22.50 | 1.82 | **2.08** |

Both sides are inside the band. c80 runs rise more than shipped runs. The extra runs come from
extra-base hits:
- Doubles rise from 2.36 to 2.88 per game, and triples from 0.52 to 0.82.
- Fly outs fall from 9.00 to 7.82.

More liners fall into the compact outfield's gaps. This is reported, not tuned.

## The batted-ball mix

These are the same 50 games per root, counted by a scratch probe that plays the cohort in process.
Its runs and outcomes equal the CLI's to the game. The count includes every **ordinary** CPU swing
that made contact (not a bunt, not a Star Swing), fair and foul together. The class is the flight's
shape (`AtBatResult.Class`):
- "grounder" is topper + grounder + chopper;
- "fly" is fly + wall + homer.

Direction counts fair balls only, by batting hand:
- pull: more than 15° toward the pull line;
- opposite: more than 15° toward the other line;
- center: between them.

| | shipped before | shipped after | c80 before | c80 after |
| --- | --- | --- | --- | --- |
| ordinary contact (count) | 1307 | 1212 | 1228 | 1186 |
| Perfect / Nice / Sour | 55.9 / 36.9 / 7.2 % | 60.6 / 33.5 / 5.9 % | 56.8 / 35.7 / 7.6 % | 59.7 / 34.1 / 6.2 % |
| **grounder** | 22.5 % | **15.2 %** | 22.6 % | **18.4 %** |
| **liner** | 42.6 % | **53.8 %** | 40.6 % | **53.8 %** |
| **fly** | 28.2 % | **25.7 %** | 29.4 % | **21.1 %** |
| **pop** | 6.7 % | **5.4 %** | 7.3 % | **6.7 %** |
| **foul share of contact** | 17.4 % (227) | **11.1 % (134)** | 16.9 % (208) | **10.6 % (126)** |
| pull / center / opposite (fair) | 29.4 / 37.7 / 32.9 % | 28.9 / 39.5 / 31.5 % | 28.6 / 41.2 / 30.2 % | 28.5 / 39.6 / 31.9 % |
| non-bunt CPU swings with 0 / 0 aims | 0 of 2035 | 1933 of 1961 | 0 of 1998 | 1945 of 1983 |

What moved, and why. These are the same effects P2-c measured under the trial:
- **Fouls fell by about 40 %** (227 → 134 on shipped, 208 → 126 on c80). The CPU's 12° spray σ
  pushed a share of balls past the chalk, and that source is gone. The remaining fouls come from
  timing, the zone's spread and the sour pull.
- **The launch distribution narrowed.** The CPU's launch aim (σ 0.45 × 12°, about 5.4°) is gone.
  Fewer balls land at either end, and liners rise by about 11 points on both roots. Grounders, liners,
  flies, pops and fouls all still happen, which PH-12 asks for.
- **Direction is still timing's.** Pull / center / opposite barely moved. The spray aim was noise
  around the timing pull, not a direction of its own.
- **The 28 non-bunt swings (shipped) and 38 (c80) that still carry aims are Star Swings.** They keep
  both stick terms until Phase 6 (S-131, S-132).
- The two columns are different games, not paired plays. The CPU's stream reseeds from the first
  ordinary swing on.

## `cli match --seed 7`, in outline

| | Before (`bbd982fe`) | After |
| --- | --- | --- |
| Final | Ember Court 4, Spark All-Stars 3 | Ember Court 5, Spark All-Stars 3 |
| Length | three innings | three innings |
| MVP | Ashlord (29) | Rio Sparks (26) |
| Called balls / walks | 19 / 0 | 27 / 1 |
| Strikeouts | 1 | 2 |
| Home runs | 1 | 3 |

The logs diverge on the first contact of the game. Cinder's single is still a single. Without the
two aim draws, the play after contact reads a different stream: "Rocket daze!" and the pickoff that
followed are gone. The logs never line up again. The difference is expected, and neither log is
golden. The after log is byte-identical to the old trial log.

## Scenario rows that moved

On the flip, five tests in the full suite failed and every other test passed (2057 of 2062). The
failures:
- `FoulTests.SprayPastTheFoulLineIsFoulNotInPlay` and `FoulTests.FoulIsAStrikeUnlessTwo`. Both used
  a 60° spray aim as the device that puts the ball foul.
- `AtBatTests.StickUpBiasesAHopper`.
- `TutorialDirectionTests.ThreeIndependentResultsPassAndReplay` for T-B06 and T-B06-F.

In the same commit as the data flip, the rows that asserted the old shipped side moved with it:
S-13, S-126, S-127 and S-128 … S-132. Each now asserts the accepted rule on the shipped root, with a
row note naming the acceptance:

| Row | What it asserted | What it asserts now |
| --- | --- | --- |
| S-13 (two methods) | stick up launches ~12° lower on shipped; the same launch under the trial | the same launch on shipped; ~12° lower on the **off path** |
| S-126 overlay | the overlay is the shipped file with one key changed | `trials/pitch5` is gone; the shipped file authors both keys true; the off path is the shipped file with `geometryOnly` put back |
| S-126 missing `geometryOnly` | absent is off | absent falls back to the code default, on; explicit false is the off path |
| S-127 | the S-29 cohort under the overlay, in process | the S-29 cohort in process from the off-path catalog (built in the test); recorded, no band |
| S-128 … S-131 | the trial ignores the stick; shipped still moves with it | shipped ignores the stick; the off path still moves with it; bunts and Star Swings follow the stick on both |
| S-132 (three methods) | shipped CPU draws both aims in today's order; the trial draws none | shipped draws none on an ordinary swing; the off path draws both in the old order; Star Swing and sac bunt draw on both |
| S-107 … S-113, S-114 … S-120 | read the pitch5 overlay (which already resolved `pitching.json` to shipped) | read the shipped root; the overlay assertions in S-113 and S-120 dropped |
| `FoulTests` (two) | a 60° aim makes a foul on shipped | the same, on the off path |
| `AtBatTests.StickUpBiasesAHopper` | stick up grounds the ball on shipped | on shipped, up and down are the same ball; the hopper is the off path |
| `TutorialDirectionTests` T-B06 / T-B06-F | the taught input passes three times | the lesson machinery runs on the off path; a new row pins that on shipped, the held stick and the centered stick are the same ball and the lesson is not passed |

The off paths are built in the test and never read from shipped data. `SwitchOffPaths.StickShapes`
copies the process's data root once per test process with `batting.geometryOnly` false. Unlike
`SplitWindow`, it keeps the process's overlay, so the compact rows that play a lesson on it keep the
c80 diamond. It refuses an overlay that overrides `batting.json`, because the edit would not be read.
The debug-protocol row `trial-cohort-reads-the-process-wide-table` now names the renamed S-127 method.

## Compact rows (`trials/c80`)

`GRAND_SLUGGERS_TRIAL=trials/c80 dotnet test src/GrandSluggers.Sim.Tests --filter "Rows=compact&Copy!=gap"`

| Revision | Result |
| --- | --- |
| origin/main `bbd982fe` (before) | 846 passed, 0 failed |
| the data flip, the code default and the scenario rows (`fc24cb5a`) | 843 passed, **3 failed** |
| final | 848 passed, 0 failed |

The three compact rows that moved are the compact-tagged rows that also moved on the shipped root,
for the same cause. c80 inherits the shipped `batting.json`.
- `AtBatTests.StickUpBiasesAHopper`: an ordinary swing no longer reads stick up, so the hopper claim
  moved to the off path.
- `TutorialDirectionTests.ThreeIndependentResultsPassAndReplay` (T-B06, T-B06-F): the lesson's
  taught input no longer makes a grounder or a fly.

No compact row moved for a c80-specific reason, and no data was retuned. The final count adds the
two new T-B06 / T-B06-F pin rows, which are compact-tagged.

## Sealed evidence

In order:
1. `dotnet run --project tools/game-feel-flight-probes -- --write`
2. `python3 tools/compact-field-report.py`
3. both `--check`
4. `dotnet run --project tools/pitch-family-probes -- --write` and `--check`

**No measured value moved. Only source hashes moved:**
- `Rules.cs` (the default and its comment) and `batting.json` (the key and its comment);
- the flight-derived file's own hash, which the compact-field report tracks.

The flight probes measure batted-ball flight from fixed launches. The stick aims never entered them.
The pitch-family probe does not read the batting switch.

## What is not claimed

- **No new human judgment.** Jack's acceptance is his sentence about the `trials/pitch5` window. The
  shipped window after merge is the same data on the shipped root, and whether it plays like the
  window he accepted is his to confirm after redelivery.
- **Nothing was looked at in Unity.** No window was built or played. `tools/unity-compile.sh` only
  compiles.
- **No balance claim.** S-29 sits in the band on both roots; the mix and the c80 run rise are
  reported, not judged.
- **The switch's off path is not removed.** That is a later cleanup child.
- **Bunts and Star Swings keep the stick.** Bunt direction is P4-b's (PH-14-R5), and each Star Swing
  is Phase 6's.
- **Stale comments outside scope.** `AtBatResolver.StickShapesContact`'s doc comment (lines ~30–35)
  and the comment in `Match.CpuSwing` (~l.1449) still describe the switch as "off — the shipped
  root" / "on — `trials/pitch5`". This child may change only the default in `Rules.cs`, so they are
  left for the next child that owns those files.

## What the book and lessons now misstate

This is the Presentation child's list. Nothing here was edited.

- **T-B06 "Hit a grounder" and T-B06-F "Lift a fly ball" can no longer be passed by their taught
  input on the shipped root.** `TutorialPlateObjectives.cs` (`grounder-fair` l.60, `fly-fair` l.62)
  still require the stick held up or down *and* a ball on the dirt or in the air. The hold no longer
  shapes the ball. On the lesson's middle fastball the held-stick swing is the same liner as the
  centered one (Perfect, launch 14.5°, class Liner), so the lesson fails with `hit-grounder` /
  `lift-ball`. `TutorialDirectionTests.OnTheShippedRootTheHeldStickNoLongerMakesTheLessonsBall`
  pins this. It moves when the lessons are rewritten or retired.
  - Rows: `data/tutorials/lessons.json` T-B06 / T-B06-F (both occurrences, ~l.443, 471, 3423, 3454).
  - `data/tutorials/mechanics.json`: `batting.06` "launch direction", and `batting.03` "early/late
    direction", whose source is `control:batting/Spray`.
  - Copy: `HowToPlay.Tutorials.cs` l.26 (titles), 41–42 ("Hold up / down at contact…"), 71 ("Up tops
    the ball; down lifts it"), 98–99 (the hold-up / hold-down hints), 220–221 (the `hit-grounder` /
    `lift-ball` feedback).
- **T-G04-F "Hit foul, then fair".** `HowToPlay.TutorialRemaining.cs` l.15 ("Aim left and swing early
  to pull the first ball foul") and `HowToPlay.cs` l.279 / l.285 ("hold stick left and tap South
  early" / "hold A and tap Space early"). The early swing still pulls, and the lesson still passes,
  but holding the stick adds nothing.
- **The "Spray" control rows.** `RoleTables.cs` l.65 ("Spray — Stick L/R at contact") and l.115
  ("Spray — A/D at contact"). The `HowToPlay` pad and keyboard cards read them.
- **The book.** `docs/how-to-play.md`:
  - l.120: "**Stick L/R at contact** — spray.";
  - l.203: the grounder / fly lessons, "hold stick up/W or stick down/S during the pitch".
- **Unity.** `AtBatDirector.cs` (~l.490) has the code comment "Stick U/D aims launch here". No Unity
  tell draws the aim, and `SwingInputIntent.Capture` is correct on both paths.
- **The Raylib debug client.** `src/GrandSluggers.Play/Game.cs` (~l.330) passes a spray aim. It is not
  a player surface, and the resolver ignores the aim on an ordinary swing.
