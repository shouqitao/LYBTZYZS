# Backend P2-Fix Batch1: Rollback Script
# Purpose: Rollback to previous state if health fixes fail

param(
    [string]$ReportDir = "_reports/2025-09/backend/acceptance/p2-fix-batch1",
    [switch]$Force
)

$ErrorActionPreference = "Continue"
Write-Host "🔙 [Rollback] Starting rollback procedure..." -ForegroundColor Yellow

# Ensure report directory exists
$fullReportPath = Join-Path $PWD $ReportDir
if (!(Test-Path $fullReportPath)) {
    Write-Host "❌ [ERROR] Report directory not found: $fullReportPath" -ForegroundColor Red
    exit 1
}

# Check if rollback is needed
if (!$Force) {
    $healthStatusPath = Join-Path $fullReportPath "health-status.json"
    if (Test-Path $healthStatusPath) {
        try {
            $healthStatus = Get-Content $healthStatusPath | ConvertFrom-Json
            if ($healthStatus.Success -eq $true) {
                Write-Host "✅ Health check passed - rollback not necessary" -ForegroundColor Green
                Write-Host "   Use -Force to rollback anyway" -ForegroundColor Gray
                exit 0
            } else {
                Write-Host "⚠️  Health check failed - proceeding with rollback" -ForegroundColor Yellow
            }
        } catch {
            Write-Host "⚠️  Cannot read health status - proceeding with rollback" -ForegroundColor Yellow
        }
    } else {
        Write-Host "⚠️  No health status found - proceeding with rollback" -ForegroundColor Yellow
    }
}

# Rollback procedure
$rollbackLog = @{
    Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Actions = @()
    Status = "IN_PROGRESS"
    Errors = @()
}

# 1. Kill all WebAPI processes
Write-Host "💀 Killing all WebAPI processes..."
try {
    $dotnetProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
    if ($dotnetProcesses) {
        taskkill /f /im dotnet.exe 2>$null | Out-Null
        $rollbackLog.Actions += "Killed $($dotnetProcesses.Count) dotnet processes"
        Write-Host "   ✅ Killed $($dotnetProcesses.Count) dotnet processes" -ForegroundColor Green
    } else {
        $rollbackLog.Actions += "No dotnet processes to kill"
        Write-Host "   ✅ No processes to kill" -ForegroundColor Green
    }
} catch {
    $error = "Failed to kill processes: $($_.Exception.Message)"
    $rollbackLog.Errors += $error
    Write-Host "   ❌ $error" -ForegroundColor Red
}

# 2. Clear WebAPI build artifacts
Write-Host "🗂️  Clearing WebAPI build artifacts..."
$webapiPath = Join-Path $PWD "src/Server/Services/LYBT.WebAPI"
try {
    $binPath = Join-Path $webapiPath "bin"
    $objPath = Join-Path $webapiPath "obj"
    
    $clearedItems = 0
    if (Test-Path $binPath) {
        Remove-Item $binPath -Recurse -Force -ErrorAction Stop
        $clearedItems++
    }
    
    if (Test-Path $objPath) {
        Remove-Item $objPath -Recurse -Force -ErrorAction Stop
        $clearedItems++
    }
    
    $rollbackLog.Actions += "Cleared $clearedItems build directories"
    Write-Host "   ✅ Cleared $clearedItems build directories" -ForegroundColor Green
} catch {
    $error = "Failed to clear build artifacts: $($_.Exception.Message)"
    $rollbackLog.Errors += $error
    Write-Host "   ❌ $error" -ForegroundColor Red
}

# 3. Reset environment variables
Write-Host "🔧 Resetting environment variables..."
try {
    $env:ASPNETCORE_URLS = $null
    $env:ASPNETCORE_ENVIRONMENT = $null
    $env:DOTNET_ENVIRONMENT = $null
    
    $rollbackLog.Actions += "Reset environment variables"
    Write-Host "   ✅ Environment variables cleared" -ForegroundColor Green
} catch {
    $error = "Failed to reset environment: $($_.Exception.Message)"
    $rollbackLog.Errors += $error
    Write-Host "   ❌ $error" -ForegroundColor Red
}

