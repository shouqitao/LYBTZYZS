@echo off
echo ==========================================
echo        UltraThink代码质量门禁检查
echo ==========================================

powershell -File "%~dp0quality-check.ps1"
if %errorlevel% neq 0 (
    echo.
    echo ❌ 质量检查失败！请修复上述问题后重试。
    pause
    exit /b 1
)

echo.
echo ✅ 所有质量检查通过！
echo.
pause