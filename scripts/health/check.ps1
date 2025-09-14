# Backend P2-Fix Batch1: Health Check Script
# Purpose: Verify WebAPI health and generate comprehensive status report

param(
    [string]$ReportDir = "_reports/2025-09/backend/acceptance/p2-fix-batch1",
    [int]$Port = 8080,
    [int]$MaxRetries = 5,
    [int]$RetryDelaySeconds = 3,
    [int]$TimeoutSeconds = 10
)

$ErrorActionPreference = "Continue"
Write-Host "🩺 [Health-Check] Starting comprehensive health verification..." -ForegroundColor Yellow

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
}

# Health check results
$healthCheck = @{
    Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Port = $Port
    Endpoint = "http://localhost:$Port/api/v1/health"
    Status = "UNKNOWN"
    HttpCode = 0
    Response = $null
    Error = $null
    Retries = 0
    Duration = 0
    ProcessCheck = @{}
    PortCheck = @{}
    ConnectionTest = @{}
}

# 1. Process verification
Write-Host "🔍 Checking dotnet processes..."
try {
    $dotnetProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
    if ($dotnetProcesses) {
        $processDetails = @()
        foreach ($proc in $dotnetProcesses) {
            try {
                $cmdline = (Get-CimInstance Win32_Process -Filter "ProcessId = $($proc.Id)" -ErrorAction SilentlyContinue).CommandLine
                $processDetails += [PSCustomObject]@{
                    PID = $proc.Id
                    StartTime = $proc.StartTime
                    CommandLine = if ($cmdline) { $cmdline } else { "Unable to retrieve" }
                }
            } catch {
                $processDetails += [PSCustomObject]@{
                    PID = $proc.Id
                    StartTime = $proc.StartTime
                    CommandLine = "Access denied"
                }
            }
        }
        
        $healthCheck.ProcessCheck = @{
            Status = "FOUND"
            Count = $dotnetProcesses.Count
            Details = $processDetails
        }
        Write-Host "   ✅ Found $($dotnetProcesses.Count) dotnet processes" -ForegroundColor Green
    } else {
        $healthCheck.ProcessCheck = @{
            Status = "NONE"
            Count = 0
            Details = @()
        }
        Write-Host "   ⚠️  No dotnet processes found" -ForegroundColor Yellow
    }
} catch {
    $healthCheck.ProcessCheck = @{
        Status = "ERROR"
        Error = $_.Exception.Message
    }
    Write-Host "   ❌ Error checking processes: $($_.Exception.Message)" -ForegroundColor Red
}

# 2. Port binding verification
Write-Host "🌐 Checking port $Port binding..."
try {
    $portInfo = netstat -ano | Select-String ":$Port"
    if ($portInfo) {
        $listeningPorts = $portInfo | Select-String "LISTENING"
        $establishedPorts = $portInfo | Select-String "ESTABLISHED"
        
        $healthCheck.PortCheck = @{
            Status = if ($listeningPorts) { "LISTENING" } else { "NOT_LISTENING" }
            ListeningCount = if ($listeningPorts) { $listeningPorts.Count } else { 0 }
            EstablishedCount = if ($establishedPorts) { $establishedPorts.Count } else { 0 }
            RawOutput = $portInfo -join "`n"
        }
        
        if ($listeningPorts) {
            Write-Host "   ✅ Port $Port is in LISTENING state ($($listeningPorts.Count) entries)" -ForegroundColor Green
        } else {
            Write-Host "   ⚠️  Port $Port is NOT in LISTENING state" -ForegroundColor Yellow
        }
    } else {
        $healthCheck.PortCheck = @{
            Status = "FREE"
            Message = "No connections on port $Port"
        }
        Write-Host "   ⚠️  Port $Port shows no connections" -ForegroundColor Yellow
    }
} catch {
    $healthCheck.PortCheck = @{
        Status = "ERROR"
        Error = $_.Exception.Message
    }
    Write-Host "   ❌ Error checking port: $($_.Exception.Message)" -ForegroundColor Red
}

