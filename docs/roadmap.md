# Roadmap — from here to a Nintendo-level party baseball game

The sim is a product. Unity is still a prototype skin on Harbor. Super Sluggers sells **bodies, tells, and two-second illegal physics** — and underneath that, **baseball that is decided by where the ball and the runner are**. We do not get there by more JSON captains, and we do not get there by polishing a toy on top of plays that a dice roll decides.

This is the production plan **after** feel infrastructure (#107) and art rails (#118). Living specs: `data/feel/`, `data/art/`, `dotnet run --project src/GrandSluggers.Cli -- art`. Play: Unity `HarborDiamond.unity`. **Rules: [gameplay-spec.md](gameplay-spec.md).**

## Where we actually are (2026-09)

**Shipped (do not rebuild as new work).** Rules, hops, tags, lines, scoops, Exhibition front-of-house, toon fill, named cameras, directors, HarborKit (diamond + dress), feel tables, F2 overlay, art catalog, HUD-off specials as catalog VFX events, audio buses with authored bat / glove / crowd, scorebug that mutes during spectacle, the How to play book, one shared rig with baked takes for every captain. Six captains, 18 role players, six park **JSON**s. Challenge exists as a session loop and stays later.

**The gap (Jack's sitting, 2026-09).** It works and it is raw. Pitching is reversed from the mound camera (#533), strikeouts happen on pitches drawn outside the frame (#535), the batting cursor cannot reach the top or bottom of the zone, and the plays underneath are not baseball yet: a grounder is an out because a stat roll said so before anyone fielded it, runners move by a lookup table, double plays are fabricated from two synthetic throws, a missed pickoff hands the runner a base, and a double steal is impossible by construction. The code map is Appendix A of the spec; the headless `cli match` at seed 7 is an 8–0 doubles fest with items on every other hit.

**Definition of Nintendo-level for this game (unchanged).** Couch, gamepad, three innings at Harbor. You can name the captain with the HUD off. A perfect swing is illegal for two seconds and still baseball. A grounder is a scoop and a race. You want to play again.

**New in this revision.** Phase P below is the gameplay-parity sequence. It goes **ahead of** the toy playset (#246) in the stack, because AGENTS.md row 1 is "Harbor Exhibition is playable" and the play is what is broken. Phases A–E are kept below as the presentation plan they always were.

Tracker #39 is the older checklist. Many of its children shipped as first-pass. This doc is the sequence from **now**.

---

## How we use coding agents

Agents are the production line. You are the director. They are fast at systems, catalogs, wiring, tests, and filling named slots. They are weak at taste. Do not ask them "is this Nintendo enough?" — play Harbor and reject with a screenshot. For Phase P, do not ask them "does this feel like baseball?" — hand them a scenario id from the spec and reject when the scenario or the sitting fails.

### Operating rules

Standing order for every agent, every ticket: **[AGENTS.md](../AGENTS.md)** (stack, sitting, rails, look). Long-term product, not the current still. **No quick fixes.** If a hack would close the issue and a rail would serve 1P, 1v1, and the next play type, build the rail. Look: **[docs/look.md](look.md)**. Rules: **[docs/gameplay-spec.md](gameplay-spec.md)**.

1. **One GitHub child issue = one worktree = one agent.** Never share `/Users/jack/repos/grand-sluggers` except a final ff-only pull. Never `git add -A`.
2. **Acceptance is the prompt.** Every issue lists: observable, files, tests, banned. For a Phase P child the observable is a list of spec scenario ids (`S-xx`) and the test is the headless scenario harness. If an agent cannot falsify the work with `dotnet test`, `cli art` / `cli match`, `tools/unity-compile.sh`, or a named Hierarchy object, the issue is too vague — rewrite it before launching. Personal Unity cannot `-batchmode`; the compile script is the Unity csc gate.
3. **Serial for feel and for the play. Parallel for slots.** Camera, swing timing, fielding verbs, runner model, and out rules are one-after-another (they share the at-bat and the live ball). Filling `data/art` rows, VFX event prefabs, audio event files, captain extras, and `data/rules/` numbers that already have a scenario can fan out.
4. **Harbor Exhibition is the only slice.** Do not start Challenge (#36), extra parks as products (#37), role-player variants (#25), online, motion, 40-man, or full-screen blinds (#38).
5. **Catalog first, files second.** New clip / VFX / audio / skin = JSON slot + validator + empty folder, then the asset. New rule = `data/rules/` field + validator + scenario, then the code. Agents that skip the catalog will grow another C# switch.
6. **Skeptic pass on every feel or play merge.** A second agent (or you) plays the path the issue named: Exhibition → pitch camera → swing → grounder → throw. First-pass "looks like baseball in the debugger" is not done.
7. **Human gates.** Screenshot of plate (full batter, not a cap). Screenshot of a scoop. Screenshot of a star swing HUD-off. **A half-inning you can narrate: every out has a reason you saw.** If you would not show that still, or could not explain that out, the epic is open.
8. **How to play stays true.** `docs/how-to-play.md` is the couch map. Same PR as `Controls.cs`, SET cameras, Exhibition flow, **or a rule that changes what a verb does** (D1 retires the lead stick; the steal verb changes).
9. **The spec is the tie-breaker.** If code and `gameplay-spec.md` disagree, the code is wrong. If the spec is silent, the child issue adds the rule to the spec in the same PR.

### What to give an agent vs what you keep

| Agents own | You own |
| --- | --- |
| Directors, binders, validators, JSON, tests, HarborKit names | "Does this swing feel late?" |
| Rule tables, scenario tests, CPU decision tables, the sim's play system | "Was that out fair?" / "Would I have made that throw?" |
| Wiring a clip/wav/prefab into an existing slot | Silhouette identity, palette, tone |
| Filling all six captain **slots** once one skin works | Commissioned hero art / final music |
| Parallel VFX/audio events from the catalog | "Is Exhibition the reason people stay?" |
| PR stacking, CI, compile notes | Killing a mode that is not fun; the pitch-pace call (spec D7) |

### Suggested agent launch shape

Issue body already has acceptance. Prompt the agent with: **AGENTS.md (rails, not patches)**, parent epic, worktree slug, banned list, `cli art` must stay OK, Unity Play path, "no new skeleton, no new park, no MatchDirector god-file", and for Phase P: **the spec sections and scenario ids the child owns**. After merge: skeptic agent with the scenario list and the screenshot checklist.

Large systems: write a design (`docs/` or a GitHub epic), then execute children. Small children: one agent.

---

## Phase P — Plays like Sluggers (now)

Parent epic: **#209** (Harbor Exhibition plays like a baseball game). Each row below becomes one child epic with the spec sections it owns and the scenario ids that close it. Rows are **serial** unless marked; the order is chosen so every step lands on the previous one's rails.

**Exit for the phase:** Jack plays three innings at Harbor on a pad against the CPU and can say, for every out and every safe, what decided it — a bag, a tag, a catch, a beaten throw, a mash. `cli match` over 50 seeds reads like baseball (spec S-29). No play is resolved by a roll or a caption (spec A.4, A.5 empty).

| Epic | Owns (spec) | Exit (scenarios) | Notes |
| --- | --- | --- | --- |
| **P0. One place for the rules** | §0, §15, §16, A.7 | S-90, S-91, S-92 | `data/rules/*.json` with a validator and load fallbacks; typed `PlayEvent` outcomes (no caption-string control flow); a headless **scenario harness** that runs recorded seat commands through `LivePlaySystem` without Unity; dead feel fields removed. Extracts the baseball that lives in `InPlayDirector` (relay chain, close play, CPU catch timing, steal phase) into the sim — this is #512, done as the first step rather than the last. |
| **P1. Pitch and swing contract** | §3, §4, §5, A.1 | S-01 … S-30 | Cursor = quality, timing = direction (D4). Windows 9 / 7 with a floor. Charge is worth ×1.25. Break capped at half a zone; charge/changeup nearly straight. Human pitch types reachable; rubber walk moves the crossing the same for both seats. HBP reachable. Bunt on the bat plane, foul bunt K. Per-pitcher stamina with `star-skills.json` costs. CPU pitcher/batter **tables** with tracking and no forced-miss clamp. Closes #533, #534, #535 as children. **Can run in parallel with P2** (different files). |
| **P2. Flight and the field** | §6, §7.9, §7.10, §7.11, A.2 | S-20 … S-24, S-56 … S-59 | 3-D flight with directional wind, fence polygon, wall carom, ground-rule double, foul lines and foul-fly catches, one batted-ball class table, positions from the lineup diamond. |
| **P3. Runner model** | §9, §10.6, A.3 | S-36 … S-39, S-77 … S-82 | Runners are bodies with positions on the path, one speed formula, a batter-runner, per-runner send/hold, no passing, auto-return on flies, slide that matters. **D1: leads retired** (verb, pips, `Lead01`), how-to-play updated in the same PR. `Complete` scores by who crossed before the third out and places runners where they stand — `AdvanceHit` / `AdvanceTagUp` / `OccupiedDestBag` deleted. Extra innings, mercy. |
| **P4. Fielding decides by geometry** | §8, §7.1 … §7.8, A.4 | S-31 … S-35 | Kill the roll at `Fielding.cs:146`. CPU catch is a radius at the window, never force-fed. One throw model that flies the ball and judges the bag; lateral misses are the error; uncovered bags lob until the cover arrives. Reaction lockout, cover / cutoff / relay / backup by geometry. CPU fielder **decision table** by makeable margins. Thrower stays put; YOU hands to the receiver. ERROR stamp. |
| **P5. Outs, double plays, close plays, rundowns** | §10, §9.6, §9.7, A.5 | S-40 … S-55, S-73 … S-76 | Every row of §10.4 as a scenario for the human seat and the CPU seat. Fielder's choice. Force removed when the batter is retired. Doubled-off and tag-up throws. Close-play prompt **only inside the margin at 3B/home** (D5), verdict written once. Rundowns. Triple play reachable. `Retire` result honored. |
| **P6. Steals and pickoffs** | §11, §4.5, A.6 | S-60 … S-72 | Arm any runner, break at release, perfect steal (D2), pickoff catches only a runner who broke (D3), no random pickoffs, double steals including first-and-third, steal of home (D10), catcher throw as a normal throw with a tag. CPU steal and pickoff tables. |
| **P7. Balance and CPU** | §4.8, §5.9, §8.8, §9.9, §11.6, §12, §13 | S-29 over 50 seeds; difficulty ladder | Tune `data/rules/` until `cli match` distributions read like a 3-inning arcade game; difficulty as multipliers in `cpu.json`; star gains / MVP / star-skill values read from JSON. Remove the item auto-throw roll; items become field effects with geometry. |
| **P8. Presentation contract** | §15 | every stamp and camera in the table | Stamps ERROR / FIELDER'S CHOICE / PICKED OFF / SAFE; cameras per batted-ball class from `shots.json`; the booklet's Running and Fielding spreads rewritten for the new verbs; `HowToPlay.cs` and `docs/how-to-play.md` together. Runs alongside P3–P6 as each verb lands, not after. |

**Human gates inside Phase P.** After P1: the #534 parity sitting (pitch and hit on pad, keyboard, two pads). After P5: one half-inning narrated (every out explained). After P7: three innings, then the #346 book sitting. Agents do not pass these.

**Banned during Phase P.** New parks, new captains, unique meshes, Challenge, motion, online, a second input toolkit, growing `MatchDirector` or `InPlayDirector`, any `Random` outside the sim's seeded `_rng` that produces a `PlayEvent`, any new number in C# that belongs in `data/rules/`, any caption that decides a play.

---

## Phase A — The at-bat sells (shipped as presentation; rules move to P1)

**Exit:** A still of the pitch and a still of the swing would not embarrass Harbor on a trailer. A stranger learns the timing window in two at-bats.

| Epic | Why | Agent notes |
| --- | --- | --- |
| **A1. Plate and mound as film** | Shots are data; framing still drifts (cap close-up, square plate). Tune `data/feel/shots.json` and HarborKit empties, not new `Vector3`s in code. | One agent. F2 overlay on. Human screenshot. |
| **A2. Batter and pitcher read as bodies** | Shipped: one shared rig, baked takes, captains as proportions + extras. | Tune takes against the reference (#558). |
| **A3. Swing and pitch are verbs you can name HUD-off** | Contact, load, release hit `Contact` / `Release` marks. Charge and smash freeze live in `data/feel/table.json`. | The *rules* behind the verbs are P1. |
| **A4. Ball is a baseball** | Seams, spin, shadow, dirt hops — hold the bar on plate cam. | Small. Skeptic with the cap-blob failure in mind. |

---

## Phase B — In-play is a scene (shipped as presentation; rules move to P3–P5)

**Exit:** Grounder, line, fly, tag are different pictures. The glove and the slide are on the body.

| Epic | Why | Agent notes |
| --- | --- | --- |
| **B1. Grounder theater** | Scoop, hop chase, throw, beat the runner — the movie exists; the race underneath is P4/P5. | Training drill "grab a grounder" is the test harness. |
| **B2. Fly / line / tag cameras** | Named shots `diamond`, `throw`, smash override. Bag tell stays. | Data + CameraDirector. |
| **B3. Clip-proof** | Shipped: authored takes play from the catalog. | Fill slots in parallel. |

---

## Phase C — Harbor looks expensive (shipped)

| Epic | Why | Agent notes |
| --- | --- | --- |
| **C1. HUD-off specials for six captains** | Two seconds, then baseball. No full-screen blinds. | Parallel per event. Values from `star-skills.json` land in P7. |
| **C2. Harbor kit pass** | Placed objects; Hierarchy names are the API. | |
| **C3. Audio identity** | Original bat / glove / crowd shipped (#223). | Human picks the crack. |
| **C4. Broadcast HUD that can shut up** | Scorebug is the product. | Stamps for the new play results are P8. |

---

## Phase T — Nintendo-quality playset (#246, after Phase P)

Toy language (#247), front of house as a carnival (#248), lineup as the chemistry toy (#249), in-play cartoon juice (#250). Was the recommended next move in 2026-08; it moves behind Phase P because juice on a play that a roll decided is juice on a lie.

---

## Phase D — Content after the screenshot

Only if A–C stills exist and Phase P has exited. Rule: **three good parks beat six ugly ones.** Harbor + one expensive second park + one gimmick.

| Epic | Gate |
| --- | --- |
| **D1. Role-player variants** (#25) | Captains read at gameplay distance. Same rig, jersey/stripe only. |
| **D2. Crystal Rink as a kit** (#37 starts here, not six parks) | Copy HarborKit pattern. Hazards are `data/rules` field effects (spec §14). |
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

**Phase P0 → P1 (and P2 in parallel).** File the eight Phase P epics as children of #209 with the spec sections and scenario ids from the table above; fold #533, #534, #535, #512 in as children of P1 / P0. Then:

1. P0 — rules tables, typed outcomes, scenario harness, sim owns live play.
2. P1 — the at-bat contract (cursor / timing / charge / break / stamina / CPU tables). Human parity sitting.
3. P2 — flight with a fence.
4. P3 → P4 → P5 → P6, each closed by its scenario list and a skeptic sitting.
5. P7 balance, P8 presentation alongside.

Command to keep agents honest:

```bash
PATH=/opt/homebrew/bin:$PATH dotnet test
PATH=/opt/homebrew/bin:$PATH dotnet run --project src/GrandSluggers.Cli -- art
PATH=/opt/homebrew/bin:$PATH dotnet run --project src/GrandSluggers.Cli -- match --home vale --away brondo --seed 7
PATH=/opt/homebrew/bin:$PATH ./tools/unity-compile.sh
```

Unity: Play `Assets/Scenes/HarborDiamond.unity`. Editor **Grand Sluggers → Validate Art Rails**.

---

## Already shipped (archive)

Milestone 0–1 (repo + vertical slice), playability (#59, #80), front of house (#94), feel rails (#107), art rails (#118), the book (#342 children), one rig + takes (#264 children), authored hits (#223). First-pass parks/roster/specials/audio/HUD/training exist as systems. Iterate them in A–C; do not open duplicate issues.

## Non-goals until Exhibition is the reason people stay

Online, motion, Toy Field, live ops, licensed music, 40-man, full-screen blinds, Nintendo IP, a second player-facing client, unique skeletons per captain.
