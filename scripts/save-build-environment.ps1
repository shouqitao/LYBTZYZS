# ========================================
# UltraThink 环境状态保存脚本
# 职责单一：保存和恢复编译环境
# 代码干净：结构化的配置管理
# 性能出色：快速环境切换
# ========================================

$ErrorActionPreference = "Stop"
$ConfigPath = "$PSScriptRoot\..\build-environment.json"

function Save-BuildEnvironment {
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "   UltraThink 环境状态保存" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""

    $environment = @{
        Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        DotNetVersion = dotnet --version
        DotNetSdks = (dotnet --list-sdks) -split "`n"
        DotNetRuntimes = (dotnet --list-runtimes) -split "`n"
        NuGetSources = (dotnet nuget list source) -split "`n"
        EnvironmentVariables = @{
            DOTNET_ROOT = $env:DOTNET_ROOT
            DOTNET_CLI_TELEMETRY_OPTOUT = $env:DOTNET_CLI_TELEMETRY_OPTOUT
            ASPNETCORE_ENVIRONMENT = $env:ASPNETCORE_ENVIRONMENT
            PATH = $env:PATH
        }
        ProjectStructure = @{
            SolutionFiles = Get-ChildItem -Path "$PSScriptRoot\.." -Filter "*.sln" | Select-Object -ExpandProperty Name
            BackendProjects = Get-ChildItem -Path "$PSScriptRoot\..\src\Backend" -Recurse -Filter "*.csproj" | Select-Object -ExpandProperty FullName
            FrontendProjects = Get-ChildItem -Path "$PSScriptRoot\..\src\Frontend" -Recurse -Filter "*.csproj" | Select-Object -ExpandProperty FullName
            TestProjects = Get-ChildItem -Path "$PSScriptRoot\..\tests" -Recurse -Filter "*.csproj" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName
        }
        PackageVersions = @{}
        CompilerSettings = @{
            Configuration = "Debug"
            Platform = "Any CPU"
            Framework = "net8.0"
            TreatWarningsAsErrors = $false
            NoWarn = "CS1591;CS1572;CS1573"
        }
    }

    # 获取主要项目的包版本
    $mainProject = "$PSScriptRoot\..\src\Backend\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj"
    if (Test-Path $mainProject) {
        $packages = dotnet list $mainProject package --format json | ConvertFrom-Json
        if ($packages.projects) {
            foreach ($framework in $packages.projects[0].frameworks) {
                foreach ($package in $framework.topLevelPackages) {
                    $environment.PackageVersions[$package.id] = $package.resolvedVersion
                }
            }
        }
    }

    # 保存到JSON文件
    $environment | ConvertTo-Json -Depth 10 | Out-File -FilePath $ConfigPath -Encoding UTF8
    
    Write-Host "[成功] 环境状态已保存到: $ConfigPath" -ForegroundColor Green
    Write-Host ""
    Write-Host "保存的信息包括:" -ForegroundColor Yellow
    Write-Host "  - .NET SDK 版本: $($environment.DotNetVersion)"
    Write-Host "  - 解决方案文件: $($environment.ProjectStructure.SolutionFiles -join ', ')"
    Write-Host "  - 后端项目数: $($environment.ProjectStructure.BackendProjects.Count)"
    Write-Host "  - 前端项目数: $($environment.ProjectStructure.FrontendProjects.Count)"
    Write-Host "  - 测试项目数: $($environment.ProjectStructure.TestProjects.Count)"
    Write-Host "  - NuGet包数: $($environment.PackageVersions.Count)"
}

function Restore-BuildEnvironment {
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "   UltraThink 环境状态恢复" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""

    if (-not (Test-Path $ConfigPath)) {
        Write-Host "[错误] 未找到环境配置文件！" -ForegroundColor Red
        Write-Host "请先运行 Save-BuildEnvironment 保存环境状态。" -ForegroundColor Yellow
        return
    }

    $environment = Get-Content $ConfigPath | ConvertFrom-Json

    Write-Host "加载环境配置 (保存时间: $($environment.Timestamp))" -ForegroundColor Yellow
    Write-Host ""

    # 检查 .NET SDK 版本
    $currentVersion = dotnet --version
    if ($currentVersion -ne $environment.DotNetVersion) {
        Write-Host "[警告] .NET SDK 版本不匹配" -ForegroundColor Yellow
        Write-Host "  期望: $($environment.DotNetVersion)"
        Write-Host "  当前: $currentVersion"
    } else {
        Write-Host "[OK] .NET SDK 版本匹配" -ForegroundColor Green
    }

    # 设置环境变量
    if ($environment.EnvironmentVariables.ASPNETCORE_ENVIRONMENT) {
        $env:ASPNETCORE_ENVIRONMENT = $environment.EnvironmentVariables.ASPNETCORE_ENVIRONMENT
        Write-Host "[OK] 设置 ASPNETCORE_ENVIRONMENT = $env:ASPNETCORE_ENVIRONMENT" -ForegroundColor Green
    }

    # 恢复 NuGet 包
    Write-Host ""
    Write-Host "恢复 NuGet 包..." -ForegroundColor Yellow
    
    foreach ($sln in $environment.ProjectStructure.SolutionFiles) {
        $slnPath = "$PSScriptRoot\..\$sln"
        if (Test-Path $slnPath) {
            Write-Host "  恢复: $sln"
            dotnet restore $slnPath --force | Out-Null
        }
    }

    Write-Host ""
    Write-Host "[成功] 环境恢复完成！" -ForegroundColor Green
}

