#!/usr/bin/env python3
"""Generate the shared 56px seamless ground atlas used by all four Tiled maps.

This is original procedural pixel art, not a runtime effect. The same PNG is
used by Windows and Raspberry Pi builds; the game only loads the finished atlas.
"""
from __future__ import annotations
import argparse
import math
import random
from pathlib import Path
from PIL import Image, ImageDraw

SIZE = 56
COLUMNS = 4
ROWS = 12

PALETTES = {
    0: ((67, 126, 72), (78, 143, 79), (93, 157, 88), (51, 105, 60)),   # grass
    1: ((196, 158, 96), (211, 175, 111), (226, 191, 129), (177, 137, 80)), # sand
    2: ((111, 126, 118), (131, 143, 133), (150, 157, 143), (82, 98, 91)), # stone
    3: ((30, 128, 166), (41, 151, 190), (76, 177, 203), (22, 100, 140)), # water
}
BASE_ROWS = {0: (0, 1), 1: (3, 4), 2: (6, 7), 3: (9, 10)}
MASK_ROWS = {0: 2, 1: 5, 2: 8, 3: 11}


def make_base(material: int, variant: int) -> Image.Image:
    rng = random.Random(1200 + material * 100 + variant)
    base, light, highlight, shadow = PALETTES[material]
    image = Image.new("RGBA", (SIZE, SIZE), base + (255,))
    draw = ImageDraw.Draw(image)

    if material == 0:
        # Fine grass grain, small directional tufts and restrained flower flecks.
        for _ in range(92):
            x, y = rng.randrange(SIZE), rng.randrange(SIZE)
            color = rng.choice((light, highlight, shadow))
            w = rng.choice((1, 1, 2))
            h = rng.choice((1, 2))
            draw.rectangle((x, y, min(SIZE-1, x+w-1), min(SIZE-1, y+h-1)), fill=color + (255,))
        for _ in range(12):
            x, y = rng.randrange(4, SIZE-4), rng.randrange(4, SIZE-4)
            color = rng.choice((light, highlight))
            draw.line((x, y+2, x-1, y-1), fill=color + (255,), width=1)
            draw.point((x+2, y), fill=color + (255,))
        if variant in (2, 5):
            for _ in range(2):
                x, y = rng.randrange(8, SIZE-8), rng.randrange(8, SIZE-8)
                draw.point((x, y), fill=(244, 229, 185, 255))
                draw.point((x+1, y-1), fill=(255, 246, 213, 255))

    elif material == 1:
        # Warm sand grains and tiny weathered pebbles.
        for _ in range(145):
            x, y = rng.randrange(SIZE), rng.randrange(SIZE)
            color = rng.choice((light, highlight, shadow))
            draw.point((x, y), fill=color + (255,))
        for _ in range(5):
            x, y = rng.randrange(5, SIZE-5), rng.randrange(5, SIZE-5)
            draw.ellipse((x, y, x+2, y+2), fill=shadow + (255,))
            draw.point((x+1, y-1), fill=highlight + (255,))

    elif material == 2:
        # Broad worn paving stones. Joints stop short of tile borders to avoid
        # drawing a hard square outline around every 56px cell.
        for row in range(4):
            y0 = row * 14
            shift = (row % 2) * 7
            for col in range(5):
                x0 = col * 14 - shift
                x1, y1 = x0 + 12, y0 + 12
                if x1 < 0 or x0 >= SIZE:
                    continue
                color = rng.choice((light, highlight, base))
                draw.rounded_rectangle((x0+1, y0+1, x1, y1), radius=2, fill=color + (255,))
                draw.line((x0+3, y0+2, x1-2, y0+2), fill=(170, 178, 159, 255), width=1)
                if rng.random() < .24:
                    draw.line((x0+5, y0+5, x0+7, y0+8), fill=shadow + (255,), width=1)
        for _ in range(10):
            x, y = rng.randrange(SIZE), rng.randrange(SIZE)
            draw.point((x, y), fill=(105, 145, 92, 255))

    else:
        # Layered, low-contrast wave bands with no square foam border.
        for _ in range(64):
            x, y = rng.randrange(SIZE), rng.randrange(SIZE)
            draw.point((x, y), fill=rng.choice((light, highlight, shadow)) + (255,))
        for row in range(4):
            y = 8 + row * 14 + rng.randrange(-2, 3)
            x0 = rng.randrange(-10, 10)
            draw.line((x0, y, x0+26, y-1, x0+50, y+1), fill=(143, 218, 225, 190), width=1)
            draw.line((x0+6, y+3, x0+24, y+2), fill=(20, 104, 146, 180), width=1)

    return image


def make_edge_mask(material: int, column: int) -> Image.Image:
    base = make_base(material, 5 + column)
    source = base.load()
    result = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    target = result.load()
    boundary = [
        15 + int(2.2 * math.sin(x * .25 + column * .8)
                 + 1.5 * math.sin(x * .61 + material * .9))
        for x in range(SIZE)
    ]
    for x in range(SIZE):
        for y in range(SIZE):
            distance = boundary[x] - y
            if distance >= 2:
                target[x, y] = source[x, y]
            elif distance >= -2 and ((x*7 + y*11 + material*3 + column) % 5) < max(1, distance+3):
                target[x, y] = source[x, y]
    return result


def generate(output: Path) -> None:
    atlas = Image.new("RGBA", (COLUMNS*SIZE, ROWS*SIZE), (0, 0, 0, 0))
    for material, rows in BASE_ROWS.items():
        for variant_index, row in enumerate(rows):
            for column in range(COLUMNS):
                tile = make_base(material, variant_index*COLUMNS + column)
                atlas.alpha_composite(tile, (column*SIZE, row*SIZE))
    for material, row in MASK_ROWS.items():
        for column in range(COLUMNS):
            mask = make_edge_mask(material, column)
            atlas.alpha_composite(mask, (column*SIZE, row*SIZE))
    output.parent.mkdir(parents=True, exist_ok=True)
    atlas.save(output, optimize=True)
    print(f"Generated seamless 56px ground atlas: {output} ({atlas.width}x{atlas.height})")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", type=Path,
                        default=Path(__file__).resolve().parents[1] / "assets/environment/topdown_terrain_atlas_v1.png")
    args = parser.parse_args()
    generate(args.output)
