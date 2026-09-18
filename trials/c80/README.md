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
a file the trial never meant to own. A captain edited next month changes both runs, because both
runs read the same file. The role players are the one character file this trial carries (#718, so
that three of them can hold Ball Dash): an edit to them in `data/` has to be mirrored here, or the
two runs field different rosters.

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
ball that fits it. #717 landed eight files in one commit — the folder carries thirteen now — because
**drag is global and park dimensions are not**:
at drag 0.0040 the best swing in the game carries 304 ft, so drag alone against the shipped 330-ft
poles is a game with no home runs in it, and the parks alone are a derby.

| File | What it changes |
| `characters/role-players.json` | the roster with Ball Dash (#718): dart, pip and jester carry `ball-dash` in place of lick-catch, snap-throw and lick-catch. Every other body is the shipped one, field for field. |
| --- | --- |
| `rules/cpu.json` | the CPU's read of a throw (#722): `relayBiasSec` 0.3 / 0.1 / 0 by rung, `runnerReadsArm` 0.5 / 1 / 1, `runnerReadsRelay` 0 / 1 / 1, `readsChemistry` 0 / 1 / 1. Nothing else in the table; the rung stays normal. |
| `rules/infield.json` | 80-ft basepaths (#717), and the ground that dresses them (#729). One file, because #711 made the infield global and every park shares it. |
| `rules/running.json` | the tag-up as a race (#732): carry gates 9999, `tagUpHomeMarginSec` 0.25, `tagUpThirdMarginSec` 0.07. Every clock key is the shipped value, byte for byte. |
| `rules/fielders.json` | where the seven gloves stand (#725): the infield four on the basepath scale, the outfield three on bearing and fence-at-bearing fraction. P and C are not in the file. |
| `rules/fielding.json` | `park.pipeReachPadFt` 8 → 5.6 (#732), and the throw clock (#722): `throw.releaseSec` 0.30, `baseFtPerSec` 88.89, `longThrowLossSec` 0.60, `chem.badSpeedMul` 0.90, `slantChance` 0, the forced-relay ceiling `throw.onTheFlyFt` set to never; and the pursuit contract (#718): `chase` 12.4 + 1.12 × Run, the four read clocks 0.35 / 0.45 / 0.25 / 0.40, cover at the body's own speed from contact, and the response law `chase.accelSec` 0.20 / `brakeSec` 0.10; and the throw commands (#723): `abilities.laserMul` 1.25 with `laserHomeOnly` 1, `snapThrowMul` 1.0 with the 0.22 s `snapReleaseSec`, `throw.relayAutoContinue` 0, `relayBufferSec` 0.25; and Ball Dash (#718): `abilities.ballDashMul` 1.20 with the universal sprint retired, `dash.chaseMul` 1.0; and the human seat's pursuit stick (#718): `stick.enterMag` 0.20 / `leaveMag` 0.15, the calibrated radial stick; and passive coverage (#719): `catch.standUpReachFt` 6.0 with `chase.outfieldAirMul` / `infieldAirMul` 1.0; and the earned dive (#719): `catch.autoDive` 0, `diveRecoverySec` 0.60, `diveRecoveryFieldCut` 0.025; and the normal jump (#719): `catch.jumpAirSec` 0.60, `jumpBufferSec` 0.10, `jumpReachFt` 0. Nothing else in the table. |
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
2.95 s and linear speed falls from 30.51 to 27.12 ft/s on the shorter path. `running.json` was not
carried by this slice; #732 carried it later for the tag-up race with every clock key byte-identical,
which keeps the anchor (its section is below).

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

---

[#732](https://github.com/jackguillet/grand-sluggers/issues/732) — the tag-up as a race (decision 5 of
[#730](https://github.com/jackguillet/grand-sluggers/issues/730)). `rules/running.json` joins the overlay as
the twelfth file. Its two carry gates go to never and two new thresholds are authored; **every clock key is the
shipped value, byte for byte**, because the runner clock is the C80 anchor, and a test now checks that on the
values rather than on the file's absence.

**The rule.** At the catch — once — a CPU runner on third or second goes when `margin(next)` clears the bag's
threshold plus the rung's `runnerMarginSec`. The margin is the same estimate every other CPU runner read uses:
the defense's arrival at the bag, thrower's arm and the fielder's relay read since #722, minus the runner's own.
A body held at the catch is not sent by a later event. The shipped table sets the thresholds to 99, a margin no
play reaches, so it decides by the two carry gates exactly as it always did — 20 `cli match` seeds and all 50
S-29 cohort games identical to pristine `main`.

**Where the numbers come from.** A sweep under this overlay with the thresholds at −99, so every runner raced:
konga, cinder and dart on third and on second, flies to left (vine, Arm 8), centre (moss, Arm 4) and right (hex,
Arm 4 with Laser), six depths from 185 to 260 ft — 108 races. Read margin against outcome:

| Race | every OUT was read at or below | every safe at or above | crossover |
| --- | --- | --- | --- |
| third → home | +0.114 | −0.037 | about +0.10 |
| second → third | −0.099 | −0.111 | about −0.08 |

The thresholds are **0.25 home / 0.07 third**: the hard rung (−0.15) sits on the crossover, because a race has a
right answer and the best judge should be on it; normal holds 0.15 s longer and easy 0.30 s longer while reading
half the arm and no relay. Verified under the overlay on each rung:

| Rung | home: sent / safe / out | third: sent / safe / out |
| --- | --- | --- |
| hard | 31 / 30 / 1 | 8 / 8 / 0 |
| normal | 28 / 28 / 0 (holds 4 who were safe) | 8 / 8 / 0 |
| easy | 29 / 29 / 0 | 11 / 9 / 2 (its worse read) |

For normal on the crossover instead, author 0.10 and −0.08. S-54's own play at 215 ft to left under normal: konga
reads −0.25 and holds, cinder +0.11 and holds, dart +0.59 and goes; the shipped rule sent all three.

**What it does to a run.** 2 of 48 trial seeds diverge from slice 2; the S-29 cohort under the trial moves
**0.82 / 1.30 → 0.90 / 1.24** with 47 of 50 scorelines unchanged; across the 48 seeds the runner tagged out at
home goes 11 → 9 and the fly-out double play 2 → 0. The control is unchanged.

**What is deliberately not here.** A runner on first has no race (the threshold is infinite); the late break on
a throw to the cutoff is shut by design; both are new behaviour for their own rows if wanted.

---

[#718](https://github.com/jackguillet/grand-sluggers/issues/718) slice 1 — the pursuit contract's speed, its
four read clocks, and cover at contact (F693-02-pursuit-speed, the read clocks, F693-02-coverage-budget). All in
`rules/fielding.json`; the shipped table gains two cover switches at the values that are today's rule.

| | shipped | c80 |
| --- | --- | --- |
| `chase.baseFtPerSec` + `ftPerSecPerRun` × Run | 21 + 1.9 × Run (30.5 at Run 5) | **12.4 + 1.12 × Run** (18.0 at Run 5, the spread kept to half a percent); floor 8 → 4.72 |
| `reaction` P / C / 1B 2B 3B SS / OF | 0.42 / 0.67 / 0.27 0.25 0.30 0.28 / 0.83 | **0.35 / 0.45 / 0.25 × 4 / 0.40** |
| `cover.startSec`, `lockoutMul`, `chaseSpeedWeight` | 0.23, 1, 0 — D11's flat 28 ft/s after the start and the body's read | **0, 0, 1** — cover, cutoff and backup walk at the body's own pursuit speed from contact, no read |

`FieldingResolver.CoverSpeedFt` is one function for the live walks, the CPU's cover-arrival estimate and the bunt
square's cover walk; at weight 0 it returns `cover.ftPerSec` itself, so the shipped walk is the same double it always
was — 20 `cli match` seeds and all 50 S-29 cohort games identical to pristine `main`.

**What it does to a run.** This is the slice the tracker said would show: every one of the 48 trial seeds diverges
from slice 3 and 39 scorelines change. Legs at 18 ft/s instead of 30.5 outweigh the faster reads —

| 48 trial seeds | slice 3 | #718 slice 1 |
| --- | --- | --- |
| fly outs | 701 | **551** |
| singles / doubles / triples | 110 / 9 / 0 | **201 / 52 / 13** |
| home runs | 75 | 63 |

— and S-29 under the trial goes **0.90 / 1.24 → 1.74 / 1.56** runs a side, the first time the compact profile has
been near its 1.8 floor. Read the direction, not the digits: the pursuit speed and the reads are accepted anchors, and
3d bands the whole profile once catch, dive and the response law have landed on top of them.

**Reported, not repaired.** `chase.outfieldAirMul` 0.6 and `infieldAirMul` 0.45 — the S-29 levers of #609 and #636
— still multiply the new speed, so an outfielder under a fly runs 10.8 ft/s at Run 5; #718 does not name them and
this slice leaves them for the calibration that decides the air balls. The pursuit planner's travel estimate assumes
the body is at speed from its first step, which is true until slice 2's acceleration ramp. The in-process scenario
that pins cover-at-contact stands the bodies on the shipped spots.

**What slice 2 does.** The response law — a 0.20 s ramp to speed, a 0.10 s brake, reversal as brake then ramp —
and the carry: ordinary carry at the pursuit top speed, the universal activated sprint retired in favour of a
1.20× Ball Dash for the ability's carriers, of whom the roster currently has none.

---

[#718](https://github.com/jackguillet/grand-sluggers/issues/718) slice 2 — the response law
(F693-02-carry-movement-response). `rules/fielding.json` carries `chase.accelSec` **0.20** and `brakeSec` **0.10**;
the shipped table gains both at **0 / 0**, and at 0 / 0 every step in the live field is the instant step the game
always had — the same code path, not a product by one.

**The law.** Every body keeps a velocity. Each frame it wants a velocity toward its goal at its commanded speed (rest
inside the stop radius; the stick's proportional want for the human glove), and its velocity answers through one
function: the component along its heading builds at the ramp rate (rest to the body's rated speed in `accelSec`) and
dies at the brake rate (the rated speed to rest in `brakeSec`); the component across it builds at the ramp rate. So a
stop is the brake, a reversal is the brake and then the ramp, and an angled turn is continuous correction through the
same two rates. A body nobody steps brakes to rest; a body the ring left keeps coasting for `handoffCoastSec` and then
brakes instead of stopping dead. The rates are measured against the body's own pursuit top speed, not the speed it is
asked for this frame, so an outfielder under a fly (× 0.6) ramps like the body it is.

**The planner knows.** `FieldingPursuit.Route` carries the ramp's half-time: travel takes it, and a body that could
just reach a sample at speed cannot once it must get to speed first. At 0 the arithmetic is the old one exactly.

**What it does to a run.** 33 of 48 trial seeds diverge from slice 1, 21 scorelines. S-29 under the trial:
**1.74 / 1.56 → 1.70 / 1.64**; 25 of 50 cohort scorelines unchanged. Read the direction: a tenth of a second at
every start and stop, on every body, is what the accepted law costs, and it lands on top of legs that already run
at 18 ft/s. The control is unchanged — 20 seeds and all 50 cohort games identical.

**What this slice leaves.** The analog stick's enter / leave thresholds (0.20 / 0.15) and controller calibration
are the human seat's feel and are not here (`Feel.FieldAssistStick` 0.35 stays the one threshold). The carry — the
universal activated sprint retired for a 1.20× Ball Dash — waits on a roster body that carries Ball Dash; there is
none today. Both are #718's remaining slice.

---

[#723](https://github.com/jackguillet/grand-sluggers/issues/723) — throw commands and abilities (F693-03-relay-ownership,
-input-buffer, -throw-cancel, -laser-throw, -snap-throw). All in `rules/fielding.json`; the shipped table gains four
keys at the values that are today's rule.

| | shipped | c80 |
| --- | --- | --- |
| `throw.relayAutoContinue` | 1 — a human's cutoff throws the armed onward leg for them | **0** — each relay leg needs its own command; the cutoff holds |
| `throw.relayBufferSec` | 0 — no queue | **0.25** — an early press is remembered that long of active play and fires at the catch; bag selectors retarget it without refreshing its age; a fresh RB / period cancels it (`LivePadInput.Cancel`, the `cancel-throw` verb) |
| `abilities.laserMul`, `laserHomeOnly` | 1.45, 0 — the boost on every throw | **1.25, 1** — only on a throw home with a live runner on third or the third–home segment; a cutoff feed never carries it, and the CPU's own forecast strips it the same way |
| `abilities.snapThrowMul`, `snapReleaseSec` | 1.22, 0.22 — a faster ball; the snap release equals the ordinary release, so it is inert | **1.0, 0.22** — a 0.22 s release against the ordinary 0.30, after a clean received teammate throw only; a pickup, a bobble, a sail or a hand-off clears it |

The CPU never queues or cancels, and the shipped abilities are untouched, so a no-overlay run is the game it always
was — 20 `cli match` seeds and all 50 S-29 cohort games identical to pristine `main`.

**What it does to a run.** The CPU's Laser holders (hex, boom, nugget, brondo) lose the boost on every throw but the
one home with a runner on third, and its Snap holders (pip, frost, pewter, vale) release a received ball in 0.22 s
against 0.30: 22 of 48 trial seeds diverge from #718 slice 2, 10 scorelines, and S-29 under the trial moves
**1.70 / 1.64 → 1.80 / 1.64** — home on the floor for the first time, away still under it — with 46 of 50 cohort
scorelines unchanged.

**What the human seat gets.** Tested on the human seat with a Snap holder at the cutoff: on the control the armed
leg fires at the catch and a press in flight is nothing; on the trial the cutoff holds a full second until South
is pressed, a press 0.15 s before the catch fires at the catch, a press 0.50 s before expires, and a remembered press
cancelled by a fresh RB is gone — and the onward throw the Snap holder makes leaves 0.08 s sooner than an ordinary
release would. `LivePlaySystem.ThrowQueued` / `QueuedThrowBag` and `LiveEvent.ThrowQueued` / `ThrowQueueCleared`
are the HUD's tells; Unity maps RB / period on the defense pad to the cancel.

**Left to the book pass.** The how-to page's row for the cancel verb (its pixel budget is P8's), and the queue's
visible feedback.

---

[#718](https://github.com/jackguillet/grand-sluggers/issues/718) — slice 3, Ball Dash (F693-02-ball-dash-carrier,
-ordinary-carry-speed; -field-dash-peak superseded as accepted). Two files: `rules/fielding.json` gains one key in both
roots and moves one on the trial; `characters/role-players.json` is the thirteenth file, and the first character file the
trial carries.

| | shipped | c80 |
| --- | --- | --- |
| `abilities.ballDashMul` (new) | 1.20 — a Ball Dash holder carries the ball at this multiple of its pursuit speed; no shipped body holds the ability, so the table never reads it | **1.20** — dart, pip and jester |
| `dash.chaseMul` | 1.35 — East held is a universal fielding sprint, no fade | **1.0** — no sprint; the only faster carry is the ability's |
| `characters/role-players.json` | dart lick-catch, pip snap-throw, jester lick-catch | **`ball-dash`** on the three — the fastest role players (Run 9, 8, 8); every other body is the shipped one, field for field |

**What the ability is.** A holder with the ball securely in the glove — caught or handed, not in flight — moves at 1.20 ×
the pursuit speed it was asked for: on the stick, on the CPU's walk to a bag or at a runner in a rundown, and in the CPU's
carry forecast (`CpuWalkSec`) that decides whether its legs or a throw make the play. No press, no timer, no cooldown; no
catch-reach or throw bonus. Only the cap moves: the response rates stay measured against the body's unboosted speed
(`RatedSpeed` no longer takes the asked speed), so dart builds 1.87 ft/s a frame exactly as zig does, parts from him at
22.48 ft/s and settles at 26.98 on the fifteenth frame against zig's twelfth. `FieldingResolver.CarrySpeedFt` is the asked
speed itself for every other body — not a product — so the shipped walk is the same double it always was.

**Control unchanged.** 20 `cli match` seeds and all 50 S-29 cohort games byte-identical to pristine `main`: the shipped
roster has no holder, the shipped `chaseMul` is untouched, and `RatedSpeed` is read only under the response law, which is
off on the shipped table.

**What it does to a run — and what does not.** 20 of 48 trial seeds diverge from #723 (16 scorelines) and S-29 under the
trial moves **1.80 / 1.64 → 1.98 / 1.68** (11 of 50 scorelines) — home in the band for the first time, away still under
it. **All of it is the roster and none of it the carry:** the same 48 seeds and 50 games run with the new roster and
`ballDashMul` 1.0 are byte-identical to the full trial. Fly outs fall 588 → 545 over the 48 seeds — dart and jester no
longer bring lick-catch's 6-ft catch bonus to the outfield, and pip no longer brings Snap's 0.22-s release to the cutoff —
and the divergence often surfaces plays after its cause, as a changed reach or release shifts the timings under identical
text. In CPU-only games the carry speed itself matters only when the CPU runs the ball to a bag or chases a rundown, and
no carrier was in one across these games. It shows where it is staged: the human seat running the ball, and the CPU first
baseman walking a grounder to the bag at 27.0 ft/s against zig's 22.5 (`BallDashTests`).

**What did not move.** `outfieldAirMul` / `infieldAirMul` (to #719 with the air-ball calibration), the analog
thresholds and controller calibration (#718's last slice), the how-to book (the profile flip).

---

[#718](https://github.com/jackguillet/grand-sluggers/issues/718) — slice 4, the human seat (F693-02-pursuit-analog-response,
-neutral-boundary, -calibration-policy, -arming, -calibration-samples). One file: `rules/fielding.json` gains a `stick`
block in both roots; the trial moves its two gates.

| | shipped | c80 |
| --- | --- | --- |
| `stick.enterMag`, `leaveMag` | 0, 0 — the stick the game shipped with: a Manhattan gate at `feel.fieldAssistStick` (0.35), the raw stick vector as the asked velocity, no calibration, no arming | **0.20, 0.15** — manual pursuit from 0.20 of the calibrated radial magnitude, back to assistance at 0.15, the owner kept between; the asked speed is `(magnitude − 0.15) / 0.85` of the glove speed, capped at 1 |
| `stick.calibrationSec`, `centerOffsetMax`, `sampleSpreadMax` | 0.50, 0.10, 0.02 — the accepted window, inert on a stick that is never calibrated | 0.50, 0.10, 0.02 — a released-stick window on an input clock, adopted whole or not at all |

**What the seat gets.** One read a frame (`LivePlaySystem.ReadPursuitStick`) that every stick site shares — the batted-ball
steer, the walk with the ball, the loose-ball chase, the take of the glove, the neutral-stick auto-catch and auto-dive. On
the trial the read is a `PursuitStick` per bound device (`LivePadInput.Device`): half the usable range asks half the speed,
a diagonal past the ring still asks full, and the want goes through the response law like any other. The seat **arms** with
a valid calibration and one neutral observation, owed again at every new half (`BeginLive`), on device recovery or
replacement and after a successful recalibration; unarmed, every read is assistance and `PursuitUnready` is the client's
tell. A fresh device is on the identity profile (what a keyboard reports); a client that binds an analog stick invalidates
it and samples a 0.50-s window — mean centre within 0.10, every sample within 0.02 of it, inclusive — through
`StickCalibration`, adopting only a complete valid window.

**Control unchanged.** 20 `cli match` seeds and all 50 S-29 cohort games byte-identical to pristine `main`: at
`enterMag` 0 the read is the Manhattan gate and the raw vector, the same doubles the sites read before.

**What it does to a run.** Nothing the CPU plays: 0 of 48 trial seeds and 0 of 50 cohort games move against the Ball Dash
slice, because the CPU has no stick. It shows on the human seat (`PursuitStickTests`): zig holding the ball runs at 11.24
ft/s on a 0.575 stick and 22.48 on a full or diagonal one; a 0.18 stick from rest stands, 0.30 goes, 0.17 after that still
goes, 0.10 stops; a stick held from the first frame never steers until it has been seen neutral once, and the assistance
takes the grounder for it meanwhile.

**Left to the Unity pass.** The pause-menu entry that runs a calibration window and the unready tell on the HUD; and
Unity's default stick processor (a 0.125 dead zone) still sits in front of the coordinate the sim calibrates — it has to
come off the defense pad's stick for the 0.20 / 0.15 gates to mean what they say.

---

[#719](https://github.com/jackguillet/grand-sluggers/issues/719) — slice 1, passive coverage (F693-02-catch-reach-envelope,
and Jack's air-multiplier addition of 2026-09-17). One file, `rules/fielding.json`: one new key in both roots, two
existing keys moved on the trial.

| | shipped | c80 |
| --- | --- | --- |
| `catch.standUpReachFt` (new) | 0 — the legacy `radiusBaseFt + Field × radiusPerField` (11.8 ft at Field 3, 16 at Field 10), or the character's authored `reachFt` | **6.0** for every body without its own `reachFt`; the 4-ft dirt pad and the 8-ft dive add to it — **6 / 10 / 14** — and the ability bonuses add as they always did (Lick / Grow / Withdraw +6, Super Jump +3 and +22 on a fly, Dive / Burrow +16 on the dirt: large against 6, reported, not moved) |
| `chase.outfieldAirMul`, `infieldAirMul` | 0.6 (#609), 0.45 (#636) — the S-29 levers of a full-size field and 30.5 ft/s legs | **1.0, 1.0** — one pursuit profile per body, in the air as on the dirt |

**Control unchanged.** 20 `cli match` seeds and all 50 S-29 cohort games byte-identical to pristine `main`.

**What it does to a run — the two levers pull apart.** Against #749 over the same 48 seeds and 50 cohort games:

| trial state | S-29 home / away | hits | extra-base hits | fly outs (share of PA) |
| --- | --- | --- | --- | --- |
| #749 (before) | 1.98 / 1.68 | 369 | 150 | 545 (37 %) |
| reach 6 ft alone | **2.68 / 2.18** | 455 | 233 | 475 (33 %) |
| air multipliers 1.0 alone | **1.20 / 1.02** | 259 | 82 | 608 (45 %) |
| both (this slice) | **1.62 / 1.06** | 289 | 73 | 548 (40 %) |

The 6-ft reach opens the field — doubles 55 → 105, triples 24 → 34 — and lands S-29 inside the band on its own. An
outfielder at the one speed under a fly (18.0 ft/s where he ran 10.8) closes it harder than the reach opens it: together,
doubles 55 → 26, **triples 24 → 0**, and both means under the 1.8 floor. This is an intermediate state and it is
reported, not repaired: the CPU still dives for free at the 14-ft rim on every play (`FlyCatch.AutoDive`, slice 2's to
retire), so the trial's passive air coverage is 14 ft with 18 ft/s legs under it. Slice 2 (the earned dive with its
recovery cost) is where that coverage comes off; judge the calibration after it, not here.

---

[#719](https://github.com/jackguillet/grand-sluggers/issues/719) — slice 2, the dive is deliberate and costs
(F693-02-dive-jump-scoop-reach, -dive-recovery-cost, -cpu-dive-intent, -cpu-dive-intent-policy). One file, `rules/fielding.json`,
three new keys in both roots.

| | shipped | c80 |
| --- | --- | --- |
| `catch.autoDive` | 1 — the dead-stick assistance and the CPU dive at the rim on their own, for free | **0** — a dive is East, or the CPU's deliberate commitment; the neutral stick never dives |
| `catch.diveRecoverySec`, `diveRecoveryFieldCut` | 0, 0 — today's dive is free | **0.60, 0.025** — the diver neither moves nor throws for `0.60 × (1 − 0.025 × (Field − 1))` after the commitment (0.60 at Field 1, 0.555 at 4, 0.465 at 10), caught or missed, human or CPU alike |

**What the dive is now.** East lunges, arms the 0.5-s window and pays at the press (`PayDive`); a catch already made
stands. The CPU (`CpuDiveCommits`) reads the live ball alone — its position and the velocity between the last two frames,
flown on under gravity with no drag and no resolved path — to where it comes down to `diveMaxBallY`, and commits at the last
makeable moment: still in the air, that point past the stand-up ring and inside the rim, arriving inside the arm window,
and its legs no longer able to bring the ring under it. It lunges to that point, pays, and takes the ball only if the live
geometry still holds when it comes; a ball moved after the commitment (`NudgeBall`, the sim's hook for a carom or a
deflection) is missed and stays live, and the recovery is owed all the same. Through the recovery `CanMove` is false for
the diver, the CPU's held-ball table waits, and a human throw press is dropped except inside the last
`throw.relayBufferSec`, where it is remembered and fires at readiness. The later end wins against the fumble because
both timers gate.

**Control unchanged.** 20 `cli match` seeds and all 50 S-29 cohort games byte-identical to pristine `main`.

**What it does to a run — and the finding.** Against #750 over the same 48 seeds: **6 of 48 diverge, 4 scorelines**; fly
outs 548 → 534, hits 289 → 298; S-29 under the trial **1.62 / 1.06 → 1.62 / 1.06**, not one cohort scoreline moved. The
free dive was not what held the trial down. The CPU dove rarely in these games, and where it did the deliberate dive
mostly commits and takes the same ball a frame later. The coverage that took S-29 under the floor in slice 1 is the legs:
an outfielder at the one 18 ft/s under a 4–6 s fly reaches the plant standing up, reach and dive beside the point. The
levers that remain are Jack's — the outfield read (0.40 s), the multipliers themselves, the flight, the outfield's
starting depth — and this folder reports the number rather than moving one of them.

**Left to slice 3 and the Unity pass.** The normal jump; a recovering-diver pose (`LivePlaySystem.DiveRecoveryT`,
`DivingPos`) and the `DiveCommit` tell.

---

[#719](https://github.com/jackguillet/grand-sluggers/issues/719) — slice 3, the normal jump (F693-02-normal-jump-input-profile,
-takeoff-ownership, -arc-trial, -air-response-trial, -startup-trial, -input-buffer, -character-profile, -catch-input, and
F693-02-jump-catch-throw-readiness). One file, `rules/fielding.json`: four new keys in both roots, one existing key moved.

| | shipped | c80 |
| --- | --- | --- |
| `catch.jumpAirSec` | 0 — West arms a window (`jumpArmSec` 0.55, 0.7 at the wall) and the body never leaves the ground | **0.60** — a fresh eligible West press is a takeoff with no added startup; the body is airborne 0.60 s with a root rise of `4 H u (1 − u)`, apex 2.0 ft at 0.30 s, the same for every character, one profile per press — no hold, no cut, no repeat while held |
| `catch.jumpRiseFt`, `jumpAirResponseMul` | 2.0, 0.10 — the accepted anchors, read only above `jumpAirSec` 0 | 2.0, 0.10 — airborne the stick works at a tenth of the ground rates and a neutral body coasts: 1.62 ft from rest, 7.56 ft against a full reversal from 18 ft/s |
| `catch.jumpBufferSec` | 0 — no buffer | **0.10** — a grounded press blocked by the read or a recovery is remembered, bound to its body, and takes off at the first eligible instant; a throw or a dive already committed prevents it; airborne presses queue nothing |
| `catch.jumpReachFt` | 8 — eight feet of horizontal reach in the armed window | **0** — the jump is the arc, reconciled away |

**What the seat gets.** The catch is the jump alone: airborne, the ball high enough, under the ring, in the window — a press too
early lands before the ball, a press too late misses the window, and geometry decides. A jumping catch releases nothing until
the body has landed: a South in the air is dropped except in the last `throw.relayBufferSec`, where it is remembered and fires
on the first grounded frame; a grounded catch adds nothing. `LivePlaySystem.Airborne`, `JumpAirT`, `JumpHeightFt`,
`JumpPending` and the `JumpTakeoff` event are the client's; `JumpT` is set to the airtime at takeoff so the pose and the buddy
leap read as before. The CPU's wall leap is untouched on both roots.

**Control unchanged.** 20 `cli match` seeds and all 50 S-29 cohort games byte-identical to pristine `main`.

**What it does to a run.** Nothing the CPU plays: 0 of 48 trial seeds and 0 of 50 cohort games move against #751 — the CPU
never jumps except at the wall, and that leap is unchanged. It shows on the human seat (`JumpTests`): the arc to the frame,
the catch that comes only in the air, the throw that leaves at takeoff + 0.60, the 0.10-s buffer, one takeoff per held press,
and the two air anchors at Run 5.

**Left to the Unity pass.** The root rise (`JumpHeightFt`) on the hero, the recovering-diver pose, and the `JumpTakeoff` /
`DiveCommit` tells. **Left to 3d:** the calibration finding of slices 1–2 — S-29 1.62 / 1.06 with the legs, not the reach or
the dive, as the coverage.
