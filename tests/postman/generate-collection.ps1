#!/usr/bin/env pwsh
# Generate Postman Collection v2.1 JSON for LocalWebAPI
# Output: local-api-tests.postman_collection.json

$ErrorActionPreference = 'Stop'
$outPath = Join-Path $PSScriptRoot 'local-api-tests.postman_collection.json'

# --- Helper Functions ---
function New-Uuid { [guid]::NewGuid().ToString() }

function New-Event($listen, $execLines) {
    @{
        listen = $listen
        script = @{
            exec = $execLines
            type = 'text/javascript'
        }
    }
}

function New-Request($name, $method, $url, $body, $headers, $events, $description) {
    $req = @{
        name = $name
        request = @{
            method = $method
            header = @()
            url = @{
                raw = $url
                protocol = 'http'
                host = @('127','0','0','1')
                port = '5290'
                path = @()
            }
        }
        response = @()
    }
    # Parse URL path - extract from template URL
    $urlPath = $url -replace '\{\{base_url\}\}', '' -replace '\?.*$', ''
    $req.request.url.path = $urlPath.TrimStart('/').Split('/')
    if ($headers -and $headers.Count -gt 0) {
        $req.request.header = $headers
    }
    if ($body) {
        $req.request.body = @{
            mode = 'raw'
            raw = $body
            options = @{ raw = @{ language = 'json' } }
        }
    }
    if ($events -and $events.Count -gt 0) {
        $req.event = $events
    }
    if ($description) {
        $req.request.description = $description
    }
    return $req
}

function New-Folder($name, $items, $description) {
    $folder = @{
        name = $name
        item = $items
    }
    if ($description) { $folder.description = $description }
    return $folder
}

# --- Pre-request script (collection-level) to auto-set auth header ---
$collectionPreRequest = New-Event 'pre-request' @(
    'if (!pm.request.headers.has("Authorization") && pm.environment.get("auth_token")) {'
    '    pm.request.headers.add({key: "Authorization", value: "Bearer " + pm.environment.get("auth_token")});'
    '}'
)

# ============================================================
# 00-SETUP
# ============================================================
$setupItems = @()

# Auto-Login
$setupItems += New-Request "Auto-Login as Admin" "POST" "{{base_url}}/api/auth/auto-login" `
    '{ "userName": "admin" }' $null @(
        (New-Event "test" @(
            'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
            'pm.test("Has token", function () { var j = pm.response.json(); pm.expect(j).to.have.property("Token"); });',
            'pm.test("Save env vars", function () { var j = pm.response.json(); pm.environment.set("auth_token", j.Token); pm.environment.set("admin_user_id", j.UserId); });'
        ))
    ) "Login as admin, save token and userId to environment"

# Create Patients
foreach ($i in 1..3) {
    $names = @("_test_患者张三", "_test_患者李四", "_test_患者王五")
    $ids = @("110101199001150001", "110101199001150002", "110101199001150003")
    $envVars = @("test_patient_id", "test_patient_id_2", "test_patient_id_3")
    $body = "{`n  `"name`": `"$($names[$i-1])`",`n  `"gender`": `"男`",`n  `"birthDate`": `"1990-01-15`",`n  `"phoneNumber`": `"1380013800$i`",`n  `"idNumber`": `"$($ids[$i-1])`",`n  `"address`": `"北京市东城区测试街$i号`"`n}"
    $setupItems += New-Request "Create Test Patient $i" "POST" "{{base_url}}/api/patients" `
        $body @(
            @{ key = 'Authorization'; value = 'Bearer {{auth_token}}' }
        ) @(
            (New-Event "test" @(
                'pm.test("Status 201", function () { pm.response.to.have.status(201); });',
                "pm.test(`"Save patient id`", function () { var j = pm.response.json(); pm.environment.set(`"$($envVars[$i-1])`", j.id); });"
            ))
        )
}

# Create Herbs
foreach ($i in 1..3) {
    $herbNames = @("_test_甘草", "_test_黄芪", "_test_当归")
    $codes = @("GC", "HQ", "DG")
    $cats = @("补气药", "补气药", "补血药")
    $envVars = @("test_herb_id", "test_herb_id_2", "test_herb_id_3")
    $body = "{`n  `"name`": `"$($herbNames[$i-1])`",`n  `"pinYinCode`": `"$($codes[$i-1])`",`n  `"category`": `"$($cats[$i-1])`",`n  `"properties`": `"平`",`n  `"origin`": `"内蒙古`",`n  `"spec`": `"统货`",`n  `"unit`": `"克`",`n  `"price`": 0.5,`n  `"costPrice`": 0.3,`n  `"effect`": `"益气补中`",`n  `"usage`": `"煎服`",`n  `"status`": 1`n}"
    $setupItems += New-Request "Create Test Herb $i" "POST" "{{base_url}}/api/herbs" `
        $body @(
            @{ key = 'Authorization'; value = 'Bearer {{auth_token}}' }
        ) @(
            (New-Event "test" @(
                'pm.test("Status 201", function () { pm.response.to.have.status(201); });',
                "pm.test(`"Save herb id`", function () { var j = pm.response.json(); pm.environment.set(`"$($envVars[$i-1])`", j.id); });"
            ))
        )
}

# Create Formulas
foreach ($i in 1..2) {
    $fNames = @("_test_四君子汤", "_test_四物汤")
    $fCodes = @("SJZT", "SWT")
    $fCats = @("补气方", "补血方")
    $envVars = @("test_formula_id", "test_formula_id_2")
    $body = "{`n  `"name`": `"$($fNames[$i-1])`",`n  `"pinYinCode`": `"$($fCodes[$i-1])`",`n  `"category`": `"$($fCats[$i-1])`",`n  `"description`": `"测试验方`",`n  `"herbs`": []`n}"
    $setupItems += New-Request "Create Test Formula $i" "POST" "{{base_url}}/api/formulas" `
        $body @(
            @{ key = 'Authorization'; value = 'Bearer {{auth_token}}' }
        ) @(
            (New-Event "test" @(
                'pm.test("Status 201", function () { pm.response.to.have.status(201); });',
                "pm.test(`"Save formula id`", function () { var j = pm.response.json(); pm.environment.set(`"$($envVars[$i-1])`", j.id); });"
            ))
        )
}

# Create Registrations
foreach ($i in 1..2) {
    $envVars = @("test_registration_id", "test_registration_id_2")
    $patVars = @("test_patient_id", "test_patient_id_2")
    $patNames = @("_test_患者张三", "_test_患者李四")
    $body = "{`n  `"patientId`": `"{{" + $patVars[$i-1] + "}}`",`n  `"patientName`": `"$($patNames[$i-1])`",`n  `"status`": 0`n}"
    $setupItems += New-Request "Create Test Registration $i" "POST" "{{base_url}}/api/registrations" `
        $body @(
            @{ key = 'Authorization'; value = 'Bearer {{auth_token}}' }
        ) @(
            (New-Event "test" @(
                'pm.test("Status 201", function () { pm.response.to.have.status(201); });',
                "pm.test(`"Save reg id`", function () { var j = pm.response.json(); pm.environment.set(`"$($envVars[$i-1])`", j.id); });"
            ))
        )
}

# Create MedicalCases
foreach ($i in 1..2) {
    $envVars = @("test_medical_case_id", "test_medical_case_id_2")
    $patVars = @("test_patient_id", "test_patient_id_2")
    $body = "{`n  `"patientId`": `"{{" + $patVars[$i-1] + "}}`",`n  `"userId`": `"{{admin_user_id}}`",`n  `"remark`": `"_test_测试医案$i`"`n}"
    $setupItems += New-Request "Create Test MedicalCase $i" "POST" "{{base_url}}/api/medicalcases" `
        $body @(
            @{ key = 'Authorization'; value = 'Bearer {{auth_token}}' }
        ) @(
            (New-Event "test" @(
                'pm.test("Status 201", function () { pm.response.to.have.status(201); });',
                "pm.test(`"Save mc id`", function () { var j = pm.response.json(); pm.environment.set(`"$($envVars[$i-1])`", j.id); });"
            ))
        )
}

$setupFolder = New-Folder "00-Setup" $setupItems "Login and create seed test data"

