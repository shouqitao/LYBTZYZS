# LocalWebAPI Postman Test Runner
# Usage: .\run-tests.ps1

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$collection = Join-Path $scriptDir "local-api-tests.postman_collection.json"
$environment = Join-Path $scriptDir "local-api.environment.json"
$resultsDir = Join-Path $scriptDir "results"

if (!(Test-Path $resultsDir)) {
    New-Item -ItemType Directory -Path $resultsDir | Out-Null
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$reportPath = Join-Path $resultsDir "report_$timestamp.html"

Write-Host "Running LocalWebAPI tests..." -ForegroundColor Cyan
Write-Host "Collection: $collection"
Write-Host "Environment: $environment"
Write-Host "Report: $reportPath"
Write-Host ""

newman run $collection `
    -e $environment `
    --reporters cli,htmlextra `
    --reporter-htmlextra-export $reportPath `
    --timeout-request 10000 `
    --delay-request 100

$exitCode = $LASTEXITCODE

if ($exitCode -eq 0) {
    Write-Host "`nAll tests passed!" -ForegroundColor Green
} else {
    Write-Host "`nSome tests failed (exit code: $exitCode)" -ForegroundColor Red
}

Write-Host "Report saved to: $reportPath"
exit $exitCode
