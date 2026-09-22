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
ball that fits it. #717 landed eight files in one commit — the folder carries fourteen now — because
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
| `rules/fielding.json` | the throw clock (#722): `throw.releaseSec` 0.30, `baseFtPerSec` 88.89, `longThrowLossSec` 0.60, `chem.badSpeedMul` 0.90, `slantChance` 0, the forced-relay ceiling `throw.onTheFlyFt` set to never; and the pursuit contract (#718): `chase` 12.4 + 1.12 × Run, the four read clocks 0.35 / 0.45 / 0.25 / 0.40, cover at the body's own speed from contact, and the response law `chase.accelSec` 0.20 / `brakeSec` 0.10; and the throw commands (#723): `abilities.laserMul` 1.25 with `laserHomeOnly` 1, `snapThrowMul` 1.0 with the 0.22 s `snapReleaseSec`, `throw.relayAutoContinue` 0, `relayBufferSec` 0.25; and Ball Dash (#718): `abilities.ballDashMul` 1.20 with the universal sprint retired, `dash.chaseMul` 1.0; and the human seat's pursuit stick (#718): `stick.enterMag` 0.20 / `leaveMag` 0.15, the calibrated radial stick; and passive coverage (#719): `catch.standUpReachFt` 6.0 with `chase.infieldAirMul` 1.0 (the outfield's multiplier went to 1.0 with it and came back to the shipped 0.6 after 3d, #715); and the earned dive (#719): `catch.autoDive` 0, `diveRecoverySec` 0.60, `diveRecoveryFieldCut` 0.025; and the normal jump (#719): `catch.jumpAirSec` 0.60, `jumpBufferSec` 0.10, `jumpReachFt` 0; and the impact recoil (#720): `recoil.onsetFtPerSec` 55, `fullFtPerSec` 75 — the knockback block is not read — with the airborne pair `airOnsetFtPerSec` 80, `airFullFtPerSec` 115; and the handling error (#721): `handling.awkwardHop` 1 — the routine pickup never rolls, the bobble block is not read; a failed take gets past on a glancing touch of a hot ball (`deflect*`, the accepted anchors in both roots). Nothing else in the table. |
| `rules/flight.json` | `drag` 0.0019 → 0.0040 (#717) and `classes.infieldLipFt` 155 → 137.78 (#728). Nothing else in the table. |
| `rules/hazards.json` | the hazard pattern library's reach pad (#732, carried here by #847): `warpPipe.reachPadFt` and `barrel.reachPadFt` 8 → 5.6. The number was `rules/fielding.json`'s `park.pipeReachPadFt` until the library moved it under its types; it did not change when it moved. Every other row and every other field — the twelve patterns, `fireBreath.nightRadiusMul` 1.6, `chomper.nightOnly` — is the shipped table, byte for byte. |
| `parks/*.json` (six) | fences at one scale, 0.70 (#717), and every hazard radius on the same scale (#732). Wall heights and wind unchanged. Funfair gained three `chomper` rows with #847, migrated by the same zone rule as the rest. |

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

**Closed by [#847](https://github.com/jackguillet/grand-sluggers/issues/847).** The hazard pattern
library made the mouths park data, so they migrate by the zone rule like every other hazard: the
centre one is at (0, 160) with a 12.60-ft rim and the centre fielder stands 53.50 ft off it, 40.90 ft
clear — the shipped 59.00 ft on a compact field. The left and right mouths are at (−50, 144) r 11.20
and (55, 139) r 11.20. Nothing stands in a mouth on either root now
(`CompactGeometryTests.TheMigratedCentreFielderIsClearOfTheFunfairChompers`). The move runs the other
way as well: at 198–228 ft the literals sat where a compact fly could not reach them, so Funfair's
night rows were byte-identical to its day rows; at 139–160 they are in play, and the night row reads
4.90 runs a game against the day's 5.04 (1.18 → 1.15 against Harbor). That is the intended
consequence of removing a park id from code, and it is the only park-factors row on either root that
#847 moved.

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
not repaired, and **closed by #847**, which made them park rows that scale with the rest.
`pipeReachPadFt` carries no `[Positive]` validator in `Rules.cs`; adding one is a sealed-
source edit and was left for a slice that already has to regenerate the packets. #847 moved the
number to `hazards.json` as `reachPadFt` and left it un-`[Positive]` for the same reason: a pad of 0
is a barrel that catches only what lands on it, which is a number, not a broken table.

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

---

[#720](https://github.com/jackguillet/grand-sluggers/issues/720) — slice 1, the ground pickup's recoil (F693-02-clean-ground-pickup-readiness,
-ground-pickup-recoil-basis, -ground-pickup-recoil-cap, -recoil-field-shaping, -recoil-field-factors, -recoil-severity-curve,
-ordinary-recoil-actions, -ordinary-recoil-displacement, -ordinary-recoil-distance-cap, -ordinary-recoil-motion-profile). One
file, `rules/fielding.json`: a new `recoil` block in both roots, two of its five keys moved.

| | shipped | c80 |
| --- | --- | --- |
| `recoil.onsetFtPerSec`, `fullFtPerSec` | 0, 0 — the recoil is the `knockback` block the game shipped with: a stop off the contact's energy and the Hands deficit (up to 0.55 s), the whole tick held for it, grounders only | **55, 75** — a ground pickup costs what the ball's actual incoming speed says: `S = clamp((v − 55) / 20, 0, 1)`, zero through the onset, one past the full speed; the `knockback` block is not read |
| `recoil.capSec`, `handsCutPerPoint`, `kickFtPerSec` | 0.20, 0.05, 10 — the accepted anchors, read only above onset 0 | 0.20, 0.05, 10 — `w = S × (1 − 0.05 × (Hands − 1))`, the recovery `0.20 w` s (0.20 / 0.16 / 0.11 at Hands 1 / 5 / 10 on a rocket), a `10 w` ft/s kick along the ball's travel slowing linearly to rest over it — `w²` ft, one foot at most |

**Where the anchors come from.** The decision left the speeds unselected pending event-sided evidence, so this slice measured
first: a probe at the take over 48 `cli match` runs on this copy (and 20 on the shipped table) recorded every batted ball's
incoming speed the frame before possession. On this copy the 464 ground pickups — grounders, and liners or flies picked up
after landing — arrive at p50 43, p90 55, p99 71, max 73 ft/s; the horizontal component is the speed (vertical share 0.11 at
the median). The onset **55** is that 90th percentile: the hottest tenth recoils, the rest is routine and costs nothing. The
full speed **75** sits just past the hottest ball seen, so the cap binds only on a true rocket — a 125-mph comebacker reaches
the mound in half a second at 91 ft/s. On the copy's own CPU play 11 % of ground pickups recoil, none at the cap, 0.034 s among
them; the shipped knockback stops 41 % of grounder pickups for 0.078 s on average, up to 0.32 s. Liners caught in the air
arrive at p50 77 / p90 91 / max 110 ft/s and flies at 36–41 with a 0.59 vertical share — slice 2's anchors, left unselected here.

**What the seat gets.** The take samples `LivePlaySystem.IncomingFtPerSec` on both tables. On this copy a routine pickup arms
nothing — no clock, no event, the play's marks the same to the frame as with the rule off. A hot one arms `RecoilT` for
`0.20 w` with `ImpactRecoil` set, and the world goes on: the body's steering and throw start wait (`CanMove`, `ThrowPress` — a
South inside the last `throw.relayBufferSec` is remembered and fires at readiness), the CPU glove waits at its decision, the
contact at the bag still counts, and the body skids `w²` ft along the ball's travel on top of whatever the idle brake leaves of
its own run — one path, the ball in the glove. A landed liner picked up off the grass is a ground pickup and costs the same way,
though no bobble is ever rolled on it; a catch in the air costs nothing yet. The shipped knockback and the fumble keep the
whole-tick stop they always had. `LiveEvent.ImpactRecoil` is the client's tell; `RecoilT` still drives the pose.

**Control unchanged.** 20 `cli match` seeds and all 50 S-29 cohort games byte-identical to pristine `main`.

**What it does to a run.** 5 of 48 trial seeds diverge from #752 (2 scorelines); across the 48 the tallies barely move — hits 298 → 299, fly outs 534 → 521 on 1376 → 1343 plate appearances, the fly-out rate 38.8 % both ways. S-29 on the copy: **1.62 / 1.06 → 1.74 / 1.06**, 3 of 50 cohort games change. That is the hottest tenth of ground pickups paying 0.03 s on average and a bang-bang play flipping now and then; the calibration finding of #719 stands — the legs are the coverage, and this slice is not a lever on it.

**Left.** Slice 2: the hard airborne catch by a grounded fielder (F693-02-grounded-air-catch-recoil) with its own anchor pair.
The specials — composition, resistance, pushback possession, repeat eligibility — stay unwritten until
F693-02-special-attack-contracts. The Unity pass owes the `ImpactRecoil` tell.

---

[#720](https://github.com/jackguillet/grand-sluggers/issues/720) — slice 2, the hard catch in the air by a body on its feet
(F693-02-grounded-air-catch-recoil). One file, `rules/fielding.json`: two new keys in the `recoil` block of both roots, both moved.

| | shipped | c80 |
| --- | --- | --- |
| `recoil.airOnsetFtPerSec`, `airFullFtPerSec` | 0, 0 — no catch in the air recoils, as the game shipped | **80, 115** — a batted ball caught in the air by a body on its feet costs the hands the same response as a hot ground pickup, off this pair: `S = clamp((v − 80) / 35, 0, 1)`, then the same `w`, `0.20 w` s, `10 w` ft/s kick and `w²` ft skid |

**Where the anchors come from, and why a second pair.** The same probe as slice 1, the same 48 runs on this copy: 531 catches
in the air. Flies, pops and wall balls come down at 22–42 ft/s with a 0.59 vertical share; liners arrive at p10 67 / p50 77 /
p90 91 / max 110. The ground pair cannot serve — at 55 / 75 it would charge 99 % of liners and cap a fifth of all catches in
the air. So the air reads its own: **80** is the 90th percentile of every catch in the air (the hottest tenth — 38 % of liners,
no fly, no pop), **115** just past the hottest catch seen (110.2). On the copy's CPU play 13.7 % of catches in the air recoil,
none at the cap, 0.033 s among them. The decision's question — can one pair work for both? — is answered no, by measurement.

**What the seat gets.** `ArmRecoil` arms the same `ArmImpact` for a take in the air when the body is `Grounded`: not airborne on
a jump, no jump or dive window open, not a diving or jumping catch, not the buddy leap. A standing shortstop takes an 80-mph
liner at 98 ft/s and pays 0.079 s (Hands 6); a right fielder takes a 122 ft/s rope at the cap, 0.17 s (Hands 4); the centre
fielder who dives on the #719 gap liner at 85 ft/s pays nothing here — the dive's own 0.555 s is his price. The catch is the out
at the take; the throw waits for the recovery, with a South inside the buffer remembered; the skid runs along the ball's
horizontal travel, and a ball dropping straight down supplies no kick. A routine fly stays a routine fly on both tables.

**Control unchanged.** 20 `cli match` seeds and all 50 S-29 cohort games byte-identical to pristine `main`.

**What it does to a run.** Nothing the CPU plays: 0 of 48 trial seeds and 0 of 50 cohort games move against #753 (S-29 stays 1.74 / 1.06). The recoil fires — about one catch in the air in seven, by the probe — but after a catch in the air nothing is racing the throw: the runners are getting back, the CPU's own reaction clock starts at readiness, and a delay of 0.03–0.17 s never turned a returning runner into an out in 48 games. A hot ground pickup races the batter to first, which is why slice 1 moved 5 seeds and this slice moves none. It shows on the human seat (`AirRecoilTests`) and in the trace's `RecoilRemainingSec`.

**Left on #720.** The specials — composition, resistance, pushback possession, repeat eligibility — stay unwritten until
F693-02-special-attack-contracts; nothing in `data/rules/` stands in for them. The Unity pass owes the `ImpactRecoil` tell.

---

[#721](https://github.com/jackguillet/grand-sluggers/issues/721) — slice 1, the awkward hop and the local bobble
(F693-02-handling-error-opportunities, -ordinary-handling-error-chance, -ordinary-handling-error-cap, -ordinary-handling-chance-curve,
-awkward-hop-difficulty-source, -ordinary-bobble-outcome, -bobble-stun, -bobble-stun-duration, -bobble-recovery-reliability,
-bobble-direction-spread, -uniform-error-direction, and the nine -local-bobble-* rows). One file, `rules/fielding.json`: a new
`handling` block in both roots, one key moved.

| | shipped | c80 |
| --- | --- | --- |
| `handling.awkwardHop` | 0 — the bobble is the `bobble` block the game shipped with: a roll off the contact's energy on every grounder take (up to 50 %), a 0.58-s fumble that holds the whole tick, the ball scattered 6.5 ft | **1** — a legal routine pickup never rolls; the one difficulty is the awkward in-between hop at the take; a failed take is a local bobble and the fumbler alone is stunned |
| `handling.chanceCap`, `handsCut` | 0.10, 0.80 — the accepted curve, read only above the switch | `p = 0.10 × D × (1 − 0.80 × H)`: 10 / 6 / 2 % at full difficulty for weak / middle / strong hands, H the Hands trait plus the glove's help (1 → 0, 10 → 1) |
| `handling.hopMinApexFt`, `hopFullApexFt`, `hopPhaseHalfWidth` | 0.5, 1.5, 0.35 | D off the ball's height and rise at the take: 0 for a roll, a falling ball or a hop under six inches; on a rising ball φ = height / projected apex, D = (1 − \|φ − 0.5\| / 0.35) × (apex − 0.5) / 1.0, clamped — the middle of a knee-high hop is the hardest ball |
| `handling.stunSec` | 0.40 | the fumbler neither steers nor takes for 0.40 s; the ball and every other body stay live |
| `handling.bobble*` | 30°, 0.20, 6 ft/s, 0.35, 0.50 ft, 0.25 ft, 0.90, 6 ft/s² | the failed take spills down from the contact with a fifth of its horizontal speed (six ft/s at most) turned a uniform ±30°, rebounds at 35 % under a six-inch ceiling, settles below three inches, keeps 90 % of its roll per impact and slows at 6 ft/s² to rest |

**Where the bands come from.** The decision left the hop phase, height and speed bands pending measurement, so this slice
measured first: a probe at the take over 48 `cli match` runs on this copy, 483 ground pickups. A fifth are rolls (the ball on
the ground), half are falling balls (the clean long hop — the CPU's take fires the moment the ball drops under the 3.2-ft
scoop ceiling), a quarter rising — and of those most are met near the top of the hop. The hops themselves are low: the median
apex is 0.7 ft. Six inches (the bobble's own rebound ceiling) is the micro-bounce line, the knee (1.5 ft) full difficulty,
0.35 the phase half-width so D is above 0 between 15 % and 85 % of the rise. With those bands **8.5 % of the copy's CPU pickups
carry any chance at all, 1.2 % on average among them — about one error a hundred CPU games**; the hardest ball seen was a
120-mph liner into left met by vine 1.3 ft up and rising 9.8 ft/s, D 0.98, 3.3 % to Hands 8 with the glove and 9.4 % to
authored Hands 1. For scale, the shipped rule rolls on 55 % of the copy's grounder takes. The seat's own South-press take can
meet any hop, so the human infielder who reaches for the ball mid-rise faces what the CPU rarely does.

**What the seat gets.** `HopDifficulty` is sampled at every landed take on both tables; on this copy `ArmRecoil` rolls once
(`Match.RollHandling`, the seeded stream) only when it is above 0, and a clean take pays the impact recoil (#720) as any other.
A failed take is `LocalBobble`: the ball goes loose from where it met the glove, `TickLocalBobble` carries it, `StunT` holds the
fumbler — no steering, no jump, no dive, no take — while the world goes on; a helper may reach it first; the loose pickup never
reaches the roll, so one mistake is one consequence. `LiveEvent.Bobble` and `PlayerBobble` are the client's as before; the
`Bobbling` whole-tick flag stays the shipped fumble's. Seed 35 on the test liner is the one in thirty where vine's 3.3 % comes
up: the ball leaves at 6.0 ft/s inside ±30° of its travel, never above where it met the glove, rests 3.2 ft away; vine is
stunned 24 frames, drifts 0.70 ft on the brake, picks it up himself 0.64 s after the take, and the batter has his double.

**Control unchanged.** 20 `cli match` seeds and all 50 S-29 cohort games byte-identical to pristine `main`.

**What it does to a run, and what it does to the stream.** 38 of 48 trial seeds diverge from #754 (27 scorelines), and S-29 on the copy goes **1.74 / 1.06 → 1.28 / 1.04** with 30 of 50 cohort games changed — but read that with care. The shipped roll drew from the one seeded stream on 43 % of the copy's grounder takes (0.19 expected bobbles a game, 8.7 % each when rolled); the routine pickup never draws now, so every later draw in a game — pitches, swings, CPU choices — lands on a different number. Home runs across the 48 games went 50 → 68 on the same 1345 plate appearances, which no bobble rule can do: that is the stream, not the hands. The slice's own physical effect is the loss of those 0.19 energy bobbles a game and the arrival of about 0.01 awkward-hop bobbles; the copy's S-29 is re-based here, and the #719 calibration finding stands.

**Left on #721.** Slice 2: the continuing deflection (F693-02-expanded-ordinary-error-outcomes, -error-outcome-selection,
-continuing-error-*) — the ball getting past the defender at 50–80 % of its speed inside ±15°, chosen by the ball's speed and the
contact, on the shared ground physics. The Unity pass owes the stun pose (`StunT`, where `Bobbling` drove the Miss pose).

---

[#721](https://github.com/jackguillet/grand-sluggers/issues/721) — slice 2, the ball that gets past (F693-02-expanded-ordinary-error-outcomes,
-error-outcome-selection, -continuing-error-reaction, -continuing-error-recovery, -continuing-error-direction, -continuing-error-speed-retention,
-continuing-error-vertical-retention, -continuing-error-ground-response, and the ±15° band of -uniform-error-direction). One file,
`rules/fielding.json`: five new keys in the `handling` block of both roots, none moved — the switch is still `awkwardHop`.

| | both roots |
| --- | --- |
| `handling.deflectObstruction`, `deflectMinFtPerSec` | 0.5, 55 — a failed take gets past when the ring met the ball glancingly (obstruction `1 − dist / window` under 0.5) **and** the ball came in at 55 ft/s or more, the recoil's hot line; a square touch or an ordinary ball is the knockdown (slice 1) |
| `handling.deflectRetainMax`, `deflectRetainMin` | 0.80, 0.50 — what the ball keeps of its horizontal and signed vertical speed, 0.80 for a glancing touch down to 0.50 at the knockdown boundary |
| `handling.deflectSpreadDeg` | 15 — one uniform draw either side of the ball's own travel |

**What the seat gets.** The failed take's outcome is the contact's, never a second roll. `ContinuingDeflection` keeps the ball a
batted ball: `BallFlight.Continue` runs the shared physics — drag, wind, gravity, the bounce, the roll, the walls — from the contact
on the hit's own time scale, the path's samples before it untouched, and `BattedBall.Reread` re-reads what it decides from there
(the wall, the fence). The fumbler is stunned the same 0.40 s; the ball is taken by whoever reaches it — the outfield hand-off
included — and that take never rolls, because the arming is spent. `Deflected` and `ErrorObstruction` are the client's.

**What the CPU does with it.** The CPU takes at the edge of its ring (obstruction 0.00–0.11 on every take in the sweep), so its rare
fumble splits on the ball alone: on the wide-open copy 43 rising takes fail, 11 get past (the hot ones, 56–61 ft/s in) and 32 drop
at the feet. Seed 35 on the test liner — slice 1's local bobble — now gets past: vine's ring meets the ball at its edge at 59.5 ft/s,
the ball keeps 80 % and rises on, 17.7 ft away before vine takes it back 0.40 s later; the batter has a single where the local
bobble gave him a double. The seat's own take at the body is always the knockdown.

**Control unchanged.** 20 `cli match` seeds and all 50 S-29 cohort games byte-identical to pristine `main`; the factored integrator
gives the shipped path byte for byte.

**What it does to a run.** Nothing the CPU played: 0 of 48 trial seeds and 0 of 50 cohort games move against #755 (S-29 stays 1.28 / 1.04). The slice draws no new number — the branch is read off the contact and the speed, not rolled — so the stream is the stream of slice 1, and at one awkward-hop fumble a hundred CPU games none fell in these 48 to be reclassified. The branch shows in `DeflectionTests` on seed 35 and on the wide-open copy, and it will show on the seat.

**With this, every Sim item of #721 is landed or in PR.** The Unity pass owes the stun pose and a tell for the ball that gets past
(`Deflected`; the `Bobble` event fires for both outcomes).

---

**Step 3d — the whole-race measurement (#715), September 18, 2026.** The copy as landed, run through the #702 instrumentation
against the shipped table on the same inputs and seeds: [docs/research-game-feel-3d.md](../../docs/research-game-feel-3d.md). The
finding carried from #719 is now a number: the copy runs under the S-29 floor in every cohort (1.28 / 1.04 mixed parks, 1.22 / 1.38
and 0.92 / 1.20 on Harbor), and `chase.outfieldAirMul` alone — 0.6 in place of this copy's 1.0 — puts every cohort in band
(2.46 / 2.36, 2.10 / 2.16, 2.02 / 2.42); 0.7 lands the away side only; the read at 0.83, the fly stretch and the outfield depth do
not get there on their own. Nothing in this copy changed for the measurement; the variants were scratch overlays. The choice is
Jack's, and it is a choice about the one-profile direction, not a tuning.

---

**The outfield's air multiplier back at 0.6 (#715, Jack's decision of 2026-09-18 on the 3d measurement).** One file, one key:
`rules/fielding.json` `chase.outfieldAirMul` 1.0 → **0.6**, which is the shipped value, so the key no longer differs from the
shipped table. `chase.infieldAirMul` stays 1.0. #719 slice 1 had retired both hit-class multipliers — one pursuit profile per body,
in the air as on the dirt; 3d ([docs/research-game-feel-3d.md](../../docs/research-game-feel-3d.md)) measured what that cost — the
copy under the S-29 floor in every cohort — and that this key alone brings every cohort into the 1.8–5 band.

**What it does to a run.** Every cohort is in the 1.8–5 band, home and away: S-29 **1.28 / 1.04 → 2.46 / 2.36** (40 of 50 games change), Harbor calibration **2.10 / 2.16**, Harbor validation **2.02 / 2.42** — the 3d sweep's numbers to the digit. All 48 trial seeds diverge from #756 (39 scorelines): doubles 21 → 112, triples 0 → 31, home runs 68 → 82, the fly-out rate 39.0 % → 34.9 % on 1345 → 1441 plate appearances. **Control unchanged**: 20 `cli match` seeds and all 50 S-29 cohort games byte-identical to pristine `main`; no Sim source changed and the sealed packets are untouched.

**What it does to the defence.** An outfielder under a ball in the air covers 10.8 ft/s at Run 5 where he covered 18. The earned
dive (#719 slice 2) becomes a routine part of the CPU outfield's coverage: in a sweep of 693 swings the CPU outfield commits on
dozens of liners it used to reach standing, where at 1.0 it committed on almost none. The catch reach, the dive's cost, the recoil
and the handling rules are untouched.

**Fixtures re-derived, not repaired.** Eleven trial tests recorded plays whose outfielder stood somewhere else at 18 ft/s under
the ball; each keeps its claim on a ball that makes the same play at 10.8, with the old ball and numbers on record in its comment:
the dive tests (a 130-mph liner at 18° in place of the 280-ft gap liner, which nobody reaches now; the neutral-stick case on a
120-mph liner at 16°), the airborne recoil's diving catch (the right fielder at 81 ft/s), the landed liner's recoil (130 mph at
10°), the awkward hop (a 100-mph liner at 20° — D 0.97 and under the hot line, so the real-table bobble is the knockdown again),
the ball that gets past (120 mph at 16°, 59 ft/s in), and the jump's reversal anchor, which moves to the shortstop under a pop at
the one speed while the centre fielder under the fly shows the same tenth-of-the-rates brake from 10.8 ft/s (3.24 ft).

---

**The compact scenario rows, slice 1 — the outs (#715, ahead of 3e).** No data changes; this is the test side of the copy.
The diamond is process-wide (`Diamond` reads the one default table), so a scenario row is only honest on this copy in a process
whose data root is the copy. `TestRoot.Compact` says which diamond the process plays, `TestRoot.Pick(shipped, compact)` takes
the row's fixture for it, and classes whose every row holds on both roots carry `[Trait("Rows", "compact")]`; CI runs those a
second time with `GRAND_SLUGGERS_TRIAL=trials/c80`. The shipped rows are untouched and still run on the shipped diamond; each
compact row sits beside its shipped row, shares its assertions, and says in a comment why the 80-ft diamond needs another ball.

`OutsScenarioTests` is the first class through: 37 of 37 on both roots, 19 of them rows the copy had broken.

| Row | Shipped ball | Compact ball | Why |
| --- | --- | --- | --- |
| S-41 4-6-3 | 120 ft / 4° / 5° | 105 / 4° / 9° | the second baseman starts at (37, 105), not (42, 118); the shipped ball is past him |
| S-43, S-48 3 then the tag | 92 / 4° / 41° | 70 / 4° / 43° | the shipped ball is taken 16 ft in front of the 80-ft bag |
| S-44 3-6-3 | 110 / 4° / 38° | 95 / 4° / 38° | at 110 ft the line runs between first and second and the preview names second |
| S-46 5 unassisted then 3 | 92 / 4° / −44° | 85 / 3° / −44° | from the shipped ball the CPU's second out goes to second and the batter reaches |
| S-54 the tag-up race | 222 / 34° / −30° | 250 / 34° / −26° | the runner reads the race (#732) and holds on the shipped fly |
| S-55 the pop dropped on purpose | 120 / 62° / −12° | 107 / 62° / −16°, six neutral frames | the shortstop's pop; the copy's stick takes the glove only after it is seen at neutral (#718) |
| S-55b the lost relay | seed 33, 210 ft | seed 33, 235 ft | the fly deep enough that the runner tags on the race and the relay still loses the ball |
| S-73 the throw well ahead | 90 mph / 30° | 90 mph / 26° | on 80-ft paths the 30° ball leaves the throw inside the margin |
| S-74, S-75 inside the margin | order 7 | order 5 | the Run-5 body the dash lands inside the margin |
| S-76 the rundown | 92 / 4° / 41° | 80 / 4° / 43° | the ball the first baseman takes on the bag |
| The triple play | CPU seat, 92 / 4° / −44° | human seat, 80 / 3° / −44° | three forces are still there; the CPU's table takes the sure out at first after the step on third |
| The rundown AI | 40 ft and 70 ft along the path | the same fractions of 80 ft | the runner covers 27.1 ft/s on the copy, 30.5 shipped |

**Left.** 44 more rows across three families, each its own slice: the control and fielding scenes (the hand-offs, the liner
past the lip, the roller to the grass — 23), the flights, parks and geometry values (fences, carries, the mound, the hazards —
16), and the seats, bunts, runners and steals (5). About a hundred more tests fail under the overlay because they name the
shipped table on purpose; those are not rows and are restructured at promotion.

---

**The compact scenario rows, slice 2 — the control and fielding scenes (#715, ahead of 3e).** Tests only: no Sim source, no
data, no CI change. Eight more classes carry `[Trait("Rows", "compact")]`: `ControlScenarioTests`, `FieldingSceneTests`,
`FieldingPursuitTests`, `FieldingScenarioTests`, `LiveBallScenarioTests`, `PlayTraceTests`, `BodyFacingTests`, `FlyCatchTests`.
102 of 102 on both roots, 22 of them rows the copy had broken; the second CI run is now 139 rows. The shipped rows are
untouched; each compact fixture sits beside its shipped one with the reason.

| Row | Shipped | Compact | Why |
| --- | --- | --- | --- |
| S-96 the roller 2B meets on the grass (both seats) | 190 ft / 5° / 14° | 150 / 9° / 14° | the lip is 137.78 ft and the legs are slower; the high hopper is the one 2B runs down at 138.7 ft (2.06 s) before RF's route (2.75 s) |
| S-97 the liner over SS: the coast | speed x 0.2 s, then a dead stop | the same coast, then the #718 brake | the response law brakes a body instead of stopping it dead; see the finding below |
| S-97 the liner SS reaches past the lip (both seats) | 88 mph / 10° / −18° | 74 / 15° / −18°, planted 132.9 ft, on the dirt | no such ball on the copy; see the finding below |
| S-97 South on the rope at the intercept | the pad runs SS from frame 0 | six neutral frames first | the copy's pursuit stick takes the glove only after it is seen at neutral (#718) |
| The human stick runs the glove at the one speed | stick from frame 0 | six neutral frames first | the same |
| S-35 bad chemistry across 100 seeds | 8 to 40 errors, each a slanted throw | 0 errors, no slant, the pair's part x0.90 | the copy's bad pair is slow, not random (#722) |
| Bad chemistry slants a share of throws | 12 to 30 % slanted, the rest x1.0 | 0 slanted, the rest x0.90 | the same |
| The close play at third against the CPU glove | 90 mph / 3° / 30° | 90 / 3° / 36° | at 30° the second baseman's route meets it first on the 8/9 infield |
| The traced grounder: glove meets ball | under 1 ft on the take's tick | inside the 6 ft reach on the take's tick, under 1 ft the next | the copy takes the ball from the stand-up reach (#719) |
| #576 the glove under a fly does not spin | 250 / 335 / 270 ft | 175 / 235 / 190 ft | CF stands at 213.5 ft under a 280 ft fence; the same three balls at 0.70 |
| The gap liner that rolls to the wall is CF's | 90 mph, 10° to 18° | 78 mph, 8° to 12° | the shipped ball at 14° beats CF to the wall in every park (13 to 26 ft short); CF is still the choice |
| Ground-ball routes are reachable within the slack | 0.02 s | `reachSlackFt` over the speed | 0.35 ft of slack is more time at the copy's legs |
| The play glove hands the hop to the outfield | balls at 120 / 140 / 200 ft | 107 / 124 / 178 ft | the lip and the infield depth at 8/9 |
| The outfielder charges the ball in the grass | (−80, 180) | (−56, 126) | LF starts at (−77, 175), on top of the shipped ball |
| The glove chases the landing on a fly | a 280 ft fly | 196 ft | 280 ft is Harbor's fence on the copy |
| Field bounds use each park's fence | 400 / 378 / 408 | 280 / 265 / 286 | the fences at 0.70 (#717) |
| The near-wall flight meets the wall | 110 mph at 35° | 111.7 mph | drag 0.0040 and a 280 ft fence: 110 mph dies at 277 ft |
| The catch ring: stand-up, rim, dive | 10 + 0.6 x Field | `standUpReachFt` 6.0 | the one authored reach (#719) |
| Dive and jump extend the window | jump reach 8 ft | 0 | the copy's jump is a leap of the body (#719) |

**Two findings, reported and not repaired.**

1. **No liner past the lip for the shortstop.** Across 1 257 liners planted within 10 ft past the copy's lip, SS's route reaches
   none; the nearest misses by 2.8 ft, and the deepest rope SS reaches plants 132.9 ft out, 4.9 ft short of the lip. The #636
   in-air guard (a position-only hand-off must not take a rope SS still reaches) has nothing to guard on the copy with this
   roster. The compact row keeps the behaviour half (never handed off, SS catches it) on the deepest rope, and a small grid
   fails the day a ball past the lip exists, so the real row comes back then.
2. **The hand-off coast and the idle brake overlap (#718, trial only).** The body the ring leaves is not stepped on the hand-off
   frame, so the next frame's idle brake steps it once (0.23 ft) on top of the coast's first step: one frame at 30.9 ft/s in a
   16.9 ft/s coast, 3.61 ft travelled where speed x coast is 3.38. After the coast the body stands for two frames, then brakes
   over 0.1 s. Nothing teleports (the largest step is 0.52 ft) and the shipped table is not touched (`ResponseLaw` is off
   there). The compact row's bounds hold with and without the overlap.

**Left.** 22 rows in two families: the flights, parks and geometry values (16: `MatchTests` hazards, `AtBatTests`,
`HarborWallTests`, `BallFlightTests`, and one each in `PitchTests`, `NightTests`, `FlightScenarioTests`, `FeelInfraTests`,
`BroadcastHudTests`, `AtBatFeelTests`) and the seats, bunts, runners and steals (6). 122 tests fail under the overlay today;
the other hundred name the shipped table on purpose and are restructured at promotion.

---

**The compact scenario rows, slice 3 — the flights, parks and geometry values (#715, ahead of 3e).** Tests, docs and one CI
filter: no Sim source, no data. Ten more classes carry `[Trait("Rows", "compact")]`: `MatchTests`, `AtBatTests`,
`HarborWallTests`, `BallFlightTests`, `PitchTests`, `NightTests`, `FlightScenarioTests`, `FeelInfraTests`, `BroadcastHudTests`,
`AtBatFeelTests`. 13 of the 16 rows the copy had broken now hold on both roots; the other 3 are two named gaps (below). The
second CI run is 299 rows.

| Row | Shipped | Compact | Why |
| --- | --- | --- | --- |
| S-59 the ground-rule double | 101 mph at 32°, stick from frame 0 | 101 mph at 44°, six neutral frames first | drag 0.0040 brings the 32° fly down too flat to hop the 12-ft wall; only flies from 42° up do. Without the neutral frames the copy's pursuit stick (#718) never takes CF and the CPU catches the fly |
| Carry of the 95 mph / 28° fly (three rows) | 300 to 450 ft | 210 to 315 ft | it carries 233 ft on the copy; the same band at 0.70 |
| The fastball at halfway | 26 to 34 ft | 23 to 30 ft | the rubber is at 53.78 ft; the same band at 8/9 |
| Harbor and Ember fences | 400 / 408 | 280 / 286 | the fences at 0.70 (#717) |
| Ember's lava pit, the Rink's freeze volume, Rooftop's star sign | (49, 100), (53, 93), (−80, 240) | (44, 89), (47, 83), (−56, 168) | the hazards at the field's scale (#732); the pit and the volume were (38, 78) / (34, 69) and (40, 70) / (36, 62), on the first–second lane, until FD-19-R1 moved them outward along their own bearings (F4-e, #862) |
| Ember's fire breath at night | 250 ft and 270 ft | 175 ft and 189 ft | the mouth at 0.70, the same 20 ft past it at 0.70 |
| Runner pips along the path | 45 ft is half, 30 ft a third, 9 ft overrun is 1.1 | 40 ft, 26.67 ft, 8 ft | the 80-ft path |
| The fielder's dash | `dash.chaseMul` above 1 | exactly 1.0 | the copy's dash is the Ball Dash carrier's, no free chase multiplier (#718) |

**Two gaps, reported and not repaired.** Each is a value in feet the copy does not carry. Their rows are tagged
`[Trait("Copy", "gap")]`; the copy's CI run is `--filter "Rows=compact&Copy!=gap"`, so they stay red under the overlay, by name,
until promotion decides them. `dotnet test --filter "Copy=gap"` under the overlay lists them.

1. **The mound camera.** `data/feel/shots.json` authors the mound shot at z 72, 11.5 ft behind the shipped rubber. The copy's
   rubber is at 53.78 ft, so the over-the-shoulder window there is 61.78 to 69.78 ft and the camera stands 2.2 ft behind it
   (18.2 ft behind the pitcher). An overlay copy of `feel/shots.json` could move it; where it goes is a look decision.
2. **The foul rail's taper.** `HarborWall` starts the rail's ramp at a literal 95 ft (`hipZ`, `flareStart`), which no overlay
   can move. On the copy's 0.70 lines the ramp is shorter: the two 8-ft parks (`funfair-park`, `crystal-rink`) get 4 taper
   vertices where `TaperIsARamp` asks for 6 (7 and 6 shipped); the other four parks still pass. Presentation only.

Also seen: ground-rule doubles are rarer on the copy. In the same probe grid Harbor gives 338 where the shipped table gives
1 382, because the steeper descent under the higher drag seldom hops a 12-ft wall.

**Left.** 6 rows: the seats, bunts, runners and steals (`SeatOwnershipTests` 2, `BuntScenarioTests` 2, `StealScenarioTests` 1,
`RunnerScenarioTests` 1). 109 tests fail under the overlay today: those 6, the 3 gap rows, and the hundred that name the
shipped table on purpose and are restructured at promotion.

---

**The hand-off coast runs straight into the brake (#718 slice 2's defect, found by the slice-2 rows; trial only).** Finding 2
of slice 2, repaired. The body the ring leaves is not stepped on the hand-off frame, so the response law's idle brake took it for an
idle body at the top of the next tick and stepped it (0.23 ft at 16.9 ft/s) before the coast stepped it too; and the coast's
clock ended on float dust (0.2 − 12/60 ≈ 5e-17), so a thirteenth, empty coast frame marked the body stepped and the brake came
two standing frames late. Three changes, all inside the `ResponseLaw` branches of `LivePlaySystem.Field.cs`:

- `TickIdleBrakes` skips `Coasting(pos)`: the coast is that body's step.
- The coast's last step (clock ≤ 1e-9) ends the coast and names the body for `TickCoastBrake`, which runs after the walks
  (`TickHandoffCoast`, `ChargeBunt`, then it): what the coast left of its last frame is braked at once, and on every later
  frame the body brakes unless a walk has already stepped it that frame, the ring is back on it, or it is at rest. The walks
  that waited for the coast still take the body at the velocity it has — a cover walk that wants it on the first frame after
  the coast gets it with one step, not the idle brake's step and then its own.
- `BrakeStep` is the idle brake's body, shared by both.

SS across the hand-off in S-97 on the copy (ft moved per frame, h = the hand-off frame):

| Frames | Before | After |
| --- | --- | --- |
| h | 0 | 0 (as shipped: nobody steps the body the ring left on that frame) |
| h+1 | **0.516** (30.9 ft/s) | 0.281 (16.9 ft/s) |
| h+2 … h+12 | 0.281 | 0.281 |
| h+13, h+14 | **0, 0** | 0.234, 0.188 |
| then | 0.234, 0.188, 0.141, 0.094, 0.047, 0 | 0.141, 0.094, 0.047, 0 |
| Coast (h → h+12) | 3.610 ft | **3.376 ft** = 16.88 ft/s × 0.2 s |
| At rest | h+20, 4.314 ft on | h+18, 4.079 ft on |

At an uneven frame the coast's last frame is part coast, part brake (at 0.021 s: nine frames at 18.0 ft/s, then 17.1, 12.4, 8.6,
4.9, 1.1, 0). A probe of 1 700 CPU balls on the copy, each at both frame times, found 234 hand-offs from a moving body: none with a
frame faster than the coast, none with a standing frame before the brake.

**Tests.** The compact half of S-97 now pins the exact coast (speed × coast within 0.05 ft, every coast frame at the coast's
step to 0.001 ft) and the brake frame by frame from h+13 (`speed − k × Frame × rated / brakeSec`, then rest) in place of bounds
that held with and without the overlap. `ResponseLawTests.TheBodyTheRingLeavesCoastsThenBrakesWithNoFasterFrameAndNoStandingFrame`
runs a CPU hand-off (SS → CF, 90 mph / 10° / −8°) at 1/60 s and at 0.021 s in the ordinary test run; both fail on the old code
(28.9 and 28.2 ft/s frames). 1 320 tests pass; 299 of 299 compact rows under `GRAND_SLUGGERS_TRIAL=trials/c80` (the CI filter, after slice 3); the
overlay's count of failing tests is unchanged at 109.

**Control unchanged.** `ResponseLaw` is off on the shipped table and nothing outside its branches moved: 20 `cli match` seeds
and all 150 cohort games (S-29, Harbor calibration, Harbor validation) byte-identical to pristine `main` (the cohort JSON differs
only in the build's `moduleId`). The sealed packets regenerate with one line moved, the sha of `LivePlaySystem.Field.cs`.

**What it does to a run.** Almost nothing the CPU plays: **1 of 48 trial seeds diverges from #760 (1 scoreline; #762 moved no Sim source and no data)** and 0 of 150
cohort games — S-29 stays **2.46 / 2.36**, Harbor calibration **2.10 / 2.16**, Harbor validation **2.02 / 2.42**. Every game's
bodies differ from the first hand-off on (0.2 to 0.3 ft where the coasting body comes to rest), and one play in 48 games sat on
that edge: seed 41, bottom of the first, a ball to right with two on. 2B hands the ring to RF at 1.72 s, coasts, and stands
0.27 ft from where it stood before; it takes RF's relay at 7.3 s either way, and where the old run played on to a double
(a 45-second play) the new one has 2B tag Grit at 14.35 s. Across the 48: singles 181 → 180, doubles 129 → 126, fly outs
448 → 446, ground outs 192 → 194; triples 38, home runs 78, strikeouts 184 and walks 143 do not move.

**One finding, reported and not repaired.** A hand-off on the frame after a dive's lunge coasts the diver at the lunge's
velocity (trial only, #719's deliberate dive under #718's §8.9 coast): `_gloveVel` is the glove's displacement over the last
frame, the CPU's lunge is 9.9 ft in that frame, so the body the ring leaves slides 9.9 ft a frame for 0.2 s — about 118 ft at
590 ft/s — and then brakes from there. Seen on the copy on 70 mph / 20° flies at sprays −32°, −8° (SS → LF) and 0° (2B → CF),
3 of those 1 700 balls. It is the coast's velocity source, not the brake, and it is its own change.

---

**The compact scenario rows, slice 4 — the seats, bunts, runners and steals (#715, ahead of 3e).** Tests and docs only: no Sim
source, no data, no CI change. The last four classes carry `[Trait("Rows", "compact")]`: `SeatOwnershipTests`,
`BuntScenarioTests`, `StealScenarioTests`, `RunnerScenarioTests`. 74 of 74 on both roots, 6 of them rows the copy had broken, and
S-71 re-based with them. The second CI run is 373 rows.

| Row | Shipped | Compact | Why |
| --- | --- | --- | --- |
| S-36 the runner on second holds on a grounder in front of them | 90 ft / 5° / −35° | 80 / 5° / −35° | the shipped ball runs through the hole between third and short (LF picks it up at 4.0 s, a single); the same grounder at 8/9 of the carry is 3B's |
| S-93 in versus the defense's stick takes the glove | stick from frame 0 | six neutral frames first | the copy's pursuit stick takes the glove only after it is seen at neutral (#718) |
| The squared bunt to third, human seat | stick from frame 0 | six neutral frames first | the same; without them the first human frame is the CPU's own pickup |
| The square brings the middle to the bags | the flat 28 ft/s, settled by 2.5 s | `CoverSpeedFt` of the body, settled by 3.0 s | a cover walks at the body's own speed (#718): 2B's 51 ft takes 2.7 s |
| S-99 Select on a sailed pickoff | Vale, the bad pair's slant | Vale with an authored Arm of 1 | finding below; and "stands still" is within 1e-9 ft, the brake's last unit in the last place |
| S-71 a pickoff that sails | the same | the same, and the sail must be the pickoff's own | finding below |

**One finding, reported and not repaired. No pickoff sails on the copy.** With `chem.slantChance` 0 the only miss is the
thrower's own spread, and Vale's (Field 8, sigma 1.05 ft) never leaves the 6 ft cover: 0 pickoffs of 400 sail on the copy (a throw
sails in 90 of the same 400 plays on the shipped table). What sailed in S-71's loop on the copy was first base's throw on to second (Ashlord, Field 3: 12 of 400), so the
row passed for a throw it does not name. Both rows now give the copy's pitcher an authored Arm of 1 (sigma 3.5 ft), and S-71
asks that the sail be the pickoff's own. Whether a pickoff should ever sail on the copy is a 3e question; today it needs a wild
arm the roster does not have on the mound.

**Done.** Every scenario row that the copy had broken now holds on both roots or is a named gap: 63 rows across 23 classes in
four slices, 373 tests in the copy's CI run. 103 tests fail under the overlay today: the 3 `Copy=gap` rows (the mound camera,
the foul rail's taper) and 100 that name the shipped table on purpose (`CompactGeometryTests`, the 3c slice tests with
`Control = ContentCatalog.Load()`, `InfieldGeometryTests`, `TrialOverlayTests`, and the like). Those are restructured at
promotion, when the copy becomes the table they name.

---

**The diver does not coast (#715, found beside the hand-off coast / idle brake overlap).** One guard in `HandGloveTo`
(`LivePlaySystem.Field.cs`): a body in its dive keeps no coast when the ring leaves it. "In its dive" is the recovery the copy's
diver still owes (`DiveRecoveryT > 0`, `DivingPos`), and, on every table, the dive's arm window (`DiveT > 0`) for the body that
lunged (`_lungePos`). No data change. The second half repairs the shipped table too, on Jack's word (2026-09-18).

**The defect.** The coast (§8.9) keeps "the glove's velocity": the last frame's displacement over the frame. Under the copy's
deliberate dive (#719) that last frame can be the lunge, 9.8 ft in one frame, which reads as about 590 ft/s. When the dive
misses and `TryHandoffOutfield` moves the ring on the next frame, the diver slid 9.87 ft a frame for the coast's 12 frames
while his recovery was still running, then "braked" from 590 ft/s under the response law (#718). This is the finding the
section above left as its own change; the numbers here are measured on main after that section's repair.

| Fixture (CPU seats, Harbor, the `ResponseLawTests` teams) | Root | Ring | Before | After |
| --- | --- | --- | --- | --- |
| 70 mph / 20° / −8° | the copy | SS → LF, frame 140 | SS ends behind the plate; the three copy rows are 146.1 to 152.0 ft from the lunge | under 1.5 ft |
| 70 mph / 20° / −32° | the copy | SS → LF | the same | under 1.5 ft |
| 70 mph / 20° / 0° | the copy | 2B → CF | the same | under 1.5 ft |
| 80 mph / 14° / −18° | the hybrid (plain process) | SS → LF, frame 51 | 151.0 ft | under 1.5 ft |

**The shipped table had the same slide with a human seat.** The shipped CPU cannot produce it: its dive is free, automatic and
always takes the ball on the lunge frame, and the only hand-off after that is the throw's release, which has no coast (0 of
1 456 CPU plays). A human could: East lunges 10 ft with no recovery, and a hand-off with a coast on the next frame carried the
lunge as a velocity.

| Human glove, 80 mph / 14° / −18°, shipped table | Before | After |
| --- | --- | --- |
| Stick runs SS, East, then the stick released (the assistance hands SS → LF on the next frame) | SS slides 9.54 ft a frame for 12 frames: 124.0 ft with the lunge, then a dead stop | no step over 1 ft |
| East on LF, then Select on the next frame (LF → CF) | LF slides 10.21 ft a frame: 132.8 ft with the lunge | no step over 1 ft |

What changes for a shipped human: a body that dove, and whose ring leaves inside the 0.5 s arm window, stops where it is; it
no longer coasts 0.2 s. That is also true if he ran on after the lunge (the shipped dive owes no recovery), at his own speed.
The window is the diver's own: a body that takes the ring after a dive still coasts when the ring leaves it.

`DiveHandoffCoastTests` (`[Trait("Rows", "compact")]`, root-aware) holds the four CPU rows above, East-then-Select on this
process's table (LF on the shipped diamond, SS on the copy), and the released stick on the shipped table. Every row fails
without its half of the guard.

**Evidence.**

| Check | Result |
| --- | --- |
| `dotnet test GrandSluggers.sln -c Release` | 1323 of 1323 (1320 on main + these 3) |
| `--filter "Rows=compact&Copy!=gap"` under the trial root | 378 of 378 (373 on main + these 5) |
| Control, 20 seeds and the three cohorts | byte-identical to main (cohort JSON: the identity block's `moduleId` and `build` only); CPU games never hand a ring off inside a shipped dive |
| Trial, 48 seeds | 0 of 48 move |
| Trial cohorts | identical to main but for the identity block: S-29 2.46 / 2.36, harbor-calibration 2.10 / 2.16, harbor-validation 2.02 / 2.42 |
| Grid, 1 456 CPU plays on the copy | 111 dive commits, 3 rings left a recovering diver; off-ring steps over 1 ft a frame: 3 before, 0 after (worst 0.58 ft) |
| The same grid on the hybrid | 39 commits, 8 such hand-offs; 0 after |
| Sealed packets | one sha line moved (`LivePlaySystem.Field.cs` in `game-feel-708-derived.json`); all three `--check` pass |

The path is live in real games: in the 150 cohort games on the copy the ring left a recovering diver 15 times, 13 of them at
557 to 598 ft/s. No result moved, because the diver owes his recovery and is in no cover, cutoff or backup while he slides. It
was a picture defect: a body crossing the infield in a fifth of a second.

---

**The 3e deferred-work boundary (#715): two more scope calls, and the park's slow held on both roots.** Jack, September 18, 2026,
recorded in `docs/plan-game-feel-693.md` beside the specials exclusion. No Sim source, no data.

- **Movement/status interactions.** The ordinary piece is resolved by rows; the special-sourced statuses (burn, charm, a
  special's freeze, an obstruction, cleansing, immunity, and the duration / refresh / stacking rules) are excluded from 3e with
  the specials.
- **Throw-before-cover.** Excluded from 3e. The rule is the same on both tables (`throw.lobMaxSec` 1.5 s: the ball hangs at an
  uncovered bag, then drops, live). The copy changes the cover, not the rule: second base needs 2.7 s to reach first on a squared
  bunt against 2.05 s shipped, so a human's early throw waits longer. It is on the Unity sitting list.

`ParkSlowRowsTests` (`[Trait("Rows", "compact")]`, 4 rows, both roots) holds the one status the ordinary loop carries,
`chase.frozenMul` 0.45, against the copy's new movement:

| Row | Shipped | Compact |
| --- | --- | --- |
| The slow is one multiplier, and Burrow ignores it | 0.45 | 0.45 |
| A fly into the Rink's deep freeze volume, CPU seat and human seat | lands at (10, 187); the chaser tops out at 0.45 of its Harbor speed | lands at (7, 131); the same, after the response law's ramp and the stick's six neutral frames. The volume was (10, 180) / (7, 126), on the copy's second-base pad, until FD-19-R1 (F4-e, #862) |
| A grounder into Ember's lava pit: what the take costs | lands at (49, 100); the knockback of the hit's energy | lands at (44, 89); `RecoilSec` of the ball's incoming speed (#720). No term for the slow in either. The pit was (38, 78) / (34, 69) until FD-19-R1 (F4-e, #862) |

The copy's CI run is 382 rows.
