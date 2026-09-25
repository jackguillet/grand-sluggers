# 3. The at-bat state machine

One pitch is one cycle. Times are seconds; the feel numbers are `data/feel/table.json` and are quoted for orientation.

```
SET ──(pitch commit)──▶ WINDUP ──(release @0.42)──▶ FLIGHT ──▶ JUDGE ──┬─▶ DEAD (take/miss/foul/K/BB/HBP) ─▶ STAMP ─▶ SET
                                                                        └─▶ LIVE (contact) ─▶ ... ─▶ COMPLETE ─▶ STAMP ─▶ SET
   ▲                                                                                                                       │
   └────────────────────────────── pickoff (SET only) ─▶ PICKOFF PLAY ─▶ STAMP ────────────────────────────────────────────┘
```

| Phase | What may happen | Who acts | Exits |
| --- | --- | --- | --- |
| **SET** | Pitcher **selects the ordinary pitch** (cycle button, below) and walks the rubber, batter walks the box, offense selects a runner with a right-stick flick and orders LB advance/steal, RB return, D-pad Up halt, defense arms a pickoff. Either side holds LT at RT release for a Star (§12). Runner orders remain independent of Stars in every phase (§9.3). Pitcher readiness beat `pitcherReadySeconds` (0.55). Runners stand on their bags (D1). | Both | Pitch commit (release of RT at the mound). Pickoff (bag + RT). Call time. |
| **WINDUP** | Delivery animation. Batter may still walk the box and start a charge. A steal armed inside the first 0.25 s is a **perfect steal** (D2). Runners with a steal armed break at **release** (perfect: 0.4 s before). | Both | Release at `Motion.PitchRelease` (0.42). |
| **FLIGHT** | See note *FLIGHT, What may happen* below. | Both | Ball crosses the plate plane (take) or bat plane meets ball (swing). |
| **JUDGE** | One function, one frame: strike/ball, swing/miss, contact quality, HBP. Fair / foul is the live ball's call (§5.6): every batted ball goes LIVE. | Sim | Dead or live. |
| **DEAD** | Count updates. A runner who broke makes the pitch a **catcher throw play** (§11.3), the same live ball as LIVE with the ball already in the catcher's glove. A pickoff in SET is the same play with the pitcher's throw in the air (§11.4). | Sim, then catcher seat | Stamp. |
| **LIVE** | Ball in play. Fielders, runners, throws, tags, forces (§7–11). | Both | `Time` (§10.6). |
| **STAMP** | Result named on the field. Scoring, outs, bag placement already applied. Hold `afterOutSeconds` / `afterCountSeconds`. | Sim | SET, half change, or game over. |

Notes to the table above:

**FLIGHT, What may happen.** Ball travels rubber → plate in `AirSeconds` (≈0.69–1.28 shipped; D7 **wait** — called in the #346 sitting before retuning). The floor `pitching.flight.airMinSec` scales with the mound: 0.78 s × 53.78 / 60.5 ≈ 0.69 s, so on the 53.78-ft mound a pitch reaches the floor only above ~109 mph (it was ~96 mph at 0.78 s) and every real pitch keeps its speed difference. Pitcher steers break (stick L/R).
Batter may swing at any moment; the press is judged against the ball's plate time less `batting.window.leadSec` (D13), and inside the window the take is warped so its Contact mark lands on the ball. Stealing runners run at ⅔ speed until the ball reaches the plate.

Rules:

- **The judged pitch is the shown pitch.** The strike/ball/contact verdict is computed from the same trajectory the batter sees, including in-flight break. The CPU batter commits at the decision instant (plate − `batting.window.leadSec` − `batting.cpu.decideLeadSec`), exactly like a human who has pressed; the umpire judges the final crossing (`AtBatMotion.CpuDecisionTime`, `CommitCpuSwing`). ✅ P1 (S-04) **What it reads at that instant** (PH-18) is what it can see:
  `CpuBatter.Swing` reads `CpuBatter.ReadPitch`, the same delivery with the stick's break frozen where it stood at the commit (the break the client has drawn, which `AtBatDirector` passes at the commit, otherwise the stick held one way from release, `BreakReach` over the time to the commit), its crossing projected with the family's own movement and no future steering.
  Zone, swing / take and the box follow that read, so **a steer after the commit beats it**; the umpire and the bat still meet the ball that is thrown. The read adds no draw, and the caller hands in no zone of its own. The batting table has no switch for it; a table that authors `cpu.commitRead` is refused by name. ✅ (S-141 … S-143)
- **A swing before release is a swing.** It resolves as an early miss (strike); the take plays at its natural length (Contact 0.30 s after the press, D13). A press during SET is ignored (it is not a swing yet): the hold still builds a charge, the release does not commit (`ChargeButton.Advance(commits: false)`). ✅ P1 (S-14, S-15)
- **The ordinary pitch is selected in SET, before the charge, and the charge locks it** (PH-02-R3/R4/R5). One button cycles the pitcher's three ordinary families in repertoire order — fastball → second → third → fastball (§2, §4.3) — and a family with no authored row in the active `pitching.families` table is **skipped**, so zero, one or two presses always land on a pitch that can fly (since #860 the shipped table authors all five, so every pitcher cycles three:
  Rio fastball → changeup → curveball; Vale fastball → curveball → slider). The family **locks on the tick the charge button arms**, including the same-tick press-and-release that commits at once; from then until the pitch commits, cycle presses are ignored, and the committed family is the locked one — nothing is re-polled at release. **Same tick:
  cycle, then lock** — a cycle press on the arming tick is applied first and is never dropped. **Reset to fastball, unlocked, at every SET entry** — the first pitch of an at-bat, the SET after a pitch, after a dead pickoff, after a foul — **and after a pitcher swap**: the pitcher card names the selected family and the charge locks it.
  A charge that **disarms without a delivery** (the swap pick opening mid-hold makes the button non-accepting and discards the charge, §4.7) releases the lock and keeps the slot; there is **no pitcher cancel** (PH-02-R3 leaves it unselected). **The lock lives only while the charge is armed** — a selection still marked locked with a disarmed button is stale (a SET frame that returned early and never saw the release) and is read as unlocked, so a skipped tick can never strand the cycle.
  Cycling before the pitcher-ready beat is allowed — a selection is not a delivery — but not while the swap pick is open. ⚠️ **sitting 1**: `PitchSelectionState` / `PitchSelection.Advance` beside `ChargeButton`, read through `Match.SelectPitch` / `Match.FamilyAt` against the current pitcher and the match's own table (S-101 … S-106, #812). The mound reads it every SET frame since #825:
  `AtBatDirector.TickSet` captures the charge button *before* the step, passes `Controls.CyclePitch` and `HumanPitches && _swapPick == null`, carries the committed step's family into `PlayerPitch`, and resets the state in `BeginSet` and again when the swap pick confirms. `Controls.Pad.Changeup` is gone; West cycles before the charge.
