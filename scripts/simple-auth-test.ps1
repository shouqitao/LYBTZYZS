# Simple Authentication & Authorization Test
# Purpose: P3-Server Hardening auth validation

param(
    [string]$WebApiUrl = "http://localhost:8080"
)

Write-Host "=== Authentication & Authorization Test ===" -ForegroundColor Cyan
Write-Host "WebAPI URL: $WebApiUrl" -ForegroundColor Gray
Write-Host ""

$passedTests = 0
$failedTests = 0

# Test 1: API Health
Write-Host "Test 1: API Health Check" -ForegroundColor Yellow
try {
    $healthResponse = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/health" -Method Get -TimeoutSec 10
    Write-Host "  PASS: API Health Check" -ForegroundColor Green
    $passedTests++
} catch {
    Write-Host "  FAIL: API Health Check - $($_.Exception.Message)" -ForegroundColor Red
    $failedTests++
}

# Test 2: Valid Admin Login
Write-Host "Test 2: Admin Authentication" -ForegroundColor Yellow
try {
    $loginData = @{
        username = "sysadmin"
        password = "Admin@123456"
        rememberMe = $false
    } | ConvertTo-Json

    $loginHeaders = @{ "Content-Type" = "application/json" }
    $loginResponse = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/auth/login" -Method POST -Body $loginData -Headers $loginHeaders -TimeoutSec 30
    
    if ($loginResponse.success -and $loginResponse.data.token) {
        $global:authHeaders = @{
            "Authorization" = "Bearer $($loginResponse.data.token)"
            "Content-Type" = "application/json"
        }
        Write-Host "  PASS: Admin Authentication" -ForegroundColor Green
        $passedTests++
    } else {
        Write-Host "  FAIL: Admin Authentication - No token received" -ForegroundColor Red
        $failedTests++
    }
} catch {
    Write-Host "  FAIL: Admin Authentication - $($_.Exception.Message)" -ForegroundColor Red
    $failedTests++
}

# Test 3: Invalid Credentials Rejection
Write-Host "Test 3: Invalid Credentials Rejection" -ForegroundColor Yellow
try {
    $invalidLoginData = @{
        username = "invalid"
        password = "wrongpassword"
        rememberMe = $false
    } | ConvertTo-Json

    $invalidResponse = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/auth/login" -Method POST -Body $invalidLoginData -Headers $loginHeaders -TimeoutSec 30
    
    if (-not $invalidResponse.success) {
        Write-Host "  PASS: Invalid Credentials Correctly Rejected" -ForegroundColor Green
        $passedTests++
    } else {
        Write-Host "  FAIL: Invalid Credentials Not Rejected" -ForegroundColor Red
        $failedTests++
    }
} catch {
    Write-Host "  PASS: Invalid Credentials Correctly Rejected (Exception)" -ForegroundColor Green
    $passedTests++
}

# Test 4: Token Validation
if ($global:authHeaders) {
    Write-Host "Test 4: Token Validation" -ForegroundColor Yellow
    try {
        $validateResponse = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/auth/validate" -Method Get -Headers $global:authHeaders -TimeoutSec 10
        
        if ($validateResponse.success) {
            Write-Host "  PASS: Token Validation" -ForegroundColor Green
            $passedTests++
        } else {
            Write-Host "  FAIL: Token Validation - Valid token rejected" -ForegroundColor Red
            $failedTests++
        }
    } catch {
        Write-Host "  FAIL: Token Validation - $($_.Exception.Message)" -ForegroundColor Red
        $failedTests++
    }
}

# Test 5-8: Protected Endpoints Without Auth
$protectedEndpoints = @("/api/v1/users", "/api/v1/patients", "/api/v1/herbs", "/api/v1/formulas")

foreach ($endpoint in $protectedEndpoints) {
    $testName = "Unauthorized Access to $endpoint"
    Write-Host "Test: $testName" -ForegroundColor Yellow
    
    try {
        $response = Invoke-WebRequest -Uri "$WebApiUrl$endpoint" -Method Get -UseBasicParsing -TimeoutSec 10
        Write-Host "  FAIL: $testName - Should require auth" -ForegroundColor Red
        $failedTests++
    } catch {
        if ($_.Exception.Response.StatusCode -eq 401) {
            Write-Host "  PASS: $testName - Correctly requires auth" -ForegroundColor Green
            $passedTests++
        } else {
            Write-Host "  FAIL: $testName - Wrong status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
            $failedTests++
        }
    }
}

# Test 9-12: Protected Endpoints With Valid Auth
if ($global:authHeaders) {
    foreach ($endpoint in $protectedEndpoints) {
        $testName = "Authorized Access to $endpoint"
        Write-Host "Test: $testName" -ForegroundColor Yellow
        
        try {
            $response = Invoke-RestMethod -Uri "$WebApiUrl$endpoint" -Method Get -Headers $global:authHeaders -TimeoutSec 10
            
            if ($response.success -ne $null) {
                Write-Host "  PASS: $testName - Successfully accessed" -ForegroundColor Green
                $passedTests++
            } else {
                Write-Host "  FAIL: $testName - Unexpected response format" -ForegroundColor Red
                $failedTests++
            }
        } catch {
            Write-Host "  FAIL: $testName - $($_.Exception.Message)" -ForegroundColor Red
            $failedTests++
        }
    }
}

# Test 13: Invalid Token Rejection
Write-Host "Test: Invalid Token Rejection" -ForegroundColor Yellow
try {
    $invalidAuthHeaders = @{
        "Authorization" = "Bearer invalid_token_here"
        "Content-Type" = "application/json"
    }
    
    $invalidTokenResponse = Invoke-WebRequest -Uri "$WebApiUrl/api/v1/auth/validate" -Method Get -Headers $invalidAuthHeaders -UseBasicParsing -TimeoutSec 10
    Write-Host "  FAIL: Invalid Token Rejection - Should have rejected" -ForegroundColor Red
    $failedTests++
} catch {
    if ($_.Exception.Response.StatusCode -eq 401) {
        Write-Host "  PASS: Invalid Token Rejection - Correctly rejected with 401" -ForegroundColor Green
        $passedTests++
    } else {
        Write-Host "  FAIL: Invalid Token Rejection - Wrong status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
        $failedTests++
    }
}

Write-Host ""
Write-Host "=== Test Summary ===" -ForegroundColor Cyan
Write-Host "Total Tests: $($passedTests + $failedTests)" -ForegroundColor White
Write-Host "Passed: $passedTests" -ForegroundColor Green
Write-Host "Failed: $failedTests" -ForegroundColor Red

$totalTests = $passedTests + $failedTests
$successRate = if ($totalTests -gt 0) { [math]::Round(($passedTests / $totalTests) * 100, 1) } else { 0 }
Write-Host "Success Rate: $successRate%" -ForegroundColor $(if ($successRate -ge 90) { "Green" } elseif ($successRate -ge 75) { "Yellow" } else { "Red" })

$assessment = if ($successRate -ge 90) { "EXCELLENT" } elseif ($successRate -ge 75) { "GOOD" } else { "NEEDS IMPROVEMENT" }
Write-Host "Assessment: $assessment" -ForegroundColor $(if ($successRate -ge 90) { "Green" } elseif ($successRate -ge 75) { "Yellow" } else { "Red" })

if ($successRate -ge 75) {
    Write-Host "Authentication & Authorization: VALIDATED" -ForegroundColor Green
    exit 0
} else {
    Write-Host "Authentication & Authorization: NEEDS ATTENTION" -ForegroundColor Red
    exit 1
}