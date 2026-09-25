# Fields: a reference-informed design discussion

> **Historical.** A finished report, kept for its evidence and reasoning; the contract is [gameplay-spec §0.3 and §14](../../spec/14-parks.md) and [the decision plan](../../decisions/plan-fields.md). Where they disagree, the contract is right.

Research date: September 21, 2026 (America/Los_Angeles). Session kind: **Gameplay research/documentation**. Inspected revision: `d0c6e12c`. This report accompanies the [decision register](../../decisions/plan-fields.md). It changes no runtime rule and accepts no mechanic or number.

Jack's brief: treat Harbor as the default. Give the other fields a unique look, possible hazards, and qualities such as size, air density, ground material and slickness. **Build the rails and the engineering process before the artwork.**

Companion records: the [sim code map](../../research/fields-code-map-sim.md), the [presentation code map](../../research/fields-code-map-presentation.md), and the [measured per-park baseline](../../research/fields-park-baseline.json).

## Evidence and limits

Labels: **P** primary (official site, rule text, physicist-authored or peer-reviewed, first-party data). **C** community wiki or datamine. **R** press or analysis. **SNIPPET** the page did not load and the fact came from a search result. **UNVERIFIED** no readable source.

Read in full: every Super Mario Wiki stadium page for both Mario baseball games (C); the Mario Superstar Baseball community datamine page for stadiums (C); three of Alan Nathan's physics pages (P); the Penn State surface-pace paper (P); the Baseball Savant park-factor table, 2023–2025 (P); MLB.com's field-dimension glossary and one Statcast wind article (P).

Not readable: both Nintendo booklets (image-only PDFs), so **no manual statement about stadiums is verified**. The Official Baseball Rules PDF. MLB.com ground-rule pages. GameSpot, IGN, GameFAQs and Fandom pages. The Super Mega Baseball dimension guide (numbers exist only as images).

Not found anywhere: Sluggers (Wii) field dimensions, wall heights or surface constants. A measured wall restitution by material. A measured wet-surface ball speed. A measured rolling deceleration for grass against turf. A measured twilight effect on strikeouts.

No reference play session, video measurement or Harbor sitting was done for this report.

## Mario Super Sluggers — Wii, 2008 (C)

Nine stadiums and Toy Field. Six have day and night forms that change play. One is day only, two are night only. Mario Stadium's night is a look only. [MW-MSS]

| Stadium | Gimmick | Acts on | Place | Learnable? | Source |
| --- | --- | --- | --- | --- | --- |
| Mario Stadium | none | — | — | — | MW-MS |
| Peach Ice Garden | Freezies: touch one and the fielder freezes for a short time; enough force breaks one | fielder | on the field | fixed, visible | MW-PIG |
| — night | ceiling stars: a batted ball that touches one blacks out the park; spotlights find the players, then the ball | visibility, fielding only | ceiling | fixed target; the hit triggers it | MW-PIG |
| Yoshi Park | pipes: a ball enters one and leaves another chosen at random | ball on the ground | shallow | entry fixed, **exit random** | MW-YP |
| — both | a train circles the field; no effect on play is documented | — | perimeter | — | MW-YP |
| — night | plants fill the pipes and try to eat the ball | ball | pipe spots | fixed | MW-YP |
| Wario City | manholes shoot water now and then and knock a fielder down | fielder | on the field | fixed place, random time | MW-WC |
| — both | arrows: a ball that lands on one bounces the arrow's way; farther at night | ball | outfield | **fixed, fully learnable** | MW-WC |
| DK Jungle | barrel cannons roll barrels across the outfield (flaming at night) | fielder | outfield only | moving, told by the cannon | MW-DKJ |
| — both | roots slow the ball; gas flowers put a fielder to sleep | ball; fielder | center; outfield | fixed | MW-DKJ |
| — night | a statue stuns the whole outfield now and then | fielders | all outfield | **random, no counter** | MW-DKJ |
| Bowser Jr. Playroom (day only) | floor pictures: a ball that hits one spawns a chaser that stuns the fielder unless outrun | fielder | outfield | fixed triggers | MW-BJP |
| Bowser Castle (night only) | lava bubbles, fire puddles, a fire-breathing statue, thrown bombs; wall blockers drop into cutouts | fielder; ball at the wall | pits, center, wall | mixed | MW-BC |
| Luigi's Mansion (night only) | gravestones attack a fielder who comes near; tall grass hides the ball | fielder; visibility | outfield | fixed, trigger range not drawn | MW-LM |
| Daisy Cruiser | day: tables break when a ball hits them and stun a sliding fielder. Night: bouncing fish; near the middle of the game the whole deck tilts | ball, fielder; whole field | outfield | fixed; one clock event | MW-DC |

