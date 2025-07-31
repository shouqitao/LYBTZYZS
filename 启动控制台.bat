@echo off
chcp 65001 >nul
echo Starting LYBT Management System Console...
cd /d "%~dp0scripts"
if not exist "%~dp0scripts\main-en.bat" (
    echo Error: scripts\main-en.bat not found!
    echo Please make sure you are running this from the project root directory.
    pause
    exit /b 1
)
call main-en.bat