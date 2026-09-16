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

Nothing yet — the mechanism landed first, on purpose, because every later slice is authored into
it. [#717](https://github.com/jackguillet/grand-sluggers/issues/717) puts the first files in:
80-ft basepaths, six migrated parks, drag 0.0040.
