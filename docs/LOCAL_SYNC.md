# 更新到 v2.0.0 / Update to v2.0.0

正式仓库为 `https://github.com/xily99371-del/mist-isle`，开发主分支为 `main`。当前源码版本见根目录 `VERSION`，实际启用素材与历史验证见 [PROJECT_STATUS.md](PROJECT_STATUS.md)。

v2.0.0 整合音乐、音效、双语字幕与战后结局，保留正式美术，并修复原生文字、萤火循环和地面缓存。发布包、源码标签和 `BUILD_INFO.json` 应指向同一次发布提交；远端完成情况以 [v2.0.0 Release](https://github.com/xily99371-del/mist-isle/releases/tag/v2.0.0) 及对应 Actions 结果为准。

## 直接运行 Windows 包

下载 `MistIsle-Windows-v2.0.0.zip`，完整解压到新文件夹后双击 `mist_isle.exe` 或 `RUN_WINDOWS.bat`。包内已含运行素材，无需 Python、CMake 或 Visual Studio。

保留旧进度：先关闭游戏，把旧文件夹里的 `mist_isle_save.dat` 复制到新版文件夹；启动后在标题页按 F9。v2.0.0 保持原有 v4/v5 存档读取逻辑。

## 更新本地源码并构建

在本地 `mist-isle` 目录打开 PowerShell：

```powershell
git fetch origin --tags
git switch main
git pull --ff-only origin main
Get-Content VERSION
git log -1 --oneline
.\RUN_WINDOWS.bat
```

只预览结局，不读取或修改存档：

```powershell
.\RUN_WINDOWS.bat --ending-preview
```

如果 Git 提示本地修改会被覆盖，先用 `git status --short` 查看修改，保留自己的代码后再合并。不要用旧压缩包覆盖更新后的 `assets`、`maps` 或生成头文件。

## English

Download and fully extract `MistIsle-Windows-v2.0.0.zip`, then run `mist_isle.exe` or `RUN_WINDOWS.bat`. No development tools are required for the playable package. Close the old game before copying `mist_isle_save.dat` into the new folder; press F9 on the title screen to load. Existing v4/v5 save formats remain supported.

The PowerShell commands above update an existing `main` checkout and rebuild it. `VERSION` gives the source version; each release package records its actual commit in `BUILD_INFO.json`. Check the versioned Release and Actions results to confirm publication. Preserve local edits when Git reports a conflict, and keep the updated runtime assets together.
