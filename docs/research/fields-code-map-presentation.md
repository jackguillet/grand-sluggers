# Fields code map — presentation, art pipeline and process rails

Read-only map made on September 21, 2026 for [research-fields.md](../research-fields.md). Inspected revision: `d0c6e12c` (origin/main). Paths are relative to the repo root; `Sim/` = `src/GrandSluggers.Sim/`, `Tests/` = `src/GrandSluggers.Sim.Tests/`. This file records the code as it was. It proposes no fix. Line numbers go stale; re-check them before you cite them in an issue.


## State in one breath

There is one Unity scene, `HarborDiamond`, and one park builder, `ParkView.BuildPark`, for all six parks. Harbor is the only park with a kit: `HarborKit` (placed scene anchors + `harbor-kit.fbx` + Sim-side geometry classes `HarborWall` / `HarborDugout` / `HarborStands` / `HarborInfield` / `HarborPostcard` / `ParkDiamond`). When the park id is not `harbor-diamond`, `HarborKit` turns itself off, and `ParkView` draws a second, older, primitive-only diamond (cubes and cylinders with code-literal positions and colors), then calls one hard-coded dress method per park id (`CrystalGarden`, `FunfairGrounds`, `RooftopDeck`, `CanopyGrounds`, `EmberCourtyard`). Sky, fog, ambient and three directional lights are code literals in `Look.cs`, one method per park, selected by an `if` chain on park id. Hazards are drawn from `data/parks` by a `switch` on hazard type, but the drawn size is tied to the sim radius for only two types, and Unity has no tell for slow, warp, or chomp. The art catalog has six park kit rows, but a row is only `{id, slot, placed}`; the validator checks only that each park has a row. The Blender script, the DCC stage catalog, the dual-stills catalog, the still-gate tools and the `StillRequest` schema all name Harbor only; a park cannot be requested for a still. Cameras, HUD, audio and post-processing have no per-park data.

---

## 1. How a park gets drawn

### 1.1 Trace, park id to pixels

| Step | What | Evidence |
| --- | --- | --- |
| 1 | Park list for the field pick is a code literal of six ids | `src/GrandSluggers.Sim/ExhibitionPick.cs:10-13` |
| 2 | `MatchDirector.ParkId` (serialized, default `harbor-diamond`) and `Night` hold the pick | `unity/Assets/Scripts/Runtime/MatchDirector.cs:27,30` |
| 3 | `NewMatch()` gives `Match.Exhibition(..., ParkId, Night, ...)`; the sim resolves `content.Parks[id]` | `unity/Assets/Scripts/Runtime/MatchDirector.cs:647` |
| 4 | `ParkView.Build(park, night, rules, feel)` is called at start and on every rebuild (title night toggle, field pick, lineup, stills) | `MatchDirector.cs:200-201,559`; `FlowDirector.cs:107,118-125,204,214,219` |
| 5 | `BuildPark` destroys the old `Park` root and builds all geometry again at runtime | `unity/Assets/Scripts/Runtime/ParkView.cs:35-44` |
| 6 | `HarborKit.Bind(park, night)`; `OwnsDiamond = HarborPostcard.Owns(park.Id)`; the kit GameObject is set inactive for a non-Harbor park | `ParkView.cs:105-115`; `HarborKit.cs:86-99`; `src/GrandSluggers.Sim/HarborPostcard.cs:10,32-33` |
| 7 | If the kit owns the diamond: `ParkView` draws only `Water`; the kit draws all of the field | `ParkView.cs:118-119`; `HarborKit.cs:190-203,481-497` |
| 8 | If not: `ParkView` draws `Water`, `Outfield`, `Infield`, `Mound`, `FoulLines`, `Bags`, `Fence`, then one per-park dress method | `ParkView.cs:120-173` |
| 9 | `Hazards(park)` for every park | `ParkView.cs:174,1244-1284` |
| 10 | `BallView` is built under the park root | `ParkView.cs:176-178` |

### 1.2 Scene

| Fact | Evidence |
| --- | --- |
| One scene only: `Assets/Scenes/HarborDiamond.unity` | `unity/Assets/Scenes/` (one file); `unity/Assets/Editor/ProjectBootstrap.cs:14,61-62` |
| Scene objects: `Sun`, `Main Camera`, `MatchDirector`, `HarborKit` with placed children (`DirtPad`, `HomeDirt`, `DirtDiamond`, `HomePlate`, `HomePoint`, `BoxL/R`, `FoulL/R`, `Mound`, `Rubber`, `ShotPlate/Mound/Diamond/Throw` + `Look`) | `unity/Assets/Scenes/HarborDiamond.unity:133,232,367,414,487-1049` |
| All validation gates open this scene and say "Harbor" | `unity/Assets/Editor/ValidationGate.cs:14,48-51`; `docs/validation.md:30-45` |

### 1.3 Builder classes: where they live

| Class | Side | Role | Evidence |
| --- | --- | --- | --- |
| `ParkView` | Unity | The one park builder. Fallback diamond, five per-park dress methods, hazards, fireworks, follow-spot | `unity/Assets/Scripts/Runtime/ParkView.cs:6-1323` |
| `HarborKit` | Unity | Harbor as placed anchors + runtime dress; drops kit FBX meshes; scoreboard digits; night floods; fireworks | `unity/Assets/Scripts/Runtime/HarborKit.cs:11-1497` |
| `Look` | Unity | Materials (`Lit`, `Toon`, `Unlit`), primitives, lighting rigs, textures | `unity/Assets/Scripts/Runtime/Look.cs:135-497` |
| `Colors` | Unity | Palette literals | `unity/Assets/Scripts/Runtime/Colors.cs:7-29` |
| `ArtBinder` | Unity | Loads kit FBX / named kit mesh | `unity/Assets/Scripts/Runtime/ArtBinder.cs:180-299` |
| `ParkDiamond` | Sim | Shared ground numbers: dirt, mound, stripes, foul chalk, poles, track, bag visual, `StandY`, `OnDirt` | `src/GrandSluggers.Sim/ParkDiamond.cs:8-438` |
| `HarborWall` | Sim | Wall ground loop, heights (D15), foul wrap; also read by the flight for every park | `src/GrandSluggers.Sim/HarborWall.cs:8-307`; `src/GrandSluggers.Sim/FieldBounds.cs:105-109,186` |
| `HarborDugout` | Sim | Pit position, rail, stairs, lawn holes, camera clearance | `src/GrandSluggers.Sim/HarborDugout.cs:12-62` |
| `HarborStands` | Sim | Bowl rows, crowd person size | `src/GrandSluggers.Sim/HarborStands.cs:8-66` |
| `HarborInfield` | Sim | Aliases of `ParkDiamond` constants + pit check | `src/GrandSluggers.Sim/HarborInfield.cs:8-26` |
| `HarborPostcard` | Sim | `Owns`, wall pieces, ad size, town and scoreboard offsets, `ReadsFromField` | `src/GrandSluggers.Sim/HarborPostcard.cs:8-116` |
| `HarborKitPaint` | Sim | Kit mesh name + material name to fill table | `src/GrandSluggers.Sim/HarborKitPaint.cs:8-92` |
| Crystal / Funfair / Rooftop / Canopy / Ember builders | — | No classes. They are private methods of `ParkView` | `ParkView.cs:530,613,805,871,984` |
| `WorldView` / `Palette` (Raylib debug sandbox) | `GrandSluggers.Play` | A third, separate park drawing | `src/GrandSluggers.Play/WorldView.cs:9-80`; `src/GrandSluggers.Play/Palette.cs:70-76` |

### 1.4 What each non-Harbor park draws today

All geometry is Unity primitives (`Cube`, `Cylinder`, `Sphere`, `Capsule`) with `Look.Lit` / `Look.Unlit` materials. No mesh, no texture other than the three shared JPGs (grass, dirt, crowd). All positions are code literals.

