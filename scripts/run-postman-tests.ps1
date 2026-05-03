# Postman/Newman Integration Test Runner
# Usage: .\scripts\run-postman-tests.ps1
# Prerequisites: npm install -g newman newman-reporter-htmlextra

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$collection = Join-Path $projectRoot "tests\postman\local-api-tests.postman_collection.json"
$environment = Join-Path $projectRoot "tests\postman\local-api.environment.json"
$resultsDir = Join-Path $projectRoot "tests\postman\results"

if (!(Test-Path $collection)) {
    Write-Host "ERROR: Collection not found: $collection" -ForegroundColor Red
    exit 1
}

if (!(Test-Path $resultsDir)) {
    New-Item -ItemType Directory -Path $resultsDir | Out-Null
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$reportPath = Join-Path $resultsDir "report_$timestamp.html"

Write-Host "Running LocalWebAPI Postman tests..." -ForegroundColor Cyan
Write-Host "  Collection: $collection"
Write-Host "  Environment: $environment"
Write-Host ""

newman run $collection `
    -e $environment `
    --reporters cli,htmlextra `
    --reporter-htmlextra-export $reportPath `
    --timeout-request 10000 `
    --delay-request 100

$exitCode = $LASTEXITCODE

if ($exitCode -eq 0) {
    Write-Host "`nAll Postman tests passed!" -ForegroundColor Green
} else {
    Write-Host "`nSome Postman tests failed (exit code: $exitCode)" -ForegroundColor Red
}

Write-Host "Report: $reportPath"
exit $exitCode
