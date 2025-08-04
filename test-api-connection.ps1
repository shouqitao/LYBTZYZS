# 测试API连接的PowerShell脚本

Write-Host "=== 测试API连接状态 ===" -ForegroundColor Cyan

# API基础地址
$baseUrl = "https://localhost:7001"

# 忽略SSL证书错误（仅用于开发环境）
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

# 先登录获取Token
Write-Host "`n0. 登录获取Token..." -ForegroundColor Yellow
try {
    $loginBody = @{
        username = "sysadmin"
        password = "Admin@123456"
        rememberMe = $false
    } | ConvertTo-Json
    
    $loginHeaders = @{
        "Content-Type" = "application/json"
    }
    
    $loginResponse = Invoke-WebRequest -Uri "$baseUrl/api/v1/auth/login" -Method Post -Body $loginBody -Headers $loginHeaders -UseBasicParsing
    $loginData = $loginResponse.Content | ConvertFrom-Json
    
    if ($loginData.success) {
        $token = $loginData.data.token
        Write-Host "   ✓ 登录成功，获取Token" -ForegroundColor Green
        
        # 设置带Token的请求头
        $authHeaders = @{
            "Authorization" = "Bearer $token"
            "Content-Type" = "application/json"
        }
    } else {
        Write-Host "   ✗ 登录失败" -ForegroundColor Red
        exit
    }
} catch {
    Write-Host "   ✗ 登录请求失败: $_" -ForegroundColor Red
    exit
}

Write-Host "`n1. 测试健康检查端点..." -ForegroundColor Yellow
try {
    $healthResponse = Invoke-WebRequest -Uri "$baseUrl/api/health" -Method Get -UseBasicParsing
    Write-Host "   ✓ 健康检查成功: $($healthResponse.StatusCode)" -ForegroundColor Green
    Write-Host "   响应内容: $($healthResponse.Content)" -ForegroundColor Gray
} catch {
    Write-Host "   ✗ 健康检查失败: $_" -ForegroundColor Red
}

Write-Host "`n2. 测试药材列表端点..." -ForegroundColor Yellow
try {
    $herbsResponse = Invoke-WebRequest -Uri "$baseUrl/api/v1/herbs" -Method Get -Headers $authHeaders -UseBasicParsing
    Write-Host "   ✓ 药材列表端点响应: $($herbsResponse.StatusCode)" -ForegroundColor Green
    $content = $herbsResponse.Content | ConvertFrom-Json
    Write-Host "   返回数据类型: $($content.GetType().Name)" -ForegroundColor Gray
} catch {
    Write-Host "   ✗ 药材列表端点失败: $_" -ForegroundColor Red
}

Write-Host "`n3. 测试药材分页查询端点..." -ForegroundColor Yellow
try {
    $body = @{
        currentPage = 1
        pageSize = 10
    } | ConvertTo-Json

    $headers = @{
        "Content-Type" = "application/json"
    }

    $pagedResponse = Invoke-WebRequest -Uri "$baseUrl/api/v1/herbs/paged" -Method Post -Body $body -Headers $authHeaders -UseBasicParsing
    Write-Host "   ✓ 分页查询端点响应: $($pagedResponse.StatusCode)" -ForegroundColor Green
    $pagedContent = $pagedResponse.Content | ConvertFrom-Json
    Write-Host "   返回数据: " -ForegroundColor Gray
    Write-Host "   - 总数: $($pagedContent.totalCount)" -ForegroundColor Gray
    Write-Host "   - 当前页: $($pagedContent.currentPage)" -ForegroundColor Gray
    Write-Host "   - 数据条数: $($pagedContent.items.Count)" -ForegroundColor Gray
} catch {
    Write-Host "   ✗ 分页查询端点失败: $_" -ForegroundColor Red
    Write-Host "   错误详情: $($_.Exception.Response.StatusCode) - $($_.Exception.Response.StatusDescription)" -ForegroundColor Red
}

Write-Host "`n4. 测试患者列表端点..." -ForegroundColor Yellow
try {
    $patientsResponse = Invoke-WebRequest -Uri "$baseUrl/api/v1/patients" -Method Get -Headers $authHeaders -UseBasicParsing
    Write-Host "   ✓ 患者列表端点响应: $($patientsResponse.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "   ✗ 患者列表端点失败: $_" -ForegroundColor Red
}

Write-Host "`n5. 测试患者分页查询端点..." -ForegroundColor Yellow
try {
    $patientBody = @{
        currentPage = 1
        pageSize = 10
    } | ConvertTo-Json

    $patientPagedResponse = Invoke-WebRequest -Uri "$baseUrl/api/v1/patients/paged" -Method Post -Body $patientBody -Headers $authHeaders -UseBasicParsing
    Write-Host "   ✓ 患者分页查询响应: $($patientPagedResponse.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "   ✗ 患者分页查询失败: $_" -ForegroundColor Red
}

Write-Host "`n6. 测试病历列表端点..." -ForegroundColor Yellow
try {
    $recordsResponse = Invoke-WebRequest -Uri "$baseUrl/api/v1/records" -Method Get -Headers $authHeaders -UseBasicParsing
    Write-Host "   ✓ 病历列表端点响应: $($recordsResponse.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "   ✗ 病历列表端点失败: $_" -ForegroundColor Red
}

Write-Host "`n7. 测试验方模板端点..." -ForegroundColor Yellow
try {
    $formulaResponse = Invoke-WebRequest -Uri "$baseUrl/api/v1/FormulaTemplate" -Method Get -Headers $authHeaders -UseBasicParsing
    Write-Host "   ✓ 验方模板端点响应: $($formulaResponse.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "   ✗ 验方模板端点失败: $_" -ForegroundColor Red
}

Write-Host "`n=== 测试完成 ===" -ForegroundColor Cyan