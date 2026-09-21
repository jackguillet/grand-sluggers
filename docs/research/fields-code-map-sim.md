# Fields code map — sim, CLI, tests, tools and data

Read-only map made on September 21, 2026 for [research-fields.md](../research-fields.md). Inspected revision: `d0c6e12c` (origin/main). Paths are relative to the repo root; `Sim/` = `src/GrandSluggers.Sim/`, `Tests/` = `src/GrandSluggers.Sim.Tests/`. This file records the code as it was. It proposes no fix. Line numbers go stale; re-check them before you cite them in an issue.


## State in one breath

A park is one record with 12 fields (`Sim/Models.cs:186-198`). Seven of them change play: the three fence posts, `fenceHeightFt`, `windMph`, `windDeg`, `nightContactWindowMul`, plus `hazards`. `surface`, `faction`, `name` change no play. `notes`, `nightOnly`, `dayOnly`, hazard `periodSec` are not in the C# type at all and are dropped silently. Issue #713 is correct on all three claims: `surface` has zero gameplay effect, the foul wrap/backstop/hip rail are `HarborWall` literals (36 ft, -36 ft, 4.2 ft) shared by every park through `FieldBounds.Of(park)`, and only fences, fence height, wind and hazards vary. All ball physics (drag, gravity, bounce, roll, wall carom, time stretch) and all body motion are global rule tables. The diamond and the nine start spots are process-wide statics. Hazards are thin: every hazard effect is decided once, at the preview, from the ball's landing point; nothing reads a fielder's or a runner's position against a hazard; four of eleven hazard types have no sim effect. The trial overlay replaces park files whole, and refuses a trial park that lacks a key the shipped park has.

---

## 1. Park schema

Types: `Park` record `Sim/Models.cs:186-209`; `Hazard` record `Sim/Models.cs:211`; loader DTOs `ParkDto` `Sim/ContentValidation.cs:508-531`, `HazardDto` `:533-540`. Load path: `ContentCatalog.Load` `Sim/Content.cs:50-68` -> `ContentDataValidator.Load` `Sim/ContentValidation.cs:32-40` (throws on any error) -> `ReadRows(root,"parks",…)` `:75`.

| JSON field | C# | DTO default | Validation (`ValidatePark`, `Sim/ContentValidation.cs:303-337`) | Required in effect |
|---|---|---|---|---|
| `id` | `Park.Id` | `""` | `Required` `:306`; duplicate ids `:141` | yes |
| `name` | `Name` | `""` | `Required` `:307` | yes |
| `faction` | `Faction` | `""` | `Required` `:308` (any non-empty string) | yes |
| `surface` | `Surface` | `""` | `Known` in {grass, dirt, ice, ash} `:12`, `:309` | yes |
| `leftFenceFt` / `centerFenceFt` / `rightFenceFt` | `int` | 0 | `Positive` `:310-312` | yes |
| `windMph` | `double` | 0 | `Finite` `:313` (negative allowed) | no (0) |
| `windDeg` | `double` | 0 | `FiniteRange` -360..360 `:314` | no (0) |
| `fenceHeightFt` | `double` | 0 in DTO (`:521`); 8 in the record (`Models.cs:197`) | > 0 and > `HarborWall.HipHeight` 4.2 `:315-318` | yes (DTO 0 fails) |
| `nightContactWindowMul` | `double` | 1.0 `:523` | in (0, 1] `:319-320` | no |
| `hazards[]` | `List<HazardDto?>?` | null -> empty `:529` | per row: `type` in `HazardTypes` `:20-24`, `:330`; `x`,`z` finite; `radius` >= 0 and > 0 unless `train` `:331-335`; null row is an error `:325-328` | no |
| hazard `tag` | `string?` | null | none | no |

Fields in JSON that no C# type has (dropped by System.Text.Json, no error, no warning): `notes`, `nightOnly`, `dayOnly` (all six parks), hazard `periodSec` (`data/parks/funfair-park.json:39`). Both JSON option sets have no `UnmappedMemberHandling` (`Sim/ContentValidation.cs:42-47`, `Sim/Content.cs:53-58`). Compare the rule tables, where an unknown field IS an error (`RulesValidation.UnknownFields`, `Sim/Rules.cs:111`).

Unknown hazard type: a hard error that stops the catalog load (`:330`, test `Tests/ContentValidationTests.cs:102`). docs/parks.md:80 ("ignores the rest with a warning") is false on both counts: unknown types are refused, unknown fields are ignored with no warning. There is no warning channel in the loader.

Who validates: the same `ContentDataValidator` runs on every `ContentCatalog.Load` (sim, CLI, tests, Unity). `cli art` adds only "park kit missing <id>" against `data/art/parks.json` (`Sim/Art.cs:237-240`, `Cli/Program.cs:173-198`). Tests: `Tests/ContentValidationTests.cs:41,102,112-154`.

Fields read by nothing in Sim/CLI: `Faction` (park), `Name` (CLI/HUD print only), hazard `Tag` (Unity labels only; `HitStarSign` does not test `tag == "star"`, `Sim/Fielding.cs:708-716`).

## 2. Read sites per field

