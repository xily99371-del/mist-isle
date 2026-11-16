# 雾屿回响 · Mist Isle v3.0.1

## 中文

**入口封面与主菜单更新版。** 在 v3.0.0 完整游戏基础上，正式加入用户确认的月夜遗迹入口封面，并将开始冒险、继续旅程、设置、退出四个菜单接入实际游戏逻辑。

- 使用原始封面图 `assets/ui/title_cover.png`，不重新绘制角色或改变构图。
- README 展示同一张正式封面。
- 菜单支持鼠标点击、方向键/W/S、Enter/Space、F9 继续与 Esc 返回。
- 继续旅程沿用原存档读取；无存档或坏存档明确提示。
- 新游戏如已有存档，先确认并创建不重名备份；备份失败时不覆盖。
- 设置提供声音、歌词字幕与无边框显示切换。
- 完整保留 v3.0.0 的四张地图、正式角色与场景、五首音乐、连续 Enemy 播放、最终脚步声、三宠战斗、全队治疗、450 HP 三阶段 Boss 连击格挡、战后对话和结局。
- 存档格式保持兼容。v3.0.0 标签与发行附件保留不变。

### 下载与升级

Windows x64：解压 `MistIsle-Windows-v3.0.1.zip` 后运行 `mist_isle.exe` 或 `RUN_WINDOWS.bat`。
Raspberry Pi 5 ARM64：解压 `MistIsle-RaspberryPi5-ARM64-v3.0.1.tar.gz` 后运行 `./RUN_RASPBERRY_PI.sh`。

保留进度：关闭旧游戏，将 `mist_isle_save.dat` 复制到新版根目录，在标题页按 F9。不要用旧 assets 覆盖新版。

### 验证边界

Windows 与 ARM64 构建、菜单/存档安全、战斗、音频、结局和素材回归由发布流水线验证。自动化构建不等同于 Windows 实机输入体验或树莓派实机帧率验收。

## English

**Title-screen update.** v3.0.1 adds the approved moonlit ruins cover and fully functional Start, Continue, Settings and Exit menu actions. The same cover is displayed in this repository's README.

The menu supports mouse, arrow keys/W/S, Enter/Space, F9 Continue and Esc. Continue preserves existing save loading; missing/corrupt saves are reported. Starting a new game over an existing save requires confirmation and a unique backup; backup failure prevents overwrite.

All v3.0.0 gameplay remains: four handcrafted areas, approved characters and art, soundtrack and continuous Enemy playback, final grass/clean-stone footsteps, three-pet combat, party-wide healing, the 450-HP three-phase Boss with multi-hit guards, post-battle dialogue and ending. Save compatibility is retained, and the v3.0.0 release remains available.

Windows and Raspberry Pi 5 ARM64 packages are provided below. Copy `mist_isle_save.dat` into the new package folder and press F9 to continue. Automated builds do not replace physical Raspberry Pi performance testing.
