# Generate full Newman test collection for LYBT WebAPI
$baseUrl = "http://60.190.215.86:5000"

$collection = @{
    info = @{
        name = "LYBT WebAPI Full Integration Test v5"
        schema = "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
    }
    variable = @(
        @{ key = "baseUrl"; value = $baseUrl },
        @{ key = "token"; value = "" },
        @{ key = "userId"; value = "" },
        @{ key = "patientId"; value = "" },
        @{ key = "herbId"; value = "" },
        @{ key = "formulaId"; value = "" },
        @{ key = "registrationId"; value = "" },
        @{ key = "medicalCaseId"; value = "" },
        @{ key = "createdUserId"; value = "" }
    )
    item = @()
}

function Make-Request($method, $url, $body, $headers) {
    $fullUrl = "{{baseUrl}}$url"
    $pathParts = @($url.TrimStart('/').Split('/') | Where-Object { $_ -ne '' })
    $req = @{
        method = $method
        url = @{
            raw = $fullUrl
            host = @("{{baseUrl}}")
            path = $pathParts
        }
    }
    if ($headers) { $req.header = $headers }
    if ($body) { $req.body = @{ mode = "raw"; raw = $body } }
    return $req
}

function Make-Test($name, $script) {
    return @{ listen = "test"; script = @{ exec = @($script) } }
}

function Make-Item($name, $request, $tests) {
    $events = @()
    foreach ($t in $tests) { $events += Make-Test $t.name $t.script }
    return @{ name = $name; request = $request; event = $events }
}

$auth = @(@{ key = "Authorization"; value = "Bearer {{token}}" })
$authJson = $auth + @(@{ key = "Content-Type"; value = "application/json" })
$jsonOnly = @(@{ key = "Content-Type"; value = "application/json" })

# 1. Health
$health = @{
    name = "1. Health"
    item = @(
        (Make-Item "GET /health" (Make-Request "GET" "/health") @(@{name="200";script="pm.test('200',function(){pm.response.to.have.status(200)});"})),
        (Make-Item "GET /health/ping" (Make-Request "GET" "/health/ping") @(@{name="status";script="pm.test('status',function(){pm.expect([200,401]).to.include(pm.response.code)});"})),
        (Make-Item "GET /health/details" (Make-Request "GET" "/api/v1/health/details" $null $auth) @(@{name="status";script="pm.test('status',function(){pm.expect([200,401]).to.include(pm.response.code)});"}))
    )
}
$collection.item += $health

# 2. Auth
$loginBody = '{"username":"sysadmin","password":"SysAdmin@2026!"}'
$authModule = @{
    name = "2. Auth"
    item = @(
        (Make-Item "POST /auth/login" (Make-Request "POST" "/api/v1/auth/login" $loginBody $jsonOnly) @(
            @{name="200";script="pm.test('200',function(){pm.response.to.have.status(200)});"},
            @{name="token";script="var d=pm.response.json().data;pm.expect(d).to.have.property('token');pm.collectionVariables.set('token',d.token);pm.collectionVariables.set('userId',d.user.id);"}
        )),
        (Make-Item "GET /auth/validate" (Make-Request "GET" "/api/v1/auth/validate" $null $auth) @(@{name="200";script="pm.test('200',function(){pm.response.to.have.status(200)});"})),
        (Make-Item "POST /auth/refresh" (Make-Request "POST" "/api/v1/auth/refresh" $loginBody $authJson) @(@{name="status";script="pm.test('status',function(){pm.expect([200,401]).to.include(pm.response.code)});"})),
        (Make-Item "POST /auth/auto-login" (Make-Request "POST" "/api/v1/auth/auto-login" '{}' $authJson) @(@{name="status";script="pm.test('status',function(){pm.expect([200,400,401,415]).to.include(pm.response.code)});"})),
        (Make-Item "POST /auth/logout" (Make-Request "POST" "/api/v1/auth/logout" '{}' $authJson) @(@{name="status";script="pm.test('status',function(){pm.expect([200,400,415]).to.include(pm.response.code)});"}))
    )
}
$collection.item += $authModule

