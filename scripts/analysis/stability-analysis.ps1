# Backend Acceptance Smoke Test - Stability Analysis
# Purpose: Analyze triple run results for stability, fluctuations, and failure patterns

param(
    [string]$ResultsFile = "_reports/2025-09/backend/acceptance-rerun3/triple-run-results.json",
    [string]$OutputDir = "_reports/2025-09/backend/acceptance-rerun3"
)

Write-Host "=== Stability Analysis ===" -ForegroundColor Cyan
Write-Host "Analyzing: $ResultsFile"
Write-Host ""

if (!(Test-Path $ResultsFile)) {
    Write-Host "❌ Results file not found: $ResultsFile" -ForegroundColor Red
    exit 1
}

# Load results
$results = Get-Content $ResultsFile | ConvertFrom-Json

Write-Host "📊 Overall Statistics:" -ForegroundColor Yellow
Write-Host "   Total Rounds: $($results.totalRounds)"
Write-Host "   Total Modules: $($results.totalModules)"
Write-Host "   Total Tests: $($results.summary.totalTests)"
Write-Host "   Overall Pass Rate: $($results.summary.avgPassRate)%"
Write-Host ""

# Module-level stability analysis
Write-Host "🔍 Module Stability Analysis:" -ForegroundColor Yellow

$moduleAnalysis = @{}
$stabilityMatrix = @()

foreach ($module in @("Auth", "Users", "Patients", "Consultation", "Prescriptions", "Herbs", "Formula")) {
    $moduleResults = @()
    
    foreach ($round in $results.rounds) {
        $moduleResult = $round.Results | Where-Object { $_.Module -eq $module }
        if ($moduleResult) {
            $moduleResults += $moduleResult
        }
    }
    
    $passCount = ($moduleResults | Where-Object { $_.Status -eq "PASS" }).Count
    $failCount = ($moduleResults | Where-Object { $_.Status -eq "FAIL" }).Count
    $passRate = if ($moduleResults.Count -gt 0) { [math]::Round(($passCount / $moduleResults.Count) * 100, 2) } else { 0 }
    
    # Calculate response time statistics
    $successfulResults = $moduleResults | Where-Object { $_.Status -eq "PASS" -and $_.ResponseTime -gt 0 }
    $avgResponseTime = if ($successfulResults.Count -gt 0) { 
        [math]::Round(($successfulResults | Measure-Object -Property ResponseTime -Average).Average, 2) 
    } else { -1 }
    
    # Determine stability level
    $stabilityLevel = switch ($passRate) {
        { $_ -eq 100 } { "STABLE" }
        { $_ -ge 75 } { "MOSTLY_STABLE" }
        { $_ -ge 50 } { "UNSTABLE" }
        default { "CRITICAL" }
    }
    
    $color = switch ($stabilityLevel) {
        "STABLE" { "Green" }
        "MOSTLY_STABLE" { "Yellow" }
        "UNSTABLE" { "Magenta" }
        "CRITICAL" { "Red" }
    }
    
    Write-Host "   $module : $passRate% ($passCount/$($moduleResults.Count)) - $stabilityLevel" -ForegroundColor $color
    if ($avgResponseTime -gt 0) {
        Write-Host "      Avg Response Time: ${avgResponseTime}ms" -ForegroundColor Gray
    }
    
    # Check for failure patterns
    $failureErrors = $moduleResults | Where-Object { $_.Status -eq "FAIL" -and $_.Error } | Select-Object -ExpandProperty Error | Sort-Object | Get-Unique
    if ($failureErrors) {
        Write-Host "      Failure Patterns:" -ForegroundColor Gray
        foreach ($error in $failureErrors) {
            Write-Host "        - $error" -ForegroundColor Gray
        }
    }
    
    $moduleAnalysis[$module] = @{
        PassCount = $passCount
        FailCount = $failCount
        PassRate = $passRate
        AvgResponseTime = $avgResponseTime
        StabilityLevel = $stabilityLevel
        FailurePatterns = $failureErrors
        Results = $moduleResults
    }
    
    $stabilityMatrix += @{
        Module = $module
        Round1 = ($moduleResults | Where-Object { ($results.rounds[0].Results | Where-Object { $_.Module -eq $module }).Status -eq $_.Status })[0].Status
        Round2 = ($moduleResults | Where-Object { ($results.rounds[1].Results | Where-Object { $_.Module -eq $module }).Status -eq $_.Status })[1].Status
        Round3 = ($moduleResults | Where-Object { ($results.rounds[2].Results | Where-Object { $_.Module -eq $module }).Status -eq $_.Status })[2].Status
        Stability = $stabilityLevel
        PassRate = $passRate
    }
}

Write-Host ""

