from pathlib import Path
from zipfile import ZipFile
from PIL import Image, ImageDraw
import io, math

ROOT = Path(__file__).resolve().parents[1]
PACK = ROOT / "third_party/kenney/tiny-town/kenney_tiny_town.zip"
OUT = ROOT / "build/kenney_tiny_town_preview.png"
if not PACK.exists():
    raise SystemExit("Kenney Tiny Town CC0 archive is missing from the repository.")
items=[]
with ZipFile(PACK) as z:
    for name in z.namelist():
        low=name.lower()
        if not low.endswith(".png") or any(k in low for k in ("preview","sample")):
            continue
        try:
            im=Image.open(io.BytesIO(z.read(name))).convert("RGBA")
        except Exception:
            continue
        if im.width>512 or im.height>512:
            continue
        items.append((name,im.copy()))
items.sort(key=lambda x:x[0].lower())
cell_w,cell_h=132,116
cols=8
rows=max(1,math.ceil(len(items)/cols))
sheet=Image.new("RGB",(cols*cell_w,rows*cell_h),(239,233,211))
d=ImageDraw.Draw(sheet)
for i,(name,im) in enumerate(items):
    x=(i%cols)*cell_w;y=(i//cols)*cell_h
    im.thumbnail((104,82),Image.Resampling.NEAREST)
    tile=Image.new("RGBA",(104,82),(239,233,211,255))
    tile.alpha_composite(im,((104-im.width)//2,(82-im.height)//2))
    sheet.paste(tile.convert("RGB"),(x+14,y+2))
    label=name.split("/")[-1]
    d.text((x+4,y+87),label[:20],fill=(62,74,61))
OUT.parent.mkdir(parents=True,exist_ok=True)
sheet.save(OUT)
print(f"Kenney Tiny Town preview: {len(items)} sprites -> {OUT}")
