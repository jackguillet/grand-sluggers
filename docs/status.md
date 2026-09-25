# Status — where the game is

The one page that says what is shipped and what is open. Other docs link here instead of keeping their own "where we are". When this page and another doc disagree, this page is right and the other doc is stale: fix it. The **order** of work is the stack in [AGENTS.md](../AGENTS.md); this page does not reorder it.

Reviewed 2026-09-25 at `e7af5433`.

## Shipped

- **Exhibition at Harbor, 1P and 1v1, gamepad only.** Title → lineup → a three-inning game, played in the standalone window ([local-player.md](local-player.md)). Every play is decided by geometry from `data/rules/` ([gameplay-spec.md](gameplay-spec.md)).
- **One diamond: 80-ft basepaths.** The compact profile (C80) is the shipped game; its numbers are the defaults in `data/`. The specials and special statuses play as authored, outside that contract.
- **Pitching and hitting:** five pitch families selected before the charge, one shared swing window, the stick shapes only a bunt and a Star Swing, a CPU pitcher and batter on human inputs ([plan](plan-pitching-hitting-implementation.md)).
- **Fields rails:** park schema, boundary and polyline fence, ground and wall libraries, park environment, hazards as live events with a hazards-off option, one field kit, look gates ([plan](plan-fields-implementation.md)). Harbor is the only park with art.
- **Tutorials:** `cli tutorials` is the live count (108 lessons implemented on 2026-09-23). They play from **Tutorials** on the title menu ([tutorials.md](tutorials.md)).
- **Authored sound:** bat crack, glove pop and crowd bed are authored clips in `data/art/audio.json` slots.

## Trials

**No trial is open.** The overlay machinery (`GRAND_SLUGGERS_TRIAL`, `DataRoot`) is kept dormant ([spec §16](spec/16-data-tables.md)); every former trial folder was promoted into `data/` and deleted. A doc that names a `trials/<name>` folder as live is stale.

## Open

In stack order ([AGENTS.md](../AGENTS.md) "The stack"):

1. **Harbor Exhibition is playable** — human gates [#346](https://github.com/jackguillet/grand-sluggers/issues/346) / [#209](https://github.com/jackguillet/grand-sluggers/issues/209): sit the 80-ft game from Call time How to play with one pad, then two; call D7 (pitch pace) in that sitting. The tutorial learning gate is [#774](https://github.com/jackguillet/grand-sluggers/issues/774).
2. **Sitting-found children** under [#209](https://github.com/jackguillet/grand-sluggers/issues/209), [#342](https://github.com/jackguillet/grand-sluggers/issues/342) (the book) and [#188](https://github.com/jackguillet/grand-sluggers/issues/188) (the toy).
3. **The toy reads HUD-off** — [#188](https://github.com/jackguillet/grand-sluggers/issues/188); the playset epic [#246](https://github.com/jackguillet/grand-sluggers/issues/246) follows the gameplay and learning gates.

Also open, not ahead of the stack:

- **Fields:** Jack's greybox sitting at Crystal, then its art ([plan-fields-implementation.md](plan-fields-implementation.md) §0).
- **Specials:** Spin Check [#1010](https://github.com/jackguillet/grand-sluggers/issues/1010) and the Phase 6 effect reviews [#1011](https://github.com/jackguillet/grand-sluggers/issues/1011).
- **Items are dormant:** the code stays, no item source offers one, and the item lessons wait.
- **Codebase review:** [#1019](https://github.com/jackguillet/grand-sluggers/issues/1019) and its children.

## Not started

Challenge (#36), extra parks as products (#37), online, motion, and the rest of AGENTS.md "Do not start".
