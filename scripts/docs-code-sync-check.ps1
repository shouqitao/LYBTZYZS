# LYBTZYZS 文档与代码同步检查脚本
# 用途: 检查文档与实际代码的一致性

param(
    [string]$DocsPath = "docs",
    [string]$SrcPath = "src",
    [switch]$Verbose
)

Write-Host "🔍 LYBTZYZS 文档与代码同步检查开始" -ForegroundColor Green

$ErrorCount = 0
$WarningCount = 0

# 1. 检查架构文档与实际代码结构
Write-Host "`n🏗️ 检查架构文档与代码结构一致性..." -ForegroundColor Cyan

# 检查Client模块
$ClientDocsPath = Join-Path $DocsPath "state\architecture\client\README.md"
if (Test-Path $ClientDocsPath) {
    Write-Host "✅ Client架构文档存在" -ForegroundColor Green

    # 检查模块列表
    $ClientModules = @("Auth", "Users", "Patients", "MedicalCase", "Consultation", "Prescriptions", "Herbs", "Formula")
    $ActualClientModules = Get-ChildItem -Path (Join-Path $SrcPath "Client\Desktop\Modules") -Directory | Select-Object -ExpandProperty Name

    foreach ($module in $ClientModules) {
        if ($module -in $ActualClientModules) {
            Write-Host "✅ Client模块存在: $module" -ForegroundColor Green
        } else {
            Write-Host "❌ Client模块缺失: $module" -ForegroundColor Red
            $ErrorCount++
        }
    }
} else {
    Write-Host "❌ Client架构文档缺失" -ForegroundColor Red
    $ErrorCount++
}

# 检查Server模块
$ServerDocsPath = Join-Path $DocsPath "state\architecture\server\README.md"
if (Test-Path $ServerDocsPath) {
    Write-Host "✅ Server架构文档存在" -ForegroundColor Green

    # 检查模块列表
    $ServerModules = @("Auth", "Users", "Patients", "MedicalCase", "Consultation", "Prescriptions", "Herbs", "Formula")
    $ActualServerModules = Get-ChildItem -Path (Join-Path $SrcPath "Server\Modules") -Directory | Select-Object -ExpandProperty Name

    foreach ($module in $ServerModules) {
        if ($module -in $ActualServerModules) {
            Write-Host "✅ Server模块存在: $module" -ForegroundColor Green
        } else {
            Write-Host "❌ Server模块缺失: $module" -ForegroundColor Red
            $ErrorCount++
        }
    }
} else {
    Write-Host "❌ Server架构文档缺失" -ForegroundColor Red
    $ErrorCount++
}

# 2. 检查API文档与实际API一致性
Write-Host "`n🌐 检查API文档与实际API一致性..." -ForegroundColor Cyan

$ApiDocsPath = Join-Path $DocsPath "reference\api"
if (Test-Path $ApiDocsPath) {
    $ApiDocFiles = Get-ChildItem -Path $ApiDocsPath -Filter "*.md" -Recurse

    foreach ($docFile in $ApiDocFiles) {
        $docContent = Get-Content $docFile.FullName -Raw
        $apiName = $docFile.BaseName

        # 检查对应的Controller是否存在
        $ControllerPath = Join-Path $SrcPath "Server\Modules\$apiName\Controllers"
        if (Test-Path $ControllerPath) {
            Write-Host "✅ API文档与Controller匹配: $apiName" -ForegroundColor Green
        } else {
            Write-Host "⚠️ API文档无对应Controller: $apiName" -ForegroundColor Yellow
            $WarningCount++
        }
    }
} else {
    Write-Host "⚠️ API文档目录不存在" -ForegroundColor Yellow
    $WarningCount++
}

# 3. 检查技术栈文档
Write-Host "`n🔧 检查技术栈文档与项目一致性..." -ForegroundColor Cyan

$TechStackDoc = Join-Path $DocsPath "reference\technology-stack.md"
if (Test-Path $TechStackDoc) {
    $ProjectFiles = @(
        "LYBT.Desktop.sln",
        "LYBT.Server.sln",
        "global.json"
    )

    foreach ($file in $ProjectFiles) {
        if (Test-Path $file) {
            Write-Host "✅ 项目文件存在: $file" -ForegroundColor Green
        } else {
            Write-Host "❌ 项目文件缺失: $file" -ForegroundColor Red
            $ErrorCount++
        }
    }

    # 检查包引用
    $DesktopProject = Join-Path $SrcPath "Client\LYBT.Desktop.csproj"
    if (Test-Path $DesktopProject) {
        $ProjectContent = Get-Content $DesktopProject -Raw
        $ExpectedPackages = @("Prism.DryIoc", "Microsoft.AspNetCore.SignalR.Client", "Refit")

        foreach ($package in $ExpectedPackages) {
            if ($ProjectContent -match $package) {
                Write-Host "✅ Desktop项目包引用: $package" -ForegroundColor Green
            } else {
                Write-Host "⚠️ Desktop项目缺失包: $package" -ForegroundColor Yellow
                $WarningCount++
            }
        }
    }
}

