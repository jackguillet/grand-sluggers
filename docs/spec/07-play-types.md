# 7. Play types — what happens on each

Each subsection is one scene: **who fields**, **what runners do**, **the throw**, **the camera and stamp**. "Runners" means offense-controlled runners; a human presses, CPU follows §9.9. Fielder decisions are §8.8 for CPU; a human fielder has the verbs in how-to-play.

Common to all live plays:

- **Batter always runs** on fair contact. ✅
- **Forced runners run** on a grounder (they have no choice). Unforced runners hold on the bag until the ball is through or fielded, then go/hold by the send rule (a human) or the margin table (CPU, §9.9). ✅ P3 (`Runner.Forced`, `RunnerAi`); the read step is presentation.
- **On a fly / liner**, all runners hold on the bag until the catch or the drop (tag-up rule §9.5). ✅ P3 (`FlyState`, per runner).
- **The throw** goes where the fielder names (human) or where the decision table says (CPU, §8.8). The out is judged when the ball arrives (§10). ✅ for named bags.
- Camera: `diamond` 45° on the dirt under the ball; a liner sits between that and the fly (`diamond-line`); fly pulls back to `diamond-fly`; a throw does not cut behind the thrower (`data/feel/shots.json`). ✅ 
- Stamp: OUT / SINGLE / DOUBLE / TRIPLE / HOME RUN / DOUBLE PLAY / TRIPLE PLAY / FOUL / ERROR when the play is dead. ✅ P4: ERROR is the throw that skipped past its cover (`PlayOutcome.Error`, `PlayStamp.Error`); it stamps on the hit it allowed, never on an out.

## 7.1 Grounder to an infielder (routine)

