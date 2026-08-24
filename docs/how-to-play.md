# How to play

Living spec for couch play. **Gamepad is the product. Keyboard is a debug overlay.**

If you change `Controls.cs`, Exhibition flow, SET cameras, or what a verb does on the body, update this file in the same PR.

Open `unity/` in Unity **6000.5.9f1**, Play `Assets/Scenes/HarborDiamond.unity`. That is the game.

---

## Face buttons

South / East / West / North are **positions**, not Nintendo vs Xbox labels.

| Position | Xbox | Nintendo | Keyboard debug |
| --- | --- | --- | --- |
| South | A | B | Space / Return |
| East | B | A | G |
| West | X | Y | F (tap) · V (hold) |
| North | Y | X | Q |
| LT | LT | ZL | Shift |
| RT | — | — | — |
| LB | LB | L | X |
| RB | RB | R | Tab |
| Start | Menu / Start | + | H |
| R3 | Right-stick click | Right-stick click | N |

Left stick = WASD / arrows. D-pad bags = 1 2 3 4.

---

## Exhibition (the product)

Three innings at Harbor. Home bats in the bottom. You pitch the top, you hit the bottom.

### Title

- **South** — pick captain
- **Start** — cycle Exhibition / Challenge / Training (Challenge stays later)
- **West** — Training drills on Harbor
- **Stick L/R** — home captain (park follows)
- **Stick U/D** — away captain
- **R3 / N** — night (rebuilds Harbor lighting)
- **C tap** — cycle park JSON (Harbor is the slice; others are not products yet)
- **C hold** — night

### Pick captain

- **Stick L/R** — home · **U/D** — away
- **South** — lineup
- Camera stays in front of the cage and looks at the home captain. Park stays what you picked on the title (Harbor is the slice).

### Lineup (draft the eight)

Chemistry graph is the point. Starting stars come from how the eight like the captain.

- **Stick** — slot (order) vs pool
- **West** — swap highlighted pool player into the slot
- **RB** — cycle glove (P / C / IF / OF)
- **LB / East** — batting order
- **South** — first pitch

### Pitching (Top)

SET starts **catcher-eye** (`plate`) so you see the batter. Take the rubber (charge or aim) and the camera is **3/4 mound**. Throw cuts to mound.

- **Stick** — aim the zone
- **LT hold** — charge
- **South** — pitch
- **RB** — cycle fastball / changeup / curve / slider
- **North** — star pitch (if you have a star)
- **Select / R** — swap pitcher

Star pitch owns the ball ~2 seconds (Heatball, Charmball, Prism, Phony, Cask, Skull). Scorebug mutes. Then baseball.

### Batting (Bottom)

- **LT hold** — charge
- **South** — swing (timing vs the pitch)
- **Stick** — spray
- **West hold** — bunt
- **North** — star swing
- **LB** — steal (runner on)
- **Stick / West / South** — lead / return when on the bags

Perfect / star swing freeze the picture briefly. HUD mutes during the spectacle.

### Fielding (the ball is in play)

Nearest glove lights. CPU runs the hop unless you take it.

- **Stick** — run the glove
- **South** — catch (in the window)
- **East** — dive
- **West** — jump / buddy
- **D-pad or stick flick** — throw: right 1B, up 2B, left 3B, down home (keyboard 1 2 3 4)
- **LT+RB / South+LT / E** — chemistry item after contact (banana grass, rocket body, POW hop), then **RB** to cycle the item, **stick** to aim the target

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
| **[** | Slow-mo cycle (F2 must be on) |
| **]** | Freeze camera (F2 must be on) |

F2 is how you name the still (plate vs mound vs diamond-line). It does not replace the scorebug.

---

## What a stranger should feel

Couch, pad, three innings. You can name the captain with the HUD off. A perfect swing is illegal for two seconds and still baseball. A grounder is a scoop and a race.

**Now (Harbor Exhibition).** Title looks into the park. SET pitching starts catcher-eye, then 3/4 mound. Baseball is 0.62 ft. Heatball is a core + embers. Scorebug can mute. Shared body is a toy-proportion blockout (one chain, captain extras). Swing and scoop are authored verbs on that body (Contact 0.30 / 0.22); MoveBones is the fallback. Still not a sculpted hero.

**Not yet the reason people stay.** Scoop still, star-swing still you would show a friend, captains that read at gameplay distance. Do not start Challenge island or extra parks as products before that.
