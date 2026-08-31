# Agents

Grand Sluggers is a **complete, polished party baseball game**. The long-term bar is Nintendo-level Exhibition (then local 1v1): Super Sluggers *systems* — cameras, HUD, plays, lineup — with original IP. Not a prototype that lucks into a still.

Vision: `docs/vision.md`. Couch map: `docs/how-to-play.md`. Sequence: `docs/roadmap.md`. Product checkpoint: GitHub **#324**.

## Rails, not patches

Every decision serves that product. **Do not ship a quick fix.** If a ticket can close with a hack *or* a rail, **build the rail**. Take longer. Do not “just make the still / test / issue pass.”

A change is a **patch** — reject it:

- Special-cases one play, one camera, one pad count, or one HUD screen
- Auto-resolves a user-owned play in the sim as a caption (`turns two`, steal as a roll)
- Hardcodes a `Vector3`, FOV, or layout when a table exists (`data/feel/`, `data/art/`, `BroadcastHud`)
- Forks 1P vs 2P presentation (SET camera, HUD anchors, body/item scale)
- Grows a `switch` in `MatchDirector` instead of a named system other plays can call
- Leaves the next play type to invent the same thing again

A change is a **rail** — do this:

- Lives in data or a named director that every play type can use
- Works the same for 1P and 1v1 unless the design names a real difference — HUD and body scale stay one recipe even then
- Lets the player own the verb (throw to a bag, jump a fly, gun a steal, second throw of a DP)
- Has a test that would catch the *next* play or seat, not only this screenshot
- Updates `docs/how-to-play.md` in the same PR when a couch verb or camera changes

Ask, before coding: *will this still be the right shape when two pads are seated, when the ball is a pop fly instead of a hopper, and when we show this still to a friend?* If not, stop and put the system in the right place.

## Operating

- One GitHub child issue = one worktree. Never share the main working copy. Never `git add -A`.
- Sim owns baseball. Unity presents. Catalog (`data/art`, `data/feel`) before files. No second skeleton, no new park product, no `MatchDirector` god-file.
- Harbor Exhibition is the slice. Challenge, extra parks, unique meshes, online, motion, Nintendo IP stay later (`docs/roadmap.md`).
- Falsify with `dotnet test`, `dotnet run --project src/GrandSluggers.Cli -- art`, `cli match`, `tools/unity-compile.sh`.
