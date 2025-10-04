# 命令绑定审计脚本 v2
# 用途：检查所有Desktop模块的XAML Command绑定与ViewModel实现的一致性
# Issue: #884

param(
    [string]$RootPath = "D:\source\repos\LYBTZYZS\src\Client\Desktop",
    [string]$OutputFile = "D:\source\repos\LYBTZYZS\docs\reports\command-bindings-audit-$(Get-Date -Format 'yyyy-MM-dd').md"
)

Write-Host ("=" * 80) -ForegroundColor Cyan
Write-Host "命令绑定审计脚本 v2" -ForegroundColor Cyan
Write-Host "扫描路径: $RootPath" -ForegroundColor Cyan
Write-Host ("=" * 80) -ForegroundColor Cyan
Write-Host ""

# 初始化统计
$totalXaml = 0
$totalCommands = 0
$totalMissing = 0
$totalWarnings = 0
$results = @()

# 扫描所有XAML文件
$xamlFiles = Get-ChildItem -Path $RootPath -Recurse -Filter "*.xaml" -ErrorAction SilentlyContinue

foreach ($xamlFile in $xamlFiles) {
    $totalXaml++
    $xamlPath = $xamlFile.FullName
    $xamlName = $xamlFile.Name

    try {
        $xamlContent = Get-Content $xamlPath -Raw -Encoding UTF8

        # 提取所有 Command 绑定 - 改进的正则表达式
        # 匹配: Command="{Binding CommandName}" 或 Command="{Binding DataContext.CommandName, ...}"
        $pattern = 'Command\s*=\s*"\{Binding\s+(?:DataContext\.)?([A-Za-z_][A-Za-z0-9_]*)'
        $matches = [regex]::Matches($xamlContent, $pattern)

        if ($matches.Count -eq 0) {
            continue
        }

        Write-Host "`n=== $xamlName ===" -ForegroundColor Yellow

        # 查找对应的 ViewModel
        $viewModelName = $xamlFile.BaseName -replace 'View$', 'ViewModel'
        $viewModelDir = $xamlFile.DirectoryName -replace 'Views', 'ViewModels'
        $viewModelPath = Join-Path $viewModelDir "$viewModelName.cs"

        if (-not (Test-Path $viewModelPath)) {
            Write-Host "⚠️  未找到 ViewModel: $viewModelName.cs" -ForegroundColor Yellow
            $totalWarnings++

            $results += [PSCustomObject]@{
                View = $xamlName
                ViewModel = "$viewModelName.cs"
                Command = "N/A"
                Status = "⚠️ ViewModel不存在"
                Type = "WARNING"
            }
            continue
        }

        $vmContent = Get-Content $viewModelPath -Raw -Encoding UTF8

        # 检查每个命令
        $commandsInXaml = @()
        foreach ($match in $matches) {
            $commandName = $match.Groups[1].Value.Trim()

            # 跳过重复的命令
            if ($commandsInXaml -contains $commandName) {
                continue
            }
            $commandsInXaml += $commandName
            $totalCommands++

            # 检查 ViewModel 中是否存在该命令
            # 匹配模式 - 支持 new 关键字（如 public new DelegateCommand）
            $cmdPattern = "(?:public|private|protected)\s+(?:new\s+)?(?:ICommand|DelegateCommand(?:<[^>]+>)?)\s+$commandName\s*[={;]"
            $commandExists = $vmContent -match $cmdPattern

            if ($commandExists) {
                Write-Host "  ✅ $commandName" -ForegroundColor Green
                $results += [PSCustomObject]@{
                    View = $xamlName
                    ViewModel = "$viewModelName.cs"
                    Command = $commandName
                    Status = "✅ 存在"
                    Type = "OK"
                }
            } else {
                Write-Host "  ❌ $commandName - 命令不存在" -ForegroundColor Red
                $totalMissing++
                $results += [PSCustomObject]@{
                    View = $xamlName
                    ViewModel = "$viewModelName.cs"
                    Command = $commandName
                    Status = "❌ 缺失"
                    Type = "MISSING"
                }
            }
        }
    }
    catch {
        Write-Host "⚠️  处理文件时出错: $xamlName - $_" -ForegroundColor Yellow
        $totalWarnings++
    }
}

