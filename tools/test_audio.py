#!/usr/bin/env python3
"""Compile production audio.c against an observable fake raylib audio backend.

Run after CMake has fetched raylib, or pass --raylib path/to/raylib/src.
This checks state transitions and event timing; it does not claim codec/device QA.
"""
from pathlib import Path
import argparse
import os
import shlex
import shutil
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--raylib", type=Path, help="Directory containing raylib.h and raymath.h")
    parser.add_argument("--cc", default=os.environ.get("CC", "cc"), help="C11 compiler (GCC/Clang)")
    args = parser.parse_args()
    candidates = [args.raylib] if args.raylib else []
    candidates += [ROOT / "build/_deps/raylib-src/src", ROOT.parent / "raylib-5.5/src"]
    candidates += list(ROOT.glob("build*/_deps/raylib-src/src"))
    raylib = next((p for p in candidates if p and (p / "raylib.h").is_file()), None)
    if raylib is None:
        parser.error("raylib headers are missing; configure CMake first or supply --raylib")
    compiler = shlex.split(args.cc)
    if not compiler or shutil.which(compiler[0]) is None:
        parser.error("C11 compiler is unavailable; supply --cc")
    with tempfile.TemporaryDirectory(prefix="mist-isle-audio-tests-") as temp:
        work = Path(temp)
        assets = work / "assets/audio"
        assets.mkdir(parents=True)
        (assets / "enemy.tsv").write_text(
            "# Test-only bilingual fixture, not song lyrics.\n"
            "\ufeff10\t12\tThe lighthouse wakes.\t灯塔醒来了。\t0.8\n"
            "11\t13\tOverlapping row must be ignored.\t重叠行。\t0.1\n"
            "14\t16\tKeep moving.\t继续前行。\t0.4\n"
            "bad\t20\tBad timestamp.\t坏时间。\t0.5\n",
            encoding="utf-8",
        )
        (assets / "enemy_energy.tsv").write_text("# 20 Hz samples\n0\n0.25\n0.5\n0.75\n1\n", encoding="utf-8")
        exe = work / ("test_audio.exe" if os.name == "nt" else "test_audio")
        command = compiler + [
            "-std=c11", "-Wall", "-Wextra", "-Werror", "-O1", "-g",
            "-I", str(ROOT / "src"), "-I", str(raylib.resolve()),
            str(ROOT / "src/audio.c"), str(ROOT / "tools/tests/test_audio.c"),
            "-lm", "-o", str(exe),
        ]
        subprocess.run(command, check=True)
        subprocess.run([str(exe)], cwd=work, check=True)


if __name__ == "__main__":
    main()
