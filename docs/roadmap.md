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

## Milestone 2 — first playable

- Full 9-man lineups from the slice roster
- Player fielding (one fielder at a time)
- Buddy throw + buddy jump
- Pitcher stamina + mound swap
- One gimmick park (Crystal Rink **or** Funfair)
- Gear: 3 bats, 2 gloves
- Local 2-player (batter vs pitcher, or pitcher+fielder vs batter — pick one and ship it)

## Milestone 3 — content pass

- All 6 captains
- ~24 characters
- 4 parks
- Challenge *prototype* (hub + recruit one character). Kill it if it is not fun in two days.

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
