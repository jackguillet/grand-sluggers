# Roster

Original characters. Factions exist so chemistry has a graph that a player can learn in one sitting.

Launch fantasy: **6 factions × 1 captain + ~3 role players = ~24**, then grow toward 40. Now in data: **10 captains + 80 sidekicks** (eight per captain, three species per faction, WD-27) (ten factions, one home park each). Exhibition auto-fills a 9 from the captain, faction mates, then chemistry, and the lineup is a draft: swap the eight, assign gloves (P, C, IF, OF), and hearts and scribbles show chemistry vs the captain (it pays off in the field; both teams start on the same Stars).

Stats are 1–10. Each character authors nine sub-stats; the four bars on the card are derived, the mean of each group rounded half up (spec §2):

- **Bat** = contact, power
- **Pitch** = pitch power (`velocity`), stamina (`endurance`), control, break (`movement`)
- **Field** = hands, throw speed (`arm`)
- **Run** = speed (`run`)

The bars below are what the derived numbers are today; the sub-stats are in "Sub-stats". The values are balance and will move.

## Factions

| Faction | Color | Vibe | Baseball identity | Home park |
| --- | --- | --- | --- | --- |
| **Spark League** | Red | Rio and the neighborhood kids, the only humans | 5-tool, portal-spark specials | Neighborhood Park |
| **Aurora Blades** | Pink / ice blue | Pageant, ice, manners with teeth | Pitching + glove, weak bats | Aurora Rink |
| **Carnival Crew** | Green / rainbow | Fairground speedsters | Run + range, contact, no power | Funfair Park |
| **Skyline Gold** | Yellow | Schemers, rooftop industrial | Power + stamina, bad gloves | Rooftop City |
| **Canopy Clan** | Brown | Jungle family, climbers | Power + wall climbs, slow | Canopy Yard |
| **Ember Keep** | Black / purple | Villain castle, lava | Pure slug, laser throws, no legs | Ember Keep |
| **Tidewater** | Sage / cream | Turtle elders | Glove + slow fog, no legs | Coconut Cove (Harbor until its park file lands) |
| **Canyon Nomads** | Sand / rust | Desert tricksters, canyon rim | Pitching (a heavy sinker), sure hands | Sunscorch Mesa (Harbor until its park file lands) |
| **Peak Guard** | Navy / white | Mountain climbers | Power and a big arm, slow feet | Summit Park (Harbor until its park file lands) |
| **Marsh Hoppers** | Green / lotus pink | Frog jumpers of the river delta | Speed and range, huge leaps | Stillwater Marsh (Harbor until its park file lands) |

Cross-faction buddy examples (authored, not generated): a Spark pitcher who grew up with a Carnival runner; a Royal and a Goldrush who date and therefore *hate* fielding together some days — no: keep bad chem as rivalry, good as buddy. Royals buddy with Spark. Goldrush buddy only with each other plus one traitor. Ember hates Spark and Royals. Canopy hates Ember’s mercenary lizards (a sub-rival).

Body types are locked in [silhouette-bible.md](silhouette-bible.md). Sidekicks wear their species' body (`data/world/species.json`).

## Captains (placeholders)

Names are working titles. Replace freely; keep the *roles*.

### Rio Sparks — Spark League

- Stats: Pitch 6 / Bat 7 / Field 6 / Run 7
- Star Pitch: **Heatball** — fire trail; fielders who catch it hop and drop on a timer.
- Star Swing: **Heat Swing** — line drive that leaves a burn patch (terrain deny, not a free homer).
- Field: **Grow** — briefly bigger catch radius.
- Bats/throws: R/R

### Queen Vale — Royal Rink

- Stats: Pitch 9 / Bat 4 / Field 8 / Run 5
- Star Pitch: **Charmball** — catcher-frame freeze; batter’s window shrinks.
- Star Swing: **Heart Swing** — flare that charms the nearest fielder into a pause.
- Field: **Snap Throw** — extra throw velocity, no windup.
- Bats/throws: R/R

### Zig — Carnival Crew

