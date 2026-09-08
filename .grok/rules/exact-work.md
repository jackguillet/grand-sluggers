---
description: Research the spec, match it exactly, and verify by looking. Similar is a fail.
alwaysApply: true
---

# Exact work

When Jack names a thing — a batter's box, a dirt shape, a camera, a HUD — **exact** is the bar. Similar is a fail. "Close" / "better" from Jack is a correction, not acceptance.

## Before you code

1. **Research the real spec.** MLB dimensions, the reference image, existing tables (`HomeSet`, `ParkDiamond`, `data/feel/`). Write the numbers down. Do not invent a rhyming shape.
2. **Name the relationships** the still must show (bags *inside* the foul line, 1B–2B–3B apron *thicker and more curved* than the home legs, 6-inch box gap). Those become tests.

## While you work

- Tests encode the spec, not "a mesh exists." `BagIsInsideTheFoulLine`, `BoxesClearThePlate` with the real gap, `BackApronIsCurved`.
- A uniform diamond when the reference is an asymmetric infield is a miss, not a first pass.
- Cartoon fat is allowed. Wrong topology is not (point toward the pitcher, bags on the chalk).

## Before you say done

- **View the change.** Play `HarborDiamond`, Scene-view orbit, still, or live mesh bounds vs the reference. Math-only is not verification.
- Personal Unity cannot `-batchmode`. If you cannot look, say so. Do not claim the look is done.
- Human look gates stay human. Your job is to not hand Jack a cousin of what he asked for.

Fail: Minkowski-offset square when the photo is a curved 1B–2B–3B apron. Fail: boxes 1.4 ft from the plate when the spec is 6 in.
