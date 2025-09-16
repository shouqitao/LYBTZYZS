#!/usr/bin/env pwsh

<#
.SYNOPSIS
Flaky Tests 检测与分析脚本

.DESCRIPTION
通过多次运行测试来检测不稳定的测试用例，分析失败模式并生成详细报告。
支持统计分析、失败率计算和稳定性评估。

.PARAMETER TestProject
测试项目路径，如果不指定则测试所有项目

.PARAMETER RunCount
测试运行次数（默认10次）

.PARAMETER ParallelRuns
并行运行数（默认3）

.PARAMETER OutputDir
输出目录（默认TestResults/FlakyAnalysis）

.EXAMPLE
.\detect-flaky-tests.ps1 -RunCount 20
运行所有测试项目20次，检测flaky tests

.EXAMPLE
.\detect-flaky-tests.ps1 -TestProject "tests/**/Users.UnitTests" -RunCount 15
仅测试用户模块15次
#>

param(
    [string]$TestProject = "",
    [int]$RunCount = 10,
    [int]$ParallelRuns = 3,
    [string]$OutputDir = "TestResults/FlakyAnalysis"
)

Write-Host "🔍 P3-Flaky Tests 检测与分析工具" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host "运行次数: $RunCount | 并行数: $ParallelRuns | 输出目录: $OutputDir" -ForegroundColor Cyan

# 清理输出目录
if (Test-Path $OutputDir) {
    Remove-Item -Recurse -Force $OutputDir
}
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

# 查找测试项目
Write-Host "🔍 查找测试项目..." -ForegroundColor Yellow
if ($TestProject) {
    $testProjects = Get-ChildItem -Path $TestProject -Name "*.csproj" -Recurse
} else {
    # 获取所有测试项目的完整路径
    $testProjects = Get-ChildItem -Recurse -Path "tests" -Name "*.csproj" | ForEach-Object {
        $relativePath = Get-ChildItem -Recurse -Path "tests" -Filter $_ | Select-Object -First 1 | ForEach-Object { 
            $_.FullName.Substring((Get-Location).Path.Length + 1) 
        }
        $relativePath
    }
    # 过滤出真正的测试项目（排除工具类项目）
    $testProjects = $testProjects | Where-Object { 
        $_ -like "*Tests.csproj" -or $_ -like "*Test.csproj" 
    } | Where-Object {
        # 排除一些工具类项目
        $_ -notlike "*TestUtilities*" -and $_ -notlike "*TestBase*"
    }
}

if ($testProjects.Count -eq 0) {
    Write-Host "❌ 未找到测试项目" -ForegroundColor Red
    exit 1
}

Write-Host "📋 找到 $($testProjects.Count) 个测试项目" -ForegroundColor Green
foreach ($project in $testProjects) {
    Write-Host "  - $project" -ForegroundColor Cyan
}

# Flaky Tests 检测结果
$flakyResults = @()
$allRunResults = @()

# 测试运行函数
function Run-TestIteration {
    param($ProjectPath, $Iteration, $OutputPath)
    
    $testCommand = @(
        "test"
        $ProjectPath
        "--logger"
        "trx;LogFileName=test-run-$Iteration.trx"
        "--logger"
        "console;verbosity=quiet"
        "--results-directory"
        $OutputPath
        "--no-build"
    )
    
    $startTime = Get-Date
    try {
        $result = & dotnet @testCommand 2>&1
        $exitCode = $LASTEXITCODE
        $endTime = Get-Date
        $duration = ($endTime - $startTime).TotalSeconds
        
        return @{
            Iteration = $Iteration
            Success = ($exitCode -eq 0)
            ExitCode = $exitCode
            Duration = $duration
            Output = $result -join "`n"
            Timestamp = $startTime
        }
    }
    catch {
        $endTime = Get-Date
        $duration = ($endTime - $startTime).TotalSeconds
        
        return @{
            Iteration = $Iteration
            Success = $false
            ExitCode = -1
            Duration = $duration
            Output = $_.Exception.Message
            Timestamp = $startTime
            Exception = $_.Exception
        }
    }
}

