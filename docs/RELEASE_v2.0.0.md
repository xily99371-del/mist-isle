# 雾屿回响 · Mist Isle v2.0.0

## 中文

第一章完整旅程的音画与结局更新。四张手工地图、正式人物和伙伴美术沿用已验收的 v1.0.0 基线。

- **音乐与音效**：五首区域和剧情音乐、14 种音效；脚步、对话、交互、碰撞和战斗获得声音反馈。Enemy 从灯塔入场连续播放至战斗，双语歌词跟随音乐进度。
- **战后与结局**：胜利后静音黑屏 1 秒，接四句战后对话；《晨间新闻》从 1:13 开始，持续伴随约 28 秒的三镜头灯塔结局。支持跳到完成页、重播及独立预览。
- **显示与输入**：Windows 文字按实际显示像素绘制；中文、歌词和结局字幕增加字重、描边与背景衬托。修复窗口内 Shift 切换输入法后的移动问题，Space 也可冲刺。
- **天气与地面**：修复辉光林地萤火离屏后不返回的问题，保持桌面 120 个光点和 Pi 56 个光点；修复首次开局、读档和切图的地面缓存。
- **完整运行包**：包含正式美术、字体、音乐、音效与来源说明，并记录版本号、源码提交和文件校验值。

**Windows**：完整解压 `MistIsle-Windows-v2.0.0.zip`，双击 `mist_isle.exe` 或 `RUN_WINDOWS.bat`。保留进度时，关闭旧游戏后复制 `mist_isle_save.dat`，在标题页按 F9；现有 v4/v5 存档仍可读取。`PREVIEW_ENDING.bat` 可预览结局。

**字幕来源**：Enemy 英文根据用户提供音频的语音识别整理，中文为项目自译；尚未按官方歌词或 LRC 逐字核验。

历史验证包括 Linux 渲染、音频/战后回归及 Windows 交叉编译；本次构建结果以关联 Actions 为准。Windows 输入法与听感、树莓派实际帧率仍需对应设备试玩。

## English

This release expands the first chapter's audio and finale while retaining the approved v1.0.0 character art and four handcrafted maps.

- Five music tracks and 14 sound effects accompany exploration, dialogue and combat. Enemy plays continuously across lighthouse entry and battle, with playback-synced bilingual lyrics.
- Boss victory leads to a one-second silent blackout and four lines of dialogue. Morning News starts at 01:13 and continues through the approximately 28-second, three-shot ending. Replay and standalone preview are included.
- Native-resolution Windows text, stronger subtitle contrast and window-scoped keyboard handling improve readability and input. Space also dashes.
- Fireflies now wrap back into view without reducing the original desktop/Pi counts. Ground caching is corrected for startup, loading and map changes.
- Playable packages include art, audio, fonts, attribution, build identity and checksums.

**Windows:** extract the entire ZIP and run `mist_isle.exe` or `RUN_WINDOWS.bat`. Copy an existing `mist_isle_save.dat` while the game is closed, then press F9 on the title screen. Existing v4/v5 saves remain supported.

Enemy lyrics were transcribed from user-supplied audio; Chinese translations are project-authored and have not been checked word for word against official lyrics/LRC. Prior validation covers Linux rendering, audio/state regressions and Windows cross-compilation; consult this release's Actions run for build results. Windows IME/audio and Raspberry Pi performance still require hardware playtesting.
