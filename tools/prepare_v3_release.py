from pathlib import Path
import hashlib
import json

ROOT = Path.cwd()
def edit(path, old, new, count=1):
    p = ROOT/path
    text = p.read_text(encoding='utf-8')
    assert text.count(old) == count, f'{path}: unexpected source ({text.count(old)} matches)'
    p.write_text(text.replace(old, new), encoding='utf-8')

# Only release presentation changes; accepted gameplay and asset files are untouched.
edit('VERSION', '2.0.0', '3.0.0')
edit('src/main.c', '" - 战斗演出版 IMPACT-02.3"', '" · 第一章：失落的灯塔"')
edit('src/battle_render.inc', '"伙伴战斗 · IMPACT-02.3"', '"伙伴战斗 · v" MISTISLE_VERSION')
edit('tools/test_battle_visuals.py', "exe=b/'battle-visual-qa'", "exe=b/'battle-visual-qa'\nversion=(ROOT/'VERSION').read_text(encoding='utf-8').strip()")
edit('tools/test_battle_visuals.py', "['cc','-std=c11'", "['cc',f'-DMISTISLE_VERSION=\"{version}\"','-std=c11'")

p = ROOT/'README.md'
s = p.read_text(encoding='utf-8')
# Preserve galleries, historical v2.0 notes and approved artwork.
s = s.replace('version-2.0.0-', 'version-3.0.0-')
s = s.replace('Download and Play — v2.0.0', 'Download and Play — v3.0.0')
s = s.replace('/download/v2.0.0/', '/download/v3.0.0/')
s = s.replace('MistIsle-Windows-v2.0.0.zip', 'MistIsle-Windows-v3.0.0.zip')
s = s.replace('MistIsle-RaspberryPi5-ARM64-v2.0.0.tar.gz', 'MistIsle-RaspberryPi5-ARM64-v3.0.0.tar.gz')
s = s.replace('/releases/tag/v2.0.0', '/releases/tag/v3.0.0')
s = s.replace('[v2.0.0 Release]', '[v3.0.0 Release]')
s = s.replace('v2.0.0 收录第一章完整旅程，包含四张相互连通的手工设计地图，并完善区域音乐、交互音效、战后对话与灯塔结局演出。',
    'v3.0.0 将已验收的最终音效与宠物战斗系统正式接入 v2.0.0 完整旅程。四张地图、探索任务、伙伴招募、音乐和灯塔结局全部保留，不是独立战斗演示。')
s = s.replace('### v2.0.0 更新', '''### v3.0.0 更新

- **三宠战斗**：旅人只指挥，三位伙伴同场并各有固定血条；全队体力耗尽即战败。
- **完整动作反馈**：蓄力、出招、命中、收招；伤害和治疗在命中时结算，敌人自动反击。
- **全队治疗与联动**：苔灯灵为所有已招募宠物恢复体力，包括自身与暂时倒下的队友；泡泡水流后可衔接晶弧导电。
- **450 HP 首领**：按 2/3、1/3 血量划分三阶段，普通攻击逐步升级为 1/2/3 连击，共鸣重击为 2/3/4 连击；逐击 Space 格挡，光圈尺寸和节奏变化，后期判定更严格。
- **最终脚步声**：主地图、辉光林地使用踩草素材；潮汐遗迹、灯塔庭院使用干净硬石路素材，不叠加沙子或碎石层。

详细数值、操作与存档迁移见 [v3.0.0 发行说明](docs/RELEASE_v3.0.0.md)。Windows 运行包附带 `PREVIEW_BATTLE.bat`，也可用 `RUN_WINDOWS.bat --battle-preview` 直接体验 Boss 战；此入口不读写旅程存档。

### 沿用 v2.0.0 的内容''')
s = s.replace('通过 Tab 可以切换当前出战伙伴。', '探索时通过 Tab 切换伙伴；战斗时用固定的 1 / 2 / 3 选择宠物，Enter 确认。')
s = s.replace('| 左 / 右 Shift 或 Space | 冲刺 |', '| 左 / 右 Shift 或 Space | 探索时冲刺 |')
s = s.replace('| Tab | 切换当前出战伙伴 |', '| Tab | 探索时切换伙伴 |\n| 1 / 2 / 3 + Enter | 战斗中选择宠物并出招 |\n| Space | 战斗中逐击格挡，按后松开再应对下一击 |')
s = s.replace('增加分地面脚步声、对话翻页、交互、碰撞、冲刺、拾取与战斗音效。',
    '草地与硬石路使用最终分离材质脚步，每类六个播放变体；主地图和辉光林地为草地，遗迹和灯塔为硬石路，不受天气切换影响。另有对话翻页、交互、碰撞、冲刺、拾取与战斗音效。')
