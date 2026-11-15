#!/usr/bin/env python3
"""Prepare support map atlases without generating or replacing character art.

All actors are authored PNG sheets. Missing final actors are reported explicitly
by prepare_runtime_assets; old geometric character fallbacks are never rebuilt.
"""
from pathlib import Path
from PIL import Image, ImageDraw
import random
from prepare_runtime_assets import ensure_assets

ROOT = Path(__file__).resolve().parents[1]
ensure_assets(ROOT)
TILES = ROOT / "assets/tiles/png"
TILES.mkdir(parents=True, exist_ok=True)
INK = (67, 65, 78, 255)
# Preserve any complete authored prop atlas already present in this checkout.
# Generation below is only needed on a source-only checkout missing these files.
try:
    with Image.open(TILES / "environment_props.png") as props_image:
        props_ready = props_image.size == (1024, 64)
    with Image.open(TILES / "island_iso_tiles.png") as tiles_image:
        tiles_ready = tiles_image.size == (384, 32)
    if props_ready and tiles_ready:
        print("Existing map atlases preserved. No character assets generated.")
        raise SystemExit(0)
except (FileNotFoundError, OSError):
    pass

# 6 variants of 64x32 isometric diamond ground tiles.
atlas=Image.new("RGBA",(384,32),(0,0,0,0));d=ImageDraw.Draw(atlas);random.seed(31)
colors=[(145,189,132,255),(160,200,141,255),(126,170,121,255),(197,174,131,255),(101,168,187,255),(138,149,134,255)]
for i,col in enumerate(colors):
    x=i*64;d.polygon([(x+32,0),(x+63,16),(x+32,31),(x,16)],fill=col,outline=(83,107,98,255))
    if i<3:
        for _ in range(12):
            px=x+random.randint(8,55);py=random.randint(7,24)
            if abs(px-(x+32))/32+abs(py-16)/16<.78:
                d.rectangle((px,py,px+1,py+1),fill=random.choice([(207,222,153,255),(95,143,96,255)]))
    elif i==3:d.line((x+12,16,x+23,11,x+34,17,x+47,12,x+55,16),fill=(230,208,165,255),width=2)
    elif i==4:
        d.line((x+9,12,x+22,12,x+34,13,x+48,12),fill=(174,226,219,255),width=2)
        d.line((x+8,21,x+20,21,x+32,22,x+46,21),fill=(174,226,219,255),width=2)
    else:d.line((x+13,9,x+24,9,x+24,15,x+42,15,x+42,23,x+52,23),fill=(91,108,99,255),width=2)
if not (TILES/"island_iso_tiles.png").exists():
    atlas.save(TILES/"island_iso_tiles.png")

# 8 original 64x64 pixel props: three trees, bush, tent, crate, sign, lighthouse.
props=Image.new("RGBA",(512,64),(0,0,0,0));d=ImageDraw.Draw(props)
def box(x,xy,fill,outline=None,width=1):d.rectangle((xy[0]+x,xy[1],xy[2]+x,xy[3]),fill=fill,outline=outline,width=width)
box(0,(28,39,36,62),(139,98,71,255),INK,2);box(0,(11,17,53,48),(111,166,109,255),INK,2);box(0,(17,8,47,35),(145,191,122,255),INK,2);box(0,(24,5,37,15),(181,212,138,255))
x=64;box(x,(28,39,36,62),(139,98,71,255),INK,2);box(x,(10,17,54,48),(215,142,170,255),INK,2);box(x,(16,8,48,35),(239,180,197,255),INK,2);box(x,(24,5,38,15),(247,211,214,255))
x=128;d.rectangle((x+28,42,x+36,62),fill=(139,98,71,255),outline=INK,width=2);d.polygon([(x+32,3),(x+8,34),(x+20,34),(x+6,51),(x+58,51),(x+44,34),(x+56,34)],fill=(79,139,105,255),outline=INK)
x=192;d.ellipse((x+6,29,x+58,60),fill=(94,152,104,255),outline=INK,width=2);d.ellipse((x+13,17,x+49,49),fill=(133,184,117,255),outline=INK,width=2)
for px,py in [(x+18,21),(x+42,17),(x+32,32)]:d.rectangle((px,py,px+6,py+6),fill=(246,194,208,255))
x=256;d.polygon([(x+4,58),(x+32,5),(x+60,58)],fill=(213,170,111,255),outline=INK);d.polygon([(x+32,5),(x+32,58),(x+60,58)],fill=(177,120,88,255),outline=INK);d.polygon([(x+21,58),(x+32,34),(x+44,58)],fill=(88,83,77,255))
x=320;d.rectangle((x+8,19,x+56,60),fill=(181,127,77,255),outline=INK,width=3);d.rectangle((x+14,25,x+50,54),fill=(211,158,97,255),outline=(126,88,61,255),width=2);d.line((x+14,25,x+50,54),fill=(126,88,61,255),width=3);d.line((x+50,25,x+14,54),fill=(126,88,61,255),width=3)
x=384;d.rectangle((x+29,31,x+36,62),fill=(138,101,72,255),outline=INK,width=2);d.rectangle((x+6,9,x+59,39),fill=(190,143,93,255),outline=INK,width=3);d.rectangle((x+13,16,x+52,32),fill=(226,190,130,255));d.line((x+20,24,x+45,24),fill=(125,94,67,255),width=3)
x=448;d.polygon([(x+17,60),(x+22,19),(x+43,19),(x+48,60)],fill=(240,228,201,255),outline=INK);d.rectangle((x+20,24,x+45,30),fill=(197,111,104,255));d.rectangle((x+22,40,x+43,46),fill=(197,111,104,255));d.rectangle((x+19,11,x+46,20),fill=(119,97,83,255),outline=INK,width=2);d.rectangle((x+25,3,x+40,12),fill=(255,217,125,255),outline=INK,width=2);d.rectangle((x+28,48,x+36,60),fill=(103,144,151,255))
props.save(TILES/"environment_props.png")
print("Prepared legacy terrain and prop atlases.")



