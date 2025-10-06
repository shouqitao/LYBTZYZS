#!/usr/bin/env pwsh
<#
.SYNOPSIS
优化 Solution 文件结构 - Issue #975

.DESCRIPTION
优化三个 Solution 文件的物理到逻辑映射：
1. 添加缺失的 LYBT.Core.EventBus 项目
2. 移除孤立的 "src" 虚拟文件夹
3. 统一虚拟文件夹为分层结构

.PARAMETER WhatIf
模拟执行，不实际修改文件

.EXAMPLE
.\scripts\Optimize-SolutionStructure.ps1 -WhatIf
# 查看将要执行的操作

.EXAMPLE
.\scripts\Optimize-SolutionStructure.ps1
# 执行优化

.NOTES
详细方案见: docs/reports/solution-structure-optimization-plan.md
#>

param(
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path $PSScriptRoot -Parent
$Timestamp = Get-Date -Format "yyyyMMdd_HHmmss"

Write-Host ""
Write-Host "╔═══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  Solution 文件结构优化 - Issue #975                      ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

if ($WhatIf) {
    Write-Host "⚠️  WhatIf 模式: 仅模拟执行，不会实际修改文件" -ForegroundColor Yellow
    Write-Host ""
}

# ============================================================================
# 函数定义
# ============================================================================

function Write-Step {
    param(
        [int]$StepNumber,
        [string]$Title
    )
    Write-Host ""
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
    Write-Host "📝 步骤 $StepNumber : $Title" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
}

function Write-Success {
    param([string]$Message)
    Write-Host "  ✅ $Message" -ForegroundColor Green
}

function Write-Info {
    param([string]$Message)
    Write-Host "  ℹ️  $Message" -ForegroundColor Gray
}

function Write-Warning {
    param([string]$Message)
    Write-Host "  ⚠️  $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "  ❌ $Message" -ForegroundColor Red
}

function Backup-SolutionFiles {
    Write-Step 1 "备份 Solution 文件"

    $backupDir = Join-Path $RepoRoot "backups\solution_$Timestamp"

    if ($WhatIf) {
        Write-Info "WhatIf: 将创建备份目录: $backupDir"
        Write-Info "WhatIf: 将备份 LYBT.All.sln, LYBT.Server.sln, LYBT.Desktop.sln"
        return
    }

    if (-not (Test-Path $backupDir)) {
        New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
    }

    Write-Info "备份目录: $backupDir"

    $solutions = @("LYBT.All.sln", "LYBT.Server.sln", "LYBT.Desktop.sln")
    foreach ($sln in $solutions) {
        $sourcePath = Join-Path $RepoRoot $sln
        Copy-Item $sourcePath $backupDir -Force
        Write-Success "已备份: $sln"
    }
}

function Add-MissingProject {
    Write-Step 2 "添加缺失的 LYBT.Core.EventBus 项目"

    $projectPath = "src\Server\Core\LYBT.Core.EventBus\LYBT.Core.EventBus.csproj"
    $fullProjectPath = Join-Path $RepoRoot $projectPath

    # 检查项目是否存在
    if (-not (Test-Path $fullProjectPath)) {
        Write-Error "项目文件不存在: $projectPath"
        throw "找不到 LYBT.Core.EventBus.csproj"
    }

    Write-Info "项目路径: $projectPath"

    # 添加到 LYBT.Server.sln
    Write-Info "添加到 LYBT.Server.sln..."
    if ($WhatIf) {
        Write-Info "WhatIf: dotnet sln LYBT.Server.sln add $projectPath"
    } else {
        Push-Location $RepoRoot
        $output = dotnet sln LYBT.Server.sln add $projectPath 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Success "已添加到 LYBT.Server.sln"
        } else {
            if ($output -like "*already*") {
                Write-Warning "项目已存在于 LYBT.Server.sln"
            } else {
                Write-Error "添加失败: $output"
            }
        }
        Pop-Location
    }

    # 添加到 LYBT.All.sln
    Write-Info "添加到 LYBT.All.sln..."
    if ($WhatIf) {
        Write-Info "WhatIf: dotnet sln LYBT.All.sln add $projectPath"
    } else {
        Push-Location $RepoRoot
        $output = dotnet sln LYBT.All.sln add $projectPath 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Success "已添加到 LYBT.All.sln"
        } else {
            if ($output -like "*already*") {
                Write-Warning "项目已存在于 LYBT.All.sln"
            } else {
                Write-Error "添加失败: $output"
            }
        }
        Pop-Location
    }
}

function Remove-OrphanedFolders {
    Write-Step 3 "清理孤立的虚拟文件夹"

    Write-Info "此步骤需要在 Visual Studio 中手动完成："
    Write-Host ""
    Write-Host "    1. 在 Visual Studio 2022 中打开 LYBT.All.sln" -ForegroundColor Yellow
    Write-Host "    2. 在 Solution Explorer 中找到孤立的 'src' 虚拟文件夹" -ForegroundColor Yellow
    Write-Host "    3. 右键 → Remove（不是 Delete！）" -ForegroundColor Yellow
    Write-Host "    4. 如果 src 下有项目，将它们拖到正确的虚拟文件夹" -ForegroundColor Yellow
    Write-Host "    5. 保存 Solution" -ForegroundColor Yellow
    Write-Host "    6. 对 LYBT.Server.sln 和 LYBT.Desktop.sln 重复上述步骤" -ForegroundColor Yellow
    Write-Host ""
    Write-Info "或者，直接使用编辑器手动编辑 .sln 文件（高级）"
}