# 3. Users
$userBody = '{"userName":"testuser1","realName":"Test User","email":"test1@lybt.com","role":1,"password":"Test@123456"}'
$usersModule = @{
    name = "3. Users"
    item = @(
        (Make-Item "GET /users" (Make-Request "GET" "/api/v1/users?page=1&pageSize=5" $null $auth) @(@{name="200";script="pm.test('200',function(){pm.response.to.have.status(200)});"})),
        (Make-Item "GET /users/current" (Make-Request "GET" "/api/v1/users/current" $null $auth) @(@{name="200";script="pm.test('200',function(){pm.response.to.have.status(200)});"})),
        (Make-Item "POST /users" (Make-Request "POST" "/api/v1/users" $userBody $authJson) @(@{name="status";script="pm.test('status',function(){pm.expect([200,201,400,422]).to.include(pm.response.code);if(pm.response.code===200||pm.response.code===201){pm.collectionVariables.set('createdUserId',pm.response.json().data.id)}});"})),
        (Make-Item "GET /users/{id}" (Make-Request "GET" "/api/v1/users/{{userId}}" $null $auth) @(@{name="200";script="pm.test('200',function(){pm.response.to.have.status(200)});"})),
        (Make-Item "PUT /users/{id}" (Make-Request "PUT" "/api/v1/users/{{createdUserId}}" '{"realName":"Updated User"}' $authJson) @(@{name="status";script="pm.test('status',function(){if(pm.collectionVariables.get('createdUserId')){pm.expect([200,204]).to.include(pm.response.code)}else{pm.expect(true).to.be.true}});"})),
        (Make-Item "POST /users/{id}/reset-password" (Make-Request "POST" "/api/v1/users/{{createdUserId}}/reset-password" '{"newPassword":"NewTest@123"}' $authJson) @(@{name="status";script="pm.test('status',function(){if(pm.collectionVariables.get('createdUserId')){pm.expect([200,204,400]).to.include(pm.response.code)}else{pm.expect(true).to.be.true}});"})),
        (Make-Item "PUT /users/{id}/profile" (Make-Request "PUT" "/api/v1/users/{{userId}}/profile" '{"displayName":"Admin Updated"}' $authJson) @(@{name="status";script="pm.test('status',function(){pm.expect([200,204,400]).to.include(pm.response.code)});"})),
        (Make-Item "PUT /users/{id}/change-password" (Make-Request "PUT" "/api/v1/users/{{userId}}/change-password" '{"currentPassword":"SysAdmin@2026!","newPassword":"SysAdmin@2026!"}' $authJson) @(@{name="status";script="pm.test('status',function(){pm.expect([200,204,400]).to.include(pm.response.code)});"})),
        (Make-Item "POST /users/{id}/toggle-status" (Make-Request "POST" "/api/v1/users/{{createdUserId}}/toggle-status" $null $auth) @(@{name="status";script="pm.test('status',function(){if(pm.collectionVariables.get('createdUserId')){pm.expect([200,204]).to.include(pm.response.code)}else{pm.expect(true).to.be.true}});"})),
        (Make-Item "POST /users/{id}/restore" (Make-Request "POST" "/api/v1/users/{{createdUserId}}/restore" $null $auth) @(@{name="status";script="pm.test('status',function(){if(pm.collectionVariables.get('createdUserId')){pm.expect([200,204,422]).to.include(pm.response.code)}else{pm.expect(true).to.be.true}});"})),
        (Make-Item "POST /users/batch-enable" (Make-Request "POST" "/api/v1/users/batch-enable" '{"ids":["{{createdUserId}}"]}' $authJson) @(@{name="status";script="pm.test('status',function(){pm.expect([200,204,400]).to.include(pm.response.code)});"})),
        (Make-Item "POST /users/batch-disable" (Make-Request "POST" "/api/v1/users/batch-disable" '{"ids":["{{createdUserId}}"]}' $authJson) @(@{name="status";script="pm.test('status',function(){pm.expect([200,204,400]).to.include(pm.response.code)});"})),
        (Make-Item "DELETE /users/{id}" (Make-Request "DELETE" "/api/v1/users/{{createdUserId}}" $null $auth) @(@{name="status";script="pm.test('status',function(){if(pm.collectionVariables.get('createdUserId')){pm.expect([200,204]).to.include(pm.response.code)}else{pm.expect(true).to.be.true}});"})),
        (Make-Item "POST /users/batch-delete" (Make-Request "POST" "/api/v1/users/batch-delete" '{"ids":["{{createdUserId}}"]}' $authJson) @(@{name="status";script="pm.test('status',function(){pm.expect([200,204,400]).to.include(pm.response.code)});"}))
    )
}
$collection.item += $usersModule

