# 综合API测试脚本
# 日期: 2025-10-09
# 版本: v1.0.0

param(
    [string]$BaseUrl = "http://localhost:5000",
    [switch]$Verbose = $false
)

# 配置
$headers = @{
    "Content-Type" = "application/json"
}

$testResults = @()
$testTotal = 0
$testPassed = 0
$testFailed = 0

# 输出函数
function Write-TestHeader {
    param([string]$Title)
    Write-Host "`n╔══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║  $Title" -ForegroundColor Cyan
    Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
}

function Write-TestResult {
    param(
        [string]$TestName,
        [bool]$Success,
        [string]$Message = "",
        [object]$Response = $null
    )

    $script:testTotal++
    $result = @{
        TestName = $TestName
        Success = $Success
        Message = $Message
        Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    }

    if ($Success) {
        $script:testPassed++
        Write-Host "✅ $TestName" -ForegroundColor Green
        if ($Message) { Write-Host "   $Message" -ForegroundColor Gray }
    } else {
        $script:testFailed++
        Write-Host "❌ $TestName" -ForegroundColor Red
        if ($Message) { Write-Host "   错误: $Message" -ForegroundColor Yellow }
    }

    if ($Verbose -and $Response) {
        Write-Host "   响应: $($Response | ConvertTo-Json -Compress)" -ForegroundColor DarkGray
    }

    $script:testResults += $result
}

# API测试函数
function Test-ApiEndpoint {
    param(
        [string]$Method,
        [string]$Endpoint,
        [object]$Body = $null,
        [hashtable]$Headers = @{},
        [string]$TestName,
        [int]$ExpectedStatus = 200
    )

    try {
        $uri = "$BaseUrl$Endpoint"
        $allHeaders = $headers.Clone()
        foreach ($key in $Headers.Keys) {
            $allHeaders[$key] = $Headers[$key]
        }

        $params = @{
            Uri = $uri
            Method = $Method
            Headers = $allHeaders
            ErrorAction = "Stop"
        }

        if ($Body) {
            $params.Body = ($Body | ConvertTo-Json -Depth 10)
        }

        $response = Invoke-RestMethod @params -StatusCodeVariable statusCode

        if ($statusCode -eq $ExpectedStatus) {
            Write-TestResult -TestName $TestName -Success $true -Message "状态码: $statusCode" -Response $response
            return @{ Success = $true; Data = $response; StatusCode = $statusCode }
        } else {
            Write-TestResult -TestName $TestName -Success $false -Message "期望状态码 $ExpectedStatus，实际 $statusCode"
            return @{ Success = $false; StatusCode = $statusCode }
        }
    }
    catch {
        $statusCode = 0
        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode
        }

        if ($statusCode -eq $ExpectedStatus) {
            Write-TestResult -TestName $TestName -Success $true -Message "状态码: $statusCode (预期失败)"
            return @{ Success = $true; StatusCode = $statusCode }
        } else {
            Write-TestResult -TestName $TestName -Success $false -Message $_.Exception.Message
            return @{ Success = $false; Error = $_.Exception.Message; StatusCode = $statusCode }
        }
    }
}

# 主测试流程
Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Magenta
Write-Host "║         LYBT API 综合测试套件 v1.0.0                      ║" -ForegroundColor Magenta
Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Magenta
Write-Host "测试环境: $BaseUrl" -ForegroundColor Cyan
Write-Host "开始时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Cyan
Write-Host ""

# 1. 认证模块测试
Write-TestHeader "认证模块测试"

# 1.1 测试Doctor登录
$doctorLogin = @{
    username = "doctor1"
    password = "Pass123!"
}
$loginResult = Test-ApiEndpoint -Method "POST" -Endpoint "/api/v1/auth/login" `
    -Body $doctorLogin -TestName "Doctor账号登录" -ExpectedStatus 200

$token = ""
if ($loginResult.Success) {
    $token = $loginResult.Data.data.token
    Write-Host "   Token获取成功 (前10字符): $($token.Substring(0, [Math]::Min(10, $token.Length)))..." -ForegroundColor Gray
}

# 1.2 测试Admin登录（预期失败）
$adminLogin = @{
    username = "sysadmin"
    password = "Admin123!"
}
Test-ApiEndpoint -Method "POST" -Endpoint "/api/v1/auth/login" `
    -Body $adminLogin -TestName "Admin账号登录（BCrypt问题）" -ExpectedStatus 200

# 1.3 测试无效凭据
$invalidLogin = @{
    username = "invalid"
    password = "wrong"
}
Test-ApiEndpoint -Method "POST" -Endpoint "/api/v1/auth/login" `
    -Body $invalidLogin -TestName "无效凭据登录" -ExpectedStatus 200

# 2. 授权头设置
if ($token) {
    $headers["Authorization"] = "Bearer $token"
}

# 3. 中药材模块测试
Write-TestHeader "中药材模块测试"

Test-ApiEndpoint -Method "GET" -Endpoint "/api/v1/herbs?page=1&pageSize=10" `
    -TestName "查询中药材列表" -ExpectedStatus 200

Test-ApiEndpoint -Method "GET" -Endpoint "/api/v1/herbs/search?keyword=甘草" `
    -TestName "搜索中药材" -ExpectedStatus 200

# 4. 验方模块测试
Write-TestHeader "验方模块测试"

Test-ApiEndpoint -Method "GET" -Endpoint "/api/v1/formulas?page=1&pageSize=10" `
    -TestName "查询验方列表（需认证）" -ExpectedStatus 200

