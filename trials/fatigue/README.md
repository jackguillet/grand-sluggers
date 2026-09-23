# `fatigue` — the arm fades a little with every pitch

The trial overlay for P3-c (#803), PH-08 and PH-08-R1: a pitcher's fatigue is a gradual, visible
loss of speed and a smaller loss of steering room, driven by the pitches thrown. It is never a random
miss. Jack ruled on 2026-09-22 that the random tired wobble goes (shipped) and that fatigue is lost
power plus less steering room.

## Running it

```
GRAND_SLUGGERS_TRIAL=trials/fatigue dotnet run --project src/GrandSluggers.Cli -- match --seed 7
python3 tools/local-player.py --trial trials/fatigue        # the window Jack sits
```

Unset, this folder does nothing. Every `cli` run names the root and the overlay it read on stderr.

## What is here

| File | What it changes |
| --- | --- |
| `rules/pitching.json` | `stamina.fadeFrom` `0` → `50`. Every other field is the shipped file (S-149). |

Whole files, never fields: an edit to `data/rules/pitching.json` has to be mirrored here, or the two
runs read different pitching tables.

## What the switch does

Shipped (`fadeFrom` 0), fatigue is a step. Below a pool of 25 (TIRED) the arm loses 6 mph and its
break is × 0.6. Below 0 it loses 10 mph.

With `fadeFrom` 50, the fade runs from 0 at a pool of 50 to 1 at an empty pool (`StaminaRules.Fade`):

| Pool | mph lost | Break × |
| --- | --- | --- |
| 50 and up | 0 | 1.0 |
| 40 | 2 | 0.92 |
| 25 (TIRED) | 5 | 0.8 |
| 10 | 8 | 0.68 |
| 0 and below | 10 | 0.6 |

- The ends are the shipped step's own ends, so no pitch is faster or slower than the shipped game
  can already throw it. D7's pace hold is untouched (S-148).
- The pool is 60 + 6 × Endurance (66 to 120). An arm starts to fade after about 4 to 17 ordinary
  pitches, sooner when it charges and breaks.
- TIRED stays the label on the card and the CPU's swap trigger.
- The switch adds no draw, and CPU-vs-CPU games do not change: seed 7 is byte-identical, although
  its arms spend 56 of its 98 plays below a pool of 50. A headless game does not read pitch speed
  (the CPU batter's timing is drawn in frames), and the CPU pitcher's rubber solve lands its intent
  through any break scale. The shipped step is invisible there for the same reason. Fatigue is felt
  only with a human at the plate or on the mound.

## What to sit

Pitch a long half with one arm and let it tire. Then bring in a reliever and bring the starter back.
Three questions:

1. Can you feel the arm fading before the card says TIRED?
2. Is the lost steering room a fair cost, or does it feel like the stick stopped working?
3. Is 50 the right place for the fade to start?

Not in this trial:
- The card and sweat still switch on at 25; a gradual tell is a Presentation child.
- The batter's extra exit speed against a tired arm (`batting.exit.tiredPitcherMul`) is still the
  step at 25.