# 分析测试结果函数
function Analyze-TestResults {
    param($Results, $ProjectName)
    
    $totalRuns = $Results.Count
    $successCount = ($Results | Where-Object { $_.Success }).Count
    $failureCount = $totalRuns - $successCount
    $successRate = [math]::Round(($successCount / $totalRuns) * 100, 2)
    
    $avgDuration = [math]::Round(($Results | Measure-Object -Property Duration -Average).Average, 2)
    $minDuration = [math]::Round(($Results | Measure-Object -Property Duration -Minimum).Minimum, 2)
    $maxDuration = [math]::Round(($Results | Measure-Object -Property Duration -Maximum).Maximum, 2)
    
    # 检测是否为Flaky Test
    $isFlaky = $failureCount -gt 0 -and $successCount -gt 0
    $flakyLevel = if ($successRate -ge 90) { "轻微" } 
                  elseif ($successRate -ge 70) { "中等" } 
                  elseif ($successRate -ge 50) { "严重" } 
                  else { "非常严重" }
    
    # 失败模式分析
    $failurePatterns = @()
    $failedRuns = $Results | Where-Object { -not $_.Success }
    
    if ($failedRuns.Count -gt 0) {
        # 分析常见失败原因
        $timeoutErrors = $failedRuns | Where-Object { $_.Output -like "*timeout*" -or $_.Output -like "*超时*" }
        $connectionErrors = $failedRuns | Where-Object { $_.Output -like "*connection*" -or $_.Output -like "*连接*" }
        $nullRefErrors = $failedRuns | Where-Object { $_.Output -like "*NullReferenceException*" -or $_.Output -like "*Object reference*" }
        $concurrencyErrors = $failedRuns | Where-Object { $_.Output -like "*deadlock*" -or $_.Output -like "*concurrency*" }
        
        if ($timeoutErrors.Count -gt 0) { $failurePatterns += "超时错误 ($($timeoutErrors.Count)次)" }
        if ($connectionErrors.Count -gt 0) { $failurePatterns += "连接错误 ($($connectionErrors.Count)次)" }
        if ($nullRefErrors.Count -gt 0) { $failurePatterns += "空引用异常 ($($nullRefErrors.Count)次)" }
        if ($concurrencyErrors.Count -gt 0) { $failurePatterns += "并发问题 ($($concurrencyErrors.Count)次)" }
        
        if ($failurePatterns.Count -eq 0) {
            $failurePatterns += "其他错误 ($failureCount次)"
        }
    }
    
    return @{
        ProjectName = $ProjectName
        TotalRuns = $totalRuns
        SuccessCount = $successCount
        FailureCount = $failureCount
        SuccessRate = $successRate
        IsFlaky = $isFlaky
        FlakyLevel = $flakyLevel
        AvgDuration = $avgDuration
        MinDuration = $minDuration
        MaxDuration = $maxDuration
        FailurePatterns = $failurePatterns
        Results = $Results
    }
}

