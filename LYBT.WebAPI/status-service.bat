@echo off
chcp 65001 >nul
title 凌隐宝堂中医诊所诊疗系统 - 服务状态

echo.
echo ====================================================
echo    凌隐宝堂中医诊所诊疗系统 - 服务状态查询
echo ====================================================
echo.

set "SERVICE_NAME=LYBT.WebAPI"

:: 检查服务是否存在
sc query "%SERVICE_NAME%" >nul 2>&1
if %errorLevel% neq 0 (
    echo ❌ 服务状态: 未安装
    echo 💡 请先运行 install-service.bat 安装服务
    echo.
    pause
    exit /b 1
)

echo 📊 服务详细状态:
echo.
sc query "%SERVICE_NAME%"
echo.

:: 检查端口占用情况
echo 🌐 端口占用情况:
netstat -an | findstr ":5000" >nul 2>&1
if %errorLevel% equ 0 (
    echo ✅ 端口 5000 正在监听
    netstat -an | findstr ":5000"
) else (
    echo ❌ 端口 5000 未在监听
)
echo.

:: 检查进程
echo 🔍 相关进程:
tasklist /fi "imagename eq LYBT.WebAPI.exe" 2>nul | findstr "LYBT.WebAPI.exe" >nul 2>&1
if %errorLevel% equ 0 (
    echo ✅ WebAPI 进程正在运行
    tasklist /fi "imagename eq LYBT.WebAPI.exe"
) else (
    echo ❌ WebAPI 进程未运行
)
echo.

:: 检查日志文件
echo 📝 日志文件状态:
if exist "%~dp0logs\service-output.log" (
    echo ✅ 输出日志: logs\service-output.log
    echo    大小: 
    for %%A in ("%~dp0logs\service-output.log") do echo    %%~zA bytes
) else (
    echo ❌ 输出日志: 不存在
)

if exist "%~dp0logs\service-error.log" (
    echo ✅ 错误日志: logs\service-error.log
    echo    大小: 
    for %%A in ("%~dp0logs\service-error.log") do echo    %%~zA bytes
) else (
    echo ❌ 错误日志: 不存在
)
echo.

:: 测试API连接
echo 🧪 API连接测试:
echo 正在测试 http://localhost:5000 ...
powershell -Command "try { $response = Invoke-WebRequest -Uri 'http://localhost:5000' -TimeoutSec 5 -UseBasicParsing; Write-Host '✅ API响应正常 - 状态码:' $response.StatusCode } catch { Write-Host '❌ API无法访问:' $_.Exception.Message }" 2>nul
echo.

echo 🎯 快速操作:
echo    启动服务: start-service.bat
echo    停止服务: stop-service.bat
echo    查看日志: notepad logs\service-output.log
echo    访问系统: http://localhost:5000
echo    API文档: http://localhost:5000/swagger
echo.

pause