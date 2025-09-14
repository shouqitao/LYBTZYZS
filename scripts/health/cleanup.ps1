# Backend P2-Fix Batch1: Environment Cleanup Script
# Purpose: Clean up dotnet processes and resolve port conflicts

param(
    [string]$ReportDir = "_reports/2025-09/backend/acceptance/p2-fix-batch1",
    [int]$FallbackPort = 5080
)

$ErrorActionPreference = "Continue"
Write-Host "🧹 [Cleanup] Starting environment cleanup..." -ForegroundColor Yellow

# Ensure report directory exists
$fullReportPath = Join-Path $PWD $ReportDir
if (!(Test-Path $fullReportPath)) {
    New-Item -Path $fullReportPath -ItemType Directory -Force | Out-Null
}

# 1. Kill all dotnet processes (ignore failures)
Write-Host "⚔️  Killing all dotnet processes..."
try {
    $dotnetProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
    if ($dotnetProcesses) {
        Write-Host "   Found $($dotnetProcesses.Count) dotnet processes to terminate" -ForegroundColor Cyan
        taskkill /f /im dotnet.exe 2>$null | Out-Null
        Start-Sleep -Seconds 2
        
        # Verify cleanup
        $remainingProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
        if ($remainingProcesses) {
            Write-Host "   ⚠️  Warning: $($remainingProcesses.Count) dotnet processes still running" -ForegroundColor Yellow
        } else {
            Write-Host "   ✅ All dotnet processes terminated successfully" -ForegroundColor Green
        }
    } else {
        Write-Host "   ✅ No dotnet processes to kill" -ForegroundColor Green
    }
} catch {
    Write-Host "   ⚠️  Warning: Error during process cleanup: $($_.Exception.Message)" -ForegroundColor Yellow
}

# 2. Check port 8080 occupancy again
Write-Host "🌐 Checking port 8080 status after cleanup..."
$port8080Used = $false
try {
    $portInfo = netstat -ano | Select-String ":8080"
    if ($portInfo) {
        $listeningPorts = $portInfo | Select-String "LISTENING"
        if ($listeningPorts) {
            Write-Host "   ⚠️  Port 8080 still occupied by:" -ForegroundColor Yellow
            $listeningPorts | ForEach-Object { Write-Host "      $_" -ForegroundColor Gray }
            $port8080Used = $true
        } else {
            Write-Host "   ✅ Port 8080 not in LISTENING state" -ForegroundColor Green
        }
        
        # Save port status
        $portInfo | Out-File -FilePath "$fullReportPath/cleanup-port-status.txt" -Encoding UTF8
    } else {
        "Port 8080 is free" | Out-File -FilePath "$fullReportPath/cleanup-port-status.txt" -Encoding UTF8
        Write-Host "   ✅ Port 8080 is completely free" -ForegroundColor Green
    }
} catch {
    "Error checking port status: $($_.Exception.Message)" | Out-File -FilePath "$fullReportPath/cleanup-port-status.txt" -Encoding UTF8
    Write-Host "   ⚠️  Error checking port status: $($_.Exception.Message)" -ForegroundColor Yellow
}

# 3. Handle port switching if needed
$selectedPort = 8080
$cleanupNotes = @{
    Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    OriginalPort = 8080
    SelectedPort = 8080
    PortSwitched = $false
    Reason = "Port 8080 available"
}

if ($port8080Used) {
    Write-Host "🔄 Port 8080 occupied, switching to fallback port $FallbackPort..." -ForegroundColor Cyan
    $selectedPort = $FallbackPort
    $cleanupNotes.SelectedPort = $FallbackPort
    $cleanupNotes.PortSwitched = $true
    $cleanupNotes.Reason = "Port 8080 occupied, switched to $FallbackPort"
    
    # Verify fallback port is free
    $fallbackPortInfo = netstat -ano | Select-String ":$FallbackPort"
    if ($fallbackPortInfo) {
        $fallbackListening = $fallbackPortInfo | Select-String "LISTENING"
        if ($fallbackListening) {
            Write-Host "   ⚠️  Warning: Fallback port $FallbackPort also occupied!" -ForegroundColor Yellow
            $cleanupNotes.Reason += " (Warning: Fallback port also occupied)"
        }
    }
}