# 生成统计摘要
Write-Host "`n" + ("=" * 80) -ForegroundColor Cyan
Write-Host "审计摘要" -ForegroundColor Cyan
Write-Host ("=" * 80) -ForegroundColor Cyan
Write-Host "扫描的XAML文件数: $totalXaml"
Write-Host "检查的命令总数: $totalCommands"
Write-Host "缺失的命令数: $totalMissing" -ForegroundColor $(if ($totalMissing -gt 0) { "Red" } else { "Green" })
Write-Host "警告数: $totalWarnings" -ForegroundColor $(if ($totalWarnings -gt 0) { "Yellow" } else { "Green" })
Write-Host ""

# 生成Markdown报告
$reportContent = @"
# 命令绑定审计报告 v2

**生成时间**: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
**扫描路径**: ``$RootPath``
**相关Issue**: #884

## 📊 审计摘要

| 指标 | 数值 |
|------|------|
| 扫描的XAML文件数 | $totalXaml |
| 检查的命令总数 | $totalCommands |
| 缺失的命令数 | **$totalMissing** |
| 警告数 | $totalWarnings |

## 🔴 缺失的命令

"@

$missingCommands = $results | Where-Object { $_.Type -eq "MISSING" }
if ($missingCommands.Count -gt 0) {
    $reportContent += "`n| View | ViewModel | 缺失的命令 |`n"
    $reportContent += "|------|-----------|------------|`n"

    foreach ($missing in $missingCommands) {
        $reportContent += "| $($missing.View) | $($missing.ViewModel) | ``$($missing.Command)`` |`n"
    }
} else {
    $reportContent += "`n✅ **未发现缺失的命令绑定！**`n"
}

$reportContent += "`n## ⚠️ 警告`n"
$warnings = $results | Where-Object { $_.Type -eq "WARNING" }
if ($warnings.Count -gt 0) {
    $reportContent += "`n| View | 问题 |`n"
    $reportContent += "|------|------|`n"

    foreach ($warning in $warnings) {
        $reportContent += "| $($warning.View) | $($warning.Status) |`n"
    }
} else {
    $reportContent += "`n✅ **无警告**`n"
}

$reportContent += "`n## ✅ 正常的命令绑定`n`n"
$okCount = ($results | Where-Object { $_.Type -eq 'OK' } | Measure-Object).Count
$reportContent += "<details>`n<summary>点击展开查看所有正常的命令绑定（$okCount 个）</summary>`n`n"
$reportContent += "| View | ViewModel | 命令 |`n"
$reportContent += "|------|-----------|------|`n"

$okCommands = $results | Where-Object { $_.Type -eq "OK" }
foreach ($ok in $okCommands) {
    $reportContent += "| $($ok.View) | $($ok.ViewModel) | ``$($ok.Command)`` |`n"
}

$reportContent += "`n</details>`n"

$reportContent += @"

## 📋 后续行动

"@

if ($totalMissing -gt 0) {
    $reportContent += @"
### 需要修复的模块

"@
    # 按ViewModel分组缺失的命令
    $groupedMissing = $missingCommands | Group-Object -Property ViewModel
    foreach ($group in $groupedMissing) {
        $commandList = ($group.Group | ForEach-Object { "``$($_.Command)``" }) -join ", "
        $reportContent += "- [ ] **$($group.Name)**: 缺失 $($group.Count) 个命令 - $commandList`n"
    }

    $reportContent += "`n### 建议`n"
    $reportContent += "1. 为每个有问题的模块创建独立的修复Issue`n"
    $reportContent += "2. 优先修复P0/P1优先级模块`n"
    $reportContent += "3. 修复后重新运行此脚本验证`n"
} else {
    $reportContent += "✅ **所有命令绑定检查通过！无需修复。**`n"
}

$reportContent += @"

## 🔗 相关资源

- Issue #884: 全面检查所有模块的事件绑定
- 脚本位置: ``scripts/analysis/check-command-bindings.ps1``

---
*此报告由自动化脚本生成*
"@

# 确保输出目录存在
$outputDir = Split-Path $OutputFile -Parent
if (-not (Test-Path $outputDir)) {
    New-Item -Path $outputDir -ItemType Directory -Force | Out-Null
}

# 写入报告
$reportContent | Out-File -FilePath $OutputFile -Encoding UTF8 -Force

Write-Host "📄 报告已生成: $OutputFile" -ForegroundColor Green
Write-Host ""

# 如果有缺失的命令，返回非零退出码
if ($totalMissing -gt 0) {
    Write-Host "⚠️  发现 $totalMissing 个缺失的命令绑定，请查看报告" -ForegroundColor Red
    exit 1
} else {
    Write-Host "✅ 所有命令绑定检查通过！" -ForegroundColor Green
    exit 0
}
