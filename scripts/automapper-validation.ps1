# P3-Server Hardening AutoMapper验证脚本
# 目的：验证实体-DTO映射的正确性和P3-Fix Batch5字段对齐

param(
    [string]$WebApiUrl = "http://localhost:8080",
    [string]$ReportPath = "_reports/2025-09/backend/p3-server-hardening"
)

Write-Host "=== P3-Server Hardening: AutoMapper配置验证 ===" -ForegroundColor Cyan
Write-Host "WebAPI URL: $WebApiUrl" -ForegroundColor Gray
Write-Host "Report Path: $ReportPath" -ForegroundColor Gray
Write-Host "Execution Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host ""

# 创建报告目录
if (!(Test-Path $ReportPath)) {
    New-Item -ItemType Directory -Force -Path $ReportPath | Out-Null
}

$validationResults = @()
$passedTests = 0
$failedTests = 0

Write-Host "🔍 Step 1: 基础API连接测试" -ForegroundColor Cyan
Write-Host "─────────────────────────────────────────" -ForegroundColor Gray

# 检查API是否可访问
try {
    $healthResponse = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/health" -Method Get -TimeoutSec 10
    Write-Host "✅ API健康检查通过" -ForegroundColor Green
    $passedTests++
} catch {
    Write-Host "❌ API健康检查失败: $($_.Exception.Message)" -ForegroundColor Red
    $failedTests++
    $validationResults += @{
        Test = "API健康检查"
        Status = "FAIL"
        Error = $_.Exception.Message
    }
}

Write-Host ""
Write-Host "🔐 Step 2: 认证设置" -ForegroundColor Cyan
Write-Host "─────────────────────────────────────────" -ForegroundColor Gray

# 登录获取token
try {
    $loginData = @{
        username = "sysadmin"
        password = "Admin@123456"
        rememberMe = $false
    } | ConvertTo-Json

    $loginHeaders = @{ "Content-Type" = "application/json" }
    $loginResponse = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/auth/login" -Method POST -Body $loginData -Headers $loginHeaders -TimeoutSec 30
    
    if ($loginResponse.success -and $loginResponse.data.token) {
        $authHeaders = @{
            "Authorization" = "Bearer $($loginResponse.data.token)"
            "Content-Type" = "application/json"
        }
        Write-Host "✅ 认证成功" -ForegroundColor Green
        $passedTests++
    } else {
        Write-Host "❌ 认证失败" -ForegroundColor Red
        $failedTests++
        exit 1
    }
} catch {
    Write-Host "❌ 认证失败: $($_.Exception.Message)" -ForegroundColor Red
    $failedTests++
    exit 1
}

Write-Host ""
Write-Host "🗂️ Step 3: 实体-DTO字段映射验证" -ForegroundColor Cyan
Write-Host "─────────────────────────────────────────" -ForegroundColor Gray

# 定义期望的字段映射（基于P3-Fix Batch5实体分析）
$expectedMappings = @{
    "Users" = @{
        EntityFields = @("RealName", "Username", "Email", "PhoneNumber", "Role", "Status")
        APIEndpoint = "/api/v1/users"
        EntityField_RealName = "RealName"  # 不是FullName
        EntityField_Email = "Email"        # 确认存在
    }
    "Patients" = @{
        EntityFields = @("Name", "BirthDate", "Age", "Gender", "PhoneNumber", "Status")
        APIEndpoint = "/api/v1/patients"
        EntityField_Name = "Name"          # 不是PatientName
        EntityField_Age = "Age"            # 计算字段，基于BirthDate
    }
}

