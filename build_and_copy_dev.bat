@echo off

set MOD_NAME="SRMultiplayerSongGrabber"
set SYNTHRIDERS_MODS_DIR="C:\Program Files (x86)\Steam\steamapps\common\SynthRiders\Mods"

echo "Building dev configuration"
python.exe SRModCore\build.py --clean -n "%MOD_NAME%" --dotnet-version=net6.0 -c Debug -o build\localdev localdev build_files.txt || goto :ERROR

echo "Copying to SR directory..."
@REM Building spits out raw file structure in build/localdev/raw
copy build\localdev\Mods\* %SYNTHRIDERS_MODS_DIR% || goto :ERROR

echo "Done"
goto :EOF

:ERROR
echo "Error occurred in build script! Error code: %errorlevel%"
