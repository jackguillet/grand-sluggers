# `cpu-read` — the CPU batter commits from what it can see

The trial overlay for P2-g ([#892](https://github.com/jackguillet/grand-sluggers/issues/892)),
the batting half of PH-18: CPU opponents commit to swings that late legal steering can defeat, and
never read a hidden pitch destination.

## Running it

```
GRAND_SLUGGERS_TRIAL=trials/cpu-read dotnet run --project src/GrandSluggers.Cli -- match --seed 7
python3 tools/local-player.py --trial trials/cpu-read        # the window Jack sits
```

Unset, this folder does nothing. Every `cli` run names the root and the overlay it read on stderr.

## What is here

| File | What it changes |
| --- | --- |
| `rules/batting.json` | `cpu.commitRead` `false` → `true`. Every other field is the shipped file, byte for byte (S-140). |

Whole files, never fields: an edit to `data/rules/batting.json` has to be mirrored here, or the two
runs read different batting tables.

## What the switch does

The CPU batter commits at plate − `batting.window.leadSec` − `batting.cpu.decideLeadSec`
(`AtBatMotion.CpuDecisionTime`). Shipped, `Match.CpuSwing` then classifies the **final** crossing of
the command it is handed. With the switch on it reads `Match.CpuReadPitch` instead: the same
delivery with the stick's break frozen where it stood at the commit, projected to the plate with
the family's own movement and no future steering. Zone, swing / take and the box all follow that
read; the umpire and the bat still meet the ball that is thrown. A steer that starts after the
commit therefore beats the read (S-138). The switch adds no draw.

The break at the commit is the stick the client watched when it passes one (`breakAtCommit`), and
otherwise the stick held one way from release: `min(|BreakX|, BreakReach(Pitch, commit))`, which is
exactly what the CPU pitcher's drawn steer and a hand holding the stick from release have reached.

## What to sit

Pitch against the CPU batter. Put an edge strike up, let the CPU commit, then steer it off the
plate late. The question is PH-18's: does a late steer fool the CPU batter fairly — not always,
not never, and only when it came after the batter had to decide?

On today's numbers most CPU-pitched steers have already reached the whole ±1 by the commit, so
CPU-vs-CPU games barely move (seed 7 is identical); the trial is felt from the mound.
