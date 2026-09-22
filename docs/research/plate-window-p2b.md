# P2-b — one shared timing window, behind a trial switch

Child [#844](https://github.com/jackguillet/grand-sluggers/issues/844) of the Phase 2 tracker
[#803](https://github.com/jackguillet/grand-sluggers/issues/803). Decisions: **PH-10-R1** (one
shared window; trial start value 9 frames, `trial-accepted`), **PH-11-R1** (a charge trades
placement forgiveness, never timing), **PH-15-R7** (Contact is spatial forgiveness only),
**PH-17** (one fixed challenge), **PH-20-R1** (numbers live in a trial until Jack accepts them).

Everything here is measured from the sim on the revision this report ships with. Nothing here is
accepted. Sitting 2 is Jack's, and it has not happened.

## The switch

`batting.window.shared`, one bool in `batting.json`.

**Off** — the shipped root — `AtBatResolver.ContactWindowFrames` is the formula that shipped:

```
frames = (charged ? chargeFrames 7 : slapFrames 9) + (Contact - 5) x framesPerContact 0.4
       x the Star Pitch's batterWindowMul
       x the park's nightContactWindowMul
       x the difficulty rung's humanWindowMul   (a pad's swing only)
       floored at floorFrames 5
```

**On** — `trials/pitch5/rules/batting.json`, which is the whole shipped file with this one key
changed:

```
frames = window.frames 9
       x the Star Pitch's batterWindowMul       (PH-16-R18 removes this later, not here)
       x the park's nightContactWindowMul       (not selected; stays)
       floored at floorFrames 5
```

Two things are gone and one is deliberately kept out.

- **The hitter is gone.** `framesPerContact` does not enter. Contact keeps the barrel it scales in
  §5.2 and buys no timing (PH-15-R7).
- **The swing is gone.** `chargeFrames` does not enter. A charge keeps the narrower cursor
  (`cursor.chargeMul` 0.8) and costs no timing (PH-11-R1).
- **The difficulty rung is kept out.** `humanWindowMul` is not applied. One fixed challenge is
  PH-17, and a rung that still widened a pad's window would be the per-seat mechanical assistance
  that decision refuses. The argument is still taken and still threaded from `Match`, because the
  switch is decided inside the formula rather than by rewiring the seat.

The shipped file authors `frames: 9.0` too, inert while the switch is off, so the trial's start
value is written down once — beside the numbers it replaces — rather than only inside a trial. 9 is
today's quick swing for an average hitter, which is why PH-10-R1 chose it as the place to start.

## The window, before and after

Frames at 60 Hz, total width; inside is ± half. Computed from the sim, both roots, Harbor in
daylight, no Star Pitch, by `AtBatResolver.ContactWindowFrames`.

| swing | Contact | EASY before | NORMAL before | HARD before | EASY after | NORMAL after | HARD after |
| --- | --- | --- | --- | --- | --- | --- | --- |
| quick | 2 | 10.14 | 7.8 | 7.02 | **9** | **9** | **9** |
| quick | 5 | 11.7 | 9 | 8.1 | **9** | **9** | **9** |
| quick | 10 | 14.3 | 11 | 9.9 | **9** | **9** | **9** |
| charged | 2 | 7.54 | 5.8 | 5.22 | **9** | **9** | **9** |
| charged | 5 | 9.1 | 7 | 6.3 | **9** | **9** | **9** |
| charged | 10 | 11.7 | 9 | 8.1 | **9** | **9** | **9** |

The multipliers that stay, on the same grid:

| row | before | after |
| --- | --- | --- |
| charmball, Contact 5, quick, NORMAL | 6.75 | 6.75 |
| charmball, Contact 5, charged, NORMAL | 5.25 | 6.75 |
| crystal rink at night, Contact 5, quick, NORMAL | 7.65 | 7.65 |
| crystal rink at night, Contact 5, charged, NORMAL | 5.95 | 7.65 |
| charmball at the rink at night, Contact 1, charged, HARD | 5 (the floor) | 5.7375 |

**How many windows there are.** Over the whole shipped roster — 25 hitters × quick and charged ×
the ordinary bat and all eight bat items × EASY / NORMAL / HARD — the shipped root produces **45
distinct windows, from 5 to 14.3 frames**. Under the trial it produces **one: 9**. That is the
change in a sentence.

**The floor.** `floorFrames` is still the last step on both paths, and the validator now refuses a
table whose `frames` sits under its own floor. On 9 frames the floor never actually bites: the
worst pair of multipliers in the catalog is the charmball (0.75) at the crystal rink at night
(0.85), which is 5.7375. Reported, not designed — if sitting 2 moves the window down, the floor
starts doing work and that is a thing to notice rather than a thing already decided.

## S-29 under `trials/pitch5`, before and after

Fifty three-inning CPU-vs-CPU games, the five captain pairs both ways, seeds 1–5
(`GRAND_SLUGGERS_TRIAL=trials/pitch5 cli match --cohort s29`). The accepted guardrail (F693-06) is
**1.8 – 5 mean runs per side**, applied separately to home and away — **and it is the shipped
root's**. There is no accepted trial band and this report proposes none. **Re-reported, never
tuned.**

"Before" is the overlay as #824 left it: the three pitch families and `cpu.humanInputs`, with the
shipped split window. "After" adds `window.shared`. The before column reproduces #824's published
pair exactly (1.56 / 2.08), which is how this report knows it is measuring the same cohort.

| | before (`trials/pitch5` at #824) | after (+ `window.shared`) |
| --- | --- | --- |
| mean runs, **home** | 1.56 | **2.02** |
| mean runs, **away** | 2.08 | **2.32** |
| most runs in a game | 9 | 9 |
| **strikeouts / game** | **3.70** | **3.70** |
| **walks / game** | **0.98** | **0.76** |
| **balls in play / game** | **20.64** | **22.74** |
| swings and misses / game | 12.02 | 10.70 |
| called strikes / game | 10.36 | 11.72 |
| singles / game | 4.72 | 5.00 |
| doubles / game | 1.28 | 1.76 |
| triples / game | 0.24 | 0.32 |
| home runs / game | 1.46 | 1.76 |

("Balls in play" is the four hit kinds plus the two batted outs the log names, `GroundOut` and
`FlyOut`. Per game means per game across all 50, not per side.)

**The bat makes contact more often, and that is the whole of it.** Swings and misses fall by 66
over the cohort (12.02 → 10.70 per game) and balls in play rise by 105 (20.64 → 22.74). Under the
shipped split window the CPU's charged swings were judged in 7 frames and its weaker bats in less;
under the switch every one of them gets 9, so a swing that used to be a frame late now meets the
ball. Runs follow: home 1.56 → 2.02, away 2.08 → 2.32.

**Strikeouts did not move at all** — 185 in both columns, to the whole number — and that is worth
saying out loud because it looks like a bug and is not. The swinging strikes that went away were
mostly not third strikes: called strikes rose by 68 (10.36 → 11.72 per game) as the same at-bats
took a different path to the same count, and the two effects land on the same strikeout total. It
is a coincidence of this cohort, not a rule, and nothing was tuned to produce it.

**Walks fell** (0.98 → 0.76 per game). With a wider window the CPU batter puts more borderline
pitches in play instead of watching ball four go by. #824 reported this cohort's walks already
collapsed from 2.86 to 0.98 when the CPU lost its vertical aim; the window takes a little more.
That remains a finding on the trial side for the sittings, not a shipped regression.

**The home mean came back over 1.8.** #824's trial column had home at 1.56, under the accepted
shipped floor, and reported it as a consequence of the human-input CPU rather than something to
fix. With the window switch on, the trial's home mean is 2.02 and both sides sit inside the range
the shipped band uses. **That is an observation, not a claim that anything is fixed**: there is no
accepted trial band, two changes are now stacked in one overlay, and no number was moved to put it
there.

## What moved, and what did not

- **The shipped root did not move.** `cli match --seed 7` is byte-identical before and after
  (empty `diff`), and the #708 and pitch-family evidence files differ only in source-hash lines.
  The shipped S-29 cohort run after the change reproduces #824's published shipped column exactly —
  1.90 home / 1.92 away, 3.74 singles, 1.44 doubles, 1.28 HR, 3.18 K, 2.86 BB per game.
- **The trial root moved, on purpose, from the third batter of seed 7.** The first two plate
  appearances are identical; Hex's double becomes a single, and the two logs never resynchronise
  after it. The game still runs to a Final (Ember Court 0–3 before, 4–3 after).
- **The spatial half of the plate is untouched on both roots.** `cursor.chargeMul` and
  `cursor.scalePerContact` are the same numbers in both files, so a charge still narrows the oval
  and a better-Contact hitter still carries a wider one. That is the whole of what PH-11-R1 and
  PH-15-R7 leave a charge and a trait to buy, and S-125 holds it.
- **The CPU batter's timing σ did not move and is not the window.** `(11 − Contact) ×
  errorFramesPerBatStat × timingSigmaMul` is how far off the ball a CPU bat *arrives*; the window
  is how far off the ball a swing may be and still meet it. A poor-Contact CPU hitter still misses
  more because its bat arrives further away, not because it was given a narrower window. Whether
  that σ should read Contact at all is PH-18's batting half and belongs to P2-g.
- **A cross-root read was found and reported, not repaired.** The cohort above has to be run by the
  CLI with `GRAND_SLUGGERS_TRIAL` set, and cannot be run from a test process that loads the overlay
  in memory, because `Match.AutoPlay`'s in-zone read — `AtBatResolver.PitchInZone` →
  `StrikeZoneGeometry.Contains` → `PitchFlight.Point` — takes no rules table and resolves the pitch
  family against the **process-wide** one instead of the match's. With the variable set the
  process-wide table *is* the overlay, so every figure above is correct; without it an
  overlay-only family stops the game by name. Nothing in the shipped game has ever hit this,
  because on the shipped root the two tables are the same object. It is a rail for whoever owns
  `Match.cs` and `PitchFlight.cs` — files #844 may not touch — so S-127 pins the read instead, and
  runs the real in-process cohort the day it is threaded. Appended to
  `data/agent/debug-protocol.json` as `trial-cohort-reads-the-process-wide-table`.
- **`humanWindowMul` is still read by the shipped path and still printed on the title.** The rung
  is not deleted; it stops reaching the window when the switch is on. The CPU-side rung
  multipliers (`timingSigmaMul`, `mistrackMul`, the reaction and margin numbers) are untouched on
  both roots.

## What is not claimed

- **No human acceptance.** Sitting 2 has not happened. This child may merge because the shipped
  root is unchanged and the overlay is inert unless named — not because anything passed.
- **No claim that 9 is right.** It is a start value (PH-10-R1, `trial-accepted`) chosen because it
  equals today's quick swing. Whether the game wants 9, 8 or 7 is exactly what the sitting is for.
- **No trial band.** The S-29 figures above are an observation of a cohort, not a target. Nothing
  was tuned to move them and no test asserts them.
- **Nothing about the Star Pitch multipliers.** `charmball`, `skullball` and `fogball` still narrow
  the window on both roots. PH-16-R18 removes them; that is Phase 6, not here.
- **Nothing about `floorFrames`, `squareFraction` or `leadSec`.** Those values were left alone
  deliberately; PH-10-R1 lists the floor as "unchanged until reviewed".
- **Nothing about how it feels.** No frame of the game was looked at. The window is a number in a
  formula until a pad is holding it.

## What a sitting should check

1. A quick swing and a charged swing feel like the **same** timing. Charge should feel like it
   costs *placement*, not clock.
2. A low-Contact hitter and a high-Contact hitter **time the same** and differ only in the oval:
   the weaker bat should miss by being off the barrel, not by being late.
3. EASY, NORMAL and HARD **do not change the window**. The rung should be visible in the CPU, not
   in how long the player has.
4. Whether 9 frames (±4.5, 150 ms) is the right amount of rope for the most-used swing in the game.
