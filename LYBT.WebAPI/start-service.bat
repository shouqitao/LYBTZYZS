@echo off
chcp 65001 >nul
title 凌隐宝堂中医诊所诊疗系统 - 启动服务

echo.
echo ====================================================
echo    凌隐宝堂中医诊所诊疗系统 - 启动服务
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

echo 🚀 正在启动服务: %SERVICE_NAME%
echo.

:: 启动服务
if exist "%NSSM_PATH%" (
    "%NSSM_PATH%" start "%SERVICE_NAME%"
) else (
    net start "%SERVICE_NAME%"
)

if %errorLevel% equ 0 (
    echo ✅ 服务启动成功!
    echo.
    echo 🌐 系统访问地址:
    echo    主页: http://localhost:5000
    echo    Swagger API文档: http://localhost:5000/swagger
    echo    健康检查: http://localhost:5000/health
    echo.
    echo 📊 服务状态:
    sc query "%SERVICE_NAME%" | findstr "STATE"
) else (
    echo ❌ 服务启动失败
    echo.
    echo 🔍 故障排除:
    echo    1. 检查端口5000是否被占用
    echo    2. 查看服务日志: logs\service-error.log
    echo    3. 检查数据库连接是否正常
    echo    4. 运行 status-service.bat 查看详细状态
)

echo.
pause