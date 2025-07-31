@echo off
:: 设置代码页为UTF-8以支持中文显示
chcp 65001 >nul 2>&1
:: 启用延迟变量扩展
setlocal enabledelayedexpansion

:: 文件监控自动部署脚本
:: 监控 C:\temp\deploy-trigger.txt 文件变化，自动触发部署

set "TRIGGER_FILE=C:\temp\deploy-trigger.txt"
set "LAST_MODIFIED="

:MONITOR_LOOP
if exist "!TRIGGER_FILE!" (
    for %%i in ("!TRIGGER_FILE!") do set "CURRENT_MODIFIED=%%~ti"
    
    if not "!CURRENT_MODIFIED!"=="!LAST_MODIFIED!" (
        echo [!date! !time!] 检测到部署触发信号...
        call "C:\LYBT\Scripts\server-deploy.bat"
        set "LAST_MODIFIED=!CURRENT_MODIFIED!"
    )
)

timeout /t 10 /nobreak >nul
goto MONITOR_LOOP