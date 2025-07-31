@echo off
chcp 65001 >nul
echo ====================================
echo   LYBT Database Setup Tool
echo ====================================
echo.

echo [INFO] This tool will help create the LYBTDB_Production database
echo [INFO] Make sure SQL Server is running before proceeding
echo.

echo [INFO] Checking SQL Server connection...
powershell -Command "try { $conn = New-Object System.Data.SqlClient.SqlConnection('Server=localhost;Integrated Security=true;Connection Timeout=10;'); $conn.Open(); Write-Host '[OK] SQL Server connection successful' -ForegroundColor Green; $conn.Close(); } catch { Write-Host '[ERROR] SQL Server connection failed:' $_.Exception.Message -ForegroundColor Red; exit 1 }"

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Cannot connect to SQL Server. Please ensure:
    echo 1. SQL Server service is running
    echo 2. Windows Authentication is enabled
    echo 3. Current user has database creation permissions
    echo.
    pause
    exit /b 1
)

echo.
echo [INFO] Creating LYBTDB_Production database...
powershell -Command "$conn = New-Object System.Data.SqlClient.SqlConnection('Server=localhost;Integrated Security=true;Connection Timeout=30;'); $conn.Open(); $cmd = $conn.CreateCommand(); $cmd.CommandText = 'IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = ''LYBTDB_Production'') CREATE DATABASE LYBTDB_Production'; try { $result = $cmd.ExecuteNonQuery(); Write-Host '[OK] Database creation command executed successfully' -ForegroundColor Green; } catch { Write-Host '[ERROR] Failed to create database:' $_.Exception.Message -ForegroundColor Red; } finally { $conn.Close(); }"

echo.
echo [INFO] Verifying database creation...
powershell -Command "try { $conn = New-Object System.Data.SqlClient.SqlConnection('Server=localhost;Database=LYBTDB_Production;Integrated Security=true;Connection Timeout=10;'); $conn.Open(); Write-Host '[OK] LYBTDB_Production database is accessible' -ForegroundColor Green; $conn.Close(); } catch { Write-Host '[ERROR] Database verification failed:' $_.Exception.Message -ForegroundColor Red }"

echo.
echo [INFO] Database setup completed!
echo [INFO] Now you can start the WebAPI service with the SQL Server configuration.
echo.
echo Press any key to continue...
pause >nul