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

Press **Play** on `Assets/Scenes/HarborDiamond.unity` for the editor; the player plays the standalone window ([docs/local-player.md](../docs/local-player.md)). Harbor is the presentation park: afternoon light (warm key, cool fill, gold rim), warning track, dugouts, backstop, stepped bleachers with crowd in the seats, town beyond the fence. Every other park draws the greybox from its own data until its art is allowed ([docs/status.md](../docs/status.md)). Every captain special owns the ball or the field for two seconds, then baseball resumes; mute the HUD and you can still name the clip. Field verbs show on the body: nearest glove lights, dive and jump open a catch window, throws leave chemistry-colored trails. Defense still plays as a scene when you bat.

The game is gamepad only: pad 1 is player 1, pad 2 is player 2. There is no keyboard or mouse scheme. F1/F2/F3 are editor-only developer keys. Couch map (title → lineup → pitch / swing / field, Training): **[docs/how-to-play.md](../docs/how-to-play.md)**. Keep that file in the same PR as control or camera-flow changes.

If the scene is missing, menu **Grand Sluggers → Bootstrap Scene**.

Validation is split by what it proves. Portable tests need no Unity install; the narrow compiler check needs explicit package assemblies; a configured Unity runner imports the project and opens Harbor; standalone launch smoke and human play remain separate. See **[docs/validation.md](../docs/validation.md)**.

The sim lives in `src/GrandSluggers.Sim` (local package `com.grandsluggers.sim`). Do not commit `Library/`, `Temp/`, or `Logs/`.
