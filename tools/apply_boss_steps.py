"""Apply the reviewed, narrowly-scoped boss/footstep source edits once."""
from pathlib import Path
import sys
ROOT=Path(sys.argv[1]) if len(sys.argv)>1 else Path(__file__).resolve().parents[1]
def edit(path,old,new,count=1):
 p=ROOT/path;s=p.read_text(encoding='utf-8')
 if s.count(old)!=count:raise RuntimeError(f'{path}: expected {count} original snippets, got {s.count(old)}')
 p.write_text(s.replace(old,new),encoding='utf-8')
edit('src/battle.h','#define MIST_ISLE_BATTLE_H','#define MIST_ISLE_BATTLE_H\n#define LIGHTHOUSE_BOSS_MAX_HP 450.0f\n/* Shared by attack logic and HUD; phase boundaries scale with maximum HP. */\nint BattleBossPhase(float hp, float maxHp);')
edit('src/battle.c','static Buddy *Partner(Game *g,int id) {','''int BattleBossPhase(float hp,float maxHp) {
    if(!isfinite(hp)||!isfinite(maxHp)||maxHp<=0)return 1;
    /* Compare cross-products, keeping exact 1/3 and 2/3 boundaries consistent. */
    float scaled=fmaxf(0,hp)*3.0f;
    return scaled<=maxHp?3:scaled<=maxHp*2.0f?2:1;
}
static Buddy *Partner(Game *g,int id) {''')
for path in ['src/battle.c','src/battle_render.inc']:
 edit(path,'int phase=e->hp<=50?3:e->hp<=100?2:1;','int phase=BattleBossPhase(e->hp,e->maxHp);')
edit('src/game.c','AddEnemy(g,ENEMY_BOSS,g->world.bossPos.x,g->world.bossPos.y,150);','AddEnemy(g,ENEMY_BOSS,g->world.bossPos.x,g->world.bossPos.y,LIGHTHOUSE_BOSS_MAX_HP);',3)
edit('src/battle_render.inc','''    if(boss) {
        int phase=BattleBossPhase''','''    if(boss) {
        /* The ticks mark 1/3 and 2/3 HP, independent of balance changes. */
        for(int i=1;i<=2;i++)DrawRectangle(925+i*102,109,2,17,(Color){23,32,43,235});
        DrawUIText(TextFormat("%.0f%%",100.0f*Clamp(e->hp/fmaxf(1,e->maxHp),0,1)),1189,43,16,(Color){206,222,232,255});
        int phase=BattleBossPhase''')
for path in ['src/main.c','src/battle_render.inc']:
 edit(path,'IMPACT-02','IMPACT-02.1',2 if path.endswith('.inc') else 1)
edit('src/audio.h','    AUDIO_CUE_COUNT','''    /* Keep existing cue IDs stable; add recorded left/right variations. */
    AUDIO_STEP_GRASS_C, AUDIO_STEP_GRASS_D, AUDIO_STEP_GRASS_E, AUDIO_STEP_GRASS_F,
    AUDIO_STEP_STONE_C, AUDIO_STEP_STONE_D, AUDIO_STEP_STONE_E, AUDIO_STEP_STONE_F,
    AUDIO_CUE_COUNT''')
edit('src/audio.h','/* Distances are actual world-space travel, never requested velocity. */','''/* Map identity deliberately wins over rain and decorative terrain tiles. */
static inline int AudioFootstepSurface(Area area) {
    return (area==AREA_RUINS||area==AREA_LIGHTHOUSE)?1:0;
}
/* Distances are actual world-space travel, never requested velocity. */''')
edit('src/game.c','''    int tileX=(int)floorf(p->pos.x+0.5f),tileY=(int)floorf(p->pos.y+0.5f);
    int surface=g->area==AREA_RUINS?2:0;
    if(surface==0&&tileX>=0&&tileX<MAPW&&tileY<MAPH&&
       TerrainKindFromAtlasIndex(g->world.terrain[tileY][tileX])==3)surface=1;
    AudioMovement(Vector2Length(actualMovement),blocked,surface,dt);''','''    AudioMovement(Vector2Length(actualMovement),blocked,AudioFootstepSurface(g->area),dt);''')
edit('src/audio.c','#define CROSSFADE_SECONDS 0.65f','#define CROSSFADE_SECONDS 0.65f\n#define FOOTSTEP_STRIDE 1.12f\n#define FOOTSTEP_MIN_INTERVAL 0.20f')
edit('src/audio.c','    unsigned int stepIndex, random;','    unsigned int stepIndex, random;\n    int stepTakes[2][2];\n    float lastFootstep;')
edit('src/audio.c','    "dash", "pickup", "attack", "hurt", "victory"','''    "dash", "pickup", "attack", "hurt", "victory",
    "step_grass_c", "step_grass_d", "step_grass_e", "step_grass_f",
    "step_stone_c", "step_stone_d", "step_stone_e", "step_stone_f"''')
