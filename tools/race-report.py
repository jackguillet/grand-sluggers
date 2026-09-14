#!/usr/bin/env python3
"""Summarize version-2 observations without inventing missing phases or gameplay targets."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess


def identity(value):
    digest = hashlib.sha256(value['inputsJson'].encode()).hexdigest()
    if digest != value['sha256']:
        raise ValueError('Effective-input identity does not match its payload')
    return {k: value[k] for k in ('build', 'moduleId', 'sha256')}


def trace_summary(path):
    trace = json.loads(path.read_text())
    if trace.get('schemaVersion') != 2 or not trace.get('context'):
        raise ValueError(f'{path}: a complete version-2 context is required')
    marks = trace['marks']
    context = dict(trace['context'])
    context['identity'] = identity(context['identity'])
    start = trace['commands'][0]['input']
    hit = start.get('hit', {})
    legs = []
    for release in (m for m in marks if m['kind'] == 'ThrowRelease'):
        following = [m for m in marks if m.get('leg') == release['leg']]
        event = lambda kind: next((m for m in following if m['kind'] == kind), None)
        target, wait, reception, loose = (event(k) for k in ('ThrowTargetReached', 'UncoveredWait', 'Reception', 'LooseBall'))
        legs.append({
            'leg': release['leg'], 'flight': release['flight'], 'releaseSec': release['t'],
            'nominalTargetSec': release['t'] + release['flight']['durationSec'],
            'observedTargetSec': target['t'] if target else None,
            'waitStartedSec': wait['t'] if wait else None,
            'receptionSec': reception['t'] if reception else None,
            'looseSec': loose['t'] if loose else None,
            'releaseToReceptionSec': reception['t'] - release['t'] if reception else None,
        })
    movement = []
    for first in trace['ticks'][0]['fielders']:
        pos = first['pos']
        samples = [(t['t'], f) for t in trace['ticks'] for f in t['fielders'] if f['pos'] == pos]
        moving = next(((t, f) for t, f in samples if abs(f.get('observedVx') or 0) + abs(f.get('observedVz') or 0) > 1e-8), None)
        movement.append({'pos': pos, 'character': first['who']['id'], 'readEligibleAtSec': first['readEligibleAt'],
                         'firstObservedDisplacementSec': moving[0] if moving else None,
                         'nominalInitialPursuitFtSec': first['pursuitSpeedFtSec']})
    first_possession = next((m['t'] for m in marks if m['kind'] == 'Possession'), None)
    return {'fixture': path.stem, 'context': context, 'contact': {k: hit[k] for k in ('exitVeloMph', 'launchDeg', 'sprayDeg', 'carryFt', 'class') if k in hit},
            'completed': trace.get('completed'), 'ticks': len(trace['ticks']), 'commands': len(trace['commands']),
            'firstPossessionSec': first_possession, 'movement': movement, 'legs': legs, 'marks': marks,
            'classification': 'fault-fixture' if path.stem.startswith('fault-') else 'movement-prefix' if trace.get('completed') is None else 'baseline-fixture',
            'uncertainty': 'Simulation hooks at command time; runner touch interval and lastTouchAt retained. Observed displacement includes snaps. Retired-runner projections are not actual arrivals.'}


def cohort_summary(path):
    report = json.loads(path.read_text())
    games = []
    outcomes = {}
    for g in report['games']:
        row = dict(g)
        row['identity'] = identity(g['identity'])
        games.append(row)
        for kind, count in g['outcomes'].items():
            outcomes[kind] = outcomes.get(kind, 0) + count
    count = len(games)
    means = {'home': sum(g['homeRuns'] for g in games) / count, 'away': sum(g['awayRuns'] for g in games) / count}
    if means != {'home': report['meanHomeRuns'], 'away': report['meanAwayRuns']}:
        raise ValueError(f'{path}: reported means do not match the games')
    return {'cohort': report['cohort'], 'seeds': report['seeds'], 'gameCount': count, 'meansRunsPerSideGame': means,
            'outcomes': outcomes, 'multipleOutPlays': sum(g['multipleOutPlays'] for g in games),
            'acceptance': report['acceptance'], 'limitations': report['limitations'], 'games': games}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--traces', type=Path, required=True)
    parser.add_argument('--cohort', type=Path, action='append', default=[])
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    root = Path(__file__).resolve().parent.parent
    revision = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=root, text=True).strip()
    paths = sorted((root / 'src/GrandSluggers.Sim').glob('*.cs')) + sorted((root / 'data/rules').glob('*.json'))
    report = {'schemaVersion': 1, 'sourceRevisionAtSummary': revision,
              'sourceHashes': {str(p.relative_to(root)): hashlib.sha256(p.read_bytes()).hexdigest() for p in paths},
              'acceptedNumericTargetsAdded': [], 'humanGate': 'open',
              'traces': [trace_summary(p) for p in sorted(args.traces.glob('*.json'))],
              'cohorts': [cohort_summary(p) for p in args.cohort]}
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, indent=2, allow_nan=False) + '\n')
    print(f"Wrote {len(report['traces'])} trace summaries and {len(report['cohorts'])} cohorts to {args.output}")


if __name__ == '__main__':
    main()
