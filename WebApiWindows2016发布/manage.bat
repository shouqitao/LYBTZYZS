@echo off
echo ====================================
echo   LYBT WebAPI 服务管理器
echo ====================================
echo.

:menu
echo 请选择操作:
echo [1] 启动 WebAPI 服务
echo [2] 停止 WebAPI 服务  
echo [3] 重启 WebAPI 服务
echo [4] 查看服务状态
echo [5] 查看日志
echo [6] 测试 API 连接
echo [0] 退出
echo.
set /p choice=请输入选项 (0-6): 

if "%choice%"=="1" goto start
if "%choice%"=="2" goto stop
if "%choice%"=="3" goto restart
if "%choice%"=="4" goto status
if "%choice%"=="5" goto logs
if "%choice%"=="6" goto test
if "%choice%"=="0" goto exit
goto menu

:start
echo [INFO] 启动 LYBT WebAPI 服务...
start /B LYBT.WebAPI.exe
echo [INFO] 服务启动命令已执行
timeout /t 3 >nul
goto menu

:stop
echo [INFO] 停止 LYBT WebAPI 服务...
taskkill /f /im LYBT.WebAPI.exe 2>nul
if %ERRORLEVEL%==0 (
    echo [INFO] 服务已停止
) else (
    echo [WARN] 未找到运行中的服务进程
)
goto menu

:restart
echo [INFO] 重启 LYBT WebAPI 服务...
call :stop
timeout /t 2 >nul
call :start
goto menu

:status
echo [INFO] 检查服务状态...
tasklist /fi "imagename eq LYBT.WebAPI.exe" 2>nul | find /i "LYBT.WebAPI.exe" >nul
if %ERRORLEVEL%==0 (
    echo [INFO] ✓ WebAPI 服务正在运行
    tasklist /fi "imagename eq LYBT.WebAPI.exe"
) else (
    echo [INFO] ✗ WebAPI 服务未运行
)
echo.
netstat -an | findstr ":5297" >nul
if %ERRORLEVEL%==0 (
    echo [INFO] ✓ 端口 5297 正在监听
) else (
    echo [INFO] ✗ 端口 5297 未在监听
)
goto menu

:test
echo [INFO] 测试 API 连接...
echo [INFO] 测试 Health Check...
curl -s http://localhost:5297/health || echo [ERROR] 连接失败
echo.
echo [INFO] 测试认证 API...
curl -s http://localhost:5297/api/v1.0/auth/hashPassword?password=test || echo [ERROR] 认证API连接失败
echo.
goto menu

:logs
echo [INFO] 查看最新日志...
if exist "logs" (
    echo [INFO] 显示最新日志文件...
    for /f "delims=" %%i in ('dir /b /o-d logs\*.txt 2^>nul') do (
        echo === 最新日志: logs\%%i ===
        type "logs\%%i" | more
        goto menu
    )
    echo [WARN] 日志目录为空
) else (
    echo [WARN] 未找到日志目录
)
goto menu

:exit
echo [INFO] 退出服务管理器
exit /b 0