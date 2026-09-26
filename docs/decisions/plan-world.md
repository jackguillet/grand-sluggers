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
| 5 | Canopy Yard | Barrel cannons (ball redirect) + climb wall (wall trait) | Redirects grounders; Clamber robs homers (built) | Fireflies (look) | Lanterns in the trees | Canopy, waterfalls |
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

## The ten captains (WD-11 to WD-16)

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
