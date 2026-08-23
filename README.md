# Grand Sluggers

Arcade baseball with a cartoon roster, team chemistry, signature bats and gloves, and ballparks that cheat.

Inspired by *Mario Super Sluggers* (Wii, 2008) — **original characters and world**, not a Mario clone. The pitch is the same: a party sports game where *who you draft together* matters as much as who swings the bat.

**Engine of record: Unity 6 (URP).** See [docs/engine-decision.md](docs/engine-decision.md). The baseball rules live in `src/GrandSluggers.Sim`. The **playable vertical slice** is a 3D Harbor Diamond client in `src/GrandSluggers.Play` (Raylib) because the Unity editor is not required to play it. The `unity/` folder is a URP shell that imports the same sim.

## Why this game

Nintendo has not shipped a new Mario baseball game since 2008. The slot is empty: cartoon roster, gimmick parks, chemistry, star skills. *Backyard Baseball* is the closest cousin, and it just came back — but it is sandlot-sim, not arcade fireworks. Grand Sluggers is the party-arcade version of that feeling.

## Pillars

1. **Draft is gameplay.** Chemistry between groups (and rivalries) changes throws, items, star-meter start, and buddy plays. A stacked team of strangers loses to a weaker team that actually likes each other.
2. **Captains are fireworks.** Every captain has a unique Star Pitch and Star Swing. Role players get simpler star versions (fastball / changeup / breaker).
3. **Parks are characters.** One clean diamond. Everything else has a gimmick that changes a routine fly into a story.
4. **Gear has identity.** Signature bats and gloves are visual *and* mechanical — not just +1 Power.
5. **Easy to pick up, nasty to master.** Timing-based swing and pitch. Gamepad first. Motion optional later.

## Repo layout

```
docs/     design, research, engine decision, systems
data/     JSON content (roster, chemistry, parks, bats, gloves, abilities)
src/      Sim (rules) · Play (3D slice) · Cli · Tests
unity/    Unity 6 URP shell (open in the editor; not required to play)
```

![Team sheet at Harbor Diamond](docs/images/lineup.png)

![Rio Sparks goes deep](docs/images/harbor-diamond.png)

## Play the slice

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download). Gamepad or keyboard.

```bash
dotnet test
dotnet run --project src/GrandSluggers.Play
```

You are the **Spark All-Stars** (Rio) at Harbor Diamond, three innings against Ashlord's Ember Court. Fielding: pitch with a timing bar (SHIFT to charge, TAB to change pitch, Q for Heatball). Batting: swing when the ball gets there (SHIFT to charge, Q for Furnace). Chemistry is on the team sheet; good-chem throws are faster.

```
SPACE / A     pitch or swing
SHIFT / LT    charge
TAB           cycle pitch (fastball / changeup / curve)
Q / Y         arm star skill
A/D           spray the ball
ESC           quit
```

Headless autoplay (no window needed for the rules):

```bash
dotnet run --project src/GrandSluggers.Cli -- match --seed 7
```

## Docs

| Doc | What it is |
| --- | --- |
| [docs/vision.md](docs/vision.md) | What we are building and what we are not |
| [docs/research-sluggers.md](docs/research-sluggers.md) | How Mario Super Sluggers actually works |
| [docs/engine-decision.md](docs/engine-decision.md) | Unity vs Godot vs Unreal — why Unity |
| [docs/systems.md](docs/systems.md) | Chemistry, stars, batting, pitching, fielding, gear, parks |
| [docs/roster.md](docs/roster.md) | Factions, captains, placeholder roster |
| [docs/parks.md](docs/parks.md) | Ballparks and gimmicks |
| [docs/roadmap.md](docs/roadmap.md) | Vertical slice → first playable → content |

## Status

Milestone 1 vertical slice is playable. Harbor Diamond, Spark vs Ember, chemistry team sheet, timing-based pitch and swing, Heatball / Furnace, CPU fielders, 3 innings, MVP. Unity editor project is a shell; open `unity/` in Unity 6 LTS when you have it.
