@echo off
setlocal enabledelayedexpansion

echo === Building Streamerfy ===

REM Step 1: Publish
dotnet publish Streamerfy/Streamerfy.csproj -f net6.0-windows -c Release -r win-x64 --self-contained true ^
  /p:PublishSingleFile=true ^
  /p:IncludeNativeLibrariesForSelfExtract=true ^
  /p:EnableCompressionInSingleFile=true ^
  /p:IncludeAllContentForSelfExtract=true ^
  /p:StripDebugSymbols=true

REM Step 2: Set paths
set "PUBLISH_DIR=Streamerfy\bin\Release\net6.0-windows\win-x64\publish"
set "RELEASE_DIR=Releases"

REM Step 3: Make sure the release folder exists
if not exist "%RELEASE_DIR%" (
    mkdir "%RELEASE_DIR%"
)

REM Step 4: Find the .exe and copy it
set "EXE_PATH="
for %%F in ("%PUBLISH_DIR%\*.exe") do (
    set "EXE_PATH=%%~fF"
    set "EXE_NAME=%%~nxF"
)

if defined EXE_PATH (
    echo Moving !EXE_NAME! to %RELEASE_DIR%...
    copy /Y "!EXE_PATH!" "%RELEASE_DIR%\!EXE_NAME!" >nul
    echo Build complete: %RELEASE_DIR%\!EXE_NAME!
) else (
    echo ERROR: Could not find an EXE in %PUBLISH_DIR%
)

echo.
pause
endlocal
