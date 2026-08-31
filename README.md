# Grand Sluggers

Arcade baseball with a cartoon roster, team chemistry, signature bats and gloves, and ballparks that cheat.

Inspired by *Mario Super Sluggers* (Wii, 2008) — **original characters and world**, not a Mario clone. The pitch is the same: a party sports game where *who you draft together* matters as much as who swings the bat.

**How you play: Unity Play.** Open `unity/` in Unity **6000.5.9f1** and press Play on `Assets/Scenes/HarborDiamond.unity`. Gamepad first. Couch map: [docs/how-to-play.md](docs/how-to-play.md). Editor setup: [unity/README.md](unity/README.md).

The baseball rules live in `src/GrandSluggers.Sim`. Engine decision: [docs/engine-decision.md](docs/engine-decision.md). Raylib (`src/GrandSluggers.Play`) is a `dotnet run` debug sandbox for the rules, not a second product.

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
data/     JSON content (roster, chemistry, parks, bats, gloves, abilities, feel, art slots)
src/      Sim (rules) · Play (Raylib debug sandbox) · Cli · Tests
unity/    Unity 6 URP client — this is how you play
```

![Team sheet at Harbor Diamond](docs/images/lineup.png)

![Rio Sparks goes deep](docs/images/harbor-diamond.png)

![Crystal Rink](docs/images/crystal-rink.png)

## Play

**Unity is the player-facing client.** New presentation (HUD, VFX, parks, cameras, bodies) lands only under `unity/`.

1. Install Unity Hub and editor **6000.5.9f1** (URP). Personal license is enough.
2. Open the `unity/` folder. If the scene is empty, menu **Grand Sluggers → Bootstrap Scene**.
3. Press **Play** on `Assets/Scenes/HarborDiamond.unity`. Gamepad is the couch product; keyboard is the same scheme.

Controls, Exhibition flow, Training drills, and F1/F2/F3 debug: **[docs/how-to-play.md](docs/how-to-play.md)**. Update that file in the same PR when verbs or cameras change.

Sim tests and a headless match (no window, no presentation):

```bash
PATH=/opt/homebrew/bin:$PATH dotnet test
PATH=/opt/homebrew/bin:$PATH dotnet run --project src/GrandSluggers.Cli -- match --home vale --away brondo --seed 7
PATH=/opt/homebrew/bin:$PATH ./tools/unity-compile.sh
```

### Debug sandbox (not the game)

`src/GrandSluggers.Play` is a Raylib window for poking the rules. Do not add HUD keys or VFX there.

```bash
PATH=/opt/homebrew/bin:$PATH dotnet run --project src/GrandSluggers.Play
```

## Docs

| Doc | What it is |
| --- | --- |
| [AGENTS.md](AGENTS.md) | Standing order for coding agents — rails, not patches |
| [docs/vision.md](docs/vision.md) | What we are building and what we are not |
| [docs/research-sluggers.md](docs/research-sluggers.md) | How Mario Super Sluggers actually works |
| [docs/how-to-play.md](docs/how-to-play.md) | Controls and how to play (living spec) |
| [docs/screenshot-gate.md](docs/screenshot-gate.md) | Plate / scoop / star stills; agent capture without -batchmode |
| [unity/README.md](unity/README.md) | Unity editor, scene, license |
| [docs/engine-decision.md](docs/engine-decision.md) | Unity vs Godot vs Unreal — why Unity |
| [docs/systems.md](docs/systems.md) | Chemistry, stars, batting, pitching, fielding, gear, parks |
| [docs/roster.md](docs/roster.md) | Factions, captains, placeholder roster |
| [docs/silhouette-bible.md](docs/silhouette-bible.md) | Locked camera, six body types, signature bats |
| [docs/parks.md](docs/parks.md) | Ballparks and gimmicks |
| [docs/roadmap.md](docs/roadmap.md) | Now → Nintendo-level Exhibition; how to use coding agents |

## Status

Unity Play is the only player-facing client. Exhibition picks any of the six captains, then drafts the eight around them. Feel and art **rails** are in; Harbor is still the slice to make expensive. Do not start extra parks or Challenge until Exhibition is the reason people stay — [roadmap](docs/roadmap.md). Editor: **6000.5.9f1**.
