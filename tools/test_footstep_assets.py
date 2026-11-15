#!/usr/bin/env python3
"""Validate shipped footstep samples without ffmpeg or a sound device."""
from array import array
import hashlib
import json
import math
from pathlib import Path
import sys
import wave
ROOT=Path(__file__).resolve().parents[1]
manifest=json.loads((ROOT/'assets/audio/footsteps_manifest.json').read_text(encoding='utf-8'))
assert manifest['license']=='CC0-1.0'
assert manifest['routing']=={'coastal_camp':'grass','glowglade_grove':'grass','tidal_ruins':'stone','lighthouse_courtyard':'stone'}
assert len(manifest['files'])==12
assert {item['path'] for item in manifest['files']}=={f'assets/audio/sfx/step_{surface}_{suffix}.wav' for surface in ('grass','stone') for suffix in 'abcdef'}
seen=set()
for item in manifest['files']:
    p=ROOT/item['path']; digest=hashlib.sha256(p.read_bytes()).hexdigest()
    assert digest==item['sha256'] and digest not in seen
    seen.add(digest)
    with wave.open(str(p)) as wav:
        assert (wav.getnchannels(),wav.getsampwidth(),wav.getframerate())==(1,2,22050)
        assert .12<=wav.getnframes()/wav.getframerate()<=.30
        data=array('h');data.frombytes(wav.readframes(wav.getnframes()))
        if sys.byteorder!='little':data.byteswap()
    peak=max(abs(x) for x in data)/32767
    rms=math.sqrt(sum((x/32767)**2 for x in data)/len(data))
    assert .02<rms<.09 and peak<.35
    assert abs(data[0])<=1 and abs(data[-1])<=1
    print(f'PASS {p.name}: {len(data)/22050:.3f}s, RMS {rms:.3f}, peak {peak:.3f}, fades and hash OK')
print('All 12 unique recorded footstep assets and four map assignments verified.')
