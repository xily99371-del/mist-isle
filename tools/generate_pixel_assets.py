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
print("Generated pixel art:", *(str(p.relative_to(ROOT)) for p in [CHAR/"hero_walk_4dir.png",CHAR/"lumi_idle_pixel.png",CHAR/"moatail_idle_pixel.png",CHAR/"gecko_idle_pixel.png",TILES/"island_iso_tiles.png",TILES/"environment_props.png"]), sep="\n - ")



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

# Camp keeper: a friendly, elderly island caretaker in a green cap and apron.
keeper=Image.new("RGBA",(32,32),(0,0,0,0));kd=ImageDraw.Draw(keeper)
keeper_ink=(67,65,78,255); keeper_skin=(239,197,160,255); keeper_hair=(224,222,201,255)
keeper_coat=(111,151,116,255); keeper_apron=(229,215,183,255); keeper_brown=(123,91,69,255)
kd.ellipse((7,27,25,30),fill=(55,76,65,90))
kd.rectangle((10,16,22,25),fill=keeper_coat,outline=keeper_ink)
kd.rectangle((12,17,20,24),fill=keeper_apron)
kd.rectangle((9,22,13,27),fill=keeper_brown);kd.rectangle((19,22,23,27),fill=keeper_brown)
kd.rectangle((9,7,23,17),fill=keeper_skin,outline=keeper_ink)
kd.rectangle((8,9,10,16),fill=keeper_hair);kd.rectangle((22,9,24,16),fill=keeper_hair)
kd.rectangle((8,5,24,9),fill=keeper_hair);kd.rectangle((10,3,22,6),fill=keeper_hair)
kd.rectangle((7,3,25,6),fill=(92,119,91,255),outline=keeper_ink)
kd.rectangle((11,2,21,4),fill=(157,180,126,255))
kd.point((12,11),fill=keeper_ink);kd.point((20,11),fill=keeper_ink)
kd.line((14,14,18,14),fill=(166,101,88,255),width=1)
kd.rectangle((6,18,9,22),fill=keeper_skin);kd.rectangle((23,18,26,22),fill=keeper_skin)
keeper.save(CHAR/"camp_keeper.png")
print("Generated campsite assets:", TILES/"environment_props.png", CHAR/"camp_keeper.png")



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


