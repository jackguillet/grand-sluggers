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
