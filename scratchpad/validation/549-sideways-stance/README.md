# Sideways batting stance (#549)

The negative baseline at `e99ab46` uses actual posed mesh landmarks: chest decoration relative to torso, eye midpoint relative to head, and the line between shoe/foot centers. Both shared Rio and Generic Fenn fail ready/load orientation checks even though the previous bat geometry checks pass. Original ready PNGs and all 16 baseline measurements are retained.

The requested stance has the chest toward the plate, feet along the mound/home direction, and eyes toward the pitcher. The capture permits 15 degrees of authored coil around those ready/load relationships. These measurements supplement visual inspection; human appearance acceptance remains open.
