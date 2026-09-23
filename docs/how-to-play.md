# How to play

Living spec for couch play. **Controller is the couch product. Keyboard and mouse are the other scheme** — every Exhibition verb has a key and a mouse bind. Keyboard + mouse are player 1 only — that is a **scheme badge**, not a sentence on every page. F1/F2/F3 stay debug.

Contents keeps navigation in the selected scheme through the shared footer: South/East on the controller page, click/Space/Esc on the keyboard page. The F6 input-mode reminder belongs to the keyboard intro. Booklet paragraphs use their rendered font height, and the intro band allows wrapped lines without colliding with the next paragraph or footer. Dense hardware controls continue across three pages; in-game controls show one role at a time, each role over two pages of five rows (batting, pitching, fielding, running; `RoleTables`). The batting table carries the swing cancel (East / G before the release) and the two held bunt sides (LT / J toward third, RT / L toward first); the fielding table carries throw, cutoff / relay, tag, and rundown; the running table carries the per-runner select, send, halt, the immediate steal departure (stick toward the bag or L3, home included), the close play at third or home, and the rundown; the pitching table carries the pitch cycle (RB / Tab, before the charge), the break, the visible swap pick, and the pickoff with its rule (a runner on the bag is always safe). `HowToPlayTests` pins those spreads, requires the cycle row, and asserts the retired verbs (leads, the West / V changeup modifier, the West / V / Ctrl bunt and the stick-aimed bunt, the post-release trajectory cycle) stay gone. Getting started separates its five-step path from its mode table so every card keeps couch-size type.

**In the game:** Start / H during SET or a play opens **Call time** — Resume, Restart, **How to play**, Title. Esc opens **How to play** directly on title, captains, field, lineup, and a pitch. From Call time, Esc / East / right click resume. How to play is an **opaque full-screen book** — HUD and field do not show through. Big white type on dark cards. Pages, captains, and the field step once per flick; hold the stick or d-pad to repeat after a beat. A sitting stick is recentered so drift does not walk the pitcher or run the roster. Right-drag aim is not a menu stick. It opens on the last input you used. A **Controller | Keyboard + mouse** toggle at the top of the book locks the scheme until you close it. Scheme- or seat-specific spreads get a pill (`BookScheme.SeatBadge` / `PageBadge`) — Two controllers, Player 1 only. Each chapter has one roster captain in a circle (`BookChapter`). Every page that names a verb has controller copy and key copy (`Page.Shown`). **Contents** is a table of contents (`ContentsToc`): chapter titles and page numbers. Numbers match the book header. **Getting started** is a numbered first-time path (`GettingStarted`): title → stadium → captains → lineup → positions/order → settings → first pitch, followed by a mode spread for Exhibition, Training, and two-controller seating. Not Challenge, Toy Field, minigames, or records. **Controls** is three measured hardware spreads (`ControlDiagram.CalloutCell`): hardware, always, **green offense / red defense**. No tiny schematic. **In-game controls** gives batting, pitching, fielding, and running their own measured role spreads (`RoleTables`): verb | what you press, one scheme at a time. **Pitch and swing** is two cards (`HowToComic`): charge chip → commit chip, one caption. **Running** is three `BagDiagrams` (bag map, all-advance, all-return) plus **Close play** and **Tag** callouts. Right is 1B, up 2B, left 3B, down home — the same map as runner selection and throw tells. **The game screen** is two labeled HUD maps (`HudCallouts`): SET (scorebug, B/S/O, on-base, cards, TIRED) and in-play (YOU, landing ring, ITEM). **Chemistry** is two cards (`ChemBook`): hearts vs scribbles. **Who you are** uses one spread for the select card (PIT / BAT / FLD / RUN) and a continuation with four full-width ability rows (pitches / swings / running / fielding). Charge at MAX is the picture. Never print South / Space / left click on one line. If you change `Controls.cs`, Exhibition flow, SET cameras, or what a verb does on the body, update this file **and** `HowToPlay.cs` in the same PR.

Open `unity/` in Unity **6000.5.9f1**, Play `Assets/Scenes/HarborDiamond.unity`. That is the game. Trailer stills (plate / scoop / star, HUD off): **[docs/screenshot-gate.md](screenshot-gate.md)**. Agents capture those from Play without grinding the top of the first (`Grand Sluggers → Capture Still Gate`).

---

## Face buttons

South / East / West / North are **positions**, not Nintendo vs Xbox labels. The bottom face button is South on Xbox and Nintendo controllers.

| Verb | Controller | Keyboard | Mouse |
| --- | --- | --- | --- |
| Confirm / pitch / swing / catch / throw | South | Space / Enter | Left click |
| Charge | Hold/release South | Hold/release Space / Enter | Hold/release left click |
| Star (hold as you let go of the pitch or swing) | LB hold | Q hold | Q hold |
| Aim / run | Left stick | WASD | Mouse move |
| Bags | D-pad diamond | 1 2 3 4 (arrows when not running) | Mouse quadrant / click bag |
| All advance / all return (after contact) | LB / RB | `,` / `.` | — |
| Freeze | LB+RB | `/` | — |
| Steal | Stick toward the next bag, or L3 | Z | — |
| Cycle pitch (mound, in SET) | RB | Tab | Tab |
| Swap pitcher / glove | Select | R | — |
| Bunt toward third / toward first | LT hold / RT hold | J hold / L hold | J / L hold in the box |
| Cancel a loaded swing | East, before you release South | G, before you release Space | G, before you release left click |
| Cutoff / relay | LB after catch | X | — |
| Dive / jump | East / West | G / F | — |
| Attack (kick / smash item) | North in-play | B | Middle click in-play |
| Start / call time | Menu / Start | H | H |
| How to play | Esc | Esc | Esc |
| Night | R3 | N | — |
| Hazards on / off (stadium) | Select | R | — |

South / East / West / North: Xbox A/B/X/Y, Nintendo B/A/Y/X. Keyboard: Space / G / F / Q. The bunt is on the triggers (LT / RT) and on J / L. The star is a finger button held with the pitch or swing: LB on either controller, Q on the keyboard.

