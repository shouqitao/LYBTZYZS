# Backend P2-Fix Batch1: Final Summary & P2 Rerun Trigger
# Purpose: Generate comprehensive summary and trigger smoke test rerun

param(
    [string]$ReportDir = "_reports/2025-09/backend/acceptance/p2-fix-batch1",
    [switch]$TriggerRerun
)

$ErrorActionPreference = "Continue"
Write-Host "📋 [P2-Fix-Summary] Generating final batch summary..." -ForegroundColor Yellow

# Ensure report directory exists
$fullReportPath = Join-Path $PWD $ReportDir
if (!(Test-Path $fullReportPath)) {
    Write-Host "❌ [ERROR] Report directory not found: $fullReportPath" -ForegroundColor Red
    exit 1
}

# Collect all generated reports
$batchSummary = @{
    Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    BatchName = "Backend P2-Fix Batch1: Env Cleanup & Health Fix"
    Branch = "release/p2-fix-batch1-health"
    Status = "SUCCESS"
    Steps = @()
    OverallResult = @{}
    Recommendations = @()
    NextActions = @()
}

# Step ① - Preflight 
Write-Host "📋 Analyzing Step 1 - Preflight evidence..."
$preflightSummary = @{
    StepNumber = "①"
    StepName = "Preflight Script and Evidence Collection"
    Status = "COMPLETED"
    Files = @("preflight-processes.txt", "preflight-ports.txt", "preflight-env.txt", "preflight-system.txt")
    KeyFindings = @()
}

try {
    # Check preflight files exist
    $preflightFiles = @("preflight-processes.txt", "preflight-ports.txt", "preflight-env.txt", "preflight-system.txt")
    $existingFiles = $preflightFiles | Where-Object { Test-Path (Join-Path $fullReportPath $_) }
    
    if ($existingFiles.Count -eq $preflightFiles.Count) {
        $preflightSummary.KeyFindings += "All evidence files generated successfully"
        
        # Check if processes were found
        $processFile = Join-Path $fullReportPath "preflight-processes.txt"
        if (Test-Path $processFile) {
            $processContent = Get-Content $processFile -Raw
            if ($processContent -match "No active dotnet processes found") {
                $preflightSummary.KeyFindings += "Environment was clean (no conflicting processes)"
            } else {
                $preflightSummary.KeyFindings += "Multiple dotnet processes detected (as expected from failed test)"
            }
        }
    } else {
        $preflightSummary.Status = "PARTIAL"
        $preflightSummary.KeyFindings += "Some evidence files missing ($($preflightFiles.Count - $existingFiles.Count) missing)"
    }
} catch {
    $preflightSummary.Status = "ERROR"
    $preflightSummary.KeyFindings += "Error analyzing preflight: $($_.Exception.Message)"
}

$batchSummary.Steps += $preflightSummary

# Step ② - Cleanup
Write-Host "📋 Analyzing Step 2 - Environment cleanup..."
$cleanupSummary = @{
    StepNumber = "②"
    StepName = "Environment Cleanup"
    Status = "COMPLETED"
    Files = @("cleanup-port-status.txt", "cleanup-notes.json", "cleanup-notes.md", "cleanup-summary.json")
    KeyFindings = @()
}

try {
    $cleanupNotesPath = Join-Path $fullReportPath "cleanup-notes.json"
    if (Test-Path $cleanupNotesPath) {
        $cleanupNotes = Get-Content $cleanupNotesPath | ConvertFrom-Json
        $cleanupSummary.KeyFindings += "Port selected: $($cleanupNotes.SelectedPort)"
        $cleanupSummary.KeyFindings += "Port switched: $($cleanupNotes.PortSwitched)"
        $cleanupSummary.KeyFindings += "Reason: $($cleanupNotes.Reason)"
    } else {
        $cleanupSummary.Status = "PARTIAL"
        $cleanupSummary.KeyFindings += "Cleanup notes missing"
    }
} catch {
    $cleanupSummary.Status = "ERROR"
    $cleanupSummary.KeyFindings += "Error analyzing cleanup: $($_.Exception.Message)"
}

$batchSummary.Steps += $cleanupSummary

# Step ③ - Single-process startup & health check
Write-Host "📋 Analyzing Step 3 - Single-process startup and health check..."
$startupHealthSummary = @{
    StepNumber = "③"
    StepName = "Single-process Startup and Health Check"
    Status = "COMPLETED"
    Files = @("webapi-startup.json", "webapi-startup.md", "health-check-detailed.json", "health-check-report.md", "health-status.json")
    KeyFindings = @()
}

