#!/usr/bin/env python3
"""Prepare warm, short grass/stone foley from Fantozzi's CC0 recordings.

Developer-only: extract third_party/audio/fantozzi/source.7z, then run
python tools/import_footsteps.py --source-dir <extracted Fantozzi-footsteps/flac>
Requires ffmpeg. Players use the tracked WAV files; no tools/downloads at runtime.
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

ROOT = Path(__file__).resolve().parents[1]
RATE = 22050
ARCHIVE_SHA256 = '415d360e0911c1c1c5355ee94023b772e6f4044f59b9b580610e7dbffd1840fd'
SOURCE_URL = 'https://opengameart.org/content/fantozzis-footsteps-grasssand-stone'


def prepare(source: Path, surface: str) -> list[float]:
    # No oscillator/sine layer: retain the original heel/sole/rustle texture.
    cutoff = 2900 if surface == 'grass' else 4200
    raw = subprocess.check_output([
        'ffmpeg', '-v', 'error', '-i', str(source), '-ac', '1', '-ar', str(RATE),
        '-af', f'highpass=f=100,lowpass=f={cutoff}', '-f', 'f32le', 'pipe:1'])
    samples = array('f'); samples.frombytes(raw)
    if sys.byteorder != 'little': samples.byteswap()
    if not samples: raise ValueError(f'Empty recording: {source}')
    peak = max(abs(x) for x in samples)
    if not math.isfinite(peak) or peak < .0001: raise ValueError(f'Invalid recording: {source}')
    threshold = peak * .018
    first = next(i for i, x in enumerate(samples) if abs(x) > threshold)
    first = max(0, first - int(.004 * RATE))
    seconds = .27 if surface == 'grass' else .22
    trimmed = list(samples[first:first + round(seconds * RATE)])
    mean = sum(trimmed) / len(trimmed)
    trimmed = [x - mean for x in trimmed]
    rms = math.sqrt(sum(x*x for x in trimmed) / len(trimmed))
    gain = (.060 if surface == 'grass' else .067) / max(rms, 1e-6)
    ceiling = .30 if surface == 'grass' else .34
    result = []
    fade = round((.040 if surface == 'grass' else .030) * RATE)
    for i, x in enumerate(trimmed):
        envelope = min(1.0, i / (.003 * RATE), (len(trimmed)-1-i) / fade)
        result.append(ceiling * math.tanh(x * gain / ceiling) * max(0, envelope))
    return result


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source-dir', type=Path, required=True)
    args = parser.parse_args()
    if not shutil.which('ffmpeg'): parser.error('ffmpeg is required to import developer assets')
    archive = ROOT / 'third_party/audio/fantozzi/source.7z'
    if hashlib.sha256(archive.read_bytes()).hexdigest() != ARCHIVE_SHA256:
        raise ValueError('Original recording archive does not match reviewed source')
    target = ROOT / 'assets/audio/sfx'; target.mkdir(parents=True, exist_ok=True)
    manifest = {'format_version': 1, 'author': 'Fantozzi', 'editor': 'qubodup (single-step cuts)',
        'license': 'CC0-1.0', 'source_url': SOURCE_URL, 'source_archive_sha256': ARCHIVE_SHA256,
        'processing': 'Mono 22050 Hz PCM16; 100 Hz high-pass; grass 2900 Hz / stone 4200 Hz low-pass; onset trim, short tail fade, RMS matching and soft peak limiting. No pitch or tempo shift in source files.',
        'routing': {'coastal_camp': 'grass', 'glowglade_grove': 'grass', 'tidal_ruins': 'stone', 'lighthouse_courtyard': 'stone'},
        'files': []}
    for surface, original in [('grass', 'Sand'), ('stone', 'Stone')]:
        for take in range(1, 4):
            for foot_index, foot in enumerate('LR'):
                name = f'Fantozzi-{original}{foot}{take}.flac'
                source = args.source_dir / name
                audio = prepare(source, surface)
                suffix = 'abcdef'[(take-1)*2 + foot_index]
                output = target / f'step_{surface}_{suffix}.wav'
                pcm = array('h', (round(max(-1, min(1, x)) * 32767) for x in audio))
                if sys.byteorder != 'little': pcm.byteswap()
                with wave.open(str(output), 'wb') as wav:
                    wav.setnchannels(1); wav.setsampwidth(2); wav.setframerate(RATE)
                    wav.writeframes(pcm.tobytes())
                manifest['files'].append({'path': output.relative_to(ROOT).as_posix(), 'source': name,
                    'source_sha256': hashlib.sha256(source.read_bytes()).hexdigest(),
                    'sha256': hashlib.sha256(output.read_bytes()).hexdigest(),
                    'foot': foot, 'take': take, 'surface': surface,
                    'seconds': len(audio)/RATE, 'peak': max(abs(x) for x in audio),
                    'rms': math.sqrt(sum(x*x for x in audio)/len(audio))})
    (ROOT / 'assets/audio/footsteps_manifest.json').write_text(json.dumps(manifest, indent=2, ensure_ascii=False)+'\n', encoding='utf-8')
    print('Imported 12 recorded, click-free grass/stone steps; music and other SFX untouched.')


if __name__ == '__main__': main()
