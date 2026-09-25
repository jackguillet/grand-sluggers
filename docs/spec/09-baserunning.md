# 9. Baserunning

## 9.1 The runner model

- Each runner (including the batter-runner) is an object with **position on the basepath** (bag index + feet along the segment), **velocity**, **state** (`OnBag`, `Advancing`, `Returning`, `Stealing`, `Sliding`, `Out`, `Scored`), a **destination bag**, and a **forced** flag snapshotted at contact. ✅ P3 (`Runner`, `RunnerSystem`; `Match.Runners`, `Match.BatterRunner`; the three nullable slots and `RunnerState` are gone, and Complete re-seats the same objects instead of rebuilding them).
- Speed: `bagSec = 3.41 − Run × 0.092` (clamped 2.45–3.65) per base, **one formula** for every segment (`running.bagSec`). Elapsed pace is the anchor, so on the 80-ft path a Run-5 body runs 27.12 ft/s. The fastest runner on the roster (Run 9, 2.58 s a base) is 1.25× the slowest (Run 2, 3.23 s) (CH-10). A runner runs his path at that speed from the first frame; the body-class ramp is the fielders' (§8.1). The batter-runner starts **0.5 s after contact** from where they stood in the box (reference: 31 frames); the box is a few feet closer to first for a left-handed batter, so they arrive about 0.1 s sooner by our geometry (the reference's 0.5 s is its box placement, not ours). Dash ×1.12 at full mash. ✅ P3 (`RunnerSystem.BagSec` / `SpeedFtPerSec`; `homeToFirst` and `bagToBag` are gone).
- **Runners cannot pass each other.** Between bags, a trailing runner inside 27 ft of the runner ahead stops behind them (`running.bagSec.noPassFt`). A runner ahead standing on a bag can be run up to: the trailing runner reaches that bag and stops on it, and never passes the body there. ✅ P3
- **Two runners on one bag** (OBR 5.06(a)(2)). Two runners may not occupy a base. When two stand on one bag, the preceding runner (the one who started the play further along) is entitled to it, and the following runner may be put out by a tag there. When the preceding runner is forced off the bag (the force at the next bag still stands and the fly was not caught), the following runner is entitled to it, and the preceding runner is out when tagged there, or by the force at the next bag.
  One query decides it (`RunnerSystem.EntitledOn` / `Protects` / `Unentitled`), and every tag reads it: a bag protects a body only when no other runner standing on it is entitled over that body. The body the bag does not protect is not settled: the play does not end while two live runners share a bag (§10.6). It is resolved by the bodies: the runner goes back to the bag behind, the lead runner moves on, or a glove tags him.
  The CPU runner goes back when the bag behind is free (nobody on it or bound for it) and otherwise waits on the bag; RB (return) sends a human's runner back to the bag behind. A CPU glove holding the ball runs at that body like any stray body (§9.7).
  The client draws the body the bag does not protect `runnerShareStepFt` (data/feel, 6 ft) off the bag — toward the bag behind, or toward the next bag for the forced runner — so the two bodies never merge (`Runner.DrawPosition`); the sim position stays on the bag.
- **A force is one bag.** A forced runner is forced from the bag he started the play on to the next one. Reaching it ends his force, even while the force ahead of him (the next runner's) still stands (`RunnerSystem.ForcedOff`).
- A runner's position is what every tag, force, and arrival uses. No closed-form "beats" re-derivation. ✅ P3 (`RunnerSystem.ArrivalSec(runner, bag)` is the sim query P4's fielder reads; `BatterBeatsThrow`, `RunnerBeatsTag`, `RelayBeats` are gone).

## 9.2 No leads (D1)

