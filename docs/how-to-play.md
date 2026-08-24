# How to play

Living spec for couch play. **Gamepad is the couch product. Keyboard is the same scheme** — every Exhibition verb has a key. F1/F2/F3 stay debug. Mouse is not a control.

If you change `Controls.cs`, Exhibition flow, SET cameras, or what a verb does on the body, update this file in the same PR.

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
| Cycle pitch | RB (SET pitching) | Tab |
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
- **Stick L/R / WASD** — home captain (park follows)
- **Stick U/D / WASD** — away captain
- **R3 / N** — night (rebuilds Harbor lighting)
- **C tap** — cycle park JSON (Harbor is the slice; others are not products yet)
- **C hold** — night

### Pick captain

- **Stick / WASD L/R** — home · **U/D** — away
- **South / Space** — lineup
- Camera stays in front of the cage and looks at the home captain. Park stays what you picked on the title (Harbor is the slice).

### Lineup (draft the eight)

Chemistry graph is the point. Starting stars come from how the eight like the captain.

- **Stick / WASD** — slot (order) vs pool
- **West / F** — swap highlighted pool player into the slot
- **RB / Tab** — cycle glove (P / C / IF / OF)
- **LB / `,` · East / G** — batting order
- **South / Space** — first pitch

### Pitching (Top)

SET pitching is **3/4 over the pitcher** (`mound`) looking at home: rubber in the bottom, batter + catcher + **chalk batter's boxes** readable at the plate. SET batting is **over the batter's shoulder** (`plate`) looking at the mound: batter in the box (feet to hat), **two chalk rectangles and a pentagon** on packed dirt, pitcher in the distance. Analog LT lights a **charge ring** on the dirt; the zone locator sits in the box; the ball trails in flight. Infield is grass with dirt *paths* and a mound hill — not a brown slab. The catcher is not the subject. Scorebug names inning, score, B/S/O, runners, P, AB, stars, and NEXT without F2. In play: hoppers stay low, lines sit mid, flies are a 3/4 in the park, homers rise with the ball, throws sit on the glove.

- **Stick / WASD** — aim the zone
- **LT / Shift** — charge (analog)
- **South / Space** — pitch (after a short ready beat)
- **RB / Tab** — cycle fastball / changeup / curve / slider
- **North / Q** — star pitch (if you have a star)
- **Select / R** — swap pitcher

Star pitch owns the ball ~2 seconds (Heatball, Charmball, Prism, Phony, Cask, Skull). Scorebug mutes. Then baseball.

### Batting (Bottom)

Camera stays **over the batter** (`plate`) for the whole pitch: you, the box, the pitcher.

- **LT / Shift** — charge (analog)
- **South / Space** — swing (timing vs the pitch)
- **Stick / WASD** — spray
- **West hold / V** — bunt
- **North / Q** — star swing
- **LB / `,`** — all advance · **RB / `.`** — all return · **both / `/`** — freeze
- **L3 / Z** — steal (lead runner; no steal home)
- **Stick / WASD** toward the next bag — lead (`Lead01`); back — return
- **West / South** near the bag — slide

Fair contact always sends the batter to first. On a fly, runners hold; all-advance tags up after the catch.

Perfect / star swing freeze the picture briefly. HUD mutes during the spectacle.

### Fielding (the ball is in play)

Nearest glove **lights**. Leave the stick still: CPU takes the hop and throws — the play ends. Push the stick to take that glove; South still scoops (the verb). Select / **R** cycles who you are. CPU covers the bags.

Hopper cam is low in the grass; a fly is a 3/4 in the park; a homer rises with the ball; a throw sits on the glove.

- **Stick / WASD** — take the glove and run it (dead stick = CPU)
- **South / Space** — catch (in the window) when you have the glove; after the catch, throw
- **East / G** — dive
- **West / F** — jump / buddy
- **D-pad / 1 2 3 4** — arm a bag (right 1B, up 2B, left 3B, down home). Stick flick or arrows after the catch. A mini-diamond pip lights the armed bag. Hopper with no direction throws to **first**. LB / X with no direction is a **relay**, not a random bag. You can arm before the glove. WASD while chasing does not throw.
- **Select / R** — swap the glove
- **LT+RB / South+LT / E** — chemistry item after contact (banana grass, rocket body, POW hop), then **RB / Tab** to cycle the item, **stick / WASD** to aim the target

Good throws are gold/purple and fast. Bad throws are muddy and offline.

---

## Training (Harbor drills)

Title **West**. Five drills, then back.

1. Paint the zone — pitches in the zone, including a star
2. Time it and charge — swing
3. Catch it, throw a bag
4. Grab a grounder, throw to first
5. Throw to a buddy, then a rival

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
