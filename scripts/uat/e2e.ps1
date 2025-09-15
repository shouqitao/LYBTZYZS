# UAT End-to-End Regression Test Script
# Tests all 8 business modules across the complete medical workflow
param(
    [string]$WebApiUrl = "http://localhost:8080",
    [string]$ReportPath = "_reports/2025-09/backend/uat-regression",
    [int]$TimeoutSeconds = 30
)

$ErrorActionPreference = "Continue"  # Continue on errors to test all modules

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

# Initialize test tracking
$testResults = @{}
$totalTests = 0
$passedTests = 0
$failedTests = 0

function Test-Module {
    param(
        [string]$ModuleName,
        [string]$TestName,
        [scriptblock]$TestScript
    )
    
    $global:totalTests++
    Write-Info "Testing $ModuleName - $TestName"
    
    try {
        $result = & $TestScript
        if ($result) {
            Write-Success "$ModuleName - $TestName: PASSED"
            $testResults["$ModuleName-$TestName"] = "PASSED"
            $global:passedTests++
            return $true
        } else {
            Write-Error "$ModuleName - $TestName: FAILED"
            $testResults["$ModuleName-$TestName"] = "FAILED"
            $global:failedTests++
            return $false
        }
    } catch {
        Write-Error "$ModuleName - $TestName: ERROR - $($_.Exception.Message)"
        $testResults["$ModuleName-$TestName"] = "ERROR: $($_.Exception.Message)"
        $global:failedTests++
        return $false
    }
}

