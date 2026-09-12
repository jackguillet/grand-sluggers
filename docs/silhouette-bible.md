# Silhouette bible

Identity is a shape problem, not a palette problem. Lock this before the 25th person.

Style: stylized, slightly oversized, original IP. Not Mario. Not MLB The Show. Heads sit large, limbs read at gameplay distance, and each captain is a different *cut* of the same primitive rig. Root scale is multiplied by `Silhouette.ToyScale` so the toys sit bigger than an honest diamond.

A player can point at the screen and name the captain without the HUD.

## Camera (turnaround, not gameplay)

Locked identity stills. Gameplay cameras stay in `CameraRig`.

| Shot | Camera | Look | FOV |
| --- | --- | --- | --- |
| Front | `(0, 5.5, -14)` | chest `(0, 3.2, 0)` | 32 |
| Side | `(14, 5.5, 0)` | chest | 32 |
| Back | `(0, 5.5, 14)` | chest | 32 |

World units. Actor at origin, facing +Z (Unity forward). Do not move FOV or distance per captain — the six types have to compare.

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
| Turtle elder | Fenn, Fen | 0.78 | 1.48 | 1.70 | 0.96 | 1.12 | Short, wide shell-brim, cane, cream plastron |

SMS research ladder (outlines only, not IP): Baby < Mario ≈ Wario < Peach < DK < Bowser. Head/Height ≥ 1.0 so the face still reads.

Head/Height ≥ 1.0 on every type so the face reads at catcher-eye. Cuts stay different. Numbers live in `Silhouette.cs`.

Numbers live in `src/GrandSluggers.Sim/Silhouette.cs`. Role players do not get a new anatomy. Unique captains are deferred: `docs/character-package.md`.

## Role players

Faction variants, not 18 new skeletons. Same proportions as the captain. Jersey, stripe, and skin from `Colors`. No horns, crown, snout, goggles, or cape on role players.

## Signature extras (captains only)

- **Rio** — round cheeks, fat sneakers (the brim returns when caps come back as accessories)
- **Vale** — neck cylinder, pageant sash, ice crown instead of a ballcap
- **Zig** — goggle discs, almost-no-neck
- **Brondo** — cube chest, brick jaw
- **Konga** — ape snout, hanging arms, belly
- **Ashlord** — horns, short cape, unlit ember eyes
- **Fenn** — shell-as-brim, cane slung across the back. Same rig, extras `shell` and `staff`.

## Bats

Every character holds the same original `bat-wood` model while batting. It stays
on the shared bat socket from ready and load through swing and follow-through;
the authored model origin stays fixed, its handle runs from Y −1.00 to −0.10,
the grip sits at Y −0.85, and its radius-0.12 barrel runs from Y −0.15 to +1.25
before the measured shared-socket bind conversion. A left-handed batter plays
the baked mirror take, in which the same `bat` socket is keyed on the other
hand. Fenn's cane is a back-slung extra, never the hitting prop.

Signature items still own gameplay stats and an inventory visual id. Defaults:

| Captain | Item | Visual |
| --- | --- | --- |
| Rio | Harbor Lumber | `bat-spark` |
| Vale | Pageant Wand | `bat-wand` |
| Zig | Prism Stick | `bat-short` |
| Brondo | Gold Brick | `bat-brick` |
| Konga | Barrel Bat | `bat-barrel` |
| Ashlord | Furnace Club | `bat-furnace` |
| Fenn | Fen Cane | `bat-staff` |

`Match.CycleBat` swaps the sim item and its contact, power, and charge effects.
It does not replace the active hitting prop. `GearMesh.HittingBatVisual()` owns
that single choice; `GearMesh.BatVisual(BatItem)` retains loadout identity for
inventory and later non-hitting presentation.

Gloves sit on the fielding hand (non-throwing) whenever the body is on defense — not only during Catch.

## Animation

Every verb is a Blender take on this rig; nothing is procedural. The clip list lives in `data/art/clips.json` and `Motion.Clips`. Handed takes are baked for both hands. Contract: `docs/character-motion.md`.
