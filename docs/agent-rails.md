# Agentic rails — how agents inherit the playbook

This is the **source of truth for how coding agents build Grand Sluggers**. When a session and this document disagree, the session is wrong. When this document is silent, the child issue adds the rule here in the same PR.

The bar is not a one-shot playable demo. The bar is the stack in [AGENTS.md](../AGENTS.md): Harbor Exhibition a stranger can finish, sitting-found children, a toy that reads HUD-off, authored sound. Agents are the production line. They do not pass human gates.

Companion docs: [playbook.md](playbook.md) (how a phase runs), [roadmap.md](roadmap.md) (sequence), [gameplay-spec.md](gameplay-spec.md) (baseball), [art-rails.md](art-rails.md) (slots), [character-motion.md](character-motion.md) (takes), [screenshot-gate.md](screenshot-gate.md) (stills). Feel numbers stay in `data/feel/`. Rule numbers stay in `data/rules/`. Art slots stay in `data/art/`. Agent memory that must survive a session lives in `data/agent/`.

Status tags used throughout, first checked against `f419603` (2026-09-13). Older ✅ tags name the PR that closed them; new ones do not (§1.2). Appendix A keeps the gaps as the work.

| Tag | Meaning |
| --- | --- |
| ✅ | Shipped and behaves as written |
| ⚠️ | Exists in culture or a cousin system, but is not a rail an agent must load |
| ❌ | Missing. The next session will rediscover it or invent a patch |

Every ❌ and ⚠️ is collected in Appendix A. Every rail that matters has a child id (`R-n`) in Appendix B; the child list is the acceptance test for the epic.

Parent epic: **#647**. Children: **#648–#654**. Product epics these rails serve stay #209 (play), #188 (toy), #342 (book). This epic does not replace them and does not move Harbor Exhibition off the top of the stack.

---

## 0. Principles

1. **The human writes the brief.** [gameplay-spec.md](gameplay-spec.md), [look.md](look.md), and the screenshot-gate table outrank any agent. A session that needs a rule the spec lacks adds the numbered rule in the same PR. It does not invent taste.
2. **Mechanics before art. Sessions do not mix.** A gameplay session owns `data/rules/` and the sim. A presentation session owns cameras, HUD, and the book. An art session owns a catalog slot and a Blender script. Mixing them is how Ashlord's hat gets shrunk to save a camera.
3. **3D is Python. Catalog first, files second.** `harbor_kit.py`, `hero_shared_blockout.py`, `hero_shared_extras.py`, `hero_shared_takes.py` are the DCC. A `.blend` is a cache, not the source. New clip / VFX / audio / skin / extra = JSON slot + validator + empty folder, then the asset.
4. **Playtest → detect failure → repair → remember.** Generating code is cheap. Keeping the game playable across files, state, and the next play type is the work. A sitting that only becomes a Slack sentence is lost.
5. **Generation cannot pass itself.** A builder does not grade its own still. A critic files diffs. Jack passes look and play. Agents do not pass #346, #209 sittings, or #188.
6. **Sim owns baseball. Unity is eyes, not the engine.** `cli match` and the scenario harness are the agent playtest loop. Unity CLI / MCP, if wired, inspect hierarchy, console, and still capture. They do not decide outs.
7. **One worktree per child. Never `git add -A`.** Stacked PRs against `main`. CI runs the breakage suite on every PR; never run the full suite locally (§1.2). After merge: `python3 tools/local-player.py` when Jack needs the window.

### 0.1 Decisions (steal / reject)

Agents do not relitigate these. Sources (Paper Route / @builtbysketch via @zekeatchan, OpenGame, VibeGame, Unity official agent plugin, The Long Silence, ThePrimeagen JSON replay, Nex loft / Blender MCP stages).

