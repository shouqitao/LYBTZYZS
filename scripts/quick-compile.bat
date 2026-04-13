@echo off
REM ========================================
REM UltraThink 一键快速编译
REM 最常用的编译命令集合
REM ========================================

cd /d "%~dp0\.."

echo ========================================
echo    UltraThink 快速编译
echo ========================================
echo.

REM 默认编译后端
if "%1"=="" goto backend

if "%1"=="backend" goto backend
if "%1"=="frontend" goto frontend
if "%1"=="test" goto test
if "%1"=="all" goto all
if "%1"=="help" goto help
goto invalid

:backend
echo [编译后端...]
dotnet build src\Backend\Services\LYBT.WebAPI\LYBT.WebAPI.csproj --configuration Debug
goto end

:frontend
echo [编译前端...]
dotnet build src\Frontend\Desktop\Shell\LYBT.WPF.Client.Shell.csproj --configuration Debug
goto end

:test
echo [编译测试...]
dotnet build tests\Backend\LYBT.Module.Herbs.Tests\LYBT.Module.Herbs.Tests.csproj --configuration Debug
goto end

:all
echo [编译全部...]
dotnet build LYBTZYZS.sln --configuration Debug
goto end

:help
echo.
echo 使用方法:
echo   quick-compile.bat          - 编译后端（默认）
echo   quick-compile.bat backend  - 编译后端
echo   quick-compile.bat frontend - 编译前端
echo   quick-compile.bat test     - 编译测试
echo   quick-compile.bat all      - 编译全部
echo   quick-compile.bat help     - 显示帮助
goto end

:invalid
echo [错误] 无效参数: %1
goto help

:end
if errorlevel 1 (
    echo.
    echo [编译失败] 运行 fix-compilation-errors.bat 修复错误
) else (
    echo.
    echo [编译成功]
)