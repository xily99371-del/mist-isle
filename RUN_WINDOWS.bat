@echo off
setlocal
cd /d "%~dp0"

rem A packaged playtest contains the executable at the package root.
rem Launch it directly instead of trying to rebuild a source tree that is not included.
if exist "%~dp0mist_isle.exe" (
  echo Starting packaged Mist Isle Windows build...
  start "" "%~dp0mist_isle.exe"
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

echo [2/3] Generating pixel-art assets...
python tools\generate_pixel_assets.py
if errorlevel 1 goto failed
python tools\generate_heroine_highres.py
if errorlevel 1 goto failed
if not exist assets\characters\png\heroine_portrait.png echo INFO: using the approved heroine pixel-sheet fallback for local dialogue.
if not exist assets\characters\png\watcher_portrait.png echo INFO: using the generated Keeper pixel sprite for local dialogue.
if not exist assets\characters\png\lighthouse_guardian_portrait.png echo INFO: using the approved guardian pixel-sheet fallback for local dialogue.
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
if exist "%~dp0assets\characters\hero\heroine_approved_8x4.png" if exist "%~dp0assets\characters\boss\lighthouse_guardian_4x2.png" (
  echo Approved heroine and lighthouse guardian sprite sheets detected.
) else (
  echo NOTE: Approved character sheets are missing from this checkout.
  echo For an older local copy, extract MistIsle_ApprovedArt_Import_v2.zip into this folder
  echo and run MistIsle_ApprovedArt\IMPORT_APPROVED_ART.bat once.
)
if exist "%~dp0build\Release\mist_isle.exe" (
  start "" "%~dp0build\Release\mist_isle.exe"
  exit /b 0
)
if exist "%~dp0build\mist_isle.exe" (
  start "" "%~dp0build\mist_isle.exe"
  exit /b 0
)
echo Could not locate mist_isle.exe after build.
goto failed

:failed
echo.
echo Build failed. Check the error messages above.
pause
exit /b 1
