@echo off
chcp 65001 >nul
title LYBT System - Script Verification

echo ====================================================
echo    LYBT System - Script Verification
echo ====================================================
echo.

echo Checking script files...
echo.

set "PROJECT_ROOT=%~dp0"
set "SCRIPTS_DIR=%PROJECT_ROOT%scripts"
set "WEBAPI_DIR=%PROJECT_ROOT%src\Backend\Services\LYBT.WebAPI"

echo [1] Project Root Directory: %PROJECT_ROOT%
if exist "%PROJECT_ROOT%" (
    echo    Status: EXISTS
) else (
    echo    Status: NOT FOUND
)
echo.

echo [2] Scripts Directory: %SCRIPTS_DIR%
if exist "%SCRIPTS_DIR%" (
    echo    Status: EXISTS
) else (
    echo    Status: NOT FOUND
)
echo.

echo [3] WebAPI Directory: %WEBAPI_DIR%
if exist "%WEBAPI_DIR%" (
    echo    Status: EXISTS
) else (
    echo    Status: NOT FOUND
)
echo.

echo [4] Script Files:
set "SCRIPT_FILES=main-en.bat start-dev-en.bat publish-production.bat deploy-all.bat database-manager.bat"

for %%f in (%SCRIPT_FILES%) do (
    if exist "%SCRIPTS_DIR%\%%f" (
        echo    scripts\%%f: EXISTS
    ) else (
        echo    scripts\%%f: NOT FOUND
    )
)
echo.

echo [5] .NET Installation:
dotnet --version >nul 2>&1
if %ERRORLEVEL% equ 0 (
    echo    .NET Status: INSTALLED
    echo    .NET Version: 
    dotnet --version
) else (
    echo    .NET Status: NOT INSTALLED
)
echo.

echo [6] Project Files:
if exist "%WEBAPI_DIR%\LYBT.WebAPI.csproj" (
    echo    LYBT.WebAPI.csproj: EXISTS
) else (
    echo    LYBT.WebAPI.csproj: NOT FOUND
)

if exist "%WEBAPI_DIR%\appsettings.json" (
    echo    appsettings.json: EXISTS
) else (
    echo    appsettings.json: NOT FOUND
)
echo.

echo ====================================================
echo Verification complete! 
echo.
echo If all items show 'EXISTS' and .NET is 'INSTALLED',
echo then the scripts should work properly.
echo.
echo Press any key to exit...
pause >nul