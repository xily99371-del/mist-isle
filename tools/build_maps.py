#!/usr/bin/env python3
"""Compile Tiled TMX maps into static C arrays. Runtime has no Python dependency."""
from __future__ import annotations
import argparse, math, xml.etree.ElementTree as ET
from pathlib import Path
from map_autotile import build_overlay

ROOT=Path(__file__).resolve().parents[1]
MAPS=["coastal_camp","glowglade_grove","tidal_ruins","lighthouse_courtyard"]
W,H,T,MAX_OBJECTS=22,18,56,64
MARKERS=("spawn","return","grove_gate","ruin_gate","shrine","boss")

def fail(message):
    raise SystemExit("MAP BUILD FAILED: "+message)

def kind(obj):
    return obj.get("class") or obj.get("type") or obj.get("name") or ""

def parse(path):
    root=ET.parse(path).getroot()
    if root.get("orientation")!="orthogonal" or tuple(int(root.get(k,"0")) for k in ("width","height","tilewidth","tileheight"))!=(W,H,T,T):
        fail(f"{path.name}: expected a {W}x{H} orthogonal map with {T}px tiles")
    data=root.find("./layer[@name='Ground']/data")
    if data is None or data.get("encoding")!="csv": fail(f"{path.name}: missing CSV Ground layer")
    gids=[int(v.strip()) for v in (data.text or "").replace("\n","").split(",") if v.strip()]
    if len(gids)!=W*H: fail(f"{path.name}: Ground layer has {len(gids)} cells")
    ground=[]
    for gid in gids:
        if not 1<=gid<=48: fail(f"{path.name}: invalid terrain GID {gid}")
        index=gid-1
        if index//4 not in (0,1,3,4,6,7,9,10): fail(f"{path.name}: edge-mask tile GID {gid} used as full ground")
        ground.append(index)
    collision=[[0]*W for _ in range(H)]
    cg=root.find("./objectgroup[@name='Collision']")
    if cg is not None:
        for obj in cg.findall("object"):
            x,y=float(obj.get("x","0")),float(obj.get("y","0"))
            w,h=float(obj.get("width","0")),float(obj.get("height","0"))
            if w<=0 or h<=0: fail(f"{path.name}: Collision objects must be rectangles")
            x0=max(0,int(math.floor(x/T))); y0=max(0,int(math.floor(y/T)))
            x1=min(W-1,int(math.ceil((x+w)/T))-1); y1=min(H-1,int(math.ceil((y+h)/T))-1)
            for yy in range(y0,y1+1):
                for xx in range(x0,x1+1): collision[yy][xx]=1
    objects=[]
    og=root.find("./objectgroup[@name='Objects']")
    if og is not None:
        for obj in og.findall("object"):
            name=kind(obj)
            if name.startswith("tree") and name[-1:] in ("0","1","2"): type_id=1000+int(name[-1])
            elif name.startswith("prop_") and name[5:].isdigit(): type_id=int(name[5:])
            elif name=="campfire": type_id=2000
            else: fail(f"{path.name}: unsupported object type '{name}'")
            x=float(obj.get("x","0"))/T-.5; y=float(obj.get("y","0"))/T-.5
            if not (0<=x<W and 0<=y<H): fail(f"{path.name}: object '{name}' outside map")
            scale=1.0
            for prop in obj.findall("./properties/property"):
                if prop.get("name")=="scale": scale=float(prop.get("value","1"))
            if not .25<=scale<=2.0: fail(f"{path.name}: invalid scale for '{name}'")
            objects.append((type_id,x,y,scale))
            # Collision follows the visible object's ground footprint, not its full sprite.
            # Tall canopies remain walk-under; only trunks, walls and structural bases block.
            cx,cy=int(round(x)),int(round(y))
            def block_rect(left,top,right,bottom):
                for by in range(max(0,top),min(H-1,bottom)+1):
                    for bx in range(max(0,left),min(W-1,right)+1):
                        collision[by][bx]=1
            if 1000<=type_id<1003:
                block_rect(cx,cy,cx,cy)                 # tree trunk
            elif type_id in (302,500,501):
                block_rect(cx-2,cy-1,cx+2,cy-1)        # rear wall of the enlarged house
                block_rect(cx-2,cy,cx-1,cy)            # left foundation; keep the doorway cell open
                block_rect(cx+1,cy,cx+2,cy)            # right foundation; keep the doorway cell open
            elif type_id==303:
                block_rect(cx-1,cy-1,cx+1,cy)          # tent footprint
            elif type_id==304:
                block_rect(cx-1,cy,cx-1,cy)
                block_rect(cx+1,cy,cx+1,cy)            # arch posts; keep doorway open
            elif type_id==305:
                block_rect(cx-1,cy-1,cx+1,cy)          # fountain plinth
            elif type_id in (24,25,26,27,29,31,33):
                block_rect(cx,cy,cx,cy)                 # flowering trees, crystal, pillar, altar, lighthouse base
            elif type_id==21:
                block_rect(cx-1,cy,cx+1,cy)              # boat hull
            elif type_id==22:
                block_rect(cx-1,cy-1,cx+1,cy)            # tent footprint
            elif type_id==28:
                block_rect(cx-1,cy,cx-1,cy)
                block_rect(cx+1,cy,cx+1,cy)            # ruin arch supports
            elif type_id==30:
                block_rect(cx-1,cy,cx+1,cy)            # broken wall segment
            elif type_id==34:
                block_rect(cx-1,cy,cx-1,cy)
                block_rect(cx+1,cy,cx+1,cy)            # ornamental gate supports
    if len(objects)>MAX_OBJECTS: fail(f"{path.name}: object count exceeds {MAX_OBJECTS}")
    markers={}
    mg=root.find("./objectgroup[@name='Markers']")
    if mg is not None:
        for obj in mg.findall("object"):
            name=kind(obj)
            if name not in MARKERS or name in markers: fail(f"{path.name}: invalid or duplicate marker '{name}'")
            markers[name]=(float(obj.get("x","0"))/T-.5,float(obj.get("y","0"))/T-.5)
    missing=[m for m in MARKERS if m not in markers]
    if missing: fail(f"{path.name}: missing markers {missing}")
    for i,index in enumerate(ground):
        if index//4>=9: collision[i//W][i%W]=1
    overlay=build_overlay(ground)
    # Bridges are walkable spans over water. Each sprite spans roughly three
    # 56px cells, so the three underlying water cells at its row are passable.
    for type_id,x,y,scale in objects:
        if type_id==32:
            bx,by=int(round(x)),int(round(y))
            for xx in range(max(0,bx-1),min(W,bx+2)):
                collision[by][xx]=0
    for name,(mx,my) in markers.items():
        gx,gy=int(round(mx)),int(round(my))
        if not (0<=gx<W and 0<=gy<H):
            fail(f"{path.name}: marker '{name}' outside map")
        if collision[gy][gx]:
            fail(f"{path.name}: marker '{name}' is blocked; move it to a walkable tile")
    # All story markers must be reachable from the authored entrance.
    from collections import deque
    sx,sy=(int(round(v)) for v in markers["spawn"])
    queue=deque([(sx,sy)])
    visited={(sx,sy)}
    while queue:
        x,y=queue.popleft()
        for dx,dy in ((1,0),(-1,0),(0,1),(0,-1)):
            nx,ny=x+dx,y+dy
            if 0<=nx<W and 0<=ny<H and not collision[ny][nx] and (nx,ny) not in visited:
                visited.add((nx,ny))
                queue.append((nx,ny))
    for name,(mx,my) in markers.items():
        target=(int(round(mx)),int(round(my)))
        if target not in visited:
            fail(f"{path.name}: marker '{name}' is unreachable from spawn")
    return ground,[v for row in collision for v in row],objects,markers,overlay

