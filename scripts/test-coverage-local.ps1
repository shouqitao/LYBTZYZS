# P3本地覆盖率验证脚本
# 用于本地验证70%覆盖率硬门槛要求

param(
    [int]$CoverageThreshold = 70,
    [string]$ModulesPath = "tests/UnitTests/Modules",
    [switch]$SkipBuild,
    [switch]$OpenReport
)

Write-Host "🎯 P3本地覆盖率验证脚本" -ForegroundColor Cyan
Write-Host "覆盖率阈值: $CoverageThreshold%" -ForegroundColor Yellow
Write-Host "=" * 50

# 检查dotnet版本
$dotnetVersion = dotnet --version
Write-Host "✓ .NET版本: $dotnetVersion" -ForegroundColor Green

# 构建项目（除非跳过）
if (-not $SkipBuild) {
    Write-Host "`n🔨 构建项目..." -ForegroundColor Cyan
    dotnet build LYBTZYZS.sln --configuration Release --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ 构建失败" -ForegroundColor Red
        exit 1
    }
    Write-Host "✓ 构建成功" -ForegroundColor Green
}

# 创建测试结果目录
$testResultsPath = "TestResults/LocalCoverage"
if (Test-Path $testResultsPath) {
    Remove-Item $testResultsPath -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $testResultsPath | Out-Null

Write-Host "`n🧪 运行测试收集覆盖率..." -ForegroundColor Cyan

# 定义所有模块测试项目
$moduleTests = @(
    "tests/UnitTests/Modules/Users.UnitTests/LYBT.Module.Users.Tests.csproj",
    "tests/UnitTests/Modules/Patients.UnitTests/LYBT.Module.Patients.Tests.csproj", 
    "tests/UnitTests/Modules/Prescriptions.UnitTests/LYBT.Module.Prescriptions.Tests.csproj",
    "tests/UnitTests/Modules/Consultation.UnitTests/LYBT.Module.Consultation.Tests.csproj",
    "tests/UnitTests/Modules/Herbs.UnitTests/LYBT.Module.Herbs.Tests.csproj",
    "tests/UnitTests/Modules/Formula.UnitTests/LYBT.Module.Formula.Tests.csproj",
    "tests/UnitTests/Modules/MedicalCase.UnitTests/LYBT.Module.MedicalCase.Tests.csproj",
    "tests/UnitTests/Modules/Auth.UnitTests/LYBT.Module.Auth.Tests.csproj"
)

# 运行每个模块的测试
$testResults = @()
foreach ($testProject in $moduleTests) {
    $moduleName = Split-Path (Split-Path $testProject -Parent) -Leaf
    Write-Host "  📦 运行 $moduleName 模块测试..." -ForegroundColor Gray
    
    $moduleResultPath = "$testResultsPath/$moduleName"
    New-Item -ItemType Directory -Force -Path $moduleResultPath | Out-Null
    
    dotnet test $testProject `
        --configuration Release `
        --no-build `
        --collect:"XPlat Code Coverage" `
        --results-directory $moduleResultPath `
        --logger "console;verbosity=minimal" `
        -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura
    
    if ($LASTEXITCODE -eq 0) {
        $testResults += @{ Module = $moduleName; Status = "PASS" }
        Write-Host "    ✓ $moduleName 测试通过" -ForegroundColor Green
    } else {
        $testResults += @{ Module = $moduleName; Status = "FAIL" }
        Write-Host "    ❌ $moduleName 测试失败" -ForegroundColor Red
    }
}

# 检查是否安装了reportgenerator
Write-Host "`n📊 生成覆盖率报告..." -ForegroundColor Cyan
$toolCheck = dotnet tool list -g | Select-String "dotnet-reportgenerator-globaltool"
if ($null -eq $toolCheck) {
    Write-Host "🔧 安装ReportGenerator..." -ForegroundColor Yellow
    dotnet tool install -g dotnet-reportgenerator-globaltool
} else {
    Write-Host "✓ ReportGenerator已安装" -ForegroundColor Green
}

# 查找所有覆盖率文件
$coverageFiles = Get-ChildItem -Path $testResultsPath -Recurse -Filter "coverage.cobertura.xml" | ForEach-Object { $_.FullName }

if ($coverageFiles.Count -eq 0) {
    Write-Host "❌ 未找到覆盖率数据文件" -ForegroundColor Red
    exit 1
}

Write-Host "✓ 找到 $($coverageFiles.Count) 个覆盖率文件" -ForegroundColor Green

# 生成合并的覆盖率报告
$reportPath = "$testResultsPath/CoverageReport"
$coverageInput = $coverageFiles -join ";"

reportgenerator `
    "-reports:$coverageInput" `
    "-targetdir:$reportPath" `
    "-reporttypes:Html;JsonSummary;TextSummary;Badges" `
    "-sourcedirs:src" `
    "-title:LYBTZYZS P3本地覆盖率报告" `
    "-assemblyfilters:-*Tests*" `
    "-classfilters:-*Tests*"

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 覆盖率报告生成失败" -ForegroundColor Red
    exit 1
}

# 读取覆盖率结果
$summaryFile = "$reportPath/Summary.json"
if (-not (Test-Path $summaryFile)) {
    Write-Host "❌ 覆盖率摘要文件不存在" -ForegroundColor Red
    exit 1
}

$summary = Get-Content $summaryFile | ConvertFrom-Json
$lineCoverage = [math]::Round($summary.coverage.linecoverage, 2)
$branchCoverage = [math]::Round($summary.coverage.branchcoverage, 2)

# 显示测试结果摘要
Write-Host "`n" + "=" * 50 -ForegroundColor Cyan
Write-Host "📊 P3本地覆盖率验证结果" -ForegroundColor Cyan
Write-Host "=" * 50 -ForegroundColor Cyan

Write-Host "`n🧪 测试执行结果:" -ForegroundColor White
foreach ($result in $testResults) {
    $color = if ($result.Status -eq "PASS") { "Green" } else { "Red" }
    $icon = if ($result.Status -eq "PASS") { "✓" } else { "❌" }
    Write-Host "  $icon $($result.Module): $($result.Status)" -ForegroundColor $color
}

Write-Host "`n📈 覆盖率指标:" -ForegroundColor White
Write-Host "  行覆盖率 (Line): $lineCoverage%" -ForegroundColor $(if ($lineCoverage -ge $CoverageThreshold) { "Green" } else { "Red" })
Write-Host "  分支覆盖率 (Branch): $branchCoverage%" -ForegroundColor White
Write-Host "  目标阈值: $CoverageThreshold%" -ForegroundColor Yellow

# P3硬门槛检查
Write-Host "`n🎯 P3硬门槛检查:" -ForegroundColor White
if ($lineCoverage -ge $CoverageThreshold) {
    Write-Host "  ✅ 覆盖率达标: $lineCoverage% ≥ $CoverageThreshold%" -ForegroundColor Green
    Write-Host "  🚀 CI门禁预期: 通过" -ForegroundColor Green
    $exitCode = 0
} else {
    Write-Host "  ❌ 覆盖率不达标: $lineCoverage% < $CoverageThreshold%" -ForegroundColor Red
    Write-Host "  🚫 CI门禁预期: 失败" -ForegroundColor Red
    Write-Host "  💡 建议: 增加测试用例提升覆盖率" -ForegroundColor Yellow
    $exitCode = 1
}

# 显示报告位置
Write-Host "`n📄 详细报告:" -ForegroundColor White
Write-Host "  HTML报告: $reportPath/index.html" -ForegroundColor Gray
Write-Host "  文本摘要: $reportPath/Summary.txt" -ForegroundColor Gray

# 可选择打开报告
if ($OpenReport -and (Test-Path "$reportPath/index.html")) {
    Write-Host "`n🌐 打开HTML报告..." -ForegroundColor Cyan
    Start-Process "$reportPath/index.html"
}

Write-Host "`n" + "=" * 50 -ForegroundColor Cyan

exit $exitCode