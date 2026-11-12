from pathlib import Path
from zipfile import ZipFile
from PIL import Image
import io

ROOT=Path(__file__).resolve().parents[1]
PACK=ROOT/"third_party/kenney/tiny-town/kenney_tiny_town.zip"
OUT=ROOT/"assets/environment/kenney_buildings_atlas.png"
if not PACK.exists():
    raise SystemExit("Missing vendored Kenney Tiny Town CC0 pack.")
with ZipFile(PACK) as z:
    tilemap=Image.open(io.BytesIO(z.read("Tilemap/tilemap.png"))).convert("RGBA")

def compose(x0,y0,w,h):
    out=Image.new("RGBA",(w*16,h*16),(0,0,0,0))
    for y in range(h):
        for x in range(w):
            tile=tilemap.crop(((x0+x)*17,(y0+y)*17,(x0+x)*17+16,(y0+y)*17+16))
            out.alpha_composite(tile,(x*16,y*16))
    return out

# Blue house, red house, stone castle gate, and wooden fence segment.
specs=[(0,4,3,4),(4,4,3,4),(3,8,4,3),(8,3,4,1)]
atlas=Image.new("RGBA",(384,384),(0,0,0,0))
for i,(x,y,w,h) in enumerate(specs):
    sprite=compose(x,y,w,h).resize((w*48,h*48),Image.Resampling.NEAREST)
    cell=Image.new("RGBA",(192,192),(0,0,0,0))
    cell.alpha_composite(sprite,((192-sprite.width)//2,192-sprite.height))
    atlas.alpha_composite(cell,((i%2)*192,(i//2)*192))
OUT.parent.mkdir(parents=True,exist_ok=True)
atlas.save(OUT,optimize=True)
print(f"Generated Kenney Tiny Town runtime atlas: {OUT} ({atlas.width}x{atlas.height})")
