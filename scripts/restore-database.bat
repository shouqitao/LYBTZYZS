@echo off
REM ==================================================================
REM 凌隐宝堂中医诊所数据库恢复脚本
REM 目标：提供简单可靠的数据恢复功能
REM 警告：此操作将覆盖现有数据库，请谨慎使用！
REM ==================================================================

setlocal enabledelayedexpansion

set DB_NAME=LYBTDB
set SERVER_NAME=localhost
set BACKUP_DIR=D:\LYBT_Backups

echo ==========================================
echo 凌隐宝堂数据库恢复工具
echo ⚠️  警告：此操作将完全替换现有数据库
echo ==========================================

REM 检查备份目录
if not exist "%BACKUP_DIR%" (
    echo ❌ 错误：备份目录不存在
    echo 预期目录: %BACKUP_DIR%
    pause
    exit /b 1
)

REM 列出可用备份文件
echo 可用的备份文件:
echo ==========================================
set FILE_COUNT=0
for %%f in ("%BACKUP_DIR%\*.bak") do (
    set /a FILE_COUNT+=1
    echo !FILE_COUNT!. %%~nxf - 大小: %%~zf 字节 - 修改时间: %%~tf
    set "FILE_!FILE_COUNT!=%%f"
)

if %FILE_COUNT% equ 0 (
    echo ❌ 错误：未找到任何备份文件
    echo 备份目录: %BACKUP_DIR%
    pause
    exit /b 1
)

echo ==========================================

REM 选择备份文件
echo.
set /p "CHOICE=请输入要恢复的备份文件编号 (1-%FILE_COUNT%): "

REM 验证输入
if not defined FILE_%CHOICE% (
    echo ❌ 错误：无效选择
    pause
    exit /b 1
)

set "SELECTED_FILE=!FILE_%CHOICE%!"
echo.
echo 选择的备份文件: %SELECTED_FILE%

REM 最终确认
echo.
echo ⚠️  重要警告：
echo    - 此操作将完全删除现有数据库 [%DB_NAME%]
echo    - 所有当前数据将被备份文件中的数据替换
echo    - 恢复过程中系统将暂时不可用
echo    - 建议在操作前通知所有用户

echo.
choice /C YN /M "确定要继续恢复操作吗？(Y/N)"
if !errorlevel! neq 1 (
    echo 操作已取消
    pause
    exit /b 0
)

REM 生成恢复时间戳
for /f "tokens=2 delims==" %%a in ('wmic OS Get localdatetime /value') do set "dt=%%a"
set "YY=%dt:~2,2%" & set "YYYY=%dt:~0,4%" & set "MM=%dt:~4,2%" & set "DD=%dt:~6,2%"
set "HH=%dt:~8,2%" & set "Min=%dt:~10,2%" & set "Sec=%dt:~12,2%"

echo.
echo ==========================================
echo 开始数据库恢复操作
echo 时间: %YYYY%-%MM%-%DD% %HH%:%Min%:%Sec%
echo ==========================================

REM 步骤1：断开所有连接
echo 1. 断开数据库连接...
sqlcmd -S %SERVER_NAME% -E -Q "ALTER DATABASE [%DB_NAME%] SET SINGLE_USER WITH ROLLBACK IMMEDIATE"
if !errorlevel! neq 0 (
    echo ❌ 警告：无法设置数据库为单用户模式（可能不影响恢复）
)

REM 步骤2：执行恢复
echo 2. 开始恢复数据库...
echo    源文件: %SELECTED_FILE%
echo    目标库: %DB_NAME%

sqlcmd -S %SERVER_NAME% -E -Q "RESTORE DATABASE [%DB_NAME%] FROM DISK = '%SELECTED_FILE%' WITH REPLACE, STATS = 10"

if !errorlevel! equ 0 (
    echo ✅ 数据库恢复成功！
    
    REM 步骤3：恢复多用户模式
    echo 3. 恢复数据库多用户模式...
    sqlcmd -S %SERVER_NAME% -E -Q "ALTER DATABASE [%DB_NAME%] SET MULTI_USER"
    
    if !errorlevel! equ 0 (
        echo ✅ 数据库已恢复正常访问模式
    ) else (
        echo ⚠️  警告：无法恢复多用户模式，请手动执行以下命令：
        echo    sqlcmd -S %SERVER_NAME% -E -Q "ALTER DATABASE [%DB_NAME%] SET MULTI_USER"
    )
    
    REM 记录恢复日志
    echo [%YYYY%-%MM%-%DD% %HH%:%Min%:%Sec%] 数据库恢复成功，源文件: %SELECTED_FILE% >> "%BACKUP_DIR%\restore_log.txt"
    
    echo.
    echo ==========================================
    echo 恢复操作完成！
    echo ==========================================
    echo 📋 恢复信息：
    echo    恢复时间: %YYYY%-%MM%-%DD% %HH%:%Min%:%Sec%
    echo    源备份文件: %SELECTED_FILE%
    echo    目标数据库: %DB_NAME%
    echo    状态: 成功
    echo.
    echo 💡 后续建议：
    echo    1. 启动应用程序测试功能是否正常
    echo    2. 检查关键数据是否恢复完整
    echo    3. 通知用户系统已恢复正常使用
    
) else (
    echo ❌ 数据库恢复失败！
    echo 错误代码: !errorlevel!
    echo.
    echo 🔧 故障排除建议：
    echo    1. 检查备份文件是否完整和兼容
    echo    2. 确认SQL Server服务正常运行
    echo    3. 验证数据库连接权限
    echo    4. 检查磁盘空间是否充足
    echo    5. 如问题持续，请联系技术支持
    
    REM 记录失败日志
    echo [%YYYY%-%MM%-%DD% %HH%:%Min%:%Sec%] 数据库恢复失败，错误代码: !errorlevel!，源文件: %SELECTED_FILE% >> "%BACKUP_DIR%\restore_log.txt"
    
    REM 尝试恢复多用户模式
    echo.
    echo 尝试恢复数据库访问...
    sqlcmd -S %SERVER_NAME% -E -Q "ALTER DATABASE [%DB_NAME%] SET MULTI_USER"
)

echo ==========================================
pause