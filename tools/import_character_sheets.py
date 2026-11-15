#!/usr/bin/env python3
"""Import authored four-direction sheets into the game's 64x80 frame contract.

This does not draw character art. It cuts the original transparent sprites at
empty gutters, applies one scale per actor, and aligns every foot to y=77.
The authored source files remain unchanged and are never needed at game launch.
"""
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
CELL_W, CELL_H, BASELINE = 64, 80, 77
SHEETS = (
    ("heroine_walk_source.png", "source/v9/candidates/heroine_walk_v9.png"),
    ("watcher_walk_source.png", "source/v9/candidates/watcher_walk_v9.png"),
)


def gutter_cuts(mask, axis, count=4):
    """Find empty gutters near each expected quarter; do not crop at guesses."""
    width, height = mask.size
    length = width if axis == 0 else height
    counts = []
    for position in range(length):
        strip = mask.crop((position, 0, position + 1, height) if axis == 0
                          else (0, position, width, position + 1))
        counts.append(strip.histogram()[255])
    cuts = [0]
    for quarter in range(1, count):
        center = round(length * quarter / count)
        radius = round(length / count * .20)
        begin, end = max(1, center - radius), min(length - 1, center + radius)
        candidates = [i for i in range(begin, end) if counts[i] == 0]
        if not candidates:
            raise ValueError(f"No empty gutter near {axis=}, {quarter=}; inspect source layout")
        runs, run = [], [candidates[0]]
        for candidate in candidates[1:]:
            if candidate == run[-1] + 1:
                run.append(candidate)
            else:
                runs.append(run)
                run = [candidate]
        runs.append(run)
        best = max(runs, key=lambda r: (len(r), -abs((r[0] + r[-1]) / 2 - center)))
        cuts.append((best[0] + best[-1]) // 2)
    cuts.append(length)
    return cuts


def import_sheet(source, destination):
    with Image.open(source) as opened:
        original = opened.convert("RGBA")
    # Discard near-transparent edge speckles when finding gutters and bounds.
    mask = original.getchannel("A").point(lambda alpha: 255 if alpha >= 96 else 0)
    rows = gutter_cuts(mask, 1)
    frames = []
    for row in range(4):
        row_mask = mask.crop((0, rows[row], original.width, rows[row + 1]))
        columns = gutter_cuts(row_mask, 0)
        for column in range(4):
            region = (columns[column], rows[row], columns[column + 1], rows[row + 1])
            frame = original.crop(region)
            visible = mask.crop(region).getbbox()
            if visible is None:
                raise ValueError(f"Missing frame {row=}, {column=} in {source}")
            frames.append(frame.crop(visible))
    scale = min(56 / max(f.width for f in frames), 72 / max(f.height for f in frames))
    sheet = Image.new("RGBA", (CELL_W * 4, CELL_H * 4))
    for index, frame in enumerate(frames):
        size = (round(frame.width * scale), round(frame.height * scale))
        resized = frame.resize(size, Image.Resampling.NEAREST)
        # The runtime sheet has crisp alpha, avoiding pale fringes on dark maps.
        resized.putalpha(resized.getchannel("A").point(lambda alpha: 255 if alpha >= 96 else 0))
        x = (index % 4) * CELL_W + (CELL_W - size[0]) // 2
        y = (index // 4) * CELL_H + BASELINE - size[1]
        sheet.alpha_composite(resized, (x, y))
    destination.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(destination, optimize=True)
    print(f"Imported {destination.relative_to(ROOT)}: 4x4 frames, 64x80 pixels, foot baseline {BASELINE}")


def import_companion(source, destination):
    """Turn four authored idle poses (2x2 source) into a 4x1 runtime strip."""
    with Image.open(source) as opened:
        original = opened.convert("RGBA")
    mask = original.getchannel("A").point(lambda alpha: 255 if alpha >= 96 else 0)
    rows = gutter_cuts(mask, 1, 2)
    frames = []
    for row in range(2):
        row_mask = mask.crop((0, rows[row], original.width, rows[row + 1]))
        columns = gutter_cuts(row_mask, 0, 2)
        for column in range(2):
            region = (columns[column], rows[row], columns[column + 1], rows[row + 1])
            visible = mask.crop(region).getbbox()
            if visible is None:
                raise ValueError(f"Missing companion pose {row=}, {column=}")
            frames.append(original.crop(region).crop(visible))
    scale = min(56 / max(f.width for f in frames), 56 / max(f.height for f in frames))
    strip = Image.new("RGBA", (256, 64))
    for index, frame in enumerate(frames):
        size = (round(frame.width * scale), round(frame.height * scale))
        resized = frame.resize(size, Image.Resampling.NEAREST)
        resized.putalpha(resized.getchannel("A").point(lambda alpha: 255 if alpha >= 96 else 0))
        strip.alpha_composite(resized, (index * 64 + (64 - size[0]) // 2, 60 - size[1]))
    destination.parent.mkdir(parents=True, exist_ok=True)
    strip.save(destination, optimize=True)
    print(f"Imported {destination.relative_to(ROOT)}: 4 idle poses, 64x64 pixels, foot baseline 60")


if __name__ == "__main__":
    for source_name, destination_name in SHEETS:
        import_sheet(ROOT / "assets/characters/source/v9" / source_name,
                     ROOT / "assets/characters" / destination_name)

    for name in ("moss_lumi", "moatail", "crystal_gecko"):
        import_companion(ROOT / "assets/characters/source/v9" / (name + "_idle_source.png"),
                         ROOT / "assets/characters/source/v9/candidates" / (name + "_idle_v9.png"))
