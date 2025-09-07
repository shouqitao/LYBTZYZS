@echo off
REM ==================================================================
REM 凌隐宝堂中医诊所数据库备份脚本
REM 目标：小诊所数据安全保障 - 简单可靠的每日备份策略
REM 适用：20人以下诊所，单机部署环境
REM ==================================================================

setlocal enabledelayedexpansion

REM 配置参数
set DB_NAME=LYBTDB
set SERVER_NAME=localhost
set BACKUP_BASE_DIR=D:\LYBT_Backups
set LOG_FILE=%BACKUP_BASE_DIR%\backup_log.txt

REM 生成时间戳
for /f "tokens=2 delims==" %%a in ('wmic OS Get localdatetime /value') do set "dt=%%a"
set "YY=%dt:~2,2%" & set "YYYY=%dt:~0,4%" & set "MM=%dt:~4,2%" & set "DD=%dt:~6,2%"
set "HH=%dt:~8,2%" & set "Min=%dt:~10,2%" & set "Sec=%dt:~12,2%"
set "TIMESTAMP=%YYYY%%MM%%DD%_%HH%%Min%%Sec%"

set BACKUP_FILE=%BACKUP_BASE_DIR%\LYBT_DB_%TIMESTAMP%.bak

echo ==========================================
echo 凌隐宝堂数据库备份开始
echo 时间: %YYYY%-%MM%-%DD% %HH%:%Min%:%Sec%
echo 备份文件: %BACKUP_FILE%
echo ==========================================

REM 创建备份目录
if not exist "%BACKUP_BASE_DIR%" (
    echo 创建备份目录: %BACKUP_BASE_DIR%
    mkdir "%BACKUP_BASE_DIR%"
)

REM 记录开始时间
echo [%YYYY%-%MM%-%DD% %HH%:%Min%:%Sec%] 开始备份数据库 %DB_NAME% >> "%LOG_FILE%"

REM 执行数据库备份
sqlcmd -S %SERVER_NAME% -E -Q "BACKUP DATABASE [%DB_NAME%] TO DISK = '%BACKUP_FILE%' WITH FORMAT, COMPRESSION, STATS = 10"

if !errorlevel! equ 0 (
    echo ✅ 数据库备份成功！
    echo [%YYYY%-%MM%-%DD% %HH%:%Min%:%Sec%] 备份成功: %BACKUP_FILE% >> "%LOG_FILE%"
    
    REM 获取备份文件大小
    for %%i in ("%BACKUP_FILE%") do set FILE_SIZE=%%~zi
    set /a FILE_SIZE_MB=!FILE_SIZE!/1024/1024
    echo    备份文件大小: !FILE_SIZE_MB! MB
    echo    备份文件路径: %BACKUP_FILE%
    
) else (
    echo ❌ 数据库备份失败！
    echo [%YYYY%-%MM%-%DD% %HH%:%Min%:%Sec%] 备份失败，错误代码: !errorlevel! >> "%LOG_FILE%"
    pause
    exit /b 1
)

REM 清理过期备份（保留7天）
echo.
echo 清理过期备份文件（保留7天）...
forfiles /p "%BACKUP_BASE_DIR%" /s /m *.bak /d -7 /c "cmd /c echo 删除过期备份: @path & del @path" 2>nul
if !errorlevel! equ 0 (
    echo ✅ 过期备份清理完成
    echo [%YYYY%-%MM%-%DD% %HH%:%Min%:%Sec%] 过期备份清理完成 >> "%LOG_FILE%"
) else (
    echo ℹ️  无过期备份文件需要清理
)

REM 显示当前备份文件列表
echo.
echo 当前备份文件列表:
echo ==========================================
dir "%BACKUP_BASE_DIR%\*.bak" /o:d 2>nul
if !errorlevel! neq 0 (
    echo 无备份文件
)

echo ==========================================
echo 备份操作完成
echo 建议：请定期检查备份文件完整性
echo 恢复命令: sqlcmd -S localhost -E -Q "RESTORE DATABASE [LYBTDB] FROM DISK = '%BACKUP_FILE%'"
echo ==========================================

REM 小诊所运维提示
echo.
echo 💡 小诊所数据安全提示:
echo    1. 建议设置Windows计划任务每日自动执行此脚本
echo    2. 备份文件存储在 D:\LYBT_Backups 目录
echo    3. 系统自动保留最近7天的备份文件
echo    4. 请定期将备份文件复制到外部存储设备
echo    5. 如需恢复数据，请联系技术支持

pause