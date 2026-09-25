# Controller-only control review

> **Historical.** A finished report, kept for its evidence and reasoning; the contract is [how-to-play.md](../../how-to-play.md). Where they disagree, the contract is right.

Presentation design review, September 23, 2026. Audited against `355f3bf2`.
Jack accepted the overall layout with corrections that catching needs no
button and North on defense activates Jump / Buddy Jump. The implementation follows this design. The physical-controller sitting remains open.
Reported control collision: [#983](https://github.com/jackguillet/grand-sluggers/issues/983), under #209.

## The recommendation

Give each hand a clear job. **Left stick moves the active body. Right stick
chooses a runner or a base. RT handles the baseball. LT requests a Star.
LB sends runners forward; RB brings them back.** Keep those offensive meanings
through SET, pitch flight, contact and a steal. A selected runner never moves
merely because the batter moved.

Retire keyboard/mouse as a product scheme, including pointer-only menus,
keyboard fallback and mixed prompts. Keep developer diagnostics separate from
player input. Every player path, recovery screen and tutorial must work with a
controller. One controller plays against CPU; two controllers own separate seats.

The largest proposed change is moving pitch/swing from South to RT. That frees
the right thumb to select a runner while charging and to choose a throw target
while the left thumb keeps moving the fielder. Trigger travel may make release
timing feel worse; this needs a physical-controller comparison before adoption.
Treat the trigger as a digital press/release, with hysteresis. Charge still comes
from time held, never trigger depth. No timing, speed, reach or Star-price tuning.

## What the audit found

1. Jack reports steal and Star hit sharing LB. On this checked-out main, LB is
   Star during SET/flight and all-advance after contact. `StarModifier` suppresses
   the held LB across the transition. The actual pre-contact steal inputs are
   left-stick direction and L3. The running build has not been identified or
   reproduced; distinguish the reported experience from this source audit.
2. The left-stick collision is direct. `ActorDirector.TickBaserunning` reads
   `RunPad.StickX/Y`, and `AtBatDirector.TickSet` reads that same offensive seat
   to move the batter. With runners aboard, one movement can issue two intents.
   Star aim also uses that stick.
3. Ordinary human-controlled aerial catches currently require South, while
   ground pickups and assisted catches can happen without it. Remove the extra
   catch press: positioning is the player's ordinary catching action. RT must
   never make the glove take a ball it would otherwise miss.
4. Bunt occupies both triggers. LT later becomes an item modifier, requiring
   another held-input suppression rule. Items currently have no source in
   Exhibition, yet appear in the control reference.
5. D-pad base selection competes with left-stick movement for the left thumb.
   Fielding can also interpret the movement stick as a throw destination.
6. All-return mixes reversal and tap-to-halt; halt uses LB+RB, while LB may be
   consumed by Star. Selection, direction and stopping need explicit semantics.
7. Menu Back alternates between West and East. East also changes lineup panels.
   The book advertises Esc, input-scheme switching and a keyboard fallback.
8. Select serves live glove switching and the between-pitch defense editor.
   Give frequent fielding actions a bumper; put management in Call time.

Evidence: `unity/Assets/Scripts/Runtime/{Controls,ActorDirector,AtBatDirector,
InPlayDirector,FlowDirector}.cs`, `src/GrandSluggers.Sim/{Scheme,RoleTables,
ControlDiagram,Seats,StarModifier}.cs`, `docs/gameplay-spec.md` sections 3–5,
8–9, 11–12, and `docs/how-to-play.md`. Source inspection is not a pad sitting.

## Full proposed layout

Button names are positions. South/East/West/North are Xbox A/B/X/Y;
PlayStation cross/circle/square/triangle. Display the connected device's glyphs
where its family is known, with a position diagram as fallback. Never infer a
Nintendo layout from Xbox letters alone.

### Menus, book, lineup and settings

- Left stick or D-pad: navigate. Navigation never changes a roster assignment.
- South: confirm / pick a source / confirm its destination.
- East: cancel the current pick, withdraw Ready, or Back, in that order. Never
  remove a roster member or change panels as a side effect of Back.
- West: the screen's labeled secondary action (remove a selected roster member,
  or retry a finished lesson). Only one action at the current focus.
