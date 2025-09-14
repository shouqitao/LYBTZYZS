# Backend P2-Fix Batch1: Environment Preflight Check
# Purpose: Collect evidence of current environment state before fixing

param(
    [string]$ReportDir = "_reports/2025-09/backend/acceptance/p2-fix-batch1"
)

$ErrorActionPreference = "Continue"
Write-Host "🔍 [Preflight] Starting environment evidence collection..." -ForegroundColor Yellow

# Ensure report directory exists
$fullReportPath = Join-Path $PWD $ReportDir
if (!(Test-Path $fullReportPath)) {
    New-Item -Path $fullReportPath -ItemType Directory -Force | Out-Null
}

# 1. List active dotnet processes
Write-Host "📋 Collecting active dotnet processes..."
try {
    $dotnetProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Select-Object Id, ProcessName, StartTime, @{Name="CommandLine";Expression={""}}
    
    if ($dotnetProcesses) {
        $processInfo = @()
        foreach ($proc in $dotnetProcesses) {
            try {
                $cmdline = (Get-CimInstance Win32_Process -Filter "ProcessId = $($proc.Id)").CommandLine
                $processInfo += [PSCustomObject]@{
                    PID = $proc.Id
                    ProcessName = $proc.ProcessName
                    StartTime = $proc.StartTime
                    CommandLine = $cmdline
                }
            } catch {
                $processInfo += [PSCustomObject]@{
                    PID = $proc.Id
                    ProcessName = $proc.ProcessName
                    StartTime = $proc.StartTime
                    CommandLine = "Unable to retrieve"
                }
            }
        }
        
        $processInfo | ConvertTo-Json -Depth 2 | Out-File -FilePath "$fullReportPath/preflight-processes.txt" -Encoding UTF8
        Write-Host "✅ Found $($processInfo.Count) dotnet processes, saved to preflight-processes.txt" -ForegroundColor Green
    } else {
        "No active dotnet processes found" | Out-File -FilePath "$fullReportPath/preflight-processes.txt" -Encoding UTF8
        Write-Host "✅ No active dotnet processes found" -ForegroundColor Green
    }
} catch {
    "Error collecting process info: $($_.Exception.Message)" | Out-File -FilePath "$fullReportPath/preflight-processes.txt" -Encoding UTF8
    Write-Host "⚠️  Error collecting process info: $($_.Exception.Message)" -ForegroundColor Yellow
}

# 2. List port 8080 usage
Write-Host "🌐 Checking port 8080 usage..."
try {
    $portInfo = netstat -ano | Select-String ":8080"
    if ($portInfo) {
        $portInfo | Out-File -FilePath "$fullReportPath/preflight-ports.txt" -Encoding UTF8
        Write-Host "✅ Port 8080 info collected: $($portInfo.Count) entries" -ForegroundColor Green
    } else {
        "Port 8080 is not in use" | Out-File -FilePath "$fullReportPath/preflight-ports.txt" -Encoding UTF8
        Write-Host "✅ Port 8080 is free" -ForegroundColor Green
    }
} catch {
    "Error checking port 8080: $($_.Exception.Message)" | Out-File -FilePath "$fullReportPath/preflight-ports.txt" -Encoding UTF8
    Write-Host "⚠️  Error checking port: $($_.Exception.Message)" -ForegroundColor Yellow
}

# 3. Export current environment variables
Write-Host "🔧 Collecting environment variables..."
try {
    $envVars = @{
        ASPNETCORE_URLS = $env:ASPNETCORE_URLS
        ASPNETCORE_ENVIRONMENT = $env:ASPNETCORE_ENVIRONMENT
        DOTNET_ENVIRONMENT = $env:DOTNET_ENVIRONMENT
        Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    }
    
    $envVars | ConvertTo-Json -Depth 2 | Out-File -FilePath "$fullReportPath/preflight-env.txt" -Encoding UTF8
    Write-Host "✅ Environment variables collected" -ForegroundColor Green
} catch {
    "Error collecting env vars: $($_.Exception.Message)" | Out-File -FilePath "$fullReportPath/preflight-env.txt" -Encoding UTF8
    Write-Host "⚠️  Error collecting env vars: $($_.Exception.Message)" -ForegroundColor Yellow
}

# 4. Additional system info
Write-Host "💻 Collecting system info..."
try {
    $systemInfo = @{
        CurrentDirectory = $PWD.Path
        Username = $env:USERNAME
        ComputerName = $env:COMPUTERNAME
        OSVersion = [System.Environment]::OSVersion.VersionString
        DotNetVersion = (dotnet --version 2>$null) -replace "`r`n", ""
        Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    }
    
    $systemInfo | ConvertTo-Json -Depth 2 | Out-File -FilePath "$fullReportPath/preflight-system.txt" -Encoding UTF8
    Write-Host "✅ System info collected" -ForegroundColor Green
} catch {
    "Error collecting system info: $($_.Exception.Message)" | Out-File -FilePath "$fullReportPath/preflight-system.txt" -Encoding UTF8
    Write-Host "⚠️  Error collecting system info: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host "✅ [Preflight] Evidence collection completed. Files saved to: $ReportDir" -ForegroundColor Green
Write-Host "📁 Generated files:" -ForegroundColor Cyan
Write-Host "   - preflight-processes.txt (dotnet processes)" -ForegroundColor Gray
Write-Host "   - preflight-ports.txt (port 8080 usage)" -ForegroundColor Gray
Write-Host "   - preflight-env.txt (environment variables)" -ForegroundColor Gray
Write-Host "   - preflight-system.txt (system information)" -ForegroundColor Gray