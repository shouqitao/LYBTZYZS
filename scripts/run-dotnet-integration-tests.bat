@echo off
REM .NET 集成测试执行脚本
chcp 65001 > nul
setlocal enabledelayedexpansion

echo ========================================
echo .NET 集成测试
echo ========================================
echo.

REM 确保在项目根目录执行
cd /d "%~dp0\.."

REM 检查.NET SDK
echo [1/5] 检查.NET SDK...
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo [错误] 未找到.NET SDK，请先安装.NET 8
    pause
    exit /b 1
)
echo ✓ .NET SDK 已安装

REM 恢复NuGet包
echo.
echo [2/5] 恢复NuGet包...
dotnet restore LYBT.All.sln
if errorlevel 1 (
    echo [错误] NuGet包恢复失败
    pause
    exit /b 1
)
echo ✓ NuGet包恢复成功

REM 构建测试项目
echo.
echo [3/5] 构建测试项目...
dotnet build tests\Integration\LYBT.IntegrationTests\LYBT.IntegrationTests.csproj -c Release
if errorlevel 1 (
    echo [错误] 测试项目构建失败
    pause
    exit /b 1
)
echo ✓ 测试项目构建成功

REM 运行集成测试
echo.
echo [4/5] 执行集成测试...
echo ----------------------------------------
dotnet test tests\Integration\LYBT.IntegrationTests\LYBT.IntegrationTests.csproj ^
    -c Release ^
    --no-build ^
    --logger "console;verbosity=detailed" ^
    --logger "trx;LogFileName=ConsultationFlowTest.trx" ^
    --results-directory TestResults

set test_result=%errorlevel%

REM 生成测试报告
echo.
echo [5/5] 测试结果汇总...
if %test_result%==0 (
    echo ✓ 所有测试通过
    set test_status=PASSED
) else (
    echo ✗ 有测试失败
    set test_status=FAILED
)

REM 显示测试报告位置
if exist "TestResults\ConsultationFlowTest.trx" (
    echo.
    echo 测试报告已生成：
    echo - TestResults\ConsultationFlowTest.trx
)

echo.
echo ========================================
echo 测试完成！
echo 结果: %test_status%
echo ========================================
echo.

REM 询问是否查看详细报告
if exist "TestResults\ConsultationFlowTest.trx" (
    echo 是否查看详细测试报告？ (Y/N)
    set /p view_report=
    if /i "!view_report!"=="Y" (
        start "" "TestResults\ConsultationFlowTest.trx"
    )
)

pause
exit /b %test_result%