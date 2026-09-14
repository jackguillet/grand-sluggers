# Editor startup and shutdown

## Blender: no Metal device in a restricted process

The 2026-09-13 crash at 20:53:13 was a background Blender 5.2.0 process launched by Codex. It failed before any Python frame, in `supports_barycentric_whitelist` → `metal_is_supported` → GPU startup. Its null-address fault reached `strstr` while inspecting a GPU name.

A small native Metal probe on the same Mac returned no default device and zero devices inside the agent sandbox. Outside it, the probe returned Apple M1 Pro and one device. The original swing bake completed outside the sandbox. This strongly identifies GPU access during startup as the cause of that crash, rather than the character script or mesh.

There are 13 retained Blender crash reports from September 10–13, all with Codex as parent. Three show this Metal startup stack. The other ten lack enough useful frames to assign the same cause. The user's existing interactive Blender process was still running, started roughly six days earlier; these reports name separate background processes.

Use `tools/blender-run.sh` for background commands. On macOS it checks Metal access without launching Blender and exits 78 if no device is available. `tools/dcc-still.sh` uses it too. `BLENDER` can override the executable. Agents should request approved GPU access for the intended command rather than first launching Blender in the restricted sandbox. Neither background mode nor factory preferences supplies GPU permissions. Do not change global sandbox settings or kill the user's Blender window.

Check with `tools/blender-run.sh --check`; with approved GPU access, a disposable startup can be tested using `tools/blender-run.sh -b --factory-startup --python-expr 'print("GS_BLENDER_STARTUP_OK")'`.

Upstream source: [Blender Metal backend](https://github.com/blender/blender/blob/main/source/blender/gpu/metal/mtl_backend.mm), `supports_barycentric_whitelist`. This is a local launch guard, not a patch to the installed Blender binary.

## Unity: generated .NET files imported as game code

`src/GrandSluggers.Sim` is a Unity local package and a .NET project. Building both Debug and Release into its default `bin/` and `obj/` directories lets Unity import generated assembly metadata and DLLs. The observed Safe Mode errors were CS0579 duplicate assembly attributes under `obj/Release/net10.0/GrandSluggers.Sim.AssemblyInfo.cs`; the log also reported conflicts between source types and an imported Sim DLL.

`Directory.Build.props` now places .NET outputs under repository-root `.artifacts/`, outside Unity's Assets and local package trees, for every configuration and project. This uses the [.NET SDK artifacts layout](https://learn.microsoft.com/en-us/dotnet/core/sdk/artifacts-output). Git ignore rules do not control Unity imports.

Existing checkouts may retain old outputs. With .NET builds stopped, move only the generated `src/GrandSluggers.Sim/bin` and `obj` directories to a backup location outside the package before reopening or refreshing Unity. Preserve source files and scene backups. During the investigation those directories were moved to `/tmp/gs-pr628-generated-backup`; Unity then left Safe Mode and passed all 17 combined input/motion checks on PR 628.

## Unity: Recovering Scene Backups

Process termination can leave `Temp/__Backupscenes` behind. Do not routinely stop a validation editor using SIGTERM, SIGKILL or process-name-wide kill commands. Stop Play and quit that exact editor normally, or use an owned editor callback calling `EditorApplication.Exit` after validation completes. Confirm normal shutdown in the log before reuse.

If recovery appears, preserve the backup through the dialog before continuing. The observed backup was retained as `unity/Assets/_Recovery/0.unity` in the PR 628 worktree. Do not delete recovery data to suppress the dialog or overwrite the authored Harbor scene with a captured test scene.

The earlier validation editor was stopped with SIGTERM; this is a likely contributor to that worktree's recovery prompt. A subsequent normal Quit completed cleanup and package-manager shutdown. A recovery prompt by itself does not establish a Unity crash.

The delivery script also previously sent SIGTERM after every build. It now opts its owned editor into `GS_BUILD_QUIT_WHEN_DONE`; PlayerBuildGate writes durable `GS_BUILD_EVIDENCE` outside Temp and calls `EditorApplication.Exit` after reporting success or failure. Manual menu builds remain open. A stalled editor is left for inspection instead of being terminated.
