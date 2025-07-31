@echo off
chcp 65001 >nul
echo ====================================
echo   LYBT WebAPI Service Startup
echo ====================================
echo.

echo [INFO] Setting Production Environment...
set ASPNETCORE_ENVIRONMENT=Production
set ASPNETCORE_URLS=http://localhost:5297;https://localhost:7297

echo [INFO] Starting WebAPI Service...
echo [INFO] Listen Ports: 5297 (HTTP), 7297 (HTTPS)
echo [INFO] Press Ctrl+C to stop service
echo.

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

echo.
echo Press any key to exit...
pause >nul