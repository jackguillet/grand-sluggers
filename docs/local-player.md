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

The owned editor writes build evidence outside its temporary project files. After a successful build, it exits normally (`EditorApplication.Exit`) in the same editor update. It does not wait for a later update: a background editor can stop updating after the build, so an exit queued for later may never run. If the editor is still open 20 seconds after successful evidence, the supervisor sends it one normal quit request, addressed to the exact process it launched. That request is the quit Apple Event that Quit sends, not a signal. The supervisor then waits up to two minutes for shutdown. It never sends termination signals to Unity. After a failed or unfinished build, the editor stays open for inspection and normal Quit.

A successful build is copied into a versioned local release beside a copy of that revision's `data/`. The Unity build result must name the same full revision and `Assets/Scenes/HarborDiamond.unity`; delivery rejects mismatched evidence. The old game remains open during the build. Only after the app and matching data exist does delivery ask the previous project player to terminate (with `--replace`) and launch the new app window at 1280 × 800. Restart ends the current match. No timer or background monitor restarts a match unexpectedly.

Builds, logs, and the running revision live under `~/Library/Application Support/Grand Sluggers/local-player/`. The GUI Unity lock is `~/Library/Application Support/Grand Sluggers/unity-gui.lock`. `current.json` identifies the running app, source revision, preview/main status, PID, player log, and evidence paths. Each release keeps `build-evidence.json` from Unity and `launch-evidence.json` for the process observed alive three seconds after launch. `build.log` records the latest Unity build. Previous release folders and build worktrees are retained for diagnosis. Remove old build worktrees with `git worktree remove` after diagnosis; preserve the active release.

If the build fails or times out, the existing game remains open. If the new player exits at launch, previous release apps remain available to reopen. Delivery refuses concurrent runs and any other GUI Unity use. Build evidence and launch-only process evidence stay separate from a rendered-window check and an actual player sitting for human gates; see [validation.md](validation.md).

## Sharing the Mac

Several sessions share this Mac and its one GUI Unity editor. Delivery holds the machine-wide GUI Unity lock (`tools/unity_gui.py`, see [editor-startup.md](editor-startup.md)) from the start of the run until the new window is launched. While another session captures stills, builds a player or delivers, delivery refuses. The refusal names that session's purpose, PID, worktree and start time. Wait for it to finish, or ask that session.

Delivery closes the window that is open. Before it builds or moves main, it looks for a delivered window. If one is open, it names the window's kind, revision, data profile, PID and delivery time, and it stops:

```text
local-player: A delivered game window is open: main d49c527351 on trials/pitch5 (pid 77559, delivered 2026-09-22T17:20:37Z). Delivery would close it and end any match in it; nothing was built and main was not moved. Re-run with --replace when closing it is yours to do (docs/local-player.md).
```

Re-run with `--replace` when closing that window is yours to do:

- An older `main` on the shipped data, replaced by merged main. This is the normal post-merge step (AGENTS.md "Local standalone delivery").
- A window that you delivered in this session.

A preview or a trial window that another session delivered is someone's test. Ask Jack before you replace it.

With `--replace`, delivery logs `Closing <window> for <new revision>…` for each window that it closes. If a window opens during the build and `--replace` was not given, delivery closes nothing, launches nothing, and keeps the new release folder.

`python3 tools/unity_gui.py status` shows the lock holder, every GUI editor with its project, and every delivered window.
