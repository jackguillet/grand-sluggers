# 13. Star skills — the two-second rule

A skill bends one rule for ≤ 2 s and then baseball resumes. The bend is one of: the ball's path (element / break / decoy) or speed, the fielder's body (status / payload), or the terrain (area). Values live in `data/abilities/star-skills.json` and are **read at runtime** (`StarSkillTable`, `ContentCatalog.StarSkills`; `speedMul`, `staminaCost`, `exitVeloMul`, `launchDeg`, validated by `ContentDataValidator`), not re-typed. The file is read strictly: a key no skill declares stops the load and is named, so a retired key cannot sit in it looking live (S-192).

**A Star Pitch never narrows the hitter's timing window and never turns contact into a miss** (PH-16-R1, PH-16-R18, PH-16-R19). The hitter is judged in the ordinary window of §5.3 against every Star Pitch; if the bat meets the ball, it is contact, graded by the cursor like any other swing. A Star Pitch's challenge is readable ball behaviour: its speed and its path.
Each replacement effect for a pitch that lost its window penalty is its own reviewed proposal with counterplay, and a Phonyball that plays too weak gets a geometric change in its own review, never a roll. ✅ P1: the C# copies are deleted; `staff-swing` is 1.08 as the JSON says.

| Skill | Bend | Then |
| --- | --- | --- |
| Heatball | +15% speed, the catcher's glove smokes; a caught fly from a heat-swing has a burn-hop (drop chance 35% for 2 s) | baseball |
| Charmball | Speed ×0.9 and a visible side-to-side wobble (`starShapes.charmball*`); its replacement effect is reviewed separately | |
| Prismball | Late break, ghost images | |
| Phonyball | Decoy path: the ball shows one side early and switches late (`starShapes.phonyball*`); reading the switch is the counterplay. Contact is contact | |
| Caskball | Slow, knockback on the catch (0.55 s) | |
| Skullball | +20% speed, nothing else until its review | |
| Fogball | Speed ×0.82, nothing else until its review | |
| Heat / Furnace swing | Exit ×1.15 / ×1.25; a burn patch / lava strip where it lands slows the fielder ×0.45 for 2 s | |
| Heart swing | The nearest fielder pauses 0.8 s | |
| Shell / Staff swing | Infield chaos: the first bounce is randomized ±30° | |
| Phony swing | Decoy ball; the real one shows at the apex | |
| Cask swing | Fragments: two decoy balls fall with it | |
| Role players | Star fastball / change / breaker; star grounder / fly / line | |

Star skills cannot produce a free home run; the exit multipliers are capped so a Perfect charged star swing at Bat 10 clears Harbor's 400 only with a Perfect. ✅ by tuning.

Each skill's row also names its cost `tier` (low 1, mid 2, top 3); which captain carries which tier, who may carry the top tier, and what happens when a team cannot pay are §12.
