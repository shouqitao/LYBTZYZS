# API端点测试脚本
$baseUrl = "https://localhost:7001"
$token = "" # 将在登录后填充

# 颜色输出函数
function Write-Success { param($msg) Write-Host $msg -ForegroundColor Green }
function Write-Error { param($msg) Write-Host $msg -ForegroundColor Red }
function Write-Info { param($msg) Write-Host $msg -ForegroundColor Cyan }

# 忽略SSL证书错误
add-type @"
    using System.Net;
    using System.Security.Cryptography.X509Certificates;
    public class TrustAllCertsPolicy : ICertificatePolicy {
        public bool CheckValidationResult(
            ServicePoint srvPoint, X509Certificate certificate,
            WebRequest request, int certificateProblem) {
            return true;
        }
    }
"@
[System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

# 登录获取Token
Write-Info "`n=== 登录获取Token ==="
try {
    $loginBody = @{
        username = "sysadmin"
        password = "Admin@123456"
        rememberMe = $false
    } | ConvertTo-Json

    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/v1/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    
    if ($loginResponse.success -and $loginResponse.data.token) {
        $token = $loginResponse.data.token
        Write-Success "✓ 登录成功，获取到Token"
    } else {
        Write-Error "✗ 登录失败"
        exit
    }
} catch {
    Write-Error "✗ 登录请求失败: $_"
    exit
}

# 设置请求头
$headers = @{
    Authorization = "Bearer $token"
    "Content-Type" = "application/json"
}

# 测试函数
function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Url,
        [object]$Body = $null
    )
    
    Write-Info "`n--- 测试: $Name ---"
    Write-Host "Method: $Method"
    Write-Host "URL: $Url"
    
    try {
        $params = @{
            Uri = $Url
            Method = $Method
            Headers = $headers
        }
        
        if ($Body) {
            $params.Body = $Body | ConvertTo-Json -Depth 10
        }
        
        $response = Invoke-RestMethod @params
        Write-Success "✓ 请求成功"
        return $true
    } catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        Write-Error "✗ 请求失败 - 状态码: $statusCode"
        Write-Error "错误信息: $_"
        return $false
    }
}

Write-Info "`n========== 开始测试API端点 =========="

# 1. 测试用户管理
Test-Endpoint -Name "用户管理 - 获取列表" -Method "GET" -Url "$baseUrl/api/v1/users"

# 2. 测试患者管理
Test-Endpoint -Name "患者管理 - 获取列表" -Method "GET" -Url "$baseUrl/api/v1/patients"
Test-Endpoint -Name "患者管理 - 分页查询" -Method "POST" -Url "$baseUrl/api/v1/patients/paged" -Body @{
    pageIndex = 1
    pageSize = 10
}

# 3. 测试药材管理
Test-Endpoint -Name "药材管理 - 获取列表" -Method "GET" -Url "$baseUrl/api/v1/herbs"
Test-Endpoint -Name "药材管理 - 分页查询" -Method "POST" -Url "$baseUrl/api/v1/herbs/paged" -Body @{
    pageIndex = 1
    pageSize = 10
}

# 4. 测试验方模板
Test-Endpoint -Name "验方模板 - 获取列表" -Method "GET" -Url "$baseUrl/api/v1/FormulaTemplate"
Test-Endpoint -Name "验方模板 - 获取列表(小写)" -Method "GET" -Url "$baseUrl/api/v1/formulatemplate"
Test-Endpoint -Name "验方模板 - 获取列表(复数)" -Method "GET" -Url "$baseUrl/api/v1/FormulaTemplates"

# 5. 测试挂号管理
Test-Endpoint -Name "挂号管理 - 获取列表" -Method "GET" -Url "$baseUrl/api/v1/registration"
Test-Endpoint -Name "挂号管理 - 获取列表(复数)" -Method "GET" -Url "$baseUrl/api/v1/registrations"
Test-Endpoint -Name "挂号管理 - 分页查询" -Method "POST" -Url "$baseUrl/api/v1/registration/paged" -Body @{
    pageIndex = 1
    pageSize = 10
}

# 6. 测试病历管理
Test-Endpoint -Name "病历管理 - 获取列表" -Method "GET" -Url "$baseUrl/api/v1/records"
Test-Endpoint -Name "病历管理 - 获取今日病历" -Method "GET" -Url "$baseUrl/api/v1/records/today"

Write-Info "`n========== 测试完成 =========="