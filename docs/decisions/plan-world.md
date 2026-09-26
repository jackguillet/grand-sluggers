# Plan: the world — ten stadiums on one continent, ten captains

Status: **planning. Nothing here is built.** Rails, data and greyboxes may be built now; art waits for #346 and each park's greybox sitting (WD-01, WD-02). Every decision is answered (see the matrix). Tracker: #1133. Register (canonical, with every option's trade-off): [world-decisions.json](../research/world-decisions.json). Ids: **WD-01 … WD-22**.

## What Jack asked for

- Ten stadiums that together make up one continent. The player picks the stadium from the menu.
- Each stadium stands in the region that fits it: ice in a cold place, one in a volcano, one on a tropical island, and so on.
- For each stadium: its hazards, its night lights and its other defining features.
- Ten captains, each with a home field. One field is a tropical beach.

## Where we are today

| | Today |
| --- | --- |
| Parks in data | 6: Harbor Diamond, Crystal Rink, Funfair Park, Rooftop City, Canopy Yard, Ember Keep |
| Parks designed but deferred | Haunt Manor, Cruise Deck, Playroom (`docs/parks.md`, #37) |
| Captains | 7: Rio, Vale, Zig, Brondo, Konga, Ashlord, Elder Fenn |
| Factions | 7. A park's `faction` is unique. Stillwater (Fenn) has no park; Fenn plays at Harbor. |
| Park pick | Left/right cycle on the stadium row (`pickOrder`). No map. |
| Park look | Harbor is the kit. The other five are the field kit in their palette plus the one bowl of bleachers. No backdrops (FD-16-R2). |
| Night | Lights stay on (FD-11-R2). Night changes only the view outside and the hazards. |
| Hazards | Closed pattern library: status volume, ball redirect, reward target, wall trait, solid body, timed mover, decoration. Hazards can be turned off. |
| Characters | Four bars from nine sub-stats (CF-1). Body classes are coming as data rows, room for about fifteen (CF-3, #1116). |
| Standing rules | Extra parks are on AGENTS.md's do-not-start list (#37). Park art waits for a greybox sitting (FD-17). No balance until Jack says. |

## Decision matrix

All 22 decisions are accepted. Planning is complete; the children below can be filed. The recommendation is not the decision. "Blocks" names the children in [Build order](#build-order).

| Id | Area | Question | Options | Recommend | Depends on | Blocks |
| --- | --- | --- | --- | --- | --- | --- |
| WD-01 | Gating | When does building start? | A after #346 · B rails + greyboxes now, art later · C everything now | **Accepted: B** (Jack, 2026-09-24) | — | all |
| WD-02 | Gating | Park art order | A keep FD-17 (greybox sittings first) · B art as built | **Accepted: A** (Jack, 2026-09-24) | 01 | C9 |
| WD-03 | Gating | Backdrops come back? | A kit art · B data greybox now, art later · C none | **Accepted: A, rough blockout is enough** (Jack, 2026-09-25) | 01 | C5, C9 |
| WD-04 | Gating | Unlocks | A all open · B unlock by play · C open in Exhibition, unlocks in Challenge | **Accepted: C** (Jack, 2026-09-25) | — | C6 |
| WD-05 | World | Continent shape and name | A the draft (the Diamond Isles) · B Jack's own | **Accepted: A, shape only; name is WD-19** (Jack, 2026-09-25) | — | C1, C6 |
| WD-06 | World | Which ten parks | A the draft ten · B swap in a deferred park · C Jack's list | **Accepted: A** (Jack, 2026-09-25) | 05 | C3 |
| WD-07 | World | Fenn's home | A Stillwater Marsh · B keep Harbor, fourth new captain | **Accepted: Fenn → Coconut Cove** (Jack, 2026-09-25) | 06 | C1, C3 |
| WD-08 | World | Hazards per park | A one primary + one night change · B two | **Accepted: A** (Jack, 2026-09-25) | — | C3, C4 |
| WD-09 | World | New hazard patterns | A surge + drift + fog · B reuse only · C surge + drift, Stillwater reuses | **Accepted: C** (Jack, 2026-09-25) | 06, 08 | C4, C7 |
| WD-10 | World | Night lights | A themed rig + look · B look only · C darker play light | **Accepted: A** (Jack, 2026-09-25) | — | C5, C9 |
| WD-11 | Captains | The three new captains | A Kai, Sable, Hollis · B Jack's own | **Accepted: A**; Kai later replaced by a marsh captain (WD-21) (Jack, 2026-09-25) | 06 | C2 |
| WD-12 | Captains | New factions? | A one each · B join existing | **Accepted: A** (Jack, 2026-09-25) | 11 | C2 |
| WD-13 | Captains | Role players | A three each now · B captains first | **Accepted: A** (Jack, 2026-09-25) | 12 | C2 |
| WD-14 | Captains | Body classes | A a new class each · B reuse | **Accepted: A** (Jack, 2026-09-25) | 11 | C2 |
| WD-15 | Captains | Star abilities | A all new · B reuse · C one signature each | **Accepted: A** (Jack, 2026-09-25) | 11 | C2, C7 |
| WD-16 | Captains | Home-field edge | A cosmetic · B small edge | **Accepted: A** (Jack, 2026-09-25) | — | — |
| WD-17 | Menu | The picker | A continent map · B cycle + postcard · C both | **Accepted: A** (Jack, 2026-09-25) | 05 | C6 |
| WD-18 | Menu | Map look | A painted 2-D · B 3-D diorama · C greybox first | **Accepted: C** (Jack, 2026-09-25) | 17 | C6, C9 |
| WD-19 | World | Continent name | A Pennant Isles · B Grand Reach · C Homeplate Isles · D own | **Accepted: B, the Grand Reach** (Jack, 2026-09-25) | 05 | C1, C6 |
| WD-20 | World | Crystal Rink's colder name | A Aurora Rink · B Glacier Garden · C Polar Palace · D own | **Accepted: A, Aurora Rink** (Jack, 2026-09-25) | 06 | C1 |
| WD-21 | Captains | Stillwater Marsh's captain; where Kai goes | A Kai to the marsh · B new marsh captain, Kai dropped · C Kai joins Fenn (11 captains) · D own | **Accepted: B, a new marsh captain** (Jack, 2026-09-25) | 07, 11 | C2, C3 |
| WD-22 | Captains | Stillwater Marsh's new captain | A Reed (frog jumper, Marsh Hoppers) · B Heron (wader, Reedwalkers) · C own | **Accepted: A, Reed** (Jack, 2026-09-25) | 21 | C2 |
| WD-23 | Story | The frame | A Rio, a human kid, is carried from his neighborhood field into the Grand Reach, and his friends come with him · B no story | **Accepted: A** (Jack, 2026-09-25) | — | C2, C10 |
| WD-24 | Captains | Who is human? | A only Rio and his friends (the Spark League role players) · B mixed | **Accepted: A**: every other captain and role player is a human-shaped animal or a made-up creature (Jack, 2026-09-25) | 23 | C2, C10 |
| WD-25 | World | Harbor Diamond | A becomes Rio's neighborhood field, in his own world · B stays a Grand Reach park | **Accepted: A** (Jack, 2026-09-25). It keeps today's dimensions and stays the calibrated control park; only the dressing changes (Jack, 2026-09-25). Its new name is open | 23 | C1, C9 |
| WD-26 | Menu | The neighborhood field on the map | A drawn apart from the Grand Reach, in another dimension, with a way across · B one of the pins | **Accepted: A** (Jack, 2026-09-25). The crossing is a **portal**: the map draws a portal between the neighborhood field and the Grand Reach (Jack, 2026-09-25); its art is a map-art slot and waits like the rest of the map art (WD-18) | 17, 25 | C6 |
| WD-27 | Sidekicks | How many, and what are they? | A eight per captain, from three species per park; a species may repeat on its team · B one species per park | **Accepted: A** (Jack, 2026-09-25). A species is an animal hybrid or a made-up creature themed to its park; Rio's are human kids. Species, builds and names are drafts on the review page. This replaces "role players copy the captain's body" (silhouette bible): a species is a body build, a palette and one shared set of add-ons, never a new mesh per sidekick (#25 stays closed); the add-ons wait for #687 | 13, 24 | C2, C10 |
| WD-28 | Chemistry | Crews | A shared traits across parks and builds: sharing a crew is good chemistry, a rival crew bad; up to two per character; crews replace the hand-listed pairs except the story pairs · B keep the hand-listed pairs | **Accepted: A** (Jack, 2026-09-25, on the recommendation) | 27 | C2, C11 |

Nothing is open. Jack's answers WD-13 A and WD-15 A differ from the recommendations.

## The continent (WD-05, WD-19)

The continent is **the Grand Reach** (WD-19): one main land, and one island, Coconut Cove, off the south coast. North is cold, south is warm, the west is wild, the east is built up.

```
                          ❄ AURORA RINK
                             frozen north
        ⛰ SUMMIT PARK                          ⚙ ROOFTOP CITY
          high peaks                              eastern capital
  🌴 CANOPY YARD            🎡 FUNFAIR PARK
     western rainforest        central plains
  🌫 STILLWATER MARSH                    ⚓ HARBOR DIAMOND
     river delta                            south coast
  🌋 EMBER KEEP             🏜 SUNSCORCH MESA
     the volcano               southern canyon
                                         ≈≈≈≈≈≈≈≈≈≈≈≈
                                     🏝 COCONUT COVE
                                        the island
```

## The ten parks (WD-06 to WD-10)

Numbers here are **proposals, not decisions**: dimensions, radii and times are tuned in play later. Every park keeps the fields rails: one diamond; a hazard never awards an out, hit or catch (FD-08); hazards stay off the base paths (FD-19); hazards can be turned off (FD-10); Harbor is the only calibrated park (FD-13).

### Park matrix

| # | Park | Region | Captain · faction | Status | Surface | Fence L / C / R, top | Wind | Air |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | Harbor Diamond | South coast | Rio · Spark League | Built, art | Grass | 232 / 280 / 232, 12 ft | 4 mph out to right-centre | Global |
| 2 | Aurora Rink (was Crystal Rink; id `crystal-rink`) | Frozen north | Vale · Royal Rink | Built, greybox | Ice | 224 / 270 / 224, 8 ft glass | 2 mph in | Drag × 1.06 (cold) |
| 3 | Funfair Park | Central plains | Zig · Carnival Crew | Built, greybox | Grass | 220 / 273 / 238, 8 ft | 6 mph out | Global |
| 4 | Rooftop City | Eastern capital | Brondo · Goldrush | Built, greybox | Tar (dirt row) | 223 / 272 / 225, 12 ft | 9 mph across | Global |
| 5 | Canopy Yard | Western rainforest | Konga · Canopy Clan | Built, greybox | Dirt | 218 / 265 / 223, 12 ft | 3 mph in | Global |
| 6 | Ember Keep | The volcano | Ashlord · Ember Keep | Built, greybox | Ash | 237 / 286 / 237, 10 ft | 1 mph out | Global |
| 7 | Stillwater Marsh | River delta | New marsh captain (WD-21, name WD-22) | **New** | Wet grass (grass row) | 226 / 268 / 226, 8 ft reed wall | 1 mph, calm | Drag × 1.03 (damp) |
| 8 | Coconut Cove | Tropical island | Elder Fenn · Stillwater (WD-07) | **New** | Sand (**new ground row**) | 222 / 275 / 222, 6 ft rope-and-post | 7 mph sea breeze across | Global |
| 9 | Sunscorch Mesa | Southern canyon | Sable · Dune Nomads | **New** | Hard-pan (dirt row) | 234 / 290 / 228, 14 ft canyon rock | 5 mph, shifting | Drag × 0.97 (hot, dry) |
| 10 | Summit Park | High peaks | Hollis · Peak Guard | **New** | Alpine grass (grass row) | 240 / 296 / 240, 10 ft | 6 mph gusts, changes each inning | Drag × 0.9 (thin air) |

### Hazard and night matrix

| # | Park | Day hazard (pattern) | What it does | Night change | Night light rig (WD-10) | Backdrop (WD-03) |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Harbor Diamond | None | The control park | Fireworks on homers (look) | Steel light towers | Harbor town and the water (built) |
| 2 | Aurora Rink | Freezers (status volume) | Touch = 3 s slow (built) | Follow spot on the ball (look) | Chandeliers, ice-crystal pylons | Ice palace, frozen peaks |
| 3 | Funfair Park | Warp cans (ball redirect) | A grounder in one can comes out another (built) | Chompers redirect flies (built) | String lights, ride neon | Ferris wheel, big top |
| 4 | Rooftop City | Star billboards (reward target) | A ball that lands in one = team star (built) | Neon glare (look) | Neon signs, roof floods | Skyline, water towers |
| 5 | Canopy Yard | Barrel cannons (ball redirect) + climb wall (wall trait) | Redirects grounders; any fielder can climb the wall and rob a homer (park rule; today it needs Clamber, built) | Fireflies (look) | Lanterns in the trees | Canopy, waterfalls |
| 6 | Ember Keep | Lava pits + fire breath (status volume) | Touch = 3 s slow (built) | Breath reach × 1.6 (built) | Braziers, lava glow | The volcano's crater rim |
| 7 | Stillwater Marsh | **Lily pads** (solid body on a timed drift) | Pads drift slowly across the outfield on a seeded path; a rolling ball caroms off one | Mist rolls in over the water past the fence (look); pads glow | Paper lanterns on the water | Reeds, a still lake, stilt houses |
| 8 | Coconut Cove | **The tide** (**new: surge**) | On a timer, a wave sweeps the foul-side corners of the outfield and carries a rolling ball a set distance toward the line | High tide: the wave reaches farther in | Tiki torches, a lighthouse beam | Palm island, open sea, a volcano cone far off |
| 9 | Sunscorch Mesa | **Dust devils** (**new: drifting redirect**) | Two small whirlwinds wander the outfield on seeded paths; a ball in flight through one is pushed sideways a set amount | Clear desert air: no dust devils, the wind drops | Lanterns on the canyon rim, a starry sky | Red mesas, a canyon arch |
| 10 | Summit Park | **Mountain gusts** (environment, **new: wind schedule**) | Thin air carries the ball farther; the wind turns to a new seeded direction each inning, shown on the flag and the card | Snow flurries (look); gusts stronger | Cable-car lights, a mountain beacon | Snow peaks, a cable car |

Why the new parks' hazards look like this:

- **Stillwater Marsh** uses existing patterns (WD-09 C). A fog that hides the ball from the player would not hide it from the CPU, which reads the park (FD-14). That is unfair, so fog is only a look past the fence.
- **Coconut Cove's tide** is new. It is a timed band that moves a rolling ball; the train (a timed mover) is the nearest existing pattern, but it blocks rather than carries. The wave never touches a ball in the air or a fielder.
- **Sunscorch Mesa's dust devils** are a redirect that moves and acts on a ball in flight. Today's redirects are fixed and act on the ground. The push is a fixed amount, not a roll (FD-08).
- **Summit Park** needs no hazard pattern: thin air is the existing `environment.dragMul` rail, and the wind schedule is a small new environment row. Its identity is "the ball flies here".

### Look sheets (greybox palette and stands, for WD-03 and WD-10)

| # | Park | Ground | Wall and cap | Seats · roof | Sky and light |
| --- | --- | --- | --- | --- | --- |
| 7 | Stillwater Marsh | Deep green grass, dark wet dirt | Woven reeds, cream cap | Sage, cream and lotus pink · a thatch roof | Soft grey-green morning; mist |
| 8 | Coconut Cove | Pale sand outfield, warm dirt infield | Driftwood posts, rope cap | Coral, teal and sun yellow · open | Bright blue midday; orange sunset at night |
| 9 | Sunscorch Mesa | Cracked orange hard-pan, red dirt | Sandstone blocks, rust cap | Rust, turquoise and bone · canvas shades | Hot white noon; deep indigo, starry night |
| 10 | Summit Park | Short alpine green, grey dirt | Timber and stone, snow cap | Navy, white and signal red · open | Crisp blue; clear starry cold night |

## The ten captains (WD-11 to WD-16; superseded in part by WD-23 to WD-28 below)

Bars are the four derived bars (CF-1): **Pitch / Bat / Field / Run**, at most one bar at 9 or higher. The new captains' values are proposals.

| Captain | Faction · colors | Home park | Bars P / B / F / R | Baseball identity | Signature ability (WD-15) |
| --- | --- | --- | --- | --- | --- |
| Rio | Spark League · red | Harbor Diamond | 6 / 7 / 6 / 7 | Five-tool hero | Heatball, Heat Swing |
| Vale | Royal Rink · pink / ice blue | Aurora Rink | 9 / 4 / 8 / 5 | Pitching and glove | Charmball, Snap Throw |
| Zig | Carnival Crew · green / rainbow | Funfair Park | 4 / 4 / 6 / 9 | Speed and range | Prismball, Lick Catch |
| Brondo | Goldrush · yellow | Rooftop City | 5 / 8 / 3 / 4 | Power, bad glove | Phonyball, Laser |
| Konga | Canopy Clan · brown | Canopy Yard | 6 / 9 / 3 / 2 | Power and wall climbs | Caskball, Clamber |
| Ashlord | Ember Keep · black / purple | Ember Keep | 5 / 10 / 3 / 3 | Pure slug | Skullball, Furnace |
| Elder Fenn | Stillwater · sage / cream | **Coconut Cove** (WD-07: a turtle on the beach) | 7 / 5 / 8 / 3 | Glove and slow fog | Fogball |
| **Reed** (WD-22) | **Marsh Hoppers** · green / lotus pink | Stillwater Marsh | 4 / 6 / 7 / 8 | Frog jumper: speed and range, huge leaps in the field | **Lily Leap** (draft): a star jump catch that reaches a fly well over a fielder's head |
| **Sable** | **Dune Nomads** · sand / rust | Sunscorch Mesa | 8 / 5 / 6 / 5 | Desert trickster: a heavy sinker, sure hands | **Mirage Ball**: the pitch draws a second, fainter ball for its first half that fades before the zone |
| **Hollis** | **Peak Guard** · navy / white | Summit Park | 6 / 8 / 5 / 4 | Mountain climber: a big arm and high power | **Updraft**: a star swing whose fly rides the wind half again as far |

Each new captain founds a faction (WD-12) with three role players of its own (WD-13 A), gets a body class row once the class table lands (WD-14, #1116), and brings all-new star abilities: a star pitch, a star swing and a field ability, each with a lesson (WD-15 A). The signature lines above are the first of the three. Names were checked against the original-IP rule.

## The cast and the parks (WD-23 to WD-28)

Jack accepted the review page as edited (2026-09-25). Names are drafts until the original-IP check passes; content ids stay. Looks are directions for the art sessions, not art: each waits for its gates.

### Captains

| Captain (id) | Team | Home park | Who they are | Crews |
| --- | --- | --- | --- | --- |
| Ronnie Sparks (`rio`) | *open* | Neighborhood field | A human kid from the neighborhood field, carried into the Grand Reach | Showboats, Cool Kids |
| Vale (`vale`) | Aurora Blades | Aurora Rink | The Aurora Rink's swan skater | Early Birds, Showboats |
| Zig (`zig`) | Carnival Crew | Funfair Park | The funfair chameleon | Showboats |
| Brondo (`brondo`) | Skyline Gold | Rooftop City | The rooftop bulldog hustler | Cool Kids |
| Tambo (`konga`) | Canopy Clan | Canopy Yard | The rainforest's storm drummer | Garage Band |
| Ashlord (`ashlord`) | Ember Keep | Ember Keep | The cinder golem of the volcano forge | Night Crew |
| Elder Fenn (`fenn`) | Tidewater | Coconut Cove | The old sea turtle of Coconut Cove | Splash Club, Brainiacs |
| Arroyo (`sable`) | Canyon Nomads | Sunscorch Mesa | The canyon coyote trickster | Brainiacs, Night Crew |
| Hollis (`hollis`) | Peak Guard | Summit Park | The summit mountain goat | Early Birds |
| Reed (`reed`) | Marsh Hoppers | Stillwater Marsh | The marsh frog | Splash Club |

#### Ronnie Sparks

- **Bio:** A five-tool kid from the neighborhood field. One swing sparked a door into the Grand Reach, and the sparks never left the bat.
- **Look:** An ordinary kid's kit: a red jersey over a hoodie, fat sneakers; Round cheeks and a plaster on one knee; A backpack clipped to the dugout rail; Gold portal sparks cling to the bat and the sneakers. Tell language: Gold portal sparks, a rocket whistle, a pop.
- **Original-IP note:** Rio and the Spark League are the only humans. They look like kids from a real neighborhood, so the Grand Reach's creatures read as strange next to them.

#### Vale

- **Bio:** A swan who skates the rink at dawn and pitches the lights out at night.
- **Look:** White swan feathers tipped pink and ice blue; The long neck of the pageant body; Skate-blade cleats; An aurora ribbon sash that trails on every move; A crest of frost feathers in place of a tiara. Tell language: Ribbon trails, crystal chimes, the rink's follow spot.
- **Original-IP note:** A swan skater is original; the crown and the queen title stay gone.

#### Zig

- **Bio:** A funfair chameleon who changes colour with the ride lights and never waits in line.
- **Look:** Turret eyes that swivel on their own (they replace the goggle discs); A curled tail; Skin cycles through the Carnival's rainbow bands; A ticket-stub bandolier. Tell language: Colour cycling, a coaster clack, a calliope toot.
- **Original-IP note:** A small green tongue-catcher reads as the dinosaur sidekick; turret eyes, a curled tail and skin that cycles the fair's colours make a chameleon of the fair.

#### Brondo

- **Bio:** A bulldog who runs the rooftop card game. The glove is for show; the bat is for real.
- **Look:** A bulldog's square jaw on the cube torso; Gold and charcoal pinstripe vest; Gold-rimmed shades and a toothpick; A deck of cards in the back pocket; neon glints off the shades. Tell language: Shuffling cards, a coin flip, a neon buzz.
- **Original-IP note:** A bulldog card sharp is original; the yellow brute look stays gone.

#### Tambo

- **Bio:** Drums the thunder over the canopy. Treats the wall as a ladder and the ball as a drum.
- **Look:** Brown with moss green; A leaf crown; Vines wound on the long arms; Drum marks on the chest that glow on a swing; Fireflies in the fur at night; Barrel Bat becomes an ironwood log. Tell language: A thunder roll, falling leaves, waterfall spray.
- **Original-IP note:** A big ape called Konga with barrels reads as the other game's jungle king, so the name and the barrels both go. Canopy Yard's barrel cannons are a park question, not a captain's.

#### Ashlord

- **Bio:** A basalt golem who forges bats in the crater. The bat is a 10; everything else is a warning.
- **Look:** Black basalt body with glowing lava seams and ember-purple crystals; A forge helm with a chimney; A leather apron with glowing rivets; Anvil-heavy feet; tongs on the back; Ember eyes. Tell language: An anvil clang, a glow that cools to iron, the braziers flaring.
- **Original-IP note:** A horned fire king reads as the turtle king. A basalt golem with lava in its seams keeps the menace in an original creature, with no horns, shell or spikes.

#### Elder Fenn

- **Bio:** An old sea turtle who has watched every tide at the cove. The shell is the brim; the staff is driftwood.
- **Look:** Sage shell with barnacles; the shell is the brim; Cream beard; Driftwood staff, slung; A fishing-net shawl; A small lighthouse-lantern charm. Tell language: Surf hiss, a conch hum, the lighthouse sweep.
- **Original-IP note:** A barnacled sea-turtle fisherman is the cove's own; a walking turtle with a spiked shell would be the turtle king's army.

#### Arroyo

- **Bio:** A coyote from the mesa rim. The sinker drops out of the heat haze; nobody reads where it went.
- **Look:** Coyote ears; A rust-striped poncho and a turquoise scarf; Bone-bead wristbands; Heat shimmer rises off the shoulders at rest. Tell language: Heat shimmer, a dry rattle, a canyon echo.
- **Original-IP note:** Sable means black; a sun-bleached canyon coyote wants a canyon name. A trickster coyote is folklore, not a game mascot.

#### Hollis

- **Bio:** A mountain goat from the high peaks. Climbs anything, hits it into thin air and lets the gust do the rest.
- **Look:** A shaggy white coat and curled horns; Navy climbing harness with signal-red straps; Cloven cleats; Frost in the beard; A coiled rope over one shoulder. Tell language: A cable-car bell, a gust, a snow flurry.
- **Original-IP note:** Original.

#### Reed

- **Bio:** A frog from the river delta. One leap covers half the outfield; the landing is optional.
- **Look:** Green with lotus pink; Reed-woven wristbands; Webbed cleats; A throat pouch that puffs on every star action; A paper lantern on the belt at night. Tell language: A croak, ripples, splash rings.
- **Original-IP note:** Original.

### Sidekicks (WD-27)

Eight per captain, from three species of their park: one Bruiser (power bat, slow feet), one Scamp (speed, small frame), one Glove (sure hands, strong arm). A species is a body build, a palette and one shared set of add-ons, not a new mesh per sidekick; the add-ons wait for #687. Every species starts on its build's specials: Bruiser Star Fastball, Star Fly, Laser; Scamp Star Change, Star Grounder, Wall Spring; Glove Star Breaker, Star Line, Snap Throw.

#### Ronnie Sparks's sidekicks (Neighborhood field)

| Species | Blend | Build | Sidekicks (crews) |
| --- | --- | --- | --- |
| Big kids | Human | Bruiser | Marlow (Brainiacs), Benny *new*, Hattie *new* |
| Small fry | Human | Scamp | Pip (Cool Kids), Jojo (Cool Kids) *new*, Ruby *new* |
| Regular kids | Human | Glove | Nico, Gull (Early Birds) |

#### Vale's sidekicks (Aurora Rink)

| Species | Blend | Build | Sidekicks (crews) |
| --- | --- | --- | --- |
| Walrams | Walrus and ram | Bruiser | Pewter, Tundra *new*, Crispin (Brainiacs) *new* |
| Frostlings | Penguin and seal | Scamp | Sleet (Cool Kids, Splash Club) *new*, Flurry (Splash Club) *new*, Glimmer (Splash Club) *new* |
| Glintfoxes | Arctic fox and moth | Glove | Frost, Lace |

#### Zig's sidekicks (Funfair Park)

| Species | Blend | Build | Sidekicks (crews) |
| --- | --- | --- | --- |
| Juggleroos | Kangaroo and monkey | Bruiser | Bingo (Showboats) *new*, Kazoo (Garage Band, Showboats) *new*, Taffy (Snack Squad, Showboats) *new* |
| Rollers | Armadillo and hamster | Scamp | Dart, Jester (Showboats), Tilt (Cool Kids) *new* |
| Pufflings | Pufferfish and poodle | Glove | Confetti (Showboats) *new*, Whirl (Showboats) *new* |

#### Brondo's sidekicks (Rooftop City)

| Species | Blend | Build | Sidekicks (crews) |
| --- | --- | --- | --- |
| Alley Ratcats | Rat and cat | Bruiser | Boom, Nugget (Snack Squad), Vinnie *new* |
| Scrappers | Raccoon and pigeon | Scamp | Slick (Cool Kids) *new*, Penny *new*, Dice *new* |
| Glowgeckos | Gecko and firefly | Glove | Hex (Night Crew, Brainiacs), Ace (Night Crew) *new* |

#### Tambo's sidekicks (Canopy Yard)

| Species | Blend | Build | Sidekicks (crews) |
| --- | --- | --- | --- |
| Mosslings | Sloth and bear | Bruiser | Moss, Bongo (Garage Band) *new*, Thicket *new* |
| Lemurkeets | Lemur and parrot | Scamp | Vine, Kiwi (Snack Squad) *new*, Liana *new* |
| Toucanines | Toucan and dog | Glove | Basil (Snack Squad), Fern *new* |

#### Ashlord's sidekicks (Ember Keep)

| Species | Blend | Build | Sidekicks (crews) |
| --- | --- | --- | --- |
| Cinderboars | Boar and beetle | Bruiser | Cinder, Grit, Slag *new* |
| Coal Moles | Mole and bat | Scamp | Soot (Night Crew), Clinker (Night Crew) *new*, Brimsy (Night Crew) *new* |
| Charcrows | Crow and hawk | Glove | Scorch (Night Crew) *new*, Kiln (Night Crew) *new* |

#### Elder Fenn's sidekicks (Coconut Cove)

| Species | Blend | Build | Sidekicks (crews) |
| --- | --- | --- | --- |
| Pelicows | Pelican and cow | Bruiser | Barnacle *new*, Breaker *new*, Conch (Garage Band) *new* |
| Crabbits | Crab and rabbit | Scamp | Sandy *new*, Shelly *new*, Kelp *new* |
| Dolphups | Dolphin and pup | Glove | Coral (Splash Club) *new*, Nori (Splash Club, Snack Squad) *new* |

#### Arroyo's sidekicks (Sunscorch Mesa)

| Species | Blend | Build | Sidekicks (crews) |
| --- | --- | --- | --- |
| Bisonhorns | Bison and rhino beetle | Bruiser | Adobe, Mesquite *new*, Rattle (Garage Band) *new* |
| Jackalopes | Jackrabbit and antelope | Scamp | Tumble, Dusty *new*, Pebble *new* |
| Meerowls | Meerkat and owl | Glove | Sirocco (Night Crew), Sage (Night Crew, Brainiacs) *new* |

#### Hollis's sidekicks (Summit Park)

| Species | Blend | Build | Sidekicks (crews) |
| --- | --- | --- | --- |
| Ibexbears | Ibex and bear | Bruiser | Boulder *new*, Cairn, Crag *new* |
| Pikayaks | Pika and yak | Scamp | Scree, Tarn *new*, Ridge *new* |
| Marmeagles | Marmot and eagle | Glove | Flint (Early Birds), Cornice (Early Birds) *new* |

#### Reed's sidekicks (Stillwater Marsh)

| Species | Blend | Build | Sidekicks (crews) |
| --- | --- | --- | --- |
| Beaverbulls | Beaver and bullfrog | Bruiser | Bog, Sedge *new*, Paddle *new* |
| Axoducks | Axolotl and duck | Scamp | Tad (Splash Club), Ripple (Splash Club) *new*, Minnow (Splash Club) *new* |
| Otterons | Otter and heron | Glove | Cattail (Early Birds), Lotus (Early Birds) *new* |

### Crews (WD-28)

Two players who share a crew have good chemistry, whatever their team; a rival crew is bad chemistry. Crews replace the hand-listed buddy and rival pairs in `data/chemistry/overrides.json`, except the story pairs (Rio and Ashlord). Each character carries up to two crews.

| Crew | Who they are | Rival crew |
| --- | --- | --- |
| Cool Kids | The hip ones: shades, skate decks, sneakers | Brainiacs |
| Garage Band | They make noise | Night Crew |
| Night Crew | Awake after dark | Early Birds |
| Early Birds | Up at dawn | Night Crew |
| Splash Club | Swimmers | — |
| Snack Squad | Always eating | — |
| Showboats | Play to the crowd | Brainiacs |
| Brainiacs | Know the count | Showboats |

### Park look and hazards

| Park | Day hazard | What it does | Night | Ground · wall | Seats · sky | Backdrop |
| --- | --- | --- | --- | --- | --- | --- |
| Neighborhood field | None | The control park: every tuned number is measured here | Fireworks on homers (look); Streetlights and one old light tower | Worn grass, a dusty base path · Chain-link fence with a painted plywood home-run wall | Wooden bleachers, folding chairs · no roof · Late-afternoon summer; warm streetlight night | Houses, backyards and trees past the fence; the portal glows faintly beyond centre field |
| Aurora Rink | Freezers (status volume) | Touch = 3 s slow (built) | Follow spot on the ball (look); Chandeliers, ice-crystal pylons | Blue-white ice outfield, packed snow infield · Clear glass, frosted cap | Pale pink, ice blue and silver · a vaulted crystal roof over the stands · Pale arctic day; green and violet aurora at night | Ice palace, frozen peaks, the aurora overhead |
| Funfair Park | Warp cans (ball redirect) | A grounder in one can comes out another (built) | Chompers redirect flies (built); String lights, ride neon | Bright green grass, orange dirt · Striped carnival panels, bulb-lit cap | Red, yellow and teal · striped tent awnings · Sunny afternoon; neon and string lights at night | Ferris wheel, big top, a roller coaster |
| Rooftop City | Star billboards (reward target) | A ball that lands in one = team star (built) | Neon glare (look); Neon signs, roof floods | Rooftop turf over black tar, painted lines · Brick parapet with a steel rail | Gold, charcoal and neon pink · fire-escape balconies · Hazy city noon; neon-lit night | Skyline, water towers |
| Canopy Yard | Barrel cannons (ball redirect) + climb wall (park rule) | Cannons redirect grounders; any fielder can climb the wall and rob a homer (park rule, was Clamber) | Fireflies (look); Lanterns in the trees | Dark jungle grass, red dirt · Vine-covered timber; the climb wall in centre | Moss green, brown and parrot red · thatched platforms in the trees · Green-filtered light; lantern-lit night | Canopy, waterfalls |
| Ember Keep | Lava pits + fire breath (status volume) | Touch = 3 s slow (built) | Breath reach × 1.6 (built); Braziers, lava glow | Grey ash outfield, black cinder dirt · Basalt blocks with glowing seams | Black, ember purple and orange · forge-chimney pillars · Smoky orange day; lava glow at night | The volcano's crater rim |
| Coconut Cove | The tide (new: surge) | On a timer, a wave sweeps the foul-side corners of the outfield and carries a rolling ball a set distance toward the line | High tide: the wave reaches farther in; Tiki torches, a lighthouse beam | Pale sand outfield, warm dirt infield · Driftwood posts, rope cap | Coral, teal and sun yellow · open · Bright blue midday; orange sunset at night | Palm island, open sea, a volcano cone far off |
| Sunscorch Mesa | Dust devils (new: drifting redirect) | Two small whirlwinds wander the outfield on seeded paths; a ball in flight through one is pushed sideways a set amount | Clear desert air: no dust devils, the wind drops; Lanterns on the canyon rim, a starry sky | Cracked orange hard-pan, red dirt · Sandstone blocks, rust cap | Rust, turquoise and bone · canvas shades · Hot white noon; deep indigo, starry night | Red mesas, a canyon arch |
| Summit Park | Mountain gusts (environment, new: wind schedule) | Thin air carries the ball farther; the wind turns to a new seeded direction each inning, shown on the flag and the card | Snow flurries (look); gusts stronger; Cable-car lights, a mountain beacon | Short alpine green, grey dirt · Timber and stone, snow cap | Navy, white and signal red · open · Crisp blue; clear starry cold night | Snow peaks, a cable car |
| Stillwater Marsh | Lily pads (solid body on a timed drift) | Pads drift slowly across the outfield on a seeded path; a rolling ball caroms off one | Mist rolls in over the water past the fence (look); pads glow; Paper lanterns on the water | Deep green grass, dark wet dirt · Woven reeds, cream cap | Sage, cream and lotus pink · a thatch roof · Soft grey-green morning; mist | Reeds, a still lake, stilt houses |

Open: the neighborhood field's name; Rio's team name; Canopy Yard's barrel cannons read as another game next to an ape captain (hollow-log or vine cannons suggested).

## The menu (WD-17, WD-18)

- The stadium row on the setup screen opens the **continent map**.
- The stick moves between park pins in map order; the pin under the cursor shows the park's postcard, its captain's crest, its hazard line and whether night is on.
- South confirms, East backs out. Day / night and hazards stay rows on the setup screen.
- One screen for one pad and two pads: in 2P, either pad drives it, as today.
- The map draws from region data (a shape and a position per park) before any art exists (WD-18 C); painted art fills a slot later.
- `docs/how-to-play.md` and `HowToPlay.cs` change in the same PR.

## Build order

Children after the decisions. One issue and one worktree each. The session kind is in brackets. Numbers match "Blocks" in the matrix.

| Child | Kind | What | Needs |
| --- | --- | --- | --- |
| C1 World data rail | Gameplay | A `regions` catalog (id, name, map position, backdrop row); a park's `region`; ten `pickOrder` places | WD-01, 05, 07 |
| C2 New captains in data | Gameplay | Three captains and factions, nine role players, sub-stats, chemistry rows, body classes | WD-11 to 14, WD-21, #1116 |
| C2b New star abilities | Gameplay | Three new abilities per new captain (star pitch, star swing, field), one child per captain, each with its lesson | WD-15 |
| C3 Four new park files | Gameplay | Dimensions, fence, surface, wind, air, hazards from existing patterns; the sand ground row; at Harbor parity; off the base paths | WD-06 to 08 |
| C4 New patterns | Gameplay | One child each: surge (tide), drifting redirect (dust devils), wind schedule (gusts) | WD-09 |
| C5 Greyboxes | Presentation + Art | The four new parks in the field kit and the kit bowl; palettes, night looks, night rig rows; a rough Blender blockout backdrop per park, all ten (WD-03 A: rough is enough) | WD-03, 10 |
| C6 Continent map picker | Presentation | The map screen, both pads, the book pair | WD-04, 17, 18 |
| C7 Tutorial coverage | Gameplay | A lesson per new hazard pattern and new ability (`docs/tutorials.md`) | C2, C4 |
| C8 Greybox sittings | Jack | One park at a time (FD-17) | C5 |
| C9 Art | Art | Finished backdrop, night rig, dress and map art, one park at a time after its sitting | WD-02, C8 |
| C10 The cast in data | Gameplay | Captain display names, teams and bios as accepted; the Harbor park's display name and region move to the neighborhood field; 80 sidekicks (53 new) with species, build and sub-stats; species rows (build, palette, add-on slots, default specials); ids stay; the original-IP check | WD-23, 24, 27 |
| C11 Crews | Gameplay | A `crews` catalog; up to two crews per character; crew chemistry replaces the hand-listed pairs except the story pairs; the lineup reads the same rule | WD-28, C10 |
| C12 Portal and cast on screen | Presentation | The portal marker on the map; crew badges and chemistry lines on the lineup; the book pages | WD-26, 28, C6 |
| C13 Cast art | Art | One child per captain for the new look, and one per sidekick species (catalog slots first, then stills); the neighborhood field re-dress after #346; each waits for its gate (#687 for add-ons) | WD-24, 25, 27 |

Balance (park factors, S-29) runs only when Jack starts it.

## Open numbers (trials, not decisions)

| Number | Where it lives | Owner |
| --- | --- | --- |
| New parks' fences, heights, wind, air | `data/parks/*.json` | C3 |
| Sand ground row (roll, bounce, friction) | `data/rules/grounds.json` | C3 |
| Tide period, reach, carry distance | `data/rules/hazards.json` | C4 |
| Dust devil radius, speed, push | `data/rules/hazards.json` | C4 |
| Summit wind schedule and drag | the park's environment block | C4 |
| New captains' sub-stats | `data/characters/*.json` | C2 |