One player with controller 1, or two controllers. Hold **South** to charge and release it to pitch or swing. Space / Enter / left click is the same one-button load for keyboard and mouse. Rumble on bat contact and on a star. **Keyboard and mouse are player 1 only.** Controller 2 is a second gamepad. Mouse analog is **right-click hold and drag this frame** — a parked cursor is dead, including while you charge, so the pitcher stays on the rubber. WASD still walks; a key already down when SET starts does not. A sitting stick is dead until it passes through rest. Left click is confirm.

The in-game booklet **Controls** page is a hardware diagram (`ControlDiagram`), not this table. **Controller | Keyboard + mouse** (`BookScheme`) follows the active Player 1 input; the toggle locks it. On launch, **Auto** chooses a connected controller before keyboard/mouse. Press **F6** on the title screen to cycle and persist **Auto → Controller → Keyboard + mouse** for controller testing. If Controller is selected with no connected controller, keyboard and mouse still navigate and become Player 1 at first pitch. Green is offense. Red is defense.

---

## Exhibition (the product)

Three innings at Harbor. Home bats in the bottom. **1 PLAYER** (the default): controller 1 pitches the top and hits the bottom against CPU. **2 PLAYERS** on captains: controller 1 is home, controller 2 is away — they pitch and hit at the same time. Plugging in controller 2 does not start 1v1 until you pick 2 PLAYERS.

### Title

The park is the poster: dirt and the diamond, from in front of the backstop. **GRAND SLUGGERS** is a sticker over the infield — reads left to right, readable without F2. No captain on the title. Captains wait for select.

- **South / Space** — play ball (pick stadium)
- **Start / H** — cycle Exhibition / Challenge / Training (Challenge stays later)
- **Esc** — How to play (the book). Works on title, captains, field, lineup, and during a pitch.
- **West / F** — Training drills on Harbor

Setup order: **stadium → captain → lineup → positions/order → settings**. The park does not follow the captain.

### Pick stadium

A **postcard**: park name, DAY / NIGHT, HAZARDS ON / OFF, and the **field card**: one line for each thing the park changes (its ground, its wall, its air, each hazard, with its own numbers). Harbor is the slice — crowd of people, padded wall with ads, a scoreboard that keeps the score, brick town. Not an empty diamond.

- **Stick / WASD L/R** — cycle the park. Captains stay put.
- **South / Space** — pick captains
- **West / F** — back to title
- **R3 / N** — night
- **Select / R** — hazards on / off. The postcard reads HAZARDS ON or HAZARDS OFF, and the park redraws without them.

### Pick captain

The toys are the UI. Highlighted captain **steps forward**. They stand on the dirt. Camera looks at the **toy** (face and body), not the brim and not the plate dirt. The **HUD card** (P / B / F / R, star pitch, star swing, field verb) is the only panel.

- **1 PLAYER / 2 PLAYERS** at the top — one controller vs CPU, or two controllers. Default is one player even if controller 2 is plugged in.
- **LB / `,`** — 1 player · **RB / Tab** — 2 players. Click the tabs. Two players needs controller 2.
- **Stick / WASD L/R** — your team · **U/D** — the other (controller 2 L/R their team when 2 PLAYERS)
- **North / Q** — you are **HOME** or **AWAY**. HOME bats the bottom. AWAY bats the top.
- **South / Space** — pick the lineup
- **West / F** — back to stadium
- Camera looks at the home captain. The title shot (West) sits **in front of the backstop** and looks into the diamond — the cage grid is not the picture. No body on that shot.

### Lineup (Team Setup, then Offense / Defense Setup)

Two screens. Not a 3D huddle with a name list.

**Arrange defense** has its own book page explaining the in-game two-position swap and the quick pitcher shortcut.

**Team Setup.** Home nine along the **top** (captain filled, eight empty). Away nine along the **bottom** (CPU-filled until a second controller sits). Center is a grid of heads (`Look.Portrait`). Portraits have no faction or chemistry frames. Hover over a player, or move the controller focus to them, to see their card; their chemistry partners get a soft highlight. Stars are chosen later on Match settings.

- **Stick / WASD** — pick a head (center) or a slot (home row)
- **South / Space** — drop the head into the highlighted empty slot. When the nine are full, South goes to defense
- **West / F** — remove (captain stays)
- **RB / Tab** — random fill
- **East / G / Back to captains** — return to captain selection

**Offense / Defense Setup.** Batting bars numbered **1–9** run across the top (home) and bottom (away), with two baseball diamonds between them. Home field is left and away field is right. Heads sit on P / C / 1B / 2B / 3B / SS / LF / CF / RF, on grass with dirt, bags, foul lines and a curved outfield. Each team has its own player card on the right, aligned with its batting bar. P1 and P2 keep independent cursors, picks and cards visible at the same time. Hover or controller focus shows that player's card and softly highlights their chemistry partners; there are no permanent character-color frames or chemistry badges.

- **Stick / WASD** — move through the bar or diamond without changing anything
- **East / G** — switch between batting bar and field
- **South / Space / click a player** — pick them; move to another slot in the same bar or diamond and confirm again to swap. Confirm the same player to cancel
- **West / F** — withdraw ready, or cancel a pick; with neither, Player 1 goes back to Team Setup
- **North / Q / Ready button** — ready your team when no player is picked. Every human player must be ready to continue to settings; the CPU is ready automatically. Ready again or West/F withdraws your ready state; picking a player to edit clears your ready state

Both controllers edit only their own team. Mouse is Player 1; the other team can be inspected but cannot be edited. There is no automatic start while you inspect the lineup. On Team Setup, click a pool player to add them, click a team slot to focus it, and use **Fill team** or **Continue** as labeled. Continue needs both complete nines.

### Match settings

Player 1 selects a row with **stick up/down / W/S** and changes it with **left/right / A/D / South / Space**, or clicks the setting. **Stars** defaults ON; OFF disables Star pitches, swings and gains for both teams. **Items** is labeled **UNAVAILABLE** because no item source is active. **Innings** cycles 3 / 6 / 9. **Mercy rule** defaults ON; it applies only to games scheduled for at least 6 innings, with a 10-run lead from the third after the trailing team bats. **CPU skill** cycles EASY / NORMAL / HARD without changing human timing windows.

