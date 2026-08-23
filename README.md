# Grand Sluggers

Arcade baseball with a cartoon roster, team chemistry, signature bats and gloves, and ballparks that cheat.

Inspired by *Mario Super Sluggers* (Wii, 2008) — **original characters and world**, not a Mario clone. The pitch is the same: a party sports game where *who you draft together* matters as much as who swings the bat.

**Engine of record: Unity 6.5 (URP).** See [docs/engine-decision.md](docs/engine-decision.md). The baseball rules live in `src/GrandSluggers.Sim`. Open `unity/` in Unity **6000.5.9f1** and press Play (`Assets/Scenes/HarborDiamond.unity`). A Raylib client (`src/GrandSluggers.Play`) is still there for `dotnet run` without the editor.

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

![Crystal Rink](docs/images/crystal-rink.png)

## Play the slice

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download). Gamepad or keyboard.

```bash
dotnet test
dotnet run --project src/GrandSluggers.Play
```

Title screen: **A/D** pick your captain (all six), **W/S** pick the opponent, **C** cycles **Harbor Diamond**, **Crystal Rink**, **Funfair Park**, and **Rooftop City**. **H** starts **Challenge** (beat a rival, recruit one role player). **T** is local 2-player exhibition. On the team sheet, B/G pick home bat/glove; N/M pick away.

On defense you control one fielder: WASD to run, Space to catch, 1/2/3/H to throw, F for a buddy jump, R to swap pitchers. Stamina is on the HUD. Freeze statues on Crystal Rink slow you down.

```
SPACE / A     pitch, swing, or catch
SHIFT / LT    charge
WASD          move the fielder / spray the bat
TAB           cycle pitch
Q / Y         star skill (Heatball / Furnace)
F             buddy jump
1 2 3 H       throw to 1st / 2nd / 3rd / home
R             new pitcher
C             cycle park     T  two-player     H  challenge
A/D           your captain     W/S  opponent
ESC           quit
```

```bash
dotnet run --project src/GrandSluggers.Play -- --park crystal-rink
dotnet run --project src/GrandSluggers.Play -- --home vale --away zig
dotnet run --project src/GrandSluggers.Play -- --challenge --captain rio
dotnet run --project src/GrandSluggers.Play -- --two
```

Headless autoplay (no window needed for the rules):

```bash
dotnet run --project src/GrandSluggers.Cli -- match --home vale --away brondo --seed 7
dotnet run --project src/GrandSluggers.Cli -- challenge --captain rio --seed 3
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

Milestone 3 is in progress. Exhibition picks any of the six captains. Challenge is a session recruit loop. Four parks, gear, fielding, local 2P. Unity 6.5 client is playable (`unity/`, editor 6000.5.9f1).
