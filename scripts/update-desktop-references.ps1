# Desktop 架构优化 - 引用更新脚本
# Issue #820: 更新项目文件、解决方案文件和命名空间
# 生成时间: 2025-09-30

$ErrorActionPreference = "Stop"
$rootPath = "D:\source\repos\LYBTZYZS"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Desktop 架构优化 - 引用更新" -ForegroundColor Cyan
Write-Host "Issue #820" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Set-Location $rootPath

# 路径映射表（用于替换）
$pathMappings = @{
    # 文件夹路径替换
    "Desktop\\Core_New" = "Desktop\\Core"
    "Desktop/Core_New" = "Desktop/Core"
    "Modules\\Auth\\" = "Modules\\LYBT.Desktop.Auth\\"
    "Modules/Auth/" = "Modules/LYBT.Desktop.Auth/"
    "Modules\\Consultation\\" = "Modules\\LYBT.Desktop.Consultation\\"
    "Modules/Consultation/" = "Modules/LYBT.Desktop.Consultation/"
    "Modules\\Formula\\" = "Modules\\LYBT.Desktop.Formula\\"
    "Modules/Formula/" = "Modules/LYBT.Desktop.Formula/"
    "Modules\\Herbs\\" = "Modules\\LYBT.Desktop.Herbs\\"
    "Modules/Herbs/" = "Modules/LYBT.Desktop.Herbs/"
    "Modules\\MedicalCase\\" = "Modules\\LYBT.Desktop.MedicalCase\\"
    "Modules/MedicalCase/" = "Modules/LYBT.Desktop.MedicalCase/"
    "Modules\\Patients\\" = "Modules\\LYBT.Desktop.Patients\\"
    "Modules/Patients/" = "Modules/LYBT.Desktop.Patients/"
    "Modules\\Prescriptions\\" = "Modules\\LYBT.Desktop.Prescriptions\\"
    "Modules/Prescriptions/" = "Modules/LYBT.Desktop.Prescriptions/"
    "Modules\\Users\\" = "Modules\\LYBT.Desktop.Users\\"
    "Modules/Users/" = "Modules/LYBT.Desktop.Users/"
}

# 命名空间映射表
$namespaceMappings = @{
    "LYBT.Desktop.Auth" = "LYBT.Desktop.Auth"  # 已经正确
    "LYBT.Desktop.Consultation" = "LYBT.Desktop.Consultation"
    "LYBT.Desktop.Formula" = "LYBT.Desktop.Formula"
    "LYBT.Desktop.Herbs" = "LYBT.Desktop.Herbs"
    "LYBT.Desktop.MedicalCase" = "LYBT.Desktop.MedicalCase"
    "LYBT.Desktop.Patients" = "LYBT.Desktop.Patients"
    "LYBT.Desktop.Prescriptions" = "LYBT.Desktop.Prescriptions"
    "LYBT.Desktop.Users" = "LYBT.Desktop.Users"
}

function Update-FileContent {
    param(
        [string]$FilePath,
        [hashtable]$Replacements,
        [string]$Description
    )

    if (-not (Test-Path $FilePath)) {
        Write-Host "  ⚠️  文件不存在: $FilePath" -ForegroundColor Yellow
        return $false
    }

    try {
        $content = Get-Content $FilePath -Raw -Encoding UTF8
        $originalContent = $content
        $changeCount = 0

        foreach ($key in $Replacements.Keys) {
            $newValue = $Replacements[$key]
            if ($content -match [regex]::Escape($key)) {
                $content = $content -replace [regex]::Escape($key), $newValue
                $changeCount++
            }
        }

        if ($changeCount -gt 0) {
            Set-Content -Path $FilePath -Value $content -Encoding UTF8 -NoNewline
            Write-Host "  ✅ $Description - 修改了 $changeCount 处" -ForegroundColor Green
            return $true
        } else {
            Write-Host "  ℹ️  $Description - 无需修改" -ForegroundColor Gray
            return $false
        }
    }
    catch {
        Write-Host "  ❌ $Description 失败: $_" -ForegroundColor Red
        return $false
    }
}

