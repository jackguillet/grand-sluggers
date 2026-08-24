# Screenshot gate — Harbor Exhibition stills

The rails are closed. These stills are the product. If you would not show a picture to a friend, epic #188 stays open.

Gamepad is the product. Keyboard is listed so an agent (or a desk without a pad) can drive the same path.

---

## What you are judging

Three stills. HUD off means **F2 off** and the scorebug not covering the subject. F3 mutes the play HUD (debug). Star spectacle already mutes it.

| # | Still | Must show | Fail if |
|---|---|---|---|
| 1 | **Batting SET** (`plate`) | Full batter in the box (feet to hat). Pentagon + two chalk rectangles on packed dirt. Pitcher in the distance. You can **name the captain**. Catcher is not the subject. | Catcher-spine, cage grid in the dirt, cap close-up, “brown cube with a brim,” camera through the backstop |
| 2 | **Scoop** | Glove on the dirt, ball in the glove, runner leaving the box, grass. Camera is a 3/4 in the park (`diamond-grounder` / hopper), not high-home. | Cubes chasing a marble, auto-glove with no scoop verb, broadcast high-home |
| 3 | **Star swing** | Body owns ~2 seconds (Heatball core+embers on Rio, etc.). Scorebug gone. Then baseball. | Full-screen white/black blind, a smear with no body, HUD still talking |

Bonus stills that save a later sitting (same rules):

| Still | Shot name (F2) | Must show |
|---|---|---|
| Pitching SET | `mound` | Pitcher 3/4, rubber in the bottom, batter + catcher + boxes at home |
| Title | `title` | Looks **into** Harbor, not at a menu wall |
| Throw | `throw` | On the glove, ball leaving the hand |
| Fly | `diamond` | 3/4 in the park, fielder reads, ball is a baseball |
| Homer | `diamond-homer` | Camera rises with the ball, then baseball |

Dolphin Super Sluggers is **compare only**. Do not dump Nintendo assets. Do not mash A into a live session unless you are okay with skipping a prompt.

---

## Setup (5 minutes)

1. Unity **6000.5.9f1**. Project folder `grand-sluggers/unity`. Scene `Assets/Scenes/HarborDiamond.unity`.
2. Click the **Game** tab. Not Scene. The editor Scene/Game view looking **through the backstop cage** is not Exhibition.
3. Hover the Game panel, **Shift+Space** to maximize it. Scale **1x** if the Game view scale slider is below 1.
4. Plug in a pad. Xbox A / Nintendo B is **South**. Analog **LT / ZL** charges.
5. Day. Harbor. Home **Rio** (short, round) vs away **Ashlord** or **Konga** (so the height ladder is obvious). Stick L/R home, U/D away on the title.
6. F1 (timing bar) **off**. F2 **off** for trailer stills. F3 mutes the scorebug when you need HUD-off without a star.
7. Put Dolphin on the other display if you want a live compare. Screenshot it with macOS **Cmd+Shift+5 → window**, not a phone pic of the editor chrome.

macOS capture of Unity: **Cmd+Shift+5 → Capture Selected Window → click the Game view** (or the Unity window if Game is maximized). Do not Cmd+Shift+3 the whole desktop — Grok/Safari steal that.

Name files so an agent can file issues from them:

```
plate-rio-hudoff.png
mound-rio.png
scoop.png
star-rio-heat.png
plate-rio-f2.png          (optional, F2 on, so we can read SHOT)
```

Drop them in this chat or `scratchpad/stills/`.

---

## Path A — Exhibition (the product)

Title → South (pick captain) → South (lineup) → South (first pitch). Do not tap Start (that cycles Exhibition / Challenge / Training). Do not tap West (Training). Leave park on Harbor (`C` cycles parks; skip it).

### Pitching SET (`mound`) — top of 1

You pitch the top. Camera should be 3/4 over the pitcher looking at home.

- Hold **LT** to charge. Ring / pull-back should read. Stick aims the zone locator.
- F2 once: the overlay must say `SHOT MOUND`. F2 again (off). F3 if you want HUD off.
- Capture.

Take three outs however you like (South to pitch, meatballs are fine). Bottom of 1 is batting.

### Batting SET (`plate`) — bottom of 1 — **still 1**

Camera stays over the batter looking at the mound for the whole pitch.

