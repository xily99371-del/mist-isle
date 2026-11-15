#!/usr/bin/env python3
"""Run long-session weather and physical-pixel viewport regressions."""
from pathlib import Path
import argparse
import os
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--raylib", type=Path, required=True, help="Directory containing raylib.h")
args = parser.parse_args()
with tempfile.TemporaryDirectory(prefix="mistisle-render-math-") as temp:
    binary = Path(temp) / "test_render_math"
    subprocess.run([os.environ.get("CC", "cc"), "-std=c11", "-Wall", "-Wextra", "-Werror",
                    "-I", str(ROOT / "src"), "-I", str(args.raylib),
                    str(ROOT / "tools/tests/test_render_math.c"), "-lm", "-o", str(binary)], check=True)
    subprocess.run([str(binary)], check=True)
