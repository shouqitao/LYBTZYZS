@echo off
REM ==================================================================
REM 凌隐宝堂数据库备份状态检查工具
REM 目标：帮助小诊所管理员监控备份系统运行状态
REM 功能：检查备份文件、计划任务、磁盘空间等
REM ==================================================================

setlocal enabledelayedexpansion

set BACKUP_DIR=D:\LYBT_Backups
set LOG_FILE=%BACKUP_DIR%\backup_log.txt

echo ==========================================
echo 凌隐宝堂 - 备份系统状态检查
echo ==========================================

REM 检查备份目录
echo 📁 检查备份目录...
if exist "%BACKUP_DIR%" (
    echo ✅ 备份目录存在: %BACKUP_DIR%
) else (
    echo ❌ 备份目录不存在: %BACKUP_DIR%
    echo 💡 建议：运行 setup-daily-backup.bat 创建备份任务
    echo.
)

REM 检查计划任务
echo.
echo 📅 检查计划任务状态...
schtasks /query /tn "LYBT_Daily_Backup" >nul 2>&1
if !errorlevel! equ 0 (
    echo ✅ 计划任务已创建: LYBT_Daily_Backup
    
    REM 获取任务详细信息
    for /f "skip=1 tokens=*" %%i in ('schtasks /query /tn "LYBT_Daily_Backup" /fo table /nh') do (
        set TASK_INFO=%%i
        goto :task_info_done
    )
    :task_info_done
    
    REM 检查任务最后运行时间
    for /f "skip=1 tokens=2 delims= " %%i in ('schtasks /query /tn "LYBT_Daily_Backup" /fo list ^| find "Last Run Time"') do (
        set LAST_RUN=%%i
    )
    
    echo    最后运行: !LAST_RUN!
    
    REM 检查任务下次运行时间
    for /f "skip=1 tokens=3* delims=:" %%i in ('schtasks /query /tn "LYBT_Daily_Backup" /fo list ^| find "Next Run Time"') do (
        set NEXT_RUN=%%i %%j
    )
    echo    下次运行: !NEXT_RUN!
    
) else (
    echo ❌ 计划任务未创建
    echo 💡 建议：运行 setup-daily-backup.bat 创建自动备份任务
)

REM 检查备份文件
echo.
echo 💾 检查备份文件...
if exist "%BACKUP_DIR%\*.bak" (
    set FILE_COUNT=0
    set TOTAL_SIZE=0
    set NEWEST_FILE=
    set NEWEST_DATE=0
    
    for %%f in ("%BACKUP_DIR%\*.bak") do (
        set /a FILE_COUNT+=1
        set /a TOTAL_SIZE+=%%~zf
        
        REM 获取文件修改时间（简化）
        set FILE_DATE=%%~tf
        if "!FILE_DATE!" gtr "!NEWEST_DATE!" (
            set NEWEST_DATE=!FILE_DATE!
            set NEWEST_FILE=%%~nxf
        )
    )
    
    set /a TOTAL_SIZE_MB=!TOTAL_SIZE!/1024/1024
    
    echo ✅ 找到 !FILE_COUNT! 个备份文件
    echo    总大小: !TOTAL_SIZE_MB! MB
    echo    最新备份: !NEWEST_FILE!
    echo    最新时间: !NEWEST_DATE!
    
    REM 检查最新备份是否在24小时内
    REM 注意：这是简化版检查，实际部署时可以使用更精确的时间比较
    echo.
    echo 📊 备份文件列表:
    echo    文件名                          大小(MB)     修改时间
    echo    ========================================================
    for %%f in ("%BACKUP_DIR%\*.bak") do (
        set /a FILE_SIZE_MB=%%~zf/1024/1024
        echo    %%~nxf    !FILE_SIZE_MB! MB     %%~tf
    )
    
) else (
    echo ❌ 未找到任何备份文件
    echo 💡 建议：运行 backup-database.bat 手动执行一次备份
)

REM 检查磁盘空间
echo.
echo 💿 检查磁盘空间...
for /f "skip=1 tokens=3" %%i in ('wmic logicaldisk where "DeviceID='D:'" get Size /format:value 2^>nul') do (
    if "%%i" neq "" set DISK_SIZE=%%i
)
for /f "skip=1 tokens=3" %%i in ('wmic logicaldisk where "DeviceID='D:'" get FreeSpace /format:value 2^>nul') do (
    if "%%i" neq "" set FREE_SPACE=%%i
)

if defined DISK_SIZE if defined FREE_SPACE (
    set /a DISK_SIZE_GB=!DISK_SIZE!/1024/1024/1024
    set /a FREE_SPACE_GB=!FREE_SPACE!/1024/1024/1024
    set /a USED_PERCENT=((!DISK_SIZE!-!FREE_SPACE!)*100)/!DISK_SIZE!
    
    echo ✅ D盘磁盘空间检查
    echo    总容量: !DISK_SIZE_GB! GB
    echo    可用空间: !FREE_SPACE_GB! GB
    echo    已使用: !USED_PERCENT!%%
    
    if !FREE_SPACE_GB! lss 5 (
        echo ⚠️  警告：磁盘空间不足5GB，请清理磁盘或扩容
    ) else (
        echo ✅ 磁盘空间充足
    )
) else (
    echo ⚠️  无法获取D盘空间信息（可能不存在D盘）
)

REM 检查备份日志
echo.
echo 📋 检查备份日志...
if exist "%LOG_FILE%" (
    echo ✅ 备份日志存在: %LOG_FILE%
    echo.
    echo 📈 最近5条日志记录:
    echo ========================================
    
    REM 显示最后5行日志
    powershell -command "Get-Content '%LOG_FILE%' | Select-Object -Last 5" 2>nul
    if !errorlevel! neq 0 (
        REM 如果PowerShell不可用，使用基本方式
        echo 最近的日志内容:
        type "%LOG_FILE%" | more +0
    )
    
) else (
    echo ❌ 备份日志文件不存在
    echo 💡 建议：执行一次备份操作生成日志文件
)

REM 系统建议
echo.
echo ==========================================
echo 💡 系统建议
echo ==========================================

if exist "%BACKUP_DIR%\*.bak" (
    if !FILE_COUNT! geq 7 (
        echo ✅ 备份文件数量正常（!FILE_COUNT! 个）
    ) else (
        echo ⚠️  建议：备份文件较少（!FILE_COUNT! 个），建议运行几天积累更多备份
    )
) else (
    echo ❌ 尚未创建任何备份文件
)

schtasks /query /tn "LYBT_Daily_Backup" >nul 2>&1
if !errorlevel! neq 0 (
    echo ❌ 建议立即设置自动备份任务
)

echo.
echo 🔧 管理操作:
echo    1. 手动备份:    backup-database.bat
echo    2. 设置自动备份: setup-daily-backup.bat  
echo    3. 恢复数据:    restore-database.bat
echo    4. 查看状态:    check-backup-status.bat (当前脚本)
echo.
echo ==========================================
echo 检查完成
echo ==========================================

pause