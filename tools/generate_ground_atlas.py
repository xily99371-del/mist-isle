#!/usr/bin/env python3
"""Generate the shared seamless 56px pixel-art terrain atlas for all Tiled maps.

The atlas is baked during development/build. The game only loads the PNG; no
procedural image generation or Python runtime dependency is introduced.
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

GRASS, SAND, STONE, WATER = range(4)
PALETTES = {
    GRASS: ((59, 111, 62), (74, 132, 70), (91, 149, 79), (43, 91, 53)),
    SAND: ((184, 145, 85), (204, 163, 98), (222, 184, 120), (153, 116, 69)),
    STONE: ((91, 108, 102), (116, 131, 119), (139, 151, 133), (66, 83, 79)),
    WATER: ((23, 102, 143), (34, 127, 166), (59, 157, 187), (14, 78, 119)),
}
BASE_ROWS = {GRASS: (0, 1), SAND: (3, 4), STONE: (6, 7), WATER: (9, 10)}
MASK_ROWS = {GRASS: 2, SAND: 5, STONE: 8, WATER: 11}


def make_tile(material: int, variant: int) -> Image.Image:
    rng = random.Random(9100 + material * 100 + variant)
    base, light, highlight, shadow = PALETTES[material]
    image = Image.new("RGBA", (SIZE, SIZE), base + (255,))
    draw = ImageDraw.Draw(image)

    if material == GRASS:
        # Low-contrast grain and small tufts keep the ground readable beneath props.
        for _ in range(85):
            x, y = rng.randrange(SIZE), rng.randrange(SIZE)
            draw.point((x, y), fill=rng.choice((light, shadow, base, base)) + (255,))
        for _ in range(12):
            x, y = rng.randrange(3, 53), rng.randrange(3, 53)
            color = rng.choice((light, highlight))
            draw.line((x, y + 2, x - 1, y - 2), fill=color + (255,), width=1)
            draw.point((x + 2, y), fill=shadow + (255,))
        for _ in range(3):
            x, y = rng.randrange(5, 51), rng.randrange(5, 51)
            draw.ellipse((x, y, x + 2, y + 1), fill=shadow + (255,))
        if variant in (1, 5):
            for _ in range(2):
                x, y = rng.randrange(8, 48), rng.randrange(8, 48)
                draw.point((x, y), fill=(228, 215, 164, 255))

    elif material == SAND:
        for _ in range(120):
            x, y = rng.randrange(SIZE), rng.randrange(SIZE)
            color = rng.choice((light, highlight, shadow, base))
            draw.point((x, y), fill=color + (255,))
        for _ in range(7):
            x, y = rng.randrange(4, 52), rng.randrange(4, 52)
            draw.ellipse((x, y, x + 2, y + 1), fill=shadow + (255,))
            draw.point((x + 1, y - 1), fill=highlight + (255,))

    elif material == STONE:
        # Irregular paving stones with worn joints and small moss flecks.
        for row in range(4):
            y0 = row * 14
            shift = (row % 2) * 7 + rng.randrange(-1, 2)
            for col in range(5):
                x0 = col * 14 - shift
                x1 = x0 + 12 + rng.randrange(-1, 2)
                y1 = y0 + 11 + rng.randrange(-1, 2)
                if x1 < 0 or x0 >= SIZE:
                    continue
                color = rng.choice((light, highlight, base))
                draw.rounded_rectangle((x0 + 1, y0 + 1, x1, y1), radius=2,
                                       fill=color + (255,))
                draw.line((x0 + 3, y0 + 2, x1 - 2, y0 + 2),
                          fill=(151, 166, 147, 255), width=1)
                if rng.random() < 0.30:
                    draw.line((x0 + 5, y0 + 5, x0 + 7, y0 + 8),
                              fill=shadow + (255,), width=1)
        for _ in range(12):
            x, y = rng.randrange(SIZE), rng.randrange(SIZE)
            draw.point((x, y), fill=(76, 119, 75, 255))

    else:
        # Deep teal water: subtle color depth, broken wavelets and bright glints.
        pixels = image.load()
        for y in range(SIZE):
            for x in range(SIZE):
                value = rng.random()
                if value < 0.035:
                    color = shadow
                elif value < 0.12:
                    color = light
                elif value < 0.16:
                    color = highlight
                else:
                    color = base
                pixels[x, y] = color + (255,)
        draw = ImageDraw.Draw(image)
        for _ in range(13):
            x, y = rng.randrange(-5, 48), rng.randrange(3, 53)
            length = rng.randrange(7, 23)
            color = rng.choice((light, highlight, shadow))
            points = [
                (x, y),
                (x + length * 0.32, y + rng.choice((-1, 0, 1))),
                (x + length * 0.68, y + rng.choice((-1, 0, 1))),
                (x + length, y),
            ]
            draw.line(points, fill=color + (rng.randrange(130, 225),), width=1)
            if rng.random() < 0.45:
                draw.point((int(x + length * 0.55), y + 2),
                           fill=highlight + (220,))
        for _ in range(5):
            x, y = rng.randrange(SIZE), rng.randrange(SIZE)
            draw.point((x, y), fill=(151, 215, 220, 255))

    return image


def make_edge_mask(material: int, column: int) -> Image.Image:
    """A top-filled transparent edge tile; the renderer rotates it for each shore."""
    source = make_tile(material, 4 + column)
    src = source.load()
    result = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    dst = result.load()
    rng = random.Random(441 + material * 20 + column)
    boundary = []
    for x in range(SIZE):
        value = (15
                 + 2.8 * math.sin(x * 0.19 + column * 0.8)
                 + 1.9 * math.sin(x * 0.47 + material * 0.6)
                 + rng.uniform(-1.1, 1.1))
        boundary.append(max(10, min(21, int(value))))

    for x, edge_y in enumerate(boundary):
        for y in range(edge_y):
            dst[x, y] = src[x, y]
        # Short, broken highlights make the transition read as a shoreline,
        # rather than a hard rectangular color boundary.
        if material == WATER:
            for dx in (-1, 0, 1):
                xx = x + dx
                if 0 <= xx < SIZE and (x + column) % 4 != 0:
                    dst[xx, edge_y] = (174, 224, 223, 220)
        elif material == SAND and (x + column) % 5 == 0:
            dst[x, edge_y] = (224, 190, 129, 255)
        elif material == GRASS and (x + column) % 4 == 0:
            dst[x, edge_y] = (83, 139, 76, 255)
    return result


def generate(output: Path) -> None:
    atlas = Image.new("RGBA", (COLUMNS * SIZE, ROWS * SIZE), (0, 0, 0, 0))
    for material, rows in BASE_ROWS.items():
        for variant_row, row in enumerate(rows):
            for column in range(COLUMNS):
                tile = make_tile(material, variant_row * COLUMNS + column)
                atlas.alpha_composite(tile, (column * SIZE, row * SIZE))
    for material, row in MASK_ROWS.items():
        for column in range(COLUMNS):
            mask = make_edge_mask(material, column)
            atlas.alpha_composite(mask, (column * SIZE, row * SIZE))

    output.parent.mkdir(parents=True, exist_ok=True)
    atlas.save(output, optimize=True)
    print(f"Generated cohesive 56px terrain atlas: {output} ({atlas.width}x{atlas.height})")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--output", type=Path,
        default=Path(__file__).resolve().parents[1]
        / "assets/environment/topdown_terrain_atlas_v1.png",
    )
    generate(parser.parse_args().output)
