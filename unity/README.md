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

Press **Play** on `Assets/Scenes/HarborDiamond.unity`. Harbor is the presentation park: afternoon light (warm key, cool fill, gold rim), warning track, dugouts, backstop, stepped bleachers with crowd in the seats, town beyond the fence. Crystal Rink (Vale home) is its own ice garden — glass boards, freeze statues, palace beyond CF, cool light — not Harbor recolored. Funfair Park (Zig home) is a carnival lot: tagged warp cans with mouths, tents, striped poles, ferris wheel, booths beyond CF, warning-track boxcar, warm carnival light — not Harbor town, not Crystal palace. Rooftop City (Brondo home) is a dusk roof: tar deck, star billboards, AC boxes, neon-capable light, city beyond CF. Canopy Yard (Konga home) is jungle: vine climb walls with ledges, barrel cannons with mouths, trees. Ember Keep (Ashlord home) is a fire courtyard: castle, lava pits with rims, fire-breath statues, night-ready even in day. Pitching-camera still should hold up. Pose heroes. Every captain special owns the ball or the field for two seconds (Heatball fire + embers, Charmball hearts, Prismball ghosts, Phonyball decoy, Caskball barrel, Skullball skull, Furnace burn + crack, Heart charm, Shell/Cask fragments, Phony hop) then baseball resumes. Mute the HUD and you can still name the clip. Field verbs show on the body: nearest glove lights, dive and jump open a catch window, throws leave chemistry-colored trails (good gold/purple and fast, bad muddy and off-line). Defense still plays as a scene when you bat. On-deck buddy: after contact, throw a banana (grass), rocket (body), or POW (infield hop) at a fielder you can see.

Gamepad is the couch product. Keyboard is the same scheme. Couch map (title → lineup → pitch / swing / field, Training, F2 debug): **[docs/how-to-play.md](../docs/how-to-play.md)**. Keep that file in the same PR as control or camera-flow changes.

If the scene is missing, menu **Grand Sluggers → Bootstrap Scene**.

The sim lives in `src/GrandSluggers.Sim` (local package `com.grandsluggers.sim`). Do not commit `Library/`, `Temp/`, or `Logs/`.