- You in the box, feet to hat. Two chalk boxes + pentagon. Pitcher in the distance.
- Hold **LT**. Charge ring on the dirt around the box.
- F2: `SHOT PLATE`. Off. F3: HUD off.
- Capture **before** you swing.
- Fail the still if you are looking through the cage or at the catcher’s spine.

### Star swing — **still 3**

You need a star (chemistry from the lineup). **North / Y / Q** arms the star (gold tell). Hold LT, South to swing.

Capture at the peak (~1 second in) while the scorebug is gone. Then one still a second later that is baseball again (scorebug can return).

Rio = Heatball / heat-swing. Vale = charm. Zig = prism/shell. Brondo = phony. Konga = cask. Ashlord = skull / furnace.

### Scoop — **still 2**

Exhibition hoppers are RNG. Faster harness is Training drill 4 (Path B). If you stay in Exhibition: a grounder, South to catch in the window, capture glove-on-dirt.

---

## Path B — Training scoop (faster for still 2)

Title **West** (or Start until TRAINING, then South). Harbor drills, Rio vs Ashlord.

| Drill | What | Pad | Keyboard |
|---|---|---|---|
| 1 Paint the zone | Four pitch types in the zone + a star | Stick aim, RB cycle type, LT charge, South pitch, North star | WASD, Tab, Shift, Space, Q |
| 2 Time it and charge | Contact with charge | LT + South on the pitch | Shift + Space |
| 3 Catch it, throw a bag | Catch + throw | South catch, D-pad / stick flick bag | Space, 1/2/3/4 |
| 4 **Grab a grounder** | **This is still 2** | Stick to the hop, South scoop, 1 to first | WASD, Space, 1 |

On drill 4: scoop still = glove in the dirt, ball in the glove, runner leaving. Then throw still if you get it.

---

## Pad ↔ keyboard (debug overlay)

| Verb | Pad | Keyboard |
|---|---|---|
| South | Xbox A / Nintendo B | Space / Return |
| East | Xbox B / Nintendo A | G |
| West | Xbox X / Nintendo Y | F tap · V hold |
| North (star) | Xbox Y / Nintendo X | Q |
| LT charge | analog LT / ZL | Shift (full) |
| RB cycle pitch | RB / R | Tab |
| Start (mode) | Menu / + | H |
| Night | R3 | N |
| Timing bar | — | F1 |
| Shot name overlay | — | F2 |
| **Mute play HUD** | — | **F3** |
| Slow-mo / freeze cam | — | [ / ] (F2 must be on) |
| Throw bags | D-pad | 1 2 3 4 |

---

## How to reject (write this on the still)

One line per picture. Examples:

- `plate: catcher-spine, cage in lens`
- `plate: Rio does not read — cube hat, no face`
- `plate: boxes read, captain reads — PASS`
- `scoop: no glove, ball is a marble`
- `star: HUD still up, no body in the fire`

A pass is “I would put this on a trailer.” A fail files a GitHub child from the **picture**, not from a vibe. Do not reopen #189–#198 as “do the rail again.”

---

## Agent path (no pad)

Personal Unity cannot `-batchmode`. The agent does **not** invent a screenshot. It drives the **already-open editor**.

1. Write `unity/Temp/gs-still-request.json` (schema: `StillRequest` in Sim).
2. Enter Play on `HarborDiamond.unity` (menu **Grand Sluggers → Capture Still Gate**, or Cmd+P after the request exists).
3. Play skips the title, jumps to Exhibition SET, **cuts** named cameras, writes PNGs with `Camera.Render` (world only — no OnGUI, so HUD-off is honest).
4. Done file: `unity/Temp/gs-still-done.json`. PNGs default to `unity/Temp/gs-stills/{shot}.png`.

Default request:

```json
{
  "shots": ["title", "plate", "mound"],
  "home": "rio",
  "away": "ashlord",
  "hudOff": true,
  "feelDebug": false,
  "width": 1920,
  "height": 1080
}
```

**What this can judge without a human:** SET cameras, diamond kit from `plate`/`mound`, toy body at gameplay distance, Harbor postcard behind the box. Same stills as #1’s framing.

**What it cannot fake:** a live scoop verb, a timed star swing, analog LT feel, pad rumble. Those stay Path A/B.

Dolphin stays compare-only. Agents do not send keys into a live Super Sluggers session.

Shell: `tools/still-gate.sh` writes the request. If Unity is already playing, the next frames consume it. If not, Play the scene.
