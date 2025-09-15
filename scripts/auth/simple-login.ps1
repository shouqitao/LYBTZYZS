# Simple Auth Login Script
param(
    [string]$BaseUrl = "http://localhost:8080",
    [string]$Username = "sysadmin", 
    [string]$Password = "Admin@123456",
    [string]$OutputFile = ""
)

Write-Host "=== Auth Login Test ===" -ForegroundColor Cyan
Write-Host "Endpoint: POST /api/v1/auth/login"
Write-Host "User: $Username"
Write-Host ""

$loginData = @{
    username = $Username
    password = $Password
} | ConvertTo-Json

try {
    Write-Host "Sending login request..." -ForegroundColor Yellow
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/v1/auth/login" -Method POST -Body $loginData -ContentType "application/json" -TimeoutSec 15
    
    if ($response.success -and $response.data -and $response.data.token) {
        Write-Host "Login SUCCESS!" -ForegroundColor Green
        Write-Host "   Username: $($response.data.username)"
        Write-Host "   Role: $($response.data.role)"
        Write-Host "   Token Length: $($response.data.token.Length) chars"
        Write-Host "   Token Prefix: $($response.data.token.Substring(0, [Math]::Min(20, $response.data.token.Length)))..."
        
        $authResult = @{
            timestamp = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss.fff")
            success = $true
            username = $response.data.username
            role = $response.data.role
            tokenLength = $response.data.token.Length
            tokenPrefix = $response.data.token.Substring(0, [Math]::Min(20, $response.data.token.Length))
            expiryTime = $response.data.expiryTime
            fullToken = $response.data.token
            fullResponse = $response
        }
        
        if ($OutputFile -ne "") {
            $authResult | ConvertTo-Json -Depth 4 | Out-File -FilePath $OutputFile -Encoding UTF8
            Write-Host "Auth result saved to: $OutputFile" -ForegroundColor Cyan
        }
        
        Write-Host "AUTH TOKEN ACQUIRED SUCCESSFULLY" -ForegroundColor Green
        exit 0
    } else {
        Write-Host "Login FAILED - Invalid response format" -ForegroundColor Red
        Write-Host "Response: $($response | ConvertTo-Json)" -ForegroundColor Gray
        exit 1
    }
} catch {
    Write-Host "Login request FAILED: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}