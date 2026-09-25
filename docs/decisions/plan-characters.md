# Character feel plan

Research and decisions: [research-characters.md](../archive/characters/research-characters.md) (CH-01 … CH-14, Jack's picks of September 24, 2026). Parents: [#188](https://github.com/jackguillet/grand-sluggers/issues/188) for the toy (body, motion, shading) and [#209](https://github.com/jackguillet/grand-sluggers/issues/209) for play (stats, size, speed, zone). Baseline: `be5c395a`.

## Current state

All fourteen directions are accepted. **Epics are filed (#1114 … #1120). Nothing is implemented. No number is accepted. No human gate has passed.** The one defect found by the research is filed as [#1111](https://github.com/jackguillet/grand-sluggers/issues/1111) (fly chases play the walk clip). It is not part of this plan and can land first.

Numbers in this file are **derived** (arithmetic on shipped data) or **proposed** (a starting point for a trial). A proposed number becomes a target only when Jack accepts a trial of it. Balance work (S-29, cohorts, seals) stays off until Jack starts a balance pass. Each epic names its moves in the PR body and stops at the breakage suite.

## The agreed contract

Directions only. Each line names its decision.

- **The toy (CH-01, CH-02).** A Sluggers-style toy: big head, fat, readable at ten feet. The shared body is about **4 heads tall** (today 5.3). Judge it in a still before any number is final.
- **The ladder (CH-03).** Heights compress toward Sluggers. The tallest captain is at most **1.35× Rio** (today Ashlord is 1.60×). The shortest is at least **0.70× Rio** (today Zig is 0.62×).
- **Build (CH-04).** Head, Arms and Torso shape the 3D body on the one rig, from each captain's `proportions`. No second rig, no unique mesh.
- **Body classes (CH-05, CH-11, CH-12).** A body class is a data row. It names size in play (ground and fly catch reach, contact width), weight (speed-up and braking ramp, knockback), and a motion style. The table is sized for **about fifteen classes**, so role players can have their own class. Seven ship first, one per captain cut.
- **Size in play (CH-05).** Reach and contact width come from the class row, not measured off the mesh. Grow stays a verb.
- **Strike zone (CH-06).** The zone runs from the batter's **knee** to the batter's **chest**. It scales vertically with the body. Its width over the plate stays fixed (0.92 ft half-width). It is judged at the plate crossing, as today.
- **Stats (CH-07, CH-08, CH-09).** Four visible bars, each the rounded mean of authored sub-stats:

  | Bar | Sub-stats | Code slot today |
  | --- | --- | --- |
  | Bat | contact, power | `Contact`, `Power` |
  | Pitch | power, stamina, control, break | `Velocity`, `Endurance`, `Control`, `Movement` |
  | Field | hands, throw speed | `Hands`, `Arm` |
  | Run | speed | `Run` |

  Every slot exists today and falls back to its bar. The change is to author all nine for every character and derive the bars from them. Pitching power and fielding throw speed stay separate. The budget is loose, with **at most one bar at 9 or higher**.
- **Speed and weight (CH-10, CH-11).** The top-speed gap between the fastest and slowest body is **about 1.25×** on the bases and in the field (today 1.34× and 1.54×). Light bodies speed up fast; heavy bodies speed up slowly.
- **Motion (CH-12).** Each body class has its own style of run, idle, batting stance and windup. Each captain has one signature beat. Every take stays in the one takes script, baked for both hands. The run loop's stride rate follows ground speed.
- **Juice (CH-13).** Anticipation, hit-stop, squash and settle scale by weight class. Squash stays on the presentation wrapper.
- **First art (CH-14).** A real toon shader with a rim light. Extras stay off (#687).
- **Roster (CH-09 note).** Ten captains is the goal. It is **deferred** until CF-1 to CF-5 land and Jack sits the seven. See "Deferred".

## The strike zone, worked through

The rest rig ([rig.json](../../data/art/rig.json)) puts the knee (thigh → shin) at z 1.16 and the torso bone from z 2.95 to 3.65. Proposed chest mark: **z 3.20** on the torso bone, a new `anatomy.chest` landmark beside `anatomy.knee`. World height is rig z × `ToyScale` 1.18 × the captain's height, plus the CH-04 leg and torso scale once CF-2 lands.

| Batter | Scale today | Knee (ft) | Chest (ft) | Zone height (ft) |
| --- | --- | --- | --- | --- |
| Shipped fixed zone | — | 1.45 | 3.65 | 2.20 |
| Rio (H 0.90) | 1.062 | 1.23 | 3.40 | 2.17 |
| Zig at the new floor (0.70× Rio) | 0.743 | 0.86 | 2.38 | 1.52 |
| Ashlord at the new cap (1.35× Rio) | 1.434 | 1.66 | 4.59 | 2.93 |

Derived from the rest rig, before CF-2 changes the body. Rio keeps a zone close to today's. The ladder cap and floor from CH-03 already keep the extremes pitchable. A clamp on zone height is proposed as a safety net, not a design lever. Its numbers are a trial.

Rules the zone must keep:

- The zone is a sim number, read from the **rest** rig and the captain's data. The batting stance and the animation never move it.
- **Aim follows the batter.** A pitch aimed at the middle crosses at the middle of *this* batter's zone. The CPU pitcher, the aim tell, the SET ring, the cursor and the sweet-spot oval read the same zone (spec §4.4, §5). S-108 ("every family crosses inside the zone with no aim") must hold against every batter.
- The same zone for both seats, 1P and 1v1, every park.

## Epics

Order: **CF-1** and **CF-2** run in parallel (no shared files). **CF-3** needs CF-2's landmarks. **CF-4** needs CF-1's stats and CF-3's class table. **CF-5**, **CF-6** and **CF-7** follow CF-2. Serial for feel: one gameplay epic on the pursuit and at-bat files at a time.

```
CF-1 stats (Gameplay) ─────────────┐
CF-2 toy body (Art) ──┬─▶ CF-3 body classes + speed (Gameplay) ─▶ CF-4 zone (Gameplay)
                      ├─▶ CF-5 motion styles (Art)
                      ├─▶ CF-6 juice + select bars (Presentation)
                      └─▶ CF-7 toon + rim (Art)
```

### CF-1 · [#1114](https://github.com/jackguillet/grand-sluggers/issues/1114) · Four bars from sub-stats (Gameplay) · CH-07, CH-08, CH-09

- **Observable.** Every character file authors nine sub-stats. The four bars are derived, never authored. Changing a sub-stat moves only its own verb: Pitch power moves the fastball, Field throw speed moves a throw, and neither moves the other.
- **Files.** `data/characters/*.json` (all nine sub-stats, captains and role players), `Models.cs` (the bars become derived; the fallback to the bar retires once every file authors its slots), `ContentValidation.cs` (all slots required, the one-bar-at-9+ cap), `docs/roster.md` stat tables.
- **Tests.** SC-01 … SC-04.
- **Also owes.** The stats section of gameplay-spec (bars are derived). The authored values are balance: name them in the PR body and stop at the breakage suite.
- **Banned.** Bars stored in data. A new stat slot. Any move to S-29, cohorts or seals.

### CF-2 · [#1115](https://github.com/jackguillet/grand-sluggers/issues/1115) · Toy body (Art) · CH-01 … CH-04

- **Observable.** The shared body is about 4 heads tall. The ladder fits the CH-03 cap and floor. Konga's arms, Brondo's torso and Fenn's head read from their data. The rig gains `anatomy.knee` and `anatomy.chest`.
- **Files.** `tools/blender/hero_shared_blockout.py`, `data/art/rig.json`, `data/characters/*.json` `proportions`, `Silhouette.cs` (head, arms and torso become body scale, not portrait only), `SharedRig.cs`. Takes stay shared and must still pass `cli art` for every captain.
- **Tests.** SC-05 … SC-08. DCC stages: blocking → fill → export → still.
- **Human gate.** Dual stills of all seven, side by side, in palette and in flat black, at the turnaround camera and at gameplay distance. Jack passes it.
- **Banned.** A second rig. A unique mesh. Shrinking a body to save a camera (tune the shot). Extras on.

### CF-3 · [#1116](https://github.com/jackguillet/grand-sluggers/issues/1116) · Body classes, size in play, speed and weight (Gameplay) · CH-05, CH-10, CH-11

- **Observable.** A body-class table with room for about fifteen rows. Each captain names a class; role players default to their captain's class and may name their own. The class sets ground and fly catch reach, contact width, the speed-up and braking ramp, and knockback. The top-speed gap is about 1.25×.
- **Files.** New `data/rules/body-classes.json` (or a block in an existing rules file), `Content.cs`, `BodyResponse.cs` (per-class ramp through the one velocity primitive), `Fielding.cs`, `FlyCatch.cs`, `Runner.cs`, `AtBatFeel.cs` (contact width), `data/rules/fielding.json` and `running.json` (the speed curves).
- **Tests.** SC-09 … SC-14.
- **Also owes.** gameplay-spec §8.1, §8.2 and the running section. Keep the pursuit consistency rule (F693-02): one movement profile per body, across hit classes and positions.
- **Banned.** Reach read from the mesh. A per-captain `switch`. A second movement system.

### CF-4 · [#1117](https://github.com/jackguillet/grand-sluggers/issues/1117) · Knee-to-chest zone (Gameplay) · CH-06

- **Observable.** The zone for each batter runs from the knee to the chest landmark, at the fixed width. Aim, the CPU pitcher, the ring, the cursor and the sweet-spot oval follow the batter's zone.
- **Files.** `StrikeZoneGeometry.cs` (a batter-aware zone; the static fixed zone retires), `PitchFlight.cs` (the aim center), `SetTells.cs`, `SweetSpot.cs`, `AtBatResolver.cs`, the CPU pitcher and batter, the HUD zone draw (reads the sim zone, no own numbers).
- **Tests.** SC-15 … SC-19. S-108 and S-134 rerun against every captain.
- **Also owes.** gameplay-spec §4.4 rewritten; `docs/how-to-play.md` and `HowToPlay.cs` if the zone copy changes; the strike-zone lesson in `docs/tutorials.md`.
- **Banned.** A zone that moves with the animation. A per-seat or per-pad zone.

### CF-5 · [#1118](https://github.com/jackguillet/grand-sluggers/issues/1118) · Motion styles and signature beats (Art) · CH-12

- **Observable.** Seven styles, one per captain cut, for run, idle, batting stance and windup. One signature beat per captain (for example an idle fidget or a home-run trot). The run loop's stride rate follows ground speed on the sim clock.
- **Files.** `tools/blender/hero_shared_takes.py` (style as a pose-table dimension), `data/art/clips.json` (style id), `Motion.ClipFor(verb, hand, style)`, `HeroActor.cs`, the class row's style id from CF-3.
- **Tests.** SC-20 … SC-22. `cli art` validates every style has every styled verb, both hands.
- **Human gate.** A motion still set: the seven runs side by side, the seven stances, the seven windups. Jack passes it.
- **Banned.** Poses in C#. A second motion system. Runtime mirroring.

### CF-6 · [#1119](https://github.com/jackguillet/grand-sluggers/issues/1119) · Juice by weight and four bars on screen (Presentation) · CH-07, CH-13

- **Observable.** Captain select and the lineup show the four bars. Anticipation, hit-stop, squash and settle scale by weight class from a `data/feel/` table.
- **Files.** `data/feel/table.json` (per weight class), the squash wrapper, the select and lineup cards, `CarnivalFront`, `HowToPlay.cs` and `docs/how-to-play.md` where the bars are explained.
- **Tests.** SC-23, SC-24.
- **Human gate.** Be the select screen as a player: every captain, both pads.

### CF-7 · [#1120](https://github.com/jackguillet/grand-sluggers/issues/1120) · Toon and rim light (Art) · CH-14

- **Observable.** Characters use a real toon shader with a rim light, and still sort correctly against the grass (the reason `Look.Toon` fell back to Lit). Extras stay off.
- **Files.** `Look.cs`, the toon shader, `SharedRig.cs` material roles.
- **Tests.** SC-25.
- **Human gate.** HUD-off stills at the plate, the scoop and the fly (`docs/screenshot-gate.md`). Jack passes it.

## Scenarios

Headless unless the row says still. Each names the reason, not only the result.

| Id | State | Expected and why |
| --- | --- | --- |
| SC-01 | Every character file | Nine sub-stats authored, 1–10. Bars are derived. A file that authors a bar is refused. |
| SC-02 | Every character | At most one bar at 9 or higher (CH-08). The validator names the character. |
| SC-03 | Two pitchers, same everything, Pitch power 3 vs 9, same Field | The power 9 fastball is faster. Their fielding throws are the same. |
| SC-04 | Two fielders, same everything, throw speed 3 vs 9, same Pitch | The throw speed 9 throw arrives sooner on the one throw clock. Their fastballs are the same. |
| SC-05 | The rest rig | About 4 heads tall: body height ÷ head diameter in the proposed band. |
| SC-06 | The seven captains | Tallest ≤ 1.35× Rio, shortest ≥ 0.70× Rio, from the same function the sim and Unity read. |
| SC-07 | Konga and Rio | Konga's arm span exceeds Rio's by his `arms` ratio. Same test for Brondo's torso and Fenn's head. |
| SC-08 | Still | The seven in flat black at gameplay distance. Each is nameable. (Human.) |
| SC-09 | Every captain and role player | Resolves exactly one body class. |
| SC-10 | Tall class with a small fly reach, short class with a large one | Reach comes from the class row, not from height (Sluggers' Peach and DK). |
| SC-11 | Fastest and slowest body, bases and field | Top-speed ratio within the accepted band (proposed 1.20–1.30). |
| SC-12 | Light and heavy body from rest | The light body reaches top speed sooner. The heavy one takes longer to brake and reverse. |
| SC-13 | The same body on a grounder, a liner and a fly | One movement profile (F693-02 consistency). |
| SC-14 | A light and a heavy body hit by the same knockback | The light body recoils longer. |
| SC-15 | Every captain as batter | Zone bottom = knee landmark, top = chest landmark, width 0.92 ft half. |
| SC-16 | The same pitch aimed middle, Zig then Ashlord batting | It crosses the middle of each batter's own zone. |
| SC-17 | Batter mid-swing vs at rest | Same zone. (The animation does not move it.) |
| SC-18 | Every family, no aim, every captain batting | S-108 holds: inside the zone with a ball's radius to spare. |
| SC-19 | 1P and 1v1, both seats | Same zone for the same batter. |
| SC-20 | `cli art` | Every style has run, idle, stance and windup, both hands. |
| SC-21 | A body at half and at full top speed | The run loop's phase advances in proportion to ground distance. |
| SC-22 | Every captain | Its class style and signature beat resolve to real clips. |
| SC-23 | A heavy and a light body, same contact | The heavy body's hit-stop and settle are longer, from the feel table. |
| SC-24 | Select screen, every captain, both pads | Four bars, values equal to the derived bars. |
| SC-25 | Still | Toon plus rim at the plate, scoop and fly, HUD off. (Human.) |

## Open numbers (trials, not decisions)

| Number | Proposed start | Epic |
| --- | --- | --- |
| Head ratio | 4.0 heads, judged in the CF-2 still | CF-2 |
| Chest landmark | rig z 3.20 on the torso bone | CF-2, CF-4 |
| Zone height clamp | safety net only; set after CF-2 | CF-4 |
| Top-speed band | 1.20–1.30× | CF-3 |
| Ramp by weight | light about 0.12 s, heavy about 0.30 s to top speed (today 0.20 s for all) | CF-3 |
| Sub-stat values | each character's nine slots, starting from its current bar | CF-1 |
| Knockback by weight | Sluggers' 1.0 → 0.5 ladder as the start | CF-3 |

## Deferred

- **Ten captains (CH-09 note).** Three new captains need factions, class rows, palettes and signature beats. Start after CF-1 to CF-5 land and Jack sits the seven. They fill class rows and motion styles; they do not add a rig.
- **Role-player classes (CH-12 note).** The table has room. Role players keep their captain's class until a sitting asks for more.
- **Signature extras.** Off until they read as toys (#687).

## How this runs

One GitHub issue per epic, in the acceptance shape (parent, decisions, observable, files, tests, human gate, order, banned). One session per epic, in its own worktree, with a self-contained prompt. PRs against `main`. After each merge: `python3 tools/local-player.py`, and state the revision Jack plays. Sitting findings become children under #188 or #209.
