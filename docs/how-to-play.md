# How to play

Living spec for couch play. **Gamepad is the couch product. Keyboard and mouse are the same scheme** — every Exhibition verb has a key and a mouse bind. Keyboard + mouse are player 1 only. F1/F2/F3 stay debug.

**In the game:** Start / H / Esc during SET or a play opens **Call time** — Resume, Restart, **How to play**, Title. How to play is a **full-screen illustrated booklet** for a 12-year-old: a picture of our toys, a handful of short sentences, couch-sized type. Click a row, or left-click to confirm. Those pages are `HowToPlay.Pages` (same map as below). Pictures live in `unity/Assets/Resources/Art/Booklet/`. If you change `Controls.cs`, Exhibition flow, SET cameras, or what a verb does on the body, update this file **and** `HowToPlay.cs` in the same PR.

Open `unity/` in Unity **6000.5.9f1**, Play `Assets/Scenes/HarborDiamond.unity`. That is the game. Trailer stills (plate / scoop / star, HUD off): **[docs/screenshot-gate.md](screenshot-gate.md)**. Agents capture those from Play without grinding the top of the first (`Grand Sluggers → Capture Still Gate`).

---

## Face buttons

South / East / West / North are **positions**, not Nintendo vs Xbox labels. The bottom face button is South on Xbox and Nintendo pads.

| Verb | Pad | Keyboard | Mouse |
| --- | --- | --- | --- |
| Confirm / pitch / swing / catch / throw | South | Space / Enter | Left click |
| Charge | LT analog | Shift (hold = 1.0) | Right click hold |
| Star | North | Q | Middle click |
| Aim / run | Left stick | WASD | Mouse move |
| Bags | D-pad diamond | 1 2 3 4 (arrows when not running) | Mouse quadrant / click bag |
| All advance / all return | LB / RB | `,` / `.` | — |
| Freeze | LB+RB | `/` | — |
| Steal | L3 | Z | — |
| Changeup (pitch) | West | V | Left Ctrl |
| Swap pitcher / glove | Select | R | — |
| Bunt | West hold | V hold | Left Ctrl hold in the box |
| Cutoff / relay | LB after catch | X | — |
| Dive / jump | East / West | G / F | — |
| Attack (kick / smash item) | North in-play | B | Middle click in-play |
| Start / call time | Menu / Start | H / Esc | Esc |
| Night | R3 | N | — |

South / East / West / North: Xbox A/B/X/Y, Nintendo B/A/Y/X. Keyboard: Space / G / F·V / Q.

One player, or two pads. Analog **LT / ZL** charges (light pull starts the clock). Rumble on bat contact and on a star. **Keyboard and mouse are player 1 only.** Pad 2 is a second gamepad. Mouse analog is **right-click hold and drag** — a parked cursor is dead stick, so field select stays Harbor. WASD still walks. Left click is confirm.

---

## Exhibition (the product)

Three innings at Harbor. Home bats in the bottom. One pad: you pitch the top, you hit the bottom. Two pads: pad 1 is home, pad 2 is away — they pitch and hit at the same time.

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

- **Stick / WASD L/R** — your team · **U/D** — the other (pad 2 L/R their team when seated)
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

**Team Setup.** Home nine along the **top** (captain filled, eight empty). Away nine along the **bottom** (CPU-filled until a second pad sits). Center is a grid of heads (`Look.Portrait`). Hearts / scribbles vs the captain. Starting stars **jump** on the home row.

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
| Tap South / Space | Normal — easier control | Slap — better contact |
| Hold LT / Shift, commit at MAX | Charge pitch — fast; rings line up then **decay** | Charge swing — extra-base; same rings |
| Modifier | West / V through release = **changeup** (hangs then dumps) | West hold / V = **bunt** |
| North + South / Q + Space | Star pitch (costs a star even if hit) | Star swing (costs a star even on a miss) |

Throw / swing when the rings line up → **Nice!** / **Nice Hit!**. Late charge is weaker than MAX.

