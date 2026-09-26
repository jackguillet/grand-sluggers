# 10. Outs

## 10.1 The five ways

| Out | Condition |
| --- | --- |
| Strikeout | Three strikes (§1) |
| Catch | A fly, liner, pop, or foul fly held (no bobble) before it touches the ground or wall |
| Force | A fielder holding the ball touches a bag that a forced runner has not yet reached, or tags the forced runner |
| Tag | A fielder holding the ball touches a runner who is not on a bag (or who is off the bag they must return to) |
| Throw-out at first | The force at first: ball in the glove on the bag before the batter's foot |

Everything else (caught stealing, picked off, doubled off, appeal) is a tag or a force. ✅ P5: these five are the only paths to `Outs++` (`Match.RecordOut` through `RetireLiveRunner`; the recorders are `RecordCatchOut`, `TryForce`, `ApplyThrow`, `TryTag`, and the close-play verdict). `Retire`'s result gates every caption and flag: a retire that fails narrates nothing. ✅ P6: caught stealing and picked off are the same tags, made on a live runner play (§11.3, §11.4); the old steal race is gone.

## 10.2 Arrival

`ballArrival(bag)` = release time + `throwSec` (§8.5), then the receiver must be inside 6 ft of the bag (cover). If nobody covers, the ball skips past: live. `runnerArrival(bag)` = from the runner's position and speed. Out iff `ballArrival + 0` < `runnerArrival` **and** the receiver is on the bag (force) or tags (unforced). Tie → runner. ✅ P5 (`LivePlaySystem.OnThrowArrived` / `ArrivalVerdict` from the bodies:
a forced body short of the bag is out, a body on it beat the throw, an unforced body inside the reach is tagged, one further out is waited for and the tag rule runs frame by frame). The scripted harness command `ThrowArrived` (tests only) still states the arrival it wants; no runtime path uses it.

## 10.3 Tag geometry

- A tag is a glove with the ball inside **reach** of the runner: `TagReachFt` = 4 ft (no field ability adds to it); the runner is not touching a bag (`TagSafeRadiusFt` **1.5**). Home plate is a bag for a runner coming home, **not** for the batter leaving the box. ✅ P5 (`InPlay.TagReachFt`, `InPlay.Touches(fielder:)`). The safe radius moved from the 3.5 of the first draft to 1.5:
  the slide takes 2 ft off the reach (§9.4), and a safe radius wider than the slid reach would make every slide untaggable; the table validates `tagSafeRadiusFt ≤ tagReachFt − slideReachCutFt`. The tag is judged **through the frame** (`InPlay.TagWithinFrame`): a body that crossed the reach on its way to the bag was tagged before it touched, however short the step, so a 60 Hz body cannot skip the four-foot window. A tag records the bag the glove stands on (or 0 in the field).
- Human: have the ball, touch the runner (walk into them). South is not required. ✅
- The runner on a bag is safe. A runner who overran second or third is off the bag and taggable. Overrun first is protected (§9.4). ✅ P5 (`Runner.OverrunProtected`; nobody overruns second or third — a body stops on the bag it is stopping at, and one that rounds is off it and live).
- A glove that walks to a bag a body is bound for **waits there** and the tag at the bag decides (the catcher at the plate on a steal of home, the first baseman on a pickoff throw); it leaves once nobody is bound there. ✅ P6 (`LivePlaySystem.PlayStandsAt`).

## 10.4 Double plays (ground ball)

The classic turn: force at second, throw to first. Each leg is its own throw with its own arrival test; **one press is one throw**. The human throws both (how-to-play: "you throw both"). CPU chains by §8.8 rule 1.

| Scenario | Ball to | First throw | Second throw | Notes | Id |
| --- | --- | --- | --- | --- | --- |
| Runner on 1st, grounder to SS | SS | 2B (2B covers) | 1B | 6-4-3 | S-40 |
| Runner on 1st, grounder to 2B | 2B | 2B (SS covers) | 1B | 4-6-3; if 2B is within 8 ft of the bag they step on it themselves (unassisted force), then throw | S-41 |
| Runner on 1st, grounder to 3B | 3B | 2B | 1B | 5-4-3 | S-42 |
| Runner on 1st, grounder to 1B near the bag | 1B | step on 1B (batter out) **then** 2B — now a **tag** because the force is removed | — | 3-6 tag; the runner may stop and return, and a rundown can start (§9.7) | S-43 |
| Runner on 1st, grounder to 1B away from the bag | 1B | 2B | 1B (P or 2B covers first) | 3-6-1 / 3-6-3 | S-44 |
| Runner on 1st, comebacker | P | 2B | 1B | 1-6-3 / 1-4-3 | S-45 |
| Runners on 1st and 2nd, grounder to 3B near the bag | 3B | step on 3B | 1B (or 2B if the batter is slow — the CPU picks the best makeable margin) | 5-3 / 5-4-3 around-the-horn | S-46 |
| Bases loaded, grounder to an infielder inside 60 ft | fielder | home (force) | 1B | 2-3 / 6-2-3; the catcher is the cover at home | S-47 |
| Bases loaded, grounder to 1B on the bag | 1B | step on 1B (batter out) | home — now a **tag** at the plate | 3-2 tag; the runner from 3rd can hold | S-48 |
| Runner on 1st, bunt popped up | C / P | catch | 1B (double off) | S-49 ✅  (`BuntScenarioTests`: the squared bunt popped 29 ft out is the catcher's; the throw back to first lands in 2B's glove, the bunt cover) |
| Runner on 1st, 2 outs | any | the **first makeable out** ends the inning; the CPU prefers the shorter throw | — | No DP attempt with 2 outs | S-50 |

