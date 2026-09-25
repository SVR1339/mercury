@echo off
title Mercury - Build
echo.
echo  ==========================================
echo   Mercury Build Script
echo  ==========================================
echo.

:: Check .NET SDK
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo  ERROR: .NET 8 SDK is required.
    echo  Download: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo  [1/3] Building GUI...
dotnet publish src\UniversalGitHubInstaller\UniversalGitHubInstaller.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o publish\gui
if errorlevel 1 (
    echo  ERROR: GUI build failed!
    pause
    exit /b 1
)
echo  [OK] GUI built.
echo.

echo  [2/3] Building CLI...
dotnet publish src\UniversalGitHubInstaller.Cli\UniversalGitHubInstaller.Cli.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish\cli
if errorlevel 1 (
    echo  ERROR: CLI build failed!
    exit /b 1
)
copy /y publish\cli\ugi.exe publish\cli\mercury.exe >nul
echo  [OK] CLI built (mercury.exe and ugi.exe).
echo.

echo  [3/3] Building installer...
where iscc >nul 2>&1
if errorlevel 1 (
    if exist "%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe" (
        "%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe" installer.iss
    ) else if exist "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" (
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer.iss
    ) else (
        echo  SKIP: Inno Setup not found. Install from https://jrsoftware.org/isinfo.php
        echo  GUI and CLI are built in publish\ folder.
    )
) else (
    iscc installer.iss
)

echo.
echo  ==========================================
echo   BUILD COMPLETE
echo  ==========================================
echo.
echo  Installer: dist\Mercury-Setup.exe
echo  GUI:       publish\gui\UniversalGitHubInstaller.exe
echo  CLI:       publish\cli\mercury.exe ^& publish\cli\ugi.exe
echo.
