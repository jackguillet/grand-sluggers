# Whole-race validation of the C80 copy — #715 step 3d, September 18, 2026

The compact profile (`trials/c80`, #716–#723) is complete on the Sim side. This is step **3d** of the [implementation plan](plan-game-feel-693.md): the copy run through the #702 instrumentation against the shipped table on the same inputs and seeds, effects reported, nothing repaired. It also closes the calibration finding carried from #719 — the copy sits under the S-29 floor — by measuring the levers one at a time. **No shipped or checked-in value changed.** No human gate is passed; every choice below is Jack's.

The machine-readable results are [game-feel-3d-race-control.json](research/game-feel-3d-race-control.json) and [game-feel-3d-race-trial.json](research/game-feel-3d-race-trial.json) (the version-2 race reports, derived by [tools/race-report.py](../tools/race-report.py) from the exported traces and the three cohorts on each root) and [game-feel-3d-lever-sweep.json](research/game-feel-3d-lever-sweep.json) (the cohorts with one key moved per variant). The traces come from `ThreeDExportTests` — the #702 fixtures, the #719 gap liner, a routine fly and a hot liner, the ten §10.4 double-play rows and the S-60 steal — exported on whichever root the environment names:

```sh
GS_RACE_TRACE_OUTPUT=/tmp/3d-control dotnet test src/GrandSluggers.Sim.Tests --filter FullyQualifiedName~ThreeDExportTests
GS_RACE_TRACE_OUTPUT=/tmp/3d-trial GRAND_SLUGGERS_TRIAL=trials/c80 dotnet test src/GrandSluggers.Sim.Tests --filter FullyQualifiedName~ThreeDExportTests
```

The production-flight probe check (`tools/game-feel-flight-probes -- --check`, 42 probes) passed first, as the plan asks; the sealed packets are untouched.

## What the measurement establishes

**The copy scores under the floor everywhere, and one key is the reason.** On the copy as landed the mixed-park S-29 cohort gives **1.28 / 1.04** runs a side (band 1.8–5), Harbor calibration **1.22 / 1.38**, Harbor validation **0.92 / 1.20**; the shipped table gives 1.90 / 1.92, 1.90 / 1.38 and 1.60 / 2.30. The outcome mix says where the runs went: doubles 72 → 24, triples 12 → 0 on S-29, fly outs 500 → 574. The lever sweep confirms the #719 finding and names the lever: moving `chase.outfieldAirMul` alone from the copy's 1.0 back to the shipped **0.6** puts every cohort in band — S-29 **2.46 / 2.36**, Harbor calibration **2.10 / 2.16**, Harbor validation **2.02 / 2.42** — with doubles 24 → 107 and triples 0 → 25 on S-29. At 0.8 the copy reaches 1.64 / 1.56, at 0.7 **1.66 / 2.02** — the away side in band, the home side not, and the same shape on both Harbor cohorts (1.60 / 2.22 and 1.62 / 2.04); the shipped pair (0.6 with `infieldAirMul` 0.45) overshoots to 2.88 / 2.70. Nothing else moves S-29 into the band on its own: the outfield read back to the shipped 0.83 gives 1.30 / 1.82 (doubles 24 → 54, the away side in band and the home side not); the fly stretch is not a lever at all (1.40 → 1.30 / 1.02; 1.10 → 1.40 / 1.58); the outfield depth moves runs the wrong way in both directions (×1.10 → 1.10 / 1.18, ×0.90 → 1.24 / 1.66); and slower legs (16.6 ft/s at Run 5, not an accepted lever) give 1.48 / 1.32.

**Why the multiplier and not the read.** The read is when the body starts; the multiplier is how much ground it covers under a ball in the air. At 18 ft/s with the multiplier at 1.0 an outfielder covers 72 ft in the four seconds a fly hangs, and the fixtures show it: the routine 245-ft fly to centre is caught standing on both roots, and the 280-ft gap liner that is a triple off the wall on the shipped table is a double the centre fielder holds to on the copy (a wall ball at 3.05 s). The #719 slices moved the reach (6 / 10 / 14 ft) and made the dive cost; neither touched what the legs cover. The multiplier at 1.0 was Jack's addition to #719 slice 1 — one pursuit profile per body, air or dirt — and it is the accepted direction this measurement puts a number on: **that direction costs the S-29 band, and the band comes back at 0.6–0.7.** The decision is his.

**The routine grounder keeps its margin, a little thinner.** S-31 on the copy: possession at 1.050 s (1.033 shipped), release at 1.52 (1.48), reception at first at 2.97 (2.80) against a runner projected at 3.46 — a 0.49-s defensive margin where the shipped table had 0.66. The thinner margin is the throw clock (#722: a 0.30-s release and 0.90 s over 80 ft), not the glove. The relay's two legs run 1.22 → 1.95 and 1.97 → 2.87 (1.18 → 1.87, 1.88 → 2.67 shipped). The steal of second is caught on both roots by the same 0.06-s margin, the catcher's throw arriving at 1.53 (1.28). The ball reaches the shortstop's ring at 72 ft/s and 3.0 ft up on the copy (80 ft/s, 2.8 ft shipped): the compact drag takes the sting out.

**Three double-play rows are not double plays on the copy — they are hits or slower outs, and the reason is the geometry, not a rule.** S-41 (the 4-6-3 up the middle) gets past the second baseman, who starts at (37, 105) on the 80/90 diamond where the shipped table stood him at (42, 118): the ball reaches his line sooner and he covers less of it in the time; the centre fielder takes it at 3.22 s and the batter has a single. S-44 (the 3-6-3) is assigned to the second baseman on the copy — the ball's line passes between him and the first baseman at (69, 64) — and nobody reaches it; a double. S-43 and S-48 (the ball to the first baseman) are outs on both roots, but the shipped first baseman steps on the bag at the take (0.78 s) while the copy's takes the ball 16 ft in front of the bag and forces the runner at 2.23 s. S-46 loses its third out. These are the "hard infield escapes" the plan asked to see; they follow from the accepted 80-ft basepaths and the migrated starts, and they are what the compact infield is supposed to do — reported here, not repaired.

**The recorded expectations the copy breaks.** Running the whole suite with every `ContentCatalog.Load()` resolving to the copy fails 166 of 1317 tests. Most of them name the shipped table on purpose — the geometry, throw-clock, recoil, stick and handling tests that assert both roots' values — and are a category error under the overlay, not an effect. The scenario rows that fail are the effect: the S-29 band; the double-play rows above (S-41, S-43, S-44, S-46, S-48 on the CPU seat, S-41, S-44, S-48 on the human seat); the tag-up race S-54 and the close-play rows S-73 to S-76 (the throw clock and the 80-ft paths change every race margin); S-36 (the runner on second holds); S-96 and the S-97 family (the liner past the lip and the roller to the grass, whose hand-offs read the shipped starts); the bunt crash rows; and the outfield hand-off scenes in `FieldingSceneTests`. Each is a fixture that recorded what the shipped diamond did; each will need its own row under the copy when the profile is promoted, and none of them should be repaired to pass now.

## The sweep, the fixtures and the cohorts

### Fixtures — control | c80

| fixture | class exit→carry | possession s | ball at take ft/s, ft | release → reception s | out s / runner projected | arrivals | kind |
|---|---|---|---|---|---|---|---|
| S31 control | Grounder 106.9→117.2 | 1.033 | 79.9, 2.82 | 1.48→2.80 | 2.80 / 3.46 | — | GroundOut |
| S31 c80 | Grounder 117.7→118.1 | 1.050 | 72.4, 3.04 | 1.52→2.97 | 2.97 / 3.46 | — | GroundOut |
| S32 control | Grounder 106.9→117.2 | 1.033 | 79.9, 2.82 | — | — / — | 1@2.85,1@3.52 | Single |
| S32 c80 | Grounder 117.7→118.1 | 1.050 | 72.4, 3.04 | — | — / — | 1@2.83,1@3.60 | Single |
| S40-6-4-3 control | Grounder 106.9→117.2 | 1.033 | 79.9, 2.82 | 1.48→2.17 | 2.17 / 2.47 | 1@3.45,1@4.25 | GroundOut |
| S40-6-4-3 c80 | Grounder 117.7→118.1 | 1.050 | 72.4, 3.04 | 1.52→2.25 | 2.25 / 2.47 | 1@3.45,1@4.35 | GroundOut |
| S41-4-6-3 control | Grounder 108.6→119.9 | 1.100 | 80.2, 2.52 | 1.43→1.83 | 1.83 / 2.47 | 1@3.45,1@4.25 | GroundOut |
| S41-4-6-3 c80 | Grounder 119.6→119.6 | 3.217 | 30.2, 0.00 | 3.50→5.23 | — / — | 2@2.47,1@3.45,1@4.35 | Single |
| S42-5-4-3 control | Grounder 95.2→99.9 | 0.817 | 74.6, 3.24 | 1.08→2.13 | 2.13 / 2.47 | 1@3.45,1@4.25 | GroundOut |
| S42-5-4-3 c80 | Grounder 103.3→99.8 | 0.867 | 69.4, 3.25 | 1.20→2.30 | 2.30 / 2.47 | 1@3.45,1@4.35 | GroundOut |
| S43-3-tag control | Grounder 89.7→91.9 | 0.783 | 71.1, 3.12 | — | 0.78 / 3.46 | — | GroundOut |
| S43-3-tag c80 | Grounder 96.3→91 | 0.850 | 66.1, 3.02 | 1.13→2.23 | 2.23 / 2.47 | 1@3.45,1@4.35 | GroundOut |
| S44-3-6-3 control | Grounder 101.7→110.1 | 0.900 | 78.1, 3.21 | 1.15→2.13 | 2.13 / 2.47 | 1@3.45,1@4.25 | GroundOut |
| S44-3-6-3 c80 | Grounder 111.6→109.8 | 4.617 | 17.6, 0.00 | 4.90→8.13 | — / — | 2@2.47,1@3.45,1@3.47 | Double |
| S45-1-6-3 control | Grounder 73.9→61.9 | 0.583 | 61.2, 2.48 | 0.78→1.60 ; 1.92→3.10 | 1.60 / 2.47 | — | GroundOut |
| S45-1-6-3 c80 | Grounder 77.5→62.2 | 0.650 | 58.3, 2.30 | 0.88→2.15 | 2.15 / 2.47 | 1@3.45,1@4.35 | GroundOut |
| S46-5u-3 control | Grounder 89.7→91.9 | 0.767 | 71.3, 3.18 | 1.03→2.12 ; 2.37→3.15 | 0.77 / 2.59 | — | GroundOut |
| S46-5u-3 c80 | Grounder 96.3→91 | 0.817 | 66.7, 3.15 | 1.12→2.27 | 0.87 / 2.59 | 1@3.45,1@4.35 | GroundOut |
| S47-1-2-3 control | Grounder 50.5→38 | 0.750 | 42.5, 1.00 | 0.95→1.47 ; 1.70→2.47 | 1.47 / 2.95 | 2@2.47,3@2.58,2@2.70 | GroundOut |
| S47-1-2-3 c80 | Grounder 51.8→37.9 | 0.883 | 39.9, 0.14 | 1.08→1.75 ; 1.98→2.87 | 1.75 / 2.95 | 2@2.47,3@2.58,2@3.28 | GroundOut |
| S48-3-plate control | Grounder 89.7→91.9 | 0.783 | 71.1, 3.12 | — | 0.78 / 3.46 | 2@1.57,3@1.57 | GroundOut |
| S48-3-plate c80 | Grounder 96.3→91 | 0.850 | 66.1, 3.02 | 1.13→2.15 ; 2.38→3.27 | 2.15 / 2.95 | 2@2.47,3@2.58,2@4.08 | GroundOut |
| S50-two-outs control | Grounder 106.9→117.2 | 1.033 | 79.9, 2.82 | 1.48→2.17 | 2.17 / 2.47 | — | GroundOut |
| S50-two-outs c80 | Grounder 117.7→118.1 | 1.050 | 72.4, 3.04 | 1.52→2.25 | 2.25 / 2.47 | — | GroundOut |
| S60-steal control | the steal of second, no hit | — | — | 0.28→1.28 | 1.93 / 1.99 | — | CaughtStealing |
| S60-steal c80 | the steal of second, no hit | — | — | 0.28→1.53 | 2.00 / 2.06 | — | CaughtStealing |
| gap-liner-719 control | Liner 121.4→282.5 | 5.667 | 0.0, 0.00 | 5.95→8.72 | — / — | 1@3.45,1@3.47,2@6.42 | Triple |
| gap-liner-719 c80 | Wall 157.8→278.2 | 3.050 | 27.8, 3.02 | 3.33→6.28 | — / — | 1@3.45,2@6.38 | Double |
| harbor-fixed-grounder control | Grounder 84→125.4 | 1.617 | 61.2, 3.20 | 1.97→3.22 | 3.22 / 3.46 | — | GroundOut |
| harbor-fixed-grounder c80 | Grounder 84→109.8 | 1.533 | 52.0, 3.31 | 1.83→3.18 | 3.18 / 3.46 | — | GroundOut |
| harbor-fly control | Fly 100→348.2 | 6.033 | 50.7, 15.85 | — | 6.03 / — | 1@3.45,1@4.25 | FlyOut |
| harbor-fly c80 | Fly 100→252.4 | 5.350 | 38.8, 13.66 | — | 5.35 / — | 1@3.45,1@4.35 | FlyOut |
| harbor-tactical-grounder control | Grounder 106.9→117.6 | 1.033 | 80.4, 2.82 | 1.48→2.80 | 2.80 / 3.46 | — | GroundOut |
| harbor-tactical-grounder c80 | Grounder 117.7→118.9 | 1.050 | 73.2, 3.05 | 1.52→2.97 | 2.97 / 3.46 | — | GroundOut |
| hot-liner-lf control | Liner 120→277.2 | 0.017 | 175.2, 2.50 | — | 0.02 / — | — | FlyOut |
| hot-liner-lf c80 | Liner 120→213.6 | 1.650 | 84.1, 8.11 | — | 1.65 / — | — | FlyOut |
| relay control | Grounder 106.9→117.2 | 1.033 | 79.9, 2.82 | 1.18→1.87 ; 1.88→2.67 | 1.87 / 2.83 | — | GroundOut |
| relay c80 | Grounder 117.7→118.1 | 1.050 | 72.4, 3.04 | 1.22→1.95 ; 1.97→2.87 | 1.95 / 2.83 | — | GroundOut |
| routine-fly-cf control | Fly 75.2→248.1 | 5.300 | 44.7, 14.19 | — | 5.30 / — | 1@3.45,1@4.25 | FlyOut |
| routine-fly-cf c80 | Fly 96.5→249.8 | 5.767 | 38.5, 14.89 | — | 5.77 / — | 1@3.45,1@4.35 | FlyOut |

### Movement — the selected body's read and first step (control | c80)

| fixture | body | read eligible s | first displacement s |
|---|---|---|---|
| S31 control | SS ashlord | 0.28 | 0.28 |
| S31 c80 | SS ashlord | 0.25 | 0.25 |
| harbor-fixed-grounder control | SS ashlord | 0.28 | 0.28 |
| harbor-fixed-grounder c80 | SS ashlord | 0.25 | 0.25 |
| harbor-fly control | LF vine | 0.83 | 0.83 |
| harbor-fly c80 | LF vine | 0.40 | 0.40 |
| gap-liner-719 control | SS grit | 0.28 | 8.73 |
| gap-liner-719 c80 | CF moss | 0.40 | 0.40 |
| routine-fly-cf control | CF moss | 0.83 | 0.83 |
| routine-fly-cf c80 | CF moss | 0.40 | 0.40 |
| S40-6-4-3 control | SS ashlord | 0.28 | 0.28 |
| S40-6-4-3 c80 | SS ashlord | 0.25 | 0.25 |

### Cohorts — control | c80 (mean runs home / away; outcome totals over 50 games)

| cohort | home / away | 1B | 2B | 3B | HR | fly outs | ground outs | K | multi-out plays |
|---|---|---|---|---|---|---|---|---|---|
| s29 control | 1.90 / 1.92 | 187 | 72 | 12 | 64 | 500 | 221 | 159 | 9 |
| s29 c80 | 1.28 / 1.04 | 168 | 24 | 0 | 52 | 574 | 198 | 180 | 2 |
| harbor-calibration control | 1.90 / 1.38 | 174 | 55 | 12 | 58 | 507 | 224 | 168 | 12 |
| harbor-calibration c80 | 1.22 / 1.38 | 186 | 19 | 1 | 54 | 596 | 203 | 193 | 3 |
| harbor-validation control | 1.60 / 2.30 | 191 | 67 | 16 | 56 | 528 | 231 | 188 | 8 |
| harbor-validation c80 | 0.92 / 1.20 | 201 | 29 | 4 | 36 | 636 | 194 | 199 | 5 |

### The S-29 lever sweep on the c80 copy (50 games each, one key moved)

| variant | key | home / away | 1B | 2B | 3B | HR | fly outs | ground outs |
|---|---|---|---|---|---|---|---|---|
| control | shipped table | **1.90 / 1.92** | 187 | 72 | 12 | 64 | 500 | 221 |
| trial | c80 as landed | **1.28 / 1.04** | 168 | 24 | 0 | 52 | 574 | 198 |
| read-060 | reaction.outfieldSec 0.40→0.60 | **1.38 / 1.12** | 187 | 34 | 1 | 52 | 535 | 213 |
| read-083 | reaction.outfieldSec 0.40→0.83 (shipped) | **1.30 / 1.82** | 182 | 54 | 3 | 55 | 511 | 196 |
| air-080 | chase.outfieldAirMul 1.0→0.8 | **1.64 / 1.56** | 196 | 41 | 8 | 62 | 539 | 211 |
| air-070 | chase.outfieldAirMul 1.0→0.7 | **1.66 / 2.02** | 172 | 82 | 12 | 61 | 503 | 211 |
| air-060 | chase.outfieldAirMul 1.0→0.6 (shipped) | **2.46 / 2.36** | 190 | 107 | 25 | 69 | 481 | 215 |
| airboth-060-045 | outfieldAirMul 0.6 + infieldAirMul 0.45 (shipped pair) | **2.88 / 2.70** | 197 | 105 | 30 | 79 | 424 | 226 |
| fly-140 | flight.timeScale 1.65→1.40 | **1.30 / 1.02** | 185 | 27 | 0 | 54 | 575 | 192 |
| fly-110 | flight.timeScale 1.65→1.10 | **1.40 / 1.58** | 191 | 40 | 2 | 61 | 538 | 212 |
| of-deep-110 | outfield starts ×1.10 radially | **1.10 / 1.18** | 210 | 31 | 4 | 47 | 554 | 242 |
| of-shallow-090 | outfield starts ×0.90 radially | **1.24 / 1.66** | 139 | 38 | 3 | 60 | 567 | 158 |
| legs-16 | chase.baseFtPerSec 12.4→11.0 (Run 5: 18.0→16.6 ft/s; not a lever) | **1.48 / 1.32** | 208 | 33 | 2 | 52 | 564 | 176 |

### The Harbor cohorts under the air multiplier (F693-06-H: home and away separately 1.8–5)

| variant | s29 | harbor-calibration | harbor-validation |
|---|---|---|---|
| control (shipped table) | 1.90 / 1.92 ✓ | 1.90 / 1.38 | 1.60 / 2.30 |
| trial (c80 as landed) | 1.28 / 1.04 | 1.22 / 1.38 | 0.92 / 1.20 |
| air-070 (chase.outfieldAirMul 1.0→0.7) | 1.66 / 2.02 | 1.60 / 2.22 | 1.62 / 2.04 |
| air-060 (chase.outfieldAirMul 1.0→0.6 (shipped)) | 2.46 / 2.36 ✓ | 2.10 / 2.16 ✓ | 2.02 / 2.42 ✓ |

### The suite under the trial overlay

166 of 1317 tests fail when every `ContentCatalog.Load()` resolves to the copy:
- CompactGeometryTests: 23
- OutsScenarioTests: 19
- ThrowClockTests: 11
- HandlingErrorTests: 7
- ControlScenarioTests: 7
- FieldingSceneTests: 6
- InfieldGeometryTests: 6
- ThrowCommandTests: 6
- PursuitContractTests: 5
- AirRecoilTests: 5
- TrialOverlayTests: 4
- DiveTests: 4
- RecoilTests: 4
- RelayDecisionTests: 3
- MatchTests: 3
- FieldingPursuitTests: 3
- TagUpDecisionTests: 3
- CatchReachTests: 3
- BallDashTests: 3
- AtBatTests: 3
- BallFlightTests: 2
- HarborWallTests: 2
- DeflectionTests: 2
- SeatOwnershipTests: 2
- BuntScenarioTests: 2
- FieldingScenarioTests: 2
- ParkDiamondTests: 2
- PursuitStickTests: 2
- LiveBallScenarioTests: 2
- FlyCatchTests: 2
- ResponseLawTests: 2
- JumpTests: 2
- DefensiveTraitTests: 1
- PlayTraceTests: 1
- OutfieldReadTests: 1
- RulesTests: 1
- RunnerScenarioTests: 1
- FeelInfraTests: 1
- AtBatFeelTests: 1
- NightTests: 1
- StealScenarioTests: 1
- PitchTests: 1
- BodyFacingTests: 1
- FlightScenarioTests: 1
- BroadcastHudTests: 1
- AtBatScenarioTests: 1

## What this leaves for Jack

1. **The S-29 lever.** `chase.outfieldAirMul` 0.6 lands every cohort in band on the copy as it stands; 0.7 puts the away side in band and leaves home at 1.6 in all three cohorts; 1.0 is the accepted one-profile direction and misses the floor everywhere. The read (0.83), the fly stretch and the depth do not get there alone. A choice here is a choice about the direction, not a tuning.
2. **The compact infield changes the §10.4 rows.** Three of ten double plays are hits or slower outs on the copy because the bodies start closer and the paths are shorter. If the profile is promoted, those rows are rewritten to what the compact diamond does, with the old rows kept on record — the rail this series has followed.
3. **The 0.49-s routine margin** (S-31) is the throw clock's, and it is the margin the dive's 0.60 s was derived against; it holds, thinner.

Nothing here passes a runtime or human gate; the sitting (3e) remains Jack's.