# ============================================================
# 01-HEALTH
# ============================================================
$healthItems = @()
$healthItems += New-Request "GET /api/health" "GET" "{{base_url}}/api/health" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Has status property", function () { var j = pm.response.json(); pm.expect(j).to.have.property("status"); });'
    ))
)
$healthItems += New-Request "GET /api/health/ping" "GET" "{{base_url}}/api/health/ping" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Status is ok", function () { var j = pm.response.json(); pm.expect(j.status).to.eql("ok"); });'
    ))
)
$healthItems += New-Request "GET /api/health/details" "GET" "{{base_url}}/api/health/details" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Has database.connected", function () { var j = pm.response.json(); pm.expect(j.database).to.have.property("connected"); });',
        'pm.test("Has version", function () { var j = pm.response.json(); pm.expect(j).to.have.property("version"); });'
    ))
)
$healthFolder = New-Folder "01-Health" $healthItems "Health check endpoints"

# ============================================================
# 02-AUTH
# ============================================================
$authItems = @()

# POST auto-login happy
$authItems += New-Request "POST /api/auth/auto-login - Happy" "POST" "{{base_url}}/api/auth/auto-login" `
    '{ "userName": "admin" }' $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Has token", function () { pm.expect(pm.response.json()).to.have.property("Token"); });',
        'pm.test("Has userId", function () { pm.expect(pm.response.json()).to.have.property("UserId"); });',
        'pm.test("Has username", function () { pm.expect(pm.response.json()).to.have.property("Username"); });',
        'pm.test("Has role", function () { pm.expect(pm.response.json()).to.have.property("Role"); });'
    ))
)

# POST auto-login invalid user
$authItems += New-Request "POST /api/auth/auto-login - Invalid User" "POST" "{{base_url}}/api/auth/auto-login" `
    '{ "userName": "nonexistent_user_12345" }' $null @(
    (New-Event "test" @(
        'pm.test("Status 401", function () { pm.response.to.have.status(401); });'
    ))
)

# POST login happy
$authItems += New-Request "POST /api/auth/login - Happy" "POST" "{{base_url}}/api/auth/login" `
    '{ "userName": "admin", "password": "admin123" }' $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Has token", function () { pm.expect(pm.response.json()).to.have.property("Token"); });'
    ))
)

# POST login wrong password
$authItems += New-Request "POST /api/auth/login - Wrong Password" "POST" "{{base_url}}/api/auth/login" `
    '{ "userName": "admin", "password": "wrong_password" }' $null @(
    (New-Event "test" @(
        'pm.test("Status 401", function () { pm.response.to.have.status(401); });'
    ))
)

# POST refresh happy
$authItems += New-Request "POST /api/auth/refresh - Happy" "POST" "{{base_url}}/api/auth/refresh" `
    '{ "userName": "admin" }' @(
    @{ key = 'Authorization'; value = 'Bearer {{auth_token}}' }
) @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Has token", function () { pm.expect(pm.response.json()).to.have.property("Token"); });'
    ))
)

# POST refresh no token
$authItems += New-Request "POST /api/auth/refresh - No Token" "POST" "{{base_url}}/api/auth/refresh" `
    '{ "userName": "admin" }' @(
    @{ key = 'Authorization'; value = '' }
) @(
    (New-Event "test" @(
        'pm.test("Status 401", function () { pm.response.to.have.status(401); });'
    ))
)

# GET validate happy
$authItems += New-Request "GET /api/auth/validate - Happy" "GET" "{{base_url}}/api/auth/validate" `
    $null @(
    @{ key = 'Authorization'; value = 'Bearer {{auth_token}}' }
) @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("isValid is true", function () { pm.expect(pm.response.json().isValid).to.eql(true); });',
        'pm.test("Has userId", function () { pm.expect(pm.response.json()).to.have.property("userId"); });'
    ))
)

# GET validate no token
$authItems += New-Request "GET /api/auth/validate - No Token" "GET" "{{base_url}}/api/auth/validate" `
    $null @(
    @{ key = 'Authorization'; value = '' }
) @(
    (New-Event "test" @(
        'pm.test("Status 401", function () { pm.response.to.have.status(401); });'
    ))
)

# POST logout happy
$authItems += New-Request "POST /api/auth/logout - Happy" "POST" "{{base_url}}/api/auth/logout" `
    '{ "userName": "admin" }' $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Success is true", function () { pm.expect(pm.response.json().success).to.eql(true); });'
    ))
)

$authFolder = New-Folder "02-Auth" $authItems "Authentication endpoints"

# ============================================================
# 03-USERS
# ============================================================
$userItems = @()

# GET list
$userItems += New-Request "GET /api/users - List" "GET" "{{base_url}}/api/users" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Response is array", function () { pm.expect(pm.response.json()).to.be.an("array"); });',
        'pm.test("Length > 0", function () { pm.expect(pm.response.json().length).to.be.above(0); });'
    ))
)

# GET by ID
$userItems += New-Request "GET /api/users/{id} - By ID" "GET" "{{base_url}}/api/users/{{admin_user_id}}" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Has id", function () { pm.expect(pm.response.json()).to.have.property("id"); });',
        'pm.test("Has username", function () { pm.expect(pm.response.json()).to.have.property("username"); });'
    ))
)

# GET not found
$userItems += New-Request "GET /api/users/{id} - Not Found" "GET" "{{base_url}}/api/users/00000000-0000-0000-0000-000000000999" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 404", function () { pm.response.to.have.status(404); });'
    ))
)

# POST create
$userItems += New-Request "POST /api/users - Create" "POST" "{{base_url}}/api/users" `
    '{ "username": "_test_user_new", "password": "test123", "role": 2, "realName": "测试用户" }' $null @(
    (New-Event "test" @(
        'pm.test("Status 201", function () { pm.response.to.have.status(201); });',
        'pm.test("Save user id", function () { pm.environment.set("temp_user_id", pm.response.json().id); });'
    ))
)

# POST duplicate username
$userItems += New-Request "POST /api/users - Duplicate Username" "POST" "{{base_url}}/api/users" `
    '{ "username": "_test_user_new", "password": "test123", "role": 2, "realName": "测试用户" }' $null @(
    (New-Event "test" @(
        'pm.test("Status 409 or 400", function () { pm.expect(pm.response.code).to.be.oneOf([400, 409]); });'
    ))
)

# PUT update
$userItems += New-Request "PUT /api/users/{id} - Update" "PUT" "{{base_url}}/api/users/{{admin_user_id}}" `
    '{ "realName": "管理员_已更新" }' $null @(
    (New-Event "test" @(
        'pm.test("Status 200 or 204", function () { pm.expect(pm.response.code).to.be.oneOf([200, 204]); });'
    ))
)

# DELETE soft delete
$userItems += New-Request "DELETE /api/users/{id} - Soft Delete" "DELETE" "{{base_url}}/api/users/{{temp_user_id}}" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200 or 204", function () { pm.expect(pm.response.code).to.be.oneOf([200, 204]); });'
    ))
)

# PUT change-password happy
$userItems += New-Request "PUT /api/users/{id}/change-password - Happy" "PUT" "{{base_url}}/api/users/{{admin_user_id}}/change-password" `
    '{ "oldPassword": "admin123", "newPassword": "admin456" }' $null @(
    (New-Event "test" @(
        'pm.test("Status 200 or 204", function () { pm.expect(pm.response.code).to.be.oneOf([200, 204]); });'
    ))
)

# PUT change-password wrong old
$userItems += New-Request "PUT /api/users/{id}/change-password - Wrong Old" "PUT" "{{base_url}}/api/users/{{admin_user_id}}/change-password" `
    '{ "oldPassword": "wrong_old", "newPassword": "new123" }' $null @(
    (New-Event "test" @(
        'pm.test("Status 400", function () { pm.response.to.have.status(400); });'
    ))
)

# POST toggle-status
$userItems += New-Request "POST /api/users/{id}/toggle-status - Toggle" "POST" "{{base_url}}/api/users/{{admin_user_id}}/toggle-status" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Has status", function () { pm.expect(pm.response.json()).to.have.property("status"); });'
    ))
)

# POST toggle back
$userItems += New-Request "POST /api/users/{id}/toggle-status - Toggle Back" "POST" "{{base_url}}/api/users/{{admin_user_id}}/toggle-status" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });'
    ))
)

# POST restore
$userItems += New-Request "POST /api/users/{id}/restore - Restore" "POST" "{{base_url}}/api/users/{{temp_user_id}}/restore" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });'
    ))
)

