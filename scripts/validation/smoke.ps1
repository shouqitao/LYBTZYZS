# WebAPI Smoke Testing Script - Step 5 Verification
# Purpose: Basic API smoke tests for WebAPI run verification
# Author: UltraThink WebAPI Run Fix Process

param(
    [string]$BaseUrl = "http://localhost:8080",
    [int]$TimeoutSeconds = 30,
    [switch]$Detailed
)

Write-Host "=== WebAPI Smoke Testing ===" -ForegroundColor Cyan
Write-Host "BaseUrl: $BaseUrl"
Write-Host "Timeout: $TimeoutSeconds seconds"
Write-Host ""

$TestResults = @()
$AllPassed = $true

function Test-Endpoint {
    param([string]$Url, [string]$TestName, [string]$ExpectedStatus = "200")
    
    Write-Host "Testing: $TestName" -ForegroundColor Yellow
    Write-Host "  URL: $Url"
    
    try {
        $response = Invoke-WebRequest -Uri $Url -Method GET -TimeoutSec $TimeoutSeconds -UseBasicParsing
        $statusCode = $response.StatusCode
        $responseTime = [System.DateTime]::Now
        
        if ($statusCode -eq [int]$ExpectedStatus) {
            Write-Host "  Result: PASS (Status: $statusCode)" -ForegroundColor Green
            $TestResults += @{
                Test = $TestName
                Status = "PASS"
                StatusCode = $statusCode
                Url = $Url
                Error = $null
            }
            return $true
        } else {
            Write-Host "  Result: FAIL (Expected: $ExpectedStatus, Got: $statusCode)" -ForegroundColor Red
            $script:AllPassed = $false
            $TestResults += @{
                Test = $TestName
                Status = "FAIL"
                StatusCode = $statusCode
                Url = $Url
                Error = "Unexpected status code"
            }
            return $false
        }
    }
    catch {
        Write-Host "  Result: FAIL (Error: $($_.Exception.Message))" -ForegroundColor Red
        $script:AllPassed = $false
        $TestResults += @{
            Test = $TestName
            Status = "FAIL"
            StatusCode = "N/A"
            Url = $Url
            Error = $_.Exception.Message
        }
        return $false
    }
}

function Test-HealthEndpoint {
    param([string]$Url, [string]$TestName)
    
    Write-Host "Testing: $TestName" -ForegroundColor Yellow
    Write-Host "  URL: $Url"
    
    try {
        $response = Invoke-RestMethod -Uri $Url -Method GET -TimeoutSec $TimeoutSeconds
        
        if ($response.status -eq "Healthy") {
            Write-Host "  Result: PASS (Health Status: $($response.status))" -ForegroundColor Green
            if ($Detailed) {
                Write-Host "    Timestamp: $($response.timestamp)"
                Write-Host "    Version: $($response.version)"
                Write-Host "    Environment: $($response.environment)"
            }
            $TestResults += @{
                Test = $TestName
                Status = "PASS"
                StatusCode = "200"
                Url = $Url
                Error = $null
                Details = $response
            }
            return $true
        } else {
            Write-Host "  Result: FAIL (Health Status: $($response.status))" -ForegroundColor Red
            $script:AllPassed = $false
            $TestResults += @{
                Test = $TestName
                Status = "FAIL"
                StatusCode = "200"
                Url = $Url
                Error = "Health status not Healthy"
                Details = $response
            }
            return $false
        }
    }
    catch {
        Write-Host "  Result: FAIL (Error: $($_.Exception.Message))" -ForegroundColor Red
        $script:AllPassed = $false
        $TestResults += @{
            Test = $TestName
            Status = "FAIL"
            StatusCode = "N/A"
            Url = $Url
            Error = $_.Exception.Message
            Details = $null
        }
        return $false
    }
}

# Core Smoke Tests
Write-Host "Starting smoke tests..." -ForegroundColor Cyan
Write-Host ""

# Test 1: Health Check
$healthPassed = Test-HealthEndpoint -Url "$BaseUrl/api/v1/health" -TestName "Health Check Endpoint"
Write-Host ""

# Test 2: Ping endpoint
$pingPassed = Test-Endpoint -Url "$BaseUrl/api/v1/health/ping" -TestName "Ping Endpoint"
Write-Host ""

# Test 3: Swagger endpoint (if available)
$swaggerPassed = Test-Endpoint -Url "$BaseUrl/swagger/index.html" -TestName "Swagger Documentation"
Write-Host ""

# Optional: Test 4: Auth endpoint availability (no auth required)
$authPassed = Test-Endpoint -Url "$BaseUrl/api/v1/auth" -TestName "Auth Endpoint Availability" -ExpectedStatus "405"
Write-Host ""

# Summary
Write-Host "=== Test Results Summary ===" -ForegroundColor Cyan
Write-Host "Total Tests: $($TestResults.Count)"
Write-Host "Passed: $(($TestResults | Where-Object {$_.Status -eq 'PASS'}).Count)" -ForegroundColor Green
Write-Host "Failed: $(($TestResults | Where-Object {$_.Status -eq 'FAIL'}).Count)" -ForegroundColor Red
Write-Host ""

if ($AllPassed) {
    Write-Host "SMOKE TESTS PASSED - WebAPI is ready for use!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "SMOKE TESTS FAILED - Check the errors above" -ForegroundColor Red
    Write-Host ""
    Write-Host "Failed Tests:" -ForegroundColor Red
    $TestResults | Where-Object {$_.Status -eq 'FAIL'} | ForEach-Object {
        Write-Host "  - $($_.Test): $($_.Error)" -ForegroundColor Red
    }
    exit 1
}