**Patterns.** Almost every hazard is in the **outfield**. Most act on the **fielder**, not the ball. Night adds or strengthens a hazard in five of the six two-form parks. No page documents a runner hazard, an input-disruption hazard, a hazards-off option, or a per-park surface number. Walls do differ: Playroom's are very high, Daisy Cruiser's are short and give ground-rule doubles.

## Mario Superstar Baseball — GameCube, 2005 (C, datamine)

Six stadiums, no day and night. The datamine shows that **parks differ in numbers, not only in gimmicks**. [MH-STAD]

| Stadium | Lines / center | Wall | First bounce, horizontal | Rolling factor |
| --- | --- | --- | --- | --- |
| Mario Stadium | 80.5 m / 100 m | regular | 0.66 | 0.999 |
| Peach Garden | ≈ 83.5 m / 87 m (heart shape: center is closer than the alleys) | 8 m, castle 18–23 m | 0.67 | 0.996 |
| Wario Palace (sand) | ≈ 73 m / 73 m | 5 m | 0.60 | 0.990 |
| Yoshi Park | ≈ 82 m / 93 m | 3.2 m | 0.64 | 0.995 |
| DK Jungle | ≈ 72 m / 97 m | 3.5–6.9 m | 0.65 | 0.996 |
| Bowser Castle | ≈ 75.5 m / 88.5 m | 16.8 m → 12 m → 5 m cutouts | 0.70 | 0.999 |

Spread: lines 16 %, center 37 %, wall height 7×. The second-bounce vertical factor is the same everywhere. The datamine notes that the sand makes bunts better at Wario Palace.

The GameCube hazards often act on the **ball** and can change the result outright: blocks that turn a foul fair or a pop-up into a home run, plants that spit a fly over the fence, a tornado near the bases. Four parks give stars for hitting a feature. Barrel cannons fire only when a ball is in the outfield, never both in one play, and never roll into the infield. Lava fireballs cycle fixed landing spots. The ranked netplay community keeps gimmick parks legal and controls them with one stadium ban per match (SNIPPET). [MH-STAD, PR-RULES]

**What changed between the two games.** Sluggers added day and night forms with play changes. It moved most hazards from the ball to the fielder. It dropped the documented star targets. It added one clock event (the tilt) and one visibility event (the blackout).

## Other baseball games

| Game | Levers the parks use | Source |
| --- | --- | --- |
| Super Mega Baseball 3 / 4 | Fence distance, asymmetry, wall height, theme. **No hazards.** Dimensions are shown before play. Press names hitter parks and pitcher parks. No readable number table | SMB-TG (R), SMB-XBL (C) |
| MLB The Show (22–26) | Distance, asymmetry, wall height, **elevation** (6 ft to 5,281 ft, thinner air carries farther), roof, **wind** with flags. The Stadium Creator edits fences, foul territory and elevation, and forbids blocking the batter's eye | SHOW-SZ, SHOW-SC (C) |
| Backyard Baseball | Size, shape, **surface** and **in-play obstacles**: pavement with a high bounce and buildings that knock balls down; sand that slows fielders, runners and the ball | BYB-WP (C), BYB-OS (R) |
| Super Baseball 2020 | Rule zones on one field: stop zones that kill a ball, jump zones at the fence, mines later in the game | SB2020 (C) |
| Wii Sports | One field | WII (SNIPPET) |

Realistic games vary **geometry and air**. Kid and arcade games vary **surface and obstacles**. Only the Mario games and Super Baseball 2020 put **active hazards** on the field.

## Real baseball: what a venue changes, in numbers