- RB on Team Setup: fill your team from any focus. West only removes a focused roster player.
- Stadium setup owns player count and P1 home/away. Captain selection has two team cards and a portrait row: left/right browses, South confirms, East undoes or returns. One controller chooses its captain then the CPU captain; two controllers confirm independently.
- North: Ready/unready where that action is present.
- LB/RB: previous/next page or panel. Order/field panel switching uses these,
  freeing East to consistently cancel. Visible tabs and focus show the context.
- View/Select: How to play. East returns to the exact screen and focus.
- Start/Menu: Call time/options; from the open pause menu, resume.
- Player count, home/away, park, night, hazards, innings, Stars and CPU skill:
  visible focusable choices, not undocumented L3/R3 shortcuts. Title has
  Exhibition, Tutorials, Controls and Quit entries. No mode-cycling Start.
- Each seat edits and readies its own team. P1 owns shared match settings. P2
  can join through the visible two-player flow; plugging in alone changes no team.

### Batting, including a live steal

- Left stick: batter positioning; existing Star swing aim when applicable.
  It never selects, sends, returns or halts a runner.
- RT: tap/release for ordinary swing; hold/release for charge swing.
- LT held at the accepted RT release: Star swing. Let go to remove the request.
  Existing costs, insufficient-Star fallback and release timing stay intact.
- East: cancel an uncommitted swing. Release RT before starting another.
- Hold West: bunt toward third. Hold North: bunt toward first. The latest fresh
  press wins; releasing it reveals the other still-held side. Release both to
  take. A bunt press discards an uncommitted swing as today. Neither alters a
  committed swing. These are alternative swings, not additional modifiers.
- Right-stick flick: select a runner. LB/RB issue their orders independently of
  RT/LT. Select before holding a bunt button; the same right thumb cannot hold
  the bunt and flick the stick comfortably. This is a specific squeeze-play gate.

### Pitching

- Left stick: position on the rubber; after release, existing break control.
- RT: tap/release for ordinary pitch; hold/release for charge pitch.
- LT held at the accepted RT release: Star pitch.
- West: cycle the three ordinary pitches before charging; show the family and
  lock it when charge begins. Each pitch still starts on Fastball.
- Hold right stick toward a base, then press RT: pickoff before charge. A target
  first introduced after charge is a pickoff attempt and uses the existing balk
  rule. A stale target from a previous play cannot turn a pitch into a pickoff.
- No pitch cancel. East must not erase pitcher commitment.
- Call time → Arrange defense: inspect and swap two positions, including the
  pitcher, only when the ordinary rules allow it. No new substitutions or stamina.
- After the ball leaves the pitcher, the defensive seat can choose and buffer
  the catcher's throw using the ordinary fielding controls and existing window.

### Fielding and throwing

- Left stick: move/take over the glove, including while selecting a throw target.
- Right-stick flick: select throw target — right 1B, up 2B, left 3B, down home.
  The selected bag remains visible after the stick recenters. Selection alone
  never throws. Retarget an uncommitted/buffered throw with another flick.
- Catch automatically when the ball meets the glove's eligible catch volume.
  Ordinary flies and liners need positioning, not a button. Ground and loose-ball
  pickups remain contact-driven. Preserve reach, height, recovery, bobble and
  special-effect restrictions; poor positioning still misses. Removing a press
  adds no pursuit assistance, snap-to-ball, reach or guaranteed catch.
- North jump and East dive remain deliberate ways to reach a ball outside an
  ordinary catch. Their actual bodies and timing decide whether they get it;
  no additional catch press is needed. High wall balls still require the leap.
