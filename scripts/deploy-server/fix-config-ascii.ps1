<#
.SYNOPSIS
    LYBT WebAPI X - Server 2012 R2 X
.DESCRIPTION
    X Kestrel X、SQL X、HTTPS X
    XserverX: powershell -ExecutionPolicy Bypass -File fix-config.ps1
#>

$ErrorActionPreference = "Continue"
$apiDir = "C:\Services\LYBT-API"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " LYBT WebAPI X" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# --- 1. X ---
Write-Host "`n[1/5] X..." -ForegroundColor Yellow
Stop-Process -Name dotnet -Force -ErrorAction SilentlyContinue
sc stop LYBT-API 2>$null
Start-Sleep -Seconds 2
Write-Host "stopped"

# --- 2. X appsettings.json (base config) ---
Write-Host "`n[2/5] Xbase config..." -ForegroundColor Yellow

$baseConfigPath = "$apiDir\appsettings.json"
$baseJson = [System.IO.File]::ReadAllText($baseConfigPath) | ConvertFrom-Json

# X JWT SecretKey
$baseJson.Jwt.SecretKey = "TFlCVF9TZWN1cmVfS2V5XzIwMjZfQUJDREVGMTIzNDU2Nzg="

# X - Trusted_Connection
$baseJson.ConnectionStrings.DefaultConnection = "Server=localhost;Database=LYBTDB;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true;Connection Timeout=30;Command Timeout=30;Max Pool Size=50;Min Pool Size=5;Pooling=true;Encrypt=false;Application Name=LYBT.WebAPI"

# X Https X
if ($baseJson.Kestrel.Endpoints.PSObject.Properties["Https"]) {
    $baseJson.Kestrel.Endpoints.PSObject.Properties.Remove("Https")
    Write-Host "  - deleted Https X"
}

# X
if ($baseJson.DefaultPasswords) {
    if (-not $baseJson.DefaultPasswords.SysAdminPassword) {
        $baseJson.DefaultPasswords | Add-Member -NotePropertyName "SysAdminPassword" -NotePropertyValue "Admin@Qt2026!"
    }
    if (-not $baseJson.DefaultPasswords.NewUserPassword) {
        $baseJson.DefaultPasswords | Add-Member -NotePropertyName "NewUserPassword" -NotePropertyValue "LYBT@2026!"
    }
}
if ($baseJson.SystemAdmin -and -not $baseJson.SystemAdmin.Email) {
    $baseJson.SystemAdmin | Add-Member -NotePropertyName "Email" -NotePropertyValue "admin@lybt.com"
}

# X
[System.IO.File]::WriteAllText($baseConfigPath, ($baseJson | ConvertTo-Json -Depth 20), [System.Text.Encoding]::UTF8)
Write-Host "  - appsettings.json fixed"

# --- 3. X appsettings.Production.json ---
Write-Host "`n[3/5] Xprod config..." -ForegroundColor Yellow

$prodConfigPath = "$apiDir\appsettings.Production.json"
$prodJson = [System.IO.File]::ReadAllText($prodConfigPath) | ConvertFrom-Json

# Kestrel X 0.0.0.0:5000
$prodJson.Kestrel.Endpoints.Http.Url = "http://0.0.0.0:5000"

# X Https X
if ($prodJson.Kestrel.Endpoints.PSObject.Properties["Https"]) {
    $prodJson.Kestrel.Endpoints.PSObject.Properties.Remove("Https")
    Write-Host "  - deleted Https X"
}

# JWT
$prodJson.Jwt.SecretKey = "TFlCVF9TZWN1cmVfS2V5XzIwMjZfQUJDREVGMTIzNDU2Nzg="

# X
$prodJson.ConnectionStrings.DefaultConnection = "Server=localhost;Database=LYBTDB;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true;Connection Timeout=30;Command Timeout=30;Max Pool Size=50;Min Pool Size=5;Pooling=true;Encrypt=false;Application Name=LYBT.WebAPI"

# X
[System.IO.File]::WriteAllText($prodConfigPath, ($prodJson | ConvertTo-Json -Depth 20), [System.Text.Encoding]::UTF8)
Write-Host "  - appsettings.Production.json fixed"
Write-Host "  - X: http://0.0.0.0:5000"

# --- 4. XconflictX .exe X ---
Write-Host "`n[4/5] clean conflict..." -ForegroundColor Yellow
if (Test-Path "$apiDir\LYBT.WebAPI.exe") {
    Remove-Item "$apiDir\LYBT.WebAPI.exe" -Force
    Write-Host "  - deleted LYBT.WebAPI.exe (Xavoid .dll conflict)"
} else {
    Write-Host "  - no conflict"
}

# --- 5. Xservice ---
Write-Host "`n[5/5] start app..." -ForegroundColor Yellow

# Xservice
sc stop LYBT-API 2>$null
sc delete LYBT-API 2>$null
Start-Sleep -Seconds 2

# Xservice (LocalSystem，X)
sc create LYBT-API binPath= "$apiDir\start-api.bat" start= auto DisplayName= "LYBT WebAPI Service"
sc description LYBT-API "LYBT WebAPI - Xservice"

# X（X 1053 X，X）
Write-Host "  starting (SCMX，X)..." -ForegroundColor Yellow
sc start LYBT-API 2>$null
Start-Sleep -Seconds 10

# X
$dotnetProc = Get-Process -Name dotnet -ErrorAction SilentlyContinue
if ($dotnetProc) {
    Write-Host "`n[OK] dotnet running (PID: $($dotnetProc.Id))" -ForegroundColor Green
} else {
    Write-Host "`n[WARN] not detected dotnet X，try manual start..." -ForegroundColor Yellow
    Start-Process -FilePath "C:\Program Files\dotnet\dotnet.exe" `
        -ArgumentList "$apiDir\LYBT.WebAPI.dll" `
        -WorkingDirectory $apiDir `
        -Environment @{"ASPNETCORE_ENVIRONMENT"="Production"} `
        -NoNewWindow
    Start-Sleep -Seconds 8
    $dotnetProc = Get-Process -Name dotnet -ErrorAction SilentlyContinue
    if ($dotnetProc) {
        Write-Host "[OK] manual start ok (PID: $($dotnetProc.Id))" -ForegroundColor Green
    }
}

# X
$portCheck = netstat -ano | findstr ":5000"
if ($portCheck) {
    Write-Host "`nport listen:" -ForegroundColor Green
    Write-Host $portCheck
} else {
    Write-Host "`n[WARN] 5000 Xnot listening，check log:" -ForegroundColor Yellow
    Get-Content "$apiDir\logs\bootstrap-*.log" -Tail 10 -ErrorAction SilentlyContinue
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host " done！X: http://$(hostname):5000/health" -ForegroundColor Cyan
Write-Host " local test: http://127.0.0.1:5000/health" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