# 3. TCP connection test
Write-Host "🔗 Testing TCP connection to localhost:$Port..."
try {
    $tcpClient = New-Object System.Net.Sockets.TcpClient
    $connectTask = $tcpClient.ConnectAsync("localhost", $Port)
    $timeoutTask = [System.Threading.Tasks.Task]::Delay([TimeSpan]::FromSeconds($TimeoutSeconds))
    
    $completedTask = [System.Threading.Tasks.Task]::WhenAny($connectTask, $timeoutTask).Result
    
    if ($completedTask -eq $connectTask -and $connectTask.IsCompletedSuccessfully) {
        $healthCheck.ConnectionTest = @{
            Status = "SUCCESS"
            Message = "TCP connection established"
        }
        Write-Host "   ✅ TCP connection successful" -ForegroundColor Green
        $tcpClient.Close()
    } else {
        $healthCheck.ConnectionTest = @{
            Status = "TIMEOUT"
            Message = "TCP connection timeout after $TimeoutSeconds seconds"
        }
        Write-Host "   ⚠️  TCP connection timeout" -ForegroundColor Yellow
    }
} catch {
    $healthCheck.ConnectionTest = @{
        Status = "FAILED"
        Error = $_.Exception.Message
    }
    Write-Host "   ❌ TCP connection failed: $($_.Exception.Message)" -ForegroundColor Red
} finally {
    if ($tcpClient) { $tcpClient.Dispose() }
}

# 4. HTTP health endpoint check with retries
Write-Host "🩺 Testing health endpoint with retries..." -ForegroundColor Yellow
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

for ($retry = 1; $retry -le $MaxRetries; $retry++) {
    Write-Host "   🔄 Attempt $retry/$MaxRetries..." -ForegroundColor Cyan
    
    try {
        $response = Invoke-WebRequest -Uri $healthCheck.Endpoint -Method GET -TimeoutSec $TimeoutSeconds -UseBasicParsing
        
        if ($response.StatusCode -eq 200) {
            $healthCheck.Status = "SUCCESS"
            $healthCheck.HttpCode = $response.StatusCode
            $healthCheck.Response = $response.Content
            $healthCheck.Retries = $retry
            
            Write-Host "   ✅ Health check SUCCESS (HTTP $($response.StatusCode))" -ForegroundColor Green
            break
        } else {
            Write-Host "   ⚠️  Unexpected HTTP status: $($response.StatusCode)" -ForegroundColor Yellow
        }
    } catch {
        $healthCheck.Error = $_.Exception.Message
        $healthCheck.Retries = $retry
        
        Write-Host "   ❌ Attempt $retry failed: $($_.Exception.Message)" -ForegroundColor Red
        
        if ($retry -lt $MaxRetries) {
            Write-Host "   ⏸️  Waiting $RetryDelaySeconds seconds before retry..." -ForegroundColor Gray
            Start-Sleep -Seconds $RetryDelaySeconds
        }
    }
}

$stopwatch.Stop()
$healthCheck.Duration = [math]::Round($stopwatch.Elapsed.TotalSeconds, 2)

if ($healthCheck.Status -eq "UNKNOWN") {
    $healthCheck.Status = "FAILED"
    Write-Host "   ❌ All retry attempts exhausted" -ForegroundColor Red
}

# 5. Generate comprehensive report
Write-Host "📊 Generating health report..." -ForegroundColor Yellow

# Save detailed JSON report
$healthCheck | ConvertTo-Json -Depth 4 | Out-File -FilePath "$fullReportPath/health-check-detailed.json" -Encoding UTF8

# Generate human-readable report
$reportStatus = if ($healthCheck.Status -eq "SUCCESS") { "✅ **PASSED**" } else { "❌ **FAILED**" }
$processStatus = switch ($healthCheck.ProcessCheck.Status) {
    "FOUND" { "✅ $($healthCheck.ProcessCheck.Count) processes running" }
    "NONE" { "⚠️ No processes found" }
    "ERROR" { "❌ Check failed" }
    default { "❓ Unknown" }
}
$portStatus = switch ($healthCheck.PortCheck.Status) {
    "LISTENING" { "✅ Port listening" }
    "NOT_LISTENING" { "⚠️ Port not listening" }
    "FREE" { "⚠️ Port free" }
    "ERROR" { "❌ Check failed" }
    default { "❓ Unknown" }
}
$connectionStatus = switch ($healthCheck.ConnectionTest.Status) {
    "SUCCESS" { "✅ Connection successful" }
    "TIMEOUT" { "⚠️ Connection timeout" }
    "FAILED" { "❌ Connection failed" }
    default { "❓ Unknown" }
}

