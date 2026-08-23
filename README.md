# Grand Sluggers

Arcade baseball with a cartoon roster, team chemistry, signature bats and gloves, and ballparks that cheat.

Inspired by *Mario Super Sluggers* (Wii, 2008) — **original characters and world**, not a Mario clone. The pitch is the same: a party sports game where *who you draft together* matters as much as who swings the bat.

**Engine of record: Unity 6 (URP).** See [docs/engine-decision.md](docs/engine-decision.md). The baseball sim in `src/` is engine-agnostic C# so we can prove hitting, pitching, chemistry, and park hazards before a Unity scene exists.

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
src/      engine-agnostic C# sim + CLI + tests
unity/    notes for the Unity 6 project (created later; not checked in yet)
```

## Quick start (sim, no Unity)

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet test
dotnet run --project src/GrandSluggers.Cli -- team spark-allstars
dotnet run --project src/GrandSluggers.Cli -- at-bat ember --seed 7
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

Pre-production. Research is in, engine is chosen, sim is a first cut of chemistry + at-bats. No Unity project checked in yet — that happens when we start the vertical slice (one park, two captains, one at-bat that feels good).
