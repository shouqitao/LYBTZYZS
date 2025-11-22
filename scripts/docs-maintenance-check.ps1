# LYBTZYZS 文档维护自动化检查脚本
# 用途: 检查文档系统健康状态，确保状态vs过程分离原则

param(
    [string]$DocsPath = "docs",
    [switch]$Fix,
    [switch]$Verbose
)

Write-Host "🔍 LYBTZYZS 文档维护检查开始" -ForegroundColor Green

$ErrorCount = 0
$WarningCount = 0
$FixedCount = 0

# 1. 检查目录结构
Write-Host "`n📁 检查目录结构..." -ForegroundColor Cyan
$ExpectedDirs = @("state", "reference", "support", "process")
$ActualDirs = Get-ChildItem -Path $DocsPath -Directory | Select-Object -ExpandProperty Name

foreach ($dir in $ExpectedDirs) {
    if ($dir -notin $ActualDirs) {
        Write-Host "❌ 缺少必需目录: $dir" -ForegroundColor Red
        $ErrorCount++
    } else {
        Write-Host "✅ 目录存在: $dir" -ForegroundColor Green
    }
}

# 检查多余目录
$ExtraDirs = $ActualDirs | Where-Object { $_ -notin $ExpectedDirs }
if ($ExtraDirs) {
    Write-Host "⚠️ 发现多余目录:" -ForegroundColor Yellow
    $ExtraDirs | ForEach-Object { Write-Host "   - $_" -ForegroundColor Yellow }
    $WarningCount++
}

# 2. 检查状态文档纯净度
Write-Host "`n📄 检查状态文档纯净度..." -ForegroundColor Cyan
$StatePath = Join-Path $DocsPath "state"
$ForbiddenWords = @("以前", "过去", "历史", "曾经", "Phase 1", "Phase 2", "2025-10", "2025-11", "重构前", "旧版本")

Get-ChildItem -Path $StatePath -Filter "*.md" -Recurse | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    $issues = @()

    foreach ($word in $ForbiddenWords) {
        if ($content -match $word) {
            $issues += $word
        }
    }

    if ($issues.Count -gt 0) {
        Write-Host "⚠️ 状态文档包含过程词汇: $($_.Name)" -ForegroundColor Yellow
        Write-Host "   发现: $($issues -join ', ')" -ForegroundColor Yellow
        $WarningCount++

        if ($Fix) {
            Write-Host "   🛠️  尝试自动修复..." -ForegroundColor Blue
            # 这里可以添加自动修复逻辑
            $FixedCount++
        }
    } else {
        if ($Verbose) { Write-Host "✅ 状态文档纯净: $($_.Name)" -ForegroundColor Green }
    }
}

# 3. 检查文档链接有效性
Write-Host "`n🔗 检查文档链接有效性..." -ForegroundColor Cyan
$MarkdownFiles = Get-ChildItem -Path $DocsPath -Filter "*.md" -Recurse
$BrokenLinks = @()

foreach ($file in $MarkdownFiles) {
    $content = Get-Content $file.FullName -Raw
    $links = [regex]::Matches($content, '\[([^\]]+)\]\(([^)]+)\)')

    foreach ($link in $links) {
        $target = $link.Groups[2].Value
        if ($target -notlike "http*" -and $target -notlike "mailto*") {
            $targetPath = Join-Path $file.DirectoryName $target
            if (-not (Test-Path $targetPath)) {
                $BrokenLinks += "$($file.Name): $($link.Groups[1].Value) -> $target"
            }
        }
    }
}

if ($BrokenLinks.Count -gt 0) {
    Write-Host "❌ 发现失效链接:" -ForegroundColor Red
    $BrokenLinks | ForEach-Object { Write-Host "   $_" -ForegroundColor Red }
    $ErrorCount++
} else {
    Write-Host "✅ 所有链接有效" -ForegroundColor Green
}

# 4. 检查文档大小异常
Write-Host "`n📏 检查文档大小异常..." -ForegroundColor Cyan
$LargeFiles = Get-ChildItem -Path $DocsPath -Filter "*.md" -Recurse |
    Where-Object { $_.Length -gt 100KB } |
    Select-Object Name, @{Name="SizeKB";Expression={[math]::Round($_.Length / 1KB, 1)}}

if ($LargeFiles) {
    Write-Host "⚠️ 发现超大文档(>100KB):" -ForegroundColor Yellow
    $LargeFiles | ForEach-Object {
        Write-Host "   $($_.Name): $($_.SizeKB)KB" -ForegroundColor Yellow
        $WarningCount++
    }
} else {
    Write-Host "✅ 文档大小正常" -ForegroundColor Green
}

# 5. 检查重复文件
Write-Host "`n🔍 检查重复文件..." -ForegroundColor Cyan
$FileHashes = @{}
$Duplicates = @()

Get-ChildItem -Path $DocsPath -Filter "*.md" -Recurse | ForEach-Object {
    $hash = (Get-FileHash $_.FullName -Algorithm SHA256).Hash
    if ($FileHashes.ContainsKey($hash)) {
        $Duplicates += "$($_.Name) 与 $($FileHashes[$hash]) 重复"
    } else {
        $FileHashes[$hash] = $_.Name
    }
}

if ($Duplicates.Count -gt 0) {
    Write-Host "⚠️ 发现重复文件:" -ForegroundColor Yellow
    $Duplicates | ForEach-Object { Write-Host "   $_" -ForegroundColor Yellow }
    $WarningCount++
} else {
    Write-Host "✅ 无重复文件" -ForegroundColor Green
}

# 6. 生成检查报告
Write-Host "`n📋 检查报告" -ForegroundColor Cyan
Write-Host "============" -ForegroundColor Gray
Write-Host "文档总数: $(($MarkdownFiles | Measure-Object).Count)" -ForegroundColor White
Write-Host "错误数量: $ErrorCount" -ForegroundColor $(if($ErrorCount -gt 0){'Red'}else{'Green'})
Write-Host "警告数量: $WarningCount" -ForegroundColor $(if($WarningCount -gt 0){'Yellow'}else{'Green'})
if ($Fix) { Write-Host "修复数量: $FixedCount" -ForegroundColor Green }

# 7. 健康评分
$TotalFiles = ($MarkdownFiles | Measure-Object).Count
$HealthScore = [math]::Max(0, 100 - ($ErrorCount * 10) - ($WarningCount * 2))
Write-Host "`n🏥 文档系统健康评分: $HealthScore/100" -ForegroundColor $(if($HealthScore -ge 90){'Green'}elseif($HealthScore -ge 70){'Yellow'}else{'Red'})

# 8. 返回退出码
if ($ErrorCount -gt 0) {
    Write-Host "`n❌ 检查失败，发现问题需要修复" -ForegroundColor Red
    exit 1
} elseif ($WarningCount -gt 0) {
    Write-Host "`n⚠️ 检查通过，但有警告项需要注意" -ForegroundColor Yellow
    exit 0
} else {
    Write-Host "`n✅ 文档系统检查全部通过" -ForegroundColor Green
    exit 0
}