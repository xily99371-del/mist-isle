from pathlib import Path
p=Path('src/render.c')
s=p.read_text(encoding='utf-8')
a='void RenderPrepareFrame(Game *g) {\n    if(g->scene==SCENE_PLAY'
b='void RenderPrepareFrame(Game *g) {\n    if(g->scene==SCENE_TITLE)return; /* Cover UI does not need world ground. */\n    if(g->scene==SCENE_PLAY'
assert s.count(a)==1
p.write_text(s.replace(a,b),encoding='utf-8')
