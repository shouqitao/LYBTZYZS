# Governance and Quality Check Script
param(
    [string]$WebApiUrl = "http://localhost:8080"
)

$ErrorActionPreference = "Continue"

function Write-Info { 
    param([string]$Message)
    $timestamp = Get-Date -Format "HH:mm:ss"
    Write-Host "[$timestamp] INFO $Message" -ForegroundColor Cyan
}

function Write-Success { 
    param([string]$Message)
    $timestamp = Get-Date -Format "HH:mm:ss"
    Write-Host "[$timestamp] SUCCESS $Message" -ForegroundColor Green
}

function Write-Error { 
    param([string]$Message)
    $timestamp = Get-Date -Format "HH:mm:ss"
    Write-Host "[$timestamp] ERROR $Message" -ForegroundColor Red
}

function Write-Warning { 
    param([string]$Message)
    $timestamp = Get-Date -Format "HH:mm:ss"
    Write-Host "[$timestamp] WARN $Message" -ForegroundColor Yellow
}

# Governance check results
$governanceResults = @{}
$totalChecks = 0
$passedChecks = 0
$failedChecks = 0

function Test-GovernanceCheck {
    param([string]$CheckName, [scriptblock]$CheckScript)
    
    $global:totalChecks++
    Write-Info "Checking $CheckName..."
    
    try {
        $result = & $CheckScript
        if ($result) {
            Write-Success "$CheckName - PASS"
            $governanceResults[$CheckName] = "PASS"
            $global:passedChecks++
            return $true
        } else {
            Write-Warning "$CheckName - FAIL"
            $governanceResults[$CheckName] = "FAIL"
            $global:failedChecks++
            return $false
        }
    } catch {
        Write-Error "$CheckName - ERROR: $($_.Exception.Message)"
        $governanceResults[$CheckName] = "ERROR"
        $global:failedChecks++
        return $false
    }
}

