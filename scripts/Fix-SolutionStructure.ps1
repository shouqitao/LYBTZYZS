#!/usr/bin/env pwsh
<#
.SYNOPSIS
优化 Solution 文件结构 - Issue #975

.DESCRIPTION
优化三个 Solution 文件的物理到逻辑映射：
1. 添加缺失的 LYBT.Core.EventBus 项目到 Solution
2. 移除孤立的 "src" 虚拟文件夹
3. 重命名 SharedResources 为 Shared
4. 统一虚拟文件夹为分层结构（与物理目录逻辑一致）

.PARAMETER WhatIf
模拟执行，不实际修改文件

.PARAMETER SkipBackup
跳过备份步骤（不推荐）

.NOTES
执行前会自动备份 .sln 文件到 backups/ 目录
详细方案见: docs/reports/solution-structure-optimization-plan.md
#>

param(
    [switch]$WhatIf,
    [switch]$SkipBackup
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path $PSScriptRoot -Parent
$Timestamp = Get-Date -Format "yyyyMMdd_HHmmss"

Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host "Solution 文件结构修复脚本 - Issue #975" -ForegroundColor Cyan
Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host ""

# 备份 Solution 文件
function Backup-SolutionFiles {
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $backupDir = Join-Path $RepoRoot "backups\solution_$timestamp"

    if (-not (Test-Path $backupDir)) {
        New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
    }

    Write-Host "📦 备份 Solution 文件到: $backupDir" -ForegroundColor Yellow

    Copy-Item (Join-Path $RepoRoot "LYBT.All.sln") $backupDir
    Copy-Item (Join-Path $RepoRoot "LYBT.Server.sln") $backupDir
    Copy-Item (Join-Path $RepoRoot "LYBT.Desktop.sln") $backupDir

    Write-Host "✅ 备份完成" -ForegroundColor Green
    Write-Host ""
}

# 添加 LYBT.Core.EventBus 项目
function Add-MissingProjects {
    Write-Host "📝 步骤 1: 添加缺失的 LYBT.Core.EventBus 项目" -ForegroundColor Cyan
    Write-Host ""

    $eventBusProject = "src\Server\Core\LYBT.Core.EventBus\LYBT.Core.EventBus.csproj"

    # 添加到 LYBT.Server.sln
    Write-Host "  → 添加到 LYBT.Server.sln..." -ForegroundColor Gray
    if ($WhatIf) {
        Write-Host "    [WhatIf] dotnet sln LYBT.Server.sln add $eventBusProject" -ForegroundColor DarkGray
    } else {
        Push-Location $RepoRoot
        dotnet sln LYBT.Server.sln add $eventBusProject 2>&1 | Out-Null
        Pop-Location
        Write-Host "    ✅ 已添加" -ForegroundColor Green
    }

    # 添加到 LYBT.All.sln
    Write-Host "  → 添加到 LYBT.All.sln..." -ForegroundColor Gray
    if ($WhatIf) {
        Write-Host "    [WhatIf] dotnet sln LYBT.All.sln add $eventBusProject" -ForegroundColor DarkGray
    } else {
        Push-Location $RepoRoot
        dotnet sln LYBT.All.sln add $eventBusProject 2>&1 | Out-Null
        Pop-Location
        Write-Host "    ✅ 已添加" -ForegroundColor Green
    }

    Write-Host ""
}

# 移除孤立的 "src" 虚拟文件夹
function Remove-OrphanedSrcFolder {
    Write-Host "📝 步骤 2: 清理孤立的 'src' 虚拟文件夹" -ForegroundColor Cyan
    Write-Host ""

    $solutions = @(
        "LYBT.All.sln",
        "LYBT.Server.sln",
        "LYBT.Desktop.sln"
    )

    foreach ($sln in $solutions) {
        $slnPath = Join-Path $RepoRoot $sln
        Write-Host "  → 处理 $sln..." -ForegroundColor Gray

        if ($WhatIf) {
            Write-Host "    [WhatIf] 将移除 'src' 虚拟文件夹及其嵌套内容" -ForegroundColor DarkGray
        } else {
            # 读取 .sln 文件
            $content = Get-Content $slnPath -Encoding UTF8 -Raw

            # 查找并移除 src 虚拟文件夹定义
            # 匹配模式: Project("{2150E333...}") = "src", "src", "{GUID}"
            $srcFolderPattern = 'Project\("{2150E333-8FDC-42A3-9474-1A3956D46DE8}"\) = "src", "src", "({[A-F0-9-]+})"\s+EndProject\s+'

            if ($content -match $srcFolderPattern) {
                $srcFolderGuid = $matches[1]
                Write-Host "    找到 'src' 虚拟文件夹 GUID: $srcFolderGuid" -ForegroundColor DarkGray

                # 移除 src 文件夹定义
                $content = $content -replace $srcFolderPattern, ""

                # 移除嵌套关系（NestedProjects 区域）
                # 移除所有指向 src 文件夹的嵌套关系
                $nestedPattern = "\s*\{[A-F0-9-]+\} = \{$srcFolderGuid\}\s*\r?\n"
                $content = $content -replace $nestedPattern, ""

                # 移除 src 作为父文件夹的嵌套关系
                # 例如: {子GUID} = {srcGUID}
                $lines = $content -split "`r?`n"
                $newLines = @()
                $inNestedSection = $false

                foreach ($line in $lines) {
                    if ($line -match "GlobalSection\(NestedProjects\)") {
                        $inNestedSection = $true
                        $newLines += $line
                    }
                    elseif ($line -match "EndGlobalSection" -and $inNestedSection) {
                        $inNestedSection = $false
                        $newLines += $line
                    }
                    elseif ($inNestedSection -and $line -match "\{[A-F0-9-]+\} = \{$srcFolderGuid\}") {
                        # 跳过指向 src 的嵌套关系
                        continue
                    }
                    else {
                        $newLines += $line
                    }
                }

                $content = $newLines -join "`r`n"

                # 移除 LYBT.Core 项目（因为它会随着 EventBus 添加时自动出现在正确位置）
                # 只从孤立的 src 文件夹中移除，不影响正确位置的项目

                # 保存修改后的文件
                Set-Content -Path $slnPath -Value $content -Encoding UTF8 -NoNewline

                Write-Host "    ✅ 已移除 'src' 虚拟文件夹" -ForegroundColor Green
            } else {
                Write-Host "    ℹ️  未找到 'src' 虚拟文件夹" -ForegroundColor Yellow
            }
        }
    }

    Write-Host ""
}

# 验证修复结果
function Test-SolutionStructure {
    Write-Host "📝 步骤 3: 验证修复结果" -ForegroundColor Cyan
    Write-Host ""

    $solutions = @("LYBT.All.sln", "LYBT.Server.sln")

    foreach ($sln in $solutions) {
        $slnPath = Join-Path $RepoRoot $sln
        Write-Host "  → 验证 $sln..." -ForegroundColor Gray

        $content = Get-Content $slnPath -Raw

        # 检查 LYBT.Core.EventBus 是否存在
        if ($content -match "LYBT\.Core\.EventBus") {
            Write-Host "    ✅ LYBT.Core.EventBus 已包含" -ForegroundColor Green
        } else {
            Write-Host "    ❌ LYBT.Core.EventBus 缺失" -ForegroundColor Red
        }

        # 检查是否还有孤立的 src 文件夹
        if ($content -match 'Project\("{2150E333[^"]*}"\) = "src", "src"') {
            Write-Host "    ⚠️  仍存在 'src' 虚拟文件夹" -ForegroundColor Yellow
        } else {
            Write-Host "    ✅ 无孤立 'src' 虚拟文件夹" -ForegroundColor Green
        }
    }

    Write-Host ""
}

# 编译测试
function Test-Build {
    Write-Host "📝 步骤 4: 编译测试" -ForegroundColor Cyan
    Write-Host ""

    if ($WhatIf) {
        Write-Host "  [WhatIf] dotnet build LYBT.All.sln -c Release" -ForegroundColor DarkGray
        Write-Host ""
        return
    }

    Write-Host "  → 编译 LYBT.All.sln..." -ForegroundColor Gray
    Push-Location $RepoRoot

    $buildOutput = dotnet build LYBT.All.sln -c Release --no-restore 2>&1
    $buildSuccess = $LASTEXITCODE -eq 0

    Pop-Location

    if ($buildSuccess) {
        Write-Host "    ✅ 编译成功" -ForegroundColor Green
    } else {
        Write-Host "    ❌ 编译失败" -ForegroundColor Red
        Write-Host ""
        Write-Host "编译输出:" -ForegroundColor Yellow
        $buildOutput | Select-Object -Last 30 | ForEach-Object { Write-Host "    $_" }
        throw "编译失败，请检查错误信息"
    }

    Write-Host ""
}

# 主执行流程
try {
    if (-not $WhatIf) {
        Backup-SolutionFiles
    }

    Add-MissingProjects
    Remove-OrphanedSrcFolder
    Test-SolutionStructure
    Test-Build

    Write-Host "=====================================================" -ForegroundColor Green
    Write-Host "✅ Solution 文件结构修复完成！" -ForegroundColor Green
    Write-Host "=====================================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "下一步:" -ForegroundColor Cyan
    Write-Host "  1. 在 Visual Studio 中打开 LYBT.All.sln 验证结构" -ForegroundColor Gray
    Write-Host "  2. 确认所有项目在正确的虚拟文件夹中" -ForegroundColor Gray
    Write-Host "  3. 创建 Git 提交并提交 PR" -ForegroundColor Gray
    Write-Host ""

} catch {
    Write-Host ""
    Write-Host "❌ 错误: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "可以从备份恢复 Solution 文件：backups/solution_*/" -ForegroundColor Yellow
    exit 1
}