| # | Question | Decision | Why |
| --- | --- | --- | --- |
| D1 | Who writes the brief? | **Jack / the spec.** Agents execute. | Paper Route: the model cannot decide what is worth keeping. We already have this as gameplay-spec §0. |
| D2 | Mix logic and visuals in one session? | **No.** Session kinds in §1. | Paper Route item 3. A mixed session patches the toy to hide a play bug, or the play to hide a look bug. |
| D3 | Where do meshes live? | **Python scripts that export FBX**, not authored `.blend` as source. | Paper Route item 4; already `tools/blender/*.py`. |
| D4 | Which pictures falsify art? | **DCC still and in-game still.** Both in the PR. Agents stop. | Paper Route item 6 ("cap doesn't cover the hair"). Named paths: `tools/dcc-still.sh` → `scratchpad/stills/dcc-*.png`, `tools/still-gate-character.sh` → `char-{id}-rest.png` / `char-{id}-pose.png`. |
| D5 | Can a critic pass look? | **No.** It files. Jack passes. | The Long Silence judge never finished, which is correct for #188. VibeGame's generation/review split is the part we take. |
| D6 | How does the next session start smarter? | **A living debug protocol in data**, not only GitHub issues. Recurring signatures promote to tests. | OpenGame Debug Skill (signature, root cause, verified fix). Sittings already file children; they do not yet become a loadable protocol. |
| D7 | How do agents playtest baseball? | **Headless geometry traces** from `cli match --trace` / the scenario harness, loopable without Unity. | ThePrimeagen JSON replay; VibeGame frame-sync. `PlayTrace` dumps ball / runner / glove / bag per tick; S-90 and S-01…S-92 stay the replay rail. |
| D8 | Unity official plugin / CLI / MCP? | **Observation only**, and only after traces exist. Deny-list in §5. | Unity plugin 2026-09-09. PhysX / NavMesh / IAP skills would put baseball in the wrong place. Personal Unity cannot `-batchmode`. |
| D9 | One-shot a captain or Harbor kit? | **No.** Named stages, save after each, a still at each stage. | Nex loft six-stage Blender MCP (2026-09-11). `data/agent/dcc-stages.json` / `cli stages`. |
| D10 | Prompt-to-game / Meshy heroes / a second engine? | **No.** | We are not generating a new game or a new skeleton. Unique packages are deferred. |
| D11 | Shrink or hide a mesh to save a camera? | **No.** Tune the shot. | AGENTS.md. Twitter "keep the engine small" is not permission to starve the toy. |
| D12 | Are extras on the field? | **No until they read as toys.** Identity is palette + `Silhouette.Proportions`. | #687 sitting: Brondo/Konga foot circles, Ashlord cape as an orange plate. `extras.json` slots stay; skins list none. Do not shrink Ashlord to hide the cape. Caps stay off (#557). |

---

## 1. Session split

A session declares its kind in the prompt and on the issue. File owners and the banned list come from this table, not from taste.

| Kind | Owns | Banned |
| --- | --- | --- |
| **Gameplay** | `data/rules/`, `trials/`, `src/GrandSluggers.Sim/`, scenario ids, `cli match` | `tools/blender/`, `data/art/extras.json` (except a clip marker the sim already reads), still PNGs, Unity presentation directors, cameras |
| **Presentation** | `data/feel/` cameras and timing, HUD, `HowToPlay.cs`, `docs/how-to-play.md`, stamps | Rule tables, `MatchDirector` switches, Blender, new captains |
| **Art** | one catalog slot in `data/art/`, the matching Blender script, still PNGs, `cli art` | Sim rules, C# poses, a second rig, a new hero, shrinking a mesh to save a shot |

✅ **R1 #648 / #655.** Standing order in [AGENTS.md](../AGENTS.md). A mixed-session change is a review fail.

End each session with a playable artifact of its kind before the next prompt: gameplay → `tools/test-fast.sh <Classes you touched>` + `cli match`; presentation → named shot / book page; art → still PNGs in `scratchpad/stills/`. Do not rebuild the Mac player as proof of look.

---

## 1.1 Tutorial coverage follows the mechanic

