#!/usr/bin/env python3
"""Render Tiled ground, four-way shoreline masks, props, collision and markers."""
from __future__ import annotations
import argparse
import xml.etree.ElementTree as ET
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
from map_autotile import build_overlay, DIRECTIONS

ROOT=Path(__file__).resolve().parents[1]
MAPS=["coastal_camp","glowglade_grove","tidal_ruins","lighthouse_courtyard"]
W,H,T=18,12,56
ATLAS=ROOT/"assets/environment/topdown_terrain_atlas_v1.png"
FLORA=ROOT/"assets/environment/flora_props_atlas_128.png"
AREA=ROOT/"assets/environment/area_props_96.png"
ENV=ROOT/"assets/environment/environment_props_96.png"
STRUCTURES=ROOT/"assets/environment/structures_atlas_128.png"
CLIFFS=ROOT/"assets/environment/cliffs_fences_atlas_128.png"
ORIGINAL=ROOT/"assets/environment/original_props_16_v1.png"
KENNEY=ROOT/"assets/environment/kenney_buildings_atlas.png"
COTTAGE=ROOT/"assets/environment/glowglade_cottage_cutout.png"
PICKUPS={
    "coastal_camp":[(8,8),(12,8),(14,6),(5,9),(5,10),(12,9)],
    "glowglade_grove":[(6,7),(12,6),(12,7),(4,10),(10,10)],
    "tidal_ruins":[(7,7),(14,6),(7,8),(12,10)],
    "lighthouse_courtyard":[(7,7),(11,7),(10,10)],
}


def load(path):
    return Image.open(path).convert("RGBA") if path.exists() else None