- RT press: throw to the selected bag, or deliberately queue that throw through
  the existing buffer before possession. No target means no throw, with a visible
  choose-base prompt. Pitch/swing release semantics do not turn a throw into a
  new charge mechanic. A throw can release only after legal possession and the
  existing recovery/transfer requirements.
- Catching alone never throws. A queued RT request may throw after the catch;
  RT held through a jump, catch, bounce or receiver handoff never produces an
  additional request. RT has no catch or manual scoop effect.
- RB: cutoff/relay using the selected destination. With no selected destination,
  preserve the existing cutoff-and-hold play. A relay press never becomes a
  second throw at receiver handoff.
- LB: switch to the indicated fielder, using the existing eligibility/lock rules.
- North: jump / eligible Buddy Jump or wall action, through the same jump input.
  East: dive when no throw is pending;
  otherwise cancel a queued/cancellable throw. Cancel wins and consumes the press.
  Once released, the throw cannot be recalled.
- West: attack when an actual eligible target exists. Dormant item mechanics
  do not get permanent prompts or repurpose a Star or runner button.
- South: the existing close-play response when its prompt appears; it is not
  also a catch or throw. Tagging still requires the ball and body to meet.

### Running, before and after contact

- Right-stick flick selects right=runner from 1B, up=2B, left=3B, down=batter-runner
  after contact. Selection follows that runner's identity for the play, not the
  next occupant of the bag. Show the selected body and its destination together.
- D-pad down: select **ALL**. Start each pitch/play sequence with ALL; preserve a
  deliberate individual selection through contact and catcher possession.
- LB press: send the selection toward its next legal base. Before contact this
  is the steal departure, immediately. After contact it advances/rounds under
  existing legal running rules. Holding can continue to request advancement at
  subsequent bags; it is never a Star request. On a fly, preserve tag-and-go.
- RB press: return the selection. No opposite-button tap-to-halt interpretation.
- D-pad up: halt the selection. Dedicated halt replaces LB+RB; both bumpers
  together also resolve to halt defensively, and neither resumes an order until
  released and pressed again. Selection changes under a held bumper require a
  fresh press before ordering the newly selected runner(s).
- South mash: existing live-play dash; the first valid South after a close-play
  prompt is the close-play response. West: explicit slide in the existing window.
  Automatic slide and force restrictions remain. No new pre-contact dash mechanic.
- Empty directional selection gives an unavailable-target tell and never falls
  back to ALL. A selected runner becoming out/scoring clears to **no selection**,
  not ALL; require a deliberate selection or D-pad down before another order.
- Before contact, down on the right stick cannot select a nonexistent
  batter-runner. Holding Star, swinging, bunting and moving the batter never
  changes runner selection or issues a runner order.

No required gameplay action uses L3/R3. D-pad left/right are unassigned in ordinary play; an offered item lesson uses them to choose its item. LT is unused for fielding/running after the Star release. An unused
control is preferable to making a hidden extra mode.

## Transition and device rules

One semantic input router resolves device → seat → active context → action.
Keep the existing Unity Input System; do not grow another toolkit or a switch
in MatchDirector. Menus and gameplay read different named actions, not a global
South boolean. Put supported bindings and prompt glyphs behind one catalog;
validate simultaneous contexts as well as individual role pages.

- One press has one owner. Cancel beats swing release; automatic possession
  permits only an explicitly requested throw; a close-play prompt consumes its
  response. Bunt controls held through
  contact cannot become slide/jump/attack: in particular, held North from a
  first-base bunt cannot activate Jump / Buddy Jump if the seat changes to defense.
  A tutorial retry, half change, pause,
  book close or reconnect requires release/neutral before gameplay rearms.
- Runner orders already issued persist across pitch/contact; a held button
  acquiring a new meaning does not. Target selections have explicit lifetimes:
  runner identity through the play; throw target cleared at new SET and on a
  completed/cancelled throw; no pre-pitch target inherited from fielding.
