@echo off
echo ==========================================
echo     安装UltraThink Git质量检查钩子
echo ==========================================

set "hooksDir=.git\hooks"
set "preCommitHook=%hooksDir%\pre-commit"

if not exist "%hooksDir%" (
    echo ❌ 错误: Git hooks目录不存在
    echo    请确保在Git仓库根目录下运行此脚本
    pause
    exit /b 1
)

echo 📝 创建pre-commit钩子...
(
echo #!/bin/sh
echo # UltraThink代码质量检查 - Git Pre-commit Hook
echo #
echo # 基于重构经验建立的自动化质量门禁
echo # 防止超过500行的Helper类等问题重现
echo #
echo echo "🔍 执行UltraThink代码质量检查..."
echo #
echo # 获取仓库根目录
echo REPO_ROOT=$^(git rev-parse --show-toplevel^)
echo QUALITY_SCRIPT="$REPO_ROOT/scripts/quality-check.ps1"
echo #
echo if [ ! -f "$QUALITY_SCRIPT" ]; then
echo     echo "⚠️  质量检查脚本不存在，跳过检查: $QUALITY_SCRIPT"
echo     exit 0
echo fi
echo #
echo # 执行质量检查
echo if command -v powershell >/dev/null 2^>^&1; then
echo     # Windows PowerShell
echo     powershell -ExecutionPolicy Bypass -File "$QUALITY_SCRIPT"
echo     RESULT=$?
echo elif command -v pwsh >/dev/null 2^>^&1; then
echo     # PowerShell Core
echo     pwsh -File "$QUALITY_SCRIPT"  
echo     RESULT=$?
echo else
echo     echo "⚠️  PowerShell未安装，跳过质量检查"
echo     exit 0
echo fi
echo #
echo if [ $RESULT -ne 0 ]; then
echo     echo ""
echo     echo "❌ 代码质量检查失败！"
echo     echo "   请修复上述问题后重新提交"
echo     echo "   如需跳过检查，使用: git commit --no-verify"
echo     echo ""
echo     exit 1
echo fi
echo #
echo echo "✅ 代码质量检查通过"
echo exit 0
) > "%preCommitHook%"

if exist "%preCommitHook%" (
    echo ✅ Git pre-commit钩子安装成功！
    echo.
    echo 📋 钩子功能:
    echo    • 自动检查Helper类行数限制 ^(500行^)
    echo    • 验证Service/Controller行数
    echo    • 检查AutoMapper使用规范
    echo    • 验证重构架构完整性
    echo.
    echo 💡 使用方法:
    echo    • 正常提交: git commit -m "消息"
    echo    • 跳过检查: git commit --no-verify -m "消息"
    echo.
    echo 🔧 手动测试: scripts\quality-check.bat
) else (
    echo ❌ Git钩子安装失败
    exit /b 1
)

pause