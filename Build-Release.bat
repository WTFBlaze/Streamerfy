@echo off
setlocal enabledelayedexpansion

echo === Building Streamerfy ===

REM Step 0: Set project paths
set "PROJECT_DIR=Streamerfy"
set "PROJECT_FILE=%PROJECT_DIR%\Streamerfy.csproj"
set "VERSION_FILE=%PROJECT_DIR%\Services\VersionService.cs"
set "PUBLISH_DIR=%PROJECT_DIR%\bin\Release\net6.0-windows\win-x64\publish"
set "RELEASE_DIR=Releases"

REM Step 1: Extract version from VersionService.cs
set "VERSION=UNKNOWN"
for /f "delims=" %%A in ('findstr /C:"CurrentVersion = " "%VERSION_FILE%"') do (
    set "line=%%A"
)

REM Extract version using string parsing
for /f "tokens=2 delims==;" %%B in ("!line!") do (
    set "raw=%%B"
    set "VERSION=!raw: =!"
    set "VERSION=!VERSION:"=!"
)

if "%VERSION%"=="UNKNOWN" (
    echo Could not extract version from VersionService.cs
    pause
    exit /b 1
)

REM Step 2: Clean release folder
if exist "%RELEASE_DIR%" (
    echo Cleaning release folder...
    del /q "%RELEASE_DIR%\*"
) else (
    mkdir "%RELEASE_DIR%"
)

REM Step 3: Build project
echo Publishing Streamerfy v%VERSION%...
dotnet publish %PROJECT_FILE% -f net6.0-windows -c Release -r win-x64 --self-contained true ^
  /p:PublishSingleFile=true ^
  /p:IncludeNativeLibrariesForSelfExtract=true ^
  /p:EnableCompressionInSingleFile=true ^
  /p:IncludeAllContentForSelfExtract=true ^
  /p:StripDebugSymbols=true

REM Step 4: Move the output .exe with versioned name
set "EXE_PATH="
for %%F in ("%PUBLISH_DIR%\*.exe") do (
    set "EXE_PATH=%%~fF"
)

if defined EXE_PATH (
    set "EXE_OUT=Streamerfy-v%VERSION%.exe"
    echo Moving !EXE_PATH! to %RELEASE_DIR%\!EXE_OUT!...
    copy /Y "!EXE_PATH!" "%RELEASE_DIR%\!EXE_OUT!" >nul
    echo Build complete: %RELEASE_DIR%\!EXE_OUT!
) else (
    echo ERROR: Could not find an .exe in %PUBLISH_DIR%
)

echo.
pause
endlocal
