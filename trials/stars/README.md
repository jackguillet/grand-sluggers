# `stars` — Star prices by tier

The trial overlay for the Star resource children of [#803](https://github.com/jackguillet/grand-sluggers/issues/803)
(P5-a), PH-16-R7 and PH-16-R8: a small set of cost tiers, the highest for captains only.

## Running it

```
GRAND_SLUGGERS_TRIAL=trials/stars dotnet run --project src/GrandSluggers.Cli -- match --seed 7
python3 tools/local-player.py --trial trials/stars        # the window Jack sits
```

Unset, this folder does nothing. Every `cli` run names the root and the overlay it read on stderr.

## What is here

| File | What it changes |
| --- | --- |
| `rules/stars.json` | P5-a: `tiers` `low` 1 / `mid` 1 / `top` 1 → `low` 1 / `mid` 2 / `top` 3. P5-b: `startingReserve` 4 → 3, `gains.plateAppearance` 0 → 0.1. Every other field is the shipped file, byte for byte (S-176). |

Whole files, never fields: an edit to `data/rules/stars.json` has to be mirrored here, or the two runs read
different star tables.

The tier each ability belongs to is not a trial field. It lives in the shipped
`data/abilities/star-skills.json`, where it prices nothing while every tier costs 1. The validator already holds
it to the decided rule there: only a captain may carry a top-tier special.

## The proposal

| Tier | Price | Abilities | Why |
| --- | --- | --- | --- |
| `low` | 1 | Star Fastball, Star Change, Star Breaker; Star Grounder, Star Fly, Star Line | The role players' specials. Speed or a fixed launch, nothing after the ball is hit. Today's price, so a role player's special costs what it always did. |
| `mid` | 2 | Charmball, Prismball, Phonyball, Fogball; Heart, Shell, Staff, Phony swings | Captains' specials that bend the read: a wobble, a late break, a decoy path, a short pause, a random first bounce. The batter or the fielder still has an answer. |
| `top` | 3 | Heatball, Caskball, Skullball; Heat swing, Furnace, Cask swing | Captains' specials that change the field after the ball is put in play, or carry the biggest speed or exit multiplier: the burn hop, the knockback, the fastest pitch (×1.20), the burn patch and lava strip, the fragments, the biggest exit (×1.25). |

The guest-captain surcharge stays at 1, so Rio pitching for Vale's team costs 4.

Why these three numbers:

- **The meter holds 5.** A top special at 3 leaves room for one more low or mid special. A full meter buys one big
  moment plus a small one, or two mid specials, and no more.
- **Low stays at today's 1.** The role players' specials were the cheap, frequent ones. They stay that way.
- **Top at 3, not 4 or 5.** At 4 a guest captain's top special (5) would take a full meter. At 5 it could not be
  bought at all, which the rules validator refuses.
- **Four captains have only mid specials** (Vale, Zig, Brondo, Fenn), and three have only top (Rio, Konga,
  Ashlord). Cheaper specials come more often; costlier ones hit harder. Whether that trade reads as fair between
  captains is the question the trial asks.

## The reserve and the base gain (P5-b)

The rules are decided and ship: one pool per team, the same reserve for both teams, and a base gain for both teams
at every completed plate appearance, on top of the event bonuses. The amounts are the trial.

| Field | Shipped | Trial | Why |
| --- | --- | --- | --- |
| `startingReserve` | 4 | 3 | Shipped 4 is what an average team started with under the old chemistry table, so the shipped game barely moves. Under the trial prices, 3 buys any captain's own top special at the opening plate appearance (PH-16-R6), and nothing more, so the first big special is a choice. A guest captain's top special (4) has to be earned. |
| `gains.plateAppearance` | 0 | 0.1 | "Modest" (PH-16-R5): a 3-inning game has about 25 completed appearances, so each team earns about 2.5 Stars from the base. That is under one top special a game on top of the bonuses, which stay today's numbers. Shipped keeps 0 until an amount is accepted. |

The event bonuses (`gains.strikeout` … `gains.robbedHomer`) are the performance bonuses PH-16-R5 asks for. The
trial keeps them as shipped.

What CPU games show (30 seeds × three pairings, three innings): the CPU uses 1.9–2.8 specials a game shipped and
1.4–2.4 under the trial, and **both pools end the game at or near the 5-Star cap** in both. The CPU's special
frequency is set by its per-pitch chance, not by the Stars. The tier prices will bite a human who spends freely;
they barely move the CPU.

## What to sit

Play Exhibitions as and against Rio (top) and Vale (mid). Ask:

- Does a top special feel like a big moment worth saving for, and a mid special like something you use when it fits?
- Does a captain whose specials are all mid feel weaker, or just different?
- Do the CPU's specials come often enough to notice and rarely enough to answer?

Most seeds of `cli match` play differently under the overlay (22 of 30 on the P5-a base). That is expected: a CPU
captain can pay for a mid or top special less often, so it throws fewer of them.