Requirement added September 19, 2026; the runtime catalog, runner, coverage validator and three-success progression are implemented. [tutorials.md](tutorials.md) owns the lesson contract and migration backlog. Every new or changed player-facing mechanic updates its tutorial mapping in the same PR: stable ids, setup and CPU policy, required human command, typed success/failure, retry, profile support, regression evidence and implementation state. Existing broad Practice lessons do not establish complete coverage.

Gameplay children own reusable sim setup, controlled CPU commands, objective evaluators and coverage validation. Presentation children own discovery, coaching, feedback, input-scheme copy and the paired `HowToPlay.cs` / `docs/how-to-play.md` updates. The lesson uses the real game; scripts manufacture an opportunity, never a credited player action or verdict. Planned/blocked coverage must name its owning issue. The catalog gate compares lessons to an independent mechanic inventory, so a feature omitted from both a lesson list and its tests is detectable. Human learning and transfer to Exhibition remain Jack's gate.

## 1.2 What a PR owes

Rules added 2026-09-22. Behavior docs stay. Bookkeeping and balance run on demand. When an older section, plan or issue asks a PR for more than this, this section wins.

**Tests.** Never run the full test suite locally. It freezes the shared Mac. Run `tools/test-fast.sh <Class> [<Class> ...]` for the classes you touched; it runs `Kind!=Balance`, narrowed to those classes. CI runs the breakage suite on every PR. A PR is done when it compiles, the breakage suite is green in CI on its final head, and the human gates that apply are named in the PR body.

**Balance runs only when Jack asks for a balance pass.** Balance and calibration means: the full test suite (Actions → **Full tests**, on GitHub as well as locally), `[Trait("Kind","Balance")]` tests, S-29 and the cohort bands, park-factor reports, flight probes and the evidence seals. Do not start any of them until Jack says he wants to balance the game (rule added 2026-09-22: constant balancing costs too much time). A PR that moves a feel or rule number, a tuning PR included, does not owe a cohort report or a Full tests run. It names the number and the move in the PR body and stops at the breakage suite. When Jack starts a balance pass, run Full tests (the `balance_only` input runs only the balance set).

**Evidence seals (FR-16).** A feature PR does not reseal, even when it edits a sealed file. Reseal only during a balance pass Jack asked for. A stale seal in the Full tests run is not a feature PR's failure.

**Trials (`trials/*`).** A feature PR does not owe twin edits or a report on both roots. Parity is restored on demand, when that trial is next used. If a breakage-suite test fails on a missing trial key, add that key and nothing more.

**Spec.** A PR that changes behavior updates the affected rule in [gameplay-spec.md](gameplay-spec.md) in the same PR. The rule says what the game does: numbers, units, scenario ids. It does not say who built it. Do not add PR numbers, "✅ Fx (#nnn, PR #nnn)" provenance, or register rows that name PRs. A status tag stays a bare tag (✅ / ⚠️ / ❌). The commit history records who did what. Existing provenance text stays; do not mass-delete it.

**Registers and ledgers.** A feature PR does not edit `docs/research/*.json` (`implementation_issues`, `validation_evidence`, `history`) or the ledger in a `docs/plan-*-implementation.md`. One batched docs PR updates them at a phase checkpoint or when Jack asks. A decision Jack makes (the answer to an open question) is behavior intent. It is still recorded, in that batched PR. Until then, quote it in the PR body.

**Debug protocol.** Add a row to `data/agent/debug-protocol.json` only for a novel failure signature (§2). A repeat of a known signature, or a row whose only news is a PR number, is not a row.

**Reading.** Start from "Start here" in [AGENTS.md](../AGENTS.md). Read the gameplay-spec sections your change touches. Look up the other docs when the work needs them. Research reports (`docs/research-*.md`, `docs/research/`) and handoffs are reference, not required reading.

## 1.3 Decided: C80 promoted

Jack decided on September 22, 2026: **promote** it. The compact-field copy's ordinary loop (#715–#723) is the shipped game, its numbers are the defaults in `data/`, and `trials/c80` is deleted. The specials and special statuses stay out of that contract, and the throw-before-cover rule is unchanged. Jack's sitting on the promoted game is still owed.

