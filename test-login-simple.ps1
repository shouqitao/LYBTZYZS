# 简化的登录测试
$baseUrl = "https://localhost:5001"

$loginBody = @{
    username = "sysadmin"
    password = "LybtAdmin2025@SecurePass!"
} | ConvertTo-Json

Write-Host "测试登录..." -ForegroundColor Cyan
Write-Host "请求体: $loginBody" -ForegroundColor Gray

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/auth/login" `
        -Method Post `
        -Body $loginBody `
        -ContentType "application/json" `
        -SkipCertificateCheck

    Write-Host "`n✅ 登录成功！" -ForegroundColor Green
    Write-Host "完整响应:" -ForegroundColor Yellow
    $response | ConvertTo-Json -Depth 5

    if ($response.data -and $response.data.token) {
        $token = $response.data.token
        Write-Host "`nToken 提取成功: $($token.Substring(0,20))..." -ForegroundColor Green

        # 测试创建药材
        Write-Host "`n测试创建药材..." -ForegroundColor Cyan
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

        $herbResponse = Invoke-RestMethod -Uri "$baseUrl/api/v1/herbs" `
            -Method Post `
            -Headers $headers `
            -Body $herbBody `
            -SkipCertificateCheck

        Write-Host "✅ 药材创建成功！" -ForegroundColor Green
        Write-Host "ID: $($herbResponse.data.id)" -ForegroundColor Gray
        Write-Host "Name: $($herbResponse.data.name)" -ForegroundColor Gray
        Write-Host "CreatedAt: $($herbResponse.data.createdAt)" -ForegroundColor Gray
        Write-Host "UpdatedAt: $($herbResponse.data.updatedAt)" -ForegroundColor Gray
        Write-Host "`n🎉 Herbs API 修复成功！" -ForegroundColor Green
    } else {
        Write-Host "❌ 响应中没有找到 token" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ 请求失败: $_" -ForegroundColor Red
    if ($_.ErrorDetails) {
        Write-Host $_.ErrorDetails.Message -ForegroundColor Red
    }
}
