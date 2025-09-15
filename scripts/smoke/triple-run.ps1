# Backend Acceptance Smoke Test - Triple Run
# Purpose: Execute 3 consecutive rounds of smoke tests on 7 major modules
# Modules: Auth / Users / Patients / Consultation / Prescriptions / Herbs / Formula

param(
    [string]$BaseUrl = "http://localhost:8080",
    [string]$AuthTokenFile = "_reports/2025-09/backend/acceptance-rerun3/auth-token.json",
    [string]$OutputDir = "_reports/2025-09/backend/acceptance-rerun3",
    [int]$Rounds = 3,
    [int]$DelayBetweenRounds = 5
)

Write-Host "=== Backend Acceptance Smoke Test - Triple Run ===" -ForegroundColor Cyan
Write-Host "Target: 7 major modules x 3 rounds"
Write-Host "Modules: Auth / Users / Patients / Consultation / Prescriptions / Herbs / Formula"
Write-Host "Base URL: $BaseUrl"
Write-Host ""

# Load auth token
if (Test-Path $AuthTokenFile) {
    $authData = Get-Content $AuthTokenFile | ConvertFrom-Json
    $authToken = $authData.fullToken
    Write-Host "✅ Auth token loaded successfully" -ForegroundColor Green
} else {
    Write-Host "❌ Auth token file not found: $AuthTokenFile" -ForegroundColor Red
    exit 1
}

# Create auth headers
$headers = @{
    "Authorization" = "Bearer $authToken"
    "Content-Type" = "application/json"
}

# Test definitions for 7 modules
$testModules = @(
    @{ Name = "Auth"; Endpoint = "/api/v1/auth"; Method = "GET"; ExpectedStatus = 405; Description = "Auth endpoint availability" },
    @{ Name = "Users"; Endpoint = "/api/v1/users"; Method = "GET"; ExpectedStatus = 200; Description = "List users" },
    @{ Name = "Patients"; Endpoint = "/api/v1/patients"; Method = "GET"; ExpectedStatus = 200; Description = "List patients" },
    @{ Name = "Consultation"; Endpoint = "/api/v1/consultation"; Method = "GET"; ExpectedStatus = 200; Description = "List consultations" },
    @{ Name = "Prescriptions"; Endpoint = "/api/v1/prescriptions"; Method = "GET"; ExpectedStatus = 200; Description = "List prescriptions" },
    @{ Name = "Herbs"; Endpoint = "/api/v1/herbs"; Method = "GET"; ExpectedStatus = 200; Description = "List herbs" },
    @{ Name = "Formula"; Endpoint = "/api/v1/formulas"; Method = "GET"; ExpectedStatus = 200; Description = "List formulas" }
)

$allRoundResults = @()

