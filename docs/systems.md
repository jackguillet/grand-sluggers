# Systems

How Grand Sluggers plays, independent of Unity. Numbers here are **starting points**; the sim in `src/` is the executable spec.

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

Average the roster’s chemistry score with the **captain** (good=100, neutral=50, bad=10), then:

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

Pitch types: fastball, changeup, curve, slider, plus captain Star Pitch.

Each pitch has: speed, break vector, stamina cost, and a **tell** (subtle) so a good batter can read.

Stamina 0 → extra break noise and “fat” fastballs. Swap or eat runs.

## Fielding

CPU fielders run a simple utility: intercept point of the ball, catch window from Field stat, throw to the lead base with chemistry applied.

Player fielding (when we add it): move the highlighted fielder, catch button, throw-to-base buttons. Buddy Jump is a timed prompt when two good-chem outfielders are under a homer.

Field abilities fire as verbs: extra jump, wall climb, teleport to ball, stretch catch, laser throw. One verb per character.

## Error items (chemistry batting)

Thrown *after* contact, aimed at the defense:

| Item | Effect |
| --- | --- |
| Banana | Slip; drop if they were about to catch |
| Rocket | Homing daze |
| Smoke | Brief vision cone cut (not a full-screen blind) |
| POW | Everyone on the dirt jumps; pop-ups become infield chaos |
| Ghost | Ball invisible 1.5s, shadow remains |
| Paint | Splatter at a *point*, not the whole screen |

Full-screen blinds are banned. See research notes.

## Gear

Loadout slots: **Bat**, **Glove**, optional **Shoes**.

Each item is JSON: stat mods, a tag that changes a system (`always_full_charge`, `chem_all`, `item_speed`), and a visual id. Signature bats are characters’ defaults and can be swapped.

## Parks

A park JSON lists: dimensions (fences), wind, surface (`grass` / `ice` / `deck` / `dirt`), hazards, day/night flag. Hazards are ids the sim ticks: freeze volumes, warp pipes, lava spitters, tilt, blackout. See [parks.md](parks.md).

## MVP

After the game, score plays not just box stats: robbed homers, buddy jumps, star-skill Ks, chemistry items that matter, stolen runs. Show one highlight. Cheap, do it in the slice.
