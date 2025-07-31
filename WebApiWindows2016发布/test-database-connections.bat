@echo off
chcp 65001 >nul
echo ====================================
echo   Database Connection Test Tool
echo ====================================
echo.

echo [INFO] Testing SQL Server connection...
powershell -Command "try { $conn = New-Object System.Data.SqlClient.SqlConnection('Server=localhost;Integrated Security=true;Connection Timeout=5;'); $conn.Open(); Write-Host '[OK] SQL Server (localhost) connection successful' -ForegroundColor Green; $conn.Close(); } catch { Write-Host '[ERROR] SQL Server (localhost) connection failed:' $_.Exception.Message -ForegroundColor Red }"

echo.
echo [INFO] Testing SQL Server Express connection...
powershell -Command "try { $conn = New-Object System.Data.SqlClient.SqlConnection('Server=.\\SQLEXPRESS;Integrated Security=true;Connection Timeout=5;'); $conn.Open(); Write-Host '[OK] SQL Server Express connection successful' -ForegroundColor Green; $conn.Close(); } catch { Write-Host '[ERROR] SQL Server Express connection failed:' $_.Exception.Message -ForegroundColor Red }"

echo.
echo [INFO] Testing LocalDB connection...
powershell -Command "try { $conn = New-Object System.Data.SqlClient.SqlConnection('Server=(localdb)\\mssqllocaldb;Integrated Security=true;Connection Timeout=5;'); $conn.Open(); Write-Host '[OK] LocalDB connection successful' -ForegroundColor Green; $conn.Close(); } catch { Write-Host '[ERROR] LocalDB connection failed:' $_.Exception.Message -ForegroundColor Red }"

echo.
echo [INFO] Checking SQLite support...
if exist "Microsoft.Data.Sqlite.dll" (
    echo [OK] SQLite library file exists
    echo [INFO] SQLite database will be created automatically
) else (
    echo [WARNING] SQLite library file not found
)

echo.
echo [INFO] Checking SQL Server services...
sc query MSSQLSERVER >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo [INFO] SQL Server service status:
    sc query MSSQLSERVER | findstr "STATE"
) else (
    echo [INFO] SQL Server service not found, checking Express...
    sc query "MSSQL$SQLEXPRESS" >nul 2>&1
    if %ERRORLEVEL% EQU 0 (
        echo [INFO] SQL Server Express service status:
        sc query "MSSQL$SQLEXPRESS" | findstr "STATE"
    ) else (
        echo [INFO] No SQL Server services found
    )
)

echo.
echo ====================================
echo Test completed! 
echo Check the results above to choose the appropriate database configuration.
echo ====================================
echo.
echo Press any key to exit...
pause >nul