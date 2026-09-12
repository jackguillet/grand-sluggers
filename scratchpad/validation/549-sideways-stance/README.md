# Sideways batting stance (#549)

The negative baseline at `e99ab46` uses actual posed mesh landmarks: chest decoration relative to torso, eye midpoint relative to head, and the line between shoe/foot centers. Both shared Rio and Generic Fenn fail ready/load orientation checks even though the previous bat geometry checks pass. Original ready PNGs and all 16 baseline measurements are retained.

The requested stance has the chest toward the plate, feet along the mound/home direction, and eyes toward the pitcher. The capture permits 15 degrees of authored coil around those ready/load relationships. These measurements supplement visual inspection; human appearance acceptance remains open.

## After the import-basis fix

`dcc-frames-after.json` scores **every baked frame** of the shipped
`swing.fbx`, read back off disk, against the runtime contract and against the
still gate's own thresholds. `score-shipped-take.py` is the script that wrote
it; it runs on any Blender with the `bpy` module and takes no arguments.

Three separate failures showed up in the same gate run and had two causes.

1. *Eyes render toward `-Z`.* `SharedRig.TryBindDrop` hides the shared
   blockout's authored eyes and rebuilds the face it draws on the head bone's
   Unity `+Z`, which is the reverse of the landmark the DCC scene measures.
   Aiming the hidden landmark at the pitcher pointed the drawn face away from
   it. `batting_stance` now takes an `eyes_basis`; the shared take passes
   `EYES_REVERSED_BY_IMPORT` and a Generic package keeps `EYES_AS_AUTHORED`.
   The pre-stance baseline in `e99ab46-before.json` confirms the reversal:
   its rio ready `chestForward` and `eyeForward` sit 173 degrees apart.
2. *Ready/load hands leave the handle, and Zig's loaded barrel sits too low.*
   Both are the same defect. The take was authored on five keys while the gate
   holds a batter at any charge and samples in between; a sideways torso moved
   the shoulders far enough that the between-key poses left the authored hand
   and socket path. The take is now authored and falsified on every frame from
   the interpolated contract. At the ready sample the left hand went from
   0.383 off the handle (tolerance 0.329) to 0.133, and Zig's loaded rise went
   from 0.557 to 0.910 against the gate's 0.70.

Not covered here: the Unity still gate itself, which needs the editor, and
human appearance acceptance, which stays a separate gate.
