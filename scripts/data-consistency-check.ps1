# Data Consistency Check Script for P3-Fix Batch3
# Purpose: Check Users/Patients data consistency issues identified in governance report

param(
    [string]$WebApiUrl = "http://localhost:8080",
    [string]$ReportPath = "_reports/2025-09/backend/p3-fix-batch3"
)

Write-Host "=== P3-Fix Batch3: Data Consistency Check ===" -ForegroundColor Cyan
Write-Host "WebAPI URL: $WebApiUrl" -ForegroundColor Gray
Write-Host "Report Path: $ReportPath" -ForegroundColor Gray
Write-Host "Execution Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host ""

# Create report directory if it doesn't exist
if (!(Test-Path $ReportPath)) {
    New-Item -ItemType Directory -Force -Path $ReportPath | Out-Null
}

$issues = @()
$fixedIssues = @()

# Function to check API endpoint
function Test-ApiEndpoint {
    param($url, $description)
    
    try {
        Write-Host "Checking: $description" -ForegroundColor Yellow
        $response = Invoke-RestMethod -Uri $url -Method Get -TimeoutSec 30
        Write-Host "✅ $description - OK" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "❌ $description - Failed: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

Write-Host "📊 Step 1: Basic API Health Check" -ForegroundColor Cyan
Write-Host "─────────────────────────────────────────" -ForegroundColor Gray

$apiHealthy = Test-ApiEndpoint "$WebApiUrl/api/v1/health" "Health endpoint"
if (-not $apiHealthy) {
    Write-Host "❌ API not available, cannot proceed with data consistency checks" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "🔍 Step 2: Data Consistency Checks" -ForegroundColor Cyan
Write-Host "─────────────────────────────────────────" -ForegroundColor Gray

# Check 1: Users Data Consistency
Write-Host "Checking Users data consistency..." -ForegroundColor Yellow
try {
    $usersResponse = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/users" -Method Get -TimeoutSec 30
    if ($usersResponse.success) {
        $userCount = $usersResponse.data.items.Count
        Write-Host "✅ Users endpoint accessible - Found $userCount users" -ForegroundColor Green
        
        # Check for required fields
        $usersWithIssues = @()
        foreach ($user in $usersResponse.data.items) {
            $userIssues = @()
            
            # Check required fields
            if ([string]::IsNullOrEmpty($user.username)) {
                $userIssues += "Missing username"
            }
            if ([string]::IsNullOrEmpty($user.email)) {
                $userIssues += "Missing email"
            }
            if ([string]::IsNullOrEmpty($user.fullName)) {
                $userIssues += "Missing fullName"
            }
            
            # Check email format
            if (![string]::IsNullOrEmpty($user.email) -and $user.email -notmatch "^[^@\s]+@[^@\s]+\.[^@\s]+$") {
                $userIssues += "Invalid email format"
            }
            
            if ($userIssues.Count -gt 0) {
                $usersWithIssues += @{
                    UserId = $user.id
                    Username = $user.username
                    Issues = $userIssues -join ", "
                }
            }
        }
        
        if ($usersWithIssues.Count -eq 0) {
            Write-Host "✅ Users data consistency - All users have valid data" -ForegroundColor Green
        } else {
            Write-Host "⚠️  Users data consistency - Found $($usersWithIssues.Count) users with issues" -ForegroundColor Yellow
            $issues += @{
                Category = "Users Data Consistency"
                Count = $usersWithIssues.Count
                Details = $usersWithIssues
            }
        }
    } else {
        Write-Host "❌ Users endpoint returned error: $($usersResponse.message)" -ForegroundColor Red
        $issues += @{
            Category = "Users API Error"
            Count = 1
            Details = $usersResponse.message
        }
    }
}
catch {
    Write-Host "❌ Users consistency check failed: $($_.Exception.Message)" -ForegroundColor Red
    $issues += @{
        Category = "Users API Access"
        Count = 1
        Details = $_.Exception.Message
    }
}

# Check 2: Patients Data Consistency
Write-Host "Checking Patients data consistency..." -ForegroundColor Yellow
try {
    $patientsResponse = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/patients" -Method Get -TimeoutSec 30
    if ($patientsResponse.success) {
        $patientCount = $patientsResponse.data.items.Count
        Write-Host "✅ Patients endpoint accessible - Found $patientCount patients" -ForegroundColor Green
        
        # Check for required fields
        $patientsWithIssues = @()
        foreach ($patient in $patientsResponse.data.items) {
            $patientIssues = @()
            
            # Check required fields
            if ([string]::IsNullOrEmpty($patient.patientName)) {
                $patientIssues += "Missing patientName"
            }
            if ([string]::IsNullOrEmpty($patient.gender)) {
                $patientIssues += "Missing gender"
            }
            if ($patient.age -eq $null -or $patient.age -le 0) {
                $patientIssues += "Invalid age"
            }
            
            # Check phone number format (if provided)
            if (![string]::IsNullOrEmpty($patient.phoneNumber) -and $patient.phoneNumber -notmatch "^[\d\-\+\(\)\s]+$") {
                $patientIssues += "Invalid phone number format"
            }
            
            # Check foreign key: createdByUserId should exist in Users
            if (![string]::IsNullOrEmpty($patient.createdByUserId)) {
                $userExists = $usersResponse.data.items | Where-Object { $_.id -eq $patient.createdByUserId }
                if (!$userExists) {
                    $patientIssues += "Invalid createdByUserId - User not found"
                }
            }
            
            if ($patientIssues.Count -gt 0) {
                $patientsWithIssues += @{
                    PatientId = $patient.id
                    PatientName = $patient.patientName
                    Issues = $patientIssues -join ", "
                }
            }
        }
        
        if ($patientsWithIssues.Count -eq 0) {
            Write-Host "✅ Patients data consistency - All patients have valid data" -ForegroundColor Green
        } else {
            Write-Host "⚠️  Patients data consistency - Found $($patientsWithIssues.Count) patients with issues" -ForegroundColor Yellow
            $issues += @{
                Category = "Patients Data Consistency"
                Count = $patientsWithIssues.Count
                Details = $patientsWithIssues
            }
        }
    } else {
        Write-Host "❌ Patients endpoint returned error: $($patientsResponse.message)" -ForegroundColor Red
        $issues += @{
            Category = "Patients API Error"
            Count = 1
            Details = $patientsResponse.message
        }
    }
}
catch {
    Write-Host "❌ Patients consistency check failed: $($_.Exception.Message)" -ForegroundColor Red
    $issues += @{
        Category = "Patients API Access"
        Count = 1
        Details = $_.Exception.Message
    }
}

# Check 3: Auth-Validate endpoint fix verification
Write-Host "Verifying Auth-Validate endpoint fix..." -ForegroundColor Yellow
try {
    # Test GET /api/v1/auth/validate without token (should return 401)
    $authValidateResponse = Invoke-WebRequest -Uri "$WebApiUrl/api/v1/auth/validate" -Method Get -UseBasicParsing
    Write-Host "❌ Auth-Validate should return 401 for requests without Authorization header" -ForegroundColor Red
    $issues += @{
        Category = "Auth-Validate Security"
        Count = 1
        Details = "Endpoint should return 401 for unauthorized requests"
    }
}
catch {
    if ($_.Exception.Response.StatusCode -eq [System.Net.HttpStatusCode]::Unauthorized) {
        Write-Host "✅ Auth-Validate endpoint correctly returns 401 for unauthorized requests" -ForegroundColor Green
        $fixedIssues += "Auth-Validate endpoint 405 error fixed"
    } elseif ($_.Exception.Response.StatusCode -eq [System.Net.HttpStatusCode]::MethodNotAllowed) {
        Write-Host "❌ Auth-Validate endpoint still returns 405 Method Not Allowed" -ForegroundColor Red
        $issues += @{
            Category = "Auth-Validate 405 Error"
            Count = 1
            Details = "GET method still not supported"
        }
    } else {
        Write-Host "⚠️  Auth-Validate endpoint returned unexpected status: $($_.Exception.Response.StatusCode)" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "📋 Step 3: Generate Report" -ForegroundColor Cyan
Write-Host "─────────────────────────────────────────" -ForegroundColor Gray

# Generate report
$reportFile = Join-Path $ReportPath "data-consistency-check-report.md"
$reportContent = @"
# Data Consistency Check Report

**Execution Time**: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
**WebAPI URL**: $WebApiUrl

## Summary

- **Total Issues Found**: $($issues.Count)
- **Issues Fixed**: $($fixedIssues.Count)
- **Status**: $(if ($issues.Count -eq 0) { "PASS" } elseif ($issues.Count -le 2) { "CONDITIONAL_PASS" } else { "FAIL" })

## Fixed Issues

"@

if ($fixedIssues.Count -gt 0) {
    foreach ($fix in $fixedIssues) {
        $reportContent += "- ✅ $fix`n"
    }
} else {
    $reportContent += "- No issues were fixed in this check`n"
}

$reportContent += @"

## Outstanding Issues

"@

if ($issues.Count -eq 0) {
    $reportContent += "- ✅ No data consistency issues found`n"
} else {
    foreach ($issue in $issues) {
        $reportContent += "- ❌ **$($issue.Category)**: $($issue.Count) issue(s)`n"
        if ($issue.Details -is [array]) {
            foreach ($detail in $issue.Details) {
                if ($detail -is [hashtable]) {
                    $reportContent += "  - $($detail.Values -join ' - ')`n"
                } else {
                    $reportContent += "  - $detail`n"
                }
            }
        } else {
            $reportContent += "  - $($issue.Details)`n"
        }
    }
}

$reportContent += @"

## Recommendations

"@

if ($issues.Count -eq 0) {
    $reportContent += "- System data consistency is good, no action required`n"
} else {
    $reportContent += "- Review and fix data consistency issues before production deployment`n"
    $reportContent += "- Implement validation rules to prevent future data inconsistencies`n"
    $reportContent += "- Consider running data cleanup scripts for existing invalid data`n"
}

$reportContent += @"

---
*Data Consistency Check Report Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')*
*Script: data-consistency-check.ps1*
"@

# Write report to file
$reportContent | Out-File -FilePath $reportFile -Encoding UTF8
Write-Host "✅ Report generated: $reportFile" -ForegroundColor Green

# Display summary
Write-Host ""
Write-Host "🎯 Summary" -ForegroundColor Cyan
Write-Host "─────────────────────────────────────────" -ForegroundColor Gray
Write-Host "Total Issues Found: $($issues.Count)" -ForegroundColor $(if ($issues.Count -eq 0) { "Green" } else { "Yellow" })
Write-Host "Issues Fixed: $($fixedIssues.Count)" -ForegroundColor Green
$status = if ($issues.Count -eq 0) { "PASS" } elseif ($issues.Count -le 2) { "CONDITIONAL_PASS" } else { "FAIL" }
Write-Host "Overall Status: $status" -ForegroundColor $(if ($status -eq "PASS") { "Green" } elseif ($status -eq "CONDITIONAL_PASS") { "Yellow" } else { "Red" })

Write-Host ""
Write-Host "Data consistency check completed!" -ForegroundColor Green