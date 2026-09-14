#!/usr/bin/env python3
"""Regenerate the measured C# bat contract from its authoring JSON. No bpy."""
import argparse
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]

def generated(doc):
    lines=['    // <swing-keys>', '    // Generated from data/art/swing-takes.json by tools/blender/sync_swing_contract.py.']
    for row in doc['takes']:
        lines += ['    public static readonly IReadOnlyList<Key> '+('SlapKeys' if row['take']=='slap' else 'ChargeKeys')+' =','    [']
        for k in row['keys']:
            fmt=lambda vals:', '.join(f'{v:g}' for v in vals)
            lines.append(f'        new({k["t"]:g}, new({fmt(k["leftHand"])}), new({fmt(k["rightHand"])}), new({fmt(k["grip"])}), Unit({fmt(k["barrel"])}), {k["legs"]["lift"]:g}),')
        lines+=['    ];','']
    return '\n'.join(lines+['    // </swing-keys>'])

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--check',action='store_true');args=p.parse_args()
    dest=ROOT/'src/GrandSluggers.Sim/SwingPresentation.cs';text=dest.read_text()
    a=text.index('    // <swing-keys>');b=text.index('    // </swing-keys>',a)+len('    // </swing-keys>')
    result=text[:a]+generated(json.loads((ROOT/'data/art/swing-takes.json').read_text()))+text[b:]
    if args.check:
        if result!=text:raise SystemExit('swing contract stale; run sync_swing_contract.py')
    else:dest.write_text(result)
