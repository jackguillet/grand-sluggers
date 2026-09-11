# Research: batting geometry

The authored swing uses baseball measurements as constraints rather than as a
literal motion-capture trace.

## Batter side and plate coverage

MLB defines a box on each side of home plate and places left- and right-handed
batters on opposite sides ([MLB batter's-box glossary](https://www.mlb.com/glossary/rules/batters-box)).
Grand Sluggers therefore mirrors the default body X and the complete hitting
pose for handedness. The player's normalized box adjustment remains a world-X
offset for both hands. Camera side never changes either relationship.

At contact, the physical barrel is the model segment from Y -0.15 to Y +1.25,
with a model-space radius of 0.12 before the shared 1.28 bat scale. The gate
tests that segment against the 17-inch plate and the pitch-height band. It does
not require the bat tip to stop inside the plate: a valid swing can carry the
tip beyond the plate while the barrel still crosses it.

## Swing plane and grip

A validated finite-element batting study found that the efficient line of
impact rises about 5–10 degrees from horizontal ([Koizumi et al., *Optimal bat
orientation and ball-impact point*](https://www.jstage.jst.go.jp/article/jjpehss/advpub/0/advpub_17049/_article/-char/en)).
The shared approach-to-contact path rises 10.34 degrees. Contact remains at
0.30 seconds and follow-through at 0.50 seconds; load keeps the barrel above the
hands rather than sweeping it toward the plate early.

Grip research treats the hand-handle contact area as part of the motion, not a
nearby visual proxy ([Dowling et al., *Swing Type and Batting Grip Affect Peak
Pressures on the Hook of Hamate*](https://pmc.ncbi.nlm.nih.gov/articles/PMC8671670/)).
The runtime gate therefore bakes the posed hand meshes, measures their centers
and extents against the physical handle from Y -0.85 to Y -0.10, and records the
same points in shared-root space. Using root space makes the check invariant
across the six body proportions and exposes any socket, import-axis, or mesh
origin error.
