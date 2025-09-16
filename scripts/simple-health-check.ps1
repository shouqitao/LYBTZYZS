# Simple Health Check for P3-Fix Batch5 UAT
# Purpose: Verify basic system health for Gate validation

param(
    [string]$WebApiUrl = "http://localhost:8080"
)

Write-Host "=== P3-Fix Batch5 UAT Health Check ===" -ForegroundColor Cyan
Write-Host "WebAPI URL: $WebApiUrl" -ForegroundColor Gray
Write-Host ""

$passedChecks = 0
$totalChecks = 4

# Test 1: API Health
Write-Host "Step 1: API Health Check..." -ForegroundColor Yellow
try {
    $healthResponse = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/health" -Method Get -TimeoutSec 10
    Write-Host "  API Health: PASS" -ForegroundColor Green
    $passedChecks++
} catch {
    Write-Host "  API Health: FAIL" -ForegroundColor Red
}

# Test 2: Authentication
Write-Host "Step 2: Authentication..." -ForegroundColor Yellow
try {
    $loginData = @{
        username = "sysadmin"
        password = "Admin@123456"
        rememberMe = $false
    } | ConvertTo-Json

    $loginHeaders = @{ "Content-Type" = "application/json" }
    $loginResponse = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/auth/login" -Method POST -Body $loginData -Headers $loginHeaders -TimeoutSec 30
    
    if ($loginResponse.success -and $loginResponse.data.token) {
        Write-Host "  Authentication: PASS" -ForegroundColor Green
        $passedChecks++
    } else {
        Write-Host "  Authentication: FAIL" -ForegroundColor Red
    }
} catch {
    Write-Host "  Authentication: FAIL" -ForegroundColor Red
}

# Test 3: Core APIs  
Write-Host "Step 3: Core API Availability..." -ForegroundColor Yellow
try {
    $authHeaders = @{
        "Authorization" = "Bearer $($loginResponse.data.token)"
        "Content-Type" = "application/json"
    }
    
    $usersResponse = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/users" -Method Get -Headers $authHeaders -TimeoutSec 10
    $patientsResponse = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/patients" -Method Get -Headers $authHeaders -TimeoutSec 10
    
    if ($usersResponse.success -and $patientsResponse.success) {
        Write-Host "  Core APIs: PASS" -ForegroundColor Green
        $passedChecks++
    } else {
        Write-Host "  Core APIs: FAIL" -ForegroundColor Red
    }
} catch {
    Write-Host "  Core APIs: FAIL" -ForegroundColor Red
}

# Test 4: Field Alignment Check
Write-Host "Step 4: Entity-DTO Field Alignment..." -ForegroundColor Yellow
try {
    $hasRealName = $usersResponse.data.items[0].PSObject.Properties.Name -contains "realName"
    $hasPatientName = $patientsResponse.data.items[0].PSObject.Properties.Name -contains "name"
    
    if ($hasRealName -and $hasPatientName) {
        Write-Host "  Field Alignment: PASS (realName/name fields present)" -ForegroundColor Green
        $passedChecks++
    } else {
        Write-Host "  Field Alignment: FAIL (realName: $hasRealName, name: $hasPatientName)" -ForegroundColor Red
    }
} catch {
    Write-Host "  Field Alignment: FAIL" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== UAT Health Summary ===" -ForegroundColor Cyan
Write-Host "Passed: $passedChecks" -ForegroundColor Green
Write-Host "Failed: $($totalChecks - $passedChecks)" -ForegroundColor Red
$successRate = [math]::Round(($passedChecks / $totalChecks) * 100, 1)
Write-Host "Success Rate: $successRate%" -ForegroundColor $(if ($successRate -ge 75) { "Green" } else { "Red" })

if ($successRate -ge 75) {
    Write-Host "UAT Health Status: PASS" -ForegroundColor Green
    exit 0
} else {
    Write-Host "UAT Health Status: FAIL" -ForegroundColor Red
    exit 1
}