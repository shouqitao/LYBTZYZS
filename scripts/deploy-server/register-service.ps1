$ErrorActionPreference = "Continue"

Write-Host "=== LYBT API Windows Service Setup ===" -ForegroundColor Cyan

# 1. Stop and remove existing processes/services
Write-Host "`n[1/4] Cleaning up..." -ForegroundColor Yellow
Stop-Process -Name dotnet -Force -ErrorAction SilentlyContinue
sc.exe stop LYBT-API 2>$null
sc.exe delete LYBT-API 2>$null
Start-Sleep -Seconds 2
Write-Host "  Done"

# 2. Create start-api.bat (service entry point)
Write-Host "`n[2/4] Creating startup script..." -ForegroundColor Yellow
$batContent = @'
@echo off
cd /d C:\Services\LYBT-API
set ASPNETCORE_ENVIRONMENT=Production
"C:\Program Files\dotnet\dotnet.exe" LYBT.WebAPI.dll
'@
[System.IO.File]::WriteAllText("C:\Services\LYBT-API\start-api.bat", $batContent, [Text.Encoding]::ASCII)
Write-Host "  start-api.bat created"

# 3. Register Windows Service
Write-Host "`n[3/4] Registering Windows Service..." -ForegroundColor Yellow
sc.exe create LYBT-API `
    binPath= "C:\Services\LYBT-API\start-api.bat" `
    start= auto `
    DisplayName= "LYBT WebAPI"

sc.exe description LYBT-API "LYBT WebAPI - Ling Yin Bao Tang TCM Clinic Backend Service"
sc.exe failure LYBT-API reset= 86400 actions= restart/5000/restart/10000/restart/30000

Write-Host "  Service registered" -ForegroundColor Green

# 4. Start the service
Write-Host "`n[4/4] Starting service..." -ForegroundColor Yellow
sc.exe start LYBT-API
Start-Sleep -Seconds 6

# Verify
$proc = Get-Process -Name dotnet -ErrorAction SilentlyContinue
if ($proc) {
    Write-Host "`n[OK] Service started! PID: $($proc.Id)" -ForegroundColor Green
} else {
    Write-Host "`n[WARN] Service process not detected, trying direct launch..." -ForegroundColor Yellow
    sc.exe start LYBT-API 2>$null
    Start-Sleep -Seconds 5
    $proc = Get-Process -Name dotnet -ErrorAction SilentlyContinue
    if ($proc) {
        Write-Host "[OK] Service started on retry! PID: $($proc.Id)" -ForegroundColor Green
    }
}

# Port check
$port = Get-NetTCPConnection -LocalPort 5000 -ErrorAction SilentlyContinue
if ($port) {
    Write-Host "Port 5000: LISTENING (PID $($port.OwningProcess))" -ForegroundColor Green
} else {
    Write-Host "Port 5000: NOT LISTENING" -ForegroundColor Red
}

# Health check
try {
    $h = (New-Object System.Net.WebClient).DownloadString("http://127.0.0.1:5000/health")
    Write-Host "Health: $h" -ForegroundColor Green
} catch {
    Write-Host "Health check failed: $_" -ForegroundColor Yellow
}

# Service status
Write-Host "`n--- Service Status ---" -ForegroundColor Cyan
sc.exe query LYBT-API

Write-Host "`nAccess: http://$(hostname):5000/health" -ForegroundColor Cyan
