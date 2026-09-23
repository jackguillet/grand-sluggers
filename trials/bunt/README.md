# `bunt` — the bunt's own response to contact quality

The trial overlay for P4-b ([#803](https://github.com/jackguillet/grand-sluggers/issues/803)),
PH-14-R1: actual contact quality governs how soft and how controlled a bunt comes off the bat, through a
response of its own. Better contact must not simply inherit the ordinary swing's rule of a harder ball.

## Running it

```
GRAND_SLUGGERS_TRIAL=trials/bunt dotnet run --project src/GrandSluggers.Cli -- match --seed 7
python3 tools/local-player.py --trial trials/bunt        # the window Jack sits
```

Unset, this folder does nothing. Every `cli` run names the root and the overlay it read on stderr.

## What is here

| File | What it changes |
| --- | --- |
| `rules/batting.json` | `bunt.response.byContact` `false` → `true`. Every other field is the shipped file, byte for byte (S-169). |

Whole files, never fields: an edit to `data/rules/batting.json` has to be mirrored here, or the two
runs read different batting tables.

## What the switch does

Shipped, a bunt is the ordinary swing's exit × `bunt.exitMul` (0.42): a Perfect bunt is the hardest one,
and Power makes it harder (a Power-9 hitter's Perfect bunt leaves at about 40 mph, past
`fielding.bunt.hardExitMph` 36). Every sour bunt pops. The spray is the zone's spread plus 28°.

With the switch on (spec §5.8):

| Contact | Exit (`response.exitMph`) | Spread (`response.spreadDeg`, total) | Sour split |
| --- | --- | --- | --- |
| Perfect | 22 mph | 10° | |
| Nice | 28 mph | 20° | |
| Sour | 40 mph | 44° | above the bat's center: a pop; below it: chopped down at 40 mph |

No Power, charge, star, pitch or tired-arm term reaches the bunt's exit. The held side's lean
(`bunt.sideDeg`, 12°) and the out-of-zone spread are the same on both roots. The high-crossing pop
(`popAboveCenterFt`) is the same.

Measured at Harbor, a right-handed Rio held toward first, 300 seeds per row, the ball's first landing:

| | Power 3 | Power 6 | Power 9 |
| --- | --- | --- | --- |
| Shipped Perfect | 30.3 mph, 25.8 ft | 34.9 mph, 31.3 ft | 39.6 mph, 37.2 ft (all too hard) |
| Shipped Nice | 28.8 mph, 24.2 ft | 33.2 mph, 29.2 ft | 37.6 mph, 34.6 ft (all too hard) |
| Shipped Sour (below center) | pop, 17 % foul | pop, 17 % foul | pop, 17 % foul |
| Trial Perfect | 22 mph, 17.0 ft, within ±5° of the lean | same | same |
| Trial Nice | 28 mph, 23.3 ft, within ±10° | same | same |
| Trial Sour (below center) | 40 mph chop, 37.7 ft, 13 % foul | same | same |

## Why these start values

- **22 mph Perfect** dies in front of the plate on the side it was held to: the catcher, the pitcher and the
  crashing corner all reach it, so it is a sacrifice, not a hit.
- **28 mph Nice** is about today's Power-3 bunt: it reaches the crashing corner.
- **40 mph Sour** is over `hardExitMph` (36), so the defense plays the lead force (§7.3): the poor bunt is
  punished by pace instead of always popping.
- **The spreads** keep today's order (Perfect tighter than Nice tighter than Sour) but make the square bunt
  much tighter than today (±5° against about ±23°), so good contact is what makes the side land.

## What to sit

Bunt with a runner on first against the CPU. Put the bat on the ball squarely, then off-center low, then
off-center high. The question is PH-14-R1's: does a squarely met bunt feel dead and placeable, and a poorly
met one punished by a pop or a chop, without a quality meter and without the ordinary swing's power?
