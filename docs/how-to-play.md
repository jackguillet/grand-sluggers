# How to play

Living spec for couch play. **Controller is the couch product. Keyboard and mouse are the other scheme** — every Exhibition verb has a key and a mouse bind. Keyboard + mouse are player 1 only — that is a **scheme badge**, not a sentence on every page. F1/F2/F3 stay debug.

Contents keeps navigation in the selected scheme through the shared footer: South/East on the controller page, click/Space/Esc on the keyboard page. The F6 input-mode reminder belongs to the keyboard intro. Booklet paragraphs use their rendered font height, and the intro band allows wrapped lines without colliding with the next paragraph or footer. Dense hardware controls continue across three pages; in-game controls show one role at a time, with continuations for dense batting and pitching rows. Getting started separates its five-step path from its mode table so every card keeps couch-size type.

**In the game:** Start / H / Esc during SET or a play opens **Call time** — Resume, Restart, **How to play**, Title. How to play is an **opaque full-screen book** — HUD and field do not show through. Big white type on dark cards. Pages, captains, and the field step once per flick; hold the stick or d-pad to repeat after a beat. A sitting stick is recentered so drift does not walk the pitcher or run the roster. Right-drag aim is not a menu stick. It opens on the last input you used. A **Controller | Keyboard + mouse** toggle at the top of the book locks the scheme until you close it. Scheme- or seat-specific spreads get a pill (`BookScheme.SeatBadge` / `PageBadge`) — Two controllers, Player 1 only. Each chapter has one roster captain in a circle (`BookChapter`). Every page that names a verb has controller copy and key copy (`Page.Shown`). **Contents** is a table of contents (`ContentsToc`): chapter titles and page numbers. Numbers match the book header. **Getting started** is a numbered first-time path (`GettingStarted`): title → captains → field → lineup → first pitch, followed by a mode spread for Exhibition, Training, and two-controller seating. Not Challenge, Toy Field, minigames, or records. **Controls** is three measured hardware spreads (`ControlDiagram.CalloutCell`): hardware, always, **green offense / red defense**. No tiny schematic. **In-game controls** gives batting, pitching, fielding, and running their own measured role spreads (`RoleTables`): verb | what you press, one scheme at a time. **Pitch and swing** is two cards (`HowToComic`): charge chip → commit chip, one caption. **Running** is three `BagDiagrams` (bag map, all-advance, all-return) plus **Close play** and **Tag** callouts. Right is 1B, up 2B, left 3B, down home — the same map as runner leads and throw tells. **The game screen** is two labeled HUD maps (`HudCallouts`): SET (scorebug, B/S/O, on-base, cards, TIRED) and in-play (YOU, landing ring, ITEM). **Chemistry** is two cards (`ChemBook`): hearts vs scribbles. **Who you are** uses one spread for the select card (PIT / BAT / FLD / RUN) and a continuation with four full-width ability rows (pitches / swings / running / fielding). Charge at MAX is the picture. Never print South / Space / left click on one line. If you change `Controls.cs`, Exhibition flow, SET cameras, or what a verb does on the body, update this file **and** `HowToPlay.cs` in the same PR.

Open `unity/` in Unity **6000.5.9f1**, Play `Assets/Scenes/HarborDiamond.unity`. That is the game. Trailer stills (plate / scoop / star, HUD off): **[docs/screenshot-gate.md](screenshot-gate.md)**. Agents capture those from Play without grinding the top of the first (`Grand Sluggers → Capture Still Gate`).

---

## Face buttons

South / East / West / North are **positions**, not Nintendo vs Xbox labels. The bottom face button is South on Xbox and Nintendo controllers.

