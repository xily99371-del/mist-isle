#!/usr/bin/env python3
"""Build the game's original, short PCM sound effects (stdlib + ffmpeg for energy).

The WAV files are committed and ready to play; players do not need this script.
Music is supplied separately by the project owner, never synthesized here.
"""
from array import array
import math
from pathlib import Path
import random
import subprocess
import sys
import wave

ROOT = Path(__file__).resolve().parents[1]
SFX = ROOT / "assets/audio/sfx"
RATE = 22050


def write(name, seconds, make, peak=0.43):
    rng = random.Random(name)
    samples = []
    memory = [0.0, 0.0]
    for i in range(round(seconds * RATE)):
        t = i / RATE
        noise = rng.uniform(-1.0, 1.0)
        memory[0] += 0.20 * (noise - memory[0])
        memory[1] += 0.06 * (noise - memory[1])
        value = make(t, noise, memory[0], memory[1])
        edge = min(1.0, t / 0.003, (seconds - t) / 0.010)
        samples.append(value * max(0.0, edge))
    max_value = max(abs(x) for x in samples) or 1.0
    pcm = array("h", (round(32767 * peak * x / max_value) for x in samples))
    if sys.byteorder != "little":
        pcm.byteswap()
    with wave.open(str(SFX / f"{name}.wav"), "wb") as out:
        out.setnchannels(1)
        out.setsampwidth(2)
        out.setframerate(RATE)
        out.writeframes(pcm.tobytes())


def note(t, frequency, decay):
    return (math.sin(math.tau * frequency * t) +
            0.20 * math.sin(math.tau * frequency * 2.003 * t)) * math.exp(-t * decay)


def main():
    SFX.mkdir(parents=True, exist_ok=True)
    for variation in range(2):
        suffix = "ab"[variation]
        # Recorded grass/stone footsteps are maintained by import_footsteps.py.
        # Never overwrite those approved recordings with synthesized impacts.
        pitch = 1 + 0.07 * variation
        write(f"step_wet_{suffix}", 0.19,
              lambda t, n, lo, bass: lo * math.exp(-23 * t) *
              (0.35 + 0.22 * math.sin(290 * t)**2) + 0.15 * note(t, 115 * pitch, 45), 0.32)
    write("dialogue", 0.11,
          lambda t, n, lo, bass: 0.4 * note(t, 720, 43) + 0.13 * note(t, 1080, 57), 0.24)
    write("interact", 0.25,
          lambda t, n, lo, bass: 0.30 * note(t, 523.25, 20) +
          (0.23 * note(t - .045, 783.99, 22) if t >= .045 else 0), 0.30)
    write("collision", 0.18,
          lambda t, n, lo, bass: 0.45 * note(t, 83 - t * 80, 31) +
          0.20 * bass * math.exp(-22 * t), 0.45)
    write("dash", 0.26,
          lambda t, n, lo, bass: lo * math.sin(math.pi * t / .26)**1.5 *
          (0.5 + 0.5 * math.cos(110 * t)), 0.28)
    write("pickup", 0.45,
          lambda t, n, lo, bass: sum(0.24 * note(t - off, freq, 15)
          for off, freq in [(0, 659.25), (.07, 987.77), (.14, 1318.51)] if t >= off), 0.31)
    write("attack", 0.24,
          lambda t, n, lo, bass: 0.38 * lo * math.exp(-21 * t) +
          0.22 * note(t, 260 - 480 * t, 25), 0.43)
    write("hurt", 0.28,
          lambda t, n, lo, bass: 0.42 * bass * math.exp(-18 * t) +
          0.28 * note(t, 130 - 190 * t, 21), 0.43)
    write("victory", 0.72,
          lambda t, n, lo, bass: sum(0.18 * note(t - off, freq, 9)
          for off, freq in [(0, 523.25), (.06, 659.25), (.12, 783.99), (.20, 1046.50)] if t >= off), 0.28)
    print(f"Generated 10 original PCM sound effects; recorded footsteps preserved in {SFX.relative_to(ROOT)}")

    enemy = ROOT / "assets/audio/music/enemy.mp3"
    if not enemy.exists():
        return
    # A 20 Hz offline RMS envelope makes the edge-light pulse follow the song,
    # with no FFT, audio callback or whole-song PCM allocation during gameplay.
    raw = subprocess.check_output([
        "ffmpeg", "-v", "error", "-i", str(enemy), "-f", "f32le",
        "-ar", "8000", "-ac", "1", "pipe:1"
    ])
    pcm = array("f")
    pcm.frombytes(raw)
    if sys.byteorder != "little":
        pcm.byteswap()
    levels = [math.sqrt(sum(x*x for x in pcm[i:i+400]) / len(pcm[i:i+400]))
              for i in range(0, len(pcm), 400)]
    ordered = sorted(levels)
    low, high = ordered[len(ordered)//10], ordered[int(len(ordered)*.96)]
    values = [max(0, min(1, (x-low)/max(.0001, high-low))) for x in levels]
    (ROOT / "assets/audio/enemy_energy.tsv").write_text(
        "# Normalized Enemy RMS envelope; 20 samples/second, first sample at 0s.\n" +
        "\n".join(f"{value:.4f}" for value in values) + "\n", encoding="utf-8")
    print(f"Generated {len(values)} song-synchronized envelope samples")


if __name__ == "__main__":
    main()
