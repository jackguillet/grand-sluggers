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

The default command finds the primary checkout through Git's common directory, requires it to be on `main`, fetches `origin`, and fast-forwards to `origin/main`. Git refuses overlapping local edits and divergent histories. The command never resets, stashes, changes branches, or discards those edits. The preview command does not update main.

Builds use a dedicated worktree and the editor version in ProjectVersion.txt. Unity runs as a GUI process to respect the Personal license restriction on batch mode; it consumes the existing PlayerBuildGate request. The delivery command closes only the editor process it started. Preview builds also get a fresh build worktree, so the source worktree is not changed by Unity imports.

A successful build is copied into a versioned local release beside a copy of that revision's `data/`. The Unity build result must name the same full revision and `Assets/Scenes/HarborDiamond.unity`; delivery rejects mismatched evidence. The old game remains open during the build. Only after the app and matching data exist does delivery ask the previous project player to terminate and launch the new app window at 1280 × 800. Restart ends the current match. No timer or background monitor restarts a match unexpectedly.

Builds, logs, and the running revision live under `~/Library/Application Support/Grand Sluggers/local-player/`. `current.json` identifies the running app, source revision, preview/main status, PID, player log, and evidence paths. Each release keeps `build-evidence.json` from Unity and `launch-evidence.json` for the process observed alive three seconds after launch. `build.log` records the latest Unity build. Previous release folders and build worktrees are retained for diagnosis. Remove old build worktrees with `git worktree remove` after diagnosis; preserve the active release.

If the build fails or times out, the existing game remains open. If the new player exits at launch, previous release apps remain available to reopen. Delivery refuses concurrent runs. Build evidence and launch-only process evidence stay separate from a rendered-window check and an actual player sitting for human gates; see [validation.md](validation.md).