edit('src/audio.c','static float Clamp01(float value)', '''static bool IsFootstep(AudioCue cue) {
    return (cue>=AUDIO_STEP_GRASS_A&&cue<=AUDIO_STEP_WET_B)||
           (cue>=AUDIO_STEP_GRASS_C&&cue<=AUDIO_STEP_STONE_F);
}
static bool IsGrassStep(AudioCue cue) {
    return cue==AUDIO_STEP_GRASS_A||cue==AUDIO_STEP_GRASS_B||
           (cue>=AUDIO_STEP_GRASS_C&&cue<=AUDIO_STEP_GRASS_F);
}
static const AudioCue FOOTSTEPS[2][2][3]={
    {{AUDIO_STEP_GRASS_A,AUDIO_STEP_GRASS_C,AUDIO_STEP_GRASS_E},
     {AUDIO_STEP_GRASS_B,AUDIO_STEP_GRASS_D,AUDIO_STEP_GRASS_F}},
    {{AUDIO_STEP_STONE_A,AUDIO_STEP_STONE_C,AUDIO_STEP_STONE_E},
     {AUDIO_STEP_STONE_B,AUDIO_STEP_STONE_D,AUDIO_STEP_STONE_F}}
};
static void PlayFootstep(int surface) {
    unsigned int foot=audioState.stepIndex++%2;
    if(surface==2) { AudioPlay((AudioCue)(AUDIO_STEP_WET_A+foot)); return; }
    int previous=audioState.stepTakes[surface][foot];
    audioState.random=audioState.random*1664525u+1013904223u;
    int take=previous<0?0:(previous+1+(int)((audioState.random>>16)%2u))%3;
    /* Missing optional variants fall back within the same surface only. */
    for(int n=0;n<3;n++) {
        int index=(take+n)%3;
        AudioCue cue=FOOTSTEPS[surface][foot][index];
        if(audioState.soundReady[cue]) {
            audioState.stepTakes[surface][foot]=index;
            AudioPlay(cue);return;
        }
    }
}
static float Clamp01(float value)''')
edit('src/audio.c','    audioState.random=0x4d495354u;','''    audioState.random=0x4d495354u;
    audioState.lastFootstep=-100.0f;
    for(int surface=0;surface<2;surface++)for(int foot=0;foot<2;foot++)
        audioState.stepTakes[surface][foot]=-1;''')
edit('src/audio.c','    float cooldown=(cue==AUDIO_COLLISION)?0.30f:((cue<=AUDIO_STEP_WET_B)?0.13f:0.055f);','    bool step=IsFootstep(cue);\n    float cooldown=(cue==AUDIO_COLLISION)?0.30f:(step?0.13f:0.055f);')
edit('src/audio.c','    float pitch=0.97f+(float)((audioState.random>>16)%61u)*0.001f;','''    float pitch=step?0.985f+(float)((audioState.random>>16)%31u)*0.001f:
                     0.97f+(float)((audioState.random>>16)%61u)*0.001f;''')
edit('src/audio.c','    SetSoundVolume(audioState.sounds[cue],cue<=AUDIO_STEP_WET_B?0.68f:0.76f);','''    float volume=step?(IsGrassStep(cue)?0.60f:0.64f):0.76f;
    if(step)volume*=0.94f+(float)((audioState.random>>8)%61u)*0.001f;
    SetSoundVolume(audioState.sounds[cue],volume);''')
edit('src/audio.c','''    if(!audioState.ready)return;
    if(blocked)''','''    if(!audioState.ready||audioState.paused||audioState.muted||audioState.silentTransition) {
        AudioResetMovement();return;
    }
    if(blocked)''')
edit('src/audio.c','''        if(audioState.stepDistance>=0.94f) {
            audioState.stepDistance=fmodf(audioState.stepDistance,0.94f);
            if(surface<0||surface>2)surface=0;
            AudioPlay((AudioCue)(surface*2+(audioState.stepIndex++%2)));
        }''','''        if(audioState.stepDistance>=FOOTSTEP_STRIDE&&
           audioState.clock-audioState.lastFootstep>=FOOTSTEP_MIN_INTERVAL) {
            audioState.stepDistance=fmodf(audioState.stepDistance,FOOTSTEP_STRIDE);
            audioState.lastFootstep=audioState.clock;
            if(surface<0||surface>2)surface=0;
            PlayFootstep(surface);
        }''')