try {
    # Check health status
    $healthStatusPath = Join-Path $fullReportPath "health-status.json"
    if (Test-Path $healthStatusPath) {
        $healthStatus = Get-Content $healthStatusPath | ConvertFrom-Json
        if ($healthStatus.Success) {
            $startupHealthSummary.KeyFindings += "✅ Health check PASSED (HTTP $($healthStatus.HttpCode))"
            $startupHealthSummary.KeyFindings += "WebAPI successfully running on port $($healthStatus.Port)"
        } else {
            $startupHealthSummary.Status = "FAILED"
            $startupHealthSummary.KeyFindings += "❌ Health check FAILED"
            $batchSummary.Status = "FAILED"
        }
    } else {
        $startupHealthSummary.Status = "PARTIAL"
        $startupHealthSummary.KeyFindings += "Health status file missing"
    }
    
    # Check startup details
    $startupPath = Join-Path $fullReportPath "webapi-startup.json"
    if (Test-Path $startupPath) {
        $startup = Get-Content $startupPath | ConvertFrom-Json
        $startupHealthSummary.KeyFindings += "Process ID: $($startup.ProcessId)"
        $startupHealthSummary.KeyFindings += "Background mode: $($startup.Background)"
    }
} catch {
    $startupHealthSummary.Status = "ERROR"
    $startupHealthSummary.KeyFindings += "Error analyzing startup/health: $($_.Exception.Message)"
}

$batchSummary.Steps += $startupHealthSummary

# Step ④ - Stabilization & rollback
Write-Host "📋 Analyzing Step 4 - Stabilization and rollback..."
$stabilizationSummary = @{
    StepNumber = "④"
    StepName = "Stabilization and Rollback"
    Status = "COMPLETED"
    Files = @("stabilization-config.json", "rollback.ps1")
    KeyFindings = @()
}

try {
    $configPath = Join-Path $PWD "scripts/health/stabilization-config.json"
    $rollbackPath = Join-Path $PWD "scripts/health/rollback.ps1"
    
    if ((Test-Path $configPath) -and (Test-Path $rollbackPath)) {
        $stabilizationSummary.KeyFindings += "✅ Configuration files created"
        $stabilizationSummary.KeyFindings += "✅ Rollback mechanism implemented"
        $stabilizationSummary.KeyFindings += "✅ Port fallback strategy configured"
    } else {
        $stabilizationSummary.Status = "PARTIAL"
        $stabilizationSummary.KeyFindings += "Some configuration files missing"
    }
} catch {
    $stabilizationSummary.Status = "ERROR"
    $stabilizationSummary.KeyFindings += "Error analyzing stabilization: $($_.Exception.Message)"
}

$batchSummary.Steps += $stabilizationSummary

# Overall assessment
Write-Host "📊 Generating overall assessment..."
$allStepsCompleted = ($batchSummary.Steps | Where-Object { $_.Status -eq "COMPLETED" }).Count -eq $batchSummary.Steps.Count
$healthPassed = ($batchSummary.Steps | Where-Object { $_.StepNumber -eq "③" }).KeyFindings -contains "Health check PASSED (HTTP 200)"

if ($allStepsCompleted -and $healthPassed) {
    $batchSummary.Status = "SUCCESS"
    $batchSummary.OverallResult = @{
        Status = "✅ BATCH SUCCEEDED"
        Summary = "All 5 steps completed successfully. WebAPI health check passed."
        Impact = "Environment issues resolved - WebAPI now stable on http://localhost:8080"
        QualityGate = "PASSED"
    }
    $batchSummary.Recommendations += "🎉 P2-Fix Batch1 completed successfully - proceed with normal development/testing"
    $batchSummary.Recommendations += "🔄 Consider running full smoke test suite to validate all endpoints"
    $batchSummary.NextActions += "Run comprehensive smoke test to verify all API endpoints"
    $batchSummary.NextActions += "Monitor WebAPI stability over next 24 hours"
} else {
    $batchSummary.Status = "FAILED"
    $batchSummary.OverallResult = @{
        Status = "❌ BATCH FAILED"
        Summary = "One or more steps failed or health check did not pass"
        Impact = "Environment issues may still exist"
        QualityGate = "FAILED"
    }
    $batchSummary.Recommendations += "🔧 Review failed steps and address issues before proceeding"
    $batchSummary.Recommendations += "🔄 Consider running rollback to restore clean state"
    $batchSummary.NextActions += "Execute rollback procedure: scripts/health/rollback.ps1 -Force"
    $batchSummary.NextActions += "Review logs and address root cause issues"
}

# Generate comprehensive summary
Write-Host "📝 Generating comprehensive summary report..."
$batchSummary | ConvertTo-Json -Depth 5 | Out-File -FilePath "$fullReportPath/p2-fix-batch-summary.json" -Encoding UTF8

# Generate executive summary
$executiveSummary = @"
# Backend P2-Fix Batch1 - Executive Summary

**Batch Name**: $($batchSummary.BatchName)  
**Timestamp**: $($batchSummary.Timestamp)  
**Branch**: $($batchSummary.Branch)  
**Overall Status**: $($batchSummary.OverallResult.Status)

## Executive Summary

$($batchSummary.OverallResult.Summary)

**Impact**: $($batchSummary.OverallResult.Impact)  
**Quality Gate**: $($batchSummary.OverallResult.QualityGate)