# 对每个测试项目执行多次测试
foreach ($project in $testProjects) {
    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($project)
    $projectPath = if ($TestProject) { Join-Path $TestProject $project } else { $project }
    $projectOutputDir = Join-Path $OutputDir $projectName
    
    Write-Host "📊 开始测试项目: $projectName" -ForegroundColor Yellow
    Write-Host "   运行 $RunCount 次，分析稳定性..." -ForegroundColor Cyan
    
    New-Item -ItemType Directory -Force -Path $projectOutputDir | Out-Null
    
    $projectResults = @()
    $runBatches = @()
    
    # 分批并行运行
    for ($i = 1; $i -le $RunCount; $i += $ParallelRuns) {
        $batchEnd = [Math]::Min($i + $ParallelRuns - 1, $RunCount)
        $batchNumbers = $i..$batchEnd
        $runBatches += ,@($batchNumbers)
    }
    
    $batchCount = 1
    foreach ($batch in $runBatches) {
        Write-Host "  ⚡ 批次 $batchCount/$($runBatches.Count): 运行测试 $($batch -join ', ')" -ForegroundColor White
        
        $jobs = @()
        foreach ($runNumber in $batch) {
            $jobs += Start-Job -ScriptBlock ${function:Run-TestIteration} -ArgumentList $projectPath, $runNumber, $projectOutputDir
        }
        
        # 等待所有任务完成
        $jobs | Wait-Job | ForEach-Object {
            $result = Receive-Job $_
            $projectResults += $result
            Remove-Job $_
        }
        
        $batchCount++
        
        # 显示批次结果
        $batchResults = $projectResults | Where-Object { $batch -contains $_.Iteration }
        $batchSuccess = ($batchResults | Where-Object { $_.Success }).Count
        Write-Host "    ✅ 批次成功: $batchSuccess/$($batch.Count)" -ForegroundColor Green
    }
    
    # 分析项目结果
    $analysis = Analyze-TestResults -Results $projectResults -ProjectName $projectName
    $allRunResults += $analysis
    
    # 显示项目分析结果
    Write-Host "📈 $projectName 稳定性分析:" -ForegroundColor Green
    Write-Host "   成功率: $($analysis.SuccessRate)% ($($analysis.SuccessCount)/$($analysis.TotalRuns))" -ForegroundColor $(if ($analysis.SuccessRate -ge 95) { "Green" } elseif ($analysis.SuccessRate -ge 80) { "Yellow" } else { "Red" })
    Write-Host "   平均耗时: $($analysis.AvgDuration)s (范围: $($analysis.MinDuration)s - $($analysis.MaxDuration)s)" -ForegroundColor Cyan
    
    if ($analysis.IsFlaky) {
        Write-Host "   🚨 检测到Flaky Test! 不稳定级别: $($analysis.FlakyLevel)" -ForegroundColor Red
        Write-Host "   失败模式: $($analysis.FailurePatterns -join '; ')" -ForegroundColor Yellow
        $flakyResults += $analysis
    } else {
        if ($analysis.SuccessRate -eq 100) {
            Write-Host "   ✅ 测试稳定，无Flaky问题" -ForegroundColor Green
        } else {
            Write-Host "   ❌ 测试持续失败，可能存在功能问题" -ForegroundColor Red
        }
    }
    
    Write-Host ""
}

# 生成详细分析报告
Write-Host "📋 生成 Flaky Tests 分析报告..." -ForegroundColor Yellow

$reportPath = Join-Path $OutputDir "flaky-tests-report.md"
$reportContent = @"
# Flaky Tests 检测与分析报告

**检测时间**: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")  
**测试配置**: 运行 $RunCount 次，并行度 $ParallelRuns  
**检测范围**: $($testProjects.Count) 个测试项目  

## 📊 总体统计

| 指标 | 数值 |
|------|------|
| 检测项目数 | $($allRunResults.Count) |
| Flaky Tests项目 | $($flakyResults.Count) |
| 稳定项目数 | $(($allRunResults | Where-Object { -not $_.IsFlaky -and $_.SuccessRate -eq 100 }).Count) |
| 持续失败项目 | $(($allRunResults | Where-Object { -not $_.IsFlaky -and $_.SuccessRate -eq 0 }).Count) |

## 🚨 Flaky Tests 详情

"@

if ($flakyResults.Count -gt 0) {
    $reportContent += "`n### 检测到的不稳定测试`n`n"
    
    foreach ($flaky in $flakyResults) {
        $reportContent += @"
#### $($flaky.ProjectName)

- **成功率**: $($flaky.SuccessRate)% ($($flaky.SuccessCount)/$($flaky.TotalRuns))
- **不稳定级别**: $($flaky.FlakyLevel)
- **平均耗时**: $($flaky.AvgDuration)s
- **失败模式**: $($flaky.FailurePatterns -join '; ')

**改进建议**:
"@
        
        # 根据失败模式提供建议
        if ($flaky.FailurePatterns -join ' ' -like "*超时*") {
            $reportContent += "- 增加测试超时时间或优化测试性能`n"
        }
        if ($flaky.FailurePatterns -join ' ' -like "*连接*") {
            $reportContent += "- 检查数据库连接配置，添加重试机制`n"
        }
        if ($flaky.FailurePatterns -join ' ' -like "*空引用*") {
            $reportContent += "- 检查Mock对象初始化，确保测试数据完整性`n"
        }
        if ($flaky.FailurePatterns -join ' ' -like "*并发*") {
            $reportContent += "- 避免共享状态，使用独立的测试数据`n"
        }
        
        $reportContent += "- 考虑添加重试机制或增加测试的确定性`n"
        $reportContent += "- 使用固定的测试数据，避免依赖外部状态`n`n"
    }
} else {
    $reportContent += "`n✅ **未检测到Flaky Tests！所有测试项目都表现稳定。**`n`n"
}

