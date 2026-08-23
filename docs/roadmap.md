# Roadmap

We do not build 40 characters and 9 parks in parallel. We prove an at-bat, then a game, then content.

## Milestone 0 — this repo (done)

- Research teardown of Mario Super Sluggers
- Engine decision (Unity 6 URP)
- Vision, systems, roster, parks
- Engine-agnostic sim: chemistry, starting stars, at-bat resolution, park dimensions
- JSON content for a slice roster
- GitHub repo

## Milestone 1 — vertical slice (done)

Playable Harbor Diamond, Spark vs Ember.

1. Harbor Diamond, empty crowd, readable diamond
2. Rio vs Ashlord, CPU fielders
3. Gamepad + keyboard: pitch (type + timing + charge) and swing (timing + charge)
4. Star Pitch **Heatball** and Star Swing **Furnace**
5. Chemistry on the team sheet; good-chem throws are faster in the sim
6. 3-inning game that ends with an MVP line

Client: `src/GrandSluggers.Play` (Raylib 3D) on top of `GrandSluggers.Sim`. Unity 6 URP shell lives in `unity/` for when the editor is installed — same match loop.

Exit: a stranger can play an inning without a tutorial card.

## Milestone 2 — first playable (in progress)

Shipped in this pass:

- 9-man lineups (already in M1)
- Player fielding (WASD the highlighted glove, Space to catch, 1/2/3/H to throw)
- Buddy jump (F when two good-chem outfielders can rob a fly) and buddy throws (faster / "lasers it")
- Pitcher stamina HUD + R to swap the mound
- Crystal Rink (ice, freeze statues) alongside Harbor Diamond
- Local 2-player pitcher vs batter (T on the title: P1 Spark, P2 Ember)

Gear loadout is on the team sheet (B/G Spark, N/M Ember). Four parks ship: Harbor, Crystal Rink, Funfair (warp cans), Rooftop City (star billboards).

## Milestone 3 — content pass (started)

- All 6 captains selectable in Exhibition (A/D you, W/S opponent)
- 18 role players + 6 captains
- 6 parks playable (Harbor, Crystal, Funfair, Rooftop, Canopy Yard, Ember Keep)
- Challenge prototype: pick a captain, beat a rival, recruit one role player. Session-only. Kill it if it is not fun.
- Gameplay verbs in this pass: unique star pitch/swing, field abilities, chemistry items, steals, slider.

## Milestone 4 — party complete

- 40-ish roster, 8–9 parks, day/night
- Minigames only if Exhibition is already the reason people stay
- Steam page. Consoles are a license + cert project of their own.

## Non-goals until M2 ships

- Online
- Motion controls
- Live ops / cosmetics shop
- Licensed music
- A custom engine
