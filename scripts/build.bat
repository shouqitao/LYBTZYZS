@echo off
REM ======================================
REM 凌隐宝堂中医诊所系统 - 编译管理器
REM 统一的编译工具入口
REM ======================================
setlocal enabledelayedexpansion

:menu
cls
color 0E
echo ╔══════════════════════════════════════════════════════════╗
echo ║     凌隐宝堂中医诊所系统 - 编译管理器 v1.0              ║
echo ╠══════════════════════════════════════════════════════════╣
echo ║                                                          ║
echo ║  【快速操作】                                            ║
echo ║  1. 快速编译检查 (最常用)                                ║
echo ║  2. 编译并显示错误                                       ║
echo ║                                                          ║
echo ║  【深度分析】                                            ║
echo ║  3. 详细错误分析                                         ║
echo ║  4. 生成编译报告                                         ║
echo ║                                                          ║
echo ║  【自动修复】                                            ║
echo ║  5. 修复常见错误                                         ║
echo ║  6. 修复编码问题                                         ║
echo ║                                                          ║
echo ║  【维护操作】                                            ║
echo ║  7. 清理并重建                                           ║
echo ║  8. 更新依赖包                                           ║
echo ║                                                          ║
echo ║  0. 退出                                                 ║
echo ║                                                          ║
echo ╚══════════════════════════════════════════════════════════╝
echo.
set /p choice="请选择操作 [0-8]: "

if "!choice!"=="1" goto :quick_check
if "!choice!"=="2" goto :build_with_errors
if "!choice!"=="3" goto :analyze
if "!choice!"=="4" goto :report
if "!choice!"=="5" goto :auto_fix
if "!choice!"=="6" goto :fix_encoding
if "!choice!"=="7" goto :clean_rebuild
if "!choice!"=="8" goto :update_packages
if "!choice!"=="0" goto :exit
goto :menu

:quick_check
cls
echo ========================================
echo 执行快速编译检查...
echo ========================================
echo.
echo 提示：默认编译前端解决方案
echo.

REM 快速编译前端（最常用）
dotnet build LYBT.Desktop.sln --no-incremental --verbosity minimal 2>&1 | findstr /R /C:"error CS" /C:"error MSB" /C:"失败" /C:"Failed"

echo.
echo ========================================
for /f %%a in ('dotnet build LYBT.Desktop.sln --no-incremental --verbosity minimal 2^>&1 ^| findstr /R /C:"error" ^| find /c "error"') do set error_count=%%a
echo 错误总数: !error_count!
echo ========================================
echo.
pause
goto :menu

:build_with_errors
cls
echo ========================================
echo 完整编译并显示所有错误...
echo ========================================
echo.

REM 编译后端
echo [后端编译]
dotnet build LYBT.Backend.sln --verbosity quiet
echo.

REM 编译前端
echo [前端编译]
dotnet build LYBT.Desktop.sln --verbosity quiet
echo.

pause
goto :menu

:analyze
cls
echo ========================================
echo 执行深度错误分析...
echo ========================================
call scripts\build-analyze.bat
pause
goto :menu

:report
cls
echo ========================================
echo 生成详细编译报告...
echo ========================================
echo.

REM 生成时间戳
for /f "tokens=2-4 delims=/ " %%a in ('date /t') do set mydate=%%c%%a%%b
for /f "tokens=1-2 delims=: " %%a in ('time /t') do set mytime=%%a%%b
set timestamp=!mydate!_!mytime: =!

REM 创建报告
echo 凌隐宝堂系统编译报告 > "build-report-!timestamp!.txt"
echo 生成时间: %date% %time% >> "build-report-!timestamp!.txt"
echo ======================================== >> "build-report-!timestamp!.txt"
echo. >> "build-report-!timestamp!.txt"

echo 后端编译结果： >> "build-report-!timestamp!.txt"
dotnet build LYBT.Backend.sln --verbosity normal >> "build-report-!timestamp!.txt" 2>&1
echo. >> "build-report-!timestamp!.txt"

echo 前端编译结果： >> "build-report-!timestamp!.txt"
dotnet build LYBT.Desktop.sln --verbosity normal >> "build-report-!timestamp!.txt" 2>&1

echo.
echo 报告已生成: build-report-!timestamp!.txt
pause
goto :menu

:auto_fix
cls
echo ========================================
echo 自动修复常见错误...
echo ========================================
call scripts\quick-fix.bat
pause
goto :menu

:fix_encoding
cls
echo ========================================
echo 修复文件编码问题...
echo ========================================
echo.

REM 使用PowerShell修复编码
powershell -ExecutionPolicy Bypass -Command "Get-ChildItem -Path 'src' -Include *.cs,*.xaml -Recurse | ForEach-Object { $content = Get-Content $_.FullName -Raw -Encoding UTF8; Set-Content -Path $_.FullName -Value $content -Encoding UTF8 }"

echo ✅ 编码修复完成
pause
goto :menu

:clean_rebuild
cls
echo ========================================
echo 清理并重建项目...
echo ========================================
echo.

echo 步骤 1/4: 清理bin和obj...
for /d /r "src" %%d in (bin obj) do (
    if exist "%%d" rd /s /q "%%d" 2>nul
)

echo 步骤 2/4: 清理包缓存...
dotnet nuget locals all --clear

echo 步骤 3/4: 还原包...
dotnet restore LYBT.Backend.sln
dotnet restore LYBT.Desktop.sln

echo 步骤 4/4: 重新编译...
dotnet build LYBT.Backend.sln
dotnet build LYBT.Desktop.sln

echo.
echo ✅ 清理并重建完成
pause
goto :menu

:update_packages
cls
echo ========================================
echo 更新NuGet包...
echo ========================================
echo.

dotnet restore LYBT.Backend.sln --force-evaluate
dotnet restore LYBT.Desktop.sln --force-evaluate

echo.
echo ✅ 包更新完成
pause
goto :menu

:exit
echo.
echo 感谢使用编译管理器！
timeout /t 2 >nul
exit /b 0