$reportContent += @"
## 📈 所有项目稳定性总览

| 项目名 | 成功率 | 运行次数 | 平均耗时 | 状态 |
|--------|--------|----------|----------|------|
"@

foreach ($result in $allRunResults) {
    $status = if ($result.IsFlaky) { "🚨 Flaky ($($result.FlakyLevel))" }
              elseif ($result.SuccessRate -eq 100) { "✅ 稳定" }
              elseif ($result.SuccessRate -eq 0) { "❌ 持续失败" }
              else { "⚠️ 部分失败" }
    
    $reportContent += "| $($result.ProjectName) | $($result.SuccessRate)% | $($result.TotalRuns) | $($result.AvgDuration)s | $status |`n"
}

$reportContent += @"

## 🎯 改进建议

### 针对Flaky Tests的通用解决方案

1. **测试隔离性**
   - 确保每个测试都有独立的测试数据
   - 避免测试之间的状态共享
   - 在测试前后正确清理资源

2. **确定性测试**
   - 使用固定的时间戳而非DateTime.Now
   - Mock所有外部依赖（数据库、网络、文件系统）
   - 避免依赖系统时间或随机数

3. **异步处理**
   - 正确处理异步操作，使用适当的等待机制
   - 避免Thread.Sleep，使用更可靠的同步原语

4. **资源管理**
   - 正确释放数据库连接和其他资源
   - 使用using语句确保资源清理
   - 避免资源竞争和死锁

### CI/CD集成建议

1. **重试机制**: 对于关键测试，可以配置自动重试1-2次
2. **隔离运行**: 避免并行运行可能冲突的测试
3. **监控报警**: 设置成功率阈值，低于95%时发送告警
4. **定期检测**: 建议每周运行此脚本检测新的Flaky Tests

---

**报告生成时间**: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")  
**工具版本**: P3-Flaky Tests 检测工具 v1.0
"@

# 保存报告
$reportContent | Out-File -FilePath $reportPath -Encoding UTF8
Write-Host "📄 详细报告已生成: $reportPath" -ForegroundColor Green

# 生成JSON格式的结果
$jsonPath = Join-Path $OutputDir "flaky-tests-results.json"
$jsonData = @{
    Timestamp = Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ"
    Configuration = @{
        RunCount = $RunCount
        ParallelRuns = $ParallelRuns
        TestProjects = $testProjects
    }
    Summary = @{
        TotalProjects = $allRunResults.Count
        FlakyProjects = $flakyResults.Count
        StableProjects = ($allRunResults | Where-Object { -not $_.IsFlaky -and $_.SuccessRate -eq 100 }).Count
    }
    Results = $allRunResults
}

$jsonData | ConvertTo-Json -Depth 10 | Out-File -FilePath $jsonPath -Encoding UTF8
Write-Host "📊 JSON结果已导出: $jsonPath" -ForegroundColor Green

# 输出最终总结
Write-Host "`n🎯 Flaky Tests 检测总结:" -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Green

if ($flakyResults.Count -eq 0) {
    Write-Host "✅ 太棒了！未检测到任何Flaky Tests" -ForegroundColor Green
    Write-Host "   所有 $($allRunResults.Count) 个测试项目都表现稳定" -ForegroundColor Cyan
} else {
    Write-Host "🚨 检测到 $($flakyResults.Count) 个不稳定的测试项目:" -ForegroundColor Red
    foreach ($flaky in $flakyResults) {
        Write-Host "   - $($flaky.ProjectName): $($flaky.SuccessRate)% 成功率 ($($flaky.FlakyLevel))" -ForegroundColor Yellow
    }
    Write-Host "`n建议优先修复成功率最低的项目" -ForegroundColor Cyan
}

Write-Host "`n📋 报告文件:" -ForegroundColor Yellow
Write-Host "   - 详细报告: $reportPath" -ForegroundColor White
Write-Host "   - JSON数据: $jsonPath" -ForegroundColor White

Write-Host "`n✨ Flaky Tests 检测完成！" -ForegroundColor Green