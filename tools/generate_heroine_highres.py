#!/usr/bin/env python3
"""Generate the high-resolution, native-pixel heroine exploration sprite sheet."""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "assets/characters/png/heroine_world_4x4_64x80.png"
OUT.parent.mkdir(parents=True, exist_ok=True)

P = {
    "outline": (47,35,34,255), "hair": (73,43,39,255), "hair2": (91,53,44,255),
    "hair_hi": (132,83,62,255), "hair_light": (157,105,77,255),
    "skin": (241,198,164,255), "skin_hi": (255,221,190,255), "blush": (222,137,126,255),
    "coat": (239,226,202,255), "coat_hi": (251,241,220,255), "coat_sh": (190,169,139,255),
    "teal": (42,128,132,255), "teal_hi": (100,185,177,255), "teal_sh": (31,91,99,255),
    "leather": (117,70,46,255), "leather_hi": (166,108,65,255), "leather_sh": (75,48,38,255),
    "boot": (73,49,42,255), "boot_hi": (149,91,53,255), "gold": (238,184,89,255),
    "shadow": (20,34,35,100)
}

def R(draw, box, color):
    draw.rectangle(box, fill=P[color])

def draw_frame(direction, frame):
    im = Image.new("RGBA", (64, 80), (0,0,0,0))
    d = ImageDraw.Draw(im)
    d.ellipse((12,73,52,78), fill=P["shadow"])
    step = (-3,0,3,0)[frame]
    if direction == "back":
        R(d,(19+step,63,29+step,73),"leather"); R(d,(35-step,63,45-step,73),"leather")
    else:
        R(d,(20+step,61,30+step,73),"coat_sh"); R(d,(34-step,61,44-step,73),"coat_sh")
    R(d,(16+step,70,30+step,77),"boot"); R(d,(34-step,70,48-step,77),"boot")
    R(d,(18+step,70,27+step,72),"boot_hi"); R(d,(37-step,70,45-step,72),"boot_hi")
    if direction in ("front","left","right"):
        R(d,(8,39,19,62),"leather_sh"); R(d,(9,41,18,58),"leather")
        R(d,(10,43,16,47),"leather_hi"); R(d,(8,54,18,58),"leather_sh"); R(d,(17,37,22,43),"gold")
    R(d,(19,34,45,62),"outline"); R(d,(21,35,43,60),"coat")
    R(d,(18,39,46,56),"coat_sh"); R(d,(21,37,43,58),"coat_hi")
    R(d,(31,38,33,53),"coat_sh"); R(d,(21,54,43,59),"leather")
    R(d,(29,54,35,60),"gold"); R(d,(31,55,33,58),"leather_sh")
    R(d,(22,57,27,61),"coat_sh"); R(d,(37,57,42,61),"coat_sh")
    arm = (-2,0,2,0)[frame]
    R(d,(13,39+arm,20,55+arm),"outline"); R(d,(14,40+arm,19,53+arm),"coat")
    R(d,(44,39-arm,51,55-arm),"outline"); R(d,(45,40-arm,50,53-arm),"coat")
    R(d,(14,53+arm,19,59+arm),"skin"); R(d,(45,53-arm,50,59-arm),"skin")
    R(d,(25,30,39,39),"teal_sh"); R(d,(24,31,40,36),"teal"); R(d,(26,30,37,33),"teal_hi")
    R(d,(38,34,43,48),"teal"); R(d,(39,35,41,43),"teal_hi"); R(d,(28,27,36,34),"skin")
    R(d,(18,9,46,33),"outline"); R(d,(20,11,44,31),"skin")
    R(d,(17,17,21,26),"skin"); R(d,(43,17,47,26),"skin")
    if direction == "front":
        R(d,(24,19,27,23),"outline"); R(d,(37,19,40,23),"outline")
        R(d,(24,19,25,20),"skin_hi"); R(d,(37,19,38,20),"skin_hi")
        R(d,(30,26,34,27),"skin_hi"); R(d,(29,28,35,29),"blush")
        R(d,(21,24,25,26),"blush"); R(d,(39,24,43,26),"blush")
    elif direction == "left":
        R(d,(22,20,25,24),"outline"); R(d,(22,20,23,21),"skin_hi")
        R(d,(27,27,31,28),"skin_hi"); R(d,(26,29,30,30),"blush")
    elif direction == "right":
        R(d,(39,20,42,24),"outline"); R(d,(40,20,41,21),"skin_hi")
        R(d,(33,27,37,28),"skin_hi"); R(d,(34,29,38,30),"blush")
    else:
        R(d,(22,18,42,30),"hair2"); R(d,(26,20,38,26),"hair")
    R(d,(15,7,48,17),"outline"); R(d,(18,5,45,14),"hair")
    R(d,(22,3,41,10),"hair2"); R(d,(26,2,38,6),"hair_hi")
    R(d,(18,11,23,25),"hair"); R(d,(42,10,47,26),"hair")
    R(d,(21,8,28,12),"hair_hi"); R(d,(31,6,38,10),"hair_light")
    if direction == "front":
        R(d,(18,11,29,16),"hair"); R(d,(24,13,36,17),"hair2")
        R(d,(34,11,44,16),"hair"); R(d,(30,14,37,18),"hair_hi")
    elif direction == "left":
        R(d,(18,10,33,17),"hair"); R(d,(20,14,29,20),"hair2"); R(d,(28,12,37,15),"hair_hi")
    elif direction == "right":
        R(d,(31,10,46,17),"hair"); R(d,(35,14,44,20),"hair2"); R(d,(27,12,36,15),"hair_hi")
    else:
        R(d,(17,12,47,29),"hair"); R(d,(21,15,43,26),"hair2"); R(d,(24,14,31,18),"hair_hi")
    R(d,(46,22,52,28),"teal"); R(d,(48,27,53,36),"hair")
    R(d,(49,35,54,42),"hair2"); R(d,(47,41,53,46),"hair"); R(d,(48,30,51,33),"hair_hi")
    R(d,(34,37,38,41),"gold"); R(d,(35,38,37,40),"coat_hi"); R(d,(11,48,15,52),"gold")
    return im

sheet = Image.new("RGBA", (256,320), (0,0,0,0))
for row, direction in enumerate(("front","left","right","back")):
    for col in range(4):
        sheet.alpha_composite(draw_frame(direction,col), (col*64,row*80))
sheet.save(OUT, optimize=True)
print(f"Generated {OUT.relative_to(ROOT)} ({sheet.width}x{sheet.height}, 4x4 cells at 64x80)")
