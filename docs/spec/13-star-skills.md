# 13. Star skills — the two-second rule

A skill bends one rule for ≤ 2 s and then baseball resumes. The bend is one of: the ball's path (element / break / decoy) or speed, the fielder's body (status / payload), or the terrain (area). Values live in `data/abilities/star-skills.json` and are **read at runtime** (`StarSkillTable`, `ContentCatalog.StarSkills`; `speedMul`, `staminaCost`, `exitVeloMul`, `launchDeg`, validated by `ContentDataValidator`), not re-typed. The file is read strictly: a key no skill declares stops the load and is named, so a retired key cannot sit in it looking live (S-192).

**A Star Pitch never narrows the hitter's timing window and never turns contact into a miss** (PH-16-R1, PH-16-R18, PH-16-R19). The hitter is judged in the ordinary window of §5.3 against every Star Pitch; if the bat meets the ball, it is contact, graded by the cursor like any other swing. A Star Pitch's challenge is readable ball behaviour: its speed and its path.
Each replacement effect for a pitch that lost its window penalty is its own reviewed proposal with counterplay, and a Phonyball that plays too weak gets a geometric change in its own review, never a roll. ✅ P1: the C# copies are deleted; `staff-swing` is 1.08 as the JSON says.

| Skill | Bend | Then |
| --- | --- | --- |
| Heatball | +15% speed, the catcher's glove smokes. The burn-hop drop roll is retired (§8.6) | baseball |
| Charmball | Speed ×0.9 and a visible side-to-side wobble (`starShapes.charmball*`); its replacement effect is reviewed separately | |
| Prismball | Late break, ghost images | |
| Phonyball | Decoy path: the ball shows one side early and switches late (`starShapes.phonyball*`); reading the switch is the counterplay. Contact is contact | |
| Caskball | Slow, knockback on the catch (0.55 s) | |
| Anvil (`skullball`) | Speed ×1.2 and a `drop`: the ball flies the plain path until `from` (0.7) of the flight — the clang, where the glowing ball turns to cold iron — then sinks on a quadratic ease to `dropFt` (1.5 ft, in the batter's zone like every vertical star shape) below it, the whole drop at the plate. The crossing moves: the umpire, the bat and the CPU judge the dropped ball, in the ordinary timing window judged at that real crossing. The drop is always straight down; a low strike can drop out of the zone and a high ball into it. The clang comes first: aim low | |
| Fogball | Speed ×0.82, nothing else until its review | |
| Heat swing | Exit ×1.15; a burn patch where it lands slows the fielder ×0.45 for 2 s | |
| Hot Iron (`furnace`) | Exit ×1.25 and a `hotBall`: the ball stays molten for `moltenSec` (2 s) after contact. A glove that holds it more than `holdSec` (0.5 s) of that time — from its take to its throw's command — drops it at its feet, a live loose ball, and cannot take it back until it cools; any other glove may. A take late enough to hold less than `holdSec` of molten ball never drops. A catch still counts: a caught fly is an out before any drop. The same clock for a CPU glove and a player's; no roll. Throw it at once, or let it cool on a hop. Nothing is left on the track | |
| Heart swing | The nearest fielder pauses 0.8 s; no drop roll (§8.6) | |
| Shell / Staff swing | Exit ×1.1 / ×1.08; the warp roll is retired (§8.6) until each swing's own effect is built | |
| Phony swing | Decoy ball; the real one shows at the apex; no drop roll (§8.6) | |
| Cask swing | Fragments: two decoy balls fall with it | |
| Mirage Ball | A faint twin (`twin`) flies `offsetFt` to the far half of the zone from the real crossing, full until `fadeFrom` of the flight and gone by `fadeTo`, never later than half the flight. The real ball is the pitch as thrown; the umpire, the bat and the CPU read only it. Picking the real ball before the twin fades is the counterplay | |
| Sidewinder | A fair ball off the swing turns `firstHopKickDeg` (≤ 45°) at its first ground contact, away from the fielder the play sent after it, and runs on the shared ground physics; every chaser re-plans. A ball caught before its hop never turns | |
| Rockfall | A `float`: the ball rises up to `riseFt` over its ordinary path, highest at `dropFrom` of the flight, then drops back onto it by the plate. The crossing is the ordinary one; only the look of the flight bends. Speed ×0.95 | |
| Updraft | A star fly (launch 34°, exit ×1.1) whose ball rides the park's wind `windMul` (1.5) times as hard: the same flight as in a wind half again as strong, in every direction the wind blows. At a calm park it is the plain ball. The factor rides on the batted ball, so every continuation of it reads the same wind | |
| Leapfrog | A `leap`: from `at` of the flight the ball crawls at `holdPace` of its pace for `holdSpan` of the flight, then leaps over the rest of its path to arrive on time. The path, the crossing and the arrival instant are the ordinary pitch's, so the timing window is too | |
| Pond Skip | A low star swing (launch 0°, exit ×1.05) whose first hop leaves the ground `firstHopBounceMul` (2.2) times as fast upward: a high chopper for a fast runner. A ball gloved before it springs is an ordinary out | |
| Role players | Star fastball / change / breaker; star grounder / fly / line | |

Star skills cannot produce a free home run; the exit multipliers are capped so a Perfect charged star swing at Bat 10 clears Harbor's 400 only with a Perfect. ✅ by tuning.

A captain's Star Pitch and Star Swing belong to that captain alone, and no two captains share an effect family; a sidekick's are the generic pool's (`kind: generic`). What a special costs and what happens when a team cannot pay are §12.
