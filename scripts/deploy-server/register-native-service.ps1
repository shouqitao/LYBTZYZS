# Fix Windows Service timeout and register LYBT-API
# Run this on the Windows server

$ErrorActionPreference = "Continue"

# Kill existing dotnet processes
Stop-Process -Name dotnet -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

# Remove old service if exists
sc.exe stop LYBT-API 2>$null
sc.exe delete LYBT-API 2>$null
Start-Sleep -Seconds 1

# Increase SCM timeout (default 30s -> 60s)
$regPath = "HKLM:\SYSTEM\CurrentControlSet\Control"
$regName = "ServicesPipeTimeout"
$current = (Get-ItemProperty -Path $regPath -Name $regName -ErrorAction SilentlyContinue).ServicesPipeTimeout
if (-not $current -or $current -lt 60000) {
    Set-ItemProperty -Path $regPath -Name $regName -Value 60000 -Type DWord
    Write-Host "Service timeout: 30s -> 60s (registry updated, reboot needed)" -ForegroundColor Yellow
} else {
    Write-Host "Service timeout already: ${current}ms" -ForegroundColor Green
}

# Register service using dotnet.exe host
$dotnetPath = "C:\Program Files\dotnet\dotnet.exe"
$dllPath = "C:\Services\LYBT-API\LYBT.WebAPI.dll"
sc.exe create LYBT-API binPath= "`"$dotnetPath`" `"$dllPath`"" start= auto DisplayName= "LYBT WebAPI"
sc.exe description LYBT-API "LYBT WebAPI - Ling Yin Bao Tang TCM Clinic"
sc.exe failure LYBT-API reset= 86400 actions= restart/5000/restart/10000/restart/30000
Write-Host "Service registered"

# Start the service
Write-Host "Starting service..."
sc.exe start LYBT-API

# Wait longer this time
Start-Sleep -Seconds 15

# Verify
$state = (sc.exe query LYBT-API | Select-String "STATE").ToString().Trim()
Write-Host "Service: $state"

$proc = Get-Process -Name dotnet -ErrorAction SilentlyContinue
if ($proc) {
    Write-Host "PID: $($proc.Id)" -ForegroundColor Green
} else {
    Write-Host "No dotnet process" -ForegroundColor Red
}

$port = Get-NetTCPConnection -LocalPort 5000 -ErrorAction SilentlyContinue
if ($port) {
    Write-Host "Port 5000: LISTENING" -ForegroundColor Green
} else {
    Write-Host "Port 5000: NOT LISTENING" -ForegroundColor Red
}

# Health check
try {
    Start-Sleep -Seconds 2
    $h = (New-Object System.Net.WebClient).DownloadString("http://127.0.0.1:5000/health")
    Write-Host "Health: $h" -ForegroundColor Green
} catch {
    Write-Host "Health check failed" -ForegroundColor Yellow
}
