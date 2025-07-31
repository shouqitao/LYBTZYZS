@echo off
chcp 65001 >nul
echo ====================================
echo   LYBT Medical System WebAPI v1.0
echo ====================================
echo.

set ASPNETCORE_ENVIRONMENT=Production
set ASPNETCORE_URLS=http://localhost:5297;https://localhost:7297

echo [INFO] Environment: %ASPNETCORE_ENVIRONMENT%
echo [INFO] Listen URLs: %ASPNETCORE_URLS%
echo [INFO] Start Date: %date%
echo [INFO] Working Dir: %CD%
echo.

echo [INFO] Starting LYBT WebAPI Service...
LYBT.WebAPI.exe

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] WebAPI startup failed, error code: %ERRORLEVEL%
    echo [INFO] Please check if ports are occupied or configuration is correct
    pause
) else (
    echo.
    echo [INFO] WebAPI service stopped
)

pause