- Runners stand on the bag until contact, a steal break (§11.2), or a send. There is no lead stick, no lead pip, no pickoff risk from standing there. The stick-toward-a-bag verb during SET **starts the selected runner immediately** (same as L3); elapsed time moves that body toward the destination. ✅ P3 (`Runner.StealArmed`; P6 lands the break).
- `Lead01`, `TakeLead`, `ReturnToBag`'s walk-back, `LeadSpot`, `MiniLead`, the Unity lead rates, the lead pips, and the Lead chapter of how-to-play are retired. ✅ P3. The random pickoff on a walking lead (`ResolvePickoff`, `pitching.cpu.pickoff`) went with them (D3): a runner on the bag is always safe; a pitcher throw can target any bag, while a runner touching a bag remains safe from a tag.

## 9.3 Send / hold per runner

Controller orders have explicit selected-runner or ALL scope. Selection follows a runner identity through contact; an empty, retired or scored selection never broadens to ALL. Advance, return and halt have independent meanings: return immediately reverses, not a tap-to-halt. Changing selection under a held order requires release and a new press. Halt suppresses held orders until release; simultaneous advance and return halt. The same command boundary operates during SET, pitch flight and live play. The legacy literal-input replay path remains available to existing recordings.


- **Selection**: right-stick flick names runner from 1B/right, 2B/up, 3B/left, or batter/down after contact. Recenter before another flick. D-pad Down selects ALL. A sequence begins at ALL; individual identity persists through contact.
- **Orders**: LB advances/steals the selection immediately; RB returns immediately; D-pad Up halts. Both bumpers also halt. Releasing one of two held bumpers cannot resume an order. Left stick has no runner-order meaning. ALL advance retains tag-and-go on flies; an individually selected runner can deliberately depart early and return.
- Forced runners cannot be held on a grounder once the batter reaches first (they are forced off). A human "hold" on a forced runner is ignored until the force is removed (batter out at first). ✅ P3 (`Runner.Forced` against the live force chain).
- The **batter-runner** is selectable (down / 4 while running) and obeys the same send/return: round first and go, or stop at the bag. ✅ P3

## 9.4 Dash, slide, rounding

- Dash: mash South, +0.28 per press to 1.0, decays 0.5/s; ×1.12 speed at full. ✅
- Slide: automatic on the last 12 ft into a bag when a tag is threatened (throw armed to that bag or a glove with the ball within 20 ft) and the runner is stopping at that bag; a runner rounding never slides (reference). West near the bag forces it. A slide shrinks the tag reach by 2 ft; it does not change arrival time. ✅ P3 (`running.bags.slideFt / slideThreatFt / slideReachCutFt`; `InPlay.Touches(sliding:)`).
  The slide's length is `slideFt` × the `body.slideMul` of the ground the bag stands on, for the automatic slide and the forced one alike (`RunnerSystem.SlideFt`, FD-04 B); ✅, **1.0** on every row. It is not behind the response law, so it acts on both roots.
- Rounding: a runner heading past a bag runs a shallow arc (presentation) and reaches the next bag at the same `bagSec` — no time penalty in the arcade rule. ✅ P3
- Overrun first: the batter-runner may run through first base and is safe from a tag while returning directly, unless they turn toward second (then live). ✅ P5 (`Runner.OverrunFt`, `running.bags.overrunFt` 12: the body runs through on the line from home and comes straight back, holding the bag and untouchable the whole way; Time waits for the return; sent on while out there they turn back at once and are live).
  The run-through's length is `overrunFt` × the `body.overrunMul` of the ground first stands on (`RunnerSystem.OverrunFt`, FD-04 B); ✅, **1.0** on every row, on both roots. Every other body stops on the bag it is stopping at; a runner who overruns any bag is FD-04 C, held.

## 9.5 Fly balls and tag-ups

