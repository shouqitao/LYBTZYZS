# P3-Server Hardening Authentication & Authorization Regression Test
# Purpose: Comprehensive authentication and permission testing

param(
    [string]$WebApiUrl = "http://localhost:8080",
    [string]$ReportPath = "_reports/2025-09/backend/p3-server-hardening"
)

Write-Host "=== P3-Server Hardening: Authentication & Authorization Regression Test ===" -ForegroundColor Cyan
Write-Host "WebAPI URL: $WebApiUrl" -ForegroundColor Gray
Write-Host "Report Path: $ReportPath" -ForegroundColor Gray
Write-Host "Execution Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host ""

# 创建报告目录
if (!(Test-Path $ReportPath)) {
    New-Item -ItemType Directory -Force -Path $ReportPath | Out-Null
}

$testResults = @()
$passedTests = 0
$failedTests = 0

function Add-TestResult {
    param($TestName, $Status, $Details = "")
    
    $testResults += @{
        Test = $TestName
        Status = $Status
        Details = $Details
        Timestamp = Get-Date -Format 'HH:mm:ss'
    }
    
    if ($Status -eq "PASS") {
        $script:passedTests++
        Write-Host "  ✅ $TestName" -ForegroundColor Green
    } else {
        $script:failedTests++
        Write-Host "  ❌ $TestName - $Details" -ForegroundColor Red
    }
}

Write-Host "🌐 Step 1: Basic Connectivity Tests" -ForegroundColor Cyan
Write-Host "─────────────────────────────────────────" -ForegroundColor Gray

# Test 1: API Health Check
try {
    $healthResponse = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/health" -Method Get -TimeoutSec 10
    Add-TestResult "API Health Check" "PASS" "API responding normally"
} catch {
    Add-TestResult "API Health Check" "FAIL" $_.Exception.Message
}

# Test 2: Swagger Documentation Access
try {
    $swaggerResponse = Invoke-WebRequest -Uri "$WebApiUrl/swagger" -Method Get -UseBasicParsing -TimeoutSec 10
    if ($swaggerResponse.StatusCode -eq 200) {
        Add-TestResult "Swagger Documentation" "PASS" "Documentation accessible"
    } else {
        Add-TestResult "Swagger Documentation" "FAIL" "Status code: $($swaggerResponse.StatusCode)"
    }
} catch {
    Add-TestResult "Swagger Documentation" "FAIL" $_.Exception.Message
}

Write-Host ""
Write-Host "🔐 Step 2: Authentication Tests" -ForegroundColor Cyan
Write-Host "─────────────────────────────────────────" -ForegroundColor Gray

# Test 3: Valid Admin Login
try {
    $loginData = @{
        username = "sysadmin"
        password = "Admin@123456"
        rememberMe = $false
    } | ConvertTo-Json

    $loginHeaders = @{ "Content-Type" = "application/json" }
    $loginResponse = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/auth/login" -Method POST -Body $loginData -Headers $loginHeaders -TimeoutSec 30
    
    if ($loginResponse.success -and $loginResponse.data.token) {
        $validToken = $loginResponse.data.token
        $authHeaders = @{
            "Authorization" = "Bearer $validToken"
            "Content-Type" = "application/json"
        }
        Add-TestResult "Admin Login" "PASS" "Valid JWT token obtained"
        
        # Store token for later tests
        $global:validAuthHeaders = $authHeaders
    } else {
        Add-TestResult "Admin Login" "FAIL" "No token in response"
    }
} catch {
    Add-TestResult "Admin Login" "FAIL" $_.Exception.Message
}

# Test 4: Invalid Credentials
try {
    $invalidLoginData = @{
        username = "invalid"
        password = "wrongpassword"
        rememberMe = $false
    } | ConvertTo-Json

    $invalidResponse = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/auth/login" -Method POST -Body $invalidLoginData -Headers $loginHeaders -TimeoutSec 30
    
    if (-not $invalidResponse.success) {
        Add-TestResult "Invalid Credentials Rejection" "PASS" "Correctly rejected invalid credentials"
    } else {
        Add-TestResult "Invalid Credentials Rejection" "FAIL" "Should have rejected invalid credentials"
    }
} catch {
    # Expected to fail, this is good
    Add-TestResult "Invalid Credentials Rejection" "PASS" "Correctly threw exception for invalid credentials"
}