| Field | Read site | What it changes |
|---|---|---|
| `surface` | `Sim/ContentValidation.cs:309` | validation only |
| `surface` | `src/GrandSluggers.Play/Palette.cs:70`, `Play/WorldView.cs:11-12` | raylib viewer colours |
| `surface` | `unity/Assets/Scripts/Runtime/ParkView.cs:46-47` | Unity dressing |
| `surface` | tests assert the string only: `Tests/AtBatTests.cs:60-141`, `Tests/MatchTests.cs:475-574`, `Tests/CompactGeometryTests.cs:251` | none |
| `windMph`, `windDeg` | `Sim/BallFlight.cs:30` (-> `:44-45`), `:64-65` (deflected ball); `Park.WindDirection` `Sim/Models.cs:201-208` | flight: drag is taken relative to wind x `flight.windMul` |
| `left/center/rightFenceFt` | `AtBatResolver.FenceAt` `Sim/AtBatResolver.cs:239-250`, `RoundFence` `:265-293` | fence radius at a spray |
| (via `FenceAt`) | `FieldBounds.Build` `Sim/FieldBounds.cs:133-137`, `FoulSide` `:183` | flight clip polygon, pole radius of the foul wrap |
| (via `FenceAt`) | `FlyCatch.WallPlant` `Sim/FlyCatch.cs:240` | glove plant inside the wall |
| (via `FieldBounds.Clamp`) | `Sim/Fielding.cs:477,480`; `Sim/FieldingPursuit.cs:114,122`; `Sim/FlyCatch.cs:212,253`; `Sim/LivePlaySystem.Field.cs:1944,2231,2237,2239,2274,2370,2375,2384,2795,3091,3120,3491` | every body and every loose ball is held 8 ft inside the wall |
| fence posts | `Sim/HarborWall.cs:50-56,80,125,186`; `Sim/ParkDiamond.cs:397,404,421,434`; `Sim/HarborPostcard.cs:73,108` | drawn wall loop, track, poles, postcard (presentation geometry that lives in Sim) |
| fence posts | `Sim/FieldBounds.cs:115` | cache key |
| `fenceHeightFt` | `Sim/FieldBounds.cs:159,177` | top of the fair fence segments (homer vs carom, `Sim/BallFlight.cs:130`) |
| `fenceHeightFt` | `Sim/BattedBall.cs:153,175` | `FenceClearFt` (rob height test) |
| `fenceHeightFt` | `Sim/HarborWall.cs:30,206,209`; `Sim/HarborStands.cs:42`; `Sim/ParkDiamond.cs:89-90`; `Sim/HarborPostcard.cs:111` | drawn wall top, stands, pole gates |
| `nightContactWindowMul` | `ParkHazards.ContactWindowMul` `Sim/Fielding.cs:648-652` <- `Sim/AtBatResolver.cs:188` <- `Sim/Match.cs:958,1006` | swing timing window at night |
| `hazards` | `Sim/Fielding.cs:659,687,706,710,720` (see section 3) | preview flags, stars, clamber |
| `hazards` | `Play/WorldView.cs:43-80`, Unity `ParkView` | drawing |
| `id` | `Sim/Fielding.cs:679` (`!= "funfair-park"`), `Sim/FieldBounds.cs:115` (cache key), `Sim/Match.cs:90`, `Sim/RaceCohort.cs:24` (trace labels), `Sim/CarnivalFront.cs:226-238` (menu copy) | chompers gate; labels |
| whole `Park` record | `PlayTraceIdentity.Capture` `Sim/PlayTraceRace.cs:12-20` | serialised into the identity SHA of every trace and cohort |

CPU: no CPU decision reads a park field directly. The CPU sees the park only through the clipped flight (`BattedBall`), `FieldBounds.Clamp`, and `Preview.Frozen`.

## 3. Hazards

All logic is `ParkHazards`, `Sim/Fielding.cs:645-729`. All tests are against one point: the preview landing mark (`BattedBall.LandingX/Z`, the first ground contact / wall / fence event, `Sim/BallFlight.cs:203-209`). No hazard is tested against a fielder position, a runner position, or the ball's later roll.

| Type (parks) | Sim effect | Where | Acts on | Geometric or roll | Live tick? |
|---|---|---|---|---|---|
| `freeze_volume` (Crystal x3) | `InSlow` true if landing is inside the disc | `Sim/Fielding.cs:657-668`, called at `:80` | sets `Preview.Frozen` for the whole play | geometric | Flag is computed once in `FieldingResolver.Preview`. The live tick reads it: chase speed x `fielding.chase.frozenMul` 0.45 (`Sim/Fielding.cs:402-407`; callers `Sim/LivePlaySystem.Field.cs:1286,1570,1757,1831`; trace `Sim/PlayTraceRace.cs:91`). Every body that chases in that play is slowed, for the whole play, carry included. Burrow exemption is tested on the first chosen fielder only (`:80`, `Sim/FieldAbilities.cs:52`). |
| same | drop roll on the catch | `Match.RollDrop` `Sim/Match.cs:696-704`, called `Sim/LivePlaySystem.Field.cs:1111` | the catch | roll: `_rng.NextDouble() < fielding.drops.frozen` (0.4, `data/rules/fielding.json:138`, `Sim/Rules.cs:1415`) | yes |
| `lava_pit` (Ember x3) | same as freeze | same | same | same | same |
| `fire_breath` (Ember x1) | same; radius x `fielding.park.emberNightFireMul` 1.6 at night | `Sim/Fielding.cs:663-664`; `data/rules/fielding.json:275` | same | geometric | same |
| `warp_pipe` (Funfair x3), `barrel` (Canopy x3) | grounder landing within `radius + fielding.park.pipeReachPadFt` (8; trial 5.6) moves the **preview landing** to a random other pipe; `Warped` true | `WarpIfPipe` `Sim/Fielding.cs:685-703`; called `:69-77` (grounder shapes only) | preview landing mark, `CoverBallX` (`Sim/LivePlaySystem.Field.cs:465`), chase target, caption `Sim/Match.cs:1556` | exit is a roll: `rng.Next(exits.Count)` `:701` (the match stream). Entry is geometric. | Resolver/preview only. The live ball path stays `Preview.Ball.Samples` (`Sim/LivePlaySystem.Field.cs:462-463`); the ball does not teleport in the tick. Needs >= 2 pipes (`:688`). First pipe in list order wins (`:691-698`). |
| (no hazard) shell/cask star swing | `Warped` true by roll | `Sim/Fielding.cs:82-83`: `rng.NextDouble() < fielding.park.shellWarpChance` (0.6) | flag only | roll | flag only |
| `billboard` (Rooftop x2) | landing inside disc and kind is Single/Double/Triple/HomeRun/FlyOut -> batting team + `stars.gains.billboard` (1.0) | `HitStarSign` `Sim/Fielding.cs:708-716`; award `Sim/Match.cs:1603-1608`; `data/rules/stars.json:11` | stars | geometric; `tag` not tested | post-play commit, not the tick |
| `climb_wall` (Canopy x1) | **presence flag only**; x/z/radius (0, 370, 90) are never read. A Clamber fielder gets: rob height 28 ft (`Sim/FlyCatch.cs:33-34`), window +0.12 s (`:56-57`), catch radius +6 ft **anywhere in the park** (`Sim/Fielding.cs:321-322`), feat label (`:129,147`) | `CanClamber` `Sim/Fielding.cs:718-720` | fielder reach | geometric (vs `FenceClearFt`) | yes (reach and window are read live) |
| chompers (not data) | `FunfairChompers` code literal: (-72,205,r16), (0,228,r18), (78,198,r16); night AND `park.Id == "funfair-park"` AND not grounder/liner -> `Chomped` -> FlyOut at the hang | `Sim/Fielding.cs:670-683`; `:87`; `:112`; live `Sim/LivePlaySystem.Field.cs:791-799,3596` | ball in air -> out | geometric | yes. Not in park JSON, so the trial does not scale them (trials/c80/README.md:115-120; `Tests/CompactGeometryTests.cs:1229`). |
| `statue` (Ember) | none | only `HazardTypes` `Sim/ContentValidation.cs:22-23` | - | - | - |
| `train` (Funfair; `z` + `periodSec` only) | none. `periodSec` is not deserialised | same | - | - | - |
| `ac_unit` (Rooftop) | none (no carom) | same | - | - | - |
| `tree` (Canopy x4) | none | same | - | - | - |

