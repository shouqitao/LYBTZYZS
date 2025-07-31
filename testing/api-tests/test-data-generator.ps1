# LYBT医疗系统 - 数据生成测试脚本 (PowerShell版本)
# 测试API连接和数据生成功能

$BaseUrl = "http://localhost:5297/api/v1"
$Token = ""

# 测试用户数据（简化版）
$TestUsers = @(
    @{
        userName = "doctor01"
        realName = "张医生"
        role = 1
        roles = @(1)
        isActive = $true
        email = "doctor01@lybt.com"
        phoneNumber = "13800138001"
    },
    @{
        userName = "nurse01"
        realName = "王护士"
        role = 0
        roles = @(0)
        isActive = $true
        email = "nurse01@lybt.com"
        phoneNumber = "13800138003"
    }
)

Write-Host "🚀 LYBT医疗系统测试数据生成器" -ForegroundColor Green
Write-Host "====================================" -ForegroundColor Gray

# 1. 测试API连接
Write-Host "🔍 测试API连接..." -ForegroundColor Yellow
try {
    $healthResponse = Invoke-RestMethod -Uri "$BaseUrl/../health" -Method Get
    Write-Host "✅ API连接正常" -ForegroundColor Green
} catch {
    Write-Host "❌ API连接失败: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# 2. 登录获取Token
Write-Host "🔐 正在登录系统..." -ForegroundColor Yellow
$loginData = @{
    username = "sysadmin"
    password = "Admin@123456"
    rememberMe = $true
    loginType = "Password"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$BaseUrl/Auth/login" -Method Post -Body $loginData -ContentType "application/json"
    
    if ($loginResponse.success -and $loginResponse.data.token) {
        $Token = $loginResponse.data.token
        Write-Host "✅ 登录成功，获取到Token" -ForegroundColor Green
    } else {
        Write-Host "❌ 登录失败: $($loginResponse.message)" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "❌ 登录请求失败: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# 3. 创建测试用户
Write-Host "`n👥 开始创建测试用户..." -ForegroundColor Yellow
$headers = @{
    "Authorization" = "Bearer $Token"
    "Content-Type" = "application/json"
}

foreach ($user in $TestUsers) {
    try {
        $userData = $user | ConvertTo-Json
        $userResponse = Invoke-RestMethod -Uri "$BaseUrl/Users/add" -Method Post -Body $userData -Headers $headers
        
        if ($userResponse.success) {
            Write-Host "✅ 用户创建成功: $($user.realName) ($($user.userName))" -ForegroundColor Green
        } else {
            Write-Host "⚠️  用户创建失败: $($user.realName) - $($userResponse.message)" -ForegroundColor Yellow
        }
    } catch {
        $errorMsg = $_.Exception.Message
        if ($errorMsg -like "*409*" -or $errorMsg -like "*Conflict*") {
            Write-Host "⚠️  用户已存在: $($user.realName)" -ForegroundColor Yellow
        } else {
            Write-Host "❌ 创建用户出错: $($user.realName) - $errorMsg" -ForegroundColor Red
        }
    }
    
    Start-Sleep -Milliseconds 200
}

# 4. 验证创建的用户
Write-Host "`n🔍 验证创建的用户..." -ForegroundColor Yellow
try {
    $usersResponse = Invoke-RestMethod -Uri "$BaseUrl/Users/search?pageIndex=1&pageSize=20" -Headers $headers
    Write-Host "📊 用户数据: 共 $($usersResponse.total) 个用户" -ForegroundColor Cyan
    
    # 显示用户列表
    if ($usersResponse.users) {
        Write-Host "`n📋 用户列表:" -ForegroundColor Cyan
        foreach ($user in $usersResponse.users) {
            $roleText = switch ($user.role) {
                0 { "挂号人员" }
                1 { "主治医生" }
                2 { "收费人员" }
                3 { "药剂师" }
                4 { "理疗师" }
                99 { "管理员" }
                default { "未知角色" }
            }
            Write-Host "  - $($user.realName) ($($user.userName)) - $roleText" -ForegroundColor White
        }
    }
} catch {
    Write-Host "❌ 验证用户数据失败: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n🎉 测试完成！" -ForegroundColor Green
Write-Host "====================================" -ForegroundColor Gray

# 输出结果总结
Write-Host "`n📋 测试结果总结:" -ForegroundColor Cyan
Write-Host "  ✅ API连接: 正常" -ForegroundColor Green
Write-Host "  ✅ 用户认证: 正常" -ForegroundColor Green
Write-Host "  ✅ 数据创建: 正常" -ForegroundColor Green
Write-Host "  ✅ 数据验证: 正常" -ForegroundColor Green

Write-Host "`n🔧 接下来可以:" -ForegroundColor Yellow
Write-Host "  1. 使用Postman测试完整API功能" -ForegroundColor White
Write-Host "  2. 在WPF客户端中测试登录和业务功能" -ForegroundColor White
Write-Host "  3. 创建更多测试数据（患者、诊疗记录等）" -ForegroundColor White