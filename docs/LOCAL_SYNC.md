# 更新到 v3.0.0 / Update to v3.0.0

正式仓库：`https://github.com/xily99371-del/mist-isle`。`main` 是正式开发主分支，`v3.0.0` 是本次固定发布版本。v3.0.0 将最终脚步与宠物战斗接入完整 v2.0 旅程；旧标签与旧运行包不覆盖。

## 直接运行

完整解压 `MistIsle-Windows-v3.0.0.zip` 到新文件夹，双击 `mist_isle.exe` 或 `RUN_WINDOWS.bat`；无需开发环境。`PREVIEW_BATTLE.bat` 为独立 Boss 战，`PREVIEW_ENDING.bat` 为结局预览。

保留进度：关闭游戏，将旧目录的 `mist_isle_save.dat` 复制到新版根目录，在标题页按 F9。不要覆盖新版 assets。预览入口不读写旅程存档。

## 已使用 mist-isle-impact02 独立目录

在该目录打开 PowerShell，将已验收的独立工作目录更新到正式标签；不需要退回旧 v1.0 目录，也不需要把旧分支强行合并：

```powershell
& {
    $ErrorActionPreference = "Stop"
    git fetch origin --tags
    if ($LASTEXITCODE -ne 0) { throw "下载更新失败，已停止。" }
    git switch --detach v3.0.0
    if ($LASTEXITCODE -ne 0) { throw "切换失败，已停止，不会运行旧版。" }
    Get-Content VERSION
    git log -1 --oneline
    .\RUN_WINDOWS.bat
}
```

应显示 `3.0.0`。只体验战斗：`.\RUN_WINDOWS.bat --battle-preview`。正常启动不加参数，仍是完整旅程。

## 日常跟随 main 的源码目录

仅对本来就使用 main 的工作目录执行，任何一步失败即停止：

```powershell
& {
    $ErrorActionPreference = "Stop"
    git fetch origin --tags
    if ($LASTEXITCODE -ne 0) { throw "下载更新失败。" }
    git switch main
    if ($LASTEXITCODE -ne 0) { throw "切换 main 失败，请保留并检查本地修改。" }
    git pull --ff-only origin main
    if ($LASTEXITCODE -ne 0) { throw "无法快进更新，已停止。" }
    Get-Content VERSION
    .\RUN_WINDOWS.bat
}
```

Git 提示素材冲突时，不要执行 `reset --hard`、`clean -fd`，也不要删除未跟踪图片。保留原工作目录，可在已有仓库根目录创建一个新的固定版本工作目录：

```powershell
git worktree add --detach ..\mist-isle-v3 v3.0.0
```

命令成功后进入新目录构建。旧目录、素材和存档不受影响。完整检查以 Release 标签、Actions 结果和包内 BUILD_INFO.json 为准。

## English

Extract the complete v3.0.0 Windows package and run its executable or launcher. Close the old game before copying `mist_isle_save.dat`; press F9 at the title. Preview launchers never touch journey saves.

The existing detached `mist-isle-impact02` worktree can fetch tags and switch directly to `v3.0.0`. The guarded PowerShell blocks above stop on failure rather than accidentally launching an older build. Main checkouts can update with a fast-forward-only pull. Preserve local edits; a separate `git worktree add --detach` checkout avoids overwriting old art or saves. Do not replace new assets with an older ZIP.
