@echo off
echo =========================================
echo          停止 LYBT WebAPI 进程
echo =========================================

REM 首先尝试优雅关闭 - 向所有运行的dotnet进程发送Ctrl+C信号
echo 1. 尝试优雅关闭dotnet进程...
for /f "tokens=2" %%a in ('tasklist /fi "imagename eq dotnet.exe" /fo csv ^| findstr /v "PID"') do (
    if not "%%a"=="" (
        echo    正在向进程 %%a 发送关闭信号...
        powershell -Command "try { $p = Get-Process -Id %%a -EA Stop; $p.CloseMainWindow() } catch { Write-Host '进程 %%a 已结束' }" >nul 2>&1
    )
)

REM 等待3秒让进程自然关闭
echo    等待进程自然关闭...
timeout /t 3 /nobreak >nul

REM 检查并强制停止仍在运行的LYBT相关进程
echo 2. 强制停止剩余的LYBT WebAPI进程...

REM 停止监听5000-5300端口的进程
for /f "tokens=5" %%a in ('netstat -aon ^| findstr :50 ^| findstr LISTENING') do (
    if not "%%a"=="0" (
        set "pid=%%a"
        setlocal enabledelayedexpansion
        for /f %%b in ('tasklist /fi "pid eq !pid!" /fo csv /nh ^| findstr /i "dotnet\|LYBT"') do (
            echo    强制停止进程 !pid! (端口相关)
            taskkill /pid !pid! /f >nul 2>&1
        )
        endlocal
    )
)

REM 停止所有包含LYBT字样的dotnet进程
for /f "tokens=2" %%a in ('tasklist /fi "imagename eq dotnet.exe" /fo csv /nh ^| findstr /v "PID"') do (
    if not "%%a"=="" (
        set "pid=%%a"
        setlocal enabledelayedexpansion
        powershell -Command "try { $proc = Get-Process -Id !pid! -EA Stop; if ($proc.ProcessName -eq 'dotnet' -and ($proc.MainWindowTitle -like '*LYBT*' -or $proc.CommandLine -like '*LYBT*')) { Stop-Process -Id !pid! -Force } } catch { }" >nul 2>&1
        endlocal
    )
)

REM 清理可能残留的端口占用
echo 3. 清理端口占用...
netstat -ano | findstr :5000 | findstr LISTENING >nul 2>&1
if not errorlevel 1 (
    for /f "tokens=5" %%a in ('netstat -aon ^| findstr :5000 ^| findstr LISTENING') do (
        echo    清理端口5000占用进程 %%a
        taskkill /pid %%a /f >nul 2>&1
    )
)

REM 验证清理结果
echo 4. 验证清理结果...
set "found_process=0"
for /f "tokens=2" %%a in ('tasklist /fi "imagename eq dotnet.exe" /fo csv /nh 2^>nul ^| findstr /v "PID"') do (
    if not "%%a"=="" (
        set "found_process=1"
    )
)

if "%found_process%"=="0" (
    echo ✅ 所有LYBT WebAPI进程已停止
    echo ✅ 端口已释放
) else (
    echo ❌ 仍有部分dotnet进程在运行，请手动检查
    echo    运行 'tasklist /fi "imagename eq dotnet.exe"' 查看剩余进程
)

echo.
echo =========================================
echo           清理完成
echo =========================================
timeout /t 2 >nul