# POST batch-delete happy
$userItems += New-Request "POST /api/users/batch-delete - Happy" "POST" "{{base_url}}/api/users/batch-delete" `
    '{ "ids": ["{{admin_user_id}}"] }' $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });'
    ))
)

# POST batch-delete empty
$userItems += New-Request "POST /api/users/batch-delete - Empty List" "POST" "{{base_url}}/api/users/batch-delete" `
    '{ "ids": [] }' $null @(
    (New-Event "test" @(
        'pm.test("Status 400", function () { pm.response.to.have.status(400); });'
    ))
)

# POST batch-enable
$userItems += New-Request "POST /api/users/batch-enable - Happy" "POST" "{{base_url}}/api/users/batch-enable" `
    '{ "ids": ["{{admin_user_id}}"] }' $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });'
    ))
)

# POST batch-disable
$userItems += New-Request "POST /api/users/batch-disable - Happy" "POST" "{{base_url}}/api/users/batch-disable" `
    '{ "ids": ["{{admin_user_id}}"] }' $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });'
    ))
)

# GET current
$userItems += New-Request "GET /api/users/current - Current User" "GET" "{{base_url}}/api/users/current" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Has id", function () { pm.expect(pm.response.json()).to.have.property("id"); });',
        'pm.test("Has username", function () { pm.expect(pm.response.json()).to.have.property("username"); });'
    ))
)

# POST reset-password happy
$userItems += New-Request "POST /api/users/{id}/reset-password - Happy" "POST" "{{base_url}}/api/users/{{admin_user_id}}/reset-password" `
    '{ "mustChangeOnNextLogin": true }' $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Has success or temporaryPassword", function () { var j = pm.response.json(); pm.expect(j.success === true || j.temporaryPassword !== undefined).to.be.true; });'
    ))
)

# POST reset-password not found
$userItems += New-Request "POST /api/users/{id}/reset-password - Not Found" "POST" "{{base_url}}/api/users/00000000-0000-0000-0000-000000000999/reset-password" `
    '{ "mustChangeOnNextLogin": true }' $null @(
    (New-Event "test" @(
        'pm.test("Status 404", function () { pm.response.to.have.status(404); });'
    ))
)

# PUT profile happy
$userItems += New-Request "PUT /api/users/{id}/profile - Happy" "PUT" "{{base_url}}/api/users/{{admin_user_id}}/profile" `
    '{ "realName": "管理员", "phoneNumber": "13800000000", "email": "admin@test.com" }' $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Has realName", function () { pm.expect(pm.response.json()).to.have.property("realName"); });'
    ))
)

# PUT profile not found
$userItems += New-Request "PUT /api/users/{id}/profile - Not Found" "PUT" "{{base_url}}/api/users/00000000-0000-0000-0000-000000000999/profile" `
    '{ "realName": "Ghost", "phoneNumber": "000", "email": "ghost@test.com" }' $null @(
    (New-Event "test" @(
        'pm.test("Status 404", function () { pm.response.to.have.status(404); });'
    ))
)

$usersFolder = New-Folder "03-Users" $userItems "User management endpoints"

# ============================================================
# 04-PATIENTS
# ============================================================
$patientItems = @()

# GET list
$patientItems += New-Request "GET /api/patients - List" "GET" "{{base_url}}/api/patients?page=1&pageSize=5" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Response is array", function () { pm.expect(pm.response.json()).to.be.an("array"); });'
    ))
)

# GET list keyword
$patientItems += New-Request "GET /api/patients - Keyword Search" "GET" "{{base_url}}/api/patients?keyword=_test_" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Response is array", function () { pm.expect(pm.response.json()).to.be.an("array"); });',
        'pm.test("Has results", function () { pm.expect(pm.response.json().length).to.be.above(0); });'
    ))
)

# GET by id
$patientItems += New-Request "GET /api/patients/{id} - By ID" "GET" "{{base_url}}/api/patients/{{test_patient_id}}" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Has name", function () { pm.expect(pm.response.json()).to.have.property("name"); });'
    ))
)

# GET not found
$patientItems += New-Request "GET /api/patients/{id} - Not Found" "GET" "{{base_url}}/api/patients/00000000-0000-0000-0000-000000000999" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 404", function () { pm.response.to.have.status(404); });'
    ))
)

# POST create invalid
$patientItems += New-Request "POST /api/patients - Invalid (empty body)" "POST" "{{base_url}}/api/patients" `
    '{}' $null @(
    (New-Event "test" @(
        'pm.test("Status 400", function () { pm.response.to.have.status(400); });'
    ))
)

# PUT update happy
$patientItems += New-Request "PUT /api/patients/{id} - Update" "PUT" "{{base_url}}/api/patients/{{test_patient_id}}" `
    '{"id":"{{test_patient_id}}","name":"_test_患者张三_已更新","gender":"男","birthDate":"1990-01-15","phoneNumber":"13800138001","idNumber":"110101199001150001","address":"北京市东城区测试街1号"}' $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });'
    ))
)

# PUT update not found
$patientItems += New-Request "PUT /api/patients/{id} - Not Found" "PUT" "{{base_url}}/api/patients/00000000-0000-0000-0000-000000000999" `
    '{"id":"00000000-0000-0000-0000-000000000999","name":"Ghost"}' $null @(
    (New-Event "test" @(
        'pm.test("Status 404", function () { pm.response.to.have.status(404); });'
    ))
)

# DELETE happy
$patientItems += New-Request "DELETE /api/patients/{id} - Soft Delete" "DELETE" "{{base_url}}/api/patients/{{test_patient_id_3}}" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200 or 204", function () { pm.expect(pm.response.code).to.be.oneOf([200, 204]); });'
    ))
)

# DELETE not found
$patientItems += New-Request "DELETE /api/patients/{id} - Not Found" "DELETE" "{{base_url}}/api/patients/00000000-0000-0000-0000-000000000999" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 404", function () { pm.response.to.have.status(404); });'
    ))
)

# GET by-id-number happy
$patientItems += New-Request "GET /api/patients/by-id-number - Happy" "GET" "{{base_url}}/api/patients/by-id-number/110101199001150001" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Has name", function () { pm.expect(pm.response.json()).to.have.property("name"); });'
    ))
)

# GET by-id-number not found
$patientItems += New-Request "GET /api/patients/by-id-number - Not Found" "GET" "{{base_url}}/api/patients/by-id-number/NONEXISTENT_ID" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 404", function () { pm.response.to.have.status(404); });'
    ))
)

# POST batch-delete happy
$patientItems += New-Request "POST /api/patients/batch-delete - Happy" "POST" "{{base_url}}/api/patients/batch-delete" `
    '{ "ids": ["{{test_patient_id_3}}"] }' $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });'
    ))
)

# POST batch-delete empty
$patientItems += New-Request "POST /api/patients/batch-delete - Empty" "POST" "{{base_url}}/api/patients/batch-delete" `
    '{ "ids": [] }' $null @(
    (New-Event "test" @(
        'pm.test("Status 400", function () { pm.response.to.have.status(400); });'
    ))
)

# POST restore
$patientItems += New-Request "POST /api/patients/{id}/restore - Happy" "POST" "{{base_url}}/api/patients/{{test_patient_id_3}}/restore" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });'
    ))
)

# POST restore not found
$patientItems += New-Request "POST /api/patients/{id}/restore - Not Found" "POST" "{{base_url}}/api/patients/00000000-0000-0000-0000-000000000999/restore" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 404", function () { pm.response.to.have.status(404); });'
    ))
)

# POST toggle-status happy
$patientItems += New-Request "POST /api/patients/{id}/toggle-status - Happy" "POST" "{{base_url}}/api/patients/{{test_patient_id}}/toggle-status" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });'
    ))
)

# POST toggle-status not found
$patientItems += New-Request "POST /api/patients/{id}/toggle-status - Not Found" "POST" "{{base_url}}/api/patients/00000000-0000-0000-0000-000000000999/toggle-status" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 404", function () { pm.response.to.have.status(404); });'
    ))
)

# GET export
$patientItems += New-Request "GET /api/patients/export" "GET" "{{base_url}}/api/patients/export" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Non-empty array", function () { pm.expect(pm.response.json()).to.be.an("array"); });'
    ))
)

# GET import-template
$patientItems += New-Request "GET /api/patients/import-template" "GET" "{{base_url}}/api/patients/import-template" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Is array", function () { pm.expect(pm.response.json()).to.be.an("array"); });'
    ))
)

# POST import happy
$patientItems += New-Request "POST /api/patients/import - Happy" "POST" "{{base_url}}/api/patients/import" `
    '{"patients":[{"name":"_test_import_患者","gender":"女","birthDate":"1985-06-15","phoneNumber":"13900139001","idNumber":"110101198506150001","address":"测试导入地址"}]}' $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("SuccessCount > 0", function () { pm.expect(pm.response.json().successCount).to.be.above(0); });'
    ))
)