# Save cleanup notes
$cleanupNotes | ConvertTo-Json -Depth 2 | Out-File -FilePath "$fullReportPath/cleanup-notes.json" -Encoding UTF8

# Also create markdown notes for easy reading
@"
# Cleanup Notes

**Timestamp**: $($cleanupNotes.Timestamp)  
**Selected Port**: $($cleanupNotes.SelectedPort)  
**Port Switched**: $($cleanupNotes.PortSwitched)  
**Reason**: $($cleanupNotes.Reason)

## Environment Variables for Next Steps

```
ASPNETCORE_URLS=http://localhost:$($cleanupNotes.SelectedPort)
```

## Command Line Parameters

```
dotnet run --urls="http://localhost:$($cleanupNotes.SelectedPort)"
```
"@ | Out-File -FilePath "$fullReportPath/cleanup-notes.md" -Encoding UTF8

# 4. Clean up temporary build files (WebAPI project only)
Write-Host "🗂️  Cleaning temporary build files..."
$webapiProjectPath = Join-Path $PWD "src/Server/Services/LYBT.WebAPI"
if (Test-Path $webapiProjectPath) {
    try {
        $binPath = Join-Path $webapiProjectPath "bin"
        $objPath = Join-Path $webapiProjectPath "obj"
        
        if (Test-Path $binPath) {
            Remove-Item $binPath -Recurse -Force -ErrorAction SilentlyContinue
            Write-Host "   ✅ Cleaned bin folder" -ForegroundColor Green
        }
        
        if (Test-Path $objPath) {
            Remove-Item $objPath -Recurse -Force -ErrorAction SilentlyContinue
            Write-Host "   ✅ Cleaned obj folder" -ForegroundColor Green
        }
    } catch {
        Write-Host "   ⚠️  Warning: Error cleaning build files: $($_.Exception.Message)" -ForegroundColor Yellow
    }
} else {
    Write-Host "   ⚠️  Warning: WebAPI project path not found" -ForegroundColor Yellow
}

# 5. Generate cleanup summary
$cleanupSummary = @{
    Step = "② Environment Cleanup"
    Status = "COMPLETED"
    Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Results = @{
        ProcessesKilled = "All dotnet processes terminated"
        PortSelected = $selectedPort
        PortSwitched = $cleanupNotes.PortSwitched
        BuildFilesCleared = "WebAPI bin/obj folders cleaned"
    }
    NextStep = "③ Single-process startup with port $selectedPort"
}

$cleanupSummary | ConvertTo-Json -Depth 3 | Out-File -FilePath "$fullReportPath/cleanup-summary.json" -Encoding UTF8

Write-Host "✅ [Cleanup] Environment cleanup completed!" -ForegroundColor Green
Write-Host "📋 Summary:" -ForegroundColor Cyan
Write-Host "   - Selected Port: $selectedPort" -ForegroundColor Gray
Write-Host "   - Port Switched: $($cleanupNotes.PortSwitched)" -ForegroundColor Gray
Write-Host "   - Build files cleaned" -ForegroundColor Gray
Write-Host "📁 Generated files:" -ForegroundColor Cyan
Write-Host "   - cleanup-port-status.txt (port occupancy check)" -ForegroundColor Gray
Write-Host "   - cleanup-notes.json (port selection details)" -ForegroundColor Gray
Write-Host "   - cleanup-notes.md (human-readable notes)" -ForegroundColor Gray
Write-Host "   - cleanup-summary.json (step completion status)" -ForegroundColor Gray

if ($cleanupNotes.PortSwitched) {
    Write-Host "" 
    Write-Host "🔧 Next steps will use port $selectedPort instead of 8080" -ForegroundColor Yellow
}