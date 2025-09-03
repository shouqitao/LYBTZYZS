# UltraThink最终字符串修复脚本
# 解决剩余73个"常量中有换行符"错误

Write-Host "UltraThink Final String Fix - Last Mile" -ForegroundColor Cyan
Write-Host "Target: Fix remaining 73 string constant errors" -ForegroundColor Yellow

$specificFixes = @(
    # PatientBusinessService.cs 第142行错误
    @{
        'file' = 'PatientBusinessService.cs'
        'pattern' = '"合并重复患者成功 - 主患者: \{PrimaryId\}, 删除患者: \{DuplicateId\}([^"]*)"'
        'replacement' = '"合并重复患者成功 - 主患者: {PrimaryId}, 删除患者: {DuplicateId}"'
    }
    
    # PatientCrudService.cs 第67和109行错误
    @{
        'file' = 'PatientCrudService.cs'  
        'pattern' = '"创建患者失败([^"]*)"'
        'replacement' = '"创建患者失败"'
    }
    @{
        'file' = 'PatientCrudService.cs'
        'pattern' = '"更新患者失败([^"]*)"'
        'replacement' = '"更新患者失败"'
    }
    
    # PrescriptionWorkflowService.cs 多个位置错误
    @{
        'file' = 'PrescriptionWorkflowService.cs'
        'pattern' = '"提交处方失败([^"]*)"'
        'replacement' = '"提交处方失败"'
    }
    @{
        'file' = 'PrescriptionWorkflowService.cs'
        'pattern' = '"审核处方失败([^"]*)"'
        'replacement' = '"审核处方失败"'
    }
    @{
        'file' = 'PrescriptionWorkflowService.cs'
        'pattern' = '"完成处方失败([^"]*)"'
        'replacement' = '"完成处方失败"'
    }
    @{
        'file' = 'PrescriptionWorkflowService.cs'
        'pattern' = '"取消处方失败([^"]*)"'
        'replacement' = '"取消处方失败"'
    }
)

# 通用修复模式
$generalPatterns = @(
    # 修复: 带换行符的字符串常量
    @{
        'pattern' = '"([^"]*)([\r\n]+)([^"]*)"'
        'replacement' = '"$1$3"'
        'description' = 'Fix string constants with newlines'
    }
    
    # 修复: 日志字符串中的换行符
    @{
        'pattern' = '_logger\.Log\w+\([^,]+,\s*"([^"]*)[\r\n]+([^"]*)"'
        'replacement' = '_logger.LogError(ex, "$1$2"'
        'description' = 'Fix logger string constants'
    }
)

$fixedCount = 0
$totalFixed = 0

# 获取所有C#文件
$csFiles = Get-ChildItem -Path "src\Server\Modules" -Filter "*.cs" -Recurse

Write-Host "Processing $($csFiles.Count) files for final fixes..." -ForegroundColor Green

foreach ($file in $csFiles) {
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $originalContent = $content
    $fileModified = $false
    
    # 应用特定修复
    foreach ($fix in $specificFixes) {
        if ($file.Name -eq $fix['file']) {
            $oldContent = $content
            $content = $content -replace $fix['pattern'], $fix['replacement']
            if ($content -ne $oldContent) {
                Write-Host "  Applied specific fix for: $($file.Name)" -ForegroundColor Gray
                $fileModified = $true
            }
        }
    }
    
    # 应用通用修复模式
    foreach ($patternInfo in $generalPatterns) {
        $oldContent = $content
        $content = $content -replace $patternInfo['pattern'], $patternInfo['replacement']
        if ($content -ne $oldContent) {
            Write-Host "  Applied: $($patternInfo['description']) in $($file.Name)" -ForegroundColor Gray
            $fileModified = $true
        }
    }
    
    # 特殊处理：手动清理明显的字符串错误
    $content = $content -replace '"\s*[\r\n]+\s*"', '""'  # 空字符串跨行
    $content = $content -replace '"([^"]*?)[\r\n]+([^"]*)"', '"$1$2"'  # 字符串中的换行符
    
    # 保存更改
    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -Encoding UTF8
        Write-Host "Final fixed: $($file.Name)" -ForegroundColor Green
        $fixedCount++
    }
}

Write-Host "`n=== UltraThink Final Fix Summary ===" -ForegroundColor Cyan
Write-Host "Fixed files: $fixedCount" -ForegroundColor Green
Write-Host "Target: Zero compilation errors" -ForegroundColor Yellow