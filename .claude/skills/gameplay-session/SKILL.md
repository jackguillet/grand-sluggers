---
name: gameplay-session
description: Change a baseball rule, the sim, a scenario or a tutorial objective (data/rules/, src/GrandSluggers.Sim/, scenario ids, cli match). Use for any Gameplay-kind session.
---

# Gameplay session

Kind: **Gameplay** (AGENTS.md "Session kind" is the list of what you own and may not touch). Rules of play: `docs/gameplay-spec.md` and the one `docs/spec/NN-*.md` your change touches. What a PR owes: `docs/agent-rails.md` §1.2. Memory: `cli protocol --kind gameplay`.

## Before you code

1. Worktree from `origin/main`; check `gh pr list --state all --search "<issue>"` — another session may already have it.
2. `dotnet run --project src/GrandSluggers.Cli -- protocol --kind gameplay` and read the unpromoted rows.
3. Read the spec section and its Appendix B rows (`docs/spec/appendix-b-scenarios.md`). A scenario id is a contract: grep the id, then read the fixture against the row's **setup** and the assertions against its **expect**. A test that carries an id is not always the row.
4. Write the numbers and relationships the change must hold before the code. A play is decided by geometry (ball, runner, glove, bag), never a roll or a caption; numbers live in `data/rules/`.

## Loop

- Tests: `tools/test-fast.sh <Classes you touched>` only. Never the full suite locally (it freezes the shared Mac; `tools/bash_guard.py` refuses a bare `dotnet test`). CI runs the breakage suite.
- A game: `dotnet run --project src/GrandSluggers.Cli -- match --seed 7 [--park X] [--night] [--trace out.json]`. The Release CLI's apphost cannot find the runtime: run `dotnet .artifacts/bin/GrandSluggers.Cli/release/GrandSluggers.Cli.dll …`. Parse the final line by its digits.
- A play's trace in a test: `live.Recording = true` before `BeginLive`, then `live.TakeTrace(play).Marks` (`ThrowRelease`, `Reception`, …). `match.TraceLog()` fills only from `Match.Play` / `AutoPlay`.
- Probe what a scenario really produces: temporarily append `Assert.Fail("PROBE " + …)` with outs, throws, stamp and bodies, run it with `--logger "console;verbosity=normal"` (`-v q` hides the message), then `git checkout` the file. Never edit a test file while a background build runs.
- Fixtures are real flights: `FlightFixtures.Hit / Landing / OverTheFence`. A hand-typed carry next to a launch that cannot produce it disagrees with the preview.
- Sweep for a fixture: turn the body into `X_Row(params)`, add a temporary `ZZ_Sweep` fact in the class that grids the params and `Assert.Fail`s the passing list; delete it before commit.

## Traps

- **Static state tears under parallel tests.** xUnit runs classes in parallel and each picks a park. A per-park static cache must be a `ConcurrentDictionary` or locked (`FieldBounds.Of`, `HarborWall.Loop`); never "last park wins".
- **The completing frame resets the live field.** `Match.Complete` → `LivePlay.Reset()` runs inside the tick whose result carries `CompletedPlay`. Read positions from `PlayOutcome.Bodies`; drop that frame from a speed track (it reads thousands of ft/s).
- **Unity ticks at `Time.deltaTime`.** Test frame-dependent sim code at an uneven dt (0.021 s), not only the harness's 1/60.
- **The sim is also a Unity package** (netstandard 2.1). A BCL call Unity lacks, or a positional call site in `unity/` you did not grep, passes `dotnet` and breaks Unity: run `UNITY_PACKAGE_ASSEMBLIES=/Users/jack/repos/grand-sluggers/unity/Library/ScriptAssemblies tools/unity-compile.sh` from a worktree.
- **Moving a constant into data** can change it: `(double)4.2f` is 4.19999980926514. Read the C# type first.
- **A derived table copy names every section** (`RulesTable.AtLevel` / `AtPark`); a new section must be added to each.
- **Two changes, one PR:** run the split (each change alone) before you credit a cohort move to either.

## Balance is not yours

Do not re-pin night games (`tools/regenerate.sh night-games`), reseal evidence, run `Kind=Balance`, S-29 cohorts or Full tests unless Jack started a balance pass. Some balance tests are red on main; that is not your PR's failure. A PR that moves a rule number names the move in its body and stops at the breakage suite.

## Finish

- Update the rule in its `docs/spec/` file (no issue numbers or dates; lines ≤ 600 characters; CI checks both).
- A player-facing mechanic updates its tutorial coverage in the same PR (`docs/tutorials.md`; `cli tutorials`).
- A novel failure signature appends a protocol row with `kind: gameplay` (agent-rails §7).
- Stage explicit paths. Open the PR; merge only when CI is green on the final head and `mergeable` is `MERGEABLE`/`CLEAN`.
- **Stacked PRs:** merging the bottom lands only the base on `main`. After a stack merge check `git log origin/main..origin/<tip>`; if it is not empty, open the tip → `main` PR.
- After a merge, deliver (`python3 tools/local-player.py`, rules in `docs/local-player.md`). Human gates stay human.
