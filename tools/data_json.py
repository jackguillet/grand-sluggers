"""Read a data file the way the game does (DataJson in the sim): JSON with // and /* */ comments and trailing
commas. Stock json.loads refuses both, and data/rules/*.json and data/agent/*.json carry notes.

Blender scripts import it with tools/ on sys.path:

    sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
    import data_json
"""
import json
from pathlib import Path


def strip(text):
    """The text with comments and trailing commas removed; string contents are left exactly as written."""
    out, i, n = [], 0, len(text)
    while i < n:
        c = text[i]
        if c == '"':
            j = i + 1
            while j < n and text[j] != '"':
                j += 2 if text[j] == '\\' else 1
            out.append(text[i:j + 1])
            i = j + 1
        elif text.startswith('//', i):
            while i < n and text[i] != '\n':
                i += 1
        elif text.startswith('/*', i):
            end = text.find('*/', i + 2)
            if end < 0:
                raise ValueError('unterminated /* comment')
            i = end + 2
        elif c == ',' and _closes_next(text, i + 1):
            i += 1
        else:
            out.append(c)
            i += 1
    return ''.join(out)


def _closes_next(text, i):
    """True when the next token after i, past whitespace and comments, closes an object or array."""
    n = len(text)
    while i < n:
        if text[i].isspace():
            i += 1
        elif text.startswith('//', i):
            while i < n and text[i] != '\n':
                i += 1
        elif text.startswith('/*', i):
            end = text.find('*/', i + 2)
            i = n if end < 0 else end + 2
        else:
            return text[i] in '}]'
    return False


def loads(text):
    return json.loads(strip(text))


def read(path):
    return loads(Path(path).read_text())
