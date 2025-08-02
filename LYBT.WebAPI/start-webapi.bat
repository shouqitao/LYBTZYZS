@echo off
chcp 65001 >nul
title 凌隐宝堂中医诊所诊疗系统 - 生产环境

echo.
echo ====================================================
echo    凌隐宝堂中医诊所诊疗系统 - 生产环境启动
echo ====================================================
echo.

echo 🚀 正在启动服务器...
echo 💡 提示: 按 Ctrl+C 可以停止服务器
echo 📖 Swagger文档: http://localhost:5000/swagger
echo 🌐 健康检查: http://localhost:5000/health
echo.

:: 设置环境变量
set ASPNETCORE_ENVIRONMENT=Production
set ASPNETCORE_URLS=http://localhost:5000

:: 启动应用程序
LYBT.WebAPI.exe

pause