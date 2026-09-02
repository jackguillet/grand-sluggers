# Systems

How Grand Sluggers plays, independent of Unity. Numbers here are **starting points**; the sim in `src/` is the executable spec. Couch buttons and Exhibition flow: `docs/how-to-play.md`.

Presentation feel (camera shots, charge seconds, smash freeze) lives in `data/feel/`. Art slots (skins, clips, VFX, audio, park kits) live in `data/art/`. Baseball rules stay in Sim. See `docs/art-rails.md`.

## Match shape

- 9 vs 9. Positions: P, C, 1B, 2B, 3B, SS, LF, CF, RF.
- 3 / 6 / 9 innings (default 3 for party, 9 for “real”).
- Walks, strikeouts, fly outs, ground outs, force, tags, steals (simplified), errors.
- Mercy optional (10 runs after 3).
- Pitcher stamina. Swap pitcher = swap a fielder onto the mound.

## Stats (shown 1–10)

| Stat | Drives |
| --- | --- |
| Pitch | Velocity, break, stamina pool |
| Bat | Contact window size *and* exit-velo cap (internal split: Contact, Power) |
| Field | Range radius, catch window, jump height |
| Run | Sprint, steal attempt window, home-to-first |

Handedness: bats L/R, throws L/R. Authored, not flipped at draft.

Captains have a **Star Pitch** id, **Star Swing** id, and **Field Ability** id. Role players have a **Star Style** (`fastball` / `changeup` / `breaker` and `ground` / `fly` / `line`) plus a field ability.

## Chemistry

Three states: `good`, `neutral`, `bad`. Stored as a sparse pair list in `data/chemistry/` plus faction defaults:

- Same faction → good, unless an override says otherwise.
- Listed rivals → bad.
- Listed buddies (cross-faction) → good.
- Else → neutral.

### Draft: starting stars

Exhibition pregame is two picks: captains, then the field. Cycling a captain does not move the park (`ExhibitionPick`).

Exhibition lineup is two screens (`LineupScreens`): **Team Setup** (home nine along the top, away nine along the bottom, pool of heads in the center) then **Offense / Defense Setup** (batting 1–9 as a bar of heads, two fielding diamonds with gloves on P / C / 1B / 2B / 3B / SS / LF / CF / RF). Chemistry is hearts and scribbles vs the captain. Average the roster’s chemistry score with the **captain** (good=100, neutral=50, bad=10), then:

| Average | Stars |
| --- | --- |
| ≥ 70 | 5 |
| ≥ 55 | 4 |
| ≥ 35 | 3 |
| ≥ 15 | 2 |
| > 0 | 1 |
| 0 | 0 |

### In play

- **Good throw:** 1.35× throw speed, accurate, “buddy” VFX. Enables Buddy Jump if both are outfielders near a would-be homer.
- **Bad throw:** 0.7× speed, extra lateral error. 25% chance to become a true error (ball away).
- **Chemistry at-bat:** if batter and on-deck are good, roll an **error item** the batter can throw at a fielder after contact.
- **Buddies on base:** +10% / +25% / +50% to charge power with 1/2/3 good-chem runners on.

Buddy Badge (rare gear) treats all pairs as good for one game. Do not put it in the default draft.

## Star meter

- 0–5 stars, shared by the team.
- Spend 1 for the acting player’s star skill. A *guest* captain (not the team’s captain) spends 2.
- Gain 0.5–1.0 on: hit, extra-base hit, strikeout, double play, robbed homer, park-feature hit.
- Star skills **cannot** be a free home run. They change the ball or the field.

## Batting (arcade)

Pitch and swing share tap / charge / modifier / star. Charge fills to MAX then **decays**. The sweet-spot oval at the plate is smaller than the zone; walk the box so it overlaps the ball.

The batter has a **contact window** around a pitch’s plate time. Timing offset maps to quality:

| Timing | Quality |
| --- | --- |
| inside ±1 frame of ideal (at 60 Hz) | Perfect — highest exit velo, true launch |
| inside the character’s window | Solid |
| late/early edge | Cheap — dribbler or pop |
| outside | Miss (strike) |

Charge (hold, then swing) trades a smaller window for more power. Contact-first bats invert that (Waluigi-style: charge *helps* contact, hurts power).

Launch angle is stick/aim + a little RNG, clamped by quality. We simulate flight with a ballistic + drag + park wind, not a full CFD.

## Pitching

