@echo off
REM P3本地覆盖率快速验证脚本
REM 验证是否达到70%覆盖率硬门槛

echo 🎯 P3本地覆盖率快速验证
echo.

REM 检查PowerShell是否可用
powershell -Command "Get-Host" >nul 2>&1
if errorlevel 1 (
    echo ❌ PowerShell不可用
    pause
    exit /b 1
)

REM 执行PowerShell覆盖率脚本
powershell -ExecutionPolicy Bypass -File "%~dp0test-coverage-local.ps1" %*

REM 保持窗口打开以查看结果
if "%1"=="-auto" goto :end
echo.
pause

:end