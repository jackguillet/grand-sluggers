# Test in the standalone Mac window

Jack's default test handoff is the standalone Mac game. The agent runs delivery after merged changes, so Jack does not need to open Unity or type a build command.

After a merge:

```sh
python3 tools/local-player.py
```

To try a committed change before its human gate / merge:

```sh
python3 tools/local-player.py --preview /absolute/path/to/worktree
```

If a delivered window is already open, either command names it and stops (see [Sharing the Mac](#sharing-the-mac)). Add `--replace` when closing that window is yours to do.

The default command finds the primary checkout through Git's common directory, requires it to be on `main`, fetches `origin`, and fast-forwards to `origin/main`. Git refuses overlapping local edits and divergent histories. The command never resets, stashes, changes branches, or discards those edits. The preview command does not update main.

Builds use a dedicated worktree and the editor version in ProjectVersion.txt. Unity runs as a GUI process to respect the Personal license restriction on batch mode; it consumes the existing PlayerBuildGate request. Preview builds also get a fresh build worktree, so the source worktree is not changed by Unity imports.

A fresh build worktree has no Unity `Library`, and a full import is the slow part of a build. So before Unity starts, delivery clones the `Library` of the newest release that built and launched: same Unity version, no editor open on it. The clone is APFS copy-on-write (`cp -c`), so it takes seconds and no disk until Unity writes. Unity then imports only what changed. The log line and `revision.json` (`librarySeed`) name the seed. If a build fails after a seeded import, the error says so; re-run with `--fresh-library` to import from scratch.

The owned editor writes build evidence outside its temporary project files. After a successful build, it exits normally (`EditorApplication.Exit`) in the same editor update. It does not wait for a later update: a background editor can stop updating after the build, so an exit queued for later may never run. If the editor is still open 20 seconds after successful evidence, the supervisor sends it one normal quit request, addressed to the exact process it launched. That request is the quit Apple Event that Quit sends, not a signal. The supervisor then waits up to two minutes for shutdown. It never sends termination signals to Unity. After a failed or unfinished build, the editor stays open for inspection and normal Quit.

A successful build is copied into a versioned local release beside that revision's runtime data: the folders and file types `data/package.json` lists. Tooling folders such as `data/agent` and scripts such as the audio bake stay in the repository. The game checks at startup that its catalog and the process rules table read the same data root. The Unity build result must name the same full revision and `Assets/Scenes/HarborDiamond.unity`; delivery rejects mismatched evidence. The old game remains open during the build. Only after the app and matching data exist does delivery ask the previous project player to terminate (with `--replace`) and launch the new app window at 1280 × 800. Restart ends the current match. No timer or background monitor restarts a match unexpectedly.

Builds, logs, and the running revision live under `~/Library/Application Support/Grand Sluggers/local-player/`. The GUI Unity lock is `~/Library/Application Support/Grand Sluggers/unity-gui.lock`. `current.json` identifies the running app, source revision, preview/main status, PID, player log, and evidence paths. Each release keeps `build-evidence.json` from Unity and `launch-evidence.json` for the process observed alive three seconds after launch. `build.log` records the latest Unity build. After a good delivery, delivery keeps the newest three releases and build worktrees (`--keep N`), every release an open window runs from, and the build worktrees those releases came from. It removes the rest; a build worktree with an editor open on it is never removed. A failed build's worktree stays until later deliveries push it out of the newest three. `python3 tools/local-player.py --prune-only` applies the same rule without building, under the same locks.

If the build fails or times out, the existing game remains open. If the new player exits at launch, previous release apps remain available to reopen. Delivery refuses concurrent runs and any other GUI Unity use. Build evidence and launch-only process evidence stay separate from a rendered-window check and an actual player sitting for human gates; see [validation.md](validation.md).

## Agent rules

This section is the one place these rules live; AGENTS.md links here.

- Jack tests the standalone window. Do not send him to Unity Play as the default test handoff.
- After your approved changes merge, run `python3 tools/local-player.py`. This update, build and restart is authorized as the normal post-merge delivery step.
- Never force, reset or stash local `main`, and never merge a human-gated change just to deliver it. Report conflicts or build failures; keep the existing game intact.
- For an unmerged change Jack needs to try, commit it in its worktree and run `--preview /absolute/path/to/worktree`. Say clearly that the window is a preview; local `main` stays on merged code.
- When delivery refuses because another session holds the GUI Unity lock, or a window someone else delivered is open, do not break the lock: report it, or ask Jack before `--replace` on a preview or trial window ([Sharing the Mac](#sharing-the-mac)).
- Confirm the new window renders, state the running revision, and report the human gates that remain. Building or launching alone does not pass a gameplay or look gate.
- No background polling or restarts while Jack is playing; the working agent runs delivery after a merge or a requested preview.

## Sharing the Mac

Several sessions share this Mac and its one GUI Unity editor. Delivery holds the machine-wide GUI Unity lock (`tools/unity_gui.py`, see [editor-startup.md](editor-startup.md)) from the start of the run until the new window is launched. While another session captures stills, builds a player or delivers, delivery refuses. The refusal names that session's purpose, PID, worktree and start time. Wait for it to finish, or ask that session.

Delivery closes the window that is open. Before it builds or moves main, it looks for a delivered window. If one is open, it names the window's kind, revision, data profile, PID and delivery time, and it stops:

```text
local-player: A delivered game window is open: main d49c527351 on trials/pitch5 (pid 77559, delivered 2026-09-22T17:20:37Z). Delivery would close it and end any match in it; nothing was built and main was not moved. Re-run with --replace when closing it is yours to do (docs/local-player.md).
```

The window in this example ran on `trials/pitch5`. That overlay is retired (#883): its last key is shipped, so there is nothing left to pass to `--trial trials/pitch5`.

Re-run with `--replace` when closing that window is yours to do:

- An older `main` on the shipped data, replaced by merged main. This is the normal post-merge step ([Agent rules](#agent-rules)).
- A window that you delivered in this session.

A preview or a trial window that another session delivered is someone's test. Ask Jack before you replace it.

With `--replace`, delivery logs `Closing <window> for <new revision>…` for each window that it closes. If a window opens during the build and `--replace` was not given, delivery closes nothing, launches nothing, and keeps the new release folder.

`python3 tools/unity_gui.py status` shows the lock holder, every GUI editor with its project, and every delivered window.
