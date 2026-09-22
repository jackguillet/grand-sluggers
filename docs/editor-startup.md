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

The delivery script also previously sent SIGTERM after every build. It now opts its owned editor into `GS_BUILD_QUIT_WHEN_DONE`; PlayerBuildGate writes durable `GS_BUILD_EVIDENCE` outside Temp. After a successful build, it calls `EditorApplication.Exit` in the same update. Manual menu builds and failed builds remain open. A stalled editor is left for inspection instead of being terminated.

Do not queue the exit on `EditorApplication.delayCall`. On 2026-09-18 that exit never ran: the background editor wrote good evidence, then idled in `NSApplication run` for minutes, and `build.log` stopped after `TrimDiskCacheJob` with no shutdown. A probe reproduced it four times out of four with the editor hidden at the end of the build; a visible editor exited. `tell application "Unity" to activate` does not wake it. A normal quit Apple Event does, so the supervisor sends one to the exact PID it launched when the editor is still open 20 seconds after good evidence. That quit took 2 to 66 seconds in the probe.

## Unity: one GUI editor user at a time

Several sessions share Jack's one Mac and its GUI Unity. Personal Unity cannot `-batchmode`, and Play advances only in the frontmost editor. On 2026-09-22 a still-capture loop fronted its editor every 10 seconds. It took focus from Jack's delivered game window and from another session's editor. A bare editor with no project sat idle for 14 hours. It was most likely started by `tell application "Unity" to activate`, which starts a fresh, empty Unity when none is running. With two editors running, that command can front either one. Also, one session's delivery closed a window that another session had delivered for Jack.

Rules (protocol row `gui-editor-focus-fight`):

1. Take the GUI Unity lock before you launch, front or click an editor, and before you deliver a player. `tools/still-gate.sh`, `tools/still-gate-character.sh`, `tools/build-player.sh` and `tools/local-player.py` take it for you and release it when they exit. The lock is `~/Library/Application Support/Grand Sluggers/unity-gui.lock`. It records the holder's PID, process start time, worktree, purpose, command and start time. While a live holder has it, the next session is refused (exit 75), and the message names the holder. If the holder is gone (its PID is dead, a zombie, or reused by a new process), the next acquire clears the lock and says so.
2. Front, click or quit an editor by its PID, through `tools/unity_gui.py front`, `menu` or `quit`. Only the lock holder can do this. Never `tell application "Unity" to activate`, `tell process "Unity"` or `open -a Unity`. Never `pkill Unity`, which closes every session's editor, and do not SIGTERM an editor (see Recovering Scene Backups above).
3. A capture that fronts an editor refuses while a delivered game window is open. Ask Jack whether the machine is free, then pass `--player-open-ok`. Delivery closes an open window only with `--replace` ([local-player.md](local-player.md#sharing-the-mac)).
4. `python3 tools/unity_gui.py status` shows the lock holder, every GUI editor with its project, and every delivered window. A bare editor shows no project. If no session owns it, quit it normally.

A hand-rolled capture that launches its own editor with `-executeMethod GrandSluggers.EditorTools.StillGateMenu.CaptureRequestFile` follows the same rules in its own shell:

```sh
gui() { python3 tools/unity_gui.py "$@"; }
gui acquire --pid $$ --worktree "$PWD" --purpose "swing matrix (#NNN)" --focus || exit $?
trap 'gui release --pid $$' EXIT
GS_STILL_REQUEST_FILE="$request" "$editor" -projectPath "$PWD/unity" \
  -executeMethod GrandSluggers.EditorTools.StillGateMenu.CaptureRequestFile -logFile "$log" &
unity=$!
# A front that fails while the editor is still starting is retried on the next pass.
until [[ -f "$out/gs-still-done.json" ]] || ! kill -0 $unity 2>/dev/null; do
  gui front --owner $$ $unity
  sleep 10
done
gui quit --owner $$ $unity
wait $unity
```
