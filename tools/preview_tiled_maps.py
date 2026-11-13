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
W,H,T=22,18,56
ATLAS=ROOT/"assets/environment/topdown_terrain_atlas_v1.png"
FLORA=ROOT/"assets/environment/flora_props_atlas_128.png"
AREA=ROOT/"assets/environment/area_props_96.png"
ENV=ROOT/"assets/environment/environment_props_96.png"
STRUCTURES=ROOT/"assets/environment/structures_atlas_128.png"
CLIFFS=ROOT/"assets/environment/cliffs_fences_atlas_128.png"
ORIGINAL=ROOT/"assets/environment/original_props_16_v1.png"
KENNEY=ROOT/"assets/environment/kenney_buildings_atlas.png"
PICKUPS={
    "coastal_camp":[(7,7),(9,11),(13,10),(15,6),(15,12)],
    "glowglade_grove":[(6,5),(8,12),(14,5),(16,11),(17,13)],
    "tidal_ruins":[(7,6),(11,12),(15,5),(18,11)],
    "lighthouse_courtyard":[(6,5),(12,12),(17,5)],
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
    sprite=sprite.resize((max(1,int(width)),max(1,int(height))),Image.Resampling.NEAREST)
    dst.alpha_composite(sprite,(int(cx-width/2),int(foot_y-height)))


def draw_object(image,obj,sheets):
    kind=obj.get("class") or obj.get("type") or obj.get("name") or ""
    x,y=float(obj.get("x","0")),float(obj.get("y","0"))
    scale=1.0
    for prop in obj.findall("./properties/property"):
        if prop.get("name")=="scale":scale=float(prop.get("value","1"))
    if kind.startswith("tree") and kind[-1:] in ("0","1","2"):
        sprite=crop_cell(sheets["flora"],8+int(kind[-1]),8,128)
        paste_anchor(image,sprite,x,y+8,138*scale,152*scale)
    elif kind=="campfire":
        d=ImageDraw.Draw(image,"RGBA")
        d.ellipse((x-15,y-18,x+15,y+7),fill=(221,112,49,220),outline=(87,57,40,255),width=2)
        d.polygon([(x,y-29),(x-7,y-8),(x+7,y-8)],fill=(255,202,104,240))
    elif kind.startswith("prop_") and kind[5:].isdigit():
        index=int(kind[5:])
        if 20<=index<=35:
            sprite=crop_cell(sheets["area"],index-20,16,96)
            dims={20:(164,112),21:(126,112),22:(132,126),24:(150,158),25:(126,126),26:(102,94),27:(156,168),28:(184,184),29:(104,122),30:(128,112),31:(132,132),32:(130,100),
                  33:(150,190),34:(110,118),35:(112,116)}
            width,height=dims.get(index,(100,108))
            paste_anchor(image,sprite,x,y+7,width*scale,height*scale)
        elif 100<=index<200:
            sprite=crop_cell(sheets["flora"],index-100,8,128)
            paste_anchor(image,sprite,x,y+7,100*scale,108*scale)
        elif 200<=index<300:
            sprite=crop_cell(sheets["cliffs"],index-200,8,128)
            paste_anchor(image,sprite,x,y+7,112*scale,112*scale)
        elif 300<=index<400:
            sprite=crop_cell(sheets["structures"],index-300,8,128)
            paste_anchor(image,sprite,x,y+7,150*scale,140*scale)
        elif 400<=index<416:
            sprite=crop_cell(sheets["original"],index-400,4,32)
            paste_anchor(image,sprite,x,y+7,92*scale,92*scale)
        elif 500<=index<504 and sheets["kenney"] is not None:
            if index in (500,501):
                sx=(index-500)*192+24
                sprite=sheets["kenney"].crop((sx,0,sx+144,192))
                paste_anchor(image,sprite,x,y+7,144*scale,192*scale)


def render_map(name,out_dir):
    root=ET.parse(ROOT/"maps"/(name+".tmx")).getroot()
    atlas=load(ATLAS)
    if atlas is None:raise SystemExit(f"Missing terrain atlas: {ATLAS}")
    sheets={
        "flora":load(FLORA),"area":load(AREA),"env":load(ENV),
        "structures":load(STRUCTURES),"cliffs":load(CLIFFS),
        "original":load(ORIGINAL),"kenney":load(KENNEY),
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
                     "tidal_ruins":{"camp","return","shrine","boss"},
                     "lighthouse_courtyard":{"camp","return","shrine","boss"}}[name]
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