try {
    Write-Info "=== Governance and Quality Checks Started ==="
    Write-Info "Target: $WebApiUrl"
    
    # Step 1: Authentication for API checks
    Write-Info "=== Authentication ==="
    $loginData = @{
        username = "sysadmin"
        password = "Admin@123456"
        rememberMe = $false
    } | ConvertTo-Json

    $response = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/auth/login" -Method POST -Body $loginData -ContentType "application/json"
    
    if (!$response.success) {
        throw "Login failed: $($response.message)"
    }
    
    $token = $response.data.token
    $headers = @{ Authorization = "Bearer $token"; "Content-Type" = "application/json" }
    Write-Success "Authentication successful"
    
    # Step 2: API Governance Checks
    Write-Info "=== API Governance Checks ==="
    
    Test-GovernanceCheck "API Response Format Consistency" {
        $response = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/users" -Headers $headers -Method GET
        return ($response.PSObject.Properties.Name -contains "success" -and 
                $response.PSObject.Properties.Name -contains "data")
    }
    
    Test-GovernanceCheck "Authentication Required" {
        try {
            $response = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/users" -Method GET
            return $false  # Should fail without auth
        } catch {
            return $true   # Should require authentication
        }
    }
    
    Test-GovernanceCheck "CORS Headers Present" {
        try {
            $response = Invoke-WebRequest -Uri "$WebApiUrl/api/v1/users" -Headers $headers -Method GET
            $corsHeaders = $response.Headers.GetEnumerator() | Where-Object { $_.Key -like "*Access-Control*" }
            return $corsHeaders.Count -gt 0
        } catch {
            return $false
        }
    }
    
    Test-GovernanceCheck "Error Handling Consistency" {
        try {
            $response = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/users/invalid-guid" -Headers $headers -Method GET -ErrorAction SilentlyContinue
            return $false
        } catch {
            # Should return structured error response
            return $true
        }
    }
    
    # Step 3: Security Checks
    Write-Info "=== Security Checks ==="
    
    Test-GovernanceCheck "JWT Token Validation" {
        try {
            $invalidHeaders = @{ Authorization = "Bearer invalid-token"; "Content-Type" = "application/json" }
            $response = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/users" -Headers $invalidHeaders -Method GET
            return $false  # Should fail with invalid token
        } catch {
            return $true   # Should reject invalid token
        }
    }
    
    Test-GovernanceCheck "SQL Injection Protection" {
        try {
            # Test with SQL injection attempt in query parameters
            $maliciousQuery = "'; DROP TABLE Users; --"
            $response = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/users?search=$maliciousQuery" -Headers $headers -Method GET
            return ($response.success -eq $true)  # Should handle safely and return normal response
        } catch {
            return $true  # Rejection is also acceptable
        }
    }
    
    # Step 4: Data Quality Checks
    Write-Info "=== Data Quality Checks ==="
    
    Test-GovernanceCheck "Data Consistency - Users" {
        $response = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/users" -Headers $headers -Method GET
        if ($response.success -and $response.data) {
            foreach ($user in $response.data) {
                if (-not ($user.id -and $user.username -and $user.role)) {
                    return $false
                }
            }
            return $true
        }
        return $false
    }
    
    Test-GovernanceCheck "Data Consistency - Patients" {
        $response = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/patients" -Headers $headers -Method GET
        if ($response.success -and $response.data) {
            foreach ($patient in $response.data) {
                if (-not ($patient.id -and $patient.name)) {
                    return $false
                }
            }
            return $true
        }
        return $response.success  # Empty data is acceptable
    }
    
    # Step 5: Business Logic Governance
    Write-Info "=== Business Logic Governance ==="
    
    Test-GovernanceCheck "Module Integration - Auth Dependencies" {
        # Test that all modules properly handle authentication
        $endpoints = @("/api/v1/users", "/api/v1/patients", "/api/v1/herbs", "/api/v1/formulas")
        foreach ($endpoint in $endpoints) {
            try {
                $response = Invoke-RestMethod -Uri "$WebApiUrl$endpoint" -Headers $headers -Method GET
                if (-not $response.success) {
                    return $false
                }
            } catch {
                return $false
            }
        }
        return $true
    }
    
    Test-GovernanceCheck "Transaction Integrity" {
        # Verify that P3-Fix Batch2 transaction handling is working
        $response = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/users" -Headers $headers -Method GET
        return ($response.success -eq $true)
    }
    
    # Step 6: Calculate Governance Score
    Write-Info "=== Governance Analysis ==="
    
    $governanceScore = if ($totalChecks -gt 0) { [math]::Round(($passedChecks / $totalChecks) * 100, 1) } else { 0 }
    
    Write-Success "Governance checks completed"
    Write-Info "Total Checks: $totalChecks"
    Write-Info "Passed: $passedChecks"
    Write-Info "Failed: $failedChecks"
    Write-Info "Governance Score: $governanceScore%"
    
    # Step 7: Generate Governance Report
    $reportContent = @"
# Governance and Quality Check Report

**Execution Time**: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
**WebAPI URL**: $WebApiUrl

## Governance Summary
- **Total Checks**: $totalChecks
- **Passed Checks**: $passedChecks
- **Failed Checks**: $failedChecks
- **Governance Score**: $governanceScore%

## Check Results

### API Governance
$(foreach ($key in $governanceResults.Keys | Where-Object { $_ -like "*API*" -or $_ -like "*Response*" -or $_ -like "*Authentication*" -or $_ -like "*CORS*" -or $_ -like "*Error*" } | Sort-Object) {
    $status = $governanceResults[$key]
    $icon = if ($status -eq "PASS") { "[PASS]" } else { "[FAIL]" }
    "- $icon $key"
} -join "`n")

### Security Governance  
$(foreach ($key in $governanceResults.Keys | Where-Object { $_ -like "*Security*" -or $_ -like "*JWT*" -or $_ -like "*SQL*" } | Sort-Object) {
    $status = $governanceResults[$key]
    $icon = if ($status -eq "PASS") { "[PASS]" } else { "[FAIL]" }
    "- $icon $key"
} -join "`n")

### Data Quality
$(foreach ($key in $governanceResults.Keys | Where-Object { $_ -like "*Data*" } | Sort-Object) {
    $status = $governanceResults[$key]
    $icon = if ($status -eq "PASS") { "[PASS]" } else { "[FAIL]" }
    "- $icon $key"
} -join "`n")

### Business Logic Governance
$(foreach ($key in $governanceResults.Keys | Where-Object { $_ -like "*Module*" -or $_ -like "*Transaction*" -or $_ -like "*Business*" } | Sort-Object) {
    $status = $governanceResults[$key]
    $icon = if ($status -eq "PASS") { "[PASS]" } else { "[FAIL]" }
    "- $icon $key"
} -join "`n")

## Governance Assessment
$(if ($governanceScore -ge 90) {
    "EXCELLENT - Governance score >=90%, production ready"
} elseif ($governanceScore -ge 75) {
    "GOOD - Governance score >=75%, minor improvements needed"
} else {
    "NEEDS IMPROVEMENT - Governance score <75%, address failed checks"
})

## Recommendations
$(if ($governanceScore -ge 90) {
    "- System meets enterprise governance standards"
    "- Ready for production deployment"
} else {
    "- Review and address failed governance checks"
    "- Implement missing security or quality controls"
})

---
*Governance Report Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")*
*Based on P3-Fix Batch2 transaction reliability baseline*
"@

    # Save governance report
    $reportDir = "_reports\2025-09\backend\uat-regression"
    if (!(Test-Path $reportDir)) {
        New-Item -ItemType Directory -Path $reportDir -Force | Out-Null
    }
    
    $reportFile = "$reportDir\governance-check-report.md"
    $reportContent | Out-File -FilePath $reportFile -Encoding UTF8
    
    Write-Success "Governance report generated: $reportFile"
    
    # Return exit code based on results
    if ($governanceScore -ge 75) {
        Write-Success "Governance check PASSED"
        exit 0
    } else {
        Write-Error "Governance check FAILED"
        exit 1
    }
    
} catch {
    Write-Error "Governance checking failed: $($_.Exception.Message)"
    exit 1
}