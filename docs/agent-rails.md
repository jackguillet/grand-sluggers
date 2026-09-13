# Agentic rails — how agents inherit the playbook

This is the **source of truth for how coding agents build Grand Sluggers**. When a session and this document disagree, the session is wrong. When this document is silent, the child issue adds the rule here in the same PR.

The bar is not a one-shot playable demo. The bar is the stack in [AGENTS.md](../AGENTS.md): Harbor Exhibition a stranger can finish, sitting-found children, a toy that reads HUD-off, authored sound. Agents are the production line. They do not pass human gates.

Companion docs: [playbook.md](playbook.md) (how a phase runs), [roadmap.md](roadmap.md) (sequence), [gameplay-spec.md](gameplay-spec.md) (baseball), [art-rails.md](art-rails.md) (slots), [character-motion.md](character-motion.md) (takes), [screenshot-gate.md](screenshot-gate.md) (stills). Feel numbers stay in `data/feel/`. Rule numbers stay in `data/rules/`. Art slots stay in `data/art/`. Agent memory that must survive a session lives in `data/agent/` once R2 ships.

Status tags used throughout, first checked against `f419603` (2026-09-13). A ✅ names the PR that closed it. Appendix A keeps the gaps as the work.

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
7. **One worktree per child. Never `git add -A`.** Stacked PRs against `main`. After merge: `dotnet test`, `cli art`, `unity-compile.sh`, then `python3 tools/local-player.py` when Jack needs the window.

### 0.1 Decisions (steal / reject)

Surveyed 2026-09-13 from X (Paper Route / @builtbysketch via @zekeatchan, OpenGame, VibeGame, Unity official agent plugin, The Long Silence, ThePrimeagen JSON replay, Nex loft / Blender MCP stages) and mapped onto rails this repo already has. Agents do not relitigate these.

| # | Question | Decision | Why |
| --- | --- | --- | --- |
| D1 | Who writes the brief? | **Jack / the spec.** Agents execute. | Paper Route: the model cannot decide what is worth keeping. We already have this as gameplay-spec §0. |
| D2 | Mix logic and visuals in one session? | **No.** Session kinds in §1. | Paper Route item 3. A mixed session patches the toy to hide a play bug, or the play to hide a look bug. |
| D3 | Where do meshes live? | **Python scripts that export FBX**, not authored `.blend` as source. | Paper Route item 4; already `tools/blender/*.py`. |
| D4 | Which pictures falsify art? | **DCC still and in-game still.** Both in the PR. Agents stop. | Paper Route item 6 ("cap doesn't cover the hair"). In-game still-gate already exists; DCC still is not yet a named PR falsifier. |
| D5 | Can a critic pass look? | **No.** It files. Jack passes. | The Long Silence judge never finished, which is correct for #188. VibeGame's generation/review split is the part we take. |
| D6 | How does the next session start smarter? | **A living debug protocol in data**, not only GitHub issues. Recurring signatures promote to tests. | OpenGame Debug Skill (signature, root cause, verified fix). Sittings already file children; they do not yet become a loadable protocol. |
| D7 | How do agents playtest baseball? | **Headless geometry traces** from `cli match` / the scenario harness, loopable without Unity. | ThePrimeagen JSON replay; VibeGame frame-sync. We already have S-90 and S-01…S-92. We do not yet dump ball / runner / glove / bag per tick. |
| D8 | Unity official plugin / CLI / MCP? | **Observation only**, and only after traces exist. Deny-list in §5. | Unity plugin 2026-09-09. PhysX / NavMesh / IAP skills would put baseball in the wrong place. Personal Unity cannot `-batchmode`. |
| D9 | One-shot a captain or Harbor kit? | **No.** Named stages, save after each, a still at each stage. | Nex loft six-stage Blender MCP (2026-09-11). Scripts exist; stages are not a rail. |
| D10 | Prompt-to-game / Meshy heroes / a second engine? | **No.** | We are not generating a new game or a new skeleton. Unique packages are deferred. |
| D11 | Shrink or hide a mesh to save a camera? | **No.** Tune the shot. | AGENTS.md. Twitter "keep the engine small" is not permission to starve the toy. |

