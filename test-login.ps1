$body = @{
    Username = 'sysadmin'
    Password = 'LybtAdmin2025@SecurePass!'
} | ConvertTo-Json

Write-Host "Testing login with sysadmin..." -ForegroundColor Cyan
Write-Host "Request body: $body" -ForegroundColor Gray

try {
    $response = Invoke-WebRequest `
        -Uri 'http://localhost:5000/api/v1/auth/login' `
        -Method Post `
        -Body $body `
        -ContentType 'application/json'

    Write-Host "`nHTTP Status: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "`nResponse:" -ForegroundColor Yellow
    $response.Content | ConvertFrom-Json | ConvertTo-Json -Depth 10
}
catch {
    Write-Host "`nError: $_" -ForegroundColor Red
    if ($_.Exception.Response) {
        Write-Host "`nStatus Code: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "`nResponse Body: $responseBody" -ForegroundColor Red
    }
}