def generate(out):
    grounds=[]; solids=[]; objects=[]; markers=[]; overlays=[]
    for name in MAPS:
        g,c,o,m,edge=parse(ROOT/"maps"/(name+".tmx"))
        grounds.append(g); solids.append(c); objects.append(o); markers.append(m); overlays.append(edge)
        print(f"MAP OK: {name}: {len(o)} objects, {sum(c)} solid cells")
    lines=["/* Generated from maps/*.tmx by tools/build_maps.py. Do not edit. */",
           "#ifndef MIST_ISLE_AREA_MAPS_H","#define MIST_ISLE_AREA_MAPS_H",
           "static const unsigned char AREA_GROUND[4][MAPH][MAPW] = {"]
    for row in grounds:
        lines.append("    {")
        for y in range(H): lines.append("        {"+", ".join(map(str,row[y*W:(y+1)*W]))+"},")
        lines.append("    },")
    lines += ["};","static const unsigned char AREA_COLLISION[4][MAPH][MAPW] = {"]
    for row in solids:
        lines.append("    {")
        for y in range(H): lines.append("        {"+", ".join(map(str,row[y*W:(y+1)*W]))+"},")
        lines.append("    },")
    lines += ["};", "static const signed char AREA_OVERLAY[4][4][MAPH][MAPW] = {"]
    for area in overlays:
        lines.append("    {")
        for direction in area:
            lines.append("        {")
            for y in range(H): lines.append("            {"+", ".join(str(v) for v in direction[y*W:(y+1)*W])+"},")
            lines.append("        },")
        lines.append("    },")
    lines += ["};", "/* Marker order: spawn, return, grove_gate, ruin_gate, shrine, boss. */",
              "static const Vector2 AREA_MARKERS[4][6] = {"]
    for m in markers:
        lines.append("    {"+", ".join("{%.3ff, %.3ff}"%m[n] for n in MARKERS)+"},")
    lines += ["};","static const unsigned char AREA_OBJECT_COUNTS[4] = {"+", ".join(str(len(o)) for o in objects)+"};",
              "static const MapObject AREA_OBJECTS[4][MAX_MAP_OBJECTS] = {"]
    for objs in objects:
        lines.append("    {")
        for t,x,y,s in objs: lines.append("        {%d, {%.3ff, %.3ff}, %.3ff},"%(t,x,y,s))
        lines.append("    },")
    lines += ["};","#endif",""]
    out.parent.mkdir(parents=True,exist_ok=True)
    out.write_text("\n".join(lines),encoding="utf-8",newline="\n")
    print("Generated",out)

if __name__=="__main__":
    ap=argparse.ArgumentParser()
    ap.add_argument("--output",type=Path,required=True)
    generate(ap.parse_args().output)