# POST import empty
$patientItems += New-Request "POST /api/patients/import - Empty" "POST" "{{base_url}}/api/patients/import" `
    '{"patients":[]}' $null @(
    (New-Event "test" @(
        'pm.test("Status 400", function () { pm.response.to.have.status(400); });'
    ))
)

$patientsFolder = New-Folder "04-Patients" $patientItems "Patient management endpoints"

# ============================================================
# 05-HERBS
# ============================================================
$herbItems = @()

# GET list
$herbItems += New-Request "GET /api/herbs - List" "GET" "{{base_url}}/api/herbs" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Response is array", function () { pm.expect(pm.response.json()).to.be.an("array"); });'
    ))
)

# GET keyword
$herbItems += New-Request "GET /api/herbs - Keyword" "GET" "{{base_url}}/api/herbs?keyword=_test_" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Has results", function () { pm.expect(pm.response.json().length).to.be.above(0); });'
    ))
)

# GET category
$herbItems += New-Request "GET /api/herbs - By Category" "GET" "{{base_url}}/api/herbs?category=补气药" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Is array", function () { pm.expect(pm.response.json()).to.be.an("array"); });'
    ))
)

# GET by id
$herbItems += New-Request "GET /api/herbs/{id} - By ID" "GET" "{{base_url}}/api/herbs/{{test_herb_id}}" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Has name", function () { pm.expect(pm.response.json()).to.have.property("name"); });'
    ))
)

# GET not found
$herbItems += New-Request "GET /api/herbs/{id} - Not Found" "GET" "{{base_url}}/api/herbs/00000000-0000-0000-0000-000000000999" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 404", function () { pm.response.to.have.status(404); });'
    ))
)

# POST create
$herbItems += New-Request "POST /api/herbs - Create" "POST" "{{base_url}}/api/herbs" `
    '{"name":"_test_白术","pinYinCode":"BZ","category":"补气药","properties":"温","origin":"浙江","spec":"统货","unit":"克","price":0.6,"costPrice":0.4,"effect":"健脾益气","usage":"煎服","status":1}' $null @(
    (New-Event "test" @(
        'pm.test("Status 201", function () { pm.response.to.have.status(201); });',
        'pm.test("Save herb id", function () { pm.environment.set("temp_herb_id", pm.response.json().id); });'
    ))
)

# POST create invalid
$herbItems += New-Request "POST /api/herbs - Invalid" "POST" "{{base_url}}/api/herbs" `
    '{}' $null @(
    (New-Event "test" @(
        'pm.test("Status 400", function () { pm.response.to.have.status(400); });'
    ))
)

# PUT update happy
$herbItems += New-Request "PUT /api/herbs/{id} - Update" "PUT" "{{base_url}}/api/herbs/{{test_herb_id}}" `
    '{"id":"{{test_herb_id}}","name":"_test_甘草_已更新","pinYinCode":"GC","category":"补气药","properties":"平","origin":"内蒙古","spec":"统货","unit":"克","price":0.5,"costPrice":0.3,"effect":"益气补中","usage":"煎服","status":1}' $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });'
    ))
)

# PUT update not found
$herbItems += New-Request "PUT /api/herbs/{id} - Not Found" "PUT" "{{base_url}}/api/herbs/00000000-0000-0000-0000-000000000999" `
    '{"id":"00000000-0000-0000-0000-000000000999","name":"Ghost"}' $null @(
    (New-Event "test" @(
        'pm.test("Status 404", function () { pm.response.to.have.status(404); });'
    ))
)

# DELETE happy
$herbItems += New-Request "DELETE /api/herbs/{id} - Soft Delete" "DELETE" "{{base_url}}/api/herbs/{{temp_herb_id}}" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200 or 204", function () { pm.expect(pm.response.code).to.be.oneOf([200, 204]); });'
    ))
)

# DELETE not found
$herbItems += New-Request "DELETE /api/herbs/{id} - Not Found" "DELETE" "{{base_url}}/api/herbs/00000000-0000-0000-0000-000000000999" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 404", function () { pm.response.to.have.status(404); });'
    ))
)

# POST batch-delete
$herbItems += New-Request "POST /api/herbs/batch-delete - Happy" "POST" "{{base_url}}/api/herbs/batch-delete" `
    '{ "ids": ["{{test_herb_id_3}}"] }' $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });'
    ))
)

# POST batch-delete empty
$herbItems += New-Request "POST /api/herbs/batch-delete - Empty" "POST" "{{base_url}}/api/herbs/batch-delete" `
    '{ "ids": [] }' $null @(
    (New-Event "test" @(
        'pm.test("Status 400", function () { pm.response.to.have.status(400); });'
    ))
)

# POST batch-enable
$herbItems += New-Request "POST /api/herbs/batch-enable - Happy" "POST" "{{base_url}}/api/herbs/batch-enable" `
    '{ "ids": ["{{test_herb_id}}", "{{test_herb_id_2}}"] }' $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });'
    ))
)

# POST batch-enable empty
$herbItems += New-Request "POST /api/herbs/batch-enable - Empty" "POST" "{{base_url}}/api/herbs/batch-enable" `
    '{ "ids": [] }' $null @(
    (New-Event "test" @(
        'pm.test("Status 400", function () { pm.response.to.have.status(400); });'
    ))
)

# POST batch-disable
$herbItems += New-Request "POST /api/herbs/batch-disable - Happy" "POST" "{{base_url}}/api/herbs/batch-disable" `
    '{ "ids": ["{{test_herb_id_2}}"] }' $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });'
    ))
)

# POST batch-disable empty
$herbItems += New-Request "POST /api/herbs/batch-disable - Empty" "POST" "{{base_url}}/api/herbs/batch-disable" `
    '{ "ids": [] }' $null @(
    (New-Event "test" @(
        'pm.test("Status 400", function () { pm.response.to.have.status(400); });'
    ))
)

# POST toggle-status
$herbItems += New-Request "POST /api/herbs/{id}/toggle-status" "POST" "{{base_url}}/api/herbs/{{test_herb_id}}/toggle-status" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });'
    ))
)

# POST restore
$herbItems += New-Request "POST /api/herbs/{id}/restore" "POST" "{{base_url}}/api/herbs/{{test_herb_id_3}}/restore" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });'
    ))
)

# GET export
$herbItems += New-Request "GET /api/herbs/export" "GET" "{{base_url}}/api/herbs/export" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Is array", function () { pm.expect(pm.response.json()).to.be.an("array"); });'
    ))
)

# GET import-template
$herbItems += New-Request "GET /api/herbs/import-template" "GET" "{{base_url}}/api/herbs/import-template" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });'
    ))
)

# POST batch-import
$herbItems += New-Request "POST /api/herbs/batch-import - Happy" "POST" "{{base_url}}/api/herbs/batch-import" `
    '{"herbs":[{"name":"_test_import_茯苓","pinYinCode":"FL","category":"利水渗湿药","properties":"平","unit":"克","price":0.4,"effect":"利水渗湿"}]}' $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("SuccessCount > 0", function () { pm.expect(pm.response.json().successCount).to.be.above(0); });'
    ))
)

