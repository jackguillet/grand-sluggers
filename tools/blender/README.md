# SharedRig blockout (Rio)

One armature, bone names from `data/art/rig.json`. Style lock: `tools/blender/style-lock/`.
Meshes are vertex-group skinned (Unity SkinnedMeshRenderer), not bone-parented.

```bash
/opt/homebrew/bin/blender --background --python tools/blender/hero_shared_blockout.py -- \
  --out unity/Assets/Art/Characters/SharedRig/hero-shared.fbx
```

Unity import: Generic rig (not Humanoid). Root at origin, faces −Z.
`Silhouette.ToyScale` (1.18) is applied in Play — do not scale the FBX again.
Captains stay extras on this chain. Do not add a second skeleton.
Missing FBX keeps `SharedRig` primitives.

Swing take (`Contact` at 0.30s, same keys as `data/art/pose-clips/swing.json`):

```bash
/opt/homebrew/bin/blender --background --python tools/blender/hero_shared_swing.py -- \
  --out unity/Assets/Art/Animation/Clips/swing.fbx
```

HeroActor samples the clip when present; missing file keeps authored eulers / MoveBones.
