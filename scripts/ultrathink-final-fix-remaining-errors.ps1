# UltraThink最终修复剩余编译错误
# 修复PatientCrudService.cs和HerbBusinessHelper.cs的字符串语法错误

Write-Host "UltraThink Final Fix - Remaining 24 Compilation Errors" -ForegroundColor Cyan
Write-Host "Target: Fix string constant and syntax errors" -ForegroundColor Yellow

$specificFixes = @(
    # PatientCrudService.cs 字符串缺少闭合引号
    @{
        'file' = 'PatientCrudService.cs'
        'pattern' = 'ServiceResult<PatientDto>\.Failure\("([^"]*)"\);'
        'replacement' = 'ServiceResult<PatientDto>.Failure("$1");'
        'description' = 'Fix missing closing quotes in PatientCrudService'
    }
    
    # 修复行尾缺少分号的情况
    @{
        'file' = 'PatientCrudService.cs'
        'pattern' = 'Failure\("([^"]*?)"\)(\s*)\}?$'
        'replacement' = 'Failure("$1");$2}'
        'description' = 'Add missing semicolons and closing braces'
    }
    
    # HerbBusinessHelper.cs 缺少分号
    @{
        'file' = 'HerbBusinessHelper.cs'
        'pattern' = '(\s*_logger\.LogError[^;]+)\s*return\s'
        'replacement' = '$1;' + "`r`n" + '                return '
        'description' = 'Fix missing semicolon in HerbBusinessHelper'
    }
)

$fixedCount = 0

# 获取相关C#文件
$targetFiles = @(
    Get-ChildItem -Path "src\Server\Modules\LYBT.Module.Patients\Services\Core\PatientCrudService.cs" -ErrorAction SilentlyContinue
    Get-ChildItem -Path "src\Server\Modules\LYBT.Module.Herbs\Helpers\HerbBusinessHelper.cs" -ErrorAction SilentlyContinue
)

Write-Host "Processing $($targetFiles.Count) files for specific fixes..." -ForegroundColor Green

foreach ($file in $targetFiles) {
    if (-not $file) { continue }
    
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $originalContent = $content
    $fileModified = $false
    
    # 应用特定修复
    foreach ($fix in $specificFixes) {
        if ($file.Name -eq $fix['file']) {
            $oldContent = $content
            $content = $content -replace $fix['pattern'], $fix['replacement']
            if ($content -ne $oldContent) {
                Write-Host "  Applied: $($fix['description'])" -ForegroundColor Gray
                $fileModified = $true
            }
        }
    }
    
    # 通用字符串修复
    # 修复: 缺少闭合引号的字符串
    $content = $content -replace '("鏂板鎮ｈ€呮。妗堝け璐?")\);', '"鏂板鎮ｈ€呮。妗堝け璐?");'
    $content = $content -replace '("鏇存柊鎮ｈ€呮。妗堝け璐?")\);', '"鏇存柊鎮ｈ€呮。妗堝け璐?");'
    
    # 修复: 缺少换行的日志语句
    $content = $content -replace '(\{[^}]+\}\", [^;]+);(\s*)return\s', '$1;' + "`r`n" + '                return '
    
    # 修复: 语句之间缺少分号和换行
    $content = $content -replace '(LogError\([^)]+\));(\s*)return\s', '$1;' + "`r`n" + '                return '
    
    # 保存更改
    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -Encoding UTF8
        Write-Host "Fixed: $($file.Name)" -ForegroundColor Green
        $fixedCount++
    }
}

Write-Host "`n=== UltraThink Final Fix Summary ===" -ForegroundColor Cyan
Write-Host "Fixed files: $fixedCount" -ForegroundColor Green
Write-Host "Next: Fix User module type reference errors" -ForegroundColor Yellow