# Extend the environment atlas with campsite props and generate the camp keeper.
base_props=Image.open(TILES/"environment_props.png").convert("RGBA")
extended=Image.new("RGBA",(768,64),(0,0,0,0))
extended.alpha_composite(base_props,(0,0))
d=ImageDraw.Draw(extended)
# 8: campfire with logs and warm flame.
x=512
d.ellipse((x+8,47,x+56,61),fill=(83,75,67,255),outline=INK,width=2)
d.polygon([(x+13,48),(x+25,39),(x+48,51),(x+39,56)],fill=(137,92,63,255),outline=INK)
d.polygon([(x+20,51),(x+31,37),(x+43,51)],fill=(177,117,70,255),outline=INK)
d.polygon([(x+24,42),(x+27,22),(x+33,30),(x+38,14),(x+43,43)],fill=(248,151,75,255),outline=(166,89,67,255))
d.polygon([(x+29,43),(x+34,28),(x+39,43)],fill=(255,222,115,255))
# 9: mailbox / little message post.
x=576
d.rectangle((x+29,30,x+35,62),fill=(135,99,71,255),outline=INK,width=2)
d.rectangle((x+9,11,x+55,37),fill=(182,123,83,255),outline=INK,width=2)
d.rectangle((x+15,17,x+49,31),fill=(228,195,147,255))
d.rectangle((x+38,7,x+54,13),fill=(194,91,91,255),outline=INK,width=1)
# 10: flower patch.
x=640
for px,py,col in [(15,39,(246,190,207,255)),(31,26,(255,220,128,255)),(47,40,(180,177,224,255))]:
    d.line((x+px,py+8,x+px,py+20),fill=(71,128,83,255),width=2)
    for dx,dy in [(-4,0),(4,0),(0,-4),(0,4)]:
        d.rectangle((x+px+dx-2,py+dy-2,x+px+dx+2,py+dy+2),fill=col)
    d.rectangle((x+px-2,py-2,x+px+2,py+2),fill=(255,231,152,255))
d.ellipse((x+5,50,x+59,62),fill=(81,141,86,255))
# 11: rounded shore rock.
x=704
d.polygon([(x+7,55),(x+12,31),(x+25,17),(x+43,21),(x+57,43),(x+52,59)],fill=(139,150,143,255),outline=INK)
d.polygon([(x+17,36),(x+27,22),(x+39,27),(x+32,43)],fill=(179,188,169,255))
d.line((x+39,29,x+48,43),fill=(100,118,111,255),width=2)
extended.save(TILES/"environment_props.png")

# Additional grove dressing: mushroom cluster, fallen log, rune stone, flower patch.
old_atlas=Image.open(TILES/"environment_props.png").convert("RGBA")
grove=Image.new("RGBA",(1024,64),(0,0,0,0))
grove.alpha_composite(old_atlas,(0,0))
gd=ImageDraw.Draw(grove)
# 12: mushroom cluster
x=768
gd.ellipse((x+4,43,x+60,62),fill=(81,125,79,255))
gd.rectangle((x+15,28,x+24,53),fill=(238,225,191,255),outline=INK,width=1)
gd.ellipse((x+8,15,x+31,34),fill=(220,119,128,255),outline=INK,width=2)
gd.ellipse((x+36,34,x+44,54),fill=(238,225,191,255),outline=INK,width=1)
gd.ellipse((x+30,25,x+51,40),fill=(240,184,132,255),outline=INK,width=2)
for px,py in [(x+17,20),(x+25,25),(x+39,30),(x+47,31)]:
    gd.rectangle((px,py,px+3,py+3),fill=(255,239,205,255))
# 13: fallen log with moss
x=832
gd.ellipse((x+5,39,x+60,59),fill=(111,145,97,255))
gd.rectangle((x+7,25,x+56,47),fill=(145,101,72,255),outline=INK,width=2)
gd.ellipse((x+3,25,x+18,47),fill=(205,164,121,255),outline=INK,width=2)
gd.ellipse((x+7,29,x+14,43),outline=(132,94,72,255),width=2)
gd.rectangle((x+25,19,x+42,28),fill=(106,164,98,255))
# 14: old rune stone
x=896
gd.polygon([(x+13,59),(x+9,25),(x+20,8),(x+47,10),(x+57,30),(x+51,59)],fill=(137,151,137,255),outline=INK)
gd.polygon([(x+20,27),(x+32,16),(x+44,28),(x+32,43)],outline=(222,205,142,255),width=3)
gd.line((x+32,23,x+32,49),fill=(222,205,142,255),width=2)
# 15: flower patch
x=960
gd.ellipse((x+4,48,x+60,62),fill=(87,143,87,255))
for px,py,col in [(x+15,30,(242,178,198,255)),(x+31,20,(255,220,130,255)),(x+47,32,(184,175,226,255))]:
    gd.line((px,py+5,px,py+20),fill=(71,128,83,255),width=2)
    gd.ellipse((px-7,py-2,px+7,py+10),fill=col,outline=INK,width=1)
    gd.ellipse((px-2,py+1,px+2,py+5),fill=(255,238,170,255))
grove.save(TILES/"environment_props.png")
print("Extended grove prop atlas:", TILES/"environment_props.png")