edit('tools/validate_project.py','AddEnemy(g,ENEMY_BOSS,g->world.bossPos.x,g->world.bossPos.y,150)','AddEnemy(g,ENEMY_BOSS,g->world.bossPos.x,g->world.bossPos.y,LIGHTHOUSE_BOSS_MAX_HP)')
edit('tools/package_release.py',"        'step_grass_a', 'step_grass_b', 'step_stone_a', 'step_stone_b',", """        'step_grass_a', 'step_grass_b', 'step_grass_c', 'step_grass_d', 'step_grass_e', 'step_grass_f',
        'step_stone_a', 'step_stone_b', 'step_stone_c', 'step_stone_d', 'step_stone_e', 'step_stone_f',""")
edit('tools/generate_audio_sfx.py','''        pitch = 1 + 0.07 * variation
        write(f"step_grass_{suffix}", 0.15,
              lambda t, n, lo, bass: (0.55 * lo + 0.08 * n) * math.exp(-30 * t) +
              0.16 * math.sin(math.tau * 96 * pitch * t) * math.exp(-46 * t), 0.31)
        write(f"step_stone_{suffix}", 0.12,
              lambda t, n, lo, bass: 0.35 * bass * math.exp(-32 * t) +
              0.10 * n * math.exp(-90 * t) + 0.14 * note(t, 175 * pitch, 52), 0.34)
''','''        # Recorded grass/stone footsteps are maintained by import_footsteps.py.
        # Never overwrite those approved recordings with synthesized impacts.
        pitch = 1 + 0.07 * variation
''')
edit('tools/generate_audio_sfx.py','Generated 14 original PCM sound effects','Generated 10 original PCM sound effects; recorded footsteps preserved')
# Existing small-HP fixtures intentionally continue to exercise proportional scaling.
edit('tools/tests/test_battle.c','NEAR(g.enemies[0].hp,150)&&g.previewOnly','NEAR(g.enemies[0].hp,LIGHTHOUSE_BOSS_MAX_HP)&&g.previewOnly')
edit('tools/tests/test_battle.c','int main(void) {','''static void TestBossHealthScaling(void) {
    const float maxima[]={150,450,900};
    for(int i=0;i<3;i++) {
        float m=maxima[i],third=m/3;
        CHECK(BattleBossPhase(m,m)==1,"full health starts in phase one");
        CHECK(BattleBossPhase(2*third+.01f,m)==1,"above two-thirds is phase one");
        CHECK(BattleBossPhase(2*third,m)==2,"exact two-thirds begins phase two");
        CHECK(BattleBossPhase(third+.01f,m)==2,"above one-third remains phase two");
        CHECK(BattleBossPhase(third,m)==3,"exact one-third begins phase three");
        CHECK(BattleBossPhase(0,m)==3,"depleted core retains final phase for defeat animation");
        for(int phase=1;phase<=3;phase++) {
            Game g=Fight();g.enemies[0].maxHp=m;
            g.enemies[0].hp=phase==1?m:(phase==2?2*third+8:third+8);
            g.battleRound=3;BattleInit(&g);BattleStartAction(&g,0);ToEnemy(&g);
            CHECK(BattleBossPhase(g.enemies[0].hp,m)==phase,"attack logic enters ratio-selected phase");
            float expected=phase==1?23:phase==2?26:20;
            CHECK(NEAR(g.battle.damage,expected),"boss damage and heavy-attack cadence use percentage phase");
        }
    }
    CHECK(BattleBossPhase(20,0)==1,"invalid maximum health is safe");
    Release();Game g;GameStartBattlePreview(&g);
    CHECK(NEAR(g.enemies[0].hp,450)&&NEAR(g.enemies[0].maxHp,450),"preview uses the new 450 HP baseline");
    g.previewOnly=false;g.scene=SCENE_DIALOGUE;g.dialogueCount=1;g.dialoguePage=0;
    g.dialogueStartsBossBattle=true;g.dialogueInputBlocked=false;g.enemyCount=0;
    Confirm(&g);
    CHECK(g.scene==SCENE_BATTLE&&NEAR(g.enemies[0].hp,450)&&NEAR(g.enemies[0].maxHp,450),
          "normal story encounter shares the 450 HP baseline");
}
int main(void) {
    TestBossHealthScaling();''')
