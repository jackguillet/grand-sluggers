# Screenshot gate — Harbor Exhibition stills

The rails are closed. These stills are the product. If you would not show a picture to a friend, epic #188 stays open.

**You do not have to pitch the top to get an out.** Play menu **Grand Sluggers → Capture Still Gate** (or an agent running `tools/still-gate.sh`) jumps to batting, scoop, and star and writes PNGs. The pad path below is optional, for taste.

Gamepad is the couch product. Keyboard and mouse are the same scheme (F1/F2/F3 stay debug).

---

## What you are judging

Three stills. HUD off means **F2 off** and the scorebug not covering the subject. F3 mutes the play HUD (debug). Star spectacle already mutes it.

| # | Still | Must show | Fail if |
|---|---|---|---|
| 1 | **Batting SET** (`plate`) | **Behind home** looking at the pitcher. Full batter in the box (feet to hat) **left of the look**. Pentagon + two chalk rectangles with dirt between them. **Pink/gold charge ring around the box** (hold South / `charge01: 1`). Pitcher **in the look**, in a windup. You can **name the captain**. Catcher is behind the camera. | First-base foul-line crop, hat/brim as the picture, catcher-spine, boxes kissing the plate, pitcher a corner speck, cage grid in the dirt, camera through the backstop, pitcher idle with both arms hanging, gold pancake under the feet, no ring |
| 2 | **Scoop** | Glove on the dirt, ball in the glove, runner leaving the box, grass. Camera is a 3/4 in the park (`diamond-grounder` / hopper), not high-home. | Cubes chasing a marble, auto-glove with no scoop verb, broadcast high-home |
| 3 | **Star swing** | Body owns ~2 seconds (Heatball core+embers on Rio, etc.). Scorebug gone. Then baseball. Side 3/4 on the torso. | Full-screen white/black blind, a smear with no body, HUD still talking, cage owns the frame, fire pancake on the dirt, loose sky blobs |

Bonus stills that save a later sitting (same rules):

| Still | Shot name (F2) | Must show |
|---|---|---|
| Pitching SET | `mound` | Close 3/4 over the pitcher’s back. Pitcher large on the right. Rubber in the bottom. Batter + catcher + boxes at home in the look. Fail if the pitcher is a distant speck, home is a speck, CF, or dirt/brim is the picture. |
| Pitch at you | `pitch` | From the box, looking at the pitcher. Arm through, ball leaving that hand toward you. |
| Title | `title` | Looks **into** Harbor, not at a menu wall. Home captain is the toy in front (not six idles, not a corner crop). **GRAND SLUGGERS** is a sticker over the infield, readable in the live player without F2. The board is not through the toy. |
| Lineup | `lineup` | Team Setup: home bar on top, away bar on the bottom, head grid in the center. Hearts / scribbles vs the captain. No AVAILABLE list, no white rays. |
| Captain card | `select` | Home captain face/body. HUD card (P/B/F/R). Dirt is the floor, not the picture. No second world-space name sign. |
| Throw | `diamond` | 45° on the dirt under the ball. CF at the top, home under second. No behind-the-thrower cut. |
| Fly | `diamond-fly` | Same 45°, pulled back, more FOV. CF at the top. Fielder reads, ball is a baseball |
| Homer | `diamond-fly` | Same pulled-back 45°. The ball flies toward CF at the top of the frame |

## Character stills (look gate)

Any change under `Art/Characters/`, `Art/Animation/Clips/`, or `tools/blender/` needs these two stills **before** a player rebuild. HUD off. Named captain. Agents file the PNGs and **stop**. Humans pass or fail.

Capture: `tools/still-gate-character.sh rio` (or menu **Grand Sluggers → Capture Character Stills**). PNGs land in `unity/Temp/gs-stills/`. Copy into `scratchpad/stills/` for the PR. Do not rebuild the Mac player as proof.

