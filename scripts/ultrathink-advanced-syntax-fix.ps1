# UltraThink高级编译错误修复脚本 v3.0
# 系统性修复剩余的55个编译错误

Write-Host "UltraThink Advanced Syntax Fixer - Phase 2" -ForegroundColor Cyan
Write-Host "Target: Fix remaining 55 compilation errors" -ForegroundColor Yellow

$errorPatterns = @{
    # 修复: 常量中有换行符 (最常见错误)
    'constant_newline' = @{
        'pattern' = 'ServiceResult<([^>]+)>\.Failure\("([^"]*)"([^,]*), ex\);'
        'replacement' = 'ServiceResult<$1>.Failure("$2", ex);'
    }
    
    # 修复: 缺少逗号的字符串参数  
    'missing_comma' = @{
        'pattern' = 'ServiceResult<([^>]+)>\.Failure\("([^"]*)"([^,\)]*)\);'
        'replacement' = 'ServiceResult<$1>.Failure("$2", ex);'
    }
    
    # 修复: 日志记录中的字符串错误
    'log_string_error' = @{
        'pattern' = '_logger\.LogError\(ex, "([^"]*)"([^,\)]*), ([^)]+)\);'
        'replacement' = '_logger.LogError(ex, "$1", $3);'
    }
    
    # 修复: 返回语句中的字符串错误  
    'return_string_error' = @{
        'pattern' = 'return ServiceResult<([^>]+)>\.Failure\("([^"]*)"([^,\)]*)\);'
        'replacement' = 'return ServiceResult<$1>.Failure("$2");'
    }
}

$fixedFiles = @()
$totalFixed = 0

# 获取所有需要修复的C#文件
$csFiles = Get-ChildItem -Path "src\Server\Modules" -Filter "*.cs" -Recurse

Write-Host "`nProcessing $($csFiles.Count) C# files..." -ForegroundColor Green

foreach ($file in $csFiles) {
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $originalContent = $content
    $fileFixed = $false
    
    # 应用所有错误模式修复
    foreach ($patternName in $errorPatterns.Keys) {
        $pattern = $errorPatterns[$patternName]['pattern']
        $replacement = $errorPatterns[$patternName]['replacement']
        
        if ($content -match $pattern) {
            $content = $content -replace $pattern, $replacement
            $fileFixed = $true
            Write-Host "  Applied fix '$patternName' to: $($file.Name)" -ForegroundColor Gray
        }
    }
    
    # 特殊修复: 手动处理复杂的字符串错误
    $complexFixes = @(
        @{
            'old' = '"获取患者就诊历史失败", ex);'
            'new' = '"获取患者就诊历史失败", ex);'
        },
        @{
            'old' = '"创建患者失败", ex);'
            'new' = '"创建患者失败", ex);'
        },
        @{
            'old' = '"更新患者失败", ex);'
            'new' = '"更新患者失败", ex);'  
        }
    )
    
    foreach ($fix in $complexFixes) {
        if ($content -match [regex]::Escape($fix['old'])) {
            $content = $content -replace [regex]::Escape($fix['old']), $fix['new']
            $fileFixed = $true
        }
    }
    
    # 如果文件被修复，保存更改
    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -Encoding UTF8
        $fixedFiles += $file.Name
        $totalFixed++
        Write-Host "Fixed: $($file.Name)" -ForegroundColor Green
    }
}

Write-Host "`n=== UltraThink修复总结 ===" -ForegroundColor Cyan
Write-Host "修复文件数量: $totalFixed" -ForegroundColor Green
Write-Host "修复文件列表:" -ForegroundColor Yellow
$fixedFiles | ForEach-Object { Write-Host "  - $_" }

Write-Host "`nNext: Run compilation test to verify fixes" -ForegroundColor Cyan