edit('tools/tests/test_audio.c','"hurt.wav","victory.wav"','"hurt.wav","victory.wav","step_grass_c.wav","step_grass_d.wav","step_grass_e.wav","step_grass_f.wav","step_stone_c.wav","step_stone_d.wav","step_stone_e.wav","step_stone_f.wav"')
edit('tools/tests/test_audio.c','for(int i=0;i<=AUDIO_STEP_WET_B;i++)total+=sounds[i].plays;','for(int i=0;i<AUDIO_CUE_COUNT;i++)if(i<=AUDIO_STEP_WET_B||i>=AUDIO_STEP_GRASS_C)total+=sounds[i].plays;')
edit('tools/tests/test_audio.c','AudioMovement(.2f,false,0,.05f)','AudioMovement(.24f,false,0,.05f)',3)
edit('tools/tests/test_audio.c','AudioResetMovement();AudioMovement(.95f,false,1,.05f);','AudioResetMovement();Tick(&game,.1f,true);Tick(&game,.1f,true);Tick(&game,.1f,true);AudioMovement(.6f,false,1,.05f);AudioMovement(.6f,false,1,.05f);')
edit('tools/tests/test_audio.c','AudioResetMovement();AudioMovement(.95f,false,2,.05f);','AudioResetMovement();Tick(&game,.1f,true);Tick(&game,.1f,true);Tick(&game,.1f,true);AudioMovement(.6f,false,2,.05f);AudioMovement(.6f,false,2,.05f);')
edit('tools/tests/test_audio.c','static void TestMissingAssetsAndDevice(void) {','''static void TestRecordedFootsteps(void) {
    CHECK(AudioFootstepSurface(AREA_CAMP)==0&&AudioFootstepSurface(AREA_GROVE)==0,"camp and grove use grass regardless of weather");
    CHECK(AudioFootstepSurface(AREA_RUINS)==1&&AudioFootstepSurface(AREA_LIGHTHOUSE)==1,"ruins and boss courtyard use stone");
    const int banks[2][6]={{AUDIO_STEP_GRASS_A,AUDIO_STEP_GRASS_B,AUDIO_STEP_GRASS_C,AUDIO_STEP_GRASS_D,AUDIO_STEP_GRASS_E,AUDIO_STEP_GRASS_F},
        {AUDIO_STEP_STONE_A,AUDIO_STEP_STONE_B,AUDIO_STEP_STONE_C,AUDIO_STEP_STONE_D,AUDIO_STEP_STONE_E,AUDIO_STEP_STONE_F}};
    for(int surface=0;surface<2;surface++) {
        ResetBackend();AudioInit();Game game=PlayingGame(surface?AREA_RUINS:AREA_GROVE);Settle(&game);
        int last[2]={-1,-1};bool seen[6]={0};
        for(int step=0;step<40;step++) {
            int before[6];for(int i=0;i<6;i++)before[i]=sounds[banks[surface][i]].plays;
            for(int tick=0;tick<4;tick++){Tick(&game,.08f,true);}
            AudioResetMovement();
            AudioMovement(.6f,false,surface,.05f);AudioMovement(.6f,false,surface,.05f);
            int picked=-1,total=0;
            for(int i=0;i<6;i++)if(sounds[banks[surface][i]].plays>before[i]){picked=i;total++;}
            CHECK(total==1,"one travel stride plays exactly one recorded step");
            CHECK(picked%2==step%2,"left and right recordings alternate");
            CHECK(picked!=last[step%2],"the same foot does not repeat its previous take");
            last[step%2]=picked;seen[picked]=true;
            CHECK(sounds[banks[surface][picked]].volume<=.64f,"footsteps stay below dialogue/impact volume");
            int count=StepCount();AudioMovement(.6f,false,surface,.001f);AudioMovement(.6f,false,surface,.001f);
            CHECK(StepCount()==count,"global cadence prevents rapid variant stacking");
        }
        for(int i=0;i<6;i++)CHECK(seen[i],"all six takes are reachable");
        AudioClose();
    }
}
static void TestMissingAssetsAndDevice(void) {''')
edit('tools/tests/test_audio.c','TestMovementAndContact();TestMissingAssetsAndDevice();','TestMovementAndContact();TestRecordedFootsteps();TestMissingAssetsAndDevice();')
print('Applied boss percentage phases, 450 HP shared baseline, recorded footsteps, map routing and regression coverage.')
edit('assets/audio/README.md','`sfx/` 为本项目生成的原创短音效，包括草地、石面、湿地脚步、对话翻页、交互、碰撞、冲刺、拾取、出招、受击与胜利。可使用 `python tools/generate_audio_sfx.py` 重建；正常运行无需安装音频工具。','''`sfx/` 中的草地、石路脚步改用 Fantozzi 的 CC0 录音并做短促、柔和化处理（不是从《星露谷物语》提取的游戏音频）。每种材质六个变体：左右脚交替，同一只脚不连续重复同一录音；轻微音高与音量变化降低循环感。主地图、辉光林地固定使用草地声；潮汐遗迹、灯塔庭院固定使用石路声，不再随雨天换成湿地声。脚步只跟随实际位移，静止、撞墙不连续触发。

`footsteps_manifest.json` 记录来源、处理方法和逐文件校验值。开发者可用 `tools/import_footsteps.py` 重建；普通编译和游戏运行直接使用已提交的 WAV，无需 ffmpeg 或下载素材。其他十个原有短音效仍由 `tools/generate_audio_sfx.py` 维护，该脚本不会再覆盖草地、石路录音。''')
credit='''\n## Recorded footsteps / 录音脚步声\n\nFantozzi's Footsteps (Grass/Sand & Stone), by Fantozzi; single-step cuts by qubodup.\nLicense: CC0-1.0. Source: https://opengameart.org/content/fantozzis-footsteps-grasssand-stone\nOriginal archive and hash: `third_party/audio/fantozzi/`; runtime provenance: `assets/audio/footsteps_manifest.json`.\nProject edits: mono conversion, EQ, onset/tail trimming, soft limiting and gain matching.\nStardew Valley was a requested tonal reference only; none of its audio was copied.\n'''
p=ROOT/'ASSET_SOURCES_AND_LICENSES.md';p.write_text(p.read_text(encoding='utf-8')+credit,encoding='utf-8')
p=ROOT/'third_party/audio/fantozzi/LICENSE.md';p.parent.mkdir(parents=True,exist_ok=True)
p.write_text('''# Fantozzi footsteps\n\nAuthor: Fantozzi. Single-step cuts: qubodup. License: CC0-1.0.\nSource: https://opengameart.org/content/fantozzis-footsteps-grasssand-stone\nOriginal archive: https://opengameart.org/sites/default/files/Fantozzi-footsteps.7z\nSHA256: 415d360e0911c1c1c5355ee94023b772e6f4044f59b9b580610e7dbffd1840fd\n\nThe pack contains six Sand and six Stone recordings, each with left/right takes.\nThe Sand recordings are EQ-processed for the grass/foliage role; Stone serves\nthe ruins and lighthouse paths. The unmodified archive is kept as source.\nProject edits are documented in assets/audio/footsteps_manifest.json.\nCC0 dedication: https://creativecommons.org/publicdomain/zero/1.0/\n''',encoding='utf-8')
(ROOT/'docs/BATTLE_IMPACT_02_1.md').write_text('''# IMPACT-02.1：Boss 血量与脚步声\n\n## Boss\n\n- 最大生命从 150 提升为 450；剧情战、直接挑战和独立预览使用同一常量。\n- 阶段一：剩余生命大于 2/3；阶段二：大于 1/3 且不超过 2/3；阶段三：不超过 1/3。\n- 450 HP 对应 300 / 150 两个切换点，血条标出这两个比例并显示剩余百分比。\n- HUD、伤害和重击频率调用同一比例函数，不再单独硬编码 100 / 50。\n- 保留命中结算、自动反击、格挡、潮湿导电、原美术、音乐和结局；不改存档格式。\n\n## 脚步声\n\n主地图 / 辉光林地：经过柔和化处理的草地沙沙落脚声。\n潮汐遗迹 / 灯塔庭院：短促石路落脚声。地图材质不受天气影响。\n每种材质六个 CC0 录音变体，左右脚交替，同脚变体不连用；音高、音量仅小幅变化。\n步距改为 1.12 个世界单位，最快间隔 0.20 秒；只按实际位移发声，不增加移动速度。\n来源与加工见 assets/audio/footsteps_manifest.json。未使用《星露谷物语》音频文件。\n\n## 更新\n\n在已建立的 mist-isle-impact02 独立目录中 fetch 后，\n用 git switch --detach origin/feature/animated-battle-v2 更新固定版本。\n正常启动 RUN_WINDOWS.bat 可体验四张地图；加 --battle-preview 可直接验证 450 HP Boss。\n窗口和战斗界面标识为 IMPACT-02.1。\n''',encoding='utf-8')
# Run actual sample/provenance validation in both platform packaging paths.
p=ROOT/'tools/validate_project.py';p.write_text(p.read_text(encoding='utf-8')+'\nimport runpy\nrunpy.run_path(str(ROOT / "tools/test_footstep_assets.py"), run_name="__main__")\n',encoding='utf-8')