# Watcher world actor: a native pixel RPG sprite sheet, not a rasterized portrait.
# Layout matches the traveller: 4 columns x 4 directions, 64x80 cells.
WATCHER_OUT = ROOT / "assets/characters/npc/watcher_pixel_idle_64x80.png"
WATCHER_OUT.parent.mkdir(parents=True, exist_ok=True)
WP = {
    "ink": (48, 39, 36, 255), "ink2": (74, 57, 47, 255),
    "skin": (226, 178, 139, 255), "skin_hi": (247, 207, 169, 255),
    "hair": (179, 132, 91, 255), "hair_hi": (220, 174, 119, 255),
    "cap": (91, 105, 62, 255), "cap_hi": (139, 151, 87, 255),
    "leaf": (133, 163, 81, 255), "cream": (238, 224, 194, 255),
    "cream_hi": (255, 242, 215, 255), "cream_sh": (190, 169, 137, 255),
    "vest": (104, 69, 48, 255), "vest_hi": (151, 103, 67, 255),
    "leather": (91, 60, 45, 255), "leather_hi": (153, 104, 68, 255),
    "green": (91, 130, 87, 255), "green_hi": (145, 177, 115, 255),
    "pants": (105, 78, 61, 255), "pants_hi": (147, 111, 81, 255),
    "boot": (65, 48, 40, 255), "boot_hi": (129, 87, 57, 255),
    "gold": (221, 174, 91, 255), "shadow": (26, 42, 37, 105),
}
def watcher_frame(direction, frame):
    im = Image.new("RGBA", (64, 80), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    walk = (-2, 0, 2, 0)[frame]
    arm = (-2, 0, 2, 0)[frame]
    d.ellipse((12, 73, 52, 78), fill=WP["shadow"])
    # Boots and trousers share a fixed ground baseline in all directions.
    d.rectangle((19 + walk, 61, 29 + walk, 72), fill=WP["pants"], outline=WP["ink"])
    d.rectangle((35 - walk, 61, 45 - walk, 72), fill=WP["pants"], outline=WP["ink"])
    d.rectangle((17 + walk, 69, 31 + walk, 77), fill=WP["boot"], outline=WP["ink"])
    d.rectangle((34 - walk, 69, 48 - walk, 77), fill=WP["boot"], outline=WP["ink"])
    d.rectangle((19 + walk, 70, 27 + walk, 72), fill=WP["boot_hi"])
    d.rectangle((37 - walk, 70, 45 - walk, 72), fill=WP["boot_hi"])
    # Travel trousers and belt.
    d.rectangle((18, 55, 46, 67), fill=WP["pants"], outline=WP["ink"])
    d.rectangle((20, 58, 44, 63), fill=WP["pants_hi"])
    d.rectangle((18, 54, 46, 59), fill=WP["leather"], outline=WP["ink"])
    d.rectangle((29, 54, 35, 59), fill=WP["gold"], outline=WP["ink"])
    d.rectangle((31, 55, 33, 57), fill=WP["leather"])
    # Shirt and fitted leather vest.
    d.rectangle((17, 34, 47, 61), fill=WP["cream"], outline=WP["ink"])
    d.rectangle((20, 37, 44, 57), fill=WP["cream_hi"])
    d.rectangle((18, 39, 25, 56), fill=WP["cream_sh"])
    d.rectangle((39, 39, 46, 56), fill=WP["cream_sh"])
    d.polygon([(18, 35), (29, 33), (32, 39), (27, 59), (18, 57)],
              fill=WP["vest"], outline=WP["ink"])
    d.polygon([(46, 35), (35, 33), (32, 39), (37, 59), (46, 57)],
              fill=WP["vest"], outline=WP["ink"])
    d.line((22, 39, 28, 55), fill=WP["vest_hi"], width=2)
    d.line((42, 39, 36, 55), fill=WP["vest_hi"], width=2)
    d.rectangle((28, 43, 35, 47), fill=WP["gold"])
    # Arms and gloves, animated subtly while walking.
    d.rectangle((10, 39 + arm, 18, 55 + arm), fill=WP["vest"], outline=WP["ink"])
    d.rectangle((11, 41 + arm, 16, 52 + arm), fill=WP["cream"])
    d.rectangle((46, 39 - arm, 54, 55 - arm), fill=WP["vest"], outline=WP["ink"])
    d.rectangle((48, 41 - arm, 53, 52 - arm), fill=WP["cream"])
    d.rectangle((11, 53 + arm, 17, 60 + arm), fill=WP["leather"], outline=WP["ink"])
    d.rectangle((47, 53 - arm, 53, 60 - arm), fill=WP["leather"], outline=WP["ink"])
    # Head, hair and short beard.
    d.rectangle((20, 13, 44, 35), fill=WP["skin"], outline=WP["ink"])
    d.rectangle((22, 16, 42, 32), fill=WP["skin_hi"])
    d.rectangle((18, 18, 22, 29), fill=WP["hair"], outline=WP["ink"])
    d.rectangle((42, 18, 46, 29), fill=WP["hair"], outline=WP["ink"])
    if direction == "front":
        d.rectangle((24, 22, 27, 25), fill=WP["ink"])
        d.rectangle((37, 22, 40, 25), fill=WP["ink"])
        d.point((25, 22), fill=WP["cream_hi"])
        d.point((38, 22), fill=WP["cream_hi"])
        d.line((29, 28, 34, 28), fill=WP["ink2"], width=1)
        d.polygon([(26, 30), (38, 30), (35, 35), (29, 35)],
                  fill=WP["hair"], outline=WP["ink"])
        d.rectangle((29, 31, 35, 32), fill=WP["hair_hi"])
    elif direction == "left":
        d.rectangle((24, 22, 27, 25), fill=WP["ink"])
        d.point((25, 22), fill=WP["cream_hi"])
        d.rectangle((20, 27, 25, 29), fill=WP["skin_hi"])
        d.polygon([(24, 29), (35, 30), (33, 35), (26, 34)],
                  fill=WP["hair"], outline=WP["ink"])
    elif direction == "right":
        d.rectangle((37, 22, 40, 25), fill=WP["ink"])
        d.point((38, 22), fill=WP["cream_hi"])
        d.rectangle((39, 27, 44, 29), fill=WP["skin_hi"])
        d.polygon([(29, 30), (40, 29), (38, 34), (31, 35)],
                  fill=WP["hair"], outline=WP["ink"])
    else:
        d.rectangle((19, 14, 45, 34), fill=WP["hair"], outline=WP["ink"])
        d.rectangle((23, 17, 41, 30), fill=WP["hair_hi"])
        d.rectangle((25, 19, 39, 28), fill=WP["hair"])
    # Olive explorer cap, with the small flower/leaf pin from the approved portrait.
    d.polygon([(15, 17), (17, 8), (26, 4), (39, 5), (48, 11),
               (49, 17), (42, 15), (24, 15)], fill=WP["cap"], outline=WP["ink"])
    d.polygon([(19, 12), (27, 7), (39, 8), (44, 12), (34, 13)],
              fill=WP["cap_hi"])
    d.rectangle((14, 15, 49, 18), fill=WP["cap"], outline=WP["ink"])
    d.line((19, 15, 43, 15), fill=WP["cap_hi"], width=2)
    d.polygon([(43, 7), (52, 1), (49, 12), (44, 14)], fill=WP["leaf"], outline=WP["ink"])
    d.rectangle((38, 9, 42, 13), fill=WP["cream_hi"], outline=WP["gold"])
    d.point((40, 11), fill=WP["gold"])
    # Green neck scarf and trailing ends.
    d.polygon([(24, 33), (40, 33), (44, 39), (32, 43), (20, 39)],
              fill=WP["green"], outline=WP["ink"])
    d.polygon([(36, 37), (43, 39), (47, 51), (42, 49), (38, 43)],
              fill=WP["green_hi"], outline=WP["ink"])
    d.line((25, 37, 35, 40), fill=WP["green_hi"], width=2)
    # Small field satchel and strap.
    if direction in ("front", "left", "right"):
        d.line((19, 37, 43, 59), fill=WP["leather"], width=3)
        d.rectangle((8, 51, 18, 64), fill=WP["leather"], outline=WP["ink"])
        d.rectangle((10, 53, 16, 61), fill=WP["leather_hi"])
        d.rectangle((12, 54, 15, 56), fill=WP["gold"])
    return im

watcher_sheet = Image.new("RGBA", (192, 304), (0, 0, 0, 0))
for row, direction in enumerate(("front", "left", "right", "back")):
    for col in range(4):
        frame = watcher_frame(direction, col).resize((48, 76), Image.Resampling.NEAREST)
        watcher_sheet.alpha_composite(frame, (col * 48, row * 76))
watcher_sheet.save(WATCHER_OUT, optimize=True)
print(f"Generated native pixel Watcher sprite sheet: {WATCHER_OUT} (192x304, 4x4 cells at 48x76)")
