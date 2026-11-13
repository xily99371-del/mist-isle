"""Small deterministic terrain transition helper shared by map build and preview tools."""
from __future__ import annotations

WIDTH, HEIGHT = 22, 18
MASK_ROW = {0: 2, 1: 5, 2: 8, 3: 11}

def material(atlas_index: int) -> int:
    row = atlas_index // 4
    if row >= 9: return 3
    if row >= 6: return 2
    if row >= 3: return 1
    return 0

def _mask_for(overlay_material: int, base_material: int):
    # Overlay the organic edge of the neighbouring material onto the base tile.
    if overlay_material == 0 and base_material in (1, 2): return MASK_ROW[0]
    if overlay_material == 1 and base_material in (2, 3): return MASK_ROW[1]
    if overlay_material == 2 and base_material in (0, 1): return MASK_ROW[2]
    if overlay_material == 3 and base_material in (0, 1): return MASK_ROW[3]
    return None

def build_overlay(flat_ground: list[int]):
    if len(flat_ground) != WIDTH * HEIGHT:
        raise ValueError("ground layer has incorrect dimensions")
    mats = [material(v) for v in flat_ground]
    overlay = [-1] * (WIDTH * HEIGHT)
    rotation = [0] * (WIDTH * HEIGHT)
    # Direction order is stable so corners resolve identically on every platform.
    directions = [
        (0, -1, 0),   # neighbouring material above: mask's natural top edge
        (1, 0, 90),   # right
        (0, 1, 180),  # below
        (-1, 0, 270), # left
    ]
    for y in range(HEIGHT):
        for x in range(WIDTH):
            index = y * WIDTH + x
            base = mats[index]
            for dx, dy, angle in directions:
                nx, ny = x + dx, y + dy
                if not (0 <= nx < WIDTH and 0 <= ny < HEIGHT):
                    continue
                neighbour = mats[ny * WIDTH + nx]
                row = _mask_for(neighbour, base)
                if row is None:
                    continue
                # Middle edge cap is used for a clean transition; the mask is
                # rotated to face the adjacent material and remains transparent.
                overlay[index] = row * 4 + 1
                rotation[index] = angle
                break
    return overlay, rotation
