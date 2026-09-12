# Lead side is on the wrong hand and foot (both stances)

Reported as "the feet are backwards for left handed batters". Measuring against
the stated contract, the defect is not left-handed-only: every captain carries
the lead hand and lead foot on the batting side instead of opposite it.

Contract:
  R: bat above the RIGHT shoulder, feet face the plate, LEFT hand below right.
  L: bat above the LEFT shoulder,  feet face the plate, RIGHT hand below left.

From the merged 40e8b5c matrix run, ready beat, normal power:

| captain | bats | lower hand | contract | foot toward pitcher | contract |
| --- | --- | --- | --- | --- | --- |
| rio, vale, brondo, fenn | R | right | left | right | left |
| zig, konga, ashlord | L | left | right | left | right |

Source is the authored right-handed take in `SwingPresentation.Keys`. Its
LoadAt key puts the right hand lower than the left:

    LeftHand  (0.300, 2.727, 0.512)
    RightHand (0.080, 2.522, 0.326)

`SwingPresentation.Mirror` then reproduces that faithfully for left-handed
batters, so both stances are wrong in the same way and lefties read as the
mirror of an already-wrong pose.

Why the swing matrix passes 56/56 anyway:

- `StillCapture` takes `Mathf.Abs` of the feet dot, so the front foot has no
  sign and a reversed stance scores identically.
- Nothing compares the two hand heights.
- Nothing checks which shoulder the bat sits above.

`src/GrandSluggers.Sim.Tests/BattingStanceHandednessTests.cs` encodes the
contract and fails 4 of 6 on this revision. The hand-gap check passes, which is
how we know the mirror is faithful and the reference take is the problem.

`swing-ashlord-normal-ready.png` beside `swing-rio-normal-ready.png`: ashlord's
bat hangs low toward the dirt rather than above the left shoulder.

Not done here: re-authoring the shared take. That is Blender work on
`hero_shared_swing.py` plus a `swing.fbx` re-bake, it moves every captain, and
it is look-gated.
