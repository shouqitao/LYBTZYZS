@echo off
chcp 65001 >nul
title 凌隐宝堂中医诊所WPF客户端

echo ====================================
echo   凌隐宝堂中医诊所WPF客户端
echo ====================================
echo.

set CLIENT_DIR=D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Shell\bin\Release\net8.0-windows
set CLIENT_EXE=LYBT.WPF.Client.Shell.exe

if not exist "%CLIENT_DIR%\%CLIENT_EXE%" (
    echo ❌ 错误：找不到客户端程序
    echo 期望路径：%CLIENT_DIR%\%CLIENT_EXE%
    echo.
    echo 请先编译项目：
    echo cd src\Frontend
    echo dotnet build LYBT.Client.sln --configuration Release
    pause
    exit /b 1
)

echo [INFO] 正在启动WPF客户端...
echo [INFO] API服务器: http://192.168.190.243:5000/
echo.

cd /d "%CLIENT_DIR%"
start "" "%CLIENT_EXE%"

echo ✅ 客户端已启动
echo.
timeout /t 3 >nul