- On a catchable fly/liner the game **sends every runner back to their bag** (reference: automatic return on a fly). A human can override with a send, at the doubled-off risk. ✅ P3 (`FlyState.InAir` holds every runner but the batter, who runs to first and waits there).
- Runners **cannot leave until the ball is firmly caught** (post-bobble). After the catch, a runner on the bag may advance (**tag up**). A runner off the bag at the catch must return and touch before advancing; if the defense throws to that bag and the ball beats them back, they are out (doubled off, §10.5). ✅ P3 (`Runner.LeftEarly`; `ThrowVerdict.DoubledOff`).
- All-advance pressed *before* the catch means "tag and go on the catch" — the runner waits on the bag and leaves at the catch. ✅ P3 (`Runner.TagAndGo`, per runner). LB orders runners before and after contact. ALL advance on an airborne fly arms tag-and-go; individually selected runners may deliberately depart and return.
- A lone runner cannot cross home on a fly with < 2 outs until the catch/drop resolves (reference restriction; keeps a dropped fly honest). ✅ P3 (the body is held a foot short of the plate).
- CPU: **a tag-up is a race, not a distance.** At the catch a runner on third (with < 2 outs) or second goes when `margin(next)` clears `tagUpHomeMarginSec` 0.25 / `tagUpThirdMarginSec` 0.07 plus the rung's `runnerMarginSec`; the carry gates are never (9999). Batter-runner on a fly stays near first until the drop/catch. Batter-runner on a fly stays near first until the drop/catch. ✅ P3 (`running.cpu.tagThirdMinCarryFt / tagSecondMinCarryFt`).
  ✅ (race decision 5): **a tag-up is a race, not a distance.** At the catch — once, `RunnerAiContext.AtCatch` — a runner on third or second also goes when `margin(next)` (§9.9, which reads the arm and the relay) clears `running.cpu.tagUpHomeMarginSec` / `tagUpThirdMarginSec` plus the rung's `runnerMarginSec`.
  The shipped table sets those to 99, a margin no play reaches, so it decides by the two gates alone as it always did; the `c80` copy sets the gates to 9999 and authors 0.25 / 0.07 from a 108-race sweep, placing the hard rung on the measured crossover (+0.10 home, −0.08 third). A body held at the catch is not sent by a later event.

## 9.6 Close plays

- A close play is a **geometric** condition: the throw arrives within ±`closeMargin` (0.25 s) of the runner at a tag bag (3B or home; a force is never close-played). Only then does the **mash contest** run: the icon appears, first press after the icon wins, CPU reacts at `0.20 + (10 − stat) × 0.032`, where the stat is the runner's Run or the tagger's Hands (§2). Outside the margin the geometry decides and no icon appears. ✅ P5 (`ClosePlay.WithinMargin`, `running.close.marginSec` 0.25):
  the ball on the bag **ahead of the body** by no more than the margin runs the mash; the body **in ahead of the ball** by no more than the margin is safe on the bag (§10.3) and pops the small SAFE, no contest; further out either way the geometry decides silently — the tag at the bag, or the runner in. Once one seat has pressed and the clock is past that press the other seat can only be later, so a seat that never presses loses to the CPU's reaction (S-75).
  The verdict is written once (the out is recorded, or the body is placed on the bag; the caption follows the record; `ClosePlaySafe` is gone).
- Stamp SAFE (small) / OUT.

## 9.7 Rundowns

- A runner caught between bags (a fielder with the ball inside 20 ft on the path, the runner not on a bag) is in a rundown: the fielder runs the runner toward the bag they came from; covering fielders take throws; the runner may reverse (stick). The tag rule (§10.3) ends it. A rundown ends by tag, by the runner reaching a bag, or by a throw that misses (runner advances).
  ✅ P5 (`running.rundown.rangeFt` 20; `LivePlaySystem.ReadRundown` flags the body each frame, `Runner.InRundown`; `LiveEvent.Rundown` cues it). A glove standing on the bag a body is still closing on is not a rundown: it waits there and the tag at the bag decides (§10.3). A body that stops or turns away is caught.
