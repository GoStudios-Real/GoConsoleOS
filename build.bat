@echo off
REM ===========================================================================
REM GoConsoleOS Launcher
REM All executables are pre-built and ready in the boot/ directory.
REM Run this script to launch any GoStudios project.
REM ===========================================================================
setlocal

set BOOT_DIR=%~dp0boot

echo.
echo === GoStudios Launcher ===
echo.
echo Select a project to launch:
echo.
echo   [1] GoConsoleOS      - Full console OS experience
echo   [2] GoCore            - Core system service
echo   [3] GoStudiosLauncher - Minecraft-style launcher
echo.
set /p choice="Enter choice (1-3): "

if "%choice%"=="1" start "" "%BOOT_DIR%\GoConsole.exe"
if "%choice%"=="2" start "" "%BOOT_DIR%\gocore.exe"
if "%choice%"=="3" start "" "%BOOT_DIR%\GoStudiosLauncher.exe"

echo.
echo Launching...
timeout /t 2 /nobreak >nul