**North / Q / Ready** readies your seat. Both human players must ready on this page before the game starts; CPU is always ready. A settings change clears both ready states. **West / F** withdraws readiness; when Player 1 is not ready it returns to positions/order. Going back through setup preserves existing order and positions for an unchanged roster.

### Pitching and hitting (same four verbs)

| Verb | Pitch | Swing |
| --- | --- | --- |
| Tap and release South / Space / left click | Normal — easier control | Slap — better contact |
| Hold the same button, release at MAX | Charge pitch — fast; rings line up then **decay** | Charge swing — extra-base; same rings |
| Modifier | **RB / Tab in SET, before the charge** = cycle the family — Fastball → your second pitch → your third → Fastball. The charge locks what is showing; nothing is re-read at release. Every pitch starts on Fastball, and the card lists your three pitches in cycle order (FB · CH · CU for Rio) with **no mark**. Every pitcher owns Fastball plus two of changeup, curveball, slider and sinker | **LT / J hold** = bunt toward third, **RT / L hold** = bunt toward first; **East / G** before the release = cancel the load |
| Hold **LB / Q** as you let go of South / Space / left click | Star pitch (costs its stars even if hit) | Star swing (costs its stars even on a miss) |

Press South / Space / left click to start the load; release the same button to throw or swing. A quick tap is the normal pitch or full slap swing. Hold until the rings line up, then release → **Nice!** on the mound, **MAX** at the plate; that is the charge tell, not a verdict. The word for the contact — **PERFECT**, **NICE**, **SOUR** — comes only when the bat meets the ball. Holding beyond the MAX band loses power. The pitch has a **gold streak** so you can see it come in. **Swing when the ball is on the plate.** The press is judged against the ball reaching home (a tenth of a second early is square), and anywhere inside the window the bat speeds its swing up to meet the ball. Outside the window it swings at its own pace and misses. The window is one width for every hitter, both swings and every difficulty: the title's difficulty line changes the CPU's skill, never your window.

**Cancel a swing.** While South / Space / left click is still down, **East / G** throws the load away: the charge is gone, the bat comes back to the stance, and the pitch goes by as a take. Once you let go the swing is committed and nothing takes it back. The button you were holding cannot swing any more: let it up and press it again for a new swing, which starts from zero. The East / G press that cancels is not also a dive, a dash or a Training skip; it counts again once you let it up.

**Bunt.** Hold **LT / J** to square toward **third base** or **RT / L** toward **first base**, and keep it held through the pitch. There is no press to time: the squared bat meets the ball if the oval is on it, and the oval decides how good the bunt is. Walk the box with the stick / A-D as usual. Change sides any time before contact by pressing the other trigger (the latest press wins; let go of it and the bat goes back to the one still held). Let go of both to pull the bat back and take the pitch. The side leans the ball toward that base; it does not place it, and a bad bunt still goes foul or pops up. Pressing a trigger while a swing is loading throws the load away and squares at once. With a swing already let go, the triggers do nothing until the next pitch. The batter card reads **BUNT 3B** or **BUNT 1B** for the held side, yours or the CPU's (the bat itself does not yet angle toward the side: #943). A trigger you held through contact means nothing else — not the item modifier, not the next pitch's bunt — until you let it up and press it again. The pitcher releases the ball at the 0.42-second mark of the delivery after button release. Pitcher, batter, and ball share contact slow-down and pause.

