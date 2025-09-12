# P3 Record-Only Smoke Validation - API 冒烟测试脚本
# 目标：自动化验证4个核心模块的CRUD操作和历史查询功能

param(
    [string[]]$Modules = @("Herbs", "Formula", "Patients", "Consultation", "Prescriptions"),
    [switch]$SkipCleanup = $false,
    [switch]$Verbose = $false,
    [int]$TimeoutSeconds = 30,
    [string]$BaseUrl = "https://localhost:7001"
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# 脚本配置
$VALIDATION_LOG = Join-Path $PSScriptRoot "api-smoke-results.json"
$TEST_DATA_LOG = Join-Path $PSScriptRoot "smoke-test-data.json"

# API配置
$API_BASE = "$BaseUrl/api/v1"
$HEADERS = @{
    "Content-Type" = "application/json"
    "Accept" = "application/json"
}

# 测试数据存储
$script:CreatedData = @{
    Herbs = @()
    Formula = @()  
    Patients = @()
    Consultation = @()
    Prescriptions = @()
}

# 测试结果存储
$script:TestResults = @{
    StartTime = Get-Date
    EndTime = $null
    TotalTests = 0
    PassedTests = 0
    FailedTests = 0
    Modules = @{}
    Errors = @()
    Summary = ""
}

Write-Host "=== P3 Record-Only API 冒烟测试 ===" -ForegroundColor Cyan
Write-Host "时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host "目标: 验证Record-Only模式CRUD操作和历史查询" -ForegroundColor Gray
Write-Host "API基础地址: $API_BASE" -ForegroundColor Gray
Write-Host ""

function Write-TestLog {
    param([string]$Message, [string]$Level = "INFO")
    $timestamp = Get-Date -Format "HH:mm:ss"
    $logEntry = "[$timestamp] [$Level] $Message"
    if ($Verbose -or $Level -eq "ERROR" -or $Level -eq "WARN") {
        Write-Host $logEntry -ForegroundColor $(if ($Level -eq "ERROR") { "Red" } elseif ($Level -eq "WARN") { "Yellow" } else { "White" })
    }
}

function Invoke-ApiCall {
    param(
        [string]$Endpoint,
        [string]$Method = "GET",
        [hashtable]$Body = $null,
        [string]$Description = ""
    )
    
    $uri = "$API_BASE$Endpoint"
    $script:TestResults.TotalTests++
    
    try {
        $params = @{
            Uri = $uri
            Method = $Method
            Headers = $HEADERS
            TimeoutSec = $TimeoutSeconds
        }
        
        if ($Body -and ($Method -eq "POST" -or $Method -eq "PUT")) {
            $params.Body = ($Body | ConvertTo-Json -Depth 10)
        }
        
        Write-TestLog "执行API调用: $Method $Endpoint $(if($Description) { "($Description)" })" "INFO"
        
        $response = Invoke-RestMethod @params -ErrorAction Stop
        $script:TestResults.PassedTests++
        
        Write-TestLog "✅ 成功: $Description" "INFO"
        return @{ Success = $true; Data = $response; StatusCode = 200 }
        
    } catch {
        $script:TestResults.FailedTests++
        $errorMsg = "API调用失败: $Method $Endpoint - $($_.Exception.Message)"
        $script:TestResults.Errors += @{
            Endpoint = $Endpoint
            Method = $Method
            Description = $Description
            Error = $_.Exception.Message
            Timestamp = Get-Date
        }
        
        Write-TestLog "❌ 失败: $errorMsg" "ERROR"
        return @{ Success = $false; Error = $_.Exception.Message; StatusCode = 500 }
    }
}

function Test-HealthCheck {
    Write-Host "验证API健康状态..." -ForegroundColor Yellow
    
    $result = Invoke-ApiCall -Endpoint "/health" -Description "系统健康检查"
    if (-not $result.Success) {
        throw "健康检查失败，API服务不可用: $($result.Error)"
    }
    
    Write-Host "✅ API服务健康检查通过" -ForegroundColor Green
    return $result.Data
}

function Test-HerbsModule {
    Write-Host "`n=== 测试 Herbs 模块 ===" -ForegroundColor Cyan
    
    $moduleResults = @{
        Create = $false
        Read = $false
        Update = $false
        Delete = $false
        List = $false
    }
    
    # 测试数据
    $herbData = @{
        name = "当归"
        category = "补血药"
        properties = "甘、辛，温"
        meridians = "心、肝、脾经"
        effects = "补血调经，活血止痛"
        dosage = "5-15克"
        price = 0.80
    }
    
    try {
        # 创建药材
        $createResult = Invoke-ApiCall -Endpoint "/herbs" -Method "POST" -Body $herbData -Description "创建药材"
        if ($createResult.Success) {
            $moduleResults.Create = $true
            $herbId = $createResult.Data.data.id
            $script:CreatedData.Herbs += $herbId
            Write-TestLog "创建药材成功，ID: $herbId" "INFO"
            
            # 查询单个药材
            $getResult = Invoke-ApiCall -Endpoint "/herbs/$herbId" -Description "查询单个药材"
            if ($getResult.Success) {
                $moduleResults.Read = $true
                Write-TestLog "药材查询成功: $($getResult.Data.data.name)" "INFO"
            }
            
            # 更新药材
            $updateData = @{ price = 0.90 }
            $updateResult = Invoke-ApiCall -Endpoint "/herbs/$herbId" -Method "PUT" -Body $updateData -Description "更新药材价格"
            if ($updateResult.Success) {
                $moduleResults.Update = $true
                Write-TestLog "药材价格更新成功" "INFO"
            }
        }
        
        # 查询药材列表
        $listResult = Invoke-ApiCall -Endpoint "/herbs?page=1&pageSize=10" -Description "查询药材列表"
        if ($listResult.Success) {
            $moduleResults.List = $true
            Write-TestLog "药材列表查询成功，共 $($listResult.Data.data.totalCount) 条记录" "INFO"
        }
        
        # 删除测试（可选）
        if (-not $SkipCleanup -and $script:CreatedData.Herbs.Count -gt 0) {
            foreach ($id in $script:CreatedData.Herbs) {
                $deleteResult = Invoke-ApiCall -Endpoint "/herbs/$id" -Method "DELETE" -Description "删除测试药材"
                if ($deleteResult.Success) {
                    $moduleResults.Delete = $true
                    Write-TestLog "测试药材删除成功" "INFO"
                }
            }
        }
        
    } catch {
        Write-TestLog "Herbs模块测试异常: $($_.Exception.Message)" "ERROR"
    }
    
    $script:TestResults.Modules.Herbs = $moduleResults
    $passCount = ($moduleResults.Values | Where-Object { $_ -eq $true }).Count
    Write-Host "Herbs模块测试完成：$passCount/5 通过" -ForegroundColor $(if($passCount -eq 5) { "Green" } else { "Yellow" })
}

function Test-FormulaModule {
    Write-Host "`n=== 测试 Formula 模块 ===" -ForegroundColor Cyan
    
    $moduleResults = @{
        Create = $false
        Read = $false
        Update = $false
        Delete = $false
        List = $false
    }
    
    # 测试数据
    $formulaData = @{
        name = "四君子汤"
        category = "补气方"
        effect = "益气健脾"
        usage = "水煎服"
        isShared = $true
        herbs = @(
            @{ herbName = "人参"; dosage = 9.0; unit = "克" },
            @{ herbName = "白术"; dosage = 9.0; unit = "克" }
        )
    }
    
    try {
        # 创建验方
        $createResult = Invoke-ApiCall -Endpoint "/formulas" -Method "POST" -Body $formulaData -Description "创建验方"
        if ($createResult.Success) {
            $moduleResults.Create = $true
            $formulaId = $createResult.Data.data.id
            $script:CreatedData.Formula += $formulaId
            Write-TestLog "创建验方成功，ID: $formulaId" "INFO"
            
            # 查询单个验方
            $getResult = Invoke-ApiCall -Endpoint "/formulas/$formulaId" -Description "查询单个验方"
            if ($getResult.Success) {
                $moduleResults.Read = $true
                Write-TestLog "验方查询成功: $($getResult.Data.data.name)" "INFO"
            }
            
            # 更新验方
            $updateData = @{ effect = "益气健脾，调和脾胃" }
            $updateResult = Invoke-ApiCall -Endpoint "/formulas/$formulaId" -Method "PUT" -Body $updateData -Description "更新验方功效"
            if ($updateResult.Success) {
                $moduleResults.Update = $true
                Write-TestLog "验方功效更新成功" "INFO"
            }
        }
        
        # 查询验方列表
        $listResult = Invoke-ApiCall -Endpoint "/formulas?page=1&pageSize=10" -Description "查询验方列表"
        if ($listResult.Success) {
            $moduleResults.List = $true
            Write-TestLog "验方列表查询成功，共 $($listResult.Data.data.totalCount) 条记录" "INFO"
        }
        
        # 删除测试（可选）
        if (-not $SkipCleanup -and $script:CreatedData.Formula.Count -gt 0) {
            foreach ($id in $script:CreatedData.Formula) {
                $deleteResult = Invoke-ApiCall -Endpoint "/formulas/$id" -Method "DELETE" -Description "删除测试验方"
                if ($deleteResult.Success) {
                    $moduleResults.Delete = $true
                    Write-TestLog "测试验方删除成功" "INFO"
                }
            }
        }
        
    } catch {
        Write-TestLog "Formula模块测试异常: $($_.Exception.Message)" "ERROR"
    }
    
    $script:TestResults.Modules.Formula = $moduleResults
    $passCount = ($moduleResults.Values | Where-Object { $_ -eq $true }).Count
    Write-Host "Formula模块测试完成：$passCount/5 通过" -ForegroundColor $(if($passCount -eq 5) { "Green" } else { "Yellow" })
}

function Test-PatientsModule {
    Write-Host "`n=== 测试 Patients 模块 ===" -ForegroundColor Cyan
    
    $moduleResults = @{
        Create = $false
        Read = $false
        Update = $false
        Delete = $false
        List = $false
    }
    
    # 测试数据
    $patientData = @{
        name = "测试患者01"
        gender = "Male"
        birthDate = "1990-01-01T00:00:00Z"
        phone = "13800138001"
        address = "测试地址123号"
    }
    
    try {
        # 创建患者
        $createResult = Invoke-ApiCall -Endpoint "/patients" -Method "POST" -Body $patientData -Description "创建患者"
        if ($createResult.Success) {
            $moduleResults.Create = $true
            $patientId = $createResult.Data.data.id
            $script:CreatedData.Patients += $patientId
            Write-TestLog "创建患者成功，ID: $patientId" "INFO"
            
            # 查询单个患者
            $getResult = Invoke-ApiCall -Endpoint "/patients/$patientId" -Description "查询单个患者"
            if ($getResult.Success) {
                $moduleResults.Read = $true
                Write-TestLog "患者查询成功: $($getResult.Data.data.name)" "INFO"
            }
            
            # 更新患者
            $updateData = @{ address = "测试地址456号" }
            $updateResult = Invoke-ApiCall -Endpoint "/patients/$patientId" -Method "PUT" -Body $updateData -Description "更新患者地址"
            if ($updateResult.Success) {
                $moduleResults.Update = $true
                Write-TestLog "患者地址更新成功" "INFO"
            }
        }
        
        # 查询患者列表
        $listResult = Invoke-ApiCall -Endpoint "/patients?page=1&pageSize=10" -Description "查询患者列表"
        if ($listResult.Success) {
            $moduleResults.List = $true
            Write-TestLog "患者列表查询成功，共 $($listResult.Data.data.totalCount) 条记录" "INFO"
        }
        
        # 软删除测试（可选）
        if (-not $SkipCleanup -and $script:CreatedData.Patients.Count -gt 0) {
            foreach ($id in $script:CreatedData.Patients) {
                $deleteResult = Invoke-ApiCall -Endpoint "/patients/$id" -Method "DELETE" -Description "软删除测试患者"
                if ($deleteResult.Success) {
                    $moduleResults.Delete = $true
                    Write-TestLog "测试患者软删除成功" "INFO"
                }
            }
        }
        
    } catch {
        Write-TestLog "Patients模块测试异常: $($_.Exception.Message)" "ERROR"
    }
    
    $script:TestResults.Modules.Patients = $moduleResults
    $passCount = ($moduleResults.Values | Where-Object { $_ -eq $true }).Count
    Write-Host "Patients模块测试完成：$passCount/5 通过" -ForegroundColor $(if($passCount -eq 5) { "Green" } else { "Yellow" })
}

function Test-ConsultationModule {
    Write-Host "`n=== 测试 Consultation 模块 ===" -ForegroundColor Cyan
    
    $moduleResults = @{
        Create = $false
        Read = $false
        History = $false
    }
    
    # 需要患者ID，使用已创建的患者或创建新患者
    $patientId = if ($script:CreatedData.Patients.Count -gt 0) {
        $script:CreatedData.Patients[0]
    } else {
        # 创建临时患者
        $tempPatient = @{
            name = "诊断测试患者"
            gender = "Female"
            birthDate = "1985-05-15T00:00:00Z"
            phone = "13900139002"
            address = "临时地址"
        }
        $tempResult = Invoke-ApiCall -Endpoint "/patients" -Method "POST" -Body $tempPatient -Description "创建临时患者用于诊断测试"
        if ($tempResult.Success) {
            $tempId = $tempResult.Data.data.id
            $script:CreatedData.Patients += $tempId
            $tempId
        } else {
            $null
        }
    }
    
    if (-not $patientId) {
        Write-TestLog "无法获取患者ID，跳过Consultation模块测试" "WARN"
        return
    }
    
    # 测试数据
    $consultationData = @{
        patientId = $patientId
        medicalCaseId = [System.Guid]::NewGuid().ToString()
        chiefComplaint = "头痛3天"
        presentIllness = "患者3天前开始出现头痛，持续性胀痛"
        inspection = "面色稍黄，精神可"
        auscultation = "语音清晰"
        inquiry = "睡眠一般，大小便正常"  
        palpation = "脉象弦细，舌质淡红苔薄白"
        diagnosis = "头痛（肝阳上亢）"
        treatment = "平肝潜阳，镇静止痛"
    }
    
    try {
        # 创建诊断
        $createResult = Invoke-ApiCall -Endpoint "/consultations" -Method "POST" -Body $consultationData -Description "创建诊断记录"
        if ($createResult.Success) {
            $moduleResults.Create = $true
            $consultationId = $createResult.Data.data.id
            $script:CreatedData.Consultation += $consultationId
            Write-TestLog "创建诊断记录成功，ID: $consultationId" "INFO"
            
            # 查询单个诊断
            $getResult = Invoke-ApiCall -Endpoint "/consultations/$consultationId" -Description "查询单个诊断记录"
            if ($getResult.Success) {
                $moduleResults.Read = $true
                Write-TestLog "诊断记录查询成功" "INFO"
            }
        }
        
        # 查询患者历史诊断
        $historyResult = Invoke-ApiCall -Endpoint "/consultations/patient/$patientId" -Description "查询患者诊疗历史"
        if ($historyResult.Success) {
            $moduleResults.History = $true
            Write-TestLog "患者诊疗历史查询成功，共 $($historyResult.Data.data.count) 条记录" "INFO"
        }
        
    } catch {
        Write-TestLog "Consultation模块测试异常: $($_.Exception.Message)" "ERROR"
    }
    
    $script:TestResults.Modules.Consultation = $moduleResults
    $passCount = ($moduleResults.Values | Where-Object { $_ -eq $true }).Count
    Write-Host "Consultation模块测试完成：$passCount/3 通过" -ForegroundColor $(if($passCount -eq 3) { "Green" } else { "Yellow" })
}

function Test-PrescriptionsModule {
    Write-Host "`n=== 测试 Prescriptions 模块 ===" -ForegroundColor Cyan
    
    $moduleResults = @{
        Create = $false
        Read = $false
        List = $false
        Delete = $false
    }
    
    # 需要患者ID和药材ID
    $patientId = if ($script:CreatedData.Patients.Count -gt 0) {
        $script:CreatedData.Patients[0]
    } else { $null }
        
    if (-not $patientId) {
        Write-TestLog "无法获取患者ID，跳过Prescriptions模块测试" "WARN"
        return
    }
    
    # 测试数据
    $prescriptionData = @{
        patientId = $patientId
        consultationId = [System.Guid]::NewGuid().ToString()
        prescriptionName = "测试处方01"
        usage = "每日3次，饭后服用"
        dosage = "每次10克"
        items = @(
            @{
                herbId = [System.Guid]::NewGuid().ToString()
                quantity = 15.0
                unit = "克"
                usage = "先煎"
            },
            @{
                herbId = [System.Guid]::NewGuid().ToString()
                quantity = 10.0
                unit = "克"
                usage = "后下"
            }
        )
    }
    
    try {
        # 创建处方
        $createResult = Invoke-ApiCall -Endpoint "/prescriptions" -Method "POST" -Body $prescriptionData -Description "创建处方"
        if ($createResult.Success) {
            $moduleResults.Create = $true
            $prescriptionId = $createResult.Data.data.id
            $script:CreatedData.Prescriptions += $prescriptionId
            Write-TestLog "创建处方成功，ID: $prescriptionId" "INFO"
            
            # 查询单个处方
            $getResult = Invoke-ApiCall -Endpoint "/prescriptions/$prescriptionId" -Description "查询单个处方"
            if ($getResult.Success) {
                $moduleResults.Read = $true
                Write-TestLog "处方查询成功: $($getResult.Data.data.prescriptionName)" "INFO"
            }
        }
        
        # 查询处方列表
        $listResult = Invoke-ApiCall -Endpoint "/prescriptions?page=1&pageSize=10" -Description "查询处方列表"
        if ($listResult.Success) {
            $moduleResults.List = $true
            Write-TestLog "处方列表查询成功，共 $($listResult.Data.data.totalCount) 条记录" "INFO"
        }
        
        # 删除测试（可选）
        if (-not $SkipCleanup -and $script:CreatedData.Prescriptions.Count -gt 0) {
            foreach ($id in $script:CreatedData.Prescriptions) {
                $deleteResult = Invoke-ApiCall -Endpoint "/prescriptions/$id" -Method "DELETE" -Description "删除测试处方"
                if ($deleteResult.Success) {
                    $moduleResults.Delete = $true
                    Write-TestLog "测试处方删除成功" "INFO"
                }
            }
        }
        
    } catch {
        Write-TestLog "Prescriptions模块测试异常: $($_.Exception.Message)" "ERROR"
    }
    
    $script:TestResults.Modules.Prescriptions = $moduleResults
    $passCount = ($moduleResults.Values | Where-Object { $_ -eq $true }).Count
    Write-Host "Prescriptions模块测试完成：$passCount/4 通过" -ForegroundColor $(if($passCount -eq 4) { "Green" } else { "Yellow" })
}

function Save-TestResults {
    $script:TestResults.EndTime = Get-Date
    $duration = $script:TestResults.EndTime - $script:TestResults.StartTime
    
    # 计算总体统计
    $totalModuleTests = 0
    $passedModuleTests = 0
    
    foreach ($module in $script:TestResults.Modules.Keys) {
        $moduleResult = $script:TestResults.Modules[$module]
        $moduleTotal = $moduleResult.Values.Count
        $modulePassed = ($moduleResult.Values | Where-Object { $_ -eq $true }).Count
        
        $totalModuleTests += $moduleTotal
        $passedModuleTests += $modulePassed
    }
    
    $script:TestResults.Summary = @"
P3 Record-Only API 冒烟测试完成
=================================

执行时间: $($script:TestResults.StartTime.ToString('yyyy-MM-dd HH:mm:ss')) - $($script:TestResults.EndTime.ToString('yyyy-MM-dd HH:mm:ss'))
总耗时: $([math]::Round($duration.TotalSeconds, 2)) 秒

API调用统计:
- 总调用次数: $($script:TestResults.TotalTests)
- 成功次数: $($script:TestResults.PassedTests)  
- 失败次数: $($script:TestResults.FailedTests)
- 成功率: $([math]::Round($script:TestResults.PassedTests / $script:TestResults.TotalTests * 100, 1))%

模块功能测试:
- 总测试项: $totalModuleTests
- 通过项目: $passedModuleTests
- 通过率: $([math]::Round($passedModuleTests / $totalModuleTests * 100, 1))%

测试状态: $(if ($script:TestResults.FailedTests -eq 0 -and $passedModuleTests -eq $totalModuleTests) { "✅ PASS" } else { "❌ FAIL" })
"@
    
    # 保存结果到JSON文件
    $script:TestResults | ConvertTo-Json -Depth 10 | Out-File -FilePath $VALIDATION_LOG -Encoding UTF8
    $script:CreatedData | ConvertTo-Json -Depth 10 | Out-File -FilePath $TEST_DATA_LOG -Encoding UTF8
    
    Write-Host "`n$($script:TestResults.Summary)" -ForegroundColor $(if ($script:TestResults.FailedTests -eq 0) { "Green" } else { "Red" })
    Write-Host "`n详细结果已保存到: $VALIDATION_LOG" -ForegroundColor Gray
    Write-Host "测试数据记录: $TEST_DATA_LOG" -ForegroundColor Gray
}

# 主执行流程
try {
    # 健康检查
    Test-HealthCheck
    
    # 按顺序执行模块测试
    foreach ($module in $Modules) {
        switch ($module) {
            "Herbs" { Test-HerbsModule }
            "Formula" { Test-FormulaModule }
            "Patients" { Test-PatientsModule }
            "Consultation" { Test-ConsultationModule }
            "Prescriptions" { Test-PrescriptionsModule }
            default { Write-TestLog "未知模块: $module" "WARN" }
        }
    }
    
} catch {
    Write-TestLog "冒烟测试执行异常: $($_.Exception.Message)" "ERROR"
    $script:TestResults.Errors += @{
        Type = "Script Error"
        Error = $_.Exception.Message
        Timestamp = Get-Date
    }
} finally {
    # 保存测试结果
    Save-TestResults
    
    Write-Host "`n冒烟测试执行完成！" -ForegroundColor Cyan
    if ($script:TestResults.FailedTests -gt 0) {
        Write-Host "存在失败项，请检查详细日志" -ForegroundColor Red
        exit 1
    } else {
        Write-Host "所有测试通过" -ForegroundColor Green
        exit 0
    }
}