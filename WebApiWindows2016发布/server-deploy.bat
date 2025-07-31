@echo off
:: 设置代码页为UTF-8以支持中文显示
chcp 65001 >nul 2>&1
:: 启用延迟变量扩展
setlocal enabledelayedexpansion
echo ====================================
echo   LYBT WebAPI 服务器端部署脚本
echo ====================================
echo.

:: 配置变量
set "DEPLOY_PATH=C:\LYBT\WebAPI"
set "BACKUP_PATH=C:\LYBT\Backup"
set "TEMP_PATH=C:\temp"
set "SERVICE_NAME=LYBTWebAPI"
set "ZIP_FILE=%TEMP_PATH%\WebAPI-Deploy.zip"

echo [步骤 1] 检查部署包...
if not exist "!ZIP_FILE!" (
    echo ❌ 部署包不存在: !ZIP_FILE!
    exit /b 1
)

echo [步骤 2] 停止 WebAPI 服务...
tasklist | findstr "LYBT.WebAPI.exe" >nul
if %errorlevel% equ 0 (
    echo 正在停止现有服务...
    taskkill /f /im "LYBT.WebAPI.exe" /t
    timeout /t 3 /nobreak >nul
)

:: 如果配置了 Windows 服务
sc query "%SERVICE_NAME%" >nul 2>&1
if %errorlevel% equ 0 (
    echo 停止 Windows 服务...
    net stop "%SERVICE_NAME%"
    timeout /t 5 /nobreak >nul
)

echo [步骤 3] 备份当前版本...
if exist "!DEPLOY_PATH!" (
    for /f "tokens=1-3 delims=/ " %%a in ('date /t') do set "DATESTR=%%c-%%a-%%b"
    for /f "tokens=1-2 delims=: " %%a in ('time /t') do set "TIMESTR=%%a%%b"
    set "TIMESTR=!TIMESTR: =!"
    set "BACKUP_FOLDER=!BACKUP_PATH!\WebAPI_!DATESTR!_!TIMESTR!"
    
    if not exist "!BACKUP_PATH!" mkdir "!BACKUP_PATH!"
    echo 备份到: !BACKUP_FOLDER!
    xcopy "!DEPLOY_PATH!\*" "!BACKUP_FOLDER!\" /E /I /Q >nul 2>&1
)

echo [步骤 4] 清理部署目录...
if exist "!DEPLOY_PATH!" (
    rmdir /s /q "!DEPLOY_PATH!" >nul 2>&1
)
mkdir "!DEPLOY_PATH!" >nul 2>&1

echo [步骤 5] 解压新版本...
powershell -Command "& {[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; Expand-Archive -Path '!ZIP_FILE!' -DestinationPath '!DEPLOY_PATH!' -Force}"

echo [步骤 6] 设置权限...
icacls "!DEPLOY_PATH!" /grant "IIS_IUSRS:(OI)(CI)F" /T >nul 2>&1
icacls "!DEPLOY_PATH!" /grant "IUSR:(OI)(CI)F" /T >nul 2>&1

echo [步骤 7] 启动服务...
cd /d "!DEPLOY_PATH!"

:: 如果配置了 Windows 服务
sc query "!SERVICE_NAME!" >nul 2>&1
if !errorlevel! equ 0 (
    echo 启动 Windows 服务...
    net start "!SERVICE_NAME!" >nul 2>&1
) else (
    echo 启动应用程序...
    start "LYBT WebAPI" /min "LYBT.WebAPI.exe"
)

echo [步骤 8] 验证服务状态...
timeout /t 10 /nobreak >nul
powershell -Command "& {[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; try { Invoke-RestMethod -Uri 'http://localhost:5297/health' -TimeoutSec 10 -ErrorAction Stop | Out-Null; Write-Host '✅ 服务启动成功！' -ForegroundColor Green } catch { Write-Host '⚠️  服务可能未完全启动' -ForegroundColor Yellow }}"

echo [步骤 9] 清理临时文件...
del "!ZIP_FILE!" /f /q >nul 2>&1

echo.
echo ✅ 部署完成！
echo 🕒 部署时间: %date% %time%
echo 🌐 服务地址: http://localhost:5297
echo.

:: 记录部署日志
echo %date% %time% - WebAPI 部署成功 >> C:\LYBT\Logs\deploy.log