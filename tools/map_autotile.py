"""Four-direction Tiled terrain transitions.

The terrain atlas contains transparent edge masks for grass, sand, stone and
water. One map cell can receive more than one edge mask (for example, at a
curved creek bend), so the compiler emits four independent overlay planes.
"""
from __future__ import annotations

WIDTH, HEIGHT = 18, 12
GRASS, SAND, STONE, WATER = range(4)
MASK_ROW = {GRASS: 2, SAND: 5, STONE: 8, WATER: 11}
PRIORITY = {GRASS: 0, SAND: 1, STONE: 2, WATER: 3}
# north, east, south, west; raylib positive rotation is clockwise.
DIRECTIONS = ((0, -1, 0), (1, 0, 90), (0, 1, 180), (-1, 0, 270))


def material(atlas_index: int) -> int:
    row = atlas_index // 4
    if row >= 9:
        return WATER
    if row >= 6:
        return STONE
    if row >= 3:
        return SAND
    return GRASS


def build_overlay(flat_ground: list[int]) -> list[list[int]]:
    """Return four flat overlay planes, with -1 meaning no edge at that side."""
    if len(flat_ground) != WIDTH * HEIGHT:
        raise ValueError("ground layer has incorrect dimensions")
    materials = [material(value) for value in flat_ground]
    overlays = [[-1] * (WIDTH * HEIGHT) for _ in DIRECTIONS]

    for y in range(HEIGHT):
        for x in range(WIDTH):
            index = y * WIDTH + x
            base = materials[index]
            for direction, (dx, dy, _angle) in enumerate(DIRECTIONS):
                nx, ny = x + dx, y + dy
                if not (0 <= nx < WIDTH and 0 <= ny < HEIGHT):
                    continue
                adjacent = materials[ny * WIDTH + nx]
                # Paint the higher-priority neighbouring material into the
                # current tile. This avoids double-thick borders on both sides.
                if adjacent == base or PRIORITY[adjacent] <= PRIORITY[base]:
                    continue
                row = MASK_ROW[adjacent]
                column = (x * 3 + y * 5 + direction) % 4
                overlays[direction][index] = row * 4 + column

    return overlays
