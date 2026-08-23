# Roadmap

We do not build 40 characters and 9 parks in parallel. We prove an at-bat, then a game, then content.

## Milestone 0 — this repo (done)

- Research teardown of Mario Super Sluggers
- Engine decision (Unity 6 URP)
- Vision, systems, roster, parks
- Engine-agnostic sim: chemistry, starting stars, at-bat resolution, park dimensions
- JSON content for a slice roster
- GitHub repo

## Milestone 1 — vertical slice (Unity starts here)

Create the Unity 6 URP project under `unity/` (or a sibling, submodule — decide then).

Must feel good:

1. Harbor Diamond, empty crowd, readable diamond
2. Rio vs Ashlord, CPU fielders
3. Gamepad: pitch (type + timing + charge) and swing (timing + charge)
4. One Star Pitch (Heatball) and one Star Swing (Furnace)
5. Chemistry shown on a team screen; a good-chem throw is visibly faster
6. 3-inning CPU game that ends with an MVP line

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
