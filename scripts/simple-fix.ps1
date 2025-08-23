# 简化的修复脚本
Write-Host "开始修复..." -ForegroundColor Green

# 修复Prescriptions模块的Guid问题  
$file = "src\Client\Desktop\Modules\Prescriptions\ViewModels\PrescriptionManagementViewModel.cs"
if (Test-Path $file) {
    Write-Host "处理: $file" -ForegroundColor Yellow
    
    $content = Get-Content $file -Raw -Encoding UTF8
    
    # 修复Guid用法
    $content = $content -replace "\.PatientId\.HasValue", ".PatientId != Guid.Empty"
    $content = $content -replace "\.PatientId\.Value", ".PatientId"
    
    # 添加System.IO
    if ($content -match "Path\." -and $content -notmatch "using System\.IO;") {
        $content = $content -replace "using System;", "using System;`r`nusing System.IO;"
    }
    
    Set-Content $file $content -Encoding UTF8
    Write-Host "✅ 已修复" -ForegroundColor Green
}

Write-Host "完成!" -ForegroundColor Green