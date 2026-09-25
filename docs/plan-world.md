# Plan: the world — ten stadiums on one continent, ten captains

Status: **planning. Nothing here is built.** Every decision below is open until Jack answers it. The tracker is #1133. Implementation starts only after the gating decisions (W-01 to W-04) are answered.

## What Jack asked for

- Ten stadiums that together make up one continent. The player picks the one they play on from the menu.
- Each stadium stands in the region that fits it: ice in a cold place, one in a volcano, one on a tropical island, and so on.
- For each stadium: its hazards, its lights in night mode, and its other defining features.
- Ten captains, each assigned a home field.
- One field is a tropical beach.

## Where we are today

| | Today |
| --- | --- |
| Parks in data | 6: Harbor Diamond, Crystal Rink, Funfair Park, Rooftop City, Canopy Yard, Ember Keep (`data/parks/`) |
| Parks designed but deferred | Haunt Manor, Cruise Deck, Playroom (`docs/parks.md`, #37) |
| Captains | 7: Rio, Vale, Zig, Brondo, Konga, Ashlord, Elder Fenn (`data/characters/`) |
| Factions | 7. One park per faction is a load rule (`faction` is unique across parks). Stillwater (Fenn) has no park; Fenn plays at Harbor. |
| Park pick | `pickOrder` cycle on the stadium setup screen (left/right). No map. |
| Park look | Harbor is the kit. The other five are the field kit in their own palette, plus the one bowl of bleachers (PR #1131). Backdrops are empty (FD-16-R2). |
| Night | Lights stay on everywhere (FD-11-R2). Night changes only the view outside the stadium and the hazards. |
| Hazards | A closed pattern library (`data/rules/hazards.json`): status volume, ball redirect, reward target, wall trait, solid body, timed mover, decoration. Hazards can be turned off on the setup screen. |
| Standing rules | AGENTS.md: "Do not start … extra parks as products (#37)". Park art is banned before a park's rules are green and Jack has sat its greybox (FD-17). "Do not balance until Jack says." |

The reference (Mario Super Sluggers) shipped nine stadiums plus Toy Field. Each captain almost always had a home park; each park had one primary gimmick and a night variant (`docs/archive/reference/research-sluggers.md`). We keep that shape — one gimmick per park, a night that changes something — and keep our own places and toys.

## Proposed continent (draft for W-05 and W-06)

Working name: **the Diamond Isles** (a main continent plus one island). North is cold, south is warm, the west is jungle, the east is the city. Every name here is a working title.

```
                    ❄  CRYSTAL RINK (Royal Rink)
                       frozen north, ice palace
     ⛰ SUMMIT PARK (new)                      ⚙ ROOFTOP CITY (Goldrush)
       high peaks, thin air                       the capital, east coast
 🌴 CANOPY YARD (Canopy)      🎡 FUNFAIR PARK (Carnival)
    western rainforest           central plains
 🌫 STILLWATER MARSH (Stillwater)     ⚓ HARBOR DIAMOND (Spark)
    misty river delta                  south coast, the home port
 🌋 EMBER KEEP (Ember)         🏜 SUNSCORCH MESA (new)
    the volcano, southwest        southern desert canyon
                                               ~~~~~~~~~~~~
                                          🏝 COCONUT COVE (new)
                                             tropical island, off the south coast
```

A map picture replaces this sketch once W-05 is answered.

## Proposed ten parks (draft for W-06 to W-10)

Four of the ten are new: Stillwater Marsh (Fenn's own field) plus three new captains' parks, one of them the beach. The three existing deferred parks (Haunt Manor, Cruise Deck, Playroom) are the alternates in W-06.

| # | Park | Region | Captain (faction) | Surface | Primary hazard (day) | Night: what changes | Night lights (look) |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | Harbor Diamond | South coast port | Rio (Spark League) | Grass | None — the control park | Fireworks on homers (no rule) | Classic steel light towers |
| 2 | Crystal Rink | Frozen north | Vale (Royal Rink) | Ice (built) | Freezers: touch = 3 s slow (built) | Follow spot on the ball (look) | Chandeliers and ice-crystal pylons |
| 3 | Funfair Park | Central plains | Zig (Carnival Crew) | Grass | Warp cans redirect grounders (built) | Chompers redirect flies (built) | String lights and ride neon |
| 4 | Rooftop City | Eastern capital | Brondo (Goldrush) | Tar roof | Star billboards: hit = team star (built) | Neon glare (look) | Neon signs and rooftop floods |
| 5 | Canopy Yard | Western rainforest | Konga (Canopy Clan) | Dirt / jungle grass | Barrel cannons; climb wall (built) | Fireflies (look) | Lanterns hung in the trees |
| 6 | Ember Keep | The volcano | Ashlord (Ember Keep) | Ash | Lava pits and fire breath: 3 s slow (built) | Breath reach × 1.6 (built) | Braziers and a lava glow |
| 7 | Stillwater Marsh | Misty river delta | Elder Fenn (Stillwater) | Wet grass | Fog banks drift across the outfield and hide the ball's shadow (proposed) | Fog thickens; lily pads glow (proposed) | Paper lanterns on the water |
| 8 | Coconut Cove | Tropical island | New captain (new faction) | Sand outfield | The tide: waves sweep the corners on a timer and carry a rolling ball (proposed timed mover + redirect) | Tide is higher at night (proposed) | Tiki torches and a lighthouse beam |
| 9 | Sunscorch Mesa | Southern desert canyon | New captain (new faction) | Hard-pan dirt | Dust devils wander the outfield and push a ball in flight (proposed ball redirect) | Clear air: longer carry, no dust (proposed) | Lanterns on the canyon rim, starry sky |
| 10 | Summit Park | High peaks | New captain (new faction) | Alpine grass | Thin air: the ball carries farther; mountain gusts shift the wind each inning (proposed environment rule) | Snow flurries, gusts stronger (proposed) | Cable-car lights and a mountain beacon |

Rules every park keeps (from the fields rails, FD-01 to FD-19): the same diamond; a hazard never awards an out, hit or catch — the ball and the bodies decide the play (FD-08); hazards stay off the base paths (FD-19); hazards can be turned off; Harbor is the only calibrated park.

## Proposed three new captains (draft for W-11 and W-12)

Working titles only. Each new captain founds a faction with about three role players on the captain's body type, so chemistry has something to learn.

| Captain | Faction | Park | Identity sketch |
| --- | --- | --- | --- |
| **Marlo** | Tide Riders (teal / coral) | Coconut Cove | Surfer; speed and contact; star swing rides a wave along the ground |
| **Sable** | Dune Nomads (sand / rust) | Sunscorch Mesa | Desert trickster; a heavy sinker; star pitch kicks up a sand veil |
| **Hollis** | Peak Guard (white / navy) | Summit Park | Mountain climber; big arm and power; star swing is a high, wind-riding fly |

## Menu (draft for W-13)

The stadium row on the setup screen becomes a **continent map**: the stick moves between park pins, each pin shows the park's postcard, captain crest and hazard line, and South confirms. Day / night and hazards stay rows on the same screen. The map is one screen for one and two pads; either pad drives it in 2P as today.

## Decisions (answer one at a time; a letter is enough)

Each has options and my recommendation. The recommendation is not the decision.

**Gating**

- **W-01 — When does this start?** AGENTS.md defers extra parks (#37) until Exhibition is the reason people stay, and #346 (Jack learns Exhibition from How to play) is open. (A) plan now, build after #346 passes; (B) plan now and build the rails and greyboxes now, art after #346; (C) build everything now. *Recommend B.*
- **W-02 — Art order.** FD-17 says a park earns art only after Jack sits its greybox. (A) keep FD-17: all ten as greyboxes first, then art one park at a time; (B) art for the four new parks as they are built. *Recommend A.*
- **W-03 — Backdrops return?** FD-16-R2 removed the hand-built backdrops. A continent wants each park to show its region past the fence (the volcano behind Ember Keep, the sea behind Coconut Cove). (A) backdrops come back as Blender kit art per park, behind W-02; (B) data-driven greybox backdrops now (a palette and a silhouette row per region); (C) no backdrops. *Recommend B, then A.*
- **W-04 — Unlocks.** (A) all ten open from the start; (B) Harbor plus a few open, the rest unlocked by play; (C) all open in Exhibition, unlocks only in a later Challenge mode. *Recommend C.*

**The world**

- **W-05 — The continent's shape and name.** The sketch above, or another layout. Includes whether the beach is an island or a coast.
- **W-06 — Which ten parks.** The table above, or swap in Haunt Manor, Cruise Deck or Playroom for one of Stillwater Marsh, Sunscorch Mesa or Summit Park. *Recommend the table: every region is visibly different, and a desert and a mountain are new for the genre.*
- **W-07 — Fenn's field.** (A) Stillwater Marsh, Fenn's own park; (B) Fenn keeps sharing Harbor, and the tenth park goes to a fourth new captain. *Recommend A: one captain, one field.*
- **W-08 — One gimmick per park.** (A) keep the parks.md rule: one primary hazard plus a night change; (B) allow two hazards per park. *Recommend A.*
- **W-09 — New hazard patterns.** The tide, dust devils and fog need behaviour the pattern library does not have (a wave that moves a rolling ball on a timer, a moving redirect for a ball in flight, a vision effect). (A) add the patterns to the closed library, each with its own row and tests; (B) only reuse the existing patterns. *Recommend A, one pattern per child.*
- **W-10 — Night lights.** Today every park has lights on at night and the look is data (`looks.json`). (A) each park gets a themed night light rig (towers, torches, neon) as kit art, plus its night look row; (B) night looks only, no light props; (C) night changes the play light, too (for example, a darker outfield). *Recommend A. C would reopen FD-11-R2.*

**Captains**

- **W-11 — Three new captains.** Names, factions, colors and identity: the sketch above or Jack's own. Includes the beach captain.
- **W-12 — Role players.** (A) each new faction gets three role players now, like the others; (B) captains first, role players later. *Recommend B, to keep one rig and one art queue (AGENTS.md).*
- **W-13 — Home field.** Today "home park" only means where a faction's captain plays at home. (A) keep it cosmetic; (B) a small home-field edge (for example, a crowd boost to the Star meter). *Recommend A until Jack wants balance.*

**Menu**

- **W-14 — The picker.** (A) a continent map with park pins; (B) keep the left/right cycle and add a map postcard; (C) both: map in Exhibition, cycle in quick rematch. *Recommend A.*

## How it would be built (after the decisions)

Children, in order. Each is one issue and one worktree. The session kind is in brackets.

1. **World data rail** [Gameplay]: a `regions` catalog (id, name, map position) and a park's `region`; ten `pickOrder` places; `faction` stays unique. No art.
2. **New captains in data** [Gameplay]: three captains and factions, stats, abilities from the existing library, chemistry rows. They draw on the one rig with a palette.
3. **Four new park files** [Gameplay]: dimensions, fence, surface, wind, environment and hazards from existing patterns, each at Harbor parity and off the base paths.
4. **New hazard patterns** [Gameplay], one child each (W-09): tide, dust devil, fog.
5. **Greyboxes** [Presentation]: the four new parks in the field kit and the kit bowl, with palettes and night looks.
6. **Continent map picker** [Presentation]: the stadium row becomes the map (W-14), `docs/how-to-play.md` and `HowToPlay.cs` updated in the same PR, both pads.
7. **Tutorial coverage** [Gameplay]: a lesson per new hazard (`docs/tutorials.md`).
8. **Greybox sittings** [Jack], one park at a time (FD-17).
9. **Art** [Art], one park at a time after its sitting: backdrop, night light rig, dress (W-02, W-03, W-10).

Balance (park factors, S-29) runs only when Jack starts it.
