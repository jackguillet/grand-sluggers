# Character packages (deferred)

Unique-anatomy captains as their own Generic FBX packages are **not** part of the game right now. Every captain, Fenn included, is the shared rig plus extras: [character-motion.md](character-motion.md).

The package path was removed in the character simplification of 2026-09 because it was a second pipeline (own mesh, controller, manifest, validator, bind capture, a second procedural motion set) for one captain whose look gate failed. Its last full state is in git history before that change (`git log -- data/art/character-packages.json`).

## If a unique captain comes back

Bring it back as a slot on the same rig, not a second rig:

- Bone **names** stay `data/art/rig.json`. Clips stay the shared takes, so the captain moves the day it lands.
- A unique mesh is a different `hero-shared.fbx`-shaped body bound by the same `SharedRig` code path: same bones, same landmarks (`torsoMesh`, `Stripe`, `headMesh`, `EyeL`, `EyeR`, `lHand`, `rHand`, `lShoe`, `rShoe`), painted weights authored in Blender.
- Do not drop a posed GLB, heat-weight it, freeze a SkinnedMeshRenderer, or write a per-captain procedural motion set.
- Look is Jack's gate: rest and posed stills in [screenshot-gate.md](screenshot-gate.md).

Until then, identity is proportions (`Silhouette`), palette, and extras (`data/art/extras.json`).
