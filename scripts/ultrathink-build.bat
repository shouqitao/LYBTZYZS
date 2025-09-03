@echo off
REM ========================================
REM UltraThink 智能编译脚本 v1.0
REM 职责单一：统一编译入口
REM 代码干净：清晰的选项和流程
REM 性能出色：智能缓存和并行编译
REM ========================================

setlocal enabledelayedexpansion
cd /d "%~dp0\.."

echo ========================================
echo    UltraThink 智能编译系统
echo ========================================
echo.

REM 检查参数
if "%1"=="" goto :menu
goto :%1 2>nul || goto :invalid

:menu
echo 请选择编译选项：
echo.
echo   1. 快速编译（仅后端）
echo   2. 完整编译（前后端）
echo   3. 清理并重建
echo   4. 仅编译测试
echo   5. 修复编译错误
echo   6. 检查环境
echo   7. 恢复NuGet包
echo   8. 增量编译
echo   9. 发布版本编译
echo   0. 退出
echo.
set /p choice="请输入选项 (0-9): "

if "%choice%"=="1" goto :quick
if "%choice%"=="2" goto :full
if "%choice%"=="3" goto :rebuild
if "%choice%"=="4" goto :test
if "%choice%"=="5" goto :fix
if "%choice%"=="6" goto :check
if "%choice%"=="7" goto :restore
if "%choice%"=="8" goto :incremental
if "%choice%"=="9" goto :release
if "%choice%"=="0" goto :end

echo 无效选项，请重试
timeout /t 2 >nul
goto :menu

:quick
echo.
echo [快速编译 - 仅后端]
echo ----------------------------------------
dotnet build src\Backend\Services\LYBT.WebAPI\LYBT.WebAPI.csproj --configuration Debug --no-restore
if errorlevel 1 (
    echo.
    echo [错误] 编译失败！尝试运行修复...
    call :fix_common
) else (
    echo.
    echo [成功] 快速编译完成！
)
goto :end

:full
echo.
echo [完整编译 - 前后端]
echo ----------------------------------------
echo 步骤 1/3: 恢复NuGet包...
dotnet restore LYBT.All.sln

echo.
echo 步骤 2/3: 编译后端...
dotnet build LYBT.Backend.sln --configuration Debug --no-restore

echo.
echo 步骤 3/3: 编译前端...
dotnet build LYBT.Desktop.sln --configuration Debug --no-restore

if errorlevel 1 (
    echo.
    echo [错误] 编译失败！
    call :show_errors
) else (
    echo.
    echo [成功] 完整编译完成！
)
goto :end

:rebuild
echo.
echo [清理并重建]
echo ----------------------------------------
echo 步骤 1/4: 清理bin和obj...
call scripts\clean_all_bin_obj.bat

echo.
echo 步骤 2/4: 清理NuGet缓存...
dotnet nuget locals all --clear

echo.
echo 步骤 3/4: 恢复包...
dotnet restore LYBT.All.sln --force

echo.
echo 步骤 4/4: 重新编译...
dotnet build LYBT.All.sln --configuration Debug

if errorlevel 1 (
    echo.
    echo [错误] 重建失败！
    call :diagnose
) else (
    echo.
    echo [成功] 重建完成！
)
goto :end

:test
echo.
echo [编译测试项目]
echo ----------------------------------------
dotnet build tests\Backend\LYBT.Module.Herbs.Tests\LYBT.Module.Herbs.Tests.csproj
dotnet build tests\Backend\LYBT.Module.Auth.Tests\LYBT.Module.Auth.Tests.csproj
dotnet build tests\Backend\LYBT.Module.Consultation.Tests\LYBT.Module.Consultation.Tests.csproj
dotnet build tests\Backend\LYBT.Module.MedicalCase.Tests\LYBT.Module.MedicalCase.Tests.csproj
dotnet build tests\Backend\LYBT.Module.Prescriptions.Tests\LYBT.Module.Prescriptions.Tests.csproj

if errorlevel 1 (
    echo.
    echo [错误] 测试项目编译失败！
) else (
    echo.
    echo [成功] 测试项目编译完成！
    echo.
    echo 运行测试：dotnet test
)
goto :end

