# `pitch5` — the stick trial (P2-c, #855)

**This overlay is now one key: `batting.geometryOnly: true`.**

Until #860 it was the duel trial: the three new pitch families (#818), the CPU pitcher on human
inputs (#823), the RB / Tab cycle (#835) and the one shared timing window (#844). Jack played that
window (`main-d49c527351`, trial revision `d49c5273`) on September 22, 2026 and wrote **"trial was
good."** [#860](https://github.com/jackguillet/grand-sluggers/issues/860) moved those numbers into
`data/rules/` (PH-20-R1: only accepted numbers move there), so they are the shipped game now:

- `data/rules/pitching.json` authors the `curveball`, `slider` and `sinker` rows, turns
  `cpu.humanInputs` on, and carries the accepted count-row `families` weights and `steerChance`.
- `data/rules/batting.json` turns `window.shared` on: one 9-frame window for every hitter, both
  swings and every difficulty.

The report is [`docs/research/promote-duel-trial.md`](../../docs/research/promote-duel-trial.md).
What is left here was **not** on the window Jack played, so it stays a trial.

## What is in it

```
trials/pitch5/rules/batting.json     overrides data/rules/batting.json
```

One file, the **whole** shipped file (a trial writes whole files, never fields), with one named
change: `geometryOnly` `false` → `true` ([#855](https://github.com/jackguillet/grand-sluggers/issues/855),
PH-12, PH-18). Under it an ordinary swing (not a bunt, not a Star Swing) ignores the stick at
contact, so timing, contact position, pitch height and the swing decide the ball, and the CPU
batter draws no aim for it. The stick still walks the box. Bunts and Star Swings still read the
stick. Everything else in the file, including `window.shared: true`, is the shipped value.
`SharedWindowScenarioTests.S126_…` restores the key and deep-compares the two files, so a shipped
edit that forgets this copy fails loudly. `pitching.json` is not overridden: it would equal the
shipped file.

Report: [`docs/research/stick-shaping-p2c.md`](../../docs/research/stick-shaping-p2c.md).

## Running it

```
GRAND_SLUGGERS_TRIAL=trials/pitch5 dotnet run --project src/GrandSluggers.Cli -- art
GRAND_SLUGGERS_TRIAL=trials/pitch5 dotnet run --project src/GrandSluggers.Cli -- match --seed 7
python3 tools/local-player.py --trial trials/pitch5
```

Every `cli` run prints the root and the overlay it read on stderr. Unset, this folder does nothing:
`geometryOnly` is `false` and the stick shapes every swing.

## What has not been decided

Whether stick shaping ships. That is P2-c's own sitting and Jack's call.
