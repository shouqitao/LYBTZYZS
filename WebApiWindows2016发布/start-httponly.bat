@echo off
chcp 65001 >nul
echo ====================================
echo   Port and Environment Diagnostic
echo ====================================
echo.

echo [INFO] Checking port availability...
netstat -an | findstr ":5297"
if %ERRORLEVEL% EQU 0 (
    echo [WARNING] Port 5297 is already in use!
    echo [INFO] Processes using port 5297:
    netstat -ano | findstr ":5297"
) else (
    echo [OK] Port 5297 is available
)

echo.
netstat -an | findstr ":7297"
if %ERRORLEVEL% EQU 0 (
    echo [WARNING] Port 7297 is already in use!
    echo [INFO] Processes using port 7297:
    netstat -ano | findstr ":7297"
) else (
    echo [OK] Port 7297 is available
)

echo.
echo [INFO] Trying to start with HTTP only (port 5297)...
set ASPNETCORE_ENVIRONMENT=Production
set ASPNETCORE_URLS=http://localhost:5297

echo [INFO] Environment: %ASPNETCORE_ENVIRONMENT%
echo [INFO] Listen URL: %ASPNETCORE_URLS%
echo.

LYBT.WebAPI.exe

echo.
echo [INFO] Service stopped or failed
pause