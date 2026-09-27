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

:: Find and run the setup exe if available
if exist "%~dp0dist\Mercury-Setup.exe" (
    echo  Launching Windows Setup Wizard...
    start "" "%~dp0dist\Mercury-Setup.exe"
    exit /b 0
)
if exist "%~dp0Mercury-Setup.exe" (
    echo  Launching Windows Setup Wizard...
    start "" "%~dp0Mercury-Setup.exe"
    exit /b 0
)

:: Otherwise seamlessly execute the automated Windows installer
echo  Pre-compiled setup wizard not found in dist\.
echo  Starting automated direct installation for Windows...
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0install.ps1"
if errorlevel 1 (
    echo.
    echo  [!] Installation encountered an issue.
    pause
    exit /b 1
)

echo.
echo  ============================================
echo   Installation Finished Successfully!
echo  ============================================
echo.
pause