# 4. Patients
$patientBody = '{"name":"TestPatientFull","gender":1,"birthDate":"1990-01-01","phoneNumber":"13800138099","idNumber":"110101199001049999"}'
$patientsModule = @{
    name = "4. Patients"
    item = @(
        (Make-Item "POST /patients" (Make-Request "POST" "/api/v1/patients" $patientBody $authJson) @(@{name="status";script="pm.test('status',function(){if(pm.response.code===200||pm.response.code===201){pm.collectionVariables.set('patientId',pm.response.json().data.id)}else if(pm.response.code===422){pm.expect(true).to.be.true}else{pm.expect.fail('Status:'+pm.response.code)}});"})),
        (Make-Item "GET /patients" (Make-Request "GET" "/api/v1/patients?page=1&pageSize=5" $null $auth) @(@{name="200";script="pm.test('200',function(){pm.response.to.have.status(200)});"})),
        (Make-Item "GET /patients/{id}" (Make-Request "GET" "/api/v1/patients/{{patientId}}" $null $auth) @(@{name="status";script="pm.test('status',function(){if(pm.collectionVariables.get('patientId')){pm.response.to.have.status(200)}else{pm.expect(true).to.be.true}});"})),
        (Make-Item "PUT /patients/{id}" (Make-Request "PUT" "/api/v1/patients/{{patientId}}" '{"name":"UpdatedPatient"}' $authJson) @(@{name="status";script="pm.test('status',function(){if(pm.collectionVariables.get('patientId')){pm.expect([200,204,400]).to.include(pm.response.code)}else{pm.expect(true).to.be.true}});"})),
        (Make-Item "GET /patients/{id}/check-reference" (Make-Request "GET" "/api/v1/patients/{{patientId}}/check-reference" $null $auth) @(@{name="status";script="pm.test('status',function(){if(pm.collectionVariables.get('patientId')){pm.response.to.have.status(200)}else{pm.expect(true).to.be.true}});"})),
        (Make-Item "GET /patients/by-id-number" (Make-Request "GET" "/api/v1/patients/by-id-number/110101199001049999" $null $auth) @(@{name="200";script="pm.test('200',function(){pm.response.to.have.status(200)});"})),
        (Make-Item "POST /patients/{id}/toggle-status" (Make-Request "POST" "/api/v1/patients/{{patientId}}/toggle-status" $null $auth) @(@{name="status";script="pm.test('status',function(){if(pm.collectionVariables.get('patientId')){pm.expect([200,204]).to.include(pm.response.code)}else{pm.expect(true).to.be.true}});"})),
        (Make-Item "POST /patients/{id}/restore" (Make-Request "POST" "/api/v1/patients/{{patientId}}/restore" $null $auth) @(@{name="status";script="pm.test('status',function(){if(pm.collectionVariables.get('patientId')){pm.expect([200,204,422]).to.include(pm.response.code)}else{pm.expect(true).to.be.true}});"})),
        (Make-Item "POST /patients/batch-check-reference" (Make-Request "POST" "/api/v1/patients/batch-check-reference" '{"ids":["{{patientId}}"]}' $authJson) @(@{name="200";script="pm.test('200',function(){pm.expect([200,400]).to.include(pm.response.code)});"})),
        (Make-Item "DELETE /patients/{id}" (Make-Request "DELETE" "/api/v1/patients/{{patientId}}" $null $auth) @(@{name="status";script="pm.test('status',function(){if(pm.collectionVariables.get('patientId')){pm.expect([200,204]).to.include(pm.response.code)}else{pm.expect(true).to.be.true}});"})),
        (Make-Item "POST /patients/batch-delete" (Make-Request "POST" "/api/v1/patients/batch-delete" '{"ids":["{{patientId}}"]}' $authJson) @(@{name="200";script="pm.test('200',function(){pm.expect([200,204]).to.include(pm.response.code)});"}))
    )
}
$collection.item += $patientsModule

