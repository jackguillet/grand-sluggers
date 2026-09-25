#!/usr/bin/env python3
"""Park factors: the same matchup and seeds at every park, reported against Harbor.

SUPERSEDED by `cli match --cohort park-factors` (#828, FR-10 / SF-30). Measure with the cohort:
its seeds and matchups are declared in code, its rows are typed, it reads the ground-rule double
off the outcome rather than off a caption, and it plays night as well as day — which this script
cannot, because it predates `cli match --night`.

    dotnet build src/GrandSluggers.Cli -c Release
    dotnet .artifacts/bin/GrandSluggers.Cli/release/GrandSluggers.Cli.dll match --cohort park-factors --table
    GRAND_SLUGGERS_TRIAL=trials/<name> dotnet .artifacts/.../GrandSluggers.Cli.dll match --cohort park-factors

This file is kept, unchanged in behaviour, because it is the tool named by
docs/research/fields-park-baseline.json: that evidence was measured with it at `d0c6e12c` and is
not re-measured. It still answers one question the cohort does not — a wide seed sweep of a single
matchup, run as parallel processes — so an operator who wants fifty seeds in a hurry can use it.

Research tool for docs/archive/fields/research-fields.md (tracker: docs/decisions/plan-fields.md, FD-02 / FD-13).
It changes no rule and passes no gate. It runs `cli match --park <id> --seed <n>` for every
park on one data root and tallies the printed play kinds.

    python3 tools/park-factors.py                       # shipped root
    python3 tools/park-factors.py --trial trials/<name>    # a trial overlay
    python3 tools/park-factors.py --json out.json

Limits: three-inning day games, one matchup (the CLI default), CPU on both sides. The seed is
the same at every park but the games diverge after the first ball in play, so this is fifty
samples per park, not fifty paired plays. Read a factor inside about 0.15 of 1.0 as noise.
"""
import argparse
import collections
import concurrent.futures
import json
import os
import re
import subprocess
import sys

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CLI = os.path.join(REPO, ".artifacts", "bin", "GrandSluggers.Cli", "release", "GrandSluggers.Cli.dll")
KINDS = ["Single", "Double", "Triple", "HomeRun", "FlyOut", "GroundOut", "Strikeout", "Walk", "Foul"]
PLAY = re.compile(r"^[TB]\d+\s+\d+-\d+\s+(\w+)\s+(.*)$")
FINAL = re.compile(r"^Final\s+(.+?)\s(\d+)\s\s(.+?)\s(\d+)\s*$")


def parks():
    return sorted(f[:-5] for f in os.listdir(os.path.join(REPO, "data", "parks")) if f.endswith(".json"))


def play(job):
    park, seed, trial = job
    env = dict(os.environ)
    env.pop("GRAND_SLUGGERS_TRIAL", None)
    if trial:
        env["GRAND_SLUGGERS_TRIAL"] = trial
    out = subprocess.run(["dotnet", CLI, "match", "--park", park, "--seed", str(seed)],
                         cwd=REPO, env=env, capture_output=True, text=True, check=True)
    return park, out.stdout


def tally(games):
    rows = {}
    for park, texts in games.items():
        kinds = collections.Counter()
        away = home = ground_rule = 0
        for text in texts:
            lines = text.splitlines()
            final = next(FINAL.match(l) for l in reversed(lines) if l.startswith("Final"))
            away += int(final.group(2))
            home += int(final.group(4))
            for line in lines:
                m = PLAY.match(line)
                if not m:
                    continue
                kinds[m.group(1)] += 1
                if "round-rule" in m.group(2):
                    ground_rule += 1
        n = len(texts)
        row = {"games": n,
               "awayRunsPerGame": round(away / n, 2),
               "homeRunsPerGame": round(home / n, 2),
               "runsPerGame": round((away + home) / n, 2),
               "groundRuleDoublesPerGame": round(ground_rule / n, 2)}
        for kind in KINDS:
            row[kind[0].lower() + kind[1:] + "PerGame"] = round(kinds[kind] / n, 2)
        rows[park] = row
    harbor = rows["harbor-diamond"]
    for row in rows.values():
        row["runFactorVsHarbor"] = round(row["runsPerGame"] / harbor["runsPerGame"], 2)
        row["homeRunFactorVsHarbor"] = round(row["homeRunPerGame"] / harbor["homeRunPerGame"], 2)
    return rows


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--trial", help="overlay folder, for example trials/<name>")
    ap.add_argument("--seeds", type=int, default=50, help="seeds 1..N at every park (default 50)")
    ap.add_argument("--jobs", type=int, default=max(1, (os.cpu_count() or 4) // 2))
    ap.add_argument("--json", help="write the rows to this file")
    args = ap.parse_args()
    if not os.path.exists(CLI):
        sys.exit("build first: dotnet build src/GrandSluggers.Cli -c Release")

    jobs = [(p, s, args.trial) for p in parks() for s in range(1, args.seeds + 1)]
    games = collections.defaultdict(list)
    with concurrent.futures.ThreadPoolExecutor(max_workers=args.jobs) as pool:
        for park, text in pool.map(play, jobs):
            games[park].append(text)
    rows = tally(games)

    print(f"root {'data + ' + args.trial if args.trial else 'data'}   seeds 1..{args.seeds}   three innings, day, CLI default matchup")
    print(f"{'park':16} {'runs/g':>7} {'x Harbor':>9} {'HR/g':>6} {'x Harbor':>9} {'2B/g':>6} {'3B/g':>6} {'GRD/g':>6}")
    for park in ["harbor-diamond"] + [p for p in rows if p != "harbor-diamond"]:
        r = rows[park]
        print(f"{park:16} {r['runsPerGame']:7.2f} {r['runFactorVsHarbor']:9.2f} {r['homeRunPerGame']:6.2f} "
              f"{r['homeRunFactorVsHarbor']:9.2f} {r['doublePerGame']:6.2f} {r['triplePerGame']:6.2f} "
              f"{r['groundRuleDoublesPerGame']:6.2f}")
    if args.json:
        with open(args.json, "w") as f:
            json.dump(rows, f, indent=2)
            f.write("\n")


if __name__ == "__main__":
    main()
