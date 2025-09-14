# Backend P2-Fix Batch1: Single-process WebAPI Startup
# Purpose: Start WebAPI in single-process mode on the designated port

param(
    [string]$ReportDir = "_reports/2025-09/backend/acceptance/p2-fix-batch1",
    [int]$Port = 8080,
    [switch]$Background,
    [int]$WaitSeconds = 10
)

$ErrorActionPreference = "Continue"
Write-Host "🚀 [Run-WebAPI-Single] Starting single-process WebAPI..." -ForegroundColor Yellow

# Ensure report directory exists
$fullReportPath = Join-Path $PWD $ReportDir
if (!(Test-Path $fullReportPath)) {
    New-Item -Path $fullReportPath -ItemType Directory -Force | Out-Null
}

# Read port from cleanup notes if available
$cleanupNotesPath = Join-Path $fullReportPath "cleanup-notes.json"
if (Test-Path $cleanupNotesPath) {
    try {
        $cleanupNotes = Get-Content $cleanupNotesPath | ConvertFrom-Json
        $Port = $cleanupNotes.SelectedPort
        Write-Host "📋 Using port from cleanup notes: $Port" -ForegroundColor Cyan
    } catch {
        Write-Host "⚠️  Warning: Could not read cleanup notes, using default port: $Port" -ForegroundColor Yellow
    }
} else {
    Write-Host "📋 No cleanup notes found, using port: $Port" -ForegroundColor Cyan
}

# 1. Final verification - ensure no dotnet processes
Write-Host "🔍 Final process verification..."
$remainingProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
if ($remainingProcesses) {
    Write-Host "   ⚠️  Warning: Found $($remainingProcesses.Count) remaining dotnet processes" -ForegroundColor Yellow
    Write-Host "   💀 Emergency cleanup..." -ForegroundColor Red
    taskkill /f /im dotnet.exe 2>$null | Out-Null
    Start-Sleep -Seconds 3
} else {
    Write-Host "   ✅ No conflicting processes found" -ForegroundColor Green
}

# 2. Prepare startup environment
$webApiPath = Join-Path $PWD "src/Server/Services/LYBT.WebAPI"
if (!(Test-Path $webApiPath)) {
    Write-Host "❌ [ERROR] WebAPI project path not found: $webApiPath" -ForegroundColor Red
    exit 1
}

# Set environment variables
$env:ASPNETCORE_URLS = "http://localhost:$Port"
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:DOTNET_ENVIRONMENT = "Development"

Write-Host "🔧 Environment setup:" -ForegroundColor Cyan
Write-Host "   ASPNETCORE_URLS: $env:ASPNETCORE_URLS" -ForegroundColor Gray
Write-Host "   ASPNETCORE_ENVIRONMENT: $env:ASPNETCORE_ENVIRONMENT" -ForegroundColor Gray
Write-Host "   DOTNET_ENVIRONMENT: $env:DOTNET_ENVIRONMENT" -ForegroundColor Gray

# 3. Start WebAPI with specific parameters
$startupInfo = @{
    Command = "dotnet run --urls=`"http://localhost:$Port`" --no-launch-profile --verbosity minimal"
    WorkingDirectory = $webApiPath
    Port = $Port
    StartTime = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    ProcessId = $null
    Background = $Background.IsPresent
}

Write-Host "🎯 Starting WebAPI process..." -ForegroundColor Yellow
Write-Host "   Working Directory: $webApiPath" -ForegroundColor Gray
Write-Host "   Command: $($startupInfo.Command)" -ForegroundColor Gray
Write-Host "   Background Mode: $($startupInfo.Background)" -ForegroundColor Gray

try {
    if ($Background) {
        # Start in background
        $process = Start-Process -FilePath "dotnet" -ArgumentList "run", "--urls=`"http://localhost:$Port`"", "--no-launch-profile", "--verbosity", "minimal" -WorkingDirectory $webApiPath -PassThru -WindowStyle Hidden
        $startupInfo.ProcessId = $process.Id
        
        Write-Host "   🔄 Background process started (PID: $($process.Id))" -ForegroundColor Green
        Write-Host "   ⏱️  Waiting $WaitSeconds seconds for startup..." -ForegroundColor Cyan
        Start-Sleep -Seconds $WaitSeconds
        
        # Check if process is still running
        if (!$process.HasExited) {
            Write-Host "   ✅ Process is running and stable" -ForegroundColor Green
        } else {
            Write-Host "   ❌ Process exited unexpectedly (Exit code: $($process.ExitCode))" -ForegroundColor Red
            $startupInfo.ProcessId = "EXITED"
        }
    } else {
        # Interactive mode - just show the command
        Write-Host "   📢 Interactive mode - you need to run this command manually:" -ForegroundColor Yellow
        Write-Host "   cd `"$webApiPath`"" -ForegroundColor White
        Write-Host "   $($startupInfo.Command)" -ForegroundColor White
        Write-Host "   💡 Or run with -Background switch for automatic startup" -ForegroundColor Cyan
    }
} catch {
    Write-Host "   ❌ Failed to start WebAPI: $($_.Exception.Message)" -ForegroundColor Red
    $startupInfo.ProcessId = "FAILED"
}

# 4. Generate startup report
$startupInfo | ConvertTo-Json -Depth 2 | Out-File -FilePath "$fullReportPath/webapi-startup.json" -Encoding UTF8

# Generate markdown report
@"
# WebAPI Single-process Startup Report

**Start Time**: $($startupInfo.StartTime)  
**Port**: $($startupInfo.Port)  
**Background Mode**: $($startupInfo.Background)  
**Process ID**: $($startupInfo.ProcessId)

## Command Used

\`\`\`
cd "$($startupInfo.WorkingDirectory)"
$($startupInfo.Command)
\`\`\`

## Environment Variables

\`\`\`
ASPNETCORE_URLS=$env:ASPNETCORE_URLS
ASPNETCORE_ENVIRONMENT=$env:ASPNETCORE_ENVIRONMENT
DOTNET_ENVIRONMENT=$env:DOTNET_ENVIRONMENT
\`\`\`

## Next Steps

1. Wait for WebAPI to fully initialize (~30 seconds)
2. Run health check: \`scripts/health/check.ps1\`
3. Verify endpoint: \`http://localhost:$Port/api/v1/health\`
"@ | Out-File -FilePath "$fullReportPath/webapi-startup.md" -Encoding UTF8

Write-Host "✅ [Run-WebAPI-Single] Startup process completed!" -ForegroundColor Green
Write-Host "📋 Summary:" -ForegroundColor Cyan
Write-Host "   - Target Port: $Port" -ForegroundColor Gray
Write-Host "   - Background Mode: $($startupInfo.Background)" -ForegroundColor Gray
Write-Host "   - Process ID: $($startupInfo.ProcessId)" -ForegroundColor Gray
Write-Host "📁 Generated files:" -ForegroundColor Cyan
Write-Host "   - webapi-startup.json (startup details)" -ForegroundColor Gray
Write-Host "   - webapi-startup.md (startup report)" -ForegroundColor Gray

if ($Background -and $startupInfo.ProcessId -ne "FAILED" -and $startupInfo.ProcessId -ne "EXITED") {
    Write-Host "" 
    Write-Host "🎯 WebAPI should now be available at: http://localhost:$Port" -ForegroundColor Green
    Write-Host "⏭️  Next: Run 'scripts/health/check.ps1' to verify health status" -ForegroundColor Yellow
}