## 2. Debug protocol (remember)

✅ **R2 #649 / #656.** OpenGame's Debug Skill, as a catalog.

`data/agent/debug-protocol.json` (name stable) holds entries. Load with `DebugProtocol.Load` or `dotnet run --project src/GrandSluggers.Cli -- protocol`. `cli art` validates it. Code-side defaults are only the load fallback when the file is missing.

| Field | Meaning |
| --- | --- |
| `id` | Stable, like `brim-in-plate-lens` |
| `signature` | What an agent would grep or see (log line, still-gate fail, sitting note) |
| `stage` | `sim` / `cli-match` / `unity-console` / `still-gate` / `dcc` / `sitting` |
| `cause` | Root cause from the code map, with `file:line` when known |
| `fix` | The rail, not the patch |
| `promoted` | C# or Python `Type.Method`, or repository-relative shell validator entry point (`tools/name.sh`), once it is a gate; else empty. CI verifies the reference exists. |
| `issue` | Sitting child or epic that found it |
| `pr` | PR that verified the fix, once closed |

Seed from existing sitting children and `exact-work` / screenshot-gate fail rows. Recurring signatures **promote** to a test (`BagIsInsideTheFoulLine` shape): the protocol is the memory, the test is the gate.

Agents **load** the protocol at session start for the kind they are in. They **append** a new signature when they repair a novel failure, in the same PR as the fix. They do not keep this only in the PR body. A repeat of a known signature is not a new row, and neither is a row whose only news is a PR number.