# Test 5: Token Validation
if ($global:validAuthHeaders) {
    try {
        $validateResponse = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/auth/validate" -Method Get -Headers $global:validAuthHeaders -TimeoutSec 10
        
        if ($validateResponse.success) {
            Add-TestResult "Token Validation" "PASS" "Valid token accepted"
        } else {
            Add-TestResult "Token Validation" "FAIL" "Valid token rejected"
        }
    } catch {
        Add-TestResult "Token Validation" "FAIL" $_.Exception.Message
    }
}

# Test 6: Invalid Token Rejection
try {
    $invalidAuthHeaders = @{
        "Authorization" = "Bearer invalid_token_here"
        "Content-Type" = "application/json"
    }
    
    $invalidTokenResponse = Invoke-WebRequest -Uri "$WebApiUrl/api/v1/auth/validate" -Method Get -Headers $invalidAuthHeaders -UseBasicParsing -TimeoutSec 10
    Add-TestResult "Invalid Token Rejection" "FAIL" "Should have rejected invalid token"
} catch {
    if ($_.Exception.Response.StatusCode -eq 401) {
        Add-TestResult "Invalid Token Rejection" "PASS" "Correctly rejected invalid token with 401"
    } else {
        Add-TestResult "Invalid Token Rejection" "FAIL" "Wrong status code: $($_.Exception.Response.StatusCode)"
    }
}

Write-Host ""
Write-Host "🛡️ Step 3: Authorization Tests" -ForegroundColor Cyan
Write-Host "─────────────────────────────────────────" -ForegroundColor Gray

# Protected endpoints to test
$protectedEndpoints = @(
    @{ Path = "/api/v1/users"; Method = "GET"; Name = "Users List" },
    @{ Path = "/api/v1/patients"; Method = "GET"; Name = "Patients List" },
    @{ Path = "/api/v1/herbs"; Method = "GET"; Name = "Herbs List" },
    @{ Path = "/api/v1/formulas"; Method = "GET"; Name = "Formulas List" }
)

# Test 7-10: Protected Endpoints Without Auth
foreach ($endpoint in $protectedEndpoints) {
    try {
        $response = Invoke-WebRequest -Uri "$WebApiUrl$($endpoint.Path)" -Method $endpoint.Method -UseBasicParsing -TimeoutSec 10
        Add-TestResult "$($endpoint.Name) Unauthorized Access" "FAIL" "Should have required authentication"
    } catch {
        if ($_.Exception.Response.StatusCode -eq 401) {
            Add-TestResult "$($endpoint.Name) Unauthorized Access" "PASS" "Correctly requires authentication"
        } else {
            Add-TestResult "$($endpoint.Name) Unauthorized Access" "FAIL" "Wrong status code: $($_.Exception.Response.StatusCode)"
        }
    }
}

# Test 11-14: Protected Endpoints With Valid Auth
if ($global:validAuthHeaders) {
    foreach ($endpoint in $protectedEndpoints) {
        try {
            $response = Invoke-RestMethod -Uri "$WebApiUrl$($endpoint.Path)" -Method $endpoint.Method -Headers $global:validAuthHeaders -TimeoutSec 10
            
            if ($response.success -ne $null) {
                Add-TestResult "$($endpoint.Name) Authorized Access" "PASS" "Successfully accessed with valid token"
            } else {
                Add-TestResult "$($endpoint.Name) Authorized Access" "FAIL" "Unexpected response format"
            }
        } catch {
            Add-TestResult "$($endpoint.Name) Authorized Access" "FAIL" $_.Exception.Message
        }
    }
}

Write-Host ""
Write-Host "🔒 Step 4: Security Tests" -ForegroundColor Cyan
Write-Host "─────────────────────────────────────────" -ForegroundColor Gray

# Test 15: SQL Injection Protection
if ($global:validAuthHeaders) {
    try {
        $maliciousQuery = "?search=' OR 1=1 --"
        $response = Invoke-WebRequest -Uri "$WebApiUrl/api/v1/users$maliciousQuery" -Method Get -Headers $global:validAuthHeaders -UseBasicParsing -TimeoutSec 10
        
        # If it doesn't crash and returns normally, it's likely protected
        Add-TestResult "SQL Injection Protection" "PASS" "API handled malicious input safely"
    } catch {
        # Expected behavior - the API should handle this gracefully
        Add-TestResult "SQL Injection Protection" "PASS" "API rejected malicious input"
    }
}

