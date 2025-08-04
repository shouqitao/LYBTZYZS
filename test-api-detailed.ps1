# 详细测试API连接的PowerShell脚本

Write-Host "=== 详细测试API通讯问题 ===" -ForegroundColor Cyan

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
        Write-Host "   ✗ 登录失败: $($loginData.message)" -ForegroundColor Red
        exit
    }
} catch {
    Write-Host "   ✗ 登录请求失败: $_" -ForegroundColor Red
    exit
}

Write-Host "`n=== 测试药材模块 ===" -ForegroundColor Yellow

Write-Host "`n1. 测试药材GET列表端点..." -ForegroundColor Yellow
try {
    $herbsResponse = Invoke-WebRequest -Uri "$baseUrl/api/v1/herbs?page=1&pageSize=10" -Method Get -Headers $authHeaders -UseBasicParsing
    Write-Host "   ✓ GET请求成功: $($herbsResponse.StatusCode)" -ForegroundColor Green
    $content = $herbsResponse.Content | ConvertFrom-Json
    if ($content.success) {
        Write-Host "   ✓ 数据格式正确" -ForegroundColor Green
        Write-Host "   - 总数: $($content.data.totalCount)" -ForegroundColor Gray
        Write-Host "   - 当前页: $($content.data.currentPage)" -ForegroundColor Gray
        Write-Host "   - 页大小: $($content.data.pageSize)" -ForegroundColor Gray
        Write-Host "   - 数据条数: $($content.data.items.Count)" -ForegroundColor Gray
    }
} catch {
    Write-Host "   ✗ GET请求失败: $_" -ForegroundColor Red
}

Write-Host "`n2. 测试药材POST分页查询端点..." -ForegroundColor Yellow
try {
    $body = @{
        currentPage = 1
        pageSize = 10
    } | ConvertTo-Json

    $pagedResponse = Invoke-WebRequest -Uri "$baseUrl/api/v1/herbs/paged" -Method Post -Body $body -Headers $authHeaders -UseBasicParsing
    Write-Host "   ✓ POST分页查询成功: $($pagedResponse.StatusCode)" -ForegroundColor Green
    $pagedContent = $pagedResponse.Content | ConvertFrom-Json
    if ($pagedContent.success) {
        Write-Host "   ✓ 数据格式正确" -ForegroundColor Green
        Write-Host "   - 总数: $($pagedContent.data.totalCount)" -ForegroundColor Gray
        Write-Host "   - 当前页: $($pagedContent.data.currentPage)" -ForegroundColor Gray
        Write-Host "   - 页大小: $($pagedContent.data.pageSize)" -ForegroundColor Gray
        Write-Host "   - 数据条数: $($pagedContent.data.items.Count)" -ForegroundColor Gray
    }
} catch {
    Write-Host "   ✗ POST分页查询失败: $_" -ForegroundColor Red
}

Write-Host "`n=== 测试患者模块 ===" -ForegroundColor Yellow

Write-Host "`n1. 测试患者GET列表端点..." -ForegroundColor Yellow
try {
    $patientsResponse = Invoke-WebRequest -Uri "$baseUrl/api/v1/patients?page=1&pageSize=10" -Method Get -Headers $authHeaders -UseBasicParsing
    Write-Host "   ✓ GET请求成功: $($patientsResponse.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "   ✗ GET请求失败: $_" -ForegroundColor Red
}

Write-Host "`n2. 测试患者POST分页查询端点..." -ForegroundColor Yellow
try {
    $patientBody = @{
        currentPage = 1
        pageSize = 10
    } | ConvertTo-Json

    $patientPagedResponse = Invoke-WebRequest -Uri "$baseUrl/api/v1/patients/paged" -Method Post -Body $patientBody -Headers $authHeaders -UseBasicParsing
    Write-Host "   ✓ POST分页查询成功: $($patientPagedResponse.StatusCode)" -ForegroundColor Green
    $patientPagedContent = $patientPagedResponse.Content | ConvertFrom-Json
    if ($patientPagedContent.success) {
        Write-Host "   ✓ 数据格式正确" -ForegroundColor Green
        Write-Host "   - 总数: $($patientPagedContent.data.totalCount)" -ForegroundColor Gray
        Write-Host "   - 数据条数: $($patientPagedContent.data.items.Count)" -ForegroundColor Gray
    }
} catch {
    Write-Host "   ✗ POST分页查询失败: $_" -ForegroundColor Red
}

Write-Host "`n=== 测试挂号模块 ===" -ForegroundColor Yellow

Write-Host "`n1. 测试挂号GET列表端点..." -ForegroundColor Yellow
try {
    $regResponse = Invoke-WebRequest -Uri "$baseUrl/api/v1/registration?page=1&pageSize=10" -Method Get -Headers $authHeaders -UseBasicParsing
    Write-Host "   ✓ GET请求成功: $($regResponse.StatusCode)" -ForegroundColor Green
    $regContent = $regResponse.Content | ConvertFrom-Json
    if ($regContent.success) {
        Write-Host "   ✓ 数据格式正确" -ForegroundColor Green
        Write-Host "   - 总数: $($regContent.data.totalCount)" -ForegroundColor Gray
        Write-Host "   - 当前页: $($regContent.data.currentPage)" -ForegroundColor Gray
        Write-Host "   - 页大小: $($regContent.data.pageSize)" -ForegroundColor Gray
    }
} catch {
    Write-Host "   ✗ GET请求失败: $_" -ForegroundColor Red
}

Write-Host "`n=== 测试病历模块 ===" -ForegroundColor Yellow

Write-Host "`n1. 测试病历GET列表端点..." -ForegroundColor Yellow
try {
    $recordsResponse = Invoke-WebRequest -Uri "$baseUrl/api/v1/records?page=1&pageSize=10" -Method Get -Headers $authHeaders -UseBasicParsing
    Write-Host "   ✓ GET请求成功: $($recordsResponse.StatusCode)" -ForegroundColor Green
    $recordsContent = $recordsResponse.Content | ConvertFrom-Json
    if ($recordsContent.success) {
        Write-Host "   ✓ 数据格式正确" -ForegroundColor Green
        Write-Host "   - 总数: $($recordsContent.data.totalCount)" -ForegroundColor Gray
        Write-Host "   - 当前页: $($recordsContent.data.currentPage)" -ForegroundColor Gray
        Write-Host "   - 页大小: $($recordsContent.data.pageSize)" -ForegroundColor Gray
    }
} catch {
    Write-Host "   ✗ GET请求失败: $_" -ForegroundColor Red
}

Write-Host "`n=== 测试验方模板模块 ===" -ForegroundColor Yellow

Write-Host "`n1. 测试验方模板GET列表端点..." -ForegroundColor Yellow
try {
    # 注意：使用复数形式 FormulaTemplates
    $formulaResponse = Invoke-WebRequest -Uri "$baseUrl/api/v1/FormulaTemplates" -Method Get -Headers $authHeaders -UseBasicParsing
    Write-Host "   ✓ GET请求成功: $($formulaResponse.StatusCode)" -ForegroundColor Green
    $formulaContent = $formulaResponse.Content | ConvertFrom-Json
    if ($formulaContent.success) {
        Write-Host "   ✓ 数据格式正确" -ForegroundColor Green
        Write-Host "   - 数据条数: $($formulaContent.data.Count)" -ForegroundColor Gray
    }
} catch {
    Write-Host "   ✗ GET请求失败: $_" -ForegroundColor Red
}

Write-Host "`n=== 测试完成 ===" -ForegroundColor Cyan