#!/usr/bin/env python3
"""Elder Fenn drop — wrapper around drop_character.py (rigid GLB on shared sockets)."""
from __future__ import annotations

import runpy
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
if "--id" not in sys.argv:
    # Insert after -- if blender-style, else at end.
    if "--" in sys.argv:
        i = sys.argv.index("--") + 1
        sys.argv[i:i] = ["--id", "fenn"]
    else:
        sys.argv.extend(["--id", "fenn"])
runpy.run_path(str(HERE / "drop_character.py"), run_name="__main__")