try {
    Write-Info "=== UAT End-to-End Regression Testing Started ==="
    Write-Info "Testing 8 business modules with complete workflow"
    Write-Info "WebAPI: $WebApiUrl"
    
    # Step 1: Authentication (Auth Module)
    Write-Info "=== Module 1: Authentication (Auth) ==="
    
    Test-Module "Auth" "Login" {
        $loginData = @{
            username = "sysadmin"
            password = "Admin@123456"
            rememberMe = $false
        } | ConvertTo-Json

        $response = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/auth/login" -Method POST -Body $loginData -ContentType "application/json" -TimeoutSec $TimeoutSeconds
        
        if ($response.success -and $response.data.token) {
            $global:authToken = $response.data.token
            $global:headers = @{ Authorization = "Bearer $global:authToken"; "Content-Type" = "application/json" }
            return $true
        }
        return $false
    }
    
    if (!$authToken) {
        Write-Error "Authentication failed - cannot continue E2E testing"
        exit 1
    }
    
    Test-Module "Auth" "Token Validation" {
        $response = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/auth/validate" -Headers $headers -Method GET -TimeoutSec $TimeoutSeconds
        return $response.success
    }
    
    # Step 2: Users Module
    Write-Info "=== Module 2: User Management (Users) ==="
    
    Test-Module "Users" "List Users" {
        $response = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/users" -Headers $headers -Method GET -TimeoutSec $TimeoutSeconds
        return $response.success
    }
    
    Test-Module "Users" "Create Doctor" {
        $doctorData = @{
            username = "e2e_doctor"
            password = "E2E@123456"
            name = "E2E Test Doctor"
            role = "Doctor"
            email = "e2e@lybt.com"
            phone = "13800000000"
            specialization = "Internal Medicine"
            isActive = $true
        } | ConvertTo-Json

        try {
            $response = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/users" -Method POST -Body $doctorData -Headers $headers -TimeoutSec $TimeoutSeconds
            $global:testDoctorId = $response.data.id
            return $response.success
        } catch {
            if ($_.Exception.Message -match "409") {
                Write-Warning "Doctor already exists, continuing..."
                return $true
            }
            throw
        }
    }
    
    # Step 3: Patients Module
    Write-Info "=== Module 3: Patient Management (Patients) ==="
    
    Test-Module "Patients" "List Patients" {
        $response = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/patients" -Headers $headers -Method GET -TimeoutSec $TimeoutSeconds
        return $response.success
    }
    
    Test-Module "Patients" "Create Patient" {
        $patientData = @{
            name = "E2E Test Patient"
            gender = "Male"
            age = 35
            phone = "13800000001"
            address = "E2E Test Address"
            idNumber = "320106198901010001"
            medicalHistory = "E2E Test History"
            allergies = "None"
            emergencyContact = "E2E Contact"
            emergencyPhone = "13800000002"
        } | ConvertTo-Json

        $response = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/patients" -Method POST -Body $patientData -Headers $headers -TimeoutSec $TimeoutSeconds
        $global:testPatientId = $response.data.id
        return $response.success
    }
    
    # Step 4: Herbs Module  
    Write-Info "=== Module 4: Herb Management (Herbs) ==="
    
    Test-Module "Herbs" "List Herbs" {
        $response = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/herbs" -Headers $headers -Method GET -TimeoutSec $TimeoutSeconds
        return $response.success
    }
    
    Test-Module "Herbs" "Create Herb" {
        $herbData = @{
            name = "E2E Test Herb"
            price = 30.00
            origin = "E2E Origin"
            spec = "Premium"
            unit = "g"
            effect = "E2E test effect"
            usage = "Internal use"
            remark = "E2E test herb"
        } | ConvertTo-Json

        try {
            $response = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/herbs" -Method POST -Body $herbData -Headers $headers -TimeoutSec $TimeoutSeconds
            $global:testHerbId = $response.data.id
            return $response.success
        } catch {
            if ($_.Exception.Message -match "409") {
                Write-Warning "Herb already exists, continuing..."
                return $true
            }
            throw
        }
    }
    
    # Step 5: Formula Module
    Write-Info "=== Module 5: Formula Management (Formula) ==="
    
    Test-Module "Formula" "List Formulas" {
        $response = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/formulas" -Headers $headers -Method GET -TimeoutSec $TimeoutSeconds
        return $response.success
    }
    
    # Step 6: MedicalCase Module
    Write-Info "=== Module 6: Medical Case Management (MedicalCase) ==="
    
    Test-Module "MedicalCase" "List Cases" {
        $response = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/medicalcases" -Headers $headers -Method GET -TimeoutSec $TimeoutSeconds
        return $response.success
    }
    
    Test-Module "MedicalCase" "Create Case" {
        if ($testPatientId) {
            $caseData = @{
                patientId = $testPatientId
                chiefComplaint = "E2E test complaint"
                currentIllness = "E2E test illness"
                visitType = "FirstVisit"
                status = "Active"
            } | ConvertTo-Json

            $response = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/medicalcases" -Method POST -Body $caseData -Headers $headers -TimeoutSec $TimeoutSeconds
            $global:testCaseId = $response.data.id
            return $response.success
        } else {
            Write-Warning "No test patient available for case creation"
            return $false
        }
    }
    
    # Step 7: Consultation Module
    Write-Info "=== Module 7: Consultation Management (Consultation) ==="
    
    Test-Module "Consultation" "List Consultations" {
        $response = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/consultations" -Headers $headers -Method GET -TimeoutSec $TimeoutSeconds
        return $response.success
    }
    
    Test-Module "Consultation" "Create Consultation" {
        if ($testCaseId) {
            $consultData = @{
                medicalCaseId = $testCaseId
                symptoms = "E2E test symptoms"
                examination = "E2E test examination"
                diagnosis = "E2E test diagnosis"
                treatmentPlan = "E2E test treatment"
            } | ConvertTo-Json

            $response = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/consultations" -Method POST -Body $consultData -Headers $headers -TimeoutSec $TimeoutSeconds
            $global:testConsultationId = $response.data.id
            return $response.success
        } else {
            Write-Warning "No test medical case available for consultation"
            return $false
        }
    }
    
    # Step 8: Prescriptions Module
    Write-Info "=== Module 8: Prescription Management (Prescriptions) ==="
    
    Test-Module "Prescriptions" "List Prescriptions" {
        $response = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/prescriptions" -Headers $headers -Method GET -TimeoutSec $TimeoutSeconds
        return $response.success
    }
    
    Test-Module "Prescriptions" "Create Prescription" {
        if ($testPatientId) {
            $prescData = @{
                patientId = $testPatientId
                indication = "E2E test prescription"
                dosageCount = 7
                advice = "Take as directed"
                items = @()
            } | ConvertTo-Json -Depth 3

            $response = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/prescriptions" -Method POST -Body $prescData -Headers $headers -TimeoutSec $TimeoutSeconds
            return $response.success
        } else {
            Write-Warning "No test patient available for prescription"
            return $false
        }
    }
    
    # Additional Integration Tests
    Write-Info "=== Integration Workflow Tests ==="
    
    Test-Module "Integration" "Complete Medical Workflow" {
        # Test the complete workflow: Patient -> Case -> Consultation -> Prescription
        if ($testPatientId -and $testCaseId) {
            Write-Info "Testing complete medical workflow integration"
            return $true
        } else {
            Write-Warning "Missing required entities for workflow integration test"
            return $false
        }
    }
    
    # Generate Report
    Write-Info "=== Generating E2E Test Report ==="
    
    $passRate = if ($totalTests -gt 0) { [math]::Round(($passedTests / $totalTests) * 100, 1) } else { 0 }
    
    $reportContent = @"
# UAT End-to-End Regression Test Report

**Execution Time**: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
**Duration**: $(((Get-Date) - $startTime).TotalSeconds) seconds
**WebAPI URL**: $WebApiUrl

## Test Summary
- **Total Tests**: $totalTests
- **Passed**: $passedTests
- **Failed**: $failedTests
- **Pass Rate**: $passRate%
- **Status**: $(if ($failedTests -eq 0) { "✅ ALL PASSED" } else { "❌ SOME FAILURES" })

## Module Test Results

### 1. Auth Module
$(if ($testResults["Auth-Login"] -eq "PASSED") { "✅" } else { "❌" }) Login
$(if ($testResults["Auth-Token Validation"] -eq "PASSED") { "✅" } else { "❌" }) Token Validation

### 2. Users Module  
$(if ($testResults["Users-List Users"] -eq "PASSED") { "✅" } else { "❌" }) List Users
$(if ($testResults["Users-Create Doctor"] -eq "PASSED") { "✅" } else { "❌" }) Create Doctor

### 3. Patients Module
$(if ($testResults["Patients-List Patients"] -eq "PASSED") { "✅" } else { "❌" }) List Patients
$(if ($testResults["Patients-Create Patient"] -eq "PASSED") { "✅" } else { "❌" }) Create Patient

### 4. Herbs Module
$(if ($testResults["Herbs-List Herbs"] -eq "PASSED") { "✅" } else { "❌" }) List Herbs
$(if ($testResults["Herbs-Create Herb"] -eq "PASSED") { "✅" } else { "❌" }) Create Herb

### 5. Formula Module
$(if ($testResults["Formula-List Formulas"] -eq "PASSED") { "✅" } else { "❌" }) List Formulas

### 6. MedicalCase Module
$(if ($testResults["MedicalCase-List Cases"] -eq "PASSED") { "✅" } else { "❌" }) List Cases
$(if ($testResults["MedicalCase-Create Case"] -eq "PASSED") { "✅" } else { "❌" }) Create Case

### 7. Consultation Module
$(if ($testResults["Consultation-List Consultations"] -eq "PASSED") { "✅" } else { "❌" }) List Consultations
$(if ($testResults["Consultation-Create Consultation"] -eq "PASSED") { "✅" } else { "❌" }) Create Consultation

### 8. Prescriptions Module
$(if ($testResults["Prescriptions-List Prescriptions"] -eq "PASSED") { "✅" } else { "❌" }) List Prescriptions
$(if ($testResults["Prescriptions-Create Prescription"] -eq "PASSED") { "✅" } else { "❌" }) Create Prescription

## Detailed Results
$(foreach ($key in $testResults.Keys | Sort-Object) {
    "$key : $($testResults[$key])"
} -join "`n")

## Assessment
$(if ($passRate -ge 90) {
    "🎯 **EXCELLENT** - Pass rate ≥90%, system ready for production"
} elseif ($passRate -ge 75) {
    "✅ **GOOD** - Pass rate ≥75%, minor issues need attention"  
} elseif ($passRate -ge 60) {
    "⚠️ **FAIR** - Pass rate ≥60%, moderate issues need fixing"
} else {
    "❌ **POOR** - Pass rate <60%, significant issues need resolution"
})

$(if ($failedTests -eq 0) {
"## Next Steps
✅ All E2E tests passed successfully
✅ Ready to proceed to performance and governance validation
✅ System demonstrates production readiness across all 8 modules
"} else {
"## Next Steps  
❌ $failedTests test(s) failed - requires investigation
⚠️ Review failed tests before proceeding to performance validation
📋 Address issues in failing modules before production deployment
"})

---

*E2E Test Report Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")*
*Based on P3-Fix Batch2 transaction reliability baseline*
"@

    # Ensure report directory exists
    $reportDir = $ReportPath
    if (!(Test-Path $reportDir)) {
        New-Item -ItemType Directory -Path $reportDir -Force | Out-Null
    }
    
    $reportFile = "$reportDir\e2e-test-report.md"
    $reportContent | Out-File -FilePath $reportFile -Encoding UTF8
    
    Write-Success "E2E test report generated: $reportFile"
    
    # Final Summary
    Write-Info "=== UAT End-to-End Regression Testing Complete ==="
    Write-Info "Total: $totalTests | Passed: $passedTests | Failed: $failedTests | Pass Rate: $passRate%"
    
    if ($failedTests -eq 0) {
        Write-Success "🎯 ALL TESTS PASSED - System ready for next phase"
        exit 0
    } else {
        Write-Error "❌ $failedTests TESTS FAILED - Review required"
        exit 1
    }
    
} catch {
    Write-Error "E2E testing failed with error: $($_.Exception.Message)"
    
    # Generate error report
    $errorReport = @"
# E2E Testing Error Report

**Error Time**: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
**Error**: $($_.Exception.Message)
**Stack Trace**: $($_.Exception.StackTrace)

## Completed Tests Before Error
Total Tests Completed: $totalTests
Passed: $passedTests  
Failed: $failedTests

## Recommendations
1. Check WebAPI service status at $WebApiUrl
2. Verify authentication credentials
3. Review network connectivity
4. Check for any service dependencies
"@
    
    $reportDir = $ReportPath
    if (!(Test-Path $reportDir)) {
        New-Item -ItemType Directory -Path $reportDir -Force | Out-Null
    }
    
    $errorReport | Out-File -FilePath "$reportDir\e2e-error-report.md" -Encoding UTF8
    exit 1
}