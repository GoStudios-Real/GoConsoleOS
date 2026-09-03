# GoConsoleOS USB Portable Builder
# Creates a self-contained portable package that can run from any USB drive

param(
    [string]$OutputPath = "C:\Users\RhysC\Downloads\GoConsole\GoConsoleOS\usb-portable",
    [string]$SourcePath = "C:\Users\RhysC\Downloads\GoConsole\GoConsoleOS\src",
    [string]$PublishPath = "C:\Users\RhysC\Downloads\GoConsole\GoConsoleOS\publish-dev"
)

Write-Host "=== GoConsoleOS USB Portable Builder ===" -ForegroundColor Cyan
Write-Host ""

# Clean output
if (Test-Path $OutputPath) {
    Write-Host "Cleaning existing output..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force $OutputPath
}

# Create directory structure
Write-Host "Creating directory structure..." -ForegroundColor Green
$dirs = @(
    "$OutputPath",
    "$OutputPath\boot",
    "$OutputPath\system",
    "$OutputPath\system\acc",
    "$OutputPath\system\discord",
    "$OutputPath\system\webview2",
    "$OutputPath\games",
    "$OutputPath\captures",
    "$OutputPath\saves",
    "$OutputPath\themes"
)
foreach ($dir in $dirs) {
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
}

# Copy executables
Write-Host "Copying executables..." -ForegroundColor Green
$exes = @(
    "GoConsole.exe",
    "GoConsoleWatcher.exe",
    "GoUsbMaker.exe",
    "GoStudiosLauncher.exe",
    "gocore.exe",
    "GoConsoleDiscordBot.exe"
)
foreach ($exe in $exes) {
    $src = Join-Path $PublishPath $exe
    if (Test-Path $src) {
        Copy-Item $src $OutputPath -Force
        Write-Host "  Copied $exe" -ForegroundColor Gray
    } else {
        Write-Host "  Missing $exe - skipping" -ForegroundColor DarkYellow
    }
}

# Copy DLLs
Write-Host "Copying libraries..." -ForegroundColor Green
Get-ChildItem "$PublishPath\*.dll" | ForEach-Object {
    Copy-Item $_.FullName $OutputPath -Force
}

# Copy config
Write-Host "Creating config files..." -ForegroundColor Green

# init.cfg
$initCfg = @"
[system]
os_name=GoConsoleOS
version=2.2.0
console_name=GoConsoleOS Portable
theme=dark
language=en

[network]
enable_networking=true
check_updates=true
update_url=https://updates.goconsoleos.com
cloud_server_url=https://gostudios.net/api
server_port=39210
"@
Set-Content -Path "$OutputPath\boot\init.cfg" -Value $initCfg

# Discord config template
$discordConfig = @"
{
  "token": "",
  "tokenType": "user"
}
"@
Set-Content -Path "$OutputPath\system\discord\config.json" -Value $discordConfig

# Launcher script
$launcher = @"
@echo off
title GoConsoleOS Portable
echo.
echo  GoConsoleOS Portable v2.2.0
echo  ========================
echo.
echo  Starting GoConsoleOS...
echo.
start "" "%~dp0GoConsole.exe"
echo.
echo  Press any key to exit...
pause >nul
"@
Set-Content -Path "$OutputPath\Start.bat" -Value $launcher

# Auto-run script for USB detection
$autorun = @"
@echo off
title GoConsoleOS Portable - Auto Run
echo GoConsoleOS detected - starting...
start "" "%~dp0GoConsole.exe"
"@
Set-Content -Path "$OutputPath\autorun.bat" -Value $autorun

# README
$readme = @"
GoConsoleOS USB Portable v2.2.0
===============================

QUICK START:
1. Insert this USB drive into any Windows PC
2. Run Start.bat (or GoConsole.exe directly)
3. GoConsoleOS will launch and be accessible on your network

ANDROID CONNECTION:
- Install GoConsoleOS apps from the Play Store
- Connect to this console via WiFi
- The console will be discoverable on your local network

DISCORD BOT:
1. Edit system\discord\config.json with your bot token
2. Run GoConsoleDiscordBot.exe
3. Use !help in Discord for available commands

CONTENTS:
- GoConsole.exe - Main console application
- GoConsoleWatcher.exe - USB auto-launch daemon
- GoUsbMaker.exe - USB drive creator
- GoStudiosLauncher.exe - Game launcher
- GoConsoleDiscordBot.exe - Discord bot
- boot\ - System configuration
- system\ - User data and settings
- games\ - Game installations
- captures\ - Screenshots and recordings
- saves\ - Game save files
- themes\ - Custom themes

NETWORK API:
- Discovery: UDP port 39100
- Link Protocol: TCP port 39101
- HTTP API: TCP port 39210

For more information, visit: https://goconsoleos.gostudios.app.com
"@
Set-Content -Path "$OutputPath\README.txt" -Value $readme

# Create USB label file
Set-Content -Path "$OutputPath\GOCONSOLEOS.txt" -Value "GoConsoleOS USB Portable v2.2.0"

Write-Host ""
Write-Host "=== Build Complete ===" -ForegroundColor Green
Write-Host "Output: $OutputPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "To install on USB drive:" -ForegroundColor Yellow
Write-Host "1. Format USB drive as FAT32 or NTFS" -ForegroundColor Gray
Write-Host "2. Copy all files from $OutputPath to USB root" -ForegroundColor Gray
Write-Host "3. Run Start.bat from the USB drive" -ForegroundColor Gray
Write-Host ""
Write-Host "Or use GoUsbMaker.exe to create a bootable USB" -ForegroundColor Gray