| Lever | Number | Source |
| --- | --- | --- |
| Rule floor | 250 ft minimum fence; parks after 1958: 325 ft lines, 400 ft center, with exemptions. The rules set no wall shape, height or material | MLB-FD (P), WP-FIELD (C) |
| Fence range | Lines 302–350 ft and center 408–415 ft today; 258 / 483 ft at the old Polo Grounds. Fair area varies only about ±6 % | WP-GM, WP-POLO (C), FG-FOUL (R) |
| Wall height | 3 ft to 37 ft 2 in; about 8 ft is typical. A tall close wall makes doubles (Fenway doubles index 122) | SI-WALLS (R), SAVANT-PF (P) |
| Asymmetry | A 314-ft right field gives a home-run index of 119 at a run index of 100. A 415-ft alley gives triples 122, home runs 81 | SAVANT-PF (P) |
| Air density | Denver air is about 82 % of sea level. A 401-ft fly gains 5.9 ft per 1,000 ft of elevation and 3.3 ft per 10 °F. Denver carries about 5–7 % farther, and the gain grows with exit speed. Humidity is nearly nothing (+0.9 ft per +50 % RH) | NATHAN-DEN, NATHAN-SC (P) |
| Wind | About 3.8 ft per mph blowing out. A headwind costs more than a tailwind gives (5 mph: −5 % against +4 %). Extremes measured: −82 ft and +56 ft | NATHAN-SC, NATHAN-CARRY, MLB-WIND (P) |
| Ground pace (speed kept through a bounce at 25°) | grass 0.445; dirt 0.44–0.59 by compaction; infill turf 0.51–0.56; old Astroturf 0.60–0.63 | BROSNAN (P) |
| Wall material | No measured restitution found. Qualitative: hard flat walls bounce true; arches, ladders and doors bounce oddly | WP-GM, WP-ORACLE (C) |
| Foul territory | 19,300 ft² to over 40,000 ft². The largest gave about one extra foul out per game | FG-FOUL, FG-FOULHFA (R) |
| In-play obstacles | Hills, flagpoles, monuments, bullpens, catwalks and ivy have all been in play. Ground rules settle them: a lodged ball is two bases | WP-GR, WP-DAIKIN (C) |
| Light | Twilight and a pale sky hide the ball. Day against night moves a called-strike probability by up to 5 points in one study. No measured strikeout effect found | THT-TWI (R) |
| **Park factors** (100 = average) | Runs 83–125. Home runs 76–127. Doubles 86–122. **Triples 52–204.** 22 of 28 parks are within 94–106 on runs. Coors is 125 on runs and only 105 on home runs: its runs come from hits in a big outfield | SAVANT-PF (P) |

**Scale for design.** In a real league a big park effect is 15–25 % on runs and about 25 % on home runs. Most parks are within 6 %. Single event kinds move far more: triples from half to double.

## Hazards in party sports games: what players accepted

| Fact | Source |
| --- | --- |
| Every series surveyed keeps a no-gimmick venue | MW-MS, MTA, MSC |
| Mario Power Tennis, Mario Tennis Aces and Smash Ultimate have a hazards-off switch. Strikers Charged has it only as an unlock. Aces had to patch in a second switch for one court's mast | MPT (SNIPPET), MTA, SSB (C) |
| Strikers: Battle League removed stage hazards. One review praised the purity; another called the stadiums too plain | MSBL (SNIPPET) |
| Smash communities ban stages whose hazards interfere with results at random. The GameCube baseball netplay community keeps learnable gimmick parks legal | SSB, PR-RULES (C) |

Author's reading (opinion): the complaints are about **lost visibility**, **random ball output**, and **stuns with no counter**. Fixed geometry that a player can learn gets the praise. Surface zones draw no complaint at all: they read as baseball and they change tactics. The accepted shape is hazards on for party play and off by one switch for serious play.

## Lever taxonomy

