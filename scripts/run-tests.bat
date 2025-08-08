@echo off
chcp 65001 > nul
setlocal enabledelayedexpansion

echo ====================================================
echo 凌隐宝堂中医诊所系统 - 核心功能测试
echo ====================================================
echo 执行时间: %date% %time%
echo.

:: 设置颜色
set "GREEN=[92m"
set "RED=[91m"
set "YELLOW=[93m"
set "RESET=[0m"

:: 初始化计数器
set /a total_tests=0
set /a passed_tests=0
set /a failed_tests=0

:: 测试结果文件
set "test_report=test_execution_report_%date:~0,4%%date:~5,2%%date:~8,2%_%time:~0,2%%time:~3,2%%time:~6,2%.txt"
set "test_report=%test_report: =0%"

echo 测试执行报告 > "%test_report%"
echo ===================== >> "%test_report%"
echo 执行时间: %date% %time% >> "%test_report%"
echo. >> "%test_report%"

:: 检查环境
echo [步骤 1/5] 检查测试环境
echo --------------------------------------------------

:: 检查 .NET SDK
dotnet --version > nul 2>&1
if errorlevel 1 (
    echo %RED%✗ .NET SDK 未安装%RESET%
    echo ✗ .NET SDK 未安装 >> "%test_report%"
    goto :error
) else (
    for /f "tokens=*" %%i in ('dotnet --version') do set dotnet_version=%%i
    echo %GREEN%✓ .NET SDK 已安装: !dotnet_version!%RESET%
    echo ✓ .NET SDK 已安装: !dotnet_version! >> "%test_report%"
)

:: 检查项目文件
if not exist "..\LYBT.Backend.sln" (
    echo %RED%✗ 未找到后端解决方案文件%RESET%
    echo ✗ 未找到后端解决方案文件 >> "%test_report%"
    goto :error
) else (
    echo %GREEN%✓ 找到后端解决方案%RESET%
    echo ✓ 找到后端解决方案 >> "%test_report%"
)

echo.

:: 运行后端单元测试
echo [步骤 2/5] 运行后端单元测试
echo --------------------------------------------------
echo 运行后端单元测试 >> "%test_report%"
echo ================== >> "%test_report%"

if exist "..\tests\Backend\LYBT.WebAPI.Tests\LYBT.WebAPI.Tests.csproj" (
    echo 正在运行 WebAPI 测试...
    cd ..\tests\Backend\LYBT.WebAPI.Tests
    
    :: 运行测试并捕获结果
    dotnet test --no-build --verbosity normal > test_output.tmp 2>&1
    set test_result=!errorlevel!
    
    :: 分析测试结果
    findstr /C:"Passed" test_output.tmp > nul
    if !errorlevel! equ 0 (
        for /f "tokens=2" %%a in ('findstr /C:"Passed" test_output.tmp') do set /a passed=%%a
        set /a passed_tests+=!passed!
    )
    
    findstr /C:"Failed" test_output.tmp > nul
    if !errorlevel! equ 0 (
        for /f "tokens=2" %%a in ('findstr /C:"Failed" test_output.tmp') do set /a failed=%%a
        set /a failed_tests+=!failed!
    )
    
    if !test_result! equ 0 (
        echo %GREEN%✓ WebAPI 测试通过%RESET%
        echo ✓ WebAPI 测试通过 >> "..\..\..\scripts\%test_report%"
    ) else (
        echo %RED%✗ WebAPI 测试失败%RESET%
        echo ✗ WebAPI 测试失败 >> "..\..\..\scripts\%test_report%"
        type test_output.tmp >> "..\..\..\scripts\%test_report%"
    )
    
    del test_output.tmp 2>nul
    cd ..\..\..\scripts
) else (
    echo %YELLOW%⚠ 未找到后端测试项目%RESET%
    echo ⚠ 未找到后端测试项目 >> "%test_report%"
)

echo.

:: 运行前端单元测试
echo [步骤 3/5] 运行前端单元测试
echo --------------------------------------------------
echo 运行前端单元测试 >> "%test_report%"
echo ================== >> "%test_report%"