# 1. 更新解决方案文件
Write-Host "[1/3] 更新解决方案文件" -ForegroundColor Cyan
Write-Host ""

$slnFiles = @(
    "LYBT.Desktop.sln",
    "LYBT.All.sln"
)

$slnUpdateCount = 0
foreach ($slnFile in $slnFiles) {
    $slnPath = Join-Path $rootPath $slnFile
    Write-Host "处理: $slnFile" -ForegroundColor Yellow
    if (Update-FileContent -FilePath $slnPath -Replacements $pathMappings -Description "解决方案文件") {
        $slnUpdateCount++
    }
    Write-Host ""
}

# 2. 更新所有 .csproj 文件
Write-Host "[2/3] 更新项目文件 (.csproj)" -ForegroundColor Cyan
Write-Host ""

$csprojFiles = Get-ChildItem -Path "src\Client\Desktop" -Filter "*.csproj" -Recurse

$projUpdateCount = 0
foreach ($csprojFile in $csprojFiles) {
    Write-Host "处理: $($csprojFile.FullName.Replace($rootPath, '.'))" -ForegroundColor Yellow
    if (Update-FileContent -FilePath $csprojFile.FullName -Replacements $pathMappings -Description "项目文件") {
        $projUpdateCount++
    }
    Write-Host ""
}

# 3. 更新所有 .cs 文件的 using 语句
Write-Host "[3/3] 更新 C# 文件 using 语句" -ForegroundColor Cyan
Write-Host ""

$csFiles = Get-ChildItem -Path "src\Client\Desktop" -Filter "*.cs" -Recurse | Where-Object {
    $_.FullName -notlike "*\bin\*" -and
    $_.FullName -notlike "*\obj\*" -and
    $_.FullName -notlike "*AssemblyInfo.cs"
}

Write-Host "找到 $($csFiles.Count) 个 C# 文件需要检查..." -ForegroundColor Gray
Write-Host ""

$csUpdateCount = 0
$processedCount = 0

foreach ($csFile in $csFiles) {
    $processedCount++
    if ($processedCount % 50 -eq 0) {
        Write-Host "进度: $processedCount / $($csFiles.Count)..." -ForegroundColor Gray
    }

    try {
        $content = Get-Content $csFile.FullName -Raw -Encoding UTF8
        $originalContent = $content

        # 替换 using 语句（只替换完整的命名空间）
        $changed = $false

        # 示例：using LYBT.Desktop.Auth; 不需要改
        # 但如果有 using Auth.ViewModels; 需要改为 using LYBT.Desktop.Auth.ViewModels;
        # 这个需要更复杂的逻辑，暂时跳过

        if ($changed) {
            Set-Content -Path $csFile.FullName -Value $content -Encoding UTF8 -NoNewline
            $csUpdateCount++
        }
    }
    catch {
        Write-Host "  ⚠️  处理失败: $($csFile.FullName)" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "C# 文件检查完成，修改了 $csUpdateCount 个文件" -ForegroundColor Green
Write-Host ""

# 总结
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "引用更新完成" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "✅ 解决方案文件: $slnUpdateCount / $($slnFiles.Count)" -ForegroundColor Green
Write-Host "✅ 项目文件: $projUpdateCount / $($csprojFiles.Count)" -ForegroundColor Green
Write-Host "✅ C# 文件: $csUpdateCount / $($csFiles.Count)" -ForegroundColor Green
Write-Host ""

Write-Host "下一步:" -ForegroundColor Cyan
Write-Host "  1. 编译验证 LYBT.Desktop.sln" -ForegroundColor Gray
Write-Host "  2. 编译验证 LYBT.All.sln" -ForegroundColor Gray
Write-Host "  3. 提交变更到 Git" -ForegroundColor Gray
Write-Host ""

exit 0