# POST batch-import empty
$herbItems += New-Request "POST /api/herbs/batch-import - Empty" "POST" "{{base_url}}/api/herbs/batch-import" `
    '{"herbs":[]}' $null @(
    (New-Event "test" @(
        'pm.test("Status 400", function () { pm.response.to.have.status(400); });'
    ))
)

# GET categories
$herbItems += New-Request "GET /api/herbs/categories" "GET" "{{base_url}}/api/herbs/categories" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Is array", function () { pm.expect(pm.response.json()).to.be.an("array"); });'
    ))
)

$herbsFolder = New-Folder "05-Herbs" $herbItems "Herb management endpoints"

# ============================================================
# 06-FORMULAS
# ============================================================
$formulaItems = @()

# GET list
$formulaItems += New-Request "GET /api/formulas - List" "GET" "{{base_url}}/api/formulas" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Response is array", function () { pm.expect(pm.response.json()).to.be.an("array"); });'
    ))
)

# GET keyword
$formulaItems += New-Request "GET /api/formulas - Keyword" "GET" "{{base_url}}/api/formulas?keyword=_test_" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Has results", function () { pm.expect(pm.response.json().length).to.be.above(0); });'
    ))
)

# GET category
$formulaItems += New-Request "GET /api/formulas - By Category" "GET" "{{base_url}}/api/formulas?category=补气方" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });'
    ))
)

# GET by id
$formulaItems += New-Request "GET /api/formulas/{id} - By ID" "GET" "{{base_url}}/api/formulas/{{test_formula_id}}" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Has name", function () { pm.expect(pm.response.json()).to.have.property("name"); });'
    ))
)

# GET not found
$formulaItems += New-Request "GET /api/formulas/{id} - Not Found" "GET" "{{base_url}}/api/formulas/00000000-0000-0000-0000-000000000999" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 404", function () { pm.response.to.have.status(404); });'
    ))
)

# POST create
$formulaItems += New-Request "POST /api/formulas - Create" "POST" "{{base_url}}/api/formulas" `
    '{"name":"_test_新加香薷饮","pinYinCode":"XJXRY","category":"解表方","description":"祛暑解表","herbs":[]}' $null @(
    (New-Event "test" @(
        'pm.test("Status 201", function () { pm.response.to.have.status(201); });',
        'pm.test("Save formula id", function () { pm.environment.set("temp_formula_id", pm.response.json().id); });'
    ))
)

# POST create invalid
$formulaItems += New-Request "POST /api/formulas - Invalid" "POST" "{{base_url}}/api/formulas" `
    '{}' $null @(
    (New-Event "test" @(
        'pm.test("Status 400", function () { pm.response.to.have.status(400); });'
    ))
)

# PUT update
$formulaItems += New-Request "PUT /api/formulas/{id} - Update" "PUT" "{{base_url}}/api/formulas/{{test_formula_id}}" `
    '{"id":"{{test_formula_id}}","name":"_test_四君子汤_已更新","pinYinCode":"SJZT","category":"补气方","description":"益气健脾_更新","herbs":[]}' $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });'
    ))
)

# PUT update not found
$formulaItems += New-Request "PUT /api/formulas/{id} - Not Found" "PUT" "{{base_url}}/api/formulas/00000000-0000-0000-0000-000000000999" `
    '{"id":"00000000-0000-0000-0000-000000000999","name":"Ghost"}' $null @(
    (New-Event "test" @(
        'pm.test("Status 404", function () { pm.response.to.have.status(404); });'
    ))
)

# DELETE
$formulaItems += New-Request "DELETE /api/formulas/{id} - Soft Delete" "DELETE" "{{base_url}}/api/formulas/{{temp_formula_id}}" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200 or 204", function () { pm.expect(pm.response.code).to.be.oneOf([200, 204]); });'
    ))
)

# DELETE not found
$formulaItems += New-Request "DELETE /api/formulas/{id} - Not Found" "DELETE" "{{base_url}}/api/formulas/00000000-0000-0000-0000-000000000999" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 404", function () { pm.response.to.have.status(404); });'
    ))
)

# POST batch-delete
$formulaItems += New-Request "POST /api/formulas/batch-delete - Happy" "POST" "{{base_url}}/api/formulas/batch-delete" `
    '{ "ids": ["{{test_formula_id_2}}"] }' $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });'
    ))
)

# POST batch-delete empty
$formulaItems += New-Request "POST /api/formulas/batch-delete - Empty" "POST" "{{base_url}}/api/formulas/batch-delete" `
    '{ "ids": [] }' $null @(
    (New-Event "test" @(
        'pm.test("Status 400", function () { pm.response.to.have.status(400); });'
    ))
)

# POST batch-enable
$formulaItems += New-Request "POST /api/formulas/batch-enable - Happy" "POST" "{{base_url}}/api/formulas/batch-enable" `
    '{ "ids": ["{{test_formula_id}}"] }' $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });'
    ))
)

# POST batch-disable
$formulaItems += New-Request "POST /api/formulas/batch-disable - Happy" "POST" "{{base_url}}/api/formulas/batch-disable" `
    '{ "ids": ["{{test_formula_id}}"] }' $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });'
    ))
)

# POST toggle-status
$formulaItems += New-Request "POST /api/formulas/{id}/toggle-status" "POST" "{{base_url}}/api/formulas/{{test_formula_id}}/toggle-status" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });'
    ))
)

# POST restore (need to delete first, then restore — using test_formula_id_2 which was batch-deleted above)
$formulaItems += New-Request "POST /api/formulas/{id}/restore" "POST" "{{base_url}}/api/formulas/{{test_formula_id_2}}/restore" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200 or 404", function () { pm.expect(pm.response.code).to.be.oneOf([200, 404]); });'
    ))
)

# POST clone
$formulaItems += New-Request "POST /api/formulas/{id}/clone" "POST" "{{base_url}}/api/formulas/{{test_formula_id}}/clone" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 201", function () { pm.response.to.have.status(201); });',
        'pm.test("New ID != original", function () { pm.expect(pm.response.json().id).to.not.eql(pm.environment.get("test_formula_id")); });'
    ))
)

# POST clone not found
$formulaItems += New-Request "POST /api/formulas/{id}/clone - Not Found" "POST" "{{base_url}}/api/formulas/00000000-0000-0000-0000-000000000999/clone" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 404", function () { pm.response.to.have.status(404); });'
    ))
)

# GET export
$formulaItems += New-Request "GET /api/formulas/export" "GET" "{{base_url}}/api/formulas/export" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });'
    ))
)

# GET import-template
$formulaItems += New-Request "GET /api/formulas/import-template" "GET" "{{base_url}}/api/formulas/import-template" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });'
    ))
)

# POST batch-import
$formulaItems += New-Request "POST /api/formulas/batch-import - Happy" "POST" "{{base_url}}/api/formulas/batch-import" `
    '{"formulas":[{"name":"_test_import_验方","effect":"测试导入","herbs":[]}]}' $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("SuccessCount > 0", function () { pm.expect(pm.response.json().successCount).to.be.above(0); });'
    ))
)

# POST batch-import empty
$formulaItems += New-Request "POST /api/formulas/batch-import - Empty" "POST" "{{base_url}}/api/formulas/batch-import" `
    '{"formulas":[]}' $null @(
    (New-Event "test" @(
        'pm.test("Status 400", function () { pm.response.to.have.status(400); });'
    ))
)

# GET categories
$formulaItems += New-Request "GET /api/formulas/categories" "GET" "{{base_url}}/api/formulas/categories" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Is array", function () { pm.expect(pm.response.json()).to.be.an("array"); });'
    ))
)

$formulasFolder = New-Folder "06-Formulas" $formulaItems "Formula management endpoints"

# ============================================================
# 07-MEDICAL CASES
# ============================================================
$mcItems = @()

# GET list
$mcItems += New-Request "GET /api/medicalcases - List" "GET" "{{base_url}}/api/medicalcases" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Response is array", function () { pm.expect(pm.response.json()).to.be.an("array"); });'
    ))
)

# GET list with patientId
$mcItems += New-Request "GET /api/medicalcases - By PatientId" "GET" "{{base_url}}/api/medicalcases?patientId={{test_patient_id}}" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Is array", function () { pm.expect(pm.response.json()).to.be.an("array"); });'
    ))
)

# GET detail
$mcItems += New-Request "GET /api/medicalcases/{id} - Detail" "GET" "{{base_url}}/api/medicalcases/{{test_medical_case_id}}" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Has patientId", function () { pm.expect(pm.response.json()).to.have.property("patientId"); });',
        'pm.test("Has consultation property", function () { pm.expect(pm.response.json()).to.have.property("consultation"); });',
        'pm.test("Has prescription property", function () { pm.expect(pm.response.json()).to.have.property("prescription"); });'
    ))
)

# GET detail not found
$mcItems += New-Request "GET /api/medicalcases/{id} - Not Found" "GET" "{{base_url}}/api/medicalcases/00000000-0000-0000-0000-000000000999" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 404", function () { pm.response.to.have.status(404); });'
    ))
)

# POST create invalid
$mcItems += New-Request "POST /api/medicalcases - Invalid" "POST" "{{base_url}}/api/medicalcases" `
    '{}' $null @(
    (New-Event "test" @(
        'pm.test("Status 400", function () { pm.response.to.have.status(400); });'
    ))
)

