# Parks

One park, one primary gimmick. Harbor Diamond has none — it is the control map and the vertical-slice park.

**Read this first (September 21, 2026).** The park entries below are **design intent**. They are not what the sim does. What a park changes today, what each hazard does today, and what is still missing are in [gameplay-spec.md](gameplay-spec.md) §14, checked against the code at `d0c6e12c`. The contract the parks are being rebuilt to is spec §0.3 (D21), decided in [plan-fields.md](plan-fields.md) ([#814](https://github.com/jackguillet/grand-sluggers/issues/814)): a park is Harbor plus the differences it names; rails and greyboxes first; Crystal Rink first; no park art before a park's greybox sitting. Where an entry below disagrees with a D21 decision, the decision wins and the entry carries a note.

Dimensions are feet, approximate, MLB-ish but cartoon-short in the corners so homers happen.

## Harbor Diamond (slice)

- Faction: Spark League
- Surface: grass
- Gimmick: none
- Night: fireworks on homers, same play (no sim rule)
- Fence: 330 / 400 / 330
- Why it exists: teach baseball before we teach gimmicks. Control park **and** trailer still: afternoon light, warning track, **sunken dugouts with stairs** set back off the dirt, backstop, bleachers with crowd in the seats. Town sits beyond the fence, not in other parks. Dirt is **paths and pads** (`HarborInfield`), not a lake. Bags are diamond-aligned squares. Lawn holes stay on `HarborDugout`.

## Crystal Rink

- Faction: Royal Rink
- Surface: ice (we do **not** make players skate. The reference disagrees with itself: an in-game card says players slip, the wiki says they do not; the manual was not readable. We pick: **no skating**, freeze *hazards* instead. FD-04: the ice may lengthen a body's start, brake and cut-back a little, with control kept; full traction is a held trial for Crystal's greybox sitting. That is how "no skating" stays true (F3-d, #857): a ground row's `body` multipliers scale only the *times* the §8 response law takes to start, stop and turn a body and the slide and overrun *lengths* at a bag — never a body's top speed, its heading or where the stick points — so a fielder on ice goes exactly where he is steered, only slower to get going, to stop and to turn. Every row carries 1.0 until Crystal's trial (F9-a) names its numbers, and on the shipped root, where the response law is off, the ice cannot touch a fielder at all. Today `surface` names the park's outfield ground in the closed library (#846) and still changes no play: every ground row is today's number, and the flight reads its own copy until F3-c)
- Gimmick: **Freezers** — statues on the dirt. Touch one, frozen 1.2s. Can be shattered by a charged runner or a line drive. *Intent, not built:* today a ball that **lands** in a freezer's disc slows every chaser for the whole play; nobody touches a statue, there is no timer and no shatter. Two of the three discs sat on a running lane, which FD-19 forbids once hazards act on bodies. ✅ F4-e (#862, FD-19-R1): each moved outward from home along its own bearing to the smallest whole-foot place that clears on both roots, size kept — (40, 70) → (53, 93) and (−45, 90) → (−49, 97), and the deep one (10, 180) → (10, 187) because its trial twin sat on second base's pad; on `trials/c80` (47, 83), (−44, 86), (7, 131). The two shallow statues now stand past the base paths, on the dirt between first and second and between second and third. Where they should stand for play is F9-a's. Map §5 Q7 answered (FD-08-R2): a touch slows the body for 3 s, not 1.2.
- Night: blackout. Contact window × 0.85. Follow-spot on the ball (presentation). The × 0.85 is built (`nightContactWindowMul`). It has no reference source — the reference blackout starts only when a batted ball touches a ceiling star, and it acts on fielding, not on the at-bat — and it is reviewed when Crystal's night block is written (FD-11). Map §5 Q9 answered (Jack, September 22, 2026, FD-11-R1): **drop** — Crystal's trial night block carries no contact-window change; the shipped Crystal keeps 0.85 until a single default exists (FD-13-R1).
- Why it exists: an ice garden, not Harbor with cyan cylinders. Same diamond kit (bags, mound, foul lines, fence). Glass boards, frozen fountain, freeze statues (body + pedestal) you walk around, royal palace beyond CF. Cool light, not Harbor afternoon. Spark lofts stay in Harbor.

## Funfair Park

- Faction: Carnival Crew
- Surface: grass
- Gimmick: **Warp cans** in the infield. A grounder that enters one exits another at random (tagged A/B/C so it can be learned, not pure grief). *Half built:* today only the preview landing moves; the live ball keeps its path. A random exit is allowed (FD-08), but the ball must really travel there, and the draw never decides the out (FD-08-R1).
- Warning-track **train** — parked boxcar you can read. Periodic catch block / launch stays in the sim notes; presentation does not sit in the dirt. *Not built:* `train` has no sim effect and its `periodSec` is not read. The reference documents no effect for its train either.
- Night: **Chompers** in the outfield eat flies (fly out). ✅ F4-a (#847): three `chomper` rows in `data/parks/funfair-park.json` at (−72, 205) r 16, (0, 228) r 18 and (78, 198) r 16 — the places the code literals stood — dispatched by the `catchStealer` pattern with `nightOnly` true, not by the park's id. `trials/c80` carries them migrated by the same zone rule as every other hazard, so on a compact field they sit at 139–160 ft and are in play; at their literal depth they were not, which is why Funfair's night row moves on the trial and nowhere else. Map §5 Q8 answered (Jack, September 22, 2026, FD-09-R2): the chomper becomes a **ball redirect** and never makes an out; built with F4-c when Funfair comes up.
- Why it exists: cans with mouths you can learn, not green cylinders. Same diamond kit (bags, mound, foul lines, fence). Carnival tents, striped poles, ferris wheel, booths beyond CF. Warm carnival light, not Harbor afternoon, not Crystal ice. Spark lofts and the royal palace stay out.

## Rooftop City

- Faction: Goldrush
- Surface: dirt (tar roof)
- Gimmick: billboards and AC units. Balls can carom; some signs award a star if you hit them. *Partly built:* every billboard awards a star when a ball lands in its disc (the `star` tag is not read); nothing caroms; `ac_unit` has no sim effect, and Unity draws two AC units that are not in the data.
- Night: neon glare (not a blind). Presentation only; day already uses dusk/neon light.
- Why it exists: it should feel like a roof. Urban rooftop geometry, star billboards, AC boxes you can carom off. Same diamond kit. Spark lofts, royal palace, and carnival tents stay out.

## Canopy Yard (playable)

- Faction: Canopy Clan
- Surface: dirt
- Fence: 312 / 378 / 318
- Gimmick: **Barrel cannons** warp grounders (same rule as Funfair cans). **Climb wall** — fielders with Clamber (Konga, Vine, Moss) can rob a homer that only just cleared the fence. *Partly built:* `climb_wall` is a flag for the whole park; its place and size are not read, and it also widens a Clamber fielder's catch anywhere in the park. It becomes a property of a wall span (FD-06). `tree` has no sim effect.
- Night: fireflies, same play. Presentation only.
- Why it exists: jungle walls you can clamber, barrels you can see kick a grounder. Trees, vine walls with ledges at fence height, barrel-cannon actors (mouths + tags, not anonymous cylinders). Same diamond kit. Spark lofts, royal palace, and carnival tents stay out.

## Haunt Manor

- Unlock. Night only.
- Gimmick: ghosts that possess a random fielder for a pitch (inputs invert or delay). Lights flicker — readability first, scare second. *Deferred:* no game surveyed disrupts inputs, and the reference's haunted park uses fixed gravestones with a trigger range plus tall grass. It needs a pattern the hazard library does not have (FD-09).

## Cruise Deck

- Unlock. Ship.
- Gimmick: **list**. At inning 4 (or night), the deck tilts; ground balls drain to one foul line. Occasional splash hazard in the corners. *Deferred:* no `tilt` type exists. In the reference the tilt is night only, near the middle of the game; the day gimmick is breakable tables and short walls that give ground-rule doubles.

## Ember Keep (playable)

- Faction: Ember Keep
- Surface: ash
- Fence: 338 / 408 / 338 (deep)
- Gimmick: **Lava pits** and the captain **statue's fire breath** slow fielders the same way Crystal freezers do. Not a free homer park — Ashlord still has to square it up. *As built:* the same landing-point slow as Crystal. `fire_breath` is its own disc 60 ft in front of the `statue`, and `statue` has no sim effect. The two shallow lava pits sat on a running lane (FD-19). ✅ F4-e (#862, FD-19-R1): each moved outward from home along its own bearing to the smallest whole-foot place that clears on both roots, size kept — (38, 78) → (49, 100) and (−42, 96) → (−45, 104); on `trials/c80` (44, 89) and (−40, 92). The first-base-side pit now stands past the first–second lane rather than on it.
- Night: fire breath radius × 1.6. Extra braziers, brighter fire. Courtyard lighting is night-ready even in day.
- Why it exists: a keep that breathes fire, not a dark cube with a sphere. Castle architecture, lava that reads as a pit (rim + glow), fire-breath statues as actors. Same diamond kit. Spark lofts, royal palace, and carnival tents stay out.

## Playroom

- Unlock. Day only.
- Gimmick: toy blocks as infield geometry, crayon walls that smear a caught ball’s throw vector. Chaotic on purpose — party park. *Deferred.* The reference's playroom has no infield geometry: outfield floor pictures spawn chasers, and the walls are very high. Blocks on the infield must also pass FD-19.

## Toy Box (not baseball)

Optional later: a point-space minigame park like Sluggers’ Toy Field. Out of scope until Exhibition is fun.

## Authoring a park

`data/parks/*.json` — id, name, faction, pickOrder, dimensions, surface, wind, fence height, hazards[], notes, and the two optional blocks `environment` (the park's air) and `zones` (the ground of its four zones). A hazard is `{ "type": "freeze_volume", "x": ..., "z": ..., "radius": ... }` etc. The types are a closed library and each one has an authored row in `data/rules/hazards.json` naming the pattern it plays (F4-a, #847); a type outside the library and a type with no row are different errors and neither loads. **A hazard stays off the base paths** (FD-19, `SF-23`, F4-e #862): `cli art` refuses a disc that crosses a running lane (10 ft wide, bag to bag), the mound-to-plate lane, a bag's 12-ft pad, the mound or the 18-ft plate area, on the root's own diamond, and names the file, the hazard's index and type, and what it crosses by how many feet. The disc is the hazard's own `radius`; a warp can's or barrel's `reachPadFt` is not counted. `faction` says whose park it is: the captain of that faction plays here at home, and no two parks may name one faction. `pickOrder` is this park's place in the pregame field-pick cycle — required, an integer, and no two parks may share one, because a directory listing is alphabetical and the cycle is authored. `notes` is prose for whoever opens the file; no rule reads it.

**As built (F1-a, #820).** An unknown hazard *type* stops the load, and so does an unknown *key*, in the park object and in a hazard row: the error names the file and the key, the way a rule table's does (spec §16). The dead fields are resolved — `nightOnly`, `dayOnly` and the train's `periodSec` are gone from all twelve files, and FD-11's night block and F4-f's mover row will bring back what a park needs. The park list, its cycle order and the home-park map are data, not lists in code, and a park id the catalog does not have is a stop rather than a silent Harbor. Nothing is ticked; each hazard is still tested once against the ball's landing point (spec §14). Unity draws Harbor from `HarborKit` and every other park from an older primitive diamond plus one `ParkView` method per park id, so "same diamond kit" in the entries above is intent, not fact. A trial root (`trials/c80`) carries its own copy of every park file, key for key.

**Target (spec §0.3, [plan-fields.md](plan-fields.md)).** Unknown fields refused. A park names only what differs from Harbor: environment, ground zones, a fence polyline with wall materials, foul territory, outfield starts, hazard instances from a closed pattern library, a night block. Positions are written relative to the diamond and the fence so one authoring serves both data roots. One field kit draws every park; a park fills slots; empty slots draw a greybox.

### Fields the flight reads (spec §6.1)

| Field | Meaning |
| --- | --- |
| `leftFenceFt` / `centerFenceFt` / `rightFenceFt` | The three posts. The fence between the poles is the circle through them (`AtBatResolver.RoundFence`); the foul wraps (hip rail 36 ft off each line, round backstop 36 ft behind the plate) are the shared diamond kit and are not per-park fields. |
| `fenceHeightFt` | The top of the outfield fence. A flight that meets it below the top caroms; above it between the poles is a home run; a ball that bounced first and then clears it is a ground-rule double. Required, greater than 0. |
| `windMph` | Flag reading. The ball feels `flight.windMul` of it (drag is taken relative to the wind). |
| `windDeg` | Where the wind blows **toward**, in the field frame: `0` out to center, `90` toward the right-field line (first-base side), `180` in at the plate, `270` toward left. The wind bends the path, so a ball's landing spray is not its spray at contact. |
| `environment.dragMul` | *Optional.* Multiplies the root's `flight.drag` for this park — thicker air is a shorter carry off the same launch. Absent is the global drag; a value must be greater than 0 and at most 4. No park names one. |
| `environment.windMul` | *Optional.* Replaces `flight.windMul`, the fraction of the flag reading the ball feels at field level. `0` is a park the wind does not reach (the flag still reads `windMph`). Absent is the global exposure; a value must be in [0, 1]. No park names one. |
| `surface` | The ground of the park's **outfield** (and, through the derived map below, of its foul apron). It must be an id the ground library has a row for — `grass`, `dirt`, `ice` or `ash`, one named row each in `data/rules/grounds.json`; a surface with no row stops the load and names the id and both files. Required. It still changes no play: every ground row is today's number, and the flight reads its own copy until F3-c. |
| `zones.infieldDirt` | *Optional.* The ground inside the infield lip. Absent is `dirt`. |
| `zones.outfield` | *Optional.* The ground past the lip and short of the warning track. Absent is the park's `surface`. |
| `zones.warningTrack` | *Optional.* The ground within a track width of the fence. Absent is `dirt`. |
| `zones.foulApron` | *Optional.* The ground outside the chalk, the ground behind the plate included. Absent is the park's `surface`. |

**The park's air (spec §0.3 D21, FD-03, #827).** The whole `environment` block is optional and so is each of its two fields, because a park is Harbor plus the differences it names: a match plays on `content.Rules.AtLevel(difficulty).AtPark(park)`, and a park that names no environment resolves to the global table **itself**, the same object. So Harbor's air is the global air by construction, on the shipped root and on `trials/c80`, and no park below names a value — the first one is a measured trial (Crystal, F9-a), not a schema change. The block is air only: the roll, the bounce and the wall carom become ground and wall-material rows with their own libraries, and gravity, the three time scales and the plate stay global in every park (one clock, D21).

**The park's ground (spec §6.1, §16; FD-05, #846).** A park's ground is four zones taken from the diamond every park already shares: the dirt inside the infield lip, the grass beyond it, the warning track against the fence, and the foul apron outside the chalk. Each zone names a ground from the closed library in `data/rules/grounds.json`, and each defaults so that `surface` keeps the meaning it has always had — **the outfield and the foul apron are the park's `surface`, the infield and the track are `dirt`**. A park may override any of the four with an optional `zones` block; **no park below names one**, and every row of the library is today's `flight.json` number, so the four zones of every park behave identically and this table changed no play. The first ground that is not today's arrives with Crystal (F9-a), as a measured trial.

Where the zones *are* is not this file's business: the lip is `flight.classes.infieldLipFt`, the track is `ParkDiamond.TrackWidth` inside the fence at that bearing, and the chalk is the ±45° foul line. Those are the numbers the field already had and they belong to #730 / #732; `GroundZones.ZoneAt(x, z)` reads them. The wall is the other library (`data/rules/walls.json`, one row: `padded`), and which span of a fence is made of what — and whether it can be climbed or robbed over — comes with the polyline fence (F2-c), not with the material.

Shipped values:

| Park | Fence | Height | Wind |
| --- | --- | --- | --- |
| Harbor Diamond | 330 / 400 / 330 | 12 ft (the padded wall) | 4 mph toward 20° (a harbor breeze out to right-center) |
| Crystal Rink | 320 / 385 / 320 | 8 ft | 2 mph toward 180° (in) |
| Funfair Park | 315 / 390 / 340 | 8 ft | 6 mph toward 0° (out) |
| Rooftop City | 318 / 388 / 322 | 12 ft (billboards) | 9 mph toward 90° (crosswind to right) |
| Canopy Yard | 312 / 378 / 318 | 12 ft (the climb wall) | 3 mph toward 200° (in, slightly left) |
| Ember Keep | 338 / 408 / 338 | 10 ft (keep wall) | 1 mph toward 0° |

**One number (spec D15, amended by D21 / FD-06: once a park lists fence points, the rule is *drawn equals flight on every span*).** `fenceHeightFt` is both the top the flight clips against and the top of the wall Unity draws: Harbor's padded wall (`HarborKit.DressWall`) reads `HarborWall.OutfieldHeight(park)`, which is the park field, and `HarborWallTests` asserts the drawn top equals the flight's fence on every outfield segment. The foul rail around the dugouts and home stays hip-high (4.2 ft) on both sides. A ball that meets the padding you see caroms off it; a homer clears it. The number is Jack's call; Harbor ships at 12 ft (taller than MLB's 8 so a rob and a carom read, far under the old 26-ft dressing). `fenceHeightFt` must stand over the 4.2 ft rail (`cli art` refuses lower).
