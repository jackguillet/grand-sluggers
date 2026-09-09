# Agents

Grand Sluggers is a **complete, polished party baseball game** we will still want in five years. The bar is Nintendo-level Exhibition (then local 1v1): Super Sluggers *systems* — cameras, HUD, plays, lineup, juice — with **original toys**. Not a prototype that lucks into a still. Not a Mario clone.

Vision: `docs/vision.md`. Look: `docs/look.md`. Couch map: `docs/how-to-play.md`. Sequence: `docs/roadmap.md`. Silhouettes: `docs/silhouette-bible.md`. Art slots: `docs/art-rails.md`. Characters: `docs/character-package.md`.

## The stack (do this, in order)

Agents start here. Do not pick a lower row because it is easier.

1. **Harbor Exhibition is playable.** Jack finishes a half from Call time How to play, pad and keyboard+mouse, no Slack. [#346](https://github.com/jackguillet/grand-sluggers/issues/346) / [#209](https://github.com/jackguillet/grand-sluggers/issues/209). Agents **do not pass** human gates.
2. **Sitting-found children.** File them. Do not silently patch. Parent is the epic that owns the lie (#342 book, #209 play, #188 toy).
3. **The toy reads HUD-off.** Six captains name themselves at gameplay distance. Cameras look at the body, not a brim. [#188](https://github.com/jackguillet/grand-sluggers/issues/188).
4. **Authored sound.** Bat crack, glove pop, crowd bed. Generated tones are not the product. [#223](https://github.com/jackguillet/grand-sluggers/issues/223). After play, not instead of it.

**Do not start:** Challenge (#36), extra parks as products (#37), unique meshes for role players (#25), online, motion, 40-man, full-screen blinds (#38), a second input toolkit. Unique *captains* are DCC Generic packages (`docs/character-package.md`) — not Mixamo, not a posed GLB, not a second Humanoid.

## Done means you played it

Unit tests are necessary and not sufficient. **Exact** is the bar; similar is a fail. Rule: `.grok/rules/exact-work.md`.

- Research the spec (MLB, the reference still, existing tables) **before** coding. Write the numbers. Tests must encode those relationships, not "a mesh exists."
- **View the change** (Play `HarborDiamond` + Scene orbit, still, live bounds vs the reference). Math-only is not verification. If you cannot look, say so — do not claim look done.
- "Close" / "better" from Jack is a correction, not acceptance.
- If you change a screen, **be that screen as a player**: every captain if select, both schemes if controls, title → lineup → first pitch if front-of-house.
- A menu still is not a half. HID Space is confirm, not baseball.
- Human gates (#346 and screenshot gates) stay human. Note what stuck. File children. Do not declare pass because CI is green.
- Look / character work is a human gate. A still in `docs/screenshot-gate.md` (character rest + posed limb) is the falsifier. `dotnet test`, `unity-compile.sh`, and a rebuilt `.app` are not a still. Agents do not pass look.
- Fail if a stranger would need Slack, F2, or `docs/how-to-play.md` on disk to finish the path you touched.
- Ask before coding: *will this still be right with two pads, a pop fly instead of a hopper, Ashlord as well as Rio, and a friend on the couch?* If not, put the system in the right place.

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

## Art — Super Sluggers weight, original toys

Steal the *feel* of Mario Super Sluggers. Do not steal Mario.

- **Look:** oversized cartoon toys, fat silhouettes, saturated toon, personality in extras (brim, crown, goggles, snout, horns). Heads read at catcher-eye. 10-foot UI.
- **Cast:** Rio, Vale, Zig, Brondo, Konga, Ashlord, Elder Fenn + faction role players. Role players reuse the captain body type and **must not** grow captain extras.
- **Characters are DCC assets.** Mesh + armature + painted weights (or Blender-authored rigid pieces) + clips. Unity: Humanoid only for T-pose bipeds; unique anatomy is **Generic** + clips on **that** rig. A posed unrigged GLB is a source, not a player mesh. Do not heat-weight it, freeze a SMR to hide tearing, or drive it with Rio eulers / `swing.fbx`. Spec: `docs/character-package.md`. Procedure: `.grok/skills/character-art/`. Style lock: `tools/blender/style-lock/`. The original six stay on `hero-shared` until they are packages.
- **Harbor is the expensive diamond** (the “real stadium”). Other parks stay JSON until Exhibition is the reason people stay. Harbor kit meshes are authored in **Blender MCP** (`tools/blender/harbor_kit.py`, server in `.grok/config.toml`). Do not invent Unity-only park art when a kit slot exists.
- **Original pictures, original tones.** No Nintendo samples, meshes, mushrooms, plumbers, princesses, or set dressing.
- Missing art is a placeholder that does not crash. Do not invent a new pipeline to hide a missing file.
- Gameplay cameras look at the **chest / dirt / bag**, not the brim. Ashlord’s hat in the lens is a framing bug, not a scale bug.

If you generate or drop art, fill an existing slot and keep identity across a set (`docs/look.md`). Do not generate a new hero.

## Operating

- One GitHub child issue = one worktree. Never share the main working copy. Never `git add -A`.
- Sim owns baseball. Unity presents. `unity/` Play `HarborDiamond` **is the game**. `GrandSluggers.Play` is a debug sandbox.
- Gamepad is the couch product. Keyboard + mouse are the same scheme, player 1 only. Pad 2 is a second gamepad.
- Couch copy lives in `HowToPlay` / `CarnivalFront` / `BroadcastHud`, not scattered strings.
- Content ids in `data/` stay stable. Feel numbers live in `data/feel/`. Do not grow `MatchDirector`.
- Falsify with `dotnet test`, `dotnet run --project src/GrandSluggers.Cli -- art`, `cli match`, `tools/unity-compile.sh`. Look/character: still-gate PNG in the PR. Personal Unity cannot `-batchmode`.
- After a feel or look merge: a skeptic pass plays the named path. A still that only works because of a one-off is not done.
