# SharedRig blockout (Rio)

One armature, bone names from `data/art/rig.json`. Style lock: `tools/blender/style-lock/`.
Meshes are vertex-group skinned (Unity SkinnedMeshRenderer), not bone-parented.

```bash
/opt/homebrew/bin/blender --background --python tools/blender/hero_shared_blockout.py -- \
  --out unity/Assets/Art/Characters/SharedRig/hero-shared.fbx
```

Unity import: Generic rig (not Humanoid). Root at origin, faces −Z.
`Silhouette.ToyScale` (1.18) is applied in Play — do not scale the FBX again.
Rio six stay extras on this chain. Unique captains are packages (`docs/character-package.md`).
Missing FBX keeps `SharedRig` primitives.

Swing take (`Contact` at 0.30s, same keys as `data/art/pose-clips/swing.json`):

```bash
/opt/homebrew/bin/blender --background --python tools/blender/hero_shared_swing.py -- \
  --out unity/Assets/Art/Animation/Clips/swing.fbx
```

HeroActor samples the clip when present; missing file keeps authored eulers / MoveBones.

Captain extras kit (one file, names match `data/art/skins.json`):

```bash
/opt/homebrew/bin/blender --background --python tools/blender/hero_shared_extras.py -- \
  --out unity/Assets/Art/Characters/SharedRig/extras.fbx
```

Pitch take (`Release` at 0.42s, throw toward home, same keys as `data/art/pose-clips/pitch.json`):

```bash
/opt/homebrew/bin/blender --background --python tools/blender/hero_shared_pitch.py -- \
  --out unity/Assets/Art/Animation/Clips/pitch.fbx
```

Scoop take (`Contact` at 0.22s, glove on the dirt, same keys as `data/art/pose-clips/scoop.json`):

```bash
/opt/homebrew/bin/blender --background --python tools/blender/hero_shared_scoop.py -- \
  --out unity/Assets/Art/Animation/Clips/scoop.fbx
```

Unique character package. Spec: `docs/character-package.md`. A posed unrigged GLB is a **source**, not a player mesh. Quality path is a Generic FBX with painted weights (or Blender-authored pieces) and clips on that armature. `drop_character.py` is a converter, not the ship pipeline.

```bash
/opt/homebrew/bin/blender --background --python tools/blender/drop_character.py -- \
  --src /path/to/hero.glb --id fenn --bind skinned --keep-weights \
  --out unity/Assets/Art/Characters/fenn/fenn.fbx \
  --resources unity/Assets/Resources/Art/Characters/fenn/fenn.fbx \
  --portrait unity/Assets/Resources/Art/fenn-hero.jpg
```

Writes `{id}-albedo.png` (1024) next to the FBX. `--bind skinned` is painted groups (`--keep-weights`) or hard 1.0 groups on fitted bones. Default `--bind rigid`. `--bind segmented` is Blender-authored pieces only — a Python split shredded Fenn. `--clay-dir` rest + limb extremes; look at those before Unity. Character stills (`docs/screenshot-gate.md`) before a player rebuild.

Generic idle + pose takes on an existing package (no remesh):

```bash
/opt/homebrew/bin/blender --background --python tools/blender/package_clips.py -- \
  --src unity/Assets/Art/Characters/fenn/fenn.fbx --id fenn \
  --out-dir unity/Assets/Art/Characters/fenn \
  --resources unity/Assets/Resources/Art/Characters/fenn
```

Elder Fenn (wrapper):

```bash
/opt/homebrew/bin/blender --background --python tools/blender/hero_fenn.py -- \
  --src /path/to/elder-game.glb \
  --out unity/Assets/Art/Characters/fenn/fenn.fbx \
  --portrait unity/Assets/Resources/Art/fenn-hero.jpg
```

Harbor kit (dugout, wall panel, crowd). Missing file keeps HarborKit primitives:

```bash
/opt/homebrew/bin/blender --background --python tools/blender/harbor_kit.py -- \
  --out unity/Assets/Art/Parks/harbor-diamond/harbor-kit.fbx
```
