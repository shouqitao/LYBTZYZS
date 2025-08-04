# 构建脚本来收集编译警告
$ErrorActionPreference = "Continue"

Write-Host "=== 开始构建后端解决方案 ===" -ForegroundColor Cyan
cd "D:\source\repos\LYBTZYZS"

# 构建并收集输出
$buildOutput = dotnet build LYBT.Backend.sln 2>&1

# 提取警告信息
$warnings = $buildOutput | Where-Object { $_ -match "warning CS" }
$errors = $buildOutput | Where-Object { $_ -match "error CS" }

Write-Host "`n=== 编译警告汇总 ===" -ForegroundColor Yellow
if ($warnings.Count -eq 0) {
    Write-Host "没有发现编译警告" -ForegroundColor Green
} else {
    Write-Host "发现 $($warnings.Count) 个警告:" -ForegroundColor Yellow
    
    # 按警告类型分组
    $groupedWarnings = $warnings | Group-Object { if ($_ -match 'CS\d+') { $matches[0] } else { 'Unknown' } }
    
    foreach ($group in $groupedWarnings | Sort-Object Count -Descending) {
        Write-Host "`n$($group.Name): $($group.Count) 个" -ForegroundColor Magenta
        $group.Group | Select-Object -First 3 | ForEach-Object {
            Write-Host "  $_" -ForegroundColor Gray
        }
        if ($group.Count -gt 3) {
            Write-Host "  ... 还有 $($group.Count - 3) 个类似警告" -ForegroundColor DarkGray
        }
    }
}

Write-Host "`n=== 编译错误汇总 ===" -ForegroundColor Red
if ($errors.Count -eq 0) {
    Write-Host "没有发现编译错误" -ForegroundColor Green
} else {
    Write-Host "发现 $($errors.Count) 个错误:" -ForegroundColor Red
    $errors | ForEach-Object {
        Write-Host "  $_" -ForegroundColor Red
    }
}

# 检查构建是否成功
if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ 构建成功" -ForegroundColor Green
} else {
    Write-Host "`n❌ 构建失败" -ForegroundColor Red
}