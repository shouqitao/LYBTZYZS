@echo off
chcp 65001 >nul
echo ====================================
echo   Port and Environment Diagnostic
echo ====================================
echo.

echo [INFO] Checking port availability...
netstat -an | findstr ":5000"
if %ERRORLEVEL% EQU 0 (
    echo [WARNING] Port 5000 is already in use!
    echo [INFO] Processes using port 5000:
    netstat -ano | findstr ":5000"
) else (
    echo [OK] Port 5000 is available
)

echo.
netstat -an | findstr ":5001"
if %ERRORLEVEL% EQU 0 (
    echo [WARNING] Port 5001 is already in use!
    echo [INFO] Processes using port 5001:
    netstat -ano | findstr ":5001"
) else (
    echo [OK] Port 5001 is available
)

echo.
echo [INFO] Trying to start with HTTP only (port 5000)...
set ASPNETCORE_ENVIRONMENT=Production
set ASPNETCORE_URLS=http://localhost:5000

echo [INFO] Environment: %ASPNETCORE_ENVIRONMENT%
echo [INFO] Listen URL: %ASPNETCORE_URLS%
echo.

LYBT.WebAPI.exe

echo.
echo [INFO] Service stopped or failed
pause