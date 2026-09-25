#!/usr/bin/env python3
"""Refuse provenance on added spec lines (#1052).

A spec rule says what the game does, not who built it or when (AGENTS.md, docs/agent-rails.md §1.2). This reads the
diff of docs/spec/ and docs/gameplay-spec.md between two revisions and fails on any added line that carries an issue
or PR number, an ISO date or a written-out date. Only added lines are read, so a change never has to clean up a line
it did not touch. The provenance the spec used to carry is in docs/archive/spec-provenance.md.

Usage: tools/spec-provenance.py [BASE [HEAD]]   (default HEAD^1 HEAD: the merge commit CI checks out)
"""
import re
import subprocess
import sys

PATHS = ["docs/spec", "docs/gameplay-spec.md"]
MONTHS = "January|February|March|April|May|June|July|August|September|October|November|December"
PROVENANCE = re.compile(r"#\d{3,4}|PR #|20\d\d-|(?:" + MONTHS + r") \d{1,2}, 20\d\d")


def added_lines(diff):
    """Yield (file, line number, text) for every line the unified diff adds."""
    path, line = None, 0
    for row in diff.splitlines():
        if row.startswith("+++ "):
            path = row[6:] if row.startswith("+++ b/") else None
        elif row.startswith("@@"):
            line = int(re.match(r"@@ -\S+ \+(\d+)", row).group(1))
        elif path and row.startswith("+"):
            yield path, line, row[1:]
            line += 1
        elif path and not row.startswith("-"):
            line += 1


def offences(diff):
    return [(p, n, m.group(0), text) for p, n, text in added_lines(diff) for m in [PROVENANCE.search(text)] if m]


def main(argv):
    base = argv[1] if len(argv) > 1 else "HEAD^1"
    head = argv[2] if len(argv) > 2 else "HEAD"
    diff = subprocess.run(["git", "diff", "--unified=0", "--no-color", base, head, "--", *PATHS],
                          check=True, capture_output=True, text=True).stdout
    found = offences(diff)
    for path, n, match, text in found:
        print(f"{path}:{n}: provenance '{match}' in an added spec line: {text[:160]}")
    if found:
        print("Write the rule, not the provenance: no issue or PR number and no date in docs/spec/ "
              "(AGENTS.md). Commit history keeps who did what.")
        return 1
    print(f"spec provenance: no added line carries an issue, PR or date ({base}..{head})")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
