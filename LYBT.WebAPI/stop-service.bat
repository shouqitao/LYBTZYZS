@echo off
chcp 65001 >nul
title 凌隐宝堂中医诊所诊疗系统 - 停止服务

echo.
echo ====================================================
echo    凌隐宝堂中医诊所诊疗系统 - 停止服务
echo ====================================================
echo.

:: 检查管理员权限
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ❌ 错误: 请以管理员身份运行此脚本
    echo 💡 右键点击脚本 → 以管理员身份运行
    pause
    exit /b 1
)

set "SERVICE_NAME=LYBT.WebAPI"
set "NSSM_PATH=%~dp0nssm.exe"

:: 检查服务是否存在
sc query "%SERVICE_NAME%" >nul 2>&1
if %errorLevel% neq 0 (
    echo ❌ 错误: 服务 '%SERVICE_NAME%' 不存在
    echo 💡 请先运行 install-service.bat 安装服务
    pause
    exit /b 1
)

echo 🛑 正在停止服务: %SERVICE_NAME%
echo.

:: 停止服务
if exist "%NSSM_PATH%" (
    "%NSSM_PATH%" stop "%SERVICE_NAME%"
) else (
    net stop "%SERVICE_NAME%"
)

if %errorLevel% equ 0 (
    echo ✅ 服务停止成功!
    echo.
    echo 📊 服务状态:
    sc query "%SERVICE_NAME%" | findstr "STATE"
) else (
    echo ❌ 服务停止失败
    echo.
    echo 🔍 可能的原因:
    echo    1. 服务已经停止
    echo    2. 服务正在处理请求，需要等待
    echo    3. 服务进程可能卡住，需要强制终止
)

echo.
pause