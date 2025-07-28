@echo off
echo =========================================
echo          启动 LYBT WebAPI
echo =========================================

REM 首先检查是否有进程正在运行
echo 1. 检查现有进程...
set "found_existing=0"
for /f "tokens=5" %%a in ('netstat -aon ^| findstr :50 ^| findstr LISTENING 2^>nul') do (
    if not "%%a"=="0" (
        set "found_existing=1"
        echo    发现端口占用: %%a
    )
)

if "%found_existing%"=="1" (
    echo ⚠️  检测到已有进程在运行相关端口
    set /p choice="是否先停止现有进程? (Y/N): "
    if /i "!choice!"=="Y" (
        echo    正在停止现有进程...
        call "%~dp0stop-webapi.bat"
        echo    等待端口释放...
        timeout /t 2 >nul
    )
)

echo.
echo 2. 启动 LYBT WebAPI...

REM 切换到WebAPI目录
cd /d "%~dp0LYBT.WebAPI"
if errorlevel 1 (
    echo ❌ 错误: 无法找到LYBT.WebAPI目录
    echo    请确保此脚本位于项目根目录
    pause
    exit /b 1
)

REM 设置环境变量
set ASPNETCORE_ENVIRONMENT=Development
set DOTNET_CLI_TELEMETRY_OPTOUT=1

echo    环境: %ASPNETCORE_ENVIRONMENT%
echo    目录: %CD%
echo.

REM 检查项目文件是否存在
if not exist "LYBT.WebAPI.csproj" (
    echo ❌ 错误: 未找到LYBT.WebAPI.csproj文件
    echo    当前目录: %CD%
    pause
    exit /b 1
)

echo 3. 构建项目...
dotnet build --no-restore --verbosity quiet
if errorlevel 1 (
    echo ❌ 项目构建失败
    echo 是否要查看详细错误信息?
    set /p choice="(Y/N): "
    if /i "!choice!"=="Y" (
        dotnet build --no-restore --verbosity normal
    )
    pause
    exit /b 1
)

echo ✅ 构建成功
echo.
echo 4. 启动应用...
echo =========================================
echo    🚀 启动中，请稍候...
echo    💡 按 Ctrl+C 可优雅停止程序
echo    📊 Swagger: http://localhost:5xxx/swagger
echo =========================================
echo.

REM 启动应用 - 直接运行，不使用start命令避免后台运行
dotnet run --no-build

echo.
echo =========================================
echo    程序已退出
echo =========================================
pause