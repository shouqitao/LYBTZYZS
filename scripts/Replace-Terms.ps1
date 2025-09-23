# 术语统一脚本 - 批量替换旧术语为新术语

$replacements = @{
    '看诊' = '诊疗'
    'MedicalWorkbench' = 'MedicalWorkbench'
    '看诊工作台' = '诊疗工作台'
    'MedicalWorkbenchMainView' = 'MedicalWorkbenchMainView'
    'MedicalWorkbenchMainViewModel' = 'MedicalWorkbenchMainViewModel'
    'MedicalWorkbenchNavigator' = 'MedicalWorkbenchNavigator'
    'MedicalWorkbenchModule' = 'MedicalWorkbenchModule'
    'IMedicalWorkbenchNavigator' = 'IMedicalWorkbenchNavigator'
    'MedicalWorkbenchContentRegion' = 'MedicalWorkbenchContentRegion'
}

# 获取所有需要处理的文件
$files = Get-ChildItem -Path "src" -Recurse -Include "*.cs", "*.xaml", "*.csproj", "*.json", "*.md" -File

$totalReplacements = 0

foreach ($file in $files) {
    $content = Get-Content -Path $file.FullName -Raw -Encoding UTF8
    $originalContent = $content
    
    foreach ($term in $replacements.Keys) {
        $content = $content -replace [regex]::Escape($term), $replacements[$term]
    }
    
    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -Encoding UTF8 -NoNewline
        Write-Host "Updated: $($file.FullName)" -ForegroundColor Green
        $totalReplacements++
    }
}

Write-Host "`nTotal files updated: $totalReplacements" -ForegroundColor Cyan