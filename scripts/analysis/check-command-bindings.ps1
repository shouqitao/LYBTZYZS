#!/usr/bin/env pwsh
<#
.SYNOPSIS
    检查所有 WPF 模块的 XAML Command 绑定与 ViewModel 的一致性

.DESCRIPTION
    扫描所有 XAML 文件，提取 Command 绑定，对比 ViewModel 中的 ICommand 属性
    生成详细的检查报告，标记缺失或不匹配的绑定

.PARAMETER OutputPath
    输出报告路径（默认：docs/reports/command-bindings-audit-{date}.md）

.EXAMPLE
    .\check-command-bindings.ps1
    .\check-command-bindings.ps1 -OutputPath "reports/bindings.md"

.NOTES
    Author: Claude Code
    Date: 2025-10-04
    Related Issue: #884
#>

[CmdletBinding()]
param(
    [string]$OutputPath = ""
)

# 设置错误处理
$ErrorActionPreference = "Stop"

# 获取项目根目录
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent (Split-Path -Parent $scriptPath)
$desktopPath = Join-Path $projectRoot "src\Client\Desktop"

# 设置默认输出路径
if ([string]::IsNullOrEmpty($OutputPath)) {
    $date = Get-Date -Format "yyyy-MM-dd"
    $OutputPath = Join-Path $projectRoot "docs\reports\command-bindings-audit-$date.md"
}

# 确保输出目录存在
$outputDir = Split-Path -Parent $OutputPath
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

Write-Host "=== 开始检查 XAML Command 绑定 ===" -ForegroundColor Cyan
Write-Host "Desktop 路径: $desktopPath" -ForegroundColor Gray
Write-Host "输出报告: $OutputPath" -ForegroundColor Gray
Write-Host ""

# 结果统计
$stats = @{
    TotalViews = 0
    TotalBindings = 0
    MissingBindings = 0
    ExistingBindings = 0
    ViewsWithErrors = 0
}

# 详细结果
$results = @()

# 扫描所有 XAML 文件
$xamlFiles = Get-ChildItem -Path $desktopPath -Recurse -Filter "*.xaml" |
    Where-Object { $_.DirectoryName -match "\\Views$" }

Write-Host "找到 $($xamlFiles.Count) 个 View 文件" -ForegroundColor Yellow
Write-Host ""

foreach ($xaml in $xamlFiles) {
    $stats.TotalViews++

    $viewName = $xaml.BaseName
    $viewPath = $xaml.FullName
    $content = Get-Content $viewPath -Raw

    Write-Host "检查: $viewName" -ForegroundColor Cyan

    # 提取所有 Command 绑定
    $commandPattern = 'Command="{Binding\s+([^},"]+)'
    $matches = [regex]::Matches($content, $commandPattern)

    if ($matches.Count -eq 0) {
        Write-Host "  ℹ️  无命令绑定" -ForegroundColor Gray
        continue
    }

    # 查找对应的 ViewModel
    $viewModelName = $viewName -replace 'View$', 'ViewModel'
    $viewModelDir = $xaml.DirectoryName -replace '\\Views$', '\ViewModels'
    $viewModelPath = Join-Path $viewModelDir "$viewModelName.cs"

    $viewResult = @{
        ViewName = $viewName
        ViewPath = $viewPath
        ViewModelName = $viewModelName
        ViewModelPath = $viewModelPath
        ViewModelExists = $false
        Bindings = @()
        HasErrors = $false
    }

    if (Test-Path $viewModelPath) {
        $viewResult.ViewModelExists = $true
        $vmContent = Get-Content $viewModelPath -Raw

        Write-Host "  ViewModel: $viewModelName" -ForegroundColor Gray

        foreach ($match in $matches) {
            $stats.TotalBindings++

            $commandName = $match.Groups[1].Value.Trim()

            # 检查 ViewModel 中是否存在该命令
            # 匹配模式：
            # 1. public ICommand CommandName { get; }
            # 2. public DelegateCommand CommandName => ...
            # 3. public DelegateCommand<T> CommandName => ...

            $exists = $false

            # 模式 1: ICommand 属性
            if ($vmContent -match "(?:public|internal|protected)\s+ICommand\s+$commandName\s*[{;]") {
                $exists = $true
            }
            # 模式 2: DelegateCommand 属性
            elseif ($vmContent -match "(?:public|internal|protected)\s+DelegateCommand(?:<[^>]+>)?\s+$commandName\s*(?:=>|{|;)") {
                $exists = $true
            }
            # 模式 3: 字段形式 (private DelegateCommand _command)
            elseif ($vmContent -match "(?:private|protected)\s+DelegateCommand(?:<[^>]+>)?\s+_?$commandName") {
                $exists = $true
            }

            if ($exists) {
                $stats.ExistingBindings++
                Write-Host "    ✅ $commandName" -ForegroundColor Green
            } else {
                $stats.MissingBindings++
                $viewResult.HasErrors = $true
                Write-Host "    ❌ 缺失: $commandName" -ForegroundColor Red
            }

            $viewResult.Bindings += @{
                CommandName = $commandName
                Exists = $exists
                Line = $match.Index
            }
        }
    } else {
        Write-Host "  ⚠️  未找到 ViewModel: $viewModelPath" -ForegroundColor Yellow
        $viewResult.HasErrors = $true

        foreach ($match in $matches) {
            $stats.TotalBindings++
            $stats.MissingBindings++

            $commandName = $match.Groups[1].Value.Trim()
            Write-Host "    ⚠️  $commandName (ViewModel 不存在)" -ForegroundColor Yellow

            $viewResult.Bindings += @{
                CommandName = $commandName
                Exists = $false
                Line = $match.Index
            }
        }
    }

    if ($viewResult.HasErrors) {
        $stats.ViewsWithErrors++
    }

    $results += $viewResult
    Write-Host ""
}

