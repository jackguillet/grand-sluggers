# Silhouette bible

Identity is a shape problem, not a palette problem. Lock this before the 25th person.

Style: stylized, slightly oversized, original IP. Not Mario. Not MLB The Show. Heads sit large, limbs read at gameplay distance, and each captain is a different *cut* of the same primitive rig.

A player can point at the screen and name the captain without the HUD.

## Camera (turnaround, not gameplay)

Locked identity stills. Gameplay cameras stay in `CameraRig`.

| Shot | Camera | Look | FOV |
| --- | --- | --- | --- |
| Front | `(0, 5.5, -14)` | chest `(0, 3.2, 0)` | 32 |
| Side | `(14, 5.5, 0)` | chest | 32 |
| Back | `(0, 5.5, 14)` | chest | 32 |

World units. Actor at origin, facing −Z for the front plate. Do not move FOV or distance per captain — the six types have to compare.

Gameplay: pitcher 3/4, batter over-shoulder, fly follow. Those cameras must still name the body.

## Six body types

Root scale in `Silhouette.Proportions` (Height × Width × Head × Arms × Torso). Role players copy the faction captain. Captains keep the extra bits (crown, snout, horns).

| Type | Who | Height | Width | Head | Arms | Torso | Read |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Harbor kid | Rio, Spark | 0.88 | 0.98 | 1.45 | 1.05 | 0.92 | Short, round head, chunky shoes, big brim |
| Pageant pitcher | Vale, Royal | 1.22 | 0.72 | 1.22 | 0.92 | 0.78 | Tall, slim, long neck, sash, ice crown — head still reads |
| Speed | Zig, Carnival | 0.64 | 1.26 | 1.55 | 0.92 | 0.70 | Tiny body, wide head, stubby legs, goggles |
| Brick | Brondo, Goldrush | 1.00 | 1.55 | 1.12 | 1.22 | 1.42 | Cube torso, thick neck, square jaw |
| Ape | Konga, Canopy | 1.32 | 1.40 | 1.35 | 1.55 | 1.18 | Hunched, snout, long arms, barrel belly |
| Villain slug | Ashlord, Ember | 1.26 | 1.16 | 1.30 | 1.14 | 1.30 | Horns, cape stub, furnace eyes, heavy boots |

Head/Height ≥ 1.0 on every type so the face reads at catcher-eye. Cuts stay different. Numbers live in `Silhouette.cs`.

Numbers live in `src/GrandSluggers.Sim/Silhouette.cs` and `HeroActor.Build`. Do not invent a seventh anatomy.

## Role players

Faction variants, not 18 new skeletons. Same proportions as the captain. Jersey, stripe, and skin from `Colors`. No horns, crown, snout, goggles, or cape on role players.

## Signature extras (captains only)

- **Rio** — oversized brim, round cheeks, fat sneakers
- **Vale** — neck cylinder, pageant sash, ice crown instead of a ballcap
- **Zig** — goggle discs, almost-no-neck
- **Brondo** — cube chest, brick jaw
- **Konga** — ape snout, hanging arms, belly
- **Ashlord** — horns, short cape, unlit ember eyes

## Bats (shape, not a string)

Loadout mesh follows `BatItem.Visual`. Defaults:

| Captain | Item | Visual |
| --- | --- | --- |
| Rio | Harbor Lumber | `bat-spark` |
| Vale | Pageant Wand | `bat-wand` |
| Zig | Prism Stick | `bat-short` |
| Brondo | Gold Brick | `bat-brick` |
| Konga | Barrel Bat | `bat-barrel` |
| Ashlord | Furnace Club | `bat-furnace` |

`Match.CycleBat` swaps the sim item **and** the mesh. Charge Bat (`bat-gold`) is the shop stick.

Gloves sit on the fielding hand (non-throwing) whenever the body is on defense — not only during Catch.

## Animation

Procedural on this rig until a clip file fills the slot. No 2D sprites on the 3D diamond. Shared clip list (idle, walk, run, jump, swing, pitch, scoop, slide, throw) lives in `data/art/clips.json` and `MoveBones.ClipList`. Captains are skins (`data/art/skins.json`), not new skeletons. See `docs/art-rails.md`.