| Park | Shared fallback diamond | Own dress (all in `ParkView.cs`) | Night |
| --- | --- | --- | --- |
| crystal-rink | Yes (`:125-133`). Ground color `Colors.Ice`, no grass texture, smooth 0.72 (`:83-95`). Ice-blue dirt (`:96-97`). Fence cyan, cap white, poles `Colors.Royal` (`:233-247`) | `CrystalGarden` `:530-552`: glass boards 7 ft inside the fence (`:554-568`), ice benches, pavilions, 3 crowd cards, `FrozenFountain` `:570-581`, `RoyalPalace` `:583-595`. No warning track | `RigIceGardenNight` + `FollowSpot` spot light on the ball (`:597-611`, `:470-477`) |
| funfair-park | Yes. Default grass. Fence alternates red / cream (`:236,241,256`) | `FunfairGrounds` `:613-636`: `WarningTrack`, cloth backstop, benches + awnings, 3 `Tent`, crowd cards, `StripedPoles` `:670`, `FerrisWheel` `:690` (static), `FunfairBooths` `:726`, `FunfairTrain` `:743` (static, at spray 18 deg, not from the `train` hazard row) | `RigCarnivalNight` + 3 `ChomperMouth` from the Sim literal `ParkHazards.FunfairChompers` (`:773-803`) |
| rooftop-city | Yes. Ground grey, no grass texture (`:86,94-95`); grey dirt (`:98-99`); fence grey, poles `Colors.Goldrush` | `RooftopDeck` `:805-833`: `WarningTrack`, steel backstop, benches + neon awnings, roof stands, crowd cards, **2 extra `AcUnit` at code-literal spots that are not in `data/parks`** (`:829-830`), `RooftopSkyline` `:835-849` | Same `RigNeon` day and night (`:73`); night adds one point light (`:861-869`) |
| canopy-yard | Yes. Ground dark green with grass texture (`:85`); brown dirt (`:100-101`); fence green | `CanopyGrounds` `:871-898`: `WarningTrack`, vine backstop, log benches, groves, crowd cards, 2 `VineWall` at literal (+-96, 210) (`:893-894`), 7 `JungleTree` past CF (`:895-896`) | Same `RigCanopy` day and night (`:74`); night adds 12 static firefly spheres (`:965-982`) |
| ember-keep | Yes. Ground from `surface == "ash"` (`:84`); default dirt color; fence `Colors.EmberFire` (`:234`) | `EmberCourtyard` `:984-1010`: `WarningTrack`, iron backstop, stone benches, keep stands, crowd cards, `KeepCastle` `:1012`, 2 `Brazier`, courtyard point light | `RigCourtyardNight` + 4 braziers + one point light (`:1107-1123`) |
| (unknown id) | Yes | `Stands(ice, ash)` `:349-364` with `KeepWall` when ash | `Look.SetupLighting(cam, sky)` only (`:80`) |

### 1.5 Every branch on park id or `surface` (presentation and nearby)

