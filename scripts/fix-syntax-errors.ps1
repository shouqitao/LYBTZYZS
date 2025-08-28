# UltraThink编译错误修复脚本 v2
# 修复之前脚本造成的语法错误

Write-Host "🔧 UltraThink语法错误修复 - 修复字符串常量和using语句" -ForegroundColor Cyan

# 定义要修复的文件和错误
$fixes = @{
    "src\Server\Modules\LYBT.Module.Patients\Services\Status\PatientStatusService.cs" = @{
        'return ServiceResult<bool>.Failure("设置患者状态失败, ex);' = 'return ServiceResult<bool>.Failure("设置患者状态失败", ex);'
        'return ServiceResult<bool>.Failure("启用患者失败, ex);' = 'return ServiceResult<bool>.Failure("启用患者失败", ex);'
        'return ServiceResult<bool>.Failure("禁用患者失败, ex);' = 'return ServiceResult<bool>.Failure("禁用患者失败", ex);'
    }
    
    "src\Server\Modules\LYBT.Module.Patients\Services\Business\PatientBusinessService.cs" = @{
        'return ServiceResult<bool>.Failure("合并患者失败, ex);' = 'return ServiceResult<bool>.Failure("合并患者失败", ex);'
        'return ServiceResult<List<PatientConsultationHistoryDto>>.Failure("获取患者就诊历史失败, ex);' = 'return ServiceResult<List<PatientConsultationHistoryDto>>.Failure("获取患者就诊历史失败", ex);'
        '"合并重复患者成功 - 主患者: {PrimaryId}, 删除患者: {DuplicateId}, ex);' = '"合并重复患者成功 - 主患者: {PrimaryId}, 删除患者: {DuplicateId}", ex);'
    }
}

$fixedCount = 0

foreach ($filePath in $fixes.Keys) {
    $fullPath = Join-Path $PSScriptRoot "..\$filePath"
    
    if (Test-Path $fullPath) {
        Write-Host "`n📝 修复文件: $filePath" -ForegroundColor Yellow
        
        $content = Get-Content $fullPath -Raw -Encoding UTF8
        $originalContent = $content
        
        # 应用所有修复
        foreach ($oldText in $fixes[$filePath].Keys) {
            $newText = $fixes[$filePath][$oldText]
            $content = $content -replace [regex]::Escape($oldText), $newText
        }
        
        # 修复using语句顺序
        if ($content -match "using LYBT\.Shared\.Models\.Contracts\.Common;") {
            # 将错误位置的using语句移动到正确位置
            $content = $content -replace "using LYBT\.Shared\.Models\.Contracts\.Common;`r?`n", ""
            $content = $content -replace "(using System;)", "`$1`r`nusing LYBT.Shared.Models.Contracts.Common;"
        }
        
        if ($content -ne $originalContent) {
            Set-Content -Path $fullPath -Value $content -Encoding UTF8
            Write-Host "  ✅ 修复完成" -ForegroundColor Green
            $fixedCount++
        }
    }
}

Write-Host "`n🎯 修复完成！共修复 $fixedCount 个文件" -ForegroundColor Green
Write-Host "请重新运行编译命令验证修复结果" -ForegroundColor Cyan