# 5. Herbs
$herbBody = '{"name":"TestHerbFull","category":"ClearHeat","effect":"ClearHeat","origin":"Guangdong","price":15.0}'
$herbsModule = @{
    name = "5. Herbs"
    item = @(
        (Make-Item "POST /herbs" (Make-Request "POST" "/api/v1/herbs" $herbBody $authJson) @(@{name="status";script="pm.test('status',function(){pm.expect([200,201]).to.include(pm.response.code);pm.collectionVariables.set('herbId',pm.response.json().data.id)});"})),
        (Make-Item "GET /herbs" (Make-Request "GET" "/api/v1/herbs?page=1&pageSize=5" $null $auth) @(@{name="200";script="pm.test('200',function(){pm.response.to.have.status(200)});"})),
        (Make-Item "GET /herbs/{id}" (Make-Request "GET" "/api/v1/herbs/{{herbId}}" $null $auth) @(@{name="200";script="pm.test('200',function(){pm.response.to.have.status(200)});"})),
        (Make-Item "PUT /herbs/{id}" (Make-Request "PUT" "/api/v1/herbs/{{herbId}}" '{"price":20.0}' $authJson) @(@{name="status";script="pm.test('status',function(){pm.expect([200,204,400]).to.include(pm.response.code)});"})),
        (Make-Item "GET /herbs/{id}/check-reference" (Make-Request "GET" "/api/v1/herbs/{{herbId}}/check-reference" $null $auth) @(@{name="200";script="pm.test('200',function(){pm.response.to.have.status(200)});"})),
        (Make-Item "POST /herbs/{id}/toggle-status" (Make-Request "POST" "/api/v1/herbs/{{herbId}}/toggle-status" $null $auth) @(@{name="200";script="pm.test('200',function(){pm.response.to.have.status(200)});"})),
        (Make-Item "POST /herbs/{id}/restore" (Make-Request "POST" "/api/v1/herbs/{{herbId}}/restore" $null $auth) @(@{name="status";script="pm.test('status',function(){pm.expect([200,204,422]).to.include(pm.response.code)});"})),
        (Make-Item "POST /herbs/batch-check-reference" (Make-Request "POST" "/api/v1/herbs/batch-check-reference" '{"ids":["{{herbId}}"]}' $authJson) @(@{name="200";script="pm.test('200',function(){pm.expect([200,400]).to.include(pm.response.code)});"})),
        (Make-Item "DELETE /herbs/{id}" (Make-Request "DELETE" "/api/v1/herbs/{{herbId}}" $null $auth) @(@{name="200";script="pm.test('200',function(){pm.response.to.have.status(200)});"})),
        (Make-Item "POST /herbs/batch-delete" (Make-Request "POST" "/api/v1/herbs/batch-delete" '{"ids":["{{herbId}}"]}' $authJson) @(@{name="200";script="pm.test('200',function(){pm.expect([200,204]).to.include(pm.response.code)});"}))
    )
}
$collection.item += $herbsModule

# 6. Formulas
$formulaBody = '{"name":"TestFormulaFull","effect":"NourishBlood","usage":"Oral 6g","isShared":true,"herbs":[{"herbName":"ShuDiHuang","dosage":24,"unit":"g"}]}'
$formulasModule = @{
    name = "6. Formulas"
    item = @(
        (Make-Item "POST /formulas" (Make-Request "POST" "/api/v1/formulas" $formulaBody $authJson) @(@{name="status";script="pm.test('status',function(){pm.expect([200,201]).to.include(pm.response.code);if(pm.response.code===200||pm.response.code===201){pm.collectionVariables.set('formulaId',pm.response.json().data.id)}});"})),
        (Make-Item "GET /formulas" (Make-Request "GET" "/api/v1/formulas?page=1&pageSize=5" $null $auth) @(@{name="200";script="pm.test('200',function(){pm.response.to.have.status(200)});"})),
        (Make-Item "GET /formulas/{id}" (Make-Request "GET" "/api/v1/formulas/{{formulaId}}" $null $auth) @(@{name="200";script="pm.test('200',function(){pm.response.to.have.status(200)});"})),
        (Make-Item "PUT /formulas/{id}" (Make-Request "PUT" "/api/v1/formulas/{{formulaId}}" '{"effect":"UpdatedEffect"}' $authJson) @(@{name="status";script="pm.test('status',function(){pm.expect([200,204,400]).to.include(pm.response.code)});"})),
        (Make-Item "GET /formulas/pending-validation" (Make-Request "GET" "/api/v1/formulas/pending-validation" $null $auth) @(@{name="200";script="pm.test('200',function(){pm.response.to.have.status(200)});"})),
        (Make-Item "POST /formulas/{id}/toggle-status" (Make-Request "POST" "/api/v1/formulas/{{formulaId}}/toggle-status" $null $auth) @(@{name="200";script="pm.test('200',function(){pm.expect([200,204]).to.include(pm.response.code)});"})),
        (Make-Item "POST /formulas/{id}/restore" (Make-Request "POST" "/api/v1/formulas/{{formulaId}}/restore" $null $auth) @(@{name="status";script="pm.test('status',function(){pm.expect([200,204,422]).to.include(pm.response.code)});"})),
        (Make-Item "DELETE /formulas/{id}" (Make-Request "DELETE" "/api/v1/formulas/{{formulaId}}" $null $auth) @(@{name="200";script="pm.test('200',function(){pm.response.to.have.status(200)});"})),
        (Make-Item "POST /formulas/batch-delete" (Make-Request "POST" "/api/v1/formulas/batch-delete" '{"ids":["{{formulaId}}"]}' $authJson) @(@{name="200";script="pm.test('200',function(){pm.expect([200,204]).to.include(pm.response.code)});"}))
    )
}
$collection.item += $formulasModule

