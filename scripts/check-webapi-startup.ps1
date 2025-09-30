# WebAPI Startup Environment Check Script
# Issue: #827
# Date: 2025-09-30

param(
    [switch]$AutoClean,
    [switch]$CheckOnly
)

Write-Host "`n=============================================" -ForegroundColor Cyan
Write-Host "  LYBT WebAPI Startup Environment Check" -ForegroundColor White
Write-Host "=============================================" -ForegroundColor Cyan

# Check 1: dotnet processes
Write-Host "`n[1/4] Checking dotnet processes..." -ForegroundColor Yellow
$dotnetProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue

if ($dotnetProcesses) {
    Write-Host "  Found $($dotnetProcesses.Count) dotnet process(es)" -ForegroundColor Red
    $dotnetProcesses | Format-Table -Property Id, ProcessName, StartTime -AutoSize
    
    if (-not $CheckOnly) {
        if ($AutoClean) {
            $shouldClean = $true
        } else {
            $response = Read-Host "  Clean these processes? (Y/N)"
            $shouldClean = $response -eq "Y"
        }
        
        if ($shouldClean) {
            $dotnetProcesses | Stop-Process -Force
            Write-Host "  Cleaned successfully" -ForegroundColor Green
        }
    }
} else {
    Write-Host "  No dotnet processes found" -ForegroundColor Green
}

# Check 2: Port usage
Write-Host "`n[2/4] Checking port usage..." -ForegroundColor Yellow
$ports = @(5000, 5001)
foreach ($port in $ports) {
    $connection = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
    if ($connection) {
        $process = Get-Process -Id $connection.OwningProcess -ErrorAction SilentlyContinue
        Write-Host "  Port $port is in use by $($process.ProcessName) (PID: $($process.Id))" -ForegroundColor Red
    } else {
        Write-Host "  Port $port is available" -ForegroundColor Green
    }
}

# Check 3: SQL Server
Write-Host "`n[3/4] Checking SQL Server..." -ForegroundColor Yellow
$sqlServices = Get-Service -Name "MSSQL*" -ErrorAction SilentlyContinue
if ($sqlServices) {
    foreach ($service in $sqlServices) {
        if ($service.Status -eq "Running") {
            Write-Host "  $($service.DisplayName): Running" -ForegroundColor Green
        } else {
            Write-Host "  $($service.DisplayName): $($service.Status)" -ForegroundColor Yellow
        }
    }
} else {
    Write-Host "  SQL Server not found" -ForegroundColor Yellow
}

# Check 4: Project cache
Write-Host "`n[4/4] Checking project cache..." -ForegroundColor Yellow
$webApiPath = Join-Path $PSScriptRoot "..\src\Server\Services\LYBT.WebAPI"
$binPath = Join-Path $webApiPath "bin"
$objPath = Join-Path $webApiPath "obj"

if (Test-Path $binPath) {
    Write-Host "  bin directory exists" -ForegroundColor Cyan
}
if (Test-Path $objPath) {
    Write-Host "  obj directory exists" -ForegroundColor Cyan
}
if (-not (Test-Path $binPath) -and -not (Test-Path $objPath)) {
    Write-Host "  Cache directories clean" -ForegroundColor Green
}

# Summary
Write-Host "`n=============================================" -ForegroundColor Cyan
Write-Host "  Quick Clean Commands:" -ForegroundColor White
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "  Clean processes: Get-Process dotnet | Stop-Process -Force" -ForegroundColor Yellow
Write-Host "  Clean cache:     dotnet clean .\src\Server\Services\LYBT.WebAPI" -ForegroundColor Yellow
Write-Host "  Rebuild:         dotnet build .\src\Server\Services\LYBT.WebAPI -c Debug" -ForegroundColor Yellow
Write-Host "`n"