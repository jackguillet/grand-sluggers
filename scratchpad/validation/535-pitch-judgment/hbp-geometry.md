# HBP geometry evidence

The prior normalized body center was ±0.75 with radius 0.32, so its inner edge (0.43) overlapped the visible zone edge (0.92 / 1.85 = 0.497). A taken rendered strike at normalized X ±0.45 therefore became `HitByPitch`.

The body center now comes from the authored batter-box center (`HomeSet.BoxX` = 3.208 ft) plus the actor's world walk (`HomeSet.BatterWalk` = 2.4 ft per offset), then converts through `PitchFlight.PlateScaleX` = 1.85. At rest the center is ±1.734 normalized and its inner edge is outside the zone. Tests cover both hands, a half-range walk, a pitch 0.05 ft outside the rendered zone, and a taken pitch centered on the actual body.

A deterministic headless sample of 100 three-inning `Match.Slice` games (seeds 0–99) changed from 316 HBP / 3,115 PA (10.1%) on integrated `8a3d0b2` to 2 HBP / 2,926 PA (0.1%) with this correction. The same post-fix sample produced 112 walks (3.8%) and 285 strikeouts (9.7%). This is simulation evidence; no Unity or player look gate was run.

Verification: `dotnet test src/GrandSluggers.Sim.Tests/GrandSluggers.Sim.Tests.csproj --no-restore` — 526 passed, 0 failed.