---

## 1. Session split

A session declares its kind in the prompt and on the issue. File owners and the banned list come from this table, not from taste.

| Kind | Owns | Banned |
| --- | --- | --- |
| **Gameplay** | `data/rules/`, `src/GrandSluggers.Sim/`, scenario ids, `cli match` | `tools/blender/`, `data/art/extras.json` (except a clip marker the sim already reads), still PNGs, Unity presentation directors, cameras |
| **Presentation** | `data/feel/` cameras and timing, HUD, `HowToPlay.cs`, `docs/how-to-play.md`, stamps | Rule tables, `MatchDirector` switches, Blender, new captains |
| **Art** | one catalog slot in `data/art/`, the matching Blender script, still PNGs, `cli art` | Sim rules, C# poses, a second rig, a new hero, shrinking a mesh to save a shot |

✅ **R1 #648 / #655.** Standing order in [AGENTS.md](../AGENTS.md) and `.grok/rules/agent-rails.md`. A mixed-session change is a review fail.

End each session with a playable artifact of its kind before the next prompt: gameplay → `dotnet test` + `cli match` in the spec band; presentation → named shot / book page; art → still PNGs in `scratchpad/stills/`. Do not rebuild the Mac player as proof of look.

---

## 2. Debug protocol (remember)

❌ **R2 #649.** OpenGame's Debug Skill, as a catalog.

`data/agent/debug-protocol.json` (name stable) holds entries:

| Field | Meaning |
| --- | --- |
| `id` | Stable, like `brim-in-plate-lens` |
| `signature` | What an agent would grep or see (log line, still-gate fail, sitting note) |
| `stage` | `sim` / `cli-match` / `unity-console` / `still-gate` / `dcc` / `sitting` |
| `cause` | Root cause from the code map, with `file:line` when known |
| `fix` | The rail, not the patch |
| `promoted` | Test or validator name once it is a gate, else empty |
| `issue` | Sitting child or epic that found it |
| `pr` | PR that verified the fix, once closed |

Seed from existing sitting children and `exact-work` / screenshot-gate fail rows. Recurring signatures **promote** to a test (`BagIsInsideTheFoulLine` shape): the protocol is the memory, the test is the gate.

Agents **load** the protocol at session start for the kind they are in. They **append** a new signature when they repair a novel failure, in the same PR as the fix. They do not keep this only in the PR body.

