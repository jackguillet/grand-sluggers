# Screenshot gate — Harbor Exhibition stills

The rails are closed. These stills are the product. If you would not show a picture to a friend, epic #188 stays open.

**You do not have to pitch the top to get an out.** Play menu **Grand Sluggers → Capture Still Gate** (or an agent running `tools/still-gate.sh`) jumps to batting, scoop, and star and writes PNGs. The pad path below is optional, for taste.

Gamepad is the couch product. Keyboard and mouse are the same scheme (F1/F2/F3 stay debug).

---

## What you are judging

Three stills. HUD off means **F2 off** and the scorebug not covering the subject. F3 mutes the play HUD (debug). Star spectacle already mutes it.

| # | Still | Must show | Fail if |
|---|---|---|---|
| 1 | **Batting SET** (`plate`) | Full batter in the box (feet to hat). Pentagon + two chalk rectangles on packed dirt. **Pink/gold charge ring around the box** (LT / `charge01: 1`). Pitcher in the distance **in a windup**, not a T-pose. You can **name the captain**. Catcher is not the subject. | Catcher-spine, cage grid in the dirt, cap close-up, “brown cube with a brim,” camera through the backstop, pitcher idle with both arms hanging, gold pancake under the feet, no ring |
| 2 | **Scoop** | Glove on the dirt, ball in the glove, runner leaving the box, grass. Camera is a 3/4 in the park (`diamond-grounder` / hopper), not high-home. | Cubes chasing a marble, auto-glove with no scoop verb, broadcast high-home |
| 3 | **Star swing** | Body owns ~2 seconds (Heatball core+embers on Rio, etc.). Scorebug gone. Then baseball. Side 3/4 on the torso. | Full-screen white/black blind, a smear with no body, HUD still talking, cage owns the frame, fire pancake on the dirt, loose sky blobs |

Bonus stills that save a later sitting (same rules):

| Still | Shot name (F2) | Must show |
|---|---|---|
| Pitching SET | `mound` | 3/4 over the pitcher, rubber in the bottom, batter + catcher + boxes at home. Fail if home is a speck, CF, or dirt/brim is the picture. |
| Pitch at you | `pitch` | From the box, looking at the pitcher. Arm through, ball leaving that hand toward you. |
| Title | `title` | Looks **into** Harbor, not at a menu wall. Home captain is the toy in front (not six idles, not a corner crop). **GRAND SLUGGERS** is a sticker over the infield, readable in the live player without F2. The board is not through the toy. |
| Lineup | `lineup` | Team Setup: home bar on top, away bar on the bottom, head grid in the center. Hearts / scribbles vs the captain. No AVAILABLE list, no white rays. |
| Captain card | `select` | Home captain face/body. HUD card (P/B/F/R). Dirt is the floor, not the picture. No second world-space name sign. |
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

You pitch the top. Camera is the **mound 3/4** — over the pitcher, rubber in the bottom, looking at the box. You can name pitcher and batter without the HUD.

- Hold **LT** to charge. Ring / pull-back should read. Stick aims the zone locator.
- F2 once: the overlay must say `SHOT MOUND`. F2 again (off). F3 if you want HUD off.
- HUD: score top-right, batter card bottom-left, pitcher card bottom-right. Same corners after a second pad.
- Capture.

Take three outs however you like (South to pitch, meatballs are fine). Bottom of 1 is batting.

### Batting SET (`plate`) — bottom of 1 — **still 1**

Camera stays beside the batter looking at the mound for the whole pitch. Catcher stays behind the lens.

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

## Pad ↔ keyboard

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
- `plate: pitcher T-pose — they are not throwing`
- `plate: Rio does not read — cube hat, no face`
- `plate: boxes read, captain reads — PASS`
- `scoop: no glove, ball is a marble`
- `star: HUD still up, no body in the fire`
- `star: cage / dirt pancake / sky blobs — body does not own it`

A pass is “I would put this on a trailer.” A fail files a GitHub child from the **picture**, not from a vibe. Do not reopen #189–#198 as “do the rail again.”

---

## Agent path (no pad)

Personal Unity cannot `-batchmode`. The agent does **not** invent a screenshot. It drives the **already-open editor**.

1. Write `unity/Temp/gs-still-request.json` (schema: `StillRequest` in Sim).
2. Enter Play on `HarborDiamond.unity` (menu **Grand Sluggers → Capture Still Gate**). Do not Cmd+P — Grok/Safari steal it.
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

Default request now includes the three trailer stills plus title/mound:

```json
{
  "shots": ["title", "select", "lineup", "plate", "pitch", "mound", "diamond-grounder", "smash"],
  "home": "rio",
  "away": "ashlord",
  "hudOff": true,
  "charge01": 1
}
```

Play **skips the top**. You do not have to get three outs. `Match.SkipToHomeHalf` puts Rio at the plate; scoop and smash are staged on the real cameras and bodies (`scoop` is an alias for `diamond-grounder`).

**What this can judge without a pad:** SET cameras, diamond kit, charge ring, toy body at gameplay distance, Harbor postcard, scoop pose with ball in the glove, pitcher throwing at you, star-swing camera with HUD muted.

**What it still is not:** analog LT feel, pad rumble, or a 3-inning Exhibition you played by hand. Those stay optional Path A/B.

Dolphin stays compare-only. Agents do not send keys into a live Super Sluggers session.

Shell: `tools/still-gate.sh` writes the request and clicks **Grand Sluggers → Capture Still Gate** (not Cmd+P). PNGs: `unity/Temp/gs-stills/`.
