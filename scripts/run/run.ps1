# LYBT 快速执行脚本 (PowerShell版本)
# 用法: .\scripts\run\run.ps1 [command] [args...]
# 示例: .\scripts\run\run.ps1 test
#       .\scripts\run\run.ps1 build

param(
    [string]$Command,
    [string[]]$Arguments
)

# 定义脚本映射
$scriptMap = @{
    # 构建相关
    "build" = @{ Type = "bat"; Path = "scripts\build.bat" }
    "build-webapi" = @{ Type = "bat"; Path = "scripts\build-webapi.bat" }

    # 测试相关
    "test" = @{ Type = "bat"; Path = "scripts\run-tests.bat" }
    "test-clean" = @{ Type = "ps1"; Path = "scripts\clean-test-results.ps1" }
    "test-port" = @{ Type = "ps1"; Path = "scripts\test-port-config.ps1" }

    # 清理相关
    "clean" = @{ Type = "bat"; Path = "scripts\clean-solution.bat" }
    "clean-all" = @{ Type = "ps1"; Path = "scripts\cleanup.ps1" }
    "clean-test" = @{ Type = "ps1"; Path = "scripts\clean-test-results.ps1" }

    # 运行应用
    "webapi" = @{ Type = "ps1"; Path = "scripts\run-webapi.ps1" }
    "desktop" = @{ Type = "bat"; Path = "scripts\run-desktop.bat" }
    "health-check" = @{ Type = "ps1"; Path = "scripts\health-check.ps1" }

    # 数据库管理
    "db-init" = @{ Type = "bat"; Path = "scripts\initialize-db.bat" }
    "db-backup" = @{ Type = "bat"; Path = "scripts\backup-database.bat" }
    "db-restore" = @{ Type = "bat"; Path = "scripts\restore-database.bat" }

    # 开发辅助
    "deps" = @{ Type = "ps1"; Path = "scripts\install-dependencies.ps1" }
    "fix-terms" = @{ Type = "ps1"; Path = "scripts\terminology-fix.ps1" }
}

function Show-Help {
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host " 凌隐宝堂中医诊所管理系统 - 快速执行脚本" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""

    Write-Host "构建命令:" -ForegroundColor Yellow
    Write-Host "  run build [Debug|Release]    - 构建整个解决方案"
    Write-Host "  run build-webapi             - 仅构建WebAPI"
    Write-Host ""

    Write-Host "测试命令:" -ForegroundColor Yellow
    Write-Host "  run test                     - 运行所有测试"
    Write-Host "  run test-clean              - 清理测试结果"
    Write-Host "  run test-port               - 测试端口配置"
    Write-Host ""

    Write-Host "清理命令:" -ForegroundColor Yellow
    Write-Host "  run clean                    - 清理解决方案"
    Write-Host "  run clean-all               - 深度清理"
    Write-Host "  run clean-test              - 清理测试结果"
    Write-Host ""

    Write-Host "应用运行:" -ForegroundColor Yellow
    Write-Host "  run webapi [port]           - 启动WebAPI服务"
    Write-Host "  run desktop                 - 启动桌面应用"
    Write-Host "  run health-check           - 健康检查"
    Write-Host ""

    Write-Host "数据库管理:" -ForegroundColor Yellow
    Write-Host "  run db-init                 - 初始化数据库"
    Write-Host "  run db-backup              - 备份数据库"
    Write-Host "  run db-restore [file]      - 恢复数据库"
    Write-Host ""

    Write-Host "开发辅助:" -ForegroundColor Yellow
    Write-Host "  run deps                    - 安装依赖"
    Write-Host "  run fix-terms              - 修正术语"
    Write-Host ""

    Write-Host "更多脚本请查看 scripts\README.md" -ForegroundColor Gray
}

# 主逻辑
if (-not $Command) {
    Show-Help
    exit 0
}

if ($Command -eq "help" -or $Command -eq "-h" -or $Command -eq "--help") {
    Show-Help
    exit 0
}

# 查找并执行脚本
if ($scriptMap.ContainsKey($Command)) {
    $script = $scriptMap[$Command]
    $scriptPath = Join-Path $PSScriptRoot $script.Path

    if (-not (Test-Path $scriptPath)) {
        Write-Host "[错误] 脚本不存在: $scriptPath" -ForegroundColor Red
        exit 1
    }

    Write-Host "[执行] $scriptPath" -ForegroundColor Green

    if ($script.Type -eq "ps1") {
        & $scriptPath @Arguments
    } elseif ($script.Type -eq "bat") {
        & cmd /c $scriptPath @Arguments
    } elseif ($script.Type -eq "sh") {
        & bash $scriptPath @Arguments
    } elseif ($script.Type -eq "py") {
        & python $scriptPath @Arguments
    }
} else {
    Write-Host "[错误] 未知命令: $Command" -ForegroundColor Red
    Write-Host "使用 '.\scripts\run\run.ps1 help' 查看可用命令" -ForegroundColor Yellow
    exit 1
}
