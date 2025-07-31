@echo off
title LYBT开发环境管理器
color 0A

echo.
echo  ╔══════════════════════════════════════╗
echo  ║     LYBT医疗系统 - 开发环境管理器     ║
echo  ╚══════════════════════════════════════╝
echo.

:MENU
echo  请选择操作：
echo  [1] 🔄 重启开发环境 (推荐)
echo  [2] 📛 仅停止所有进程
echo  [3] 🔨 仅重新编译
echo  [4] 🚀 启动服务
echo  [5] 🧹 深度清理
echo  [0] 退出
echo.
set /p choice=请输入选项 (0-5): 

if %choice%==1 goto RESTART_ALL
if %choice%==2 goto STOP_PROCESSES
if %choice%==3 goto BUILD_ONLY
if %choice%==4 goto START_SERVICES
if %choice%==5 goto DEEP_CLEAN
if %choice%==0 goto EXIT
goto MENU

:RESTART_ALL
echo.
echo 🔄 正在重启LYBT开发环境...
echo ================================
call :STOP_PROCESSES_SILENT
call :BUILD_PROJECT
if %errorlevel% neq 0 goto BUILD_ERROR
call :START_SERVICES_SILENT
echo ✅ 开发环境重启完成！
goto MENU

:STOP_PROCESSES
echo.
echo 📛 正在停止所有LYBT进程...
call :STOP_PROCESSES_SILENT
echo ✅ 所有进程已停止
goto MENU

:STOP_PROCESSES_SILENT
echo   - 停止WebAPI进程...
taskkill /F /IM "LYBT.WebAPI.exe" 2>nul
echo   - 停止WPF客户端...
taskkill /F /IM "LYBT.WPF.Client.Shell.exe" 2>nul
echo   - 停止其他LYBT进程...
powershell -Command "Get-Process -Name '*LYBT*' -ErrorAction SilentlyContinue | Stop-Process -Force" 2>nul
timeout /t 1 /nobreak >nul
exit /b 0

:BUILD_ONLY
echo.
echo 🔨 正在编译项目...
call :BUILD_PROJECT
if %errorlevel% neq 0 goto BUILD_ERROR
echo ✅ 编译完成
goto MENU

:BUILD_PROJECT
echo   - 清理编译输出...
dotnet clean >nul 2>&1
echo   - 正在编译解决方案...
dotnet build
exit /b %errorlevel%

:START_SERVICES
echo.
echo 🚀 正在启动服务...
call :START_SERVICES_SILENT
echo ✅ 服务启动完成
goto MENU

:START_SERVICES_SILENT
echo   - 启动WebAPI服务...
start "LYBT WebAPI" /MIN cmd /c "cd /d src\Backend\Services\LYBT.WebAPI && dotnet run"
echo   - 等待服务初始化...
timeout /t 3 /nobreak >nul
echo   - 启动WPF客户端...
if exist "BIN\net8.0-windows\LYBT.WPF.Client.Shell.exe" (
    start "LYBT WPF" "BIN\net8.0-windows\LYBT.WPF.Client.Shell.exe"
) else (
    echo     ⚠️ WPF客户端未找到，请先编译项目
)
exit /b 0

:DEEP_CLEAN
echo.
echo 🧹 正在进行深度清理...
echo   - 停止所有进程...
call :STOP_PROCESSES_SILENT
echo   - 删除BIN目录...
if exist "BIN" rmdir /s /q "BIN" 2>nul
echo   - 删除obj目录...
for /d /r . %%d in (obj) do @if exist "%%d" rmdir /s /q "%%d" 2>nul
echo   - 删除临时文件...
for /d /r . %%d in (bin) do @if exist "%%d" rmdir /s /q "%%d" 2>nul
echo   - 清理NuGet缓存...
dotnet nuget locals all --clear >nul 2>&1
echo   - 重新还原包...
dotnet restore >nul 2>&1
echo ✅ 深度清理完成
goto MENU

:BUILD_ERROR
echo.
echo ❌ 编译失败！
echo 请检查编译错误信息，修复后重试。
echo.
pause
goto MENU

:EXIT
echo.
echo 👋 再见！开发愉快！
timeout /t 2 /nobreak >nul
exit

:ERROR
echo ❌ 操作失败！
pause
goto MENU