| Verb | Controller | Keyboard | Mouse |
| --- | --- | --- | --- |
| Confirm / pitch / swing / catch / throw | South | Space / Enter | Left click |
| Charge | Hold/release South | Hold/release Space / Enter | Hold/release left click |
| Star | North | Q | Middle click |
| Aim / run | Left stick | WASD | Mouse move |
| Bags | D-pad diamond | 1 2 3 4 (arrows when not running) | Mouse quadrant / click bag |
| All advance / all return | LB / RB | `,` / `.` | — |
| Freeze | LB+RB | `/` | — |
| Steal | Stick toward the next bag, or L3 | Z | — |
| Changeup (pitch) | West | V | Left Ctrl |
| Swap pitcher / glove | Select | R | — |
| Bunt | West hold | V hold | Left Ctrl hold in the box |
| Cutoff / relay | LB after catch | X | — |
| Dive / jump | East / West | G / F | — |
| Attack (kick / smash item) | North in-play | B | Middle click in-play |
| Start / call time | Menu / Start | H / Esc | Esc |
| Night | R3 | N | — |

South / East / West / North: Xbox A/B/X/Y, Nintendo B/A/Y/X. Keyboard: Space / G / F·V / Q.

One player with controller 1, or two controllers. Hold **South** to charge and release it to pitch or swing. Space / Enter / left click is the same one-button load for keyboard and mouse. Rumble on bat contact and on a star. **Keyboard and mouse are player 1 only.** Controller 2 is a second gamepad. Mouse analog is **right-click hold and drag this frame** — a parked cursor is dead, including while you charge, so the pitcher stays on the rubber. WASD still walks; a key already down when SET starts does not. A sitting stick is dead until it passes through rest. Left click is confirm.

The in-game booklet **Controls** page is a hardware diagram (`ControlDiagram`), not this table. **Controller | Keyboard + mouse** (`BookScheme`) follows the active Player 1 input; the toggle locks it. On launch, **Auto** chooses a connected controller before keyboard/mouse. Press **F6** on the title screen to cycle and persist **Auto → Controller → Keyboard + mouse** for controller testing. If Controller is selected with no connected controller, keyboard and mouse still navigate and become Player 1 at first pitch. Green is offense. Red is defense.

---

## Exhibition (the product)

Three innings at Harbor. Home bats in the bottom. **1 PLAYER** (the default): controller 1 pitches the top and hits the bottom against CPU. **2 PLAYERS** on captains: controller 1 is home, controller 2 is away — they pitch and hit at the same time. Plugging in controller 2 does not start 1v1 until you pick 2 PLAYERS.

### Title

The park is the poster: dirt and the diamond, from in front of the backstop. **GRAND SLUGGERS** is a sticker over the infield — readable without F2, not a board through the toy. The home captain is the toy in front. Other captains wait for select.

- **South / Space** — play ball (pick captain)
- **Start / H** — cycle Exhibition / Challenge / Training (Challenge stays later)
- **Esc** — How to play (the book). Works on title, captains, field, lineup, and during a pitch.
- **West / F** — Training drills on Harbor
- **R3 / N** — night (sky gag)
- **Tab** — 3 / 6 / 9 innings

Captains and the field are two screens. The park does not follow the captain.

### Pick captain

The toys are the UI. Highlighted captain **steps forward**. Camera looks at the **toy** (face and body), not the brim and not the plate dirt. The **HUD card** (P / B / F / R, star pitch, star swing, field verb) is the only panel.

- **1 PLAYER / 2 PLAYERS** at the top — one controller vs CPU, or two controllers. Default is one player even if controller 2 is plugged in.
- **LB / `,`** — 1 player · **RB / Tab** — 2 players. Click the tabs. Two players needs controller 2.
- **Stick / WASD L/R** — your team · **U/D** — the other (controller 2 L/R their team when 2 PLAYERS)
- **North / Q** — you are **HOME** or **AWAY**. HOME bats the bottom. AWAY bats the top.
- **South / Space** — pick the field
- **West / F** — title
- Camera looks at the home captain. The title shot sits **in front of the backstop** and looks into the diamond — the cage grid is not the picture.

### Pick the field

A **postcard**: park name, DAY / NIGHT, one-line gimmick. Harbor is the slice — crowd of people, padded wall with ads, a scoreboard that keeps the score, brick town. Not an empty diamond.

- **Stick / WASD L/R** — cycle the park. Captains stay put.
- **South / Space** — lineup
- **West / F** — back to captains
- **R3 / N** — night

