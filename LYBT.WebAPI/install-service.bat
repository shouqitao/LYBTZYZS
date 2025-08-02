@echo off
chcp 65001 >nul
title 凌隐宝堂中医诊所诊疗系统 - 服务安装器

echo.
echo ====================================================
echo    凌隐宝堂中医诊所诊疗系统 - Windows服务安装
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

:: 设置变量
set "SERVICE_NAME=LYBT.WebAPI"
set "SERVICE_DISPLAY_NAME=凌隐宝堂中医诊所诊疗系统"
set "SERVICE_DESCRIPTION=凌隐宝堂中医诊所诊疗系统 WebAPI 服务"
set "APP_PATH=%~dp0LYBT.WebAPI.exe"
set "NSSM_PATH=%~dp0nssm.exe"

echo 📋 服务配置信息:
echo    服务名称: %SERVICE_NAME%
echo    显示名称: %SERVICE_DISPLAY_NAME%
echo    程序路径: %APP_PATH%
echo    NSSM路径: %NSSM_PATH%
echo.

:: 检查NSSM是否存在
if not exist "%NSSM_PATH%" (
    echo ❌ 错误: 找不到 nssm.exe
    echo 💡 请将 nssm.exe 复制到当前目录
    echo 💡 下载地址: https://nssm.cc/download
    pause
    exit /b 1
)

:: 检查应用程序是否存在
if not exist "%APP_PATH%" (
    echo ❌ 错误: 找不到 LYBT.WebAPI.exe
    echo 💡 请确保应用程序已正确发布到当前目录
    pause
    exit /b 1
)

:: 检查服务是否已存在
sc query "%SERVICE_NAME%" >nul 2>&1
if %errorLevel% equ 0 (
    echo ⚠️  警告: 服务 '%SERVICE_NAME%' 已存在
    set /p "OVERWRITE=是否要重新安装? (Y/N): "
    if /i "%OVERWRITE%" neq "Y" (
        echo 🚫 安装已取消
        pause
        exit /b 0
    )
    
    echo 🗑️  正在移除现有服务...
    "%NSSM_PATH%" stop "%SERVICE_NAME%" >nul 2>&1
    "%NSSM_PATH%" remove "%SERVICE_NAME%" confirm >nul 2>&1
    timeout /t 2 >nul
)

echo.
echo 🔧 正在安装服务...

:: 安装服务
"%NSSM_PATH%" install "%SERVICE_NAME%" "%APP_PATH%"
if %errorLevel% neq 0 (
    echo ❌ 服务安装失败
    pause
    exit /b 1
)

:: 配置服务显示名称和描述
"%NSSM_PATH%" set "%SERVICE_NAME%" DisplayName "%SERVICE_DISPLAY_NAME%"
"%NSSM_PATH%" set "%SERVICE_NAME%" Description "%SERVICE_DESCRIPTION%"

:: 配置服务启动类型
"%NSSM_PATH%" set "%SERVICE_NAME%" Start SERVICE_AUTO_START

:: 配置工作目录
"%NSSM_PATH%" set "%SERVICE_NAME%" AppDirectory "%~dp0"

:: 配置环境变量
"%NSSM_PATH%" set "%SERVICE_NAME%" AppEnvironmentExtra "ASPNETCORE_ENVIRONMENT=Production" "ASPNETCORE_URLS=http://localhost:5000"

:: 配置日志
"%NSSM_PATH%" set "%SERVICE_NAME%" AppStdout "%~dp0logs\service-output.log"
"%NSSM_PATH%" set "%SERVICE_NAME%" AppStderr "%~dp0logs\service-error.log"
"%NSSM_PATH%" set "%SERVICE_NAME%" AppRotateFiles 1
"%NSSM_PATH%" set "%SERVICE_NAME%" AppRotateOnline 1
"%NSSM_PATH%" set "%SERVICE_NAME%" AppRotateSeconds 86400
"%NSSM_PATH%" set "%SERVICE_NAME%" AppRotateBytes 10485760

:: 配置服务恢复
"%NSSM_PATH%" set "%SERVICE_NAME%" AppThrottle 1500
"%NSSM_PATH%" set "%SERVICE_NAME%" AppExit Default Restart
"%NSSM_PATH%" set "%SERVICE_NAME%" AppRestartDelay 5000

:: 创建日志目录
if not exist "%~dp0logs" mkdir "%~dp0logs"

echo.
echo ✅ 服务安装成功!
echo.
echo 📋 服务信息:
echo    服务名称: %SERVICE_NAME%
echo    显示名称: %SERVICE_DISPLAY_NAME%
echo    状态: 已安装，未启动
echo    端口: 5000
echo    日志目录: %~dp0logs
echo.
echo 🎯 下一步操作:
echo    1. 启动服务: start-service.bat
echo    2. 查看状态: status-service.bat
echo    3. 访问系统: http://localhost:5000
echo    4. API文档: http://localhost:5000/swagger
echo.

set /p "START_NOW=是否立即启动服务? (Y/N): "
if /i "%START_NOW%"=="Y" (
    echo.
    echo 🚀 正在启动服务...
    "%NSSM_PATH%" start "%SERVICE_NAME%"
    if %errorLevel% equ 0 (
        echo ✅ 服务启动成功!
        echo 🌐 系统访问地址: http://localhost:5000
    ) else (
        echo ❌ 服务启动失败，请检查日志文件
    )
)

echo.
pause