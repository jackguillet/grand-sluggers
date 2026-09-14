# Blender authoring

Everything a character is comes from three scripts. Contract: `docs/character-motion.md`. Stages: `data/agent/dcc-stages.json` (`cli stages`). One-shotting a captain extra or a kit mesh is a patch.

## Stages

Named checkpoints. Save after each (script + still). The next prompt names the stage it continues.

| # | Stage | Character | Harbor kit | Checkpoint |
| --- | --- | --- | --- | --- |
| 1 | blocking | `hero_shared_blockout.py` silhouette | diamond / wall ring volumes | `--clay` (`scratchpad/takes/body.png` / `harbor-kit.png`) |
| 2 | fill | extras from `hero_shared_extras.py` + `extras.json` | kit slots | `--clay` (`extras.png` / `harbor-kit.png`) |
| 3 | motion | takes from `hero_shared_takes.py` | — | `--sheets` (`{clip}.png`) |
| 4 | export | FBX into the catalog slot | FBX into the catalog slot | `--out` |
| 5 | still | DCC still + in-game character still | in-game park still | `tools/dcc-still.sh` + still-gate (R4) |

Existing bake flags (`--clay`, `--sheets`, `--out`) still run. A `.blend` is a cache, not the source.

## macOS startup

Use `tools/blender-run.sh` for background Blender commands. It checks Metal device access before starting Blender and exits safely when the caller cannot see a GPU. Agents must run with approved GPU access outside the sandbox; `-b` still initializes Metal. `--factory-startup` does not fix missing GPU access. Do not retry a restricted launch or terminate the user's existing Blender window. See [editor startup diagnostics](../../docs/editor-startup.md).

## Scripts

| Script | Makes | Check |
| --- | --- | --- |
| `hero_shared_blockout.py` | the one body, `hero-shared.fbx` | `--clay` renders a four-view sheet |
| `hero_shared_extras.py` | captain accessories + common props, `extras.fbx` | `--clay` renders one tile per extra on the body |
| `hero_shared_takes.py` | every take, both hands, `Clips/*.fbx` | refuses to export a take that misses its contract; `--sheets` renders a clay contact sheet per clip |

```bash
B=tools/blender-run.sh
$B -b --python tools/blender/hero_shared_blockout.py -- --out unity/Assets/Art/Characters/SharedRig/hero-shared.fbx --resources unity/Assets/Resources/Art/Characters/SharedRig --clay scratchpad/takes
$B -b --python tools/blender/hero_shared_extras.py -- --out unity/Assets/Art/Characters/SharedRig/extras.fbx --resources unity/Assets/Resources/Art/Characters/SharedRig --clay scratchpad/takes
$B -b --python tools/blender/hero_shared_takes.py -- --out unity/Assets/Art/Animation/Clips --resources unity/Assets/Resources/Art/Animation/Clips --sheets scratchpad/takes [--only swing,pitch]
```

`--resources` writes the standalone player copy from the same export, so the two slots in the catalog cannot drift; `cli art` fails if they do.

## Conventions (stated once, proven by the script)

The body is authored facing Blender −Y with its left hand at +X, so the FBX (`axis_forward="-Z"`, `axis_up="Y"`, X reflected on Unity import) lands facing Unity +Z with `lHand` at Unity −X. DCC → Unity is `(bx, by, bz) → (−bx, bz, −by)`; Unity → DCC is `batting_stance.unity_to_dcc`.

Poses in `hero_shared_takes.py` are written in body terms — `flex` forward, `abduct` outward, `twist`; `lean`, `turn` (left), `tilt`; `lift` — and converted to bone-local Eulers per side. `assert_conventions()` moves a hand and a foot on the built rig and fails the bake if any sign is wrong, so the table cannot drift from the rig.

The swing solves both hands to `SwingPresentation.Keys` (analytic two-bone IK, deterministic), aims the stance at `data/art/batting-stance.json`, and keys the `bat` socket from the grip and barrel direction. A left-handed take is the exact reflection of the right-handed one: every rendered landmark must land within a millimetre of its mirror, and the mirrored take is validated against the left-handed contract (lead foot, lead hand at the knob, chest and toes to the plate).

Look before Unity: the clay sheets are the author's own check, and `tools/dcc-still.sh` is the named DCC still in the PR. Jack's look gate is `docs/screenshot-gate.md`.

## Harbor kit

`harbor_kit.py` authors the sunken dugout, wall panel, crowd, home plate, and bag. Missing file keeps HarborKit primitives. Walk blocking → fill → export → still (Harbor skips motion). One-shotting a kit mesh is a patch.

```bash
$B -b --python tools/blender/harbor_kit.py -- --out unity/Assets/Art/Parks/harbor-diamond/harbor-kit.fbx --clay scratchpad/takes
```

Named PR stills (dual stills, #651): `tools/dcc-still.sh body|extras|takes [clip]|harbor` copies the clay sheet to `scratchpad/stills/dcc-*.png`. Pair with `tools/still-gate-character.sh` / `tools/still-gate.sh`. A look-critic files; Jack passes.
