#!/usr/bin/env python3
"""Build material-specific steps from tracked grass and solid-surface sources.

Developer-only, requires ffmpeg. Players use the committed WAVs directly.
Grass: Blender Foundation / Yo Frankie!, CC-BY-3.0 (actual L/R grass steps).
Stone: Kenney Impact Sounds concrete, CC0-1.0 (short hard-surface taps).
Never uses Fantozzi Sand/Stone, gravel, or a sand/noise overlay.
"""
from array import array
import argparse
import hashlib
import json
import math
from pathlib import Path
import shutil
import subprocess
import sys
import wave

ROOT=Path(__file__).resolve().parents[1]
RATE=22050
SOURCE_DIR=ROOT/'third_party/audio/clean_footsteps'

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def prepare(source, surface, variant):
    cutoff=(1900,2050,1750)[variant] if surface=='grass' else (3400,3700,3200)[variant]
    raw=subprocess.check_output(['ffmpeg','-v','error','-i',str(source),'-ac','1','-ar',str(RATE),
        '-af',f'highpass=f=70,lowpass=f={cutoff}:p=2','-f','f32le','pipe:1'])
    data=array('f');data.frombytes(raw)
    if sys.byteorder!='little':data.byteswap()
    if not data or any(not math.isfinite(x) for x in data):raise ValueError(f'Invalid source {source}')
    peak=max(map(abs,data))
    if peak<1e-5:raise ValueError(f'Silent source {source}')
    onset=next(i for i,x in enumerate(data) if abs(x)>peak*.02)
    start=max(0,onset-round(.004*RATE))
    # Grass retains the soft plant-compression/leaf texture. Stone retains only
    # the very short original solid tap, never a granular tail or new layer.
    limit=.255 if surface=='grass' else .145
    data=list(data[start:start+round(limit*RATE)])
    mean=sum(data)/len(data);data=[x-mean for x in data]
    fade=round((.065 if surface=='grass' else .015)*RATE)
    for i in range(len(data)):
        edge=min(1.,i/(.004*RATE),(len(data)-1-i)/fade)
        data[i]*=max(0.,edge)
    rms=math.sqrt(sum(x*x for x in data)/len(data))
    target=(.043 if surface=='grass' else .052)*(1.,.97,1.03)[variant]
    gain=min(target/max(rms,1e-8),.28/max(map(abs,data)))
    return [x*gain for x in data]

def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--source-dir',type=Path,default=SOURCE_DIR)
    args=p.parse_args()
    if not shutil.which('ffmpeg'):p.error('ffmpeg is needed only to regenerate developer assets')
    source_manifest=json.loads((args.source_dir/'SOURCES.json').read_text(encoding='utf-8'))
    for name,sha in source_manifest['sha256'].items():
        if digest(args.source_dir/name)!=sha:raise ValueError(f'Original source hash mismatch: {name}')
    target=ROOT/'assets/audio/sfx';target.mkdir(parents=True,exist_ok=True)
    manifest={'format_version':2,'revision':'IMPACT-02.2-clean-materials',
        'sources':source_manifest['sources'],
        'processing':'No mixed sand/gravel sources. Mono PCM16 22050 Hz; 70 Hz high-pass; grass 1750-2050 Hz low-pass and 255 ms soft tail; concrete 3200-3700 Hz low-pass and original short tap; linear RMS matching, peak ceiling 0.28, click-free fades. No synthetic noise added.',
        'variation_note':'Six playback variations per material, not six independent L/R recordings. Grass derives from two true L/R steps; stone from five concrete steps. Reused source variations differ in EQ/level. Foot denotes playback lane.',
        'routing':{'coastal_camp':'grass','glowglade_grove':'grass','tidal_ruins':'stone','lighthouse_courtyard':'stone'},'files':[]}
    for surface in ('grass','stone'):
        for i,suffix in enumerate('abcdef'):
            name=f'sfx_step_grass_{"lr"[i%2]}.flac' if surface=='grass' else f'footstep_concrete_{i%5:03}.ogg'
            source=args.source_dir/name
            data=prepare(source,surface,i//2)
            out=target/f'step_{surface}_{suffix}.wav'
            pcm=array('h',(round(x*32767) for x in data))
            if sys.byteorder!='little':pcm.byteswap()
            with wave.open(str(out),'wb') as wav:
                wav.setnchannels(1);wav.setsampwidth(2);wav.setframerate(RATE);wav.writeframes(pcm.tobytes())
            manifest['files'].append({'path':out.relative_to(ROOT).as_posix(),'source':name,
                'source_sha256':digest(source),'sha256':digest(out),'foot':'LR'[i%2],'take':i//2+1,
                'surface':surface,'license':'CC-BY-3.0' if surface=='grass' else 'CC0-1.0',
                'seconds':len(data)/RATE,'peak':max(map(abs,data)),
                'rms':math.sqrt(sum(x*x for x in data)/len(data))})
    (ROOT/'assets/audio/footsteps_manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print('Rebuilt 12 grass/solid-stone variations from separate tracked materials; no sand/gravel source.')
if __name__=='__main__':main()