# 4. Port release verification
Write-Host "🌐 Verifying port release..."
try {
    Start-Sleep -Seconds 3  # Wait for port release
    $portInfo = netstat -ano | Select-String ":8080"
    if ($portInfo) {
        $listeningPorts = $portInfo | Select-String "LISTENING"
        if ($listeningPorts) {
            $rollbackLog.Actions += "Warning: Port 8080 still in use after cleanup"
            Write-Host "   ⚠️  Port 8080 still in use" -ForegroundColor Yellow
        } else {
            $rollbackLog.Actions += "Port 8080 released successfully"
            Write-Host "   ✅ Port 8080 released" -ForegroundColor Green
        }
    } else {
        $rollbackLog.Actions += "Port 8080 completely free"
        Write-Host "   ✅ Port 8080 completely free" -ForegroundColor Green
    }
} catch {
    $error = "Failed to verify port status: $($_.Exception.Message)"
    $rollbackLog.Errors += $error
    Write-Host "   ❌ $error" -ForegroundColor Red
}

# 5. Generate rollback summary
$rollbackLog.Status = if ($rollbackLog.Errors.Count -eq 0) { "SUCCESS" } else { "PARTIAL" }
$rollbackLog | ConvertTo-Json -Depth 3 | Out-File -FilePath "$fullReportPath/rollback-log.json" -Encoding UTF8

# Generate markdown summary
@"
# Rollback Summary

**Timestamp**: $($rollbackLog.Timestamp)  
**Status**: $($rollbackLog.Status)  
**Actions Taken**: $($rollbackLog.Actions.Count)  
**Errors**: $($rollbackLog.Errors.Count)

## Actions Performed

$(($rollbackLog.Actions | ForEach-Object { "- $_" }) -join "`n")

## Errors Encountered

$(if ($rollbackLog.Errors.Count -eq 0) {
    "No errors encountered"
} else {
    ($rollbackLog.Errors | ForEach-Object { "- $_" }) -join "`n"
})

## Next Steps

$(if ($rollbackLog.Status -eq "SUCCESS") {
    "✅ **Rollback completed successfully**
- System has been restored to clean state
- You can now start fresh development/testing
- All WebAPI processes terminated
- Build artifacts cleared
- Environment variables reset"
} else {
    "⚠️ **Rollback completed with warnings**  
- Some issues encountered during rollback
- Manual verification may be required
- Check process list and port usage
- Review error details above"
})

## Manual Verification

To verify rollback success:

\`\`\`powershell
# Check processes
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue

# Check port 8080
netstat -ano | Select-String ":8080"

# Check environment
echo $env:ASPNETCORE_URLS
\`\`\`
"@ | Out-File -FilePath "$fullReportPath/rollback-summary.md" -Encoding UTF8

Write-Host "✅ [Rollback] Procedure completed!" -ForegroundColor Green
Write-Host "📋 Summary:" -ForegroundColor Cyan
Write-Host "   - Status: $($rollbackLog.Status)" -ForegroundColor Gray
Write-Host "   - Actions: $($rollbackLog.Actions.Count)" -ForegroundColor Gray
Write-Host "   - Errors: $($rollbackLog.Errors.Count)" -ForegroundColor Gray
Write-Host "📁 Generated files:" -ForegroundColor Cyan
Write-Host "   - rollback-log.json (detailed log)" -ForegroundColor Gray
Write-Host "   - rollback-summary.md (summary report)" -ForegroundColor Gray

if ($rollbackLog.Status -eq "SUCCESS") {
    Write-Host "" 
    Write-Host "🎯 System restored to clean state - ready for fresh start" -ForegroundColor Green
    exit 0
} else {
    Write-Host "" 
    Write-Host "⚠️  Rollback completed with issues - manual verification recommended" -ForegroundColor Yellow
    exit 1
}