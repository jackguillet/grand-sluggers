# Promote the accepted duel trial to the shipped root (#860)

Parent: #803. Session kind: Gameplay. Decisions: PH-02-R2, PH-03, PH-04, PH-20-R1 (the family
numbers), PH-18-R1 (the CPU on human inputs), PH-10-R1, PH-11-R1, PH-15-R7, PH-17 (the shared
window). **Not PH-12**: `geometryOnly` (P2-c, #855) was not on the window Jack played and stays a
trial.

Jack played the `trials/pitch5` window `main-d49c527351` (trial revision `d49c5273`) on
September 22, 2026 and wrote **"trial was good."** PH-20-R1 says only accepted numbers move to
`data/rules/`. This change moves them. It is a deliberate change to shipped behaviour.

## What moved

### Data

`data/rules/pitching.json` is now **byte-identical to the old `trials/pitch5/rules/pitching.json`**.
Its SHA-256 is the one the pitch-family evidence recorded for the trial copy (`ce1eae6b…`). Before
the edit, a whole-JSON diff showed the two files differed in exactly these keys and nothing else:

| Key | Before (shipped) | After (accepted) |
| --- | --- | --- |
| `families.curveball` | absent | `mph` 72.5, `chargeMph` 4, `hump` 0.8, `hangUntil` 0.55, `hangRate` 0.42, `dumpRate` 1.72, `dropFt` 0.9, `sweepFt` 0.32, `sweepFrom` 0.42, `breakDamped` false, `staminaCost` 2, `offSpeed` true |
| `families.slider` | absent | `mph` 78, `chargeMph` 5, `hump` 0.15, `hangUntil` 0.8, `hangRate` 0.95, `dumpRate` 1.26, `dropFt` 0.3, `sweepFt` 0.58, `sweepFrom` 0.45, `breakDamped` false, `staminaCost` 2, `offSpeed` false |
| `families.sinker` | absent | `mph` 81, `chargeMph` 6, `hump` 0.3, `hangUntil` 0.75, `hangRate` 0.88, `dumpRate` 1.37, `dropFt` 0.55, `sweepFt` −0.2, `sweepFrom` 0.55, `breakDamped` false, `staminaCost` 1, `offSpeed` false |
| `cpu.humanInputs` | false | **true** |
| `cpu.even.families` (fb / ch / cu / sl / si) | 85 / 15 / 0 / 0 / 0 | 40 / 15 / 12 / 18 / 15 |
| `cpu.even.steerChance` | 0.20 | 0.30 |
| `cpu.ahead.families` | 65 / 35 / 0 / 0 / 0 | 20 / 24 / 26 / 20 / 10 |
| `cpu.ahead.steerChance` | 0.30 | 0.45 |
| `cpu.behind.families` | 95 / 5 / 0 / 0 / 0 | 44 / 6 / 6 / 14 / 30 |
| `cpu.behind.steerChance` | 0.05 | 0.15 |
| `cpu.runnerTwoOuts.families` | 100 / 0 / 0 / 0 / 0 | 38 / 10 / 10 / 20 / 22 |
| `cpu.runnerTwoOuts.steerChance` | 0.10 | 0.25 |

`data/rules/batting.json`: `window.shared` false → **true**. `frames` stays 9. Only the comments
beside the key changed with it.

`trials/pitch5` is now the P2-c stick trial only:
- `rules/pitching.json` is deleted, because it would equal the shipped file.
- `rules/batting.json` differs from the shipped file only in `geometryOnly: true`.
- The README says so.

`trials/c80` has no pitching or batting overlay, so the compact root inherits all of this.

### Code defaults (scope amendment on #860, option A)

`RulesTests.ShippedJsonEqualsTheCodeFallbackFieldForField` holds every shipped JSON value equal to
its code default in `Rules.cs`. This is the JSON = code parity rail. So the defaults follow the
data:
- `CpuPitcherRules.HumanInputs` is true.
- The four count rows' `Families` and `SteerChance` carry the values above.
- `ContactWindowRules.Shared` is true.

The three family rows stay `null` in code: they are `[Optional]`, and the parity test skips nulls.
There is no logic change. Doc comments in `Rules.cs`, `Match.cs` and `AtBatResolver.cs` no longer
say the switches are "on in `trials/pitch5`". The stop message in `PitchFamilyTable.Of` no longer
says the numbers are a trial.

One visible consequence: a rules file that **omits** `window.shared` or `cpu.humanInputs` now
falls back to on (the shipped value), not off. The off path is reached only by writing `false`.
S-126's "missing is off" clause for `shared` was moved accordingly. `geometryOnly` still defaults
to off.

## S-29 on the shipped root: re-reported, never tuned

The cohort is 50 three-inning CPU-vs-CPU games, seeds 1–5, five captain pairs both ways
(`cli match --cohort s29`, Release CLI). Before is origin/main `36d7cc13`; after is this branch.
The F693-06 band is 1.8–5 per side.

| Shipped root | Runs away / home | Walks / g | Strikeouts / g | Balls in play / g | Hits / g | Home runs / g | Called balls / g |
| --- | --- | --- | --- | --- | --- | --- | --- |
| before | 1.92 / 1.90 | 2.86 | 3.18 | 21.12 | 6.70 | 1.28 | 34.68 |
| **after** | **2.32 / 2.02** | **0.76** | 3.70 | 22.74 | 8.84 | 1.76 | 22.78 |

Both sides are inside the band, and S-29 is green in the full suite. The after pair is exactly the
pair P2-b published for `trials/pitch5` before P2-c added `geometryOnly` (2.02 home / 2.32 away,
[`plate-window-p2b.md`](plate-window-p2b.md)). That is the evidence that the shipped root now plays
the accepted trial and nothing else.

## S-29 on the c80 root

`GRAND_SLUGGERS_TRIAL=trials/c80 cli match --cohort s29`

| c80 | Runs away / home | Walks / g | Strikeouts / g | Balls in play / g | Hits / g | Home runs / g | Called balls / g |
| --- | --- | --- | --- | --- | --- | --- | --- |
| before | 2.36 / 2.46 | 3.26 | 3.58 | 21.74 | 7.82 | 1.38 | 38.58 |
| **after** | **2.48 / 2.30** | **0.84** | 3.52 | 22.04 | 8.76 | 1.66 | 22.12 |

For the record, `trials/pitch5` (now the stick trial on the new shipped root) measures 2.12 away /
2.30 home: 0.78 walks, 3.36 strikeouts and 22.86 balls in play per game. That is P2-c's own "after"
pair ([`stick-shaping-p2c.md`](stick-shaping-p2c.md)), unchanged.

## Why walks fell by about 3.7×

Height is the family (PH-03). Under `cpu.humanInputs` the CPU pitcher has no vertical input, so it
cannot miss high or low:
- Its crossing height is the zone center less the family's `dropFt`.
- No ordinary family drops out of the zone on its own (S-108).
- Its scatter is a Gaussian on its own rubber intent, in X only (§4.8).

The old endpoint model aimed a height and scattered in both axes, so many of its misses were high
or low balls. Every miss is now a horizontal miss, and only the *waste* row aims outside on purpose
(S-119: at 0-2 the waste comes from the rubber alone). Called balls fall from 34.7 to 22.8 per game
on the shipped root, and walks from 2.86 to 0.76. More pitches are hittable, so hits (6.70 → 8.84)
and home runs (1.28 → 1.76) rise. That is where the runs come from. Strikeouts rise slightly
(3.18 → 3.70) because fewer counts reach ball four.

The shared window is the smaller half of the change here. It widens a charged swing (7 → 9 frames)
and a low-Contact hitter's window, and narrows a high-Contact hitter's. The CPU batter's timing
error is unchanged (§5.9). P2-b measured that half alone.

This is reported, not tuned. Whether 0.76 walks a game is right is a balance question for a later
child and for Jack; no number moved to change it.

## `cli match --seed 7`, in outline

| | Before (`36d7cc13`) | After |
| --- | --- | --- |
| Final | Ember Court 6, Spark All-Stars 3 | Ember Court 4, Spark All-Stars 3 |
| Length | five innings | three innings |
| MVP | Boom (27) | Ashlord (29) |
| Called balls / walks | 64 / 5 | 19 / 0 |
| Strikeouts | 8 | 1 |
| Home runs | 3 | 1 |

The logs diverge on the first pitch. `CpuPitch` now draws from the human-input model, which spends
a different number of draws from the one seeded stream, so every later event reseeds. The
difference is expected; neither log is golden.

## Scenario rows that moved

Fifteen tests failed on the data flip, and each now asserts its new expectation with a row note that
names the acceptance:

| Row | What it asserted | What it asserts now |
| --- | --- | --- |
| `RulesTests.ShippedJsonEqualsTheCodeFallbackFieldForField` | JSON = code | unchanged; passes because the defaults followed |
| S-10 (two methods) | split window on shipped / one window under trial | one window on shipped; the floored split window on the **off path** |
| S-30 (two methods) | Charge Bat keeps the slap window on shipped | shared window on shipped; slap-window clause on the off path |
| S-113 | shipped stops the three by name; overlay = shipped + rows + CPU | shipped authors all five, CPU on; overlay overrides only `batting.json`; the stop is the code-default table |
| S-114 | shipped CPU is the endpoint model | the off path (`humanInputs` false, built in the test) is the endpoint model; shipped never aims |
| S-116 | the shipped family table only reaches fb / ch | a table with the three rows dropped, built in the test |
| S-120 | switch off shipped; rows are the port of the exclusive mix | switch on shipped; every row weights all five; `chargeChance` is still the mix's charge share; `steerChance` is above the old break share |
| S-124 (two methods) | shipped root = split-window formula; window follows Contact | the same, on the off path |
| S-125 | under the trial | on the shipped root |
| S-126 overlay | two keys changed | one key (`geometryOnly`) changed; overlay list is `batting.json` alone |
| S-126 missing switch | absent `shared` is off | absent `shared` falls back to the code default, on; explicit false is the off path |
| `BalanceTests.TheRungWidensOnlyAPadsSwingWindow` (EASY, HARD) | the rung widens a pad's window | the same, on the off path; new `OnTheShippedRootTheRungLeavesAPadsSwingWindowAlone` pins PH-17 on shipped |
| `AtBatFeelTests.ChargeMaxIsStrongerThanOverchargeAndSlapContactsMore` | slap contacts more at the charge-window edge | the same, on the off path |
| `PitchTests.BreakIsAStickVerbNotAFamily…` | shipped refuses the three | the code-default table refuses them; shipped flies them |
| `PitcherSwapTests.ThePitcherCardLists…` | no card names CU | shipped card is `FB · CH · CU`; the two-row table still skips |

The off paths are built in the test and never read from shipped data. A new helper,
`SwitchOffPaths`, copies the shipped root once per process with `window.shared` false. The CPU and
family rows use the existing `WriteRoot` copy with one key changed. Rows S-107…S-112 and
S-115…S-119 read the overlay, which now resolves `pitching.json` to the shipped file. They hold the
shipped rows to the same roles and needed no change.

## Compact rows (`trials/c80`)

`GRAND_SLUGGERS_TRIAL=trials/c80 dotnet test src/GrandSluggers.Sim.Tests --filter "Rows=compact&Copy!=gap"`
was run three times:

| Revision | Result |
| --- | --- |
| origin/main `36d7cc13` (before) | 811 passed, 0 failed |
| the data flip and the code defaults, old tests | 809 passed, **2 failed** |
| final (tests moved) | 811 passed, 0 failed |

The two rows that moved on c80 are the two compact-tagged rows that also moved on the shipped root,
for the same cause. c80 inherits the shipped `pitching.json` and `batting.json`:
- `PitchTests.BreakIsAStickVerbNotAFamilyAndAnUnauthoredFamilyDoesNotFly`: the curveball, slider
  and sinker now fly on the shipped table, so the "does not fly" half moved to the code-default
  table.
- `AtBatFeelTests.ChargeMaxIsStrongerThanOverchargeAndSlapContactsMore`: the "slap contacts more at
  the charge-window edge" half is a split-window claim, so it moved to the off path.

No compact row moved for a c80-specific reason, and no data was retuned.

## Sealed evidence

In order:
1. `dotnet run --project tools/game-feel-flight-probes -- --write`
2. `python3 tools/compact-field-report.py`
3. both `--check`
4. `dotnet run --project tools/pitch-family-probes -- --write` and `--check`

**No measured value moved.** Only source hashes moved:
- `Rules.cs` (the defaults and comments), `AtBatResolver.cs` and `Match.cs` (comments), and
  `batting.json` (`window.shared` and its comments).
- The flight-derived file's own hash, which the compact-field report tracks.

The flight probes measure batted-ball flight, which neither switch touches. The pitch-family probe
measures the same three rows it measured under the overlay.

The pitch-family probe read `trials/pitch5` and hashed `trials/pitch5/rules/pitching.json`, which is
gone. It now reads the shipped root. This was a tools change only. Its report text says the numbers
are shipped after Jack's acceptance, and its source list drops the trial file.

## What is not claimed

- No new human judgment. Jack's acceptance is his sentence about the `trials/pitch5` window. The
  shipped window after merge is the same data on the shipped root, and whether it plays like the
  window he accepted is his to confirm.
- Nothing was looked at in Unity. No window was built or played. `tools/unity-compile.sh` only
  compiles.
- No balance claim. S-29 sits in the band; the walk rate fell sharply and is reported, not judged.
- The switches' off paths are not removed. That is a later cleanup child.
- `Training.CorePitches` reads the **code-default** table, which still authors two families
  (the three rows have no code default). The `GrandSluggers.Play` debug sandbox therefore still
  practises fastball and changeup only. This is reported, not changed: fixing it needs a Sim change
  outside this scope.

## What the book and lessons now misstate

This is the Presentation child's list. Nothing here was edited:
- **`docs/how-to-play.md` §"swing" (line ~113) and its `HowToPlay` pair.** They say "The title's
  difficulty line says how wide your window is: EASY ×1.3, NORMAL ×1.0, HARD ×0.9." Under PH-17 the
  rung no longer widens a pad's window.
- **`CarnivalFront.TitleSetup`.** The title prints `SWING WINDOW ×1.3 / ×1.0 / ×0.9` from
  `humanWindowMul`, which no longer enters a pad's window.
- **`HowToPlay.cs` pitching card (lines ~219 and ~225).** "Cycle pitch: RB / Tab before the charge
  (FB, CH changeup)". The cycle now walks all three of a pitcher's families: fastball, second,
  third.
- **The book and lessons have no curveball, slider or sinker.** `data/tutorials/mechanics.json` and
  `lessons.json` cover the changeup by the cycle only (T-P03). The three new families and the CPU
  pitcher's human-input behaviour have no lesson or mechanic row.
- **Charge and Contact wording.** Any copy that says a charged swing has a narrower timing window,
  or that Contact widens the window, is now the off path. Under PH-11-R1 and PH-15-R7 both act on the
  oval only.