A sitting note is still **one GitHub issue per finding** under the epic that owns the lie (#342 / #209 / #188). The protocol does not replace issues. Issues that repeat become `promoted` rows.

---

## 3. Play traces (see baseball without Unity)

⚠️ **R3 #650.** Cousin: S-90 (same seed + commands → identical `PlayEvent` stream), S-01…S-92, `cli match --seed 7`, S-29 over fifty seeds. Missing: a per-tick geometry dump an agent can grep.

A play is still decided by geometry (ball, runner, glove, bag). The dump is how an agent *looks* at that geometry at reasoning speed.

Observable when R3 ships:

- `dotnet run --project src/GrandSluggers.Cli -- match --trace` (or the scenario harness equivalent) writes JSON: one record per tick with ball, each runner, glove, bags, the live `PlayEvent`.
- A cheap loop can replay or scan N plays without Unity.
- At least one test asserts a named play's trace contains the geometric reason (a tag, a bag, a catch) — not only the caption.
- S-29 and existing scenarios stay green. This epic does not retune `data/rules/`.

This is VibeGame's "frame-synchronous control" without replacing Unity, and ThePrimeagen's JSON-replay loop without a second engine.

---

## 4. Dual stills (see the toy)

⚠️ **R4 #651.** Cousin: [screenshot-gate.md](screenshot-gate.md) in-game stills and `tools/still-gate-character.sh`. Missing: the DCC render as a named PR falsifier, and a critic that files instead of passing.

For any change under `Art/Characters/`, `Art/Animation/Clips/`, `tools/blender/`, or Harbor kit meshes:

1. **DCC still** from the script (`--clay` / `--sheets` / a named stage render). Catches "cap doesn't cover the hair" before import.
2. **In-game still** from `still-gate-character.sh` / `still-gate.sh`. Catches brim-in-lens, HUD-on, wrong shot.
3. Both PNGs in `scratchpad/stills/` and linked in the PR.
4. A **read-only critic** (separate session or subagent) compares them to the screenshot-gate table and [silhouette-bible.md](silhouette-bible.md). Output: specific diffs. It **files** a child or a PR comment. It cannot mark #188 done. It cannot edit its own rubric.
5. The builder **stops**. Jack passes look.

Math-only, `dotnet test`, `unity-compile.sh`, the DCC bake, and a rebuilt `.app` are not a still.

---

## 5. Unity observation (later)

❌ **R5 #652.** Later. Does not block R2–R4.

If wired: Unity CLI / MCP may inspect hierarchy, read console, and trigger **Grand Sluggers → Capture Still Gate**. That is how an agent *views* presentation when the Editor is up.

Deny-list (never enable as a baseball rail):

- `/physics-3d-collision`, `/initialize-ai-navigation`
- `/new-unity-project`, `/implement-in-app-purchases`, `/setup-multiplayer-services`, `/build-live-game`
- Any skill that would decide an out, a safe, or a throw in PhysX or NavMesh

Personal Unity cannot `-batchmode`. `tools/unity-compile.sh` stays the CI csc gate. `python3 tools/local-player.py` stays Jack's window. Do not poll or restart while he is playing.

---

## 6. Stage-save DCC

⚠️ **R6 #653.** Cousin: the Blender scripts already exist. Missing: named stages with a save and a still at each, so a session continues from a checkpoint instead of one-shotting Harbor or a captain.

| Stage | Harbor kit | Character |
| --- | --- | --- |
| 1 Blocking | diamond / wall ring volumes | `hero_shared_blockout.py` silhouette |
| 2 Fill | kit slots from `harbor_kit.py` | extras from `hero_shared_extras.py` + `extras.json` |
| 3 Motion | — | takes from `hero_shared_takes.py` |
| 4 Export | FBX into the catalog slot | FBX into the catalog slot |
| 5 Still | in-game park still | DCC still + in-game character still (R4) |

Save after each stage. The next prompt names the stage it continues. One-shotting a captain extra or a kit mesh is a patch.

---

## 7. Distill (file *and* remember)

⚠️ **R7 #654.** Cousin: playbook §5 (one issue per sitting finding). Missing: the finding also lands in the debug protocol (R2) and, when it repeats, in a skill or test.

After a sitting or a failed still:

1. File the child issue under the epic that owns the lie.
2. Append a protocol row (R2) in the same PR as the fix, or in the sitting-child PR.
3. If the signature has fired twice, promote it to a validator or a scenario. Do not wait for a third.
4. If the lesson is procedural (how to look, how to bake), add it to `.grok/skills/character-art/` or this document, not only the PR body.

The Long Silence dumped a `blender-hardsurface` skill. We already have `character-art`. Grow it from failed stills.

---

## 8. Already shipped (do not rebuild)

These are the rails Twitter is rediscovering. Keep them. Do not replace them with a prompt-to-game stack.

| Rail | Where |
| --- | --- |
| Spec outranks code | [gameplay-spec.md](gameplay-spec.md) |
| How a phase runs | [playbook.md](playbook.md) |
| One worktree per child | [AGENTS.md](../AGENTS.md), [roadmap.md](roadmap.md) |
| Rules in data | `data/rules/` |
| Art catalog | `data/art/`, `cli art`, [art-rails.md](art-rails.md) |
| One rig + extras | [character-motion.md](character-motion.md), `hero-shared` |
| Headless baseball | `cli match`, S-01…S-92, S-29 |
| In-game still gate | [screenshot-gate.md](screenshot-gate.md) |
| Human gates stay human | #346, #209 sittings, #188 |
| Blender MCP for Harbor kit | `tools/blender/harbor_kit.py`, `.grok/config.toml` |
| Local standalone window | [local-player.md](local-player.md) |

---

## Appendix A — Gap audit

Grouped by the child that owns the fix. Lines are "what exists today," not a hunt for a new engine.

| Id | Gap | Today | Child |
| --- | --- | --- | --- |
| G1 | Session kind is not a fail condition | Standing order in AGENTS.md + `.grok/rules/agent-rails.md` (#648 / #655) | R1 ✅ |
| G2 | Sitting memory is GitHub issues only | Playbook §5 | R2, R7 |
| G3 | No loadable `(signature, cause, fix)` catalog | — | R2 |
| G4 | Agents cannot grep a play's geometry | `PlayEvent` stream, S-90, no per-tick dump | R3 |
| G5 | DCC still is not a PR falsifier | Clay/sheets in `character-art`; screenshot-gate is in-game | R4 |
| G6 | No critic that files look diffs | Humans pass #188 | R4 |
| G7 | No Unity observation path | `unity-compile.sh`; personal Editor cannot `-batchmode` | R5 (later) |
| G8 | DCC stages are not named checkpoints | Scripts exist, one-shot is possible | R6 |
| G9 | Failed stills do not grow the skill | `character-art` is static | R7 |

---

## Appendix B — Children (acceptance)

Parent: **#647**. Sequence: R1 with the spec PR; R2 ∥ R3; R4 ∥ R6 after or beside them; R7 needs R2; R5 later and does not block Exhibition.

| Child | Owns | Exit | Serves | Order |
| --- | --- | --- | --- | --- |
| **R1. Session split** #648 | §1, G1 | AGENTS.md + `.grok/rules/agent-rails.md` name the three kinds and the banned paths. A mixed-session change is a review fail. | all | With the spec PR |
| **R2. Debug protocol** #649 | §2, G2, G3 | `data/agent/debug-protocol.json` + validator + at least five seeded rows from existing sittings. Agents load it. A new repair appends a row. | #209, #188 | After R1 |
| **R3. Play traces** #650 | §3, G4 | Tick JSON of ball / runner / glove / bag. One test per a grounder, a fly, a tag, a steal. S-29 unchanged. | #209 | After R1; ∥ R2 |
| **R4. Dual stills** #651 | §4, G5, G6 | DCC still + in-game still required in the PR for character / kit changes. Critic files, does not pass. screenshot-gate and character-art skill updated. | #188 | After R1; ∥ R6 |
| **R5. Unity observation** #652 | §5, G7 | CLI/MCP can capture stills and read console. Deny-list documented and enforced. No PhysX outs. | #188, presentation | Later; after R3/R4 |
| **R6. Stage-save DCC** #653 | §6, G8 | character-art skill + harbor kit name the stages. A still at each. One-shot banned in the skill. | #188 | With or after R4 |
| **R7. Distill** #654 | §7, G2, G9 | Playbook §5 includes protocol append + promote-on-second. character-art grows from one real failed still. | #209, #188 | After R2 |

### Banned on every child

Prompt-to-game engines, Meshy / unique meshes / a second skeleton, new captains, new parks as products, Challenge, online, motion, a second input toolkit, Unity PhysX or NavMesh as baseball, an adversarial judge that can pass #188, shrinking a mesh to save a camera, growing `MatchDirector`, `git add -A`, passing a human gate.

### What a sitting should check (parent)

Nothing here is a sitting Jack must play. The product sittings stay #534, the narrated half, and #346. This epic is done when the next gameplay agent loads the protocol and a trace, and the next art agent files two stills and stops — without being told in Slack.
