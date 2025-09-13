# Health Check Poller Script
# Usage: .\health.ps1 -BaseUrl http://localhost:8080
param(
    [Parameter(Mandatory=$false)]
    [string]$BaseUrl = "http://localhost:8080",
    
    [Parameter(Mandatory=$false)]
    [int]$MaxAttempts = 30,
    
    [Parameter(Mandatory=$false)]
    [int]$IntervalSeconds = 2
)

$OutputFile = "_reports/2025-09/webapi/run-fix/health.json"
$HealthUrl = "$BaseUrl/api/v1/health"

Write-Host "🏥 Starting health check polling..." -ForegroundColor Green
Write-Host "🎯 Target: $HealthUrl" -ForegroundColor Cyan
Write-Host "📊 Max attempts: $MaxAttempts, Interval: ${IntervalSeconds}s" -ForegroundColor Cyan
Write-Host "📝 Output: $OutputFile" -ForegroundColor Yellow

# Ensure output directory exists
New-Item -ItemType Directory -Force -Path (Split-Path $OutputFile -Parent) | Out-Null

$results = @()
$attempt = 1

while ($attempt -le $MaxAttempts) {
    $timestamp = Get-Date -Format "yyyy-MM-ddTHH:mm:ss.fffZ"
    
    try {
        Write-Host "[$attempt/$MaxAttempts] Checking health..." -NoNewline
        
        $response = Invoke-RestMethod -Uri $HealthUrl -Method GET -TimeoutSec 5
        $statusCode = 200
        $status = "Healthy"
        $message = "Success"
        
        Write-Host " ✅ Healthy" -ForegroundColor Green
        
        $result = @{
            attempt = $attempt
            timestamp = $timestamp
            url = $HealthUrl
            statusCode = $statusCode
            status = $status
            message = $message
            response = $response
        }
        
        $results += $result
        
        # If healthy, we can exit early
        Write-Host "🎉 WebAPI is healthy! Health check passed." -ForegroundColor Green
        break
    }
    catch {
        $statusCode = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 }
        $status = "Unhealthy"
        $message = $_.Exception.Message
        
        Write-Host " ❌ Failed: $message" -ForegroundColor Red
        
        $result = @{
            attempt = $attempt
            timestamp = $timestamp
            url = $HealthUrl
            statusCode = $statusCode
            status = $status
            message = $message
            response = $null
        }
        
        $results += $result
    }
    
    $attempt++
    if ($attempt -le $MaxAttempts) {
        Start-Sleep -Seconds $IntervalSeconds
    }
}

# Save results to JSON file
$outputData = @{
    summary = @{
        totalAttempts = $attempt - 1
        maxAttempts = $MaxAttempts
        healthUrl = $HealthUrl
        finalStatus = if ($results[-1].status -eq "Healthy") { "SUCCESS" } else { "FAILED" }
        timestamp = Get-Date -Format "yyyy-MM-ddTHH:mm:ss.fffZ"
    }
    results = $results
}

$outputData | ConvertTo-Json -Depth 10 | Out-File -FilePath $OutputFile -Encoding UTF8

if ($results[-1].status -eq "Healthy") {
    Write-Host "✅ Health check completed successfully!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "❌ Health check failed after $MaxAttempts attempts" -ForegroundColor Red
    exit 1
}