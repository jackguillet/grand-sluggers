# Shipped phases — roadmap tables and playbook output

> **Historical.** The exit tables of the roadmap phases that shipped, and what the Phase P playbook produced. The contract is [gameplay-spec.md](../../gameplay-spec.md); what is shipped now is [status.md](../../status.md).

## From the roadmap

### Phase P — Plays like Sluggers (code shipped 2026-09-12; gates open)

Parent epic: **#209** (Harbor Exhibition plays like a baseball game). Each row is one child epic with the spec sections it owns and the scenario ids that closed it. Rows were **serial** except P1 ∥ P2 and P8 alongside P7.

| Epic | Shipped as |
| --- | --- |
| P0 #562 | #571 rules tables · #584 typed outcomes + scenario harness · #573 live ball into the sim (closes #512) |
| P1 #563 | #586 cursor / timing · #587 pitch shapes, one crossing, aim tell (#533 #577) · #589 CPU tables, per-pitcher stamina, star skills from JSON · #592 pad verbs + book (#582) · integrated onto main by #593 · sitting: changeup hang/dump (#668 / #672) · swing `leadSec` 0.18 (#670 / #673) |
| P2 #564 | #588 3-D flight, fence, classes · #590 foul geometry and foul fielding (#575) · #591 positions from the lineup · integrated by #594 |
| P3 #565 | #595 runners are bodies, no leads, Complete places them · #596 extra innings, mercy, walk-off |
| P4 #566 | #597 the roll is dead, one throw model, CPU fielder table · sitting: liner FlyOut (#666 / #674) · OF glove by the roll (#667 / #678) · stand-up ring, dive at the rim (#669 / #679) |
| P5 #567 | #598 tag reach, close-play margin, rundowns, the DP matrix |
| P6 #568 | #599 steals and pickoffs as one live runner play |
| P7 #569 | #603 S-29 in CI, difficulty ladder, stars / MVP / items from tables, park window as data |
| P8 #570 | #600 stamps from typed outcomes (#578) · #601 cameras per class, the catcher stays put (#574) · #602 the book follows the verbs · sitting: liner shot (#665 / #675) · no throw laser (#689 / #699) |

Lesson from the day: stacked PRs must be opened **against `main`** (or the top of the stack merged into `main` at the end). P1 and P2 merged their parts into their own parent branches and `main` only had part (a) of each until #593 / #594.

#### Phase P exit — the three sittings (Jack)

The consolidated checklist is on #209. In order:

