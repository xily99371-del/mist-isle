#!/usr/bin/env python3
"""Render Tiled ground/collision/marker review images for visual QA."""
import argparse
import xml.etree.ElementTree as ET
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(__file__).resolve().parents[1]
MAPS=["coastal_camp","glowglade_grove","tidal_ruins","lighthouse_courtyard"]
W,H,T=22,18,56
ATLAS=ROOT/"assets/environment/topdown_terrain_atlas_v1.png"

def render_map(name,out_dir):
    root=ET.parse(ROOT/"maps"/(name+".tmx")).getroot()
    atlas=Image.open(ATLAS).convert("RGBA")
    layer=root.find("./layer[@name='Ground']/data")
    gids=[int(v.strip()) for v in (layer.text or "").replace("\n","").split(",") if v.strip()]
    image=Image.new("RGBA",(W*T,H*T),(20,34,39,255))
    for i,gid in enumerate(gids):
        index=gid-1
        tile=atlas.crop(((index%4)*T,(index//4)*T,(index%4+1)*T,(index//4+1)*T))
        image.alpha_composite(tile,((i%W)*T,(i//W)*T))
    draw=ImageDraw.Draw(image,"RGBA")
    collision=root.find("./objectgroup[@name='Collision']")
    if collision is not None:
        for obj in collision.findall("object"):
            x,y=float(obj.get("x","0")),float(obj.get("y","0"))
            w,h=float(obj.get("width","0")),float(obj.get("height","0"))
            draw.rectangle((x,y,x+w,y+h),fill=(215,55,48,48),outline=(235,87,75,190),width=2)
    markers=root.find("./objectgroup[@name='Markers']")
    if markers is not None:
        font=ImageFont.load_default()
        for obj in markers.findall("object"):
            name2=obj.get("type") or obj.get("class") or obj.get("name") or ""
            x,y=float(obj.get("x","0")),float(obj.get("y","0"))
            draw.ellipse((x-7,y-7,x+7,y+7),fill=(255,221,105,230),outline=(35,32,20,255),width=2)
            draw.text((x+9,y-7),name2,font=font,fill=(20,28,31,255),stroke_width=1,stroke_fill=(240,231,196,220))
    out=out_dir/(name+".png")
    image.save(out)
    return image,out

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument("--output",type=Path,default=ROOT/"build"/"map-previews")
    args=ap.parse_args()
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
