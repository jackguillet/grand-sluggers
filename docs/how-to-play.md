# How to play

Living spec for couch play. **Gamepad is the couch product. Keyboard is the same scheme** — every Exhibition verb has a key. F1/F2/F3 stay debug. Mouse is not a control.

**In the game:** Start / H during SET or a play opens **Call time** — Resume, Restart, **How to play**, Title. Those pages are `HowToPlay.Pages` (same copy as below). If you change `Controls.cs`, Exhibition flow, SET cameras, or what a verb does on the body, update this file **and** `HowToPlay.cs` in the same PR.

Open `unity/` in Unity **6000.5.9f1**, Play `Assets/Scenes/HarborDiamond.unity`. That is the game. Trailer stills (plate / scoop / star, HUD off): **[docs/screenshot-gate.md](screenshot-gate.md)**. Agents capture those from Play without grinding the top of the first (`Grand Sluggers → Capture Still Gate`).

---

## Face buttons

South / East / West / North are **positions**, not Nintendo vs Xbox labels. The bottom face button is South on Xbox and Nintendo pads.

| Verb | Pad | Keyboard |
| --- | --- | --- |
| Confirm / pitch / swing / catch / throw | South | Space / Enter |
| Charge | LT analog | Shift (hold = 1.0) |
| Star | North | Q |
| Aim / run | Left stick | WASD |
| Bags | D-pad diamond | 1 2 3 4 (arrows when not running) |
| All advance / all return | LB / RB | `,` / `.` |
| Freeze | LB+RB | `/` |
| Steal | L3 | Z |
| Changeup (pitch) | West | V |
| Swap pitcher / glove | Select | R |
| Bunt | West hold | V hold |
| Cutoff / relay | LB after catch | X |
| Dive / jump | East / West | G / F |
| Start | Menu / Start | H |
| Night | R3 | N |

South / East / West / North: Xbox A/B/X/Y, Nintendo B/A/Y/X. Keyboard: Space / G / F·V / Q.

One player. Analog **LT / ZL** charges (light pull starts the clock). Rumble on bat contact and on a star. Mouse is not a control.

---

## Exhibition (the product)

Three innings at Harbor. Home bats in the bottom. You pitch the top, you hit the bottom.

### Title

- **South / Space** — pick captain
- **Start / H** — cycle Exhibition / Challenge / Training (Challenge stays later)
- **West / F** — Training drills on Harbor
- **R3 / N** — night (rebuilds Harbor lighting)
- **Tab** — 3 / 6 / 9 innings

Captains and the field are two screens. The park does not follow the captain.

### Pick captain

- **Stick / WASD L/R** — home · **U/D** — away
- **South / Space** — pick the field
- **West / F** — title
- Highlighted captain shows a **card**: P / B / F / R pips (1–10), star pitch, star swing, field verb.
- Camera stays in front of the cage and looks at the home captain. The title shot sits **in front of the backstop** and looks into the diamond — the cage grid is not the picture.

### Pick the field

- **Stick / WASD L/R** — cycle the park. Captains stay put.
- **South / Space** — lineup
- **West / F** — back to captains
- **R3 / N** — night
- Looks into the park you picked (Harbor is the slice; other parks are JSON, not products yet).

### Lineup (draft the eight)

Chemistry graph is the point. Starting stars come from how the eight like the captain.

- **Stick / WASD** — slot (order) vs pool
- **West / F** — swap highlighted pool player into the slot
- **RB / Tab** — cycle glove (P / C / IF / OF)
- **LB / `,` · East / G** — batting order
- **South / Space** — first pitch

### Pitching and hitting (same four verbs)

| Verb | Pitch | Swing |
| --- | --- | --- |
| Tap South / Space | Normal — easier control | Slap — better contact |
| Hold LT / Shift, commit at MAX | Charge pitch — fast; rings line up then **decay** | Charge swing — extra-base; same rings |
| Modifier | West / V through release = **changeup** (hangs then dumps) | West hold / V = **bunt** |
| North + South / Q + Space | Star pitch (costs a star even if hit) | Star swing (costs a star even on a miss) |

Throw / swing when the rings line up → **Nice!** / **Nice Hit!**. Late charge is weaker than MAX.

SET pitching is **3/4 over the pitcher** (`mound`) looking at home. SET batting is **over the batter's shoulder** (`plate`) looking at the mound. The ball leaves the **hand**, stays readable to the plate, and trails. Home bats the bottom.

