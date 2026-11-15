@echo off
setlocal
cd /d "%~dp0"

rem A packaged playtest contains the executable at the package root.
rem Launch it directly instead of trying to rebuild a source tree that is not included.
if not exist "%~dp0src\main.c" if exist "%~dp0mist_isle.exe" (
  echo Starting packaged Mist Isle Windows build...
  start "" "%~dp0mist_isle.exe" %*
  exit /b 0
)

echo [1/3] Checking Python and Pillow...
where python >nul 2>nul
if errorlevel 1 (
  echo Python 3 is required. Install Python and enable Add to PATH.
  pause
  exit /b 1
)
python -c "import PIL" >nul 2>nul
if errorlevel 1 python -m pip install pillow
if errorlevel 1 goto failed

echo [2/3] Verifying final artwork and preparing map assets...
python tools\prepare_runtime_assets.py
if errorlevel 1 goto failed
python tools\generate_pixel_assets.py
if errorlevel 1 goto failed

if not exist assets\characters\portraits\watcher_portrait.png goto missing_art
if not exist assets\characters\portraits\guardian_portrait.png goto missing_art
if not exist assets\environment\generated\nature_props_sheet.png goto missing_art
if not exist assets\environment\generated\glowglade_props_sheet.png goto missing_art
if not exist assets\environment\generated\tidal_ruins_props_sheet.png goto missing_art
if not exist assets\environment\generated\lighthouse_ruins_props_sheet.png goto missing_art
if not exist assets\characters\portraits\heroine_portrait.png (
  echo.
  echo Final artwork is missing. Pull the latest repository files and run again.
  goto failed
)
if not exist assets\characters\hero\heroine_pixel_4x4_64x80.png goto missing_art
if not exist assets\characters\npc\watcher_pixel_4x4_64x80.png goto missing_art
if not exist assets\characters\boss\lighthouse_guardian_4x2.png goto missing_art
if not exist assets\characters\portraits\moss_lumi_portrait.png goto missing_art
if not exist assets\characters\portraits\moatail_portrait.png goto missing_art
if not exist assets\characters\portraits\crystal_gecko_portrait.png goto missing_art
if not exist assets\environment\coastal_keeper_house.png goto missing_art
python tools\generate_ground_atlas.py
if errorlevel 1 goto failed
python tools\build_kenney_runtime_atlas.py
if errorlevel 1 goto failed

echo [3/3] Configuring and building...
where cmake >nul 2>nul
if errorlevel 1 (
  echo.
  echo CMake was not found. This is a missing Windows build dependency.
  echo Install CMake using one of these methods:
  echo   winget install Kitware.CMake
  echo or download the Windows x64 installer from https://cmake.org/download/
  echo During installation, select "Add CMake to the system PATH".
  echo Then close this window, open a NEW terminal, and run RUN_WINDOWS.bat again.
  echo.
  goto failed
)
cmake -S . -B build
if errorlevel 1 goto failed
cmake --build build --config Release
if errorlevel 1 goto failed

echo.
echo Build complete.
echo Official release character artwork verified.
if exist "%~dp0build\Release\mist_isle.exe" (
  start "" "%~dp0build\Release\mist_isle.exe" %*
  exit /b 0
)
if exist "%~dp0build\mist_isle.exe" (
  start "" "%~dp0build\mist_isle.exe" %*
  exit /b 0
)
echo Could not locate mist_isle.exe after build.
goto failed

:missing_art
echo Final runtime artwork is incomplete. Pull the latest repository files.
goto failed

:failed
echo.
echo Build failed. Check the error messages above.
pause
exit /b 1
