# Look

Grand Sluggers should **feel and look like Mario Super Sluggers** — party baseball, oversized toys, readable at ten feet — with a **different cast**. We steal weight, cameras, juice, and the “real diamond plus gimmick parks” shape. We do not steal faces, names, mushrooms, or Nintendo set dressing.

Identity stills and proportions: `docs/silhouette-bible.md`. Character contract: `docs/character-package.md`. Slots and drop rules: `docs/art-rails.md`. Systems teardown: `docs/research-sluggers.md`. Style lock (Rio, three views): `tools/blender/style-lock/`.

## What “like Sluggers” means here

| Steal | Do not steal |
| --- | --- |
| Fat silhouettes, big heads, simple shapes | Mario, Peach, DK, Bowser, or lookalikes |
| Saturated toon fill, hard color blocks | PBR sports-game sheen, MLB broadcast |
| Identity from palette and size (`Silhouette.Proportions`) | Mixamo as identity; unique sculpts; extras that read as geometry junk |
| One expensive “real” diamond (Harbor) | Shipping six park kits before Exhibition is fun |
| Timing + charge, star cutscenes, chemistry comedy | Motion controls, Nintendo UI chrome |
| 10-foot couch read: you can point at a body and name them HUD-off | Fine print, nostril cameras, brim-as-the-picture |

A player who loved Sluggers should feel at home in three pitches and never think they launched a Mario ROM.

## The cast (this is the art)

Six faction cuts plus Elder Fenn. Shared bone names; unique captains are deferred. Identity is **palette + `Silhouette.Proportions` only** (#687). Extras catalog slots stay; skins list none until extras read as toys, not geometry junk (Brondo/Konga foot discs, Ashlord's cape as an orange plate). Role players are the same cut, same empty extras.

| Captain | Cut | Read |
| --- | --- | --- |
| **Rio** | Harbor kid | Short, round — spark palette, the poster toy |
| **Vale** | Pageant pitcher | Tall, slim, long neck — royal palette |
| **Zig** | Speed | Tiny body, huge head, stubby legs — carnival palette |
| **Brondo** | Brick | Rio-height, cube torso, thick neck — goldrush palette |
| **Konga** | Ape | Hunched, longest arms — canopy palette |
| **Ashlord** | Villain slug | Tallest — ember palette |
| **Elder Fenn** | Turtle elder | Short, wide, big head — fen palette |

Do not unique-sculpt a role player. Unique captains are packages (same bone names, own mesh) and stay deferred. Numbers live in each captain's `proportions` in `data/characters`. Spec: `docs/character-package.md`. Do not bring caps back as geometry (#557).

## Cameras look at toys

Gameplay shots (`data/feel/shots.json`, `CarnivalFront` looks) aim at **chest, dirt, or the bag**. A tall extra (Ashlord brim, Vale crown, Zig goggles) that fills the lens is a **framing bug**. Tune look Y / distance / FOV. Do not shrink the toy to save one camera.

Select: the pick **steps forward**. Highlight, not cheer — cheer bobs a foot through the dirt (#686). Grow is a field verb, not a menu scale. A glove with the ball is rest scale; YOU is the ring, not a size-up. They stand on the dirt. Camera sits at **chest height** and looks at the face/body — the plate dirt is the floor, not the picture. A world-space name placard is not the card; HUD is.

Plate / scoop / star HUD-off stills: `docs/screenshot-gate.md`. If you would not show the still to a friend, the look epic is open.

## Juice

Readable in a half-second: charge ring, dirt puff, heat trail, buddy flash, smash freeze. Events are catalog ids (`data/art/vfx.json`, `data/art/audio.json`). Authored clips beat generated tones. Missing files placeholder — they must not crash.

## Pipeline

Shared sockets (`hero-shared` names). Unique captains are DCC Generic packages (`docs/character-package.md`). Named clips. Harbor kit for the diamond. Artists fill slots; code does not grow a switch. After a drop: `dotnet test` and `cli art` still print `OK`, **and** character stills exist.