### Lineup (Team Setup, then Offense / Defense Setup)

Two screens. Not a 3D huddle with a name list.

**Team Setup.** Home nine along the **top** (captain filled, eight empty). Away nine along the **bottom** (CPU-filled until a second controller sits). Center is a grid of heads (`Look.Portrait`). Hearts / scribbles vs the captain. Starting stars **jump** on the home row.

- **Stick / WASD** — pick a head (center) or a slot (home row)
- **South / Space** — drop the head into the highlighted empty slot. When the nine are full, South goes to defense
- **West / F** — remove (captain stays)
- **Tab** — random fill. A button, not the product path

**Offense / Defense Setup.** Home batting **1–9 as a bar of heads** across the top. Away bar across the bottom. **Two fielding diamonds** in the middle (home left, away right). Heads sit on P / C / 1B / 2B / 3B / SS / LF / CF / RF. The character card stickers the highlighted head — it does not replace the diamonds.

- **Stick on the bar** — reorder batting (1–9 round-trips)
- **Stick on the diamond** — move the glove
- **LB / `,` · East / G** — cycle order
- **RB / Tab** — cycle glove
- **West / F** — back to Team Setup
- **South / Space** — first pitch

### Pitching and hitting (same four verbs)

| Verb | Pitch | Swing |
| --- | --- | --- |
| Tap and release South / Space / left click | Normal — easier control | Slap — better contact |
| Hold the same button, release at MAX | Charge pitch — fast; rings line up then **decay** | Charge swing — extra-base; same rings |
| Modifier | West / V through release = **changeup** (hangs then dumps) | West hold / V = **bunt** |
| North + South / Q + Space | Star pitch (costs a star even if hit) | Star swing (costs a star even on a miss) |

Press South / Space / left click to start the load; release the same button to throw or swing. A quick tap is the normal pitch or full slap swing. Hold until the rings line up, then release → **Nice!** on the mound, **MAX** at the plate; that is the charge tell, not a verdict. The word for the contact — **PERFECT**, **NICE**, **SOUR** — comes only when the bat meets the ball. Holding beyond the MAX band loses power. The pitch has a **gold streak** so you can see it come in. Release the swing before the ball reaches home: the bat reaches contact 0.30 seconds after release. Timing is judged when the bat comes around. Only holding West / V / Ctrl bunts. The pitcher releases the ball at the 0.42-second mark of the delivery after button release. Pitcher, batter, and ball share contact slow-down and pause.

