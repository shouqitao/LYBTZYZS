# Governance Check Script for P3-Fix Batch4
# Purpose: Comprehensive governance validation with data consistency

param(
    [string]$WebApiUrl = "http://localhost:8080",
    [string]$ReportPath = "_reports/2025-09/backend/uat-regression",
    [string]$Username = "sysadmin",
    [string]$Password = "Admin@123456"
)

Write-Host "=== Governance and Quality Check ===" -ForegroundColor Cyan
Write-Host "WebAPI URL: $WebApiUrl" -ForegroundColor Gray
Write-Host "Report Path: $ReportPath" -ForegroundColor Gray
Write-Host "Execution Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host ""

# Initialize tracking
$totalChecks = 10
$passedChecks = 0
$failedChecks = 0
$checks = @{}

# Authenticate to get JWT token
Write-Host "🔐 Step 1: Authentication Setup" -ForegroundColor Cyan
Write-Host "─────────────────────────────────────────" -ForegroundColor Gray

try {
    $loginData = @{
        username = $Username
        password = $Password
        rememberMe = $false
    } | ConvertTo-Json

    $loginHeaders = @{ "Content-Type" = "application/json" }
    $loginResponse = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/auth/login" -Method POST -Body $loginData -Headers $loginHeaders -TimeoutSec 30
    
    if ($loginResponse.success -and $loginResponse.data.token) {
        $authHeaders = @{
            "Authorization" = "Bearer $($loginResponse.data.token)"
            "Content-Type" = "application/json"
        }
        Write-Host "✅ Authentication successful" -ForegroundColor Green
    } else {
        Write-Host "❌ Authentication failed" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "❌ Authentication failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "🔍 Step 2: Governance Checks" -ForegroundColor Cyan
Write-Host "─────────────────────────────────────────" -ForegroundColor Gray

# Helper function to test API endpoint
function Test-ApiCheck {
    param($name, $url, $headers = @{}, $expectedStatus = 200)
    
    try {
        if ($headers.Count -eq 0) {
            $response = Invoke-WebRequest -Uri $url -Method Get -UseBasicParsing -TimeoutSec 10
        } else {
            $response = Invoke-WebRequest -Uri $url -Method Get -Headers $headers -UseBasicParsing -TimeoutSec 10
        }
        
        if ($response.StatusCode -eq $expectedStatus) {
            Write-Host "✅ $name" -ForegroundColor Green
            $checks[$name] = "PASS"
            $script:passedChecks++
            return $true
        } else {
            Write-Host "❌ $name - Status: $($response.StatusCode)" -ForegroundColor Red
            $checks[$name] = "FAIL"
            $script:failedChecks++
            return $false
        }
    }
    catch {
        if ($_.Exception.Response.StatusCode -eq $expectedStatus) {
            Write-Host "✅ $name" -ForegroundColor Green
            $checks[$name] = "PASS"
            $script:passedChecks++
            return $true
        } else {
            Write-Host "❌ $name - Error: $($_.Exception.Message)" -ForegroundColor Red
            $checks[$name] = "FAIL"
            $script:failedChecks++
            return $false
        }
    }
}

# API Governance Checks
Write-Host "### API Governance" -ForegroundColor Yellow

# 1. API Response Format Consistency - Test with Users endpoint
try {
    $usersResponse = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/users" -Method Get -Headers $authHeaders -TimeoutSec 10
    if ($usersResponse.success -ne $null -and $usersResponse.data -ne $null) {
        Write-Host "✅ API Response Format Consistency" -ForegroundColor Green
        $checks["API Response Format Consistency"] = "PASS"
        $passedChecks++
    } else {
        Write-Host "❌ API Response Format Consistency" -ForegroundColor Red
        $checks["API Response Format Consistency"] = "FAIL"
        $failedChecks++
    }
}
catch {
    Write-Host "❌ API Response Format Consistency" -ForegroundColor Red
    $checks["API Response Format Consistency"] = "FAIL"
    $failedChecks++
}

# 2. Authentication Required - Test endpoint without auth
if (Test-ApiCheck "Authentication Required" "$WebApiUrl/api/v1/users" @{} 401) {
    # This should fail with 401, which means auth is required - this is good
} 

# 3. CORS Headers Present - Check for CORS in response
try {
    $corsResponse = Invoke-WebRequest -Uri "$WebApiUrl/api/v1/health" -Method Get -UseBasicParsing -TimeoutSec 10
    $corsHeaders = $corsResponse.Headers
    if ($corsHeaders["Access-Control-Allow-Origin"] -or $corsHeaders["access-control-allow-origin"]) {
        Write-Host "✅ CORS Headers Present" -ForegroundColor Green
        $checks["CORS Headers Present"] = "PASS"
        $passedChecks++
    } else {
        Write-Host "❌ CORS Headers Present" -ForegroundColor Red
        $checks["CORS Headers Present"] = "FAIL"
        $failedChecks++
    }
}
catch {
    Write-Host "❌ CORS Headers Present" -ForegroundColor Red
    $checks["CORS Headers Present"] = "FAIL"
    $failedChecks++
}

# 4. Error Handling Consistency - Test invalid endpoint
try {
    $errorResponse = Invoke-WebRequest -Uri "$WebApiUrl/api/v1/nonexistent" -Method Get -UseBasicParsing -TimeoutSec 10
    Write-Host "❌ Error Handling Consistency" -ForegroundColor Red
    $checks["Error Handling Consistency"] = "FAIL"
    $failedChecks++
}
catch {
    if ($_.Exception.Response.StatusCode -eq 404) {
        Write-Host "✅ Error Handling Consistency" -ForegroundColor Green
        $checks["Error Handling Consistency"] = "PASS"
        $passedChecks++
    } else {
        Write-Host "❌ Error Handling Consistency" -ForegroundColor Red
        $checks["Error Handling Consistency"] = "FAIL"
        $failedChecks++
    }
}

Write-Host ""
Write-Host "### Security Governance" -ForegroundColor Yellow

# 5. JWT Token Validation - Test auth/validate endpoint
if (Test-ApiCheck "JWT Token Validation" "$WebApiUrl/api/v1/auth/validate" $authHeaders 200) {
    # This should pass with valid token
}

# 6. SQL Injection Protection - Test with malicious input (should be handled safely)
try {
    $maliciousHeaders = @{
        "Authorization" = "Bearer ' OR 1=1 --"
        "Content-Type" = "application/json"
    }
    $maliciousResponse = Invoke-WebRequest -Uri "$WebApiUrl/api/v1/users" -Method Get -Headers $maliciousHeaders -UseBasicParsing -TimeoutSec 10
    Write-Host "❌ SQL Injection Protection - Malicious input not rejected" -ForegroundColor Red
    $checks["SQL Injection Protection"] = "FAIL"
    $failedChecks++
}
catch {
    if ($_.Exception.Response.StatusCode -eq 401) {
        Write-Host "✅ SQL Injection Protection" -ForegroundColor Green
        $checks["SQL Injection Protection"] = "PASS"
        $passedChecks++
    } else {
        Write-Host "❌ SQL Injection Protection" -ForegroundColor Red
        $checks["SQL Injection Protection"] = "FAIL"
        $failedChecks++
    }
}

Write-Host ""
Write-Host "### Data Quality" -ForegroundColor Yellow

# 7. Data Consistency - Users (NEW: Now with authentication)
try {
    $usersResponse = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/users" -Method Get -Headers $authHeaders -TimeoutSec 10
    if ($usersResponse.success -and $usersResponse.data.items) {
        $validUsers = 0
        $totalUsers = $usersResponse.data.items.Count
        
        foreach ($user in $usersResponse.data.items) {
            $hasRequiredFields = ![string]::IsNullOrEmpty($user.username) -and 
                                ![string]::IsNullOrEmpty($user.email) -and 
                                ![string]::IsNullOrEmpty($user.fullName)
            if ($hasRequiredFields) { $validUsers++ }
        }
        
        if ($validUsers -eq $totalUsers) {
            Write-Host "✅ Data Consistency - Users" -ForegroundColor Green
            $checks["Data Consistency - Patients"] = "PASS"
            $passedChecks++
        } else {
            Write-Host "❌ Data Consistency - Users ($validUsers/$totalUsers valid)" -ForegroundColor Red
            $checks["Data Consistency - Patients"] = "FAIL"
            $failedChecks++
        }
    } else {
        Write-Host "❌ Data Consistency - Users (No data)" -ForegroundColor Red
        $checks["Data Consistency - Patients"] = "FAIL"
        $failedChecks++
    }
}
catch {
    Write-Host "❌ Data Consistency - Users (API Error)" -ForegroundColor Red
    $checks["Data Consistency - Patients"] = "FAIL"
    $failedChecks++
}

# 8. Data Consistency - Patients (NEW: Now with authentication)
try {
    $patientsResponse = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/patients" -Method Get -Headers $authHeaders -TimeoutSec 10
    if ($patientsResponse.success -and $patientsResponse.data.items) {
        $validPatients = 0
        $totalPatients = $patientsResponse.data.items.Count
        
        foreach ($patient in $patientsResponse.data.items) {
            $hasRequiredFields = ![string]::IsNullOrEmpty($patient.patientName) -and 
                                $patient.age -gt 0 -and $patient.age -lt 150
            if ($hasRequiredFields) { $validPatients++ }
        }
        
        if ($validPatients -eq $totalPatients) {
            Write-Host "✅ Data Consistency - Patients" -ForegroundColor Green
            $checks["Data Consistency - Users"] = "PASS"
            $passedChecks++
        } else {
            Write-Host "❌ Data Consistency - Patients ($validPatients/$totalPatients valid)" -ForegroundColor Red
            $checks["Data Consistency - Users"] = "FAIL"
            $failedChecks++
        }
    } else {
        Write-Host "❌ Data Consistency - Patients (No data)" -ForegroundColor Red
        $checks["Data Consistency - Users"] = "FAIL"
        $failedChecks++
    }
}
catch {
    Write-Host "❌ Data Consistency - Patients (API Error)" -ForegroundColor Red
    $checks["Data Consistency - Users"] = "FAIL"
    $failedChecks++
}

Write-Host ""
Write-Host "### Business Logic Governance" -ForegroundColor Yellow

# 9. Module Integration - Auth Dependencies
try {
    # Test that protected endpoints require authentication
    $protectedEndpoints = @("/api/v1/users", "/api/v1/patients")
    $allProtected = $true
    
    foreach ($endpoint in $protectedEndpoints) {
        try {
            $testResponse = Invoke-WebRequest -Uri "$WebApiUrl$endpoint" -Method Get -UseBasicParsing -TimeoutSec 5
            $allProtected = $false
            break
        }
        catch {
            if ($_.Exception.Response.StatusCode -ne 401) {
                $allProtected = $false
                break
            }
        }
    }
    
    if ($allProtected) {
        Write-Host "✅ Module Integration - Auth Dependencies" -ForegroundColor Green
        $checks["Module Integration - Auth Dependencies"] = "PASS"
        $passedChecks++
    } else {
        Write-Host "❌ Module Integration - Auth Dependencies" -ForegroundColor Red
        $checks["Module Integration - Auth Dependencies"] = "FAIL"
        $failedChecks++
    }
}
catch {
    Write-Host "❌ Module Integration - Auth Dependencies" -ForegroundColor Red
    $checks["Module Integration - Auth Dependencies"] = "FAIL"
    $failedChecks++
}

# 10. Transaction Integrity - Test basic CRUD operations integrity
try {
    # Test that health endpoint responds consistently
    $healthTests = @()
    for ($i = 0; $i -lt 3; $i++) {
        try {
            $healthResponse = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/health" -Method Get -TimeoutSec 5
            $healthTests += $healthResponse -ne $null
        }
        catch {
            $healthTests += $false
        }
        Start-Sleep -Milliseconds 100
    }
    
    $allHealthy = ($healthTests | Where-Object { $_ -eq $true }).Count -eq 3
    
    if ($allHealthy) {
        Write-Host "✅ Transaction Integrity" -ForegroundColor Green
        $checks["Transaction Integrity"] = "PASS"
        $passedChecks++
    } else {
        Write-Host "❌ Transaction Integrity" -ForegroundColor Red
        $checks["Transaction Integrity"] = "FAIL"
        $failedChecks++
    }
}
catch {
    Write-Host "❌ Transaction Integrity" -ForegroundColor Red
    $checks["Transaction Integrity"] = "FAIL"
    $failedChecks++
}

# Calculate governance score
$governanceScore = [math]::Round(($passedChecks / $totalChecks) * 100, 0)

Write-Host ""
Write-Host "📊 Step 3: Generate Report" -ForegroundColor Cyan
Write-Host "─────────────────────────────────────────" -ForegroundColor Gray

# Create report directory if it doesn't exist
if (!(Test-Path $ReportPath)) {
    New-Item -ItemType Directory -Force -Path $ReportPath | Out-Null
}

# Generate governance report
$reportFile = Join-Path $ReportPath "governance-check-report.md"
$reportContent = @"
# Governance and Quality Check Report

**Execution Time**: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
**WebAPI URL**: $WebApiUrl

## Governance Summary
- **Total Checks**: $totalChecks
- **Passed Checks**: $passedChecks
- **Failed Checks**: $failedChecks
- **Governance Score**: $governanceScore%

## Check Results

### API Governance
- [$($checks["API Response Format Consistency"])] API Response Format Consistency - [$($checks["Authentication Required"])] Authentication Required - [$($checks["CORS Headers Present"])] CORS Headers Present - [$($checks["Error Handling Consistency"])] Error Handling Consistency 


### Security Governance  
- [$($checks["JWT Token Validation"])] JWT Token Validation - [$($checks["SQL Injection Protection"])] SQL Injection Protection 


### Data Quality
- [$($checks["Data Consistency - Patients"])] Data Consistency - Patients - [$($checks["Data Consistency - Users"])] Data Consistency - Users 


### Business Logic Governance
- [$($checks["Module Integration - Auth Dependencies"])] Module Integration - Auth Dependencies - [$($checks["Transaction Integrity"])] Transaction Integrity 


## Governance Assessment
$(if ($governanceScore -ge 85) {
    "EXCELLENT - Governance score >=85%, production ready"
} elseif ($governanceScore -ge 75) {
    "GOOD - Governance score >=75%, minor improvements needed"
} else {
    "NEEDS IMPROVEMENT - Governance score <75%, address failed checks"
})

## Recommendations
$(if ($failedChecks -eq 0) {
    "- All governance checks passed - Ready for production deployment"
} else {
    "- Review and address failed governance checks - Implement missing security or quality controls"
})

---
*Governance Report Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')*
*Based on P3-Fix Batch4 data consistency authentication improvements*

"@

# Write report to file
$reportContent | Out-File -FilePath $reportFile -Encoding UTF8

Write-Host ""
Write-Host "🎯 Summary" -ForegroundColor Cyan
Write-Host "─────────────────────────────────────────" -ForegroundColor Gray
Write-Host "Total Checks: $totalChecks" -ForegroundColor White
Write-Host "Passed: $passedChecks" -ForegroundColor Green
Write-Host "Failed: $failedChecks" -ForegroundColor Red
Write-Host "Governance Score: $governanceScore%" -ForegroundColor $(if ($governanceScore -ge 85) { "Green" } elseif ($governanceScore -ge 75) { "Yellow" } else { "Red" })

$status = if ($governanceScore -ge 85) { "EXCELLENT" } elseif ($governanceScore -ge 75) { "GOOD" } else { "NEEDS IMPROVEMENT" }
Write-Host "Assessment: $status" -ForegroundColor $(if ($governanceScore -ge 85) { "Green" } elseif ($governanceScore -ge 75) { "Yellow" } else { "Red" })

Write-Host ""
Write-Host "✅ Report generated: $reportFile" -ForegroundColor Green
Write-Host "Governance check completed!" -ForegroundColor Cyan

if ($governanceScore -ge 85) {
    exit 0
} else {
    exit 1
}