| # | Lever | Who uses it | Acts on | Real number? |
| --- | --- | --- | --- | --- |
| 1 | Fence distance | MSB, SMB, The Show, Backyard, MLB | home-run rate, outfield to cover | yes |
| 2 | Asymmetry | MSB, SMB, MLB | handedness, triples | yes |
| 3 | Fence height | MSB (3.2–23 m), MSS, SMB, MLB | homer or double, robs, bounce-over doubles | yes |
| 4 | Wall material and shape | MLB, MSB cutouts | carom | no |
| 5 | Air density | The Show, MLB | carry, pitch break | yes |
| 6 | Wind | The Show, MLB | carry and drift | yes |
| 7 | Ground bounce | MSB, Backyard, MLB turf | hop height and speed | yes |
| 8 | Ground roll | MSB (0.990–0.999), MSS roots, Backyard sand | grounder speed, bunts, balls to the wall | no |
| 9 | Body traction by ground | Backyard sand; MSS ice is disputed | range, time to base | no |
| 10 | Foul territory | MLB, The Show creator | foul outs | yes |
| 11 | Passive obstacles | MLB, Backyard, MSS tables, MSB blocks | carom, fielder path | rules only |
| 12 | Slope | Tal's Hill, MSS tilt | roll, footing | geometry only |
| 13 | Ball redirects | MSS pipes and arrows, MSB plants | ball path, sometimes the result | — |
| 14 | Catch stealers and wall blockers | MSB and MSS | the near-homer | — |
| 15 | Fielder status, fixed | MSS Freezies, flowers, puddles, gravestones | control for a short time | — |
| 16 | Moving or timed hazards | barrels, fireballs, manholes | fielder, ball | — |
| 17 | Runner hazards | **none found** in normal play | — | — |
| 18 | Visibility | MSS blackout and grass, MSB hedges, MLB twilight | tracking the ball | weak |
| 19 | Time form (day, night, clock event) | MSS | which hazards exist, how strong | — |
| 20 | Reward targets | MSB only | star meter | — |
| 21 | Ground rules | MLB, MSB lava pits, MSS short walls | dead-ball results | yes |
| 22 | Input disruption | **none found** in any game | — | — |

## What this suggests for Grand Sluggers

These are **author recommendations**, pending Jack's decisions.

1. **Make parks differ in numbers first.** The GameCube game varied fence, wall, bounce and roll per park, and real baseball varies air, wind, wall and foul ground. These levers are learnable, they need no new verb, and they draw no complaints. They are also what Jack named: size, air, ground (FD-03, FD-05, FD-06, FD-07).
2. **Declare each park's intent, then measure it.** Real parks have a direction (more triples, fewer home runs). Today's parks already move runs by up to 1.35× and nobody chose that. State the direction, bound the size, measure against Harbor (FD-02, FD-13).
3. **Hazards are geometry a player can learn.** Fixed place, told effect, a route around it, no roll. Drop random exits and play-wide penalties (FD-08).
4. **Keep the infield lanes clean.** No reference puts a hazard on a basepath (FD-19).
5. **Build a few hazard patterns, not many hazards.** The catalog above reduces to about seven patterns. Two planned parks need patterns with no precedent (input disruption) or a poor record (lost visibility); leave them for later (FD-09).
6. **Give players the switch.** Later games all added hazards-off. If hazards are a list of instances, the switch is an empty list (FD-10).
7. **Night is a declared rule layer.** That is how the reference uses it. Crystal's tighter contact window has no source and acts on the at-bat, the part of play no reference hazard touches; review it when night blocks are written (FD-11).
8. **Rails before looks.** One geometry owner, one field kit with slots, a greybox drawn from data, and look gates that can name a park. No mesh before a park's rules and greybox have had a sitting (FD-16, FD-17).

## Harbor baseline

The [sim map](../../research/fields-code-map-sim.md) and the [presentation map](../../research/fields-code-map-presentation.md) hold the `file:line` detail. This is a snapshot of existing behavior, **not an endorsement of it**.

**What a park can change today.** Three fence posts, `fenceHeightFt`, `windMph`, `windDeg`, `nightContactWindowMul`, and `hazards` (`Models.cs:186-211`). `surface` is read only by the validator and by presentation. `notes`, `nightOnly`, `dayOnly` and the train's `periodSec` are in no C# type and are dropped with no warning, while the rule tables refuse an unknown field. #713's three claims are all verified.

