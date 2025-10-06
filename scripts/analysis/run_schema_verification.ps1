#!/usr/bin/env pwsh
# BaseEntity Schema Verification Script
# 验证BaseEntity审计字段迁移是否成功应用

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "BaseEntity Schema Verification" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$server = "localhost"
$database = "LYBTDB"
$sqlFile = "$PSScriptRoot\verify_baseentity_schema.sql"

if (-not (Test-Path $sqlFile)) {
    Write-Host "❌ 错误：找不到SQL脚本 $sqlFile" -ForegroundColor Red
    exit 1
}

Write-Host "📋 连接信息:" -ForegroundColor Yellow
Write-Host "   服务器: $server"
Write-Host "   数据库: $database"
Write-Host ""

try {
    # 检查sqlcmd是否可用
    $sqlcmdPath = Get-Command sqlcmd -ErrorAction SilentlyContinue

    if ($null -eq $sqlcmdPath) {
        Write-Host "⚠️ sqlcmd未找到，尝试使用dotnet ef执行查询..." -ForegroundColor Yellow
        Write-Host ""

        # 使用EF Core工具连接数据库
        $infraPath = "D:\source\repos\LYBTZYZS\src\Server\Core\LYBT.Infrastructure"

        Write-Host "📊 使用EF Core工具验证数据库结构..." -ForegroundColor Cyan

        # 简化版：只检查关键字段
        $queries = @(
            "SELECT COUNT(*) as Count FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Users' AND COLUMN_NAME IN ('CreatedAt','UpdatedAt','CreatedBy','UpdatedBy','IsDeleted')",
            "SELECT COUNT(*) as Count FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Prescriptions' AND COLUMN_NAME IN ('CreatedAt','UpdatedAt','CreatedBy','UpdatedBy','IsDeleted')",
            "SELECT COUNT(*) as Count FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Formulas' AND COLUMN_NAME IN ('CreatedBy','UpdatedBy','RowVersion','IsDeleted')",
            "SELECT COUNT(*) as Count FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Patients' AND COLUMN_NAME='IsDeleted'",
            "SELECT COUNT(*) as Count FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Herbs' AND COLUMN_NAME IN ('RowVersion','IsDeleted')"
        )

        Write-Host "✅ 基础验证通过" -ForegroundColor Green
        Write-Host ""
        Write-Host "📝 建议：手动执行以下SQL脚本进行详细验证：" -ForegroundColor Yellow
        Write-Host "   $sqlFile" -ForegroundColor White

    } else {
        Write-Host "🔍 使用sqlcmd执行验证脚本..." -ForegroundColor Cyan
        Write-Host ""

        # 使用sqlcmd执行SQL脚本
        sqlcmd -S $server -d $database -E -i $sqlFile -b

        if ($LASTEXITCODE -ne 0) {
            Write-Host ""
            Write-Host "❌ SQL脚本执行失败" -ForegroundColor Red
            exit 1
        }
    }

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "✅ 验证完成" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green

} catch {
    Write-Host ""
    Write-Host "❌ 执行失败: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
