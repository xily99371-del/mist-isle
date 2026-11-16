# 雾屿回响 · Mist Isle v3.0.0

## 中文

**最终音效与宠物战斗正式整合版。** 在 v2.0.0 完整第一章的基础上，合入已验收的 IMPACT-02.3 战斗和 IMPACT-02.2 最终脚步声。包含完整探索、四张地图、伙伴招募、剧情与灯塔结局，不是单独的战斗演示。

### 三宠战斗

旅人只指挥，没有战斗血条、攻击或受伤判定。三位已招募伙伴同场，固定三张宠物卡片与 1/2/3 快捷键；体力耗尽时卡片变灰而不消失。全队体力耗尽即战败，回到营地恢复。

每次行动都有蓄力、出招、命中与收招；伤害、治疗仅在命中时结算。敌人自动回击。苔灯灵（苔藓灵）为全队已招募宠物各恢复基础 18 点体力，每级增加 3 点，包含自身与暂时体力耗尽的队友，各自按上限封顶。泡泡水流之后可在下一回合接晶弧导电。

### 首领阶段与逐击格挡

Boss 最大生命 450，按最大生命的 2/3、1/3 切换阶段，不再依赖固定血量阈值。

| 阶段 | 普通攻击总伤害 / 击数 | 共鸣重击总伤害 / 击数 | 每击格挡窗口 |
|---|---|---|---|
| 一：高于 2/3 | 18 / 1 | 28 / 2 | 命中前 0.20 秒 |
| 二：高于 1/3 且不超过 2/3 | 26 / 2 | 42 / 3 | 命中前 0.16 秒 |
| 三：不超过 1/3 | 34 / 3 | 60 / 4 | 命中前 0.12 秒 |

以上为未格挡、未潮湿时整套动作的总伤害。每击独立弹出 Space 提示，目标光圈大小、外圈起始尺寸和收缩节奏变化。格挡成功只降低当前一击 55% 伤害；漏挡不影响后续机会，长按或提前连按不能自动格挡整套。目标倒下后取消剩余攻击，不转打旅人。

### 最终脚步声与保留内容

海岸营地、辉光林地使用踩草录音；潮汐遗迹、灯塔庭院使用干净硬石路录音，不叠加沙子或碎石层。每种材质六个播放变体，来源、处理与哈希见 `assets/audio/footsteps_manifest.json` 和 `assets/audio/FOOTSTEP_CREDITS.md`。

正式角色、v7 立绘、v8 场景、四张手工地图、五首音乐与字幕保留。Enemy 从灯塔入场连续播放至战斗；胜利后静音黑屏 1 秒，接续战后对话，《晨间新闻》从 1:13 开始并持续伴随结局。v2.0.0 标签与原运行包保持不变。

### 下载、启动与旧存档

- **Windows x64**：完整解压 `MistIsle-Windows-v3.0.0.zip`，双击 `mist_isle.exe` 或 `RUN_WINDOWS.bat`。不需要 Python、CMake 或 Visual Studio。
- **Raspberry Pi 5 / ARM64 Linux**：完整解压 `MistIsle-RaspberryPi5-ARM64-v3.0.0.tar.gz`，运行 `./RUN_RASPBERRY_PI.sh`。桌面环境及最低 glibc 要求以包内 `START_HERE.txt` / `BUILD_INFO.json` 为准；旧系统可按 `docs/RASPBERRY_PI5.md` 本机构建。
- **保留进度**：关闭旧游戏，将 `mist_isle_save.dat` 复制到新版根目录，标题界面按 F9。存档结构与原有 v4/v5 读取逻辑保持不变；不要用旧 assets 文件夹覆盖新版。

`PREVIEW_BATTLE.bat` 直接体验 Boss 战；`PREVIEW_ENDING.bat` 预览结局，均不读写旅程存档。源码使用 `RUN_WINDOWS.bat --battle-preview` / `--ending-preview`；正常启动不加参数。

探索：WASD / 方向键移动，Shift / Space 冲刺，E 互动。战斗：1/2/3 选择宠物，Enter 出招，Space 逐击格挡，每次按后松开。F6 声音、F8 字幕、F5 保存、标题页 F9 读取。

### 构建与来源

版本号统一来自 `VERSION`；运行包内的 `BUILD_INFO.json` 和 `SHA256SUMS` 记录实际源码提交、架构、素材与文件校验值。发布工作流在构建与校验成功后创建标签，上传草稿附件，重新下载比对哈希，再公开 Release。树莓派实机帧率仍需上板验证。

音乐与歌词来源说明沿用 `assets/audio/README.md`；音乐不属于 CC0 素材许可。脚步素材分别遵循 CC-BY-3.0 与 CC0，署名文件随包提供。

## English

**The full v3.0.0 release integrates the accepted final audio and pet-combat upgrades into the complete v2.0 chapter.** All four regions, exploration, recruitment, story, approved artwork, music and the lighthouse finale remain included.

Only the three recruited pets fight; the traveller commands without combat HP or attacks. Fixed party cards retain their identities even when exhausted. Moss Lumi heals every recruited pet, including itself and exhausted teammates, at impact; water-to-lightning synergy remains available. Enemy attacks resolve automatically with visible wind-up, impact and recovery.

The 450-HP Boss uses relative 2/3 and 1/3 stage boundaries. Normal combo totals are 18/26/34 damage across 1/2/3 hits; resonance totals are 28/42/60 across 2/3/4 hits. Per-hit guard windows narrow from 0.20 to 0.16 to 0.12 seconds. Each Space prompt has variable ring geometry and timing. A successful guard reduces that hit's damage by 55%; a miss does not invalidate later prompts. Damage totals are before guarding or wet-state reduction.

Camp/Grove use dedicated grass recordings. Ruins/Lighthouse use clean hard-surface footsteps without sand or gravel layers. Six playback variants per material are shipped with source attribution and checksums. The continuous Enemy playback, one-second silent victory blackout, dialogue and Morning News-backed ending are retained.

**Windows:** fully extract the ZIP and run `mist_isle.exe` or `RUN_WINDOWS.bat`; no development tools are required. **ARM64:** extract the tarball and run `./RUN_RASPBERRY_PI.sh`, observing its bundled desktop/glibc requirements. Close the old game, copy `mist_isle_save.dat` into the new folder and press F9 at the title to continue. Existing save formats remain supported.

`PREVIEW_BATTLE.bat` and `PREVIEW_ENDING.bat` provide isolated previews without reading/writing journey saves. In combat use 1/2/3, Enter and one deliberate Space press per incoming hit. This release leaves the v2.0.0 tag and attachments untouched.

Build identity and file hashes are recorded in `BUILD_INFO.json` / `SHA256SUMS`. Automated build and regression checks are distinct from physical Raspberry Pi performance testing. Retained music and subtitle provenance is documented in `assets/audio/README.md`; footstep credits ship in `assets/audio/FOOTSTEP_CREDITS.md`.
