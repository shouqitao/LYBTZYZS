# UltraThink代码质量门禁检查脚本
# 基于重构经验建立的质量标准，防止代码质量倒退

param(
    [switch]$Detailed,
    [int]$MaxHelperLines = 500,
    [int]$MaxServiceLines = 300,
    [int]$MaxControllerLines = 200
)

$ErrorActionPreference = "Stop"
$issues = @()

Write-Host "🔍 开始UltraThink代码质量检查..." -ForegroundColor Green

# 设置源码路径
$srcPath = Join-Path $PSScriptRoot "..\src"
if (-not (Test-Path $srcPath)) {
    Write-Error "源码路径不存在: $srcPath"
    exit 1
}

function Get-FileLineCount {
    param([string]$FilePath)
    
    try {
        $lines = Get-Content $FilePath -ErrorAction SilentlyContinue
        if ($lines -is [array]) {
            return $lines.Count
        } elseif ($lines) {
            return 1
        } else {
            return 0
        }
    } catch {
        return 0
    }
}

function Test-HelperClassLimits {
    Write-Host "📊 检查Helper类行数限制..." -ForegroundColor Yellow
    
    $helperFiles = Get-ChildItem -Path $srcPath -Recurse -Name "*Helper.cs" | Where-Object { 
        $_ -notlike "*Refactored*" -and $_ -notlike "*Base*" 
    }
    
    $violations = @()
    
    foreach ($file in $helperFiles) {
        $fullPath = Join-Path $srcPath $file
        $lineCount = Get-FileLineCount $fullPath
        
        if ($lineCount -gt $MaxHelperLines) {
            $violations += [PSCustomObject]@{
                File = $file
                Lines = $lineCount
                Limit = $MaxHelperLines
                Severity = "HIGH"
                Message = "Helper类超过${MaxHelperLines}行限制"
            }
        }
    }
    
    if ($violations.Count -gt 0) {
        Write-Host "❌ 发现${violations.Count}个Helper类超过行数限制:" -ForegroundColor Red
        foreach ($v in $violations) {
            Write-Host "   • $($v.File): $($v.Lines)行 (限制: $($v.Limit)行)" -ForegroundColor Red
        }
        return $violations
    } else {
        Write-Host "✅ 所有Helper类行数符合规范" -ForegroundColor Green
        return @()
    }
}

function Test-ServiceClassLimits {
    Write-Host "📊 检查Service类行数限制..." -ForegroundColor Yellow
    
    $serviceFiles = Get-ChildItem -Path $srcPath -Recurse -Name "*Service.cs" | Where-Object {
        $_ -notlike "*Interface*" -and $_ -notlike "*Base*"
    }
    
    $violations = @()
    
    foreach ($file in $serviceFiles) {
        $fullPath = Join-Path $srcPath $file
        $lineCount = Get-FileLineCount $fullPath
        
        if ($lineCount -gt $MaxServiceLines) {
            $violations += [PSCustomObject]@{
                File = $file
                Lines = $lineCount  
                Limit = $MaxServiceLines
                Severity = "MEDIUM"
                Message = "Service类超过${MaxServiceLines}行限制"
            }
        }
    }
    
    if ($violations.Count -gt 0) {
        Write-Host "⚠️  发现${violations.Count}个Service类超过行数限制:" -ForegroundColor Yellow
        foreach ($v in $violations) {
            Write-Host "   • $($v.File): $($v.Lines)行 (限制: $($v.Limit)行)" -ForegroundColor Yellow
        }
        return $violations
    } else {
        Write-Host "✅ 所有Service类行数符合规范" -ForegroundColor Green
        return @()
    }
}

