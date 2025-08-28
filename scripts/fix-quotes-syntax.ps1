# UltraThink语法修复脚本 - 修复缺少引号的语法错误
Write-Host "修复语法错误 - 双引号和括号问题" -ForegroundColor Cyan

# 获取所有需要修复的C#文件  
$files = Get-ChildItem -Path "src\Server\Modules" -Filter "*.cs" -Recurse

$fixedCount = 0

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $originalContent = $content
    
    # 修复模式: ServiceResult<T>.Failure("message, ex);
    # 替换为: ServiceResult<T>.Failure("message", ex);
    $content = $content -replace 'ServiceResult<([^>]+)>\.Failure\("([^"]*), ex\);', 'ServiceResult<$1>.Failure("$2", ex);'
    
    # 修复特定的常见错误模式
    $content = $content -replace 'Failure\("([^"]*), ex\);', 'Failure("$1", ex);'
    
    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -Encoding UTF8
        Write-Host "修复文件: $($file.Name)" -ForegroundColor Green
        $fixedCount++
    }
}

Write-Host "修复完成！共修复 $fixedCount 个文件" -ForegroundColor Green