Night: `Match.Night` `Sim/Match.cs:16`, set in the constructor `:95,103`; default false in `Slice` `:151`, `Exhibition` `:165,179`, `Challenge.MakeMatch` `Sim/Challenge.cs:63`. Sim readers: `Sim/Match.cs:958,1006` (contact window), `:1039,1048` (preview/resolve -> `InSlow` fire radius, `ChompFly`), `Sim/PlayTraceRace.cs:19` (identity). The CLI has no night flag (`Cli/Program.cs:102`). `ExhibitionPick` has no night field (`Sim/ExhibitionPick.cs:8`); Unity toggles it (`unity/.../FlowDirector.cs:95,218`). `nightOnly` / `dayOnly` in JSON are read by nothing.

Radii and the pad (#732): the trial scales every hazard radius x0.70 and `pipeReachPadFt` 8 -> 5.6; positions migrate by zone (basepath scale inside the shipped 155 ft lip, fence scale beyond) - `trials/c80/README.md:69,83-91`; gates `Tests/CompactGeometryTests.cs:276,524,1091`. Nothing in code scales a hazard with the diamond; the scale is only in the data files. `ParkSlowRowsTests` (`Tests/ParkSlowRowsTests.cs`, 3 tests, `Rows=compact`) holds frozenMul 0.45 on both roots, on both seats, and that the slow has no term in the handling cost.

## 4. Field geometry

| Item | Where | Per park? |
|---|---|---|
| Fair fence = circle through three posts; lerp fallback under 50 ft | `Sim/AtBatResolver.cs:239-293`; `RoundFenceMinFt` 50 `:19` | yes (posts) |
| Foul lines at +-45 deg | `AtBatResolver.FoulLineDeg` `Sim/AtBatResolver.cs:16`; `FieldBounds.IsFair` `Sim/FieldBounds.cs:197-198` | no (const; 25 refs in src) |
| Boundary polygon: 48 fence + 2x20 foul + 16 backstop segments | `Sim/FieldBounds.cs:120-178` | fence only |
| Foul wrap: rail parallel to the line at `FoulOffset` 36 ft, flares to the pole from `flareStart` 95 ft | `HarborWall.FoulWall` `Sim/HarborWall.cs:109-121`; `FoulOffset` `:21` | no (literals) |
| Backstop: round arc, radius = the rail's closest point (about 36 ft); `HomeZ` -36 | `Sim/FieldBounds.cs:140-147`; `Sim/HarborWall.cs:23` | no |
| Foul wall height 4.2 ft on every foul segment, pole to pole | `FieldBounds.FoulWallHeightFt` `Sim/FieldBounds.cs:109,159` <- `HarborWall.HipHeight` `Sim/HarborWall.cs:32` | no |
| Over a foul wall = stands (foul, or ground-rule double if already fair) | `Sim/BallFlight.cs:130-136`; `Sim/BattedBall.cs:157-167` | no |
| Wall carom: normal x `flight.wall.restitution` 0.48, tangent x 0.82; restart 0.15 ft inside (code const) | `Sim/BallFlight.cs:183-194`; rolling carom `:104-108` | no |
| Ground-rule double: grounded then over the fence, or fair then into the foul stands; two bases | `Sim/BattedBall.cs:151,164`; award `Sim/Match.cs:1506-1511`; live end `Sim/LivePlaySystem.Field.cs:788` | no |
| Warning-track clamp: bodies and loose balls held `InsideFt` 8 inside the boundary; floor radius 12 | `Sim/FieldBounds.cs:103,216-226` | via fence |
| Wall plant | `fielding.wallPlant` (8 / 0.45 / 10 / 8) `data/rules/fielding.json:140-145`; `Sim/FlyCatch.cs:232-246` | via fence |
| Rob heights / windows | `fielding.catch.jumpRobFt` 4, `superJumpRobFt` 18, `clamberRobFt` 28, `buddyJumpRobFt` 18 (`data/rules/fielding.json:95-98`); `Sim/FlyCatch.cs:25-41`; `NeedsJump` `:17-18` | clamber only, by flag |
| Grass/dirt split: radial `flight.classes.infieldLipFt` 155 | `FieldingResolver.OutfieldGrass` `Sim/Fielding.cs:332-333`; pop class `Sim/BattedBall.cs:219` | no |
| Drawn wall loop (lives in Sim, used by Unity): single-slot cache under a lock, keyed by the three posts; **mirrors the RF half**, so an asymmetric park's drawn LF is its RF | `Sim/HarborWall.cs:39-60,70-103` (mirror `:99-101`); noted at `Sim/FieldBounds.cs:125-127` | posts |
| Drawn rail height ramps 4.2 -> fence from z 95 to the pole; the sim foul wall stays 4.2 to the pole | `Sim/HarborWall.cs:177-191` (`hipZ` 95) vs `Sim/FieldBounds.cs:159` | - |
| Dugouts: presentation only; the sim polygon has no dugout hole. Literals `Along0` 52, `X` 64.7, `Z` 8.84 | `Sim/HarborDugout.cs:12-38`; only sim-side consumer `Sim/StillPose.cs:159` | no |
| Track width 15, pole 72 ft, stripes, dirt shapes | `Sim/ParkDiamond.cs:17-99` (consts); `BackR`, `InnerHalf` from `Rules.Default.Infield` `:15,24` | no |
| `Diamond` (bags, mound, nine starts) | `Sim/Diamond.cs:15-52`: static, reads `Rules.Default` (process-wide), `_positions` built once | no |

`HarborWall` reference counts (lines that name it): **src non-test 33 in 7 files** (`HarborDugout.cs` 11, `HarborPostcard.cs` 8, `FieldBounds.cs` 8 of which 3 are code and 5 are doc comments, `ContentValidation.cs` 2, `ParkDiamond.cs` 2, `HarborStands.cs` 1, `HarborWall.cs` 1 = the class line). The only gameplay consumers are `FieldBounds.cs:106,109,186` and `ContentValidation.cs:317-318`; **tests 28 in 4 files** (`HarborWallTests` 16, `HarborPostcardTests` 8, `FlightScenarioTests` 3, `ParkDiamondTests` 1); **unity 13 in 2 files** (`HarborKit.cs` 11, `ParkView.cs` 2).
Other counts (src non-test / tests / unity): `HarborDugout` 12/31/38; `HarborStands` 3/10/29; `HarborPostcard` 4/30/14; `ParkDiamond` 17/90/61; `Diamond.Positions` 26/90/1; `FieldBounds.` 32/48/0; `AtBatResolver.FenceAt` 16/7/4; `FoulLineDeg` 25/4/1.

## 5. Environment physics that is global today

`data/rules/flight.json` -> `FlightRules` `Sim/Rules.cs:931-1006`. One consumer loop: `BallFlight.Run` `Sim/BallFlight.cs:79-180`.

| Key (value) | Consumer |
|---|---|
| `gravity` 32.174 | `Sim/BallFlight.cs:120`; also `Sim/Fielding.cs:251`, `Sim/LivePlaySystem.Field.cs:2508,3061` |
| `drag` 0.0019 (trial 0.0040) | `Sim/BallFlight.cs:118-120` |
| `timeScale` 1.65 / `linerTimeScale` 1.0 / `dirtTimeScale` 1.65 | `FlightRules.TimeScaleFor` `Sim/Rules.cs:951-956` -> `Sim/BallFlight.cs:47,67,87` |
| `plateHeightFt` 2.5 | `Sim/BallFlight.cs:48,246`; `Sim/LivePlaySystem.Field.cs:477` |
| `windMul` 0.35 | `Sim/BallFlight.cs:44-45,64-65` |
| `sampleHz` 120, `maxSeconds` 12 | `Sim/BallFlight.cs:82,84` |
| `bounce.restitution` 0.48 / `horizontal` 0.82 / `minVy` 3.6 | `Sim/BallFlight.cs:159-171` |
| `skid.*` (14-22 deg, 0.28 / 0.93 / 2.2) | `Sim/BallFlight.cs:46,66,159-161` |
| `roll.friction` 22, `roll.restSpeed` 1.4 | `Sim/BallFlight.cs:90-97` |
| `wall.restitution` 0.48 / `tangential` 0.82 | `Sim/BallFlight.cs:183-194` |
| `landing.firstGrassMinSec` 0.08 | `Sim/BallFlight.cs:145` |
| `classes.*` (launch bands, `infieldLipFt` 155) | `Sim/BattedBall.cs:48,116,206-220`; `Sim/Fielding.cs:333`; `Sim/ParkDiamond.cs:420` |
| `deadBall.*` | `Sim/InPlay.cs:16`; `Sim/LivePlaySystem.cs:734`; `Sim/LivePlaySystem.Field.cs:789,822` |

Surface-like numbers outside flight.json (all global):

| Key | File:line | Consumer |
|---|---|---|
| `chase.baseFtPerSec` 21, `ftPerSecPerRun` 1.9, `frozenMul` 0.45, `minFtPerSec` 8 | `data/rules/fielding.json:3-6` | `Sim/Fielding.cs:402-407` |
| `chase.outfieldAirMul` 0.6, `infieldAirMul` 0.45 | `:18,27` | `Sim/Fielding.cs:416-417` (`AirMul`) |
| `chase.accelSec` 0 / `brakeSec` 0 (response law; trial 0.20 / 0.10) | `:31-32` | `Sim/LivePlaySystem.Field.cs:2231-2274` |
| `chase.handoffCoastSec` 0.2 | `:12` | `Sim/LivePlaySystem.Field.cs:1944` |
| `overthrow.decelFtPerSec2` 18 (a second ground friction, for a loose thrown ball) | `:192` | `Sim/LivePlaySystem.Field.cs:3116` |
| `handling.bobbleDecelFtPerSec2` 6, `bobbleGroundRetain` 0.90, `bobbleRestitution` 0.35 (a third ground model, for the local bobble) | `:263-267` | `Sim/LivePlaySystem.Field.cs:3059-3099` |
| `recoil.kickFtPerSec` 10, `capSec` 0.20 | `:239-247` | recoil skid |
| `drops.frozen` 0.4 | `:138` | `Sim/Match.cs:702` |
| `running.bagSec.*` (3.55 - 0.12 x Run, clamp 2.45-3.65) - runner pace is seconds per bag, not ft/s | `data/rules/running.json:2-10` | `Sim/Runner.cs` / `Sim/Baserunning.cs` |
| `running.bags.slideFt` 12, `slideThreatFt` 20, `slideReachCutFt` 2, `overrunFt` 12 | `data/rules/running.json:17-20` | slide / overrun geometry |
| `infield.json` (90 / 60.5 / 63.64 / 127.28 / 50 / 92) | `data/rules/infield.json` | `Sim/Diamond.cs:15-24`, `Sim/ParkDiamond.cs:15,24` via `Rules.Default` |
| `fielders.json` seven start spots; LF/RF (+-110, 250), CF (0, 305) for all six parks | `data/rules/fielders.json:5-37` | `Sim/Diamond.cs:40-52` via `Rules.Default` |

Code literals (not in JSON) that a per-park environment would meet:
- `HarborWall.FoulOffset` 36, `HomeZ` -36, `HipHeight` 4.2, `DugoutPad` 18 (`Sim/HarborWall.cs:21-32`); `flareStart` 95 (`:114`); `hipZ` 95 (`:183`). `Tests/HarborWallTests.cs:42-45` records the 95 as a `Copy=gap` on the trial.
- `FieldBounds.InsideFt` 8 and the 12 ft clamp floor (`Sim/FieldBounds.cs:103,222`); carom restart 0.15 ft (`Sim/BallFlight.cs:192`); `FenceSegs` 48 / `FoulSegs` 20 / `HomeSegs` 16 (`Sim/FieldBounds.cs:120-122`).
- `AtBatResolver.FoulLineDeg` 45, `RoundFenceMinFt` 50 (`Sim/AtBatResolver.cs:16,19`).
- `ParkHazards.FunfairChompers` and the `"funfair-park"` id test (`Sim/Fielding.cs:670-679`).
- `ExhibitionPick.Parks` six ids (`Sim/ExhibitionPick.cs:10-11`); `PresetTeams.HomeParkId` (`Sim/Teams.cs:37-45`); `Training.ParkId` (`Sim/Training.cs:18`); `CarnivalFront.Gimmick` copy (`Sim/CarnivalFront.cs:229-238`).
- `HarborDugout` feet literals (`Sim/HarborDugout.cs:12-38`); `ParkDiamond` consts (`Sim/ParkDiamond.cs:17-99`); `HomeSet.CatcherZ` (catcher start, `Sim/Diamond.cs:44`).
- The outfield starts are data since #725 but global: one set for six parks (`data/rules/fielders.json:20-24`; `Sim/Diamond.cs:34-38` names #713 as the owner of per-park depth). `ClampField` pins a start that is outside a park (`Sim/LivePlaySystem.Field.cs:2372-2385`).
- Direct process-wide reads that bypass a match's table: `Sim/Diamond.cs:15-16`, `Sim/ParkDiamond.cs:15,24,420`, `Sim/RunnerAi.cs:82`. Also 118 `Rules.Or(rules)` sites fall back to `Rules.Default` when a caller omits `rules`.

## 6. Data roots and overlays

- Root: env `GRAND_SLUGGERS_DATA` (`Sim/Content.cs:129,136-142`), else the `data/` above the binary (`:160-171`). Overlay: env `GRAND_SLUGGERS_TRIAL` (`Sim/DataRoot.cs:43`), relative paths resolve beside `data/` (`:180-192`). The overlay rides only on the environment root, never on a root a caller passes (`Sim/Content.cs:149-158`).
- Resolution is **per file** (`DataRoot.Resolve` `Sim/DataRoot.cs:110-116`). Directory listings come from the shipped root (`Files` `:143-152`): an overlay may replace a park, never add one (`Declared` `:211-238` throws on a file the shipped root lacks).
- **Whole files**: a trial JSON must carry every object key the shipped file has, recursively; arrays are not walked (`WholeFiles` / `Missing` `Sim/DataRoot.cs:280-341`). So a new key in a shipped park JSON makes the load of `trials/c80` throw until all six trial parks carry it. `Tests/CompactGeometryTests.cs:1039` also asserts equal key sets, hazard rows included.
- Parks ARE overlaid: `trials/c80/parks/*.json` (six). Diff vs shipped: fences x0.70, hazard x/z by zone, radii x0.70; height, wind, night window, surface unchanged (`Tests/CompactGeometryTests.cs:191,206,241,276`).
- `Rules.Default` (`Sim/Rules.cs:128-167`): lazy static, loaded once from the environment root + overlay; a named root that cannot load throws (`:150-166`). `Diamond` and `Diamond.Positions` read it (`Sim/Diamond.cs:15-16,40-52`), so the diamond is the process's, not the match's. A test that loads the trial catalog by hand gets trial parks and rules on the shipped diamond; that is why compact rows run in a second CI process (`.github/workflows/validation.yml:29-32`; `Tests/TestRoot.cs:15-17`).

Static caches:

| Cache | Key | Thread safety | Reset |
|---|---|---|---|
| `FieldBounds.Cache` `Sim/FieldBounds.cs:111-117` | `id|L|C|R|height` | `ConcurrentDictionary` | never; shipped and trial parks coexist because the posts are in the key. A new per-park boundary input (foul offset, rail height) is not in the key. |
| `HarborWall._loop` `Sim/HarborWall.cs:39-60` | the three posts, one slot | `lock` | overwritten on a different park |
| `Diamond._positions` `Sim/Diamond.cs:26,40-52` | none (process) | benign race (`??=`) | never |
| `Rules._default` `Sim/Rules.cs:130-132` | none | benign race | never |
| `StarSkillTable._default` `Sim/StarSkillTable.cs:57` | none | - | never |

No test resets any of them.

## 7. Where a park is chosen and threaded

| Entry | Park | Where |
|---|---|---|
| `new Match(content, away, home, park, innings, seed, night, mercy, difficulty)` | caller's | `Sim/Match.cs:95-123`. `Park` is a property `:15`; rules are `content.Rules.AtLevel(difficulty)` `:98` (`Sim/Rules.cs:38-47`: the one precedent for a per-match derived table). No MatchConfig type. |
| `Match.Slice` | default `"harbor-diamond"`; unknown id falls back to Harbor silently | `Sim/Match.cs:151-156` |
| `Match.Exhibition` | `parkId ?? PresetTeams.HomeParkId(homeCaptain)`; unknown id -> Harbor | `Sim/Match.cs:158-186`; `Sim/Teams.cs:37-45` (vale->crystal, zig->funfair, brondo->rooftop, konga->canopy, ashlord->ember, else Harbor) |
| `Challenge.MakeMatch` | the **opponent's** home park | `Sim/Challenge.cs:63-71` |
| Tutorials / Practice | always Harbor | `Sim/Training.cs:18`; `Sim/TutorialSession.cs:113` |
| Exhibition pick | six-id literal list, default Harbor | `Sim/ExhibitionPick.cs:10-15,46-54` |
| `cli match [--park id]` | `--park`/`-p`; absent -> home captain's park; 3 innings; day only | `Cli/Program.cs:67,102,121-127,294-298` |
| `cli match --cohort s29` | home park of each home captain | `Sim/RaceCohort.cs:20` |
| `cli match --cohort harbor-calibration|harbor-validation` | forced `"harbor-diamond"`, seeds 1-5 / 1001-1005 | `Sim/RaceCohort.cs:7-8,21` |
| `cli at-bat` | Harbor literal | `Cli/Program.cs:343` |
| `cli challenge` | via `Challenge.MakeMatch` | `Cli/Program.cs:326-330` |

S-29 (`Tests/AtBatScenarioTests.cs:690-741`): 5 pairs x both ways x seeds 1-5 = 50 three-inning day games through `Match.Exhibition` with no park id, so each game is at the home captain's park. Homes: rio x2, fenn (-> Harbor) = 15 Harbor; konga x2 = 10 Canopy; brondo x2 = 10 Rooftop; ashlord 5 Ember; zig 5 Funfair; vale 5 Crystal. So the balance gate is pooled over all six parks, 70 % of it away from Harbor. It asserts pooled means only (1.8-5 runs a side, doubles < singles, HR <= 2 a game); there is no per-park band. The `harbor-*` cohorts are the only Harbor-only balance evidence. No gate runs at night except `Tests/NightTests.cs:111` (games finish).

## 8. Tests and gates that touch parks

| Class (file under `Tests/`) | Tests | What |
|---|---|---|
| `CompactGeometryTests` | 28 | trial parks: fences, order, hazards by zone, radii, pad, key equality (`:1039`), chomper vs CF start (`:1229`). Iterates all six. |
| `HarborWallTests` | 5 (2 theories x 6 parks; 1 is `Copy=gap`) | D15: drawn top = flight fence; rail 4.2 = `FoulWallHeightFt` |
| `HarborPostcardTests` 6, `ParkDiamondTests` 6, `HarborKitPaintTests` | - | presentation geometry that lives in Sim |
| `NightTests` | 7 | day default, Crystal window 0.85, Funfair chompers, Ember fire x1.6, Harbor night = day at the same seed |
| `ParkSlowRowsTests` | 3 (`Rows=compact`) | frozenMul on both roots and both seats |
| `MatchTests` | 34 (about 8 park rows, `:470-580`) | each park loads; `InFreeze`, `WarpIfPipe`, billboards, clamber |
| `AtBatTests` | 17 (park rows `:55-141`) | assert surface strings and that contact is equal across parks |
| `BalanceTests` | 10 (`:245-246` window mul) | - |
| `FlightScenarioTests` | 16 (`:334` loops all parks) | no fair sample leaves the polygon; wall / fence / foul events |
| `FieldingPursuitTests` | 6 (`:14,68,138` loop all parks) | routes stay legal in every park |
| `FlyCatchTests` | 14 (`:15` loops all parks) | chase target inside each park |
| `FoulTests` 7, `FieldingSceneTests` 20, `JumpTests` 7 | - | Harbor fixtures |
| `AtBatScenarioTests.S29_…` `:690` | 1 | pooled six-park band (section 7) |
| `ContentValidationTests` `:41,102,112-154` | - | park id case, hazard type case, numeric domains, null hazard |
| `TrialOverlayTests` | 29 | overlay rules (per file, whole file, no add) |
| `RaceTraceTests`, `ThreeDExportTests` | - | cohort identity, 3d export |
| `ArtCatalogTests` | 12 | a kit slot per park |

CI (`.github/workflows/validation.yml`): full test run `:23`; compact rows with `GRAND_SLUGGERS_TRIAL=trials/c80` `:29-32`; `cli art` `:33`; `cli tutorials` `:34`; flight probe `--check` `:41`; compact-field report `--check` `:43`.

Seals that a park change would break:

| Seal | Hashes | A new field in a park JSON | A new property on `Park` / `Models.cs` |
|---|---|---|---|
| `tools/compact-field-report.py:132-150` (CI) | `data/parks/harbor-diamond.json`, `fielding.json`, `running.json`, `Diamond.cs`, `AtBatResolver.cs`, `BallFlight.cs`, `BattedBall.cs`, `Fielding.cs`, `FlyCatch.cs`, `ParkDiamond.cs`, `InPlay.cs`, `LivePlaySystem.Field.cs`, `FieldingPursuit.cs`, `Match.cs`, `FieldAbilities.cs`, more | breaks if Harbor's file changes | breaks on any listed `.cs` |
| `tools/game-feel-flight-probes/Program.cs:98-104` (CI) | `flight.json`, `batting.json`, `star-skills.json`, `BallFlight.cs`, `FieldBounds.cs`, `Rules.cs`, `BattedBall.cs`, `AtBatResolver.cs`, `HarborWall.cs`, `Models.cs` | no | **yes** (`Models.cs`, `Rules.cs`, `FieldBounds.cs`, `HarborWall.cs`) |
| `tools/game-feel-scale-probes/Program.cs:213-218` (#730; not in CI) | `flight.json`, `fielding.json`, `running.json`, trial `infield.json` / `flight.json`, `BattedBall.cs`, `Fielding.cs`, `Diamond.cs`, `HarborWall.cs`, `AtBatResolver.cs` | no | yes on those files |
| `PlayTraceIdentity` `Sim/PlayTraceRace.cs:10-26` | serialises `match.Rules`, `match.Park`, teams, feel, star skills, night | yes, if the field is deserialised | yes: every identity `sha256` moves. Stored in `docs/research/game-feel-3d-race-control.json`, `-trial.json`, `game-feel-702-baseline.json`; checked by `tools/race-report.py:8-14` |
| `RaceEvidence.Validate(root)` `Sim/RaceEvidence.cs:12` | runs inside every content load (`Sim/ContentValidation.cs:52`) | not inspected in depth | - |

## 9. Tools that read parks

| Path:line | Purpose |
|---|---|
| `tools/compact-field-report.py:19-21,91-110` | own Python copy of `RoundFence` (symmetric case); fence scale and wall-per-basepath rows |
| `tools/compact-field-report.py:138` | hashes `data/parks/harbor-diamond.json` into the seal |
| `tools/compact-field-report.py:1649-1663,1704-1709` | asserts the park-migration rows (one 0.70 scale, size order kept, Harbor 232/280/232) |
| `tools/game-feel-scale-probes/Program.cs:19` | literal list of six park ids |
| `tools/game-feel-scale-probes/Program.cs:90-146,172-185,254-270` | foul share per park, hazard area share and capture disc per park, outfield start vs `FenceAt` at its bearing, shape counts per park; loads shipped and trial catalogs |
| `tools/game-feel-flight-probes/Program.cs:44-47,78` | builds a synthetic `new Park("research-c80", … "grass", …)` positionally - a new required `Park` constructor parameter breaks this compile |
| `tools/race-report.py:8-14,85` | verifies identity SHA; hashes source paths |
| `tools/dcc-still.sh:76-80`, `tools/blender/harbor_kit.py`, `tools/blender/README.md:50-58` | Harbor kit FBX authoring; reads no park JSON |
| `tools/local-player.py`, `tools/build-player.sh`, `tools/still-gate*.sh` | no park data read found (grep for `parks/`, park ids) |

## 10. Docs vs code drift

| # | Doc says | Code does |
|---|---|---|
| 1 | docs/parks.md:80: the sim "ignores the rest with a warning" | unknown hazard type = load error (`Sim/ContentValidation.cs:330`); unknown fields dropped silently; no warning channel |
| 2 | parks.md:20: freezers "on the dirt. Touch one, frozen 1.2 s. Can be shattered by a charged runner or a line drive" | no touch test, no timer, no shatter. Landing-in-disc -> x0.45 for the whole play for every chaser, + 0.4 drop roll (`Sim/Fielding.cs:80,402-407`; `Sim/Match.cs:702`). One Crystal volume is at z 180, not on the dirt. |
| 3 | gameplay-spec.md:1053 (section 14): "freeze volumes (x0.45 speed 1.2 s)" | no 1.2 s anywhere in rules or code |
| 4 | spec:1053: "tilt (grounders drain to a line)" listed as a ticked hazard | no `tilt` type (`Sim/ContentValidation.cs:20-24`), no code |
| 5 | spec:1053: "Each is a field rule with geometry"; "never a park-id string in code" | chompers are a code literal gated on `park.Id != "funfair-park"` (`Sim/Fielding.cs:670-679`). The sentence is true for the window only. |
| 6 | spec:1053 "Others tick hazard ids"; systems.md:137 "Hazards are ids the sim ticks" | nothing ticks. All hazard tests run once at `Preview`, on the landing mark. |
| 7 | parks.md:28: a grounder "that enters" a warp can "exits another" | tested on the first landing point within radius + 8 ft pad; the live ball path is not moved (`Sim/LivePlaySystem.Field.cs:462-463`); only the preview landing, cover side and caption change |
| 8 | parks.md:29: train "Periodic catch block / launch stays in the sim notes" | `train` has no sim code; `periodSec` is not deserialised |
| 9 | parks.md:37: "Balls can carom; some signs award a star"; rooftop notes: "AC units carom grounders" | `ac_unit` has no effect; every billboard awards (no `tag` test); billboards do not carom |
| 10 | parks.md:46: Clamber "can rob a homer that only just cleared the fence" at the climb wall | climb wall position/radius unused; Clamber also adds +6 ft catch radius and +0.12 s window anywhere in a park that lists `climb_wall` (`Sim/Fielding.cs:321-322`, `Sim/FlyCatch.cs:56-57`). Rob is up to 28 ft over. |
| 11 | parks.md:65 and ember notes: "the captain statue's fire breath" | `statue` has no effect; `fire_breath` is its own disc at (0, 250), the statue is at (0, 310) |
| 12 | systems.md:137: surfaces `grass / ice / deck / dirt`; park JSON has a "day/night flag"; hazards "lava spitters, tilt, blackout" | valid set is grass / dirt / ice / ash; `deck` is refused; `nightOnly` / `dayOnly` are read by nothing; blackout is the `nightContactWindowMul` field |
| 13 | parks.md:19,36,63 give surfaces a meaning (ice, tar roof, ash) | no gameplay read of `surface` (section 2). #713 is correct. |
| 14 | spec:571: "Any touch of a foul wall, or over one, -> foul, dead" | only if the ball is not yet decided: a ball already fair caroms off the rail and stays live (`Sim/BattedBall.cs:168-170`, `Decide` is first-wins `:131-139`); fair then over the rail is a ground-rule double (`:164`) |
| 15 | parks.md:102 / D15: the drawn wall and the flight are "one number" | true between the poles. On the foul side the drawn rail ramps 4.2 -> fence from z 95 (`Sim/HarborWall.cs:177-191`); the sim foul wall is 4.2 to the pole (`Sim/FieldBounds.cs:159`). The drawn loop also mirrors RF onto LF (`Sim/HarborWall.cs:99-101`); Funfair (315 / 340), Rooftop, Canopy are asymmetric. |
| 16 | parks.md:86 "round backstop 36 ft behind the plate" | the arc radius is the rail's closest point to home (`Sim/FieldBounds.cs:140-147`), not `HomeZ`; `BackstopZ` (`:106`) is a separate -36 constant |
| 17 | spec:701 "Dash x1.35 for 2 s then fades" | the same line records the fade was never in code |
| 18 | parks.md Haunt Manor, Cruise Deck, Playroom | no data files; `ExhibitionPick.Parks` is a six-id literal |
| 19 | `Tests/ParkSlowRowsTests.cs:8` "the statue's breath" | the type is `fire_breath`; `statue` is inert |

#713's claims: all verified. `surface`: zero gameplay reads. Foul territory: `FieldBounds.Build` -> `HarborWall.FoulWall` (FoulOffset 36) + `HipHeight` 4.2 for every park (`Sim/FieldBounds.cs:109,159,181-188`). Varying today: three posts, fence height, wind speed + direction, hazards, and one more that #713 does not list: `nightContactWindowMul`.

---

## Harbor-is-hardcoded inventory (ranked by blast radius)

| Rank | Assumption | Where | Refs (src non-test / tests / unity) |
|---|---|---|---|
| 1 | One diamond and nine start spots for the process; outfield starts are one global set | `Sim/Diamond.cs:15-52`; `data/rules/fielders.json`; `Rules.Default` | `Diamond.Positions` 26 / 90 / 1; `Diamond.` far more |
| 2 | All ball physics is one table for every park (drag, gravity, bounce, skid, roll, wall, stretch, windMul) | `data/rules/flight.json`; `Sim/BallFlight.cs:79-194` | 118 `Rules.Or` fallbacks; every flight fixture in tests |
| 3 | All body motion is one table (chase, air multipliers, response law, coast, frozenMul); runner pace is sec/bag | `data/rules/fielding.json:2-33`; `data/rules/running.json:2-10` | 15 chase-speed calls in `LivePlaySystem.Field.cs` alone |
| 4 | Foul lines at 45 deg | `Sim/AtBatResolver.cs:16` | 25 / 4 / 1 |
| 5 | Foul wrap, backstop, rail height are `HarborWall` literals | `Sim/HarborWall.cs:21-32,109-121`; `Sim/FieldBounds.cs:106,109,181-188` | `HarborWall` 33 / 28 / 13 |
| 6 | Boundary cache key = id + posts + height | `Sim/FieldBounds.cs:115` | `FieldBounds.` 32 / 48 / 0 |
| 7 | Warning-track pad 8 ft, clamp floor 12 ft, carom restart 0.15 ft | `Sim/FieldBounds.cs:103,222`; `Sim/BallFlight.cs:192` | 22 `Clamp` call sites |
| 8 | Grass/dirt split is one radial lip (155) | `data/rules/flight.json` `classes.infieldLipFt`; `Sim/Fielding.cs:332-333` | 11 `OutfieldGrass` sites |
| 9 | Three loose-ball ground models, none per surface (`flight.roll`, `overthrow.decel`, `handling.bobble*`) | `Sim/BallFlight.cs:90-97`; `Sim/LivePlaySystem.Field.cs:3059-3126` | 3 sites |
| 10 | Hazards act once, at preview, on the landing point; slow is a play-wide bool | `Sim/Fielding.cs:69-90`; `FieldingPreview.Frozen` `:628` | 7 `Frozen` readers |
| 11 | Chompers: literal coordinates + park id string | `Sim/Fielding.cs:670-683` | 1 + Unity `ParkView` |
| 12 | Default / fallback park is the `"harbor-diamond"` literal; unknown ids fall back silently | `Sim/Match.cs:151,154,184`; `Sim/Challenge.cs:69`; `Sim/Training.cs:18`; `Sim/Teams.cs:44`; `Sim/ExhibitionPick.cs:13`; `Sim/RaceCohort.cs:21`; `Cli/Program.cs:343`; `Sim/HarborPostcard.cs:10` | 10 sites |
| 13 | Park list and home-park map are code | `Sim/ExhibitionPick.cs:10-11`; `Sim/Teams.cs:37-45`; `Sim/CarnivalFront.cs:229-238`; `tools/game-feel-scale-probes/Program.cs:19`; `Tests/HarborWallTests.cs:16` | 5 lists |
| 14 | Drawn-geometry classes in Sim named Harbor and used for every park (`HarborWall`, `HarborDugout`, `HarborStands`, `HarborPostcard`, `HarborKitPaint`, `HarborInfield`); feet literals do not scale (95, 52, 64.7) | `Sim/Harbor*.cs` | `HarborDugout` 12 / 31 / 38; `ParkDiamond` 17 / 90 / 61 |
| 15 | Validation couples every park's fence height to the Harbor rail (must be > 4.2) | `Sim/ContentValidation.cs:317-318` | 1 |
| 16 | Trial overlay: whole-file key rule + key-equality test bind shipped and trial park schemas | `Sim/DataRoot.cs:280-341`; `Tests/CompactGeometryTests.cs:1039` | 6 trial park files |
| 17 | Seals hash `harbor-diamond.json`, `Models.cs`, `FieldBounds.cs`, `HarborWall.cs`; identity SHA serialises `Park` | section 8 | 3 tools + 3 research JSON |

## Unknowns

1. Whether the live grounder route is materially changed by a warp beyond `CoverBallX` and the chase target: read from code, not traced in a run.
2. "Every chaser is slowed when `Frozen`" is read from the call sites (all pass `Preview.Frozen`); not confirmed with a play trace. Cover walks use `CoverSpeedFt` (`Sim/Fielding.cs:424`) and were not checked for the flag.
3. How Unity picks its data root and whether it honours `GRAND_SLUGGERS_TRIAL` (`Sim/Content.cs:147` says Unity passes its own root). Out of scope here.
4. What `RaceEvidence.Validate` (`Sim/RaceEvidence.cs`) checks in `data/race-evidence.json`, and if any of it names a park.
5. The exact set of `docs/research/*.json` files that store a `PlayTraceIdentity` SHA (three found by name; not grepped exhaustively).
6. Per-park run and homer rates inside S-29: the gate does not compute them and no run was made (read-only task, no builds).
7. Whether `GrandSluggers.Play` (raylib viewer) is still built or gated; it has its own park-id literals (`Play/WorldView.cs:13`, `Play/Palette.cs:74`, `Play/Game.cs`).
8. Reference counts are `grep` line counts (a line that names the symbol twice counts once; doc comments count).