- Diagonal right-stick flicks must resolve consistently to the base diamond.
  Require neutral before the next selection; calibrate each stick separately.
- Bind devices to seats, not their current array order, including setup. A lost
  controller pauses every phase. Reconnect the same device or deliberately claim
  the missing seat with South on an unowned pad. No keyboard takeover. Release
  that confirm before gameplay resumes. Losing P2 never hands its team to P1.
- At launch without a pad, display Connect a controller. Plugging one in makes
  the full flow usable. OS window controls remain available; a connected pad
  has a visible Quit path.
- Remove runtime keyboard/mouse readers, pointer hit routes, Auto/Keyboard mode,
  F6 switching, scheme toggles, key legends and keyboard recovery. Retain only
  explicitly developer-only diagnostics and the separate debug client's needs.

## Completeness and teaching

Update the active spec, `HowToPlay.cs` and its partials, `docs/how-to-play.md`,
Scheme/RoleTables/ControlDiagram, CarnivalFront, BroadcastHud, setup/lineup,
recovery and every tutorial prompt together with the implementation. Do not
replace live copy with this proposal before the bindings actually ship.

Tutorial coverage to revise: T-P01–P09, T-B01–B11 where active, T-F01–F16,
T-R01–R08, T-D06, T-G01/G03/G03-U/G05/G06 and all T-SP/T-SS lessons. Keep ids,
real setup/CPU policy, typed evidence, three-success progression and supported
profile. Bump affected instruction/progress revisions deliberately; do not
claim old keyboard or old-binding completion proves the new scheme. Blocked
items and unavailable mechanics remain blocked.

T-F04 and other ordinary aerial-catch objectives must credit human positioning
plus a real geometric catch, not a catch-button receipt. No-input or CPU-assisted
positioning cannot earn a manual-fielding lesson. Jump/dive lessons still require
the human jump/dive action and the real catch. Update catch/throw coaching and
regressions along with the Gameplay change; a UI-only removal would leave the
current manual catch requirement hidden.

New combined input regressions must check: charge+runner selection+steal+Star
release; hold either bunt through contact; move batter with all base occupancy
patterns; running reversals/tag-up/force ignored-halt; carry ball while targeting
all four bases; automatic fly/liner catch without RT; missed/too-high ball with
RT held; jump/dive catch without a second press; catch with no requested throw;
queued throw after possession; jump-catch→throw; catch→relay; queue→retarget→cancel; pickoff before
and after commitment; tutorial retry; pause/book; P1/P2 disconnect in each phase.
Repeat with both seat assignments and different captain sizes/hands.

The physical gate is a standalone title→setup→first pitch→half inning, one pad
then two, learned entirely from the new book and tutorials. Specifically compare
RT release timing with a digital button, selection under pressure, squeeze plays,
and drift. A diagram, source test or build cannot pass this gate.

## Delivery boundaries and remaining design risks

The input layout, menu consistency and device/copy cleanup belong to
Presentation. Removing the manual catch requirement belongs to a separate
Gameplay child, including geometry, command buffering and tutorial evidence.
The proposed single/all-runner selector and explicit order
semantics may also require Gameplay command/runner support;
keep geometry, AI and outcomes in Sim. Do that work serially before wiring the
presentation. The reported collision is tracked in #983 under #209; the book
migration belongs under #342. Do not silently patch a sitting finding.

The proposal retains charge timing, mash dash and close-play rules to avoid
turning an input overhaul into a balance pass. They still deserve a usability
decision: controller remapping, Star hold/toggle and alternatives to repeated
presses should be designed against the same action contract. A held-dash option
must not silently alter race speed or auto-win a close play. Microsoft's
[input accessibility guidance](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/107)
recommends remapping and alternatives to demanding holds, repetition and chords;
that supports reviewing these options, not a claim this proposed layout is
accessible or physically validated.
