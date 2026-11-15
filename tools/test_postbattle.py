#!/usr/bin/env python3
"""Exercise the production Boss victory, dialogue and ending flow without a window.

Run after CMake has fetched raylib, or pass --raylib path/to/raylib/src.
Audio stream continuity and rendered black pixels have separate runtime checks.
"""
from pathlib import Path
import argparse
import os
import shlex
import shutil
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[1]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--raylib", type=Path, help="Directory with raylib.h and raymath.h")
    parser.add_argument("--cc", default=os.environ.get("CC", "cc"))
    args = parser.parse_args()
    candidates = [args.raylib] if args.raylib else []
    candidates += [ROOT / "build/_deps/raylib-src/src", ROOT.parent / "raylib-5.5/src"]
    candidates += list(ROOT.glob("build*/_deps/raylib-src/src"))
    raylib = next((p for p in candidates if p and (p / "raylib.h").is_file()), None)
    if raylib is None:
        parser.error("raylib headers are missing; configure CMake or supply --raylib")
    compiler = shlex.split(args.cc)
    if not compiler or shutil.which(compiler[0]) is None:
        parser.error("C11 compiler is unavailable; supply --cc")
    with tempfile.TemporaryDirectory(prefix="mist-isle-postbattle-tests-") as temp:
        work = Path(temp)
        subprocess.run(
            [sys.executable, str(ROOT / "tools/build_maps.py"), "--output", str(work / "area_maps.h")],
            cwd=ROOT, check=True,
        )
        exe = work / ("test_postbattle.exe" if os.name == "nt" else "test_postbattle")
        subprocess.run(compiler + [
            "-std=c11", "-O1", "-g", "-DRAYMATH_STATIC_INLINE",
            "-I", str(ROOT / "src"), "-I", str(raylib.resolve()), "-I", str(work),
            str(ROOT / "src/game.c"), str(ROOT / "tools/tests/test_postbattle.c"),
            "-lm", "-o", str(exe),
        ], check=True)
        subprocess.run([str(exe)], cwd=work, check=True)


if __name__ == "__main__":
    main()
