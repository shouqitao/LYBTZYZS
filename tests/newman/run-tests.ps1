# LYBT WebAPI 集成测试 (PowerShell)
param([string]$BaseUrl = "http://60.190.215.86:5000")

$pass = 0; $fail = 0; $token = ""; $userId = ""

function Test-Api($desc, $method, $url, $body, $expectStatus) {
    $headers = @{ "Content-Type" = "application/json" }
    if ($script:token) { $headers["Authorization"] = "Bearer $($script:token)" }
    
    try {
        $resp = switch ($method) {
            "GET"    { Invoke-RestMethod -Uri $url -Method GET -Headers $headers -ErrorAction Stop }
            "POST"   { Invoke-RestMethod -Uri $url -Method POST -Headers $headers -Body $body -ErrorAction Stop }
            "DELETE" { Invoke-RestMethod -Uri $url -Method DELETE -Headers $headers -ErrorAction Stop }
        }
        Write-Host "  PASS $desc" -ForegroundColor Green
        $script:pass++
        return $resp
    } catch {
        $code = $_.Exception.Response.StatusCode.value__
        Write-Host "  FAIL $desc (HTTP $code)" -ForegroundColor Red
        $script:fail++
        return $null
    }
}

Write-Host "=== LYBT WebAPI Integration Test ===" -ForegroundColor Cyan
Write-Host "Target: $BaseUrl" -ForegroundColor Cyan

# 1. Health
Write-Host "`n[1/9] Health"
Test-Api "GET /health" GET "$BaseUrl/health" "" 200 | Out-Null
Test-Api "GET /health/ping" GET "$BaseUrl/health/ping" "" 200 | Out-Null

# 2. Auth
Write-Host "`n[2/9] Auth"
$body = '{"username":"sysadmin","password":"SysAdmin@2026a!"}'
$loginResp = Test-Api "POST /auth/login" POST "$BaseUrl/api/v1/auth/login" $body 200
if ($loginResp -and $loginResp.data.token) {
    $token = $loginResp.data.token
    $userId = $loginResp.data.user.id
    Write-Host "  Token acquired" -ForegroundColor Gray
} else {
    Write-Host "  ABORT: no token" -ForegroundColor Red
    exit 1
}
Test-Api "GET /auth/validate" GET "$BaseUrl/api/v1/auth/validate" "" 200 | Out-Null

# 3. Users
Write-Host "`n[3/9] Users"
Test-Api "GET /users" GET "$BaseUrl/api/v1/users?page=1&pageSize=5" "" 200 | Out-Null
Test-Api "GET /users/current" GET "$BaseUrl/api/v1/users/current" "" 200 | Out-Null

# 4. Patients
Write-Host "`n[4/9] Patients"
$pb = '{"name":"TestPatient","gender":1,"birthDate":"1990-01-01","phoneNumber":"13900239001","idNumber":"110101199001028888"}'
$patResp = Test-Api "POST /patients" POST "$BaseUrl/api/v1/patients" $pb 200
$patId = if ($patResp) { $patResp.data.id } else { "" }
Test-Api "GET /patients" GET "$BaseUrl/api/v1/patients?page=1&pageSize=5" "" 200 | Out-Null
if ($patId) { Test-Api "GET /patients/$patId" GET "$BaseUrl/api/v1/patients/$patId" "" 200 | Out-Null }
if ($patId) { Test-Api "GET /patients/$patId/check-ref" GET "$BaseUrl/api/v1/patients/$patId/check-reference" "" 200 | Out-Null }

# 5. Herbs
Write-Host "`n[5/9] Herbs"
$hb = '{"name":"TestHerb-LianQiao","category":"ClearHeat","effect":"ClearHeat","price":25.0}'
$herbResp = Test-Api "POST /herbs" POST "$BaseUrl/api/v1/herbs" $hb 201
$herbId = if ($herbResp) { $herbResp.data.id } else { "" }
Test-Api "GET /herbs" GET "$BaseUrl/api/v1/herbs?page=1&pageSize=5" "" 200 | Out-Null
if ($herbId) { Test-Api "GET /herbs/$herbId/check-ref" GET "$BaseUrl/api/v1/herbs/$herbId/check-reference" "" 200 | Out-Null }
if ($herbId) { Test-Api "POST /herbs/$herbId/toggle" POST "$BaseUrl/api/v1/herbs/$herbId/toggle-status" "" 200 | Out-Null }

# 6. Registrations
Write-Host "`n[6/9] Registrations"
$rb = @{patientId=$patId;patientName="TestPatient";doctorId=$userId;doctorName="Admin";source="Doctor";remark="test"} | ConvertTo-Json
$regResp = Test-Api "POST /registrations" POST "$BaseUrl/api/v1/registrations" $rb 201
Test-Api "GET /registrations" GET "$BaseUrl/api/v1/registrations?page=1&pageSize=5" "" 200 | Out-Null
Test-Api "GET /registrations/queue" GET "$BaseUrl/api/v1/registrations/queue" "" 200 | Out-Null

# 7. Reports
Write-Host "`n[7/9] Reports"
Test-Api "GET /reports/daily/income" GET "$BaseUrl/api/v1/reports/daily/income" "" 200 | Out-Null
Test-Api "GET /reports/daily/consultations" GET "$BaseUrl/api/v1/reports/daily/consultations" "" 200 | Out-Null
Test-Api "GET /reports/daily/herbs" GET "$BaseUrl/api/v1/reports/daily/herbs" "" 200 | Out-Null

# 8. Configuration
Write-Host "`n[8/9] Configuration"
Test-Api "GET /configuration" GET "$BaseUrl/api/v1/configuration" "" 200 | Out-Null

# 9. Cleanup
Write-Host "`n[9/9] Cleanup"
if ($herbId) { Test-Api "DELETE /herbs/$herbId" DELETE "$BaseUrl/api/v1/herbs/$herbId" "" 200 | Out-Null }
if ($patId) { Test-Api "DELETE /patients/$patId" DELETE "$BaseUrl/api/v1/patients/$patId" "" 200 | Out-Null }

# Summary
$total = $pass + $fail
Write-Host "`n=== Result ===" -ForegroundColor Cyan
Write-Host "Pass: $pass / Fail: $fail / Total: $total" -ForegroundColor Cyan
if ($fail -eq 0) { Write-Host "ALL TESTS PASSED" -ForegroundColor Green } else { Write-Host "SOME TESTS FAILED" -ForegroundColor Red }