- **Fields**: the infielder whose planned route meets the ball earliest (`FieldingPursuit.Choose`) — 1B/2B/SS/3B, P on comebackers, C on toppers. Gloves scoop by touching the ball on the dirt (no button). ✅
- **Runners**: batter to first; forced runners go; unforced hold at the read step, then advance only if the throw goes elsewhere and they can beat a relay (§9.9).
- **Throw**: nobody on → 1B. Runner on 1st → 2B for the force (then 1B if time, §10.4). Runners on 1st and 2nd → 3B if the fielder is 3B/SS near the bag, else 2B. Loaded → home if the fielder is inside 60 ft of the plate, else 2B. Human names the bag; the default follows this table. ✅ default bags; ✅ P4: the CPU throw is the decision table (§8.8) from the live bodies; the roll is gone and nothing is force-fed at hang (the resolver's `Kind` is only what the flight decides alone — a homer, a foul, a chomp — or `PlayKind.InPlay`).
- **Out**: force at the bag if the ball (in a glove on the bag) arrives before the runner. Tie to runner. Bobble (§8.6) adds time; it does not decide.
- Stamp OUT (one out) / FORCE OUT caption / SINGLE if the runner beats it (a fielder's choice or an error — stamp ERROR if the throw sailed).

## 7.2 Slow roller / topper

- **Fields**: P or C, or a charging 3B/1B. Bare-hand pose if the fielder is running toward home.
- **Runners**: batter races; forced runners usually safe (the throw goes to 1B by default because the force at 2B is not makeable — the decision table computes margins, §8.8).
- **Throw**: 1B unless a runner on 3rd is going home and the fielder is inside 45 ft (then home).
- Beat the throw → infield single (stamp SINGLE). ✅ P4 (S-32): the arrival compare, never a roll.

## 7.3 Bunt

- Defense tell: the batter squares at the bunt press (§5.8); 1B and 3B **crash** (charge 25 ft toward the plate), 2B covers 1B, SS covers 2B, P and C charge the triangle. ✅ (`BuntDefense`, `fielding.bunt`):
  the square is the swing's `SquareSec` (the client's West clock for a human, `batting.cpu.sacBuntSquareSec` for the headless CPU, read at SET so a human pitcher sees the squared bat and the corners running in before the pitch);
  `BuntDefense.Spots(held)` is where every body stands after that long on the square — the crash bodies run toward the plate at their own chase speed (§8.1) up to `crashFt`, the covers walk to first and second at the cover speed (§8.7) — and the Unity presenter draws it during SET and the pitch from the same function the live ball seeds its bodies from at contact (`LivePlaySystem.InitGloves`, `Match.PreviewHit(hit, swing)`).
  After contact every charge body converges on the ball to `chargeStopFt` unless a play stands at the bag it covers (the catcher stays home on a squeeze). Hand-offs stay events (§8.9): a human who takes a crashing body with the stick keeps it.
- **Fields**: earliest of P / C / 1B / 3B. ✅ (`FieldingResolver.BuntPursuitPositions`; the routes start from the square's bodies, so the crashing corner beats the pitcher to a bunt down its line that would have been the pitcher's from the rest spots).
- **Runners**: batter runs; forced runners go (sac). Runner on 3rd with a squeeze: goes at contact only if the offense sent them (`send 3B`), else holds. ✅ (`BallSituation.Bunt`: the CPU runner from third holds until a glove has the ball; the send is the human's stick).
- **Throw**: 1B (batter) by default. Lead runner if the bunt is popped or too hard and the margin is makeable. Runner from 3rd on a squeeze → home only if inside 30 ft. ✅ (`LivePlaySystem.CpuDecide`, the bunt rows ahead of §8.8 rule 1: home only from inside `fielding.bunt.squeezeHomeFt`, the lead force only on a bunt at or above `hardExitMph`, else first; a popped bunt is a pop, §5.8, and the doubled-off race is its row). The throw to first lands in the second baseman's glove: the bunt cover map (`BuntDefense.CoverMap`) is the diamond's with the middle behind the crash.
- Bunt pop-up caught → out; runners who left are doubled off if the fielder throws back (§10.5). ✅ S-49 (`BuntScenarioTests`).
- Stamp BUNT + SINGLE / OUT. ✅ stamp exists.

## 7.4 Chopper

- High first bounce (§6.2). The fielder waits at the second hop (`Rolling`, first reachable point) or charges to take it on the short hop — the human chooses by stick; CPU charges if the runner's margin at 1B is < 0.3 s.
- Runners: as grounder. Choppers are the classic infield single.

## 7.5 Grounder through the infield (to the outfield)

- Infielder misses (no route reaches) → outfield hand-off: LF/CF/RF charge the roll (`HandoffToOutfield`). ✅
- **Runners**: batter rounds first and reads the outfielder (send to 2B if the pickup is deep and the arm is weak, §9.9). Runner on 1st → 3rd if the ball is to RF/CF and picked up beyond 200 ft, else 2B. Runner on 2nd → home unless the ball is hit to LF shallow and the arm is strong. Runner on 3rd scores. All by the margin formula, not a table. ✅ P3 (`RunnerAi.Margin` at contact, at the pickup, at each throw, at each bag).
- **Throw**: outfielder throws to the base *ahead* of the lead runner if makeable, else to the **cutoff** (§8.7) to hold the batter at first. A throw home goes through the cutoff unless the arm can reach on the fly.
- Runner thrown out at a base = tag (unforced) or force (batter at 2B when an outfielder throws there? no — the batter is only forced at 1B). ✅ tag/force distinction.
- Stamp SINGLE / DOUBLE; OUT at a bag stamps OUT with the caption naming the throw.

## 7.6 Line drive

- **Fields**: the infielder or outfielder on the line if a route meets the live ball within the catch-height envelope before the first surface contact. Liners are a **jump or dive** verb inside the window; a straight-at-you liner is a South catch.
- **Caught**: out. The glove on the live ball before the bounce is the catch, not a scoop at the landing ring. Runners who left the bag are **doubled off** if the fielder throws to that bag (or steps on it) before they return (§10.5). Runners at the read step are safe if they return in time — that is the tension.
- **Not caught**: it bounces or skids from its actual impact; the outfielder whose route meets the roll earliest chases it (D16), not the body nearest the bounce; runners as §7.5.
- **One flight clock:** every batted ball uses `flight.timeScale` **1.65**, independent of launch, exit speed or descriptive class. Gravity and velocity produce the different hang times. Neither `linerTimeScale` nor `dirtTimeScale` is a supported rule key. Throws and pitches keep their own clocks.

## 7.7 Pop-up (infield fly)

- **Fields**: the infielder or catcher whose route reaches the landing earliest; the P never takes a pop if anyone else can. Camera pulls back (`diamond-fly`).
- **Runners**: hold at the bag (CPU) or wherever the human puts them. No infield fly rule: a **dropped pop** is live, and forced runners are forced. CPU runners stay on the bag on a pop so a drop costs at most the force at the lead bag.
- **Caught**: out. Send after the catch = tag-up (rarely wise).
- **Dropped** (bobble, §8.6): live; the fielder picks up and throws by the grounder table.

## 7.8 Fly ball (outfield)

- **Fields**: LF/CF/RF by route to the landing; CF has priority on a tie. Runs to the **landing**, not the ball. ✅ (`FlyCatch.ChaseTarget`). Jump/dive windows from Field and ability (§8.4).
- **Runners**: hold; tag-up on the catch if sent (§9.5). A runner on 3rd with < 2 outs tags on any fly caught ≥ 200 ft from the plate (CPU rule; human decides).
- **Caught**: out (routine / DIVE / JUMP stamp). Throw after the catch to the bag ahead of a tagging runner; the margin decides (§10.2).
- **Dropped**: live, runners go by the outfield-single rule.
- **Sac fly**: the runner from 3rd scores if home arrival < throw arrival. It is a live throw, can be an out. ✅ P3 (the body tags at the catch and races the throw; `AdvanceTagUp` and the 230 ft literal are gone).

## 7.9 Wall ball / carom

- A fly or liner that meets the fence below fence height caroms (restitution 0.48, angle mirrored) and drops at the base of the wall. The outfielder plays the carom (route to the first reachable point on the post-carom path). ✅ (`BattedBallClass.Wall`; `LiveEvent.WallCarom` is the thump the client plays; S-58). ✅ The carom's two numbers are the row of the wall material the segment is made of (FD-06):
  `walls.json` `padded` (0.48 / 0.82); the same carom off the `padded` row at two values follows each (`GroundReadTests.SF12_…`). ✅ Per span:
  `WallMaterial.OfSegment` answers the material of the polyline span the piece lies on (`padded` when the span names none, for the foul rail, and for every park without points), and a ball caroms off that span's own normal — a notch face that is not square to home sends the ball along it (`PolylineFenceTests.SF07_ABallRolledIntoTheNotchCaromsByThatSpansNormal`, `…SF12_EachSpanAsksTheLibraryForItsOwnMaterialsRow`). Below the top means below the span's top where the ball met it.
- Runners: this is the **double / triple** scene. Batter reads the carom; runner on 1st scores on a carom to the gap with < 2 outs if the margin says so. ✅ P3: the bodies take what the carom and the arm give (S-58 asserts second or third, never a dead double).
- Rob: in the window at the wall, West (jump) can catch a ball that would clear the fence by ≤ the leap's rob height: the plain jump's, Wall Spring's, the park's climb wall's or a Buddy Jump's (§8.4). ✅ (`FlyCatch.CanRob` against `BattedBall.FenceClearFt`; S-56, S-57). ⚠️ `BattedBall` still measures that clearance against the park's `fenceHeightFt`, not the top of the span the ball crossed. No park names points, so nothing plays differently; the first park whose spans stand at other heights (or the child that makes a span robbable) moves that read to the crossing's top.

## 7.10 Home run

- Fence crossed above fence height between the poles. Dead ball. Batter and all runners circle at trot speed; scoring is immediate (the throw cannot happen). Stamp HOME RUN / GRAND SLAM. ✅ (the crossing is the one homer rule, P2)
- The ball keeps flying into the stands (presentation). Night: fireworks (Harbor).

## 7.11 Foul ball

- Foul grounder / foul pop: dead when it lands or leaves the field, **unless a fielder catches it** (foul fly out: C, 1B, 3B, LF, RF near the lines). ✅ P2: the foul pool is `FieldingResolver.FoulPursuitPositions`, routed by the same planner; the call is made where the ball lands, rolls foul, rests, leaves, or is first touched (`BattedBall.DecidedT`, `LivePlaySystem.Call`). A touch on fair ground before the roll went foul makes it a fair ball and the play goes on.
- Runners return. A caught foul fly is a fly ball for tag-up purposes. ✅ (it is `PlayKind.FlyOut`)
- **The pop behind the plate** comes from real contact under the ball (§5.4): the catcher is in the foul pool and catches it coming down for a fly out (S-24c); a glove that is not under it lets it land foul, dead, runners return, a strike under two (S-24d).
- **Off the bat.** No glove takes a batted ball until it has been `catch.offTheBatFt` **10 ft** (3-D) from where it left the bat, or has come down to the ground (`FlyCatch.OffTheBat`, `LivePlaySystem.GloveMayTake`). The catcher stands 15 ft back inside his own catch radius of the plate, so without this a ball straight off the bat would be caught on the contact frame; straight into the mitt is a foul tip, not a catch. A bunt on the dirt in front of the plate is fieldable at once.
- Stamp FOUL when dead, within the count hold (S-24b). The play never hangs: a foul is a dead-ball result the sim commits itself. ✅ P2

## 7.12 Strikeout / walk / HBP with runners

- Strikeout: dead. A runner stealing on the pitch is a **catcher throw play** (strike-em-out-throw-em-out DP, §11.5). ✅ steal throw after a miss.
- Walk: batter to first; only forced runners advance. ✅
- HBP: as walk. ✅ placement; ✅ geometry (P1: `batting.hbp.bodyRadiusFt` 0.45 around the body the box walk moves, S-16 / S-17).
