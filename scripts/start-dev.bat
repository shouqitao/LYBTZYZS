@echo off
chcp 65001 >nul
title 凌隐宝堂中医诊所管理系统 - 开发环境启动器

echo.
echo ====================================================
echo    凌隐宝堂中医诊所管理系统 - 开发环境启动器
echo ====================================================
echo.

:: 设置项目根目录
set "PROJECT_ROOT=%~dp0"
set "WEBAPI_DIR=%PROJECT_ROOT%src\Backend\Services\LYBT.WebAPI"

:: 检查项目目录是否存在
if not exist "%WEBAPI_DIR%" (
    echo ❌ 错误: 找不到WebAPI项目目录
    echo    期望路径: %WEBAPI_DIR%
    echo.
    pause
    exit /b 1
)

echo 📂 项目根目录: %PROJECT_ROOT%
echo 🚀 WebAPI目录: %WEBAPI_DIR%
echo.

:: 切换到WebAPI目录
cd /d "%WEBAPI_DIR%"

echo 🔄 正在启动开发服务器...
echo 💡 提示: 按 Ctrl+C 可以停止服务器
echo.

:: 启动开发服务器
dotnet run

echo.
echo 🔚 服务器已停止
pause