s = s.replace('Version 2.0.0 contains the complete first-chapter journey across four handcrafted, connected areas, with regional music, interaction sounds, post-battle dialogue and an expanded lighthouse finale.',
    'Version 3.0.0 integrates the accepted final footsteps and pet-only combat into the complete v2.0 journey: all four areas, quests, recruitment, music and the lighthouse finale remain included. This is the full game, not a standalone battle demo.')
s = s.replace('### What Is New in v2.0.0', '''### What Is New in v3.0.0

- Three-pet combat with fixed party HP cards. The traveller only commands; total party exhaustion ends the battle.
- Wind-up, attacks, impact-timed damage/healing, recoil and automatic enemy retaliation.
- Moss Lumi heals the whole recruited party, including itself and exhausted teammates. Water into lightning remains a functional combo.
- A 450-HP Boss uses relative 2/3 and 1/3 thresholds. Normal attacks progress from 1 to 3 hits; resonance attacks from 2 to 4. Each hit has an independent Space guard and variable timing rings.
- Final grass recordings for Camp/Grove and clean hard-surface recordings for Ruins/Lighthouse, without sand or gravel layers.

See the [v3.0.0 release notes](docs/RELEASE_v3.0.0.md) for damage values, controls and save migration. `PREVIEW_BATTLE.bat` or `RUN_WINDOWS.bat --battle-preview` starts an isolated Boss encounter without reading or writing journey saves.

### Retained from v2.0.0''')
s = s.replace('Press Tab to switch the active companion.', 'Use Tab during exploration; in battle, fixed keys 1/2/3 choose pets and Enter confirms.')
s = s.replace('| Left / Right Shift or Space | Dash |', '| Left / Right Shift or Space | Dash during exploration |')
s = s.replace('| Tab | Switch active companion |', '| Tab | Switch companion during exploration |\n| 1 / 2 / 3 + Enter | Select a pet and execute its skill in battle |\n| Space | Guard each enemy hit; release before the next prompt |')
s = s.replace('Surface-specific footsteps, dialogue, interactions, collisions, dash, pickups, and battle actions now have sound effects.',
    'Final grass footsteps play in Camp/Grove and clean hard-surface footsteps in Ruins/Lighthouse. Each material has six playback variants and is not replaced by rainy-weather samples. Dialogue, interactions, collisions, dash, pickups and battle effects remain included.')
p.write_text(s, encoding='utf-8')