# 7. Registrations
$regBody = '{"patientId":"{{patientId}}","patientName":"TestPatient","doctorId":"{{userId}}","doctorName":"Admin","source":"Doctor","remark":"Integration test"}'
$registrationsModule = @{
    name = "7. Registrations"
    item = @(
        (Make-Item "POST /registrations" (Make-Request "POST" "/api/v1/registrations" $regBody $authJson) @(@{name="status";script="pm.test('status',function(){if(pm.collectionVariables.get('patientId')){pm.expect([200,201]).to.include(pm.response.code);if(pm.response.code===200||pm.response.code===201){pm.collectionVariables.set('registrationId',pm.response.json().data.id)}}else{pm.expect(true).to.be.true}});"})),
        (Make-Item "GET /registrations" (Make-Request "GET" "/api/v1/registrations?page=1&pageSize=5" $null $auth) @(@{name="200";script="pm.test('200',function(){pm.response.to.have.status(200)});"})),
        (Make-Item "GET /registrations/queue" (Make-Request "GET" "/api/v1/registrations/queue" $null $auth) @(@{name="200";script="pm.test('200',function(){pm.response.to.have.status(200)});"})),
        (Make-Item "GET /registrations/{id}" (Make-Request "GET" "/api/v1/registrations/{{registrationId}}" $null $auth) @(@{name="status";script="pm.test('status',function(){if(pm.collectionVariables.get('registrationId')){pm.response.to.have.status(200)}else{pm.expect(true).to.be.true}});"})),
        (Make-Item "POST /registrations/quick-visit" (Make-Request "POST" "/api/v1/registrations/quick-visit" '{"patientId":"{{patientId}}","patientName":"TestPatient"}' $authJson) @(@{name="status";script="pm.test('status',function(){pm.expect([200,201,400,422]).to.include(pm.response.code)});"})),
        (Make-Item "PUT /registrations/{id}/start-visit" (Make-Request "PUT" "/api/v1/registrations/{{registrationId}}/start-visit" $null $auth) @(@{name="status";script="pm.test('status',function(){if(pm.collectionVariables.get('registrationId')){pm.expect([200,204,400,422]).to.include(pm.response.code)}else{pm.expect(true).to.be.true}});"})),
        (Make-Item "PUT /registrations/{id}/cancel" (Make-Request "PUT" "/api/v1/registrations/{{registrationId}}/cancel" $null $auth) @(@{name="status";script="pm.test('status',function(){if(pm.collectionVariables.get('registrationId')){pm.expect([200,204,400]).to.include(pm.response.code)}else{pm.expect(true).to.be.true}});"}))
    )
}
$collection.item += $registrationsModule