# Round-by-round fluctuation analysis
Write-Host "📈 Round Fluctuation Analysis:" -ForegroundColor Yellow
for ($i = 0; $i -lt $results.rounds.Count; $i++) {
    $round = $results.rounds[$i]
    Write-Host "   Round $($round.Round): $($round.PassRate)% pass rate ($($round.PassCount)/$($round.TotalTests)) - Duration: $($round.Duration)s"
}

# Calculate fluctuation coefficient
$passRates = $results.rounds | ForEach-Object { $_.PassRate }
$avgPassRate = ($passRates | Measure-Object -Average).Average
$stdDev = if ($passRates.Count -gt 1) {
    $variance = ($passRates | ForEach-Object { [math]::Pow($_ - $avgPassRate, 2) } | Measure-Object -Sum).Sum / ($passRates.Count - 1)
    [math]::Sqrt($variance)
} else { 0 }
$fluctuationCoeff = if ($avgPassRate -gt 0) { [math]::Round(($stdDev / $avgPassRate) * 100, 2) } else { 0 }

Write-Host "   Fluctuation Coefficient: $fluctuationCoeff%" -ForegroundColor $(if ($fluctuationCoeff -lt 5) { "Green" } elseif ($fluctuationCoeff -lt 15) { "Yellow" } else { "Red" })
Write-Host ""

# Generate failure matrix
Write-Host "❌ Failure Matrix:" -ForegroundColor Yellow
$failedModules = $moduleAnalysis.Keys | Where-Object { $moduleAnalysis[$_].FailCount -gt 0 }
if ($failedModules.Count -gt 0) {
    foreach ($module in $failedModules) {
        $analysis = $moduleAnalysis[$module]
        Write-Host "   $module ($($analysis.FailCount) failures):" -ForegroundColor Red
        foreach ($pattern in $analysis.FailurePatterns) {
            Write-Host "     - $pattern" -ForegroundColor Gray
        }
    }
} else {
    Write-Host "   No failures detected across all rounds" -ForegroundColor Green
}

Write-Host ""

# Recommendations
Write-Host "💡 Recommendations:" -ForegroundColor Yellow
$stableModules = $moduleAnalysis.Keys | Where-Object { $moduleAnalysis[$_].StabilityLevel -eq "STABLE" }
$criticalModules = $moduleAnalysis.Keys | Where-Object { $moduleAnalysis[$_].StabilityLevel -eq "CRITICAL" }
$unstableModules = $moduleAnalysis.Keys | Where-Object { $moduleAnalysis[$_].StabilityLevel -in @("UNSTABLE", "MOSTLY_STABLE") }

if ($stableModules.Count -gt 0) {
    Write-Host "   ✅ Stable Modules ($($stableModules.Count)): $($stableModules -join ', ')" -ForegroundColor Green
}
if ($unstableModules.Count -gt 0) {
    Write-Host "   ⚠️ Review Required ($($unstableModules.Count)): $($unstableModules -join ', ')" -ForegroundColor Yellow
}
if ($criticalModules.Count -gt 0) {
    Write-Host "   🔥 Immediate Attention ($($criticalModules.Count)): $($criticalModules -join ', ')" -ForegroundColor Red
}

# Generate analysis report
$analysisReport = @{
    timestamp = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss.fff")
    summary = @{
        totalTests = $results.summary.totalTests
        totalPassed = $results.summary.totalPassed
        totalFailed = $results.summary.totalFailed
        overallPassRate = $results.summary.avgPassRate
        fluctuationCoefficient = $fluctuationCoeff
        stabilityAssessment = if ($fluctuationCoeff -lt 5) { "HIGH_STABILITY" } elseif ($fluctuationCoeff -lt 15) { "MODERATE_STABILITY" } else { "LOW_STABILITY" }
    }
    moduleAnalysis = $moduleAnalysis
    stabilityMatrix = $stabilityMatrix
    roundFluctuations = $results.rounds | ForEach-Object { @{ Round = $_.Round; PassRate = $_.PassRate; Duration = $_.Duration } }
    recommendations = @{
        stableModules = $stableModules
        unstableModules = $unstableModules
        criticalModules = $criticalModules
    }
}

$outputFile = "$OutputDir/stability-analysis.json"
$analysisReport | ConvertTo-Json -Depth 5 | Out-File -FilePath $outputFile -Encoding UTF8

Write-Host ""
Write-Host "📋 Analysis complete. Report saved to: $outputFile" -ForegroundColor Cyan

# Return appropriate exit code
if ($criticalModules.Count -eq 0 -and $fluctuationCoeff -lt 15) {
    Write-Host "✅ System stability assessment: ACCEPTABLE" -ForegroundColor Green
    exit 0
} else {
    Write-Host "⚠️ System stability assessment: NEEDS_ATTENTION" -ForegroundColor Yellow
    exit 1
}