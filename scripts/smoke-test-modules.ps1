# 7-Module Smoke Test Script
param(
    [string]$BaseUrl = "http://localhost:5001", 
    [string]$ReportDir = "_reports/2025-09/backend/acceptance-rerun"
)

$ErrorActionPreference = "Continue"
Write-Host "Smoke Test: Starting 7-module comprehensive testing..." -ForegroundColor Yellow

# Ensure report directory exists
$fullReportPath = Join-Path $PWD $ReportDir
if (!(Test-Path $fullReportPath)) {
    New-Item -Path $fullReportPath -ItemType Directory -Force | Out-Null
}

# Test modules configuration
$modules = @(
    @{ Name = "Auth"; Endpoints = @("/api/v1/auth/login", "/api/v1/auth/logout") },
    @{ Name = "Users"; Endpoints = @("/api/v1/users", "/api/v1/users/statistics") },
    @{ Name = "Patients"; Endpoints = @("/api/v1/patients", "/api/v1/patients/search") },
    @{ Name = "Consultation"; Endpoints = @("/api/v1/consultation", "/api/v1/consultation/statistics") },
    @{ Name = "Prescriptions"; Endpoints = @("/api/v1/prescriptions", "/api/v1/prescriptions/search") },
    @{ Name = "Herbs"; Endpoints = @("/api/v1/herbs", "/api/v1/herbs/search") },
    @{ Name = "Formula"; Endpoints = @("/api/v1/formulas", "/api/v1/formulas/search") }
)

# Initialize test results
$testResults = @{
    timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    baseUrl = $BaseUrl
    totalModules = $modules.Count
    results = @()
    summary = @{
        passed = 0
        failed = 0
        blocked = 0
    }
}

Write-Host "Testing $($modules.Count) modules on $BaseUrl..." -ForegroundColor Cyan

foreach ($module in $modules) {
    Write-Host "Testing module: $($module.Name)" -ForegroundColor White
    
    $moduleResult = @{
        moduleName = $module.Name
        status = "UNKNOWN"
        endpoints = @()
        errors = @()
        timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    }
    
    $allEndpointsOk = $true
    
    foreach ($endpoint in $module.Endpoints) {
        $fullUrl = "$BaseUrl$endpoint"
        Write-Host "  Testing endpoint: $endpoint" -ForegroundColor Gray
        
        try {
            # Try HEAD request first to check endpoint existence
            $response = Invoke-WebRequest -Uri $fullUrl -Method HEAD -TimeoutSec 5 -ErrorAction Stop
            
            $endpointResult = @{
                endpoint = $endpoint
                status = "REACHABLE"
                statusCode = $response.StatusCode
                method = "HEAD"
            }
            
            Write-Host "    SUCCESS: $endpoint (Status: $($response.StatusCode))" -ForegroundColor Green
        } catch {
            $endpointResult = @{
                endpoint = $endpoint
                status = "FAILED"
                error = $_.Exception.Message
                method = "HEAD"
            }
            
            $moduleResult.errors += $_.Exception.Message
            $allEndpointsOk = $false
            
            Write-Host "    FAILED: $endpoint - $($_.Exception.Message)" -ForegroundColor Red
        }
        
        $moduleResult.endpoints += $endpointResult
    }
    
    # Determine module status
    if ($allEndpointsOk) {
        $moduleResult.status = "PASSED"
        $testResults.summary.passed++
        Write-Host "Module $($module.Name): PASSED" -ForegroundColor Green
    } elseif ($moduleResult.errors.Count -eq $module.Endpoints.Count) {
        $moduleResult.status = "BLOCKED"
        $testResults.summary.blocked++
        Write-Host "Module $($module.Name): BLOCKED (all endpoints failed)" -ForegroundColor Magenta
    } else {
        $moduleResult.status = "PARTIAL"
        $testResults.summary.failed++
        Write-Host "Module $($module.Name): PARTIAL (some endpoints failed)" -ForegroundColor Yellow
    }
    
    $testResults.results += $moduleResult
}

# Generate summary
Write-Host "`nTest Summary:" -ForegroundColor Cyan
Write-Host "  Total Modules: $($testResults.totalModules)" -ForegroundColor White
Write-Host "  Passed: $($testResults.summary.passed)" -ForegroundColor Green
Write-Host "  Failed: $($testResults.summary.failed)" -ForegroundColor Yellow  
Write-Host "  Blocked: $($testResults.summary.blocked)" -ForegroundColor Magenta

# Save results
$testResults | ConvertTo-Json -Depth 5 | Out-File -FilePath "$fullReportPath/smoke-test-results.json" -Encoding UTF8

Write-Host "`nResults saved to: $ReportDir/smoke-test-results.json" -ForegroundColor Cyan

# Return overall status
if ($testResults.summary.passed -eq $testResults.totalModules) {
    Write-Host "OVERALL RESULT: ALL MODULES PASSED" -ForegroundColor Green
    return $true
} elseif ($testResults.summary.blocked -eq $testResults.totalModules) {
    Write-Host "OVERALL RESULT: ALL MODULES BLOCKED" -ForegroundColor Red
    return $false
} else {
    Write-Host "OVERALL RESULT: MIXED RESULTS - NEEDS INVESTIGATION" -ForegroundColor Yellow
    return $false
}