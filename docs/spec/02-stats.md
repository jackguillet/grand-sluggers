# 2. Stats, in numbers

Every character authors **nine sub-stats**, each 1–10, in `data/characters/`. The sim reads only the sub-stats, and each one drives only the quantities in its row below. Nothing else may read a stat. Formulas here are the contract; the exact coefficients are the `data/rules/` tables in §16 and are tunable without touching this document.

**The four bars are derived, never authored** (CH-07). Each bar is the mean of its sub-stats, **rounded half up** (6.5 → 7, 5.25 → 5; `Stats.Bar`, integer arithmetic). The Run bar is the one Run sub-stat.

| Bar | Sub-stat (JSON key) | Drives | Not allowed to drive |
| --- | --- | --- | --- |
| **Bat** | Contact (`contact`) | The cursor's barrel (§5.2); the CPU batter's timing error, chase and sac-bunt gate (§5.9) | Exit velocity, loft, whether a fielder catches |
| | Power (`power`) | Exit velocity (§5.5) and launch loft (§5.4); the CPU's forced charge and archetype split (§5.9) | The barrel, whether a fielder catches |
| **Pitch** | Pitch power (`velocity`) | Pitch speed (§4.1) | A fielding throw, whether a batter misses |
| | Stamina (`endurance`) | The stamina pool (§4.7) | Pitch speed, a throw |
| | Control (`control`) | The steer rate (§4.1) and the CPU arm's scatter (§4.8) | The zone's size (§4.4) |
| | Break (`movement`) | The arm's damping of non-perfect contact (§5.5) | Pitch speed, a throw |
| **Field** | Hands (`hands`) | Bobble quality (§8), ordinary recoil, dive recovery, the CPU fielder's transfer before a throw (§8.8), the CPU catcher's release (§11), the tagger's close-play reaction (§9.6) | Catch reach, glove positioning, a pitch |
| | Throw speed (`arm`) | A fielding throw's speed, comfortable range and accuracy (§8.5) | A pitch, catch reach |
| **Run** | Speed (`run`) | Sprint speed on the bases and out of the box, chase speed in the field, dash, CPU send aggression, the runner's close-play reaction | Anything about the ball |

Catch reach is not a stat: it is the table's stand-up reach, or a character's authored `reachFt` (§8).

**Pitch power and throw speed are separate verbs.** A pitcher's fastball comes from Pitch power; a fielder's throw comes from throw speed. A strong-armed outfielder is not a hard-throwing pitcher, and a changed Pitch power never moves a throw's arrival (SC-03, SC-04).

**What reads a bar.** A bar is the displayed number: the character card, the team sheet, `cli roster` / `cli teams`, the Raylib HUD, `HudView`, `CardToy`, and `Teams.Tools`' four-bar sum. Two selections rank by the displayed Pitch bar rather than a rating: the swap pick (`SwapPitcher` with nobody named, `PitcherSwapPick`'s starting arm) and the defense setup's arm. A verb never reads a bar.

**The stat budget** (CH-08). At most one derived bar per character may be 9 or higher (`Stats.StarBar`, `Stats.MaxStarBars`). One bar at 10 is allowed. The cap reads the derived bar, never a sub-stat: two 10s inside the Pitch group can still round to an 8.

**The validator** (`ContentDataValidator`) requires all nine keys on every character row and refuses a value outside 1–10, by character and key. It refuses a row that authors `pitch`, `bat` or `field`, naming the sub-stats to author instead: the loader drops unknown keys, so a stale bar would otherwise sit in the file as if it counted. It refuses a row with two or more bars at 9 or higher, naming the character and the bars.

A bat item's `contactMod` / `powerMod` still adds to the sub-stat it names (`data/bats/`), after the sub-stat is read and before the 1–10 clamp.

- **Contact is spatial forgiveness** (PH-15-R7): it scales the cursor's barrel (§5.2), so a crossing further from the center still finds the bat. It does **not** buy per-character timing assistance and does not touch the timing window (§5.3). The CPU batter's timing error is a separate thing that also reads Contact: it is that bat's *execution*, not its window (§5.9).
- **Power is hitting strength**: the exit-velocity base (§5.5) and the launch loft (§5.4).

Where each pitching read lives:

| Read | Where | Sub-stat | Why |
| --- | --- | --- | --- |
| Pitch mph (`speed.mphPerPitchStat`) | `AtBatResolver.PitchSpeedMph(pitch, Character)`, so `Match.PitchSpeedMph`, the air time, the client's mph and the CPU's reach all follow | **Pitch power** | pitch speed (§4.1) |
| The arm's say over non-perfect contact (`batting.pitchFactor` `niceDampPerPitch` / `sourDampPerPitch`) | `AtBatResolver.Resolve` → `PitchFactor` | **Break** | the ball that is hard to square is the arm's natural stuff, not its speed or its steering (§5.5). ⚠️ No family's authored break reads a rating today (§4.3), so this is Break's only read |
| The steer rate (`flight.breakRatePerSec` × `breakRatePerPitchStat`) | `PitchFlight.BreakStep` (the hand, `AtBatDirector`) and `PitchFlight.BreakReach` (the CPU pitcher, `CpuPitcher.PitchByInputs`, and the CPU batter's read of it, `CpuBatter.ReadPitch`) | **Control** | the player's steering correction (§4.1, §4.8 rule 5) |
| The CPU arm's scatter (`cpu.scatterFtPerPitchStat`) | `CpuPitcher.PitchByInputs` | **Control** | missing its own intent is command, not speed (§4.8) |
| The stamina pool (`stamina.poolPerPitch`) | `Match.StaminaPool` | **Stamina** | resistance to fatigue (§4.7). The fatigue reads (the fade, `tiredBreakMul`, the CPU swap at TIRED) read the pool and not a rating |
| `PitchInZone`'s stat argument | `Match`, `Training.RecordPitch`, the Raylib `Game`, `AtBatDirector`, `CpuBatter` | Control (passed, never read) | the zone is never resized by skill (§4.4) |

The rules keys keep their historical `…PerPitchStat` / `poolPerPitch` / `…PerField` names: renaming a key is a data migration.

**Ordinary pitch repertoire (PH-02-R1/R2, PH-15-R1/R2/R4).** Beside the stats, every character carries an ordered repertoire of exactly three ordinary pitches: the **fastball every pitcher throws**, then two *different* families drawn from Changeup / Curveball / Slider / Sinker, in the order the decision register accepts (`PH-15-R2` for the seven captains, `PH-15-R4` for the eighteen role players).
The data field is `"repertoire": ["<second>", "<third>"]` in `data/characters/`; the fastball is implied and may never be listed or removed, and the trial overlays carry the same rows.
`ContentDataValidator` **requires** the field on every row — the character loader ignores unknown keys, so a misspelled one has to surface as missing — and refuses a count other than two, the same family twice, a listed `fastball`, and any id outside the four (`PitchFamily`, `Repertoire`; `RepertoireTests` holds all 25 rows to the register). ✅ — **membership only**; §4.3 says what flies.

Family ids are a **different namespace from the Star Pitch ids** in `data/abilities/star-skills.json`, which already spells two of its own skills `fastball` and `changeup` (§13). A character's `starPitch` is never resolved against its repertoire and the two sets are never validated against each other: Boom's star pitch is spelled `fastball` while his ordinary repertoire is slider + sinker. ✅ 
