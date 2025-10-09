# LYBT 本地测试运行脚本
# 用于在本地环境执行所有Server端测试
# 作者: Claude Code
# 创建时间: 2025-01-09

param(
    [Parameter(HelpMessage="测试类型: All, Unit, Integration, Auth")]
    [string]$TestType = "All",
    
    [Parameter(HelpMessage="是否生成覆盖率报告")]
    [switch]$Coverage,
    
    [Parameter(HelpMessage="是否详细输出")]
    [switch]$Verbose
)

# 设置错误处理
$ErrorActionPreference = "Stop"

# 颜色输出函数
function Write-Success($message) {
    Write-Host "✅ $message" -ForegroundColor Green
}

function Write-Error($message) {
    Write-Host "❌ $message" -ForegroundColor Red
}

function Write-Info($message) {
    Write-Host "ℹ️  $message" -ForegroundColor Cyan
}

function Write-Warning($message) {
    Write-Host "⚠️  $message" -ForegroundColor Yellow
}

# 开始执行
Write-Host ""
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "     LYBT Server端测试运行脚本         " -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""

# 获取项目根目录
$projectRoot = Split-Path -Parent $PSScriptRoot
Write-Info "项目根目录: $projectRoot"

# 切换到项目根目录
Set-Location $projectRoot

# 步骤1: 清理之前的测试结果
Write-Info "清理测试结果..."
if (Test-Path ".\TestResults") {
    Remove-Item ".\TestResults" -Recurse -Force
}

# 步骤2: 还原依赖
Write-Info "还原NuGet包..."
$restoreResult = dotnet restore LYBT.Server.sln
if ($LASTEXITCODE -ne 0) {
    Write-Error "还原依赖失败"
    exit 1
}
Write-Success "依赖还原成功"

# 步骤3: 构建解决方案
Write-Info "构建Server解决方案..."
$buildResult = dotnet build LYBT.Server.sln -c Release --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Error "构建失败"
    exit 1
}
Write-Success "构建成功"

# 步骤4: 运行测试
Write-Info "运行测试 (类型: $TestType)..."

$testFilter = ""
$testProjects = @()

switch ($TestType) {
    "Unit" {
        $testProjects = Get-ChildItem -Path "tests\UnitTests\Server\Modules" -Directory | 
                       ForEach-Object { Join-Path $_.FullName "*.csproj" }
        Write-Info "运行单元测试..."
    }
    "Integration" {
        $testProjects = @("tests\IntegrationTests\LYBT.ServerIntegrationTests\LYBT.ServerIntegrationTests.csproj")
        Write-Info "运行集成测试..."
    }
    "Auth" {
        $testProjects = @("tests\UnitTests\Server\Modules\LYBT.Module.Auth.Tests\LYBT.Module.Auth.Tests.csproj")
        Write-Info "运行Auth模块测试..."
    }
    default {
        # 运行所有测试
        $testProjects = Get-ChildItem -Path "tests" -Include "*.csproj" -Recurse | 
                       Where-Object { $_.FullName -match "Server" -or $_.FullName -match "Shared" }
        Write-Info "运行所有Server端测试..."
    }
}

$totalTests = 0
$passedTests = 0
$failedTests = 0
$skippedTests = 0

foreach ($project in $testProjects) {
    if (Test-Path $project) {
        $projectName = [System.IO.Path]::GetFileNameWithoutExtension($project)
        Write-Info "测试项目: $projectName"
        
        # 构建测试命令
        $testCommand = "dotnet test `"$project`" -c Release --no-build --settings `".runsettings`""
        
        if ($Coverage) {
            $testCommand += " --collect:`"Code Coverage`""
        }
        
        if ($Verbose) {
            $testCommand += " --logger:`"console;verbosity=detailed`""
        } else {
            $testCommand += " --logger:`"console;verbosity=normal`""
        }
        
        # 执行测试
        $testOutput = Invoke-Expression $testCommand 2>&1
        
        # 解析测试结果
        $testOutput | ForEach-Object {
            if ($_ -match "Passed:\s+(\d+)") {
                $passedTests += [int]$matches[1]
            }
            if ($_ -match "Failed:\s+(\d+)") {
                $failedTests += [int]$matches[1]
            }
            if ($_ -match "Skipped:\s+(\d+)") {
                $skippedTests += [int]$matches[1]
            }
            if ($_ -match "Total:\s+(\d+)") {
                $totalTests += [int]$matches[1]
            }
        }
        
        # 输出测试结果
        if ($Verbose) {
            $testOutput | Write-Host
        }
    } else {
        Write-Warning "未找到项目: $project"
    }
}

# 步骤5: 生成测试报告
Write-Host ""
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "           测试结果总结                " -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""

Write-Host "总测试数: $totalTests" -ForegroundColor White
Write-Host "通过数量: $passedTests" -ForegroundColor Green
Write-Host "失败数量: $failedTests" -ForegroundColor Red
Write-Host "跳过数量: $skippedTests" -ForegroundColor Yellow

if ($totalTests -gt 0) {
    $passRate = [math]::Round(($passedTests / $totalTests) * 100, 2)
    Write-Host ""
    if ($passRate -eq 100) {
        Write-Host "🎉 测试通过率: $passRate% - 完美通过!" -ForegroundColor Green
    } elseif ($passRate -ge 80) {
        Write-Host "✅ 测试通过率: $passRate% - 良好" -ForegroundColor Yellow
    } else {
        Write-Host "❌ 测试通过率: $passRate% - 需要改进" -ForegroundColor Red
    }
}

# 步骤6: 覆盖率报告
if ($Coverage -and (Test-Path ".\TestResults")) {
    Write-Host ""
    Write-Info "代码覆盖率报告已生成到: .\TestResults"
    
    # 查找覆盖率文件
    $coverageFiles = Get-ChildItem -Path ".\TestResults" -Filter "*.cobertura.xml" -Recurse
    if ($coverageFiles.Count -gt 0) {
        Write-Success "找到 $($coverageFiles.Count) 个覆盖率报告文件"
        foreach ($file in $coverageFiles) {
            Write-Info "  - $($file.Name)"
        }
    }
}

Write-Host ""
Write-Host "测试完成时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Cyan

# 返回退出码
if ($failedTests -eq 0) {
    Write-Success "所有测试通过!"
    exit 0
} else {
    Write-Error "有 $failedTests 个测试失败"
    exit 1
}