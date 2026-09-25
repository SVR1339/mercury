@echo off
title Mercury - Setup
echo.
echo  ============================================
echo   Mercury - Setup v1.0.0
echo   Universal GitHub & Terminal Installer
echo  ============================================
echo.
echo  This will install Mercury on your computer.
echo.
echo  Press any key to begin installation...
echo  (or close this window to cancel)
echo.
pause >nul

:: Find and run the setup exe
if exist "%~dp0dist\Mercury-Setup.exe" (
    start "" "%~dp0dist\Mercury-Setup.exe"
) else if exist "%~dp0Mercury-Setup.exe" (
    start "" "%~dp0Mercury-Setup.exe"
) else (
    echo.
    echo  ERROR: Setup file not found!
    echo  Please run build.bat or check dist\Mercury-Setup.exe.
    echo.
    pause
)
