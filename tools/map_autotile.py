"""Deterministic four-direction terrain transition helper for Tiled maps.

The terrain atlas reserves one transparent edge-mask row for each material.
The compiler writes the selected mask and its rotation into static C arrays;
the game never parses TMX or runs Python at runtime.
"""
from __future__ import annotations

WIDTH, HEIGHT = 22, 18
MASK_ROW = {0: 2, 1: 5, 2: 8, 3: 11}


def material(atlas_index: int) -> int:
    row = atlas_index // 4
    if row >= 9:
        return 3  # water
    if row >= 6:
        return 2  # stone
    if row >= 3:
        return 1  # sand
    return 0      # grass


def _mask_for(overlay_material: int, base_material: int):
    # Draw the edge of the neighbouring material over the current base tile.
    if overlay_material == 0 and base_material in (1, 2):
        return MASK_ROW[0]
    if overlay_material == 1 and base_material in (2, 3):
        return MASK_ROW[1]
    if overlay_material == 2 and base_material in (0, 1):
        return MASK_ROW[2]
    if overlay_material == 3 and base_material in (0, 1):
        return MASK_ROW[3]
    return None


def build_overlay(flat_ground: list[int]):
    if len(flat_ground) != WIDTH * HEIGHT:
        raise ValueError("ground layer has incorrect dimensions")
    materials = [material(value) for value in flat_ground]
    overlay = [-1] * (WIDTH * HEIGHT)
    rotation = [0] * (WIDTH * HEIGHT)
    directions = (
        (0, -1, 0),
        (1, 0, 90),
        (0, 1, 180),
        (-1, 0, 270),
    )
    for y in range(HEIGHT):
        for x in range(WIDTH):
            index = y * WIDTH + x
            base = materials[index]
            for dx, dy, angle in directions:
                nx, ny = x + dx, y + dy
                if not (0 <= nx < WIDTH and 0 <= ny < HEIGHT):
                    continue
                adjacent = materials[ny * WIDTH + nx]
                row = _mask_for(adjacent, base)
                if row is None:
                    continue
                overlay[index] = row * 4 + 1
                rotation[index] = angle
                break
    return overlay, rotation
