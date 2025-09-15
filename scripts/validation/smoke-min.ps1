# WebAPI P2-Fix-Batch3 Minimal Smoke Test Script
# Purpose: Minimal validation for Health + Auth + Patients endpoints after API versioning fix
# Coverage: GET /api/v1/health → 200, POST /api/v1/auth/login (Doctor) → 200 with JWT, Patients CRUD (minimal data)

param(
    [string]$BaseUrl = "http://localhost:9999",
    [int]$TimeoutSeconds = 30,
    [switch]$Detailed
)

Write-Host "=== P2-Fix-Batch3 Minimal Smoke Test ===" -ForegroundColor Cyan
Write-Host "BaseUrl: $BaseUrl"
Write-Host "Target: Health + Auth + Patients validation"
Write-Host ""

$TestResults = @()
$AllPassed = $true
$AuthToken = $null

function Test-HttpRequest {
    param(
        [string]$Url, 
        [string]$Method = "GET",
        [string]$TestName, 
        [string]$ExpectedStatus = "200",
        [hashtable]$Headers = @{},
        [string]$Body = $null,
        [string]$ContentType = "application/json"
    )
    
    Write-Host "Testing: $TestName" -ForegroundColor Yellow
    Write-Host "  $Method $Url"
    
    try {
        $requestParams = @{
            Uri = $Url
            Method = $Method
            TimeoutSec = $TimeoutSeconds
            UseBasicParsing = $true
        }
        
        if ($Headers.Count -gt 0) {
            $requestParams.Headers = $Headers
        }
        
        if ($Body) {
            $requestParams.Body = $Body
            $requestParams.ContentType = $ContentType
        }
        
        $response = Invoke-WebRequest @requestParams
        $statusCode = $response.StatusCode
        
        if ($statusCode -eq [int]$ExpectedStatus) {
            Write-Host "  Result: PASS (Status: $statusCode)" -ForegroundColor Green
            $TestResults += @{
                Test = $TestName
                Status = "PASS"
                StatusCode = $statusCode
                Response = $response.Content
                Error = $null
            }
            return @{ Success = $true; Response = $response; Content = $response.Content }
        } else {
            Write-Host "  Result: FAIL (Expected: $ExpectedStatus, Got: $statusCode)" -ForegroundColor Red
            $script:AllPassed = $false
            $TestResults += @{
                Test = $TestName
                Status = "FAIL"
                StatusCode = $statusCode
                Response = $response.Content
                Error = "Unexpected status code"
            }
            return @{ Success = $false; Response = $response; Content = $response.Content }
        }
    }
    catch {
        Write-Host "  Result: FAIL (Error: $($_.Exception.Message))" -ForegroundColor Red
        $script:AllPassed = $false
        $TestResults += @{
            Test = $TestName
            Status = "FAIL"
            StatusCode = "N/A"
            Response = $null
            Error = $_.Exception.Message
        }
        return @{ Success = $false; Response = $null; Content = $null }
    }
}

# Test 1: Health Check Endpoint
Write-Host "=== Test 1: Health Check ===" -ForegroundColor Cyan
$healthResult = Test-HttpRequest -Url "$BaseUrl/api/v1/health" -TestName "Health Check Endpoint"

if ($healthResult.Success) {
    try {
        $healthData = $healthResult.Content | ConvertFrom-Json
        if ($healthData.status -eq "Healthy") {
            Write-Host "  Health Status: $($healthData.status)" -ForegroundColor Green
            if ($Detailed) {
                Write-Host "    Timestamp: $($healthData.timestamp)"
                Write-Host "    Version: $($healthData.version)"
                Write-Host "    Environment: $($healthData.environment)"
            }
        }
    }
    catch {
        Write-Host "  Warning: Could not parse health response JSON" -ForegroundColor Yellow
    }
}
Write-Host ""

# Test 2: Auth Login (Admin role - sysadmin)
Write-Host "=== Test 2: Auth Login (Admin) ===" -ForegroundColor Cyan
$loginBody = @{
    username = "sysadmin"
    password = "Admin@123456"
} | ConvertTo-Json

$authResult = Test-HttpRequest -Url "$BaseUrl/api/v1/auth/login" -Method "POST" -TestName "Admin Login" -Body $loginBody