This input shape is confirmed by the original *Mario Super Sluggers* booklet: the sideways Wii Remote uses one button for a normal action and hold-then-release for a charge; its batting cursor moves with the batter; and pitch curve is applied left/right after release. Harbor adapts that contract to South / Space / left click and keeps its existing changeup and star modifiers. The current feel values — 0.55 seconds to fill a pitch, 0.45 seconds to fill a swing, a 0.50-second MAX band, and 0.80 charge/second overcharge decay — are Grand Sluggers tuning pending the human parity gate, not measured Nintendo values. Reference: [Nintendo instruction booklet, printed pp. 6–9](https://www.mariomayhem.com/downloads/mario_instruction_booklets/Mario_Super_Sluggers_-_ML1_Manual_-_WII.pdf).

SET forks **by role in 1P**, and **stays behind home in 1v1**. **One controller, pitching:** camera stays on the **mound 3/4** (`mound`) — first-base over-the-shoulder behind the rubber, pitcher large on the right, rubber in the bottom, looking at the box — through SET and the throw. **One controller, batting:** camera stays on the **plate 3/4** (`plate`) — behind home looking at the mound, batter left of the look, pitcher in the diamond — through SET and the throw. Catcher crouches behind the camera. Pentagon and two boxes have dirt between them. It does not cut to `pitch`. **Two controllers:** camera stays on the **plate 3/4** (`plate`) — behind home — through SET and the throw, whether you pitch or hit. Controller 2 does not fork the HUD. Pink/gold charge ring **around the box** on the packed dirt (not a pancake under the feet). ~1s to the plate (Sluggers pace, not MLB 90). Home bats the bottom. Scorebug sits top-right; batter card bottom-left; pitcher card bottom-right. Highlight “your” card. Those anchors do not move.

- **Stick L/R / WASD** — walk the rubber before the throw, or walk the box until the swing commits. **Down** resets (in SET only). Pitching L/R is screen-relative from both the mound and plate cameras. Walking the rubber moves where the pitch crosses by the same distance you moved; a normal pitch crosses mid-frame, a changeup crosses low.
- **Stick L/R at contact** — spray. Past the foul line is a **foul** (strike unless you already have two). The ball flies there. Not a K at two strikes.
- **Stick L/R after release** — break toward that side of the screen from either pitching camera; the bend grows late and moves the crossing at most half a frame-width, and a charged pitch or a changeup barely bends. Not a pitch-type cycle. A pale ring on the plate is your **aim tell**: it sits where the ball will cross, walks with you on the rubber, and slides with the break.
- **Sweet-spot oval** follows the batter, never the pitch. Walk so its center eats the ball. The oval is the bat: its heart is PERFECT, the oval NICE, the rim SOUR; it is as tall as the strike frame, so any strike is on the bat. Where the ball meets the oval decides how hard you hit it; **when** you swing decides where it goes — early pulls, late pushes, outside the window is a whiff (9 frames for a slap, 7 for a charge). A charge narrows the oval and the window and pays ×1.25 on a perfect. Stick L/R at contact nudges the direction; stick up tops it, stick down lifts it.
- **Take outside the white frame** — ball. Swing and miss outside it — strike.
- **D-pad / 1 2 3 + South** — pickoff before the pitch. A glued runner goes back; a dancing lead can be out.
- **Select / R** — swap pitcher (when they sweat, they are tired).
- **Start / H / Esc** during SET or in-play — **call time**: Resume, Restart, How to play, Title. **WASD or arrows** choose. South / Space / left click ok. Click a row. Wheel turns How to play pages. East / right click / Esc back. Tab on the title cycles 3 / 6 / 9 innings.

Star pitch owns the ball ~2 seconds. Scorebug mutes. Then baseball.

### Batting (running)

- **LB / `,`** — all advance · **RB / `.`** — all return · **both / `/`** — halt all
- **Stick toward a bag + halt** — freeze that runner only. They keep the lead they have.
- **D-pad / 1 2 3** — select a runner (right 1B, up 2B, left 3B). **Down / 4** is home — not stealable. Default highlight is the lead runner.
- **Stick** toward the next bag — arm a **steal** on the highlighted runner (same as L3). Back — return, which cancels it. There is no lead stick.
- **L3 / Z** — steal the selected runner toward their next bag. They go on the pitch. No steal home.
- After a take or swing-and-miss the **catcher guns**. Arm **2B** (default on a steal of second), or **1B** to try a pickoff, then press **South**. The ball must reach the bag and tag the runner before the result is called. Early throw that beats the runner is **CAUGHT STEALING**; late is **STOLEN BASE**. Dead stick: CPU catcher still guns. Take the stick and you own it.
- **Mash South / Space** after contact — **dash**. The play stays live until every **live** runner has been **on a bag for 1 second**. An out with nobody left (throw-out at first, empty bases) ends it there. 3 outs too. Picking up the ball does not end a race. Sac fly: they leave on the catch if you sent them.
- **West / South** near the bag — slide
- **Close play** at third or home — the camera sits on the bag. First **South / left click** after the icon wins. Runner is safe if offense is first; out if defense is first. CPU mashes on a delay from Run / Field. A bang-bang beat (throw to first they just make, or the mash) pops a small **SAFE**.
- **Tag.** Have the ball and touch a runner off a bag. That's a tag. On a bag they are safe. Force still needs a throw.

Fair contact always sends the batter to first. On a fly, runners hold; all-advance tags up after the catch. Mini diamond shows leads, not just occupied bags. Mini diamond + banner match the out/safe.

In-play HUD (the booklet screen): **YOU** names the glove (bag + name) and **stays up** — a gold ring sits at their feet. When the stick is dead, they still run to the ball like CPU. Stick steers; it does **not** throw for you. A **yellow circle on the grass** is where the fly lands (the landing ring). It turns **red** in the jump window. **ITEM → name** plus a gold ring on that body when a chemistry item is armed. Pitcher card is **ARM**; below 25 it reads **TIRED** and sweats. When the play is **dead** (nobody still running), a stamp names it **on the field** — **BALL**, **STRIKE**, **FOUL**, **WALK** (smaller and quicker), **HIT BY PITCH**, **STRIKE OUT**, **BUNT**, **STOLEN BASE**, **CAUGHT STEALING**, **OUT**, **DOUBLE PLAY**, **TRIPLE PLAY**, **SINGLE**, **DOUBLE**, **TRIPLE**, **HOME RUN**, **GRAND SLAM** — then SET for the next pitch. An inside take that hits the batter is **HIT BY PITCH** and awards first, like a walk. Hold **West / V** through the swing to **bunt**. **Stick toward the next bag / L3 / Z** with a runner on arms a **steal** (no steal home). A runner still going with fewer than three outs keeps play alive.

### Fielding (the ball is in play)

Nearest glove **lights**. The ball **hangs** so you can get there. Leave the stick still: CPU runs to the **landing** on a fly (not the live ball — that would send them home first) and **still can catch** if they are under the ring. Then they chase the hop once it is down. They do **not** throw for you. Gloves stay **inside the wall** — each park's fence is the boundary. Push the stick to take that glove. **Standing on the ball scoops it** — no South, no stick. South still catches a fly in the window. Select / **R** cycles who you are. CPU covers the bags. A ball over an infielder stays their hop on the dirt; once it reaches the outfield grass the outfielder charges and the glove hands off.

On contact the camera sits at **45°** on the dirt under the ball. **CF is the top of the look** — home sits under second. A **fly pulls back** (same angle, more grass). The ball keeps the gold streak. It follows that spot through the hopper, fly, and throw. No cut behind the thrower. Charge ring sits **on the dirt**. Contact is a crack, a camera punch, and a dirt puff. Fielders **run**; they do not skate. A good throw is a purple laser; a bad throw is muddy.

- **Stick / WASD** — steer the YOU glove. Dead stick they still chase and scoop like CPU. After the catch, stick **runs with the ball**. They do not throw for you. **Hold East / G** to dash.
- **Select / R** — swap to the pulsing glove (stick points at who you want; dead stick is next-nearest to the landing / ball). HUD **R → CF**. Not while you hold the ball.
- **South / Space** — catch (in the window) when you have the glove; after the catch, throw. On a fly, South still scoops if you are under it.
- **East tap / G** — **dive**. The body lunges toward the ball. A dive catch stamps **DIVE**.
- **West / F** — **jump** in the window / buddy jump. Press West as the ring turns red; the leap stays armed through the window. A jump catch stamps **JUMP**. A would-be homer is a wall play: West (or two bodies, West) in the window robs. South does not. Super Jump / Grow / Clamber add window, not a skip. Miss = the ball drops (or a homer). Dead stick: CPU still can catch.
- **South / Space** — catch a fly in the window; after the catch, throw. On the dirt, touching the ball scoops it.
- **East tap / G** — **dive**. The body lunges toward the ball. A dive catch stamps **DIVE**.
- **West / F** — **jump** in the window / buddy jump. Press West as the ring turns red; the leap stays armed through the window. A jump catch stamps **JUMP**. A would-be homer is a wall play: West (or two bodies, West) in the window robs. South does not. Super Jump / Grow / Clamber add window, not a skip. Miss = the ball drops (or a homer). Dead stick: CPU still can catch.
- **E** while chasing a chem partner — **buddy toss** (they take the laser)
- **North / B / middle click** — **attack**. Kick the ball to a nearby glove (chem partner lasers; anyone close takes a short toss). Smash a flying error item before it lands.
- **D-pad / 1 2 3 4** — arm a bag (right 1B, up 2B, left 3B, down home). A mini-diamond pip lights the armed bag. **South** throws. Hopper with no bag throws to **second** when first is occupied, else **first**. LB / X with no direction is a **relay**, not a random bag. You can arm before the glove; the throw waits for South. Stick after the catch **runs**, it does not throw. **Outs land on the catch, when the throw lands, when you have the ball and touch a runner off a bag, or when you step on a force bag with the ball** (1B on first retires the batter — you do not tag them). A ball to the outfield is not a force at second until someone throws there. They do not gun to first on a dead stick when you are on defense. CPU defense (you are batting) still throws — you see it.
- **Turn two.** Runner on first, hopper to an infielder: throw to second (force), you are the glove at that bag, throw to first. Beat the batter → two outs. Late → runner on first, force at second. Mini diamond updates as each out records. You throw both — one South on the hopper is not two outs. Dead stick does not turn two for you when you are on defense.
- **Force.** Batter is always forced to first. First occupied → force at second. First and second → force at third. Bases loaded → force at home. Beat the throw to the bag and they are out — no mash. Putting the batter out at first removes the other forces (then it is a tag). A throw to third or home is an out when you beat them. Mash (close play) is a tag at third or home, never a force.
- After the ball leaves your hand **you are the glove at that bag**. Stick can still take a different glove. A steal gun is the same throw from the catcher, without a hop.
- **LT+RB / South+LT / E** after contact — chemistry item (banana grass, rocket body, POW hop)

Good throws are gold/purple and fast. Bad throws are muddy and offline.

---

## Practice (Harbor)

Title **West**, then **stick** picks Pitch / Bat / Field / Run / Special / Free and **South** starts that lesson. **East / G** from pitching **skips to Fielding** (scoop), not the title. You are not trapped painting pitch types.

1. **Pitching** — throw; charge at MAX (rings line up); changeup / break; star
2. **Batting** — walk the oval onto the ball; charge at MAX
3. **Fielding** — catch, jump a fly, throw a named bag, **turn two** (second, then first), dash, buddy toss
4. **Running** — pick a runner, lead, steal, dash
5. **Special** — star pitch / star swing
6. **Free practice** — any verb, no gate

---

Lineup is Team Setup then Offense / Defense Setup. Chemistry still drafts as hearts and scribbles. Defense is nine gloves (P / C / 1B / 2B / 3B / SS / LF / CF / RF) on two diamonds. Home bats the bottom.

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

**Now (Harbor Exhibition).** Title looks into the park. One controller: SET is over the pitcher’s shoulder looking at the box when you pitch (mound 3/4) and behind home when you hit (plate 3/4). Two controllers: SET stays behind home (plate 3/4). Two chalk boxes and a pentagon on packed dirt. The infield is grass with dirt paths and a mound hill. On contact the camera sits at 45° on the dirt under the ball (CF at the top, home under second) and follows it through the throw. From those cameras Harbor is a place: outfield grass, a padded wall with ads, a scoreboard with numbers, a crowd of people not one card. Baseball is 0.62 ft. From the box the pitcher throws — windup, then the ball leaves that hand. Star specials own the ball or the field ~2 seconds HUD-off (Heatball/heat-swing core+embers on the body; Charm hearts; Prism ghosts; Phony grin decoy; Cask barrel; Skull; Furnace lava pool), then baseball. Scorebug mutes. Shared body is one chain with six SMS-ladder cuts (kid / pageant / speed / brick / ape / slug) so a HUD-off plate still names the type. Captain extras stay data. Still primitives, not a sculpted hero. Controller input uses the Input System: hold/release South charges and commits, South is a position on Xbox and Nintendo, rumble on contact and star. Keyboard and mouse are the same verbs (Space / left click hold/release, WASD / mouse run, 1–4 bags). Bat / glove / crowd bed are original wavs, not beeps. Still not a sculpted hero.

**Not yet the reason people stay.** Scoop still, star-swing still you would show a friend, captains that read at gameplay distance. Do not start Challenge island or extra parks as products before that.
