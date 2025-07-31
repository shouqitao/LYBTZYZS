@echo off
chcp 65001 >nul
echo ====================================
echo   LYBT Complete Database Setup
echo ====================================
echo.

echo [INFO] Step 1: Create database using SQL script...
echo.

if exist "create-database.sql" (
    echo [INFO] Executing SQL script to create database...
    sqlcmd -S localhost -E -i create-database.sql
    
    if %ERRORLEVEL% EQU 0 (
        echo [OK] Database created successfully!
    ) else (
        echo [ERROR] Failed to create database using SQL script
        echo [INFO] Trying alternative method...
        call create-database.bat
    )
) else (
    echo [WARNING] SQL script not found, using PowerShell method...
    call create-database.bat
)

echo.
echo [INFO] Step 2: Starting WebAPI to initialize tables and data...
echo [INFO] The WebAPI will automatically create all tables and seed initial data
echo.

set ASPNETCORE_ENVIRONMENT=Production
set ASPNETCORE_URLS=http://0.0.0.0:5297

echo [INFO] Environment: %ASPNETCORE_ENVIRONMENT%
echo [INFO] Listen URL: %ASPNETCORE_URLS%
echo [INFO] Database: SQL Server (LYBTDB_Production)
echo.
echo [INFO] Starting WebAPI... (Press Ctrl+C to stop)
echo [INFO] Watch for "Database initialization completed" message
echo.

LYBT.WebAPI.exe

echo.
echo [INFO] WebAPI stopped
echo [INFO] Database setup process completed!
echo.
pause