1. **Parity sitting (#534, closed 2026-09-24; the D7 call moved to #209)** — **played twice, not passed.** First: `95026535d3` (2026-09-13). Re-sit: `850dd95` with #628 body (2026-09-14). **D7 wait** (#677) until the next re-sit. Closes #563, #564 when Jack would keep the feel.
2. **Narrated half-inning** — every out has a reason you saw. Watch the outfield on every fly for #580 and the glove read (#609). Closes #565–#568 and the fielding notes. **Not run yet.**
3. **Three innings, then the book (#346)** — one pad, then two pads; Call time → How to play must be enough. Closes #209 and #342. **Not run yet.**

What sticks becomes a sitting-found child. Distill: file, append `data/agent/debug-protocol.json` in the fix PR, promote on the second firing.

##### First sitting children (2026-09-13) — all landed

| Child | Observed | Status |
| --- | --- | --- |
| #665 | Liner vs hopper HUD-off | ✅ #675 |
| #666 | Liner in the air is an out | ✅ #674 |
| #667 | OF glove by the roll, not the bounce | ✅ #678 |
| #668 | Changeup hang/dump | ✅ #672 |
| #669 | Ring too wide; rim dives | ✅ #679 |
| #670 | Swing `leadSec` 0.18 | ✅ #673 |

##### Re-sit children (2026-09-14, `850dd95`)

| Child | Observed | Status |
| --- | --- | --- |
| #683 | Bags brown, not white | ✅ #697 |
| #684 | Foul pops behind the batter | closed |
| #685 | Title captain arms-up X — remove | ✅ #694 |
| #686 | Select bobs through the dirt | ✅ #695 |
| #687 | Strip extras; size + color only | ✅ #698 |
| #688 | Runners cannot share a bag; per-runner send/hold | ✅ per-runner orders; two runners on one bag: the lead keeps it unless forced |
| #689 | Yellow throw-destination line | ✅ #699 |
| #690 | Stamp each event when it happens | closed; typed live stamps landed |
| #691 | Infielders scale up holding the ball | ✅ #700 |
| #692 | CPU doubled off on a clear fly out | closed; caught-fly state preserved |
| #693 | Research Sluggers field proportions; bat/throws too fast | closed research tracker; C80 work/gates continue in #715/#708 |

Also landed beside: plate bat (#560 / #671), turntable (#559 / #664), Esc/H (#629 / #663), shadow code (#646; visibility gate #645 still open), shared body (#628).

#### The original Phase P plan (for the record)

**Exit for the phase:** Jack plays three innings at Harbor on a pad against the CPU and can say, for every out and every safe, what decided it — a bag, a tag, a catch, a beaten throw, a mash. `cli match` over 50 seeds reads like baseball (spec S-29). No play is resolved by a roll or a caption (spec A.4, A.5 empty).

| Epic | Owns (spec) | Exit (scenarios) | Notes |
| --- | --- | --- | --- |
| **P0. One place for the rules** (#562) | §0, §15, §16, A.7 | S-90, S-91, S-92 | `data/rules/*.json` with a validator and load fallbacks; typed `PlayEvent` outcomes (no caption-string control flow); a headless **scenario harness** that runs recorded seat commands through `LivePlaySystem` without Unity; dead feel fields removed. Extracts the baseball that lives in `InPlayDirector` (relay chain, close play, CPU catch timing, steal phase) into the sim — this is #512, done as the first step rather than the last. |
| **P1. Pitch and swing contract** (#563) | §3, §4, §5, A.1 | S-01 … S-30 | Cursor = quality, timing = direction (D4). Windows 9 / 7 with a floor. Charge is worth ×1.25. Break capped at half a zone; charge/changeup nearly straight. Human pitch types reachable; rubber walk moves the crossing the same for both seats. HBP reachable. Bunt on the bat plane, foul bunt K. Per-pitcher stamina with `star-skills.json` costs. CPU pitcher/batter **tables** with tracking and no forced-miss clamp. Closes #533, #534, #535 as children. **Can run in parallel with P2** (different files). |
| **P2. Flight and the field** (#564) | §6, §7.9, §7.10, §7.11, A.2 | S-20 … S-24, S-56 … S-59 | 3-D flight with directional wind, fence polygon, wall carom, ground-rule double, foul lines and foul-fly catches, one batted-ball class table, positions from the lineup diamond. |
| **P3. Runner model** (#565) | §9, §10.6, A.3 | S-36 … S-39, S-77 … S-82 | Runners are bodies with positions on the path, one speed formula, a batter-runner, per-runner send/hold, no passing, auto-return on flies, slide that matters. **D1: leads retired** (verb, pips, `Lead01`), how-to-play updated in the same PR. `Complete` scores by who crossed before the third out and places runners where they stand — `AdvanceHit` / `AdvanceTagUp` / `OccupiedDestBag` deleted. Extra innings, mercy. |
| **P4. Fielding decides by geometry** (#566) | §8, §7.1 … §7.8, A.4 | S-31 … S-35 | Kill the roll at `Fielding.cs:146`. CPU catch is a radius at the window, never force-fed. One throw model that flies the ball and judges the bag; lateral misses are the error; uncovered bags lob until the cover arrives. Reaction lockout, cover / cutoff / relay / backup by geometry. CPU fielder **decision table** by makeable margins. Thrower stays put; YOU hands to the receiver. ERROR stamp. |
| **P5. Outs, double plays, close plays, rundowns** (#567) | §10, §9.6, §9.7, A.5 | S-40 … S-55, S-73 … S-76 | Every row of §10.4 as a scenario for the human seat and the CPU seat. Fielder's choice. Force removed when the batter is retired. Doubled-off and tag-up throws. Close-play prompt **only inside the margin at 3B/home** (D5), verdict written once. Rundowns. Triple play reachable. `Retire` result honored. |
| **P6. Steals and pickoffs** (#568) | §11, §4.5, A.6 | S-60 … S-72 | Arm any runner, break at release, perfect steal (D2), pickoff catches only a runner who broke (D3), no random pickoffs, double steals including first-and-third, steal of home (D10), catcher throw as a normal throw with a tag. CPU steal and pickoff tables. |
| **P7. Balance and CPU** (#569) | §4.8, §5.9, §8.8, §9.9, §11.6, §12, §13 | S-29 over 50 seeds; difficulty ladder | Tune `data/rules/` until `cli match` distributions read like a 3-inning arcade game; difficulty as multipliers in `cpu.json`; star gains / MVP / star-skill values read from JSON. Remove the item auto-throw roll; items become field effects with geometry. |
| **P8. Presentation contract** (#570) | §15 | every stamp and camera in the table | Stamps ERROR / FIELDER'S CHOICE / PICKED OFF / SAFE; cameras per batted-ball class from `shots.json`; the booklet's Running and Fielding spreads rewritten for the new verbs; `HowToPlay.cs` and `docs/how-to-play.md` together. Runs alongside P3–P6 as each verb lands, not after. |

**Human gates inside Phase P.** After P1: #534 (played twice; remaining re-sit children then D7). After P5: narrated half. After P7: three innings, then #346. Agents do not pass these.

**Banned during Phase P.** New parks, new captains, unique meshes, Challenge, motion, online, a second input toolkit, growing `MatchDirector` or `InPlayDirector`, any `Random` outside the sim's seeded `_rng` that produces a `PlayEvent`, any new number in C# that belongs in `data/rules/`, any caption that decides a play.

---

### Phase A — The at-bat sells (shipped as presentation; rules move to P1)

**Exit:** A still of the pitch and a still of the swing would not embarrass Harbor on a trailer. A stranger learns the timing window in two at-bats.

| Epic | Why | Agent notes |
| --- | --- | --- |
| **A1. Plate and mound as film** | Shots are data; framing still drifts (cap close-up, square plate). Tune `data/feel/shots.json` and HarborKit empties, not new `Vector3`s in code. | One agent. F2 overlay on. Human screenshot. |
| **A2. Batter and pitcher read as bodies** | Shipped: one shared rig, baked takes. Captains are **size + color** (#687 / #698); extras off until they read as toys. | Tune takes against the reference (#558). |
| **A3. Swing and pitch are verbs you can name HUD-off** | Contact, load, release hit `Contact` / `Release` marks. Charge and smash freeze live in `data/feel/table.json`. | The *rules* behind the verbs are P1. |
| **A4. Ball is a baseball** | Seams, spin, shadow, dirt hops — hold the bar on plate cam. | Small. Skeptic with the cap-blob failure in mind. |

---

### Phase B — In-play is a scene (shipped as presentation; rules move to P3–P5)

**Exit:** Grounder, line, fly, tag are different pictures. The glove and the slide are on the body.

| Epic | Why | Agent notes |
| --- | --- | --- |
| **B1. Grounder theater** | Scoop, hop chase, throw, beat the runner — the movie exists; the race underneath is P4/P5. | Training drill "grab a grounder" is the test harness. |
| **B2. Fly / line / tag cameras** | Named shots `diamond`, `throw`, smash override. Bag tell stays. | Data + CameraDirector. |
| **B3. Clip-proof** | Shipped: authored takes play from the catalog. | Fill slots in parallel. |

---

### Phase C — Harbor looks expensive (shipped)

| Epic | Why | Agent notes |
| --- | --- | --- |
| **C1. HUD-off specials for six captains** | Two seconds, then baseball. No full-screen blinds. | Parallel per event. Values from `star-skills.json` land in P7. |
| **C2. Harbor kit pass** | Placed objects; Hierarchy names are the API. | |
| **C3. Audio identity** | Original bat / glove / crowd shipped (#223). | Human picks the crack. |
| **C4. Broadcast HUD that can shut up** | Scorebug is the product. | Stamps for the new play results are P8. |

## From the playbook

### What it produced (2026-09-12)

| | |
| --- | --- |
| Sessions | 10 (P0–P8 and the seat fix #579), one worktree each |
| PRs merged to `main` | 30, #561 → #604 |
| Issues opened | 23 (9 epics, 10 sitting children, spec/roadmap threads) |
| Rules moved to data | `data/rules/` batting, pitching, flight, fielding, running, stars, cpu, match |
| Tests | ≈ 800 (from ≈ 600), including the scenario harness S-01 … S-92 and S-29 over fifty seeds |
| Headless game | seed 7: 8–0, items on half the hits → 10–3 with walks, a sac fly, a ground-rule double, pickoff beats; fifty seeds 2.5–2.7 runs a side |
