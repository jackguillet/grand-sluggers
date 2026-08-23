# Unity 6 client

Pinned editor: **6000.5.9f1** (URP 17.2). Hub: `/Applications/Unity Hub.app`.
Also installed: 6000.0.82f1. This project uses 6.5 because the sim is C# 12 (collection expressions, records).

Personal license is already on this machine (`unity auth status` → Jack Guillet).

```bash
export PATH="$HOME/.unity/bin:$PATH"
cd /path/to/grand-sluggers
unity open ./unity --editor-version 6000.5.9f1
```

Press **Play** on `Assets/Scenes/HarborDiamond.unity`.

- SPACE — start / pitch / swing / catch
- C (title) — Harbor Diamond / Crystal Rink
- Shift — charge
- WASD — field
- Q — star skill
- F — buddy jump
- 1 2 3 H — throw
- R — new pitcher

If the scene is missing, menu **Grand Sluggers → Bootstrap Scene**.

The sim lives in `src/GrandSluggers.Sim` (local package `com.grandsluggers.sim`). Do not commit `Library/`, `Temp/`, or `Logs/`.
