# Roadmap — from here to a Nintendo-level party baseball game

The sim is a product. Unity is still a prototype skin on Harbor. Super Sluggers sells **bodies, tells, and two-second illegal physics** — and underneath that, **baseball that is decided by where the ball and the runner are**. We do not get there by more JSON captains, and we do not get there by polishing a toy on top of plays that a dice roll decides.

This is the production plan **after** feel infrastructure (#107) and art rails (#118). Living specs: `data/feel/`, `data/art/`, `dotnet run --project src/GrandSluggers.Cli -- art`. Play: Unity `HarborDiamond.unity`. **Rules: [gameplay-spec.md](gameplay-spec.md).** **Agents: [agent-rails.md](agent-rails.md)** (#647).

## Where we actually are (reviewed 2026-09-19 at `05471a60`)

**Shipped (do not rebuild as new work).** Rules, hops, tags, lines, scoops, Exhibition front-of-house, toon fill, named cameras, directors, HarborKit (diamond + dress), feel tables, F2 overlay, art catalog, HUD-off specials as catalog VFX events, audio buses with authored bat / glove / crowd, scorebug that mutes during spectacle, the How to play book, one shared rig with baked takes for every captain. Six captains, 18 role players, six park **JSON**s. Challenge exists as a session loop and stays later. **Agent rails** (#647): session split, debug protocol, play traces, dual stills, stage-save DCC, distill — R5 Unity CLI stays later. **Shared baseball body** (#628 / #627): default takes, handed equipment; extras stripped to size + color (#687 / #698).

**Phase P shipped (2026-09-12, one day, nine sessions).** Every play is now decided by geometry from `data/rules/` tables: the cursor decides contact quality and timing decides direction; the flight has a fence, a wall, and foul lines; runners are bodies on the basepath with no lead-offs; the infield out is a race, not a roll; double plays, fielder's choices, close plays inside the margin, and rundowns follow spec §10; steals break at release and a pickoff catches only a runner who broke; the CPU pitcher, batter, fielder, runner, and steal decisions are tables with an EASY / NORMAL / HARD ladder; stamps and cameras come from typed outcomes; the book follows the verbs. `cli match` over fifty seeds sits in the spec band (S-29 in CI: 2.5–2.7 runs a side, singles over doubles, ~1.3 HR). What was true in the morning — the roll at `Fielding.cs:146`, the runner lookup table, the synthetic double play, the free base on a missed pickoff — is gone.

**The gameplay mapping has advanced.** The C80 trial now includes the overlay, geometry/drag, movement/read clocks, catch/dive/jump, recovery/recoil, handling errors, throw clocks, and throw commands/abilities (#716–#723). Their sim slices are merged; #769 adds the fielding chain's Unity input and body tells. Whole-race measurements and the accepted outfield calibration are in [research-game-feel-3d.md](research-game-feel-3d.md). These are substantial foundations, not an artwork pass. C80 remains a selectable trial; the shipped default has not been promoted.

**C80 promoted (2026-09-22, Jack's decision).** The compact profile's ordinary loop is the shipped game and `trials/c80` is retired (3e, #715); Jack's sitting on the promoted game and the Harbor kit's 80-ft art/presentation child are still owed.

**The gap now.** Harbor still needs the narrated-half and book gates (#209 / #346), played on the promoted 80-ft game with one pad and then two (the game is gamepad only), plus the D7 pitch-pace call in that sitting and the tutorial learning gate (#774). The sitting findings #684 and #688 are closed. The tracker closes #690 (live stamps), #692 (CPU fly return), and the original #693 research issue; its ongoing compact-profile work remains under #715/#708 and the [decision plan](plan-game-feel-693.md). Geometry/scaling questions #730/#732 are closed. Closed implementation issues do not pass the human gates. Special-attack/status work excluded from the ordinary C80 validation remains separate debt.

**Tutorials are part of the bones.** Jack's September 19 direction is to keep building gameplay foundations before generating artwork and to provide a playable tutorial for every mechanic. [tutorials.md](tutorials.md), tracked in [#770](https://github.com/jackguillet/grand-sluggers/issues/770), defines the coverage contract, controlled CPU setups, and implementation slices. The tutorial catalog now covers plate skills, fielding and relays, running and steals, guided team screens, chemistry, named stars and items, with category navigation and three-success completion. The exact counts and remaining scenarios are reported by `cli tutorials`; standalone learning gates remain separate. Tutorial coverage grows with each feature rather than becoming a documentation sweep at the end.

**Definition of Nintendo-level for this game (unchanged).** Couch, gamepad, three innings at Harbor. You can name the captain with the HUD off. A perfect swing is illegal for two seconds and still baseball. A grounder is a scoop and a race. You want to play again.

**Sequence from here.** Continue the C80/Exhibition gameplay gates and sitting-found fixes; expand the existing tutorial runner and Tutorials section with each mechanic. Complete parity/D7, the narrated half, and the #346 learning gate. Artwork/Phase T stays behind this learnable gameplay foundation; tutorials do not start deferred modes.

Tracker #39 is the older checklist. Many of its children shipped as first-pass. This doc is the sequence from **now**.

---

## Scale and pace foundation (#693)

The sitting found contact and throws too fast. This is a coupled game contract, not a local park shrink. [Research](research-game-feel-693.md), [decision register and staged work](plan-game-feel-693.md), and [gameplay-spec D19](spec/00-decisions.md#02-field-proportions-and-race-calibration--d19) govern this work. Compare Wii and GameCube before choosing; collect proportions and full race timings, then accept targets, calibrate gameplay, present the approved race, and re-sit in the standalone. Many trial anchors and their implementations have since landed; the decision plan distinguishes accepted, measured and deferred work. Final promotion and human acceptance remain open. Harbor Exhibition retains priority; this does not start more parks or deferred modes.

## How we use coding agents

Agents are the production line. You are the director. They are fast at systems, catalogs, wiring, tests, and filling named slots. They are weak at taste. Do not ask them "is this Nintendo enough?" — play Harbor and reject with a screenshot. For Phase P, do not ask them "does this feel like baseball?" — hand them a scenario id from the spec and reject when the scenario or the sitting fails.

### Operating rules

Standing order for every agent, every ticket: **[AGENTS.md](../AGENTS.md)** (stack, sitting, rails, look). Long-term product, not the current still. **No quick fixes.** If a hack would close the issue and a rail would serve 1P, 1v1, and the next play type, build the rail. Look: **[docs/look.md](look.md)**. Rules: **[docs/gameplay-spec.md](gameplay-spec.md)**. How agents work: **[docs/agent-rails.md](agent-rails.md)** (#647). Session kind is gameplay / presentation / art — do not mix.

1. **One GitHub child issue = one worktree = one agent.** Never share `/Users/jack/repos/grand-sluggers` except a final ff-only pull. Never `git add -A`.
2. **Acceptance is the prompt.** Every issue lists: observable, files, tests, banned. For a Phase P child the observable is a list of spec scenario ids (`S-xx`) and the test is the headless scenario harness. If an agent cannot falsify the work with `dotnet test`, `cli art` / `cli match`, `tools/unity-compile.sh`, or a named Hierarchy object, the issue is too vague — rewrite it before launching. Personal Unity cannot `-batchmode`; the compile script is the Unity csc gate.
3. **Serial for feel and for the play. Parallel for slots.** Camera, swing timing, fielding verbs, runner model, and out rules are one-after-another (they share the at-bat and the live ball). Filling `data/art` rows, VFX event prefabs, audio event files, captain extras, and `data/rules/` numbers that already have a scenario can fan out.
4. **Harbor Exhibition is the only slice.** Do not start Challenge (#36), extra parks as products (#37), role-player variants (#25), online, motion, 40-man, or full-screen blinds (#38). Park **rails and greyboxes** are allowed work under [#814](https://github.com/jackguillet/grand-sluggers/issues/814) ([plan-fields.md](plan-fields.md), FD-01, September 21, 2026): Harbor parity first, one proving park, then one park at a time. Park art stays behind #37.
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

## Phase P — Plays like Sluggers (code shipped 2026-09-12; gates open)

Parent epic: **#209** (Harbor Exhibition plays like a baseball game). Each row is one child epic with the spec sections it owns and the scenario ids that closed it. Rows were **serial** except P1 ∥ P2 and P8 alongside P7.

| Epic | Shipped as |
| --- | --- |
| P0 #562 | #571 rules tables · #584 typed outcomes + scenario harness · #573 live ball into the sim (closes #512) |
| P1 #563 | #586 cursor / timing · #587 pitch shapes, one crossing, aim tell (#533 #577) · #589 CPU tables, per-pitcher stamina, star skills from JSON · #592 pad verbs + book (#582) · integrated onto main by #593 · sitting: changeup hang/dump (#668 / #672) · swing `leadSec` 0.18 (#670 / #673) |
| P2 #564 | #588 3-D flight, fence, classes · #590 foul geometry and foul fielding (#575) · #591 positions from the lineup · integrated by #594 |
| P3 #565 | #595 runners are bodies, no leads, Complete places them · #596 extra innings, mercy, walk-off |
| P4 #566 | #597 the roll is dead, one throw model, CPU fielder table · sitting: liner FlyOut (#666 / #674) · OF glove by the roll (#667 / #678) · stand-up ring, dive at the rim (#669 / #679) |
| P5 #567 | #598 tag reach, close-play margin, rundowns, the DP matrix |
| P6 #568 | #599 steals and pickoffs as one live runner play |
| P7 #569 | #603 S-29 in CI, difficulty ladder, stars / MVP / items from tables, park window as data |
| P8 #570 | #600 stamps from typed outcomes (#578) · #601 cameras per class, the catcher stays put (#574) · #602 the book follows the verbs · sitting: liner shot (#665 / #675) · no throw laser (#689 / #699) |

Lesson from the day: stacked PRs must be opened **against `main`** (or the top of the stack merged into `main` at the end). P1 and P2 merged their parts into their own parent branches and `main` only had part (a) of each until #593 / #594.

### Phase P exit — the three sittings (Jack)

The consolidated checklist is on #209. In order:

1. **Parity sitting (#534, closed 2026-09-24; the D7 call moved to #209)** — **played twice, not passed.** First: `95026535d3` (2026-09-13). Re-sit: `850dd95` with #628 body (2026-09-14). **D7 wait** (#677) until the next re-sit. Closes #563, #564 when Jack would keep the feel.
2. **Narrated half-inning** — every out has a reason you saw. Watch the outfield on every fly for #580 and the glove read (#609). Closes #565–#568 and the fielding notes. **Not run yet.**
3. **Three innings, then the book (#346)** — one pad, then two pads; Call time → How to play must be enough. Closes #209 and #342. **Not run yet.**

What sticks becomes a sitting-found child. Distill: file, append `data/agent/debug-protocol.json` in the fix PR, promote on the second firing.

#### First sitting children (2026-09-13) — all landed

| Child | Observed | Status |
| --- | --- | --- |
| #665 | Liner vs hopper HUD-off | ✅ #675 |
| #666 | Liner in the air is an out | ✅ #674 |
| #667 | OF glove by the roll, not the bounce | ✅ #678 |
| #668 | Changeup hang/dump | ✅ #672 |
| #669 | Ring too wide; rim dives | ✅ #679 |
| #670 | Swing `leadSec` 0.18 | ✅ #673 |

#### Re-sit children (2026-09-14, `850dd95`)

| Child | Observed | Status |
| --- | --- | --- |
| #683 | Bags brown, not white | ✅ #697 |
| #684 | Foul pops behind the batter | closed |
| #685 | Title captain arms-up X — remove | ✅ #694 |
| #686 | Select bobs through the dirt | ✅ #695 |
| #687 | Strip extras; size + color only | ✅ #698 |
| #688 | Runners cannot share a bag; per-runner send/hold | ✅ per-runner orders; two runners on one bag: the lead keeps it unless forced |
| #689 | Yellow throw-destination line | ✅ #699 |
| #690 | Stamp each event when it happens | closed; typed live stamps landed |
| #691 | Infielders scale up holding the ball | ✅ #700 |
| #692 | CPU doubled off on a clear fly out | closed; caught-fly state preserved |
| #693 | Research Sluggers field proportions; bat/throws too fast | closed research tracker; C80 work/gates continue in #715/#708 |

Also landed beside: plate bat (#560 / #671), turntable (#559 / #664), Esc/H (#629 / #663), shadow code (#646; visibility gate #645 still open), shared body (#628).

### The original Phase P plan (for the record)

**Exit for the phase:** Jack plays three innings at Harbor on a pad against the CPU and can say, for every out and every safe, what decided it — a bag, a tag, a catch, a beaten throw, a mash. `cli match` over 50 seeds reads like baseball (spec S-29). No play is resolved by a roll or a caption (spec A.4, A.5 empty).

| Epic | Owns (spec) | Exit (scenarios) | Notes |
| --- | --- | --- | --- |
| **P0. One place for the rules** (#562) | §0, §15, §16, A.7 | S-90, S-91, S-92 | `data/rules/*.json` with a validator and load fallbacks; typed `PlayEvent` outcomes (no caption-string control flow); a headless **scenario harness** that runs recorded seat commands through `LivePlaySystem` without Unity; dead feel fields removed. Extracts the baseball that lives in `InPlayDirector` (relay chain, close play, CPU catch timing, steal phase) into the sim — this is #512, done as the first step rather than the last. |
| **P1. Pitch and swing contract** (#563) | §3, §4, §5, A.1 | S-01 … S-30 | Cursor = quality, timing = direction (D4). Windows 9 / 7 with a floor. Charge is worth ×1.25. Break capped at half a zone; charge/changeup nearly straight. Human pitch types reachable; rubber walk moves the crossing the same for both seats. HBP reachable. Bunt on the bat plane, foul bunt K. Per-pitcher stamina with `star-skills.json` costs. CPU pitcher/batter **tables** with tracking and no forced-miss clamp. Closes #533, #534, #535 as children. **Can run in parallel with P2** (different files). |
| **P2. Flight and the field** (#564) | §6, §7.9, §7.10, §7.11, A.2 | S-20 … S-24, S-56 … S-59 | 3-D flight with directional wind, fence polygon, wall carom, ground-rule double, foul lines and foul-fly catches, one batted-ball class table, positions from the lineup diamond. |
| **P3. Runner model** (#565) | §9, §10.6, A.3 | S-36 … S-39, S-77 … S-82 | Runners are bodies with positions on the path, one speed formula, a batter-runner, per-runner send/hold, no passing, auto-return on flies, slide that matters. **D1: leads retired** (verb, pips, `Lead01`), how-to-play updated in the same PR. `Complete` scores by who crossed before the third out and places runners where they stand — `AdvanceHit` / `AdvanceTagUp` / `OccupiedDestBag` deleted. Extra innings, mercy. |
| **P4. Fielding decides by geometry** (#566) | §8, §7.1 … §7.8, A.4 | S-31 … S-35 | Kill the roll at `Fielding.cs:146`. CPU catch is a radius at the window, never force-fed. One throw model that flies the ball and judges the bag; lateral misses are the error; uncovered bags lob until the cover arrives. Reaction lockout, cover / cutoff / relay / backup by geometry. CPU fielder **decision table** by makeable margins. Thrower stays put; YOU hands to the receiver. ERROR stamp. |
| **P5. Outs, double plays, close plays, rundowns** (#567) | §10, §9.6, §9.7, A.5 | S-40 … S-55, S-73 … S-76 | Every row of §10.4 as a scenario for the human seat and the CPU seat. Fielder's choice. Force removed when the batter is retired. Doubled-off and tag-up throws. Close-play prompt **only inside the margin at 3B/home** (D5), verdict written once. Rundowns. Triple play reachable. `Retire` result honored. |
| **P6. Steals and pickoffs** (#568) | §11, §4.5, A.6 | S-60 … S-72 | Arm any runner, break at release, perfect steal (D2), pickoff catches only a runner who broke (D3), no random pickoffs, double steals including first-and-third, steal of home (D10), catcher throw as a normal throw with a tag. CPU steal and pickoff tables. |
| **P7. Balance and CPU** (#569) | §4.8, §5.9, §8.8, §9.9, §11.6, §12, §13 | S-29 over 50 seeds; difficulty ladder | Tune `data/rules/` until `cli match` distributions read like a 3-inning arcade game; difficulty as multipliers in `cpu.json`; star gains / MVP / star-skill values read from JSON. Remove the item auto-throw roll; items become field effects with geometry. |
| **P8. Presentation contract** (#570) | §15 | every stamp and camera in the table | Stamps ERROR / FIELDER'S CHOICE / PICKED OFF / SAFE; cameras per batted-ball class from `shots.json`; the booklet's Running and Fielding spreads rewritten for the new verbs; `HowToPlay.cs` and `docs/how-to-play.md` together. Runs alongside P3–P6 as each verb lands, not after. |

**Human gates inside Phase P.** After P1: #534 (played twice; remaining re-sit children then D7). After P5: narrated half. After P7: three innings, then #346. Agents do not pass these.

**Banned during Phase P.** New parks, new captains, unique meshes, Challenge, motion, online, a second input toolkit, growing `MatchDirector` or `InPlayDirector`, any `Random` outside the sim's seeded `_rng` that produces a `PlayEvent`, any new number in C# that belongs in `data/rules/`, any caption that decides a play.

---

## Phase A — The at-bat sells (shipped as presentation; rules move to P1)

**Exit:** A still of the pitch and a still of the swing would not embarrass Harbor on a trailer. A stranger learns the timing window in two at-bats.

| Epic | Why | Agent notes |
| --- | --- | --- |
| **A1. Plate and mound as film** | Shots are data; framing still drifts (cap close-up, square plate). Tune `data/feel/shots.json` and HarborKit empties, not new `Vector3`s in code. | One agent. F2 overlay on. Human screenshot. |
| **A2. Batter and pitcher read as bodies** | Shipped: one shared rig, baked takes. Captains are **size + color** (#687 / #698); extras off until they read as toys. | Tune takes against the reference (#558). |
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

Only if A–C stills exist and Phase P has exited. Rule: **three good parks beat six ugly ones.** Harbor + one expensive second park + one gimmick. The fields rails come first ([plan-fields.md](plan-fields.md), epics F1–F8): D2 and D3 are the *art* of a park whose rules and greybox already passed a sitting (epic F9).

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

Online, motion, Toy Field, live ops, licensed music, 40-man, full-screen blinds, Nintendo IP, a second player-facing client, unique skeletons per captain.