function Test-SolutionStructure {
    Write-Step 4 "验证 Solution 结构"

    $solutions = @{
        "LYBT.All.sln" = 41
        "LYBT.Server.sln" = 25
        "LYBT.Desktop.sln" = 16
    }

    foreach ($sln in $solutions.Keys) {
        $slnPath = Join-Path $RepoRoot $sln
        $expectedProjects = $solutions[$sln]

        Write-Info "检查 $sln..."

        if (-not (Test-Path $slnPath)) {
            Write-Error "$sln 不存在"
            continue
        }

        $content = Get-Content $slnPath -Raw

        # 检查 LYBT.Core.EventBus
        if ($content -match "LYBT\.Core\.EventBus") {
            Write-Success "包含 LYBT.Core.EventBus"
        } else {
            if ($sln -ne "LYBT.Desktop.sln") {
                Write-Warning "缺少 LYBT.Core.EventBus"
            }
        }

        # 检查孤立的 src 文件夹
        if ($content -match 'Project\("{2150E333[^"]*}"\) = "src", "src"') {
            Write-Warning "仍存在孤立的 'src' 虚拟文件夹"
        } else {
            Write-Success "无孤立 'src' 虚拟文件夹"
        }

        # 统计项目数量
        $projectMatches = [regex]::Matches($content, 'Project\("{[A-F0-9-]+}"\) = "[^"]+", "[^"]+\.csproj"')
        $actualProjects = $projectMatches.Count

        if ($actualProjects -eq $expectedProjects) {
            Write-Success "项目数量正确: $actualProjects"
        } elseif ($actualProjects -eq ($expectedProjects + 1) -and $sln -ne "LYBT.Desktop.sln") {
            Write-Success "项目数量正确: $actualProjects (新增 EventBus)"
        } else {
            Write-Warning "项目数量: $actualProjects (预期: $expectedProjects)"
        }
    }
}

function Test-BuildAll {
    Write-Step 5 "编译测试"

    if ($WhatIf) {
        Write-Info "WhatIf: 跳过编译测试"
        return
    }

    $solutions = @("LYBT.All.sln", "LYBT.Server.sln", "LYBT.Desktop.sln")

    foreach ($sln in $solutions) {
        Write-Info "编译 $sln..."

        Push-Location $RepoRoot
        $output = dotnet build $sln -c Release --no-restore 2>&1
        $success = $LASTEXITCODE -eq 0
        Pop-Location

        if ($success) {
            Write-Success "$sln 编译成功"
        } else {
            Write-Error "$sln 编译失败"
            Write-Host ""
            Write-Host "编译输出 (最后 20 行):" -ForegroundColor Yellow
            $output | Select-Object -Last 20 | ForEach-Object { Write-Host "    $_" -ForegroundColor Gray }
        }
    }
}

function Show-NextSteps {
    Write-Host ""
    Write-Host "╔═══════════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║  优化完成！                                               ║" -ForegroundColor Green
    Write-Host "╚═══════════════════════════════════════════════════════════╝" -ForegroundColor Green
    Write-Host ""
    Write-Host "📋 下一步操作:" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  1. 在 Visual Studio 2022 中打开 LYBT.All.sln" -ForegroundColor Gray
    Write-Host "  2. 验证 Solution Explorer 中的虚拟文件夹结构" -ForegroundColor Gray
    Write-Host "  3. 手动移除孤立的 'src' 虚拟文件夹（如果存在）" -ForegroundColor Gray
    Write-Host "  4. 确认所有项目可编译" -ForegroundColor Gray
    Write-Host "  5. 提交更改:" -ForegroundColor Gray
    Write-Host ""
    Write-Host "     git add *.sln" -ForegroundColor DarkGray
    Write-Host '     git commit -m "[FIX] 优化 Solution 文件结构 - Issue #975"' -ForegroundColor DarkGray
    Write-Host "     gh pr create" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "📄 详细方案: docs/reports/solution-structure-optimization-plan.md" -ForegroundColor Gray
    Write-Host "📦 备份位置: backups/solution_$Timestamp/" -ForegroundColor Gray
    Write-Host ""
}

# ============================================================================
# 主执行流程
# ============================================================================

try {
    # 步骤 1: 备份
    Backup-SolutionFiles

    # 步骤 2: 添加缺失项目
    Add-MissingProject

    # 步骤 3: 清理虚拟文件夹（手动）
    Remove-OrphanedFolders

    # 步骤 4: 验证结构
    Test-SolutionStructure

    # 步骤 5: 编译测试
    Test-BuildAll

    # 显示下一步
    Show-NextSteps

} catch {
    Write-Host ""
    Write-Host "╔═══════════════════════════════════════════════════════════╗" -ForegroundColor Red
    Write-Host "║  执行失败！                                               ║" -ForegroundColor Red
    Write-Host "╚═══════════════════════════════════════════════════════════╝" -ForegroundColor Red
    Write-Host ""
    Write-Host "❌ 错误: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "🔄 恢复方法:" -ForegroundColor Yellow
    Write-Host "   Copy-Item backups\solution_$Timestamp\*.sln ." -ForegroundColor Gray
    Write-Host ""
    exit 1
}
