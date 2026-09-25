# Agents

Grand Sluggers is a **complete, polished party baseball game** we will still want in five years. The bar is Nintendo-level Exhibition (then local 1v1): Super Sluggers *systems* — cameras, HUD, plays, lineup, juice — with **original toys**. Not a prototype that lucks into a still. Not a Mario clone.

## Start here

1. Read this file.
2. Read only the spec files your change touches: `docs/spec/NN-*.md`, one per section of `docs/gameplay-spec.md` (the index). Scenarios are `docs/spec/appendix-b-scenarios.md`.
3. Look up the other docs when the work needs them. Accepted decision plans are in `docs/decisions/`. Finished research reports, ledgers and handoffs are history in `docs/archive/` (index: `docs/archive/README.md`), and sealed evidence is in `docs/research/`; they are reference, not required reading, and never the source of a rule.

Where to look things up. **What is shipped and open: `docs/status.md`.** **Contract** (a rule lives in exactly one of these): rules of play `docs/gameplay-spec.md` (when code and spec disagree, the code is wrong); how agents work `docs/agent-rails.md` (when a session and that document disagree, the session is wrong); couch map `docs/how-to-play.md`; tutorials `docs/tutorials.md`; look `docs/look.md`, silhouettes `docs/silhouette-bible.md`, art slots `docs/art-rails.md`, characters and motion `docs/character-motion.md`, stills `docs/screenshot-gate.md`; validation `docs/validation.md`; delivery `docs/local-player.md`. **Direction:** vision `docs/vision.md`, sequence `docs/roadmap.md`, how a phase runs `docs/playbook.md`, accepted decisions `docs/decisions/`. **History, never a rule:** `docs/archive/`.

## The stack (do this, in order)

Pick work from the top. Do not pick a lower row because it is easier.

