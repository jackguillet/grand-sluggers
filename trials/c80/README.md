# `c80` — the compact profile

The trial overlay the 3c series is authored into ([#715](https://github.com/jackguillet/grand-sluggers/issues/715)).
Mechanism: [#716](https://github.com/jackguillet/grand-sluggers/issues/716).

C80 is the candidate contract behind an 80-foot basepath: a smaller diamond, a heavier ball, and
the reads, clocks and throws that were derived to match it. Roughly a hundred accepted anchors,
none of which is a shipping default. Nothing here flips the game; **3e** does that, and only after
**3d** has run the profile through the #702 instrumentation and reported what it did to a race.

## Running it

```
GRAND_SLUGGERS_TRIAL=trials/c80 dotnet run --project src/GrandSluggers.Cli -- match --seed 7
```

The same binary, the same seed, one variable — so the control and the trial are two runs to diff
rather than two tables inside one run. Every `cli` run prints the root and the overlay it read on
stderr, which is how a trace is attributed after the fact.

Unset, this folder does nothing at all. A checked-in trial must not touch a run that did not ask
for it.

## What may live here

Files that **override** `data/`, at the same relative path and nothing else:

```
trials/c80/rules/infield.json     overrides data/rules/infield.json
trials/c80/parks/harbor-diamond.json   overrides data/parks/harbor-diamond.json
```

Anything not carried here is read from `data/`, so the trial and the control cannot drift apart on
a file the trial never meant to own. A character edited next month changes both runs, because both
runs read the same character.

Three rules. Each is checked when the overlay is read, not left to care:

- **Whole files, never fields.** A trial's copy must carry every field the shipped file carries; a
  partial one is refused by name. A field it did not name would fall back to the C# default, making
  a table that is half this profile and half whatever the code says — with a provenance line
  claiming the whole table.
- **Override, never add.** A file `data/` does not have stops the run, **including a name that
  differs only in case** — `rules/Flight.json` is a stray file, not an override, and the refusal
  tells you the spelling `data/` uses. A trial that quietly runs the control is worse than one that
  fails.
- **This README is the only exception**, because a trial that cannot explain itself is not evidence
  either.

## What is here now

[#717](https://github.com/jackguillet/grand-sluggers/issues/717) — 3c-1, the compact field and the
ball that fits it. Eight files, one commit, because **drag is global and park dimensions are not**:
at drag 0.0040 the best swing in the game carries 304 ft, so drag alone against the shipped 330-ft
poles is a game with no home runs in it, and the parks alone are a derby.

| File | What it changes |
| --- | --- |
| `rules/infield.json` | 80-ft basepaths. One file, because #711 made the infield global and every park shares it. |
| `rules/flight.json` | `drag` 0.0019 → 0.0040, and nothing else in the table. |
| `parks/*.json` (six) | fences at one scale, 0.70. Wall heights, wind and hazard radii unchanged. |

**The infield.** `baselineFt` 80, `moundFt` 53.78, and the bags at 56.57 / 113.14 — the shipped
rounded 63.64 / 127.28 multiplied by 80/90, kept at the two decimals `data/` already spells them in.

**The parks.** One scale for all six is what preserves each park's identity and their order: Ember
Keep stays the biggest, Canopy Yard the smallest. Re-deriving each from Harbor's ratios would
flatten all six into the same stadium. Harbor keeps its accepted 232 / 280 / 232; a flat 0.70 gives
231 at the poles, and that foot is rounding.

**Wall heights do not scale.** Bodies did not shrink. An 8-ft wall stays 8 ft and Crystal Rink and
Funfair Park simply stay the friendlier parks they already are.

**Hazards migrate by the zone they sit in** — the basepath scale inside the infield lip
(`flight.classes.infieldLipFt`, radial from home, the same predicate the fielders use), the fence
scale beyond it. **Radii are not scaled**: a barrel is a physical object, and it did not shrink for
the same reason a wall did not. Harbor has no hazards, so the reference park is untouched either
way.

**The runner clock is not retuned here.** Elapsed pace is the C80 anchor, so a Run-5 bag stays
2.95 s and linear speed falls from 30.51 to 27.12 ft/s on the shorter path. `running.json` is not
carried.

**What this slice deliberately leaves to the chain.** `Diamond.Positions` still stands the nine
fielders at their 90-ft spots — they are C# literals, not data, so no overlay can move them — and
that is what keeps grounder arrival times at infielders unchanged, which #717 has to hold.
Funfair's night chompers are `ParkHazards.FunfairChompers` in code, so they did not migrate with the
park's data hazards. Both belong to the fielding chain (#718 on) and 3d should watch them.
