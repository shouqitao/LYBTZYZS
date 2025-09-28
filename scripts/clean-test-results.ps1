# 清理测试结果脚本
# 用于清理所有测试产生的临时文件和结果

param(
    [switch]$WhatIf = $false  # 仅显示将要删除的内容，不实际删除
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " 测试结果清理工具" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

if ($WhatIf) {
    Write-Host "模拟模式：仅显示将要删除的内容" -ForegroundColor Yellow
}

# 要清理的目录列表
$directoriesToClean = @(
    "TestResults",           # 根目录（如果存在）
    "BIN\TestResults",       # BIN目录
    "tests\TestResults",     # 新的统一测试结果目录
    "tests\Coverage",        # 覆盖率报告
    "scripts\TestResults"    # 脚本目录
)

# 要查找并清理的模式
$patternsToClean = @{
    "测试结果文件" = @("*.trx", "*.coverage", "*.coveragexml", "*.xml")
    "临时测试文件" = @("*.tmp", "*.temp", "*.log")
}

$totalDeleted = 0

# 清理指定目录
foreach ($dir in $directoriesToClean) {
    if (Test-Path $dir) {
        $items = Get-ChildItem $dir -Recurse 2>$null | Measure-Object
        if ($items.Count -gt 0) {
            Write-Host "`n发现目录: $dir (包含 $($items.Count) 个项目)" -ForegroundColor Yellow

            if (-not $WhatIf) {
                Remove-Item $dir -Recurse -Force
                Write-Host "  [删除] 已清理目录: $dir" -ForegroundColor Green
                $totalDeleted += $items.Count
            } else {
                Write-Host "  [模拟] 将删除: $dir" -ForegroundColor Gray
            }
        }
    }
}

# 递归查找并清理TestResults目录
Write-Host "`n查找所有TestResults目录..." -ForegroundColor Cyan
$testResultsDirs = Get-ChildItem -Path . -Directory -Recurse -Filter "TestResults" 2>$null |
    Where-Object { $_.FullName -notmatch "\\\.git\\" }

foreach ($dir in $testResultsDirs) {
    $relativePath = $dir.FullName.Replace($PWD.Path + "\", "")
    $items = Get-ChildItem $dir.FullName -Recurse 2>$null | Measure-Object

    if ($items.Count -gt 0) {
        Write-Host "发现: $relativePath (包含 $($items.Count) 个项目)" -ForegroundColor Yellow

        if (-not $WhatIf) {
            Remove-Item $dir.FullName -Recurse -Force
            Write-Host "  [删除] 已清理" -ForegroundColor Green
            $totalDeleted += $items.Count
        } else {
            Write-Host "  [模拟] 将删除" -ForegroundColor Gray
        }
    }
}

# 清理特定类型文件
Write-Host "`n查找测试相关文件..." -ForegroundColor Cyan
foreach ($category in $patternsToClean.Keys) {
    Write-Host "  $category :" -ForegroundColor White

    foreach ($pattern in $patternsToClean[$category]) {
        $files = Get-ChildItem -Path . -Filter $pattern -Recurse -File 2>$null |
            Where-Object {
                $_.FullName -notmatch "\\\.git\\" -and
                $_.FullName -match "\\tests\\"
            }

        if ($files.Count -gt 0) {
            Write-Host "    找到 $($files.Count) 个 $pattern 文件" -ForegroundColor Yellow

            if (-not $WhatIf) {
                $files | ForEach-Object {
                    Remove-Item $_.FullName -Force
                    $totalDeleted++
                }
                Write-Host "      [删除] 已清理" -ForegroundColor Green
            } else {
                Write-Host "      [模拟] 将删除" -ForegroundColor Gray
            }
        }
    }
}

# 统计信息
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host " 清理完成" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan

if (-not $WhatIf) {
    Write-Host "  已删除项目: $totalDeleted 个" -ForegroundColor White
} else {
    Write-Host "`n提示: 使用不带 -WhatIf 参数重新运行以实际删除文件" -ForegroundColor Yellow
}

Write-Host ""