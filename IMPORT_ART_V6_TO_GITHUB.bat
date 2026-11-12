@echo off
setlocal EnableExtensions
title Mist Isle - Import complete art pack to GitHub

echo =====================================================
echo   MIST ISLE - ONE-TIME COMPLETE ART IMPORT
echo =====================================================
echo This will clone the project, import the v6 art pack,
echo validate all images, and push the assets to GitHub.
echo.

where git >nul 2>nul
if errorlevel 1 (
  echo ERROR: Git is not installed or not on PATH.
  pause
  exit /b 1
)
where python >nul 2>nul
if errorlevel 1 (
  echo ERROR: Python 3 is not installed or not on PATH.
  pause
  exit /b 1
)

set "REPO_DIR=%USERPROFILE%\Documents\GitHub\mist-isle"
if not exist "%REPO_DIR%\.git" (
  if exist "%REPO_DIR%" (
    echo ERROR: %REPO_DIR% exists but is not a Git clone.
    echo Rename or move that folder, then run this script again.
    pause
    exit /b 1
  )
  if not exist "%USERPROFILE%\Documents\GitHub" mkdir "%USERPROFILE%\Documents\GitHub"
  echo Cloning the project...
  git clone https://github.com/xily99371-del/mist-isle.git "%REPO_DIR%"
  if errorlevel 1 goto failed
)

echo.
echo Select MistIsle_ArtPack_v6_TopDown_Polished.zip in the file picker...
powershell -NoProfile -STA -ExecutionPolicy Bypass -Command ^
  "Add-Type -AssemblyName System.Windows.Forms; $d=New-Object System.Windows.Forms.OpenFileDialog; $d.Title='Select Mist Isle v6 art pack'; $d.Filter='Mist Isle art pack (*.zip)|*.zip'; $d.FileName='MistIsle_ArtPack_v6_TopDown_Polished.zip'; if($d.ShowDialog() -eq 'OK'){[IO.File]::WriteAllText('%TEMP%\mist_isle_artpack_path.txt',$d.FileName)}"
if errorlevel 1 goto failed
if not exist "%TEMP%\mist_isle_artpack_path.txt" (
  echo No art pack was selected.
  pause
  exit /b 1
)
set /p ART_ZIP=<"%TEMP%\mist_isle_artpack_path.txt"
del "%TEMP%\mist_isle_artpack_path.txt" >nul 2>nul
if not exist "%ART_ZIP%" goto failed

echo.
echo Extracting v6 art pack into the repository...
powershell -NoProfile -ExecutionPolicy Bypass -Command "Expand-Archive -LiteralPath '%ART_ZIP%' -DestinationPath '%REPO_DIR%' -Force"
if errorlevel 1 goto failed

cd /d "%REPO_DIR%"
echo.
echo Installing Pillow if needed...
python -c "import PIL" >nul 2>nul
if errorlevel 1 python -m pip install pillow
if errorlevel 1 goto failed

echo.
echo Generating Kenney runtime building atlas...
python tools\build_kenney_runtime_atlas.py
if errorlevel 1 goto failed

echo.
echo Validating all original and third-party art...
python tools\validate_project.py
if errorlevel 1 goto failed

echo.
echo Updating main branch...
git checkout main
if errorlevel 1 goto failed
git pull --rebase origin main
if errorlevel 1 goto failed

echo.
echo Staging the complete assets folder...
git add assets/
git diff --cached --stat
git diff --cached --quiet
if not errorlevel 1 (
  echo All v6 assets are already present in GitHub.
  goto success
)

if "%GIT_AUTHOR_NAME%"=="" (
  for /f "delims=" %%A in ('git config user.name') do set "GIT_AUTHOR_NAME=%%A"
)
if "%GIT_AUTHOR_EMAIL%"=="" (
  for /f "delims=" %%A in ('git config user.email') do set "GIT_AUTHOR_EMAIL=%%A"
)
if "%GIT_AUTHOR_NAME%"=="" set /p GIT_AUTHOR_NAME=Enter your Git commit name:
if "%GIT_AUTHOR_EMAIL%"=="" set /p GIT_AUTHOR_EMAIL=Enter your Git commit email:
git -c user.name="%GIT_AUTHOR_NAME%" -c user.email="%GIT_AUTHOR_EMAIL%" commit -m "Import complete Mist Isle v6 art assets"
if errorlevel 1 goto failed

echo.
echo Pushing assets to GitHub...
git push origin main
if errorlevel 1 goto failed

:success
echo.
echo =====================================================
echo SUCCESS: Complete art assets are now committed to GitHub.
echo A Windows build will start automatically.
echo Future git clone / git pull includes the full art pack.
echo =====================================================
echo.
echo Repository: %REPO_DIR%
echo Actions: https://github.com/xily99371-del/mist-isle/actions
pause
exit /b 0

:failed
echo.
echo IMPORT FAILED. No successful push was confirmed.
echo Check the error above, then rerun this script.
pause
exit /b 1
