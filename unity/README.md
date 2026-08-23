# Unity project (not in the repo yet)

The engine of record is **Unity 6 LTS + URP**. Do not create this project until Milestone 1 (vertical slice). The baseball rules already live in `src/GrandSluggers.Sim`.

When you start the slice:

1. Install Unity Hub and a Unity 6 LTS (record the exact version here).
2. New project: 3D (URP), name `GrandSluggers`, placed in this folder or as a sibling. If sibling, add a git submodule or a second repo — do not dump `Library/` into git.
3. Add `src/GrandSluggers.Sim` as a local assembly. Easiest path: copy or symlink the `.cs` files under `Assets/Sim/` with an asmdef that references nothing Unity-specific. Keep `ContentCatalog.FindDataRoot` able to see `../data` from the project.
4. Input System package. Gamepad only for the slice.
5. One scene: `HarborDiamond`. Camera behind the pitcher, then a batting camera. No Cinemachine brain dump until the at-bat reads.
6. Pin the editor version in `ProjectSettings/ProjectVersion.txt` and paste it below.

## Pinned editor

- Version: *not created yet*
- Pipeline: URP
- Input: new Input System
- Physics: we drive the ball from the sim, not from PhysX as the rules authority

## .gitignore when the project exists

Use Unity’s standard gitignore (`Library/`, `Temp/`, `Obj/`, `Build/`, `Logs/`, `.csproj` generated at root, etc.). Keep `Assets/`, `Packages/manifest.json`, `ProjectSettings/`.
