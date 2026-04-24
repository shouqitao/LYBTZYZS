$ErrorActionPreference = "Continue"
$apiDir = "C:\Services\LYBT-API"

Write-Host "Stopping old processes..."
sc.exe stop LYBT-API 2>$null
sc.exe delete LYBT-API 2>$null
Stop-Process -Name dotnet -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

Write-Host "Starting LYBT WebAPI..."
$env:ASPNETCORE_ENVIRONMENT = "Production"
Start-Process -FilePath "C:\Program Files\dotnet\dotnet.exe" `
    -ArgumentList "$apiDir\LYBT.WebAPI.dll" `
    -WorkingDirectory $apiDir `
    -NoNewWindow

Start-Sleep -Seconds 8

$proc = Get-Process -Name dotnet -ErrorAction SilentlyContinue
if ($proc) {
    Write-Host "[OK] PID: $($proc.Id)" -ForegroundColor Green
} else {
    Write-Host "[FAIL] dotnet not running" -ForegroundColor Red
}

Write-Host "Port check:"
netstat -ano | findstr ":5000"
