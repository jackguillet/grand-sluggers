# Grand Sluggers

Arcade baseball with a cartoon roster, team chemistry, signature bats and gloves, and ballparks that cheat.

Inspired by *Mario Super Sluggers* (Wii, 2008) — **original characters and world**, not a Mario clone. The pitch is the same: a party sports game where *who you draft together* matters as much as who swings the bat.

**How you play: Unity Play.** Open `unity/` in Unity **6000.5.9f1** and press Play on `Assets/Scenes/HarborDiamond.unity`. That is the game — Harbor Diamond, posed heroes, a camera behind the pitcher or batter, captain specials on the ball and the field. See [unity/README.md](unity/README.md).

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
3. Press **Play** on `Assets/Scenes/HarborDiamond.unity`. Gamepad first; keyboard is debug.

```
South / SPACE     pitch, swing, or catch
LT / Shift        charge
Left stick        pitch location / spray / field / lead
RB / Tab          cycle pitch
North / Q         star
West hold / V     bunt (batter) · jump (fielder)
East / G          dive
LB / X            steal
D-pad / 1 2 3 4   throw
Start / H         exhibition / challenge / training
West / F          training (title)
```

Full pad map: [unity/README.md](unity/README.md).

Sim tests and a headless match (no window, no presentation):

```bash
PATH=/opt/homebrew/bin:$PATH dotnet test
PATH=/opt/homebrew/bin:$PATH dotnet run --project src/GrandSluggers.Cli -- match --home vale --away brondo --seed 7
```

### Debug sandbox (not the game)

`src/GrandSluggers.Play` is a Raylib window for poking the rules. Do not add HUD keys or VFX there.

```bash
PATH=/opt/homebrew/bin:$PATH dotnet run --project src/GrandSluggers.Play
```

## Docs

| Doc | What it is |
| --- | --- |
| [docs/vision.md](docs/vision.md) | What we are building and what we are not |
| [docs/research-sluggers.md](docs/research-sluggers.md) | How Mario Super Sluggers actually works |
| [unity/README.md](unity/README.md) | How you play (Unity editor, pad map) |
| [docs/engine-decision.md](docs/engine-decision.md) | Unity vs Godot vs Unreal — why Unity |
| [docs/systems.md](docs/systems.md) | Chemistry, stars, batting, pitching, fielding, gear, parks |
| [docs/roster.md](docs/roster.md) | Factions, captains, placeholder roster |
| [docs/silhouette-bible.md](docs/silhouette-bible.md) | Locked camera, six body types, signature bats |
| [docs/parks.md](docs/parks.md) | Ballparks and gimmicks |
| [docs/roadmap.md](docs/roadmap.md) | Vertical slice → first playable → content |

## Status

Unity Play is the only player-facing client. Exhibition picks any of the six captains, then drafts the eight around them (gloves, batting order, chemistry graph, live stars). Challenge is a session recruit loop. Six parks, gear, fielding, local 2P exist in the sim; Harbor Diamond is the presentation park. Editor: **6000.5.9f1**.