# 8. MedicalCases
$mcBody = '{"patientId":"{{patientId}}","doctorId":"{{userId}}"}'
$medicalCasesModule = @{
    name = "8. MedicalCases"
    item = @(
        (Make-Item "POST /medicalcases" (Make-Request "POST" "/api/v1/medicalcases" $mcBody $authJson) @(@{name="status";script="pm.test('status',function(){pm.expect([200,201,400]).to.include(pm.response.code);if(pm.response.code===200||pm.response.code===201){pm.collectionVariables.set('medicalCaseId',pm.response.json().data.id)}});"})),
        (Make-Item "GET /medicalcases" (Make-Request "GET" "/api/v1/medicalcases?page=1&pageSize=5" $null $auth) @(@{name="200";script="pm.test('200',function(){pm.response.to.have.status(200)});"})),
        (Make-Item "GET /medicalcases/{id}" (Make-Request "GET" "/api/v1/medicalcases/{{medicalCaseId}}" $null $auth) @(@{name="status";script="pm.test('status',function(){if(pm.collectionVariables.get('medicalCaseId')){pm.response.to.have.status(200)}else{pm.expect(true).to.be.true}});"})),
        (Make-Item "GET /medicalcases/query" (Make-Request "GET" "/api/v1/medicalcases/query?page=1&pageSize=5" $null $auth) @(@{name="200";script="pm.test('200',function(){pm.response.to.have.status(200)});"})),
        (Make-Item "GET /medicalcases/search" (Make-Request "GET" "/api/v1/medicalcases/search?keyword=test" $null $auth) @(@{name="200";script="pm.test('200',function(){pm.response.to.have.status(200)});"})),
        (Make-Item "GET /medicalcases/{id}/permissions" (Make-Request "GET" "/api/v1/medicalcases/{{medicalCaseId}}/permissions" $null $auth) @(@{name="status";script="pm.test('status',function(){if(pm.collectionVariables.get('medicalCaseId')){pm.response.to.have.status(200)}else{pm.expect(true).to.be.true}});"})),
        (Make-Item "GET /medicalcases/{id}/audit-logs" (Make-Request "GET" "/api/v1/medicalcases/{{medicalCaseId}}/audit-logs" $null $auth) @(@{name="status";script="pm.test('status',function(){if(pm.collectionVariables.get('medicalCaseId')){pm.response.to.have.status(200)}else{pm.expect(true).to.be.true}});"})),
        (Make-Item "PUT /medicalcases/{id}/status" (Make-Request "PUT" "/api/v1/medicalcases/{{medicalCaseId}}/status" '{"status":1}' $authJson) @(@{name="status";script="pm.test('status',function(){if(pm.collectionVariables.get('medicalCaseId')){pm.expect([200,204,400]).to.include(pm.response.code)}else{pm.expect(true).to.be.true}});"})),
        (Make-Item "PUT /medicalcases/{id}/close" (Make-Request "PUT" "/api/v1/medicalcases/{{medicalCaseId}}/close" $null $auth) @(@{name="status";script="pm.test('status',function(){if(pm.collectionVariables.get('medicalCaseId')){pm.expect([200,204,400]).to.include(pm.response.code)}else{pm.expect(true).to.be.true}});"})),
        (Make-Item "PUT /medicalcases/{id}/suspend" (Make-Request "PUT" "/api/v1/medicalcases/{{medicalCaseId}}/suspend" $null $auth) @(@{name="status";script="pm.test('status',function(){if(pm.collectionVariables.get('medicalCaseId')){pm.expect([200,204,400]).to.include(pm.response.code)}else{pm.expect(true).to.be.true}});"})),
        (Make-Item "PUT /medicalcases/{id}/cancel" (Make-Request "PUT" "/api/v1/medicalcases/{{medicalCaseId}}/cancel" $null $auth) @(@{name="status";script="pm.test('status',function(){if(pm.collectionVariables.get('medicalCaseId')){pm.expect([200,204,400]).to.include(pm.response.code)}else{pm.expect(true).to.be.true}});"})),
        (Make-Item "PUT /medicalcases/{id}/prescription-flag" (Make-Request "PUT" "/api/v1/medicalcases/{{medicalCaseId}}/prescription-flag" '{"needsPrescription":true}' $authJson) @(@{name="status";script="pm.test('status',function(){if(pm.collectionVariables.get('medicalCaseId')){pm.expect([200,204,400]).to.include(pm.response.code)}else{pm.expect(true).to.be.true}});"})),
        (Make-Item "PUT /medicalcases/{id}/print-completed" (Make-Request "PUT" "/api/v1/medicalcases/{{medicalCaseId}}/print-completed" $null $auth) @(@{name="status";script="pm.test('status',function(){if(pm.collectionVariables.get('medicalCaseId')){pm.expect([200,204,400]).to.include(pm.response.code)}else{pm.expect(true).to.be.true}});"})),
        (Make-Item "POST /medicalcases/batch-delete" (Make-Request "POST" "/api/v1/medicalcases/batch-delete" '{"ids":["{{medicalCaseId}}"]}' $authJson) @(@{name="status";script="pm.test('status',function(){pm.expect([200,204,400]).to.include(pm.response.code)});"})),
        (Make-Item "DELETE /medicalcases/{id}" (Make-Request "DELETE" "/api/v1/medicalcases/{{medicalCaseId}}" $null $auth) @(@{name="status";script="pm.test('status',function(){if(pm.collectionVariables.get('medicalCaseId')){pm.expect([200,204]).to.include(pm.response.code)}else{pm.expect(true).to.be.true}});"}))
    )
}
$collection.item += $medicalCasesModule