**What is global.** All ball physics in `data/rules/flight.json` (drag 0.0019, roll friction 22, rest speed 1.4, bounce 0.48 / 0.82, wall 0.48 / 0.82, `windMul` 0.35), consumed in `BallFlight.cs:79-194`. All body motion in `fielding.json` `chase`. The infield and the seven fielder starts. The foul wrap (36 ft), the backstop (−36 ft), the 4.2-ft rail and the 95-ft flare, which are `HarborWall` literals that `FieldBounds.Build` uses for every park. Two more loose-ball ground models sit beside `flight.roll`.

**Hazards.** `ParkHazards` (`Fielding.cs:645-729`). Every test runs once, in the preview, against the ball's landing point. A landing inside a freeze, lava or breath disc slows every chaser to 0.45 for the whole play and adds a 0.4 drop roll. A grounder landing near a can moves the *preview* landing to a random other can; the live ball is not moved. A billboard landing gives a star. `climb_wall` is a park-wide flag that also widens a Clamber fielder's catch radius anywhere. `statue`, `train`, `ac_unit` and `tree` do nothing. Chompers are three literal discs behind `park.Id != "funfair-park"`. Unity reads none of the hazard flags; a caption is the only tell.

**One derived table already exists.** A match plays on `content.Rules.AtLevel(difficulty)` (`Match.cs:98`, `Rules.cs:38`). That is the precedent for a per-park resolved table. The risk beside it: `Diamond`, `ParkDiamond` and `RunnerAi` read the process-wide `Rules.Default`, and 118 call sites fall back to it.

**Two data roots.** `trials/c80` carries all six parks (fences × 0.70, hazards moved by zone, radii × 0.70). A trial file must carry every key the shipped file has, so a new park field must land in twelve files. `PlayTraceIdentity` serialises the whole `Park` record, and the CI seals hash `Models.cs`, `FieldBounds.cs`, `HarborWall.cs` and Harbor's park file.