# 生成 Markdown 报告
$reportContent = @"
# XAML Command 绑定检查报告

**生成时间**: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
**相关 Issue**: #884
**检查范围**: Desktop 所有模块

## 概述

| 指标 | 数量 |
|------|------|
| 总 View 数 | $($stats.TotalViews) |
| 总绑定数 | $($stats.TotalBindings) |
| ✅ 正常绑定 | $($stats.ExistingBindings) |
| ❌ 缺失绑定 | $($stats.MissingBindings) |
| ⚠️ 有问题的 View | $($stats.ViewsWithErrors) |

## 检查结果

"@

# 按模块分组
$moduleGroups = $results | Group-Object {
    $_.ViewPath -replace '.*\\Desktop\\', '' -replace '\\.*', ''
}

foreach ($group in $moduleGroups | Sort-Object Name) {
    $moduleName = $group.Name
    $reportContent += "`n### 模块: $moduleName`n`n"

    foreach ($view in $group.Group | Sort-Object ViewName) {
        $icon = if ($view.HasErrors) { "❌" } else { "✅" }
        $reportContent += "#### $icon $($view.ViewName)`n`n"

        if (-not $view.ViewModelExists) {
            $reportContent += "**⚠️ ViewModel 不存在**: ``$($view.ViewModelPath)```n`n"
        }

        if ($view.Bindings.Count -eq 0) {
            $reportContent += "_无命令绑定_`n`n"
            continue
        }

        $reportContent += "| 命令 | 状态 |`n"
        $reportContent += "|------|------|`n"

        foreach ($binding in $view.Bindings | Sort-Object CommandName) {
            $status = if ($binding.Exists) { "✅ 存在" } else { "❌ 缺失" }
            $reportContent += "| ``$($binding.CommandName)`` | $status |`n"
        }

        $reportContent += "`n"
    }
}

# 添加需要修复的问题列表
if ($stats.ViewsWithErrors -gt 0) {
    $reportContent += "`n## 需要修复的问题`n`n"

    foreach ($view in $results | Where-Object { $_.HasErrors } | Sort-Object ViewName) {
        $missingCommands = $view.Bindings | Where-Object { -not $_.Exists } | Select-Object -ExpandProperty CommandName

        if ($missingCommands.Count -gt 0) {
            $reportContent += "### $($view.ViewName)`n`n"
            $reportContent += "**ViewModel**: ``$($view.ViewModelName)```n`n"
            $reportContent += "**缺失命令**:`n`n"

            foreach ($cmd in $missingCommands | Sort-Object) {
                $reportContent += "- [ ] ``$cmd```n"
            }

            $reportContent += "`n"
        }
    }
}

# 添加结论
$reportContent += "`n## 结论`n`n"

if ($stats.MissingBindings -eq 0) {
    $reportContent += "✅ **所有命令绑定均正常，无需修复。**`n"
} else {
    $reportContent += "❌ **发现 $($stats.MissingBindings) 个缺失的命令绑定，需要立即修复。**`n`n"
    $reportContent += "建议为每个有问题的模块创建独立的修复 Issue。`n"
}

$reportContent += "`n## 下一步行动`n`n"

if ($stats.ViewsWithErrors -gt 0) {
    $reportContent += "1. 为每个有问题的模块创建修复 Issue`n"
    $reportContent += "2. 实现缺失的命令`n"
    $reportContent += "3. 手动测试所有修复的绑定`n"
    $reportContent += "4. 回归测试确保无副作用`n"
} else {
    $reportContent += "1. 手动启动应用验证所有功能`n"
    $reportContent += "2. 点击所有按钮确保无熔断器异常`n"
    $reportContent += "3. 关闭 Issue #884`n"
}

$reportContent += "`n---`n"
$reportContent += "*此报告由自动化脚本生成：``scripts/analysis/check-command-bindings.ps1``*`n"

# 保存报告
$reportContent | Out-File -FilePath $OutputPath -Encoding UTF8

# 打印摘要
Write-Host "=== 检查完成 ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "总结:" -ForegroundColor Yellow
Write-Host "  总 View 数: $($stats.TotalViews)"
Write-Host "  总绑定数: $($stats.TotalBindings)"
Write-Host "  ✅ 正常绑定: $($stats.ExistingBindings)" -ForegroundColor Green
Write-Host "  ❌ 缺失绑定: $($stats.MissingBindings)" -ForegroundColor Red
Write-Host "  ⚠️ 有问题的 View: $($stats.ViewsWithErrors)" -ForegroundColor Yellow
Write-Host ""
Write-Host "报告已保存: $OutputPath" -ForegroundColor Green

# 返回退出代码
if ($stats.MissingBindings -gt 0) {
    exit 1
} else {
    exit 0
}