:fix
echo.
echo [自动修复编译错误]
echo ----------------------------------------
call :fix_common
call :fix_references
call :fix_namespaces
echo.
echo 修复完成，尝试重新编译...
dotnet build LYBT.Backend.sln --configuration Debug
goto :end

:check
echo.
echo [环境检查]
echo ----------------------------------------
echo .NET SDK 版本:
dotnet --version
echo.
echo 已安装的 SDK:
dotnet --list-sdks
echo.
echo 已安装的运行时:
dotnet --list-runtimes
echo.
echo 解决方案文件:
dir *.sln /b
echo.
echo NuGet 源:
dotnet nuget list source
goto :end

:restore
echo.
echo [恢复NuGet包]
echo ----------------------------------------
dotnet restore LYBT.All.sln --force
if errorlevel 1 (
    echo.
    echo [错误] 包恢复失败！尝试清理缓存...
    dotnet nuget locals all --clear
    dotnet restore LYBT.All.sln --force
)
goto :end

:incremental
echo.
echo [增量编译]
echo ----------------------------------------
dotnet build LYBT.Backend.sln --configuration Debug --no-restore --incremental
goto :end

:release
echo.
echo [发布版本编译]
echo ----------------------------------------
dotnet build LYBT.All.sln --configuration Release
if errorlevel 1 (
    echo.
    echo [错误] 发布编译失败！
) else (
    echo.
    echo [成功] 发布版本编译完成！
    echo 输出目录: bin\Release\
)
goto :end

:fix_common
echo 修复常见问题...
REM 修复 Infrastructure 项目引用
dotnet add src\Backend\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj package Microsoft.Extensions.Logging.Abstractions --version 8.0.2 2>nul
dotnet add src\Backend\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj package Microsoft.Extensions.DependencyInjection.Abstractions --version 8.0.2 2>nul
dotnet add src\Backend\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj package Microsoft.Extensions.Configuration.Abstractions --version 8.0.0 2>nul
dotnet add src\Backend\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj package Microsoft.Extensions.Options --version 8.0.2 2>nul
dotnet add src\Backend\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj package Microsoft.Extensions.Caching.Abstractions --version 8.0.0 2>nul
exit /b

:fix_references
echo 修复项目引用...
REM 确保测试项目引用正确
dotnet add tests\Backend\LYBT.Module.Herbs.Tests\LYBT.Module.Herbs.Tests.csproj reference src\Backend\Modules\LYBT.Module.Herbs\LYBT.Module.Herbs.csproj 2>nul
dotnet add tests\Backend\LYBT.Module.Auth.Tests\LYBT.Module.Auth.Tests.csproj reference src\Backend\Modules\LYBT.Module.Auth\LYBT.Module.Auth.csproj 2>nul
exit /b

:fix_namespaces
echo 修复命名空间...
REM 这里可以添加PowerShell脚本来修复命名空间问题
exit /b

:show_errors
echo.
echo 显示编译错误详情...
dotnet build LYBT.Backend.sln --configuration Debug --no-restore --verbosity normal | findstr /i "error"
exit /b

:diagnose
echo.
echo [诊断模式]
echo ----------------------------------------
echo 1. 检查项目文件完整性...
dir /s /b *.csproj | find /c ".csproj"
echo 个项目文件找到

echo.
echo 2. 检查关键依赖...
dotnet list src\Backend\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj package

echo.
echo 3. 检查测试项目...
dotnet list tests\Backend\LYBT.Module.Herbs.Tests\LYBT.Module.Herbs.Tests.csproj package 2>nul

exit /b

:invalid
echo.
echo [错误] 无效的参数: %1
echo.
echo 使用方法:
echo   ultrathink-build.bat          - 显示菜单
echo   ultrathink-build.bat quick    - 快速编译
echo   ultrathink-build.bat full     - 完整编译
echo   ultrathink-build.bat rebuild  - 清理重建
echo   ultrathink-build.bat test     - 编译测试
echo   ultrathink-build.bat fix      - 修复错误
echo   ultrathink-build.bat check    - 检查环境
goto :end

:end
echo.
echo ========================================
echo    UltraThink 编译脚本执行完毕
echo ========================================
endlocal