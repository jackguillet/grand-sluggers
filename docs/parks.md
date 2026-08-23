# Parks

One park, one primary gimmick. Harbor Diamond has none — it is the control map and the vertical-slice park.

Dimensions are feet, approximate, MLB-ish but cartoon-short in the corners so homers happen.

## Harbor Diamond (slice)

- Faction: Spark League
- Surface: grass
- Gimmick: none
- Night: fireworks on homers, same play
- Fence: 330 / 400 / 330
- Why it exists: teach baseball before we teach gimmicks

## Crystal Rink

- Faction: Royal Rink
- Surface: ice (we do **not** make players skate — Sluggers’ manual said they slip, the wiki says they don’t. We pick: **no skating**, freeze *hazards* instead)
- Gimmick: **Freezers** — statues on the dirt. Touch one, frozen 1.2s. Can be shattered by a charged runner or a line drive.
- Night: ceiling snowflakes. Hit one → stadium lights cut, follow-spot on the ball for 2s.

## Funfair Park

- Faction: Carnival Crew
- Gimmick: **Warp cans** in the infield. A grounder that enters one exits another at random (tagged so it can be learned, not pure grief).
- Warning-track **train** — periodic moving catch block / launch.
- Night: **Chompers** in the outfield eat flies and spit them elsewhere.

## Rooftop City

- Faction: Goldrush
- Gimmick: billboards and AC units. Balls can carom; some signs award a star if you hit them.
- Night: neon, same geometry, extra glare (not a blind).

## Canopy Yard

- Faction: Canopy Clan
- Gimmick: climbable outfield walls (Clamber characters only). Barrel cannons that re-fire a caught ball if you are sloppy.
- Night: fireflies, same play, monkeys in the trees as catch interference.

## Haunt Manor

- Unlock. Night only.
- Gimmick: ghosts that possess a random fielder for a pitch (inputs invert or delay). Lights flicker — readability first, scare second.

## Cruise Deck

- Unlock. Ship.
- Gimmick: **list**. At inning 4 (or night), the deck tilts; ground balls drain to one foul line. Occasional splash hazard in the corners.

## Ember Keep

- Faction: Ember Keep. Night only for the “real” version; day is a locked-in practice courtyard if we need it.
- Gimmick: lava pits with hoppers that knock the ball (and the fielder) if they land. Captain statue breathes fire across CF after a star swing lands in play.

## Playroom

- Unlock. Day only.
- Gimmick: toy blocks as infield geometry, crayon walls that smear a caught ball’s throw vector. Chaotic on purpose — party park.

## Toy Box (not baseball)

Optional later: a point-space minigame park like Sluggers’ Toy Field. Out of scope until Exhibition is fun.

## Authoring a park

`data/parks/*.json` — id, name, dimensions, surface, wind, hazards[]. A hazard is `{ "type": "freeze_volume", "x": ..., "z": ..., "radius": ... }` etc. The sim ticks types it knows and ignores the rest with a warning. Unity draws whatever the id says.
