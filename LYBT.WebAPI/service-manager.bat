@echo off
chcp 65001 >nul
title 凌隐宝堂中医诊所诊疗系统 - 服务管理器

:MAIN_MENU
cls
echo.
echo ====================================================
echo    凌隐宝堂中医诊所诊疗系统 - 服务管理器
echo ====================================================
echo.

:: 检查服务状态
set "SERVICE_NAME=LYBT.WebAPI"
sc query "%SERVICE_NAME%" >nul 2>&1
if %errorLevel% equ 0 (
    for /f "tokens=4" %%i in ('sc query "%SERVICE_NAME%" ^| findstr "STATE"') do set SERVICE_STATE=%%i
    if "!SERVICE_STATE!"=="RUNNING" (
        set STATUS_ICON=✅
        set STATUS_TEXT=运行中
        set STATUS_COLOR=绿色
    ) else (
        set STATUS_ICON=🔴
        set STATUS_TEXT=已停止
        set STATUS_COLOR=红色
    )
) else (
    set STATUS_ICON=❌
    set STATUS_TEXT=未安装
    set STATUS_COLOR=灰色
)

echo 📊 当前状态: %STATUS_ICON% %STATUS_TEXT%
echo.
echo 🎯 操作选项:
echo    [1] 安装服务
echo    [2] 启动服务
echo    [3] 停止服务
echo    [4] 重启服务
echo    [5] 卸载服务
echo    [6] 查看状态
echo    [7] 查看日志
echo    [8] 打开系统网页
echo    [9] 打开API文档
echo    [0] 退出
echo.

set /p "CHOICE=请选择操作 (0-9): "

if "%CHOICE%"=="1" goto INSTALL_SERVICE
if "%CHOICE%"=="2" goto START_SERVICE
if "%CHOICE%"=="3" goto STOP_SERVICE
if "%CHOICE%"=="4" goto RESTART_SERVICE
if "%CHOICE%"=="5" goto UNINSTALL_SERVICE
if "%CHOICE%"=="6" goto CHECK_STATUS
if "%CHOICE%"=="7" goto VIEW_LOGS
if "%CHOICE%"=="8" goto OPEN_WEB
if "%CHOICE%"=="9" goto OPEN_API_DOCS
if "%CHOICE%"=="0" goto EXIT

echo ❌ 无效选择，请重新输入
timeout /t 2 >nul
goto MAIN_MENU

:INSTALL_SERVICE
echo.
echo 🔧 正在安装服务...
call install-service.bat
pause
goto MAIN_MENU

:START_SERVICE
echo.
echo 🚀 正在启动服务...
call start-service.bat
pause
goto MAIN_MENU

:STOP_SERVICE
echo.
echo 🛑 正在停止服务...
call stop-service.bat
pause
goto MAIN_MENU

:RESTART_SERVICE
echo.
echo 🔄 正在重启服务...
echo 停止服务中...
call stop-service.bat >nul 2>&1
timeout /t 3 >nul
echo 启动服务中...
call start-service.bat
pause
goto MAIN_MENU

:UNINSTALL_SERVICE
echo.
echo 🗑️ 正在卸载服务...
call uninstall-service.bat
pause
goto MAIN_MENU

:CHECK_STATUS
echo.
echo 📊 正在检查服务状态...
call status-service.bat
pause
goto MAIN_MENU

:VIEW_LOGS
echo.
echo 📝 查看日志文件...
echo.
echo [1] 输出日志 (service-output.log)
echo [2] 错误日志 (service-error.log)
echo [3] 返回主菜单
echo.
set /p "LOG_CHOICE=请选择 (1-3): "

if "%LOG_CHOICE%"=="1" (
    if exist "%~dp0logs\service-output.log" (
        notepad "%~dp0logs\service-output.log"
    ) else (
        echo ❌ 输出日志文件不存在
        timeout /t 2 >nul
    )
) else if "%LOG_CHOICE%"=="2" (
    if exist "%~dp0logs\service-error.log" (
        notepad "%~dp0logs\service-error.log"
    ) else (
        echo ❌ 错误日志文件不存在
        timeout /t 2 >nul
    )
) else if "%LOG_CHOICE%"=="3" (
    goto MAIN_MENU
) else (
    echo ❌ 无效选择
    timeout /t 2 >nul
)
goto MAIN_MENU

:OPEN_WEB
echo.
echo 🌐 正在打开系统网页...
start http://localhost:5000
timeout /t 1 >nul
goto MAIN_MENU

:OPEN_API_DOCS
echo.
echo 📖 正在打开API文档...
start http://localhost:5000/swagger
timeout /t 1 >nul
goto MAIN_MENU

:EXIT
echo.
echo 👋 感谢使用凌隐宝堂中医诊所诊疗系统服务管理器
echo.
exit /b 0