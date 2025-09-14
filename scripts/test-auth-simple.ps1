# Auth Login Test Script
param(
    [string]$BaseUrl = "http://localhost:8080",
    [string]$ReportDir = "_reports/2025-09/backend/acceptance-rerun"
)

$ErrorActionPreference = "Continue"
Write-Host "Auth Test: Starting login test..." -ForegroundColor Yellow

# Ensure report directory exists
$fullReportPath = Join-Path $PWD $ReportDir
if (!(Test-Path $fullReportPath)) {
    New-Item -Path $fullReportPath -ItemType Directory -Force | Out-Null
}

# Test data
$loginData = @{
    username = "sysadmin"
    password = "LybtAdmin2025@SecurePass!"
    rememberMe = $false
} | ConvertTo-Json

# Headers
$headers = @{
    "Content-Type" = "application/json"
    "Accept" = "application/json"
}

# Test Auth login
try {
    Write-Host "Testing login endpoint: $BaseUrl/api/v1/auth/login"
    
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/v1/auth/login" -Method POST -Body $loginData -Headers $headers -TimeoutSec 10
    
    if ($response -and $response.success) {
        $token = $response.data.token
        Write-Host "SUCCESS: Auth login successful, JWT token acquired" -ForegroundColor Green
        
        # Save auth result
        $authResult = @{
            success = $true
            timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
            endpoint = "$BaseUrl/api/v1/auth/login"
            username = "sysadmin"
            tokenPrefix = $token.Substring(0, [Math]::Min(20, $token.Length)) + "..."
            message = "Auth login successful, JWT token acquired"
        }
        
        $authResult | ConvertTo-Json -Depth 3 | Out-File -FilePath "$fullReportPath/auth.json" -Encoding UTF8
        
        # Save full token for subsequent tests
        $token | Out-File -FilePath "$fullReportPath/jwt-token.txt" -Encoding UTF8
        
        Write-Host "Auth info saved to: $ReportDir/auth.json" -ForegroundColor Cyan
        return $true
    } else {
        throw "Invalid response format or login failed"
    }
} catch {
    Write-Host "FAILED: Auth login failed: $($_.Exception.Message)" -ForegroundColor Red
    
    # Save failure result
    $authResult = @{
        success = $false
        timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        endpoint = "$BaseUrl/api/v1/auth/login"
        username = "sysadmin"
        error = $_.Exception.Message
        message = "Auth login failed"
    }
    
    $authResult | ConvertTo-Json -Depth 3 | Out-File -FilePath "$fullReportPath/auth.json" -Encoding UTF8
    return $false
}