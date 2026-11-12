@echo off
setlocal EnableExtensions
cd /d "%~dp0"
echo ==========================================
echo Mist Isle - One-time art import to GitHub
echo ==========================================

git rev-parse --is-inside-work-tree >nul 2>nul
if errorlevel 1 (
  echo ERROR: This folder is not a Git clone.
  echo First run: git clone https://github.com/xily99371-del/mist-isle.git
  pause
  exit /b 1
)

git remote get-url origin
if errorlevel 1 goto failed

if not exist "assets\environment\area_props_96.png" (
  if exist "MistIsle_ArtPack_v4_TopDown_Refined.zip" (
    echo Extracting approved v3 art pack...
    powershell -NoProfile -ExecutionPolicy Bypass -Command "Expand-Archive -LiteralPath '%CD%\MistIsle_ArtPack_v3_HubSubmaps.zip' -DestinationPath '%CD%' -Force"
    if errorlevel 1 goto failed
  ) else (
    echo ERROR: Art pack not found.
    echo Put MistIsle_ArtPack_v3_HubSubmaps.zip beside this script.
    pause
    exit /b 1
  )
)

echo.
echo Checking required approved art files...
for %%F in (
  "assets\environment\environment_props_96.png"
  "assets\environment\area_props_96.png"
  "assets\characters\hero\hero_walk_4dir_64.png"
  "assets\characters\companions\moss_lumi_idle_64.png"
  "assets\characters\companions\moatail_idle_64.png"
  "assets\characters\companions\crystal_gecko_idle_64.png"
  "assets\tiles\png\island_iso_tiles.png"
) do (
  if not exist "%%~F" (
    echo ERROR: Missing %%~F
    goto failed
  )
)

python -m pip install --quiet pillow
if errorlevel 1 goto failed
python tools\validate_project.py
if errorlevel 1 goto failed

echo.
echo Syncing latest main branch...
git checkout main
if errorlevel 1 goto failed
git pull --rebase origin main
if errorlevel 1 goto failed

echo.
echo Staging only runtime art assets...
git add assets/
git diff --cached --stat
git diff --cached --quiet
if not errorlevel 1 (
  echo Art assets are already committed. No upload needed.
  pause
  exit /b 0
)

git commit -m "Add approved Mist Isle art pack v3"
if errorlevel 1 goto failed
git push origin main
if errorlevel 1 goto failed

echo.
echo SUCCESS: Art assets are now committed to GitHub.\necho The full v4 top-down terrain atlas is included.
echo A complete Windows build will start automatically.
pause
exit /b 0

:failed
echo.
echo FAILED. No successful upload was confirmed.
echo Check GitHub sign-in, proxy connectivity and the error above.
pause
exit /b 1
