# Simple AutoMapper Testing Script
# Purpose: Verify Entity-DTO mapping after P3-Fix Batch5

param(
    [string]$WebApiUrl = "http://localhost:8080"
)

Write-Host "=== AutoMapper Configuration Test ===" -ForegroundColor Cyan
Write-Host "WebAPI URL: $WebApiUrl" -ForegroundColor Gray
Write-Host ""

$passedTests = 0
$failedTests = 0

# Test API Health
Write-Host "Step 1: Testing API Health..." -ForegroundColor Yellow
try {
    $healthResponse = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/health" -Method Get -TimeoutSec 10
    Write-Host "  API Health: OK" -ForegroundColor Green
    $passedTests++
} catch {
    Write-Host "  API Health: FAILED" -ForegroundColor Red
    $failedTests++
    exit 1
}

# Authenticate
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
        $authHeaders = @{
            "Authorization" = "Bearer $($loginResponse.data.token)"
            "Content-Type" = "application/json"
        }
        Write-Host "  Authentication: OK" -ForegroundColor Green
        $passedTests++
    } else {
        Write-Host "  Authentication: FAILED" -ForegroundColor Red
        $failedTests++
        exit 1
    }
} catch {
    Write-Host "  Authentication: FAILED" -ForegroundColor Red
    $failedTests++
    exit 1
}

# Test Users Entity Mapping
Write-Host "Step 3: Testing Users Entity Mapping..." -ForegroundColor Yellow
try {
    $usersResponse = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/users" -Method Get -Headers $authHeaders -TimeoutSec 10
    
    if ($usersResponse.success -and $usersResponse.data -and $usersResponse.data.items) {
        $user = $usersResponse.data.items[0]
        
        # Check for correct field names (P3-Fix Batch5)
        $hasRealName = $user.PSObject.Properties.Name -contains "realName"
        $hasEmail = $user.PSObject.Properties.Name -contains "email"
        
        if ($hasRealName -and $hasEmail) {
            Write-Host "  Users Mapping: OK (realName and email fields present)" -ForegroundColor Green
            $passedTests++
        } else {
            Write-Host "  Users Mapping: FAILED (realName: $hasRealName, email: $hasEmail)" -ForegroundColor Red
            $failedTests++
        }
    } else {
        Write-Host "  Users Mapping: FAILED (No data)" -ForegroundColor Red
        $failedTests++
    }
} catch {
    Write-Host "  Users Mapping: FAILED ($($_.Exception.Message))" -ForegroundColor Red
    $failedTests++
}

# Test Patients Entity Mapping
Write-Host "Step 4: Testing Patients Entity Mapping..." -ForegroundColor Yellow
try {
    $patientsResponse = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/patients" -Method Get -Headers $authHeaders -TimeoutSec 10
    
    if ($patientsResponse.success -and $patientsResponse.data -and $patientsResponse.data.items) {
        $patient = $patientsResponse.data.items[0]
        
        # Check for correct field names (P3-Fix Batch5)
        $hasName = $patient.PSObject.Properties.Name -contains "name"
        $hasAge = $patient.PSObject.Properties.Name -contains "age"
        $hasBirthDate = $patient.PSObject.Properties.Name -contains "birthDate"
        
        if ($hasName -and $hasBirthDate) {
            Write-Host "  Patients Name Mapping: OK (name and birthDate fields present)" -ForegroundColor Green
            $passedTests++
        } else {
            Write-Host "  Patients Name Mapping: FAILED (name: $hasName, birthDate: $hasBirthDate)" -ForegroundColor Red
            $failedTests++
        }
        
        if ($hasAge) {
            Write-Host "  Patients Age Mapping: OK (age field present)" -ForegroundColor Green
            $passedTests++
        } else {
            Write-Host "  Patients Age Mapping: FAILED (age field missing)" -ForegroundColor Red
            $failedTests++
        }
    } else {
        Write-Host "  Patients Mapping: FAILED (No data)" -ForegroundColor Red
        $failedTests += 2
    }
} catch {
    Write-Host "  Patients Mapping: FAILED ($($_.Exception.Message))" -ForegroundColor Red
    $failedTests += 2
}

Write-Host ""
Write-Host "=== Test Summary ===" -ForegroundColor Cyan
Write-Host "Passed Tests: $passedTests" -ForegroundColor Green
Write-Host "Failed Tests: $failedTests" -ForegroundColor Red

$totalTests = $passedTests + $failedTests
$successRate = if ($totalTests -gt 0) { [math]::Round(($passedTests / $totalTests) * 100, 1) } else { 0 }
Write-Host "Success Rate: $successRate%" -ForegroundColor $(if ($successRate -ge 90) { "Green" } elseif ($successRate -ge 70) { "Yellow" } else { "Red" })

if ($successRate -ge 90) {
    Write-Host "AutoMapper Configuration: VALIDATED" -ForegroundColor Green
    exit 0
} else {
    Write-Host "AutoMapper Configuration: NEEDS ATTENTION" -ForegroundColor Red
    exit 1
}