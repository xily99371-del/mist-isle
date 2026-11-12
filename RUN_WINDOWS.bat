@echo off
setlocal
cd /d "%~dp0"
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
if exist "%~dp0assets\environment\environment_props_96.png" (
  echo Approved art pack detected. Loading imported character and environment assets.
) else (
  echo NOTE: Approved art pack not found. The game will use fallback visuals.
  echo Extract MistIsle_ArtIntegration_v2_1.zip into this folder to enable approved art.
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