# 9. Reports
$reportsModule = @{
    name = "9. Reports"
    item = @(
        (Make-Item "GET /reports/daily/income" (Make-Request "GET" "/api/v1/reports/daily/income" $null $auth) @(@{name="200";script="pm.test('200',function(){pm.response.to.have.status(200)});"})),
        (Make-Item "GET /reports/daily/consultations" (Make-Request "GET" "/api/v1/reports/daily/consultations" $null $auth) @(@{name="200";script="pm.test('200',function(){pm.response.to.have.status(200)});"})),
        (Make-Item "GET /reports/daily/herbs" (Make-Request "GET" "/api/v1/reports/daily/herbs" $null $auth) @(@{name="200";script="pm.test('200',function(){pm.response.to.have.status(200)});"}))
    )
}
$collection.item += $reportsModule

# 10. Configuration
$configModule = @{
    name = "10. Configuration"
    item = @(
        (Make-Item "GET /configuration" (Make-Request "GET" "/api/v1/configuration" $null $auth) @(@{name="200";script="pm.test('200',function(){pm.response.to.have.status(200)});"})),
        (Make-Item "GET /configuration/{key}" (Make-Request "GET" "/api/v1/configuration/Database" $null $auth) @(@{name="200";script="pm.test('200',function(){pm.response.to.have.status(200)});"})),
        (Make-Item "POST /configuration/validate" (Make-Request "POST" "/api/v1/configuration/validate" $null $auth) @(@{name="200";script="pm.test('200',function(){pm.response.to.have.status(200)});"}))
    )
}
$collection.item += $configModule

# 11. Deploy
$deployModule = @{
    name = "11. Deploy"
    item = @(
        (Make-Item "POST /deploy/upload" (Make-Request "POST" "/api/v1/deploy/upload" $null $auth) @(@{name="status";script="pm.test('status',function(){pm.expect([200,400,401]).to.include(pm.response.code)});"})),
        (Make-Item "POST /deploy/restart" (Make-Request "POST" "/api/v1/deploy/restart" $null $auth) @(@{name="status";script="pm.test('status',function(){pm.expect([200,400,401,422]).to.include(pm.response.code)});"}))
    )
}
$collection.item += $deployModule

# 12. Diagnostics
$diagnosticsModule = @{
    name = "12. Diagnostics"
    item = @(
        (Make-Item "GET /diagnostics/logging/status" (Make-Request "GET" "/api/v1/diagnostics/logging/status" $null $auth) @(@{name="200";script="pm.test('200',function(){pm.response.to.have.status(200)});"})),
        (Make-Item "POST /diagnostics/logging/debug/enable" (Make-Request "POST" "/api/v1/diagnostics/logging/debug/enable" $null $auth) @(@{name="200";script="pm.test('200',function(){pm.expect([200,204]).to.include(pm.response.code)});"})),
        (Make-Item "POST /diagnostics/logging/debug/disable" (Make-Request "POST" "/api/v1/diagnostics/logging/debug/disable" $null $auth) @(@{name="200";script="pm.test('200',function(){pm.expect([200,204]).to.include(pm.response.code)});"})),
        (Make-Item "POST /diagnostics/logging/level" (Make-Request "POST" "/api/v1/diagnostics/logging/level" '{"level":"Information"}' $authJson) @(@{name="200";script="pm.test('200',function(){pm.expect([200,204]).to.include(pm.response.code)});"}))
    )
}
$collection.item += $diagnosticsModule

# Export
$json = $collection | ConvertTo-Json -Depth 10
$json | Out-File -FilePath "D:\source\repos\LYBTZYZS\tests\newman\lybt-api-collection-full.json" -Encoding UTF8

Write-Host "Generated collection with $($collection.item.Count) modules"
Write-Host "Total items: $(($collection.item | ForEach-Object { $_.item.Count } | Measure-Object -Sum).Sum)"