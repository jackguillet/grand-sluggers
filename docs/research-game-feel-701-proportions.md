# Character and ball proportions — sizing audit

[#701](https://github.com/jackguillet/grand-sluggers/issues/701), parent [#693](https://github.com/jackguillet/grand-sluggers/issues/693). September 14, 2026. Gameplay research/documentation only. The [measurement dataset](research/game-feel-701-proportions.json) records source hashes, pixel brackets, exclusions, baseline calculations, and the accepted ball-readability direction. Companion to the [geometry packet](research-game-feel-701-geometry.md). No model, camera, physics, or runtime value changes.

## What the new visual inspection supports

In the [Wii recording at 01:00](https://www.youtube.com/watch?v=3-5aQh-fxZk&t=60s), Boo is the controlled shortstop approaching the ball; Mario is nearer the camera on the mound. The settled browser screenshot is 1512 × 949, with active gameplay approximately 1512 × 851 after removing the horizontal bars. Decoded video was 1280 × 720.

Manual brackets put Boo's vertical silhouette extent at **7.0–8.6%** of active image height and Mario's at **13.3–16.0%**. The dataset preserves the top/bottom pixel brackets; these are conservative observation bounds, not statistical confidence intervals. Exclude shadows, HUD markers, and the held equipment's horizontal reach. These numbers describe this pose, camera, and screen position. They are neither world heights nor a target range for every character in every shot. Comparing the nearer Mario with Boo does not establish a difference in their world stature or prove field compression.

The [GameCube recording around 03:11](https://www.youtube.com/watch?v=fdtUfRUjyAY&t=191s), inspected at 191.125513 seconds, showed Luigi in the foreground and Wario on the mound during contact. Adaptive delivery was only 256 × 144, and the camera phase differs from the Wii fielding sample. Exclude it from numerical proportion calibration. The additional 196.234431-second sample was a black transition. Neither supports a Wii/GameCube world-scale ranking. The previously annotated two-play comparison remains available; this pass does not replace its coverage requirements.

The Wii introduction at 00:30 still crops the near diamond and home plate. It does not supply four bag centers for the planned ground-plane reconstruction. Ball core, glow, and trail are not cleanly separable in the 01:00 sample, so no ball diameter is claimed. No remote media was downloaded; source locators and annotations are the retained evidence.

## Harbor's nominal character proportions

At research revision `63abc97`, the shared rig's rest head center is **4.36** units high and its head radius **0.45**, placing the nominal head-top marker at **4.81**. `Silhouette.SharedRootScale` multiplies height by the character's stature and the shared **1.18** toy scale. `SharedRig.Spawn` applies that root scale. See [rig data](../data/art/rig.json), [Silhouette.cs](../src/GrandSluggers.Sim/Silhouette.cs), and [SharedRig.cs](../unity/Assets/Scripts/Runtime/SharedRig.cs).

The resulting nominal rest head-top markers are **5.11 ft for Rio**, **3.18 ft for Zig**, and **8.17 ft for Ashlord**. Relative to the audited 90-foot basepath, these are about **5.68%, 3.53%, and 9.08%**. All seven characters are retained in the dataset. These are authored marker calculations, not live mesh bounds: extras, pose, import state, effects, and rendered framing must be checked separately. The root multipliers are not themselves feet or final body heights.

This establishes a reproducible Harbor ratio to compare with future reference measurements. It does not select shorter basepaths or enlarge a character. Scaling field, toys, ball, and camera uniformly leaves these proportions unchanged. Changing character/basepath or fence/basepath relationships is the relevant design operation; camera framing separately determines their projected sizes.

## Possession-scale correction and the baseline

[#691](https://github.com/jackguillet/grand-sluggers/issues/691) is closed through merged [PR #700](https://github.com/jackguillet/grand-sluggers/pull/700), revision `08eab5bba940f8ec5c5f020afac772ed16b8a367` (September 14, 2026). It concerns characters changing apparent stature on possession/highlight, not baseball diameter. The research branch's historical baseline predates that fix. Any new live visual baseline must identify a build containing the correction and record ordinary rest/possession alongside abilities separately. Closure is not a substitute for inspecting the exact build used in #693.

## Ball size is already a separate presentation choice

[Baseball.cs](../src/GrandSluggers.Sim/Baseball.cs), [ToyMesh.cs](../src/GrandSluggers.Sim/ToyMesh.cs), and [BallView.cs](../unity/Assets/Scripts/Runtime/BallView.cs) describe a drawn ordinary ball diameter of **0.62 ft held**, up to **1.42 ft for a far pitch**, and **1.12 ft in ordinary in-play flight**. The pitch helper interpolates from 0.62 to 1.42 using `clamp((z − 8) / 40, 0, 1)`; it uses world Z, not measured camera depth. Special effects and transform parenting need their own inspection. These are baseline render values, not selected target diameters or catch radii.

## Accepted direction — F693-02 ball readability

**Accepted by Jack, September 14, 2026, with the explicit qualification “make it moderate”:** allow moderate, bounded visual size assistance for a distant moving ball, alongside contrast and readable trails. Keep a believable glove-fitting size when held and a smooth visual handoff into possession. The goal is to follow the ball across the compact field without requiring an oversized held prop. This is an accepted Harbor presentation direction; neither inspected Mario recording establishes its distance-scaling algorithm. Moderation is a constraint: enlargement should support tracking without drawing attention as a growth effect, dominating the character, or creating an obvious size jump into the glove. Numeric limits still require comparison and playtesting; the current baseline diameters are not accepted merely because the policy is approved.

**Alternative considered, not selected:** use one fixed drawn world-space diameter, relying on framing, contrast, and trails as the ball becomes smaller with distance. This gives more literal size consistency, but a distant ball can become harder to follow. The choice is about readability and appearance, not the ball's physical trajectory.

Either policy must leave ball center, travel time, strike/throw/catch judgments, and live geometry authoritative in the sim. Visual scaling must not enlarge catch coverage or change a result. Keep characters at their correct stature through possession; preserve #691's correction. Do not choose a size merely to rescue one camera, one character, or a HUD-off still.

With the direction accepted, a separate presentation task will define the bounded size policy through the existing visual/data owners after the approved geometry and camera references are available. Test short/long throws, pitches, liners, high flies, hops, wall retrieval, and held-ball transitions, for small and large captains and both seats. Compare HUD-on and diagnostic HUD-off footage. Numerical pixel/diameter bounds, smoothing, world/body ratios, and human visual acceptance remain open. No ball-size value or game dimension is selected by this packet.

## Validation

Recompute nominal head-top markers and their basepath ratios from the retained rig/root inputs; recompute projected extents from the raw pixel brackets and active image height. Validate source hashes, local links, exclusions, accepted direction status, and empty accepted numerical targets. No runtime tests, standalone build, or look gate are claimed for this documentation change.