A sitting note is still **one GitHub issue per finding** under the epic that owns the lie (#342 / #209 / #188). The protocol does not replace issues. Issues that repeat become `promoted` rows.

---

### 2.1 Scale and pace decisions (#693)

Before a session changes gameplay distances or clocks, read [gameplay-spec D19](gameplay-spec.md#02-field-proportions-and-race-calibration--d19-693). [The reference research](research-game-feel-693.md) and [the decision register](plan-game-feel-693.md) are reference: look up the rows the change touches. Compare reference versions explicitly; carry source status, units, uncertainty, and the accepting decision with every target. A proposed or unresolved number cannot become an active default or a verified debug-protocol fix.

Measure full races, not only outcomes: contact, pursuit/possession, command/release, receiver/coverage, runner/tag. Keep field/body proportions separate from camera projection. Use the existing trace/scenario/catalog paths; the versioned #702 extensions and evidence validation are specified in [race-traces.md](race-traces.md); their draft/merge state remains in the #693 plan. Preserve both fixed-input and fixed-tactical fixtures so an inverse carry solver cannot conceal a changed flight.

Gameplay, presentation, and art implementation stay separate and serial for shared feel. Coordinate #558 takes, #691 body scaling, and the book/stamp owners. A research PR may establish evidence and procedure; it cannot claim that a math check, test suite, or rebuilt player passes the human race/look gate.

## 3. Play traces (see baseball without Unity)

✅ **R3 #650 / #657.** Cousin: S-90 (same seed + commands → identical `PlayEvent` stream), S-01…S-92, `cli match --seed 7`, S-29 over fifty seeds.

A play is still decided by geometry (ball, runner, glove, bag). The dump is how an agent *looks* at that geometry at reasoning speed. The sim still owns the verdict; the JSON is observation.

- `dotnet run --project src/GrandSluggers.Cli -- match --trace` writes a `PlayTraceLog`: one record per tick with ball, each runner, the glove, bags, and typed `PlayEvent` facts (`PlayTrace`). `--trace file.json` writes the file and keeps the human log on stdout; `--trace` alone writes JSON to stdout and the captions to stderr.
- `LivePlaySystem.Recording` / `Match.Tracing` is the same dump the scenario harness and `cli match` share. Off by default, so S-29 allocates nothing extra.
- `PlayTraceTests`: a grounder (glove meets ball; runner vs bag is the out), a fly (catch is a radius at the window), a tag (runner and glove at the bag), a steal (break at release; pickoff only if already broke, D3). Captions are not the reason.
- [Race trace version 2](race-traces.md) adds effective-input identity, submitted commands, ordered possession/throw/receiver/runner marks, all fielders, and coverage. `RaceEvidence` validates source-and-budget records through `ContentDataValidator`. #702 is instrumentation, not calibration.
- S-29 and existing scenarios stay green. This epic does not retune `data/rules/`.

---

## 4. Dual stills (see the toy)

✅ **R4 #651 / #659.** Cousin: [screenshot-gate.md](screenshot-gate.md) in-game stills and `tools/still-gate-character.sh`. The DCC render is a named PR falsifier (`tools/dcc-still.sh`). A critic files instead of passing.

`data/agent/dual-stills.json` (name stable) holds the kinds. Load with `DualStills.Load` or `dotnet run --project src/GrandSluggers.Cli -- stills`. `cli art` validates it. Code-side defaults are only the load fallback when the file is missing.

For any change under `Art/Characters/`, `Art/Animation/Clips/`, `tools/blender/`, or Harbor kit meshes:

1. **DCC still** from `tools/dcc-still.sh` (`--clay` / `--sheets` / Harbor `--clay`). Named files: `scratchpad/stills/dcc-body.png`, `dcc-extras.png`, `dcc-{clip}.png`, `dcc-harbor-kit.png`. Catches "cap doesn't cover the hair" before import.
2. **In-game still** from `still-gate-character.sh` / `still-gate.sh`. Named files: `char-{id}-rest.png`, `char-{id}-pose.png` (park shots from still-gate). Catches brim-in-lens, HUD-on, wrong shot. A still can name a park and a night — `StillRequest.park` / `night`, `tools/still-gate.sh --park <id> [--night]` (✅ #829) — and the default park in daylight keeps today's names, so any other park and any night name themselves in the file (`plate-crystal-rink-night.png`).
3. Both PNGs in `scratchpad/stills/` and linked in the PR.
4. A **read-only critic** (`.claude/skills/look-critic/`, separate session or subagent) compares them to the screenshot-gate table and [silhouette-bible.md](silhouette-bible.md). Output: specific diffs. It **files** a child or a PR comment. It cannot mark #188 done. It cannot edit its own rubric.
5. The builder **stops**. Jack passes look.

Math-only, `dotnet test`, `unity-compile.sh`, the DCC bake, and a rebuilt `.app` are not a still. There is no CI image-diff.

---

## 5. Unity observation (later)

❌ **R5 #652.** Later. Does not block R2–R4.

If wired: Unity CLI / MCP may inspect hierarchy, read console, and trigger **Grand Sluggers → Capture Still Gate**. That is how an agent *views* presentation when the Editor is up.

Deny-list (never enable as a baseball rail):

- `/physics-3d-collision`, `/initialize-ai-navigation`
- `/new-unity-project`, `/implement-in-app-purchases`, `/setup-multiplayer-services`, `/build-live-game`
- Any skill that would decide an out, a safe, or a throw in PhysX or NavMesh

Before launching editors, load [editor-startup.md](editor-startup.md): Blender requires Metal access even in background mode; .NET outputs stay outside the Unity local package. Quit owned validation editors normally; never routinely kill them or discard scene recovery backups.

Personal Unity cannot `-batchmode`. `tools/unity-compile.sh` stays the CI csc gate. `python3 tools/local-player.py` stays Jack's window. Do not poll or restart while he is playing.

One GUI Unity user on this Mac at a time. Take the lock in `tools/unity_gui.py` before you launch, front or click an editor, or deliver a player. The still gates, `build-player.sh` and `local-player.py` take it for you. A live holder refuses the next session by name. Front an editor by its PID, never by name. A capture refuses while Jack's window is open until he says the machine is free (`--player-open-ok`). Delivery closes an open window only with `--replace`. Rules and the capture recipe: [editor-startup.md](editor-startup.md#unity-one-gui-editor-user-at-a-time). Protocol row: `gui-editor-focus-fight`.

---

## 6. Stage-save DCC

✅ **R6 #653 / #662.** Cousin: the Blender scripts already exist. Named stages with a save and a still at each, so a session continues from a checkpoint instead of one-shotting Harbor or a captain.

`data/agent/dcc-stages.json` (name stable) holds the five stages. Load with `DccStages.Load` or `dotnet run --project src/GrandSluggers.Cli -- stages`. `cli art` validates it. Code-side defaults are only the load fallback when the file is missing. One-shot is `banned`.

| Stage | Harbor kit | Character | Still |
| --- | --- | --- | --- |
| 1 blocking | diamond / wall ring volumes | `hero_shared_blockout.py` silhouette | `--clay` sheet |
| 2 fill | kit slots from `harbor_kit.py` | extras from `hero_shared_extras.py` + `extras.json` | `--clay` sheet |
| 3 motion | — | takes from `hero_shared_takes.py` | `--sheets` |
| 4 export | FBX into the catalog slot | FBX into the catalog slot | catalog FBX |
| 5 still | in-game park still | DCC still + in-game character still (R4) | dual stills |

Save after each stage (the script edit + the still). The next prompt names the stage it continues. One-shotting a captain extra or a kit mesh is a patch, banned in `.claude/skills/character-art/` and the Harbor kit header. Existing bake flags (`--clay`, `--sheets`, `--out`) still run. This rail does not retarget the rig or add a take.

---

## 7. Distill (file *and* remember)

✅ **R7 #654 / #660.** Cousin: playbook §5 (one issue per sitting finding). The finding also lands in the debug protocol (R2) and, when it repeats, in a skill or test. character-art grew from one real failed still: `swing-*-max-load` (#623 / `bat-through-head`).

After a sitting or a failed still:

1. File the child issue under the epic that owns the lie.
2. If the signature is novel, append a protocol row (R2) in the same PR as the fix, or in the sitting-child PR.
3. If the signature has fired twice, promote it to a validator or a scenario. Do not wait for a third.
4. If the lesson is procedural (how to look, how to bake), add it to `.claude/skills/character-art/` or this document, not only the PR body.

Grow `character-art` from failed stills. Do not add a second art skill.

---

## 8. Already shipped (do not rebuild)

Keep these rails. Do not replace them with a prompt-to-game stack.

| Rail | Where |
| --- | --- |
| Spec outranks code | [gameplay-spec.md](gameplay-spec.md) |
| How a phase runs | [playbook.md](playbook.md) |
| One worktree per child | [AGENTS.md](../AGENTS.md), [roadmap.md](roadmap.md) |
| Rules in data | `data/rules/` |
| Art catalog | `data/art/`, `cli art`, [art-rails.md](art-rails.md) |
| One rig; extras off until they read as toys | [character-motion.md](character-motion.md), `hero-shared`, D12 #687 |
| Headless baseball | `cli match`, S-01…S-92, S-29 |
| Play traces | `cli match --trace`, `PlayTrace`, `LivePlaySystem.Recording` |
| In-game still gate | [screenshot-gate.md](screenshot-gate.md) |
| Dual stills | `data/agent/dual-stills.json`, `tools/dcc-still.sh`, look-critic |
| Stage-save DCC | `data/agent/dcc-stages.json`, `cli stages`, character-art + Harbor kit stages |
| Human gates stay human | #346, #209 sittings, #188 |
| Scripted Harbor kit | `tools/blender/harbor_kit.py`, [tools/blender/README.md](../tools/blender/README.md) |
| Local standalone window | [local-player.md](local-player.md) |
| Debug protocol | `data/agent/debug-protocol.json`, `cli protocol` |
| Distill | [playbook.md](playbook.md) §5, `.claude/skills/character-art/` from #623, promote-on-second |

---

## Appendix A — Gap audit

Grouped by the child that owns the fix. Lines are "what exists today," not a hunt for a new engine.

| Id | Gap | Today | Child |
| --- | --- | --- | --- |
| G1 | Session kind is not a fail condition | Standing order in AGENTS.md (#648 / #655) | R1 ✅ |
| G2 | Sitting memory is GitHub issues only | `data/agent/debug-protocol.json`; playbook §5 is file + append + promote-on-second | R2 ✅, R7 ✅ |
| G3 | No loadable `(signature, cause, fix)` catalog | `data/agent/debug-protocol.json` + `DebugProtocol.Validate` / `cli protocol` | R2 ✅ |
| G4 | Agents cannot grep a play's geometry | `cli match --trace`, `PlayTrace` per tick | R3 ✅ |
| G5 | DCC still is not a PR falsifier | `tools/dcc-still.sh` → `scratchpad/stills/dcc-*.png` | R4 ✅ |
| G6 | No critic that files look diffs | `.claude/skills/look-critic/` files; cannot mark #188 | R4 ✅ |
| G7 | No Unity observation path | `unity-compile.sh`; personal Editor cannot `-batchmode` | R5 (later) |
| G8 | DCC stages are not named checkpoints | `data/agent/dcc-stages.json`; skill + Harbor kit name blocking → fill → motion → export → still | R6 ✅ |
| G9 | Failed stills do not grow the skill | `character-art` grew from `swing-*-max-load` (#623 / `bat-through-head`) | R7 ✅ |

---

## Appendix B — Children (acceptance)

Parent: **#647**. Sequence: R1 with the spec PR; R2 ∥ R3; R4 ∥ R6 after or beside them; R7 needs R2; R5 later and does not block Exhibition.

| Child | Owns | Exit | Serves | Order |
| --- | --- | --- | --- | --- |
| **R1. Session split** #648 | §1, G1 | AGENTS.md names the three kinds and the banned paths. A mixed-session change is a review fail. | all | With the spec PR |
| **R2. Debug protocol** #649 | §2, G2, G3 | ✅ #656. `data/agent/debug-protocol.json` + validator + five seeded rows. Agents load it. A new repair appends a row. | #209, #188 | After R1 |
| **R3. Play traces** #650 | §3, G4 | ✅ #657. Tick JSON of ball / runner / glove / bag. One test per a grounder, a fly, a tag, a steal. S-29 unchanged. | #209 | After R1; ∥ R2 |
| **R4. Dual stills** #651 | §4, G5, G6 | ✅ #659. `data/agent/dual-stills.json` + `tools/dcc-still.sh` + look-critic. DCC still + in-game still required in the PR. Critic files, does not pass. | #188 | After R1; ∥ R6 |
| **R5. Unity observation** #652 | §5, G7 | CLI/MCP can capture stills and read console. Deny-list documented and enforced. No PhysX outs. | #188, presentation | Later; after R3/R4 |
| **R6. Stage-save DCC** #653 | §6, G8 | character-art skill + harbor kit name the stages. A still at each. One-shot banned in the skill. `data/agent/dcc-stages.json` + `cli stages`. | #188 | With or after R4 |
| **R7. Distill** #654 | §7, G2, G9 | ✅ #660. Playbook §5 is file + append + promote-on-second. character-art grew from `swing-*-max-load` (#623). Promoted signatures name a real test. | #209, #188 | After R2 |

### Banned on every child

Prompt-to-game engines, Meshy / unique meshes / a second skeleton, new captains, new parks as products, Challenge, online, motion, a second input toolkit, Unity PhysX or NavMesh as baseball, an adversarial judge that can pass #188, shrinking a mesh to save a camera, growing `MatchDirector`, `git add -A`, passing a human gate.

### What a sitting should check (parent)

Nothing here is a sitting Jack must play. The product sittings stay #534, the narrated half, and #346. This epic is done when the next gameplay agent loads the protocol and a trace, and the next art agent files two stills and stops — without being told in Slack.