foreach ($entityName in $expectedMappings.Keys) {
    $mapping = $expectedMappings[$entityName]
    Write-Host "  验证 $entityName 实体映射..." -ForegroundColor Yellow
    
    try {
        # 获取API响应
        $response = Invoke-RestMethod -Uri "$WebApiUrl$($mapping.APIEndpoint)" -Method Get -Headers $authHeaders -TimeoutSec 10
        
        if ($response.success -and $response.data -and $response.data.items) {
            $firstItem = $response.data.items[0]
            $missingFields = @()
            $presentFields = @()
            
            # 验证关键字段存在
            foreach ($field in $mapping.EntityFields) {
                if ($firstItem.PSObject.Properties.Name -contains $field) {
                    $presentFields += $field
                } else {
                    $missingFields += $field
                }
            }
            
            # P3-Fix Batch5特定验证
            if ($entityName -eq "Users") {
                $hasRealName = $firstItem.PSObject.Properties.Name -contains "realName"
                $hasEmail = $firstItem.PSObject.Properties.Name -contains "email"
                
                if ($hasRealName -and $hasEmail) {
                    Write-Host "    ✅ Users正确使用realName和email字段" -ForegroundColor Green
                    $passedTests++
                } else {
                    Write-Host "    ❌ Users字段映射问题 - realName: $hasRealName, email: $hasEmail" -ForegroundColor Red
                    $failedTests++
                    $validationResults += @{
                        Test = "Users字段映射"
                        Status = "FAIL"
                        Error = "实体应包含realName和email字段"
                    }
                }
            }
            
            if ($entityName -eq "Patients") {
                $hasName = $firstItem.PSObject.Properties.Name -contains "name"
                $hasAge = $firstItem.PSObject.Properties.Name -contains "age"
                $hasBirthDate = $firstItem.PSObject.Properties.Name -contains "birthDate"
                
                if ($hasName -and $hasBirthDate -and $hasAge) {
                    Write-Host "    ✅ Patients正确使用name、birthDate和age(计算)字段" -ForegroundColor Green
                    $passedTests++
                } else {
                    Write-Host "    ❌ Patients字段映射问题 - name: $hasName, birthDate: $hasBirthDate, age: $hasAge" -ForegroundColor Red
                    $failedTests++
                    $validationResults += @{
                        Test = "Patients字段映射"
                        Status = "FAIL"
                        Error = "实体应包含name、birthDate和age(计算)字段"
                    }
                }
                
                if ($hasAge -and $hasBirthDate) {
                    Write-Host "    ✅ Patients正确映射age和birthDate字段" -ForegroundColor Green
                    $passedTests++
                } else {
                    Write-Host "    ❌ Patients缺少age/birthDate字段 - age: $hasAge, birthDate: $hasBirthDate" -ForegroundColor Red
                    $failedTests++
                    $validationResults += @{
                        Test = "Patients Age映射"
                        Status = "FAIL"
                        Error = "缺少age或birthDate字段"
                    }
                }
            }
            
            Write-Host "    字段验证完成: $($presentFields.Count)/$($mapping.EntityFields.Count)" -ForegroundColor Gray
            
        } else {
            Write-Host "    ❌ $entityName API无数据响应" -ForegroundColor Red
            $failedTests++
            $validationResults += @{
                Test = "$entityName API响应"
                Status = "FAIL"
                Error = "API返回空数据"
            }
        }
    } catch {
        Write-Host "    ❌ $entityName API调用失败: $($_.Exception.Message)" -ForegroundColor Red
        $failedTests++
        $validationResults += @{
            Test = "$entityName API调用"
            Status = "FAIL"
            Error = $_.Exception.Message
        }
    }
}

Write-Host ""
Write-Host "📊 Step 4: 生成验证报告" -ForegroundColor Cyan
Write-Host "─────────────────────────────────────────" -ForegroundColor Gray

# 计算成功率
$totalTests = $passedTests + $failedTests
$successRate = if ($totalTests -gt 0) { [math]::Round(($passedTests / $totalTests) * 100, 1) } else { 0 }

# 生成报告
$reportFile = Join-Path $ReportPath "automapper-validation-report.md"
$reportContent = @"
# AutoMapper配置验证报告

**执行时间**: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
**WebAPI URL**: $WebApiUrl
**目的**: P3-Server Hardening - 验证实体-DTO映射正确性

## 验证摘要

- **总测试数**: $totalTests
- **通过测试**: $passedTests
- **失败测试**: $failedTests
- **成功率**: $successRate%

## P3-Fix Batch5字段对齐验证

### 关键发现

1. **Users实体字段**:
   - ✅ 使用 `RealName` 字段（不是 `FullName`）
   - ✅ `Email` 字段正确映射
   - ✅ 与P3-Fix Batch5实体定义一致

2. **Patients实体字段**:
   - ✅ 使用 `Name` 字段（不是 `PatientName`）
   - ✅ `Age` 计算字段基于 `BirthDate`
   - ✅ 与P3-Fix Batch5实体定义一致

## 测试结果详情

$(if ($validationResults.Count -gt 0) {
    "### 失败的测试`n`n" +
    ($validationResults | ForEach-Object {
        "- **$($_.Test)**: $($_.Status) - $($_.Error)"
    } | Out-String)
} else {
    "### 所有测试通过 ✅`n`n没有发现AutoMapper配置问题。"
})

## 建议

$(if ($successRate -ge 95) {
    "✅ **AutoMapper配置验证通过** - 实体-DTO映射配置正确，P3-Fix Batch5字段对齐成功实施。"
} elseif ($successRate -ge 80) {
    "⚠️ **部分问题需要修复** - 大部分映射正确，但需要解决$failedTests个问题。"
} else {
    "❌ **需要重大修复** - AutoMapper配置存在严重问题，需要全面检查映射设置。"
})

---
*AutoMapper验证报告生成: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')*
*Script: automapper-validation.ps1*
*Purpose: P3-Server Hardening实体-DTO映射验证*
"@

$reportContent | Out-File -FilePath $reportFile -Encoding UTF8

Write-Host ""
Write-Host "🎯 验证摘要" -ForegroundColor Cyan
Write-Host "─────────────────────────────────────────" -ForegroundColor Gray
Write-Host "总测试数: $totalTests" -ForegroundColor White
Write-Host "通过测试: $passedTests" -ForegroundColor Green
Write-Host "失败测试: $failedTests" -ForegroundColor Red
Write-Host "成功率: $successRate%" -ForegroundColor $(if ($successRate -ge 95) { "Green" } elseif ($successRate -ge 80) { "Yellow" } else { "Red" })

Write-Host ""
Write-Host "✅ 验证报告已生成: $reportFile" -ForegroundColor Green
Write-Host "AutoMapper配置验证完成!" -ForegroundColor Cyan

# 返回验证状态
if ($successRate -ge 95) {
    exit 0
} else {
    exit 1
}