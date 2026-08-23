# Unity 6 URP (shell)

Pinned editor: **Unity 6000.0.82f1 LTS** (URP, arm64), installed at
`/Applications/Unity/Hub/Editor/6000.0.82f1/Unity.app`.

Apple Silicon still wants Rosetta 2 for the Editor helper (`softwareupdate --install-rosetta --agree-to-license`). Hub is in `/Applications/Unity Hub.app`.

The editor will not open a project until a Personal license is on the machine:

```bash
export PATH="$HOME/.unity/bin:$PATH"
unity auth login          # browser; Unity ID, Personal is free under $200k
unity open ./unity        # from the repo root
```

The **playable vertical slice is not this folder.** Run:

```bash
dotnet run --project src/GrandSluggers.Play
```

Unity was not available on the machine that cut Milestone 1, so Harbor Diamond ships as a Raylib 3D client driven by the same `GrandSluggers.Sim` match loop. This folder is the Unity shell:

1. Install Unity Hub + a 6000.0 LTS.
2. Open `unity/` (Unity will import URP and the local sim package `com.grandsluggers.sim`).
3. New scene → empty GameObject → add `MatchBootstrap`.
4. Replace the Raylib `WorldView` with URP meshes, the Input System, and the existing `Match.Play(pitch, swing)` calls.

Do not check in `Library/`, `Temp/`, or `Logs/`.

## Pinned editor

- Version: 6000.0.58f2 (upgrade in-place to whatever 6000.0 LTS you have)
- Pipeline: URP
- Input: new Input System (package listed)
- Physics: sim owns the strike zone; Unity draws it