function Test-BuildEnvironment {
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "   UltraThink 环境测试" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""

    $tests = @()
    
    # 测试1: .NET SDK
    $sdkTest = @{
        Name = ".NET SDK"
        Status = "PASS"
        Details = dotnet --version
    }
    try {
        $version = dotnet --version
        if ([version]$version -lt [version]"8.0.0") {
            $sdkTest.Status = "FAIL"
            $sdkTest.Details = "需要 .NET 8.0 或更高版本"
        }
    } catch {
        $sdkTest.Status = "FAIL"
        $sdkTest.Details = "无法检测 .NET SDK"
    }
    $tests += $sdkTest

    # 测试2: 解决方案文件
    $slnTest = @{
        Name = "解决方案文件"
        Status = "PASS"
        Details = ""
    }
    $slnFiles = Get-ChildItem -Path "$PSScriptRoot\.." -Filter "*.sln"
    if ($slnFiles.Count -eq 0) {
        $slnTest.Status = "FAIL"
        $slnTest.Details = "未找到解决方案文件"
    } else {
        $slnTest.Details = "找到 $($slnFiles.Count) 个解决方案文件"
    }
    $tests += $slnTest

    # 测试3: 关键项目
    $projectTest = @{
        Name = "关键项目"
        Status = "PASS"
        Details = ""
    }
    $criticalProjects = @(
        "$PSScriptRoot\..\src\Backend\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj",
        "$PSScriptRoot\..\src\Backend\Services\LYBT.WebAPI\LYBT.WebAPI.csproj"
    )
    $missingProjects = @()
    foreach ($proj in $criticalProjects) {
        if (-not (Test-Path $proj)) {
            $missingProjects += (Split-Path $proj -Leaf)
        }
    }
    if ($missingProjects.Count -gt 0) {
        $projectTest.Status = "FAIL"
        $projectTest.Details = "缺失: $($missingProjects -join ', ')"
    } else {
        $projectTest.Details = "所有关键项目存在"
    }
    $tests += $projectTest

    # 测试4: NuGet 连接
    $nugetTest = @{
        Name = "NuGet 连接"
        Status = "PASS"
        Details = ""
    }
    try {
        $sources = dotnet nuget list source
        if ($sources -match "nuget.org") {
            $nugetTest.Details = "nuget.org 可访问"
        } else {
            $nugetTest.Status = "WARN"
            $nugetTest.Details = "未配置 nuget.org"
        }
    } catch {
        $nugetTest.Status = "FAIL"
        $nugetTest.Details = "无法访问 NuGet"
    }
    $tests += $nugetTest

    # 显示测试结果
    Write-Host "测试结果:" -ForegroundColor Yellow
    Write-Host ""
    
    foreach ($test in $tests) {
        $color = switch ($test.Status) {
            "PASS" { "Green" }
            "WARN" { "Yellow" }
            "FAIL" { "Red" }
            default { "Gray" }
        }
        
        $statusText = switch ($test.Status) {
            "PASS" { "[✓]" }
            "WARN" { "[!]" }
            "FAIL" { "[✗]" }
            default { "[?]" }
        }
        
        Write-Host "$statusText $($test.Name): $($test.Details)" -ForegroundColor $color
    }
    
    Write-Host ""
    $failCount = ($tests | Where-Object { $_.Status -eq "FAIL" }).Count
    if ($failCount -eq 0) {
        Write-Host "[成功] 所有环境测试通过！" -ForegroundColor Green
    } else {
        Write-Host "[警告] $failCount 个测试失败，请检查环境配置。" -ForegroundColor Red
    }
}

# 主菜单
function Show-Menu {
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "   UltraThink 环境管理工具" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "1. 保存当前环境状态"
    Write-Host "2. 恢复环境状态"
    Write-Host "3. 测试环境配置"
    Write-Host "4. 显示当前环境"
    Write-Host "0. 退出"
    Write-Host ""
    
    $choice = Read-Host "请选择操作 (0-4)"
    
    switch ($choice) {
        "1" { Save-BuildEnvironment }
        "2" { Restore-BuildEnvironment }
        "3" { Test-BuildEnvironment }
        "4" { 
            Write-Host ""
            Write-Host "当前环境信息:" -ForegroundColor Yellow
            Write-Host "  .NET 版本: $(dotnet --version)"
            Write-Host "  当前目录: $PWD"
            Write-Host "  脚本目录: $PSScriptRoot"
            if (Test-Path $ConfigPath) {
                $saved = Get-Content $ConfigPath | ConvertFrom-Json
                Write-Host "  上次保存: $($saved.Timestamp)"
            }
        }
        "0" { 
            Write-Host "退出环境管理工具。" -ForegroundColor Yellow
            exit 
        }
        default { 
            Write-Host "无效选项，请重试。" -ForegroundColor Red
            Show-Menu
        }
    }
    
    Write-Host ""
    Read-Host "按回车键继续..."
    Show-Menu
}

# 如果直接运行脚本，显示菜单
if ($MyInvocation.InvocationName -ne '.') {
    Show-Menu
}