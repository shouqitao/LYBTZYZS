@echo off
REM ==================================================================
REM 设置凌隐宝堂数据库每日自动备份计划任务
REM 目标：为小诊所建立无人值守的数据备份保障
REM 执行时间：每日凌晨2:00自动执行
REM ==================================================================

echo ==========================================
echo 凌隐宝堂 - 设置每日自动备份
echo ==========================================

REM 检查管理员权限
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ 错误：需要管理员权限运行此脚本
    echo 请右键点击此脚本，选择"以管理员身份运行"
    pause
    exit /b 1
)

REM 获取当前脚本所在目录
set "SCRIPT_DIR=%~dp0"
set "BACKUP_SCRIPT=%SCRIPT_DIR%backup-database.bat"

REM 检查备份脚本是否存在
if not exist "%BACKUP_SCRIPT%" (
    echo ❌ 错误：找不到备份脚本文件
    echo 预期位置: %BACKUP_SCRIPT%
    pause
    exit /b 1
)

echo ✅ 找到备份脚本: %BACKUP_SCRIPT%

REM 创建计划任务
echo.
echo 正在创建Windows计划任务...
echo 任务名称: LYBT_Daily_Backup
echo 执行时间: 每日凌晨2:00
echo 执行脚本: %BACKUP_SCRIPT%

schtasks /create /tn "LYBT_Daily_Backup" /tr "\"%BACKUP_SCRIPT%\"" /sc daily /st 02:00 /ru "SYSTEM" /f

if %errorlevel% equ 0 (
    echo ✅ 计划任务创建成功！
    echo.
    echo 📋 任务详细信息:
    echo    任务名称: LYBT_Daily_Backup
    echo    执行时间: 每日凌晨2:00
    echo    执行账户: SYSTEM（系统账户）
    echo    备份位置: D:\LYBT_Backups\
    echo    日志文件: D:\LYBT_Backups\backup_log.txt
    echo.
    echo 💡 管理提示:
    echo    - 可通过"任务计划程序"查看和管理此任务
    echo    - 系统会自动保留最近7天的备份
    echo    - 建议定期检查备份日志文件
    
    REM 立即测试备份任务
    echo.
    choice /C YN /M "是否立即测试备份功能？(Y/N)"
    if !errorlevel! equ 1 (
        echo.
        echo 正在执行测试备份...
        call "%BACKUP_SCRIPT%"
    )
    
) else (
    echo ❌ 计划任务创建失败！
    echo 错误代码: %errorlevel%
    echo 请检查：
    echo    1. 是否以管理员身份运行
    echo    2. Windows计划任务服务是否正常运行
    echo    3. 备份脚本文件是否存在且可访问
)

echo.
echo ==========================================
echo 安装完成
echo 
echo 🔧 后续管理操作:
echo    查看任务: schtasks /query /tn "LYBT_Daily_Backup"
echo    删除任务: schtasks /delete /tn "LYBT_Daily_Backup" /f
echo    手动执行: schtasks /run /tn "LYBT_Daily_Backup"
echo    查看日志: type D:\LYBT_Backups\backup_log.txt
echo ==========================================

pause