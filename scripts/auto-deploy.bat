@echo off
:: 设置代码页为UTF-8以支持中文显示
chcp 65001 >nul 2>&1
:: 启用延迟变量扩展
setlocal enabledelayedexpansion
echo ====================================
echo     LYBT WebAPI 自动化部署脚本
echo ====================================
echo.

:: 配置变量
set "SERVER_IP=192.168.190.243"
set "SERVER_USER=Administrator"
set "SERVER_DEPLOY_PATH=C:\LYBT\WebAPI"
set "LOCAL_PROJECT_PATH=D:\source\repos\LYBTZYZS\src\Backend\Services\LYBT.WebAPI"
set "LOCAL_PUBLISH_PATH=D:\source\repos\LYBTZYZS\Release\WebAPI"
set "BACKUP_PATH=C:\LYBT\Backup"

echo [步骤 1] 清理本地发布目录...
if exist "%LOCAL_PUBLISH_PATH%" rmdir /s /q "%LOCAL_PUBLISH_PATH%"
mkdir "%LOCAL_PUBLISH_PATH%"

echo [步骤 2] 发布 WebAPI 项目...
cd /d "%LOCAL_PROJECT_PATH%"
dotnet publish -c Release -o "%LOCAL_PUBLISH_PATH%" --self-contained true -r win-x64
if %errorlevel% neq 0 (
    echo ❌ 发布失败！
    pause
    exit /b 1
)

echo [步骤 3] 创建部署包...
cd /d "!LOCAL_PUBLISH_PATH!\.."
powershell -Command "& {[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; Compress-Archive -Path 'WebAPI\*' -DestinationPath 'WebAPI-Deploy.zip' -Force}"

echo [步骤 4] 上传到服务器...
:: 使用 SCP 或 PowerShell Remoting 上传文件
powershell -ExecutionPolicy Bypass -Command "& {[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; & '%~dp0upload-to-server.ps1' '%SERVER_IP%' '%SERVER_USER%' 'WebAPI-Deploy.zip'}"

if %errorlevel% neq 0 (
    echo ❌ 上传失败！
    pause
    exit /b 1
)

echo [步骤 5] 触发服务器端部署...
powershell -ExecutionPolicy Bypass -Command "& {[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; & '%~dp0trigger-server-deploy.ps1' '%SERVER_IP%' '%SERVER_USER%'}"

echo.
echo ✅ 部署完成！
echo 🌐 服务地址: http://!SERVER_IP!:5297
echo.
pause