# DELETE
$mcItems += New-Request "DELETE /api/medicalcases/{id} - Soft Delete" "DELETE" "{{base_url}}/api/medicalcases/{{test_medical_case_id_2}}" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200 or 204", function () { pm.expect(pm.response.code).to.be.oneOf([200, 204]); });'
    ))
)

# DELETE not found
$mcItems += New-Request "DELETE /api/medicalcases/{id} - Not Found" "DELETE" "{{base_url}}/api/medicalcases/00000000-0000-0000-0000-000000000999" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 404", function () { pm.response.to.have.status(404); });'
    ))
)

# GET search
$mcItems += New-Request "GET /api/medicalcases/search" "GET" "{{base_url}}/api/medicalcases/search?startDate=2020-01-01&endDate=2030-12-31&page=1&pageSize=10" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Has items", function () { pm.expect(pm.response.json()).to.have.property("items"); });',
        'pm.test("Has totalCount", function () { pm.expect(pm.response.json()).to.have.property("totalCount"); });'
    ))
)

# GET query - All
$mcItems += New-Request "GET /api/medicalcases/query - All" "GET" "{{base_url}}/api/medicalcases/query?queryType=0&pageIndex=1&pageSize=10" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Has items", function () { pm.expect(pm.response.json()).to.have.property("items"); });'
    ))
)

# GET query - ByPatient
$mcItems += New-Request "GET /api/medicalcases/query - ByPatient" "GET" "{{base_url}}/api/medicalcases/query?queryType=1&patientId={{test_patient_id}}&pageIndex=1&pageSize=10" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });'
    ))
)

# GET query - Recent
$mcItems += New-Request "GET /api/medicalcases/query - Recent" "GET" "{{base_url}}/api/medicalcases/query?queryType=4&limit=5&pageIndex=1&pageSize=10" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });'
    ))
)

# POST batch-details
$mcItems += New-Request "POST /api/medicalcases/batch-details - Happy" "POST" "{{base_url}}/api/medicalcases/batch-details" `
    '{ "ids": ["{{test_medical_case_id}}"] }' $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Is array", function () { pm.expect(pm.response.json()).to.be.an("array"); });'
    ))
)

# POST batch-details > 50
$mcItems += New-Request "POST /api/medicalcases/batch-details - Too Many" "POST" "{{base_url}}/api/medicalcases/batch-details" `
    '{ "ids": ["00000000-0000-0000-0000-000000000001","00000000-0000-0000-0000-000000000002","00000000-0000-0000-0000-000000000003","00000000-0000-0000-0000-000000000004","00000000-0000-0000-0000-000000000005","00000000-0000-0000-0000-000000000006","00000000-0000-0000-0000-000000000007","00000000-0000-0000-0000-000000000008","00000000-0000-0000-0000-000000000009","00000000-0000-0000-0000-000000000010","00000000-0000-0000-0000-000000000011","00000000-0000-0000-0000-000000000012","00000000-0000-0000-0000-000000000013","00000000-0000-0000-0000-000000000014","00000000-0000-0000-0000-000000000015","00000000-0000-0000-0000-000000000016","00000000-0000-0000-0000-000000000017","00000000-0000-0000-0000-000000000018","00000000-0000-0000-0000-000000000019","00000000-0000-0000-0000-000000000020","00000000-0000-0000-0000-000000000021","00000000-0000-0000-0000-000000000022","00000000-0000-0000-0000-000000000023","00000000-0000-0000-0000-000000000024","00000000-0000-0000-0000-000000000025","00000000-0000-0000-0000-000000000026","00000000-0000-0000-0000-000000000027","00000000-0000-0000-0000-000000000028","00000000-0000-0000-0000-000000000029","00000000-0000-0000-0000-000000000030","00000000-0000-0000-0000-000000000031","00000000-0000-0000-0000-000000000032","00000000-0000-0000-0000-000000000033","00000000-0000-0000-0000-000000000034","00000000-0000-0000-0000-000000000035","00000000-0000-0000-0000-000000000036","00000000-0000-0000-0000-000000000037","00000000-0000-0000-0000-000000000038","00000000-0000-0000-0000-000000000039","00000000-0000-0000-0000-000000000040","00000000-0000-0000-0000-000000000041","00000000-0000-0000-0000-000000000042","00000000-0000-0000-0000-000000000043","00000000-0000-0000-0000-000000000044","00000000-0000-0000-0000-000000000045","00000000-0000-0000-0000-000000000046","00000000-0000-0000-0000-000000000047","00000000-0000-0000-0000-000000000048","00000000-0000-0000-0000-000000000049","00000000-0000-0000-0000-000000000050","00000000-0000-0000-0000-000000000051"] }' $null @(
    (New-Event "test" @(
        'pm.test("Status 400", function () { pm.response.to.have.status(400); });'
    ))
)

# GET permissions
$mcItems += New-Request "GET /api/medicalcases/{id}/permissions" "GET" "{{base_url}}/api/medicalcases/{{test_medical_case_id}}/permissions" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("CanEdit is true", function () { pm.expect(pm.response.json().canEdit).to.eql(true); });',
        'pm.test("Has canDelete", function () { pm.expect(pm.response.json()).to.have.property("canDelete"); });'
    ))
)

# GET permissions not found
$mcItems += New-Request "GET /api/medicalcases/{id}/permissions - Not Found" "GET" "{{base_url}}/api/medicalcases/00000000-0000-0000-0000-000000000999/permissions" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 404", function () { pm.response.to.have.status(404); });'
    ))
)

# GET by-status
$mcItems += New-Request "GET /api/medicalcases/by-status/{status}" "GET" "{{base_url}}/api/medicalcases/by-status/1" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Is array", function () { pm.expect(pm.response.json()).to.be.an("array"); });'
    ))
)

# PUT close
$mcItems += New-Request "PUT /api/medicalcases/{id}/close" "PUT" "{{base_url}}/api/medicalcases/{{test_medical_case_id}}/close" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });'
    ))
)

# PUT close already completed
$mcItems += New-Request "PUT /api/medicalcases/{id}/close - Already Completed" "PUT" "{{base_url}}/api/medicalcases/{{test_medical_case_id}}/close" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 400", function () { pm.response.to.have.status(400); });'
    ))
)

# PUT close not found
$mcItems += New-Request "PUT /api/medicalcases/{id}/close - Not Found" "PUT" "{{base_url}}/api/medicalcases/00000000-0000-0000-0000-000000000999/close" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 404", function () { pm.response.to.have.status(404); });'
    ))
)

# Create a fresh MC for suspend/cancel tests
$mcItems += New-Request "POST /api/medicalcases - Create for Suspend" "POST" "{{base_url}}/api/medicalcases" `
    '{"patientId":"{{test_patient_id}}","userId":"{{admin_user_id}}","remark":"_test_suspend_target"}' $null @(
    (New-Event "test" @(
        'pm.test("Status 201", function () { pm.response.to.have.status(201); });',
        'pm.test("Save mc id", function () { pm.environment.set("temp_mc_for_suspend", pm.response.json().id); });'
    ))
)

# PUT suspend
$mcItems += New-Request "PUT /api/medicalcases/{id}/suspend" "PUT" "{{base_url}}/api/medicalcases/{{temp_mc_for_suspend}}/suspend" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });'
    ))
)

# PUT suspend not found
$mcItems += New-Request "PUT /api/medicalcases/{id}/suspend - Not Found" "PUT" "{{base_url}}/api/medicalcases/00000000-0000-0000-0000-000000000999/suspend" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 404", function () { pm.response.to.have.status(404); });'
    ))
)

# Create a fresh MC for cancel
$mcItems += New-Request "POST /api/medicalcases - Create for Cancel" "POST" "{{base_url}}/api/medicalcases" `
    '{"patientId":"{{test_patient_id_2}}","userId":"{{admin_user_id}}","remark":"_test_cancel_target"}' $null @(
    (New-Event "test" @(
        'pm.test("Status 201", function () { pm.response.to.have.status(201); });',
        'pm.test("Save mc id", function () { pm.environment.set("temp_mc_for_cancel", pm.response.json().id); });'
    ))
)

