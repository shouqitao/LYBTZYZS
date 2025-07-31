@echo off
chcp 65001 >nul
title LYBT Medical System - Main Console

:MAIN_MENU
cls
echo.
echo ====================================================
echo    LYBT Traditional Chinese Medicine System
echo              Main Control Console
echo ====================================================
echo.
echo [Development Tools]
echo   1. Start Development Server
echo   2. Database Management Tool
echo.
echo [Build and Deploy]
echo   3. Publish Production Version
echo   4. One-Click Deploy (With Config Wizard)
echo.
echo [Maintenance Tools]
echo   5. Build Entire Solution
echo   6. Run Tests
echo   7. Clean Build Cache
echo.
echo [Help and Info]
echo   8. View System Information
echo   9. Open Project Documentation
echo.
echo   0. Exit
echo.

set /p "CHOICE=Please select an option (0-9): "

if "%CHOICE%"=="1" goto DEV_SERVER
if "%CHOICE%"=="2" goto DATABASE_MANAGER
if "%CHOICE%"=="3" goto PUBLISH
if "%CHOICE%"=="4" goto DEPLOY
if "%CHOICE%"=="5" goto BUILD_SOLUTION
if "%CHOICE%"=="6" goto RUN_TESTS
if "%CHOICE%"=="7" goto CLEAN_BUILD
if "%CHOICE%"=="8" goto SYSTEM_INFO
if "%CHOICE%"=="9" goto OPEN_DOCS
if "%CHOICE%"=="0" goto EXIT

echo Invalid option, please try again...
timeout /t 2 >nul
goto MAIN_MENU

:DEV_SERVER
echo.
echo Starting Development Server...
call "%~dp0start-dev.bat"
goto MAIN_MENU

:DATABASE_MANAGER
echo.
echo Opening Database Management Tool...
call "%~dp0database-manager.bat"
goto MAIN_MENU

:PUBLISH
echo.
echo Publishing Production Version...
call "%~dp0publish-production.bat"
echo.
echo Press any key to return to main menu...
pause >nul
goto MAIN_MENU

:DEPLOY
echo.
echo One-Click Deploy (Configuration Wizard)...
call "%~dp0deploy-all.bat"
echo.
echo Press any key to return to main menu...
pause >nul
goto MAIN_MENU

:BUILD_SOLUTION
cls
echo.
echo Building Entire Solution...
cd /d "%~dp0.."
dotnet build LYBTZYZS.sln
if %ERRORLEVEL% equ 0 (
    echo Build SUCCESS!
) else (
    echo Build FAILED!
)
echo.
echo Press any key to return to main menu...
pause >nul
goto MAIN_MENU

:RUN_TESTS
cls
echo.
echo Running Tests...
cd /d "%~dp0.."
dotnet test
if %ERRORLEVEL% equ 0 (
    echo Tests PASSED!
) else (
    echo Tests FAILED!
)
echo.
echo Press any key to return to main menu...
pause >nul
goto MAIN_MENU

:CLEAN_BUILD
cls
echo.
echo Cleaning Build Cache...
cd /d "%~dp0.."
echo Cleaning BIN directory...
if exist "BIN" rmdir /s /q "BIN"
echo Cleaning obj directories...
for /d /r . %%d in (obj) do @if exist "%%d" rd /s /q "%%d"
for /d /r . %%d in (bin) do @if exist "%%d" rd /s /q "%%d"
dotnet clean
echo Clean completed!
echo.
echo Press any key to return to main menu...
pause >nul
goto MAIN_MENU

:SYSTEM_INFO
cls
echo.
echo ====================================================
echo              System Information
echo ====================================================
echo.
echo [Project Information]
echo   Project Name: LYBT Traditional Chinese Medicine System
echo   Version: 1.0.0
echo   Architecture: ASP.NET Core 8.0 + WPF
echo.
echo [System Environment]
dotnet --version >nul 2>&1
if %ERRORLEVEL% equ 0 (
    echo   .NET Version: 
    dotnet --version
) else (
    echo   .NET Status: Not Installed
)
echo   Operating System: %OS%
echo   Computer Name: %COMPUTERNAME%
echo.
echo [Directory Structure]
echo   Project Root: %~dp0..
echo   Backend API: src\Backend\Services\LYBT.WebAPI
echo   Frontend WPF: src\Frontend\Desktop\Shell
echo   Publish Directory: publish
echo.
echo [Default Ports]
echo   Development: http://localhost:5297
echo   Production: http://localhost:5000
echo   Swagger Documentation: /swagger
echo.
echo [Important Files]
echo   Development Guide: docs\development\CLAUDE.md
echo   Test Report: docs\TestReport.md
echo   Configuration: src\Backend\Services\LYBT.WebAPI\appsettings.json
echo.
echo Press any key to return to main menu...
pause >nul
goto MAIN_MENU

:OPEN_DOCS
echo.
echo Opening Project Documentation...
if exist "%~dp0..\docs\development\CLAUDE.md" (
    start notepad "%~dp0..\docs\development\CLAUDE.md"
)
if exist "%~dp0..\docs\测试报告.md" (
    start notepad "%~dp0..\docs\测试报告.md"
)
if exist "%~dp0..\README.md" (
    start notepad "%~dp0..\README.md"
)
echo Documentation opened
timeout /t 2 >nul
goto MAIN_MENU

:EXIT
cls
echo.
echo ====================================================
echo              Thank You for Using
echo    LYBT Traditional Chinese Medicine System
echo              Main Control Console
echo ====================================================
echo.
echo System is complete and running stable
echo If you need help, please check the documentation in docs directory
echo If you encounter issues, please check the log files
echo.
echo Goodbye!
echo.
timeout /t 3 >nul
exit /b 0