# Validation lanes

Grand Sluggers keeps portable rules/content checks separate from Unity-specific evidence. A pass in one lane does not stand in for another.

## Portable checks

These run from a clean checkout on macOS or Linux and do not require Unity or a checkout-specific `Library/`:

```sh
dotnet test src/GrandSluggers.Sim.Tests/GrandSluggers.Sim.Tests.csproj
dotnet run --project src/GrandSluggers.Cli -- art
python3 -m unittest discover -s tools/tests -p 'test_*.py'
```

The .NET test assembly contains simulation and content contracts only. Tool tests cover revision-safe local delivery and the compiler source inventory.

## Narrow Unity C# compile

`tools/unity-compile.sh` invokes the compiler shipped with the pinned editor. It recursively compiles the tracked Sim, Runtime, `Assembly-CSharp`, and Editor source groups, including `MatchBootstrap.cs`. It needs package DLLs imported for this checkout, or an explicit configured cache:

```sh
UNITY_EDITOR=/path/to/6000.5.9f1 \
UNITY_PACKAGE_ASSEMBLIES=/path/to/ScriptAssemblies \
tools/unity-compile.sh
```

The script never searches another working copy. Its pass means only that this compiler emulation succeeded; it does not prove Unity imported assets or built a player.

## Configured Unity import gate

`tools/unity-project-validate.sh` is for a licensed, configured runner that supports Unity batch mode. Set `UNITY_EDITOR_BIN` to the Unity executable and explicitly set `GS_UNITY_BATCH_LICENSED=1` on that runner. Do not set the opt-in for the local Personal editor; local delivery uses the GUI path. The gate requires a clean tracked checkout, records its full Git revision, imports the real project, verifies the four expected source assemblies, runs art validation, and opens `Assets/Scenes/HarborDiamond.unity`.

It writes `unity/Temp/validation/unity-evidence.json` and a Unity log. The evidence names the revision, Unity version, assemblies, scene, result, and any art errors. GitHub runs this job only when the repository variable `UNITY_VALIDATION_ENABLED` is `true`, on a runner labeled `self-hosted`, `macOS`, and `grand-sluggers-unity`. Configure `UNITY_EDITOR_BIN`, `UNITY_EDITOR_ROOT`, and `GS_UNITY_BATCH_LICENSED=1` as repository variables on that runner.

## Harbor standalone evidence

`python3 tools/local-player.py` builds a clean detached worktree and passes that worktree's full revision into `PlayerBuildGate`. Delivery refuses a successful build result whose revision or scene does not match the requested Harbor build. Each versioned release contains:

- `build-evidence.json`: Unity version, full revision, Harbor scene, output path, timestamp, and build result.
- `launch-evidence.json`: the same revision and scene plus the launched process observed alive after three seconds; it is explicitly marked `launch-only`.
- `player.log`: runtime output for that launch.

Launch-only evidence proves that the Harbor player process started and stayed alive for the observation window. It does not prove that a frame rendered, the title-to-half route worked, controller ownership worked, presentation quality passed, or a screenshot gate passed. Those remain named human checks and must be reported separately.

The configured Unity gate requires the exact editor version pinned in `unity/ProjectSettings/ProjectVersion.txt`. It captures that version before launching Unity and rejects mismatched runtime or output evidence. An import from another compatible editor is not a passing validation of the pinned project.

## Opt-in live-play lifecycle gate

In a dedicated validation worktree, open Harbor in the pinned GUI Unity editor and choose **Grand Sluggers → Verify Live Play Lifecycle** from Edit mode. This is an Editor-only, opt-in regression harness. For command-line GUI launch, set `GS_VALIDATION_REVISION` to the clean checkout's full Git revision and optionally set `GS_LIVE_PLAY_EVIDENCE` to an absolute output path, then pass `-projectPath /absolute/worktree/unity -executeMethod GrandSluggers.EditorTools.LivePlayLifecycleGate.Run` to the pinned Unity executable. Do not use `-batchmode` for Personal Unity, or `-quit`: the gate enters Play mode asynchronously.

The default output is `unity/Temp/live-play-lifecycle.json`. Startup is bounded to 180 seconds; each scenario is bounded by its flight duration. Failure evidence records the active scenario, phase, elapsed/live clocks, score, ownership, possession, pause and effect state. On success, eight CPU/human cases verify solo and loaded homers, a wall robbery, and a loaded walk-off through actual `TickLive` → Result → `TickFlow` → SET/GameOver, including exactly-once scoring and next-batter progression. Inspect `ok`, `cases`, the exact revision and editor version; a file existing is not a pass. Close the dedicated validation editor afterward.

Contact and the robbery catch observation are injected deterministically. The gate does not operate physical controllers, test the player's wall-jump timing, play a full half, or pass a human gameplay/look gate. Retained failing and passing #531 evidence lives in `scratchpad/validation/531-home-run/`.