- **A CPU pitcher selects by presses too** (§4.8, PH-18-R1). It does not name a family: it rolls its count row's weights over the slots its own repertoire and the active table make *selectable*, and then walks that choice through the same cycle from the same SET reset, so what it throws is always 0, 1 or 2 presses from the fastball. It has no vertical aim either — its location is the rubber it walks to during this SET, and it has all of SET to walk there.
  ✅ shipped since #860 (Jack accepted the `trials/pitch5` window on September 22, 2026 ("trial was good."); `CpuPitcher.PitchByInputs`, S-115 … S-120). #887 removed the `pitching.cpu.humanInputs` switch and the endpoint model behind it.
- **The box recenters after every pitch** (D12, #607); Down recenters early in SET; the pitcher's rubber persists. ✅ #607: every pitch's finish returns through `Match.AfterPitch`, which calls `ResetBatter` (take, swing and miss, foul, strikeout, walk, HBP, ball in play); `BatterContactOffsetX` still latches the walk the swing used (S-16 under D12).
- **Nothing advances baseball while a seat is disconnected** (how-to-play.md, two controllers). ✅

---

## 3.1 Controller input contract

Controls are gamepad only: pad 1 is player 1, pad 2 is player 2. There is no keyboard or mouse scheme. RT is the pitch/swing hold-release and the fielding throw press; LT is the Star modifier read at accepted release. Trigger hysteresis prevents travel noise from creating extra edges; charge remains time held. South confirms menus, dashes runners and answers close-play prompts. Ordinary catches have no button.
North is jump / eligible Buddy Jump; East dives or cancels a queued throw, with cancel consuming the press. West attacks eligible defensive targets. LB switches glove; RB feeds the cutoff / relay.

Right stick flick selects runner identity on offense or a latched throw base on defense. Selection does not move a body or throw a ball. Left stick moves bodies and controls existing pitch break / Star aim. Before a pitch charge, a held right-stick direction plus RT attempts the pickoff; introducing a base while charged keeps the existing balk. Targets clear at new SET and after a completed/cancelled throw; a cutoff feed retains its onward destination. No target means no direct throw.

West cycles the ordinary pitch before charge, and its family is visible. Hold West/North to bunt third/first. Latest fresh press wins; releasing both takes. East cancels only an uncommitted swing. Start → Arrange defense is available only before pitcher commitment and uses the existing roster/allowance rules.

Menus use South confirm, East cancel/back, labeled West secondary, North Ready, LB/RB panels/pages. View/Select always opens How to play; Start opens Call time/options, including at title. The title's controls line (`CarnivalFront.TitleFooter`) names those verbs every time the title shows: fresh, after a match, and after a finished or abandoned lesson. It is navigation, not onboarding, and no play state hides it. Stadium time/hazards, player count and sides are visible focusable settings.
No keyboard, mouse or pointer input reaches a player path. There is no scheme switch and no keyboard recovery. F1/F2/F3 are editor-only developer keys, not player controls. Old literal-input replay fields stay separate.

Controller device identity is fixed through setup and play. A missing active seat pauses before any game clock; South on an unseated pad deliberately recovers it. Menu close, pause, retry, half change and recovery require release/neutral before gameplay rearms. Bunt holds cannot become jump/attack/item actions at contact. Runner orders already issued survive contact. No array reorder or surviving pad takes over the missing team.

Item buttons are contextual to an actual offer: D-pad left/right chooses, left stick aims, North throws. They never replace Star or runner buttons and have no permanent Exhibition prompts.
