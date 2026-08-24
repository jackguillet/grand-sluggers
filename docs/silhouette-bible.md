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
| Harbor kid | Rio, Spark | 0.90 | 1.00 | 1.38 | 1.02 | 0.94 | Short, round head, chunky shoes, big brim |
| Pageant pitcher | Vale, Royal | 1.24 | 0.70 | 1.24 | 0.88 | 0.74 | Tall, slim, long neck, sash, ice crown — head still reads |
| Speed | Zig, Carnival | 0.56 | 1.18 | 1.62 | 0.82 | 0.68 | Tiny body, huge head, stubby legs, goggles |
| Brick | Brondo, Goldrush | 0.96 | 1.58 | 1.16 | 1.28 | 1.48 | Rio-height, cube torso, thick neck, square jaw |
| Ape | Konga, Canopy | 1.30 | 1.36 | 1.34 | 1.72 | 1.20 | Hunched, snout, longest arms, barrel belly |
| Villain slug | Ashlord, Ember | 1.44 | 1.28 | 1.48 | 1.18 | 1.38 | Tallest, horns, cape, furnace eyes, heavy boots |

SMS research ladder (outlines only, not IP): Baby < Mario ≈ Wario < Peach < DK < Bowser. Head/Height ≥ 1.0 so the face still reads.

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
