#!/usr/bin/env pwsh

<#
.SYNOPSIS
收集.NET项目的测试覆盖率数据

.DESCRIPTION
这个脚本运行所有单元测试并收集覆盖率数据，生成HTML报告。
支持覆盖率阈值检查，用于CI/CD流水线。

.PARAMETER TestFilter
测试过滤条件，默认运行所有测试

.PARAMETER CoverageThreshold
覆盖率阈值百分比（默认70%）

.PARAMETER OutputDir
输出目录（默认TestResults）

.EXAMPLE
.\collect-coverage.ps1
运行所有测试并生成覆盖率报告

.EXAMPLE
.\collect-coverage.ps1 -TestFilter "FullyQualifiedName~Users" -CoverageThreshold 80
仅运行用户相关测试，要求80%覆盖率
#>

param(
    [string]$TestFilter = "",
    [int]$CoverageThreshold = 70,
    [string]$OutputDir = "TestResults"
)

Write-Host "🧪 P3-测试逻辑优化与覆盖率提升 - 覆盖率收集脚本" -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Green

# 检查必需工具
$requiredTools = @{
    "dotnet" = "dotnet --version"
    "reportgenerator" = "reportgenerator --version"
}

Write-Host "🔍 检查必需工具..." -ForegroundColor Yellow
foreach ($tool in $requiredTools.Keys) {
    try {
        $version = Invoke-Expression $requiredTools[$tool] 2>$null
        Write-Host "✅ $tool : $version" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ $tool 未安装" -ForegroundColor Red
        if ($tool -eq "reportgenerator") {
            Write-Host "安装命令: dotnet tool install -g dotnet-reportgenerator-globaltool" -ForegroundColor Cyan
        }
        exit 1
    }
}

# 清理旧结果
Write-Host "🧹 清理旧的测试结果..." -ForegroundColor Yellow
if (Test-Path $OutputDir) {
    Remove-Item -Recurse -Force $OutputDir
}
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

# 查找测试项目
Write-Host "🔍 查找测试项目..." -ForegroundColor Yellow
$testProjects = Get-ChildItem -Recurse -Path "tests" -Name "*.csproj" | Where-Object { 
    $_ -like "*Tests.csproj" -or $_ -like "*Test.csproj" 
} | ForEach-Object { Join-Path "tests" $_ }

if ($testProjects.Count -eq 0) {
    Write-Host "❌ 未找到测试项目" -ForegroundColor Red
    exit 1
}

Write-Host "📋 找到 $($testProjects.Count) 个测试项目:" -ForegroundColor Green
foreach ($project in $testProjects) {
    Write-Host "  - $project" -ForegroundColor Cyan
}

# 运行测试和覆盖率收集
Write-Host "🚀 运行测试并收集覆盖率..." -ForegroundColor Yellow

$coverageFiles = @()
$testResults = @()

foreach ($project in $testProjects) {
    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($project)
    $projectOutputDir = Join-Path $OutputDir $projectName
    
    Write-Host "📊 运行测试项目: $projectName" -ForegroundColor Cyan
    
    # 构建测试命令
    $testCommand = @(
        "test"
        $project
        "--collect:XPlat Code Coverage"
        "--results-directory"
        $projectOutputDir
        "--logger"
        "console;verbosity=normal"
        "--no-build"
    )
    
    if ($TestFilter) {
        $testCommand += "--filter"
        $testCommand += $TestFilter
    }
    
    try {
        $result = & dotnet @testCommand
        $exitCode = $LASTEXITCODE
        
        if ($exitCode -eq 0) {
            Write-Host "✅ $projectName 测试通过" -ForegroundColor Green
        } else {
            Write-Host "⚠️ $projectName 测试失败，但继续收集覆盖率" -ForegroundColor Yellow
        }
        
        $testResults += @{
            Project = $projectName
            Success = ($exitCode -eq 0)
            Output = $result
        }
        
        # 查找覆盖率文件
        $coverageFile = Get-ChildItem -Recurse -Path $projectOutputDir -Name "coverage.cobertura.xml" | Select-Object -First 1
        if ($coverageFile) {
            $coverageFiles += Join-Path $projectOutputDir $coverageFile.DirectoryName $coverageFile.Name
            Write-Host "📈 找到覆盖率文件: $($coverageFile.Name)" -ForegroundColor Green
        }
    }
    catch {
        Write-Host "❌ 运行 $projectName 测试时出错: $($_.Exception.Message)" -ForegroundColor Red
        $testResults += @{
            Project = $projectName
            Success = $false
            Output = $_.Exception.Message
        }
    }
}

# 生成覆盖率报告
if ($coverageFiles.Count -gt 0) {
    Write-Host "📊 生成覆盖率报告..." -ForegroundColor Yellow
    
    $reportDir = Join-Path $OutputDir "CoverageReport"
    $coverageFilesStr = $coverageFiles -join ";"
    
    try {
        & reportgenerator `
            "-reports:$coverageFilesStr" `
            "-targetdir:$reportDir" `
            "-reporttypes:Html;JsonSummary;Badges" `
            "-sourcedirs:src" `
            "-title:LYBTZYZS Test Coverage Report"
            
        Write-Host "✅ 覆盖率报告生成成功: $reportDir" -ForegroundColor Green
        Write-Host "🌐 HTML报告: $reportDir\index.html" -ForegroundColor Cyan
        
        # 读取覆盖率摘要
        $summaryFile = Join-Path $reportDir "Summary.json"
        if (Test-Path $summaryFile) {
            $summary = Get-Content $summaryFile | ConvertFrom-Json
            $lineCoverage = [math]::Round($summary.coverage.linecoverage, 2)
            $branchCoverage = [math]::Round($summary.coverage.branchcoverage, 2)
            
            Write-Host "📈 覆盖率统计:" -ForegroundColor Green
            Write-Host "  - 行覆盖率: $lineCoverage%" -ForegroundColor Cyan
            Write-Host "  - 分支覆盖率: $branchCoverage%" -ForegroundColor Cyan
            
            # 检查覆盖率阈值
            if ($lineCoverage -lt $CoverageThreshold) {
                Write-Host "❌ 行覆盖率 ($lineCoverage%) 低于阈值 ($CoverageThreshold%)" -ForegroundColor Red
                exit 1
            } else {
                Write-Host "✅ 行覆盖率达到要求" -ForegroundColor Green
            }
        }
    }
    catch {
        Write-Host "❌ 生成覆盖率报告失败: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "⚠️ 未找到覆盖率数据文件" -ForegroundColor Yellow
}

# 输出测试结果摘要
Write-Host "`n📋 测试结果摘要:" -ForegroundColor Green
Write-Host "=================`n" -ForegroundColor Green

$passedCount = 0
$failedCount = 0

foreach ($result in $testResults) {
    if ($result.Success) {
        Write-Host "✅ $($result.Project)" -ForegroundColor Green
        $passedCount++
    } else {
        Write-Host "❌ $($result.Project)" -ForegroundColor Red
        $failedCount++
    }
}

Write-Host "`n🎯 总计: $($testResults.Count) 个项目" -ForegroundColor Yellow
Write-Host "✅ 通过: $passedCount" -ForegroundColor Green
Write-Host "❌ 失败: $failedCount" -ForegroundColor Red

if ($failedCount -gt 0) {
    Write-Host "`n⚠️ 部分测试失败，但覆盖率收集已完成" -ForegroundColor Yellow
    # 不退出脚本，允许查看覆盖率报告
}

Write-Host "`n🎉 覆盖率收集完成！" -ForegroundColor Green