# Include the battle entry point and current controls in both playable packages.
edit('tools/package_release.py', "    'assets/audio/enemy.tsv', 'assets/audio/enemy_energy.tsv',", "    'assets/audio/enemy.tsv', 'assets/audio/enemy_energy.tsv',\n    'assets/audio/footsteps_manifest.json', 'assets/audio/FOOTSTEP_CREDITS.md',")
edit('tools/package_release.py', "        '此包不附带测试存档。预览结局不会读取或修改旅程存档。\\n\\n'", "        '战斗：1/2/3 选择宠物，Enter 出招；Space 逐击格挡，每次按后松开。\\n'\n        '此包不附带测试存档。战斗与结局预览均不读取或修改旅程存档。\\n\\n'")
edit('tools/package_release.py', "        shutil.copy2(ROOT / 'RUN_WINDOWS.bat', package / 'RUN_WINDOWS.bat')", "        shutil.copy2(ROOT / 'RUN_WINDOWS.bat', package / 'RUN_WINDOWS.bat')\n        shutil.copy2(ROOT / 'PREVIEW_BATTLE.bat', package / 'PREVIEW_BATTLE.bat')")
edit('tools/package_release.py', 'PREVIEW_ENDING.bat 可直接预览结局。', 'PREVIEW_BATTLE.bat 可直接体验 Boss 战；PREVIEW_ENDING.bat 可预览结局。')
edit('tools/package_release.py', "                  '结局预览：./RUN_RASPBERRY_PI.sh --ending-preview。\\n')", "                  '战斗预览：./RUN_RASPBERRY_PI.sh --battle-preview。\\n'\n                  '结局预览：./RUN_RASPBERRY_PI.sh --ending-preview。\\n')")
edit('tools/package_release.py', "        for filename in (f'RELEASE_v{version}.md', 'HUB_SUBMAPS.md', 'V1_0_0_PARITY_AUDIT.md'):", "        for filename in (f'RELEASE_v{version}.md', 'HUB_SUBMAPS.md', 'V1_0_0_PARITY_AUDIT.md',\n                         'BATTLE_IMPACT_02_2.md', 'BATTLE_IMPACT_02_3.md', 'LOCAL_SYNC.md'):")
edit('tools/package_release.py', "        art = json.loads(read('assets/characters/runtime_art.json'))", """        steps = json.loads(read('assets/audio/footsteps_manifest.json'))
        require(steps['format_version'] == 2 and len(steps['files']) == 12,
                'Final grass/stone footstep manifest is required.')
        for item in steps['files']:
            require(actual.get(item['path']) == item['sha256'], 'Final footstep sample mismatch: ' + item['path'])
        if info['target'] == 'windows-x64':
            require('PREVIEW_BATTLE.bat' in relative_names and b'--battle-preview' in read('PREVIEW_BATTLE.bat'),
                    'Battle preview launcher is missing.')
        require(f'docs/RELEASE_v{version}.md' in relative_names, 'Release notes are missing.')
        art = json.loads(read('assets/characters/runtime_art.json'))""")

p = ROOT/'docs/PROJECT_STATUS.md'
s = p.read_text(encoding='utf-8').replace('2.0.0', '3.0.0')
heading = '## 本轮已实现'
assert heading in s
s = s.replace(heading, '''## v3.0.0 正式整合

- 最终战斗基线为 `7d99ab4fd29f10d3fbffb4bc59fa7e88a664deb4`（IMPACT-02.3），音效采用最终 IMPACT-02.2 分离材质版本。发布准备不更改已验收的规则或 WAV。
- 三宠同场、旅人仅指挥、全队治疗、450 HP 百分比三阶段、逐击连击与变化光圈格挡均用于完整旅程。
- 游戏窗口、战斗 HUD、源码 VERSION、README 与运行包统一为 v3.0.0。
- PREVIEW_BATTLE.bat 是不触碰旅程存档的独立入口；正常 RUN_WINDOWS.bat 仍启动完整游戏。
- 发布说明见 RELEASE_v3.0.0.md，更新和旧工作目录迁移见 LOCAL_SYNC.md。v2.0.0 标签与原附件保留，不覆盖。

## 沿用的 v2.0 音画与结局实现''')
p.write_text(s,encoding='utf-8')

compile((ROOT/'tools/package_release.py').read_text(encoding='utf-8'), 'tools/package_release.py', 'exec')
manifest=json.loads((ROOT/'assets/audio/footsteps_manifest.json').read_text(encoding='utf-8'))
assert len(manifest['files'])==12
for item in manifest['files']:
    assert hashlib.sha256((ROOT/item['path']).read_bytes()).hexdigest()==item['sha256']
print('v3.0.0 release metadata and launchers prepared; 12 final footstep hashes retained.')
