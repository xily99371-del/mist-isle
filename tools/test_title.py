#!/usr/bin/env python3
"""Run production title-menu input and save-safety regression tests."""
import argparse, subprocess, tempfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser(description=__doc__)
p.add_argument("--raylib",type=Path,required=True)
a=p.parse_args()
with tempfile.TemporaryDirectory(prefix="title-tests-") as folder:
    exe=Path(folder)/"title-tests"
    subprocess.run(["cc","-std=c11","-Wall","-Wextra","-Werror",
        "-I"+str(ROOT/"src"),"-I"+str(a.raylib),
        str(ROOT/"src/title_menu.c"),str(ROOT/"tools/tests/test_title.c"),
        "-lm","-o",str(exe)],check=True)
    subprocess.run([str(exe)],check=True,cwd=folder)
