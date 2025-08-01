# =====================================================================
# LYBTZYZS 项目构建输出清理脚本 (PowerShell版本)
# 用途：清理所有项目的bin/obj等构建输出文件，保持项目目录干净整洁
# 版本：v1.0
# 创建日期：2025-08-01
# =====================================================================

param(
    [switch]$Verbose = $false
)

function Write-Status {
    param([string]$Message, [string]$Type = "Info")
    
    $color = switch ($Type) {
        "Success" { "Green" }
        "Warning" { "Yellow" }
        "Error" { "Red" }
        default { "White" }
    }
    
    Write-Host $Message -ForegroundColor $color
}

function Remove-DirectoryIfExists {
    param([string]$Path)
    
    if (Test-Path $Path) {
        Remove-Item -Path $Path -Recurse -Force -ErrorAction SilentlyContinue
        if ($Verbose) {
            Write-Status "    已删除: $Path" "Success"
        }
        return $true
    }
    return $false
}

# 获取脚本所在目录的上级目录作为项目根目录
$projectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $projectRoot

Write-Host ""
Write-Status "======================================"
Write-Status " LYBTZYZS 构建输出清理工具 (PowerShell)"
Write-Status "======================================"
Write-Host ""

try {
    # 清理Backend解决方案
    Write-Status "[1/4] 正在清理Backend解决方案输出..."
    Set-Location "src\Backend"
    $result = Start-Process -FilePath "dotnet" -ArgumentList "clean --verbosity quiet" -Wait -PassThru -NoNewWindow
    if ($result.ExitCode -ne 0) {
        throw "Backend清理失败"
    }
    Write-Status "     ✓ Backend清理完成" "Success"
    
    # 清理Frontend解决方案
    Set-Location "..\Frontend"
    Write-Status "[2/4] 正在清理Frontend解决方案输出..."
    $result = Start-Process -FilePath "dotnet" -ArgumentList "clean --verbosity quiet" -Wait -PassThru -NoNewWindow
    if ($result.ExitCode -ne 0) {
        throw "Frontend清理失败"
    }
    Write-Status "     ✓ Frontend清理完成" "Success"
    
    # 回到项目根目录
    Set-Location $projectRoot
    
    # 清理临时构建目录
    Write-Status "[3/4] 正在清理临时构建目录..."
    $tempDir = Join-Path $projectRoot "BIN\temp"
    if (Remove-DirectoryIfExists $tempDir) {
        Write-Status "     ✓ 临时目录已清理" "Success"
    } else {
        Write-Status "     ✓ 无需清理临时目录" "Success"
    }
    
    # 清理历史构建输出
    Write-Status "[4/4] 正在清理历史构建输出..."
    $cleanedCount = 0
    
    # 查找并删除所有bin和obj目录
    Get-ChildItem -Path $projectRoot -Recurse -Directory | Where-Object { 
        $_.Name -eq "bin" -or $_.Name -eq "obj" 
    } | ForEach-Object {
        if (Remove-DirectoryIfExists $_.FullName) {
            $cleanedCount++
        }
    }
    
    if ($cleanedCount -gt 0) {
        Write-Status "     ✓ 已清理 $cleanedCount 个历史构建目录" "Success"
    } else {
        Write-Status "     ✓ 无需清理历史构建目录" "Success"
    }
    
    Write-Host ""
    Write-Status "======================================"
    Write-Status " 清理完成！项目目录已经干净整洁"
    Write-Status "======================================"
    Write-Host ""
    Write-Status "提示：下次构建时，所有输出将按新的目录结构生成："
    Write-Status "  - WebAPI 输出到: BIN\LybtWebApi"
    Write-Status "  - WPF桌面端输出到: BIN\LybtDesktop"
    Write-Status "  - 其他项目输出到: BIN\temp"
    Write-Host ""
    
} catch {
    Write-Host ""
    Write-Status "======================================"
    Write-Status " 清理过程中发生错误！"
    Write-Status " 错误信息: $($_.Exception.Message)"
    Write-Status "======================================"
    Write-Host ""
    exit 1
}

if (-not $env:AUTOMATED) {
    Read-Host "按任意键继续..."
}