SET forks **by role in 1P**, and **stays behind home in 1v1**. **One pad, pitching:** camera stays on the **mound 3/4** (`mound`) — first-base over-the-shoulder behind the rubber, pitcher large on the right, rubber in the bottom, looking at the box — through SET and the throw. **One pad, batting:** camera stays on the **plate 3/4** (`plate`) — behind home looking at the mound, batter left of the look, pitcher in the diamond — through SET and the throw. Catcher crouches behind the camera. Pentagon and two boxes have dirt between them. It does not cut to `pitch`. **Two pads:** camera stays on the **plate 3/4** (`plate`) — behind home — through SET and the throw, whether you pitch or hit. Pad 2 does not fork the HUD. Pink/gold charge ring **around the box** on the packed dirt (not a pancake under the feet). ~1s to the plate (Sluggers pace, not MLB 90). Home bats the bottom. Scorebug sits top-right; batter card bottom-left; pitcher card bottom-right. Highlight “your” card. Those anchors do not move.

- **Stick L/R / WASD** — walk the rubber (pitch) or the box (hit). **Down** resets.
- **Stick L/R at contact** — spray. Past the foul line is a **foul** (strike unless you already have two). The ball flies there. Not a K at two strikes.
- **Stick L/R after release** — curve / late bite. Not a pitch-type cycle.
- **Sweet-spot oval** on the dirt is smaller than the zone. Walk so it eats the ball.
- **D-pad / 1 2 3 + South** — pickoff before the pitch. A glued runner goes back; a dancing lead can be out.
- **Select / R** — swap pitcher (when they sweat, they are tired).
- **Start / H / Esc** during SET or in-play — **call time**: Resume, Restart, How to play, Title. **WASD or arrows** choose. South / Space / left click ok. Click a row. Wheel turns How to play pages. East / right click / Esc back. Tab on the title cycles 3 / 6 / 9 innings.

Star pitch owns the ball ~2 seconds. Scorebug mutes. Then baseball.

### Batting (running)

- **LB / `,`** — all advance · **RB / `.`** — all return · **both / `/`** — halt all
- **Stick toward a bag + halt** — freeze that runner only. They keep the lead they have.
- **D-pad / 1 2 3** — select a runner (right 1B, up 2B, left 3B). **Down / 4** is home — not stealable. Default highlight is the lead runner.
- **Stick** toward the next bag — lead on the highlighted runner; back — return
- **L3 / Z** — steal the selected runner toward their next bag. They go on the pitch. No steal home.
- After a take or swing-and-miss the **catcher guns**. Arm **2B** (default on a steal of second) and **South**. Early throw that beats the runner is **CAUGHT STEALING**; late is **STOLEN BASE**. Dead stick: CPU catcher still guns. Take the stick and you own it.
- **Mash South / Space** after contact — **dash**. The play stays live until every **live** runner has been **on a bag for 1 second**. An out with nobody left (throw-out at first, empty bases) ends it there. 3 outs too. Picking up the ball does not end a race. Sac fly: they leave on the catch if you sent them.
- **West / South** near the bag — slide
- **Close play** at third or home — the camera sits on the bag. First **South / left click** after the icon wins. Runner is safe if offense is first; out if defense is first. CPU mashes on a delay from Run / Field.

Fair contact always sends the batter to first. On a fly, runners hold; all-advance tags up after the catch. Mini diamond shows leads, not just occupied bags. Mini diamond + banner match the out/safe.

In-play HUD (the booklet screen): **YOU** names the glove you have (gone when the stick is dead). A **yellow circle on the grass** is where the fly lands (the landing ring). It turns **red** in the jump window. **ITEM → name** plus a gold ring on that body when a chemistry item is armed. Pitcher card is **ARM**; below 25 it reads **TIRED** and sweats.

### Fielding (the ball is in play)

Nearest glove **lights**. The ball **hangs** so you can get there. Leave the stick still: CPU runs to the **landing** on a fly (not the live ball — that would send them home first) and **still can catch** if they are under the ring. Then they chase the hop once it is down. They do **not** throw for you. Gloves stay **inside the wall** — each park's fence is the boundary. Push the stick to take that glove; South still scoops (the verb). Select / **R** cycles who you are. CPU covers the bags. A ball over an infielder stays their hop on the dirt; once it reaches the outfield grass the outfielder charges and the glove hands off.

On contact the camera sits at **45°** on the dirt under the ball. **CF is the top of the look** — home sits under second. A **fly pulls back** (same angle, more grass). It follows that spot through the hopper, fly, and throw. No cut behind the thrower. Charge ring sits **on the dirt**. Contact is a crack, a camera punch, and a dirt puff. Fielders **run**; they do not skate. A good throw is a purple laser; a bad throw is muddy.