# PUT cancel
$mcItems += New-Request "PUT /api/medicalcases/{id}/cancel" "PUT" "{{base_url}}/api/medicalcases/{{temp_mc_for_cancel}}/cancel" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200 or 204", function () { pm.expect(pm.response.code).to.be.oneOf([200, 204]); });'
    ))
)

# PUT cancel not found
$mcItems += New-Request "PUT /api/medicalcases/{id}/cancel - Not Found" "PUT" "{{base_url}}/api/medicalcases/00000000-0000-0000-0000-000000000999/cancel" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 404", function () { pm.response.to.have.status(404); });'
    ))
)

# Create MC for prescription-flag
$mcItems += New-Request "POST /api/medicalcases - Create for Flag" "POST" "{{base_url}}/api/medicalcases" `
    '{"patientId":"{{test_patient_id}}","userId":"{{admin_user_id}}","remark":"_test_flag_target"}' $null @(
    (New-Event "test" @(
        'pm.test("Status 201", function () { pm.response.to.have.status(201); });',
        'pm.test("Save mc id", function () { pm.environment.set("temp_mc_for_flag", pm.response.json().id); });'
    ))
)

# PUT prescription-flag
$mcItems += New-Request "PUT /api/medicalcases/{id}/prescription-flag" "PUT" "{{base_url}}/api/medicalcases/{{temp_mc_for_flag}}/prescription-flag" `
    '{ "needsPrescription": true }' $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });'
    ))
)

# PUT prescription-flag not found
$mcItems += New-Request "PUT /api/medicalcases/{id}/prescription-flag - Not Found" "PUT" "{{base_url}}/api/medicalcases/00000000-0000-0000-0000-000000000999/prescription-flag" `
    '{ "needsPrescription": true }' $null @(
    (New-Event "test" @(
        'pm.test("Status 404", function () { pm.response.to.have.status(404); });'
    ))
)

# PUT status
$mcItems += New-Request "PUT /api/medicalcases/{id}/status" "PUT" "{{base_url}}/api/medicalcases/{{temp_mc_for_flag}}/status" `
    '{ "status": 1 }' $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });'
    ))
)

# PUT status not found
$mcItems += New-Request "PUT /api/medicalcases/{id}/status - Not Found" "PUT" "{{base_url}}/api/medicalcases/00000000-0000-0000-0000-000000000999/status" `
    '{ "status": 1 }' $null @(
    (New-Event "test" @(
        'pm.test("Status 404", function () { pm.response.to.have.status(404); });'
    ))
)

# PUT print-completed
$mcItems += New-Request "PUT /api/medicalcases/{id}/print-completed" "PUT" "{{base_url}}/api/medicalcases/{{temp_mc_for_flag}}/print-completed" `
    '{ "printType": 0 }' $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });'
    ))
)

# PUT print-completed not found
$mcItems += New-Request "PUT /api/medicalcases/{id}/print-completed - Not Found" "PUT" "{{base_url}}/api/medicalcases/00000000-0000-0000-0000-000000000999/print-completed" `
    '{ "printType": 0 }' $null @(
    (New-Event "test" @(
        'pm.test("Status 404", function () { pm.response.to.have.status(404); });'
    ))
)

# PUT save
$mcItems += New-Request "PUT /api/medicalcases/{id} - Save" "PUT" "{{base_url}}/api/medicalcases/{{temp_mc_for_flag}}" `
    '{"patientId":"{{test_patient_id}}","userId":"{{admin_user_id}}","remark":"_test_updated_remark"}' $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });'
    ))
)

# PUT save not found
$mcItems += New-Request "PUT /api/medicalcases/{id} - Not Found" "PUT" "{{base_url}}/api/medicalcases/00000000-0000-0000-0000-000000000999" `
    '{"patientId":"{{test_patient_id}}","userId":"{{admin_user_id}}","remark":"x"}' $null @(
    (New-Event "test" @(
        'pm.test("Status 404", function () { pm.response.to.have.status(404); });'
    ))
)

# POST batch-delete happy
$mcItems += New-Request "POST /api/medicalcases/batch-delete - Happy" "POST" "{{base_url}}/api/medicalcases/batch-delete" `
    '{ "ids": ["{{temp_mc_for_flag}}"] }' $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });'
    ))
)

# POST batch-delete empty
$mcItems += New-Request "POST /api/medicalcases/batch-delete - Empty" "POST" "{{base_url}}/api/medicalcases/batch-delete" `
    '{ "ids": [] }' $null @(
    (New-Event "test" @(
        'pm.test("Status 400", function () { pm.response.to.have.status(400); });'
    ))
)

# GET pending
$mcItems += New-Request "GET /api/medicalcases/pending" "GET" "{{base_url}}/api/medicalcases/pending" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Is array", function () { pm.expect(pm.response.json()).to.be.an("array"); });'
    ))
)

# GET pending with patientId
$mcItems += New-Request "GET /api/medicalcases/pending - By PatientId" "GET" "{{base_url}}/api/medicalcases/pending?patientId={{test_patient_id}}" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });'
    ))
)

# GET audit-logs
$mcItems += New-Request "GET /api/medicalcases/{id}/audit-logs" "GET" "{{base_url}}/api/medicalcases/{{test_medical_case_id}}/audit-logs" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Has logs", function () { pm.expect(pm.response.json()).to.have.property("logs"); });'
    ))
)

# GET audit-logs not found
$mcItems += New-Request "GET /api/medicalcases/{id}/audit-logs - Not Found" "GET" "{{base_url}}/api/medicalcases/00000000-0000-0000-0000-000000000999/audit-logs" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 404", function () { pm.response.to.have.status(404); });'
    ))
)

# POST print-logs
$mcItems += New-Request "POST /api/medicalcases/{id}/print-logs" "POST" "{{base_url}}/api/medicalcases/{{test_medical_case_id}}/print-logs" `
    '{ "printType": 0 }' $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Success is true", function () { pm.expect(pm.response.json().success).to.eql(true); });'
    ))
)

# POST print-logs not found
$mcItems += New-Request "POST /api/medicalcases/{id}/print-logs - Not Found" "POST" "{{base_url}}/api/medicalcases/00000000-0000-0000-0000-000000000999/print-logs" `
    '{ "printType": 0 }' $null @(
    (New-Event "test" @(
        'pm.test("Status 404", function () { pm.response.to.have.status(404); });'
    ))
)

$mcFolder = New-Folder "07-MedicalCases" $mcItems "Medical case management endpoints"

# ============================================================
# 08-REGISTRATIONS
# ============================================================
$regItems = @()

# GET list
$regItems += New-Request "GET /api/registrations - List" "GET" "{{base_url}}/api/registrations" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Response is array", function () { pm.expect(pm.response.json()).to.be.an("array"); });'
    ))
)

# GET list with date
$regItems += New-Request "GET /api/registrations - By Date" "GET" "{{base_url}}/api/registrations?date=2026-05-03" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });'
    ))
)

# GET detail
$regItems += New-Request "GET /api/registrations/{id} - Detail" "GET" "{{base_url}}/api/registrations/{{test_registration_id}}" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Has patientId", function () { pm.expect(pm.response.json()).to.have.property("patientId"); });'
    ))
)

# GET detail not found
$regItems += New-Request "GET /api/registrations/{id} - Not Found" "GET" "{{base_url}}/api/registrations/00000000-0000-0000-0000-000000000999" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 404", function () { pm.response.to.have.status(404); });'
    ))
)

# POST create invalid
$regItems += New-Request "POST /api/registrations - Invalid" "POST" "{{base_url}}/api/registrations" `
    '{}' $null @(
    (New-Event "test" @(
        'pm.test("Status 400", function () { pm.response.to.have.status(400); });'
    ))
)

# PUT update
$regItems += New-Request "PUT /api/registrations/{id} - Update" "PUT" "{{base_url}}/api/registrations/{{test_registration_id}}" `
    '{"id":"{{test_registration_id}}","patientId":"{{test_patient_id}}","patientName":"_test_患者张三","status":0}' $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });'
    ))
)

# PUT update not found
$regItems += New-Request "PUT /api/registrations/{id} - Not Found" "PUT" "{{base_url}}/api/registrations/00000000-0000-0000-0000-000000000999" `
    '{"id":"00000000-0000-0000-0000-000000000999","patientId":"{{test_patient_id}}","patientName":"Ghost","status":0}' $null @(
    (New-Event "test" @(
        'pm.test("Status 404", function () { pm.response.to.have.status(404); });'
    ))
)

