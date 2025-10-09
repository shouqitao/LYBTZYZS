# 简化API测试脚本 - 兼容PowerShell 5.1
# 日期: 2025-10-09
# 版本: v1.0.0

param(
    [string]$BaseUrl = "http://localhost:5000"
)

Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║         LYBT API 测试套件 - 简化版                        ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host "测试环境: $BaseUrl" -ForegroundColor Yellow
Write-Host "开始时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Yellow
Write-Host ""

$testResults = @()
$passCount = 0
$failCount = 0

function Test-Api {
    param(
        [string]$Method,
        [string]$Endpoint,
        [object]$Body = $null,
        [string]$Token = "",
        [string]$TestName
    )

    Write-Host "测试: $TestName" -NoNewline

    try {
        $uri = "$BaseUrl$Endpoint"
        $headers = @{
            "Content-Type" = "application/json"
        }

        if ($Token) {
            $headers["Authorization"] = "Bearer $Token"
        }

        $params = @{
            Uri = $uri
            Method = $Method
            Headers = $headers
            UseBasicParsing = $true
        }

        if ($Body) {
            $params.Body = ($Body | ConvertTo-Json -Depth 10)
        }

        $response = Invoke-WebRequest @params
        $statusCode = $response.StatusCode
        $content = $response.Content | ConvertFrom-Json

        Write-Host " ✅ 通过 (状态码: $statusCode)" -ForegroundColor Green
        $script:passCount++

        return @{
            Success = $true
            StatusCode = $statusCode
            Data = $content
        }
    }
    catch {
        $errorMessage = $_.Exception.Message
        $statusCode = 0

        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode.Value__
        }

        Write-Host " ❌ 失败 (状态码: $statusCode)" -ForegroundColor Red
        Write-Host "     错误: $errorMessage" -ForegroundColor Yellow
        $script:failCount++

        return @{
            Success = $false
            StatusCode = $statusCode
            Error = $errorMessage
        }
    }
}

# 1. 认证测试
Write-Host "`n═══ 认证模块测试 ═══" -ForegroundColor Cyan

$loginResult = Test-Api -Method "POST" -Endpoint "/api/v1/auth/login" `
    -Body @{username="doctor1"; password="Pass123!"} `
    -TestName "Doctor登录"

$token = ""
if ($loginResult.Success -and $loginResult.Data.data.token) {
    $token = $loginResult.Data.data.token
    Write-Host "     Token获取成功" -ForegroundColor Gray
}

Test-Api -Method "POST" -Endpoint "/api/v1/auth/login" `
    -Body @{username="sysadmin"; password="Admin123!"} `
    -TestName "Admin登录(预期失败)"

# 2. 中药材测试
Write-Host "`n═══ 中药材模块测试 ═══" -ForegroundColor Cyan

Test-Api -Method "GET" -Endpoint "/api/v1/herbs?page=1&pageSize=5" `
    -Token $token -TestName "查询中药材"

# 3. 验方测试
Write-Host "`n═══ 验方模块测试 ═══" -ForegroundColor Cyan

Test-Api -Method "GET" -Endpoint "/api/v1/formulas?page=1&pageSize=5" `
    -Token $token -TestName "查询验方"

# 4. 患者测试
Write-Host "`n═══ 患者模块测试 ═══" -ForegroundColor Cyan

Test-Api -Method "GET" -Endpoint "/api/v1/patients?page=1&pageSize=5" `
    -Token $token -TestName "查询患者列表"

$patientData = @{
    name = "测试患者_$(Get-Random -Maximum 999)"
    gender = "Male"
    birthDate = "1990-01-01"
    phoneNumber = "13800138000"
    idCardNumber = "110101199001011234"
}

$createResult = Test-Api -Method "POST" -Endpoint "/api/v1/patients" `
    -Body $patientData -Token $token -TestName "创建患者"

if ($createResult.Success -and $createResult.Data.data.id) {
    $patientId = $createResult.Data.data.id
    Test-Api -Method "GET" -Endpoint "/api/v1/patients/$patientId" `
        -Token $token -TestName "查询单个患者"
}

# 5. 病历测试
Write-Host "`n═══ 病历模块测试 ═══" -ForegroundColor Cyan

Test-Api -Method "GET" -Endpoint "/api/v1/medicalcases?page=1&pageSize=5" `
    -Token $token -TestName "查询病历"

# 6. 处方测试
Write-Host "`n═══ 处方模块测试 ═══" -ForegroundColor Cyan

Test-Api -Method "GET" -Endpoint "/api/v1/prescriptions?page=1&pageSize=5" `
    -Token $token -TestName "查询处方"

# 7. 健康检查
Write-Host "`n═══ 系统健康检查 ═══" -ForegroundColor Cyan

Test-Api -Method "GET" -Endpoint "/health" -TestName "健康检查"

# 测试汇总
Write-Host "`n╔══════════════════════════════════════════════════════════╗" -ForegroundColor Magenta
Write-Host "║                    测试结果汇总                           ║" -ForegroundColor Magenta
Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Magenta

$totalTests = $passCount + $failCount
$passRate = if ($totalTests -gt 0) { [Math]::Round(($passCount / $totalTests) * 100, 2) } else { 0 }

Write-Host "总测试数: $totalTests" -ForegroundColor Cyan
Write-Host "通过: $passCount" -ForegroundColor Green
Write-Host "失败: $failCount" -ForegroundColor $(if ($failCount -gt 0) { "Red" } else { "Gray" })
Write-Host "通过率: $passRate%" -ForegroundColor $(if ($passRate -eq 100) { "Green" } else { "Yellow" })
Write-Host "完成时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Cyan

# 导出结果
$report = @{
    TestDate = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Environment = $BaseUrl
    TotalTests = $totalTests
    Passed = $passCount
    Failed = $failCount
    PassRate = "$passRate%"
}

$reportFile = "api-test-report-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
$report | ConvertTo-Json | Out-File $reportFile -Encoding UTF8
Write-Host "`n测试报告已保存至: $reportFile" -ForegroundColor Green