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
ball that fits it. #717 landed eight files in one commit — the folder carries eleven now — because
**drag is global and park dimensions are not**:
at drag 0.0040 the best swing in the game carries 304 ft, so drag alone against the shipped 330-ft
poles is a game with no home runs in it, and the parks alone are a derby.

| File | What it changes |
| --- | --- |
| `rules/cpu.json` | the CPU's read of a throw (#722): `relayBiasSec` 0.3 / 0.1 / 0 by rung, `runnerReadsArm` 0.5 / 1 / 1, `runnerReadsRelay` 0 / 1 / 1, `readsChemistry` 0 / 1 / 1. Nothing else in the table; the rung stays normal. |
| `rules/infield.json` | 80-ft basepaths (#717), and the ground that dresses them (#729). One file, because #711 made the infield global and every park shares it. |
| `rules/fielders.json` | where the seven gloves stand (#725): the infield four on the basepath scale, the outfield three on bearing and fence-at-bearing fraction. P and C are not in the file. |
| `rules/fielding.json` | `park.pipeReachPadFt` 8 → 5.6 (#732), and the throw clock (#722): `throw.releaseSec` 0.30, `baseFtPerSec` 88.89, `longThrowLossSec` 0.60, `chem.badSpeedMul` 0.90, `slantChance` 0, and the forced-relay ceiling `throw.onTheFlyFt` set to never. Nothing else in the table. |
| `rules/flight.json` | `drag` 0.0019 → 0.0040 (#717) and `classes.infieldLipFt` 155 → 137.78 (#728). Nothing else in the table. |
| `parks/*.json` (six) | fences at one scale, 0.70 (#717), and every hazard radius on the same scale (#732). Wall heights and wind unchanged. |

**The infield.** `baselineFt` 80, `moundFt` 53.78, and the bags at 56.57 / 113.14 — the shipped
rounded 63.64 / 127.28 multiplied by 80/90, kept at the two decimals `data/` already spells them in.
`innerHalfFt` 44.44 and `backArcFt` 81.78 followed in #729, on the same factor.

**The parks.** One scale for all six is what preserves each park's identity and their order: Ember
Keep stays the biggest, Canopy Yard the smallest. Re-deriving each from Harbor's ratios would
flatten all six into the same stadium. Harbor keeps its accepted 232 / 280 / 232; a flat 0.70 gives
231 at the poles, and that foot is rounding.

**Wall heights do not scale.** Bodies did not shrink. An 8-ft wall stays 8 ft and Crystal Rink and
Funfair Park simply stay the friendlier parks they already are.

**Hazards migrate by the zone they sit in** — the basepath scale inside the infield lip
(`flight.classes.infieldLipFt`, radial from home, the same predicate the fielders use), the fence
scale beyond it. The rule reads the **shipped** 155-ft lip, because it asks where a hazard stood on
the historical field, not where it will stand on the compact one. #728 moved the trial's lip to
137.78 and the positions did not change: no shipped hazard has a radius in [137.78, 155), so
re-deriving against the new lip is byte-identical. **Radii were not scaled here**: a barrel is a
physical object, and it did not shrink for the same reason a wall did not. (#730 decision 4 read the
code and overturned that; #732 scaled them, in its own section below.) Harbor has no hazards, so the
reference park is untouched either way.

**The runner clock is not retuned here.** Elapsed pace is the C80 anchor, so a Run-5 bag stays
2.95 s and linear speed falls from 30.51 to 27.12 ft/s on the shorter path. `running.json` is not
carried.

**What this slice left to other issues, and what it cost.** `Diamond.Positions` stood the nine
fielders at their 90-ft spots — they were C# literals, not data, so no overlay could move them.
That was not a virtue, and it was not only an outfield problem. Both bullets below are closed by
**#725**, and the numbers stay here because the distance between the two columns is what the slice
cost while it was open:

- **The corners lost their bags.** 1B and 3B went from 16.62 ft to **26.41 ft** from the bag they
  cover, a 59% increase, at exactly the spots where the close plays are. 2B and SS barely moved
  (43.01 → 42.28 ft) because they already play deep. **Closed by #725**: 14.77 ft and 38.23 ft, the
  shipped gaps on the basepath scale.
- **The outfield stood outside the park.** LF and RF at (±110, 250) and CF at (0, 305) were further
  out than every migrated fence. `FieldBounds.Clamp` pinned them to the warning track: CF snapped
  from 305 to 272 against a 280-ft wall, and in Canopy Yard it was 40 ft beyond the fence. Nothing
  can land behind an outfielder pinned to the wall, which is why the extra-base line in the trial
  table below collapses. **Closed by #725**: all eighteen of those starts (three bodies × six parks)
  were clamped; none of the eighteen migrated ones is.

Funfair's night chompers are `ParkHazards.FunfairChompers` in code, so they did not migrate with the
park's data hazards. They sit at z 198–228 in a park whose centre fence is now 273 — still inbounds,
and since #725 inside the band a centre fielder starting at (0, 213.50) can work in, which he could
not while he stood pinned to the wall.

**And he now starts inside one.** The centre mouth is at (0, 228) with an 18-ft radius. The migrated
centre fielder starts 14.50 ft from that centre — **3.50 ft inside the rim**. Shipped, he stood
77.00 ft away, 59 ft clear of it.
`ChompFly` is evaluated at the ball's landing point, not at the fielder, so the body is not frozen —
but on a Funfair night a fly landing at the centre fielder's own start is stamped an out by the
hazard before his glove resolves. Measured through `ParkHazards.ChompFly`: the centre line chomps a
night fly from z 210 to z 245, and LF and RF at (∓77.09, 175.19) are clear. No other position and no
park flips. Some of the trial's fly-out movement at Funfair is therefore the chomper rather than the
geometry this slice owns. The chompers are code literals and no overlay can move them (#717's gap);
this is recorded, not repaired.

None of this was fixed in #717 on purpose: absorbing #725 would have merged two slices into one and
destroyed the attribution 3d depends on. It is recorded so a 3d reader does not mistake these
effects for the anchors under test — and so the fix has a before to be measured against.

---

[#728](https://github.com/jackguillet/grand-sluggers/issues/728) — the infield lip, on the basepath
scale. `flight.classes.infieldLipFt` 155 → **137.78** in `rules/flight.json`, and nothing else.
155 × 80/90, at the two decimals the rest of the overlay spells. The shipped table is untouched, so
a no-overlay run is byte-identical by construction.

**What it fixes.** `BattedBall` downgrades a fly landing inside the lip to a pop. #717 left the lip
at 155 ft against a 280-ft centre field, and the pops doubled: **489 → 976** over the shape grid.
They are now **652**. The flies follow: 4103 → 3494 → **3818**. In live play, 12 of 40 `cli match`
seeds change under the trial.

**What it does not fix, on purpose.** 163 extra pops remain. That residual is the compact profile's
two scales, not a miss: the lip follows the basepath at 0.8889 while fair territory follows the
fence at 0.70, so the same radius still covers a larger share of a smaller field — 137.78 / 280 =
0.492 against the shipped 155 / 400 = 0.388. Closing it the rest of the way means a lip on the fence
scale, 108.50 ft, which [#730](https://github.com/jackguillet/grand-sluggers/issues/730) ruled out:
it sits inside the middle infield, so `FieldingResolver.OutfieldGrass` starts calling 2B and SS
outfielders and puts them on the outfield read. 137.78 clears that floor by 12.53 ft at the shipped
starts and by 26.45 ft at the ones #725 authored, so the two issues did not have to be ordered.

**What it costs.** The lip now sits **inside** the drawn dirt. `ParkDiamond.BackR` is a C# constant
no overlay can reach, so the dirt still ends at 145.78 ft under the trial while the lip is at
137.78 — 8.00 ft inside it. Shipped, the lip sits 2.50 ft outside the dirt; #717 widened that to
9.22 ft, and this slice flips the sign. **#729** is the issue that closes it: `BackR` on the
basepath scale is 81.78, which puts the lip back 2.22 ft outside — the shipped 2.50 × 8/9, exactly.

---

[#729](https://github.com/jackguillet/grand-sluggers/issues/729) — the ground dress, on the basepath
scale. `ParkDiamond.InnerHalf` and `ParkDiamond.BackR` were `const float` 50 and 92; they are now
`innerHalfFt` and `backArcFt` in `rules/infield.json`, and the trial carries **44.44** and **81.78**.
The shipped values did not move — 50 and 92 are what the constants were — so a no-overlay run draws
exactly the field it drew before.

**What it repairs.** #717 moved the bags and left the dirt at 90-ft scale. That put the drawn grass
vertex 6.57 ft from the corner bag it points at — **inside** the 12-ft bag pad, by 5.43 ft, so the
lawn was drawn over the dirt the bag sits on. Migrated, the gap is 12.13 ft and the vertex clears the
pad again.

**What it closes for #728.** The dirt's far edge comes in from 145.78 ft to 135.56, which is the
shipped 152.50 through the same factor. That puts the migrated lip back 2.22 ft *outside* the dirt —
the shipped 2.50 × 8/9 — instead of 8.00 ft inside it. The rule and the picture agree again.

**What stays in feet.** Path width, the bag pads, the home pad, the mound table, the warning track
and the foul apron are bodies and equipment, and they stay `const` on `ParkDiamond` — the same rule
that kept the wall heights at 8 ft. The cost is visible at the corners: the vertex clears the pad by
0.13 ft here against 1.64 ft shipped, because the pad did not shrink with the diamond. That margin is
pinned in the tests rather than enforced by the validator, because a smaller trial would fail the
check on a dress that draws correctly.

**Nothing in the sim reads it.** `flight.dirtTimeScale` keys off the batted-ball class, not ground
position, and `ParkDiamond.OnDirt` has two non-test consumers, both boolean shape gates. The dress is
presentation; it is in `data/rules/` because it is measured from geometry that moved.

---

[#725](https://github.com/jackguillet/grand-sluggers/issues/725) — where the fielders stand, on the
compact field. Seven of the nine spots in `Diamond.Positions` were C# literals; they are now
`data/rules/fielders.json`, and the trial carries its own copy. The shipped file is authored at
today's exact values, so a no-overlay run is byte-identical by construction — checked on `cli match`
seeds 1, 7, 29, 104 and 2718. "P" still forwards to the rubber `infield.json` names and "C" to
`HomeSet.CatcherZ`; neither is in the file, because naming them there would be a second source of
truth.

| | shipped | c80 |
| --- | --- | --- |
| 1B / 3B | (±78, 72) | (±69.33, 64.00) |
| 2B / SS | (±42, 118) | (±37.33, 104.89) |
| LF / RF | (±110, 250) | (±77.09, 175.19) |
| CF | (0, 305) | (0, 213.50) |

**The infield takes the basepath scale**, 80/90, the same factor as the bags. The corner gap follows
it exactly: 16.62 ft shipped → **14.77 ft** here, where an unmigrated corner stood 26.41 ft out. 2B
goes 43.01 → **38.23 ft**. The middle infield radius lands at 111.33 ft, which clears the migrated
137.78-ft lip by 26.45 ft — the floor #728 argued from, now measured against the authored starts
instead of scaled ones.

**The outfield does not take the fence scale, and it does not take one scale at all.** Each body
keeps its bearing and its fraction of the fence *at that bearing*, computed through the circular
fence arc (`AtBatResolver.FenceAt`) on harbor-diamond. LF and RF keep 0.7203 of a 379.16-ft fence at
∓23.75°, so radius 273.13 → 191.40. CF keeps 0.7625 of 400, so 305 → 213.50. The radial factors
those imply are **0.7008 and 0.7000** — close enough to look like one number and not one number, and
`110 × 0.70 = 77.00` is the wrong answer that looks right. The reason they differ is in this file
already: Harbor keeps its accepted 232 at the poles, and a flat 0.70 gives 231.

**One global set clears all six parks.** Before: all eighteen outfield starts (three bodies × six
parks) sat outside the wall and `FieldBounds.Clamp` pinned every one of them to the warning track.
After: none is clamped, and the tightest of the eighteen is Canopy Yard's centre field at **51.50 ft**
of fence in front of it. So no per-park exception is needed **to clear a wall**, and per-park depth
stays with #713, exactly as #730 decision 3 decided. That is a claim about fences only: Funfair's
coded night chompers are a per-park consequence the one global set does walk into, recorded above.

**What it does to a run.** Twelve `cli match` seeds under the trial, before and after: 43 runs → 35,
hits 63 → 52, balls-in-play outs 186 → 194, home runs 21 → 17. Read the direction, not the digits —
the moment one play differs the seed's whole stream diverges, which is why pitch-level counts that
the fielders cannot touch move too (called balls 367 → 398). Ten of the twelve seeds end on a
different scoreline. The controlled numbers in this section are the geometric ones above.

**What it does not fix, on purpose.** `ChemistryToy.GroupTokenSpot` carries the shipped depths
pre-normalised (P 0.20 = 60.5/305, IF 0.39 = 118/305, OF 0.82 = 250/305) for the draft screen's
group rows. It does not follow a trial root, and deciding whether it should — whether a group token
tracks the live centre-field depth or stays a fixed layout — is a design call nobody has made.
`tools/compact-field-report.py` holds its own nine-spot copy of the shipped starts; that is the
frozen #708 control record the packet's arithmetic is published from, and pointing it at live C#
would change what the packet means.

---

[#732](https://github.com/jackguillet/grand-sluggers/issues/732) — hazard radii and the reach pad, on
the fence scale ([#730](https://github.com/jackguillet/grand-sluggers/issues/730) decision 4). Every
`radius` in the five parks that have hazards is the shipped radius × 0.70, at the two decimals the rest
of the overlay spells, and `rules/fielding.json` joins the overlay as the tenth file with one change:
`park.pipeReachPadFt` 8 → **5.6**. `emberNightFireMul` 1.6 is dimensionless and stays. The shipped
files are untouched, so a no-overlay run is byte-identical by construction — checked on `cli match`
seeds 1, 7, 29, 104 and 2718 at the default park and at Canopy, Funfair and Ember, twenty runs.

**Why the radius moved after #717 said it would not.** #717 left radii alone because a barrel is a
physical object. #730 measured the code instead: a barrel's capture test is `radius + pipeReachPadFt`
(`Fielding.cs`, `WarpIfPipe`), a fire breath's radius is × 1.6 at night, a freeze triggers on where the
*ball* lands and then slows a body that need not be near it, and a `climb_wall` radius is never read at
all. A radius is a trigger zone, not a body. Fair territory falls to 0.70² = 49% of shipped, so an
unscaled zone roughly doubles its share of the field — Ember's night fire breath would have covered
3.6% of fair ground. × 0.70 preserves each hazard's share exactly, and unlike the *positions* it takes
the fence factor everywhere, infield barrels included: share is an area question and fair area follows
the fences whatever zone a hazard sits in. `climb_wall` 90 → 63 is scaled with the rest for the same
rule, and nothing reads it.

**Why the pad had to move with it, in the same commit.** The pad is larger than any barrel or pipe
radius. Scaling the radius alone would take a Canopy barrel's real catch from 13.00 ft to 11.50 — an
11.5% cut on a field that lost 30%, and the packet's own `captureFenceScaledFt` column records that
trap. With the pad at 5.60 the disc is 3.50 + 5.60 = **9.10 ft**, exactly 0.70 of 13.00; a Funfair pipe
goes 12.00 → 8.40. That one number is why the fielding table enters the overlay here, whole, ahead of
the two 3c chains that will write it (#727).

| Hazard | shipped r | c80 r | what the ball meets, shipped → c80 |
| --- | --- | --- | --- |
| canopy `barrel` ×3 | 5 | 3.5 | capture disc 13.00 → 9.10 |
| funfair `warp_pipe` ×3 | 4 | 2.8 | capture disc 12.00 → 8.40 |
| ember `fire_breath` | 16 | 11.2 | day 16 → 11.2; night (×1.6) 25.60 → 17.92 |
| ember `lava_pit` ×3 | 10, 10, 12 | 7, 7, 8.4 | slow zone |
| crystal `freeze_volume` ×3 | 8, 8, 10 | 5.6, 5.6, 7 | freeze zone |
| rooftop `billboard` ×2 | 12 | 8.4 | star sign |
| rooftop `ac_unit` | 6 | 4.2 | — |
| canopy `tree` ×4 | 8, 8, 10, 10 | 5.6, 5.6, 7, 7 | — |
| ember `statue` | 10 | 7 | — |
| canopy `climb_wall` | 90 | 63 | never read; the type is |

**What it does to a run.** Forty-eight `cli match` seeds under the trial (seeds 1–8 in all six parks),
before and after: **3 of 48 diverge** — Canopy seeds 3 and 5 and Funfair seed 7 — and 45 are
byte-identical, Harbor's eight included because it has no hazards. Each divergence starts at a fly or a
hop that used to fall inside a disc and no longer does; the one warp in the before set ("it hopped a
barrel cannon", Canopy seed 3) is gone from the after set. One scoreline changes (Canopy 3: 4–3 → 5–4).
Read the direction, not the digits: once one play differs the seed's stream diverges and pitch-level
counts move with it. The controlled numbers are the geometric ones in the table.

**What this leaves open.** `rules/fielding.json` is now carried whole, so from here every key added to
or removed from `data/rules/fielding.json` must change this copy in the same commit — `DataRoot.WholeFiles`
refuses a copy missing a shipped key, and only `RulesValidation.UnknownFields` catches a key the copy has
and the shipped file no longer does. That makes #730 decision 6, should it proceed and delete
`throw.onTheFlyFt`, a two-copy edit. Funfair's coded night chompers (`ParkHazards.FunfairChompers`,
radii 16 / 18 / 16) are C# literals and did not scale, as their positions did not in #717 — recorded,
not repaired. `pipeReachPadFt` carries no `[Positive]` validator in `Rules.cs`; adding one is a sealed-
source edit and was left for a slice that already has to regenerate the packets.

---

[#722](https://github.com/jackguillet/grand-sluggers/issues/722) slice 1 — the throw clock becomes the
accepted curve (F693-03-release-clock, -travel-clock, -long-throws, -long-throw-numbers, -good-chemistry,
-negative-chemistry). One function, `InPlay.ThrowSec`, flies the live ball and feeds both CPU estimates:

```
throwSec = releaseSec + [ d / (baseFtPerSec × arm) + longThrowLossSec × (max(0, d − R) / 80)² ] / (chem × ability)
arm      = armBase + armPerField × Arm            R = comfortableRangeFt + rangePerArmFt × (Arm − 5)
```

`rules/fielding.json` carries `releaseSec` **0.30**, `baseFtPerSec` **88.89** (0.90 s over 80 ft),
`comfortableRangeFt` 160, `rangePerArmFt` 5, `longThrowLossSec` **0.60**, `chem.badSpeedMul` **0.90**
and `chem.slantChance` **0**. The shipped table gained the three new keys at 160 / 5 / **0** and
`badSpeedMul` **1.0**: with the loss at zero the loss term is exactly 0.0 and the clock is the flat one
the game always had, to the bit — checked on 20 `cli match` seeds and all 50 S-29 cohort games against
pristine `main`. The switch is the table, nothing else.

**What the curve gives, neutral chemistry, command to arrival.** konga's third→home is 3.31 s.

| From | Arm 3 | Arm 5 | Arm 8 | Arm 10 |
| --- | --- | --- | --- | --- |
| 160 ft | 2.22 | 2.10 | 1.95 | 1.87 |
| 220 ft | 3.39 | 3.11 | 2.76 | 2.57 |
| 280 ft | 5.24 | 4.80 | 4.22 | 3.89 |

A relay through SS on the compact centre line (second leg 104.89 ft) arrives first beyond **221 / 237 /
258 / 272 ft** for an Arm 3 / 5 / 8 / 10 thrower against an Arm 5 cutter; an Arm 8 cutter pulls each in
by 11–13 ft. On the shipped clock the relay never arrives first anywhere inside 400 ft, which is why that
table keeps its 200-ft gate. Good chemistry divides the whole flight by 1.30 once, the loss included:
280 ft is 4.80 → 3.76. A bad pair is 0.90 on every throw and never slanted; its lateral spread is the
thrower's Field accuracy alone.

**What it does to a run.** The trial now throws on a different clock, so most seeds diverge: 33 of the
48 (seeds 1–8 × six parks) against the PR #740 state, 15 scorelines. S-29 under the trial moves
**1.00 / 1.20 → 1.06 / 1.34** runs a side (home / away). That is still under the 1.8 floor — it was
under it before this slice, and the whole compact profile is what 3d bands, not one clock. Read the
direction, not the digits.

**What this slice leaves to slice 2.** The CPU still relays by the 200-ft gate on both roots and the
runner's estimate still reads a null arm; slice 2 makes both read this clock — `direct` against
`relay = leg + transfer + leg` with real arms and pair chemistry — and re-describes `onTheFlyFt` as the
forced-relay ceiling the trial sets to never. Laser and Snap Throw keep their multipliers until #723.

---

[#722](https://github.com/jackguillet/grand-sluggers/issues/722) slice 2 — the CPU relays by total time
(decision 6 of [#730](https://github.com/jackguillet/grand-sluggers/issues/730)). `rules/cpu.json` joins the
overlay as the eleventh file, and `rules/fielding.json` sets the forced-relay ceiling `throw.onTheFlyFt` to
never (9999).

**The rule.** For a throw to a bag the CPU computes `direct` and `relay = leg + the cutoff's reaction + leg`,
each on the one clock with the real arms and the rung's read of the pair chemistry, and takes the relay when
it saves more than the rung's `relayBiasSec`. Beyond the ceiling it always relays. The shipped table keeps
the 200-ft ceiling beside a bias no relay can save (99), so a no-overlay run relays exactly when it did
before — checked on 20 `cli match` seeds and all 50 S-29 cohort games against pristine `main`.

**The runner reads the same plan.** `RunnerAi.ThrowArrivalSec` takes the clock the live ball carries
(`BallSituation.ThrowClock`): the fielder's own plan from whoever holds the ball next, with the rung's read of
the arm, the relay and the chemistry. The shipped rungs read none of it, which is the neutral flat throw the
runner always read.

| Rung | `relayBiasSec` | `runnerReadsArm` | `runnerReadsRelay` | `readsChemistry` |
| --- | --- | --- | --- | --- |
| easy | 0.3 | 0.5 | 0 | 0 |
| normal | 0.1 | 1 | 1 | 1 |
| hard | 0 | 1 | 1 | 1 |
| shipped, every rung | 99 | 0 | 0 | 0 |

**What it does to a run.** Forty-eight trial seeds (1–8 × six parks), slice 1 → slice 2, read from the live
traces: outfield throws through the cutoff **14 → 2** and direct **102 → 112**. The fourteen that used to relay
left from 206–251 ft from home, all just past the old gate; the two that still do leave from 250 ft. 12 of the
48 seeds diverge. S-29 under the trial: **1.06 / 1.34 → 0.82 / 1.30** runs a side; the control is unchanged.
On the compact field most outfield releases are 154–265 ft from home and the break-even for most arms is
221–272 ft, so the relay is now the deep play and the direct throw the ordinary one — which is what the
decision asked for. How often that is the right call is 3d's to judge, not this slice's.

**Tests.** `RelayDecisionTests`: the arithmetic, the rungs' reads, the runner's clock, and six live scenarios
with konga tagging from third on a 245-ft fly to centre. On the control both arms relay (forced); on the
trial moss (Arm 4) relays, vine (Arm 8) throws home direct, and an Arm 5 relays on hard but not on easy —
each checked against the plan recomputed from the live positions at the release frame. The in-process trial
stands the bodies on the shipped spots (`Diamond` reads the shipped root), so these are decision tests, not
geometry.

**What slice 3 does.** The two tag-up carry gates become margins on this estimate, and `running.json`
enters the overlay with its clock keys byte-identical.
