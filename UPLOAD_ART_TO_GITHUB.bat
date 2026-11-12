@echo off
setlocal
cd /d "%~dp0"
echo ==========================================
echo Mist Isle - Commit art assets to GitHub
echo ==========================================
git rev-parse --is-inside-work-tree >nul 2>nul
if errorlevel 1 (
  echo ERROR: This folder is not a Git repository.
  pause
  exit /b 1
)
git remote get-url origin >nul 2>nul
if errorlevel 1 (
  echo ERROR: No GitHub remote named origin is configured.
  pause
  exit /b 1
)
if not exist "assets\environment\area_props_96.png" (
  echo ERROR: v3 art pack not found.
  echo Extract MistIsle_ArtPack_v3_HubSubmaps.zip into this folder first.
  pause
  exit /b 1
)
echo.
echo The following asset files will be committed:
git status --short assets
echo.
git add assets/
git diff --cached --quiet
if not errorlevel 1 (
  echo No new asset changes to commit.
  pause
  exit /b 0
)
git commit -m "Add Mist Isle art pack v3"
if errorlevel 1 goto failed
git push origin HEAD
if errorlevel 1 goto failed
echo.
echo SUCCESS: Art assets are now stored in the GitHub repository.
echo Future source downloads will include these assets automatically.
pause
exit /b 0
:failed
echo.
echo Upload failed. Check GitHub authentication and the error above.
pause
exit /b 1
