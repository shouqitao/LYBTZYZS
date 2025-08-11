@echo off
REM ======================================
REM 编译错误分析脚本
REM 详细分析和分类编译错误
REM ======================================
setlocal enabledelayedexpansion

echo ========================================
echo 凌隐宝堂系统 - 编译错误深度分析
echo ========================================
echo.

REM 创建临时目录
if not exist temp mkdir temp

REM 步骤1：编译并收集所有输出
echo [1/4] 收集编译信息...
dotnet build LYBT.Backend.sln --no-incremental --verbosity normal > temp\build-backend.log 2>&1
dotnet build LYBT.Desktop.sln --no-incremental --verbosity normal > temp\build-frontend.log 2>&1

REM 步骤2：提取错误
echo [2/4] 提取错误信息...
findstr /R /C:"error CS" temp\build-backend.log temp\build-frontend.log > temp\cs-errors.txt 2>nul
findstr /R /C:"error MSB" temp\build-backend.log temp\build-frontend.log > temp\msb-errors.txt 2>nul
findstr /R /C:"error NU" temp\build-backend.log temp\build-frontend.log > temp\nu-errors.txt 2>nul

REM 步骤3：分类错误
echo [3/4] 分类错误类型...

REM CS错误分类
echo ===== CS编译错误 ===== > temp\error-analysis.txt
for /f %%a in ('type temp\cs-errors.txt 2^>nul ^| find /c "CS"') do set cs_count=%%a
echo CS错误总数: !cs_count! >> temp\error-analysis.txt
echo. >> temp\error-analysis.txt

REM 常见CS错误分类
findstr "CS0246" temp\cs-errors.txt > temp\cs0246.txt 2>nul
for /f %%a in ('type temp\cs0246.txt 2^>nul ^| find /c "CS0246"') do (
    if %%a GTR 0 echo   CS0246 (类型或命名空间未找到): %%a 个 >> temp\error-analysis.txt
)

findstr "CS0117" temp\cs-errors.txt > temp\cs0117.txt 2>nul
for /f %%a in ('type temp\cs0117.txt 2^>nul ^| find /c "CS0117"') do (
    if %%a GTR 0 echo   CS0117 (不包含定义): %%a 个 >> temp\error-analysis.txt
)

findstr "CS1061" temp\cs-errors.txt > temp\cs1061.txt 2>nul
for /f %%a in ('type temp\cs1061.txt 2^>nul ^| find /c "CS1061"') do (
    if %%a GTR 0 echo   CS1061 (不包含定义且找不到扩展方法): %%a 个 >> temp\error-analysis.txt
)

findstr "CS0029" temp\cs-errors.txt > temp\cs0029.txt 2>nul
for /f %%a in ('type temp\cs0029.txt 2^>nul ^| find /c "CS0029"') do (
    if %%a GTR 0 echo   CS0029 (无法隐式转换类型): %%a 个 >> temp\error-analysis.txt
)

echo. >> temp\error-analysis.txt

REM MSBuild错误
echo ===== MSBuild错误 ===== >> temp\error-analysis.txt
for /f %%a in ('type temp\msb-errors.txt 2^>nul ^| find /c "MSB"') do set msb_count=%%a
echo MSBuild错误总数: !msb_count! >> temp\error-analysis.txt
echo. >> temp\error-analysis.txt

REM NuGet错误
echo ===== NuGet错误 ===== >> temp\error-analysis.txt
for /f %%a in ('type temp\nu-errors.txt 2^>nul ^| find /c "NU"') do set nu_count=%%a
echo NuGet错误总数: !nu_count! >> temp\error-analysis.txt
echo. >> temp\error-analysis.txt

REM 步骤4：生成报告
echo [4/4] 生成分析报告...

echo ======================================== > build-report.txt
echo 编译错误分析报告 >> build-report.txt
echo 生成时间: %date% %time% >> build-report.txt
echo ======================================== >> build-report.txt
echo. >> build-report.txt
type temp\error-analysis.txt >> build-report.txt
echo. >> build-report.txt
echo ======================================== >> build-report.txt
echo 详细错误列表（前50个）： >> build-report.txt
echo ======================================== >> build-report.txt
type temp\cs-errors.txt 2>nul | head -50 >> build-report.txt

REM 显示摘要
cls
echo ========================================
echo 编译错误分析完成
echo ========================================
type temp\error-analysis.txt
echo.
echo 详细报告已保存到: build-report.txt
echo 完整日志文件:
echo   - temp\build-backend.log
echo   - temp\build-frontend.log
echo.
pause