# Blender authoring

Everything a character is comes from three scripts. Contract: `docs/character-motion.md`.

| Script | Makes | Check |
| --- | --- | --- |
| `hero_shared_blockout.py` | the one body, `hero-shared.fbx` | `--clay` renders a four-view sheet |
| `hero_shared_extras.py` | captain accessories + common props, `extras.fbx` | `--clay` renders one tile per extra on the body |
| `hero_shared_takes.py` | every take, both hands, `Clips/*.fbx` | refuses to export a take that misses its contract; `--sheets` renders a clay contact sheet per clip |

```bash
B=/opt/homebrew/bin/blender
$B -b --python tools/blender/hero_shared_blockout.py -- --out unity/Assets/Art/Characters/SharedRig/hero-shared.fbx --resources unity/Assets/Resources/Art/Characters/SharedRig --clay scratchpad/takes
$B -b --python tools/blender/hero_shared_extras.py -- --out unity/Assets/Art/Characters/SharedRig/extras.fbx --resources unity/Assets/Resources/Art/Characters/SharedRig --clay scratchpad/takes
$B -b --python tools/blender/hero_shared_takes.py -- --out unity/Assets/Art/Animation/Clips --resources unity/Assets/Resources/Art/Animation/Clips --sheets scratchpad/takes [--only swing,pitch]
```

`--resources` writes the standalone player copy from the same export, so the two slots in the catalog cannot drift; `cli art` fails if they do.

## Conventions (stated once, proven by the script)

The body is authored facing Blender −Y with its left hand at +X, so the FBX (`axis_forward="-Z"`, `axis_up="Y"`, X reflected on Unity import) lands facing Unity +Z with `lHand` at Unity −X. DCC → Unity is `(bx, by, bz) → (−bx, bz, −by)`; Unity → DCC is `batting_stance.unity_to_dcc`.

Poses in `hero_shared_takes.py` are written in body terms — `flex` forward, `abduct` outward, `twist`; `lean`, `turn` (left), `tilt`; `lift` — and converted to bone-local Eulers per side. `assert_conventions()` moves a hand and a foot on the built rig and fails the bake if any sign is wrong, so the table cannot drift from the rig.

The swing solves both hands to `SwingPresentation.Keys` (analytic two-bone IK, deterministic), aims the stance at `data/art/batting-stance.json`, and keys the `bat` socket from the grip and barrel direction. A left-handed take is the exact reflection of the right-handed one: every rendered landmark must land within a millimetre of its mirror, and the mirrored take is validated against the left-handed contract (lead foot, lead hand at the knob, chest and toes to the plate).

Look before Unity: the clay sheets are the author's own check. Jack's look gate is `docs/screenshot-gate.md`.

## Harbor kit

`harbor_kit.py` authors the sunken dugout, wall panel, crowd, home plate, and bag. Missing file keeps HarborKit primitives.

```bash
$B -b --python tools/blender/harbor_kit.py -- --out unity/Assets/Art/Parks/harbor-diamond/harbor-kit.fbx
```
