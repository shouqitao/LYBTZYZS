# 凌隐宝堂中医诊所系统 - 快速冒烟测试脚本
# UltraThink Phase 3 实用化优化 - 快速验证核心功能

param(
    [string]$BaseUrl = "https://localhost:7001"
)

Write-Host "🏥 凌隐宝堂中医诊所系统 - 快速冒烟测试" -ForegroundColor Cyan
Write-Host "目标地址: $BaseUrl" -ForegroundColor Yellow
Write-Host ""

# 忽略SSL证书错误（开发环境）
if (-not ([System.Management.Automation.PSTypeName]'ServerCertificateValidationCallback').Type) {
    $certCallback = @"
        using System;
        using System.Net;
        using System.Net.Security;
        using System.Security.Cryptography.X509Certificates;
        public class ServerCertificateValidationCallback {
            public static void Ignore() {
                if(ServicePointManager.ServerCertificateValidationCallback == null) {
                    ServicePointManager.ServerCertificateValidationCallback += 
                        delegate(Object obj, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors) {
                            return true;
                        };
                }
            }
        }
"@
    Add-Type $certCallback
}
[ServerCertificateValidationCallback]::Ignore()

$testCount = 0
$passCount = 0

function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Url,
        [string]$Method = "GET",
        [object]$Body = $null,
        [hashtable]$Headers = @{}
    )
    
    $global:testCount++
    Write-Host "[$global:testCount] 测试 $Name..." -NoNewline -ForegroundColor Cyan
    
    try {
        $parameters = @{
            Uri = $Url
            Method = $Method
            Headers = $Headers
            TimeoutSec = 10
            UseBasicParsing = $true
        }
        
        if ($Body -and ($Method -ne "GET")) {
            $parameters.Body = $Body
            $parameters.ContentType = "application/json"
        }
        
        $response = Invoke-RestMethod @parameters
        
        Write-Host " ✅ 通过" -ForegroundColor Green
        $global:passCount++
        return $response
    }
    catch {
        $statusCode = "未知"
        if ($_.Exception.Response) {
            $statusCode = $_.Exception.Response.StatusCode
        }
        Write-Host " ❌ 失败 ($statusCode)" -ForegroundColor Red
        return $null
    }
}

# 1. 健康检查
$healthResponse = Test-Endpoint -Name "系统健康检查" -Url "$BaseUrl/health"

# 2. 数据库连接
Test-Endpoint -Name "数据库连接检查" -Url "$BaseUrl/health/database"

# 3. Swagger文档
Test-Endpoint -Name "API文档访问" -Url "$BaseUrl/swagger/v1/swagger.json"

# 4. 登录测试
$loginData = @{
    username = "admin"
    password = "admin"
    loginType = "Password"
    rememberMe = $false
} | ConvertTo-Json

$loginResponse = Test-Endpoint -Name "用户登录功能" -Url "$BaseUrl/api/v1/auth/login" -Method "POST" -Body $loginData

# 5. 认证测试（如果登录成功）
if ($loginResponse -and $loginResponse.success -and $loginResponse.data.token) {
    $authHeaders = @{
        "Authorization" = "Bearer $($loginResponse.data.token)"
    }
    
    Test-Endpoint -Name "认证API访问" -Url "$BaseUrl/api/v1/users" -Headers $authHeaders
    Test-Endpoint -Name "中药材API" -Url "$BaseUrl/api/v1/herbs" -Headers $authHeaders
}

# 结果总结
Write-Host ""
Write-Host "="*50 -ForegroundColor Cyan
$passRate = [math]::Round($passCount / $testCount * 100, 1)

if ($passRate -ge 80) {
    Write-Host "🎉 测试结果: $passCount/$testCount 通过 ($passRate%) - 系统正常" -ForegroundColor Green
    exit 0
} elseif ($passRate -ge 60) {
    Write-Host "⚠️ 测试结果: $passCount/$testCount 通过 ($passRate%) - 有轻微问题" -ForegroundColor Yellow
    exit 1
} else {
    Write-Host "🚨 测试结果: $passCount/$testCount 通过 ($passRate%) - 有严重问题" -ForegroundColor Red
    exit 2
}