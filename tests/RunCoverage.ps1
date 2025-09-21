# PowerShell脚本：运行测试并生成覆盖率报告
# 使用方法: .\tests\RunCoverage.ps1

param(
    [string]$Configuration = "Release",
    [bool]$OpenReport = $true,
    [bool]$EnforceThresholds = $false
)

Write-Host "======================================" -ForegroundColor Cyan
Write-Host " 服务端单元测试覆盖率收集工具" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# 定义路径
$RootDir = Split-Path $PSScriptRoot -Parent
$TestDir = $PSScriptRoot
$OutputDir = Join-Path $RootDir "BIN\TestResults"
$CoverageDir = Join-Path $OutputDir "coverage"

# 清理旧的测试结果
Write-Host "清理旧的测试结果..." -ForegroundColor Yellow
if (Test-Path $OutputDir) {
    Remove-Item -Path $OutputDir -Recurse -Force
}
New-Item -Path $OutputDir -ItemType Directory -Force | Out-Null
New-Item -Path $CoverageDir -ItemType Directory -Force | Out-Null

# 还原NuGet包
Write-Host ""
Write-Host "还原NuGet包..." -ForegroundColor Yellow
dotnet restore "$RootDir\LYBT.Server.sln" --nologo

# 构建解决方案
Write-Host ""
Write-Host "构建解决方案..." -ForegroundColor Yellow
dotnet build "$RootDir\LYBT.Server.sln" -c $Configuration --no-restore --nologo

if ($LASTEXITCODE -ne 0) {
    Write-Host "构建失败！" -ForegroundColor Red
    exit $LASTEXITCODE
}

# 运行测试并收集覆盖率
Write-Host ""
Write-Host "运行测试并收集覆盖率..." -ForegroundColor Yellow
Write-Host "这可能需要几分钟时间，请耐心等待..." -ForegroundColor Gray

$testCommand = "dotnet test `"$RootDir\LYBT.Server.sln`" " +
    "-c $Configuration " +
    "--no-build " +
    "--no-restore " +
    "--collect:`"XPlat Code Coverage`" " +
    "--results-directory `"$OutputDir`" " +
    "--logger `"trx;LogFileName=test-results.trx`" " +
    "--logger `"console;verbosity=minimal`""

if ($EnforceThresholds) {
    $testCommand += " -p:EnforceCoverageThresholds=true"
}

Invoke-Expression $testCommand

$testExitCode = $LASTEXITCODE

# 查找覆盖率文件
Write-Host ""
Write-Host "查找覆盖率文件..." -ForegroundColor Yellow
$coverageFiles = Get-ChildItem -Path $OutputDir -Filter "coverage.cobertura.xml" -Recurse

if ($coverageFiles.Count -eq 0) {
    Write-Host "未找到覆盖率文件！" -ForegroundColor Red
    exit 1
}

Write-Host "找到 $($coverageFiles.Count) 个覆盖率文件" -ForegroundColor Green

# 安装ReportGenerator（如果需要）
Write-Host ""
Write-Host "检查ReportGenerator工具..." -ForegroundColor Yellow
$reportGeneratorPath = & dotnet tool list -g | Select-String "reportgenerator"

if (-not $reportGeneratorPath) {
    Write-Host "安装ReportGenerator工具..." -ForegroundColor Yellow
    dotnet tool install -g dotnet-reportgenerator-globaltool
}

# 生成HTML报告
Write-Host ""
Write-Host "生成覆盖率报告..." -ForegroundColor Yellow

$coverageFilePaths = $coverageFiles | ForEach-Object { $_.FullName }
$reportCommand = "reportgenerator " +
    "-reports:`"$($coverageFilePaths -join ';')`" " +
    "-targetdir:`"$CoverageDir`" " +
    "-reporttypes:Html;Cobertura;JsonSummary;Badges " +
    "-title:`"LYBT服务端测试覆盖率报告`" " +
    "-tag:`"$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')`" " +
    "-verbosity:Info"

Invoke-Expression $reportCommand

if ($LASTEXITCODE -ne 0) {
    Write-Host "报告生成失败！" -ForegroundColor Red
    exit $LASTEXITCODE
}

# 显示覆盖率摘要
Write-Host ""
Write-Host "======================================" -ForegroundColor Green
Write-Host " 覆盖率收集完成" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Green

# 读取并显示覆盖率摘要
$summaryFile = Join-Path $CoverageDir "Summary.json"
if (Test-Path $summaryFile) {
    $summary = Get-Content $summaryFile | ConvertFrom-Json

    Write-Host ""
    Write-Host "覆盖率摘要：" -ForegroundColor Cyan
    Write-Host "  - 行覆盖率: $($summary.summary.linecoverage)%" -ForegroundColor White
    Write-Host "  - 分支覆盖率: $($summary.summary.branchcoverage)%" -ForegroundColor White
    Write-Host "  - 方法覆盖率: $($summary.summary.methodcoverage)%" -ForegroundColor White

    # 检查是否达到阈值
    if ($EnforceThresholds) {
        $lineThreshold = 90
        $branchThreshold = 80

        if ($summary.summary.linecoverage -lt $lineThreshold) {
            Write-Host ""
            Write-Host "警告：行覆盖率 ($($summary.summary.linecoverage)%) 低于阈值 ($lineThreshold%)！" -ForegroundColor Yellow
        }

        if ($summary.summary.branchcoverage -lt $branchThreshold) {
            Write-Host "警告：分支覆盖率 ($($summary.summary.branchcoverage)%) 低于阈值 ($branchThreshold%)！" -ForegroundColor Yellow
        }
    }
}

Write-Host ""
Write-Host "报告位置：" -ForegroundColor Cyan
Write-Host "  HTML报告: $CoverageDir\index.html" -ForegroundColor White
Write-Host "  Cobertura: $CoverageDir\Cobertura.xml" -ForegroundColor White
Write-Host "  JSON摘要: $CoverageDir\Summary.json" -ForegroundColor White

# 打开HTML报告
if ($OpenReport) {
    Write-Host ""
    Write-Host "正在打开HTML报告..." -ForegroundColor Yellow
    Start-Process "$CoverageDir\index.html"
}

# 返回测试结果
if ($testExitCode -ne 0) {
    Write-Host ""
    Write-Host "测试失败！请检查测试输出。" -ForegroundColor Red
    exit $testExitCode
}

Write-Host ""
Write-Host "所有测试通过！" -ForegroundColor Green
exit 0