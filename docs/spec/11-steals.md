# 11. Steals and the catcher

## 11.1 Starting a steal

- Select a runner and press the steal control, or point toward their next bag: the runner departs on that command. Departure is allowed in SET, during the pitcher’s held windup, and during pitch flight. There is no automatic release-time departure and no distance granted before the press.
- First → second, second → third and third → home are all available. Several runners can depart independently. A runner cannot start toward an occupied next bag unless its occupant is advancing; a returning occupant is not permission to send another runner into that bag.
- Before pitcher commitment, any-base throws can challenge an early departure. After commitment, the pitcher can release the pitch and let the catcher challenge it, or incur the balk for abandoning the pitch (§4.5).

## 11.2 Continuous movement and return

- The existing runner body advances on the pre-contact clock at its ordinary speed before pitch release, and at `running.steal.airSpeedMul` (0.6667) during pitch flight. Catcher possession resumes ordinary live-play speed. No phase transition recalculates a head start or resets the runner’s position, destination or hold.
- Return is available after departure: the selected runner moves back from their actual position, remaining vulnerable off the bag. Send can reverse them again. Halt stops the body where it stands. All-return applies to every runner; an ordinary live-ball shoulder tap may first halt under §9.3.
- A runner reaching a destination while the pitcher still holds the ball has stolen it. The pitcher’s charge remains committed and the appearance remains unfinished. A home crossing scores once; a walk-off ends the game.
- During pitch flight, arrivals remain part of that pitch until its disposition is known. A dead foul returns the runners to their starting bags. On contact, the same positions continue into the batted-ball play. A walk/HBP awards forced bases without erasing an unforced runner’s home crossing.
- There is no perfect-arm bonus. An earlier jump is the distance actually travelled between the command and the catch. A pre-contact return must never snap the runner back onto a bag.

## 11.3 Catcher throw play (after a take or a miss)

- On a take or miss, catcher possession starts the live runner play with the existing runner bodies. The defense can select its target before receiving and buffer a throw for `fielding.throw.relayBufferSec` (0.25 s) of active play. A later target selection retargets that buffer; cancel clears it; pause consumes no buffer time. An expired press does not throw.
- A human throw command starts transfer at possession or when pressed afterward. CPU command reaction is separate: `0.42 − Field × 0.014 ± 0.10` seconds, clamped to 0.10–0.58 before the difficulty reaction multiplier. Both then pay the shared physical preparation time.
- Every throw separates command, transfer/plant and release from flight. The ordinary `fielding.throw.releaseSec` remains 0.30 s (Snap Throw uses its authored release). The ball stays at the thrower through preparation; the receiver takes control and the throw sound/trace fires at actual release. Flight uses the remaining time from the existing total throw clock, so this separation does not add a second release delay. Geometry still decides the catch and tag.
- The default catcher target is the lead advancing runner’s destination. The player may choose any bag or a cutoff. Receiver coverage and chemistry use the shared live-ball systems.
- **LB on the runner play** is the §8.9 rule, shared with the batted-ball play (`LivePlaySystem.SelectTakes`, `FieldAssist.SwapGlove`): with the ball in the catcher's (or the receiver's) glove a press is refused — the ring and the ball stay put and the throw still goes where the right stick says; while the throw flies it is dead; on a loose ball (a sailed pickoff, S-71) it takes the body the stick names under the one `chase.swapLockSec` lock, which also holds the nearest-body hand-off and the CPU walk, as on a batted ball. ✅ / (S-99 on the runner play, `StealScenarioTests`).
- The out is a **tag** at the bag: ball arrival + receiver on the bag + tag reach vs runner arrival (§10.3). With two runners stealing the catcher picks one (human) or the lead runner unless the trailing runner's margin is ≥ 0.3 better (CPU). ✅ P6 (`ArrivalVerdict` / `TryTag` at the bag, the mash at third or home inside the margin (§9.6); `running.steal.cpuTrailPreferSec`).
- On a **walk** or **HBP** the runner from 1st is entitled to 2nd — no play on them; other runners' steals are live. ✅ P6 (`PlaceByWalk` seats the forced bodies; an unforced body that broke still runs its play, S-63).
- With a runner on third watching a steal of second the free middle infielder cuts `running.steal.cutInFrontFt` (25) in front of the bag on the throw line: the cutoff verb (LB with nothing armed) sends the throw to them and they hold for the play at the plate (S-66); the cover at second can return it home too.
- Stamp STOLEN BASE / CAUGHT STEALING. ✅ A strikeout with the runner caught is two outs on one pitch: DOUBLE PLAY (S-62). Time seats the bodies and completes the pitch's event (`Match.FinishRunnerPlay`): a runner out is CAUGHT STEALING, a runner who took a bag is a STOLEN BASE, a body back on its bag leaves the pitch as it was; a run that crossed counts by §1.