- Stats: Pitch 4 / Bat 4 / Field 6 / Run 9
- Star Pitch: **Prismball** — rainbow break, late two-plane cut.
- Star Swing: **Shell Swing** — bouncing egg/grounder that randomizes infield hops.
- Field: **Lick Catch** — tongue/stretch catch (range).
- Bats/throws: L/R

### Brondo — Goldrush

- Stats: Pitch 5 / Bat 8 / Field 3 / Run 4
- Star Pitch: **Phonyball** — decoy ball; real one is late.
- Star Swing: **Phony Swing** — two balls; fielders must pick.
- Field: **Laser** — fastest throw in the game, terrible first step.
- Bats/throws: R/R

### Konga — Canopy Clan

- Stats: Pitch 6 / Bat 9 / Field 3 / Run 2
- Star Pitch: **Vine Swing** — the pitch swings in wide on a vine from a pivot above and crosses where it was aimed, on time.
- Star Swing: **Lightning Liner** — a liner that jags sideways twice, up to 3 ft, and lands where a straight liner would.
- Field: **Clamber** — can catch on walls and fences.
- Bats/throws: L/R

### Ashlord — Ember Keep

- Stats: Pitch 5 / Bat 10 / Field 3 / Run 3
- Star Pitch: **Skullball** — heavy, late hop, intimidation (smaller swing window).
- Star Swing: **Furnace** — scorched fly; warning track becomes lava for a beat.
- Field: **Spin Check** — knock runners off the base path on contact.
- Bats/throws: L/R

### Elder Fenn — Stillwater

- Stats: Pitch 7 / Bat 5 / Field 8 / Run 3
- Star Pitch: **Fogball** — slow wet pitch; batter’s window shrinks in the mist.
- Star Swing: **Staff Swing** — cane hop that randomizes infield bounces.
- Field: **Withdraw** — shell catch, bigger glove window, still slow.
- Bats/throws: R/R


### Sable — Dune Nomads