# Generate markdown report content
$httpResponseSection = if ($healthCheck.Status -eq "SUCCESS") {
    "**Status Code**: $($healthCheck.HttpCode)
**Response**: 
``````
$($healthCheck.Response)
``````"
} else {
    "**Error**: $($healthCheck.Error)"
}

$processInfoSection = if ($healthCheck.ProcessCheck.Details) {
    ($healthCheck.ProcessCheck.Details | ForEach-Object { "- **PID $($_.PID)**: $($_.CommandLine)" }) -join "`n"
} else {
    "No process details available"
}

$recommendationsSection = if ($healthCheck.Status -eq "SUCCESS") {
    "✅ **System is healthy** - WebAPI is responding correctly on port $Port"
} else {
    if ($healthCheck.ProcessCheck.Status -eq "NONE") {
        "🔄 **Start WebAPI process** - No dotnet processes found. Run: ``scripts/health/run-webapi-single.ps1 -Background``"
    } elseif ($healthCheck.PortCheck.Status -ne "LISTENING") {
        "🔧 **Check port binding** - Process may be running but not listening on port $Port"
    } elseif ($healthCheck.ConnectionTest.Status -eq "FAILED") {
        "🌐 **Network issue** - TCP connection failed, check firewall/network configuration"
    } else {
        "🩺 **WebAPI issue** - Process and network OK, but health endpoint not responding"
    }
}

$markdownReport = @"
# Health Check Report

**Timestamp**: $($healthCheck.Timestamp)  
**Endpoint**: $($healthCheck.Endpoint)  
**Status**: $reportStatus  
**Duration**: $($healthCheck.Duration)s  
**Retries**: $($healthCheck.Retries)/$MaxRetries

## Results Summary

| Check Type | Status | Details |
|------------|--------|---------|
| Process Check | $processStatus | $($healthCheck.ProcessCheck.Count) dotnet processes |
| Port Check | $portStatus | Port $Port binding status |
| TCP Connection | $connectionStatus | Direct connection test |
| HTTP Health | $(if ($healthCheck.Status -eq 'SUCCESS') {'✅ HTTP ' + $healthCheck.HttpCode} else {'❌ Failed'}) | Health endpoint response |

## Detailed Results

### HTTP Response
$httpResponseSection

### Process Information
$processInfoSection

## Recommendations

$recommendationsSection
"@

$markdownReport | Out-File -FilePath "$fullReportPath/health-check-report.md" -Encoding UTF8

# Save simple status for automation
$simpleStatus = @{
    Status = $healthCheck.Status
    HttpCode = $healthCheck.HttpCode
    Port = $Port
    Timestamp = $healthCheck.Timestamp
    Success = ($healthCheck.Status -eq "SUCCESS")
}
$simpleStatus | ConvertTo-Json | Out-File -FilePath "$fullReportPath/health-status.json" -Encoding UTF8

Write-Host "✅ [Health-Check] Verification completed!" -ForegroundColor Green
Write-Host "📋 Final Result: $($healthCheck.Status)" -ForegroundColor $(if ($healthCheck.Status -eq "SUCCESS") { "Green" } else { "Red" })
Write-Host "📊 Summary:" -ForegroundColor Cyan
Write-Host "   - HTTP Status: $($healthCheck.HttpCode)" -ForegroundColor Gray
Write-Host "   - Duration: $($healthCheck.Duration)s" -ForegroundColor Gray
Write-Host "   - Retries: $($healthCheck.Retries)/$MaxRetries" -ForegroundColor Gray
Write-Host "   - Processes: $($healthCheck.ProcessCheck.Status)" -ForegroundColor Gray
Write-Host "   - Port: $($healthCheck.PortCheck.Status)" -ForegroundColor Gray
Write-Host "📁 Generated files:" -ForegroundColor Cyan
Write-Host "   - health-check-detailed.json (complete results)" -ForegroundColor Gray
Write-Host "   - health-check-report.md (human-readable report)" -ForegroundColor Gray
Write-Host "   - health-status.json (simple status for automation)" -ForegroundColor Gray

if ($healthCheck.Status -eq "SUCCESS") {
    Write-Host "" 
    Write-Host "🎉 Health check PASSED! WebAPI is healthy at http://localhost:$Port" -ForegroundColor Green
    exit 0
} else {
    Write-Host "" 
    Write-Host "💔 Health check FAILED! See report for details and recommendations" -ForegroundColor Red
    exit 1
}