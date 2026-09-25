---
name: presentation-session
description: Change a camera, the HUD, a stamp, a menu, the couch book (HowToPlay / docs/how-to-play.md) or pad controls. Use for any Presentation-kind session.
---

# Presentation session

Kind: **Presentation** (AGENTS.md "Session kind" is the list of what you own and may not touch). Couch map: `docs/how-to-play.md`. What a PR owes: `docs/agent-rails.md` §1.2. Memory: `cli protocol --kind presentation`.

## Before you code

1. Worktree from `origin/main`; check `gh pr list --state all --search "<issue>"` first.
2. `dotnet run --project src/GrandSluggers.Cli -- protocol --kind presentation`.
3. Find the table the change belongs in: shots and timing in `data/feel/` (`shots.json`, `table.json`), HUD layout in `BroadcastHud`, menus in `CarnivalFront`, book pages in `HowToPlay`. Couch copy lives in `HowToPlay` / `CarnivalFront` / `BroadcastHud` / `PauseMenu`, never in a Unity view (`CouchCopyTests`).
4. Ask: does it hold for one pad and two, both seats, every captain, a pop fly and a hopper? 1P and 1v1 do not fork unless the design names a difference.

## Where things are

- **Cameras:** `PlayCamera.LiveBeat` / `PlayCamera.LiveFraming` over a typed `LiveView` is the one table; `CameraDirector.Live(framing)` is the one client call. A new beat is a `Beat` arm plus a `shots.json` id — no `Vector3` in code. Cameras look at the chest, dirt or bag, never the brim; tune the shot, never shrink a mesh.
- **Stamps:** `PlayStamp.Label(PlayEvent)` only; a new stamp rides a `PlayOutcome` field. `PlayStampTests` feeds a lying caption, so a stamp read from a caption fails.
- **Positions after a play:** the completing frame resets the live field; read `PlayOutcome.Bodies`, not the live map on the result beat.
- **Book copy has a pixel budget.** `BookletLayoutTests` measures each rendered page at couch size. Fit copy before you run it: compare `textwrap.wrap` line counts of the old and new lines at widths 20–30; the screen, fielding and box pages have no slack — replace a clause, never add one. Fit the copy to the band; do not widen the band or drop the assertion.
- **Pad words:** the game is gamepad only. `HowToPlayTests` refuses a book line that names a keyboard key or a mouse button (`HowToPlay.NamesKeyboard`, whole key names). Do not file or fix keyboard-only issues.

## The pair

A couch verb or camera change updates `docs/how-to-play.md` **and** `HowToPlay.cs` / `HowToPlay.Screens.cs` in the same PR. CI enforces it (`tools/book-pair.py`); label the PR `book-unchanged` only when HowToPlay code moves and the book does not. A mechanic's lesson copy belongs to `docs/tutorials.md`.

## Check

- Tests: `tools/test-fast.sh <Classes you touched>` (never the full suite locally). Unity code: `UNITY_PACKAGE_ASSEMBLIES=/Users/jack/repos/grand-sluggers/unity/Library/ScriptAssemblies tools/unity-compile.sh`, and name the result in the PR.
- Look at it: `python3 tools/local-player.py --preview <worktree>` after committing, and **be the screen as a player** — every captain on a select screen, both pads and both seats for controls, title → lineup → first pitch for front-of-house. A still is not a half. Say what you could not look at.
- Stills: one GUI Unity editor on the Mac at a time (`tools/unity_gui.py`, `docs/editor-startup.md`). If another session holds the lock, wait or ask; never break it.

## Finish

- A novel failure appends a protocol row with `kind: presentation` (agent-rails §7).
- Stage explicit paths, merge on green CI with `MERGEABLE`/`CLEAN`, deliver after merge. The end artifact of the session is a named shot or a book page; look and play gates stay Jack's.
