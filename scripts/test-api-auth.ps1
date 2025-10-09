# API认证测试脚本
# 日期: 2025-10-09
# 目的: 测试sysadmin登录并创建测试账号

$BaseUrl = "http://localhost:5000"

Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║             API认证测试与账号创建                         ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan

# 测试函数
function Test-Login {
    param(
        [string]$Username,
        [string]$Password,
        [string]$Description
    )

    Write-Host "`n测试: $Description" -ForegroundColor Yellow
    Write-Host "用户名: $Username" -ForegroundColor Gray

    $body = @{
        username = $Username
        password = $Password
    } | ConvertTo-Json

    try {
        $response = Invoke-WebRequest `
            -Uri "$BaseUrl/api/v1/auth/login" `
            -Method POST `
            -Body $body `
            -ContentType "application/json" `
            -UseBasicParsing

        $content = $response.Content | ConvertFrom-Json

        if ($content.success -eq $true) {
            Write-Host "✅ 登录成功!" -ForegroundColor Green
            Write-Host "   Token: $($content.data.token.Substring(0, 20))..." -ForegroundColor Gray
            return $content.data.token
        } else {
            Write-Host "❌ 登录失败: $($content.message)" -ForegroundColor Red
            return $null
        }
    } catch {
        Write-Host "❌ 请求失败: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.ErrorDetails.Message) {
            $errorContent = $_.ErrorDetails.Message | ConvertFrom-Json
            Write-Host "   响应: $($errorContent.message)" -ForegroundColor Yellow
        }
        return $null
    }
}

# 1. 尝试sysadmin登录
Write-Host "`n═══ 步骤1: 测试管理员登录 ═══" -ForegroundColor Cyan

# 尝试配置中的密码1
$adminToken = Test-Login -Username "sysadmin" -Password "LybtAdmin2025@SecurePass!" -Description "SysAdmin密码1"

# 如果失败，尝试密码2
if (-not $adminToken) {
    $adminToken = Test-Login -Username "sysadmin" -Password "Dev@Admin2025!" -Description "SysAdmin密码2"
}

# 如果失败，尝试密码3
if (-not $adminToken) {
    $adminToken = Test-Login -Username "sysadmin" -Password "Admin123!" -Description "SysAdmin密码3"
}

# 2. 如果管理员登录成功，创建doctor1账号
if ($adminToken) {
    Write-Host "`n═══ 步骤2: 创建Doctor测试账号 ═══" -ForegroundColor Cyan

    $newUser = @{
        username = "doctor1"
        password = "Pass123!"
        realName = "测试医生"
        email = "doctor1@lybt.com"
        phoneNumber = "13800138001"
        role = "Doctor"
    } | ConvertTo-Json

    try {
        $headers = @{
            "Authorization" = "Bearer $adminToken"
            "Content-Type" = "application/json"
        }

        Write-Host "创建用户: doctor1" -ForegroundColor Yellow

        $response = Invoke-WebRequest `
            -Uri "$BaseUrl/api/v1/users" `
            -Method POST `
            -Body $newUser `
            -Headers $headers `
            -UseBasicParsing

        $content = $response.Content | ConvertFrom-Json

        if ($content.success -eq $true) {
            Write-Host "✅ 用户创建成功!" -ForegroundColor Green
            Write-Host "   用户ID: $($content.data.id)" -ForegroundColor Gray
        } else {
            Write-Host "⚠️ 创建响应: $($content.message)" -ForegroundColor Yellow
        }
    } catch {
        if ($_.Exception.Response.StatusCode -eq 400) {
            Write-Host "⚠️ 用户可能已存在" -ForegroundColor Yellow
        } else {
            Write-Host "❌ 创建失败: $($_.Exception.Message)" -ForegroundColor Red
        }
    }

    # 3. 测试doctor1登录
    Write-Host "`n═══ 步骤3: 验证Doctor账号登录 ═══" -ForegroundColor Cyan
    $doctorToken = Test-Login -Username "doctor1" -Password "Pass123!" -Description "Doctor1登录测试"

    if ($doctorToken) {
        Write-Host "`n✅ Doctor测试账号已就绪!" -ForegroundColor Green
    }
} else {
    Write-Host "`n❌ 无法获取管理员Token，无法创建测试账号" -ForegroundColor Red
}

# 4. 测试健康检查端点
Write-Host "`n═══ 步骤4: 测试健康检查端点 ═══" -ForegroundColor Cyan
try {
    $response = Invoke-WebRequest `
        -Uri "$BaseUrl/health" `
        -Method GET `
        -UseBasicParsing

    Write-Host "✅ 健康检查端点存在 (状态码: $($response.StatusCode))" -ForegroundColor Green
} catch {
    if ($_.Exception.Response.StatusCode -eq 404) {
        Write-Host "❌ 健康检查端点不存在 (404)" -ForegroundColor Red
    } else {
        Write-Host "❌ 健康检查失败: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`n测试完成!" -ForegroundColor Cyan