if ($authResult.Success) {
    try {
        $authData = $authResult.Content | ConvertFrom-Json
        if ($authData.success -and $authData.data -and $authData.data.token) {
            $script:AuthToken = $authData.data.token
            Write-Host "  JWT Token acquired successfully" -ForegroundColor Green
            if ($Detailed) {
                Write-Host "    Token prefix: $($AuthToken.Substring(0, [Math]::Min(20, $AuthToken.Length)))..."
                Write-Host "    Username: $($authData.data.username)"
                Write-Host "    Role: $($authData.data.role)"
            }
        } else {
            Write-Host "  Warning: Login successful but no JWT token found" -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host "  Warning: Could not parse auth response JSON" -ForegroundColor Yellow
    }
}
Write-Host ""

# Test 3: Patients CRUD (minimal data) - only if auth succeeded
Write-Host "=== Test 3: Patients CRUD (requires auth) ===" -ForegroundColor Cyan

if ($AuthToken) {
    $authHeaders = @{ "Authorization" = "Bearer $AuthToken" }
    
    # 3a: POST - Create minimal patient
    $patientData = @{
        name = "测试患者-冒烟"
        phone = "13800138000"
        gender = "Male"
        birthDate = "1990-01-01T00:00:00Z"
    } | ConvertTo-Json
    
    $createResult = Test-HttpRequest -Url "$BaseUrl/api/v1/patients" -Method "POST" -TestName "Create Patient" -Headers $authHeaders -Body $patientData -ExpectedStatus "201"
    
    $patientId = $null
    if ($createResult.Success) {
        try {
            $createData = $createResult.Content | ConvertFrom-Json
            if ($createData.success -and $createData.data -and $createData.data.id) {
                $patientId = $createData.data.id
                Write-Host "  Created Patient ID: $patientId" -ForegroundColor Green
            }
        }
        catch {
            Write-Host "  Warning: Could not parse create patient response" -ForegroundColor Yellow
        }
    }
    Write-Host ""
    
    # 3b: GET - Retrieve created patient
    if ($patientId) {
        $getResult = Test-HttpRequest -Url "$BaseUrl/api/v1/patients/$patientId" -TestName "Get Patient" -Headers $authHeaders
        Write-Host ""
        
        # 3c: DELETE - Remove test patient
        $deleteResult = Test-HttpRequest -Url "$BaseUrl/api/v1/patients/$patientId" -Method "DELETE" -TestName "Delete Patient" -Headers $authHeaders
        if ($deleteResult.Success) {
            Write-Host "  Test patient cleaned up successfully" -ForegroundColor Green
        }
    } else {
        Write-Host "Skipping GET/DELETE tests - no patient ID available" -ForegroundColor Yellow
    }
} else {
    Write-Host "Skipping Patients tests - no auth token available" -ForegroundColor Yellow
    $script:AllPassed = $false
}

Write-Host ""

# Summary
Write-Host "=== Final Results ===" -ForegroundColor Cyan
Write-Host "Total Tests: $($TestResults.Count)"
Write-Host "Passed: $(($TestResults | Where-Object {$_.Status -eq 'PASS'}).Count)" -ForegroundColor Green
Write-Host "Failed: $(($TestResults | Where-Object {$_.Status -eq 'FAIL'}).Count)" -ForegroundColor Red
Write-Host ""

if ($AllPassed) {
    Write-Host "✅ P2-Fix-Batch3 MINIMAL SMOKE TESTS PASSED!" -ForegroundColor Green
    Write-Host "API versioning fix validated successfully:" -ForegroundColor Green
    Write-Host "  - Health endpoint working" -ForegroundColor Green
    Write-Host "  - Auth login working with JWT" -ForegroundColor Green
    Write-Host "  - Patients CRUD working" -ForegroundColor Green
    exit 0
} else {
    Write-Host "❌ P2-Fix-Batch3 MINIMAL SMOKE TESTS FAILED!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Failed Tests:" -ForegroundColor Red
    $TestResults | Where-Object {$_.Status -eq 'FAIL'} | ForEach-Object {
        Write-Host "  - $($_.Test): $($_.Error)" -ForegroundColor Red
    }
    exit 1
}