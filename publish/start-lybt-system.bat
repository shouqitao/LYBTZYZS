@echo off
chcp 65001 >nul
title 凌隐宝堂中医诊所管理系统

echo.
echo ==========================================
echo    凌隐宝堂中医诊所管理系统 v2.0
echo ==========================================
echo.
echo 正在启动 WebAPI 服务器...
echo.

cd /d "%~dp0WebAPI"
start "WebAPI Server" cmd /k "echo WebAPI服务器已启动！ & echo 访问地址: https://localhost:7001 & echo Swagger文档: https://localhost:7001/swagger & echo. & LYBT.WebAPI.exe"

echo WebAPI 服务器启动中...
echo.
echo 服务器信息:
echo - WebAPI地址: https://localhost:7001
echo - Swagger文档: https://localhost:7001/swagger  
echo - 默认管理员: sysadmin / Admin@123456
echo.
echo 注意: 请等待WebAPI服务器完全启动后再使用客户端
echo.
pause