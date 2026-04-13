@echo off
REM ======================================
REM 快速编译检查脚本
REM 用于快速检查编译错误
REM ======================================
setlocal enabledelayedexpansion

echo ========================================
echo 凌隐宝堂中医诊所系统 - 快速编译检查
echo ========================================
echo.

REM 设置颜色
color 0A

REM 记录开始时间
set start_time=%time%

REM 选择编译目标
if "%1"=="" (
    echo 请选择编译目标：
    echo 1. 后端 (LYBT.Backend.sln)
    echo 2. 前端 (LYBT.Desktop.sln) 
    echo 3. 完整 (LYBTZYZS.sln)
    echo 4. 仅检查错误（不编译）
    echo.
    set /p choice="请输入选择 (1-4): "
) else (
    set choice=%1
)

REM 清理临时文件
if exist build-errors.txt del build-errors.txt
if exist build-warnings.txt del build-warnings.txt

REM 根据选择执行编译
if "!choice!"=="1" (
    echo 正在编译后端解决方案...
    echo.
    dotnet build LYBT.Backend.sln --no-incremental --verbosity quiet 2>&1 | findstr /R /C:"error CS" /C:"error MSB" /C:"error NU" > build-errors.txt
    set solution=LYBT.Backend.sln
) else if "!choice!"=="2" (
    echo 正在编译前端解决方案...
    echo.
    dotnet build LYBT.Desktop.sln --no-incremental --verbosity quiet 2>&1 | findstr /R /C:"error CS" /C:"error MSB" /C:"error NU" > build-errors.txt
    set solution=LYBT.Desktop.sln
) else if "!choice!"=="3" (
    echo 正在编译完整解决方案...
    echo.
    REM 处理 LYBTZYZS.sln 的特殊情况（避免 MSB1008 错误）
    dotnet build LYBT.Backend.sln --no-incremental --verbosity quiet 2>&1 | findstr /R /C:"error CS" /C:"error MSB" /C:"error NU" > build-errors-backend.txt
    dotnet build LYBT.Desktop.sln --no-incremental --verbosity quiet 2>&1 | findstr /R /C:"error CS" /C:"error MSB" /C:"error NU" > build-errors-frontend.txt
    type build-errors-backend.txt build-errors-frontend.txt > build-errors.txt 2>nul
    del build-errors-backend.txt build-errors-frontend.txt 2>nul
    set solution=All Solutions
) else if "!choice!"=="4" (
    echo 跳过编译，仅分析现有错误...
    goto :analyze
) else (
    echo 无效选择！
    goto :end
)

:analyze
REM 统计错误
for /f %%a in ('type build-errors.txt 2^>nul ^| find /c "error"') do set error_count=%%a

echo.
echo ========================================
echo 编译结果摘要
echo ========================================
echo 解决方案: %solution%
echo 错误数量: %error_count%
echo.

if %error_count% GTR 0 (
    echo ========================================
    echo 错误详情（前20个）：
    echo ========================================
    type build-errors.txt | head -20
    echo.
    echo 完整错误列表已保存到: build-errors.txt
) else (
    echo ✅ 编译成功！没有错误。
)

REM 记录结束时间
echo.
echo 编译检查完成时间: %time%
echo 开始时间: %start_time%

:end
echo.
pause