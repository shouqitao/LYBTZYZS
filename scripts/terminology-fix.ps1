# 凌隐宝堂中医诊所管理系统 - 术语修正脚本
# 用于批量修正文档中的术语错误

param(
    [switch]$WhatIf = $false,  # 仅显示将要修改的内容，不实际修改
    [switch]$Verbose = $false  # 显示详细信息
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " 术语修正工具 - 凌隐宝堂中医诊所系统" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

if ($WhatIf) {
    Write-Host "模拟模式：仅显示将要修改的内容" -ForegroundColor Yellow
}

# 定义术语替换规则
$replacements = @{
    # UltraThink误用修正
    "UltraThink双层架构" = "模块化双层架构"
    "UltraThink三层架构" = "标准三层架构"
    "UltraThink架构" = "模块化架构"
    "Desktop UltraThink" = "Desktop架构"
    "UltraThink重构" = "架构重构"
    "UltraThink分析" = "深度分析"
    "UltraThink简化" = "架构简化"
    "UltraThink模块化架构" = "模块化架构"
    "基于UltraThink" = "基于深度分析"
    "UltraThink方法" = "系统化方法"
    "UltraThink优化" = "架构优化"

    # 中文规范（仅在md文件中）
    "LYBTZYZS系统" = "凌隐宝堂中医诊所管理系统"
    "LYBT系统" = "凌隐宝堂系统"
    "LYBTZYZS项目" = "凌隐宝堂中医诊所项目"
    "LYBT项目" = "凌隐宝堂项目"
}

# 获取所有markdown文件
$files = Get-ChildItem -Path "docs" -Filter "*.md" -Recurse

$totalFiles = 0
$modifiedFiles = 0
$totalReplacements = 0

foreach ($file in $files) {
    $totalFiles++
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $originalContent = $content
    $fileReplacements = 0

    foreach ($key in $replacements.Keys) {
        $pattern = [regex]::Escape($key)
        $matches = [regex]::Matches($content, $pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)

        if ($matches.Count -gt 0) {
            if ($Verbose -or $WhatIf) {
                Write-Host "`n文件: $($file.FullName)" -ForegroundColor Green
                Write-Host "  找到 '$key' 出现 $($matches.Count) 次" -ForegroundColor Gray
            }

            $content = $content -replace $pattern, $replacements[$key]
            $fileReplacements += $matches.Count
            $totalReplacements += $matches.Count
        }
    }

    if ($fileReplacements -gt 0) {
        $modifiedFiles++

        if (-not $WhatIf) {
            # 保存文件（保持UTF8编码）
            [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.Encoding]::UTF8)
            if ($Verbose) {
                Write-Host "  ✓ 已修改并保存（$fileReplacements 处替换）" -ForegroundColor Green
            }
        } else {
            Write-Host "  → 将修改 $fileReplacements 处" -ForegroundColor Yellow
        }
    }
}

# 统计信息
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host " 修正完成" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  扫描文件: $totalFiles 个" -ForegroundColor White
Write-Host "  修改文件: $modifiedFiles 个" -ForegroundColor White
Write-Host "  替换总数: $totalReplacements 处" -ForegroundColor White

if ($WhatIf) {
    Write-Host "`n提示: 使用不带 -WhatIf 参数重新运行以实际修改文件" -ForegroundColor Yellow
}

Write-Host ""