| Still | Must show | Fail if |
| --- | --- | --- |
| **Rest** | Idle take at 0. Painted by faction. Feet on dirt. Silhouette reads; the captain's extras are on the right bones. | Import-white material, a placeholder capsule, an extra floating off its socket |
| **Pose** | Swing take at contact. Both hands on the handle, bat through the plate line, hips to the plate. | Bat behind the head, one hand off the handle, a bind-pose statue |

### Swing takes (#613) — load, contact, finish

The slap and the charge are two takes on the one rig. Judge them against the reference footage: a slap is a quick swing with no windup; a charge shows a windup during the hold and swings a bigger arc; both end on a held finish (weight forward, bat around) that stays up through the STRIKE stamp. Agents file the PNGs and stop.

Capture with **Grand Sluggers → Capture Request File** and `{"shots":["swing-matrix"],"swingCaptains":["rio","brondo"]}` (Rio is the kid cut, Brondo the brick). Normal is the slap, MAX is the charge; the matrix writes `ready`, `load`, `contact`, `follow` and `finish` for each.

| Still | Must show | Fail if |
| --- | --- | --- |
| **Load** (`swing-{id}-normal-ready`, `swing-{id}-max-load`) | Slap: hands by the back shoulder, bat standing up beside the head, knees bent. Charge at MAX: hands higher, the bat taller, the lead knee up — a windup you can see from the plate camera. | Bat in front of the face, **the bat through the head (#623)**, a standing statue, the charge load identical to the slap |
| **Contact** (`swing-{id}-normal-contact`, `swing-{id}-max-contact`) | Both hands on the handle, barrel through the plate, hips turned toward the pitcher, back knee driving. | Bat behind the head, a hand off the handle, a stiff upright body |
| **Finish** (`swing-{id}-normal-finish`, `swing-{id}-max-finish`) | Weight on the front foot, the bat around over the lead shoulder; the charge finish wraps further than the slap. | Snapping back to ready, the bat hidden inside the body, both finishes the same |

Name files:

```
char-{id}-rest.png
char-{id}-pose.png
swing-{id}-{normal|max}-{ready|load|contact|follow|finish}.png
```

Contract: `docs/character-motion.md`.

Dolphin Super Sluggers is **compare only**. Do not dump Nintendo assets. Do not mash A into a live session unless you are okay with skipping a prompt.

---

## Setup (5 minutes)

1. Unity **6000.5.9f1**. Project folder `grand-sluggers/unity`. Scene `Assets/Scenes/HarborDiamond.unity`.
2. Click the **Game** tab. Not Scene. The editor Scene/Game view looking **through the backstop cage** is not Exhibition.
3. Hover the Game panel, **Shift+Space** to maximize it. Scale **1x** if the Game view scale slider is below 1.
4. Plug in a pad. Xbox A / Nintendo B is **South**. Hold South to charge; release to pitch or swing.
5. Day. Harbor. Home **Rio** (short, round) vs away **Ashlord** or **Konga** (so the height ladder is obvious). Stick L/R your team, U/D the other. North HOME/AWAY. Default is HOME.
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
char-fenn-rest.png
char-fenn-pose.png
```

Drop them in this chat or `scratchpad/stills/`.

---

## Path A — Exhibition (the product)

Title → South (pick captain) → South (lineup) → South (first pitch). Do not tap Start (that cycles Exhibition / Challenge / Training). Do not tap West (Training). Leave park on Harbor (`C` cycles parks; skip it).

### Pitching SET (`mound`) — top of 1

You pitch the top. Camera is the **mound 3/4** — over the pitcher’s shoulder, pitcher large on the right, rubber in the bottom, looking at the box. You can name pitcher and batter without the HUD.

- Hold **South** to charge. Ring / pull-back should read. Release to pitch; stick then curves it.
- F2 once: the overlay must say `SHOT MOUND`. F2 again (off). F3 if you want HUD off.
- HUD: score top-right, batter card bottom-left, pitcher card bottom-right. Same corners after a second pad.
- Capture.

Take three outs however you like (South to pitch, meatballs are fine). Bottom of 1 is batting.

### Batting SET (`plate`) — bottom of 1 — **still 1**

Camera sits **behind home** looking at the mound for the whole pitch. Batter is a full body on the left. Catcher is behind the lens. Pentagon and both boxes read with dirt between them.

- You in the box, feet to hat — not a brim close-up. Pitcher in the look.
- Hold **South**. Charge ring on the dirt around the box.
- F2: `SHOT PLATE`. Off. F3: HUD off.
- Capture **before** you swing.
- Fail the still if you are looking down the foul line, at a hat, through the cage, or at the catcher’s spine.

### Star swing — **still 3**

You need a star (chemistry from the lineup). **North / Y / Q** arms the star (gold tell). Hold South, then release to swing.

Capture at the peak (~1 second in) while the scorebug is gone. Then one still a second later that is baseball again (scorebug can return).

Rio = Heatball / heat-swing. Vale = charm. Zig = prism/shell. Brondo = phony. Konga = cask. Ashlord = skull / furnace.

### Scoop — **still 2**

Exhibition hoppers are RNG. Faster harness is Training drill 4 (Path B). If you stay in Exhibition: a grounder, South to catch in the window, capture glove-on-dirt.

---

## Path B — Training scoop (faster for still 2)

Title **West** (or Start until TRAINING, then South). Harbor drills, Rio vs Ashlord.

| Drill | What | Controller | Keyboard |
|---|---|---|---|
| 1 Paint the zone | Four pitch types in the zone + a star | Stick aim, RB cycle type, hold/release South, North star | WASD, Tab, hold/release Space, Q |
| 2 Time it and charge | Contact with charge | Hold/release South on the pitch | Hold/release Space |
| 3 Catch it, throw a bag | Catch + throw | South catch, D-pad / stick flick bag | Space, 1/2/3/4 |
| 4 **Grab a grounder** | **This is still 2** | Stick to the hop, South scoop, 1 to first | WASD, Space, 1 |

On drill 4: scoop still = glove in the dirt, ball in the glove, runner leaving. Then throw still if you get it.

---

## Pad ↔ keyboard

| Verb | Controller | Keyboard |
|---|---|---|
| South | Xbox A / Nintendo B | Space / Return |
| East | Xbox B / Nintendo A | G |
| West | Xbox X / Nintendo Y | F tap · V hold |
| North (star) | Xbox Y / Nintendo X | Q |
| Pitch / swing charge | South hold/release | Space hold/release |
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

**What it still is not:** hold/release timing feel, pad rumble, or a 3-inning Exhibition you played by hand. Those stay optional Path A/B.

Dolphin stays compare-only. Agents do not send keys into a live Super Sluggers session.

Shell: `tools/still-gate.sh` writes the request and clicks **Grand Sluggers → Capture Still Gate** (not Cmd+P). PNGs: `unity/Temp/gs-stills/`.

For a request that must survive Unity startup, keep the JSON outside
`unity/Temp`, set `GS_STILL_REQUEST_FILE` to its absolute path before launching
the editor, then choose **Grand Sluggers → Capture Request File**. The menu reads
and validates the external JSON with `StillRequest.Parse`, stages it in
`unity/Temp`, and enters the same capture path. Any allowed shot and home/away
pair works, for example `{"shots":["plate"],"home":"fenn","away":"rio"}`.
For the four-beat bat/socket gate on any captain, request
`{"shots":["swing-matrix"],"swingCaptains":["fenn"]}`. The default matrix is
the six original captains; every captain is measured with the same shared-rig
hand, stance, and plate thresholds.

Each matrix row captures `ready`, `load`, `contact`, and `follow` before it
measures the pose, so a failure still leaves a PNG. The JSON records posed hand
mesh centers/extents, physical grip/handle/barrel endpoints, imported bat mesh
bounds, and the authored-direction dot product. Shared-rig rows pass only when
both rendered hands meet the physical handle in shared-root space, MAX load and
the two swing keys preserve their authored direction, the loaded barrel rises, the physical bat stays outside
the drawn head on every beat (#623), and the physical barrel segment crosses the plate volume. Normal and MAX each
run for every selected captain in one editor launch.
