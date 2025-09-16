# P3-Server Hardening - 可观测性最小集检查
param(
    [string]$WebApiUrl = "http://localhost:8080"
)

Write-Host "=== P3-Server Hardening: 可观测性最小集检查 ===" -ForegroundColor Cyan
Write-Host "WebAPI URL: $WebApiUrl" -ForegroundColor Gray
Write-Host ""

$passedChecks = 0
$failedChecks = 0

# 健康检查端点
$healthEndpoints = @(
    "/api/v1/health",
    "/health",
    "/healthz",
    "/api/v1/health/detailed"
)

# 监控端点
$monitoringEndpoints = @(
    "/swagger",
    "/api/v1/auth/version"
)

Write-Host "Step 1: 健康检查端点验证" -ForegroundColor Yellow
foreach ($endpoint in $healthEndpoints) {
    try {
        $response = Invoke-RestMethod -Uri "$WebApiUrl$endpoint" -Method Get -TimeoutSec 10
        Write-Host "  ✅ $endpoint - 可用" -ForegroundColor Green
        $passedChecks++
    } catch {
        Write-Host "  ❌ $endpoint - 不可用" -ForegroundColor Red
        $failedChecks++
    }
}

Write-Host ""
Write-Host "Step 2: 监控端点验证" -ForegroundColor Yellow
foreach ($endpoint in $monitoringEndpoints) {
    try {
        $response = Invoke-WebRequest -Uri "$WebApiUrl$endpoint" -Method Get -TimeoutSec 10 -UseBasicParsing
        if ($response.StatusCode -eq 200) {
            Write-Host "  ✅ $endpoint - 可用" -ForegroundColor Green
            $passedChecks++
        } else {
            Write-Host "  ⚠️ $endpoint - 状态码: $($response.StatusCode)" -ForegroundColor Yellow
            $passedChecks++
        }
    } catch {
        Write-Host "  ❌ $endpoint - 不可用" -ForegroundColor Red
        $failedChecks++
    }
}

Write-Host ""
Write-Host "Step 3: API响应时间检查" -ForegroundColor Yellow
try {
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $response = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/health" -Method Get -TimeoutSec 10
    $stopwatch.Stop()
    $responseTime = $stopwatch.ElapsedMilliseconds
    
    if ($responseTime -lt 2000) {
        Write-Host "  ✅ API响应时间: $($responseTime)ms - 正常" -ForegroundColor Green
        $passedChecks++
    } else {
        Write-Host "  ⚠️ API响应时间: $($responseTime)ms - 偏慢" -ForegroundColor Yellow
        $passedChecks++
    }
} catch {
    Write-Host "  ❌ API响应时间检查失败" -ForegroundColor Red
    $failedChecks++
}

Write-Host ""
Write-Host "Step 4: 错误处理验证" -ForegroundColor Yellow
try {
    # 测试不存在的端点
    $errorResponse = Invoke-WebRequest -Uri "$WebApiUrl/api/v1/nonexistent" -Method Get -TimeoutSec 10 -UseBasicParsing
    Write-Host "  ❌ 错误处理: 应该返回404错误" -ForegroundColor Red
    $failedChecks++
} catch {
    if ($_.Exception.Response.StatusCode -eq 404) {
        Write-Host "  ✅ 错误处理: 正确返回404" -ForegroundColor Green
        $passedChecks++
    } else {
        Write-Host "  ⚠️ 错误处理: 状态码 $($_.Exception.Response.StatusCode)" -ForegroundColor Yellow
        $passedChecks++
    }
}

Write-Host ""
Write-Host "=== 可观测性检查总结 ===" -ForegroundColor Cyan
Write-Host "通过检查: $passedChecks" -ForegroundColor Green
Write-Host "失败检查: $failedChecks" -ForegroundColor Red

$totalChecks = $passedChecks + $failedChecks
$successRate = if ($totalChecks -gt 0) { [math]::Round(($passedChecks / $totalChecks) * 100, 1) } else { 0 }
Write-Host "成功率: $successRate%" -ForegroundColor White

if ($successRate -ge 85) {
    Write-Host "可观测性最小集: PASS" -ForegroundColor Green
    exit 0
} else {
    Write-Host "可观测性最小集: NEEDS IMPROVEMENT" -ForegroundColor Red
    exit 1
}