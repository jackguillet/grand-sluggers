# Batting stance: the side opposite the batting hand leads

Reported by Jack: "the feet are backwards for left handed batters". Measured in
Blender, the authored take had every captain's lower body backwards; the
left-handed body is an exact mirror of it, and Ashlord's block shoes are where
the toe direction reads. `HANDOFF.md` has the numbers, the fix, and the gates.

## Contract

    R: bat above the RIGHT shoulder, feet face the plate, LEFT hand below right.
    L: bat above the LEFT shoulder,  feet face the plate, RIGHT hand below left.

Stated once: the lead side is opposite the batting hand. Lead foot toward the
pitcher, lead hand at the knob end under the top hand. `BattingStance.LeadSide`.

## Two defects, both on all seven captains

| defect | root cause | fix | caught by |
| --- | --- | --- | --- |
| hips out of the box, toes away from the plate, right foot forward | `feet_axis` was right-minus-left; the catalog's feetAxis points at the pitcher, so the root yawed the right foot forward and the torso twisted to keep the chest on the plate | feetAxis means back foot to lead foot; DCC aims the root at it | signed lead-foot dot in Sim, DCC and the matrix; DCC toe check |
| hands swapped on every key | keys authored with the right hand at the knob | swap LeftHand/RightHand on all five keys, swap the DCC targets, re-bake | `HandAlongHandle` along the handle in Sim and DCC; drawn hand stack in the matrix |

## Why earlier runs missed it

- The matrix took `Mathf.Abs` of the feet dot, and the DCC validator did the
  same, so hips turned out of the box scored as correct (56/56).
- The feet line was right-minus-left, which is only the lead foot for a
  left-handed batter.
- Hand order was read by height. Once the barrel comes level at approach,
  height cannot say which hand is at the knob; along the handle the right hand
  was at the knob on every key.

## Files here

- `1291c0a-rio-ashlord.json`, `gate-catches-lefties.json` — matrix runs on the
  gate-only branch (before the fix)
- `swing-rio-normal-ready.png`, `swing-ashlord-normal-ready.png` — before stills
- `fix-lead-side.json` — matrix run on the fix branch, 56/56 with the signed
  lead-foot line and the lead-hand stack live
- `after-swing-{rio,vale,ashlord}-normal-ready.png` — after stills (vale has the
  blockout's cube shoes, so the toe direction reads on a right-hander too)
- `look-gate-before-after.png` — the assembled comparison for Jack's look gate
- `HANDOFF.md` — the full write-up and what the next session should do
