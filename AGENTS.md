# Agents

Grand Sluggers is a **complete, polished party baseball game** we will still want in five years. The bar is Nintendo-level Exhibition (then local 1v1): Super Sluggers *systems* — cameras, HUD, plays, lineup, juice — with **original toys**. Not a prototype that lucks into a still. Not a Mario clone.

## Start here

1. Read this file.
2. Read only the spec files your change touches: `docs/spec/NN-*.md`, one per section of `docs/gameplay-spec.md` (the index). Scenarios are `docs/spec/appendix-b-scenarios.md`.
3. Look up the other docs when the work needs them. Research reports (`docs/research-*.md`, `docs/research/`, for example `docs/research-game-feel-708.md`) and handoffs are reference, not required reading.

Where to look things up. **What is shipped and open: `docs/status.md`.** Vision: `docs/vision.md`. Look: `docs/look.md`. Couch map: `docs/how-to-play.md`. **Rules of play: `docs/gameplay-spec.md`** (when code and spec disagree, the code is wrong). Sequence: `docs/roadmap.md`. How a phase runs: `docs/playbook.md`. **How agents work: `docs/agent-rails.md`** (when a session and that document disagree, the session is wrong). Silhouettes: `docs/silhouette-bible.md`. Art slots: `docs/art-rails.md`. Characters and motion: `docs/character-motion.md`. Fields: `docs/plan-fields.md` (#814).

## The stack (do this, in order)

Pick work from the top. Do not pick a lower row because it is easier.

1. **Harbor Exhibition is playable.** Jack finishes a half from Call time How to play, with one pad and then with two pads, no Slack. [#346](https://github.com/jackguillet/grand-sluggers/issues/346) / [#209](https://github.com/jackguillet/grand-sluggers/issues/209). Agents **do not pass** human gates.
2. **Sitting-found children.** File them. Do not silently patch. Parent is the epic that owns the lie (#342 book, #209 play, #188 toy).
3. **The toy reads HUD-off.** Six captains name themselves at gameplay distance. Cameras look at the body, not a brim. [#188](https://github.com/jackguillet/grand-sluggers/issues/188).
4. **Authored sound.** Shipped: bat crack, glove pop and crowd bed are authored clips in `data/art/audio.json` slots ([#223](https://github.com/jackguillet/grand-sluggers/issues/223)). Generated tones are not the product; a new sound fills a slot, after play, not instead of it.

**Do not start:** Challenge (#36), extra parks as products (#37), unique meshes for role players (#25), online, motion, 40-man, full-screen blinds (#38), a second input toolkit, a second skeleton or a second motion system, a prompt-to-game engine, Unity PhysX or NavMesh as baseball. Every captain is the one rig; unique packages are deferred (`docs/character-package.md`). Extras stay off until they read as toys (#687).

**Fields (#814, FD-01): rails and greyboxes are allowed; park art is not.** Allowed: the park schema, the geometry owner, the per-park environment table, the hazard runtime, park factors, the one field kit, the look gates, and a data-driven greybox, proven on one second park first and then one park at a time. Every rail lands at Harbor parity. A park hazard may surprise (FD-08): it may draw from the match's seeded stream to decide what the hazard *does* (when it fires, which exit it picks, where it sends the ball), as a typed live event. It never awards an out, a hit, a drop or a catch; the ball and the bodies still decide the play. Still banned: a mesh, a texture or an authored light rig for a non-Harbor park before that park's rules are green and Jack has sat its greybox (#37). Contract, decisions and epics: `docs/plan-fields.md`. A child names its FD ids, FR ids and banned files.

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

Behavior docs stay. Bookkeeping and balance run on demand. Contract: `docs/agent-rails.md` §1.2.

- Never run the full test suite locally; it freezes the shared Mac. Run `tools/test-fast.sh <Classes you touched>`. CI runs the breakage suite on every PR.
- A PR is done when it compiles, the breakage suite is green in CI on its final head, and the human gates that apply are noted.
- **Do not balance the game until Jack says he wants to.** Balance means the full test suite (Actions → Full tests, on GitHub too, not only locally), the `Kind=Balance` tests, S-29 and cohort bands, park-factor reports, flight probes and evidence seals. Constant balancing costs too much time. A PR that moves a feel or rule number, even a tuning PR, names the move in its body and stops at the breakage suite. Jack starts a balance pass; then run Full tests (`balance_only` for the balance set only).
- A feature PR does not reseal the evidence seals, edit a `trials/` twin (none is open today), or touch a decision register or an implementation ledger. One batched docs PR updates registers and ledgers at a phase checkpoint or when Jack asks.
- A behavior change updates its rule in its `docs/spec/` file in the same PR. Keep every spec line at 600 characters or fewer (`SpecLineLengthTests`). Write the rule, not the provenance: no issue or PR numbers, no "✅ (#nnn, PR #nnn)", no dates. CI refuses an added spec line that carries one (`tools/spec-provenance.py`).
- A debug-protocol row is for a novel failure signature only.
- There is one diamond: 80-ft basepaths. Its numbers are the defaults in `data/`.

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
- Updates `docs/how-to-play.md` **and** `HowToPlay.cs` in the same PR when a couch verb or camera changes

Catalog first, files second. New clip / VFX / audio / skin = JSON slot + validator + empty folder, then the asset. Serial for feel (cameras, timing, in-play verbs). Parallel only for filling slots after the shared rig exists.

## Tutorials grow with gameplay

Every player-facing mechanic needs a playable tutorial, including repeatable setup and real success/failure evidence. Contract and migration backlog: `docs/tutorials.md`. New or changed mechanics update their tutorial coverage in the same PR (lesson id, setup/CPU policy, player-owned action, objective, regression evidence, profile support and status). Planned/blocked lessons are debt, not completed coverage. Script opportunities and opponents; never award success for a CPU-performed player action or force an out. Reuse the existing sim and Practice entry. Gameplay owns setup/objectives; a separate Presentation child owns the Tutorials UI and paired book updates. Keep building these bones before generating more artwork.

## Session kind

Declare one kind per session. Mixing them is a patch (shrinking a mesh to save a camera, putting an out in Unity, posing in C#). Contract: `docs/agent-rails.md` §1. Tracker: #647.

| Kind | Owns | Banned |
| --- | --- | --- |
| **Gameplay** | `data/rules/`, `trials/`, Sim, scenario ids, `cli match` | Blender, extras (except a clip marker the sim already reads), still PNGs, Unity presentation |
| **Presentation** | cameras, HUD, `HowToPlay` / `docs/how-to-play.md`, stamps | Rule tables, `MatchDirector` switches, Blender, new captains |
| **Art** | one `data/art/` slot, the matching Blender script, still PNGs, `cli art` | Sim rules, C# poses, a second rig, a new hero, shrinking a mesh to save a shot |

End the session with the artifact of its kind: gameplay → `tools/test-fast.sh <Classes you touched>` + `cli match`; presentation → named shot or book page; art → still PNGs in `scratchpad/stills/`. Do not rebuild the `.app` as proof of look.

## Art — Super Sluggers weight, original toys

Steal the *feel* of Mario Super Sluggers. Do not steal Mario.

- **Look:** oversized cartoon toys, fat silhouettes, saturated toon. Identity is palette + size. Heads read at catcher-eye. 10-foot UI.
- **Cast:** Rio, Vale, Zig, Brondo, Konga, Ashlord, Elder Fenn + faction role players. Role players reuse the captain body type. No skin lists extras until they read as toys (#687).
- **Characters are DCC assets.** One rig (`hero-shared`), one body script, one takes script; every verb is a Blender take baked for both hands; C# holds no pose. A captain is proportions + palette in data. Contract: `docs/character-motion.md`. Procedure: `.claude/skills/character-art/`. Style lock: `tools/blender/style-lock/`. No caps yet; hats come back as accessories.
- **Harbor is the expensive diamond** (the “real stadium”). Other parks stay JSON until Exhibition is the reason people stay; their rules and their greybox may be built first (`docs/plan-fields.md`), their art may not. Harbor kit meshes are authored in `tools/blender/harbor_kit.py` (procedure: `tools/blender/README.md`). Do not invent Unity-only park art when a kit slot exists.
- **Original pictures, original tones.** No Nintendo samples, meshes, mushrooms, plumbers, princesses, or set dressing.
- Missing art is a placeholder that does not crash. Do not invent a new pipeline to hide a missing file.
- Gameplay cameras look at the **chest / dirt / bag**, not the brim. Ashlord’s hat in the lens is a framing bug, not a scale bug.

If you generate or drop art, fill an existing slot and keep identity across a set (`docs/look.md`). Do not generate a new hero.

## Operating

- One GitHub child issue = one worktree. Never share the main working copy. Never `git add -A`. In Claude Code, `tools/bash_guard.py` (wired in `.claude/settings.json`) refuses `git add -A` / `git add .` and `dotnet test` without `--filter`; other agents follow the rule by hand.
- Load `data/agent/debug-protocol.json` at session start for the kind you are in (`cli protocol`). A novel repair appends a row in the same PR as the fix; a repeat or a PR name is not a row. If the signature has fired twice, promote it to a validator or a scenario. If the lesson is procedural, grow `.claude/skills/character-art/` or `docs/agent-rails.md`. GitHub sitting children stay; they are not the memory. Art sessions also load `data/agent/dual-stills.json` (`cli stills`) and `data/agent/dcc-stages.json` (`cli stages`) and walk the stages: blocking → fill → motion → export → still. One-shotting a captain extra or a kit mesh is a patch.
- Sim owns baseball. Unity presents. `unity/` Play `HarborDiamond` **is the game**. `GrandSluggers.Play` is a debug sandbox.
- The game is gamepad only. Pad 1 is player 1; pad 2 is a second gamepad (player 2). There is no keyboard or mouse scheme.
- Couch copy lives in `HowToPlay` / `CarnivalFront` / `BroadcastHud`, not scattered strings.
- Content ids in `data/` stay stable. Feel numbers live in `data/feel/`. Do not grow `MatchDirector`.
- Falsify with `tools/test-fast.sh <Classes you touched>`, `dotnet run --project src/GrandSluggers.Cli -- art`, `cli match`, `tools/unity-compile.sh`. Capture look stills with `tools/dcc-still.sh` and `tools/still-gate-character.sh`. Personal Unity cannot `-batchmode`.
- After a feel or look merge: a skeptic pass plays the named path. A still that only works because of a one-off is not done.

## Local standalone delivery (Jack's default)

- Jack tests the Mac standalone game in its own window. Do not send him to Unity Play as the default test handoff.
- After your approved changes merge, run `python3 tools/local-player.py`. It fetches origin, fast-forwards the primary checkout's `main`, builds in a dedicated worktree with the installed GUI Unity editor, packages the matching data, and restarts the standalone window after a successful build.
- This update/build/restart is authorized as the normal post-merge delivery step. Never force/reset/stash local main or merge a human-gated change just to deliver it. Report conflicts or build failures; keep the existing game intact.
- For an unmerged change Jack needs to try, commit it in its worktree and run `python3 tools/local-player.py --preview /absolute/path/to/worktree`. Say clearly that the window is a preview; local main stays on merged code.
- Sessions share this Mac. Delivery holds the machine-wide GUI Unity lock, and it refuses, naming the holder, while another session captures, builds or delivers. If a delivered window is open, delivery names that window's revision and trial and stops before it builds. Add `--replace` when closing that window is yours to do. An older `main` on the shipped data is the normal post-merge replacement. A preview or a trial window is someone's test: ask Jack before you replace it.
- Confirm the new standalone window renders, state the running revision, and report remaining human gates. Building or launching alone does not pass a gameplay/look gate.
- Details and diagnostics: `docs/local-player.md`. No background polling/restarts while Jack is playing; the working agent runs delivery after a merge or requested preview.
