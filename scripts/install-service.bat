@echo off
:: 设置代码页为UTF-8以支持中文显示
chcp 65001 >nul 2>&1
:: 启用延迟变量扩展
setlocal enabledelayedexpansion
echo ====================================
echo    LYBT WebAPI Windows 服务安装
echo ====================================
echo.

::$检查管理员权限
net session >nul 2>&1
if !errorlevel! neq 0 (
    echo ❌ 请以管理员身份运行此脚本！
    pause
    exit /b 1
)

set "SERVICE_NAME=LYBTWebAPI"
set "SERVICE_DISPLAY_NAME=LYBT WebAPI Service"
set "SERVICE_DESCRIPTION=凌隐宝堂中医诊所管理系统 WebAPI 服务"
set "EXE_PATH=C:\LYBT\WebAPI\LYBT.WebAPI.exe"

echo [步骤 1] 检查服务是否已存在...
sc query "!SERVICE_NAME!" >nul 2>&1
if !errorlevel! equ 0 (
    echo 服务已存在，正在删除...
    net stop "!SERVICE_NAME!" >nul 2>&1
    sc delete "!SERVICE_NAME!" >nul 2>&1
    timeout /t 3 /nobreak >nul
)

echo [步骤 2] 创建 Windows 服务...
sc create "!SERVICE_NAME!" binPath= "\"!EXE_PATH!\"" DisplayName= "!SERVICE_DISPLAY_NAME!" start= auto >nul 2>&1
if !errorlevel! neq 0 (
    echo ❌ 服务创建失败！
    pause
    exit /b 1
)

echo [步骤 3] 设置服务描述...
sc description "!SERVICE_NAME!" "!SERVICE_DESCRIPTION!" >nul 2>&1

echo [步骤 4] 配置服务恢复选项...
sc failure "!SERVICE_NAME!" reset= 86400 actions= restart/5000/restart/10000/restart/20000 >nul 2>&1

echo [步骤 5] 启动服务...
net start "!SERVICE_NAME!" >nul 2>&1

echo [步骤 6] 配置防火墙规则...
netsh advfirewall firewall delete rule name="LYBT WebAPI" >nul 2>&1
netsh advfirewall firewall add rule name="LYBT WebAPI" dir=in action=allow protocol=TCP localport=5297 >nul 2>&1

echo.
echo ✅ Windows 服务安装完成！
echo 📋 服务名称: !SERVICE_NAME!
echo 🌐 服务端口: 5297
echo 📁 服务路径: !EXE_PATH!
echo.
pause