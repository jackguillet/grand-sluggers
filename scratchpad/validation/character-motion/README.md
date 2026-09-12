# Character simplification — evidence

Branch `claude/character-simplify-motions-589867`. Contract: `docs/character-motion.md`.

| File | What |
| --- | --- |
| `look-gate-before-after.png` | Jack's look gate, assembled: main vs this branch, both hands, pitch, plate, rest |
| `matrix/gs-still-done.json` | Unity swing matrix, seven captains × normal/MAX × ready/load/contact/follow: **56/56 pass** |
| `matrix/*.png` | ready and contact frames for rio (R), zig (L), ashlord (L), fenn (R, shared rig) |
| `stills/*.png` | still-gate captures: plate, pitch, mound, grounder, char-rest, char-pose for rio, fenn, ashlord |
| `takes/*.png` | clay contact sheets straight out of Blender for idle, run, jump, pitch, throw, scoop, swing (right row, mirrored row) |

## Reproduce

```sh
B=/opt/homebrew/bin/blender
$B -b --python tools/blender/hero_shared_blockout.py -- --out unity/Assets/Art/Characters/SharedRig/hero-shared.fbx --resources unity/Assets/Resources/Art/Characters/SharedRig
$B -b --python tools/blender/hero_shared_extras.py -- --out unity/Assets/Art/Characters/SharedRig/extras.fbx --resources unity/Assets/Resources/Art/Characters/SharedRig
$B -b --python tools/blender/hero_shared_takes.py -- --out unity/Assets/Art/Animation/Clips --resources unity/Assets/Resources/Art/Animation/Clips --sheets scratchpad/takes
dotnet test src/GrandSluggers.Sim.Tests/GrandSluggers.Sim.Tests.csproj
dotnet run --project src/GrandSluggers.Cli -- art
tools/unity-compile.sh
```

Swing matrix (GUI editor, no batch mode):

```sh
GS_STILL_REQUEST_FILE=/abs/swing-request.json \
  /Applications/Unity/Hub/Editor/6000.5.9f1/Unity.app/Contents/MacOS/Unity \
  -projectPath /abs/worktree/unity -executeMethod GrandSluggers.EditorTools.StillGateMenu.CaptureRequestFile \
  -logFile /abs/editor.log &
```

with `{"shots":["swing-matrix"],"swingCaptains":["rio","vale","brondo","fenn","zig","konga","ashlord"],"hudOff":true,"width":1920,"height":1080}`. Results: `unity/Temp/gs-still-done.json`, PNGs in `unity/Temp/gs-stills/`.

## Editor gotchas found on this branch

- `runInBackground` is 0 in Player Settings, so a Play-mode capture only advances while the editor is the frontmost app: `osascript -e 'tell application "Unity" to activate'` after launch.
- Killing the editor mid-Play leaves `unity/Temp/__Backupscenes`; the next launch blocks on a "Recovering Scene Backups" dialog before the log gets past licensing. Delete that folder before relaunching (or click No).
- Exactly one editor instance. `pkill -x Unity` between runs; Unity clears `unity/Temp` on a clean quit, so copy evidence out first.

## What the Unity run proved, and what it did not

The matrix proves the takes reach the bones through the Playables path, the stance and grip meet the contract in the rendered body for both hands, and every captain passes the same thresholds. It does not pass look: that is Jack's, from the sheet above.
