@echo off
REM 看诊流程集成测试执行脚本
chcp 65001 > nul
setlocal enabledelayedexpansion

echo ========================================
echo 看诊流程集成测试
echo ========================================
echo.

REM 检查Python是否安装
python --version >nul 2>&1
if errorlevel 1 (
    echo [错误] 未找到Python，请先安装Python 3.x
    pause
    exit /b 1
)

REM 确保在项目根目录执行
cd /d "%~dp0\.."

REM 检查API服务是否运行
echo [1/4] 检查API服务状态...
curl -k -s https://localhost:7001/swagger/index.html >nul 2>&1
if errorlevel 1 (
    echo [警告] API服务未运行，请先启动服务
    echo.
    echo 是否要启动开发服务器？ (Y/N)
    set /p start_server=
    if /i "!start_server!"=="Y" (
        echo 正在启动开发服务器...
        start "API Server" cmd /c "scripts\start-dev.bat"
        echo 等待服务启动...
        timeout /t 10 /nobreak >nul
    ) else (
        echo 请手动启动API服务后重试
        pause
        exit /b 1
    )
)
echo ✓ API服务正在运行

REM 安装Python依赖
echo.
echo [2/4] 检查Python依赖...
pip show requests >nul 2>&1
if errorlevel 1 (
    echo 正在安装依赖包...
    pip install requests
)
echo ✓ 依赖包已安装

REM 运行集成测试
echo.
echo [3/4] 执行看诊流程测试...
echo ----------------------------------------
python tests\Integration\consultation_flow_test.py
set test_result=%errorlevel%

REM 生成测试报告
echo.
echo [4/4] 生成测试报告...
if %test_result%==0 (
    echo ✓ 测试通过
    set test_status=PASSED
) else (
    echo ✗ 测试失败
    set test_status=FAILED
)

REM 记录测试结果
echo. >> test-results.log
echo ======================================== >> test-results.log
echo 看诊流程集成测试 - %date% %time% >> test-results.log
echo 测试结果: %test_status% >> test-results.log
echo ======================================== >> test-results.log

echo.
echo ========================================
echo 测试完成！
echo 结果: %test_status%
echo ========================================
echo.

pause
exit /b %test_result%