# Execute 3 rounds
for ($round = 1; $round -le $Rounds; $round++) {
    Write-Host "🔄 === Round $round/$Rounds ===" -ForegroundColor Yellow
    $roundStartTime = Get-Date
    $roundResults = @()
    
    foreach ($module in $testModules) {
        Write-Host "   Testing $($module.Name)..." -ForegroundColor Cyan
        
        try {
            $testStartTime = Get-Date
            $url = "$BaseUrl$($module.Endpoint)"
            
            if ($module.Name -eq "Auth") {
                # Auth endpoint doesn't need authorization
                $response = Invoke-WebRequest -Uri $url -Method $module.Method -TimeoutSec 10 -UseBasicParsing
            } else {
                # Other endpoints need authorization
                $response = Invoke-WebRequest -Uri $url -Method $module.Method -Headers $headers -TimeoutSec 10 -UseBasicParsing
            }
            
            $testEndTime = Get-Date
            $responseTime = ($testEndTime - $testStartTime).TotalMilliseconds
            
            $success = ($response.StatusCode -eq $module.ExpectedStatus)
            $status = if ($success) { "PASS" } else { "FAIL" }
            $color = if ($success) { "Green" } else { "Red" }
            
            Write-Host "      Result: $status (HTTP $($response.StatusCode), ${responseTime}ms)" -ForegroundColor $color
            
            $roundResults += @{
                Module = $module.Name
                Status = $status
                HttpCode = $response.StatusCode
                ExpectedCode = $module.ExpectedStatus
                ResponseTime = [math]::Round($responseTime, 2)
                Timestamp = $testStartTime.ToString("yyyy-MM-dd HH:mm:ss.fff")
                Error = $null
            }
        } catch {
            $testEndTime = Get-Date
            $responseTime = ($testEndTime - $testStartTime).TotalMilliseconds
            
            Write-Host "      Result: FAIL (Error: $($_.Exception.Message))" -ForegroundColor Red
            
            $roundResults += @{
                Module = $module.Name
                Status = "FAIL"
                HttpCode = -1
                ExpectedCode = $module.ExpectedStatus
                ResponseTime = [math]::Round($responseTime, 2)
                Timestamp = $testStartTime.ToString("yyyy-MM-dd HH:mm:ss.fff")
                Error = $_.Exception.Message
            }
        }
    }
    
    $roundEndTime = Get-Date
    $roundDuration = ($roundEndTime - $roundStartTime).TotalSeconds
    $passCount = ($roundResults | Where-Object { $_.Status -eq "PASS" }).Count
    $failCount = ($roundResults | Where-Object { $_.Status -eq "FAIL" }).Count
    
    Write-Host "   Round $round Summary: $passCount PASS, $failCount FAIL (${roundDuration}s)" -ForegroundColor $(if ($failCount -eq 0) { "Green" } else { "Yellow" })
    
    # Store round results
    $roundData = @{
        Round = $round
        StartTime = $roundStartTime.ToString("yyyy-MM-dd HH:mm:ss.fff")
        EndTime = $roundEndTime.ToString("yyyy-MM-dd HH:mm:ss.fff")
        Duration = [math]::Round($roundDuration, 2)
        PassCount = $passCount
        FailCount = $failCount
        TotalTests = $roundResults.Count
        PassRate = [math]::Round(($passCount / $roundResults.Count) * 100, 2)
        Results = $roundResults
    }
    
    $allRoundResults += $roundData
    
    # Delay between rounds (except last round)
    if ($round -lt $Rounds) {
        Write-Host "   Waiting ${DelayBetweenRounds}s before next round..." -ForegroundColor Gray
        Start-Sleep -Seconds $DelayBetweenRounds
        Write-Host ""
    }
}

# Save detailed results
$finalResults = @{
    timestamp = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss.fff")
    totalRounds = $Rounds
    totalModules = $testModules.Count
    baseUrl = $BaseUrl
    rounds = $allRoundResults
    summary = @{
        avgPassRate = [math]::Round((($allRoundResults | ForEach-Object { $_.PassRate } | Measure-Object -Average).Average), 2)
        totalTests = $allRoundResults.Count * $testModules.Count
        totalPassed = ($allRoundResults | ForEach-Object { $_.PassCount } | Measure-Object -Sum).Sum
        totalFailed = ($allRoundResults | ForEach-Object { $_.FailCount } | Measure-Object -Sum).Sum
        avgDuration = [math]::Round((($allRoundResults | ForEach-Object { $_.Duration } | Measure-Object -Average).Average), 2)
    }
}

$outputFile = "$OutputDir/triple-run-results.json"
$finalResults | ConvertTo-Json -Depth 5 | Out-File -FilePath $outputFile -Encoding UTF8

Write-Host ""
Write-Host "=== Triple Run Summary ===" -ForegroundColor Cyan
Write-Host "Total Tests: $($finalResults.summary.totalTests)"
Write-Host "Total Passed: $($finalResults.summary.totalPassed)" -ForegroundColor Green
Write-Host "Total Failed: $($finalResults.summary.totalFailed)" -ForegroundColor $(if ($finalResults.summary.totalFailed -eq 0) { "Green" } else { "Red" })
Write-Host "Average Pass Rate: $($finalResults.summary.avgPassRate)%"
Write-Host "Average Round Duration: $($finalResults.summary.avgDuration)s"
Write-Host ""
Write-Host "Detailed results saved to: $outputFile" -ForegroundColor Cyan

if ($finalResults.summary.totalFailed -eq 0) {
    Write-Host "✅ ALL TRIPLE RUNS PASSED!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "⚠️ Some tests failed in triple runs" -ForegroundColor Yellow
    exit 1
}