`surface` is read in five places, all presentation (matches #713):

| # | Place | Evidence |
| --- | --- | --- |
| 1 | `ParkView.BuildPark` `ice` | `unity/Assets/Scripts/Runtime/ParkView.cs:46` |
| 2 | `ParkView.BuildPark` `ash` | `unity/Assets/Scripts/Runtime/ParkView.cs:47` |
| 3 | Play sandbox `WorldView.DrawPark` `ice` | `src/GrandSluggers.Play/WorldView.cs:11` |
| 4 | Play sandbox `WorldView.DrawPark` `ash` | `src/GrandSluggers.Play/WorldView.cs:12` |
| 5 | Play sandbox `Palette.SkyOf` switch | `src/GrandSluggers.Play/Palette.cs:70` |
| (validator, not a read for behavior) | `Surfaces = {grass, dirt, ice, ash}` | `src/GrandSluggers.Sim/ContentValidation.cs:12,309` |

Uses of those two booleans in `ParkView`: sky `:55`, ground color `:83-84`, water `:88-89`, `skipGrass` and smoothness `:94-95`, water smoothness `:103`, `Fence(park, ash)` `:133,234`, `Stands(ice, ash)` `:172,351,360`. `dirt` and `grass` surfaces are never read; Rooftop and Canopy get their ground from the park id.

Branches on park id:

| Place | Branch | Evidence |
| --- | --- | --- |
| `ParkView.BuildPark` | six id booleans | `unity/Assets/Scripts/Runtime/ParkView.cs:48-53` |
| `ParkView.BuildPark` | lighting rig `if` chain | `ParkView.cs:58-80` |
| `ParkView.BuildPark` | ground / water / dirt material | `ParkView.cs:83-102` |
| `ParkView.BuildPark` | create `HarborKit` if missing and Harbor | `ParkView.cs:108-113` |
| `ParkView.BuildPark` | dress method `if` chain | `ParkView.cs:134-173` |
| `ParkView.Fence` | wall / cap / pole colors per id | `ParkView.cs:229-247,256` |
| `HarborKit.Bind` | `HarborPostcard.Owns` | `HarborKit.cs:90`; `HarborPostcard.cs:32-33` |
| `HarborKit.DropMesh` | literal `"harbor-diamond"` | `HarborKit.cs:1354` |
| `ArtBinder.LoadParkKit` | returns null unless Harbor | `unity/Assets/Scripts/Runtime/ArtBinder.cs:190-191` |
| `ArtBinder.ParkKitFbx` | Harbor fallback path; file name `harbor-kit.fbx` for every slot | `ArtBinder.cs:288-299` |
| `CarnivalFront.HarborIsTheProduct`, `Gimmick` | per-id copy switch | `src/GrandSluggers.Sim/CarnivalFront.cs:225-238` |
| `HudView.Field` | "Harbor is the slice." for a non-Harbor park | `unity/Assets/Scripts/Runtime/HudView.cs:168-169` |
| `PresetTeams` home park per captain | id switch | `src/GrandSluggers.Sim/Teams.cs:39-44` |
| `ParkHazards.ChompFly` | `park.Id != "funfair-park"` (sim rule on an id) | `src/GrandSluggers.Sim/Fielding.cs:679` |
| `Training.ParkId`, `Challenge`, `Match.Slice` fallbacks | Harbor literal | `src/GrandSluggers.Sim/Training.cs:18`; `Challenge.cs:69`; `Match.cs:154,184` |
| Play sandbox | `canopy-yard` | `src/GrandSluggers.Play/WorldView.cs:13`; `Palette.cs:74` |

---

## 2. The shared "diamond kit"

`docs/parks.md:22,31,39,48,67` says every park uses the "same diamond kit (bags, mound, foul lines, fence)", and `docs/parks.md:86` says the foul wraps are "the shared diamond kit". In code there are two different diamonds.

| Part | Harbor path (`HarborKit`) | Non-Harbor path (`ParkView` fallback) | Same numbers? |
| --- | --- | --- | --- |
| Ground | Striped lawn columns with dugout holes, `ParkDiamond` stripe numbers (`HarborKit.cs:770-810`) | One 620 x 620 cube at z 190 (`ParkView.cs:123`) | No |
| Infield dirt | Solid mesh from `ParkDiamond.OuterVerts` / `InnerOnRay`: paths, pads, back arc, grass Y inside (`HarborKit.cs:425-470`) | 150 x 150 square `DirtPad` + 4 cylinders, radius 18 / 11 (`ParkView.cs:181-188`) | No (`ParkDiamond.BagPadR` is 12, `:26`) |
| Bags | Kit mesh `bag` at `ParkDiamond.BagVisual` (inside the foul line), 4 ft, 45 deg (`HarborKit.cs:205-225`) | 2.4 ft axis-aligned cubes at `Diamond.First/Second/Third` (on the line) (`ParkView.cs:209-216`) | No |
| Home plate + boxes | Kit mesh `home-plate`, chalk boxes from `HomeSet` (`HarborKit.cs:227-298`) | Two cubes for the plate; **no batter's boxes** (`ParkView.cs:219-225`; boxes only in `HarborDiamondSkin` `:378-394`, which is Harbor-only and dead while the kit exists) | No |
| Mound | Kit mesh `mound` or sphere at `ParkDiamond.MoundR/H`; rubber at `RubberY` 1.02 (`HarborKit.cs:526-540`) | Cylinder radius 10, height 1.15; rubber at y 1.08 (`ParkView.cs:190-195`). Actors still stand at `ParkDiamond.StandY` (`ActorDirector.cs:209`) | No |
| Foul lines | `ParkDiamond.FoulWidth` 4 in, length to the park pole (`HarborKit.cs:499-524`) | 0.95 ft wide, 200 ft long cubes centred at (+-112, 112) (`ParkView.cs:197-207`) | No |
| Foul poles | `ParkDiamond.FoulPole` + screen (`HarborKit.cs:594-616`) | Same `ParkDiamond` numbers (`ParkView.cs:259-272`) | **Yes** |
| Outfield fence | Ramp prisms along `HarborWall.Loop`, top = `HarborWall.OutfieldHeight(park)` (`HarborKit.cs:811-873`) | 37 boxes from -48 to +48 deg, 14 ft wide, top = `park.FenceHeightFt`, radius `AtBatResolver.FenceAt` (`ParkView.cs:249-258`) | Height and radius yes; shape no |
| Foul rail + round backstop (the wall the flight clips for every park, `FieldBounds.cs:105-109,186`) | Drawn: hip rail 4.2 ft, wraps dugouts and home (`HarborKit.cs:834-849`; `HarborWall.cs:21-32`) | **Not drawn.** Each park draws a flat backstop cube at z -24 and benches at (+-42, 22) | No |
| Warning track | Annulus mesh along `HarborWall.TrackInner/Outer` (`HarborKit.cs:542-581`) | Boxes along `ParkDiamond.TrackMid` (`ParkView.cs:275-288`); Crystal has none | Width yes; shape no |

Harbor-named but shared (used for every park):

| Symbol | Shared use | Evidence |
| --- | --- | --- |
| `HarborWall.Loop`, `FoulWall`, `HomeZ`, `HipHeight` | Flight boundary for every park | `src/GrandSluggers.Sim/FieldBounds.cs:7,105-109,126-127,186` |
| `HarborWall.HipHeight` | Park data validator floor for `fenceHeightFt` | `src/GrandSluggers.Sim/ContentValidation.cs:317-318` |
| `HarborWall.OutfieldHeight(park)` | `ParkDiamond.ScreenFacesFair` | `src/GrandSluggers.Sim/ParkDiamond.cs:89-90` |
| `HarborDugout.InPitHole` | `ParkDiamond.LawnCovers` for any park | `src/GrandSluggers.Sim/ParkDiamond.cs:414` |
| `HarborDugout.RailAt`, `HarborKit.DugoutFasciaY` | `StarMeter` pips for every park (they stand where the Harbor dugout fascia is) | `unity/Assets/Scripts/Runtime/StarMeter.cs:24-35`; `MatchDirector.cs:213,562` |
| `HarborDugout.CameraClears` | `StillPose` | `src/GrandSluggers.Sim/StillPose.cs:159` |
| `harbor-kit.fbx` file name | `ArtBinder.ParkKitFbx` appends it to any park slot | `unity/Assets/Scripts/Runtime/ArtBinder.cs:297-298` |
| Import post-processor | Applies to any path with `Art/Parks/` | `unity/Assets/Editor/SharedRigImport.cs:21,135-144` |

Harbor-only meshes / materials:

| Item | Evidence |
| --- | --- |
| Kit FBX meshes authored: `dugout-1b`, `dugout-3b`, `wall-panel`, `fan-stand`, `fan-sit`, `home-plate`, `bag`, `mound`, `foul-pole`, `warning-track`, `infield-dirt` (+ `wall-ring` function exists, not called in `build()`) | `tools/blender/harbor_kit.py:592-619,303-345` |
| Kit meshes that `HarborKit` drops: `bag`, `home-plate`, `mound`, `dugout-1b/3b`, `fan-sit`, `fan-stand` | `HarborKit.cs:218,232,531,653,1108,1131,1167` |
| Kit meshes authored but not used by Unity: `wall-panel`, `foul-pole`, `warning-track`, `infield-dirt` (wall ring off: `DropAuthoredRing = false`) | no `DropMesh` call; `src/GrandSluggers.Sim/HarborWall.cs:34-35` |
| Kit materials are nine code-literal toon fills chosen by `HarborKitPaint.For(mesh, materialName)` | `HarborKit.cs:1371-1396`; `HarborKitPaint.cs:24-40` |
| Scoreboard with live seven-segment digits, town, bowl stands with `fan-sit` crowd, wall ads, night floods | `HarborKit.cs:917-982,1179-1201,984-1083,820-870,1216-1227` |

Dead code: the old Harbor primitive dress in `ParkView` (`HarborDiamondSkin`, `Backstop`, `Dugouts`, `HarborDugoutsPlus`, `HarborWallDress`, `HarborScoreboard`, `HarborBleachers`, `HarborTown`, `HarborNightHook`) runs only when `harbor && !placed` (`ParkView.cs:134-149`); `ParkView` makes a kit when Harbor has none (`:108-113`), so that path does not run.

---

## 3. Art slots for parks

### 3.1 Catalog rows

| File | Park-related content | Evidence |
| --- | --- | --- |
| `data/art/parks.json` | `kits[]`: six rows `{id, slot, placed}`; only `harbor-diamond` is `placed: true` | `data/art/parks.json:1-10` |
| `data/art/folders.json` | Six `Assets/Art/Parks/{id}` folders, `Assets/Art/Materials`, `VFX`, `Audio` | `data/art/folders.json:3-16` |
| `data/art/materials.json` | Five slots: `toon-body`, `dirt`, `grass`, `chalk`, `crowd` (shader `ToonFill`) | `data/art/materials.json:1-9` |
| `data/art/vfx.json` | One park VFX slot: `fireworks` (`kind: park`) | `data/art/vfx.json:18` |
| `data/art/audio.json` | `crowd-bed` (authored), `crowd-swell`; no park ids | `data/art/audio.json:8-9` |

Schema: `ParkKitSlot(Id, Slot, Placed)` (`src/GrandSluggers.Sim/Art.cs:18,389-394`). There is no per-mesh slot list, no material list, no texture list, no backdrop / sky / light / audio / hazard-actor field. There is no per-park slot structure beyond the one folder path.

### 3.2 Validators

| Check | Evidence |
| --- | --- |
| `ArtCatalog.Validate`: every `content.Parks` key has a kit row. That is the only park check | `src/GrandSluggers.Sim/Art.cs:237-241` |
| `cli art` prints `PARKS n kit slots (m placed)` then runs `ArtCatalog.Validate` + `DebugProtocol` + `DualStills` + `DccStages` validators | `src/GrandSluggers.Cli/Program.cs:173-196` |
| Not checked by `cli art`: that the kit FBX exists, that the player copy matches, mesh names, `placed` meaning, materials slots, vfx file for `fireworks` | `Art.cs:106-278` (rig / clips / extras have these checks; parks do not) |
| The kit FBX checks live in a test instead: both copies exist, > 10 KB, same length, ASCII contains six mesh names | `src/GrandSluggers.Sim.Tests/HarborPostcardTests.cs:121-140` |
| `ArtCatalogTests.EveryParkHasAKitSlotAndHarborIsPlaced`: exactly one placed kit | `src/GrandSluggers.Sim.Tests/ArtCatalogTests.cs:220-227` |
| Unity menu **Validate Art Rails** creates missing folders and runs the same validator | `unity/Assets/Editor/ArtRailsValidate.cs:12-33` |
| Park JSON content validator: required id / name / faction, known surface, positive fences, wind, `fenceHeightFt > HipHeight`, `nightContactWindowMul`, hazard type in a fixed set of 11, finite x / z, radius > 0 except `train` | `src/GrandSluggers.Sim/ContentValidation.cs:20-24,303-337` |

### 3.3 Folder layout under `unity/Assets`

| Path | Content today |
| --- | --- |
| `Assets/Art/Parks/harbor-diamond/` | `harbor-kit.fbx`, `DROP.txt`, `.gitkeep` |
| `Assets/Art/Parks/{crystal-rink,funfair-park,rooftop-city,canopy-yard,ember-keep}/` | `.gitkeep` only |
| `Assets/Resources/Art/Parks/harbor-diamond/harbor-kit.fbx` | Player copy (loaded by `Resources.Load`) |
| `Assets/Art/Materials/` | Empty |
| `Assets/Art/VFX/`, `Assets/Art/Audio/` | `DROP.txt` only |
| `Assets/Resources/Art/tex-grass.jpg`, `tex-dirt.jpg`, `tex-crowd.jpg` | The only park textures; loaded by name in `Look.cs:27-29,499-506`; not in any catalog |

`harbor_kit.py` has `--out` and `--clay` only; it has no `--resources` flag, so the player copy is a manual copy (`tools/blender/harbor_kit.py:664-672`; compare the character scripts in `tools/blender/README.md:33-39`).

### 3.4 Missing-asset fallback

| Case | Fallback | Evidence |
| --- | --- | --- |
| Kit FBX missing or mesh name missing | `ArtBinder.LoadParkMesh` gives null; `HarborKit` keeps its primitive (bag cube, plate cubes, sphere hill, `BuildDugout`, capsule fans) | `ArtBinder.cs:242-267`; `HarborKit.cs:218-224,232,531-535,626-630,1108-1113` |
| Non-Harbor park | `LoadParkKit` gives null by id; `ParkView` primitives | `ArtBinder.cs:190-191` |
| Texture missing | `Look.Load` gives null; `Look.Lit` uses a 1 x 1 white map | `Look.cs:142,151-166,499-506` |
| Materials slots | No loader reads `ArtCatalog.Materials`; all materials are made in code | no reference to `.Materials` in `unity/Assets`; `Art.cs:53,319-320` |
| `fireworks` VFX slot | Fireworks are sphere primitives in code; the slot is not loaded by the park code | `HarborKit.cs:1250-1275`; `ParkView.cs:483-512` |

---

## 4. Blender pipeline for the field

| Question | Answer | Evidence |
| --- | --- | --- |
| Scripts for parks / field | One: `tools/blender/harbor_kit.py` (681 lines). No script for any other park | `tools/blender/` listing |
| What it makes | 11 named meshes in one FBX (see 2); 15 Principled materials with literal colors | `harbor_kit.py:592-619` |
| Inputs | None. It does not read `data/parks`, `data/art`, or `data/rules`. All numbers are Python constants with "keep in sync with ...cs" comments | `harbor_kit.py:28-65` |
| Outputs | `--out` FBX (`axis_forward=-Z`, `axis_up=Y`, mesh only); `--clay` renders four tiles (`home-plate`, `bag`, `mound`, `fan-stand`) into `harbor-kit.png` | `harbor_kit.py:62,622-661` |
| How dimensions stay in sync with the sim | By string-match tests on the Python source for a few constants: `PATH_Y`, `PATH_THICK`, `MOUND_R`, `MOUND_H`, `BAG_SIZE`, the plate inches, and the chalk material calls | `src/GrandSluggers.Sim.Tests/ParkDiamondTests.cs:85-100,126-134`; `HomeSetTests.cs:42-51`; `HarborKitPaintTests.cs:87-97` |
| Constants that have drifted from the sim (no test) | `WALL_H = 26.0` (sim: `fenceHeightFt` 12); `PATH_WIDTH = 8.0` (sim 10); `HOME_PACKED_R = 16.0` (sim 18); `DUGOUT_X/Z = 70/40` (sim 64.7 / 8.84); `DUGOUT_PAD = 14` (sim 18); `WRAP_SEGS = 120` (sim 108); `HOME_RADIUS = 34` (sim `HomeZ` -36) | `harbor_kit.py:43-48,57,61,42` vs `ParkDiamond.cs:17,46`; `HarborDugout.cs:15,18`; `HarborWall.cs:16,23,24` |
| Why the drift does not show | The wall, track, infield dirt, poles and the lawn are built in C# from the Sim classes; the matching kit meshes are not dropped | `HarborKit.cs:425-470,542-616,811-873`; `HarborWall.cs:35` |
| Wall height D15 | `HarborKit.DressWall` reads `HarborWall.OutfieldHeight(park)` = `park.FenceHeightFt`; ads clamp to the face by `HarborPostcard.OnWallFace`; `HarborWallTests` asserts drawn top = flight fence top for all six parks | `HarborKit.cs:816,858-867`; `HarborWall.cs:30,177-191`; `HarborPostcard.cs:95-96`; `HarborWallTests.cs:16-41` |
| Fence arc | `AtBatResolver.FenceAt(park, spray)` drives both builders and `ParkDiamond.FoulPole / TrackMid` | `ParkView.cs:253`; `HarborWall.cs:123-128`; `ParkDiamond.cs:353-368` |
| Known literal in the wall | Rail taper starts at a literal 95 ft (`hipZ`, `flareStart`), which no overlay can move; noted as a gap | `HarborWall.cs:114,183`; `HarborWallTests.cs:43-46` |
| DCC stages | Five stages. The schema has exactly two lanes, named `character` and `harbor`; the validator requires keys `id, n, kind, character, harbor` and matches the Harbor lane to fixed script / flag / checkpoint tables. Harbor skips `motion` | `data/agent/dcc-stages.json:5-89`; `src/GrandSluggers.Sim/DccStages.cs:59-92,255-256,299,310,405-410` |
| Do stages cover kit meshes? | Yes, for the Harbor kit only. Blocking and fill share one script and one checkpoint PNG | `data/agent/dcc-stages.json:16-21,33-38` |
| Blender start rail | `tools/blender-run.sh` checks Metal before launch | `tools/blender/README.md:19-21`; protocol row `blender-no-metal-device-at-startup` |
| Agent rule | "Do not start a second park pipeline." Numbers stay in Sim and are copied into Python; "A test should catch drift." | `.grok/rules/harbor-art.md:9-13` |

---

## 5. Hazard presentation

Hazards are drawn by `ParkView.Hazards`, a `switch` on `h.Type` (`ParkView.cs:1244-1284`). There is no `train` case. Chompers are not in `data/parks`; they are a Sim literal (`src/GrandSluggers.Sim/Fielding.cs:670-675`) drawn by `FunfairNightHook`.

| Type | Actor | Drawn size vs sim radius | Sim effect | Evidence |
| --- | --- | --- | --- | --- |
| `freeze_volume` | `FreezeStatue`: pedestal + capsule body, 3 poses, **`FrostRing` disc diameter = 2 x radius** | Disc = sim radius. Pedestal clamp 2.0-3.4 ft | Slow when the landing point is in the disc | `ParkView.cs:1160-1204` (`:1173`); `Fielding.cs:657-668` |
| `warp_pipe` | `WarpCan`: tilted can, lip, dark well, colored by tag, 1/2/3 pips for A/B/C | `r = max(2.4, radius)`; lip diameter 2.2 r. **Sim reach is `radius + pipeReachPadFt` (4 + 8 = 12 ft). No disc is drawn; the pad is not drawn** | Grounder landing in reach exits another can | `ParkView.cs:1206-1242`; `Fielding.cs:685-703`; `data/rules/fielding.json:276` |
| `barrel` | `BarrelCannon`: wood barrel, bands, mouth, pips by tag (no tag color) | `r = max(2.2, radius)`; sim reach 5 + 8 = 13 ft (test names 13.00). No disc | Same rule as warp | `ParkView.cs:937-963`; `CompactGeometryTests.cs:527` |
| `lava_pit` | `LavaPit`: stone rim, lava well, glow disc, point light | Rim diameter 2.2 r, well 1.7 r, `r = max(3, radius)`. Near the sim radius | Slow | `ParkView.cs:1054-1067` |
| `fire_breath` | `FireStatue(breath: true)`: statue + flame cylinder and cone | Flame scales with radius and with `EmberNightFireMul` at night. **No ground disc for the slow radius** | Slow; radius x 1.6 at night | `ParkView.cs:1069-1100` (`:1094`); `Fielding.cs:662-663` |
| `statue` | `FireStatue(breath: false)` | Radius sets only the pedestal | None in the sim | `ParkView.cs:1273-1275`; no Sim reader |
| `billboard` | `StarBillboard`: fixed 26 x 18 frame, star, neon, light | **Radius ignored** | Landing in radius 12 gives a team star | `ParkView.cs:1125-1142`; `Fielding.cs:708-716`; `Match.cs:1603-1608` |
| `ac_unit` | `AcUnit`: fixed 8 x 4.4 x 8 box | **Radius ignored**; 2 more units at code literals not in data | None in the sim (docs say carom) | `ParkView.cs:1144-1158,829-830`; no Sim reader |
| `climb_wall` | `ClimbWall`: `VineWall` width `max(28, radius x 0.7)`, height 14, at (0, 370) | Not tied to the fence arc or `fenceHeightFt` | Presence enables Clamber | `ParkView.cs:928-935`; `Fielding.cs:718-720` |
| `tree` | `JungleTree(p, radius)` inside the outfield | Trunk = 0.42 radius | None in the sim | `ParkView.cs:912-926` |
| `train` | Not in the switch. A static train is drawn by `FunfairTrain` at spray 18 deg | — | None in the sim | `ParkView.cs:743-765`; `data/parks/funfair-park.json` hazard `{type: train, z: 310, periodSec: 18}` |
| chomper (night) | `ChomperMouth`: stem, head, jaw, teeth, red point light; name uses tag L/C/R | `r = max(3.2, radius x 0.38)`; sim radius 16-18. No disc | Fly landing in radius is an out | `ParkView.cs:786-803`; `Fielding.cs:670-683` |

Tells, VFX, audio:

| Tell | State | Evidence |
| --- | --- | --- |
| Fielder frozen / slow | The sim sets `FieldingPreview.Frozen` from the **landing point** and uses it for chase speed and drop roll. Unity never reads it. No body tint, no VFX, no sound | `Fielding.cs:80-81,402-405`; `LivePlaySystem.Field.cs:1111,1286`; no `.Frozen` in `unity/Assets/Scripts` |
| Warp | `Warped` is set in the preview; the only output is a caption string. Unity never reads `Warped`; there is no ball effect at the can | `Fielding.cs:68-77`; `Match.cs:1556` |
| Chomp | Caption "A chomper ate it!" only | `Match.cs:1583-1584` |
| Billboard star | Caption "Billboard STAR!" only | `Match.cs:1607` |
| `LiveEvent` | No hazard event exists (`WallCarom` is the only park-related event) | `LivePlaySystem.Field.cs:82-112`; `InPlayDirector.cs:160-199` |
| VFX / audio slots for hazards | None in `data/art/vfx.json` or `audio.json` | `data/art/vfx.json:1-24`; `data/art/audio.json:1-21` |
| Tutorial | Park hazard lessons are blocked (#37) | `docs/tutorials.md:194,202` |

---

## 6. Lighting, sky, day / night

| Topic | State | Evidence |
| --- | --- | --- |
| Where time of day lives | `MatchDirector.Night` bool; toggled on the title and on the field pick by `Controls.NightToggle` (key N / R3); passed to `Match.Exhibition(..., Night, ...)`; read back as `_match.Night` | `MatchDirector.cs:30,647`; `FlowDirector.cs:93-97,216-220`; `Controls.cs:139,491` |
| `nightOnly` / `dayOnly` | Present in all six park JSON files (all `false`). **No code reads them**; the `Park` record has no such members | `data/parks/*.json`; `src/GrandSluggers.Sim/Models.cs:186-198`; grep finds no reader |
| Sky | `Camera.clearFlags = SolidColor` + `backgroundColor`. No skybox | `Look.cs:298-299` |
| Rig per park | `if` chain on id in `ParkView`; one `Look.Rig*` method per look | `ParkView.cs:56-81` |
| Rooftop and Canopy at night | Same rig as day | `ParkView.cs:73-74` |
| Fireworks | `AtBatDirector` calls `BurstFireworks` on a homer at night for any park; only the Harbor kit has a `Fireworks` root, so only Harbor shows them. 28 sphere primitives, literal colors and velocity | `AtBatDirector.cs:538-539`; `ParkView.cs:483-491`; `HarborKit.cs:1250-1275` |
| Harbor night floods | Four point lights, literals | `HarborKit.cs:1216-1227` |
| Crystal follow-spot | Spot light follows the ball at night, literals | `ParkView.cs:470-477,597-611` |
| Post-processing | `DefaultVolumeProfile.asset` exists (Unity default). No script reads or sets a `Volume` | `unity/Assets/DefaultVolumeProfile.asset`; no match in `unity/Assets/Scripts` |
| Data-driven? | No. `data/feel/table.json` has no park, light, sky, fog or night key. `data/art` has no light slot | grep of `data/feel/table.json` |
| Toon shader | `Look.Toon` falls back to URP Lit because `ToonFill` loses Z to Lit grass | `Look.cs:179-187` |

Lighting literals (`unity/Assets/Scripts/Runtime/Look.cs`); each rig sets sky color, fog color / start / end, three ambient colors, and Sun / Fill / Rim color, intensity, euler:

| Rig | Lines | Sky | Fog start-end | Sun intensity |
| --- | --- | --- | --- | --- |
| `SetupLighting` (base) | `:296-314` | caller | 180-620 | 1.15 |
| `RigAfternoon` (Harbor day) | `:317-331` | (0.52, 0.70, 0.88) | 380-820 | 1.55 |
| `RigIceGarden` | `:334-348` | (0.56, 0.70, 0.82) | 200-700 | 1.12 |
| `RigCarnival` | `:351-365` | (0.78, 0.50, 0.58) | 220-720 | 1.38 |
| `RigNeon` (Rooftop day + night) | `:368-382` | (0.22, 0.16, 0.38) | 180-640 | 1.05 |
| `RigCanopy` (day + night) | `:385-399` | (0.38, 0.58, 0.42) | 160-580 | 1.18 |
| `RigCourtyard` | `:402-416` | (0.28, 0.14, 0.16) | 160-620 | 1.22 |
| `RigHarborNight` | `:419-433` | (0.07, 0.10, 0.20) | 200-700 | 0.42 |
| `RigIceGardenNight` | `:436-450` | (0.03, 0.04, 0.08) | 70-380 | 0.06 |
| `RigCarnivalNight` | `:453-467` | (0.12, 0.06, 0.14) | 180-640 | 0.55 |
| `RigCourtyardNight` | `:470-484` | (0.12, 0.04, 0.06) | 140-560 | 1.55 |

Other palette literals: `Colors.cs:7-29`; ground / water / dirt colors `ParkView.cs:83-103`; fence colors `ParkView.cs:233-247`; every per-park dress color is inline in its method.

---

## 7. Cameras and HUD vs park

| Topic | State | Evidence |
| --- | --- | --- |
| Shot table | 16 named shots, one global list. No park key. No per-park file | `data/feel/shots.json` (ids: title, plate, mound, pitch, diamond, diamond-fly, diamond-line, diamond-grounder, tag, throw, smash, field, select, lineup, result, replay) |
| Runtime cameras read JSON only; the kit's placed shot transforms are not used (`Placed` always false) | `unity/Assets/Scripts/Runtime/CameraDirector.cs:63-73`; `HarborKit.cs:360-397` |
| Live cameras | `PlayCamera.LiveFraming` follow-cams translate a shot onto the subject; "Wall and live fly/homer are follow-cams, not a second JSON park still" | `src/GrandSluggers.Sim/PlayCamera.cs:264-300`; `CameraDirector.cs:89-101` |
| Shots tuned to Harbor distances | `field` pos (16, 18, 90) target (0, 14, **380**): tested against Harbor only (`ReadsFromField` needs target Z >= 250 and the Harbor wall / board to subtend 1.5 deg). `result` target z 110, `replay` target z 180 are fixed | `data/feel/shots.json`; `HarborPostcard.cs:105-115`; `HarborPostcardTests.cs:16-21` |
| CF fence range the `field` shot meets | 378 (Canopy) to 408 (Ember) | `docs/parks.md:93-100` |
| Camera clearance | `StillPose` uses `HarborDugout.CameraClears` | `src/GrandSluggers.Sim/StillPose.cs:159` |
| HUD | `BroadcastHud` rects are screen fractions; no park input | `src/GrandSluggers.Sim/BroadcastHud.cs:92` (grep finds no park or fence term) |
| World-space HUD tied to Harbor | `StarMeter` pips on the Harbor dugout fascia for every park; in-park scoreboard digits only in the Harbor kit (`SetScore` guarded by `OwnsDiamond`) | `StarMeter.cs:24-35`; `MatchDirector.cs:311-312` |

Field pick:

| Topic | State | Evidence |
| --- | --- | --- |
| Park list | Code literal, six ids, fixed order; wraps left / right | `src/GrandSluggers.Sim/ExhibitionPick.cs:10-11,46-55` |
| What is selectable | All six, day or night. No lock | `FlowDirector.cs:208-229` |
| Locked / unlock logic (#37) | None in code. Haunt Manor, Cruise Deck, Playroom, Toy Box exist only in the doc | `docs/parks.md:50-76`; grep finds no unlock term in `ExhibitionPick`, `CarnivalFront`, `FlowDirector` |
| Postcard / thumbnail | None. The "postcard" is the live 3D park, rebuilt on each stick move, seen through the `field` shot | `FlowDirector.cs:192-215` |
| HUD copy | Park name, sky gag, one gimmick line per id, "Harbor is the slice." for non-Harbor | `unity/Assets/Scripts/Runtime/HudView.cs:162-172`; `src/GrandSluggers.Sim/CarnivalFront.cs:225-238` |
| Default home park per captain | id switch | `src/GrandSluggers.Sim/Teams.cs:39-44` |
| Training / Challenge | Always Harbor | `Training.cs:18`; `FlowDirector.cs:82,274`; `Challenge.cs:69` |

---

## 8. Look gates and process rails

| Rail | What it supports for a PARK today | Evidence |
| --- | --- | --- |
| `docs/screenshot-gate.md` | Rubric rows are Harbor shots and characters. No row for a non-Harbor park, no park fail-if list. `field` is not in the still table | `docs/screenshot-gate.md:15-33,48-51` |
| `data/agent/dual-stills.json` | Four kinds: `body`, `extras`, `takes`, `harbor-kit`. Trigger paths include only `unity/Assets/Art/Parks/harbor-diamond/`. The Harbor in-game pair is `plate.png`, `mound.png`, `diamond-grounder.png` | `data/agent/dual-stills.json:5-45` |
| `DualStills` validator | Requires exactly those kind ids and triggers | `src/GrandSluggers.Sim/DualStills.cs:17-34` |
| `tools/dcc-still.sh harbor` | Runs `harbor_kit.py --clay`, copies `harbor-kit.png` to `scratchpad/stills/dcc-harbor-kit.png`. Four tiles only (plate, bag, mound, fan) | `tools/dcc-still.sh:36-40,76-81`; `harbor_kit.py:62,622-641` |
| `tools/still-gate.sh` | Writes a fixed request (8 shots, rio vs ashlord), clicks the Unity menu, copies PNGs. No park argument, no night argument | `tools/still-gate.sh:11-13` |
| `StillRequest` schema | `shots, swingCaptains, home, away, hudOff, feelDebug, width, height, outDir, charge01`. **No `park`, no `night`**. `field` is an allowed shot | `src/GrandSluggers.Sim/StillRequest.cs:15-34` |
| `StillCapture` | Uses `NewMatch()`, so the park is the scene's `MatchDirector.ParkId` | `unity/Assets/Scripts/Runtime/StillCapture.cs:218-241` |
| Reference-vs-live comparison | None automated. "There is no CI image-diff." A read-only look-critic compares PNGs to the rubric and files; Jack passes | `docs/screenshot-gate.md:42-44`; `.grok/skills/look-critic/SKILL.md:8-21` |
| Named park shots | None beyond the global list in 7 | `data/feel/shots.json` |
| Editor gates | All open `HarborDiamond` and use Harbor matches | `unity/Assets/Editor/FieldingPursuitGate.cs:87`; `SwingOutcomeGate.cs:265`; `ValidationGate.cs:48-51` |
| `tools/local-player.py` | Copies the whole `data/` tree (so `data/parks`) beside the app; copies a trial overlay if named. Meshes reach the player only through `Assets/Resources` | `tools/local-player.py:180-186`; `unity/Assets/Scripts/Runtime/DataProfile.cs:19` |
| `tools/build-player.sh` | Same: copies `data/` beside the Linux build | `tools/build-player.sh:24-31` |
| Asset `.meta` rail | Portable test on tracked `.meta` GUIDs | `tools/tests/test_asset_metas.py:1-40` |
| Session kinds | Art session owns one `data/art` slot + the matching Blender script + stills; "Do not invent Unity-only park art when a kit slot exists"; "extra parks as products (#37)" is on the do-not-start list | `AGENTS.md:16,64-70,79` |
| Roadmap text for parks | Phase D: "Crystal Rink as a kit (#37 starts here, not six parks). Copy HarborKit pattern."; "D4. Night as rules + look ... kit + lighting slot" | `docs/roadmap.md:205-215` |

Debug-protocol rows about parks / field / kit (38 rows total; `data/agent/debug-protocol.json`):

| id | Stage | Lesson |
| --- | --- | --- |
| `harbor-bags-paint-brown` (#683) | sitting | Unity drops Blender material names on import; paint kit meshes from the `HarborKitPaint` table; unknown names never fall to wood / dirt. Promoted: `HarborKitPaintTests.ChalkMeshesPaintChalkNotDirtOrWood` |
| `boxes-kiss-plate` (#470) | still-gate | Field layout numbers come from the official diagram (6 in gap), not an invented gap. Promoted: `HomeSetTests.OfficialHomeLayout` |
| `title-wordmark-mirrored` (#696) | sitting | A board that looks into the park must not use `LookRotation(toCam)`; glyphs on local -Z |
| `brim-in-plate-lens` (#352) | still-gate | Tune the shot to the chest; do not shrink a mesh to save a camera |
| `tall-captain-turntable-crop` (#559) | still-gate | A fixed camera constant sized for one subject crops another; frame from the subject's size |
| `blender-no-metal-device-at-startup` (#681) | dcc | Use `tools/blender-run.sh`; do not retry a sandboxed launch |
| `unity-imports-dotnet-generated-assemblies`, `unity-recovery-after-delivery-termination`, `delivery-build-editor-idles-open` (#681, #765) | unity-console | Editor / delivery process rails |

No row is about a non-Harbor park, a hazard actor, lighting, or the fallback diamond.

---

## 9. Audio

| Topic | State | Evidence |
| --- | --- | --- |
| Per-park audio slot | None. `audio.json` has 17 events; none has a park id or a park field | `data/art/audio.json:1-21` |
| Crowd bed | One `crowd-bed` (authored wav) + `crowd-swell` (generated tone); started at SET, stopped on menus; same for all parks and for day / night | `unity/Assets/Scripts/Runtime/AudioBus.cs:42-43,80-101`; `AtBatDirector.cs:92`; `FlowDirector.cs:307,335` |
| Authored wavs | `bat-cheap`, `bat-perfect`, `bat-solid`, `crowd-bed`, `glove`, `throw` | `data/art/audio-clips/` |
| Hazard / ambience sounds | None | same files |
| Validator | Requires the seven core ids and `vo-{captain}`; checks bus and authored wav | `src/GrandSluggers.Sim/Art.cs:243-261` |

---

## 10. Tests that guard park presentation

There are no Unity edit-mode or play-mode test assemblies; all tests are in `src/GrandSluggers.Sim.Tests` plus opt-in Editor gates.

| Test | Guards | All parks? | Evidence |
| --- | --- | --- | --- |
| `HarborWallTests.TheDrawnOutfieldTopIsTheParkFenceOnEveryLoopSegment` | D15: `HarborWall.Height` = `fenceHeightFt` = `FieldBounds` fair fence top | Yes (six ids as a literal list). It tests the Sim function, which only the Harbor kit draws | `HarborWallTests.cs:16-41` |
| `HarborWallTests.TheFoulRailStaysHipHigh...` | Rail 4.2 ft = flight foul wall; taper is a ramp | Yes; `Copy=gap` trait | `HarborWallTests.cs:43-66` |
| `HarborWallTests.AdsAndTheMarkSitInsideTheWallFace`, `AFenceUnderTheRailIsRefused` | Ad clamp; validator floor | 8 / 12 / 26 ft theory | `HarborWallTests.cs:75-91` |
| `ParkDiamondTests.SharedOutlinesAreNotAHarborHardcode` | Dirt / bag / mound / stripe / chalk predicates, `BagIsInsideTheFoulLine(1,3)`, `FoulLinesAreSquare` | Park-independent numbers | `ParkDiamondTests.cs:11-49` |
| `ParkDiamondTests.PolesAndTrackFollowTheParkFence` | Pole and track follow the fence; a short synthetic park | Harbor + one synthetic | `ParkDiamondTests.cs:51-82` |
| `ParkDiamondTests.InfieldDirtAuthoringMatchesParkDiamond`, `BagAuthoringMatchesParkDiamond`; `HomeSetTests.PlateMeshAuthoringMatchesOfficialInches` | String match of Python constants | Harbor script | `ParkDiamondTests.cs:84-100,125-134`; `HomeSetTests.cs:42-51` |
| `HarborPostcardTests` (6 facts) | One Harbor dress for field pick and SET; wall connects, wraps, symmetric; stands; dugouts sunken; kit FBX in both slots with mesh names; how-to-play names the postcard | Harbor only | `HarborPostcardTests.cs:10-160` |
| `HarborKitPaintTests` | Paint table; `HarborKit.cs` source uses the table; Blender assigns chalk | Harbor kit | `HarborKitPaintTests.cs:6-97` |
| `ArtCatalogTests.EveryParkHasAKitSlotAndHarborIsPlaced` | Kit rows; exactly one placed | Yes (`content.Parks.Keys`) | `ArtCatalogTests.cs:220-227` |
| `ContentValidationTests` | Bad surface etc. rejected | Data rows | `ContentValidationTests.cs:117` |
| `DualStillsTests`, `DccStagesTests` | Catalog shape, Harbor lane, script header text | Harbor lane | `DualStillsTests.cs:11,167`; `DccStagesTests.cs:92-95,157,192` |
| `StillRequestTests`, `StillHarnessTests` | Request schema, framing | No park field | files in `src/GrandSluggers.Sim.Tests/` |
| `ExhibitionPickTests`, `CarnivalFrontTests` | Park cycle; front-of-house looks | Six-id list | files in `src/GrandSluggers.Sim.Tests/` |
| `NightTests` | Night stored; Harbor night = day play; Crystal window; per-park loop over 4 ids | Partly | `NightTests.cs:22-69,113` |
| `ParkSlowRowsTests`, `CompactGeometryTests` (ParkIds loops) | Sim hazard rows and geometry under the trial (sim side) | Yes | `CompactGeometryTests.cs:208-1258` |
| `BallShadowGate` (Editor, opt-in) | Shadow above `ParkDiamond.GrassTop` in Harbor | Harbor | `unity/Assets/Editor/BallShadowGate.cs:83` |

Not guarded by any test: the `ParkView` fallback diamond (bag position, dirt, foul chalk, mound), any per-park dress method, hazard drawn size vs sim radius, lighting rigs, the `field` shot against a non-Harbor park, `StarMeter` position in a non-Harbor park, the player copy of the kit FBX being byte-equal (only length is compared, `HarborPostcardTests.cs:140`).

---

## 11. Docs vs code drift (presentation)

Doc text: `docs/parks.md` "Why it exists" paragraphs. "Exists" means a primitive version in `ParkView`; there is no authored art for any row.

| Park | Doc says | In code today | Not at all |
| --- | --- | --- | --- |
| Harbor (`parks.md:14`) | Afternoon light, warning track, sunken dugouts with stairs, backstop, bleachers with crowd, town beyond the fence, dirt as paths and pads, diamond-aligned bags | All exist in `HarborKit` (`HarborKit.cs:481-497`); `DressBackstop` only wipes (no backstop mesh; the padded wall wraps home) (`:618-621`) | A backstop object as the doc names it |
| Crystal (`:19-22`) | Ice garden; same diamond kit; glass boards; frozen fountain; freeze statues (body + pedestal) you walk around; royal palace beyond CF; cool light; night blackout + follow-spot | Boards, fountain, palace, statues with frost ring, cool rig, blackout rig, follow-spot (`ParkView.cs:530-611,1160-1204`) | "Same diamond kit" (uses the fallback diamond); statues "can be shattered by a charged runner or a line drive" has no presentation; no park primitive has a collider (`Look.cs:214`; `ParkView.cs:1308,1319`), so "walk around" is not a presentation fact |
| Funfair (`:26-31`) | Cans with mouths + tags A/B/C; parked boxcar train; night chompers; tents, striped poles, ferris wheel, booths; warm light | All exist as primitives (`ParkView.cs:613-803,1206-1242`) | Warp tell on the ball; train from data (`train` row unused); chompers in `data/parks` |
| Rooftop (`:35-39`) | Tar roof; star billboards; AC boxes you can carom off; neon glare at night; dusk / neon day light | Tar-grey ground, billboards, AC boxes, skyline, neon rig (`ParkView.cs:805-869,1125-1158`) | Carom off AC units (no sim reader for `ac_unit`); 2 of 3 drawn AC units are not in data; a separate night rig |
| Canopy (`:43-48`) | Jungle walls you can clamber; vine walls with ledges at fence height; barrel-cannon actors with mouths + tags; trees; fireflies at night | `VineWall` with three ledges at 2.2 / 4.2 / 6.4 ft (fence is 12 ft), barrels with pips, trees, fireflies (`ParkView.cs:871-982`) | Ledges at fence height; climb wall tied to the fence arc (it is one box at z 370); Clamber take at the wall is an actor verb (`ActorDirector.cs:127,165,424`), not tied to the drawn wall |
| Ember (`:62-67`) | Castle; lava that reads as a pit (rim + glow); fire-breath statues as actors; night radius x 1.6, extra braziers, brighter fire; courtyard light night-ready | Castle, towers, lava rim + well + glow, statue with flame that scales by `EmberNightFireMul`, night braziers, night rig (`ParkView.cs:984-1123`) | A visible slow disc for fire breath; a slow tell on the fielder |
| Haunt Manor, Cruise Deck, Playroom, Toy Box (`:50-76`) | Unlock parks | Nothing (no data file, no kit row, no code) | All |
| "Authoring a park" (`:80`) | "Unity draws whatever the id says" | True literally: the look comes from the id, not from data | — |
| `docs/art-rails.md:38,47` | "Park kits ... `ParkView` elsewhere"; "Parks are kits, not new `ParkView` methods" | Five parks are `ParkView` methods | — |
| `docs/parks.md:86` | Foul wraps "are the shared diamond kit" | Shared in the sim boundary; drawn only by the Harbor kit | — |
| `docs/screenshot-gate.md:113` | "`C` cycles parks" | `Controls.ParkHeld` (key C) has no caller; parks cycle with the stick on the field screen | `Controls.cs:493`; `FlowDirector.cs:210-214` |
| `unity/Assets/Art/Parks/harbor-diamond/DROP.txt` | "`ParkDiamond` — HarborKit fills it; other parks reuse the outlines" | Other parks reuse only poles / track numbers | `ParkView.cs:259-288` |

---

## Harbor-only vs shared vs per-park inventory

| Element | Who builds it | Harbor-only / shared / per-park | Data-driven or code literal |
| --- | --- | --- | --- |
| Ground (lawn) | Harbor: `HarborKit.DressGrass` (`:770`). Others: `ParkView` `Outfield` cube (`:123`) | Two builders | Harbor: Sim constants (`ParkDiamond`) + `centerFenceFt`. Others: literal; color by id / surface |
| Water / base plane | `ParkView` (`:118-124`) | Shared, color per id / surface | Literal |
| Infield dirt | Harbor: `DressInfieldDirt` (`:425`). Others: `ParkView.Infield` (`:181`) | Two builders | Harbor: `data/rules/infield.json` via `ParkDiamond.InnerHalf / BackR` + constants. Others: literal |
| Bags | Harbor: kit mesh `bag`. Others: cubes (`:209`) | Two builders | Position from `Diamond` (data); size literal; two different rules |
| Home plate + boxes | Harbor: kit mesh + `HomeSet` chalk. Others: plate cubes, no boxes | Two builders | `HomeSet` constants / literals |
| Mound | Harbor: kit mesh `mound`. Others: cylinder (`:190`) | Two builders | `ParkDiamond` constants vs literals |
| Foul lines | Harbor: `DressFoulLines` (`:499`). Others: `FoulLines` (`:197`) | Two builders | Constants vs literals |
| Foul poles | Both use `ParkDiamond.FoulPole` | Shared numbers, two callers | `leftFenceFt / rightFenceFt` + constants; pole color per id |
| Outfield wall | Harbor: `DressWall` ramp prisms + ads. Others: `ParkView.Fence` boxes | Two builders; per-id colors | Radius and height from park data; colors literal |
| Foul rail / backstop wrap | Harbor: `DressWall`. Others: not drawn (a flat backstop cube per park) | Harbor-only drawing of a shared sim boundary | `HarborWall` constants |
| Warning track | Harbor: annulus. Others: `ParkView.WarningTrack` boxes (not Crystal) | Two builders | Fence from data; width constant |
| Dugouts | Harbor: kit `dugout-1b/3b` or `BuildDugout`, sunken. Others: bench + awning cubes at (+-42, 22) | Harbor-only; others per-park literals | `HarborDugout` constants / literals |
| Stands | Harbor: `DressBleachers` bowls. Others: 3 blocks per park | Harbor-only; others per-park literals | `HarborStands` constants / literals |
| Crowd | Harbor: `fan-sit` / `fan-stand` meshes + crowd texture cards. Others: 3 `CrowdCard` with `tex-crowd.jpg` | Harbor-only meshes; shared texture | Literal |
| Scoreboard | Harbor: `DressScoreboard` with live digits | Harbor-only | `centerFenceFt` + `HarborPostcard` constants |
| Backdrop / skyline | Harbor: `DressTown`. Others: palace / ferris wheel + booths / skyline / trees / castle | Already per-park, as `ParkView` methods | Literal (z 448-528) |
| Sky | `Look.Rig*` | Per-park by id | Literal |
| Light rig | `Look.Rig*` (Sun / Fill / Rim + ambient + fog) | Per-park by id; Rooftop and Canopy have no night rig | Literal |
| Night extras | Harbor floods + fireworks; Crystal follow-spot; Funfair chompers; Rooftop glow; Canopy fireflies; Ember braziers | Per-park | Literal (chompers: Sim literal) |
| Hazard actors | `ParkView.Hazards` switch + methods | Shared by type | Position and (for some) radius from `data/parks`; shape and color literal |
| Hazard tells (slow, warp, chomp, billboard) | None in Unity; captions in `Match` | — | — |
| Audio | `AudioBus` | Shared, no park input | `data/art/audio.json`; tones in code |
| Postcard (field pick) | Live park + `field` shot + `HudView.Field` copy | Shared; copy per id | Shot in `data/feel/shots.json`; copy literal in `CarnivalFront` |
| Cameras | `CameraDirector` + `PlayCamera` | Shared; one table | `data/feel/shots.json` |
| Star meter (world) | `StarMeter` on the Harbor dugout fascia | Shared, Harbor position | `HarborDugout` constants |
| Materials / textures | `Look` + 3 JPGs in `Resources/Art` | Shared | Literal; `materials.json` slots have no reader |
| Kit FBX + Blender script | `harbor_kit.py` | Harbor-only | Python literals |
| Art catalog row | `data/art/parks.json` | Per-park row, Harbor-only content | Data (`id, slot, placed`) |
| DCC stages / dual stills / still gate | `data/agent/*.json`, `tools/*still*` | Harbor-only lane | Data with a fixed schema |

---

## Unknowns

1. Whether the non-Harbor parks have been looked at in the standalone player since the kit and D15 work. No still of any non-Harbor park is in the repo; I did not open Unity.
2. Whether the `StarMeter` pips are visible or clip into the bench / awning in non-Harbor parks (code shows the position only).
3. Whether `HarborKit._dressed` leaves stale state after Harbor -> other park -> Harbor in one session (`HarborKit.cs:193-197`); the kit is only set inactive, not rebuilt.
4. How `harbor-kit.fbx` was last exported relative to the current script: the script's wall / dugout / path constants have drifted, but those meshes are unused, so the file alone does not show it. The FBX binary was not inspected beyond the test's name list.
5. Whether `materialImportMode: 1` on the kit FBX keeps any Blender material name in the player build (the #683 row says names are dropped; `HarborKitPaint` covers both cases).
6. The intended meaning of `placed` in `parks.json` beyond "Harbor is the template" (`docs/art-rails.md:47`); no code reads `Placed` except `cli art` output and one test.
7. Whether `nightOnly` / `dayOnly` were ever read; no reader exists at this commit.
8. Issue text for #713, #248, #37, #732 was not read (GitHub not queried); statements about them come from repo docs and code comments only.
9. `.grok/skills/character-art/` was not read in full; it may hold more kit procedure text.
10. The sim-side behavior of hazards (what is a rule vs what is absent) is owned by the other mapping agent; rows in section 5 list only what the presentation can or cannot key off.
