#!/usr/bin/env python3
"""Original toys, original tones (#1056; AGENTS.md "Art": no Nintendo samples, meshes, mushrooms, plumbers,
princesses, or set dressing).

Scans what a player can see or hear for Nintendo names: every file name under data/ and unity/Assets/Art, the
text of data/ (except data/agent/, which is research evidence about the reference), the couch book
(docs/how-to-play.md), and the string literals of the couch-copy classes and the Unity scripts. Comments are not
scanned, so a line that says "never Nintendo samples" passes. Research docs are not scanned: they study the reference.

Usage: tools/original_ip.py   (exit 1 and one line per hit)
"""
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
NAMES = re.compile(
    r"\b(nintendo|mario|luigi|peach|princess|bowser|yoshi|wario|waluigi|rosalina|koopa|goomba|birdo|"
    r"donkey\s*kong|diddy\s*kong|mushroom|plumber)s?\b", re.IGNORECASE)
COPY = re.compile(r"^src/GrandSluggers\.Sim/(HowToPlay|CarnivalFront|BroadcastHud|PauseMenu)[\w.]*\.cs$")
STRING = re.compile(r'"(?:[^"\\\n]|\\.)*"')


def tracked():
    out = subprocess.run(["git", "ls-files"], cwd=ROOT, check=True, capture_output=True, text=True).stdout
    return out.split("\n")


def hits_in(text, strings_only):
    """(line number, name) for each hit; in code only string literals count."""
    found = []
    for n, line in enumerate(text.splitlines(), 1):
        spans = [m.group(0) for m in STRING.finditer(line)] if strings_only else [line]
        found += [(n, m.group(0)) for s in spans for m in NAMES.finditer(s)]
    return found


def scan(paths):
    offences = []
    for path in paths:
        if not path:
            continue
        if path.startswith(("data/", "unity/Assets/Art/")):
            for m in NAMES.finditer(Path(path).name):
                offences.append(f"{path}: file name names '{m.group(0)}'")
        if path.startswith("data/agent/"):
            continue
        strings_only = path.endswith(".cs")
        text_file = (path.startswith("data/") and path.endswith((".json", ".md", ".txt"))) or path == "docs/how-to-play.md" \
            or COPY.match(path) or (path.startswith("unity/Assets/Scripts/") and path.endswith(".cs"))
        if not text_file:
            continue
        text = (ROOT / path).read_text(encoding="utf-8", errors="replace")
        offences += [f"{path}:{n}: '{name}'" for n, name in hits_in(text, strings_only)]
    return offences


def main():
    offences = scan(tracked())
    for o in offences:
        print("original ip: " + o)
    if offences:
        print("Original pictures, original tones: no Nintendo names in what a player sees or hears (AGENTS.md \"Art\").")
        return 1
    print("original ip: no Nintendo name in data, art, the book or couch copy")
    return 0


if __name__ == "__main__":
    sys.exit(main())
