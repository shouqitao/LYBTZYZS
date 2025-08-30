# UltraThink编译错误修复脚本
# 自动添加缺少的 LYBT.Shared.Models.Contracts.Common using 语句

Write-Host "🔧 UltraThink编译错误修复 - 添加ServiceResult using语句" -ForegroundColor Cyan

# 定义需要修复的模块路径
$modules = @(
    "src\Server\Modules\LYBT.Module.Patients",
    "src\Server\Modules\LYBT.Module.Users", 
    "src\Server\Modules\LYBT.Module.Prescriptions"
)

$usingToAdd = "using LYBT.Shared.Models.Contracts.Common;"
$fixedCount = 0

foreach ($module in $modules) {
    $modulePath = Join-Path $PSScriptRoot "..\$module"
    
    if (Test-Path $modulePath) {
        Write-Host "`n📂 处理模块: $module" -ForegroundColor Yellow
        
        # 查找所有C#文件
        $csFiles = Get-ChildItem -Path $modulePath -Filter "*.cs" -Recurse
        
        foreach ($file in $csFiles) {
            $content = Get-Content $file.FullName -Raw
            
            # 检查文件是否包含ServiceResult但缺少Contracts.Common的using
            if ($content -match "ServiceResult" -and 
                $content -notmatch "using LYBT\.Shared\.Models\.Contracts\.Common;") {
                
                Write-Host "  ✅ 修复文件: $($file.Name)" -ForegroundColor Green
                
                # 在using System;之后添加新的using语句
                if ($content -match "using System;") {
                    $newContent = $content -replace "(using System;)", "`$1`r`n$usingToAdd"
                    Set-Content -Path $file.FullName -Value $newContent -Encoding UTF8
                    $fixedCount++
                }
            }
        }
    }
}

Write-Host "`n🎯 修复完成！共修复 $fixedCount 个文件" -ForegroundColor Green
Write-Host "请运行编译命令验证修复结果" -ForegroundColor Cyan