1. **Harbor Exhibition is playable.** Jack finishes a half from Call time How to play, with one pad and then with two pads, no Slack. [#346](https://github.com/jackguillet/grand-sluggers/issues/346) / [#209](https://github.com/jackguillet/grand-sluggers/issues/209). Agents **do not pass** human gates.
2. **Sitting-found children.** File them. Do not silently patch. Parent is the epic that owns the lie (#342 book, #209 play, #188 toy).
3. **The toy reads HUD-off.** Six captains name themselves at gameplay distance. Cameras look at the body, not a brim. [#188](https://github.com/jackguillet/grand-sluggers/issues/188).
4. **Authored sound.** Shipped: bat crack, glove pop and crowd bed are authored clips in `data/art/audio.json` slots ([#223](https://github.com/jackguillet/grand-sluggers/issues/223)). Generated tones are not the product; a new sound fills a slot, after play, not instead of it.

**Do not start** (the one list; the roadmap and agent-rails link here): Challenge (#36), extra parks as products (#37), unique meshes for role players (#25), online, motion, Toy Field, live ops, licensed music, 40-man, full-screen blinds (#38), Nintendo IP, a second player-facing client, a second input toolkit, a second skeleton or a second motion system, a prompt-to-game engine, Unity PhysX or NavMesh as baseball. Every captain is the one rig; unique packages are deferred (`docs/character-package.md`). Extras stay off until they read as toys (#687).

**Fields: rails and greyboxes are allowed; park art is not.** The fields rails are code-complete ([status](docs/status.md)). A non-Harbor park gets no mesh, texture or authored light rig before its rules are green and Jack has sat its greybox (#37). What a park hazard may and may not decide is principle 2 in `docs/spec/00-decisions.md`; the accepted directions are `docs/decisions/plan-fields.md` (FD-01 … FD-19).

**World: ten parks and ten captains may be built as rails, data and greyboxes now; their art waits for #346.** WD-01 B (#1133): the world data, the new captains in data, the new park files, new hazard patterns, greyboxes and the map picker are allowed; no park art, backdrop art or map art before Jack passes #346, and a park's art still waits for its greybox sitting. Each child waits for the decisions it names in `docs/decisions/plan-world.md` (WD-01 … WD-18).

## Done means you played it

Unit tests are necessary and not sufficient. When Jack names a thing (a batter's box, a dirt shape, a camera, a HUD), **exact** is the bar; similar is a fail. Your job is to not hand Jack a cousin of what he asked for.

- Research the spec (MLB, the reference still, existing tables, `docs/gameplay-spec.md`) **before** coding. Write the numbers and name the relationships the still must show (bags *inside* the foul line, the 1B–2B–3B apron thicker and more curved than the home legs, a 6-inch box gap). Tests must encode those relationships (`BagIsInsideTheFoulLine`, `BoxesClearThePlate`), not "a mesh exists." Cartoon fat is allowed; wrong topology is not. A play is decided by geometry (ball, runner, glove, bag), never by a roll or a caption; rule numbers live in `data/rules/` (a trial's own copies in `trials/`, never a second default).
- **View the change** (Play `HarborDiamond` + Scene orbit, still, live bounds vs the reference). Math-only is not verification. If you cannot look, say so — do not claim look done.
- "Close" / "better" from Jack is a correction, not acceptance.
- If you change a screen, **be that screen as a player**: every captain if select, both pads and both seats if controls, title → lineup → first pitch if front-of-house.
- A menu still is not a half. South on a menu is confirm, not baseball.
- Human gates (#346 and screenshot gates) stay human. Note what stuck. File children. Do not declare pass because CI is green.
- Look / character work is a human gate. Dual stills in `docs/screenshot-gate.md` (DCC `dcc-*.png` + in-game `char-{id}-rest.png` / `char-{id}-pose.png`) are the falsifier. A critic files; Jack passes. `dotnet test`, `unity-compile.sh`, the DCC bake, and a rebuilt `.app` are not a still. Agents do not pass look.
- Fail if a stranger would need Slack, F2, or `docs/how-to-play.md` on disk to finish the path you touched.
- Ask before coding: *will this still be right with two pads, a pop fly instead of a hopper, Ashlord as well as Rio, and a friend on the couch?* If not, put the system in the right place.

## What a PR owes

The rule is `docs/agent-rails.md` §1.2, in one place: tests (`tools/test-fast.sh <Classes you touched>`, never the full suite locally; CI runs the breakage suite), when a PR is done, balance (only when Jack asks), evidence seals, trials, the spec rule (no provenance, 600-character lines), registers and ledgers, and the debug protocol. There is one diamond: 80-ft basepaths; its numbers are the defaults in `data/`.

## Rails, not patches

Every decision serves the long-term product. **Do not ship a quick fix.** If a ticket can close with a hack *or* a rail, **build the rail**. Take longer. Do not “just make the still / test / issue pass.”

A change is a **patch** — reject it:

- Special-cases one play, one camera, one pad count, one captain, or one HUD screen
- Auto-resolves a user-owned play in the sim as a caption
- Hardcodes a `Vector3`, FOV, or layout when a table exists (`data/feel/`, `data/art/`, `BroadcastHud`, `CarnivalFront`)
- Forks 1P vs 2P presentation (SET camera, HUD anchors, body/item scale)
- Grows a `switch` in `MatchDirector` instead of a named system other plays can call
- Leaves the next play type or the next captain to invent the same thing again
- Shrinks or hides a mesh to save one camera (tune the shot / look, not the toy)

A change is a **rail** — do this:

- Lives in data or a named director every play type can use
- Works the same for 1P and 1v1 unless the design names a real difference
- Lets the player own the verb
- Has a test that would catch the *next* play, seat, or captain — not only this screenshot
- Updates `docs/how-to-play.md` **and** `HowToPlay.cs` in the same PR when a couch verb or camera changes (CI: `tools/book-pair.py`; label `book-unchanged` when HowToPlay code moves but the book does not)

Catalog first, files second. New clip / VFX / audio / skin = JSON slot + validator + empty folder, then the asset. Serial for feel (cameras, timing, in-play verbs). Parallel only for filling slots after the shared rig exists.

## Tutorials grow with gameplay

Every player-facing mechanic needs a playable tutorial, including repeatable setup and real success/failure evidence. Contract and migration backlog: `docs/tutorials.md`. New or changed mechanics update their tutorial coverage in the same PR (lesson id, setup/CPU policy, player-owned action, objective, regression evidence, profile support and status). Planned/blocked lessons are debt, not completed coverage. Script opportunities and opponents; never award success for a CPU-performed player action or force an out. Reuse the existing sim and Practice entry. Gameplay owns setup/objectives; a separate Presentation child owns the Tutorials UI and paired book updates. Keep building these bones before generating more artwork.

## Session kind

Declare one kind per session, in the prompt and on the issue. Mixing them is a patch (shrinking a mesh to save a camera, putting an out in Unity, posing in C#) and a review fail. This table is the one list of owners and bans. Tracker: #647.

| Kind | Owns | Banned |
| --- | --- | --- |
| **Gameplay** | `data/rules/`, `trials/`, `src/GrandSluggers.Sim/`, scenario ids, `cli match` | `tools/blender/`, `data/art/extras.json` (except a clip marker the sim already reads), still PNGs, Unity presentation directors, cameras |
| **Presentation** | `data/feel/` cameras and timing, HUD, `HowToPlay.cs`, `docs/how-to-play.md`, stamps | Rule tables, `MatchDirector` switches, Blender, new captains |
| **Art** | one catalog slot in `data/art/`, the matching Blender script, still PNGs, `cli art` | Sim rules, C# poses, a second rig, a new hero, shrinking a mesh to save a shot |

Each kind has a procedure skill: `.claude/skills/gameplay-session/`, `.claude/skills/presentation-session/`, `.claude/skills/character-art/` (art; `look-critic` reads its stills). A durable pitfall goes into its skill or a protocol row, not only a private note.

End the session with the artifact of its kind: gameplay → `tools/test-fast.sh <Classes you touched>` + `cli match`; presentation → named shot or book page; art → still PNGs in `scratchpad/stills/`. Do not rebuild the `.app` as proof of look.

## Art — Super Sluggers weight, original toys

Steal the *feel* of Mario Super Sluggers. Do not steal Mario.

- **Look:** oversized cartoon toys, fat silhouettes, saturated toon. Identity is palette + size. Heads read at catcher-eye. 10-foot UI.
- **Cast:** Rio, Vale, Zig, Brondo, Konga, Ashlord, Elder Fenn + faction role players. Role players reuse the captain body type. No skin lists extras until they read as toys (#687).
- **Characters are DCC assets.** One rig (`hero-shared`), one body script, one takes script; every verb is a Blender take baked for both hands; C# holds no pose. A captain is proportions + palette in data. Contract: `docs/character-motion.md`. Procedure: `.claude/skills/character-art/`. Style lock: `tools/blender/style-lock/`. No caps yet; hats come back as accessories.
- **Harbor is the expensive diamond** (the “real stadium”). Other parks stay JSON until Exhibition is the reason people stay; their rules and their greybox may be built first (`docs/decisions/plan-fields.md`), their art may not. Harbor kit meshes are authored in `tools/blender/harbor_kit.py` (procedure: `tools/blender/README.md`). Do not invent Unity-only park art when a kit slot exists.
- **Original pictures, original tones.** No Nintendo samples, meshes, mushrooms, plumbers, princesses, or set dressing. CI: `tools/original_ip.py` scans data, art, the book and couch copy.
- Missing art is a placeholder that does not crash. Do not invent a new pipeline to hide a missing file.
- Gameplay cameras look at the **chest / dirt / bag**, not the brim. Ashlord’s hat in the lens is a framing bug, not a scale bug.

If you generate or drop art, fill an existing slot and keep identity across a set (`docs/look.md`). Do not generate a new hero.

## Operating

- One GitHub child issue = one worktree. Never share the main working copy. Never `git add -A`. In Claude Code, `tools/bash_guard.py` (wired in `.claude/settings.json`) refuses `git add -A` / `git add .` and `dotnet test` without `--filter`; other agents follow the rule by hand.
- Load `data/agent/debug-protocol.json` at session start for the kind you are in (`cli protocol --kind gameplay|presentation|art`: the unpromoted rows in full; `--full` adds the promoted ones). After a sitting or a failed still: file, append, promote on the second firing — the rule is `docs/agent-rails.md` §7.
- Sim owns baseball. Unity presents. `unity/` Play `HarborDiamond` **is the game**. `GrandSluggers.Play` is a debug sandbox.
- The game is gamepad only. Pad 1 is player 1; pad 2 is a second gamepad (player 2). There is no keyboard or mouse scheme.
- Couch copy lives in `HowToPlay` / `CarnivalFront` / `BroadcastHud`, not scattered strings.
- Content ids in `data/` stay stable. Feel numbers live in `data/feel/`. Do not grow `MatchDirector`.
- Falsify with `tools/test-fast.sh <Classes you touched>`, `dotnet run --project src/GrandSluggers.Cli -- art`, `cli match`, `tools/unity-compile.sh`. Capture look stills with `tools/dcc-still.sh` and `tools/still-gate-character.sh`. Personal Unity cannot `-batchmode`.
- After a feel or look merge: a skeptic pass plays the named path. A still that only works because of a one-off is not done.

## Local standalone delivery (Jack's default)

Jack tests the Mac standalone game in its own window, not Unity Play. After your approved changes merge, run `python3 tools/local-player.py`; this update, build and restart is authorized as the normal post-merge step. The rules — previews, `--replace`, the shared GUI Unity lock, what to report — are in `docs/local-player.md` ("Agent rules").
