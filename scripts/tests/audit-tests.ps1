# 测试代码审计脚本
# Purpose: 扫描项目中所有测试文件和项目，生成清理整理计划

param(
    [string]$RootPath = ".",
    [string]$ReportPath = "_reports/2025-09/backend/test-cleanup"
)

Write-Host "=== 测试代码审计 ===" -ForegroundColor Cyan
Write-Host "根目录: $RootPath" -ForegroundColor Gray
Write-Host "报告路径: $ReportPath" -ForegroundColor Gray
Write-Host ""

# 确保报告目录存在
New-Item -ItemType Directory -Path $ReportPath -Force | Out-Null

$auditResults = @{
    TestProjects = @()
    TestFiles = @()
    TestConfigs = @()
    SolutionFiles = @()
    CIConfigs = @()
}

# 1. 扫描测试项目文件
Write-Host "🔍 Step 1: 扫描测试项目文件..." -ForegroundColor Yellow
$testProjectPatterns = @("*Test*.csproj", "*Tests.csproj", "*.Test.csproj", "*.Tests.csproj")

foreach ($pattern in $testProjectPatterns) {
    Get-ChildItem -Path $RootPath -Filter $pattern -Recurse | ForEach-Object {
        $relativePath = $_.FullName.Replace((Get-Location).Path, "").TrimStart("\")
        $auditResults.TestProjects += @{
            Name = $_.Name
            Path = $relativePath
            Directory = $_.Directory.Name
            Size = [math]::Round($_.Length / 1KB, 2)
        }
        Write-Host "  Found: $relativePath" -ForegroundColor Green
    }
}

# 2. 扫描测试源码文件
Write-Host "🔍 Step 2: 扫描测试源码文件..." -ForegroundColor Yellow
$testFilePatterns = @("*Test*.cs", "*Tests.cs", "*.Test.cs", "*.Tests.cs")

foreach ($pattern in $testFilePatterns) {
    Get-ChildItem -Path $RootPath -Filter $pattern -Recurse | Where-Object {
        $_.FullName -notlike "*\bin\*" -and $_.FullName -notlike "*\obj\*"
    } | ForEach-Object {
        $relativePath = $_.FullName.Replace((Get-Location).Path, "").TrimStart("\")
        $auditResults.TestFiles += @{
            Name = $_.Name
            Path = $relativePath
            Directory = $_.Directory.Name
            Size = [math]::Round($_.Length / 1KB, 2)
        }
    }
}

# 3. 扫描测试配置文件
Write-Host "🔍 Step 3: 扫描测试配置文件..." -ForegroundColor Yellow
$configPatterns = @("*.runsettings", "appsettings.test.json", "xunit.runner.json", "coverlet.runsettings")

foreach ($pattern in $configPatterns) {
    Get-ChildItem -Path $RootPath -Filter $pattern -Recurse | ForEach-Object {
        $relativePath = $_.FullName.Replace((Get-Location).Path, "").TrimStart("\")
        $auditResults.TestConfigs += @{
            Name = $_.Name
            Path = $relativePath
            Type = $_.Extension
        }
        Write-Host "  Config: $relativePath" -ForegroundColor Blue
    }
}

# 4. 扫描解决方案文件
Write-Host "🔍 Step 4: 扫描解决方案文件..." -ForegroundColor Yellow
Get-ChildItem -Path $RootPath -Filter "*.sln" | ForEach-Object {
    $auditResults.SolutionFiles += @{
        Name = $_.Name
        Path = $_.FullName.Replace((Get-Location).Path, "").TrimStart("\")
    }
    Write-Host "  Solution: $($_.Name)" -ForegroundColor Magenta
}

# 5. 扫描CI配置文件
Write-Host "🔍 Step 5: 扫描CI配置文件..." -ForegroundColor Yellow
$ciPatterns = @(".github/workflows/*.yml", ".github/workflows/*.yaml", "azure-pipelines*.yml", "build.yml")

foreach ($pattern in $ciPatterns) {
    Get-ChildItem -Path $pattern -ErrorAction SilentlyContinue | ForEach-Object {
        $auditResults.CIConfigs += @{
            Name = $_.Name
            Path = $_.FullName.Replace((Get-Location).Path, "").TrimStart("\")
        }
        Write-Host "  CI Config: $($_.Name)" -ForegroundColor Cyan
    }
}

# 生成统计报告
Write-Host ""
Write-Host "📊 审计统计:" -ForegroundColor Cyan
Write-Host "  测试项目: $($auditResults.TestProjects.Count)" -ForegroundColor Green
Write-Host "  测试文件: $($auditResults.TestFiles.Count)" -ForegroundColor Green
Write-Host "  配置文件: $($auditResults.TestConfigs.Count)" -ForegroundColor Blue
Write-Host "  解决方案: $($auditResults.SolutionFiles.Count)" -ForegroundColor Magenta
Write-Host "  CI配置: $($auditResults.CIConfigs.Count)" -ForegroundColor Cyan

# 生成迁移计划
Write-Host ""
Write-Host "📋 生成迁移计划..." -ForegroundColor Yellow

$migrationPlan = @()

foreach ($project in $auditResults.TestProjects) {
    $currentPath = $project.Path
    $projectName = $project.Name -replace "\.csproj$", ""
    
    # 确定测试类型
    $testType = "UnitTests"
    if ($projectName -match "Integration|Integ") { $testType = "IntegrationTests" }
    elseif ($projectName -match "E2E|End2End|EndToEnd") { $testType = "E2E" }
    elseif ($projectName -match "Api|WebApi") { $testType = "IntegrationTests" }
    
    # 生成目标路径
    $cleanName = $projectName -replace "\.Tests?$", "" -replace "Tests?$", ""
    $targetPath = "tests/$cleanName.$testType"
    
    $migrationPlan += @{
        From = $currentPath
        To = $targetPath
        Type = $testType
        ProjectName = $cleanName
    }
}

# 输出迁移计划
Write-Host ""
Write-Host "🎯 迁移计划:" -ForegroundColor Cyan
foreach ($plan in $migrationPlan) {
    Write-Host "  $($plan.From) → $($plan.To)" -ForegroundColor Yellow
}

# 生成审计报告
$reportContent = @"
# 测试代码审计报告

**执行时间**: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
**根目录**: $RootPath

## 审计统计

| 类型 | 数量 | 详情 |
|------|------|------|
| 测试项目 | $($auditResults.TestProjects.Count) | 需要迁移整理 |
| 测试文件 | $($auditResults.TestFiles.Count) | 跟随项目迁移 |
| 配置文件 | $($auditResults.TestConfigs.Count) | 需要统一配置 |
| 解决方案 | $($auditResults.SolutionFiles.Count) | 需要更新引用 |
| CI配置 | $($auditResults.CIConfigs.Count) | 需要更新路径 |

## 发现的测试项目

| 项目名 | 当前路径 | 目录 | 大小(KB) |
|--------|----------|------|----------|
$($auditResults.TestProjects | ForEach-Object { "| $($_.Name) | $($_.Path) | $($_.Directory) | $($_.Size) |" } | Out-String)

## 迁移计划

| 当前路径 | 目标路径 | 测试类型 |
|----------|----------|----------|
$($migrationPlan | ForEach-Object { "| $($_.From) | $($_.To) | $($_.Type) |" } | Out-String)

## 建议的目录结构

```
tests/
├── Core.UnitTests/
├── Infrastructure.UnitTests/
├── WebAPI.IntegrationTests/
├── Desktop.E2E/
└── Shared/
    ├── TestHelpers/
    └── TestData/
```

## 下一步操作

1. 执行 `organize-tests.ps1` 进行实际迁移
2. 更新解决方案文件引用
3. 创建统一的 `.runsettings` 配置
4. 更新CI/CD配置文件

---
*测试代码审计报告*
*Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')*
"@

$reportFile = "$ReportPath/test-audit-report.md"
$reportContent | Out-File -FilePath $reportFile -Encoding UTF8

Write-Host ""
Write-Host "✅ 审计完成！" -ForegroundColor Green
Write-Host "📄 报告已生成: $reportFile" -ForegroundColor Blue

# 输出JSON格式的结果，供后续脚本使用
$jsonResult = @{
    Timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    AuditResults = $auditResults
    MigrationPlan = $migrationPlan
} | ConvertTo-Json -Depth 10

$jsonFile = "$ReportPath/test-audit-results.json"
$jsonResult | Out-File -FilePath $jsonFile -Encoding UTF8

Write-Host "📊 结构化数据: $jsonFile" -ForegroundColor Blue
Write-Host ""
Write-Host "Ready to execute migration! Run: pwsh ./scripts/tests/organize-tests.ps1" -ForegroundColor Green