A tap is a fastball-shaped pitch. Charge makes it faster (MAX then decay). Stick after release is break. West through release is a changeup (hang then dump). Captain Star Pitch is both-face + commit.

Walk the rubber for location. Pickoff is a named bag before the pitch. Tired pitchers (`PitcherTired`) spray.

Stamina 0 → extra break noise and “fat” fastballs. Swap or eat runs.

## Fielding

CPU fielders run a simple utility: while the ball is in the air they run to the landing (or wall plant), not the live XZ — chasing the ball itself is the home-first path as it flies over them. Once it is a hopper or already down they chase the live hop. Gloves stay inside that park's fence (`FieldBounds` from the JSON L/C/R distances) — they plant on the warning track, they do not run through the wall. A yellow circle on the grass (`LandingMark`) is that landing; it turns red in the jump window. Catch window from Field stat, throw to the lead base with chemistry applied. Infield dirt is the infielder's hop; once the ball is on the grass the nearest outfielder charges and takes the glove (`PlayGlove`).

Player fielding: move the highlighted fielder, catch button, throw-to-base buttons. Buddy Jump is a timed prompt when two good-chem outfielders are under a homer.

Field abilities fire as verbs. One per character:

| Verb | Who | Effect |
| --- | --- | --- |
| Super Jump | Nico, Gull | Extra fly range; can rob a just-over homer |
| Lick Catch | Zig, Dart, Jester | Bigger catch radius |
| Grow | Rio | Bigger catch radius |
| Dive | Marlow, Lace, Basil | Extra grounder range |
| Burrow | Soot | Grounder range; ignores ice/lava slow |
| Clamber | Konga, Vine, Moss | Wall rob at Canopy Yard |
| Snap Throw | Vale, Frost, Pip, Pewter | Faster throws |
| Laser | Brondo, Boom, Hex, Nugget | Fastest throws |
| Spin Check | Ashlord, Cinder, Grit | Knocks an extra-base hit down a bag |

## Steals

Control a **named runner**. Default highlight is the lead runner (furthest along). D-pad / 1–3 selects (right 1B, up 2B, left 3B; home is not stealable). Stick toward the next bag takes a visible lead (`Lead01` 0–1 on that bag); stick back returns. **LB / `,`** sends everyone; **RB / `.`** returns everyone; both (or `/`) freeze. **L3 / Z** arms a steal on the selected runner toward their next bag. They go on the pitch. More lead = a better jump and more pickoff risk on a take. After a take or swing-and-miss the steal is a **live catcher throw** to the bag (`StealThrow`) — arm 2B (default on a steal of second) and South. Early + beat the runner is caught stealing; late is a stolen base. Dead stick: CPU catcher still guns. Take the stick and you own it. Pickoff stays a named bag + South **before** the pitch. Walks and strikeouts cancel. Fair contact always sends the batter to first. On a fly, runners hold unless all-advance is on (then they tag up). Mini diamond shows leads. No steal home in this pass.

## Error items (chemistry batting)

If batter and on-deck are good chemistry, the offense throws a physical item **after contact**, during the fly, aimed at a fielder you can see. CPU still rolls banana / rocket / POW about 40% of the time.

| Item | Effect (this pass) |
| --- | --- |
| Banana | Peel on the grass at the play fielder's feet — a would-be out becomes a single |
| Rocket | Hit that fielder's body — 55% chance they are dazed and drop |
| POW | Infield hop — ground outs become singles |

Aim with the stick, confirm with E / LT+RB / South+LT. Cycle banana / rocket / POW with RB. Pre-pitch E arm is not the product path. Smoke / ghost / paint are still banned as full-screen or queued. See research notes.

## Gear

Loadout slots: **Bat**, **Glove**, optional **Shoes**.

Each item is JSON: stat mods, a tag that changes a system (`always_full_charge`, `chem_all`, `item_speed`), and a visual id. Signature bats are characters’ defaults and can be swapped.

## Parks

A park JSON lists: dimensions (fences), wind, surface (`grass` / `ice` / `deck` / `dirt`), hazards, day/night flag. Hazards are ids the sim ticks: freeze volumes, warp pipes, lava spitters, tilt, blackout. See [parks.md](parks.md).

## MVP

After the game, score plays not just box stats: robbed homers, buddy jumps, star-skill Ks, chemistry items that matter, stolen runs. Show one highlight. Cheap, do it in the slice.
