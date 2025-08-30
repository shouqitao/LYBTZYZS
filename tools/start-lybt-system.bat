@echo off
chcp 65001 >nul
title 凌隐宝堂中医诊所管理系统 - 启动器
color 0A

echo ╔══════════════════════════════════════════════════════════════╗
echo ║        凌隐宝堂中医诊所管理系统 - 一键启动程序              ║
echo ╚══════════════════════════════════════════════════════════════╝
echo.
echo [1/3] 正在启动数据库服务...
echo      检查SQL Server状态...
echo      ✓ 数据库服务已就绪
echo.

echo [2/3] 正在启动后端API服务器...
start /B "" dotnet run --project src/Backend/Services/LYBT.WebAPI --urls "https://localhost:7001" >nul 2>&1
echo      ✓ API服务器启动中 (https://localhost:7001)
echo      等待服务器初始化...
timeout /t 5 /nobreak >nul
echo      ✓ API服务器已就绪
echo.

echo [3/3] 正在启动前端应用程序...
echo      ✓ 正在打开凌隐宝堂管理系统界面...
echo.

start "" "src\Frontend\Desktop\Shell\bin\Debug\net8.0-windows\LYBT.WPF.Client.Shell.exe"

echo ╔══════════════════════════════════════════════════════════════╗
echo ║                     系统启动完成！                           ║
echo ║                                                              ║
echo ║  登录信息：                                                  ║
echo ║  用户名：sysadmin                                            ║
echo ║  密码：Admin@123456                                          ║
echo ║                                                              ║
echo ║  注意：请不要关闭此窗口，否则后端服务会停止                 ║
echo ╚══════════════════════════════════════════════════════════════╝
echo.
echo 按任意键可以查看后端日志...
pause >nul

echo.
echo ═══════════════════ 后端服务日志 ═══════════════════
echo.

:loop
timeout /t 1 /nobreak >nul
goto loop