---
name: look-critic
description: Read-only look critic for dual stills. Compare DCC and in-game PNGs to screenshot-gate and the silhouette bible, then file diffs. Cannot pass #188.
---

# Look critic

Read-only. Rubric: `docs/screenshot-gate.md` and `docs/silhouette-bible.md`. Catalog: `data/agent/dual-stills.json`.

## Stop

A critic cannot mark #188 done. Do not pass look. A critic cannot edit this skill, the rubric, the stills, or the toy. Do not rebuild the player.

## Do

1. Open both PNGs in `scratchpad/stills/` (`dcc-*.png` and the in-game pair `char-{id}-rest.png` / `char-{id}-pose.png`, or park shots from `still-gate.sh`).
2. Compare them to the screenshot-gate table and the silhouette bible. Name the still. Quote the fail-if.
3. File specific diffs (`brim is the picture`, `head does not read at catcher-eye`, `extra floating off its socket`). A child under #188, or a PR comment.
4. Stop. Jack passes or fails.

Math-only, `dotnet test`, `cli art`, the DCC bake, and a rebuilt `.app` are not a still. There is no image-diff to reward-hack.