**Presentation.** One scene and one builder. Harbor draws `HarborKit`. The other five parks draw an older primitive diamond in `ParkView` (bags on the foul line, a square dirt pad, no batter's boxes, no foul rail) and one private dress method each. Eleven light rigs are code literals chosen by park id. The drawn wall mirrors right field onto left, so a lopsided park draws wrong. `data/art/parks.json` rows are `{id, slot, placed}`. `StillRequest` cannot name a park or night. The stage and dual-still catalogs have a Harbor lane only. `harbor_kit.py` reads no data and seven of its constants have drifted from the sim.

**Gates.** S-29 pools fifty day games at the home captain's park: 15 at Harbor, 10 Canopy, 10 Rooftop, 5 each at Ember, Funfair and Crystal. No park has its own band. ~~The CLI has no night flag.~~ ✅ #828 (PR #834): `cli match --night`, and `cli match --cohort park-factors` measures every park day and night — a report, not a gate, so no park has a band still. Tutorials and Practice are always at Harbor.

### Measured today: the same matchup and seeds at every park

`tools/park-factors.py`, fifty three-inning day games per park, CPU both sides. Read a factor within about 0.15 of 1.0 as noise. Measured at `d0c6e12c`; **the measurement path is now `cli match --cohort park-factors`** (#828), which adds the whole catalog, ten matchups and night, and the script is superseded.

| Park | Shipped: runs × Harbor | HR × Harbor | C80: runs × Harbor | HR × Harbor |
| --- | --- | --- | --- | --- |
| Harbor Diamond | 1.00 (3.90 / game) | 1.00 (1.24) | 1.00 (5.84 / game) | 1.00 (1.56) |
| Crystal Rink | 1.31 | 1.45 | 0.92 | 1.24 |
| Funfair Park | 1.35 | 1.44 | 1.02 | 1.33 |
| Rooftop City | 1.04 | 1.15 | 0.98 | 1.10 |
| Canopy Yard | 1.03 | 1.27 | 0.74 | 0.97 |
| Ember Keep | 1.11 | 1.03 | 0.93 | 0.92 |

Two readings. The 8-ft parks give 0.58–0.80 ground-rule doubles a game against Harbor's 0.10, so fence height is already a strong lever. And the park order changes between the two roots, so a park's character today is an accident of scale, not a design.

## Corrections to in-repo documents

Recorded here. Not yet applied; epic F0 in the plan owns them.

| Document says | Evidence says |
| --- | --- |
| parks.md: the sim ignores unknown hazard types with a warning | An unknown type stops the load; unknown fields are dropped silently; there is no warning channel |
| parks.md and spec §14: freezers freeze for 1.2 s on touch and can be shattered | No touch test, no timer, no shatter |
| spec §14: hazards are ticked; tilt is a hazard; no park id in code | Nothing ticks; no `tilt` type exists; chompers test a park id |
| parks.md: the train blocks catches; AC units carom; the statue breathes fire | None has a sim effect; `fire_breath` is its own disc 60 ft from the statue |
| parks.md: every park uses the same diamond kit; art-rails.md: parks are kits, not `ParkView` methods | Two diamonds; five `ParkView` methods |
| parks.md: the Sluggers manual said players slip on ice | The wiki credits an in-game card; the manual was not readable |
| research-sluggers.md: Wario City has gem gimmicks; DK Jungle has climbable walls; stars come from park features in Sluggers | Manholes and arrows; roots, flowers and a statue; star targets are documented for the GameCube game only |
| parks.md: Cruise Deck tilts at inning 4; Haunt Manor disrupts inputs | The reference tilts at night near mid-game; input disruption has no precedent anywhere |

## Research needed before selecting numbers

- **Per lever, a fixed-input probe** on both roots: the same fly at two drags; the same grounder on two grounds; the same carom off two walls. Report distance, time to the wall, and whether the play kind changes. Use the flight-probe tool's pattern.
- ~~**Park factors on predeclared seeds**, more than one matchup, day and night (needs a CLI night flag).~~ ✅ #828 (PR #834): `cli match --night` exists, and `cli match --cohort park-factors` runs every park in the catalog on predeclared seeds, ten matchups, day and night, either root. Still open: **fifty three-inning games per park per condition is too few to read a 10 % effect** — the cohort size that could is the tuning step's call (implementation map §5 Q10).
- **Reference captures** if a Sluggers number is wanted: ball roll-out on Peach Ice Garden against Mario Stadium, wall heights by body height, hazard positions against the bases. Record title, version, park, day or night, and capture rate. No Wii datamine exists.
- **Ground values from the literature are bounce pace, not roll.** Do not map a 0.445 bounce value onto `roll.friction`. Derive roll from a named target (how far a routine grounder runs) and test it.
- **The races.** Any ground or air change moves the fielding races that #693 calibrated. Re-run the #702 trace rows per park before a number is accepted.

## Sources

Primary (P)
- **NATHAN-DEN** — A. Nathan, [Baseball at High Altitude](https://baseball.physics.illinois.edu/Denver.html)
- **NATHAN-SC** — A. Nathan, [Using Statcast Data to Study the Flight of a Baseball](https://baseball.physics.illinois.edu/THT-Fly-Ball-Distances-Statcast.pdf)
- **NATHAN-CARRY** — A. Nathan, [The Carry of a Fly Ball](https://baseball.physics.illinois.edu/carry.html)
- **BROSNAN** — Brosnan, McNitt, Serensits, [Effects of Surface Conditions on Baseball Playing Surface Pace](https://plantscience.psu.edu/research/centers/ssrc/documents/effects-of-surface-conditions-of-baseball-playing-surface-pace.pdf)
- **SAVANT-PF** — [Baseball Savant Statcast Park Factors](https://baseballsavant.mlb.com/leaderboard/statcast-park-factors), 2023–2025 rolling, read September 21, 2026
- **MLB-FD** — [MLB.com glossary, Field Dimensions](https://www.mlb.com/glossary/rules/field-dimensions)
- **MLB-WIND** — [MLB.com, the impact of wind](https://www.mlb.com/news/the-big-impact-of-wind-on-baseball-outcomes)

Community wikis and datamines (C)
- **MH-STAD** — [Mario Superstar Baseball datamine, Stadiums](https://mariobaseball.miraheze.org/wiki/Stadiums)
- **MW-MSS** [Mario Super Sluggers](https://www.mariowiki.com/Mario_Super_Sluggers) · **MW-MS** [Mario Stadium](https://www.mariowiki.com/Mario_Stadium_(baseball_stadium)) · **MW-PIG** [Peach Ice Garden](https://www.mariowiki.com/Peach_Ice_Garden) · **MW-YP** [Yoshi Park](https://www.mariowiki.com/Yoshi_Park) · **MW-WC** [Wario City](https://www.mariowiki.com/Wario_City) · **MW-DKJ** [DK Jungle](https://www.mariowiki.com/DK_Jungle_(baseball_stadium)) · **MW-BJP** [Bowser Jr. Playroom](https://www.mariowiki.com/Bowser_Jr._Playroom) · **MW-BC** [Bowser Castle](https://www.mariowiki.com/Bowser_Castle_(baseball_stadium)) · **MW-LM** [Luigi's Mansion](https://www.mariowiki.com/Luigi%27s_Mansion_(baseball_stadium)) · **MW-DC** [Daisy Cruiser](https://www.mariowiki.com/Daisy_Cruiser_(baseball_stadium))
- **MTA** [Mario Tennis Aces](https://www.mariowiki.com/Mario_Tennis_Aces) · **MSC** [Mario Strikers Charged](https://www.mariowiki.com/Mario_Strikers_Charged) · **SSB** [Smash Wiki, Stage hazard](https://www.ssbwiki.com/Stage_hazard)
- **PR-RULES** — [Project Rio game modes](https://www.projectrio.online/gamemode) (SNIPPET)
- **SHOW-SZ** [ShowZone stadiums](https://showzone.gg/stadiums) · **SHOW-SC** [ShowZone, custom stadiums](https://showzone.gg/news/how-to-build-a-custom-stadium-in-mlb-the-show)
- **SMB-XBL** [SMB stadium reference](https://www.xblbaseball.com/references/smb-stadiums) · **BYB-WP** [Backyard Baseball](https://en.wikipedia.org/wiki/Backyard_Baseball_(1997_video_game)) · **SB2020** [Super Baseball 2020](https://en.wikipedia.org/wiki/Super_Baseball_2020) · **WII** Wii Sports wiki (SNIPPET)
- **WP-FIELD** [Baseball field](https://en.wikipedia.org/wiki/Baseball_field) · **WP-GR** [Ground rules](https://en.wikipedia.org/wiki/Ground_rules) · **WP-GM** [Green Monster](https://en.wikipedia.org/wiki/Green_Monster) · **WP-DAIKIN** [Daikin Park](https://en.wikipedia.org/wiki/Daikin_Park) · **WP-POLO** [Polo Grounds](https://en.wikipedia.org/wiki/Polo_Grounds) · **WP-ORACLE** [Oracle Park](https://en.wikipedia.org/wiki/Oracle_Park)

Press and analysis (R)
- **FG-FOUL** [FanGraphs, shrinking playing surfaces](https://blogs.fangraphs.com/ballpark-playing-surfaces-are-shrinking-in-a-surprising-way/) · **FG-FOULHFA** [FanGraphs, foul ground](https://blogs.fangraphs.com/foul-ground-home-field-advantage/) · **THT-TWI** [The twilight strike zone](https://tht.fangraphs.com/the-twilight-strike-zone/) · **SI-WALLS** [SI, outfield walls](https://www.si.com/mlb/2021/03/24/mlb-outfield-walls-ranked-fenway-park-yankee-stadium)
- **SMB-TG** [TheGamer, SMB3 stadiums](https://www.thegamer.com/super-mega-baseball-3-best-stadiums/) · **BYB-OS** [Operation Sports, Backyard Baseball 2026 stadiums](https://www.operationsports.com/all-stadiums-in-backyard-baseball-2026-which-ones-are-best/)
- **MPT** Mario Power Tennis reviews via [Metacritic](https://www.metacritic.com/game/mario-power-tennis/critic-reviews/?platform=gamecube) (SNIPPET) · **MSBL** [Nintendo Life, Battle League review](https://www.nintendolife.com/reviews/nintendo-switch/mario-strikers-battle-league) (SNIPPET)
- **Legacy:** [research-sluggers.md](../reference/research-sluggers.md). Its park table is corrected above; this report does not renew its other claims.