# 4. 检查开发指南与实际开发流程
Write-Host "`n📚 检查开发指南与实际开发流程一致性..." -ForegroundColor Cyan

$DevelopmentGuidesPath = Join-Path $DocsPath "support\guides"
if (Test-Path $DevelopmentGuidesPath) {
    $GuideFiles = Get-ChildItem -Path $DevelopmentGuidesPath -Filter "*.md"

    foreach ($guide in $GuideFiles) {
        $guideContent = Get-Content $guide.FullName -Raw

        # 检查提到的工具是否可用
        if ($guideContent -match "dotnet build") {
            if (Get-Command dotnet -ErrorAction SilentlyContinue) {
                Write-Host "✅ dotnet命令可用: $guide" -ForegroundColor Green
            } else {
                Write-Host "❌ dotnet命令不可用: $guide" -ForegroundColor Red
                $ErrorCount++
            }
        }
    }
}

# 5. 检查测试文档与实际测试
Write-Host "`n🧪 检查测试文档与实际测试一致性..." -ForegroundColor Cyan

$TestingDocsPath = Join-Path $DocsPath "support\testing"
if (Test-Path $TestingDocsPath) {
    $TestProjects = Get-ChildItem -Path (Join-Path $SrcPath "Tests") -Filter "*.Tests.csproj"

    if ($TestProjects) {
        Write-Host "✅ 测试项目存在: $($TestProjects.Count)个" -ForegroundColor Green

        # 检查测试文档是否与实际测试项目匹配
        foreach ($testProject in $TestProjects) {
            $testName = $testProject.BaseName
            $TestDoc = Join-Path $TestingDocsPath "$testName.md"

            if (Test-Path $TestDoc) {
                Write-Host "✅ 测试文档存在: $testName" -ForegroundColor Green
            } elseif ($Verbose) {
                Write-Host "ℹ️ 测试文档可选: $testName" -ForegroundColor Gray
            }
        }
    } else {
        Write-Host "⚠️ 未发现测试项目" -ForegroundColor Yellow
        $WarningCount++
    }
}

# 6. 检查版本信息一致性
Write-Host "`n📋 检查版本信息一致性..." -ForegroundColor Cyan

$VersionFiles = @(
    "src/Server/appsettings.json",
    "src/Client/appsettings.json",
    "docs/state/README.md"
)

$Versions = @()
foreach ($file in $VersionFiles) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        if ($content -match "version.*?([0-9]+\.[0-9]+\.[0-9]+)") {
            $Versions += $matches[1]
            Write-Host "✅ 版本信息: $file -> $($matches[1])" -ForegroundColor Green
        }
    }
}

# 检查版本一致性
if ($Versions.Count -gt 1) {
    $UniqueVersions = $Versions | Get-Unique
    if ($UniqueVersions.Count -eq 1) {
        Write-Host "✅ 版本信息一致" -ForegroundColor Green
    } else {
        Write-Host "❌ 版本信息不一致" -ForegroundColor Red
        $UniqueVersions | ForEach-Object { Write-Host "   $_" -ForegroundColor Red }
        $ErrorCount++
    }
}

# 7. 生成同步报告
Write-Host "`n📋 文档与代码同步检查报告" -ForegroundColor Cyan
Write-Host "==============================" -ForegroundColor Gray
Write-Host "错误数量: $ErrorCount" -ForegroundColor $(if($ErrorCount -gt 0){'Red'}else{'Green'})
Write-Host "警告数量: $WarningCount" -ForegroundColor $(if($WarningCount -gt 0){'Yellow'}else{'Green'})

# 8. 计算同步评分
$TotalChecks = $ErrorCount + $WarningCount
$SyncScore = [math]::Max(0, 100 - ($ErrorCount * 15) - ($WarningCount * 3))
Write-Host "`n📊 文档与代码同步评分: $SyncScore/100" -ForegroundColor $(if($SyncScore -ge 90){'Green'}elseif($SyncScore -ge 70){'Yellow'}else{'Red'})

# 9. 返回退出码
if ($ErrorCount -gt 0) {
    Write-Host "`n❌ 发现文档与代码不一致问题" -ForegroundColor Red
    exit 1
} elseif ($WarningCount -gt 0) {
    Write-Host "`n⚠️ 文档与代码基本一致，但有警告项" -ForegroundColor Yellow
    exit 0
} else {
    Write-Host "`n✅ 文档与代码完全同步" -ForegroundColor Green
    exit 0
}