function Test-ControllerClassLimits {
    Write-Host "📊 检查Controller类行数限制..." -ForegroundColor Yellow
    
    $controllerFiles = Get-ChildItem -Path $srcPath -Recurse -Name "*Controller.cs" | Where-Object {
        $_ -notlike "*Base*"
    }
    
    $violations = @()
    
    foreach ($file in $controllerFiles) {
        $fullPath = Join-Path $srcPath $file
        $lineCount = Get-FileLineCount $fullPath
        
        if ($lineCount -gt $MaxControllerLines) {
            $violations += [PSCustomObject]@{
                File = $file
                Lines = $lineCount
                Limit = $MaxControllerLines  
                Severity = "MEDIUM"
                Message = "Controller类超过${MaxControllerLines}行限制"
            }
        }
    }
    
    if ($violations.Count -gt 0) {
        Write-Host "⚠️  发现${violations.Count}个Controller类超过行数限制:" -ForegroundColor Yellow
        foreach ($v in $violations) {
            Write-Host "   • $($v.File): $($v.Lines)行 (限制: $($v.Limit)行)" -ForegroundColor Yellow
        }
        return $violations
    } else {
        Write-Host "✅ 所有Controller类行数符合规范" -ForegroundColor Green
        return @()
    }
}

function Test-AutoMapperUsage {
    Write-Host "📊 检查AutoMapper使用规范..." -ForegroundColor Yellow
    
    # 查找手动字段映射模式（可能遗漏字段的危险模式）
    $suspiciousPatterns = @(
        "\.([A-Z][a-zA-Z]*)\s*=\s*dto\.([A-Z][a-zA-Z]*)",  # model.Field = dto.Field
        "if\s*\(\s*!string\.IsNullOrWhiteSpace\(dto\.[A-Z]"  # 手动null检查模式
    )
    
    $violations = @()
    $helperFiles = Get-ChildItem -Path $srcPath -Recurse -Name "*Helper.cs" -ErrorAction SilentlyContinue
    
    foreach ($file in $helperFiles) {
        $fullPath = Join-Path $srcPath $file
        try {
            $content = Get-Content $fullPath -Raw -ErrorAction SilentlyContinue
            if (-not $content) { continue }
            
            foreach ($pattern in $suspiciousPatterns) {
                if ($content -match $pattern) {
                    $violations += [PSCustomObject]@{
                        File = $file
                        Pattern = $pattern
                        Severity = "MEDIUM" 
                        Message = "可能存在手动字段映射，建议使用AutoMapper"
                    }
                    break  # 每个文件只报告一次
                }
            }
        } catch {
            # 忽略无法读取的文件
        }
    }
    
    if ($violations.Count -gt 0) {
        Write-Host "⚠️  发现${violations.Count}个可能的手动映射问题:" -ForegroundColor Yellow
        foreach ($v in $violations) {
            Write-Host "   • $($v.File): $($v.Message)" -ForegroundColor Yellow
        }
        return $violations
    } else {
        Write-Host "✅ AutoMapper使用规范检查通过" -ForegroundColor Green
        return @()
    }
}

function Test-RefactoredArchitecture {
    Write-Host "📊 检查重构架构完整性..." -ForegroundColor Yellow
    
    $refactoredModules = @("Users", "Patients", "Prescriptions")
    $missing = @()
    
    foreach ($module in $refactoredModules) {
        $refactoredFile = Get-ChildItem -Path $srcPath -Recurse -Name "*$module*BusinessHelper.Refactored.cs" -ErrorAction SilentlyContinue
        if (-not $refactoredFile) {
            $missing += $module
        }
    }
    
    if ($missing.Count -gt 0) {
        Write-Host "⚠️  缺少重构文件:" -ForegroundColor Yellow
        foreach ($m in $missing) {
            Write-Host "   • ${m}BusinessHelper.Refactored.cs" -ForegroundColor Yellow
        }
        
        $violations = $missing | ForEach-Object {
            [PSCustomObject]@{
                File = "${_}BusinessHelper.Refactored.cs"
                Severity = "LOW"
                Message = "缺少重构版本文件"
            }
        }
        return $violations
    } else {
        Write-Host "✅ 重构架构文件检查通过" -ForegroundColor Green
        return @()
    }
}

