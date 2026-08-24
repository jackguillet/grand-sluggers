# Art rails

How Grand Sluggers takes a lot of art later without rewriting baseball. This document is the **concept**. The living spec is `data/art/` plus `dotnet run --project src/GrandSluggers.Cli -- art`.

No paintings live here. Slots do.

## What first-party sports games actually scale

Nintendo-quality roster games do not start by unique-sculpting every body.

- **Mario Super Sluggers** kept a humanoid test actor and unused leg bones on Boo / Lakitu / Koopa. One skeleton still drove the cast; meshes opted into bones. Palette and accessory swaps were the volume play, not new armatures. (Mod tools still cannot drop a foreign skeleton in — the contract is the shared chain.)
- **Nintendo Switch Sports** (Ask the Developer Vol. 5): Wii Sports Miis had ~30 motions. Sportsmates, with arms attached to a torso, needed **650+** motions on **one** body. Volume went into the clip list, not into 650 rigs.
- **Zelda BotW / TotK** (CEDEC 2024): implement in data, not in a spec that rots. Specs explain concepts. Tables, tools, and play feel are the living spec. Character control was split so animation is a late stage, not the rules.
- **Donkey Kong Bananza** (GDC 2026): Maya → named pipeline → runtime. Artists feed existing models into a slot. Recycling is a pipeline feature.
- **Destruction AllStars**: a core skeleton every clip is authored on; shared solvers; character-specific bits as an overlay that is not baked into the clip.
- **Riot LoR**: content ids in data the whole stack can read; Unity prefabs are the presentation fill for those ids.

The pattern: **one rig, named clips, skins, named VFX/audio events, a validator.** Art files fill slots. Code does not grow a switch.

## What Grand Sluggers already has

Feel infrastructure (#107): named camera shots, `MoveBones` clip list, Harbor as placed objects, feel tables, debug overlay. Sim vs Unity wall.

What was still a prototype skin: portraits in `Resources/Art`, bodies as capsules, VFX/audio spawned in C#, parks as `ParkView` programs.

## Rails (this epic)

| Slot | Data | Unity drop path | Until a file exists |
| --- | --- | --- | --- |
| Shared rig | `data/art/rig.json` | `Assets/Art/Characters/SharedRig/hero-shared.fbx` | `SharedRig` primitives |
| Clips | `data/art/clips.json` | `Assets/Art/Animation/Clips/{id}` | `MoveBones.Evaluate` (swing.fbx, scoop.fbx dropped) |
| Skins | `data/art/skins.json` | `Assets/Art/Characters/SharedRig/extras.fbx` | primitive extras on the shared chain |
| Portraits | skin `portrait` | `Assets/Art/UI/Portraits/{id}` | `Resources/Art/{id}-hero` |
| VFX | `data/art/vfx.json` | `Assets/Art/VFX/{id}` | `SpecialFx` primitives |
| Audio | `data/art/audio.json` + `data/art/audio-clips/{id}.wav` | `Assets/Art/Audio/{id}` | generated tones in `AudioBus` |
| Materials | `data/art/materials.json` | `Assets/Art/Materials/{id}` | `Look.Toon` / `ToonFill` |
| Park kits | `data/art/parks.json` | `Assets/Art/Parks/{id}` | `harbor-kit.fbx` bind; else `HarborKit` primitive dress; `ParkView` elsewhere |

Role players inherit the faction body type and **must not** grow captain extras (crown, horns, snout). That is how 18 bodies stay cheap.

## Drop rules (when art is ready)

1. **Do not add a second skeleton.** Retarget to `hero-shared`. Unused bones are fine (SMS Boo legs).
2. **One clip file per catalog id.** Name the file the clip id (`swing.fbx` / `swing.anim`). Events on the clip: `Contact`, `Release`, `FootPlant` — the same names Sim already understands.
3. **Captains are skins.** Palette, extras, portrait, scale. `Silhouette.Proportions` stays the identity.
4. **Parks are kits**, not new `ParkView` methods. Harbor is the template (`placed: true`). Other parks wait until Exhibition is the reason people stay (#37).
5. **Original tones / original pictures.** No Nintendo samples, no Mario meshes.
6. **Missing files are placeholders, not crashes.** The binder keeps MoveBones / generated audio / code VFX until the slot is filled.
7. After a drop: `dotnet test` and `dotnet run --project src/GrandSluggers.Cli -- art` must still print `OK`.

## Import (Unity)

- Clips: Generic rig (not a new Humanoid avatar per captain). Loop only what the catalog marks `loop`.
- Portraits: sRGB, no mip maps, square.
- Park textures: sRGB, mips on.
- FBX: bake animations, one take per file, root at origin, facing −Z to match the silhouette bible.

## Validator

`ArtCatalog.Validate(ContentCatalog)` is the unit under test. The CLI prints it. The Editor menu **Grand Sluggers → Validate Art Rails** creates missing Unity folders and reports the same errors.

Do not mock the catalog in tests. Drive the shipped JSON.
