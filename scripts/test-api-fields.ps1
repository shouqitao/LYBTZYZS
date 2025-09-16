# 检查API响应字段结构
param(
    [string]$WebApiUrl = "http://localhost:8080"
)

try {
    # Login first
    $loginData = @{
        username = "sysadmin"
        password = "Admin@123456"
        rememberMe = $false
    } | ConvertTo-Json
    
    $loginHeaders = @{"Content-Type" = "application/json"}
    $loginResponse = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/auth/login" -Method POST -Body $loginData -Headers $loginHeaders -TimeoutSec 30
    
    $authHeaders = @{
        "Authorization" = "Bearer $($loginResponse.data.token)"
        "Content-Type" = "application/json"
    }
    
    # Check Users API
    Write-Host "=== Users API Response Structure ===" -ForegroundColor Cyan
    $usersResponse = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/users" -Method Get -Headers $authHeaders -TimeoutSec 10
    if ($usersResponse.success -and $usersResponse.data -and $usersResponse.data.items -and $usersResponse.data.items.Count -gt 0) {
        $user = $usersResponse.data.items[0]
        Write-Host "Available fields:" -ForegroundColor Yellow
        $user.PSObject.Properties.Name | Sort-Object | ForEach-Object { Write-Host "  - $_" }
        
        Write-Host "`nSample User Data:" -ForegroundColor Yellow
        $user | ConvertTo-Json -Depth 2
    } else {
        Write-Host "No users found or API error" -ForegroundColor Red
    }
    
    Write-Host ""
    
    # Check Patients API  
    Write-Host "=== Patients API Response Structure ===" -ForegroundColor Cyan
    $patientsResponse = Invoke-RestMethod -Uri "$WebApiUrl/api/v1/patients" -Method Get -Headers $authHeaders -TimeoutSec 10
    if ($patientsResponse.success -and $patientsResponse.data -and $patientsResponse.data.items -and $patientsResponse.data.items.Count -gt 0) {
        $patient = $patientsResponse.data.items[0]
        Write-Host "Available fields:" -ForegroundColor Yellow
        $patient.PSObject.Properties.Name | Sort-Object | ForEach-Object { Write-Host "  - $_" }
        
        Write-Host "`nSample Patient Data:" -ForegroundColor Yellow
        $patient | ConvertTo-Json -Depth 2
    } else {
        Write-Host "No patients found or API error" -ForegroundColor Red
    }
    
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}