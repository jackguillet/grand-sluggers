# Look

Grand Sluggers should **feel and look like Mario Super Sluggers** — party baseball, oversized toys, readable at ten feet — with a **different cast**. We steal weight, cameras, juice, and the “real diamond plus gimmick parks” shape. We do not steal faces, names, mushrooms, or Nintendo set dressing.

Identity stills and proportions: `docs/silhouette-bible.md`. Slots and drop rules: `docs/art-rails.md`. Systems teardown: `docs/research-sluggers.md`. Style lock (Rio, three views): `tools/blender/style-lock/`.

## What “like Sluggers” means here

| Steal | Do not steal |
| --- | --- |
| Fat silhouettes, big heads, simple shapes | Mario, Peach, DK, Bowser, or lookalikes |
| Saturated toon fill, hard color blocks | PBR sports-game sheen, MLB broadcast |
| Personality in extras (hat, crown, goggles, snout, horns, cape) | A unique skeleton per captain |
| One expensive “real” diamond (Harbor) | Shipping six park kits before Exhibition is fun |
| Timing + charge, star cutscenes, chemistry comedy | Motion controls, Nintendo UI chrome |
| 10-foot couch read: you can point at a body and name them HUD-off | Fine print, nostril cameras, brim-as-the-picture |

A player who loved Sluggers should feel at home in three pitches and never think they launched a Mario ROM.

## The cast (this is the art)

Six captains, six body types, one shared rig. Role players are palette + jersey on the faction cut — no crown, horns, snout, goggles, or cape.

| Captain | Cut | Read |
| --- | --- | --- |
| **Rio** | Harbor kid | Short, round, big brim, fat sneakers — the poster toy |
| **Vale** | Pageant pitcher | Tall, slim, long neck, ice crown, sash |
| **Zig** | Speed | Tiny body, huge head, goggles, stubby legs |
| **Brondo** | Brick | Cube torso, thick neck, square jaw |
| **Konga** | Ape | Hunch, snout, long arms, barrel belly |
| **Ashlord** | Villain slug | Tallest, horns, cape, furnace eyes, heavy boots |

Do not invent a seventh anatomy. Do not unique-sculpt a role player. Numbers live in `Silhouette.cs`.

## Cameras look at toys

Gameplay shots (`data/feel/shots.json`, `CarnivalFront` looks) aim at **chest, dirt, or the bag**. A tall extra (Ashlord brim, Vale crown, Zig goggles) that fills the lens is a **framing bug**. Tune look Y / distance / FOV. Do not shrink the toy to save one camera.

Select: the pick **steps forward**. Highlight + cheer. Grow is a field verb, not a menu scale. Camera sits at **chest height** and looks at the face/body — the plate dirt is the floor, not the picture. A world-space name placard is not the card; HUD is.

Plate / scoop / star HUD-off stills: `docs/screenshot-gate.md`. If you would not show the still to a friend, the look epic is open.

## Juice

Readable in a half-second: charge ring, dirt puff, heat trail, buddy flash, smash freeze. Events are catalog ids (`data/art/vfx.json`, `data/art/audio.json`). Authored clips beat generated tones. Missing files placeholder — they must not crash.

## Pipeline

One chain (`hero-shared`). Named clips. Skins and extras. Harbor kit for the diamond. Artists fill slots; code does not grow a switch. After a drop: `dotnet test` and `cli art` still print `OK`.