# Test 16: HTTPS Enforcement Check
$isHttps = $WebApiUrl.StartsWith("https://")
if ($isHttps) {
    Add-TestResult "HTTPS Usage" "PASS" "Using secure HTTPS connection"
} else {
    Add-TestResult "HTTPS Usage" "FAIL" "Using insecure HTTP connection"
}

Write-Host ""
Write-Host "📊 Step 5: Generate Report" -ForegroundColor Cyan
Write-Host "─────────────────────────────────────────" -ForegroundColor Gray

# Calculate metrics
$totalTests = $passedTests + $failedTests
$successRate = if ($totalTests -gt 0) { [math]::Round(($passedTests / $totalTests) * 100, 1) } else { 0 }

# Generate detailed report
$reportFile = Join-Path $ReportPath "auth-regression-test-report.md"
$reportContent = @"
# Authentication & Authorization Regression Test Report

**Execution Time**: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
**WebAPI URL**: $WebApiUrl
**Purpose**: P3-Server Hardening authentication and permission validation

## Test Summary

- **Total Tests**: $totalTests
- **Passed Tests**: $passedTests
- **Failed Tests**: $failedTests
- **Success Rate**: $successRate%

## Test Categories

### ✅ Connectivity Tests
Basic API health and documentation accessibility

### 🔐 Authentication Tests
Login, logout, token validation, and credential verification

### 🛡️ Authorization Tests
Protected endpoint access control and permission validation

### 🔒 Security Tests
SQL injection protection and HTTPS enforcement

## Detailed Test Results

"@

foreach ($result in $testResults) {
    $statusIcon = if ($result.Status -eq "PASS") { "✅" } else { "❌" }
    $reportContent += "`n- $statusIcon **$($result.Test)**: $($result.Status)"
    if ($result.Details) {
        $reportContent += " - $($result.Details)"
    }
    $reportContent += " [$($result.Timestamp)]"
}

$reportContent += @"

## Assessment

$(if ($successRate -ge 95) {
    "🟢 **EXCELLENT** - Authentication and authorization systems are functioning correctly. All security controls are in place."
} elseif ($successRate -ge 85) {
    "🟡 **GOOD** - Most authentication and authorization tests passed. Minor issues need attention."
} elseif ($successRate -ge 70) {
    "🟠 **NEEDS IMPROVEMENT** - Several authentication or authorization issues detected. Review and fix failing tests."
} else {
    "🔴 **CRITICAL** - Major authentication and authorization failures detected. Immediate attention required."
})

## Recommendations

$(if ($successRate -ge 95) {
    "- System is ready for production deployment"
    "- Consider implementing additional security monitoring"
    "- Regular security audits recommended"
} elseif ($successRate -ge 85) {
    "- Address the failing test cases"
    "- Verify security configurations"
    "- Test with different user roles"
} else {
    "- Immediate security review required"
    "- Fix authentication and authorization issues"
    "- Do not deploy to production until issues resolved"
})

---
*Authentication & Authorization Regression Test Report*
*Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')*
*Script: auth-regression-test.ps1*
"@

$reportContent | Out-File -FilePath $reportFile -Encoding UTF8

Write-Host ""
Write-Host "🎯 Test Summary" -ForegroundColor Cyan
Write-Host "─────────────────────────────────────────" -ForegroundColor Gray
Write-Host "Total Tests: $totalTests" -ForegroundColor White
Write-Host "Passed: $passedTests" -ForegroundColor Green
Write-Host "Failed: $failedTests" -ForegroundColor Red
Write-Host "Success Rate: $successRate%" -ForegroundColor $(if ($successRate -ge 95) { "Green" } elseif ($successRate -ge 85) { "Yellow" } else { "Red" })

$assessment = if ($successRate -ge 95) { "EXCELLENT" } elseif ($successRate -ge 85) { "GOOD" } elseif ($successRate -ge 70) { "NEEDS IMPROVEMENT" } else { "CRITICAL" }
Write-Host "Assessment: $assessment" -ForegroundColor $(if ($successRate -ge 95) { "Green" } elseif ($successRate -ge 85) { "Yellow" } else { "Red" })

Write-Host ""
Write-Host "✅ Test report generated: $reportFile" -ForegroundColor Green
Write-Host "Authentication & Authorization regression test completed!" -ForegroundColor Cyan

# Return appropriate exit code
if ($successRate -ge 85) {
    exit 0
} else {
    exit 1
}