# DELETE
$regItems += New-Request "DELETE /api/registrations/{id} - Soft Delete" "DELETE" "{{base_url}}/api/registrations/{{test_registration_id_2}}" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200 or 204", function () { pm.expect(pm.response.code).to.be.oneOf([200, 204]); });'
    ))
)

# DELETE not found
$regItems += New-Request "DELETE /api/registrations/{id} - Not Found" "DELETE" "{{base_url}}/api/registrations/00000000-0000-0000-0000-000000000999" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 404", function () { pm.response.to.have.status(404); });'
    ))
)

# GET queue
$regItems += New-Request "GET /api/registrations/queue" "GET" "{{base_url}}/api/registrations/queue" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Is array", function () { pm.expect(pm.response.json()).to.be.an("array"); });'
    ))
)

# PUT start-visit
$regItems += New-Request "PUT /api/registrations/{id}/start-visit" "PUT" "{{base_url}}/api/registrations/{{test_registration_id}}/start-visit" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });'
    ))
)

# PUT start-visit not waiting (already InProgress from above)
$regItems += New-Request "PUT /api/registrations/{id}/start-visit - Not Waiting" "PUT" "{{base_url}}/api/registrations/{{test_registration_id}}/start-visit" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 400", function () { pm.response.to.have.status(400); });'
    ))
)

# PUT cancel
$regItems += New-Request "PUT /api/registrations/{id}/cancel" "PUT" "{{base_url}}/api/registrations/{{test_registration_id}}/cancel" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });'
    ))
)

# PUT cancel not found
$regItems += New-Request "PUT /api/registrations/{id}/cancel - Not Found" "PUT" "{{base_url}}/api/registrations/00000000-0000-0000-0000-000000000999/cancel" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 404", function () { pm.response.to.have.status(404); });'
    ))
)

# POST quick-visit
$regItems += New-Request "POST /api/registrations/quick-visit" "POST" "{{base_url}}/api/registrations/quick-visit" `
    '{"patientId":"{{test_patient_id}}","patientName":"_test_患者张三","remark":"快速接诊测试"}' $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Has registrationId", function () { pm.expect(pm.response.json()).to.have.property("registrationId"); });',
        'pm.test("Has medicalCaseId", function () { pm.expect(pm.response.json()).to.have.property("medicalCaseId"); });'
    ))
)

# POST quick-visit empty patient
$regItems += New-Request "POST /api/registrations/quick-visit - Empty Patient" "POST" "{{base_url}}/api/registrations/quick-visit" `
    '{"patientId":"00000000-0000-0000-0000-000000000000","patientName":""}' $null @(
    (New-Event "test" @(
        'pm.test("Status 400", function () { pm.response.to.have.status(400); });'
    ))
)

$regFolder = New-Folder "08-Registrations" $regItems "Registration management endpoints"

# ============================================================
# 09-DIAGNOSTICS
# ============================================================
$diagItems = @()

$diagItems += New-Request "GET /api/diagnostics/db-info" "GET" "{{base_url}}/api/diagnostics/db-info" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Has provider", function () { pm.expect(pm.response.json()).to.have.property("provider"); });',
        'pm.test("Has connectionState", function () { pm.expect(pm.response.json()).to.have.property("connectionState"); });'
    ))
)

$diagItems += New-Request "GET /api/diagnostics/version" "GET" "{{base_url}}/api/diagnostics/version" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Has assemblyVersion", function () { pm.expect(pm.response.json()).to.have.property("assemblyVersion"); });',
        'pm.test("Has frameworkVersion", function () { pm.expect(pm.response.json()).to.have.property("frameworkVersion"); });'
    ))
)

$diagItems += New-Request "GET /api/diagnostics/logs/recent" "GET" "{{base_url}}/api/diagnostics/logs/recent?count=5" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Has count", function () { pm.expect(pm.response.json()).to.have.property("count"); });',
        'pm.test("Has items", function () { pm.expect(pm.response.json()).to.have.property("items"); });'
    ))
)

$diagFolder = New-Folder "09-Diagnostics" $diagItems "Diagnostics endpoints"

# ============================================================
# 10-CONFIGURATION
# ============================================================
$configItems = @()

# PUT set
$configItems += New-Request "PUT /api/configuration/{key} - Set" "PUT" "{{base_url}}/api/configuration/test_key" `
    '"test_value"' @(
    @{ key = 'Content-Type'; value = 'application/json' }
) @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Key matches", function () { pm.expect(pm.response.json().key).to.eql("test_key"); });',
        'pm.test("Value matches", function () { pm.expect(pm.response.json().value).to.eql("test_value"); });'
    ))
)

# GET get
$configItems += New-Request "GET /api/configuration/{key} - Get" "GET" "{{base_url}}/api/configuration/test_key" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Value matches", function () { pm.expect(pm.response.json().value).to.eql("test_value"); });'
    ))
)

# GET not found
$configItems += New-Request "GET /api/configuration/{key} - Not Found" "GET" "{{base_url}}/api/configuration/nonexistent_key_12345" $null $null @(
    (New-Event "test" @(
        'pm.test("Status 404", function () { pm.response.to.have.status(404); });'
    ))
)

# PUT empty key
$configItems += New-Request "PUT /api/configuration/{key} - Empty Key" "PUT" "{{base_url}}/api/configuration/ " `
    '"value"' @(
    @{ key = 'Content-Type'; value = 'application/json' }
) @(
    (New-Event "test" @(
        'pm.test("Status 400", function () { pm.response.to.have.status(400); });'
    ))
)

# GET after overwrite
$configItems += New-Request "PUT /api/configuration/{key} - Overwrite" "PUT" "{{base_url}}/api/configuration/test_key" `
    '"updated_value"' @(
    @{ key = 'Content-Type'; value = 'application/json' }
) @(
    (New-Event "test" @(
        'pm.test("Status 200", function () { pm.response.to.have.status(200); });',
        'pm.test("Value updated", function () { pm.expect(pm.response.json().value).to.eql("updated_value"); });'
    ))
)

$configFolder = New-Folder "10-Configuration" $configItems "Configuration endpoints"

# ============================================================
# ASSEMBLE COLLECTION
# ============================================================
$collection = @{
    info = @{
        name = "LocalWebAPI - Full Test Suite"
        description = "Complete Postman Collection v2.1 for LocalWebAPI at http://127.0.0.1:5290. Covers all 10 controllers with ~310 test assertions across ~97 endpoints."
        schema = "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
    }
    item = @(
        $setupFolder,
        $healthFolder,
        $authFolder,
        $usersFolder,
        $patientsFolder,
        $herbsFolder,
        $formulasFolder,
        $mcFolder,
        $regFolder,
        $diagFolder,
        $configFolder
    )
    event = @($collectionPreRequest)
    variable = @(
        @{ key = 'base_url'; value = 'http://127.0.0.1:5290' }
    )
}

# Write JSON
$json = $collection | ConvertTo-Json -Depth 20
[System.IO.File]::WriteAllText($outPath, $json, [System.Text.Encoding]::UTF8)

# Count items
$totalRequests = 0
function Count-Requests($items) {
    $count = 0
    foreach ($item in $items) {
        if ($item.request) { $count++ }
        if ($item.item) { $count += (Count-Requests $item.item) }
    }
    return $count
}
$totalRequests = Count-Requests $collection.item

$totalTests = 0
function Count-Tests($items) {
    $count = 0
    foreach ($item in $items) {
        if ($item.event) {
            foreach ($ev in $item.event) {
                if ($ev.listen -eq 'test') {
                    $count += ($ev.script.exec | Where-Object { $_ -match 'pm\.test\(' }).Count
                }
            }
        }
        if ($item.item) { $count += (Count-Tests $item.item) }
    }
    return $count
}
$totalTests = Count-Tests $collection.item

Write-Output "Collection written to: $outPath"
Write-Output "Total requests: $totalRequests"
Write-Output "Total test assertions: $totalTests"
Write-Output "File size: $((Get-Item $outPath).Length) bytes"