This input shape is confirmed by the original *Mario Super Sluggers* booklet: the sideways Wii Remote uses one button for a normal action and hold-then-release for a charge; its batting cursor moves with the batter; and pitch curve is applied left/right after release. Harbor adapts that contract to South / Space / left click, keeps the star as a held modifier (LB / Q), and makes the changeup a family the pre-charge cycle selects instead of a held button (PH-02-R5). The current feel values — 0.55 seconds to fill a pitch, 0.45 seconds to fill a swing, a 0.50-second MAX band, and 0.80 charge/second overcharge decay — are Grand Sluggers tuning pending the human parity gate, not measured Nintendo values. Reference: [Nintendo instruction booklet, printed pp. 6–9](https://www.mariomayhem.com/downloads/mario_instruction_booklets/Mario_Super_Sluggers_-_ML1_Manual_-_WII.pdf).

SET forks **by role in 1P**, and **stays behind home in 1v1**. **One controller, pitching:** camera stays on the **mound 3/4** (`mound`) — first-base over-the-shoulder behind the rubber, pitcher large on the right, rubber in the bottom, looking at the box — through SET and the throw. **One controller, batting:** camera stays on the **plate 3/4** (`plate`) — behind home looking at the mound, batter left of the look, pitcher in the diamond — through SET and the throw. Catcher crouches behind the camera. Pentagon and two boxes have dirt between them. It does not cut to `pitch`. **Two controllers:** camera stays on the **plate 3/4** (`plate`) — behind home — through SET and the throw, whether you pitch or hit. Controller 2 does not fork the HUD. Pink/gold charge ring **around the box** on the packed dirt (not a pancake under the feet). ~1s to the plate (Sluggers pace, not MLB 90). Home bats the bottom. Scorebug sits top-right; batter card bottom-left; pitcher card bottom-right. Highlight “your” card. Those anchors do not move.

- **Stick L/R / WASD** — walk the rubber before the throw, or walk the box until the swing commits. The box **recenters after every pitch** (ball, strike, foul, walk, ball in play); **Down** recenters it early (in SET only). The rubber stays where you walked it. Pitching L/R is screen-relative from both the mound and plate cameras. Walking the rubber moves where the pitch crosses by the same distance you moved; a fastball crosses mid-frame; every other pitch crosses lower by its own drop, the changeup and the curveball most.
- **Past the foul line** — a **foul** (strike unless you already have two). The ball flies there. Not a K at two strikes. Timing sends it: a swing well early or late can pull or push it foul.
- **Stick L/R after release** — break toward that side of the screen from either pitching camera; the bend grows late and moves the crossing at most half a frame-width, and a charged pitch or a changeup barely bends. This is not the pitch cycle: the family is chosen in SET, before the charge. A pale ring in SET is your **rubber tell**: it sits where *you* stand, at mid-frame height, and says nothing about the pitch. It hides when the ball leaves your hand — after that the ball is the cue. Practice and the Tutorials keep the older ring, on the crossing, because there the shape is the lesson.
- **Sweet-spot oval** follows the batter, never the pitch. Walk so its center eats the ball. The oval is the bat: its heart is PERFECT, the oval NICE, the rim SOUR; it is as tall as the strike frame, so any strike is on the bat. Where the ball meets the oval decides how hard you hit it; **when** you swing decides where it goes — early pulls, late pushes, outside the window is a whiff (9 frames for every swing, centered on the ball at the plate). A charge narrows the oval, never the timing, and pays ×1.25 on a perfect. Contact grows the oval, never the timing. The stick at contact does not steer an ordinary swing or a bunt (the bunt's side is its trigger); only a Star Swing still reads it.
- **Take outside the white frame** — ball. Swing and miss outside it — strike.
- **Hold D-pad / 1–4 + South / Space before charging** — throw from the mound to any base, occupied or empty. A runner on the bag is safe; an off-bag runner can be tagged or run down. Starting the pitch hold commits you. Selecting a base while holding attempts to step off and causes a **BALK**: every runner one base, count unchanged.
- **Select / R before charging** — opens **Arrange defense**, like position setup. Stick / d-pad / WASD moves through all nine players; player 1 can hover to inspect. See stats, ARM, pitches, throwing hand and Good / Poor / Neutral chemistry with teammates. **South / Space / click** picks a player, then a second position to exchange the two. Same position or **East / G** cancels a pending pick. **Done** closes; East / G with no pick also closes. Completed swaps stay. **Select / R** is also a quick swap from the focused glove onto the mound. Pitcher changes remain once per half; you can keep rearranging the other gloves afterward. Each character keeps their own stamina and batting slot. Baseball waits while the window is open.
- **Start / H** during SET or in-play — **call time**: Resume, Restart, How to play, Title. **WASD or arrows** choose. South / Space / left click ok. Click a row. Wheel turns How to play pages. East / right click resume. From Call time, Esc also resumes. Tab on the title cycles 3 / 6 / 9 innings; X / LB cycles the CPU difficulty.
- **Esc** during SET or in-play — **How to play** (the book), same as title. H does not open the book.

**Star pitch / star swing.** Hold **LB** (controller) or **Q** (keyboard) as you let go of the pitch or the swing. Only the moment you let go counts: press or let go of LB while you charge and change your mind freely; after the release LB changes nothing. Each toy has one star pitch and one star swing, so there is nothing to pick. The card reads **STAR** while you hold LB and your team can pay. **Not enough stars:** the ordinary pitch or swing goes out at the same moment, with the pitch you already chose, and nothing is spent; your team's stars on the scorebug flash red and the line under it reads **NO STARS**. An LB you held for the star means nothing else — not all advance, not the cutoff — until you let it up.

Star pitch owns the ball ~2 seconds. The HUD stays readable throughout the effect.

### Batting (running)

- **LB / `,`** — all advance, **once the ball is in play** · **RB / `.`** — all return · **both / `/`** — halt all. A tap of the opposite shoulder halts a runner who is going; holding it turns them around. Forced runners cannot be held: they are going anyway. During the pitch (SET and the flight) LB is the star, not all advance: start steals per runner (D-pad + L3, or the stick), and send everyone after contact (on a fly, LB before the catch is still tag and go).
- **Stick toward a bag + halt (LB+RB / `/`)** — freeze that runner only, where they stand.
- **D-pad / 1 2 3** — select a runner (right 1B, up 2B, left 3B). **Down / 4** is the batter-runner once the ball is live (before the pitch it is home — not a runner). Default highlight is the lead runner.
- **Stick** toward the next bag — start the selected runner immediately in SET, windup or pitch flight. Point back to return from the current position. Point forward again to reverse. There is no lead and no snap back.
- **L3 / Z** — start the selected runner toward the next bag, **home included**. Select several runners for a double steal. Earlier departures earn only the distance actually run. The runner moves at ordinary speed before release and two-thirds speed during pitch flight; there is no perfect-steal bonus.
- After a take or swing-and-miss with a runner going, the **catcher's throw** is a normal throw from the plate: arm a bag (the runner's bag is the default) and press **South**; the cover is already on the bag; the ball must beat the body there and the tag (§ tag rules) decides. **CAUGHT STEALING** or **STOLEN BASE**. Strike three plus the runner caught is a **DOUBLE PLAY**. Dead stick: the CPU catcher still throws. Ball four entitles a forced runner to the next bag — no play on them; an unforced runner's steal is still live. First-and-third: send the runner on third with the stick as the throw passes the mound; the defense can hold **LB** for the cutoff in front of second and come home.
- **Mash South / Space** after contact — **dash**. The play stays live until every **live** runner has been **on a bag for 1 second**. An out with nobody left (throw-out at first, empty bases) ends it there. 3 outs too. Picking up the ball does not end a race. Sac fly: they leave on the catch if you sent them.
- **West / South** near the bag — slide. Runners slide on their own into a bag they are stopping at when a tag is coming; a runner rounding a bag never slides. A slide makes the tag reach shorter, not the runner faster.
- **Close play** at third or home — only when the ball is at the bag first by a quarter second or less (the booklet's Close play card: "3rd or home only, bang-bang"). Then the camera cuts to the bag and the first **South / left click** after the icon wins: runner safe if offense is first, out if defense is first. CPU mashes on a delay from Run / Field, and a seat that never presses loses to it. Ball there earlier than that: the tag, no icon. Runner there first: safe, and a bang-bang beat (by a quarter second or less) pops a small **SAFE**.
- **Tag.** Have the ball and touch a runner off a bag — about four feet (Lick Catch and Grow reach six). That's a tag. On a bag they are safe. Force still needs a throw or a foot on the bag.
- **Run through first.** The batter runs through the bag and comes straight back, safe the whole way; turn toward second and you are live. Everyone else stops on the bag.
- **Rundown.** Caught between bags with the ball in a glove nearby? Stick back or forward turns you; the fielder chases and throws to the covered bag. A glove waiting on the bag you are running to just tags you. It ends with a tag, a bag, or a throw that gets away.

Fair contact always sends the batter to first. On a fly, every runner goes back to the bag and waits for the catch or the drop; all-advance before the catch is tag-and-go (they leave at the catch); a stick send before the catch is your call, at the risk of being doubled off. A lone runner cannot cross home on a fly with fewer than two outs until it lands or is caught. Runners never pass each other. The mini diamond shows **every runner where they are**: on a live ball each pip slides between the bags at the runner's true place, the batter-runner included (and past first on a run-through, back toward the bag on a return); when the ball is dead the pips sit on the bags. The runner you have selected is the bigger dark pip. Mini diamond + banner match the out/safe.

In-play HUD (the booklet screen): At contact, the cards and score/count panel give way to a compact **runner diamond and outs** at top-right. They stay visible through hit-freeze, home-run smash and special effects. The full plate HUD returns immediately when play ends. **YOU** names the glove (bag + name) — a gold ring sits at their feet — and **hands to the receiver** when the ball leaves the throwing hand; a small **ERROR** pops when a throw skips past its cover. When the stick is dead, they still run to the ball like CPU. Stick steers; it does **not** throw for you. A **dark shadow on the ground** follows the ball directly overhead, shrinking as it rises and growing as it falls. It disappears when the ball is caught. A **yellow circle on the grass** is the stand-up catch (the landing ring). At the rim the body **dives** (East). It turns **red** in the jump window. **ITEM → name** plus a gold ring on that body when a chemistry item is armed. Pitcher card is **ARM**; below 25 it reads **TIRED** and sweats. Live events stamp **when they happen**, at the glove or the bag, never a center card: a catch is **OUT** (or **DIVE** / **JUMP** / **BUDDY JUMP**) at the glove; a force or tag is **OUT** at that bag; a runner who crosses is **SCORE** at the plate; a bang-bang body in ahead of the ball pops **SAFE**; a throw that sails pops **ERROR**. A double play names both outs as they happen. When the play is **dead** (nobody still running), counts and hits still stamp **on the field** — **BALL**, **STRIKE**, **FOUL**, **WALK** (smaller and quicker), **HIT BY PITCH**, **STRIKE OUT**, **BUNT**, **STOLEN BASE**, **SINGLE**, **DOUBLE**, **TRIPLE**, **HOME RUN**, **GRAND SLAM** — then SET for the next pitch. Every stamp is read from the play's typed result (the outs, the catch, the throw that got away), never from the caption. An inside take that hits the batter is **HIT BY PITCH** and awards first, like a walk. Hold **LT / J** (toward third) or **RT / L** (toward first) through the pitch to **bunt**; the card reads **BUNT 3B** / **BUNT 1B**. **Stick toward the next bag / L3 / Z** with a runner on arms a **steal** (home included; early in the windup is a perfect steal). A runner still going with fewer than three outs keeps play alive.

### Fielding (the ball is in play)

Nearest glove **lights**. The ball **hangs** so you can get there. Leave the stick still: CPU runs to the **landing** on a fly (not the live ball — that would send them home first) and **still can catch** standing under the ring; at the rim they **dive**. Then they chase the hop once it is down. They do **not** throw for you. Gloves stay **inside the wall** — each park's fence is the boundary. Push the stick to take that glove. **Standing on the ball scoops it** — no South, no stick. South still catches a fly in the window. Select / **R** cycles who you are. CPU covers the bags. A ball over an infielder stays their hop on the dirt; once it reaches the outfield grass the outfielder charges and the glove hands off.

On contact the camera holds the SET shot for a beat (0.42 s, `contactCutSeconds`), then **cuts** (one cut, no swoop) to **45°** on the dirt under the ball. **CF is the top of the look** — home sits under second. A **hopper** stays on that look (`diamond`). A **liner** sits between hopper and fly (`diamond-line`, same angle, a little more grass) so the rope reads vs a bounce. A **fly or a wall ball pulls back** further (`diamond-fly`). A **home run** smashes in on the batter at the crack, then rides the ball out. The ball keeps the gold streak. It follows that spot through the hopper, the fly, and **every throw**: the camera stays over the ball, and the fielder taking the throw is in frame because the ball is going there. The **bag camera is only for a close play** (third or home, bang-bang): it cuts tight on the bag and cuts back to the ball on the verdict. A **rundown** follows the ball between the bags, and a **steal or pickoff** uses the same live camera, following the ball from the catcher or pitcher through the throw and reception. Before catcher possession, a race inset preserves the ordinary batting view. A target the camera just took is kept for a quarter second (`cameraHoldSeconds`), so a relay or a flicker does not jerk it. Steals and ordinary batted-ball throws use the same ball-follow view. Every shot is named in `data/feel/shots.json`, and each shot's `blend` says how the camera enters it (0 is a cut); the game owns no camera numbers of its own. Charge ring sits **on the dirt**. Contact is a crack, a camera punch, and a dirt puff. Fielders **run**; they do not skate. The fielder who caught the ball **stays where the catch happened** and throws from there; when the play is dead every body stands where it stood, and the next SET resets the diamond. A good throw is a purple laser; a bad throw is muddy.

- **Stick / WASD** — steer the YOU glove. Dead stick they still chase and scoop like CPU. After the catch, stick **runs with the ball**. They do not throw for you. There is no sprint button: a **Ball Dash** fielder (dart, pip, jester) carries the ball faster on its own.
- **Select / R** — swap to the pulsing glove (stick points at who you want; dead stick is next-nearest to the landing / ball). HUD **R → CF**. Not while you hold the ball.
- **South / Space** — catch (in the window) when you have the glove; after the catch, throw. On a fly, South still scoops if you are under it. On the dirt, touching the ball scoops it.
- **East tap / G** — **dive**. An East / G press that cancelled a swing is not a dive until you let it up. The body lunges toward the ball. A dive catch or a dive scoop stamps **DIVE**. Nobody dives for you: a dead stick never dives.
- **West / F** — **jump** / buddy jump. Press West as the ring turns red; the body leaves the ground. A jump catch stamps **JUMP** (two gloves: **BUDDY JUMP**). A would-be homer is a wall play: West (or two bodies, West) in the window robs. South does not. Super Jump / Grow / Clamber add window, not a skip. Miss = the ball drops (or a homer). Dead stick: CPU still can catch.
- **E** while chasing a chem partner — **buddy toss** (they take the laser)
- **North / B / middle click** — **attack**. Kick the ball to a nearby glove (chem partner lasers; anyone close takes a short toss). Smash a flying error item before it lands.
- **D-pad / 1 2 3 4** — arm a bag (right 1B, up 2B, left 3B, down home). A mini-diamond pip lights the armed bag. **South** throws. Hopper with no bag throws to **second** when first is occupied, else **first**. LB / X with no direction is a **relay**, not a random bag. You can arm before the glove; the throw waits for South. Stick after the catch **runs**, it does not throw. **Outs land on the catch, when the throw lands, when you have the ball and touch a runner off a bag, or when you step on a force bag with the ball** (1B on first retires the batter — you do not tag them). A ball to the outfield is not a force at second until someone throws there. They do not gun to first on a dead stick when you are on defense. CPU defense (you are batting) still throws — you see it.
- **Turn two.** Runner on first, hopper to an infielder: throw to second (force), you are the glove at that bag, throw to first. Beat the batter → two outs. Late → runner on first, force at second. Mini diamond updates as each out records. You throw both — one South on the hopper is not two outs. Dead stick does not turn two for you when you are on defense.
- **Force.** Batter is always forced to first. First occupied → force at second. First and second → force at third. Bases loaded → force at home. Beat the throw to the bag and they are out — no mash. Putting the batter out at first removes the other forces (then it is a tag). A throw to third or home is an out when you beat them. Mash (close play) is a tag at third or home, never a force.
- After the ball leaves your hand **you are the glove at that bag** — the YOU ring hands to the cover and rides them to the bag; the thrower's body stays where it threw from. A throw that misses the cover's reach skips past and is live (**ERROR** when it costs you); a throw to a bag nobody covers yet hangs until the cover gets there. Stick can still take a different glove. The catcher's throw on a steal is the same throw from the plate, without a hop; the pickoff is the same throw from the rubber.
- **LT+RB / South+LT / E** after contact — chemistry item (banana grass, rocket body, POW hop). An LT you held through a bunt's contact is not the modifier until you let it up and press it again.

Good throws are gold/purple and fast. Bad throws are muddy and slow; they do not slant.

A glove that **fumbles** throws its arms out (the spin take) for as long as the fumble holds — never the batter's miss, which carried the bat.

#### The stick, relays and the body

- **The stick is calibrated per controller** (#718). A seated controller starts a match with no centre: at SET or the result beat the HUD reads **LET GO OF THE STICK** (**P1** / **P2** when two play) with a bar that fills over half a second of a released stick. Moving it starts the bar over. Keyboard seats never wait. Nothing is sampled while the ball is live. **Call time → Reset stick** runs the same half second for every seated controller; East / Esc / right click backs out and the old centre stays. The analog stick reaches the game before any dead zone: past **0.20** you steer, back under **0.15** the glove runs on its own, and the speed grows smoothly from there.
- **LET GO OF THE STICK TO STEER** during a live ball: your seat has not been seen at rest since it took the field (a new half, a reconnect, a reset). The glove runs on its own until the stick is at rest once.
- **Relay inputs:** the cutoff waits for you: command the next leg with **South / Space**. A press just before the catch waits briefly for the receiver. Change the **D-pad / 1/2/3/4** target to retarget it without refreshing its lifetime; tap **RB / period** to cancel. An expired or cancelled press never throws.
- **Jump** (West) lifts the body two feet over 0.60 s, the same for everyone; the body reaches up while it is in the air, and dirt kicks at the takeoff.
- **Dive** (East) costs a recovery. The diver lies laid out, then gets up in the last fifth of a second (crouch), caught or missed, whoever holds the ring meanwhile — and still after a diving catch ends the play.
- **A hard ball** braces the glove: the body squashes and skids a little, harder for a harder ball, and eases back as the recovery runs out. A routine ball costs nothing and shows nothing.
- **A fumble** stuns the body that fumbled for 0.40 s (arms out). A ball that **gets past** kicks dirt at the fumbler's feet and keeps going as a live batted ball.

---

## Tutorials and free practice (Harbor)

From title, **West** on a pad or **F** on keyboard opens **Tutorials**. Use left/right on the stick or **A/D** for categories, and up/down or **W/S** for lessons. Click a category tab or lesson to select it. Lists show at most six lessons per page; keep moving up/down or click the page arrows to browse. Use **South / Space** (or click) to read the goal and start. Tutorials are player 1 versus a controlled CPU setup; a second pad does not take over the teaching opponent.

The lessons are grouped by skill:

- **Pitching:** strike, called ball, changeup by the cycle (one press of RB / Tab in SET), your third pitch by the cycle (two presses), MAX pitch, break, and rubber positioning.
- **Batting:** slap, sweet spot, early pull, late push, MAX swing, box positioning, two-strike bunt, taking a ball, cancelling a loaded swing (load, press East / G, take the high ball), and bunting toward first (hold RT / L with a runner on first; the bunt must go fair toward first).
- **Fielding:** ground pickup, manual takeover, throws to first/second/third/home, airborne catch, dive, and jump catch.
- **Outs:** turn a double play.
- **Running:** send, halt and return one runner; dash along the first-base path.

Running lessons give you the offense controls while the CPU fields. For Send, hold and return, select the runner on second, send toward third, halt that runner, then return to second. On a pad: D-pad Up, stick left, left + LB + RB to halt, then stick up. On keyboard: 2, A, A + slash to halt, then W. Dash toward first asks for repeated South / Space presses while the batter-runner moves.

Each lesson shows its goal, setup, and controls for the active input scheme before the attempt. During practice, the ordinary scoreboard, occupied bases and player cards stay visible: the pitcher picker names the candidate, the arm card shows stamina, and the runner card shows steal state. The coaching panel sits clear of these readouts. Pitching lessons use a batter who takes. Batting gets a repeatable middle fastball, except Take a ball and Cancel a swing get a high ball and Move in the box gets an offset strike; the two-strike bunt begins with two strikes, and Bunt toward first starts with a runner on first. Fielding starts from authored contact and base occupancy. Real baseball rules resolve every attempt.

Early and late timing are separate lessons, so each requires its own three successes. Timing mirrors the batter’s handedness. Batting spans two pages; moving past the last visible row reveals the next page.

Fielding lessons teach manual takeover, throws to each named base, ordinary airborne catches, and jump catches as separate actions. Select a bag with **D-pad Right/Up/Left/Down** or **1/2/3/4**, then throw with **South/Space**. For the ordinary airborne-catch lesson, keep steering with **stick/WASD** while pressing **South/Space**; a neutral stick hands the catch to assistance. Jump with **West/F**. A throw needs a real receiver, and an airborne catch must happen before the bounce.

Every lesson requires **three successful attempts**. The counter shows 0/3 through 3/3 in the list, brief, play HUD and feedback. Failures keep earlier successes, and partial progress is saved when you leave or close the game. Old one-success passes do not satisfy this requirement.

After a success or failure, the same lesson immediately starts a fresh attempt until you reach **3/3**. There is no Continue press or return to the briefing between attempts, including guided lessons. After a miss, the coaching panel keeps the correction alongside the controls during the next attempt. At 3/3 the completion screen offers **South / Space** to replay, **West / F** for the next lesson, and **East / G** to return to lessons. Checkmarks appear only at 3/3, separately from Exhibition. Automatic assistance and demonstrations do not earn completion. Call time / How to play still works during an attempt. Restart resets that lesson; Title leaves it.

**Free practice** lives in the **Free play** category for ungated Harbor play. New mechanics and future lessons are tracked in `data/tutorials/`; unavailable lessons are not presented as playable.

---

Lineup is Team Setup then Offense / Defense Setup. Chemistry partners softly highlight when a player is inspected. Defense is nine gloves (P / C / 1B / 2B / 3B / SS / LF / CF / RF) on two diamonds. Home bats the bottom.

### Two controllers (local 1v1)

Controller **1 is player 1**. **North** on captains picks HOME or AWAY for that controller. Controller **2 sits the other side**. Keyboard and mouse stay player 1. A second controller does not split the screen and does not go online.

- **Title / captains / Team Setup / Defense Setup.** Controller 1 edits their team. Controller 2 edits the other. Each picks their captain, roster, order, gloves.
- **First pitch.** Home pitches the top, away bats. Bottom: they swap. CPU never bats or pitches while both controllers are seated.
- **SET.** Mound when a human is on the rubber, plate when you bat vs CPU. Same role recipe as 1P. Cards stay batter bottom-left, pitcher bottom-right. Highlight yours.
- **Pitch / swing.** Controller-on-mound walks the rubber, charges, throws. Controller-in-the-box walks the box, charges, swings. Same four verbs as 1P.
- **In-play.** Fielding controller takes the glove (stick to take, dead stick = CPU cover). Batting controller sends / returns / steals. Both at once.
- **Disconnect recovery.** A seated controller is remembered by physical device identity for the whole match. If it drops during SET, pitch flight, a live ball, or a throw, play pauses before baseball advances and the other controller keeps its team. Reconnect the same controller to resume automatically, or press South on an unseated controller to deliberately take the missing seat. Player 1 can also press Space, Enter, or left click to take that seat on keyboard and mouse for the rest of the match. If the game was already at Call time, reconnecting returns to Call time rather than resuming play.

---

## Debug (not the product)

| Key | What |
| --- | --- |
| F1 | Timing bar |
| F2 | Feel overlay: shot, verb, charge, hang, rest, event |
| F3 | Mute play HUD (trailer stills without a star) |
| **[** | Slow-mo cycle (F2 must be on) |
| **]** | Freeze camera (F2 must be on) |

F2 is how you name the still (plate vs mound vs diamond-line). It does not replace the scorebug. F3 mutes the scorebug so a plate still can be HUD-off. How to capture and reject those stills: `docs/screenshot-gate.md`.

---

## What a stranger should feel

Couch, controller, three innings. You can name the captain with the HUD off. A perfect swing is illegal for two seconds and still baseball. A grounder is a scoop and a race.

**Now (Harbor Exhibition).** Title looks into the park. One controller: SET is over the pitcher’s shoulder looking at the box when you pitch (mound 3/4) and behind home when you hit (plate 3/4). Two controllers: SET stays behind home (plate 3/4). Two chalk boxes and a pentagon on packed dirt. The infield is grass with dirt paths and a mound hill. On contact the camera cuts to 45° on the dirt under the ball (CF at the top, home under second) and follows it through the throw; it cuts to the bag only on a close play. From those cameras Harbor is a place: outfield grass, a padded wall with ads, a scoreboard with numbers, a crowd of people not one card. Baseball is 0.62 ft. From the box the pitcher throws — windup, then the ball leaves that hand. Star specials own the ball or the field ~2 seconds (Heatball/heat-swing core+embers on the body; Charm hearts; Prism ghosts; Phony grin decoy; Cask barrel; Skull; Furnace lava pool), then baseball. The HUD follows the plate or live play. Shared body is one chain with six SMS-ladder cuts (kid / pageant / speed / brick / ape / slug) so a HUD-off plate still names the type. Captain extras stay data. Still primitives, not a sculpted hero. Controller input uses the Input System: hold/release South charges and commits, South is a position on Xbox and Nintendo, rumble on contact and star. Keyboard and mouse are the same verbs (Space / left click hold/release, WASD / mouse run, 1–4 bags). Bat / glove / crowd bed are original wavs, not beeps. Still not a sculpted hero.

**Not yet the reason people stay.** Scoop still, star-swing still you would show a friend, captains that read at gameplay distance. Do not start Challenge island or extra parks as products before that.
# Guided team and controller lessons

The Tutorials list includes Build your lineup, Seat two players, Call time, Recover your seat, and Reset the stick. Each opens the ordinary Exhibition screens and needs three separate successful attempts. Build your lineup asks you to drop a left-handed batter from the pool, change batting order, and change a field position. The shared character card names BATS LEFT or BATS RIGHT for the highlighted player on the lineup and captain screens. Seat two players asks you to connect two distinct gamepads, select 2 PLAYERS on captain select, and bind both seats. Keyboard and mouse can take Player 1 only. Call time begins in a prepared Harbor SET and asks you to open the menu, visit the book, and restart. Recover your seat and Reset the stick also begin at SET with a bound gamepad; choose Controller or Auto with F6 before starting. Recover your seat asks you to disconnect an active pad and recover that same player's seat. Reset the stick asks you to complete the calibration card. The first and third lessons can be practiced with keyboard and mouse or a pad; the others need the physical pads or pursuit controller their goals describe. The lesson banner names the next action still needed in the current attempt.

### More scenario lessons

The steal lessons use the normal runner controls. Time a steal: select first and start the runner before release, then beat the throw to second. Delay the break for home: send first, wait for the catcher to throw to second, then select third and send home as the throw passes the mound. Catch a stealing runner: select second and make the catcher throw yourself. A runner being tagged at home does not pass the delayed-steal lesson.

Each named star pitch and star swing has a prepared batter or pitcher with the matching skill and enough meter. Hold LB / Q as you let go of South / Space. A star swing needs fair contact. **When the stars run out** starts your pitcher with no stars: hold LB / Q as you let go anyway, and the ordinary pitch goes out, nothing is spent, and your stars flash red. Earn and spend stars begins with two strikes: first finish the strikeout with an ordinary pitch, then use a star pitch against the next batter. One strikeout followed by one star pitch is one success; complete that sequence three times.

Item lessons start with a chemistry pair that offers an item on fair contact. Hit first, cycle with RB / Tab, aim with the stick / WASD, then throw with LT + RB (or LT + South) / E. Each lesson checks its named item landed and affected the defender. Field a star grounder asks you to take over and collect the CPU's star hit yourself. Tag up from third asks you to hold the runner until the catch, then hold LB / comma to send home. Double off a runner asks you to catch the fly and return the throw to second before the early runner retouches.

The fielding scenarios include a bases-loaded force at home (collect, select home, throw), a pickoff rundown (check first, then throw ahead to second), recovering a wall carom with your own movement, and throwing to first while its covering fielder arrives. Grow, Lick Catch and Withdraw lessons place you at the right edge of the landing ring: take over and catch at a distance that needs the ability's extra reach. Burrow teaches a ground pickup at the outer edge of its range. An ordinary catch or a helper-controlled pickup does not prove ability use.

Close plays have separate runner and defender lessons at third. Offense: send from second, dash, then press South / Space when the close-play icon appears. Defense: throw to third, then make a fresh South / Space press at the icon. The actual safe arrival or tag decides the lesson; each side requires three successes.

Make the third force out starts with bases loaded and two outs. Field and throw home before the forced runner arrives: the third out ends the half and the teams change sides. This is a separate lesson from making the same force while other outs remain.

The baseball lessons practice a full sequence per success: build a 1–1 count with a ball from the rubber's edge, then a centered called strike (down/S resets the pitcher during SET); stay centered in SET, then pull a well-early swing foul, then time the next swing fair; or finish a two-out, two-strike half with a called third strike. South/Space pitches and swings. Watch the ordinary count and out readouts. Each sequence must succeed three times.

For the triple-play lesson, catch the fly and return the ball to second, then first, before the two early runners retouch. Select the bag with D-pad Up then Right (2 then 1 on keyboard), and press South/Space for each throw after possession reaches the receiver. All three actual outs and both player throws are required.

Bobble recovery starts with the helper's real ground-ball error. Wait for the bobble, then use the stick/WASD to chase and scoop the loose ball. A single steering input followed by an assisted pickup does not count, for either bobbles or wall caroms.

Dash around first teaches the actual turn: after touching first, select the runner with D-pad Right/1, send toward second with stick up/W, then tap South/Space as the runner turns. The drill checks progress beyond first and a dash during the turn; reaching second safely is a separate outcome.

Return on a fly is separate from tagging up: select third with D-pad Left/3, send home with stick down/S while the ball is airborne, then hold RB/period to return safely before the catch. The attempt requires both your early send and your return; simply staying on the bag does not pass.

The standard-rules fumble lesson adds the return throw: after the helper's fumble, steer through the manual scoop, then select first (D-pad Right/1) and throw (South/Space). The scoop alone does not pass; the throw must also be yours.

The in-game book separates recovery/reach, fielding scenarios, live-ball running, and item lessons into short pages so the instructions fit at couch text size.

Cancel a run with a force starts with two outs and runners at first and third. Collect the grounder and throw as the fast runner nears home, so the crossing precedes the force on the slower runner at second (D-pad Up/2 and South/Space). A third force out cancels that earlier crossing. This is distinct from forcing home before the run arrives.

See a run count before a tag contrasts that force: two outs, runners on second and third. Collect and throw to third as the lead runner approaches home (D-pad Left/3 and South/Space); press South/Space freshly at the close-play icon. The lead runner must cross before the actual nonforce tag. This time the earlier run counts.

### Steals, pitcher commitment and catcher readiness

Select a runner with D-pad / 1–3; L3 / Z or pointing toward the next bag starts them immediately, even before the pitch. Point back or use RB / period to return from their current position. Point forward again to reverse. There is no perfect-arm bonus. A small race view shows the departure while the batting camera stays readable; catcher possession switches to the normal live-play camera. It follows the ball through the throw exactly as on an ordinary fielding play. Pitcher throws, returns and rundowns use those same camera rules.

Before charging, hold any base direction / 1–4 and press South / Space to throw from the mound, including to home or an empty bag. Holding the pitch button starts the visible windup and commits the pitcher. Choosing a base while holding tries to step off: **BALK**, every runner one base, count unchanged. Holding indefinitely is legal; release to deliver and let the catcher challenge the runner.

During pitch flight, D-pad / 1–4 selects the catcher's target. A South / Space press within 0.25 seconds of possession is buffered; RB / period cancels it. After possession the catcher transfers and throws. The ball and YOU ring leave the thrower at actual release, after preparation. Pitch steering stays on the stick and does not change the catcher's target.
