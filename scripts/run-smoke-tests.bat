@echo off
chcp 65001 >nul
echo.
echo ================================================================
echo 🏥 凌隐宝堂中医诊所系统 - 冒烟测试工具
echo ================================================================
echo.

:MENU
echo 请选择测试类型：
echo.
echo [1] 快速冒烟测试 (约30秒)
echo [2] 完整冒烟测试 (约2-3分钟)
echo [3] 完整测试(自动启动WebAPI)
echo [4] 查看上次测试报告
echo [5] 清理测试文件
echo [0] 退出
echo.
set /p choice="请输入选项 [0-5]: "

if "%choice%"=="1" goto QUICK_TEST
if "%choice%"=="2" goto FULL_TEST
if "%choice%"=="3" goto FULL_TEST_AUTO
if "%choice%"=="4" goto VIEW_REPORT
if "%choice%"=="5" goto CLEANUP
if "%choice%"=="0" goto EXIT
echo.
echo ❌ 无效选项，请重新选择
echo.
goto MENU

:QUICK_TEST
echo.
echo 🚀 执行快速冒烟测试...
echo.
powershell -ExecutionPolicy Bypass -File "scripts\quick-smoke-test.ps1"
set EXITCODE=%ERRORLEVEL%
goto RESULT

:FULL_TEST
echo.
echo 🚀 执行完整冒烟测试（需要手动启动WebAPI）...
echo.
echo 💡 请确保WebAPI服务已启动：
echo    dotnet run --project src/Backend/Services/LYBT.WebAPI --urls "https://localhost:7001"
echo.
pause
powershell -ExecutionPolicy Bypass -File "scripts\smoke-test.ps1" -StartWebAPI $false
set EXITCODE=%ERRORLEVEL%
goto RESULT

:FULL_TEST_AUTO
echo.
echo 🚀 执行完整冒烟测试（自动启动WebAPI）...
echo.
powershell -ExecutionPolicy Bypass -File "scripts\smoke-test.ps1" -StartWebAPI $true
set EXITCODE=%ERRORLEVEL%
goto RESULT

:VIEW_REPORT
echo.
echo 📄 查看测试报告...
echo.
if exist "temp\smoke-test-results.json" (
    echo 最后测试报告位置: temp\smoke-test-results.json
    echo.
    powershell -Command "try { $report = Get-Content 'temp\smoke-test-results.json' | ConvertFrom-Json; Write-Host '测试时间:' $report.StartTime -ForegroundColor Cyan; Write-Host '总计测试:' $report.Summary.Total; Write-Host '通过测试:' $report.Summary.Passed -ForegroundColor Green; Write-Host '失败测试:' $report.Summary.Failed -ForegroundColor Red; Write-Host '测试时长:' ([math]::Round($report.TotalDuration, 2)) '秒' } catch { Write-Host '无法读取测试报告文件' -ForegroundColor Red }"
) else (
    echo ❌ 未找到测试报告文件
    echo    请先运行完整冒烟测试以生成报告
)
echo.
pause
goto MENU

:CLEANUP
echo.
echo 🧹 清理测试文件...
if exist "temp\smoke-test-results.json" del "temp\smoke-test-results.json"
if exist "temp\*.log" del "temp\*.log"
echo ✅ 测试文件已清理
echo.
pause
goto MENU

:RESULT
echo.
echo ================================================================
if %EXITCODE%==0 (
    echo ✅ 测试完成 - 系统状态正常
) else if %EXITCODE%==1 (
    echo ⚠️ 测试完成 - 系统有轻微问题
) else if %EXITCODE%==2 (
    echo 🚨 测试完成 - 系统有严重问题  
) else (
    echo ❌ 测试过程中发生错误
)
echo ================================================================
echo.
echo 按任意键返回主菜单...
pause >nul
goto MENU

:EXIT
echo.
echo 👋 再见！
echo.