- Bars: Pitch 8 / Bat 5 / Field 6 / Run 5. Body class `trickster` (borrows Vale's takes until its own style exists).
- Star abilities: Mirage Ball (a faint twin on the far half of the zone, gone by half the flight), Sidewinder (the first hop turns away from the chaser) and Sand Scoop (extra reach on a low grounder that never bobbles).
- Signature bat: Mirage Bat. Bats/throws: R/R. Role players: Sirocco, Tumble, Adobe.

### Hollis — Peak Guard

- Bars: Pitch 6 / Bat 8 / Field 5 / Run 4. Body class `climber` (borrows Brondo's takes until its own style exists).
- Star abilities: Rockfall (floats high, drops onto its crossing late), Updraft (the fly rides the wind half again as hard) and Long Toss (a deep throw keeps its pace 80 ft further).
- Signature bat: Summit Axe. Bats/throws: L/R. Role players: Flint, Cairn, Scree.

### Reed — Marsh Hoppers

- Bars: Pitch 4 / Bat 6 / Field 7 / Run 8. Body class `hopper` (borrows Zig's takes until its own style exists).
- Star abilities: Leapfrog (hangs mid-flight, then leaps to the plate on time), Pond Skip (the first hop springs high) and Lily Leap (the jump rises 4.5 ft, for a liner over his head).
- Signature bat: Reed Switch. Bats/throws: L/L. Role players: Cattail, Bog, Tad.

## Role players (slice set)

Enough to fill two lineups. Full bios later.

| Id | Name | Faction | Pitch | Bat | Field | Run | Verb |
| --- | --- | --- | --- | --- | --- | --- | --- |
| nico | Nico | Spark | 5 | 5 | 7 | 7 | Super Jump |
| pip | Pip | Spark | 7 | 3 | 5 | 8 | Snap Throw |
| marlow | Marlow | Spark | 4 | 6 | 6 | 6 | Dive |
| frost | Frost | Royal | 8 | 2 | 6 | 6 | Snap Throw |
| lace | Lace | Royal | 6 | 5 | 8 | 4 | Dive |
| dart | Dart | Carnival | 3 | 3 | 5 | 9 | Lick Catch |
| boom | Boom | Goldrush | 4 | 8 | 2 | 5 | Laser |
| vine | Vine | Canopy | 3 | 6 | 8 | 4 | Clamber |
| cinder | Cinder | Ember | 4 | 8 | 4 | 5 | Spin Check |
| soot | Soot | Ember | 6 | 4 | 5 | 6 | Burrow |
| grit | Grit | Ember | 5 | 6 | 6 | 5 | Spin Check |
| hex | Hex | Goldrush | 6 | 5 | 4 | 6 | Laser |
| gull | Gull | Spark | 4 | 4 | 7 | 8 | Super Jump |
| pewter | Pewter | Royal | 7 | 3 | 7 | 4 | Snap Throw |
| jester | Jester | Carnival | 3 | 5 | 5 | 8 | Lick Catch |
| nugget | Nugget | Goldrush | 5 | 7 | 2 | 5 | Laser |
| moss | Moss | Canopy | 4 | 7 | 4 | 3 | Clamber |
| basil | Basil | Canopy | 5 | 4 | 7 | 5 | Dive |

## Sub-stats

Authored in `data/characters/`. Every row carries all nine; the bars above are derived from them.

| Id | Contact | Power | Pitch power | Stamina | Control | Break | Hands | Throw speed | Speed |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| ashlord (C) | 10 | 10 | 5 | 5 | 5 | 5 | 3 | 3 | 3 |
| brondo (C) | 8 | 8 | 5 | 5 | 5 | 5 | 3 | 3 | 4 |
| fenn (C) | 5 | 5 | 7 | 7 | 7 | 7 | 8 | 8 | 3 |
| konga (C) | 9 | 9 | 6 | 6 | 6 | 6 | 3 | 3 | 2 |
| rio (C) | 7 | 7 | 6 | 6 | 6 | 6 | 6 | 6 | 7 |
| vale (C) | 4 | 4 | 9 | 9 | 9 | 9 | 8 | 8 | 5 |
| zig (C) | 4 | 4 | 4 | 4 | 4 | 4 | 6 | 6 | 9 |
| nico | 5 | 5 | 5 | 5 | 5 | 5 | 7 | 7 | 7 |
| pip | 3 | 3 | 7 | 7 | 7 | 7 | 5 | 5 | 8 |
| marlow | 6 | 6 | 4 | 4 | 4 | 4 | 6 | 6 | 6 |
| frost | 2 | 2 | 8 | 8 | 8 | 8 | 6 | 6 | 6 |
| lace | 5 | 5 | 6 | 6 | 6 | 6 | 8 | 8 | 4 |
| dart | 3 | 3 | 3 | 3 | 3 | 3 | 5 | 5 | 9 |
| boom | 8 | 8 | 4 | 4 | 4 | 4 | 2 | 2 | 5 |
| vine | 6 | 6 | 3 | 3 | 3 | 3 | 8 | 8 | 4 |
| cinder | 8 | 8 | 4 | 4 | 4 | 4 | 4 | 4 | 5 |
| grit | 6 | 6 | 5 | 5 | 5 | 5 | 6 | 6 | 5 |
| hex | 5 | 5 | 6 | 6 | 6 | 6 | 4 | 4 | 6 |
| soot | 4 | 4 | 6 | 6 | 6 | 6 | 5 | 5 | 6 |
| gull | 4 | 4 | 4 | 4 | 4 | 4 | 7 | 7 | 8 |
| pewter | 3 | 3 | 7 | 7 | 7 | 7 | 7 | 7 | 4 |
| jester | 5 | 5 | 3 | 3 | 3 | 3 | 5 | 5 | 8 |
| nugget | 7 | 7 | 5 | 5 | 5 | 5 | 2 | 2 | 5 |
| moss | 7 | 7 | 4 | 4 | 4 | 4 | 4 | 4 | 3 |
| basil | 4 | 4 | 5 | 5 | 5 | 5 | 7 | 7 | 5 |

## Authorship rules

- One defensive verb each.
- At most one bar at 9 or higher per character (CH-08); the validator refuses a second. Specialization is the joke.
- Color variants (if any) are palette swaps of role players, not new kits.
- Captains are the only unique Star Pitch/Swing. Do not secretly give role players cutscene specials.
- Names must pass a 10-second “not a Mario character” test. If it sounds like Luigi, change it.
