# WebAPI Runner Script
# Usage: .\run-webapi.ps1 -Port 8080 -Env Development
param(
    [Parameter(Mandatory=$false)]
    [int]$Port = 8080,
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("Development", "Production")]
    [string]$Env = "Development"
)

$LogFile = "_reports/2025-09/webapi/run-fix/webapi-run.log"
$ProjectPath = "src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj"

Write-Host "🚀 Starting WebAPI on port $Port with environment $Env" -ForegroundColor Green
Write-Host "📝 Logging to: $LogFile" -ForegroundColor Yellow

# Ensure log directory exists
New-Item -ItemType Directory -Force -Path (Split-Path $LogFile -Parent) | Out-Null

# Set environment variables
$env:ASPNETCORE_ENVIRONMENT = $Env
$env:ASPNETCORE_URLS = "http://localhost:$Port"

Write-Host "🔧 Environment: $env:ASPNETCORE_ENVIRONMENT" -ForegroundColor Cyan
Write-Host "🌐 URLs: $env:ASPNETCORE_URLS" -ForegroundColor Cyan

try {
    # Start the WebAPI and capture output
    Write-Output "=== WebAPI Run Log - Started at $(Get-Date) ===" | Out-File -FilePath $LogFile -Encoding UTF8
    Write-Output "Environment: $Env" | Out-File -FilePath $LogFile -Append -Encoding UTF8
    Write-Output "Port: $Port" | Out-File -FilePath $LogFile -Append -Encoding UTF8
    Write-Output "URLs: $env:ASPNETCORE_URLS" | Out-File -FilePath $LogFile -Append -Encoding UTF8
    Write-Output "" | Out-File -FilePath $LogFile -Append -Encoding UTF8
    
    dotnet run --project $ProjectPath --verbosity minimal *>&1 | Tee-Object -FilePath $LogFile -Append
}
catch {
    Write-Host "❌ Error starting WebAPI: $($_.Exception.Message)" -ForegroundColor Red
    Write-Output "ERROR: $($_.Exception.Message)" | Out-File -FilePath $LogFile -Append -Encoding UTF8
    exit 1
}