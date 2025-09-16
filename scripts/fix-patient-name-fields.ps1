# P3-Fix Batch5 DTO字段名修复脚本
# 目的：将所有DTO中的PatientName字段替换为Name，以与Patient实体对齐

param(
    [string]$ProjectRoot = "D:\source\repos\LYBTZYZS",
    [string]$ReportPath = "_reports/2025-09/backend/p3-fix-batch5"
)

Write-Host "=== P3-Fix Batch5: DTO字段名修复 ===" -ForegroundColor Cyan
Write-Host "修复PatientName -> Name字段对齐问题" -ForegroundColor Gray
Write-Host "执行时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host ""

# 需要修复的文件列表（基于grep搜索结果）
$filesToFix = @(
    "src\Shared\LYBT.Shared.Models\Contracts\Patients\PatientStatisticsDtos.cs",
    "src\Shared\LYBT.Shared.Models\Contracts\Patients\PatientOperationDtos.cs", 
    "src\Shared\LYBT.Shared.Models\Contracts\MedicalCase\MedicalCaseDtos.cs",
    "src\Shared\LYBT.Shared.Models\Contracts\Consultation\ConsultationOperationDtos.cs",
    "src\Shared\LYBT.Shared.Models\Contracts\Consultation\ConsultationDtos.cs"
)

$fixedFiles = @()
$errorFiles = @()

foreach ($file in $filesToFix) {
    $fullPath = Join-Path $ProjectRoot $file
    
    if (Test-Path $fullPath) {
        Write-Host "修复文件: $file" -ForegroundColor Yellow
        
        try {
            # 读取文件内容
            $content = Get-Content $fullPath -Raw -Encoding UTF8
            
            # 替换PatientName为Name（保持注释和属性不变）
            $updatedContent = $content -replace 'public string\? PatientName \{ get; set; \}', 'public string? Name { get; set; }'
            $updatedContent = $updatedContent -replace 'public string PatientName \{ get; set; \}', 'public string Name { get; set; }'
            
            # 写回文件
            $updatedContent | Out-File -FilePath $fullPath -Encoding UTF8 -NoNewline
            
            $fixedFiles += $file
            Write-Host "  ✅ 修复完成" -ForegroundColor Green
        }
        catch {
            Write-Host "  ❌ 修复失败: $($_.Exception.Message)" -ForegroundColor Red
            $errorFiles += $file
        }
    }
    else {
        Write-Host "  ⚠️ 文件不存在: $fullPath" -ForegroundColor Yellow
        $errorFiles += $file
    }
}

Write-Host ""
Write-Host "📊 修复结果汇总" -ForegroundColor Cyan
Write-Host "─────────────────────────────────────────" -ForegroundColor Gray
Write-Host "成功修复: $($fixedFiles.Count) 个文件" -ForegroundColor Green
Write-Host "修复失败: $($errorFiles.Count) 个文件" -ForegroundColor Red

if ($fixedFiles.Count -gt 0) {
    Write-Host ""
    Write-Host "✅ 成功修复的文件:" -ForegroundColor Green
    foreach ($file in $fixedFiles) {
        Write-Host "  - $file" -ForegroundColor Gray
    }
}

if ($errorFiles.Count -gt 0) {
    Write-Host ""
    Write-Host "❌ 修复失败的文件:" -ForegroundColor Red
    foreach ($file in $errorFiles) {
        Write-Host "  - $file" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "🎯 下一步操作:" -ForegroundColor Yellow
Write-Host "1. 验证编译是否成功" -ForegroundColor Gray
Write-Host "2. 更新AutoMapper映射配置" -ForegroundColor Gray
Write-Host "3. 运行单元测试确认修复效果" -ForegroundColor Gray

# 生成修复报告
$reportFile = Join-Path $ReportPath "dto-field-name-fixes.md"
$reportContent = @"
# DTO字段名修复报告

**执行时间**: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
**修复类型**: PatientName → Name 字段对齐
**目的**: 与Patient实体字段名保持一致

## 修复结果

- **成功修复**: $($fixedFiles.Count) 个文件
- **修复失败**: $($errorFiles.Count) 个文件

## 修复详情

### 成功修复的文件
$(if ($fixedFiles.Count -gt 0) { ($fixedFiles | ForEach-Object { "- $_" }) -join "`n" } else { "无" })

### 修复失败的文件  
$(if ($errorFiles.Count -gt 0) { ($errorFiles | ForEach-Object { "- $_" }) -join "`n" } else { "无" })

## 影响范围

修复后的字段对齐将解决P3-Fix Batch4中发现的验证失败问题：
- 数据一致性检查脚本将能够正确验证患者姓名字段
- AutoMapper映射将与实体字段名匹配
- API响应格式保持一致性

---
*P3-Fix Batch5 DTO字段名修复 - $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')*
"@

# 确保报告目录存在
if (!(Test-Path $ReportPath)) {
    New-Item -ItemType Directory -Force -Path $ReportPath | Out-Null
}

$reportContent | Out-File -FilePath $reportFile -Encoding UTF8
Write-Host "✅ 修复报告已生成: $reportFile" -ForegroundColor Green

exit $(if ($errorFiles.Count -eq 0) { 0 } else { 1 })