Rules that fall out of geometry, and must not be tabled:

- The second throw is only an out if it beats the batter; a slow turn is a **fielder's choice** (one out, batter safe at first). Caption and stamp FIELDER'S CHOICE (✅ P8: `PlayStamp.FieldersChoice` from `PlayOutcome.FieldersChoice`; the first draft stamped OUT). ✅ P5 (the synthetic DP went with P3's Complete; `ApplyThrow` records the out first and narrates only what was recorded; `PlayOutcome.FieldersChoice` and the caption "Fielder's choice."
  come from the same typed facts; a chain of outs captions "Double play." / "Triple play!" ahead of its last decision; `PlayStamp.Label(PlayEvent)` reads the typed outs). One press is one throw on the human seat; the CPU steps on a force bag inside `fielding.throw.unassistedFt` instead of throwing (S-41, S-46).
- The force at second is removed the moment the batter is retired at first; any later play on that runner is a tag.
- The receiver must be on the bag: if the cover has not arrived (slow SS), the ball waits in the air — the out is late. That is how a **fast runner beats a DP**.
- A **neighborhood play** does not exist; the foot must be on the bag (6 ft occupancy radius is the arcade tolerance).
- Mini diamond updates as each out lands. ✅

## 10.5 Doubled off and the tag-up DP

- Liner or fly caught with a runner off the bag: the fielder throws to (or steps on) that bag; if the ball arrives before the runner returns, the runner is out. This is an **appeal-less force back**. Works at every bag. S-51..S-53.
- Fly ball, runner tags and goes, throw beats them: **tag** at the next bag (never a force). S-54 (sac fly thrown out at home).
- Pop-up dropped on purpose with runners on: no infield fly rule; forced runners must go. CPU runners stay on the bag, so the drop is a force at the lead bag only. S-55.
- **A firm catch is firm.** Once the batted ball is caught in the air it stays caught for the rest of the play. A glove that loses the ball afterwards — a throw that sails, a lob nobody covers, an item that knocks it loose — is possession changing, not the batted ball coming down. The retouch is owed only by a body that was off the bag **at the catch** (§9.5), and is judged **once**, on that catch. S-55b. ✅ (`LivePlaySystem.UpdateFly`).
  Note the rule is about a ball that reached the glove: a fly an out is awarded on *without* a glove (the chomper, §14) does not enter the caught state at all, and is not covered here.

✅ P5: the CPU reads the doubled-off race ahead of its table (`RunnerSystem.ReturnSec` against the throw to the start bag, or a walk onto it inside `fielding.throw.unassistedFt`); a human sends a runner into the doubled-off risk with the stick on that runner (LB on a ball in the air is tag-and-go, §9.5). A body owing a retouch is not settled: Time waits for it (§10.6).

## 10.6 When the play ends (Time)

`Time` is true when: three outs; **or** the ball is held by a fielder on the infield (inside the dirt / grass lip, `flight.classes.infieldLipFt` — the 100 ft of the first draft put 2B and SS on the grass) and not thrown, **and** every live runner is on a bag or out, for `TimeOnBagSec` (1.0), each on a bag of their own (two runners on one bag keep the play alive until one leaves it or is out, §9.1).
Nobody left to play on (every runner out or home) is Time wherever the ball is, and so is a ball lying at rest that nobody picked up once every body has settled. A home run ends at the crossing plus the trot. ✅ P3 (`InPlay.Time` over the bodies; a CPU outfielder holding a ball with everyone settled throws it in, §8.8 rule 5).

At `Complete`: runs = runners who crossed home before the third out (with the §1 force exception), outs already recorded, bags = where each runner stands. **No table placement.** ✅ P3 (`Match.SettleRunners`; `AdvanceHit`, `AdvanceTagUp`, `OccupiedDestBag`, `BatterDestBag`, and the `goto case Single` reclassifications are gone; walks, hits by pitch, homers, and ground-rule doubles are the only placements by rule). The stamp reads the bodies:
an out on the play stamps OUT (the batter safe at first behind it stamps FIELDER'S CHOICE, §10.4); no out, the batter's bag names the hit. Every CPU ball — `cli match`, `AutoPlay`, a cold `FinishAtBat` — runs through the same live ball (`Match.RunLive`), so there is one path.

## 10.7 Triple play

Three outs on one live ball by the rules above (liner, double off, double off; or force, force, tag). Stamp TRIPLE PLAY. ✅ P5: reachable through the forces alone (runners on first and second, a hard grounder beside third: step on third, the force at second, the throw to first — `OutsScenarioTests`), stamped from the typed outs, captioned "Triple play!".
