# UltraThink Ex变量错误修复脚本
# 修复错误引入的ex变量引用问题

Write-Host "UltraThink Ex Variable Error Fixer" -ForegroundColor Cyan
Write-Host "Fixing incorrect ex variable references..." -ForegroundColor Yellow

$fixedCount = 0
$patterns = @(
    # 修复: ServiceResult.Failure调用中错误的ex引用
    @{
        'pattern' = 'ServiceResult<([^>]+)>\.Failure\("([^"]*)", ex\);'
        'replacement' = 'ServiceResult<$1>.Failure("$2");'
        'description' = 'Remove incorrect ex parameter from Failure calls'
    }
    
    # 修复: ServiceResult.Success调用中错误的ex引用  
    @{
        'pattern' = 'ServiceResult<([^>]+)>\.Success\(([^,]+), ex\);'
        'replacement' = 'ServiceResult<$1>.Success($2);'
        'description' = 'Remove incorrect ex parameter from Success calls'
    }
    
    # 修复: return语句中错误的ex引用
    @{
        'pattern' = 'return ServiceResult<([^>]+)>\.Failure\("([^"]*)", ex\);'
        'replacement' = 'return ServiceResult<$1>.Failure("$2");'
        'description' = 'Fix return statements with incorrect ex parameter'
    }
)

# 获取所有C#文件
$csFiles = Get-ChildItem -Path "src\Server\Modules" -Filter "*.cs" -Recurse

Write-Host "Processing $($csFiles.Count) files..." -ForegroundColor Green

foreach ($file in $csFiles) {
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $originalContent = $content
    $fileModified = $false
    
    foreach ($patternInfo in $patterns) {
        $oldContent = $content
        $content = $content -replace $patternInfo['pattern'], $patternInfo['replacement']
        
        if ($content -ne $oldContent) {
            Write-Host "  Applied: $($patternInfo['description']) in $($file.Name)" -ForegroundColor Gray
            $fileModified = $true
        }
    }
    
    # 保存更改
    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -Encoding UTF8
        Write-Host "Fixed: $($file.Name)" -ForegroundColor Green
        $fixedCount++
    }
}

Write-Host "`n=== Ex Variable Fix Summary ===" -ForegroundColor Cyan
Write-Host "Fixed files: $fixedCount" -ForegroundColor Green
Write-Host "Ready for compilation test" -ForegroundColor Yellow