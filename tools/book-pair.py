#!/usr/bin/env python3
"""The couch book changes in pairs (#1056; AGENTS.md "Rails, not patches": a couch verb or camera change updates
docs/how-to-play.md and HowToPlay.cs in the same PR).

Reads the files a change touches between two revisions. When it touches one side of the pair and not the other, it
fails, unless the PR carries the override label: a change that moves code in HowToPlay without changing what the book
says (a rename, a refactor) is labelled `book-unchanged` by its author, on purpose.

Usage: tools/book-pair.py [BASE [HEAD]]   (default HEAD^1 HEAD: the merge commit CI checks out)
Environment: PR_LABELS, comma-separated label names (CI passes the PR's labels).
"""
import os
import subprocess
import sys

BOOK = "docs/how-to-play.md"
CODE = ("src/GrandSluggers.Sim/HowToPlay.cs", "src/GrandSluggers.Sim/HowToPlay.Screens.cs")
OVERRIDE = "book-unchanged"


def verdict(changed, labels):
    """None when the pair holds, else the message that says what is missing."""
    book = BOOK in changed
    code = sorted(p for p in changed if p in CODE)
    if book == bool(code) or OVERRIDE in labels:
        return None
    if book:
        return (f"{BOOK} changed but {' / '.join(CODE)} did not. The couch copy lives in HowToPlay; "
                f"change both in this PR, or label it '{OVERRIDE}' if the in-game book does not change.")
    return (f"{', '.join(code)} changed but {BOOK} did not. Update the couch map in this PR, "
            f"or label it '{OVERRIDE}' if what the book says does not change.")


def main(argv):
    base = argv[1] if len(argv) > 1 else "HEAD^1"
    head = argv[2] if len(argv) > 2 else "HEAD"
    changed = set(subprocess.run(["git", "diff", "--name-only", base, head], check=True,
                                 capture_output=True, text=True).stdout.split())
    labels = {l.strip() for l in os.environ.get("PR_LABELS", "").split(",") if l.strip()}
    message = verdict(changed, labels)
    if message:
        print("book pair: " + message)
        return 1
    print(f"book pair: holds ({base}..{head})")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
