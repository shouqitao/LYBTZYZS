# Auth登录测试脚本
param(
    [string]$BaseUrl = "http://localhost:8080",
    [string]$ReportDir = "_reports/2025-09/backend/acceptance-rerun"
)

$ErrorActionPreference = "Continue"
Write-Host "🔐 [Auth Test] 开始测试Auth登录接口..." -ForegroundColor Yellow

# 确保报告目录存在
$fullReportPath = Join-Path $PWD $ReportDir
if (!(Test-Path $fullReportPath)) {
    New-Item -Path $fullReportPath -ItemType Directory -Force | Out-Null
}

# 测试数据
$loginData = @{
    username = "sysadmin"
    password = "LybtAdmin2025@SecurePass!"
    rememberMe = $false
} | ConvertTo-Json

# 设置请求头
$headers = @{
    "Content-Type" = "application/json"
    "Accept" = "application/json"
}

# 测试Auth登录
try {
    Write-Host "📋 测试登录接口: $BaseUrl/api/v1/auth/login"
    
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/v1/auth/login" -Method POST -Body $loginData -Headers $headers -TimeoutSec 10
    
    if ($response -and $response.success) {
        $token = $response.data.token
        Write-Host "✅ Auth登录成功，获取到JWT Token" -ForegroundColor Green
        
        # 保存认证结果
        $authResult = @{
            success = $true
            timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
            endpoint = "$BaseUrl/api/v1/auth/login"
            username = "sysadmin"
            tokenPrefix = $token.Substring(0, [Math]::Min(20, $token.Length)) + "..."
            message = "Auth登录成功，JWT令牌获取正常"
        }
        
        $authResult | ConvertTo-Json -Depth 3 | Out-File -FilePath "$fullReportPath/auth.json" -Encoding UTF8
        
        # 保存完整token到临时文件供后续测试使用
        $token | Out-File -FilePath "$fullReportPath/jwt-token.txt" -Encoding UTF8
        
        Write-Host "📁 认证信息已保存到: $ReportDir/auth.json" -ForegroundColor Cyan
        return $true
    } else {
        throw "认证响应格式错误或登录失败"
    }
} catch {
    Write-Host "❌ Auth登录失败: $($_.Exception.Message)" -ForegroundColor Red
    
    # 保存失败结果
    $authResult = @{
        success = $false
        timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        endpoint = "$BaseUrl/api/v1/auth/login"
        username = "sysadmin"
        error = $_.Exception.Message
        message = "Auth登录失败"
    }
    
    $authResult | ConvertTo-Json -Depth 3 | Out-File -FilePath "$fullReportPath/auth.json" -Encoding UTF8
    return $false
}