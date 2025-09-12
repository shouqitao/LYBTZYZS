# P3 Record-Only Smoke Validation - 测试矩阵验证脚本
# 目标：运行架构测试和单元测试，确保系统Record-Only模式合规性

param(
    [switch]$ArchOnly = $false,
    [switch]$Coverage = $false,
    [switch]$Verbose = $false,
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# 脚本配置
$PROJECT_ROOT = Split-Path -Path $PSScriptRoot -Parent | Split-Path -Parent
$RESULTS_LOG = Join-Path $PSScriptRoot "test-matrix-results.json"

# 测试项目路径
$ARCH_TESTS = Join-Path $PROJECT_ROOT "tests\Architecture\ArchTests.csproj"
$UNIT_TESTS = @(
    "tests\Backend\LYBT.Module.Auth.Tests\LYBT.Module.Auth.Tests.csproj",
    "tests\Backend\LYBT.Module.Users.Tests\LYBT.Module.Users.Tests.csproj",
    "tests\Backend\LYBT.Module.Patients.Tests\LYBT.Module.Patients.Tests.csproj",
    "tests\Backend\LYBT.Module.Herbs.Tests\LYBT.Module.Herbs.Tests.csproj"
)

# 测试结果存储
$script:TestMatrix = @{
    StartTime = Get-Date
    EndTime = $null
    Configuration = $Configuration
    ArchitectureTests = @{
        Status = "NotRun"
        Duration = 0
        TotalTests = 0
        PassedTests = 0
        FailedTests = 0
        Details = @()
    }
    UnitTests = @{
        Status = "NotRun"
        Duration = 0
        TotalTests = 0
        PassedTests = 0
        FailedTests = 0
        Coverage = @{
            Enabled = $Coverage
            LineRate = 0.0
            BranchRate = 0.0
        }
        ProjectResults = @{}
    }
    RecordOnlyCompliance = @{
        Status = "NotRun"
        IntelligenceFeaturesFound = @()
        ConditionCompilationFound = @()
        ProhibitedNamingFound = @()
    }
    Summary = ""
}

Write-Host "=== P3 Record-Only 测试矩阵验证 ===" -ForegroundColor Cyan
Write-Host "时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host "配置: $Configuration" -ForegroundColor Gray
Write-Host "覆盖率: $(if($Coverage) { '启用' } else { '禁用' })" -ForegroundColor Gray
Write-Host ""

function Write-TestLog {
    param([string]$Message, [string]$Level = "INFO")
    $timestamp = Get-Date -Format "HH:mm:ss"
    $logEntry = "[$timestamp] [$Level] $Message"
    if ($Verbose -or $Level -eq "ERROR" -or $Level -eq "WARN") {
        Write-Host $logEntry -ForegroundColor $(
            switch ($Level) {
                "ERROR" { "Red" }
                "WARN" { "Yellow" }
                "SUCCESS" { "Green" }
                default { "White" }
            }
        )
    }
}

function Test-ProjectExists {
    param([string]$ProjectPath)
    
    $fullPath = Join-Path $PROJECT_ROOT $ProjectPath
    if (-not (Test-Path $fullPath)) {
        Write-TestLog "测试项目不存在: $fullPath" "WARN"
        return $false
    }
    return $true
}

function Run-ArchitectureTests {
    Write-Host "`n=== 运行架构测试 ===" -ForegroundColor Cyan
    
    if (-not (Test-Path $ARCH_TESTS)) {
        Write-TestLog "架构测试项目不存在: $ARCH_TESTS" "ERROR"
        $script:TestMatrix.ArchitectureTests.Status = "Error"
        return
    }
    
    try {
        $startTime = Get-Date
        Write-TestLog "执行架构测试..." "INFO"
        
        # 运行架构测试
        $testOutput = & dotnet test $ARCH_TESTS --configuration $Configuration --logger "console;verbosity=detailed" --no-build 2>&1
        $endTime = Get-Date
        $duration = ($endTime - $startTime).TotalSeconds
        
        $script:TestMatrix.ArchitectureTests.Duration = $duration
        
        # 解析测试结果
        $passed = 0
        $failed = 0
        $total = 0
        $details = @()
        
        foreach ($line in $testOutput) {
            if ($line -match "Passed:\s+(\d+)") {
                $passed = [int]$matches[1]
            }
            elseif ($line -match "Failed:\s+(\d+)") {
                $failed = [int]$matches[1]
            }
            elseif ($line -match "Total:\s+(\d+)") {
                $total = [int]$matches[1]
            }
            elseif ($line -match "FAIL|ERROR") {
                $details += $line
            }
        }
        
        $script:TestMatrix.ArchitectureTests.TotalTests = $total
        $script:TestMatrix.ArchitectureTests.PassedTests = $passed
        $script:TestMatrix.ArchitectureTests.FailedTests = $failed
        $script:TestMatrix.ArchitectureTests.Details = $details
        
        if ($failed -eq 0) {
            $script:TestMatrix.ArchitectureTests.Status = "Passed"
            Write-TestLog "✅ 架构测试全部通过 ($passed/$total)" "SUCCESS"
        } else {
            $script:TestMatrix.ArchitectureTests.Status = "Failed"
            Write-TestLog "❌ 架构测试失败 ($failed/$total)" "ERROR"
            
            # 输出失败详情
            foreach ($detail in $details) {
                Write-TestLog "架构测试失败详情: $detail" "ERROR"
            }
        }
        
    } catch {
        $script:TestMatrix.ArchitectureTests.Status = "Error"
        Write-TestLog "架构测试执行异常: $($_.Exception.Message)" "ERROR"
    }
}

function Run-UnitTests {
    Write-Host "`n=== 运行单元测试 ===" -ForegroundColor Cyan
    
    if ($ArchOnly) {
        Write-TestLog "跳过单元测试（仅运行架构测试模式）" "INFO"
        $script:TestMatrix.UnitTests.Status = "Skipped"
        return
    }
    
    try {
        $startTime = Get-Date
        $totalPassed = 0
        $totalFailed = 0
        $totalTests = 0
        
        # 逐个运行单元测试项目
        foreach ($testProject in $UNIT_TESTS) {
            if (-not (Test-ProjectExists -ProjectPath $testProject)) {
                continue
            }
            
            $projectName = [System.IO.Path]::GetFileNameWithoutExtension($testProject)
            Write-TestLog "运行单元测试: $projectName" "INFO"
            
            $projectPath = Join-Path $PROJECT_ROOT $testProject
            
            # 构建测试命令
            $testArgs = @(
                "test", $projectPath
                "--configuration", $Configuration
                "--logger", "console;verbosity=normal"
                "--no-build"
            )
            
            if ($Coverage) {
                $testArgs += @("--collect", "XPlat Code Coverage")
            }
            
            # 运行测试
            $testOutput = & dotnet $testArgs 2>&1
            
            # 解析项目测试结果
            $projectPassed = 0
            $projectFailed = 0
            $projectTotal = 0
            
            foreach ($line in $testOutput) {
                if ($line -match "Passed:\s+(\d+)") {
                    $projectPassed = [int]$matches[1]
                }
                elseif ($line -match "Failed:\s+(\d+)") {
                    $projectFailed = [int]$matches[1]
                }
                elseif ($line -match "Total:\s+(\d+)") {
                    $projectTotal = [int]$matches[1]
                }
            }
            
            # 记录项目结果
            $script:TestMatrix.UnitTests.ProjectResults[$projectName] = @{
                Passed = $projectPassed
                Failed = $projectFailed
                Total = $projectTotal
                Status = if ($projectFailed -eq 0) { "Passed" } else { "Failed" }
            }
            
            $totalPassed += $projectPassed
            $totalFailed += $projectFailed
            $totalTests += $projectTotal
            
            Write-TestLog "$projectName: $projectPassed/$projectTotal 通过" $(if ($projectFailed -eq 0) { "SUCCESS" } else { "ERROR" })
        }
        
        $endTime = Get-Date
        $duration = ($endTime - $startTime).TotalSeconds
        
        $script:TestMatrix.UnitTests.Duration = $duration
        $script:TestMatrix.UnitTests.TotalTests = $totalTests
        $script:TestMatrix.UnitTests.PassedTests = $totalPassed
        $script:TestMatrix.UnitTests.FailedTests = $totalFailed
        
        if ($totalFailed -eq 0) {
            $script:TestMatrix.UnitTests.Status = "Passed"
            Write-TestLog "✅ 单元测试全部通过 ($totalPassed/$totalTests)" "SUCCESS"
        } else {
            $script:TestMatrix.UnitTests.Status = "Failed"
            Write-TestLog "❌ 单元测试存在失败 ($totalFailed/$totalTests)" "ERROR"
        }
        
    } catch {
        $script:TestMatrix.UnitTests.Status = "Error"
        Write-TestLog "单元测试执行异常: $($_.Exception.Message)" "ERROR"
    }
}

function Check-RecordOnlyCompliance {
    Write-Host "`n=== Record-Only 合规性检查 ===" -ForegroundColor Cyan
    
    try {
        $script:TestMatrix.RecordOnlyCompliance.Status = "Running"
        
        # 检查智能推荐相关残留
        Write-TestLog "检查智能推荐功能残留..." "INFO"
        $intelligencePatterns = @("Recommendation", "Intelligence", "MachineLearning", "Prediction", "Analytics", "SmartEngine")
        $intelligenceFound = @()
        
        foreach ($pattern in $intelligencePatterns) {
            $searchResult = Select-String -Path "$PROJECT_ROOT\src\**\*.cs" -Pattern $pattern -ErrorAction SilentlyContinue
            if ($searchResult) {
                $intelligenceFound += @{
                    Pattern = $pattern
                    Files = $searchResult | ForEach-Object { $_.Filename }
                }
            }
        }
        
        # 检查条件编译残留
        Write-TestLog "检查条件编译残留..." "INFO"
        $conditionalPatterns = @("#if ENABLE_SMART_FEATURES", "#ifdef ENABLE_SMART_FEATURES", "ENABLE_SMART_FEATURES")
        $conditionalFound = @()
        
        foreach ($pattern in $conditionalPatterns) {
            $searchResult = Select-String -Path "$PROJECT_ROOT\src\**\*.cs" -Pattern $pattern -ErrorAction SilentlyContinue
            if ($searchResult) {
                $conditionalFound += @{
                    Pattern = $pattern
                    Files = $searchResult | ForEach-Object { $_.Filename }
                }
            }
        }
        
        # 检查禁止命名模式
        Write-TestLog "检查禁止命名模式..." "INFO"
        $prohibitedPatterns = @("Pipeline", "Workflow", "Bus", "Engine", "Saga")
        $namingFound = @()
        
        foreach ($pattern in $prohibitedPatterns) {
            $searchResult = Select-String -Path "$PROJECT_ROOT\src\**\*.cs" -Pattern "class.*$pattern|interface.*$pattern|enum.*$pattern" -ErrorAction SilentlyContinue
            if ($searchResult) {
                $namingFound += @{
                    Pattern = $pattern
                    Files = $searchResult | ForEach-Object { $_.Filename }
                }
            }
        }
        
        # 记录结果
        $script:TestMatrix.RecordOnlyCompliance.IntelligenceFeaturesFound = $intelligenceFound
        $script:TestMatrix.RecordOnlyCompliance.ConditionCompilationFound = $conditionalFound  
        $script:TestMatrix.RecordOnlyCompliance.ProhibitedNamingFound = $namingFound
        
        # 判断合规状态
        $totalViolations = $intelligenceFound.Count + $conditionalFound.Count + $namingFound.Count
        
        if ($totalViolations -eq 0) {
            $script:TestMatrix.RecordOnlyCompliance.Status = "Compliant"
            Write-TestLog "✅ Record-Only模式合规性检查通过" "SUCCESS"
        } else {
            $script:TestMatrix.RecordOnlyCompliance.Status = "NonCompliant"
            Write-TestLog "❌ 发现 $totalViolations 项合规性违规" "ERROR"
            
            if ($intelligenceFound.Count -gt 0) {
                Write-TestLog "智能推荐功能残留: $($intelligenceFound.Count) 项" "ERROR"
            }
            if ($conditionalFound.Count -gt 0) {
                Write-TestLog "条件编译残留: $($conditionalFound.Count) 项" "ERROR"
            }
            if ($namingFound.Count -gt 0) {
                Write-TestLog "禁止命名模式: $($namingFound.Count) 项" "ERROR"
            }
        }
        
    } catch {
        $script:TestMatrix.RecordOnlyCompliance.Status = "Error"
        Write-TestLog "合规性检查异常: $($_.Exception.Message)" "ERROR"
    }
}

function Save-TestMatrix {
    $script:TestMatrix.EndTime = Get-Date
    $duration = $script:TestMatrix.EndTime - $script:TestMatrix.StartTime
    
    # 生成总结
    $archStatus = $script:TestMatrix.ArchitectureTests.Status
    $unitStatus = $script:TestMatrix.UnitTests.Status
    $complianceStatus = $script:TestMatrix.RecordOnlyCompliance.Status
    
    $overallStatus = if ($archStatus -eq "Passed" -and ($unitStatus -eq "Passed" -or $unitStatus -eq "Skipped") -and $complianceStatus -eq "Compliant") {
        "PASS ✅"
    } else {
        "FAIL ❌"
    }
    
    $script:TestMatrix.Summary = @"
P3 Record-Only 测试矩阵验证完成
====================================

执行时间: $($script:TestMatrix.StartTime.ToString('yyyy-MM-dd HH:mm:ss')) - $($script:TestMatrix.EndTime.ToString('yyyy-MM-dd HH:mm:ss'))
总耗时: $([math]::Round($duration.TotalSeconds, 2)) 秒
配置: $Configuration

架构测试: $archStatus
- 通过: $($script:TestMatrix.ArchitectureTests.PassedTests)/$($script:TestMatrix.ArchitectureTests.TotalTests)
- 耗时: $([math]::Round($script:TestMatrix.ArchitectureTests.Duration, 2)) 秒

单元测试: $unitStatus
- 通过: $($script:TestMatrix.UnitTests.PassedTests)/$($script:TestMatrix.UnitTests.TotalTests)  
- 耗时: $([math]::Round($script:TestMatrix.UnitTests.Duration, 2)) 秒

Record-Only合规: $complianceStatus
- 智能推荐残留: $($script:TestMatrix.RecordOnlyCompliance.IntelligenceFeaturesFound.Count) 项
- 条件编译残留: $($script:TestMatrix.RecordOnlyCompliance.ConditionCompilationFound.Count) 项
- 禁止命名残留: $($script:TestMatrix.RecordOnlyCompliance.ProhibitedNamingFound.Count) 项

总体状态: $overallStatus
"@
    
    # 保存结果
    $script:TestMatrix | ConvertTo-Json -Depth 10 | Out-File -FilePath $RESULTS_LOG -Encoding UTF8
    
    Write-Host "`n$($script:TestMatrix.Summary)" -ForegroundColor $(if ($overallStatus.Contains("PASS")) { "Green" } else { "Red" })
    Write-Host "`n详细结果已保存到: $RESULTS_LOG" -ForegroundColor Gray
}

# 主执行流程
try {
    # 切换到项目根目录
    Set-Location $PROJECT_ROOT
    
    # 运行架构测试
    Run-ArchitectureTests
    
    # 运行单元测试
    if (-not $ArchOnly) {
        Run-UnitTests
    }
    
    # Record-Only合规性检查
    Check-RecordOnlyCompliance
    
} catch {
    Write-TestLog "测试矩阵验证异常: $($_.Exception.Message)" "ERROR"
} finally {
    # 保存测试矩阵结果
    Save-TestMatrix
    
    Write-Host "`n测试矩阵验证完成！" -ForegroundColor Cyan
    
    # 根据结果确定退出代码
    $hasFailures = ($script:TestMatrix.ArchitectureTests.Status -eq "Failed") -or 
                   ($script:TestMatrix.UnitTests.Status -eq "Failed") -or
                   ($script:TestMatrix.RecordOnlyCompliance.Status -ne "Compliant")
    
    if ($hasFailures) {
        Write-Host "存在失败项或合规性问题，请检查详细日志" -ForegroundColor Red
        exit 1
    } else {
        Write-Host "所有测试验证通过" -ForegroundColor Green
        exit 0
    }
}