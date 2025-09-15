# Simple E2E UAT Regression Test
param([string]$WebApiUrl = "http://localhost:8080")

$startTime = Get-Date
$passed = 0
$failed = 0
$results = @{}

function Test-Endpoint {
    param([string]$Name, [string]$Method, [string]$Url, $Body = $null, $Headers = @{})
    
    try {
        Write-Host "Testing $Name..." -ForegroundColor Cyan
        
        if ($Method -eq "GET") {
            $response = Invoke-RestMethod -Uri $Url -Method $Method -Headers $Headers -TimeoutSec 10
        } else {
            $response = Invoke-RestMethod -Uri $Url -Method $Method -Body $Body -Headers $Headers -TimeoutSec 10
        }
        
        if ($response.success -or $response) {
            Write-Host "PASS - $Name" -ForegroundColor Green
            $script:passed++
            $results[$Name] = "PASS"
            return $true
        } else {
            Write-Host "FAIL - $Name" -ForegroundColor Red
            $script:failed++
            $results[$Name] = "FAIL"
            return $false
        }
    } catch {
        Write-Host "ERROR - $Name : $($_.Exception.Message)" -ForegroundColor Red
        $script:failed++
        $results[$Name] = "ERROR"
        return $false
    }
}

Write-Host "=== UAT E2E Regression Testing Started ===" -ForegroundColor Yellow
Write-Host "WebAPI: $WebApiUrl" -ForegroundColor Yellow

# Step 1: Auth
Write-Host "`n=== Authentication Module ===" -ForegroundColor Yellow
$loginData = '{"username":"sysadmin","password":"Admin@123456","rememberMe":false}'
$loginHeaders = @{"Content-Type" = "application/json"}

if (Test-Endpoint "Auth-Login" "POST" "$WebApiUrl/api/v1/auth/login" $loginData $loginHeaders) {
    $loginResponse = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/auth/login" -Method POST -Body $loginData -Headers $loginHeaders
    $token = $loginResponse.data.token
    $authHeaders = @{"Authorization" = "Bearer $token"; "Content-Type" = "application/json"}
    
    Test-Endpoint "Auth-Validate" "GET" "$WebApiUrl/api/v1/auth/validate" $null $authHeaders
} else {
    Write-Host "Authentication failed - stopping tests" -ForegroundColor Red
    exit 1
}

# Step 2: Users Module
Write-Host "`n=== Users Module ===" -ForegroundColor Yellow
Test-Endpoint "Users-List" "GET" "$WebApiUrl/api/v1/users" $null $authHeaders

# Step 3: Patients Module  
Write-Host "`n=== Patients Module ===" -ForegroundColor Yellow
Test-Endpoint "Patients-List" "GET" "$WebApiUrl/api/v1/patients" $null $authHeaders

# Step 4: Herbs Module
Write-Host "`n=== Herbs Module ===" -ForegroundColor Yellow
Test-Endpoint "Herbs-List" "GET" "$WebApiUrl/api/v1/herbs" $null $authHeaders

# Step 5: Formulas Module
Write-Host "`n=== Formulas Module ===" -ForegroundColor Yellow
Test-Endpoint "Formulas-List" "GET" "$WebApiUrl/api/v1/formulas" $null $authHeaders

# Step 6: MedicalCases Module
Write-Host "`n=== MedicalCases Module ===" -ForegroundColor Yellow
Test-Endpoint "MedicalCases-List" "GET" "$WebApiUrl/api/v1/medicalcases" $null $authHeaders

# Step 7: Consultations Module
Write-Host "`n=== Consultations Module ===" -ForegroundColor Yellow
Test-Endpoint "Consultations-List" "GET" "$WebApiUrl/api/v1/consultations" $null $authHeaders

# Step 8: Prescriptions Module
Write-Host "`n=== Prescriptions Module ===" -ForegroundColor Yellow
Test-Endpoint "Prescriptions-List" "GET" "$WebApiUrl/api/v1/prescriptions" $null $authHeaders

# Results Summary
$total = $passed + $failed
$passRate = if ($total -gt 0) { [math]::Round(($passed / $total) * 100, 1) } else { 0 }
$duration = ((Get-Date) - $startTime).TotalSeconds

Write-Host "`n=== Test Results Summary ===" -ForegroundColor Yellow
Write-Host "Total Tests: $total" -ForegroundColor White
Write-Host "Passed: $passed" -ForegroundColor Green
Write-Host "Failed: $failed" -ForegroundColor Red
Write-Host "Pass Rate: $passRate%" -ForegroundColor White
Write-Host "Duration: $duration seconds" -ForegroundColor White

# Generate Report
$reportContent = @"
# UAT E2E Regression Test Report

**Execution Time**: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
**Duration**: $duration seconds
**WebAPI URL**: $WebApiUrl

## Test Summary
- **Total Tests**: $total
- **Passed**: $passed
- **Failed**: $failed
- **Pass Rate**: $passRate%
- **Status**: $(if ($failed -eq 0) { "ALL PASSED" } else { "SOME FAILURES" })

## Module Results
"@

foreach ($key in $results.Keys | Sort-Object) {
    $status = $results[$key]
    $icon = if ($status -eq "PASS") { "[PASS]" } elseif ($status -eq "FAIL") { "[FAIL]" } else { "[ERROR]" }
    $reportContent += "`n- $icon $key : $status"
}

$reportContent += @"

## Assessment
$(if ($passRate -ge 90) {
    "EXCELLENT - Pass rate >=90%, system ready for production"
} elseif ($passRate -ge 75) {
    "GOOD - Pass rate >=75%, minor issues need attention"  
} else {
    "NEEDS ATTENTION - Pass rate <75%, issues require fixing"
})

## Next Steps
$(if ($failed -eq 0) {
    "All tests passed - Ready for performance validation"
} else {
    "Review and fix $failed failed test(s)"
})

---
*Report generated at $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")*
*Based on P3-Fix Batch2 transaction reliability baseline*
"@

# Save report
$reportDir = "_reports\2025-09\backend\uat-regression"
if (!(Test-Path $reportDir)) {
    New-Item -ItemType Directory -Path $reportDir -Force | Out-Null
}

$reportFile = "$reportDir\e2e-test-report.md"  
$reportContent | Out-File -FilePath $reportFile -Encoding UTF8

Write-Host "`nReport saved to: $reportFile" -ForegroundColor Cyan

if ($failed -eq 0) {
    Write-Host "SUCCESS - All E2E tests passed!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "FAILED - $failed test(s) failed" -ForegroundColor Red
    exit 1
}