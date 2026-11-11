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
python -c "import zipfile; zipfile.ZipFile(r'assets\approved_art\MistIsle_RuntimeSprites_v1.zip').extractall('.')"
if errorlevel 1 goto failed

echo [3/3] Configuring and building...
cmake -S . -B build
if errorlevel 1 goto failed
cmake --build build --config Release
if errorlevel 1 goto failed

echo.
echo Build complete. Launching Mist Isle...
start "" "%~dp0build\Release\mist_isle.exe"
exit /b 0

:failed
echo.
echo Build failed. Check the error messages above.
pause
exit /b 1
