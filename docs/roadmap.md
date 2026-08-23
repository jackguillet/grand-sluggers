# Roadmap — from here to a Nintendo-level party baseball game

The sim is a product. Unity is still a prototype skin on Harbor. Super Sluggers sells **bodies, tells, and two-second illegal physics**. We do not get there by more JSON captains.

This is the production plan **after** feel infrastructure (#107) and art rails (#118). Living specs: `data/feel/`, `data/art/`, `dotnet run --project src/GrandSluggers.Cli -- art`. Play: Unity `HarborDiamond.unity`.

## Where we actually are (2026-08)

**Shipped (do not rebuild as new work).** Rules, hops, tags, lines, scoops, Exhibition front-of-house, toon fill, named cameras, directors, HarborKit, feel tables, F2 overlay, art catalog and empty Unity slots. Six captains, 18 role players, six park **JSON**s. Challenge exists as a session loop and stays later.

**The gap.** A stranger still sees capsules, generated beeps, and a park that is mostly cubes. Plate camera and chalk were code bugs because presentation was a program. Rails exist so the next work is **quality on one at-bat**, not a 40-man art push.

**Definition of Nintendo-level for this game.** Couch, gamepad, three innings at Harbor. You can name the captain with the HUD off. A perfect swing is illegal for two seconds and still baseball. A grounder is a scoop and a race. You want to play again. That is the exit for Phase A–C. Content (Phase D) is allowed only after that screenshot exists.

Tracker #39 is the older checklist. Many of its children shipped as first-pass. This doc is the sequence from **now**.

---

## How we use coding agents

Agents are the production line. You are the director. They are fast at systems, catalogs, wiring, tests, and filling named slots. They are weak at taste. Do not ask them “is this Nintendo enough?” — play Harbor and reject with a screenshot.

### Operating rules

1. **One GitHub child issue = one worktree = one agent.** Never share `/Users/jack/repos/grand-sluggers` except a final ff-only pull. Never `git add -A`.
2. **Acceptance is the prompt.** Every issue lists: observable, files, tests, banned. If an agent cannot falsify the work with `dotnet test`, `cli art` / `cli match`, or a named Hierarchy object, the issue is too vague — rewrite it before launching.
3. **Serial for feel. Parallel for slots.** Camera, swing timing, fielding verbs, and Harbor framing are one-after-another (they share the at-bat). Filling `data/art` rows, VFX event prefabs, audio event files, and captain extras can fan out **after** the shared rig and one swing clip exist.
4. **Harbor Exhibition is the only slice.** Do not start Challenge (#36), extra parks as products (#37), role-player variants (#25), online, motion, 40-man, or full-screen blinds (#38).
5. **Catalog first, files second.** New clip / VFX / audio / skin = JSON slot + validator + empty folder, then the asset. Agents that skip the catalog will grow another C# switch.
6. **Skeptic pass on every feel merge.** A second agent (or you) plays the path the issue named: Exhibition → pitch camera → swing → grounder. First-pass “looks like baseball in the debugger” is not done.
7. **Human gates.** Screenshot of plate (full batter, not a cap). Screenshot of a scoop. Screenshot of a star swing HUD-off. If you would not show that still to a friend, the epic is open.

### What to give an agent vs what you keep

| Agents own | You own |
| --- | --- |
| Directors, binders, validators, JSON, tests, HarborKit names | “Does this swing feel late?” |
| Wiring a clip/wav/prefab into an existing slot | Silhouette identity, palette, tone |
| Filling all six captain **slots** once one skin works | Commissioned hero art / final music |
| Parallel VFX/audio events from the catalog | “Is Exhibition the reason people stay?” |
| PR stacking, CI, compile notes | Killing a mode that is not fun |

### Parallel pattern (only after Phase B clip-proof)

```
human: lock style on rio + swing clip
        │
        ├─ agent: vale/zig/brondo/konga/ashlord skins (same rig)
        ├─ agent: audio events bat-perfect / glove / crowd-bed
        └─ agent: vfx events puff / heatball / buddy-flash
```

Do **not** parallel six parks or a Challenge island.

### Suggested agent launch shape

Issue body already has acceptance. Prompt the agent with: parent epic, worktree slug, banned list, `cli art` must stay OK, Unity Play path, “no new skeleton, no new park, no MatchDirector god-file.” After merge: skeptic agent with the screenshot checklist.

Large systems: write a design (`docs/` or a GitHub epic), then execute children. Small children: one agent.

---

## Phase A — The at-bat sells (next)

**Exit:** A still of the pitch and a still of the swing would not embarrass Harbor on a trailer. A stranger learns the timing window in two at-bats.

| Epic | Why | Agent notes |
| --- | --- | --- |
| **A1. Plate and mound as film** | Shots are data; framing still drifts (cap close-up, square plate). Tune `data/feel/shots.json` and HarborKit empties, not new `Vector3`s in code. | One agent. F2 overlay on. Human screenshot. |
| **A2. Batter and pitcher read as bodies** | Capsules do not sell. Put a **single** shared mesh (even a blockout) in `Assets/Art/Characters/SharedRig` and drive it from `MoveBones` / clip slots. Captains = scale + extras from `skins.json`. | One agent for the rig bind. Do not model six heroes. |
| **A3. Swing and pitch are verbs you can name HUD-off** | Contact, load, release must hit `Contact` / `Release` marks. Charge and smash freeze live in `data/feel/table.json`. | Tune table + bones. Optional: first real `swing` clip in the slot as a pipeline proof. |
| **A4. Ball is a baseball** | Seams, spin, shadow, dirt hops — already started; hold the bar on plate cam. | Small. Skeptic with the cap-blob failure in mind. |

**Do not** start A2 as six character sculpts. One chain. Six skins later.

---

## Phase B — In-play is a scene

**Exit:** Grounder, line, fly, tag are different pictures. The glove and the slide are on the body.

| Epic | Why | Agent notes |
| --- | --- | --- |
| **B1. Grounder theater** | Scoop, hop chase, throw, beat the runner — sim exists; presentation must be the movie. | Serial. Training drill “grab a grounder” is the test harness. |
| **B2. Fly / line / tag cameras** | Named shots `diamond`, `throw`, smash override. Bag tell stays. | Data + CameraDirector. Star swing may override diamond. |
| **B3. Clip-proof** | Drop one authored `run` or `scoop` into `Assets/Art/Animation/Clips/` and have HeroActor play it when present, else `MoveBones`. That proves the rail. | One agent. After this, clip fills can parallel. |

---

## Phase C — Harbor looks expensive

**Exit:** Three stills (title, at-bat, in-play) look like a place, not a debug draw. Specials are named with HUD muted.

| Epic | Why | Agent notes |
| --- | --- | --- |
| **C1. HUD-off specials for six captains** | Heatball, Charmball, Prism, Phony, Cask, Skull / Furnace — code VFX exist. Catalog events exist. Fill or direct the **event**, two seconds, then baseball. No full-screen blinds. | Parallel per event **after** one heatball looks right. |
| **C2. Harbor kit pass** | Dirt, pentagon plate, boxes, chalk, mound, wall, crowd, scoreboard are placed objects. Dress them; do not emit a new diamond in `ParkView.Build`. | Hierarchy names are the API. |
| **C3. Audio identity** | `data/art/audio.json` buses. Replace generated tones with original hits (bat, glove, crowd bed). No Nintendo samples, no licensed music. | Parallel wav drops into slots. Human picks the bat crack. |
| **C4. Broadcast HUD that can shut up** | Scorebug is the product. Mute-HUD Exhibition still plays. F2 stays debug. | Do not invent a second UI toolkit. |

Phase C is where agents shine: many slots, one Harbor, no new modes.

---

## Phase D — Content after the screenshot

Only if A–C stills exist. Rule: **three good parks beat six ugly ones.** Harbor + one expensive second park + one gimmick.

| Epic | Gate |
| --- | --- |
| **D1. Role-player variants** (#25) | Captains read at gameplay distance. Same rig, jersey/stripe only. |
| **D2. Crystal Rink as a kit** (#37 starts here, not six parks) | Copy HarborKit pattern. JSON already exists; presentation does not. |
| **D3. One gimmick park** (Funfair **or** Ember, not both) | Hazard must change a routine fly into a story. |
| **D4. Night as rules + look** | Already in sim; kit + lighting slot. |
| **D5. Challenge island** (#36) | Kill it if it is not more fun than Exhibition. |

40-man roster stays in #38 until Exhibition is why people stay.

---

## Phase E — Party complete / ship

- Local 2–4 players that a couch understands (already sketched).
- Steam page. Trailer is Harbor at-bats, not a feature list.
- Consoles = license + cert + Unity Pro; not a coding-agent epic.
- Minigames / Toy Field / online / motion: #38.

---

## Recommended next move

**File and run epic A: “The at-bat sells.”** Children A1–A4 above. One agent at a time. You play Exhibition on Harbor after each merge with F2 on, then off. If the plate still looks like a cap blob or the batter is a capsule, A is not done — do not start B3 clip-proof as six animations, and do not start D.

Command to keep agents honest:

```bash
PATH=/opt/homebrew/bin:$PATH dotnet test
PATH=/opt/homebrew/bin:$PATH dotnet run --project src/GrandSluggers.Cli -- art
PATH=/opt/homebrew/bin:$PATH dotnet run --project src/GrandSluggers.Cli -- match --home vale --away brondo --seed 7
```

Unity: Play `Assets/Scenes/HarborDiamond.unity`. Editor **Grand Sluggers → Validate Art Rails**.

---

## Already shipped (archive)

Milestone 0–1 (repo + vertical slice), playability (#59, #80), front of house (#94), feel rails (#107), art rails (#118). First-pass parks/roster/specials/audio/HUD/training exist as systems. Iterate them in A–C; do not open duplicate issues.

## Non-goals until Exhibition is the reason people stay

Online, motion, Toy Field, live ops, licensed music, 40-man, full-screen blinds, Nintendo IP, a second player-facing client, unique skeletons per captain.
