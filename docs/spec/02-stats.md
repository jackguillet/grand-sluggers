# 2. Stats, in numbers

Stats are 1–10 per character (`data/characters/`). They drive *only* the quantities below. Nothing else may read a stat. Formulas here are the contract; the exact coefficients are the `data/rules/` tables in §16 and are tunable without touching this document.

| Stat | Drives | Not allowed to drive |
| --- | --- | --- |
| **Pitch** | Nothing directly: it is the displayed pitching aggregate, the swap pick's ranking and the seed for the four ratings below. **Velocity** drives pitch mph; **Movement** drives the arm's damping of non-perfect contact; **Control** drives the steer rate and the CPU's aim scatter; **Endurance** drives the stamina pool | Whether a batter misses |
| **Bat** | Nothing directly: it is the displayed batting aggregate and the seed for the two traits below. **Contact** drives sweet-spot size — and, ⚠️ on the shipped root, contact window width; the CPU's timing error is its own execution skill (§5.9). **Power** drives exit velocity and loft | Whether a fielder catches |
| **Field** | Chase speed in the field, catch radius, jump/dive window, throw speed, throw accuracy, bobble chance, CPU throw decision delay | Whether a runner is out |
| **Run** | Sprint speed on the bases and out of the box, dash, CPU send aggression, close-play reaction | Anything about the ball |

**Design amendment (F693-02-character-catch-range):** the target catch range is an explicit property independent of displayed Fielding; Field must not determine glove positioning/response. The historical Field-driven catch-radius model above needs reconciliation before implementation. The later F693-02-defensive-trait-mapping supersedes summary-only Fielding: Arm controls throwing and Fielding controls hands/recovery.
The table above describes historical runtime; the target amendments in §0.2 govern the migration, preserving approved anchors and separately authored reach.

The four stats are shown on the character card. **Contact and Power are ratings of their own** (PH-15-R5), authored beside `bat` the way `arm` and `hands` are authored beside `field`: optional keys in `data/characters/`, 1–10 when present, and **an absent or 0 key tracks `Bat`**, so a roster that authors neither behaves exactly as it did before the split. `Stats.ContactAuthored` / `PowerAuthored` say which it was; neither flag is serialized. No shipped character authors a value today, and the values are Jack's to choose later. ✅ P2-a

- **Contact is spatial forgiveness** (PH-15-R7): it scales the cursor's barrel (§5.2), so a crossing further from the center still finds the bat. It does **not** buy per-character timing assistance. It does not touch the timing window; the barrel is all it does (§5.3). ✅ The CPU batter's timing error is a separate thing that also reads Contact: it is that bat's *execution*, not its window (§5.9), and whether it should read Contact at all is P2-g's question.
- **Power is hitting strength**: the exit-velocity base (§5.5) and the launch loft (§5.4).
- A bat item's `contactMod` / `powerMod` still adds to the trait it names (`data/bats/`), after the trait is resolved and before the 1–10 clamp.
- `Bat` stays the **displayed aggregate**: the character card, `cli teams` / `cli chem`, the Raylib HUD and `Teams.Tools`' four-rating sum all read `Bat` and none of them reads a trait. What the card shows once the traits carry different numbers is P2-f's.

**Velocity, Movement, Control and Endurance are ratings of their own** (PH-15-R6), authored beside `pitch` the same way: optional `velocity` / `movement` / `control` / `endurance` keys in `data/characters/`, 1–10 when present, and **an absent or 0 key tracks `Pitch`**, so a roster that authors none behaves exactly as it did before the split. `Stats.VelocityAuthored` … `EnduranceAuthored` say which it was; no flag is serialized. No shipped character and no trial overlay authors a value; the values are Jack's to choose later. ✅ P3-a

Every read that took `Pitch` now takes the rating it means:

| Read | Where | Rating | Why |
| --- | --- | --- | --- |
| Pitch mph (`speed.mphPerPitchStat`) | `AtBatResolver.PitchSpeedMph(pitch, Character)`, so `Match.PitchSpeedMph`, the air time, the client's mph and the CPU's reach all follow | **Velocity** | pitch speed (§4.1) |
| The arm's say over non-perfect contact (`batting.pitchFactor` `niceDampPerPitch` / `sourDampPerPitch`) | `AtBatResolver.Resolve` → `PitchFactor` | **Movement** | the ball that is hard to square is the arm's natural stuff, not its speed or its steering (§5.5). ⚠️ No family's authored break reads a rating today (§4.3), so this is Movement's only read; a Movement term in the family break is a number and is not this child's |
| The steer rate (`flight.breakRatePerSec` × `breakRatePerPitchStat`) | `PitchFlight.BreakStep` (the hand, `AtBatDirector`) and `PitchFlight.BreakReach` (the CPU, `CpuPitcher.PitchByInputs`) | **Control** | the player's steering correction (§4.1, §4.8 rule 5) |
| The CPU arm's scatter (`cpu.scatterFtPerPitchStat`) | `CpuPitcher.PitchByInputs` | **Control** | missing its own intent is command, not speed (§4.8) |
| The stamina pool (`stamina.poolPerPitch`) | `Match.StaminaPool` | **Endurance** | resistance to fatigue (§4.7). What TIRED and exhausted cost stays as it is: fatigue itself is P3-b / P3-c's |
| `PitchInZone`'s stat argument | `Match`, `Training.RecordPitch`, the Raylib `Game`, `AtBatDirector` | Control (passed, never read) | the zone is never resized by skill (§4.4); the argument is discarded as before |
| The swap pick (`SwapPitcher` with nobody named, `PitcherSwapPick`'s starting arm) | `Match`, `PitcherSwap` | **Pitch** | a selection by the displayed aggregate, not a rating's read; what it should rank by once the ratings differ is left open |
| The card, `cli teams` / `cli chem`, the Raylib HUD, `HudView`, `CardToy`, `Teams.Tools` | — | **Pitch** | the displayed aggregate, as `Bat` stayed |

The rules keys keep their historical `…PerPitchStat` / `poolPerPitch` names: renaming a key is a data migration and no number moves here. The fatigue reads (the fade, `tiredBreakMul`, the CPU swap at TIRED) read the pool and not a rating, and are left for P3-b / P3-c.

**Ordinary pitch repertoire (PH-02-R1/R2, PH-15-R1/R2/R4).** Beside the stats, every character carries an ordered repertoire of exactly three ordinary pitches: the **fastball every pitcher throws**, then two *different* families drawn from Changeup / Curveball / Slider / Sinker, in the order the decision register accepts (`PH-15-R2` for the seven captains, `PH-15-R4` for the eighteen role players).
The data field is `"repertoire": ["<second>", "<third>"]` in `data/characters/`; the fastball is implied and may never be listed or removed, and the trial overlays carry the same rows.
`ContentDataValidator` **requires** the field on every row — the character loader ignores unknown keys, so a misspelled one has to surface as missing — and refuses a count other than two, the same family twice, a listed `fastball`, and any id outside the four (`PitchFamily`, `Repertoire`; `RepertoireTests` holds all 25 rows to the register). ✅ — **membership only**; §4.3 says what flies.

Family ids are a **different namespace from the Star Pitch ids** in `data/abilities/star-skills.json`, which already spells two of its own skills `fastball` and `changeup` (§13). A character's `starPitch` is never resolved against its repertoire and the two sets are never validated against each other: Boom's star pitch is spelled `fastball` while his ordinary repertoire is slider + sinker. ✅ 
