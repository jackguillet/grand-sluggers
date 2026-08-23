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

Press **Play** on `Assets/Scenes/HarborDiamond.unity`. Harbor is the presentation park: textured grass and dirt, stands, crowd cards, a camera that sits behind the pitcher or batter, posed heroes. Every captain special owns the ball or the field for two seconds (Heatball fire + embers, Charmball hearts, Prismball ghosts, Phonyball decoy, Caskball barrel, Skullball skull, Furnace burn + crack, Heart charm, Shell/Cask fragments, Phony hop) then baseball resumes. Mute the HUD and you can still name the clip. Field verbs show on the body.

- SPACE / gamepad South — start, pitch, swing, catch
- LT / Shift — charge (body pulls back)
- Left stick — pitch location (in/out, up/down) on the mound; spray / field otherwise
- Y / Q — arm star
- A/D (title) — captain · W/S opponent · C park
- F / East — buddy jump
- 1 2 3 H — throw
- R — new pitcher
- F1 — timing bar (off by default; the ball and bodies are the tell)

If the scene is missing, menu **Grand Sluggers → Bootstrap Scene**.

The sim lives in `src/GrandSluggers.Sim` (local package `com.grandsluggers.sim`). Do not commit `Library/`, `Temp/`, or `Logs/`.
