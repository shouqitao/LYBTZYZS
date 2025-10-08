# 测试 Herbs API 创建功能
$baseUrl = "https://localhost:5001"

Write-Host "=== 测试 Herbs API ===" -ForegroundColor Cyan

# 1. 登录获取 token
Write-Host "`n[1] 登录获取 JWT token..." -ForegroundColor Yellow
$loginBody = @{
    username = "admin"
    password = "Admin@123"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/v1/auth/login" `
        -Method Post `
        -Body $loginBody `
        -ContentType "application/json" `
        -SkipCertificateCheck

    $token = $loginResponse.data.token
    Write-Host "✅ 登录成功，token: $($token.Substring(0,20))..." -ForegroundColor Green
} catch {
    Write-Host "❌ 登录失败: $_" -ForegroundColor Red
    exit 1
}

# 2. 创建药材
Write-Host "`n[2] 创建测试药材..." -ForegroundColor Yellow
$herbBody = @{
    name = "测试药材-$(Get-Date -Format 'HHmmss')"
    pinYinCode = "CSYC"
    unit = "克"
    price = 15.50
    costPrice = 10.00
    status = 1
} | ConvertTo-Json

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

try {
    $createResponse = Invoke-RestMethod -Uri "$baseUrl/api/v1/herbs" `
        -Method Post `
        -Headers $headers `
        -Body $herbBody `
        -SkipCertificateCheck

    Write-Host "✅ 药材创建成功！" -ForegroundColor Green
    Write-Host "   ID: $($createResponse.data.id)" -ForegroundColor Gray
    Write-Host "   Name: $($createResponse.data.name)" -ForegroundColor Gray
    Write-Host "   CreatedAt: $($createResponse.data.createdAt)" -ForegroundColor Gray
    Write-Host "   UpdatedAt: $($createResponse.data.updatedAt)" -ForegroundColor Gray

    Write-Host "`n🎉 测试通过！Herbs API 修复成功！" -ForegroundColor Green
    exit 0
} catch {
    Write-Host "❌ 药材创建失败:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    if ($_.ErrorDetails) {
        Write-Host $_.ErrorDetails.Message -ForegroundColor Red
    }
    exit 1
}