- CPU runner in a rundown reverses when the ball is thrown to the bag ahead of them and the bag behind is still reachable ahead of the ball's next leg (the §9.9 margin read from where the throw lands); a throw to the bag behind sends them on the same way. A body the ball beats both ways keeps going and takes the tag at the bag rather than running into the glove that has it.
  CPU fielders run at the runner — with the ball behind them the body is trapped — and, inside the rundown range, throw ahead to the covered bag at the last makeable moment (the body's arrival inside the §8.8 margin of the throw's, the cover's walk included, while the throw still lands inside the close margin — a body the throw cannot beat is run at, never thrown behind), at full speed;
  a throw that races nobody to its bag, with every moving body ≥ 80% of the way to a bag, is a lazy lob (reference). ✅
  the reference's fixed 8 ft throw could not beat this throw clock (a 21 ft throw is 0.40 s, a Run-3 body covers 8 ft in 0.28 s) and its lob was always lazy (a body 8 ft short of a bag is past 80%), so a slow runner the glove could not gain on was never retired (`RunnerAi`, `LivePlaySystem.TickCpuRundown`, `LazyLob`; `running.rundown.throwWithinFt` is gone, `lazyLobFraction` 0.8 / `lazyLobSpeedMul` 0.5 stay). A CPU glove with nothing makeable on the table runs at a stray body wherever it stands:
  a runner frozen on the path is never left standing (Time needs every body on a bag, §10.6). A runner standing on a bag another runner is entitled to (§9.1) is a stray body too.
- Runners stay on the basepath (no running off the line to dodge — the reference exploit is closed by clamping the runner to the path). ✅ (the bodies are one-dimensional on the path).

## 9.8 Scoring and the play's end — see §10.6.

## 9.9 CPU baserunner decisions

Evaluated at contact, at every fielder touch, and at every throw release (events, not per frame). For each runner, `margin(next) = throwArrival(next) − runnerArrival(next)` using the fielder currently on (or nearest) the ball, their arm, and a reaction of 0.35 s.

| Situation | Go if | Notes |
| --- | --- | --- |
| Forced on a grounder | always | Forced one bag only (§9.1) |
| On a bag another runner is entitled to (§9.1) | back to the bag behind when it is free; otherwise wait on the bag | The forced runner leaving is the force row |
| Unforced on a grounder to the infield | margin(next) > 0.4 and the ball is not in front of them | A runner on 2nd does not run at a grounder to SS/3B in front of them |
| Runner on 3rd, grounder, < 2 outs | infield back (1B, 2B, SS or 3B meets the ball at or behind his own depth from home; the pitcher and the catcher never) or margin(home) > 0.3 | "Contact play" with 2 outs: always go |
| Hit through / to the outfield | margin(next) > 0.5 − Run × 0.03 | Aggression by Run; two outs: +0.3 (go more) |
| Batter-runner rounding first | margin(2B) > 0.6 − Run × 0.03 | Reads the pickup: ball behind the outfielder = go |
| Fly ball | hold; tag-up rules §9.5 | ✅: the tag-up is `margin(next) > tagUp*MarginSec + runnerMarginSec`, read once at the catch |
| Score situation | trailing by ≥ 3 in the last inning: thresholds −0.2 | Aggressive when desperate |
| Steal | §11.6 | Not in the swing function. A body on its steal segment never turns back on the catcher's read (only the rundown reverses it, §9.7) ✅ P6 |

Reference shape for the "go" rule: keep going if time-to-bag < throw-time − 0.33 s (−0.5 s on easy), with a bonus once past 40% of the segment; otherwise turn back with a 12–20% chance of a mistake. ✅ P3 (`RunnerAi`, `running.cpu`: the thresholds above, `commitFraction` 0.4 for the turn-back, the mistake roll left out — no roll decides a runner). Difficulty adds `cpu.*.runnerMarginSec` (+0.15 easy, −0.15 hard) to every threshold. `throwArrival` is the defense's live read:
the glove on (or the route to) the ball, `running.cpu.reactionSec` 0.35, then `InPlay.ThrowSec` over the distance. ✅: the runner reads the defense's own plan through `BallSituation.ThrowClock` — as much of the thrower's arm (`cpu.*.runnerReadsArm`), of the relay the fielder would take (`runnerReadsRelay`) and of the pair chemistry (`readsChemistry`) as the rung reads. Easy reads half the arm and no relay; normal and hard read the arm, the relay and the pair chemistry in full.
