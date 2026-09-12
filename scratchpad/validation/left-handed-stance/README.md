# Left-handed batters stand with their feet reversed

Reported by Jack: "the feet are backwards for left handed batters". Right-handed
batters are confirmed correct on screen, so they are the anchor.

## Contract

    R: bat above the RIGHT shoulder, feet face the plate, LEFT hand below right.
    L: bat above the LEFT shoulder,  feet face the plate, RIGHT hand below left.

`BattingStance` already authors this correctly. Its `FeetAxis` is `(0,0,1)` at
every key and `MirrorX` leaves Z alone, so both batters set their feet along the
pitch line pointing the same way. Handedness turns the chest and swaps the low
hand; it does not reverse the feet. `BattingStanceHandednessTests` pins that and
passes 7/7 -- the authored data is not the problem.

The rendered left-handed body disagrees.

## Why the matrix scored 56/56 anyway

`StillCapture` took `Mathf.Abs` of the feet dot, so a reversed stance was
indistinguishable from a correct one. The gate is now signed, and it also
compares the drawn hand stack against the authored key for that hand. Landmark
names are not anatomy -- DCC X is reflected on import -- so the hand check is
anchored to the authored key rather than to the name "left".

## Result on this revision (gate-catches-lefties.json)

| captain | bats | rows | signed feetAlongPitch |
| --- | --- | --- | --- |
| rio, vale, brondo, fenn | R | 8/8 | +1.00 |
| zig, konga, ashlord | L | 4/8 | -1.00 |

12 failures: 3 left-handed captains x 2 powers x ready and load. Hand stacking
passes for all seven, so the hands are right and the feet alone are reversed.

## Not done here

Fixing the rendered left-handed stance. The gate is the falsifier now; the fix
is a separate change and stays look-gated.

## Hands are wrong too, for every captain

Jack's reference photo: the bottom hand on the handle is the lead hand. Right-
handed hitter -> LEFT hand under the right. Left-handed -> RIGHT under the left.

`lHand` really is the batter's left hand. `hero_shared_blockout.py` places it at
Blender X -0.95 with the character facing +Y, and the FBX import X reflection
cancels against the `axis_forward="-Z"` export facing. An earlier reading of
this file concluded the names were reflected in Unity; that was wrong, and it is
what made the first hand check pass when it should not have.

The authored `SwingPresentation` LoadAt key has the two hands swapped:

    LeftHand  (0.300, 2.727, 0.512)
    RightHand (0.080, 2.522, 0.326)   <- right hand is the low hand

For a right-handed batter the left hand must be the low one. `Mirror` then
reproduces the same error flipped for left-handers, so the defect is on all
seven captains, not only the three lefties.

`LeadHandRidesUnderTheTopHand` fails for both hands on this revision.

## Two independent defects

| defect | who | authored data | caught by |
| --- | --- | --- | --- |
| feet reversed | left-handed only | correct | signed feet dot in the matrix |
| hands swapped | all seven | wrong | lead-hand check, Sim + matrix |