## 11.4 Pitcher throw play

- Before commitment, a throw to any of the four bases begins the ordinary live runner play, even when the destination is empty. The receiver covers that target; other departing runners retain their position and direction.
- After commitment, §4.5’s balk replaces the attempted throw. After release, the pitcher cannot make a second throw while the pitch is airborne.
- A runner can keep going, return or reverse in a rundown. Tags use the ball and bodies; a failed or sailed pitcher throw never awards a base by itself.

## 11.5 Scenarios

| Scenario | Rule | Id |
| --- | --- | --- |
| Straight steal of 2nd, take | Catcher throw to 2B, tag | S-60 ✅ |
| Steal of 2nd, swing and miss | Same; the batter's body does not block | S-61 ✅ |
| Strikeout + caught stealing | Two outs on one pitch; stamp DOUBLE PLAY | S-62 ✅ |
| Steal of 2nd, ball four | Runner entitled to 2B; no play (an unforced runner's steal is live) | S-63 ✅ |
| Steal of 2nd, ball in play | Steal becomes running; forced anyway | S-64 ✅ |
| Double steal 1st & 2nd | Catcher picks; lead runner default | S-65 ✅ |
| Double steal 1st & 3rd (delayed) | Runner on 1st goes; catcher throws to 2B → runner on 3rd may break for home when the throw passes the mound (stick); the SS/2B can cut the throw (cutoff verb, `running.steal.cutInFrontFt`) or the cover at 2B returns it home | S-66 ✅ |
| Steal of home | Catcher receives, walks to the plate, tags; the runner needs enough actual departure time; a slow pitch gives more running time | S-67 ✅ |
| Pickoff at 1st, runner standing on the bag | Live throw; the runner remains safe on the bag | S-68 ✅ |
| Pickoff at 1st, runner departed during SET | Existing position and return direction survive; tag at 1B or 2B / rundown by geometry | S-69 ✅ (the one cover read; the Run-8 body is tagged at second on the throw ahead, a Run 2–4 body through the rundown) |
| Early departure, average catcher | Runner travels for the actual time before release; safe at 2B against a Field-5 catcher, out against the roster's best arm (Field 8; the row's Field 9 is not on any roster) with a Nice release | S-70 ✅ |
| Pickoff throw sails (bad chem; a bad pair never slants, and no pickoff of 400 sails with Vale on the mound, so the S-71 and S-99 rows give the pitcher an authored Arm of 1) | Ball live; runner advances (ERROR) | S-71 ✅ |
| CPU never picks off a runner at random | | S-72 ✅ |

✅ P6 (`StealScenarioTests`): every row runs headlessly on the seats it names; the CPU catcher and the human catcher drive the same live ball.

## 11.6 CPU steal decisions

At SET, for each runner with an open next bag: `P(steal) = base(Run) × situation`, base = 0 for Run ≤ 4, 0.06 at Run 6, 0.16 at Run 8, 0.25 at Run 10 (`running.cpu.stealAnchors`, linear between them and holding past the last); ×1.5 with 2 outs, ×0.5 with the captain slugger up, ×0 with a runner already armed ahead of them (no double steal into a body), ×0 when trailing by ≥ 5. A chosen CPU runner departs in SET and is exposed to a legal pitcher throw; no perfect-arm distance bonus is applied.
Evaluated once per at-bat (not per pitch), as a runner-AI event — not inside `CpuBatter.Swing`. ✅ P6 (`RunnerAi.StealPlan`, `running.cpu.stealMinRun / stealBaseRun6 / 8 / 10 / stealTwoOutsMul / stealCaptainUpMul / stealTrailingRuns`, `cpu.*.perfectStealChance`; `CpuBatter.ArmSteal` runs it once per at-bat on the one seeded stream; the old `stealChance` roll is gone).
