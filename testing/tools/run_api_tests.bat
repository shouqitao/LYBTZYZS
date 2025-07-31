@echo off
echo ====================================
echo LYBT API 自动化测试脚本
echo ====================================
echo.

REM 检查Newman是否安装
newman --version >nul 2>&1
if errorlevel 1 (
    echo [错误] Newman未安装，请先安装Newman:
    echo npm install -g newman
    pause
    exit /b 1
)

echo [信息] 开始执行API测试...
echo.

REM 执行API测试
newman run "LYBT_API_Tests.postman_collection.json" ^
    -e "LYBT_Dev_Environment.postman_environment.json" ^
    --reporters cli,html ^
    --reporter-html-export "test_results.html" ^
    --timeout-request 10000 ^
    --delay-request 500

echo.
echo ====================================
echo 测试完成！
echo 测试报告已生成: test_results.html
echo ====================================
pause