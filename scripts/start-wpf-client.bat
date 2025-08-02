@echo off
chcp 65001 >nul
title 启动LYBT WPF客户端

echo ====================================
echo   启动凌隐宝堂中医诊所WPF客户端
echo ====================================
echo.

set CLIENT_PATH=D:\source\repos\LYBTZYZS\BIN\LYBT.Desktop\LYBT.WPF.Client.Shell.exe

if exist "%CLIENT_PATH%" (
    echo [INFO] 正在启动WPF客户端...
    echo [INFO] API服务器: http://192.168.190.243:5000/
    echo.
    start "" "%CLIENT_PATH%"
    echo ✅ 客户端已启动
) else (
    echo ❌ 错误：找不到客户端程序
    echo 期望路径：%CLIENT_PATH%
    echo.
    echo 请先编译项目：
    echo cd src\Frontend
    echo dotnet build LYBT.Client.sln
)

echo.
pause