if exist "..\tests\Frontend\LYBT.WPF.Client.Tests\LYBT.WPF.Client.Tests.csproj" (
    echo 正在运行 WPF Client 测试...
    cd ..\tests\Frontend\LYBT.WPF.Client.Tests
    
    :: 运行测试
    dotnet test --no-build --verbosity normal > test_output.tmp 2>&1
    set test_result=!errorlevel!
    
    :: 分析测试结果
    findstr /C:"Passed" test_output.tmp > nul
    if !errorlevel! equ 0 (
        for /f "tokens=2" %%a in ('findstr /C:"Passed" test_output.tmp') do set /a passed=%%a
        set /a passed_tests+=!passed!
    )
    
    if !test_result! equ 0 (
        echo %GREEN%✓ WPF Client 测试通过%RESET%
        echo ✓ WPF Client 测试通过 >> "..\..\..\scripts\%test_report%"
    ) else (
        echo %RED%✗ WPF Client 测试失败%RESET%
        echo ✗ WPF Client 测试失败 >> "..\..\..\scripts\%test_report%"
    )
    
    del test_output.tmp 2>nul
    cd ..\..\..\scripts
) else (
    echo %YELLOW%⚠ 未找到前端测试项目%RESET%
    echo ⚠ 未找到前端测试项目 >> "%test_report%"
)

echo.

:: 运行集成测试
echo [步骤 4/5] 运行集成测试
echo --------------------------------------------------
echo 运行集成测试 >> "%test_report%"
echo ============== >> "%test_report%"

if exist "integration-test.py" (
    echo 检查 Python 环境...
    python --version > nul 2>&1
    if errorlevel 1 (
        echo %YELLOW%⚠ Python 未安装，跳过集成测试%RESET%
        echo ⚠ Python 未安装，跳过集成测试 >> "%test_report%"
    ) else (
        echo %YELLOW%⚠ 集成测试需要API服务运行中%RESET%
        echo 请确保:
        echo   1. SQL Server 服务已启动
        echo   2. 后端API服务运行在 https://localhost:7001
        echo.
        echo 是否继续运行集成测试? (Y/N)
        set /p run_integration=
        
        if /i "!run_integration!"=="Y" (
            echo 正在运行集成测试...
            python integration-test.py
            if !errorlevel! equ 0 (
                echo %GREEN%✓ 集成测试通过%RESET%
                echo ✓ 集成测试通过 >> "%test_report%"
            ) else (
                echo %RED%✗ 集成测试失败%RESET%
                echo ✗ 集成测试失败 >> "%test_report%"
            )
        ) else (
            echo 跳过集成测试
            echo 跳过集成测试 >> "%test_report%"
        )
    )
) else (
    echo %YELLOW%⚠ 未找到集成测试脚本%RESET%
    echo ⚠ 未找到集成测试脚本 >> "%test_report%"
)

echo.

:: 测试总结
echo [步骤 5/5] 测试总结
echo --------------------------------------------------
set /a total_tests=passed_tests+failed_tests

echo.
echo ===== 测试结果总结 ===== >> "%test_report%"
echo 总测试数: !total_tests! >> "%test_report%"
echo 通过: !passed_tests! >> "%test_report%"
echo 失败: !failed_tests! >> "%test_report%"

if !failed_tests! equ 0 (
    echo %GREEN%========================================%RESET%
    echo %GREEN%    所有测试通过！%RESET%
    echo %GREEN%    总计: !total_tests! 个测试%RESET%
    echo %GREEN%========================================%RESET%
    echo.
    echo 测试状态: 通过 >> "%test_report%"
) else (
    echo %RED%========================================%RESET%
    echo %RED%    测试失败！%RESET%
    echo %RED%    通过: !passed_tests! / !total_tests!%RESET%
    echo %RED%    失败: !failed_tests!%RESET%
    echo %RED%========================================%RESET%
    echo.
    echo 测试状态: 失败 >> "%test_report%"
)

echo.
echo 详细报告已保存到: %test_report%
echo.

:: 选项菜单
echo 请选择操作:
echo 1. 查看测试报告
echo 2. 运行代码覆盖率分析
echo 3. 退出
echo.
set /p choice=请输入选项 (1-3): 

if "%choice%"=="1" (
    notepad "%test_report%"
) else if "%choice%"=="2" (
    echo.
    echo 运行代码覆盖率分析...
    cd ..
    dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
    echo.
    echo 覆盖率分析完成
    cd scripts
)

goto :end

:error
echo.
echo %RED%测试执行失败！%RESET%
echo 请检查环境配置和错误信息。

:end
echo.
pause