def crop_cell(sheet,index,columns,cell):
    if sheet is None:return None
    x=(index%columns)*cell
    y=(index//columns)*cell
    if x+cell>sheet.width or y+cell>sheet.height:return None
    return sheet.crop((x,y,x+cell,y+cell))


def paste_anchor(dst,sprite,cx,foot_y,width,height):
    if sprite is None:return
    # Trim transparent padding before scaling; the visible alpha bounds, not the
    # rectangular atlas cell, determine the sprite's ground-contact point.
    bbox=sprite.getchannel("A").getbbox()
    if bbox is None:return
    sprite=sprite.crop(bbox)
    sprite=sprite.resize((max(1,int(width)),max(1,int(height))),Image.Resampling.NEAREST)
    dst.alpha_composite(sprite,(int(cx-width/2),int(foot_y-height)))



def draw_water_detail(image,indices,name):
    """Static counterpart of the runtime water shimmer for map-review images."""
    palette={
        "coastal_camp":((111,188,199,105),(231,246,225,150)),
        "glowglade_grove":((119,194,179,105),(230,245,211,155)),
        "tidal_ruins":((100,164,187,110),(211,235,229,150)),
        "lighthouse_courtyard":((120,177,202,110),(241,241,220,160)),
    }
    deep,crest=palette[name]
    draw=ImageDraw.Draw(image,"RGBA")
    water=lambda x,y: indices[y*W+x]//4>=9
    for y in range(H):
        for x in range(W):
            if not water(x,y):
                continue
            left,top=x*T,y*T
            seed=(x*73856093+y*19349663+len(name)*83492791)&0xffffffff
            ox=7+(seed%23)
            oy=9+((seed>>5)%29)
            span=13+((seed>>11)%14)
            draw.line([(left+ox,top+oy+2),(left+ox+span//2,top+oy),
                       (left+ox+span,top+oy+1)],fill=crest,width=2)
            ox2=8+((seed>>17)%25)
            oy2=34+((seed>>23)%13)
            draw.line([(left+ox2,top+oy2),(left+ox2+8,top+oy2-1)],
                      fill=deep,width=1)
            if seed%3==0:
                gx=left+5+((seed>>9)%45)
                gy=top+5+((seed>>15)%45)
                draw.ellipse((gx-1,gy-1,gx+1,gy+1),fill=crest)
            # A broken foam glint sits just inside the water edge, never as a
            # continuous white outline that would make the shore look tiled.
            if y>0 and not water(x,y-1):
                fx=left+8+((seed>>7)%34)
                draw.line([(fx,top+2),(fx+5,top+3)],fill=(236,245,222,135),width=1)
            if x>0 and not water(x-1,y):
                fy=top+8+((seed>>13)%34)
                draw.line([(left+2,fy),(left+3,fy+5)],fill=(236,245,222,125),width=1)


GENERATED_ASSETS = {
    500: ("grove_generated",(48,68,801,569),.35),
    600: ("nature_generated",(18,86,293,362),1), 601: ("nature_generated",(324,87,582,363),1),
    602: ("nature_generated",(631,82,900,364),1), 603: ("nature_generated",(939,86,1234,366),1),
    604: ("nature_generated",(20,444,221,598),1), 605: ("nature_generated",(233,448,416,598),1),
    606: ("nature_generated",(434,453,622,597),1), 607: ("nature_generated",(673,473,787,578),1),
    608: ("nature_generated",(964,483,1084,579),1), 609: ("nature_generated",(1110,485,1231,578),1),
    610: ("nature_generated",(820,502,939,578),1), 611: ("nature_generated",(41,702,223,811),1),
    612: ("nature_generated",(269,732,404,803),1), 613: ("nature_generated",(463,739,558,797),1),
    614: ("nature_generated",(615,745,736,802),1), 615: ("nature_generated",(814,679,927,807),1),
    616: ("nature_generated",(1120,693,1215,803),1), 617: ("nature_generated",(971,717,1063,803),1),
    618: ("nature_generated",(36,900,267,1133),1), 619: ("nature_generated",(348,907,573,1127),1),
    621: ("grove_generated",(834,247,1216,543),1), 622: ("grove_generated",(53,614,303,824),1),
    623: ("grove_generated",(349,629,581,825),1), 624: ("grove_generated",(630,605,915,835),1),
    625: ("grove_generated",(967,603,1210,831),1), 626: ("grove_generated",(24,871,404,1178),1),
    627: ("grove_generated",(425,885,779,1174),1), 628: ("grove_generated",(816,894,988,1172),1),
    629: ("grove_generated",(1023,908,1214,1164),1),
    640: ("ruins_generated",(42,59,363,375),1), 641: ("ruins_generated",(394,73,693,367),1),
    642: ("ruins_generated",(723,107,863,361),1), 643: ("ruins_generated",(895,189,1027,364),1),
    644: ("ruins_generated",(1047,267,1223,363),1), 645: ("ruins_generated",(58,433,411,619),1),
    646: ("ruins_generated",(474,449,801,620),1), 647: ("ruins_generated",(856,449,1190,602),1),
    648: ("ruins_generated",(31,660,286,885),1), 649: ("ruins_generated",(319,711,592,873),1),
    650: ("ruins_generated",(625,738,914,882),1), 651: ("ruins_generated",(920,975,1194,1165),1),
    652: ("ruins_generated",(69,1002,283,1133),1), 653: ("ruins_generated",(611,944,886,1176),1),
    654: ("ruins_generated",(356,1065,560,1138),1),
    660: ("lighthouse_generated",(49,15,455,362),1), 661: ("lighthouse_generated",(449,49,818,361),1),
    662: ("lighthouse_generated",(842,100,1170,361),1), 663: ("lighthouse_generated",(21,398,233,645),1),
    664: ("lighthouse_generated",(262,422,471,633),1), 665: ("lighthouse_generated",(486,411,678,630),1),
    666: ("lighthouse_generated",(702,446,958,641),1), 667: ("lighthouse_generated",(977,398,1245,641),1),
    668: ("lighthouse_generated",(13,732,269,902),1), 669: ("lighthouse_generated",(286,736,488,908),1),
    670: ("lighthouse_generated",(513,702,721,907),1), 671: ("lighthouse_generated",(751,742,962,903),1),
    672: ("lighthouse_generated",(989,707,1239,906),1), 673: ("lighthouse_generated",(27,1020,396,1209),1),
    674: ("lighthouse_generated",(420,1018,739,1203),1), 675: ("lighthouse_generated",(764,983,936,1210),1),
    676: ("lighthouse_generated",(998,957,1226,1218),1),
}

def draw_object(image,obj,sheets):
    kind=obj.get("class") or obj.get("type") or obj.get("name") or ""
    x,y=float(obj.get("x","0")),float(obj.get("y","0"))
    scale=1.0
    for prop in obj.findall("./properties/property"):
        if prop.get("name")=="scale":scale=float(prop.get("value","1"))
    if kind.startswith("tree") and kind[-1:] in ("0","1","2"):
        sprite=crop_cell(sheets["flora"],8+int(kind[-1]),8,128)
        paste_anchor(image,sprite,x,y+8,108*scale,120*scale)
    elif kind=="campfire":
        d=ImageDraw.Draw(image,"RGBA")
        d.ellipse((x-15,y-18,x+15,y+7),fill=(221,112,49,220),outline=(87,57,40,255),width=2)
        d.polygon([(x,y-29),(x-7,y-8),(x+7,y-8)],fill=(255,202,104,240))
    elif kind.startswith("prop_") and kind[5:].isdigit():
        index=int(kind[5:])
        if 20<=index<=35:
            sprite=crop_cell(sheets["area"],index-20,16,96)
            dims={20:(164,112),21:(126,112),22:(144,116),24:(150,158),
                  25:(126,126),26:(102,94),27:(156,168),28:(184,184),
                  29:(104,122),30:(128,112),31:(132,132),32:(130,100),
                  33:(150,190),34:(110,118),35:(112,116)}
            width,height=dims.get(index,(100,108))
            paste_anchor(image,sprite,x,y+7,width*scale,height*scale)
        elif 100<=index<200:
            sprite=crop_cell(sheets["flora"],index-100,8,128)
            paste_anchor(image,sprite,x,y+7,100*scale,108*scale)
        elif 200<=index<300:
            sprite=crop_cell(sheets["cliffs"],index-200,8,128)
            paste_anchor(image,sprite,x,y+7,112*scale,112*scale)
        elif index==302 and sheets["coastal_house"] is not None:
            paste_anchor(image,sheets["coastal_house"],x,y+7,252*scale,204*scale)
        elif 300<=index<400:
            sprite=crop_cell(sheets["structures"],index-300,8,128)
            dims={302:(196,156),303:(176,154),304:(166,180),305:(168,152)}
            width,height=dims.get(index,(150,140))
            paste_anchor(image,sprite,x,y+7,width*scale,height*scale)
        elif 400<=index<416:
            sprite=crop_cell(sheets["original"],index-400,4,32)
            paste_anchor(image,sprite,x,y+7,92*scale,92*scale)
        elif index in GENERATED_ASSETS:
            sheet_key,box,factor=GENERATED_ASSETS[index]
            sheet=sheets.get(sheet_key)
            if sheet is not None:
                sprite=sheet.crop(box)
                factor_scale=scale*factor
                paste_anchor(image,sprite,x,y+7,(box[2]-box[0])*factor_scale,(box[3]-box[1])*factor_scale)
        elif 500<=index<504 and sheets["kenney"] is not None:
            if index in (501,):
                sx=(index-500)*192+24
                sprite=sheets["kenney"].crop((sx,0,sx+144,192))
                paste_anchor(image,sprite,x,y+7,144*scale,192*scale)
            elif index==502:
                sprite=sheets["kenney"].crop((0,240,192,384))
                paste_anchor(image,sprite,x,y+7,192*scale,144*scale)
            else:
                sprite=sheets["kenney"].crop((204,336,372,384))
                paste_anchor(image,sprite,x,y+7,168*scale,48*scale)


def render_map(name,out_dir):
    root=ET.parse(ROOT/"maps"/(name+".tmx")).getroot()
    atlas=load(ATLAS)
    if atlas is None:raise SystemExit(f"Missing terrain atlas: {ATLAS}")
    sheets={
        "flora":load(FLORA),"area":load(AREA),"env":load(ENV),
        "structures":load(STRUCTURES),"cliffs":load(CLIFFS),
        "original":load(ORIGINAL),"kenney":load(KENNEY),"cottage":load(COTTAGE),
        "coastal_house":load(ROOT/"assets/environment/coastal_keeper_house.png"),
        "nature_generated":load(ROOT/"assets/environment/generated/nature_props_sheet.png"),
        "grove_generated":load(ROOT/"assets/environment/generated/glowglade_props_sheet.png"),
        "ruins_generated":load(ROOT/"assets/environment/generated/tidal_ruins_props_sheet.png"),
        "lighthouse_generated":load(ROOT/"assets/environment/generated/lighthouse_ruins_props_sheet.png"),
    }
    layer=root.find("./layer[@name='Ground']/data")
    gids=[int(v.strip()) for v in (layer.text or "").replace("\n","").split(",") if v.strip()]
    if len(gids)!=W*H:raise SystemExit(f"{name}: invalid Ground layer size")
    indices=[gid-1 for gid in gids]
    overlays=build_overlay(indices)
    image=Image.new("RGBA",(W*T,H*T),(20,34,39,255))

    # Base terrain and all four transparent material edges.
    for i,index in enumerate(indices):
        tx,ty=(i%W)*T,(i//W)*T
        tile=atlas.crop(((index%4)*T,(index//4)*T,(index%4+1)*T,(index//4+1)*T))
        image.alpha_composite(tile,(tx,ty))
        for direction,(dx,dy,angle) in enumerate(DIRECTIONS):
            overlay=overlays[direction][i]
            if overlay<0:continue
            edge=atlas.crop(((overlay%4)*T,(overlay//4)*T,
                             (overlay%4+1)*T,(overlay//4+1)*T))
            if angle:edge=edge.rotate(-angle,resample=Image.Resampling.NEAREST)
            image.alpha_composite(edge,(tx,ty))

    draw_water_detail(image,indices,name)

    # Collision review is translucent so it does not hide the actual art.
    collision_layer=Image.new("RGBA",image.size,(0,0,0,0))
    collision_draw=ImageDraw.Draw(collision_layer,"RGBA")
    collision=root.find("./objectgroup[@name='Collision']")
    if collision is not None:
        for obj in collision.findall("object"):
            x,y=float(obj.get("x","0")),float(obj.get("y","0"))
            w,h=float(obj.get("width","0")),float(obj.get("height","0"))
            collision_draw.rectangle((x,y,x+w,y+h),fill=(215,55,48,28),
                                     outline=(235,87,75,170),width=2)
    image.alpha_composite(collision_layer)

    # Tiled-authored scene props are depth sorted by their ground anchor.
    objects=root.find("./objectgroup[@name='Objects']")
    if objects is not None:
        sorted_objects=sorted(objects.findall("object"),key=lambda o:float(o.get("y","0")))
        for obj in sorted_objects:draw_object(image,obj,sheets)

    # Story markers and gameplay pickups remain visible for route validation.
    marker_layer=Image.new("RGBA",image.size,(0,0,0,0))
    draw=ImageDraw.Draw(marker_layer,"RGBA")
    font=ImageFont.load_default()
    markers=root.find("./objectgroup[@name='Markers']")
    visible_markers={"coastal_camp":{"spawn","return","grove_gate","ruin_gate","shrine"},
                     "glowglade_grove":{"spawn","return","shrine","boss"},
                     "tidal_ruins":{"spawn","return","shrine","boss"},
                     "lighthouse_courtyard":{"spawn","return","shrine","boss"}}[name]
    if markers is not None:
        for obj in markers.findall("object"):
            marker=obj.get("type") or obj.get("class") or obj.get("name") or ""
            if marker not in visible_markers:continue
            x,y=float(obj.get("x","0")),float(obj.get("y","0"))
            draw.ellipse((x-6,y-6,x+6,y+6),fill=(255,221,105,235),
                         outline=(35,32,20,255),width=2)
            draw.text((x+8,y-7),marker,font=font,fill=(20,28,31,255),
                      stroke_width=1,stroke_fill=(240,231,196,230))
    for x,y in PICKUPS[name]:
        cx,cy=(x+.5)*T,(y+.5)*T
        draw.ellipse((cx-4,cy-4,cx+4,cy+4),fill=(255,91,133,240),
                     outline=(40,25,35,255),width=1)
    image.alpha_composite(marker_layer)

    out=out_dir/(name+".png")
    image.save(out)
    return image,out


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--output",type=Path,default=ROOT/"build"/"map-previews")
    args=parser.parse_args()
    args.output.mkdir(parents=True,exist_ok=True)
    previews=[]
    for name in MAPS:
        image,path=render_map(name,args.output)
        previews.append(image)
        print("MAP PREVIEW:",path)
    thumb_w,thumb_h=616,504
    sheet=Image.new("RGB",(thumb_w*2+48,thumb_h*2+72),(14,25,37))
    draw=ImageDraw.Draw(sheet)
    font=ImageFont.load_default()
    for i,(name,image) in enumerate(zip(MAPS,previews)):
        thumb=image.convert("RGB").resize((thumb_w,thumb_h),Image.Resampling.NEAREST)
        x=16+(i%2)*(thumb_w+16)
        y=32+(i//2)*(thumb_h+16)
        sheet.paste(thumb,(x,y))
        draw.text((x,y-17),name.replace("_"," ").title(),font=font,fill=(240,225,190))
    contact=args.output/"chapter1_map_review.png"
    sheet.save(contact)
    print("MAP CONTACT SHEET:",contact)


if __name__=="__main__":
    main()
