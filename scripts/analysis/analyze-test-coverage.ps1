# 测试覆盖率分析脚本
# 用于评估各模块的测试文件覆盖情况

$modules = @("Auth", "Consultation", "Formula", "Herbs", "MedicalCase", "Patients", "Prescriptions", "Users")
$results = @()

Write-Host "=== 模块测试覆盖分析 ===" -ForegroundColor Cyan
Write-Host ""

foreach ($module in $modules) {
    $srcPath = "src\Server\Modules\LYBT.Module.$module"
    $testPath = "tests\UnitTests\Modules\${module}.UnitTests"

    if (Test-Path $srcPath) {
        $srcFiles = (Get-ChildItem -Path $srcPath -Filter "*.cs" -Recurse | Where-Object { $_.Name -notlike "*AssemblyInfo*" }).Count
        $testFiles = 0

        if (Test-Path $testPath) {
            $testFiles = (Get-ChildItem -Path $testPath -Filter "*Tests.cs" -Recurse).Count
        }

        $ratio = if ($srcFiles -gt 0) { [math]::Round(($testFiles / $srcFiles) * 100, 2) } else { 0 }

        $results += [PSCustomObject]@{
            Module = $module
            SourceFiles = $srcFiles
            TestFiles = $testFiles
            Ratio = "$ratio%"
            Status = if ($testFiles -eq 0) { "❌ 无测试" } elseif ($ratio -lt 50) { "⚠️ 覆盖不足" } else { "✅ 较好" }
        }
    }
}

# 输出表格
$results | Format-Table -AutoSize

# 统计汇总
Write-Host ""
Write-Host "=== 汇总统计 ===" -ForegroundColor Cyan
$totalSrc = ($results | Measure-Object -Property SourceFiles -Sum).Sum
$totalTest = ($results | Measure-Object -Property TestFiles -Sum).Sum
$avgRatio = [math]::Round(($totalTest / $totalSrc) * 100, 2)

Write-Host "总源文件数: $totalSrc"
Write-Host "总测试文件数: $totalTest"
Write-Host "平均测试文件比例: $avgRatio%"
Write-Host ""

# 优先级建议
Write-Host "=== 优先级建议 ===" -ForegroundColor Yellow
$noTests = $results | Where-Object { $_.TestFiles -eq 0 }
$lowCoverage = $results | Where-Object { $_.TestFiles -gt 0 -and [int]($_.Ratio -replace '%','') -lt 50 }

if ($noTests) {
    Write-Host "🔴 无测试模块(最高优先级):"
    $noTests | ForEach-Object { Write-Host "   - $($_.Module)" }
}

if ($lowCoverage) {
    Write-Host ""
    Write-Host "🟡 覆盖不足模块(高优先级):"
    $lowCoverage | ForEach-Object { Write-Host "   - $($_.Module) ($($_.Ratio))" }
}

Write-Host ""
Write-Host "分析完成!" -ForegroundColor Green
