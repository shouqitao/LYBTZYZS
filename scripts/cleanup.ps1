# LYBTZYZS 项目清理脚本
# 用于清理项目中的临时文件和构建产物

param(
    [switch]$WhatIf = $false,  # 仅显示将要删除的文件，不实际删除
    [switch]$Verbose = $false  # 显示详细信息
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " LYBTZYZS 项目清理工具" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

if ($WhatIf) {
    Write-Host "模拟模式：仅显示将要删除的文件" -ForegroundColor Yellow
}

# 定义要清理的文件模式
$patterns = @(
    # 构建日志
    "*.log",
    "build.log",
    "desktop-build*.log",
    "desktop-warnings.log",
    "*_build_output.txt",

    # 备份文件
    "*.bak",
    "*.bak.*",
    "*.backup",
    "*_backup.*",

    # 临时文件
    "*.tmp",
    "*.temp",
    "temp_*.txt",
    "nul",

    # IDE用户配置
    "*.slnLaunch.user",

    # MCP相关
    "mcp-commands.log"
)

# 定义要清理的目录
$directories = @(
    ".vs",
    ".idea",
    "temp",
    "logs"
)

$totalCleaned = 0
$totalSize = 0

Write-Host "`n清理文件..." -ForegroundColor Green

# 清理文件
foreach ($pattern in $patterns) {
    $files = Get-ChildItem -Path . -Filter $pattern -Recurse -File -ErrorAction SilentlyContinue |
             Where-Object { $_.FullName -notmatch "\\(node_modules|packages|bin|obj)\\" }

    foreach ($file in $files) {
        $size = $file.Length
        $totalSize += $size
        $totalCleaned++

        if ($Verbose -or $WhatIf) {
            $sizeKB = [math]::Round($size / 1KB, 2)
            Write-Host "  删除: $($file.FullName) (${sizeKB}KB)" -ForegroundColor Gray
        }

        if (-not $WhatIf) {
            Remove-Item $file.FullName -Force -ErrorAction SilentlyContinue
        }
    }
}

Write-Host "`n清理目录..." -ForegroundColor Green

# 清理目录
foreach ($dir in $directories) {
    $dirs = Get-ChildItem -Path . -Filter $dir -Recurse -Directory -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -notmatch "\\(node_modules|packages)\\" }

    foreach ($d in $dirs) {
        if ($Verbose -or $WhatIf) {
            Write-Host "  删除目录: $($d.FullName)" -ForegroundColor Gray
        }

        if (-not $WhatIf) {
            Remove-Item $d.FullName -Recurse -Force -ErrorAction SilentlyContinue
        }
        $totalCleaned++
    }
}

# 统计信息
$totalSizeMB = [math]::Round($totalSize / 1MB, 2)

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host " 清理完成" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  清理项目: $totalCleaned 个" -ForegroundColor White
Write-Host "  释放空间: ${totalSizeMB} MB" -ForegroundColor White

if ($WhatIf) {
    Write-Host "`n提示: 使用不带 -WhatIf 参数重新运行以实际删除文件" -ForegroundColor Yellow
}

Write-Host ""