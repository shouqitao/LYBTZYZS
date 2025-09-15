# Auth Login Script for Backend Acceptance Smoke Test
# Purpose: Get JWT token for system admin user
# Target: POST /api/v1/auth/login

param(
    [string]$BaseUrl = "http://localhost:8080",
    [string]$Username = "sysadmin",
    [string]$Password = "Admin@123456",
    [string]$OutputFile = ""
)

Write-Host "=== 第②步：获取认证令牌 ===" -ForegroundColor Cyan
Write-Host "目标: Doctor账号登录获取JWT令牌" -ForegroundColor Yellow
Write-Host "端点: POST /api/v1/auth/login"
Write-Host "账户: $Username (系统管理员)"
Write-Host ""

$loginData = @{
    username = $Username
    password = $Password
} | ConvertTo-Json

try {
    Write-Host "发送登录请求..." -ForegroundColor Yellow
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/v1/auth/login" -Method POST -Body $loginData -ContentType "application/json" -TimeoutSec 15
    
    if ($response.success -and $response.data -and $response.data.token) {
        Write-Host "✅ 登录成功！" -ForegroundColor Green
        Write-Host "   用户名: $($response.data.username)"
        Write-Host "   角色: $($response.data.role)"
        Write-Host "   令牌长度: $($response.data.token.Length) 字符"
        Write-Host "   令牌前缀: $($response.data.token.Substring(0, [Math]::Min(20, $response.data.token.Length)))..."
        
        # 保存认证结果
        $authResult = @{
            timestamp = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss.fff")
            success = $true
            username = $response.data.username
            role = $response.data.role
            tokenLength = $response.data.token.Length
            tokenPrefix = $response.data.token.Substring(0, [Math]::Min(20, $response.data.token.Length))
            expiryTime = $response.data.expiryTime
            fullToken = $response.data.token
            fullResponse = $response
        }
        
        if ($OutputFile -ne "") {
            $authResult | ConvertTo-Json -Depth 4 | Out-File -FilePath $OutputFile -Encoding UTF8
            Write-Host ""
            Write-Host "📋 认证信息已保存到: $OutputFile" -ForegroundColor Cyan
        }
        
        Write-Host "✅ 第②步完成 - JWT令牌获取成功" -ForegroundColor Green
        Write-Host ""
        
        # 返回成功状态
        exit 0
    } else {
        Write-Host "❌ 登录失败 - 响应格式错误" -ForegroundColor Red
        Write-Host "响应内容: $($response | ConvertTo-Json)" -ForegroundColor Gray
        exit 1
    }
} catch {
    Write-Host "❌ 登录请求失败: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "错误详情: $($_.Exception)" -ForegroundColor Gray
    exit 1
}