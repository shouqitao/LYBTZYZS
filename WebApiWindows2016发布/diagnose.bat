@echo off
chcp 65001 >nul
echo ====================================
echo   LYBT WebAPI Troubleshooting
echo ====================================
echo.

echo [INFO] System Information:
echo OS: %OS%
echo Computer: %COMPUTERNAME%
echo User: %USERNAME%
echo.

echo [INFO] Checking .NET Runtime...
where dotnet >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo [OK] .NET CLI is available
    dotnet --version 2>nul
) else (
    echo [INFO] .NET CLI not found (using self-contained runtime)
)

echo.
echo [INFO] Checking file permissions...
if exist "LYBT.WebAPI.exe" (
    echo [OK] LYBT.WebAPI.exe exists
    dir LYBT.WebAPI.exe
) else (
    echo [ERROR] LYBT.WebAPI.exe not found!
    pause
    exit /b 1
)

echo.
echo [INFO] Checking configuration files...
if exist "appsettings.json" (
    echo [OK] appsettings.json exists
) else (
    echo [WARNING] appsettings.json missing
)

if exist "appsettings.Production.json" (
    echo [OK] appsettings.Production.json exists
) else (
    echo [WARNING] appsettings.Production.json missing
)

echo.
echo [INFO] Current directory: %CD%
echo [INFO] Available files:
dir *.exe *.dll *.json

echo.
echo [INFO] Testing with minimal configuration...
set ASPNETCORE_ENVIRONMENT=Development
set ASPNETCORE_URLS=http://*:5297

echo Environment: %ASPNETCORE_ENVIRONMENT%
echo Listen URL: %ASPNETCORE_URLS%
echo.

echo [INFO] Starting WebAPI with debug output...
LYBT.WebAPI.exe --urls "http://*:5297" --environment Development

echo.
echo Service stopped with exit code: %ERRORLEVEL%
pause