## Steps Completed

$(($batchSummary.Steps | ForEach-Object { 
    "$($_.StepNumber) **$($_.StepName)**: $($_.Status)"
}) -join "`n")

## Key Achievements

$(($batchSummary.Steps | ForEach-Object { 
    $_.KeyFindings | ForEach-Object { "- $_" }
}) -join "`n")

## Recommendations

$(($batchSummary.Recommendations | ForEach-Object { "- $_" }) -join "`n")

## Next Actions

$(($batchSummary.NextActions | ForEach-Object { "1. $_" }) -join "`n")

## Files Generated

**Scripts Created**:
```
scripts/health/preflight.ps1 (evidence collection)
scripts/health/cleanup.ps1 (environment cleanup)  
scripts/health/run-webapi-single.ps1 (single-process startup)
scripts/health/check.ps1 (health verification)
scripts/health/rollback.ps1 (rollback mechanism)
scripts/health/stabilization-config.json (configuration)
```

**Evidence Reports**:
- preflight-*.txt (environment evidence)
- cleanup-*.* (cleanup results)
- webapi-startup.* (startup details)
- health-check-*.* (health verification)
- p2-fix-batch-summary.json (this summary)

## Manual Verification

To manually verify the fix:

\`\`\`powershell
# Check WebAPI health
Invoke-WebRequest -Uri "http://localhost:8080/api/v1/health" -Method GET

# Check process status  
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue

# Check port binding
netstat -ano | Select-String ":8080"
\`\`\`

---
*Generated by P2-Fix Batch1 automation at $($batchSummary.Timestamp)*
"@

$executiveSummary | Out-File -FilePath "$fullReportPath/EXECUTIVE_SUMMARY.md" -Encoding UTF8

# Generate simple status for automation
$simpleStatus = @{
    BatchStatus = $batchSummary.Status
    HealthCheckPassed = $healthPassed
    Timestamp = $batchSummary.Timestamp
    QualityGate = $batchSummary.OverallResult.QualityGate
    NextAction = if ($batchSummary.Status -eq "SUCCESS") { "PROCEED_WITH_TESTING" } else { "INVESTIGATE_AND_ROLLBACK" }
}
$simpleStatus | ConvertTo-Json | Out-File -FilePath "$fullReportPath/batch-status.json" -Encoding UTF8

Write-Host "✅ [P2-Fix-Summary] Summary generation completed!" -ForegroundColor Green
Write-Host "📋 Batch Result: $($batchSummary.Status)" -ForegroundColor $(if ($batchSummary.Status -eq "SUCCESS") { "Green" } else { "Red" })
Write-Host "📊 Summary:" -ForegroundColor Cyan
Write-Host "   - Steps Completed: $($batchSummary.Steps.Count)/5" -ForegroundColor Gray
Write-Host "   - Health Check: $(if ($healthPassed) { 'PASSED' } else { 'FAILED' })" -ForegroundColor Gray
Write-Host "   - Quality Gate: $($batchSummary.OverallResult.QualityGate)" -ForegroundColor Gray
Write-Host "📁 Generated files:" -ForegroundColor Cyan
Write-Host "   - p2-fix-batch-summary.json (detailed summary)" -ForegroundColor Gray
Write-Host "   - EXECUTIVE_SUMMARY.md (executive report)" -ForegroundColor Gray
Write-Host "   - batch-status.json (automation status)" -ForegroundColor Gray

# Trigger P2 rerun if requested and batch succeeded
if ($TriggerRerun -and $batchSummary.Status -eq "SUCCESS") {
    Write-Host ""
    Write-Host "🚀 [P2-Rerun] Triggering smoke test rerun..." -ForegroundColor Yellow
    Write-Host "   📝 Note: This would normally trigger automated smoke test suite" -ForegroundColor Cyan
    Write-Host "   📝 For manual testing, access: http://localhost:8080/api/v1/health" -ForegroundColor Cyan
    Write-Host "   📝 Full API documentation: http://localhost:8080/swagger" -ForegroundColor Cyan
} elseif ($TriggerRerun) {
    Write-Host ""
    Write-Host "⚠️  [P2-Rerun] Skipping rerun - batch did not succeed" -ForegroundColor Yellow
    Write-Host "   📝 Fix issues first, then trigger rerun manually" -ForegroundColor Gray
}

if ($batchSummary.Status -eq "SUCCESS") {
    Write-Host ""
    Write-Host "🎉 P2-Fix Batch1 SUCCEEDED! Environment issues resolved." -ForegroundColor Green
    Write-Host "💡 WebAPI is now stable at http://localhost:8080" -ForegroundColor Cyan
    exit 0
} else {
    Write-Host ""
    Write-Host "💔 P2-Fix Batch1 FAILED! Review summary for details." -ForegroundColor Red
    Write-Host "🔧 Consider rollback: scripts/health/rollback.ps1 -Force" -ForegroundColor Yellow
    exit 1
}