# Roadmap — from here to a Nintendo-level party baseball game

The sim is a product. Unity is still a prototype skin on Harbor. Super Sluggers sells **bodies, tells, and two-second illegal physics** — and underneath that, **baseball that is decided by where the ball and the runner are**. We do not get there by more JSON captains, and we do not get there by polishing a toy on top of plays that a dice roll decides.

This is the production plan **after** feel infrastructure (#107) and art rails (#118). Living specs: `data/feel/`, `data/art/`, `dotnet run --project src/GrandSluggers.Cli -- art`. Play: the standalone window ([local-player.md](local-player.md)). **Rules: [gameplay-spec.md](gameplay-spec.md).** **Agents: [agent-rails.md](agent-rails.md)** (#647).

## Where we are

**[status.md](status.md) is the one status page**: what is shipped, what is open, and that no trial is open. This roadmap is the sequence; the stack in [AGENTS.md](../AGENTS.md) is the order of work.

**Definition of Nintendo-level for this game (unchanged).** Couch, gamepad, three innings at Harbor. You can name the captain with the HUD off. A perfect swing is illegal for two seconds and still baseball. A grounder is a scoop and a race. You want to play again.

Tracker #39 is the older checklist. Many of its children shipped as first-pass. This doc is the sequence from **now**.

---

## Scale and pace foundation (#693)

The sitting found contact and throws too fast. This is a coupled game contract, not a local park shrink. [Research](archive/game-feel/research-game-feel-693.md), [decision register and staged work](decisions/plan-game-feel-693.md), and [gameplay-spec D19](spec/00-decisions.md#02-field-proportions-and-race-calibration--d19) govern this work. Compare Wii and GameCube before choosing; collect proportions and full race timings, then accept targets, calibrate gameplay, present the approved race, and re-sit in the standalone. Many trial anchors and their implementations have since landed; the decision plan distinguishes accepted, measured and deferred work. The compact profile is promoted and shipped; human acceptance remains open ([status.md](status.md)). Harbor Exhibition retains priority; this does not start more parks or deferred modes.

## How we use coding agents

Agents are the production line. You are the director. They are fast at systems, catalogs, wiring, tests, and filling named slots. They are weak at taste. Do not ask them "is this Nintendo enough?" — play Harbor and reject with a screenshot. For Phase P, do not ask them "does this feel like baseball?" — hand them a scenario id from the spec and reject when the scenario or the sitting fails.

### Operating rules

Standing order for every agent, every ticket: **[AGENTS.md](../AGENTS.md)** (stack, sitting, rails, look). Long-term product, not the current still. **No quick fixes.** If a hack would close the issue and a rail would serve 1P, 1v1, and the next play type, build the rail. Look: **[docs/look.md](look.md)**. Rules: **[docs/gameplay-spec.md](gameplay-spec.md)**. How agents work: **[docs/agent-rails.md](agent-rails.md)** (#647). Session kind is gameplay / presentation / art — do not mix.

1. **One GitHub child issue = one worktree = one agent.** Never share `/Users/jack/repos/grand-sluggers` except a final ff-only pull. Never `git add -A`.
2. **Acceptance is the prompt.** Every issue lists: observable, files, tests, banned. For a Phase P child the observable is a list of spec scenario ids (`S-xx`) and the test is the headless scenario harness. If an agent cannot falsify the work with `dotnet test`, `cli art` / `cli match`, `tools/unity-compile.sh`, or a named Hierarchy object, the issue is too vague — rewrite it before launching. Personal Unity cannot `-batchmode`; the compile script is the Unity csc gate.
3. **Serial for feel and for the play. Parallel for slots.** Camera, swing timing, fielding verbs, runner model, and out rules are one-after-another (they share the at-bat and the live ball). Filling `data/art` rows, VFX event prefabs, audio event files, captain extras, and `data/rules/` numbers that already have a scenario can fan out.
4. **Harbor Exhibition is the only slice.** What not to start is the one list in [AGENTS.md](../AGENTS.md) "Do not start". Park **rails and greyboxes** are allowed; park art waits (AGENTS.md "Fields").
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

### Agentic rails (#647)

Living spec: **[agent-rails.md](agent-rails.md)**. Does not replace #209 / #188 / #346. R5 is later and does not block Exhibition.

| Child | Rail | Status |
| --- | --- | --- |
| R1 #648 | Session split (gameplay / presentation / art) | #655 |
| R2 #649 | Debug protocol in `data/agent/` | #656 |
| R3 #650 | Play traces (tick JSON) | #657 |
| R4 #651 | Dual stills + critic that files | #659 |
| R5 #652 | Unity CLI observation only | later |
| R6 #653 | Stage-save DCC | #662 |
| R7 #654 | Distill sittings into protocol / tests | #660 |

#647 is **code-complete except later R5**. Do not build more agent infrastructure.

---

## Shipped phases (P, A, B, C)

Their exit tables are history: [archive/phase-p/shipped-phases.md](archive/phase-p/shipped-phases.md). What they shipped is in [status.md](status.md).

---

## Phase T — Nintendo-quality playset (#246, after Phase P)

Toy language (#247), front of house as a carnival (#248), lineup as the chemistry toy (#249), in-play cartoon juice (#250). Was the recommended next move in 2026-08; it moves behind Phase P because juice on a play that a roll decided is juice on a lie.

---

## Phase D — Content after the screenshot

Only if A–C stills exist and Phase P has exited. Rule: **three good parks beat six ugly ones.** Harbor + one expensive second park + one gimmick. The fields rails come first ([plan-fields.md](decisions/plan-fields.md), epics F1–F8): D2 and D3 are the *art* of a park whose rules and greybox already passed a sitting (epic F9).

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

**Finish the learnable ordinary game, with tutorial coverage, before more artwork.**

1. Sit #346 on the promoted 80-ft game with one pad, then two; call D7 in that sitting; do not retune pitch pace from an older one. The special/status exclusions stand (#1010, #1011).
2. File what the sitting finds under #209 / #342 / #188 and work it in dedicated worktrees.
3. Keep [tutorials.md](tutorials.md) coverage growing with each mechanic (`cli tutorials` reports the live counts). Keep gameplay and presentation in separate children. Every new mechanic carries tutorial coverage in its own PR.
4. Narrated half, #346 book-to-play gate, and the tutorial learning/transfer check in the Mac standalone. Record build and profile; agents do not pass these.
5. Skeptic pass on the named Exhibition path and lesson retries. Phase T (#246) follows the gameplay/learning gates. R5, extra parks and deferred modes stay later.

Command to keep agents honest (never the full test suite locally; CI runs the breakage suite, and Actions → Full tests runs the rest on demand):

```bash
PATH=/opt/homebrew/bin:$PATH tools/test-fast.sh <Classes you touched>
PATH=/opt/homebrew/bin:$PATH dotnet run --project src/GrandSluggers.Cli -- art
PATH=/opt/homebrew/bin:$PATH dotnet run --project src/GrandSluggers.Cli -- protocol
PATH=/opt/homebrew/bin:$PATH dotnet run --project src/GrandSluggers.Cli -- match --home vale --away brondo --seed 7 --trace
PATH=/opt/homebrew/bin:$PATH ./tools/unity-compile.sh
```

Unity: Play `Assets/Scenes/HarborDiamond.unity`. Editor **Grand Sluggers → Validate Art Rails**.

---

## Already shipped (archive)

Milestone 0–1 (repo + vertical slice), playability (#59, #80), front of house (#94), feel rails (#107), art rails (#118), the book (#342 children), one rig + takes (#264 children), authored hits (#223). First-pass parks/roster/specials/audio/HUD/training exist as systems. Iterate them in A–C; do not open duplicate issues.

## Non-goals until Exhibition is the reason people stay

The one list is [AGENTS.md](../AGENTS.md) "Do not start".