- **Stick / WASD** — take the glove and run it (dead stick = CPU). After the catch, stick still **runs with the ball**. **Hold East / G** to dash.
- **Select / R** — swap to the pulsing glove (stick points at who you want; dead stick is next-nearest to the landing / ball). HUD **R → CF**. Not while you hold the ball.
- **South / Space** — catch (in the window) when you have the glove; after the catch, throw. On a fly, South still scoops if you are under it.
- **East tap / G** — dive
- **West / F** — jump in the window / buddy jump. A would-be homer is a wall play: West (or two bodies, West) in the window robs. South does not. Super Jump / Grow / Clamber add window, not a skip. Miss = the ball drops (or a homer). Dead stick: CPU still can catch.
- **E** while chasing a chem partner — **buddy toss** (they take the laser)
- **North / B / middle click** — **attack**. Kick the ball to a nearby glove (chem partner lasers; anyone close takes a short toss). Smash a flying error item before it lands.
- **D-pad / 1 2 3 4** — arm a bag (right 1B, up 2B, left 3B, down home). A mini-diamond pip lights the armed bag. **South** throws. Hopper with no bag throws to **second** when first is occupied, else **first**. LB / X with no direction is a **relay**, not a random bag. You can arm before the glove; the throw waits for South. Stick after the catch **runs**, it does not throw. They do not gun to first on a dead stick when you are on defense. CPU defense (you are batting) still throws.
- **Turn two.** Runner on first, hopper to an infielder: throw to second (force), you are the glove at that bag, throw to first. Beat the batter → two outs. Late → runner on first, force at second. Mini diamond updates as each out records. You throw both — one South on the hopper is not two outs. Dead stick does not turn two for you when you are on defense.
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

### Two pads (local 1v1)

Gamepad **0 is player 1**. **North** on captains picks HOME or AWAY for that pad. Gamepad **1 sits the other side**. Keyboard and mouse stay player 1. A second pad does not split the screen and does not go online.

- **Title / captains / Team Setup / Defense Setup.** Pad 1 edits their team. Pad 2 edits the other. Each picks their captain, roster, order, gloves.
- **First pitch.** Home pitches the top, away bats. Bottom: they swap. CPU never bats or pitches while both pads are seated.
- **SET.** Mound when a human is on the rubber, plate when you bat vs CPU. Same role recipe as 1P. Cards stay batter bottom-left, pitcher bottom-right. Highlight yours.
- **Pitch / swing.** Pad-on-mound walks the rubber, charges, throws. Pad-in-the-box walks the box, charges, swings. Same four verbs as 1P.
- **In-play.** Fielding pad takes the glove (stick to take, dead stick = CPU cover). Batting pad sends / returns / steals. Both at once.
- **Unplug pad 2.** That team becomes CPU without restarting the inning.

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

Couch, pad, three innings. You can name the captain with the HUD off. A perfect swing is illegal for two seconds and still baseball. A grounder is a scoop and a race.

**Now (Harbor Exhibition).** Title looks into the park. One pad: SET is over the pitcher’s shoulder looking at the box when you pitch (mound 3/4) and behind home when you hit (plate 3/4). Two pads: SET stays behind home (plate 3/4). Two chalk boxes and a pentagon on packed dirt. The infield is grass with dirt paths and a mound hill. On contact the camera sits at 45° on the dirt under the ball (CF at the top, home under second) and follows it through the throw. From those cameras Harbor is a place: outfield grass, a padded wall with ads, a scoreboard with numbers, a crowd of people not one card. Baseball is 0.62 ft. From the box the pitcher throws — windup, then the ball leaves that hand. Star specials own the ball or the field ~2 seconds HUD-off (Heatball/heat-swing core+embers on the body; Charm hearts; Prism ghosts; Phony grin decoy; Cask barrel; Skull; Furnace lava pool), then baseball. Scorebug mutes. Shared body is one chain with six SMS-ladder cuts (kid / pageant / speed / brick / ape / slug) so a HUD-off plate still names the type. Captain extras stay data. Still primitives, not a sculpted hero. Swing and scoop are authored verbs on that body (Contact 0.30 / 0.22); MoveBones is the fallback. Gamepad is Input System: analog LT charges, South is a position on Xbox and Nintendo, rumble on contact and star. Keyboard and mouse are the same verbs (Space / left click confirm, WASD / mouse run, 1–4 bags). Bat / glove / crowd bed are original wavs, not beeps. Still not a sculpted hero.

**Not yet the reason people stay.** Scoop still, star-swing still you would show a friend, captains that read at gameplay distance. Do not start Challenge island or extra parks as products before that.
