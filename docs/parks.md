# Parks

One park, one primary gimmick. Harbor Diamond has none — it is the control map and the vertical-slice park.

Dimensions are feet, approximate, MLB-ish but cartoon-short in the corners so homers happen.

## Harbor Diamond (slice)

- Faction: Spark League
- Surface: grass
- Gimmick: none
- Night: fireworks on homers, same play (no sim rule)
- Fence: 330 / 400 / 330
- Why it exists: teach baseball before we teach gimmicks. Control park **and** trailer still: afternoon light, warning track, dugouts, backstop, bleachers with crowd in the seats. Town sits beyond the fence, not in other parks.

## Crystal Rink

- Faction: Royal Rink
- Surface: ice (we do **not** make players skate — Sluggers’ manual said they slip, the wiki says they don’t. We pick: **no skating**, freeze *hazards* instead)
- Gimmick: **Freezers** — statues on the dirt. Touch one, frozen 1.2s. Can be shattered by a charged runner or a line drive.
- Night: blackout. Contact window × 0.85. Follow-spot on the ball (presentation).
- Why it exists: an ice garden, not Harbor with cyan cylinders. Same diamond kit (bags, mound, foul lines, fence). Glass boards, frozen fountain, freeze statues (body + pedestal) you walk around, royal palace beyond CF. Cool light, not Harbor afternoon. Spark lofts stay in Harbor.

## Funfair Park

- Faction: Carnival Crew
- Surface: grass
- Gimmick: **Warp cans** in the infield. A grounder that enters one exits another at random (tagged A/B/C so it can be learned, not pure grief).
- Warning-track **train** — parked boxcar you can read. Periodic catch block / launch stays in the sim notes; presentation does not sit in the dirt.
- Night: **Chompers** in the outfield eat flies (fly out).
- Why it exists: cans with mouths you can learn, not green cylinders. Same diamond kit (bags, mound, foul lines, fence). Carnival tents, striped poles, ferris wheel, booths beyond CF. Warm carnival light, not Harbor afternoon, not Crystal ice. Spark lofts and the royal palace stay out.

## Rooftop City

- Faction: Goldrush
- Surface: dirt (tar roof)
- Gimmick: billboards and AC units. Balls can carom; some signs award a star if you hit them.
- Night: neon glare (not a blind). Presentation only; day already uses dusk/neon light.
- Why it exists: it should feel like a roof. Urban rooftop geometry, star billboards, AC boxes you can carom off. Same diamond kit. Spark lofts, royal palace, and carnival tents stay out.

## Canopy Yard (playable)

- Faction: Canopy Clan
- Surface: dirt
- Fence: 312 / 378 / 318
- Gimmick: **Barrel cannons** warp grounders (same rule as Funfair cans). **Climb wall** — fielders with Clamber (Konga, Vine, Moss) can rob a homer that only just cleared the fence.
- Night: fireflies, same play. Presentation only.
- Why it exists: jungle walls you can clamber, barrels you can see kick a grounder. Trees, vine walls with ledges at fence height, barrel-cannon actors (mouths + tags, not anonymous cylinders). Same diamond kit. Spark lofts, royal palace, and carnival tents stay out.

## Haunt Manor

- Unlock. Night only.
- Gimmick: ghosts that possess a random fielder for a pitch (inputs invert or delay). Lights flicker — readability first, scare second.

## Cruise Deck

- Unlock. Ship.
- Gimmick: **list**. At inning 4 (or night), the deck tilts; ground balls drain to one foul line. Occasional splash hazard in the corners.

## Ember Keep (playable)

- Faction: Ember Keep
- Surface: ash
- Fence: 338 / 408 / 338 (deep)
- Gimmick: **Lava pits** and the captain **statue's fire breath** slow fielders the same way Crystal freezers do. Not a free homer park — Ashlord still has to square it up.
- Night: fire breath radius × 1.6. Extra braziers, brighter fire. Courtyard lighting is night-ready even in day.
- Why it exists: a keep that breathes fire, not a dark cube with a sphere. Castle architecture, lava that reads as a pit (rim + glow), fire-breath statues as actors. Same diamond kit. Spark lofts, royal palace, and carnival tents stay out.

## Playroom

- Unlock. Day only.
- Gimmick: toy blocks as infield geometry, crayon walls that smear a caught ball’s throw vector. Chaotic on purpose — party park.

## Toy Box (not baseball)

Optional later: a point-space minigame park like Sluggers’ Toy Field. Out of scope until Exhibition is fun.

## Authoring a park

`data/parks/*.json` — id, name, dimensions, surface, wind, hazards[]. A hazard is `{ "type": "freeze_volume", "x": ..., "z": ..., "radius": ... }` etc. The sim ticks types it knows and ignores the rest with a warning. Unity draws whatever the id says.
