#!/usr/bin/env python3
"""Generate the native-resolution pixel art used by Mist Isle.
Requires Pillow: python -m pip install pillow
"""
from pathlib import Path
from PIL import Image, ImageDraw
import math, random

ROOT=Path(__file__).resolve().parents[1]
CHAR=ROOT/"assets/characters/png"
TILES=ROOT/"assets/tiles/png"
CHAR.mkdir(parents=True,exist_ok=True); TILES.mkdir(parents=True,exist_ok=True)
INK=(67,65,78,255); SKIN=(250,211,177,255); HAIR=(229,191,154,255)
COAT=(238,226,192,255); SCARF=(194,91,91,255); BOOTS=(104,77,69,255)
BLUE=(100,171,202,255); BLUEHI=(169,222,230,255)
GREEN=(133,185,112,255); GREENHI=(206,229,157,255)
PURPLE=(139,119,196,255); GEM=(119,218,233,255); GOLD=(255,216,112,255)

def hero_frame(direction,step):
    im=Image.new("RGBA",(32,32),(0,0,0,0));d=ImageDraw.Draw(im)
    d.ellipse((8,27,24,30),fill=(55,76,65,90))
    if step%2==0: d.rectangle((10,23,14,27),fill=BOOTS);d.rectangle((18,23,21,27),fill=BOOTS)
    else: d.rectangle((9,23,13,27),fill=BOOTS);d.rectangle((19,23,23,27),fill=BOOTS)
    d.rectangle((6,14,10,22),fill=(139,103,75,255));d.rectangle((7,15,9,19),fill=(185,142,94,255))
    d.rectangle((10,15,22,24),fill=COAT);d.rectangle((9,17,23,22),fill=(194,177,146,255))
    d.rectangle((11,15,21,20),fill=COAT);d.rectangle((11,13,21,16),fill=SCARF);d.rectangle((20,15,23,18),fill=SCARF)
    d.rectangle((10,5,22,15),fill=SKIN);d.rectangle((9,7,23,13),fill=SKIN)
    d.rectangle((9,4,22,8),fill=HAIR);d.rectangle((8,7,11,14),fill=HAIR);d.rectangle((21,7,24,13),fill=HAIR)
    d.rectangle((10,4,21,6),fill=(250,222,185,255))
    d.rectangle((8,3,23,6),fill=(157,113,89,255));d.rectangle((10,1,21,4),fill=(206,157,119,255))
    d.rectangle((11,1,17,3),fill=(97,86,80,255));d.rectangle((12,1,16,2),fill=GOLD)
    if direction=="up":
        d.rectangle((11,8,21,12),fill=HAIR);d.rectangle((12,10,20,12),fill=SKIN)
    elif direction=="left":
        d.rectangle((10,9,11,11),fill=INK);d.point((12,9),fill=(255,231,203,255))
    elif direction=="right":
        d.rectangle((21,9,22,11),fill=INK);d.point((19,9),fill=(255,231,203,255))
    else:
        d.rectangle((12,9,13,11),fill=INK);d.rectangle((19,9,20,11),fill=INK)
        d.point((13,8),fill=(255,231,203,255));d.point((20,8),fill=(255,231,203,255))
        d.line((15,12,18,12),fill=(175,103,95,255))
    d.rectangle((8,17,10,20),fill=SKIN);d.rectangle((22,17,24,20),fill=SKIN)
    d.rectangle((23,18,25,21),fill=GOLD);d.point((24,17),fill=(255,237,164,255))
    if step%2:
        shifted=Image.new("RGBA",(32,32),(0,0,0,0));shifted.alpha_composite(im,(0,-1));im=shifted
    return im

hero=Image.new("RGBA",(128,128),(0,0,0,0))
for row,direction in enumerate(("down","left","right","up")):
    for col in range(4): hero.alpha_composite(hero_frame(direction,col),(col*32,row*32))
hero.save(CHAR/"hero_walk_4dir.png")

def buddy_frame(kind,frame):
    im=Image.new("RGBA",(32,32),(0,0,0,0));d=ImageDraw.Draw(im);bob=1 if frame%2 else 0
    d.ellipse((7,27,25,30),fill=(50,70,70,75))
    if kind=="lumi":
        d.polygon([(14,9),(7,3),(11,12)],fill=(104,163,95,255),outline=INK)
        d.polygon([(17,9),(25,3),(21,13)],fill=(130,185,105,255),outline=INK)
        d.ellipse((7,9-bob,25,27-bob),fill=GREEN,outline=INK,width=1)
        d.ellipse((10,12-bob,22,24-bob),fill=GREENHI)
        d.ellipse((11,16-bob,13,19-bob),fill=INK);d.ellipse((19,16-bob,21,19-bob),fill=INK)
        d.point((12,16-bob),fill="white");d.point((20,16-bob),fill="white")
        d.ellipse((14,20-bob,18,22-bob),fill=(229,140,135,255))
        d.ellipse((13,4-bob,19,11-bob),fill=GOLD,outline=(185,143,74,255))
    elif kind=="moatail":
        d.ellipse((6,11-bob,26,27-bob),fill=BLUE,outline=INK,width=1)
        d.ellipse((10,13-bob,22,25-bob),fill=BLUEHI)
        d.ellipse((11,16-bob,13,19-bob),fill=INK);d.ellipse((19,16-bob,21,19-bob),fill=INK)
        d.ellipse((14,21-bob,18,23-bob),fill=(185,103,115,255))
        d.polygon([(8,12),(7,6),(13,11)],fill=BLUEHI,outline=INK)
        d.polygon([(19,11),(25,6),(24,14)],fill=BLUEHI,outline=INK)
        d.arc((20,19-bob,31,29-bob),190,310,fill=BLUE,width=3)
    else:
        d.ellipse((6,12-bob,26,27-bob),fill=PURPLE,outline=INK,width=1)
        d.ellipse((10,14-bob,22,25-bob),fill=(181,164,221,255))
        d.polygon([(8,14),(7,5),(14,12)],fill=GEM,outline=INK)
        d.polygon([(17,12),(24,4),(24,15)],fill=GEM,outline=INK)
        d.ellipse((11,17-bob,13,20-bob),fill=INK);d.ellipse((19,17-bob,21,20-bob),fill=INK)
        d.line((24,22-bob,29,19-bob,27,25-bob),fill=GOLD,width=2)
    return im

for kind,name in (("lumi","lumi_idle_pixel.png"),("moatail","moatail_idle_pixel.png"),("gecko","gecko_idle_pixel.png")):
    sheet=Image.new("RGBA",(64,32),(0,0,0,0))
    sheet.alpha_composite(buddy_frame(kind,0),(0,0));sheet.alpha_composite(buddy_frame(kind,1),(32,0))
    sheet.save(CHAR/name)

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
print("Generated pixel art:", *(str(p.relative_to(ROOT)) for p in [CHAR/"hero_walk_4dir.png",CHAR/"lumi_idle_pixel.png",CHAR/"moatail_idle_pixel.png",CHAR/"gecko_idle_pixel.png",TILES/"island_iso_tiles.png",TILES/"environment_props.png"]), sep="\n - ")
