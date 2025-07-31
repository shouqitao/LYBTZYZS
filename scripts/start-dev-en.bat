@echo off
chcp 65001 >nul
title LYBT Medical System - Development Server

echo.
echo ====================================================
echo    LYBT Traditional Chinese Medicine System
echo              Development Server Launcher
echo ====================================================
echo.

:: Set project root directory
set "PROJECT_ROOT=%~dp0.."
set "WEBAPI_DIR=%PROJECT_ROOT%\src\Backend\Services\LYBT.WebAPI"

:: Check if project directory exists
if not exist "%WEBAPI_DIR%" (
    echo ERROR: Cannot find WebAPI project directory
    echo Expected path: %WEBAPI_DIR%
    echo.
    pause
    exit /b 1
)

echo Project Root: %PROJECT_ROOT%
echo WebAPI Directory: %WEBAPI_DIR%
echo.

:: Change to WebAPI directory
cd /d "%WEBAPI_DIR%"

echo Starting development server...
echo Tip: Press Ctrl+C to stop the server
echo.

:: Check if dotnet is available
dotnet --version >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo ERROR: .NET is not installed or not in PATH
    echo Please install .NET 8.0 SDK from https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

:: Start development server
dotnet run

echo.
echo Server stopped
pause