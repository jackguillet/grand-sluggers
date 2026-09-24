"""Read the game's data files from Python the way the sim reads them (DataJson: comments skipped, trailing commas
allowed). Half of data/ carries // notes, so plain json.loads fails on them. Import it, do not copy it:

    sys.path.insert(0, str(REPO / 'tools'))  # tools/ is not a package
    import jsonc
    rules = jsonc.load(REPO / 'data' / 'rules' / 'infield.json')
"""
import json
import re
from pathlib import Path

_TRAILING_COMMA = re.compile(r",(\s*[}\]])")


def strip(text):
    """The text with // and /* */ comments removed outside strings, and trailing commas dropped."""
    out, i, n, in_string = [], 0, len(text), False
    while i < n:
        c = text[i]
        if in_string:
            out.append(c)
            if c == "\\" and i + 1 < n:
                out.append(text[i + 1])
                i += 1
            elif c == '"':
                in_string = False
        elif c == '"':
            in_string = True
            out.append(c)
        elif text.startswith("//", i):
            while i < n and text[i] != "\n":
                i += 1
            continue
        elif text.startswith("/*", i):
            end = text.find("*/", i + 2)
            if end < 0:
                raise ValueError("unterminated /* comment")
            i = end + 2
            continue
        else:
            out.append(c)
        i += 1
    return _TRAILING_COMMA.sub(r"\1", "".join(out))


def loads(text):
    return json.loads(strip(text))


def load(path):
    return loads(Path(path).read_text())
