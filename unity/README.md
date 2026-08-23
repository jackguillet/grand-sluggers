# Unity 6 client — how you play

This folder **is** Grand Sluggers. Open it in the editor and press Play. Raylib (`src/GrandSluggers.Play`) is a rules sandbox, not the product.

Pinned editor: **6000.5.9f1** (URP 17.2). Hub: `/Applications/Unity Hub.app`.
Also installed: 6000.0.82f1. This project uses 6.5 because the sim is C# 12 (collection expressions, records).

Personal license is already on this machine (`unity auth status` → Jack Guillet).

```bash
export PATH="$HOME/.unity/bin:$PATH"
cd /path/to/grand-sluggers
unity open ./unity --editor-version 6000.5.9f1
```

Press **Play** on `Assets/Scenes/HarborDiamond.unity`. Harbor is the presentation park: afternoon light (warm key, cool fill, gold rim), warning track, dugouts, backstop, stepped bleachers with crowd in the seats, town beyond the fence. Crystal Rink (Vale home) is its own ice garden — glass boards, freeze statues, palace beyond CF, cool light — not Harbor recolored. Funfair Park (Zig home) is a carnival lot: tagged warp cans with mouths, tents, striped poles, ferris wheel, booths beyond CF, warning-track boxcar, warm carnival light — not Harbor town, not Crystal palace. Pitching-camera still should hold up. Pose heroes. Every captain special owns the ball or the field for two seconds (Heatball fire + embers, Charmball hearts, Prismball ghosts, Phonyball decoy, Caskball barrel, Skullball skull, Furnace burn + crack, Heart charm, Shell/Cask fragments, Phony hop) then baseball resumes. Mute the HUD and you can still name the clip. Field verbs show on the body: nearest glove lights, dive and jump open a catch window, throws leave chemistry-colored trails (good gold/purple and fast, bad muddy and off-line). Defense still plays as a scene when you bat. On-deck buddy: after contact, throw a banana (grass), rocket (body), or POW (infield hop) at a fielder you can see.

Gamepad is the product. Keyboard is a debug overlay.

| Verb | Gamepad | Keyboard debug |
| --- | --- | --- |
| Aim / move / lead | Left stick | WASD |
| Charge | LT hold | Shift |
| Pitch / swing / catch | South | Space |
| Cycle pitch | RB | Tab |
| Star | North | Q |
| Dive | East | G |
| Jump / buddy / bunt (hold) | West | F / V hold |
| Throw to bag | D-pad / stick flick | 1 2 3 4 |
| Steal | LB | X |
| Throw chemistry item | LT+RB / South+LT | E |
| Cycle item (during the fly) | RB | Tab |
| Exhibition / Challenge / Training | Start | H |
| Training (from title) | West | F |
| Timing bar | — | F1 |

If the scene is missing, menu **Grand Sluggers → Bootstrap Scene**.

The sim lives in `src/GrandSluggers.Sim` (local package `com.grandsluggers.sim`). Do not commit `Library/`, `Temp/`, or `Logs/`.