Test-ApiEndpoint -Method "GET" -Endpoint "/api/v1/formulas/search?keyword=四君子汤" `
    -TestName "搜索验方" -ExpectedStatus 200

# 5. 患者模块测试
Write-TestHeader "患者模块测试"

# 5.1 查询患者列表
Test-ApiEndpoint -Method "GET" -Endpoint "/api/v1/patients?page=1&pageSize=10" `
    -TestName "查询患者列表" -ExpectedStatus 200

# 5.2 创建新患者
$newPatient = @{
    name = "测试患者_$(Get-Random -Maximum 9999)"
    gender = "Male"
    birthDate = "1990-01-01"
    phoneNumber = "138$(Get-Random -Minimum 10000000 -Maximum 99999999)"
    idCardNumber = "110101199001011234"
    address = "北京市测试地址"
}
$createResult = Test-ApiEndpoint -Method "POST" -Endpoint "/api/v1/patients" `
    -Body $newPatient -TestName "创建新患者" -ExpectedStatus 200

$patientId = ""
if ($createResult.Success -and $createResult.Data.data) {
    $patientId = $createResult.Data.data.id
    Write-Host "   创建患者ID: $patientId" -ForegroundColor Gray
}

# 5.3 查询单个患者
if ($patientId) {
    Test-ApiEndpoint -Method "GET" -Endpoint "/api/v1/patients/$patientId" `
        -TestName "查询单个患者" -ExpectedStatus 200
}

# 5.4 更新患者信息
if ($patientId) {
    $updatePatient = @{
        id = $patientId
        name = $newPatient.name
        gender = "Female"
        birthDate = $newPatient.birthDate
        phoneNumber = $newPatient.phoneNumber
        idCardNumber = $newPatient.idCardNumber
        address = "更新后的地址"
    }
    Test-ApiEndpoint -Method "PUT" -Endpoint "/api/v1/patients/$patientId" `
        -Body $updatePatient -TestName "更新患者信息" -ExpectedStatus 200
}

# 6. 病历模块测试
Write-TestHeader "病历模块测试"

Test-ApiEndpoint -Method "GET" -Endpoint "/api/v1/medicalcases?page=1&pageSize=10" `
    -TestName "查询病历列表" -ExpectedStatus 200

# 6.1 创建新病历
if ($patientId) {
    $newCase = @{
        patientId = $patientId
        chiefComplaint = "测试主诉：头痛发热"
        presentIllness = "昨日开始头痛，今日发热"
        diagnosis = "外感风寒"
        treatment = "解表散寒"
    }
    $caseResult = Test-ApiEndpoint -Method "POST" -Endpoint "/api/v1/medicalcases" `
        -Body $newCase -TestName "创建新病历" -ExpectedStatus 200

    if ($caseResult.Success -and $caseResult.Data.data) {
        $caseId = $caseResult.Data.data.id
        Write-Host "   创建病历ID: $caseId" -ForegroundColor Gray
    }
}

# 7. 处方模块测试
Write-TestHeader "处方模块测试"

Test-ApiEndpoint -Method "GET" -Endpoint "/api/v1/prescriptions?page=1&pageSize=10" `
    -TestName "查询处方列表" -ExpectedStatus 200

# 8. 咨询模块测试
Write-TestHeader "咨询模块测试"

Test-ApiEndpoint -Method "GET" -Endpoint "/api/v1/consultations?page=1&pageSize=10" `
    -TestName "查询咨询列表" -ExpectedStatus 200

# 9. 用户管理模块测试（需要管理员权限）
Write-TestHeader "用户管理模块测试"

Test-ApiEndpoint -Method "GET" -Endpoint "/api/v1/users?page=1&pageSize=10" `
    -TestName "查询用户列表（需管理员权限）" -ExpectedStatus 200

# 10. 系统健康检查
Write-TestHeader "系统健康检查"

Test-ApiEndpoint -Method "GET" -Endpoint "/health" `
    -TestName "健康检查端点" -ExpectedStatus 200

Test-ApiEndpoint -Method "GET" -Endpoint "/api/health" `
    -TestName "API健康检查" -ExpectedStatus 200

# 生成测试报告
Write-Host "`n╔══════════════════════════════════════════════════════════╗" -ForegroundColor Magenta
Write-Host "║                    测试结果汇总                           ║" -ForegroundColor Magenta
Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Magenta

Write-Host "总测试数: $testTotal" -ForegroundColor Cyan
Write-Host "通过数: $testPassed" -ForegroundColor Green
Write-Host "失败数: $testFailed" -ForegroundColor $(if ($testFailed -gt 0) { "Red" } else { "Gray" })
Write-Host "通过率: $([Math]::Round(($testPassed / $testTotal) * 100, 2))%" -ForegroundColor $(if ($testPassed -eq $testTotal) { "Green" } else { "Yellow" })
Write-Host "完成时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Cyan

# 导出结果到JSON
$reportFile = "api-test-results-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
$report = @{
    TestDate = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Environment = $BaseUrl
    Summary = @{
        Total = $testTotal
        Passed = $testPassed
        Failed = $testFailed
        PassRate = "$([Math]::Round(($testPassed / $testTotal) * 100, 2))%"
    }
    Results = $testResults
}

$report | ConvertTo-Json -Depth 10 | Out-File $reportFile -Encoding UTF8
Write-Host "`n测试报告已保存至: $reportFile" -ForegroundColor Green

# 返回测试状态
exit $(if ($testFailed -eq 0) { 0 } else { 1 })