- **Stick L/R / WASD** — walk the rubber (pitch) or the box (hit). **Down** resets.
- **Stick L/R after release** — curve / late bite. Not a pitch-type cycle.
- **Sweet-spot oval** on the dirt is smaller than the zone. Walk so it eats the ball.
- **D-pad / 1 2 3 + South** — pickoff before the pitch. A glued runner goes back; a dancing lead can be out.
- **Select / R** — swap pitcher (when they sweat, they are tired).
- **Start / H** during SET or in-play — **call time**: Resume, Restart, How to play, Title. Tab on the title cycles 3 / 6 / 9 innings.

Star pitch owns the ball ~2 seconds. Scorebug mutes. Then baseball.

### Batting (running)

- **LB / `,`** — all advance · **RB / `.`** — all return · **both / `/`** — freeze
- **L3 / Z** — steal (lead runner; no steal home)
- **Stick** toward the next bag — lead; back — return
- **Mash South / Space** after contact — **dash** to first
- **West / South** near the bag — slide

Fair contact always sends the batter to first. On a fly, runners hold; all-advance tags up after the catch.

### Fielding (the ball is in play)

Nearest glove **lights**. Leave the stick still: CPU takes the hop and throws — the play ends. Push the stick to take that glove; South still scoops (the verb). Select / **R** cycles who you are. CPU covers the bags. A ball over an infielder stays their hop on the dirt; once it reaches the outfield grass the outfielder charges and the glove hands off.

Hopper cam is low in the grass; a fly is a 3/4 in the park; a homer rises with the ball; a throw sits on the glove.

- **Stick / WASD** — take the glove and run it (dead stick = CPU). **Hold East / G** to dash.
- **South / Space** — catch (in the window) when you have the glove; after the catch, throw
- **East tap / G** — dive
- **West / F** — jump / buddy jump
- **E** while chasing a chem partner — **buddy toss** (they take the laser)
- **D-pad / 1 2 3 4** — arm a bag (right 1B, up 2B, left 3B, down home). Stick flick or arrows after the catch. A mini-diamond pip lights the armed bag. Hopper with no direction throws to **first**. LB / X with no direction is a **relay**, not a random bag. You can arm before the glove. WASD while chasing does not throw.
- **Select / R** — swap the glove
- **LT+RB / South+LT / E** after contact — chemistry item (banana grass, rocket body, POW hop)

Good throws are gold/purple and fast. Bad throws are muddy and offline.

---

## Practice (Harbor)

Title **West**, then **stick** picks Pitch / Bat / Field / Run / Special / Free and **South** starts that lesson. **East / G** from pitching **skips to Fielding** (scoop), not the title. You are not trapped painting pitch types.

1. **Pitching** — throw; charge at MAX (rings line up); changeup / break; star
2. **Batting** — walk the oval onto the ball; charge at MAX
3. **Fielding** — catch, throw a named bag, dash, buddy toss
4. **Running** — lead, steal, dash
5. **Special** — star pitch / star swing
6. **Free practice** — any verb, no gate

---

Lineup still drafts chemistry. Defense is nine gloves (P / C / 1B / 2B / 3B / SS / LF / CF / RF). Home bats the bottom.

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

**Now (Harbor Exhibition).** Title looks into the park. SET pitching is 3/4 over the pitcher at the batter's box. SET batting is over the batter looking at the mound, two chalk boxes and a pentagon on packed dirt. The infield is grass with dirt paths and a mound hill. Flies, hoppers, throws, and homers are named 3/4s in the park, not a broadcast high-home. From those cameras Harbor is a place: outfield grass, a padded wall with ads, a scoreboard with numbers, a crowd of people not one card. Baseball is 0.62 ft. Star specials own the ball or the field ~2 seconds HUD-off (Heatball core+embers; Charm hearts; Prism ghosts; Phony grin decoy; Cask barrel; Skull; Furnace lava pool), then baseball. Scorebug mutes. Shared body is one chain with six SMS-ladder cuts (kid / pageant / speed / brick / ape / slug) so a HUD-off plate still names the type. Captain extras stay data. Still primitives, not a sculpted hero. Swing and scoop are authored verbs on that body (Contact 0.30 / 0.22); MoveBones is the fallback. Gamepad is Input System: analog LT charges, South is a position on Xbox and Nintendo, rumble on contact and star. Keyboard is the same verbs (Space confirm, WASD run, 1–4 bags). Bat / glove / crowd bed are original wavs, not beeps. Still not a sculpted hero.

**Not yet the reason people stay.** Scoop still, star-swing still you would show a friend, captains that read at gameplay distance. Do not start Challenge island or extra parks as products before that.
