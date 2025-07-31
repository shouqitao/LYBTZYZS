@echo off
chcp 65001 >nul
echo ====================================
echo   LYBT WebAPI Database Diagnostic Tool
echo ====================================
echo.

echo [INFO] Checking SQL Server service status...
sc query MSSQLSERVER >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    sc query MSSQLSERVER | findstr "STATE"
    echo [OK] SQL Server service installed
) else (
    echo [INFO] Checking SQL Server Express...
    sc query "MSSQL$SQLEXPRESS" >nul 2>&1
    if %ERRORLEVEL% EQU 0 (
        sc query "MSSQL$SQLEXPRESS" | findstr "STATE"
        echo [OK] SQL Server Express installed
    ) else (
        echo [WARNING] No SQL Server services found
    )
)

echo.
echo [INFO] Checking LocalDB...
sqllocaldb info >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo [OK] LocalDB available
    sqllocaldb info
) else (
    echo [WARNING] LocalDB not available
)

echo.
echo [INFO] Testing database connections...
echo Please choose database type:
echo [1] SQL Server (localhost)
echo [2] SQL Server (LocalDB) 
echo [3] SQLite
echo [0] Exit
echo.

:retry
set /p choice=Please enter option (0-3): 

if "%choice%"=="1" goto sqlserver
if "%choice%"=="2" goto localdb  
if "%choice%"=="3" goto sqlite
if "%choice%"=="0" goto exit

echo Invalid option, please try again.
goto retry

:sqlserver
echo.
echo [INFO] Using SQL Server configuration...
if exist appsettings.Production.json (
    copy /Y appsettings.Production.json appsettings.Production.backup.json >nul
    echo [INFO] Backup created: appsettings.Production.backup.json
)
echo [INFO] Starting WebAPI with SQL Server...
set ASPNETCORE_ENVIRONMENT=Production
set ASPNETCORE_URLS=http://0.0.0.0:5297

echo [INFO] Environment: %ASPNETCORE_ENVIRONMENT%
echo [INFO] Listen URL: %ASPNETCORE_URLS%
echo [INFO] Database: SQL Server (localhost)
echo.
echo Press Ctrl+C to stop the service
echo.

LYBT.WebAPI.exe

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] WebAPI failed to start, error code: %ERRORLEVEL%
    echo [INFO] Please check database connection and port availability
    echo.
)
goto cleanup

:localdb
echo.
echo [INFO] Using LocalDB configuration...
if exist appsettings.Production.json (
    copy /Y appsettings.Production.json appsettings.Production.backup.json >nul
    echo [INFO] Backup created: appsettings.Production.backup.json
)
if exist appsettings.Production.LocalDB.json (
    copy /Y appsettings.Production.LocalDB.json appsettings.Production.json >nul
    echo [INFO] Configuration switched to LocalDB
) else (
    echo [ERROR] LocalDB configuration file not found!
    goto cleanup
)
echo [INFO] Starting WebAPI with LocalDB...
set ASPNETCORE_ENVIRONMENT=Production
set ASPNETCORE_URLS=http://0.0.0.0:5297

echo [INFO] Environment: %ASPNETCORE_ENVIRONMENT%
echo [INFO] Listen URL: %ASPNETCORE_URLS%
echo [INFO] Database: LocalDB
echo.
echo Press Ctrl+C to stop the service
echo.

LYBT.WebAPI.exe

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] WebAPI failed to start, error code: %ERRORLEVEL%
    echo [INFO] Please check LocalDB installation and configuration
    echo.
)
goto cleanup

:sqlite
echo.
echo [INFO] Using SQLite configuration...
if exist appsettings.Production.json (
    copy /Y appsettings.Production.json appsettings.Production.backup.json >nul
    echo [INFO] Backup created: appsettings.Production.backup.json
)
if exist appsettings.Production.SQLite.json (
    copy /Y appsettings.Production.SQLite.json appsettings.Production.json >nul
    echo [INFO] Configuration switched to SQLite
) else (
    echo [ERROR] SQLite configuration file not found!
    goto cleanup
)
echo [INFO] Starting WebAPI with SQLite...
set ASPNETCORE_ENVIRONMENT=Production
set ASPNETCORE_URLS=http://0.0.0.0:5297

echo [INFO] Environment: %ASPNETCORE_ENVIRONMENT%
echo [INFO] Listen URL: %ASPNETCORE_URLS%
echo [INFO] Database: SQLite (LYBT_Production.db)
echo.
echo Press Ctrl+C to stop the service
echo.

LYBT.WebAPI.exe

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] WebAPI failed to start, error code: %ERRORLEVEL%
    echo [INFO] Please check file permissions and SQLite support
    echo.
)
goto cleanup

:cleanup
echo.
echo [INFO] Service stopped
if exist appsettings.Production.backup.json (
    echo [INFO] Restoring original configuration...
    copy /Y appsettings.Production.backup.json appsettings.Production.json >nul
    del appsettings.Production.backup.json >nul
    echo [INFO] Configuration restored
)
goto exit

:exit
echo.
echo Press any key to exit...
pause >nul