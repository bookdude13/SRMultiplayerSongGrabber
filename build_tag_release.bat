@echo off

IF %1.==. goto :Usage

set MOD_NAME="SRMultiplayerSongGrabber"
set VERSION=%1

echo "Building release..."
python.exe SRModCore\build.py --clean --tag -n "%MOD_NAME%" --dotnet-version=net6.0 -c Release %VERSION% build_files.txt || goto :ERROR

echo "Done"
goto :EOF

:ERROR
echo "Error occurred in build script! Error code: %errorlevel%"

:Usage
echo "Usage: ./build_release.bat <version>"
