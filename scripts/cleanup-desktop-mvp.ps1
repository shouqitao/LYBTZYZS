# Desktop层MVP功能清理脚本
# 用途：根据Issue #778清理超出MVP范围的功能代码
# 创建日期：2025-09-28

param(
    [switch]$WhatIf = $false,
    [switch]$BackupOnly = $false
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$desktopPath = Join-Path $projectRoot "src\Client\Desktop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Desktop层MVP功能清理脚本" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 创建备份目录
$backupDir = Join-Path $projectRoot "backup\desktop-mvp-cleanup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
if (-not (Test-Path $backupDir)) {
    New-Item -ItemType Directory -Path $backupDir | Out-Null
    Write-Host "创建备份目录: $backupDir" -ForegroundColor Green
}

# 定义需要备份和处理的文件
$filesToProcess = @(
    # 收费管理相关
    "Services\PermissionService.cs",
    
    # 库存管理相关
    "Core\Converters\StockStatusConverter.cs",
    "Core\Converters\PerformanceConverters.cs",
    "Core\Validation\CommonValidators.cs",
    "Modules\Herbs\Models\HerbItem.cs",
    
    # 统计报表相关
    "Core\Events\EnhancedEventAggregator.cs",
    "Core\Events\EventManager.cs",
    "Core\Models\Cache\CacheOptions.cs",
    "Core\Services\Configuration\ConfigurationManagerService.cs",
    "Core\Services\Configuration\FeatureToggleService.cs",
    
    # 离线模式相关
    "Core\Converters\BooleanToOnlineBrushConverter.cs",
    "Core\Converters\BooleanToOnlineStatusConverter.cs",
    "Core\Converters\Unified\StatusToColorConverter.cs",
    "Core\Converters\Unified\UnifiedBooleanConverter.cs",
    
    # 智能功能相关
    "Core\Managers\SearchManager.cs",
    "Shell\App.xaml.cs",
    "Modules\Consultation\ViewModels\ConsultationMainViewModel.cs"
)

# 备份文件
Write-Host "`n备份文件..." -ForegroundColor Yellow
foreach ($file in $filesToProcess) {
    $sourcePath = Join-Path $desktopPath $file
    if (Test-Path $sourcePath) {
        $backupPath = Join-Path $backupDir $file
        $backupFileDir = Split-Path -Parent $backupPath
        if (-not (Test-Path $backupFileDir)) {
            New-Item -ItemType Directory -Path $backupFileDir -Force | Out-Null
        }
        Copy-Item -Path $sourcePath -Destination $backupPath -Force
        Write-Host "  备份: $file" -ForegroundColor Gray
    }
}

if ($BackupOnly) {
    Write-Host "`n仅备份模式，未执行清理操作" -ForegroundColor Yellow
    exit 0
}

if ($WhatIf) {
    Write-Host "`n预览模式 - 以下操作不会实际执行：" -ForegroundColor Yellow
}

Write-Host "`n开始清理操作..." -ForegroundColor Green

# 1. 删除完整文件
$filesToDelete = @(
    "Core\Converters\StockStatusConverter.cs",
    "Core\Converters\BooleanToOnlineBrushConverter.cs",
    "Core\Converters\BooleanToOnlineStatusConverter.cs"
)

Write-Host "`n删除未使用的文件..." -ForegroundColor Yellow
foreach ($file in $filesToDelete) {
    $filePath = Join-Path $desktopPath $file
    if (Test-Path $filePath) {
        if (-not $WhatIf) {
            Remove-Item -Path $filePath -Force
            Write-Host "  已删除: $file" -ForegroundColor Red
        } else {
            Write-Host "  将删除: $file" -ForegroundColor Gray
        }
    }
}

# 2. 修改PermissionService.cs - 移除收费和库存权限
Write-Host "`n清理PermissionService.cs..." -ForegroundColor Yellow
$permissionFile = Join-Path $desktopPath "Services\PermissionService.cs"
if (Test-Path $permissionFile) {
    $content = Get-Content $permissionFile -Raw
    
    # 移除收费权限
    $content = $content -replace '"PaymentProcess",\s*"InvoiceManagement",\s*"RefundProcess",\s*\r?\n\s*"PaymentReports",\s*"CashierReports"', '// MVP阶段不实现收费管理功能'
    
    # 移除库存权限
    $content = $content -replace '"InventoryManagement",\s*', ''
    
    # 移除药房报表权限
    $content = $content -replace '"PharmacyReports",\s*', ''
    
    if (-not $WhatIf) {
        Set-Content -Path $permissionFile -Value $content -Encoding UTF8
        Write-Host "  已清理权限定义" -ForegroundColor Green
    } else {
        Write-Host "  将清理权限定义" -ForegroundColor Gray
    }
}

# 3. 清理HerbItem.cs中的库存相关代码
Write-Host "`n清理HerbItem.cs..." -ForegroundColor Yellow
$herbFile = Join-Path $desktopPath "Modules\Herbs\Models\HerbItem.cs"
if (Test-Path $herbFile) {
    $lines = Get-Content $herbFile
    $newLines = @()
    $skipLines = $false
    $lineNum = 0
    
    foreach ($line in $lines) {
        $lineNum++
        # 跳过库存相关属性定义（134-139, 248-303）
        if ($lineNum -eq 134) { $skipLines = $true }
        if ($lineNum -eq 140) { $skipLines = $false }
        if ($lineNum -eq 248) { $skipLines = $true }
        if ($lineNum -eq 304) { $skipLines = $false }
        
        # 跳过Stock赋值行
        if ($line -match "Stock\s*=") { continue }
        
        # 修改IsAvailable属性
        if ($line -match "IsAvailable.*HasStock") {
            $line = "    public bool IsAvailable => IsActive;"
        }
        
        if (-not $skipLines) {
            $newLines += $line
        }
    }
    
    if (-not $WhatIf) {
        Set-Content -Path $herbFile -Value $newLines -Encoding UTF8
        Write-Host "  已移除库存相关代码" -ForegroundColor Green
    } else {
        Write-Host "  将移除库存相关代码" -ForegroundColor Gray
    }
}

# 4. 简化统计功能
Write-Host "`n简化统计功能..." -ForegroundColor Yellow

# CacheOptions.cs - 默认禁用统计
$cacheOptionsFile = Join-Path $desktopPath "Core\Models\Cache\CacheOptions.cs"
if (Test-Path $cacheOptionsFile) {
    $content = Get-Content $cacheOptionsFile -Raw
    $content = $content -replace 'EnableStatistics\s*{\s*get;\s*set;\s*}\s*=\s*true;', 'EnableStatistics { get; set; } = false;'
    
    if (-not $WhatIf) {
        Set-Content -Path $cacheOptionsFile -Value $content -Encoding UTF8
        Write-Host "  已禁用缓存统计" -ForegroundColor Green
    } else {
        Write-Host "  将禁用缓存统计" -ForegroundColor Gray
    }
}

# 5. 清理智能功能相关注释
Write-Host "`n清理智能功能相关描述..." -ForegroundColor Yellow

$filesToCleanComments = @(
    @{Path = "Shell\App.xaml.cs"; Pattern = "智能模块加载|智能.*加载"; Replace = "模块加载"},
    @{Path = "Core\Managers\SearchManager.cs"; Pattern = "智能搜索功能"; Replace = "搜索功能"},
    @{Path = "Core\Services\Configuration\FeatureToggleService.cs"; Pattern = "SmartDiagnosis"; Remove = $true}
)

foreach ($item in $filesToCleanComments) {
    $filePath = Join-Path $desktopPath $item.Path
    if (Test-Path $filePath) {
        $content = Get-Content $filePath -Raw
        
        if ($item.Remove) {
            # 移除SmartDiagnosis功能块（106-116行）
            $content = $content -replace '(?s)new FeatureDefinition\s*\{\s*Name\s*=\s*"SmartDiagnosis"[^}]+\},?\s*', ''
        } else {
            $content = $content -replace $item.Pattern, $item.Replace
        }
        
        if (-not $WhatIf) {
            Set-Content -Path $filePath -Value $content -Encoding UTF8
            Write-Host "  已清理: $($item.Path)" -ForegroundColor Green
        } else {
            Write-Host "  将清理: $($item.Path)" -ForegroundColor Gray
        }
    }
}

# 6. 验证编译
Write-Host "`n验证清理结果..." -ForegroundColor Yellow

if (-not $WhatIf) {
    Write-Host "正在编译Desktop项目..." -ForegroundColor Cyan
    $buildResult = & dotnet build "$projectRoot\src\Client\Desktop\LYBT.Desktop.csproj" --no-restore 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "编译成功！清理操作完成。" -ForegroundColor Green
    } else {
        Write-Host "编译失败！请检查错误信息：" -ForegroundColor Red
        Write-Host $buildResult
        Write-Host "`n备份文件位于: $backupDir" -ForegroundColor Yellow
    }
} else {
    Write-Host "`n预览模式完成。使用不带 -WhatIf 参数重新运行以执行实际清理。" -ForegroundColor Yellow
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host " 清理脚本执行完成" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan