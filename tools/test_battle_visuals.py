#!/usr/bin/env python3
"""Capture the real battle renderer, not a mockup; compile after CMake build."""
from pathlib import Path
import argparse, subprocess, shutil
ROOT=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser(description=__doc__)
p.add_argument('--build', type=Path, default=ROOT/'build')
p.add_argument('--boss-combo', action='store_true', help='Capture third-stage multi-hit guard prompts')
p.add_argument('--output', type=Path, default=ROOT/'build/battle-frames')
a=p.parse_args();b=a.build.resolve();out=a.output.resolve();out.mkdir(parents=True,exist_ok=True)
raylib=b/'_deps/raylib-src/src'
# Also supports a local CMake override with the pinned vendor source.
if not raylib.is_dir():raylib=ROOT/'vendor/raylib/src'
libs=list((b/'_deps').rglob('libraylib.a'))
if not libs:raise SystemExit('Build the Linux game before visual QA.')
exe=b/'battle-visual-qa'
sources=['tools/battle_visual_qa.c','src/game.c','src/battle.c','src/render.c','src/ending.c','src/ui_text.c']
subprocess.run(['cc','-std=c11','-O2','-DRAYMATH_STATIC_INLINE','-I'+str(ROOT/'src'),'-I'+str(raylib),'-I'+str(b/'generated'),
                *[str(ROOT/f) for f in sources],str(libs[0]),'-lm','-lpthread','-ldl','-lrt','-lX11','-o',str(exe)],check=True)
subprocess.run(['xvfb-run','-a',str(exe),str(out)]+(['--boss-combo'] if a.boss_combo else []),cwd=ROOT,check=True)
if shutil.which('ffmpeg'):
    subprocess.run(['ffmpeg','-y','-loglevel','error','-framerate','20','-i',str(out/'frame-%04d.png'),
                    '-c:v','libx264','-crf','22','-pix_fmt','yuv420p','-movflags','+faststart',str(out.parent/('boss-guard-combo.mp4' if a.boss_combo else 'battle-impact-02.mp4'))],check=True)