function Generate-QualityReport {
    param([array]$AllViolations)
    
    if ($AllViolations.Count -eq 0) {
        Write-Host "`n🎉 恭喜！所有质量检查都通过了！" -ForegroundColor Green
        Write-Host "   代码质量符合UltraThink标准" -ForegroundColor Green
        return 0
    }
    
    Write-Host "`n📋 质量检查报告:" -ForegroundColor Cyan
    Write-Host "==========================================" -ForegroundColor Cyan
    
    $highIssues = $AllViolations | Where-Object { $_.Severity -eq "HIGH" }
    $mediumIssues = $AllViolations | Where-Object { $_.Severity -eq "MEDIUM" }  
    $lowIssues = $AllViolations | Where-Object { $_.Severity -eq "LOW" }
    
    if ($highIssues.Count -gt 0) {
        Write-Host "🚨 高优先级问题 ($($highIssues.Count)个):" -ForegroundColor Red
        foreach ($issue in $highIssues) {
            Write-Host "   • $($issue.File): $($issue.Message)" -ForegroundColor Red
        }
        Write-Host ""
    }
    
    if ($mediumIssues.Count -gt 0) {
        Write-Host "⚠️  中优先级问题 ($($mediumIssues.Count)个):" -ForegroundColor Yellow
        foreach ($issue in $mediumIssues) {
            Write-Host "   • $($issue.File): $($issue.Message)" -ForegroundColor Yellow
        }
        Write-Host ""
    }
    
    if ($lowIssues.Count -gt 0) {
        Write-Host "ℹ️  低优先级问题 ($($lowIssues.Count)个):" -ForegroundColor Blue
        foreach ($issue in $lowIssues) {
            Write-Host "   • $($issue.File): $($issue.Message)" -ForegroundColor Blue
        }
        Write-Host ""
    }
    
    Write-Host "总计问题: $($AllViolations.Count)个" -ForegroundColor Cyan
    Write-Host "==========================================" -ForegroundColor Cyan
    
    # 高优先级问题导致失败
    if ($highIssues.Count -gt 0) {
        return 1
    } else {
        return 0
    }
}

# 执行所有检查
try {
    Write-Host "基础配置:" -ForegroundColor Cyan
    Write-Host "  • Helper类限制: ${MaxHelperLines}行" -ForegroundColor Gray
    Write-Host "  • Service类限制: ${MaxServiceLines}行" -ForegroundColor Gray  
    Write-Host "  • Controller类限制: ${MaxControllerLines}行" -ForegroundColor Gray
    Write-Host ""
    
    $allViolations = @()
    
    # 执行各项检查
    $allViolations += Test-HelperClassLimits
    $allViolations += Test-ServiceClassLimits  
    $allViolations += Test-ControllerClassLimits
    $allViolations += Test-AutoMapperUsage
    $allViolations += Test-RefactoredArchitecture
    
    Write-Host ""
    
    # 生成报告并返回结果
    $exitCode = Generate-QualityReport $allViolations
    exit $exitCode
    
} catch {
    Write-Host "❌ 质量检查过程中发生错误:" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# UltraThink代码质量门禁规则说明
<#
基于UltraThink重构经验制定的质量标准：

1. 文件行数限制:
   - Helper类: 最大500行（基于重构前问题）
   - Service类: 最大300行（合理的单一职责范围） 
   - Controller类: 最大200行（API层应该轻量）

2. AutoMapper使用规范:
   - 检测手动字段映射模式
   - 防止字段遗漏问题重现

3. 重构架构完整性:
   - 确保重构后的文件结构完整
   - 验证专业服务分离是否到位

4. 质量门禁策略:
   - 高优先级问题: 阻止提交
   - 中优先级问题: 警告但允许提交
   - 低优先级问题: 信息提示

使用方法:
  .\quality-check.ps1                    # 标准检查
  .\quality-check.ps1 -Detailed          # 详细报告
  .\quality-check.ps1 -MaxHelperLines 400 # 自定义限制

集成建议:
- 在CI/CD pipeline中执行
- Git pre-commit hook中使用
- 定期代码审查时运行
#>