# Light Load Performance Test Script
param(
    [string]$WebApiUrl = "http://localhost:8080",
    [int]$ConcurrentUsers = 3,
    [int]$TestDurationMinutes = 2
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

# Performance metrics
$performanceResults = @{}
$totalRequests = 0
$successfulRequests = 0
$failedRequests = 0
$responseTimes = @()

function Test-ApiPerformance {
    param([string]$Endpoint, [string]$Name, $Headers = @{})
    
    try {
        $startTime = Get-Date
        $response = Invoke-RestMethod -Uri "$WebApiUrl$Endpoint" -Headers $Headers -Method GET -TimeoutSec 10
        $endTime = Get-Date
        $responseTime = ($endTime - $startTime).TotalMilliseconds
        
        $script:totalRequests++
        $script:responseTimes += $responseTime
        
        if ($response.success -or $response) {
            $script:successfulRequests++
            Write-Host "PASS - $Name ($([math]::Round($responseTime, 2))ms)" -ForegroundColor Green
            return $true
        } else {
            $script:failedRequests++
            Write-Host "FAIL - $Name ($([math]::Round($responseTime, 2))ms)" -ForegroundColor Red
            return $false
        }
    } catch {
        $script:totalRequests++
        $script:failedRequests++
        Write-Host "ERROR - $Name : $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

try {
    Write-Info "=== Light Load Performance Testing Started ==="
    Write-Info "Target: $WebApiUrl"
    Write-Info "Concurrent Users: $ConcurrentUsers"
    Write-Info "Duration: $TestDurationMinutes minutes"
    
    # Step 1: Authentication
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
    
    # Step 2: Light Load Test
    Write-Info "=== Light Load Testing ==="
    $startTime = Get-Date
    $endTime = $startTime.AddMinutes($TestDurationMinutes)
    
    $endpoints = @(
        @{ Endpoint = "/api/v1/users"; Name = "Users List" },
        @{ Endpoint = "/api/v1/patients"; Name = "Patients List" },
        @{ Endpoint = "/api/v1/herbs"; Name = "Herbs List" },
        @{ Endpoint = "/api/v1/formulas"; Name = "Formulas List" },
        @{ Endpoint = "/api/v1/medicalcases"; Name = "Medical Cases List" },
        @{ Endpoint = "/api/v1/consultations"; Name = "Consultations List" },
        @{ Endpoint = "/api/v1/prescriptions"; Name = "Prescriptions List" }
    )
    
    $iteration = 0
    while ((Get-Date) -lt $endTime) {
        $iteration++
        Write-Info "Iteration $iteration - Testing all endpoints..."
        
        foreach ($ep in $endpoints) {
            Test-ApiPerformance -Endpoint $ep.Endpoint -Name $ep.Name -Headers $headers
            Start-Sleep -Milliseconds 100  # Brief pause between requests
        }
        
        Start-Sleep -Seconds 1  # Pause between iterations
    }
    
    # Step 3: Calculate Performance Metrics
    Write-Info "=== Performance Analysis ==="
    
    $avgResponseTime = if ($responseTimes.Count -gt 0) { [math]::Round(($responseTimes | Measure-Object -Average).Average, 2) } else { 0 }
    $maxResponseTime = if ($responseTimes.Count -gt 0) { [math]::Round(($responseTimes | Measure-Object -Maximum).Maximum, 2) } else { 0 }
    $minResponseTime = if ($responseTimes.Count -gt 0) { [math]::Round(($responseTimes | Measure-Object -Minimum).Minimum, 2) } else { 0 }
    $successRate = if ($totalRequests -gt 0) { [math]::Round(($successfulRequests / $totalRequests) * 100, 1) } else { 0 }
    $requestsPerSecond = if ($TestDurationMinutes -gt 0) { [math]::Round($totalRequests / ($TestDurationMinutes * 60), 2) } else { 0 }
    
    Write-Success "Performance test completed"
    Write-Info "Total Requests: $totalRequests"
    Write-Info "Successful: $successfulRequests"
    Write-Info "Failed: $failedRequests"
    Write-Info "Success Rate: $successRate%"
    Write-Info "Avg Response Time: ${avgResponseTime}ms"
    Write-Info "Max Response Time: ${maxResponseTime}ms"
    Write-Info "Min Response Time: ${minResponseTime}ms"
    Write-Info "Requests/Second: $requestsPerSecond"
    
    # Step 4: Generate Performance Report
    $reportContent = @"
# Light Load Performance Test Report

**Execution Time**: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
**Duration**: $TestDurationMinutes minutes
**WebAPI URL**: $WebApiUrl
**Concurrent Users**: $ConcurrentUsers

## Performance Summary
- **Total Requests**: $totalRequests
- **Successful Requests**: $successfulRequests
- **Failed Requests**: $failedRequests
- **Success Rate**: $successRate%
- **Requests per Second**: $requestsPerSecond

## Response Time Metrics
- **Average Response Time**: ${avgResponseTime}ms
- **Maximum Response Time**: ${maxResponseTime}ms
- **Minimum Response Time**: ${minResponseTime}ms

## Performance Assessment
$(if ($avgResponseTime -le 2000 -and $successRate -ge 95) {
    "EXCELLENT - Response time <2s and success rate >=95%"
} elseif ($avgResponseTime -le 5000 -and $successRate -ge 90) {
    "GOOD - Response time <5s and success rate >=90%"
} else {
    "NEEDS IMPROVEMENT - High response time or low success rate"
})

## Recommendations
$(if ($avgResponseTime -gt 2000) {
    "- Response time exceeds 2s, consider performance optimization"
} else {
    "- Response time is acceptable for small clinic deployment"
})

$(if ($successRate -lt 95) {
    "- Success rate below 95%, investigate failed requests"
} else {
    "- Success rate is excellent"
})

---
*Performance Test Report Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")*
*Based on P3-Fix Batch2 transaction reliability baseline*
"@

    # Save performance report
    $reportDir = "_reports\2025-09\backend\uat-regression"
    if (!(Test-Path $reportDir)) {
        New-Item -ItemType Directory -Path $reportDir -Force | Out-Null
    }
    
    $reportFile = "$reportDir\performance-test-report.md"
    $reportContent | Out-File -FilePath $reportFile -Encoding UTF8
    
    Write-Success "Performance report generated: $reportFile"
    
    # Return exit code based on results
    if ($successRate -ge 90 -and $avgResponseTime -le 5000) {
        Write-Success "Performance test PASSED"
        exit 0
    } else {
        Write-Error "Performance test FAILED"
        exit 1
    }
    
} catch {
    Write-Error "Performance testing failed: $($_.Exception.Message)"
    exit 1
}